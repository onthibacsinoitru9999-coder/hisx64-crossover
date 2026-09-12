using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Inventec.Core;
using Inventec.Token.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.LocalStorage.LocalData;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string[] searchPaths = new string[]
            {
                Path.Combine(folderPath, name),
                Path.Combine(folderPath, "ReferencedAssemblies", name),
                Path.Combine(folderPath, "Plugins", "Module", name),
                Path.Combine(folderPath, "HisAutoPrescribe_Portable", name),
                Path.Combine(Environment.CurrentDirectory, name),
                Path.Combine(Environment.CurrentDirectory, "ReferencedAssemblies", name),
                Path.Combine(Environment.CurrentDirectory, "Plugins", "Module", name)
            };

            foreach (var p in searchPaths)
            {
                if (File.Exists(p))
                {
                    try { return Assembly.LoadFrom(p); } catch { }
                }
            }
            return null;
        };

        Execute(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Execute(string[] args)
    {
        HisDressingOrder.Run(args);
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class HisDressingOrder
{
    static MyAdapter adapter = new MyAdapter();
    static CommonParam commonParam = new CommonParam();

    public const long MEDI_STOCK_ID_TU_TRUC_57 = 810; // Kho Tủ Trực Khoa CTCH & CS
    public const long MEDICINE_TYPE_ID_POVIDONE = 17385; // Povidone 10% 125ml
    public const long MEDICINE_TYPE_ID_MUOI_RUA = 27127; // Muối rửa_Natri clorid 0,9% 500ml

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("==========================================================================");
        Console.WriteLine("   🏥 HIS DRESSING ORDER - CHỈ ĐỊNH VẬT TƯ TIÊU HAO THAY BĂNG 1-CLICK    ");
        Console.WriteLine("==========================================================================");

        string treatmentCode = "";
        string customContent = "Vết mổ khô sạch, thay băng rửa vết thương hàng ngày.";
        string dateStr = DateTime.Now.ToString("yyyyMMdd");
        decimal povidoneQty = 1m;
        decimal salineQty = 1m;
        string loginName = "034727";
        string userName = "NGUYỄN HỮU SÂM";
        long departmentId = 57;
        long workingRoomId = 5248; // Phòng trực 734 CTCH

        for (int i = 0; i < args.Length; i++)
        {
            string k = args[i].ToLower();
            if ((k == "-t" || k == "--treatment" || k == "-p" || k == "--patient") && i + 1 < args.Length)
                treatmentCode = args[++i];
            else if ((k == "-d" || k == "--date") && i + 1 < args.Length)
                dateStr = args[++i].Replace("-", "").Replace("/", "");
            else if ((k == "-c" || k == "--content") && i + 1 < args.Length)
                customContent = args[++i];
            else if (k == "--povidone" && i + 1 < args.Length)
                povidoneQty = decimal.Parse(args[++i]);
            else if (k == "--saline" && i + 1 < args.Length)
                salineQty = decimal.Parse(args[++i]);
        }

        if (string.IsNullOrEmpty(treatmentCode))
        {
            Console.WriteLine("HƯỚNG DẪN SỬ DỤNG:");
            Console.WriteLine("  HisDressingOrder.exe -t <mã_bệnh_án_hoặc_mã_điều_trị> [-c \"nhận xét vết thương\"]");
            Console.WriteLine("\nVÍ DỤ THỰC TẾ:");
            Console.WriteLine("  1. Kê thay băng cho bệnh nhân hôm nay:");
            Console.WriteLine("     HisDressingOrder.exe -t 000007092253");
            Console.WriteLine("  2. Kê thay băng kèm nhận xét vết thương tùy biến:");
            Console.WriteLine("     HisDressingOrder.exe -t 000007092253 -c \"Vết lóc da mu chân khô, thay băng rửa sát khuẩn hàng ngày.\"");
            Console.WriteLine("  3. Kê ngày cụ thể:");
            Console.WriteLine("     HisDressingOrder.exe -t 000007092253 -d 2026-08-26");
            return;
        }

        // 1. Init HIS Config & Login / Read Token
        try
        {
            HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        }
        catch { }

        string tokenCode = GetLiveToken();
        if (string.IsNullOrEmpty(tokenCode))
        {
            Console.WriteLine("[INFO] Đang đăng nhập tự động lấy Token...");
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            var token = tokenManager.Login(commonParam, "034727", "998199", "2.390.0");
            if (token != null && !string.IsNullOrEmpty(token.TokenCode))
            {
                tokenCode = token.TokenCode;
                Console.WriteLine("[SUCCESS] Đăng nhập thành công, Token: " + tokenCode.Substring(0, 8) + "...");
            }
            else
            {
                Console.WriteLine("[ERROR] Đăng nhập thất bại!");
                return;
            }
        }
        else
        {
            Console.WriteLine("[SUCCESS] Sử dụng Live Token: " + tokenCode.Substring(0, 8) + "...");
        }

        ApiConsumers.SetConsunmer(tokenCode);

        // Kích hoạt WorkInfo phòng làm việc Khoa 57
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 }, // Phòng 734
                    new RoomSDO { RoomId = 5252 }, // Phòng 712
                    new RoomSDO { RoomId = 5251 }, // Phòng 714
                    new RoomSDO { RoomId = 5257 }  // Phòng 724
                }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, commonParam);
        }
        catch { }

        // 2. Tra cứu đợt điều trị của bệnh nhân
        Console.WriteLine("[INFO] Đang tra cứu hồ sơ điều trị: " + treatmentCode);
        var treatmentFilter = new HisTreatmentViewFilter
        {
            TREATMENT_CODE__EXACT = treatmentCode.PadLeft(12, '0')
        };
        var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, treatmentFilter, commonParam);
        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine("[ERROR] Không tìm thấy hồ sơ điều trị với mã: " + treatmentCode);
            return;
        }

        var tm = treatments[0];
        long treatmentId = tm.ID;
        long patientTypeId = tm.TDL_PATIENT_TYPE_ID ?? 1;

        Console.WriteLine("--------------------------------------------------------------------------");
        Console.WriteLine(string.Format("BỆNH NHÂN : {0} ({1}) | NĂM SINH: {2}", tm.TDL_PATIENT_NAME, tm.TDL_PATIENT_GENDER_NAME, tm.TDL_PATIENT_DOB.ToString().Substring(0, 4)));
        Console.WriteLine(string.Format("MÃ BỆNH ÁN: {0} | MÃ ĐỢT ĐIỀU TRỊ: {1} | ID: {2}", tm.TREATMENT_CODE, tm.TREATMENT_CODE, tm.ID));
        Console.WriteLine(string.Format("CHẨN ĐOÁN : {0} - {1}", tm.ICD_CODE, tm.ICD_NAME));
        Console.WriteLine("--------------------------------------------------------------------------");

        // 3. BƯỚC 1: XÁC ĐỊNH / TẠO TỜ ĐIỀU TRỊ (HIS_TRACKING)
        long trackingDateNum = long.Parse(dateStr + "080000");
        long trackingId = 0;

        var trkFilter = new HisTrackingViewFilter
        {
            TREATMENT_ID = treatmentId
        };
        var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, commonParam);
        var todayTrk = trackings != null ? trackings.FirstOrDefault(x => x.TRACKING_TIME.ToString().StartsWith(dateStr)) : null;

        if (todayTrk != null)
        {
            trackingId = todayTrk.ID;
            Console.WriteLine(string.Format("✔ Bước 1: Đã có Tờ điều trị ngày {0} (TrackingID: {1})", dateStr, trackingId));
        }
        else
        {
            Console.WriteLine(string.Format("• Bước 1: Đang tạo Tờ điều trị ngày {0} kèm nhận xét thay băng...", dateStr));
            var newTracking = new HIS_TRACKING
            {
                TREATMENT_ID = treatmentId,
                DEPARTMENT_ID = departmentId,
                ROOM_ID = workingRoomId,
                TRACKING_TIME = trackingDateNum,
                CONTENT = customContent,
                MEDICAL_INSTRUCTION = "Chăm sóc cấp II. Thay băng rửa vết thương hàng ngày. Thuốc theo đơn.",
                ICD_CODE = tm.ICD_CODE,
                ICD_NAME = tm.ICD_NAME,
                ICD_SUB_CODE = tm.ICD_SUB_CODE,
                ICD_TEXT = tm.ICD_TEXT
            };

            var sdo = new HisTrackingSDO
            {
                Tracking = newTracking,
                WorkingRoomId = workingRoomId
            };

            var createdTrk = adapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, commonParam);
            if (createdTrk != null && createdTrk.Tracking != null && createdTrk.Tracking.ID > 0)
            {
                trackingId = createdTrk.Tracking.ID;
                Console.WriteLine(string.Format("✔ Tạo Tờ điều trị thành công! (TrackingID: {0})", trackingId));
            }
            else
            {
                Console.WriteLine("❌ Lỗi khi tạo Tờ điều trị!");
                if (commonParam.Messages != null) foreach (var m in commonParam.Messages) Console.WriteLine("  Lỗi: " + m);
                return;
            }
        }

        // 4. BƯỚC 2: KÊ ĐƠN THUỐC TỦ TRỰC CTCH (MediStockId = 810) - TÍCH HAO PHÍ (IsExpend = true)
        Console.WriteLine("\n• Bước 2: Đang kê đơn thuốc Tủ trực CTCH (Povidone + Muối rửa NaCl 0.9% tích Hao phí)...");

        long instructionTime = long.Parse(dateStr + "083000");
        var presDto = new InPatientPresSDO
        {
            TreatmentId = treatmentId,
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            TrackingInfos = trackingId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = instructionTime } } : null,
            InstructionTimes = new List<long> { instructionTime },
            UseTimes = new List<long> { instructionTime },
            RequestRoomId = workingRoomId,
            RequestLoginName = loginName,
            RequestUserName = userName,
            IcdCode = tm.ICD_CODE,
            IcdName = tm.ICD_NAME,
            IcdSubCode = tm.ICD_SUB_CODE,
            IcdText = tm.ICD_TEXT,
            Medicines = new List<PresMedicineSDO>
            {
                // 1. Povidone 10% 125ml
                new PresMedicineSDO
                {
                    MedicineTypeId = MEDICINE_TYPE_ID_POVIDONE,
                    MediStockId = MEDI_STOCK_ID_TU_TRUC_57,
                    Amount = povidoneQty,
                    PatientTypeId = patientTypeId,
                    MedicineUseFormId = 25, // Dùng ngoài
                    Tutorial = "thay băng",
                    IsExpend = true, // BẮT BUỘC: Hao phí tiêu hao
                    NumOfDays = 1
                },
                // 2. Muối rửa_Natri clorid 0,9% 500ml
                new PresMedicineSDO
                {
                    MedicineTypeId = MEDICINE_TYPE_ID_MUOI_RUA,
                    MediStockId = MEDI_STOCK_ID_TU_TRUC_57,
                    Amount = salineQty,
                    PatientTypeId = patientTypeId,
                    MedicineUseFormId = 25, // Dùng ngoài
                    Tutorial = "thay băng",
                    IsExpend = true, // BẮT BUỘC: Hao phí tiêu hao
                    NumOfDays = 1
                }
            }
        };

        CommonParam presParam = new CommonParam();
        var presResult = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, presDto, presParam);

        if (presResult != null && presResult.ExpMests != null && presResult.ExpMests.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================================================");
            Console.WriteLine("✅ KÊ ĐƠN VẬT TƯ TIÊU HAO THAY BĂNG THÀNH CÔNG 100%!");
            Console.WriteLine(string.Format("   - Bệnh nhân   : {0} ({1})", tm.TDL_PATIENT_NAME, tm.TREATMENT_CODE));
            Console.WriteLine(string.Format("   - Tờ điều trị : TrackingID {0} | Ngày {1}", trackingId, dateStr));
            Console.WriteLine(string.Format("   - Kho cấp     : Kho Tủ Trực Khoa 57 (MediStockId: {0})", MEDI_STOCK_ID_TU_TRUC_57));
            Console.WriteLine(string.Format("   - Mục 1       : Povidone 10% 125ml x {0} Chai (Hao phí: Có)", povidoneQty));
            Console.WriteLine(string.Format("   - Mục 2       : Muối rửa Natri clorid 0.9% 500ml x {0} Chai (Hao phí: Có)", salineQty));
            Console.WriteLine("   - Hướng dẫn   : \"thay băng\" | Đường dùng: Dùng ngoài");
            foreach (var exp in presResult.ExpMests)
            {
                Console.WriteLine(string.Format("   - Phiếu xuất  : {0} (ID: {1})", exp.EXP_MEST_CODE, exp.ID));
            }
            Console.WriteLine("   - Ký EMR      : Đã sẵn sàng ký Tờ điều trị (EMR Document Type 7)");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ KÊ ĐƠN THAY BĂNG THẤT BẠI!");
            if (presParam.Messages != null && presParam.Messages.Count > 0)
            {
                foreach (var msg in presParam.Messages) Console.WriteLine("   - Lỗi: " + msg);
            }
            if (presParam.BugCodes != null && presParam.BugCodes.Count > 0)
            {
                foreach (var bug in presParam.BugCodes) Console.WriteLine("   - Bug: " + bug);
            }
            Console.ResetColor();
        }
    }

    static string GetLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";

        // 1. Cache
        foreach (var cf in new[] { Path.Combine(baseDir, "doctor_standalone.token"), Path.Combine(preferredDir, "doctor_standalone.token") })
        {
            try
            {
                if (File.Exists(cf))
                {
                    var parts = File.ReadAllText(cf, Encoding.UTF8).Trim().Split('|');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                    {
                        long ticks = long.Parse(parts[1]);
                        if ((DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromHours(6).Ticks)
                            return parts[0];
                    }
                }
            }
            catch { }
        }

        // 2. Preferred log + parent chain with identity guard
        List<string> candidates = new List<string>();
        candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                string hisPath = procs[0].MainModule.FileName;
                if (hisPath.Contains("LBP2900_R150_V330_W64_uk_EN_2"))
                    candidates.Add(Path.Combine(Path.GetDirectoryName(hisPath), "Logs", "LogSystem.txt"));
            }
        }
        catch { }
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;
                    if (!chunk.Contains("034727") && !chunk.Contains("vmc")) continue;
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                            return chunk.Substring(start, 64);
                    }
                }
            }
            catch { }
        }
        return "";
    }
}
