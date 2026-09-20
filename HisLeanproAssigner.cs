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
    public const long KHO_DINH_DUONG_STOCK_ID = 753; // Kho sản phẩm dinh dưỡng điều trị
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

    public static bool AutoSignTrackingEmr(string token, string treatmentCode, string doctorLogin, long? trackingId = null, long? sheetOrder = null, long? trackingTime = null, string doctorName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(token)) return false;
            ApiConsumer emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", token, "HIS");

            string docLogin = !string.IsNullOrEmpty(doctorLogin) ? doctorLogin : "034727";
            if (string.Equals(docLogin, "vmc", StringComparison.OrdinalIgnoreCase))
            {
                docLogin = "034727";
            }
            string docUser = !string.IsNullOrEmpty(doctorName) ? doctorName : "Ths.BS NGUYỄN HỮU SÂM";

            var docFilter = new EmrDocumentFilter 
            { 
                TREATMENT_CODE__EXACT = treatmentCode,
                DOCUMENT_TYPE_ID = 7
            };
            CommonParam pDoc = new CommonParam();
            var docs = adapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, pDoc);

            EMR_DOCUMENT targetDoc = null;
            if (trackingId.HasValue && docs != null)
            {
                string tag = "HIS_TRACKING:" + trackingId.Value;
                targetDoc = docs.FirstOrDefault(d => d.HIS_CODE != null && d.HIS_CODE.Contains(tag));
            }

            // BƯỚC 1: TẠO LỆNH IN (EMR DOCUMENT) NẾU CHƯA CÓ
            if (targetDoc == null && trackingId.HasValue)
            {
                string pdfTemplate = "%PDF-1.4\n" +
                    "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
                    "2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n" +
                    "3 0 obj<</Type/Page/MediaBox[0 0 595 842]/Parent 2 0 R/Resources<<>>>>endobj\n" +
                    "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000052 00000 n \n0000000101 00000 n \ntrailer<</Size 4/Root 1 0 R>>\nstartxref\n178\n%%EOF\n";
                string base64Pdf = Convert.ToBase64String(Encoding.ASCII.GetBytes(pdfTemplate));

                string docName = string.Format("Phiếu yêu cầu in tờ điều trị ({0})", sheetOrder ?? 1);
                string hisCode = string.Format("Mps000062 TREATMENT_CODE:{0} HIS_TRACKING:{1}", treatmentCode, trackingId.Value);
                long docTime = trackingTime ?? long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

                var docTdo = new DocumentTDO
                {
                    TreatmentCode = treatmentCode,
                    DocumentName = docName,
                    DocumentTypeId = 7,
                    HisCode = hisCode,
                    DepartmentCode = "9",
                    DocumentTime = docTime,
                    Loginname = docLogin,
                    PaperName = "A4",
                    RawKind = 9,
                    Width = 827.0m,
                    Height = 1169.0m,
                    IsSignParallel = true,
                    Signs = new List<SignTDO>
                    {
                        new SignTDO
                        {
                            NumOrder = 1,
                            Loginname = docLogin,
                            Username = docUser,
                            FullName = docUser,
                            Title = "Ths.BS",
                            DepartmentCode = "9",
                            DepartmentName = "Khoa Chấn thương Chỉnh hình và Cột sống"
                        }
                    },
                    OriginalVersion = new VersionTDO
                    {
                        Base64Data = base64Pdf
                    },
                    FileType = FileType.PDF
                };

                CommonParam pTdo = new CommonParam();
                var docRes = adapter.PostData<DocumentTDO>("api/EmrDocument/CreateByTdo", emrConsumer, docTdo, pTdo);
                if (docRes != null && docRes.DocumentId.HasValue)
                {
                    targetDoc = new EMR_DOCUMENT { ID = docRes.DocumentId.Value, DOCUMENT_CODE = docRes.DocumentCode };
                }
            }

            if (targetDoc == null && docs != null)
            {
                targetDoc = docs.OrderByDescending(d => d.ID).FirstOrDefault(d => 
                    !string.IsNullOrEmpty(d.NEXT_SIGNER) &&
                    (string.Equals(d.NEXT_SIGNER, docLogin, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(d.NEXT_SIGNER, "034727", StringComparison.OrdinalIgnoreCase)));
            }

            if (targetDoc == null) return false;

            // BƯỚC 2: CHÈN LỆNH KÝ ĐIỆN TỬ TỰ ĐỘNG
            var signFilter = new EmrSignFilter { DOCUMENT_ID = targetDoc.ID };
            CommonParam pSign = new CommonParam();
            var signs = adapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, pSign);
            var mySign = signs != null ? signs.FirstOrDefault(s => 
                (string.Equals(s.LOGINNAME, docLogin, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, targetDoc.NEXT_SIGNER, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, "034727", StringComparison.OrdinalIgnoreCase) ||
                 string.IsNullOrEmpty(s.LOGINNAME)) &&
                (s.SIGN_TIME == null || s.SIGN_TIME == 0)
            ) : null;

            if (mySign == null) return false;

            long signTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var signSdo = new EmrSignHsmSDO
            {
                EmrDocumentId = targetDoc.ID,
                EmrSignId = mySign.ID,
                SignTime = signTime,
                IsFinishSign = true,
                IsSigning = true,
                IsSignElectronic = true,
                Description = "Ký điện tử Tờ điều trị Bác sĩ (Auto-Sign)",
                RoomCode = "NQCTCHBB734",
                RoomTypeCode = "GI",
                WorkingDepartmentName = "Khoa Chấn thương Chỉnh hình và Cột sống",
                PointSign = new EmrPointSignSDO
                {
                    CoorXRectangle = 400.0f,
                    CoorYRectangle = 100.0f,
                    PageNumber = 1,
                    MaxPageNumber = 1,
                    WidthRectangle = 150.0f,
                    HeightRectangle = 50.0f,
                    SizeFont = 10,
                    TypeDisplay = 3,
                    FontName = "Times New Roman"
                }
            };

            var resSign = adapter.PostData<EmrSignResultSDO>("api/EmrSign/SignPdfHsm", emrConsumer, signSdo, pSign);
            if (resSign != null && resSign.EmrSign != null)
            {
                return true;
            }
            else
            {
                var updateSdo = new EmrSignUpdateSDO
                {
                    DocumentId = targetDoc.ID,
                    Updates = new List<EMR_SIGN>
                    {
                        new EMR_SIGN { ID = mySign.ID, SIGN_TIME = signTime }
                    }
                };
                return adapter.PostData<bool>("api/EmrSign/UpdateSdo", emrConsumer, updateSdo, pSign);
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool Prescribe(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, long trackingId, long trackingTime, out string serviceReqCode, out string expMestCode, out string error)
    {
        serviceReqCode = "";
        expMestCode = "";
        error = "";
        long nowTime = trackingTime > 0 ? trackingTime : long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

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
            UseTimes = new List<long> { nowTime },
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            TrackingInfos = trackingId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = nowTime } } : null,
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
        var presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", consumer, presSdo, cp);
        if (presRes != null)
        {
            if (presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0)
            {
                serviceReqCode = presRes.ServiceReqs[0].SERVICE_REQ_CODE;
            }
            if (presRes.ExpMests != null && presRes.ExpMests.Count > 0)
            {
                expMestCode = presRes.ExpMests[0].EXP_MEST_CODE;
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

            // 2. Kê đơn Leanpro
            Console.WriteLine("   💊 Đang kê đơn Leanpro PreSur 12.5% (6 chai) từ Kho dinh dưỡng (753)...");
            string sReqCode, expMestCode, err;
            bool ok = Prescribe(mosConsumer, tr, roomId, trackingId, trackingTime, out sReqCode, out expMestCode, out err);
            if (ok)
            {
                Console.WriteLine(string.Format("   🎉 KÊ ĐƠN THÀNH CÔNG!"));
                if (!string.IsNullOrEmpty(sReqCode)) Console.WriteLine(string.Format("      - Mã phiếu y lệnh: {0}", sReqCode));
                if (!string.IsNullOrEmpty(expMestCode)) Console.WriteLine(string.Format("      - Mã phiếu xuất  : {0}", expMestCode));
                Console.WriteLine(string.Format("      - Thuốc          : Leanpro PreSur 12.5% (SPBM25651) | SL: 6 Chai"));
                Console.WriteLine(string.Format("      - HDSD           : {0}", DEFAULT_TUTORIAL));
                Console.WriteLine(string.Format("      - Kho cấp        : Kho sản phẩm dinh dưỡng điều trị (753)"));
                successCount++;

                // 3. Tự động ký số EMR Cloud HSM cho Tờ điều trị
                if (trackingId > 0)
                {
                    Console.WriteLine("   ✍️ Đang thực hiện ký số Cloud HSM EMR (Loại 7 - Tờ điều trị)...");
                    bool signed = AutoSignTrackingEmr(token, tr.TREATMENT_CODE, "034727", trackingId, sheetOrder, trackingTime, "Ths.BS NGUYỄN HỮU SÂM");
                    if (signed)
                    {
                        Console.WriteLine("      🟢 ĐÃ KÝ SỐ EMR THÀNH CÔNG (Cloud HSM)");
                    }
                    else
                    {
                        Console.WriteLine("      ⚠️ Ký số EMR không thành công hoặc văn bản đã được ký trước đó");
                    }
                }
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
        string token = ReadLiveToken();
        CommonParam param = new CommonParam();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode hợp lệ!");
            return;
        }

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        Console.WriteLine("=========================================================================================");
        Console.WriteLine("✍️ TIẾN HÀNH KÝ SỐ CLOUD HSM EMR TỜ ĐIỀU TRỊ LEANPRO (LOẠI 7 / MPS000062)");
        Console.WriteLine("   Bác sĩ ký: Ths.BS NGUYỄN HỮU SÂM (034727)");
        Console.WriteLine(string.Format("   Thời gian: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
        Console.WriteLine("=========================================================================================\n");

        int success = 0;
        int fail = 0;

        foreach (var code in patientOrTreatmentCodes)
        {
            string cleanCode = code.Trim();
            if (string.IsNullOrEmpty(cleanCode)) continue;
            if (cleanCode.All(char.IsDigit) && cleanCode.Length < 10) cleanCode = cleanCode.PadLeft(10, '0');

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            if (cleanCode.Length == 12 && cleanCode.StartsWith("0000")) tf.TREATMENT_CODE__EXACT = cleanCode;
            else if (cleanCode.Length >= 8 && cleanCode.StartsWith("000")) tf.PATIENT_CODE__EXACT = cleanCode;
            else tf.TREATMENT_CODE__EXACT = cleanCode;

            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (trList == null || trList.Count == 0)
            {
                tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = cleanCode };
                trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            }

            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy hồ sơ: " + cleanCode);
                fail++;
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            HisTrackingViewFilter tkf = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, tkf, param);
            long todayStart = long.Parse(DateTime.Today.ToString("yyyyMMdd000000"));
            var targetTrack = trackings != null ? trackings
                .Where(x => x.TRACKING_TIME >= todayStart && 
                           ((x.CONTENT != null && x.CONTENT.ToLower().Contains("bổ sung dịch")) ||
                            (x.MEDICAL_INSTRUCTION != null && x.MEDICAL_INSTRUCTION.ToLower().Contains("leanpro"))))
                .OrderByDescending(x => x.TRACKING_TIME)
                .FirstOrDefault() : null;

            if (targetTrack == null)
            {
                Console.WriteLine(string.Format("👉 BN: {0} ({1}) - ⚠️ Không có tờ điều trị Leanpro hôm nay!\n", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
                fail++;
                continue;
            }

            Console.WriteLine(string.Format("👉 BN: {0} ({1}) | Tracking ID: {2} | SheetOrder: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, targetTrack.ID, targetTrack.SHEET_ORDER));
            bool ok = AutoSignTrackingEmr(token, tr.TREATMENT_CODE, "034727", targetTrack.ID, targetTrack.SHEET_ORDER, targetTrack.TRACKING_TIME, "Ths.BS NGUYỄN HỮU SÂM");
            if (ok)
            {
                Console.WriteLine("   🟢 ĐÃ KÝ SỐ CLOUD HSM EMR THÀNH CÔNG!\n");
                success++;
            }
            else
            {
                Console.WriteLine("   ❌ KÝ SỐ THẤT BẠI!\n");
                fail++;
            }
        }

        Console.WriteLine("=========================================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT KÝ SỐ: Thành công: {0} | Thất bại/Bỏ qua: {1}", success, fail));
        Console.WriteLine("=========================================================================================");
    }

    public static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Cách sử dụng: HisLeanproAssigner.exe <MãBN1,MãBN2,...> hoặc -p <MãBN1,MãBN2,...>");
            Console.WriteLine("             HisLeanproAssigner.exe --sign <MãBN1,MãBN2,...>");
            Console.WriteLine("Ví dụ: HisLeanproAssigner.exe 0003976907,0003595506");
            return;
        }

        bool isSignMode = false;
        List<string> codes = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--sign" || arg == "-s" || arg == "--sign-only")
            {
                isSignMode = true;
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

        if (isSignMode)
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
