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
    public const long KHO_DINH_DUONG_STOCK_ID = 753; // Kho sản phẩm dinh dưỡng điều trị
    public const long PATIENT_TYPE_ID_VIEN_PHI = 42; // Viện phí
    public const string DEFAULT_TUTORIAL = "Ngày uống 4 chai buổi tối 20h 2 chai sáng 6h";

    public static MyAdapter adapter = new MyAdapter();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();
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
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            return chunk.Substring(start, 64);
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

    public static long CreateTracking(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, long departmentId = 57)
    {
        long nowTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            TRACKING_TIME = nowTime,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT,
            CONTENT = "bổ sung dịch dinh dưỡng trước mổ",
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
            return res.ID;
        }
        return 0;
    }

    public static bool Prescribe(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, out string expMestCode, out string error)
    {
        expMestCode = "";
        error = "";
        long nowTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        var presSdo = new InPatientPresSDO
        {
            TreatmentId = tr.ID,
            RequestRoomId = roomId,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            PrescriptionTypeId = (PrescriptionType)1, // Đơn nội trú
            InstructionTimes = new List<long> { nowTime },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = LEANPRO_MEDICINE_TYPE_ID,
                    MediStockId = KHO_DINH_DUONG_STOCK_ID,
                    PatientTypeId = PATIENT_TYPE_ID_VIEN_PHI,
                    Amount = 6.0m,
                    Evening = "06",
                    Tutorial = DEFAULT_TUTORIAL,
                    NumOfDays = 1
                }
            }
        };

        CommonParam cp = new CommonParam();
        consumer.Post<object>("api/HisServiceReq/InPatientPresCreate", cp, presSdo, new object[0]);

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
            error = "Không xác nhận được bản ghi đơn thuốc trong cơ sở dữ liệu";
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
                var tok = tm.Login(param, "034727", "9981", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tm.Login(param, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
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
        Console.WriteLine("   - Kho cấp   : Kho sản phẩm dinh dưỡng điều trị (MediStockId: 753)");
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
            Console.WriteLine("   📝 Đang tạo Tờ điều trị 'bổ sung dịch dinh dưỡng trước mổ'...");
            long trackingId = CreateTracking(mosConsumer, tr, roomId, deptId);
            if (trackingId > 0)
            {
                Console.WriteLine(string.Format("   ✔ Đã tạo Tờ điều trị thành công (Tracking ID: {0})", trackingId));
            }
            else
            {
                Console.WriteLine("   ⚠️ Tạo tờ điều trị không thành công, tiếp tục tạo đơn thuốc...");
            }

            // 2. Kê đơn Leanpro
            Console.WriteLine("   💊 Đang kê đơn Leanpro PreSur 12.5% (6 chai) từ Kho dinh dưỡng (753)...");
            string expMestCode, err;
            bool ok = Prescribe(mosConsumer, tr, roomId, out expMestCode, out err);
            if (ok)
            {
                Console.WriteLine(string.Format("   🎉 KÊ ĐƠN THÀNH CÔNG! Mã phiếu xuất: {0}", expMestCode));
                Console.WriteLine(string.Format("      - Thuốc  : Leanpro PreSur 12.5% (SPBM25651) | SL: 6 Chai"));
                Console.WriteLine(string.Format("      - HDSD   : {0}", DEFAULT_TUTORIAL));
                Console.WriteLine(string.Format("      - Kho cấp: Kho sản phẩm dinh dưỡng điều trị (753)"));
                successCount++;
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

    public static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Cách sử dụng: HisLeanproAssigner.exe <MãBN1,MãBN2,...> hoặc -p <MãBN1,MãBN2,...>");
            Console.WriteLine("Ví dụ: HisLeanproAssigner.exe 0003976907,0003595506");
            return;
        }

        List<string> codes = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "-p" || arg == "--patients" || arg == "-t" || arg == "--treatments")
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

        ProcessPatients(codes);
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
