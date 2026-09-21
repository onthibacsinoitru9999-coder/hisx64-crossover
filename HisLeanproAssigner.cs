using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;
using EMR.EFMODEL.DataModels;
using EMR.Filter;
using EMR.SDO;
using EMR.TDO;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class HisLeanproAssigner
{
    public const long LEANPRO_MEDICINE_TYPE_ID = 26851;
    public const string LEANPRO_MEDICINE_TYPE_CODE = "SPBM25651";
    public const long KHO_DINH_DUONG_STOCK_ID = 7787; // TTSPDD_9 - Tủ trực Sản phẩm dinh dưỡng - Khoa Chấn thương Chỉnh hình và Cột sống
    public const long PATIENT_TYPE_ID_VIEN_PHI = 42; // Viện phí
    public const string DEFAULT_TUTORIAL = "Ngày uống 4 chai buổi tối 20h 2 chai sáng 6h";

    public static MyAdapter adapter = new MyAdapter();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
            if (!File.Exists(cacheFile))
            {
                string alt1 = Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_standalone.token");
                if (File.Exists(alt1)) cacheFile = alt1;
                else
                {
                    string alt2 = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
                    if (File.Exists(alt2)) cacheFile = alt2;
                }
            }
            if (File.Exists(cacheFile))
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2)
                {
                    long savedTime;
                    if (long.TryParse(parts[1], out savedTime))
                    {
                        DateTime savedDt = new DateTime(savedTime);
                        if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                        {
                            return parts[0];
                        }
                    }
                }
            }
        }
        catch { }

        List<string> candidates = new List<string>();
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true"))
                    {
                        continue;
                    }

                    // BẢO VỆ DANH TÍNH: Phải thuộc 034727 hoặc vmc
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string tok = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                return tok;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void UpdateWorkInfo(ApiConsumer consumer, long roomId)
    {
        try
        {
            CommonParam p = new CommonParam();
            var rooms = new List<RoomSDO> { new RoomSDO { RoomId = roomId } };
            if (roomId != 5248) rooms.Add(new RoomSDO { RoomId = 5248 });
            var workInfo = new WorkInfoSDO { Rooms = rooms };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, workInfo, p);
        }
        catch { }
    }

    public static bool CheckEligibility(V_HIS_TREATMENT tr, out int age, out string reason)
    {
        age = 0;
        if (tr == null)
        {
            reason = "Không tìm thấy hồ sơ điều trị";
            return false;
        }

        int yob = 0;
        string dobStr = tr.TDL_PATIENT_DOB.ToString();
        if (dobStr.Length >= 4)
        {
            int.TryParse(dobStr.Substring(0, 4), out yob);
        }
        age = yob > 0 ? (DateTime.Now.Year - yob) : 0;

        // 1. Kiểm tra tuổi: phải dưới 70
        if (age >= 70)
        {
            reason = string.Format("Tuổi {0} >= 70 (Chỉ định Leanpro chỉ áp dụng cho người bệnh < 70 tuổi)", age);
            return false;
        }

        // 2. Kiểm tra bệnh lý Đái tháo đường
        string diag = string.Format("{0} {1} {2} {3}", tr.ICD_CODE, tr.ICD_SUB_CODE, tr.ICD_NAME, tr.ICD_TEXT).ToLower();
        string[] diabetesKeys = new string[] { "tháo đường", "đái đường", "tiểu đường", "diabetes", "đtđ" };
        bool isDiabetes = diabetesKeys.Any(k => diag.Contains(k)) ||
                          (tr.ICD_CODE != null && (tr.ICD_CODE.StartsWith("E10") || tr.ICD_CODE.StartsWith("E11") || tr.ICD_CODE.StartsWith("E12") || tr.ICD_CODE.StartsWith("E13") || tr.ICD_CODE.StartsWith("E14")));

        if (isDiabetes)
        {
            reason = string.Format("Bệnh nhân mắc Đái tháo đường [{0} - {1}] (Chống chỉ định nạp Carbohydrate Leanpro)", tr.ICD_CODE, tr.ICD_NAME);
            return false;
        }

        reason = string.Format("ĐỦ ĐIỀU KIỆN (Tuổi: {0} < 70 | Không ĐTĐ)", age);
        return true;
    }

    public static long CreateTracking(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, long departmentId, out long trackingTime, out long? sheetOrder)
    {
        sheetOrder = null;
        trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        // Kiểm tra xem hôm nay đã có tờ điều trị bổ sung dịch Leanpro chưa, nếu có thì tái sử dụng
        long todayStart = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "000000");
        try
        {
            HisTrackingViewFilter tf = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            CommonParam cpCheck = new CommonParam();
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, tf, cpCheck);
            var existingTrk = trkList != null ? trkList
                .Where(x => x.TRACKING_TIME >= todayStart && 
                           ((x.CONTENT != null && x.CONTENT.ToLower().Contains("bổ sung dịch")) ||
                            (x.MEDICAL_INSTRUCTION != null && x.MEDICAL_INSTRUCTION.ToLower().Contains("leanpro"))))
                .OrderByDescending(x => x.TRACKING_TIME)
                .FirstOrDefault() : null;

            if (existingTrk != null)
            {
                trackingTime = existingTrk.TRACKING_TIME;
                sheetOrder = existingTrk.SHEET_ORDER;
                Console.WriteLine(string.Format("   ℹ️ Đã có Tờ điều trị Leanpro trước đó (ID: {0}, Sheet: {1}) -> Tái sử dụng!", existingTrk.ID, existingTrk.SHEET_ORDER));
                return existingTrk.ID;
            }
        }
        catch { }

        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            TRACKING_TIME = trackingTime,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT,
            CONTENT = "bn lịch mổ mai bổ sung dịch",
            MEDICAL_INSTRUCTION = "Bổ sung dịch dinh dưỡng trước mổ (Leanpro PreSur 12.5% - 6 chai): Uống tối 4 chai lúc 20h, sáng uống 2 chai lúc 6h.",
            DEPARTMENT_ID = departmentId > 0 ? departmentId : 57,
            ROOM_ID = roomId
        };

        var sdo = new HisTrackingSDO
        {
            Tracking = tracking,
            WorkingRoomId = roomId
        };

        CommonParam cp = new CommonParam();
        var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", consumer, sdo, cp);
        if (res != null && res.ID > 0)
        {
            sheetOrder = res.SHEET_ORDER;
            return res.ID;
        }
        return 0;
    }

    public static bool Prescribe(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, long trackingId, long trackingTime, out string serviceReqCode, out string expMestCode, out string error)
    {
        serviceReqCode = "";
        expMestCode = "";
        error = "";
        long nowTime = trackingTime > 0 ? trackingTime : long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        // Lùi thời gian y lệnh 5 phút sau tờ điều trị theo quy tắc an toàn
        long presInstructionTime = nowTime;
        try
        {
            string timeStr = nowTime.ToString();
            if (timeStr.Length == 14)
            {
                int y = int.Parse(timeStr.Substring(0, 4));
                int m = int.Parse(timeStr.Substring(4, 2));
                int d = int.Parse(timeStr.Substring(6, 2));
                int h = int.Parse(timeStr.Substring(8, 2));
                int min = int.Parse(timeStr.Substring(10, 2));
                int s = int.Parse(timeStr.Substring(12, 2));
                DateTime dt = new DateTime(y, m, d, h, min, s);
                presInstructionTime = long.Parse(dt.AddMinutes(5).ToString("yyyyMMddHHmmss"));
            }
        }
        catch { }

        // BƯỚC 1: GIỮ BEAN TỦ TRỰC TTSPDD_9 (STOCK 7787)
        string sessionKey = Guid.NewGuid().ToString();
        var takeBean = new TakeBeanSDO
        {
            TypeId = LEANPRO_MEDICINE_TYPE_ID,
            MediStockId = KHO_DINH_DUONG_STOCK_ID, // 7787 (TTSPDD_9)
            PatientTypeId = PATIENT_TYPE_ID_VIEN_PHI, // 42 (Viện phí)
            Amount = 6.0m,
            ClientSessionKey = sessionKey,
            ExpiredDate = null
        };
        CommonParam cpTake = new CommonParam();
        var beans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
        if (beans == null || beans.Count == 0)
        {
            // Thử lại với PatientTypeId của hồ sơ nếu 42 không khớp cấu hình tủ
            takeBean.PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1;
            beans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
        }

        if (beans == null || beans.Count == 0)
        {
            error = (cpTake.Messages != null && cpTake.Messages.Count > 0) ? string.Join("; ", cpTake.Messages) : "Không giữ được Leanpro trong tủ trực 7787 (TTSPDD_9)";
            if (cpTake.BugCodes != null && cpTake.BugCodes.Count > 0) error += " | BugCodes: " + string.Join("; ", cpTake.BugCodes);
            return false;
        }

        // BƯỚC 2: TẠO Y LỆNH TỦ TRỰC (OutPatientPresCreateList VỚI IsCabinet = true)
        var outPresSDO = new OutPatientPresSDO
        {
            TreatmentId = tr.ID,
            InstructionTime = presInstructionTime,
            UseTimes = new List<long> { presInstructionTime },
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RequestRoomId = roomId > 0 ? roomId : 5248,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            IsCabinet = true,
            ClientSessionKey = sessionKey,
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = LEANPRO_MEDICINE_TYPE_ID,
                    MediStockId = KHO_DINH_DUONG_STOCK_ID,
                    Amount = 6.0m,
                    PresAmount = 6.0m,
                    PatientTypeId = takeBean.PatientTypeId ?? PATIENT_TYPE_ID_VIEN_PHI,
                    Tutorial = DEFAULT_TUTORIAL,
                    MedicineUseFormId = 32, // Uống
                    Evening = "06",
                    NumOfDays = 1,
                    MedicineBeanIds = beans.Select(b => b.ID).ToList()
                }
            }
        };

        CommonParam cp = new CommonParam();
        var outRes = adapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", consumer, new List<OutPatientPresSDO> { outPresSDO }, cp);
        if (outRes != null)
        {
            if (outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0)
            {
                serviceReqCode = outRes.ServiceReqs[0].SERVICE_REQ_CODE;
            }
            if (outRes.ExpMests != null && outRes.ExpMests.Count > 0)
            {
                expMestCode = outRes.ExpMests[0].EXP_MEST_CODE;
            }
            if (!string.IsNullOrEmpty(serviceReqCode) || !string.IsNullOrEmpty(expMestCode))
            {
                return true;
            }
        }

        // Post-verify: Truy vấn DB để lấy chính xác bản ghi vừa tạo
        HisExpMestMedicineViewFilter emmf = new HisExpMestMedicineViewFilter
        {
            TDL_TREATMENT_ID = tr.ID,
            MEDICINE_TYPE_ID = LEANPRO_MEDICINE_TYPE_ID
        };
        var meds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", consumer, emmf, cp);
        if (meds != null && meds.Count > 0)
        {
            var latest = meds.OrderByDescending(x => x.CREATE_TIME).First();
            if (latest.CREATE_TIME >= nowTime - 100)
            {
                expMestCode = latest.EXP_MEST_CODE;
                return true;
            }
        }

        if (cp.Messages != null && cp.Messages.Count > 0)
        {
            error = string.Join("; ", cp.Messages);
        }
        else if (cp.BugCodes != null && cp.BugCodes.Count > 0)
        {
            error = string.Join("; ", cp.BugCodes);
        }
        else
        {
            error = "Không xác nhận được bản ghi đơn thuốc tủ trực trong cơ sở dữ liệu";
        }
        return false;
    }

    public static void ProcessPatients(List<string> patientOrTreatmentCodes)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        CommonParam param = new CommonParam();
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                Load.Init();
                ClientTokenManager tm = new ClientTokenManager("HIS");
                var tok = tm.Login(param, "034727", "998199", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tm.Login(param, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
                }
                if (!string.IsNullOrEmpty(token))
                {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), token + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8); } catch { }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode hợp lệ!");
            return;
        }

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        Console.WriteLine("=========================================================================================");
        Console.WriteLine("🥛 QUY TRÌNH CHỈ ĐỊNH DỊCH DINH DƯỠNG LEANPRO PRESUR 12.5% CHO BỆNH NHÂN TRƯỚC MỔ");
        Console.WriteLine("   - Tiêu chuẩn: BN < 70 tuổi, KHÔNG Đái tháo đường");
        Console.WriteLine("   - Liều dùng : 6 chai (Tối uống 4 chai lúc 20h, sáng uống 2 chai lúc 06h)");
        Console.WriteLine("   - Kho cấp   : Tủ trực Sản phẩm dinh dưỡng Khoa 57 (TTSPDD_9 - MediStockId: 7787)");
        Console.WriteLine("   - Tờ ĐT kèm : 'bổ sung dịch dinh dưỡng trước mổ'");
        Console.WriteLine(string.Format("   - Thời gian : {0} | Bác sĩ: Ths.BS Nguyễn Hữu Sâm (034727)", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
        Console.WriteLine("=========================================================================================\n");

        int successCount = 0;
        int skipCount = 0;

        foreach (var code in patientOrTreatmentCodes)
        {
            string cleanCode = code.Trim();
            if (string.IsNullOrEmpty(cleanCode)) continue;

            if (cleanCode.All(char.IsDigit) && cleanCode.Length < 10)
            {
                cleanCode = cleanCode.PadLeft(10, '0');
            }

            Console.WriteLine(string.Format("🔍 Đang tra cứu hồ sơ: {0}...", cleanCode));

            // Tìm hồ sơ điều trị
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            if (cleanCode.Length == 12 && cleanCode.StartsWith("0000"))
                tf.TREATMENT_CODE__EXACT = cleanCode;
            else if (cleanCode.Length >= 8 && cleanCode.StartsWith("000"))
                tf.PATIENT_CODE__EXACT = cleanCode;
            else
                tf.TREATMENT_CODE__EXACT = cleanCode;

            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (trList == null || trList.Count == 0)
            {
                tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = cleanCode };
                trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            }

            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine(string.Format("   ❌ Không tìm thấy bệnh nhân có mã: {0}\n", cleanCode));
                skipCount++;
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            // Tìm buồng giường hiện tại
            long roomId = 5248; // Mặc định P716
            long deptId = 57;
            string bedName = "Chưa rõ giường";
            string roomName = "Khoa 57";

            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tr.ID, IS_IN_ROOM = true };
            var inBedList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
            if (inBedList != null && inBedList.Count > 0)
            {
                var curBed = inBedList.First();
                bedName = curBed.BED_NAME;
                roomName = curBed.BED_ROOM_NAME;

                HisBedRoomViewFilter brf = new HisBedRoomViewFilter { ID = curBed.BED_ROOM_ID };
                var brList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
                if (brList != null && brList.Count > 0 && brList[0].ROOM_ID > 0)
                {
                    roomId = brList[0].ROOM_ID;
                    deptId = brList[0].DEPARTMENT_ID;
                }
            }

            Console.WriteLine(string.Format("👉 BN: {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            Console.WriteLine(string.Format("   Vị trí: {0} - {1} (RoomId: {2} | DeptId: {3})", roomName, bedName, roomId, deptId));
            Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));

            // Kiểm tra điều kiện
            int age;
            string reason;
            bool eligible = CheckEligibility(tr, out age, out reason);
            if (!eligible)
            {
                Console.WriteLine(string.Format("   ⛔ TỪ CHỐI CHỈ ĐỊNH: {0}\n", reason));
                skipCount++;
                continue;
            }
            Console.WriteLine(string.Format("   ✅ {0}", reason));

            // Kiểm tra đã kê Leanpro hôm nay chưa
            long todayStart = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "000000");
            HisExpMestMedicineViewFilter emmf = new HisExpMestMedicineViewFilter
            {
                TDL_TREATMENT_ID = tr.ID,
                MEDICINE_TYPE_ID = LEANPRO_MEDICINE_TYPE_ID
            };
            var existingMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emmf, param);
            var todayMeds = existingMeds != null ? existingMeds.Where(x => (x.TDL_INTRUCTION_TIME ?? x.CREATE_TIME) >= todayStart).ToList() : null;
            if (todayMeds != null && todayMeds.Count > 0)
            {
                Console.WriteLine(string.Format("   ⚠️ Bệnh nhân ĐÃ CÓ y lệnh Leanpro hôm nay ({0} chai). Bỏ qua để tránh kê trùng.\n", todayMeds.Sum(x => x.AMOUNT)));
                skipCount++;
                continue;
            }

            // Kích hoạt WorkInfo phòng
            UpdateWorkInfo(mosConsumer, roomId);

            // 1. Tạo Tờ điều trị
            Console.WriteLine("   📝 Đang tạo Tờ điều trị: 'bn lịch mổ mai bổ sung dịch'...");
            long trackingTime;
            long? sheetOrder;
            long trackingId = CreateTracking(mosConsumer, tr, roomId, deptId, out trackingTime, out sheetOrder);
            if (trackingId > 0)
            {
                Console.WriteLine(string.Format("   ✔ Đã tạo Tờ điều trị thành công (Tracking ID: {0})", trackingId));
            }
            else
            {
                Console.WriteLine("   ⚠️ Tạo tờ điều trị không thành công, tiếp tục tạo đơn thuốc...");
            }

            // 2. Kê đơn Leanpro từ Tủ trực TTSPDD_9 (7787)
            Console.WriteLine("   💊 Đang kê đơn Leanpro PreSur 12.5% (6 chai) từ Tủ trực TTSPDD_9 (7787)...");
            string sReqCode, expMestCode, err;
            bool ok = Prescribe(mosConsumer, tr, roomId, trackingId, trackingTime, out sReqCode, out expMestCode, out err);
            if (ok)
            {
                Console.WriteLine(string.Format("   🎉 KÊ ĐƠN TỦ TRỰC THÀNH CÔNG!"));
                if (!string.IsNullOrEmpty(sReqCode)) Console.WriteLine(string.Format("      - Mã phiếu y lệnh: {0}", sReqCode));
                if (!string.IsNullOrEmpty(expMestCode)) Console.WriteLine(string.Format("      - Mã phiếu xuất  : {0}", expMestCode));
                Console.WriteLine(string.Format("      - Thuốc          : Leanpro PreSur 12.5% (SPBM25651) | SL: 6 Chai"));
                Console.WriteLine(string.Format("      - HDSD           : {0}", DEFAULT_TUTORIAL));
                Console.WriteLine(string.Format("      - Kho cấp        : Tủ trực Sản phẩm dinh dưỡng Khoa 57 (TTSPDD_9 - ID: 7787)"));
                successCount++;

                Console.WriteLine("      💡 Bác sĩ in & ký Tờ điều trị trực tiếp trên HIS Client Desktop để render đầy đủ dữ liệu mẫu chuẩn.");
            }
            else
            {
                Console.WriteLine(string.Format("   ❌ KÊ ĐƠN THẤT BẠI: {0}", err));
            }
            Console.WriteLine();
        }

        Console.WriteLine("=========================================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT: Hoàn tất {0} bệnh nhân | Bỏ qua/Không đủ điều kiện: {1} bệnh nhân", successCount, skipCount));
        Console.WriteLine("=========================================================================================");
    }

    public static void SignExistingTrackings(List<string> patientOrTreatmentCodes)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("⚠️ Chức năng ký số EMR qua API đã được gỡ bỏ hoàn toàn theo chỉ đạo của Bác sĩ.");
        Console.WriteLine("   Bác sĩ in và ký trực tiếp Tờ điều trị trên phần mềm EMR Desktop Client.");
    }

    public static void DeleteBlankEmrDocs(List<string> docIdsOrCodes)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        CommonParam param = new CommonParam();
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var tk = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (tk == null) tk = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (tk != null) token = tk.TokenCode;
            }
            catch { }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không lấy được token đăng nhập!");
            return;
        }

        ApiConsumer emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", token, "HIS");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("🗑️ BẮT ĐẦU HỦY VĂN BẢN KÝ EMR TRẮNG...");
        Console.WriteLine("=========================================================================================");

        List<long> docIds = new List<long>();
        if (docIdsOrCodes == null || docIdsOrCodes.Count == 0)
        {
            docIds.AddRange(new long[] { 93506998, 93507000, 93507001, 93507003, 93507004, 93507005, 93507006 });
        }
        else
        {
            foreach (var item in docIdsOrCodes)
            {
                long id;
                if (long.TryParse(item, out id) && id > 90000000)
                {
                    docIds.Add(id);
                }
            }
            if (docIds.Count == 0)
            {
                docIds.AddRange(new long[] { 93506998, 93507000, 93507001, 93507003, 93507004, 93507005, 93507006 });
            }
        }

        int success = 0;
        int fail = 0;
        foreach (var docId in docIds)
        {
            try
            {
                // 1. Lấy thông tin chi tiết của Document
                var filter = new EmrDocumentFilter { ID = docId };
                CommonParam cpGet = new CommonParam();
                var docs = adapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, filter, cpGet);
                EMR_DOCUMENT doc = (docs != null && docs.Count > 0) ? docs[0] : null;

                if (doc == null)
                {
                    Console.WriteLine(string.Format("   ℹ️ Văn bản EMR DocID {0} không tồn tại hoặc đã bị xóa trước đó.", docId));
                    success++;
                    continue;
                }

                Console.WriteLine(string.Format("   📄 Đang xử lý EMR DocID {0} ({1})...", docId, doc.DOCUMENT_NAME));

                // 2. Kiểm tra và hủy chữ ký liên kết nếu có
                try
                {
                    var signFilter = new EmrSignFilter { DOCUMENT_ID = docId };
                    CommonParam cpSign = new CommonParam();
                    var signs = adapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, cpSign);
                    if (signs != null && signs.Count > 0)
                    {
                        foreach (var s in signs)
                        {
                            CommonParam cpDelSign = new CommonParam();
                            bool delSignOk = adapter.PostData<bool>("api/EmrSign/Delete", emrConsumer, s, cpDelSign);
                            Console.WriteLine(string.Format("      - Xóa chữ ký SignID {0}: {1}", s.ID, delSignOk ? "OK" : "K/thành công"));
                        }
                    }
                }
                catch { }

                // 3. Xóa Document
                CommonParam cp = new CommonParam();
                bool ok = adapter.PostData<bool>("api/EmrDocument/Delete", emrConsumer, doc, cp);
                if (ok)
                {
                    Console.WriteLine(string.Format("   ✔ Đã xóa thành công văn bản EMR ID {0}!", docId));
                    success++;
                }
                else
                {
                    string msg = (cp.Messages != null && cp.Messages.Count > 0) ? string.Join("; ", cp.Messages) : "";
                    if (cp.BugCodes != null && cp.BugCodes.Count > 0) msg += " | BugCodes: " + string.Join("; ", cp.BugCodes);
                    if (string.IsNullOrEmpty(msg)) msg = "MOS/EMR từ chối (có thể do phân quyền hoặc trạng thái văn bản)";
                    Console.WriteLine(string.Format("   ❌ Không thể xóa EMR ID {0}: {1}", docId, msg));
                    fail++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("   ❌ Lỗi khi xóa EMR ID {0}: {1}", docId, ex.Message));
                fail++;
            }
        }

        Console.WriteLine("=========================================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT XÓA VĂN BẢN EMR: Thành công: {0}/{1} | Thất bại: {2}", success, docIds.Count, fail));
        Console.WriteLine("=========================================================================================\n");
    }

    public static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Cách sử dụng: HisLeanproAssigner.exe <MãBN1,MãBN2,...> hoặc -p <MãBN1,MãBN2,...>");
            Console.WriteLine("             HisLeanproAssigner.exe --delete-emr [DocId1,DocId2,...]");
            Console.WriteLine("Ví dụ: HisLeanproAssigner.exe 0003976907,0003595506");
            return;
        }

        bool isSignMode = false;
        bool isDeleteEmrMode = false;
        List<string> codes = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--sign" || arg == "-s" || arg == "--sign-only")
            {
                isSignMode = true;
            }
            else if (arg == "--delete-emr" || arg == "--del-emr" || arg == "--clean-blank-emr" || arg == "--clean-emr")
            {
                isDeleteEmrMode = true;
            }
            else if (arg == "-p" || arg == "--patients" || arg == "-t" || arg == "--treatments")
            {
                if (i + 1 < args.Length)
                {
                    codes.AddRange(args[i + 1].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    i++;
                }
            }
            else if (!arg.StartsWith("-"))
            {
                codes.AddRange(arg.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        if (isDeleteEmrMode)
        {
            DeleteBlankEmrDocs(codes);
        }
        else if (isSignMode)
        {
            SignExistingTrackings(codes);
        }
        else
        {
            ProcessPatients(codes);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            return null;
        };

        HisLeanproAssigner.Run(args);
    }
}
