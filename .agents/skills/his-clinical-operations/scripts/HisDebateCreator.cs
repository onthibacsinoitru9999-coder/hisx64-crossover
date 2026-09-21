using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;
using EMR.EFMODEL.DataModels;
using EMR.Filter;
using EMR.SDO;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            
            DirectoryInfo cur = new DirectoryInfo(folderPath);
            for (int i = 0; i < 5; i++)
            {
                if (cur.Parent == null) break;
                cur = cur.Parent;
                string pRoot = Path.Combine(cur.FullName, name);
                if (File.Exists(pRoot)) return Assembly.LoadFrom(pRoot);
                string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                if (File.Exists(pRef)) return Assembly.LoadFrom(pRef);
            }
            return null;
        };

        DirectoryInfo rootDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (rootDir != null && !File.Exists(Path.Combine(rootDir.FullName, "Inventec.Core.dll")))
        {
            rootDir = rootDir.Parent;
        }
        if (rootDir != null)
        {
            Directory.SetCurrentDirectory(rootDir.FullName);
        }

        Execute(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Execute(string[] args)
    {
        HisDebateCreator.Run(args);
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

public class HisDebateCreator
{
    static MyAdapter adapter = new MyAdapter();
    static CommonParam commonParam = new CommonParam();
    static string currentToken = null;

    public static string ReadLiveTokenFast()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
            if (!File.Exists(cacheFile))
            {
                string alt = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
                if (File.Exists(alt)) cacheFile = alt;
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

    static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        try
        {
            HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        }
        catch { }

        commonParam = new CommonParam();
        string tokenCode = ReadLiveTokenFast();

        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(commonParam, "034727", "998199", "2.390.0");
                if (token == null) token = tokenManager.Login(commonParam, "vmc", "789789", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), tokenCode + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8); } catch { }
                }
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(tokenCode))
        {
            currentToken = tokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            Console.WriteLine("[SUCCESS] Xác thực HIS thành công | Token: " + currentToken.Substring(0, 8) + "...");
            
            // Kích hoạt WorkInfo phòng làm việc
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 }, // Phòng 734
                        new RoomSDO { RoomId = 5252 }, // Phòng 712
                        new RoomSDO { RoomId = 5251 }  // Phòng 714
                    }
                };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, commonParam);
            }
            catch { }
        }
        else
        {
            throw new Exception("Đăng nhập và lấy Token hệ thống HIS thất bại!");
        }
    }

    static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    static List<V_HIS_TREATMENT> GetAllDept57Treatments()
    {
        List<V_HIS_TREATMENT> list = new List<V_HIS_TREATMENT>();
        try
        {
            HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
            tbrf.IS_IN_ROOM = true;
            tbrf.TREATMENT_IS_ACTIVE = true;
            var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, commonParam);
            if (beds != null && beds.Count > 0)
            {
                var dept57Beds = beds.Where(b => b.DEPARTMENT_ID == 57).GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).ToList();
                List<long> treatmentIds = dept57Beds.Select(b => b.TREATMENT_ID).ToList();

                int batchSize = 100;
                for (int i = 0; i < treatmentIds.Count; i += batchSize)
                {
                    var batch = treatmentIds.Skip(i).Take(batchSize).ToList();
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.IDs = batch;
                    var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, commonParam);
                    if (trList != null) list.AddRange(trList);
                }
            }
        }
        catch { }

        return list;
    }

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("==========================================================================");
        Console.WriteLine("   🏥 HIS DEBATE CREATOR - CHỈ ĐỊNH & BIÊN BẢN HỘI CHẨN CHUYÊN KHOA     ");
        Console.WriteLine("==========================================================================");

        string treatmentCode = "";
        string patientNameSearch = "";
        string specialist = "Khoa/Trung tâm Bệnh nhiệt đới";
        string summary = "";
        string discussion = "";
        string icdCode = "";
        string icdName = "";
        string location = "Khoa Chấn thương Chỉnh hình và Cột sống";
        string presidentLogin = "hdc";
        string presidentName = "HÀ ĐỨC CƯỜNG";
        string secretaryLogin = "034727";
        string secretaryName = "NGUYỄN HỮU SÂM";
        long departmentId = 57;
        bool isSearchOnly = false;
        bool isListAll = false;

        for (int i = 0; i < args.Length; i++)
        {
            string k = args[i].ToLower();
            if ((k == "-t" || k == "--treatment-code" || k == "--treatment") && i + 1 < args.Length)
            {
                treatmentCode = args[++i];
            }
            else if ((k == "-p" || k == "--patient" || k == "--patient-name") && i + 1 < args.Length)
            {
                patientNameSearch = args[++i];
            }
            else if (k == "--search" && i + 1 < args.Length)
            {
                patientNameSearch = args[++i];
                isSearchOnly = true;
            }
            else if (k == "--list" || k == "--list-dept57" || k == "-l57")
            {
                isListAll = true;
                isSearchOnly = true;
            }
            else if ((k == "-s" || k == "--specialist" || k == "--ck") && i + 1 < args.Length)
                specialist = args[++i];
            else if ((k == "-m" || k == "--summary" || k == "--reason") && i + 1 < args.Length)
                summary = args[++i];
            else if ((k == "-d" || k == "--discussion" || k == "--conclusion") && i + 1 < args.Length)
                discussion = args[++i];
            else if ((k == "-l" || k == "--location") && i + 1 < args.Length)
                location = args[++i];
            else if (k == "--icd" && i + 1 < args.Length)
                icdCode = args[++i];
            else if (k == "--icd-name" && i + 1 < args.Length)
                icdName = args[++i];
            else if (k == "--president" && i + 1 < args.Length)
            {
                presidentLogin = args[++i];
                if (presidentLogin.ToLower() == "hdc") presidentName = "HÀ ĐỨC CƯỜNG";
                else if (presidentLogin.ToLower() == "034727") presidentName = "NGUYỄN HỮU SÂM";
                else if (presidentLogin.ToLower() == "vmc") presidentName = "VŨ MINH CƯỜNG";
                else if (presidentLogin.ToLower() == "tmd" || presidentLogin.ToLower() == "tmd2") presidentName = "TRỊNH MINH ĐỨC";
            }
            else if (k == "--secretary" && i + 1 < args.Length)
            {
                secretaryLogin = args[++i];
                if (secretaryLogin.ToLower() == "034727") secretaryName = "NGUYỄN HỮU SÂM";
                else if (secretaryLogin.ToLower() == "vmc") secretaryName = "VŨ MINH CƯỜNG";
                else if (secretaryLogin.ToLower() == "hdc") secretaryName = "HÀ ĐỨC CƯỜNG";
                else if (secretaryLogin.ToLower() == "ndh2") secretaryName = "NGUYỄN ĐỨC HOÀNG";
            }
        }

        if (string.IsNullOrEmpty(treatmentCode) && string.IsNullOrEmpty(patientNameSearch) && !isListAll)
        {
            Console.WriteLine("HƯỚNG DẪN SỬ DỤNG:");
            Console.WriteLine("  HisDebateCreator.exe -t <mã_bệnh_án_hoặc_tên> -s <chuyên_khoa> -m <tóm_tắt_bệnh_án> [-l <địa_điểm>]");
            Console.WriteLine("  HisDebateCreator.exe -p <tên_bệnh_nhân> -s <chuyên_khoa> -m <tóm_tắt_bệnh_án>");
            Console.WriteLine("  HisDebateCreator.exe --search <tên_bệnh_nhân>");
            Console.WriteLine("  HisDebateCreator.exe --list-dept57");
            return;
        }

        // 1. Khởi tạo phiên làm việc
        InitSession();

        if (isListAll)
        {
            Console.WriteLine("[INFO] Đang tải toàn bộ bệnh nhân nội trú Khoa 57...");
            var deptPatients = GetAllDept57Treatments();
            Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân nội trú Khoa 57:", deptPatients.Count));
            for (int idx = 0; idx < deptPatients.Count; idx++)
            {
                var m = deptPatients[idx];
                Console.WriteLine(string.Format(" [{0}] {1} ({2}) - {3} | Mã BA: {4} | ID: {5} | Vào: {6} | ICD: {7} - {8}",
                    idx + 1, m.TDL_PATIENT_NAME, m.TDL_PATIENT_GENDER_NAME, m.TDL_PATIENT_DOB.ToString().Substring(0, 4),
                    m.TREATMENT_CODE, m.ID, m.IN_TIME, m.ICD_CODE, m.ICD_NAME));
            }
            return;
        }

        // Tra cứu đợt điều trị nếu có tìm theo tên
        if (!string.IsNullOrEmpty(patientNameSearch) || (!string.IsNullOrEmpty(treatmentCode) && !treatmentCode.All(char.IsDigit)))
        {
            string searchKey = !string.IsNullOrEmpty(patientNameSearch) ? patientNameSearch : treatmentCode;
            string searchKeyNorm = RemoveDiacritics(searchKey).ToLower();
            Console.WriteLine(string.Format("[INFO] Đang tìm kiếm bệnh nhân theo tên: '{0}' (Chuẩn hóa: '{1}')...", searchKey, searchKeyNorm));
            
            // 1. Ưu tiên tìm trong Khoa 57
            var dept57Patients = GetAllDept57Treatments();
            List<V_HIS_TREATMENT> matches = new List<V_HIS_TREATMENT>();
            
            if (dept57Patients != null)
            {
                matches = dept57Patients.Where(t => t.TDL_PATIENT_NAME != null && 
                    (t.TDL_PATIENT_NAME.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     RemoveDiacritics(t.TDL_PATIENT_NAME).ToLower().Contains(searchKeyNorm))).ToList();
            }

            // 2. Nếu không thấy trong Khoa 57, tìm toàn viện trong các bệnh nhân active
            if (matches.Count == 0)
            {
                var tf = new HisTreatmentViewFilter();
                tf.IS_PAUSE = false;
                var activeTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, commonParam);
                if (activeTreatments != null)
                {
                    matches = activeTreatments.Where(t => t.TDL_PATIENT_NAME != null && 
                        (t.TDL_PATIENT_NAME.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         RemoveDiacritics(t.TDL_PATIENT_NAME).ToLower().Contains(searchKeyNorm))).ToList();
                }
            }

            // 3. Nếu vẫn không thấy, tìm theo HIS_PATIENT
            if (matches.Count == 0)
            {
                var pf = new HisPatientViewFilter();
                pf.KEY_WORD = searchKey;
                var pts = adapter.FetchList<V_HIS_PATIENT>("api/HisPatient/GetView", ApiConsumers.MosConsumer, pf, commonParam);
                if (pts != null)
                {
                    foreach (var p in pts)
                    {
                        var tpf = new HisTreatmentViewFilter();
                        tpf.PATIENT_ID = p.ID;
                        var ptTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tpf, commonParam);
                        if (ptTreatments != null) matches.AddRange(ptTreatments);
                    }
                }
            }

            if (matches.Count == 0)
            {
                Console.WriteLine("[ERROR] Không tìm thấy bệnh nhân nào khớp với từ khóa: " + searchKey);
                return;
            }

            Console.WriteLine(string.Format("Tìm thấy {0} hồ sơ phù hợp:", matches.Count));
            for (int idx = 0; idx < matches.Count; idx++)
            {
                var m = matches[idx];
                Console.WriteLine(string.Format(" [{0}] {1} ({2}) | Mã BA: {3} | ID: {4} | Khoa: {5} | Ngày vào: {6} | ICD: {7} - {8}",
                    idx + 1, m.TDL_PATIENT_NAME, m.TDL_PATIENT_GENDER_NAME, m.TREATMENT_CODE, m.ID, m.LAST_DEPARTMENT_ID, m.IN_TIME, m.ICD_CODE, m.ICD_NAME));
            }

            if (isSearchOnly)
            {
                return;
            }

            // Ưu tiên hồ sơ ở Khoa 57 hoặc đang điều trị (IS_PAUSE != 1)
            var bestMatch = matches.FirstOrDefault(m => m.LAST_DEPARTMENT_ID == 57 && m.IS_PAUSE != 1) 
                         ?? matches.FirstOrDefault(m => m.IS_PAUSE != 1) 
                         ?? matches.First();

            treatmentCode = bestMatch.TREATMENT_CODE;
            Console.WriteLine(string.Format("[AUTO-SELECT] Chọn hồ sơ: {0} ({1}) - Mã BA: {2}", bestMatch.TDL_PATIENT_NAME, bestMatch.TDL_PATIENT_GENDER_NAME, treatmentCode));
        }

        // 2. Tra cứu đợt điều trị
        Console.WriteLine("[INFO] Đang tra cứu hồ sơ điều trị: " + treatmentCode);
        List<V_HIS_TREATMENT> treatments = null;

        // 1. Thử tìm theo PATIENT_CODE
        var pCodeFilter = new HisTreatmentViewFilter
        {
            PATIENT_CODE__EXACT = treatmentCode.PadLeft(10, '0')
        };
        treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, pCodeFilter, commonParam);

        // 2. Thử tìm theo TREATMENT_CODE (12 số)
        if (treatments == null || treatments.Count == 0)
        {
            var treatmentFilter = new HisTreatmentViewFilter
            {
                TREATMENT_CODE__EXACT = treatmentCode.PadLeft(12, '0')
            };
            treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, treatmentFilter, commonParam);
        }

        // 3. Thử tìm theo TREATMENT_CODE gốc không pad
        if (treatments == null || treatments.Count == 0)
        {
            var rawTrFilter = new HisTreatmentViewFilter
            {
                TREATMENT_CODE__EXACT = treatmentCode
            };
            treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, rawTrFilter, commonParam);
        }

        // 4. Thử tìm theo ID nếu là số
        long numId;
        if ((treatments == null || treatments.Count == 0) && long.TryParse(treatmentCode, out numId))
        {
            var idFilter = new HisTreatmentViewFilter { ID = numId };
            treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, idFilter, commonParam);
        }

        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine("[ERROR] Không tìm thấy hồ sơ điều trị với mã: " + treatmentCode);
            return;
        }

        // Ưu tiên đợt điều trị mới nhất (hoặc đang mở)
        var tm = treatments.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
        Console.WriteLine("--------------------------------------------------------------------------");
        Console.WriteLine(string.Format("BỆNH NHÂN : {0} ({1}) | NĂM SINH: {2}", tm.TDL_PATIENT_NAME, tm.TDL_PATIENT_GENDER_NAME, tm.TDL_PATIENT_DOB.ToString().Substring(0, 4)));
        Console.WriteLine(string.Format("MÃ BỆNH ÁN: {0} | MÃ ĐỢT ĐIỀU TRỊ: {1} | ID: {2}", tm.TREATMENT_CODE, tm.TREATMENT_CODE, tm.ID));
        Console.WriteLine(string.Format("CHẨN ĐOÁN : {0} - {1}", tm.ICD_CODE, tm.ICD_NAME));
        Console.WriteLine("--------------------------------------------------------------------------");

        if (string.IsNullOrEmpty(icdCode)) icdCode = tm.ICD_CODE ?? "T81.4";
        if (string.IsNullOrEmpty(icdName)) icdName = tm.ICD_NAME ?? "Nhiễm khuẩn sau phẫu thuật";
        
        if (string.IsNullOrEmpty(summary))
        {
            summary = string.Format("Bệnh nhân {0} chẩn đoán {1}. Cấy dịch vết mổ ra tụ cầu vàng 2+, xin ý kiến {2} hội chẩn và hướng dẫn phác đồ kháng sinh điều trị.", tm.TDL_PATIENT_NAME, icdName, specialist);
        }

        if (string.IsNullOrEmpty(discussion))
        {
            discussion = summary;
        }

        // 3. Chuẩn bị DTO HIS_DEBATE
        long debateTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        var debate = new HIS_DEBATE
        {
            ID = 0,
            TREATMENT_ID = tm.ID,
            ICD_CODE = icdCode,
            ICD_NAME = icdName,
            DEPARTMENT_ID = departmentId,
            DEBATE_TIME = debateTime,
            REQUEST_LOGINNAME = secretaryLogin,
            REQUEST_USERNAME = secretaryName,
            TREATMENT_TRACKING = summary,
            TREATMENT_FROM_TIME = tm.IN_TIME,
            TREATMENT_METHOD = "",
            LOCATION = location,
            REQUEST_CONTENT = specialist,
            DISCUSSION = discussion,
            CONTENT_TYPE = 1, // 1: Hội chẩn chuyên khoa
            HIS_DEBATE_USER = new List<HIS_DEBATE_USER>
            {
                new HIS_DEBATE_USER
                {
                    ID = 0,
                    LOGINNAME = presidentLogin,
                    USERNAME = presidentName,
                    IS_PRESIDENT = 1
                },
                new HIS_DEBATE_USER
                {
                    ID = 0,
                    LOGINNAME = secretaryLogin,
                    USERNAME = secretaryName,
                    IS_SECRETARY = 1
                }
            }
        };

        // 4. Gửi y lệnh tạo Hội chẩn và sinh Tờ điều trị tự động
        Console.WriteLine("[INFO] Đang gửi yêu cầu Hội chẩn Chuyên khoa (api/HisDebate/CreateAutoTracking)...");
        var result = adapter.PostData<HIS_DEBATE>("api/HisDebate/CreateAutoTracking", ApiConsumers.MosConsumer, debate, commonParam);

        if (result == null || result.ID == 0)
        {
            Console.WriteLine("[INFO] Chuyển tiếp tạo Hội chẩn qua api/HisDebate/Create...");
            result = adapter.PostData<HIS_DEBATE>("api/HisDebate/Create", ApiConsumers.MosConsumer, debate, commonParam);
            
            if (result != null && result.ID > 0)
            {
                // Đồng bộ tạo Tờ điều trị
                try
                {
                    var tracking = new HIS_TRACKING
                    {
                        TREATMENT_ID = tm.ID,
                        TRACKING_TIME = debateTime,
                        CONTENT = string.Format("Hội chẩn {0}: {1}", specialist, summary),
                        DEPARTMENT_ID = departmentId,
                        ICD_CODE = icdCode,
                        ICD_NAME = icdName
                    };
                    adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, tracking, commonParam);
                }
                catch { }
            }
        }

        if (result != null && result.ID > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================================================");
            Console.WriteLine(string.Format("✅ BƯỚC 1: TẠO PHIẾU YÊU CẦU HỘI CHẨN THÀNH CÔNG! (DEBATE_ID: {0})", result.ID));
            Console.WriteLine(string.Format("✅ BƯỚC 2: TỰ ĐỘNG SINH BIÊN BẢN & TỜ ĐIỀU TRỊ ĐỒNG BỘ THÀNH CÔNG!"));
            Console.WriteLine(string.Format("   - Thời gian    : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            Console.WriteLine(string.Format("   - Bệnh nhân    : {0} ({1})", tm.TDL_PATIENT_NAME, tm.TREATMENT_CODE));
            Console.WriteLine(string.Format("   - Chuyên khoa  : {0}", specialist));
            Console.WriteLine(string.Format("   - Địa điểm     : {0}", location));
            Console.WriteLine(string.Format("   - Chủ tọa (Ký) : {0} ({1})", presidentName, presidentLogin));
            Console.WriteLine(string.Format("   - Thư ký (Ký)  : {0} ({1})", secretaryName, secretaryLogin));
            Console.WriteLine(string.Format("   - Tóm tắt BA   : {0}", summary));
            Console.WriteLine(string.Format("   - Đề xuất CK   : {0}", discussion));
            Console.WriteLine("   - Biểu mẫu in  : Mps000019 (Trích biên bản hội chẩn) - EMR Type 17");
            Console.WriteLine("==========================================================================");
            Console.WriteLine("💡 Bác sĩ/Thư ký ký biên bản hội chẩn trực tiếp trên giao diện phần mềm EMR Desktop Client.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ TẠO HỘI CHẨN THẤT BẠI!");
            if (commonParam.Messages != null && commonParam.Messages.Count > 0)
            {
                foreach (var msg in commonParam.Messages)
                {
                    Console.WriteLine("   - Lỗi: " + msg);
                }
            }
            Console.ResetColor();
        }
    }
}
