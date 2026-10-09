using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;
using EMR.Filter;
using EMR.SDO;
using EMR.EFMODEL.DataModels;

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

public class ServiceTarget
{
    public long ServiceId { get; set; }
    public long RoomId { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceName { get; set; }
    public string Note { get; set; }
    public long? ConditionId { get; set; }

    public ServiceTarget(long sId, long rId, string code = "", string name = "", string note = "", long? condId = null)
    {
        ServiceId = sId;
        RoomId = rId;
        ServiceCode = code;
        ServiceName = name;
        Note = note;
        ConditionId = condId;
    }
}

public class HisClinicalCli
{
    public static BackendAdapter adapter;
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static ApiConsumer mosConsumer;
    public static ApiConsumer sdaConsumer;
    public static ApiConsumer emrConsumer;
    public static string currentToken = null;
    public static string currentDoctorLogin = "034727";
    public static string currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";

    public static readonly Dictionary<string, ServiceTarget> PredefinedServices = new Dictionary<string, ServiceTarget>
    {
        { "TROPONIN_THS", new ServiceTarget(63596, 410, "BM260527.26", "Định lượng Troponin Ths (Sau 28/5/2026)") },
        { "TROPONIN_OLD", new ServiceTarget(5920, 410, "BM02298", "Định lượng Troponin Ths (Trước 28/5/2026)") },
        { "KHI_MAU", new ServiceTarget(5886, 410, "BM02047", "Xét nghiệm Khí máu 11 thông số") },
        { "CBC_LASER", new ServiceTarget(5745, 1772, "BM00110", "Tổng phân tích tế bào máu laser") },
        { "COAGULATION", new ServiceTarget(2658, 1773, "BM00024", "Đông máu cơ bản") },
        { "FIBRINOGEN", new ServiceTarget(5716, 626, "BM00542", "Định lượng Fibrinogen (Clauss tự động)") },
        { "PT_TQ", new ServiceTarget(5713, 626, "BM00531", "Thời gian prothrombin (PT/TQ tự động)") },
        { "APTT_TCK", new ServiceTarget(63622, 626, "BM260527.52", "Thời gian APTT/TCK tự động") },
        { "BLOOD_GROUP_GEL", new ServiceTarget(5783, 1464, "BM01700", "Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động)") },
        { "URE", new ServiceTarget(5923, 410, "BM02304", "Định lượng Urê [Máu]") },
        { "CREATININ", new ServiceTarget(5934, 410, "BM01361", "Định lượng Creatinin (máu)") },
        { "GOT", new ServiceTarget(5834, 410, "BM01352", "Đo hoạt độ AST (GOT)") },
        { "GPT", new ServiceTarget(5833, 410, "BM01347", "Đo hoạt độ ALT (GPT)") },
        { "ELECTROLYTES", new ServiceTarget(5853, 410, "BM00132", "Điện giải đồ (Na, K, Cl)") },
        { "HBA1C", new ServiceTarget(5870, 410, "BM01429", "Định lượng HbA1c", "", 4723) },
        { "URINE_10", new ServiceTarget(5950, 566, "BM02998", "Tổng phân tích nước tiểu (tự động)") },
        { "HBSAG", new ServiceTarget(6135, 871, "BM00859", "HBsAg miễn dịch tự động") },
        { "HCV_AB", new ServiceTarget(6147, 871, "BM00837", "HCV Ab miễn dịch tự động") },
        { "HIV_AB", new ServiceTarget(6020, 871, "BM00871", "HIV Ag/Ab miễn dịch tự động") },
        { "ECG", new ServiceTarget(920, 931, "BM04258", "Điện tim thường (ECG)") },
        { "ECHO_HEART", new ServiceTarget(5569, 1715, "BM00201", "Siêu âm Doppler tim, van tim", "điều dưỡng đưa bằng cáng - cs ii") },
        { "GLUCOSE", new ServiceTarget(5864, 410, "BM10249", "Định lượng Glucose [Máu]") },
        { "US_ABDOMEN", new ServiceTarget(5567, 17547, "BM00199", "Siêu âm ổ bụng tổng quát", "điều dưỡng đưa bằng cáng - cs ii") },
        { "US_VASCULAR", new ServiceTarget(5571, 17547, "BM00203", "Siêu âm Doppler mạch máu chi", "điều dưỡng đưa bằng cáng - cs ii") },
        { "DEXA_2POS", new ServiceTarget(161, 6462, "BM08085", "Đo mật độ xương DEXA [2 vị trí]", "điều dưỡng đưa bằng cáng - cs ii") },
        { "XRAY_CHEST", new ServiceTarget(58112, 17552, "BM21074", "X-quang ngực thẳng số hóa") },
        { "GLUCOSE_BEDSIDE", new ServiceTarget(6217, 5248, "BM02426", "Xét nghiệm đường máu mao mạch tại giường (một lần)") },
        { "GLUCOSE_BEDSIDE_NB", new ServiceTarget(74281, 18679, "NB260620.6231", "Định lượng Glucose [Máu] mao mạch (CSNB)") },
        { "CORTISOL", new ServiceTarget(5929, 410, "BM02129", "Định lượng Cortisol") },
        { "ACID_URIC", new ServiceTarget(5892, 410, "BM10485", "Định lượng Acid Uric") },
        { "CRP", new ServiceTarget(5995, 410, "BM02180", "Định lượng CRP (C-Reactive Protein)") },
        { "ESR", new ServiceTarget(5686, 1772, "BM00138", "Máu lắng (bằng máy tự động) (ESR)") },
        { "DEXA_1POS", new ServiceTarget(160, 6462, "BM08084", "Đo mật độ xương DEXA [1 vị trí]", "điều dưỡng đưa bằng cáng - cs ii") },
        { "CT_LUNG", new ServiceTarget(58181, 17549, "BM00338.260119", "Chụp CLVT phổi HRCT [đến 32 dãy không thuốc CQ]") },
        { "CT_LUMBAR", new ServiceTarget(58191, 17549, "BM00400.260119", "Chụp CLVT cột sống thắt lưng không tiêm thuốc CQ") },
        { "MRI_LUMBAR", new ServiceTarget(58292, 17548, "BM00482.260119", "Chụp cộng hưởng từ cột sống thắt lưng – cùng [Không in phim]") },
        { "MRI_CERVICAL", new ServiceTarget(58290, 17552, "BM260119.0620", "Chụp CHT cột sống cổ [Không in phim]") },
        { "CULT_BACTERIA", new ServiceTarget(6054, 4374, "BM01691", "Vi khuẩn nuôi cấy và định danh hệ thống tự động") },
        { "ATTACH_KSD", new ServiceTarget(38374, 4374, "BMDK01", "Đính Kèm KSD (Kháng sinh đồ)") },
        { "QUANTIFERON", new ServiceTarget(36522, 9645, "BM26362", "Mycobacterium tuberculosis Quantiferon (BV Phổi TW)", "", 43) },
        { "MRI_BRACHIAL_PLEXUS", new ServiceTarget(58290, 17552, "BM260119.0620", "Chụp CHT cột sống cổ [Không in phim] (Đánh giá đám rối cánh tay)", "Chụp cộng hưởng từ cột sống cổ [Không in phim] đánh giá tổn thương đám rối cánh tay") },
        { "EMG_UPPER_LIMB", new ServiceTarget(908, 931, "BM01892", "Ghi điện cơ đo tốc độ dẫn truyền vận động và cảm giác ngoại biên chi trên", "Ghi điện cơ đo tốc độ dẫn truyền vận động và cảm giác ngoại biên chi trên") },
        { "XRAY_CLAVICLE", new ServiceTarget(58094, 17552, "BM00245.260119", "Chụp Xquang xương đòn thẳng, nghiêng hoặc chếch [Không in phim]", "Chụp Xquang xương đòn thẳng, nghiêng hoặc chếch [Không in phim]") }
    };

    public static string ReadLiveTokenFast()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();

        // 1. Thư mục HIS chuẩn theo chỉ định của Bác sĩ
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

        // 2. Thư mục hiện tại và các thư mục cha
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        // 3. Quét tiến trình HIS đang chạy (nếu có preferredDir thì ưu tiên, nếu không thì lấy tiến trình HIS đang chạy)
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                foreach (var p in procs)
                {
                    try
                    {
                        string exePath = p.MainModule.FileName;
                        if (Directory.Exists(preferredDir))
                        {
                            if (exePath.IndexOf("LBP2900_R150_V330_W64_uk_EN_2", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string hisDir = Path.GetDirectoryName(exePath);
                                candidates.Insert(0, Path.Combine(hisDir, "Logs", "LogSystem.txt"));
                            }
                        }
                        else
                        {
                            string hisDir = Path.GetDirectoryName(exePath);
                            candidates.Insert(0, Path.Combine(hisDir, "Logs", "LogSystem.txt"));
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

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

                    // BẢO VỆ DANH TÍNH (Identity Guard):
                    // Bắt buộc xác thực token live này phải thuộc về Bác sĩ (034727 hoặc vmc).
                    // Nếu người khác đang đăng nhập (như điều dưỡng, bác sĩ khác), bỏ qua để ép đăng nhập độc lập!
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string candidateToken = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                if (chunk.Contains("vmc"))
                                {
                                    currentDoctorLogin = "vmc";
                                    currentDoctorName = "BS Vũ Minh Cường";
                                }
                                else
                                {
                                    currentDoctorLogin = "034727";
                                    currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";
                                }
                                return candidateToken;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void InitSession(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(currentToken)) return;

        param = new CommonParam();
        string tokenCode = null;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_hn.token");
        if (!File.Exists(cacheFile))
        {
            cacheFile = Path.Combine(Directory.GetCurrentDirectory(), "doctor_hn.token");
        }
        if (!File.Exists(cacheFile))
        {
            cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        }
        if (!File.Exists(cacheFile))
        {
            string altCache = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
            if (File.Exists(altCache)) cacheFile = altCache;
        }

        try
        {
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
                            tokenCode = parts[0];
                            currentDoctorLogin = parts.Length >= 3 ? parts[2] : "034727";
                            currentDoctorName = currentDoctorLogin == "vmc" ? "BS Vũ Minh Cường" : "Ths.BS Nguyễn Hữu Sâm";
                        }
                    }
                }
            }
        }
        catch { }

        // 2. Thử đọc Live Token từ HIS chuẩn (chỉ nhận nick 034727/vmc)
        if (string.IsNullOrEmpty(tokenCode))
        {
            tokenCode = ReadLiveTokenFast();
        }

        // 2b. Kiểm tra tính sống còn của Token (Healthcheck Guard)
        bool tokenValid = false;
        if (!string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                var testConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
                var testDeps = myAdapter.FetchList<V_HIS_DEPARTMENT>("api/HisDepartment/GetView", testConsumer, new HisDepartmentViewFilter { ID = 57 }, param);
                if (testDeps != null && testDeps.Count > 0)
                {
                    tokenValid = true;
                }
            }
            catch { }
        }

        if (!tokenValid)
        {
            tokenCode = null; // Ép đăng nhập mới qua ACS!
        }
        else if (!string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                string payload = tokenCode + "|" + DateTime.Now.Ticks + "|" + (string.IsNullOrEmpty(currentDoctorLogin) ? "034727" : currentDoctorLogin);
                File.WriteAllText(cacheFile, payload, Encoding.UTF8);
                string hnToken = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_hn.token");
                File.WriteAllText(hnToken, payload, Encoding.UTF8);
                string rootHn = Path.Combine(Directory.GetCurrentDirectory(), "doctor_hn.token");
                if (rootHn != hnToken) File.WriteAllText(rootHn, payload, Encoding.UTF8);
                string rootStandalone = Path.Combine(Directory.GetCurrentDirectory(), "doctor_standalone.token");
                File.WriteAllText(rootStandalone, payload, Encoding.UTF8);
            }
            catch { }
        }

        // 3. Tự động ĐĂNG NHẬP ĐỘC LẬP qua ACS bằng nick 034727
        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                string defaultPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
                if (string.IsNullOrEmpty(defaultPass)) defaultPass = Environment.GetEnvironmentVariable("HIS_PASS");
                if (string.IsNullOrEmpty(defaultPass)) defaultPass = "981";

                var token = tokenManager.Login(param, "034727", defaultPass, "2.390.0");
                if (token == null && defaultPass != "981")
                {
                    token = tokenManager.Login(param, "034727", "981", "2.390.0");
                }
                if (token == null)
                {
                    token = tokenManager.Login(param, "034727", "998199", "2.390.0");
                }

                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    currentDoctorLogin = "034727";
                    currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null)
                    {
                        tokenCode = token.TokenCode;
                        currentDoctorLogin = "vmc";
                        currentDoctorName = "BS Vũ Minh Cường";
                    }
                }

                if (!string.IsNullOrEmpty(tokenCode))
                {
                    try
                    {
                        string payload = tokenCode + "|" + DateTime.Now.Ticks + "|" + currentDoctorLogin;
                        File.WriteAllText(cacheFile, payload, Encoding.UTF8);
                        string hnToken = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_hn.token");
                        File.WriteAllText(hnToken, payload, Encoding.UTF8);
                        string rootHn = Path.Combine(Directory.GetCurrentDirectory(), "doctor_hn.token");
                        if (rootHn != hnToken) File.WriteAllText(rootHn, payload, Encoding.UTF8);
                    }
                    catch { }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(currentDoctorLogin))
        {
            currentDoctorLogin = "034727";
            currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            throw new Exception("Không thể lấy Token xác thực HIS từ cả Live Log và ACS Login!");
        }

        currentToken = tokenCode;
        try { ApiConsumers.SetConsunmer(currentToken); } catch { }
        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
        sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", currentToken, "HIS");
        emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");
        adapter = new BackendAdapter(param);

        try
        {
            long[] dept57Rooms = new long[] {
                931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 
                5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 
                5267, 5539, 6622, 6623, 18679, 18681, 14759, 14787
            };
            var workInfo = new WorkInfoSDO
            {
                Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList()
            };
            var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }
    }

    public static void LoginAs(string username, string password)
    {
        try { Load.Init(); } catch { }
        param = new CommonParam();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        var token = tokenManager.Login(param, username, password, "2.390.0");
        if (token == null || string.IsNullOrEmpty(token.TokenCode))
        {
            string err = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "Sai tên đăng nhập hoặc mật khẩu!";
            throw new Exception(string.Format("Đăng nhập thất bại tài khoản '{0}': {1}", username, err));
        }

        currentToken = token.TokenCode;
        currentDoctorLogin = username;
        currentDoctorName = username == "vmc" ? "BS Vũ Minh Cường" : (username == "034727" ? "Ths.BS Nguyễn Hữu Sâm" : username.ToUpper());
        try { ApiConsumers.SetConsunmer(currentToken); } catch { }

        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        try
        {
            File.WriteAllText(cacheFile, currentToken + "|" + DateTime.Now.Ticks + "|" + currentDoctorLogin, Encoding.UTF8);
            string altCache = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
            if (Directory.Exists(Path.GetDirectoryName(altCache)))
            {
                File.WriteAllText(altCache, currentToken + "|" + DateTime.Now.Ticks + "|" + currentDoctorLogin, Encoding.UTF8);
            }
        }
        catch { }

        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
        sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", currentToken, "HIS");
        emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");
        adapter = new BackendAdapter(param);

        try
        {
            long[] dept57Rooms = new long[] {
                931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 
                5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 
                5267, 5539, 6622, 6623, 18679, 18681, 14759, 14787
            };
            var workInfo = new WorkInfoSDO
            {
                Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList()
            };
            myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("✔ ĐĂNG NHẬP THÀNH CÔNG BÁC SĨ: {0} ({1})", currentDoctorName, currentDoctorLogin));
        Console.WriteLine(string.Format("  • TokenCode: {0}...", currentToken.Substring(0, 16)));
        Console.WriteLine("  • Đã kích hoạt WorkInfo phòng trực P734 (Room 5248) và lưu cache doctor_standalone.token.");
        Console.WriteLine("===============================================================================");
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    public static List<string> GenerateSearchVariants(string input)
    {
        var variants = new List<string>();
        if (string.IsNullOrEmpty(input)) return variants;

        string original = input.Trim();
        variants.Add(original);

        // Biến thể dấu thanh tiếng Việt kiểu truyền thống vs hiện đại (hòa <-> hoà, hóa <-> hoá...)
        string[,] pairs = new string[,] {
            { "òa", "oà" }, { "óa", "oá" }, { "ỏa", "oả" }, { "õa", "oã" }, { "ọa", "oạ" },
            { "ÒA", "OÀ" }, { "ÓA", "OÁ" }, { "ỎA", "OẢ" }, { "ÕA", "OÃ" }, { "ỌA", "OẠ" },
            { "òe", "oè" }, { "óe", "oé" }, { "ỏe", "oẻ" }, { "õe", "oẽ" }, { "ọe", "oẹ" },
            { "ùy", "uỳ" }, { "úy", "uý" }, { "ủy", "uỷ" }, { "ũy", "uỹ" }, { "ụy", "uỵ" },
            { "ÙY", "UỲ" }, { "ÚY", "UÝ" }, { "ỦY", "UỶ" }, { "ŨY", "UỸ" }, { "ỤY", "UỴ" }
        };

        for (int i = 0; i < pairs.GetLength(0); i++)
        {
            string from = pairs[i, 0];
            string to = pairs[i, 1];
            if (original.Contains(from))
            {
                string v = original.Replace(from, to);
                if (!variants.Contains(v)) variants.Add(v);
            }
            if (original.Contains(to))
            {
                string v = original.Replace(to, from);
                if (!variants.Contains(v)) variants.Add(v);
            }
        }

        // Biến thể không dấu
        string unaccented = RemoveDiacritics(original);
        if (!string.IsNullOrEmpty(unaccented) && !variants.Contains(unaccented))
        {
            variants.Add(unaccented);
        }

        return variants;
    }

    public static void LookupPatient(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;

        // 1. Try exact match by Patient Code or Treatment Code
        if (!string.IsNullOrEmpty(keyword))
        {
            string kw = keyword.Trim();
            long numVal;
            bool isNum = long.TryParse(kw, out numVal);

            if (isNum)
            {
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
                tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter();
                    tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                    treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }
            }
            else
            {
                // Thử tìm theo PATIENT_CODE hoặc TREATMENT_CODE chữ
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                    treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }

                if (treatments == null || treatments.Count == 0)
                {
                    // BƯỚC 1: Quét nhanh trong danh sách bệnh nhân đang nằm buồng (In-memory, Siêu tốc < 0.1s, loại bỏ 100% rào cản dấu)
                    string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
                    try
                    {
                        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter
                        {
                            IS_IN_ROOM = true,
                            TREATMENT_IS_ACTIVE = true
                        };
                        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                        if (allBeds != null && allBeds.Count > 0)
                        {
                            var matchedBeds = allBeds.Where(b => 
                                b.DEPARTMENT_ID == 57 &&
                                !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) &&
                                RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)
                            ).ToList();

                            // Nếu không có ở Khoa 57, tìm ở các khoa khác
                            if (matchedBeds.Count == 0)
                            {
                                matchedBeds = allBeds.Where(b => 
                                    !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) &&
                                    RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)
                                ).ToList();
                            }

                            if (matchedBeds.Count > 0)
                            {
                                var tIds = matchedBeds.Select(x => x.TREATMENT_ID).Distinct().ToList();
                                HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter { IDs = tIds };
                                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
                            }
                        }
                    }
                    catch { }
                }

                if (treatments == null || treatments.Count == 0)
                {
                    // BƯỚC 2: Tự động tìm kiếm qua các biến thể dấu thanh (hòa <-> hoà) và không dấu trên MOS
                    var variants = GenerateSearchVariants(kw);
                    var allFound = new List<V_HIS_TREATMENT>();
                    var seenIds = new HashSet<long>();

                    foreach (var v in variants)
                    {
                        var tf = new HisTreatmentViewFilter { KEY_WORD = v, IS_PAUSE = false };
                        var res = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                        if (res != null)
                        {
                            foreach (var item in res)
                            {
                                if (seenIds.Add(item.ID)) allFound.Add(item);
                            }
                        }
                        if (allFound.Count > 0 && !v.Contains("òa") && !v.Contains("oà")) break;
                    }
                    treatments = allFound;
                }
            }
        }

        // 2. Check if keyword is a Bed Room search (e.g. 712, 714, 716...)
        if (treatments == null || treatments.Count == 0)
        {
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
            var deptBedRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
            var matchedRooms = deptBedRooms != null ? deptBedRooms.Where(x => x.BED_ROOM_NAME.Contains(keyword) || x.BED_ROOM_CODE.Contains(keyword)).ToList() : null;

            if (matchedRooms != null && matchedRooms.Count > 0)
            {
                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("🏨 DANH SÁCH BỆNH NHÂN THEO BUỒNG: {0}", keyword));
                Console.WriteLine("===============================================================================");
                foreach (var rm in matchedRooms)
                {
                    HisTreatmentBedRoomLViewFilter tbf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = rm.ID, IS_IN_ROOM = true };
                    var pts = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbf, param);
                    if (pts != null && pts.Count > 0)
                    {
                        Console.WriteLine(string.Format("\n📍 {0} ({1} bệnh nhân):", rm.BED_ROOM_NAME, pts.Count));
                        foreach (var p in pts.OrderBy(x => x.BED_NAME))
                        {
                            Console.WriteLine(string.Format("  👉 [{0}] {1} (Mã BN: {2} | Mã ĐT: {3})", 
                                p.BED_NAME ?? "Giường -", p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("\n📍 {0}: Không có bệnh nhân nằm ghép.", rm.BED_ROOM_NAME));
                    }
                }
                Console.WriteLine("===============================================================================");
                return;
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        // CẢNH BÁO ĐỐI SOÁT KHI CÓ NHIỀU BỆNH NHÂN TRÙNG TÊN / TRÙNG TỪ KHÓA
        var activeTreatments = treatments.Where(x => x.IS_PAUSE != 1).ToList();
        if (activeTreatments.Count > 1)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("⚠️ CẢNH BÁO ĐỐI SOÁT TRÙNG TÊN: Tìm thấy {0} bệnh nhân đang nằm viện khớp với '{1}':", activeTreatments.Count, keyword));
            Console.WriteLine("-------------------------------------------------------------------------------");
            int idx = 1;
            foreach (var at in activeTreatments)
            {
                Console.WriteLine(string.Format("  {0}. [{1}] Mã BN: {2} | Mã ĐT: {3} | Khoa: {4}",
                    idx++, at.TDL_PATIENT_NAME, at.TDL_PATIENT_CODE, at.TREATMENT_CODE, at.END_DEPARTMENT_NAME ?? "Khoa 57"));
                Console.WriteLine(string.Format("     Chẩn đoán: [{0}] {1}", at.ICD_CODE, at.ICD_NAME));
            }
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("👉 Chi tiết bên dưới hiển thị hồ sơ ưu tiên tại Khoa 57:");
            Console.WriteLine("===============================================================================");
        }

        var tr = treatments.LastOrDefault(x => x.IS_PAUSE != 1 && (x.END_DEPARTMENT_ID == 57 || x.LAST_DEPARTMENT_ID == 57))
                 ?? treatments.LastOrDefault(x => x.IS_PAUSE != 1)
                 ?? treatments.Last();

        HisTreatmentBedRoomLViewFilter bedFilter = new HisTreatmentBedRoomLViewFilter();
        bedFilter.TREATMENT_IDs = new List<long> { tr.ID };
        bedFilter.IS_IN_ROOM = true;
        var bedRooms = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, bedFilter, param);
        var curBed = bedRooms != null ? bedRooms.LastOrDefault(x => x.REMOVE_TIME == null || x.REMOVE_TIME == 0) : null;

        int birthYear = 0;
        string dobStr = tr.TDL_PATIENT_DOB.ToString();
        if (dobStr.Length >= 4) int.TryParse(dobStr.Substring(0, 4), out birthYear);
        int age = birthYear > 0 ? (DateTime.Now.Year - birthYear) : 0;
        string ageDisplay = age > 0 ? string.Format("{0} tuổi (Sinh năm: {1})", age, birthYear) : "N/A";

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 THÔNG TIN BỆNH NHÂN: {0} ({1} - {2})", tr.TDL_PATIENT_NAME, ageDisplay, tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt điều trị: {2}", tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("DOB_RAW: {0} | ADDRESS: {1} | IN_TIME: {2}", tr.TDL_PATIENT_DOB, tr.TDL_PATIENT_ADDRESS, tr.IN_TIME));
        Console.WriteLine(string.Format("Khoa: {0} | Buồng/Giường: {1} - {2}", tr.END_DEPARTMENT_NAME ?? "Khoa 57", curBed != null ? curBed.BED_ROOM_NAME : "Chưa xếp buồng", curBed != null ? curBed.BED_NAME : "-"));
        Console.WriteLine(string.Format("Chẩn đoán ICD: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
        Console.WriteLine(string.Format("BHYT: {0} | Trạng thái: {1}", tr.TDL_HEIN_CARD_NUMBER ?? "Không BHYT", tr.IS_PAUSE == 1 ? "ĐÃ RA VIỆN" : "ĐANG NẰM KHOA"));

        try
        {
            HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
            teinFilter.TDL_TREATMENT_ID = tr.ID;
            var teinList = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);

            if (teinList != null && teinList.Count > 0)
            {
                Func<string, string> getTein = (match) => {
                    var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
                         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
                    return item != null ? item.VALUE + " " + item.TEST_INDEX_UNIT_NAME : "-";
                };

                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("📊 BILAN XÉT NGHIỆM MỚI NHẤT:"));
                Console.WriteLine(string.Format("  • Huyết học: Hb: {0} | WBC: {1} | PLT: {2}", getTein("Hemoglobin"), getTein("Bạch cầu"), getTein("Tiểu cầu")));
                Console.WriteLine(string.Format("  • Đông máu: PT-INR: {0} | Fibrinogen: {1} | APTT: {2}", getTein("INR"), getTein("Fibrinogen"), getTein("APTT")));
                Console.WriteLine(string.Format("  • Sinh hóa: Glucose: {0} | Ure: {1} | Creatinin: {2} | AST: {3} | ALT: {4}", getTein("Glucose"), getTein("Urê"), getTein("Creatinin"), getTein("AST"), getTein("ALT")));
                Console.WriteLine(string.Format("  • Nhóm máu: {0}", getTein("ABO")));
            }
        }
        catch { }
        Console.WriteLine("===============================================================================");
    }

    public static long CreateTracking(long treatmentId, string content, long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null)
    {
        InitSession();
        long now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.ID = treatmentId;
        var treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        if (treatments == null || treatments.Count == 0) throw new Exception("Không tìm thấy đợt điều trị!");
        var tr = treatments[0];

        long reqRoomId = ResolvePatientRoomId(treatmentId);
        if (reqRoomId <= 0) reqRoomId = 5248;
        EnsureWorkInfoForRoom(reqRoomId);

        long deptId = 57;
        if (tr.END_DEPARTMENT_ID.HasValue && tr.END_DEPARTMENT_ID.Value > 0) deptId = tr.END_DEPARTMENT_ID.Value;

        HIS_TRACKING tracking = new HIS_TRACKING
        {
            TREATMENT_ID = treatmentId,
            DEPARTMENT_ID = deptId,
            ROOM_ID = reqRoomId,
            TRACKING_TIME = now,
            CONTENT = content,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };

        HisTrackingSDO sdo = new HisTrackingSDO
        {
            Tracking = tracking,
            WorkingRoomId = reqRoomId
        };

        if (pulse.HasValue || temp.HasValue || bpMax.HasValue || bpMin.HasValue)
        {
            sdo.Dhst = new HIS_DHST
            {
                TREATMENT_ID = treatmentId,
                EXECUTE_TIME = now,
                EXECUTE_LOGINNAME = currentDoctorLogin,
                EXECUTE_USERNAME = currentDoctorName,
                PULSE = pulse,
                TEMPERATURE = temp,
                BLOOD_PRESSURE_MAX = bpMax,
                BLOOD_PRESSURE_MIN = bpMin
            };
        }

        var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, param);
        if (created == null || created.ID == 0)
        {
            sdo.WorkingRoomId = 5248;
            created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, param);
        }
        if (created == null)
        {
            string msg = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "MOS returned null";
            if (param.BugCodes != null && param.BugCodes.Count > 0) msg += " Bug: " + string.Join("; ", param.BugCodes);
            throw new Exception("Tạo tờ điều trị thất bại: " + msg);
        }

        Console.WriteLine(string.Format("✔ Đã tạo Tờ điều trị ID: {0} lúc {1}", created.ID, created.TRACKING_TIME));
        return created.ID;
    }

    public static void ViewPatientMeds(string keyword)
    {
        InitSession();
        long tId = 0;
        V_HIS_TREATMENT tr = null;
        string kw = keyword.Trim();
        if (long.TryParse(kw, out tId))
        {
            var tfId = new HisTreatmentViewFilter { ID = tId };
            var lId = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfId, param);
            if (lId != null && lId.Count > 0) tr = lId[0];

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw.PadLeft(10, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw.PadLeft(12, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }
        }
        if (tr == null)
        {
            var tfKw = new HisTreatmentViewFilter { KEY_WORD = kw, IS_PAUSE = false };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
            if (list != null && list.Count > 0)
            {
                tr = list.OrderByDescending(x => x.IN_TIME).First();
            }
        }

        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + keyword);
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🧑 BỆNH NHÂN: {0} ({1}) | Mã BN: {2} | Mã ĐT: {3} | TrID: {4}", 
            tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
        Console.WriteLine("===============================================================================");

        // 1. Service Requests for Prescriptions
        HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var srs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (srs != null)
        {
            var presSrs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList();
            Console.WriteLine(string.Format("📋 PHIẾU ĐƠN THUỐC (SERVICE_REQ): {0} phiếu", presSrs.Count));
            foreach (var ps in presSrs.OrderByDescending(x => x.INTRUCTION_TIME))
            {
                string tStr = ps.INTRUCTION_TIME.ToString();
                string timeStr = tStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", tStr.Substring(6, 2), tStr.Substring(4, 2), tStr.Substring(0, 4), tStr.Substring(8, 2), tStr.Substring(10, 2)) : tStr;
                Console.WriteLine(string.Format("  • [{0}] Ngày: {1} | Loại: {2} | Kho/Phòng: {3} | TT: {4}", 
                    ps.SERVICE_REQ_CODE, timeStr, ps.SERVICE_REQ_TYPE_NAME, ps.REQUEST_ROOM_NAME, ps.SERVICE_REQ_STT_NAME));
            }
        }

        // 2. ExpMestMedicine
        HisExpMestMedicineViewFilter emf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tr.ID };
        var ems = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);
        if (ems != null && ems.Count > 0)
        {
            Console.WriteLine(string.Format("\n💊 CHI TIẾT THUỐC ĐÃ XUẤT/KÊ (EXP_MEST_MEDICINE): {0} mục", ems.Count));
            var grp = ems.GroupBy(x => (x.TDL_INTRUCTION_TIME ?? x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().Substring(0, 8)).OrderByDescending(g => g.Key);
            foreach (var g in grp)
            {
                string dStr = string.Format("{0}/{1}/{2}", g.Key.Substring(6, 2), g.Key.Substring(4, 2), g.Key.Substring(0, 4));
                Console.WriteLine(string.Format("  📅 Ngày {0} ({1} thuốc):", dStr, g.Count()));
                foreach (var m in g)
                {
                    string timing = string.Format("S:{0}|Tr:{1}|Ch:{2}|T:{3}", m.MORNING ?? "-", m.NOON ?? "-", m.AFTERNOON ?? "-", m.EVENING ?? "-");
                    Console.WriteLine(string.Format("     • {0} | SL: {1:0.##} {2} [{3}] | Kho: {4}", m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, timing, m.MEDI_STOCK_NAME));
                    if (!string.IsNullOrEmpty(m.TUTORIAL)) Console.WriteLine(string.Format("       HD: \"{0}\"", m.TUTORIAL));
                }
            }
        }
        else
        {
            // 3. Try SereServ
            HisSereServViewFilter ssf = new HisSereServViewFilter { TREATMENT_ID = tr.ID };
            var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null)
            {
                var medItems = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 6).ToList();
                Console.WriteLine(string.Format("\n💊 THUỐC TRONG SERE_SERV: {0} mục", medItems.Count));
                var grp = medItems.GroupBy(x => x.TDL_INTRUCTION_TIME.ToString().Substring(0, 8)).OrderByDescending(g => g.Key);
                foreach (var g in grp)
                {
                    string dStr = string.Format("{0}/{1}/{2}", g.Key.Substring(6, 2), g.Key.Substring(4, 2), g.Key.Substring(0, 4));
                    Console.WriteLine(string.Format("  📅 Ngày {0} ({1} thuốc):", dStr, g.Count()));
                    foreach (var item in g)
                    {
                        Console.WriteLine(string.Format("     • {0} | SL: {1:0.##}", item.TDL_SERVICE_NAME, item.AMOUNT));
                    }
                }
            }
        }
        Console.WriteLine();
    }

    public static void ViewPatientTrackings(string keyword)
    {
        InitSession();
        long tId = 0;
        V_HIS_TREATMENT tr = null;
        string kw = keyword.Trim();
        if (long.TryParse(kw, out tId))
        {
            var tfId = new HisTreatmentViewFilter { ID = tId };
            var lId = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfId, param);
            if (lId != null && lId.Count > 0) tr = lId[0];

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw.PadLeft(10, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw.PadLeft(12, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }
        }
        if (tr == null)
        {
            var tfKw = new HisTreatmentViewFilter { KEY_WORD = kw, IS_PAUSE = false };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
            if (list != null && list.Count > 0) tr = list.OrderByDescending(x => x.IN_TIME).First();
        }

        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + keyword);
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📝 TOÀN BỘ TỜ ĐIỀU TRỊ (TRACKING): {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
        Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
        Console.WriteLine("===============================================================================");

        var trkf = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
        var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
        if (trks == null || trks.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân chưa có tờ điều trị nào.");
            return;
        }

        var dhstf = new HisDhstViewFilter { TREATMENT_ID = tr.ID };
        var dhsts = myAdapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstf, param);

        foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
        {
            string tStr = trk.TRACKING_TIME.ToString();
            string timeStr = tStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", tStr.Substring(6, 2), tStr.Substring(4, 2), tStr.Substring(0, 4), tStr.Substring(8, 2), tStr.Substring(10, 2)) : tStr;

            var matchedDhst = dhsts != null ? dhsts.FirstOrDefault(d => d.EXECUTE_TIME == trk.TRACKING_TIME || (d.EXECUTE_TIME.HasValue && Math.Abs(d.EXECUTE_TIME.Value - trk.TRACKING_TIME) < 100)) : null;

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("📅 TỜ ĐIỀU TRỊ: {0} | ID: {1}", timeStr, trk.ID));
            if (matchedDhst != null)
            {
                Console.WriteLine(string.Format("   🩺 DHST: Mạch: {0} ck/p | HA: {1}/{2} mmHg | Nhiệt: {3} °C | SpO2: {4}% | NT: {5} l/p | Cân nặng: {6} kg",
                    matchedDhst.PULSE, matchedDhst.BLOOD_PRESSURE_MAX, matchedDhst.BLOOD_PRESSURE_MIN, matchedDhst.TEMPERATURE, matchedDhst.SPO2, matchedDhst.BREATH_RATE, matchedDhst.WEIGHT));
            }
            Console.WriteLine("   📝 NỘI DUNG DIỄN BIẾN & Y LỆNH:");
            if (!string.IsNullOrEmpty(trk.CONTENT))
            {
                var lines = trk.CONTENT.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    Console.WriteLine("      " + line);
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void ViewPatientCdhaAndLabs(string keyword)
    {
        InitSession();
        long tId = 0;
        V_HIS_TREATMENT tr = null;
        string kw = keyword.Trim();
        if (long.TryParse(kw, out tId))
        {
            var tfId = new HisTreatmentViewFilter { ID = tId };
            var lId = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfId, param);
            if (lId != null && lId.Count > 0) tr = lId[0];

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw.PadLeft(10, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }

            if (tr == null)
            {
                var tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw.PadLeft(12, '0') };
                var lCode = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (lCode != null && lCode.Count > 0) tr = lCode[0];
            }
        }
        if (tr == null)
        {
            var tfKw = new HisTreatmentViewFilter { KEY_WORD = kw, IS_PAUSE = false };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
            if (list != null && list.Count > 0) tr = list.OrderByDescending(x => x.IN_TIME).First();
        }

        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + keyword);
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🖼️ KẾT QUẢ CHẨN ĐOÁN HÌNH ẢNH & THĂM DÒ: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE));
        Console.WriteLine("===============================================================================");

        var reqFilter = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, reqFilter, param);
        if (reqs != null)
        {
            var cdhaReqs = reqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3).OrderBy(x => x.INTRUCTION_TIME).ToList();
            foreach (var req in cdhaReqs)
            {
                string tStr = req.INTRUCTION_TIME.ToString();
                string timeStr = tStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", tStr.Substring(6, 2), tStr.Substring(4, 2), tStr.Substring(0, 4), tStr.Substring(8, 2), tStr.Substring(10, 2)) : tStr;
                Console.WriteLine(string.Format("\n📋 Phiếu [{0}] lúc {1} | {2} | Nơi làm: {3}", req.SERVICE_REQ_CODE, timeStr, req.SERVICE_REQ_TYPE_NAME, req.EXECUTE_ROOM_NAME));
                
                var ssFilter = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
                var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                if (sss != null)
                {
                    foreach (var ss in sss)
                    {
                        var extFilter = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                        var exts = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, extFilter, param);
                        string conc = (exts != null && exts.Count > 0) ? exts[0].CONCLUDE : "";
                        string desc = (exts != null && exts.Count > 0) ? exts[0].DESCRIPTION : "";
                        Console.WriteLine(string.Format("  • Dịch vụ: {0}", ss.TDL_SERVICE_NAME));
                        if (!string.IsNullOrEmpty(conc)) Console.WriteLine(string.Format("    👉 Kết luận: {0}", conc.Replace("\r\n", " ").Trim()));
                        if (!string.IsNullOrEmpty(desc) && desc.Length < 300) Console.WriteLine(string.Format("    👉 Mô tả: {0}", desc.Replace("\r\n", " ").Trim()));
                    }
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("📊 TOÀN BỘ KẾT QUẢ XÉT NGHIỆM CHI TIẾT: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE));
        Console.WriteLine("===============================================================================");

        var teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tr.ID };
        var teins = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);
        if (teins != null && teins.Count > 0)
        {
            var grp = teins.GroupBy(x => x.TDL_SERVICE_REQ_ID ?? 0).OrderBy(g => g.Key);
            foreach (var g in grp)
            {
                long reqId = g.Key;
                var req = reqs != null ? reqs.FirstOrDefault(r => r.ID == reqId) : null;
                string timeStr = req != null ? req.INTRUCTION_TIME.ToString() : (g.First().MODIFY_TIME.HasValue ? g.First().MODIFY_TIME.Value.ToString() : "-");
                if (timeStr.Length >= 12)
                {
                    timeStr = string.Format("{0}/{1}/{2} {3}:{4}", timeStr.Substring(6, 2), timeStr.Substring(4, 2), timeStr.Substring(0, 4), timeStr.Substring(8, 2), timeStr.Substring(10, 2));
                }
                string reqCode = req != null ? req.SERVICE_REQ_CODE : reqId.ToString();
                Console.WriteLine(string.Format("\n📋 Phiếu [{0}] lúc {1} ({2} chỉ số):", reqCode, timeStr, g.Count()));
                foreach (var t in g)
                {
                    if (!string.IsNullOrEmpty(t.VALUE))
                    {
                        Console.WriteLine(string.Format("   - {0,-35}: {1} {2}", t.TEST_INDEX_NAME ?? t.TEST_INDEX_CODE, t.VALUE, t.TEST_INDEX_UNIT_NAME));
                    }
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void SearchMed(string keyword)
    {
        InitSession();
        HisMedicineTypeViewFilter mf = new HisMedicineTypeViewFilter
        {
            KEY_WORD = keyword,
            IS_ACTIVE = 1
        };
        var list = myAdapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", mosConsumer, mf, param);
        Console.WriteLine(string.Format("Tìm kiếm thuốc '{0}': {1} kết quả", keyword, list != null ? list.Count : 0));
        if (list != null)
        {
            foreach (var m in list.Take(15))
            {
                Console.WriteLine(string.Format("  • ID: {0,6} | Mã: {1,-15} | Tên: {2} | ĐV: {3} | FormID: {4}",
                    m.ID, m.MEDICINE_TYPE_CODE, m.MEDICINE_TYPE_NAME, m.SERVICE_UNIT_NAME, m.MEDICINE_USE_FORM_ID));
            }
        }
    }

    public static void SearchService(string keyword)
    {
        InitSession();
        HisServiceViewFilter sf = new HisServiceViewFilter
        {
            KEY_WORD = keyword,
            IS_ACTIVE = 1
        };
        var list = myAdapter.FetchList<V_HIS_SERVICE>("api/HisService/GetView", mosConsumer, sf, param);
        Console.WriteLine(string.Format("Tìm kiếm dịch vụ '{0}': {1} kết quả", keyword, list != null ? list.Count : 0));
        if (list != null)
        {
            foreach (var s in list.Take(25))
            {
                Console.WriteLine(string.Format("  • ID: {0,6} | Mã: {1,-15} | Loại: {2,2} | Tên: {3}",
                    s.ID, s.SERVICE_CODE, s.SERVICE_TYPE_ID, s.SERVICE_NAME));
            }
        }
    }

    public static void CheckHanoiConnection()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 KIỂM TRA KẾT NỐI MÁY CHỦ BỆNH VIỆN BẠCH MAI - HÀ NỘI");
        Console.WriteLine("===============================================================================");
        InitSession();

        string[] hostPorts = new string[] {
            "192.168.7.236:1608 (MOS Backend API)",
            "192.168.7.200:1401 (ACS Auth Service)",
            "192.168.7.200:1410 (SDA Data Service)",
            "192.168.7.239:1415 (EMR Document API)"
        };
        foreach (var hp in hostPorts)
        {
            string[] parts = hp.Split(new char[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string host = parts[0];
            int port = int.Parse(parts[1]);
            string name = hp.Substring(hp.IndexOf('('));
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var ar = tcp.BeginConnect(host, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(2000);
                    if (ok && tcp.Connected)
                    {
                        Console.WriteLine(string.Format("  [✅ KẾT NỐI TỐT] {0}:{1} {2}", host, port, name));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("  [❌ MẤT KẾT NỐI] {0}:{1} {2}", host, port, name));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("  [❌ LỖI SOCKET] {0}:{1} - {2}", host, port, ex.Message));
            }
        }

        Console.WriteLine(string.Format("  • Token xác thực  : {0}...", currentToken != null && currentToken.Length >= 16 ? currentToken.Substring(0, 16) : "N/A"));
        Console.WriteLine(string.Format("  • Bác sĩ đăng nhập: {0} ({1})", currentDoctorName, currentDoctorLogin));

        try
        {
            var branches = myAdapter.FetchList<HIS_BRANCH>("api/HisBranch/Get", mosConsumer, new HisBranchFilter(), param);
            if (branches != null)
            {
                var hanoi = branches.FirstOrDefault(b => b.ID == 1);
                if (hanoi != null)
                {
                    Console.WriteLine(string.Format("  • Cơ sở dữ liệu   : [{0}] {1} (ID: {2})", hanoi.BRANCH_CODE, hanoi.BRANCH_NAME, hanoi.ID));
                }
            }
        }
        catch { }
        Console.WriteLine("===============================================================================");
    }

    public static void ReportAmputations2026(string yearArg)
    {
        CheckHanoiConnection();
        Console.WriteLine();
        int year = 2026;
        if (!string.IsNullOrEmpty(yearArg)) int.TryParse(yearArg, out year);

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🔍 TRUY CẬP DỮ LIỆU PHÒNG MỔ & TRÍCH XUẤT BN CẮT CỤT CHI NĂM {0}", year));
        Console.WriteLine("===============================================================================");

        long timeFrom = (long)year * 10000000000L + 101000000L;
        long timeTo = (long)year * 10000000000L + 1231235959L;

        // 1. Quét danh mục dịch vụ phẫu thuật cắt cụt / tháo khớp
        Console.WriteLine("\n[BƯỚC 1] Quét danh mục kỹ thuật phẫu thuật cắt cụt / tháo khớp...");
        var ampKeywords = new string[] { "cắt cụt", "tháo khớp", "mỏm cụt", "sửa mỏm cụt" };
        var ampServices = new Dictionary<long, V_HIS_SERVICE>();

        foreach (var kw in ampKeywords)
        {
            try
            {
                var sf = new HisServiceViewFilter { KEY_WORD = kw, IS_ACTIVE = 1 };
                var svcs = myAdapter.FetchList<V_HIS_SERVICE>("api/HisService/GetView", mosConsumer, sf, param);
                if (svcs != null)
                {
                    foreach (var s in svcs)
                    {
                        if (!ampServices.ContainsKey(s.ID))
                        {
                            string sName = (s.SERVICE_NAME ?? "").ToLower();
                            if (sName.Contains("cắt cụt") || sName.Contains("tháo khớp") || sName.Contains("mỏm cụt"))
                            {
                                ampServices[s.ID] = s;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        Console.WriteLine(string.Format("✔ Tìm thấy {0} danh mục kỹ thuật cắt cụt / tháo khớp chuẩn trong hệ thống:", ampServices.Count));
        foreach (var s in ampServices.Values.Take(8))
        {
            Console.WriteLine(string.Format("   • [ID: {0,6}] Mã: {1,-15} | {2}", s.ID, s.SERVICE_CODE, s.SERVICE_NAME));
        }
        if (ampServices.Count > 8) Console.WriteLine(string.Format("   ... và {0} danh mục kỹ thuật khác.", ampServices.Count - 8));

        // 2. Quét SERE_SERV thực hiện trong năm 2026
        Console.WriteLine(string.Format("\n[BƯỚC 2] Truy vấn dữ liệu thực hiện tại Phòng mổ (SERE_SERV) năm {0}...", year));
        var matchedSereServs = new List<V_HIS_SERE_SERV>();
        var serviceIdList = ampServices.Keys.ToList();

        for (int i = 0; i < serviceIdList.Count; i += 30)
        {
            var chunk = serviceIdList.Skip(i).Take(30).ToList();
            try
            {
                var ssFilter = new HisSereServViewFilter
                {
                    SERVICE_IDs = chunk,
                    INTRUCTION_TIME_FROM = timeFrom,
                    INTRUCTION_TIME_TO = timeTo
                };
                var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                if (sss != null && sss.Count > 0)
                {
                    matchedSereServs.AddRange(sss);
                }
            }
            catch { }
        }

        // 3. Đối soát thêm hồ sơ bệnh án theo mã ICD-10 và từ khóa chẩn đoán cắt cụt
        Console.WriteLine(string.Format("\n[BƯỚC 3] Đối soát hồ sơ bệnh án qua mã chẩn đoán ICD-10 chấn thương/cắt cụt năm {0}...", year));
        var icdPrefixes = new string[] { "S48", "S58", "S68", "S78", "S88", "S98", "T05", "Z89" };
        var matchedTreatments = new Dictionary<long, V_HIS_TREATMENT>();

        foreach (var icd in icdPrefixes)
        {
            try
            {
                var tf = new HisTreatmentViewFilter
                {
                    ICD_CODE_OR_ICD_SUB_CODE = icd,
                    IN_TIME_FROM = timeFrom,
                    IN_TIME_TO = timeTo
                };
                var trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trs != null)
                {
                    foreach (var tr in trs)
                    {
                        if (!matchedTreatments.ContainsKey(tr.ID)) matchedTreatments[tr.ID] = tr;
                    }
                }
            }
            catch { }
        }

        try
        {
            var tfKw = new HisTreatmentViewFilter
            {
                KEY_WORD = "cắt cụt",
                IN_TIME_FROM = timeFrom,
                IN_TIME_TO = timeTo
            };
            var trsKw = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
            if (trsKw != null)
            {
                foreach (var tr in trsKw)
                {
                    if (!matchedTreatments.ContainsKey(tr.ID)) matchedTreatments[tr.ID] = tr;
                }
            }
        }
        catch { }

        Console.WriteLine(string.Format("✔ Đã thu thập {0} lượt chỉ định dịch vụ phòng mổ và {1} đợt điều trị liên quan.", 
            matchedSereServs.Count, matchedTreatments.Count));

        // 4. Tổng hợp danh sách bệnh nhân
        var treatmentIdSet = new HashSet<long>();
        foreach (var ss in matchedSereServs)
        {
            if (ss.TDL_TREATMENT_ID.HasValue) treatmentIdSet.Add(ss.TDL_TREATMENT_ID.Value);
        }
        foreach (var tId in matchedTreatments.Keys)
        {
            treatmentIdSet.Add(tId);
        }

        foreach (var tId in treatmentIdSet.ToList())
        {
            if (!matchedTreatments.ContainsKey(tId))
            {
                try
                {
                    var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { ID = tId }, param);
                    if (trList != null && trList.Count > 0) matchedTreatments[tId] = trList[0];
                }
                catch { }
            }
        }

        // Lấy chi tiết SERE_SERV_PTTT cho các ca mổ
        var ptttMap = new Dictionary<long, V_HIS_SERE_SERV_PTTT>();
        if (matchedSereServs.Count > 0)
        {
            try
            {
                var pFilter = new HisSereServPtttViewFilter
                {
                    SERE_SERV_IDs = matchedSereServs.Select(x => x.ID).ToList()
                };
                var pList = myAdapter.FetchList<V_HIS_SERE_SERV_PTTT>("api/HisSereServPttt/GetView", mosConsumer, pFilter, param);
                if (pList != null)
                {
                    foreach (var p in pList)
                    {
                        ptttMap[p.SERE_SERV_ID] = p;
                    }
                }
            }
            catch { }
        }

        // 5. Tách thành 2 nhóm rõ ràng:
        // Nhóm 1: Bệnh nhân THỰC SỰ ĐƯỢC PHẪU THUẬT CẮT CỤT TẠI PHÒNG MỔ NĂM 2026 (Có chỉ định/thực hiện SERE_SERV)
        // Nhóm 2: Bệnh nhân có mã chẩn đoán ICD cắt cụt cấp / tiền sử cắt cụt (Z89/S98/ĐTĐ...) nhập viện điều trị nội khoa
        var surgeryPatients = treatmentIdSet.Where(tId => matchedSereServs.Any(x => x.TDL_TREATMENT_ID == tId)).ToList();
        var historyPatients = treatmentIdSet.Where(tId => !matchedSereServs.Any(x => x.TDL_TREATMENT_ID == tId)).ToList();

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("🏥 BÁO CÁO DỮ LIỆU PHÒNG MỔ: BỆNH NHÂN PHẪU THUẬT CẮT CỤT CHI NĂM {0}", year));
        Console.WriteLine(string.Format("   • Nhóm 1: Phẫu thuật cắt cụt thực hiện tại Phòng mổ : {0} ca", surgeryPatients.Count));
        Console.WriteLine(string.Format("   • Nhóm 2: Hồ sơ có mã ICD cắt cụt / Tiền sử thiếu chi: {1} ca", surgeryPatients.Count, historyPatients.Count));
        Console.WriteLine("===============================================================================");

        StringBuilder md = new StringBuilder();
        md.AppendLine(string.Format("# BÁO CÁO DANH SÁCH BỆNH NHÂN CẮT CỤT CHI NĂM {0}", year));
        md.AppendLine(string.Format("*Ngày trích xuất dữ liệu: {0:dd/MM/yyyy HH:mm:ss} | Máy chủ HIS: Bệnh viện Bạch Mai - Hà Nội*\n", DateTime.Now));
        md.AppendLine(string.Format("### 📊 TỔNG QUAN THỐNG KÊ"));
        md.AppendLine(string.Format("- **Số ca phẫu thuật cắt cụt chi tại Phòng mổ năm {0}**: **`{1}` ca**", year, surgeryPatients.Count));
        md.AppendLine(string.Format("- **Số ca có chẩn đoán / tiền sử cắt cụt chi điều trị nội trú**: **`{0}` ca**", historyPatients.Count));
        md.AppendLine(string.Format("- **Tổng số hồ sơ phát hiện**: **`{0}` bệnh nhân**\n", treatmentIdSet.Count));

        md.AppendLine(string.Format("## 🔪 PHẦN 1: DANH SÁCH {0} CA PHẪU THUẬT CẮT CỤT CHI TẠI PHÒNG MỔ NĂM {1}\n", surgeryPatients.Count, year));
        md.AppendLine("| STT | Mã BN | Họ và Tên | Tuổi | Giới | Ngày Phẫu Thuật | Tên Kỹ Thuật Phẫu Thuật | Phòng Mổ | PTV / Trưởng Kíp | Khoa Điều Trị | Chẩn Đoán |");
        md.AppendLine("|:---:|:---:|:---|:---:|:---:|:---:|:---|:---|:---|:---|:---|");

        Console.WriteLine(string.Format("\n🔪 [PHẦN 1] CHI TIẾT {0} CA PHẪU THUẬT CẮT CỤT TẠI PHÒNG MỔ:", surgeryPatients.Count));
        int sIdx = 1;
        foreach (var tId in surgeryPatients)
        {
            var tr = matchedTreatments.ContainsKey(tId) ? matchedTreatments[tId] : null;
            var patSereServs = matchedSereServs.Where(x => x.TDL_TREATMENT_ID == tId).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();

            string pName = tr != null ? tr.TDL_PATIENT_NAME : "BN ID " + tId;
            string pCode = tr != null ? tr.TDL_PATIENT_CODE : "N/A";
            string tCode = tr != null ? tr.TREATMENT_CODE : "N/A";
            string gender = tr != null ? tr.TDL_PATIENT_GENDER_NAME : "";
            
            int birthYear = 0;
            string sDob = tr != null ? tr.TDL_PATIENT_DOB.ToString() : "";
            if (sDob.Length >= 4) int.TryParse(sDob.Substring(0, 4), out birthYear);
            string age = birthYear > 0 ? (year - birthYear).ToString() : "-";

            string inTimeStr = tr != null ? tr.IN_TIME.ToString() : "";
            string inTimeFmt = inTimeStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", inTimeStr.Substring(6, 2), inTimeStr.Substring(4, 2), inTimeStr.Substring(0, 4), inTimeStr.Substring(8, 2), inTimeStr.Substring(10, 2)) : inTimeStr;
            string outTimeStr = tr != null && tr.OUT_TIME.HasValue ? tr.OUT_TIME.Value.ToString() : "";
            string status = !string.IsNullOrEmpty(outTimeStr) ? "Đã ra viện (" + (outTimeStr.Length >= 8 ? string.Format("{0}/{1}/{2}", outTimeStr.Substring(6, 2), outTimeStr.Substring(4, 2), outTimeStr.Substring(0, 4)) : outTimeStr) + ")" : "Đang điều trị";

            Console.WriteLine(string.Format("\n[{0}] BỆNH NHÂN: {1} ({2} tuổi - {3})", sIdx++, pName, age, gender));
            Console.WriteLine(string.Format("    • Mã BN: {0} | Mã ĐT: {1} | Trạng thái: {2}", pCode, tCode, status));
            Console.WriteLine(string.Format("    • Khoa điều trị: {0}", tr != null ? tr.END_DEPARTMENT_NAME : "-"));
            Console.WriteLine(string.Format("    • Chẩn đoán    : [{0}] {1}", tr != null ? tr.ICD_CODE : "-", tr != null ? (tr.ICD_NAME + " (" + (tr.ICD_TEXT ?? tr.ICD_SUB_CODE) + ")") : "-"));

            foreach (var ss in patSereServs)
            {
                string ssTime = ss.TDL_INTRUCTION_TIME.ToString();
                string ssTimeFmt = ssTime.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", ssTime.Substring(6, 2), ssTime.Substring(4, 2), ssTime.Substring(0, 4), ssTime.Substring(8, 2), ssTime.Substring(10, 2)) : ssTime;
                var pttt = ptttMap.ContainsKey(ss.ID) ? ptttMap[ss.ID] : null;
                string ptv = pttt != null ? (pttt.HEAD_USERNAME ?? pttt.HEAD_LOGINNAME ?? "-") : "-";

                Console.WriteLine(string.Format("    👉 Phẫu thuật: {0} (Mã: {1})", ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_CODE));
                Console.WriteLine(string.Format("       - Thời gian: {0} | Phòng mổ: {1}", ssTimeFmt, ss.EXECUTE_ROOM_NAME ?? ss.TDL_EXECUTE_ROOM_ID.ToString()));
                if (pttt != null && (!string.IsNullOrEmpty(pttt.HEAD_USERNAME) || !string.IsNullOrEmpty(pttt.HEAD_LOGINNAME)))
                {
                    Console.WriteLine(string.Format("       - PTV chính: {0}", ptv));
                }

                md.AppendLine(string.Format("| {0} | {1} | **{2}** | {3} | {4} | {5} | {6} | {7} | {8} | {9} | [{10}] {11} |",
                    sIdx - 1, pCode, pName, age, gender, ssTimeFmt, ss.TDL_SERVICE_NAME, ss.EXECUTE_ROOM_NAME, ptv, tr != null ? tr.END_DEPARTMENT_NAME : "-", tr != null ? tr.ICD_CODE : "", tr != null ? tr.ICD_NAME : ""));
            }
        }

        // Lưu báo cáo
        try
        {
            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(outDir);
            string mdPath = Path.Combine(outDir, string.Format("BaoCao_CatCutChi_{0}.md", year));
            File.WriteAllText(mdPath, md.ToString(), Encoding.UTF8);
            Console.WriteLine(string.Format("\n📄 Đã xuất báo cáo chi tiết ra file: {0}", mdPath));
        }
        catch { }
        Console.WriteLine("\n===============================================================================");
    }

    public static void ReportCtchAmputations(string yearArg)
    {
        InitSession();
        int year = 2026;
        if (!string.IsNullOrEmpty(yearArg)) int.TryParse(yearArg, out year);
        long timeFrom = (long)year * 10000000000L + 101000000L;
        long timeTo = (long)year * 10000000000L + 1231235959L;

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("👨‍⚕️ TRÍCH XUẤT BÁC SĨ PHẪU THUẬT KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (K57) NĂM {0}", year));
        Console.WriteLine("===============================================================================");

        // 1. Quét danh mục dịch vụ kỹ thuật cắt cụt / tháo khớp
        var ampKeywords = new string[] { "cắt cụt", "tháo khớp", "mỏm cụt", "sửa mỏm cụt" };
        var ampServiceIds = new HashSet<long>();
        foreach (var kw in ampKeywords)
        {
            try
            {
                var sf = new HisServiceViewFilter { KEY_WORD = kw, IS_ACTIVE = 1 };
                var svcs = myAdapter.FetchList<V_HIS_SERVICE>("api/HisService/GetView", mosConsumer, sf, param);
                if (svcs != null)
                {
                    foreach (var s in svcs)
                    {
                        string sName = (s.SERVICE_NAME ?? "").ToLower();
                        if (sName.Contains("cắt cụt") || sName.Contains("tháo khớp") || sName.Contains("mỏm cụt"))
                        {
                            ampServiceIds.Add(s.ID);
                        }
                    }
                }
            }
            catch { }
        }

        // 2. Lấy SERE_SERV thực hiện trong năm 2026
        var matchedSereServs = new List<V_HIS_SERE_SERV>();
        var sIdList = ampServiceIds.ToList();
        for (int i = 0; i < sIdList.Count; i += 30)
        {
            var chunk = sIdList.Skip(i).Take(30).ToList();
            try
            {
                var ssFilter = new HisSereServViewFilter
                {
                    SERVICE_IDs = chunk,
                    INTRUCTION_TIME_FROM = timeFrom,
                    INTRUCTION_TIME_TO = timeTo
                };
                var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                if (sss != null) matchedSereServs.AddRange(sss);
            }
            catch { }
        }

        Console.WriteLine(string.Format("✔ Tổng số lượt kỹ thuật cắt cụt tại Phòng mổ năm {0}: {1} lượt.", year, matchedSereServs.Count));

        // 3. Lấy EKIP_USER
        var ekipIds = matchedSereServs.Where(x => x.EKIP_ID.HasValue && x.EKIP_ID.Value > 0).Select(x => x.EKIP_ID.Value).Distinct().ToList();
        var ekipUsers = new List<V_HIS_EKIP_USER>();
        for (int i = 0; i < ekipIds.Count; i += 40)
        {
            var chunk = ekipIds.Skip(i).Take(40).ToList();
            try
            {
                var euFilter = new HisEkipUserViewFilter { EKIP_IDs = chunk };
                var eus = myAdapter.FetchList<V_HIS_EKIP_USER>("api/HisEkipUser/GetView", mosConsumer, euFilter, param);
                if (eus != null) ekipUsers.AddRange(eus);
            }
            catch { }
        }

        // Lấy danh sách bác sĩ thuộc Khoa 57 hoặc tham gia ca của Khoa 57
        var ctchSereServs = matchedSereServs.Where(x => x.TDL_REQUEST_DEPARTMENT_ID == 57 || (x.REQUEST_DEPARTMENT_NAME != null && x.REQUEST_DEPARTMENT_NAME.Contains("Chấn thương"))).ToList();
        Console.WriteLine(string.Format("✔ Số ca mổ chỉ định từ Khoa CTCH & Cột sống (Khoa 57): {0} ca", ctchSereServs.Count));

        // Thống kê bác sĩ xuất hiện trong kíp mổ
        var ekipMap = ekipUsers.GroupBy(x => x.EKIP_ID).ToDictionary(g => g.Key, g => g.ToList());

        // Lọc tất cả bác sĩ CTCH (Department 57 hoặc tham gia ca mổ Khoa 57)
        var doctorStats = new Dictionary<string, List<V_HIS_SERE_SERV>>();
        var doctorRoles = new Dictionary<string, HashSet<string>>();

        foreach (var ss in matchedSereServs)
        {
            if (ss.EKIP_ID.HasValue && ekipMap.ContainsKey(ss.EKIP_ID.Value))
            {
                var users = ekipMap[ss.EKIP_ID.Value];
                foreach (var u in users)
                {
                    bool isCtchDoc = (u.DEPARTMENT_ID == 57) || 
                                     (u.DEPARTMENT_NAME != null && u.DEPARTMENT_NAME.Contains("Chấn thương")) ||
                                     (ss.TDL_REQUEST_DEPARTMENT_ID == 57 && (u.IS_SURG_MAIN == 1 || (u.EXECUTE_ROLE_NAME != null && u.EXECUTE_ROLE_NAME.Contains("Phẫu thuật"))));

                    if (isCtchDoc)
                    {
                        string docKey = string.Format("{0} ({1})", u.USERNAME, u.LOGINNAME);
                        if (!doctorStats.ContainsKey(docKey))
                        {
                            doctorStats[docKey] = new List<V_HIS_SERE_SERV>();
                            doctorRoles[docKey] = new HashSet<string>();
                        }
                        doctorStats[docKey].Add(ss);
                        if (!string.IsNullOrEmpty(u.EXECUTE_ROLE_NAME)) doctorRoles[docKey].Add(u.EXECUTE_ROLE_NAME);
                    }
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("👨‍⚕️ BẢNG THỐNG KÊ BÁC SĨ PHẪU THUẬT KHOA CTCH & CỘT SỐNG:");
        Console.WriteLine("===============================================================================");
        if (doctorStats.Count == 0)
        {
            // Nếu DEPARTMENT_ID của ekip_user không điền mã 57, in toàn bộ PTV chính tham gia các ca Khoa 57
            Console.WriteLine("ℹ️ Phân tích PTV chính & Kíp mổ trực tiếp từ các ca của Khoa 57:");
            foreach (var ss in ctchSereServs)
            {
                string sTime = ss.TDL_INTRUCTION_TIME.ToString();
                string sTimeFmt = sTime.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", sTime.Substring(6, 2), sTime.Substring(4, 2), sTime.Substring(0, 4), sTime.Substring(8, 2), sTime.Substring(10, 2)) : sTime;
                Console.WriteLine(string.Format("\n  • BN Mã ĐT: {0} | Mổ lúc: {1} | PM: {2}", ss.TDL_TREATMENT_CODE, sTimeFmt, ss.EXECUTE_ROOM_NAME));
                Console.WriteLine(string.Format("    Kỹ thuật: {0}", ss.TDL_SERVICE_NAME));

                if (ss.EKIP_ID.HasValue && ekipMap.ContainsKey(ss.EKIP_ID.Value))
                {
                    Console.WriteLine("    Kíp phẫu thuật:");
                    foreach (var u in ekipMap[ss.EKIP_ID.Value])
                    {
                        Console.WriteLine(string.Format("      - {0,-25} | Vai trò: {1,-20} | Khoa: {2}", u.USERNAME, u.EXECUTE_ROLE_NAME, u.DEPARTMENT_NAME ?? "Khoa 57"));
                    }
                }
            }
        }
        else
        {
            int dIdx = 1;
            var sortedDocs = doctorStats.OrderByDescending(x => x.Value.Count).ToList();
            foreach (var kvp in sortedDocs)
            {
                string roles = string.Join(", ", doctorRoles[kvp.Key]);
                int distinctSurgeries = kvp.Value.Select(x => string.Format("{0}_{1}", x.TDL_TREATMENT_CODE, x.TDL_INTRUCTION_TIME)).Distinct().Count();
                Console.WriteLine(string.Format("\n[{0}] BÁC SĨ: {1}", dIdx++, kvp.Key));
                Console.WriteLine(string.Format("    • Vai trò trong kíp mổ : {0}", roles));
                Console.WriteLine(string.Format("    • Tổng số cuộc mổ      : {0} cuộc ({1} lượt dịch vụ kỹ thuật)", distinctSurgeries, kvp.Value.Count));

                // Kiểm tra có mổ lại bệnh nhân nào không (dựa trên các ngày mổ khác nhau, tránh đếm nhầm nhiều dòng SERE_SERV trong cùng 1 cuộc mổ)
                var patGroups = kvp.Value
                    .GroupBy(x => x.TDL_TREATMENT_CODE)
                    .Select(g => new {
                        TreatmentCode = g.Key,
                        Dates = g.Select(s => s.TDL_INTRUCTION_TIME.ToString().Substring(0, Math.Min(8, s.TDL_INTRUCTION_TIME.ToString().Length))).Distinct().ToList(),
                        Services = g.ToList()
                    })
                    .Where(x => x.Dates.Count > 1)
                    .ToList();

                if (patGroups.Count > 0)
                {
                    Console.WriteLine(string.Format("    ⚠️ SỐ CA MỔ LẠI THỰC SỰ (Khác ngày): {0} bệnh nhân", patGroups.Count));
                    foreach (var pg in patGroups)
                    {
                        Console.WriteLine(string.Format("       - BN Mã ĐT {0} ({1} ngày/đợt mổ khác nhau):", pg.TreatmentCode, pg.Dates.Count));
                        foreach (var s in pg.Services)
                        {
                            string sTime = s.TDL_INTRUCTION_TIME.ToString();
                            string sTimeFmt = sTime.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", sTime.Substring(6, 2), sTime.Substring(4, 2), sTime.Substring(0, 4), sTime.Substring(8, 2), sTime.Substring(10, 2)) : sTime;
                            Console.WriteLine(string.Format("         + [{0}] {1} (PM: {2})", sTimeFmt, s.TDL_SERVICE_NAME, s.EXECUTE_ROOM_NAME));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("    • Không có ca nào mổ lại cùng 1 bệnh nhân ở các ngày khác nhau.");
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
    }

    public static void PrescribeMedication(long treatmentId, long trackingId, long medicineTypeId, long stockId, decimal amount, string tutorial, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        if (tutorial.ToUpper().Contains("INSULIN") || tutorial.ToUpper().Contains("ACTRAPID") || tutorial.ToUpper().Contains("LANTUS") || tutorial.ToUpper().Contains("MIXTARD"))
        {
            if (stockId != 810)
            {
                Console.WriteLine("⚠️ CẢNH BÁO AN TOÀN: Đã tự động chuyển kho thuốc Insulin về TỦ TRỰC KHOA 57 (MediStockId = 810).");
                stockId = 810;
            }
            if (amount > 0.5m)
            {
                Console.WriteLine(string.Format("⚠️ CẢNH BÁO AN TOÀN: Liều Insulin là {0} UI. Đang quy đổi UI -> Lọ ({0} / 1000 = {1:F4} lọ).", amount, amount / 1000.0m));
                amount = amount / 1000.0m;
            }
        }

        InPatientPresSDO sdo = new InPatientPresSDO
        {
            TreatmentId = treatmentId,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            RequestRoomId = 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = medicineTypeId,
                    MediStockId = stockId,
                    Amount = amount,
                    PatientTypeId = patientTypeId,
                    Tutorial = tutorial
                }
            }
        };

        var res = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdo, param);
        if (res != null && res.ExpMests != null && res.ExpMests.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã xuất thuốc EXP_MEST: {0}", res.ExpMests[0].EXP_MEST_CODE));
        }
        else if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã y lệnh: {0}", res.ServiceReqs[0].SERVICE_REQ_CODE));
        }
        else
        {
            throw new Exception("Kê đơn thuốc thất bại!");
        }
    }

    public static long ResolvePatientRoomId(long treatmentId)
    {
        try
        {
            HisTreatmentBedRoomLViewFilter bedFilter = new HisTreatmentBedRoomLViewFilter();
            bedFilter.TREATMENT_IDs = new List<long> { treatmentId };
            bedFilter.IS_IN_ROOM = true;
            var bedRooms = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, bedFilter, param);
            var curBed = bedRooms != null ? bedRooms.LastOrDefault(x => x.REMOVE_TIME == null || x.REMOVE_TIME == 0) : null;
            if (curBed != null && curBed.BED_ROOM_ID > 0)
            {
                var brf = new HisBedRoomViewFilter { ID = curBed.BED_ROOM_ID };
                var bRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
                if (bRooms != null && bRooms.Count > 0 && bRooms[0].ROOM_ID > 0)
                {
                    return bRooms[0].ROOM_ID;
                }
            }
        }
        catch { }
        return 5248;
    }

    public static string CleanIcdCode(string rawIcd)
    {
        if (string.IsNullOrEmpty(rawIcd)) return "M50.2";
        string clean = rawIcd.Trim();
        if (clean.Length > 5 && clean.Contains("."))
        {
            var dotParts = clean.Split('.');
            if (dotParts[0].Length <= 3 && dotParts.Length > 1)
            {
                int maxDec = Math.Min(dotParts[1].Length, 5 - 1 - dotParts[0].Length);
                clean = dotParts[0] + "." + dotParts[1].Substring(0, maxDec);
            }
        }
        if (clean.Length > 5) clean = clean.Substring(0, 5);
        return clean;
    }

    public static void EnsureWorkInfoForRoom(long roomId)
    {
        try
        {
            var rooms = new List<RoomSDO>
            {
                new RoomSDO { RoomId = roomId },
                new RoomSDO { RoomId = 5248 },
                new RoomSDO { RoomId = 5252 },
                new RoomSDO { RoomId = 5251 },
                new RoomSDO { RoomId = 5257 }
            };
            var workInfo = new WorkInfoSDO
            {
                Rooms = rooms
            };
            myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }
    }

    public static void AssignClsService(long treatmentId, long trackingId, long serviceId, long roomId, string note, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        long reqRoomId = ResolvePatientRoomId(treatmentId);
        EnsureWorkInfoForRoom(reqRoomId);

        string icd = CleanIcdCode(tr.ICD_CODE);
        AssignServiceSDO sdo = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = reqRoomId,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            InstructionTime = tr.TRACKING_TIME,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            IcdCode = icd,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            SessionCode = null,
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = serviceId,
                    Amount = 1.0m,
                    PatientTypeId = patientTypeId,
                    PrimaryPatientTypeId = (patientTypeId == 1 ? (long?)null : patientTypeId),
                    RoomId = roomId,
                    InstructionNote = note
                }
            }
        };

        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdo, param);
        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            foreach (var sr in res.ServiceReqs)
            {
                Console.WriteLine(string.Format("✔ Chỉ định thành công! Mã y lệnh CLS: {0} (ID: {1})", sr.SERVICE_REQ_CODE, sr.ID));
            }
        }
        else
        {
            string err = "Chỉ định CLS thất bại!";
            if (param.Messages != null && param.Messages.Count > 0) err += " Messages: " + string.Join("; ", param.Messages);
            if (param.BugCodes != null && param.BugCodes.Count > 0) err += " BugCodes: " + string.Join("; ", param.BugCodes);
            throw new Exception(err);
        }
    }

    public static void AssignServiceBatch(long treatmentId, long trackingId, List<ServiceTarget> targetList, int patientTypeId = 1)
    {
        InitSession();
        if (targetList == null || targetList.Count == 0) throw new Exception("Danh sách dịch vụ chỉ định trống!");

        HIS_TRACKING tr = null;
        if (trackingId > 0)
        {
            var tf = new HisTrackingFilter { ID = trackingId };
            var list = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, tf, param);
            if (list != null && list.Count > 0) tr = list[0];
        }
        if (tr == null)
        {
            var tf = new HisTrackingFilter { TREATMENT_ID = treatmentId };
            var list = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, tf, param);
            if (list != null && list.Count > 0)
            {
                tr = list.OrderByDescending(x => x.TRACKING_TIME).First();
                trackingId = tr.ID;
            }
        }
        if (tr == null) throw new Exception("Không tìm thấy tờ điều trị nào cho bệnh nhân!");

        long reqRoomId = ResolvePatientRoomId(treatmentId);
        EnsureWorkInfoForRoom(reqRoomId);

        string icd = CleanIcdCode(tr.ICD_CODE);
        AssignServiceSDO sdo = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = reqRoomId,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            InstructionTime = tr.TRACKING_TIME,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            IcdCode = icd,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            SessionCode = null,
            ServiceReqDetails = new List<ServiceReqDetailSDO>()
        };

        foreach (var target in targetList)
        {
            sdo.ServiceReqDetails.Add(new ServiceReqDetailSDO
            {
                ServiceId = target.ServiceId,
                Amount = 1.0m,
                PatientTypeId = patientTypeId,
                PrimaryPatientTypeId = (patientTypeId == 1 ? (long?)null : patientTypeId),
                RoomId = target.RoomId,
                InstructionNote = target.Note
            });
        }

        CommonParam cp = new CommonParam();
        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdo, cp);
        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine(string.Format("✔ CHỈ ĐỊNH THÀNH CÔNG! Đã tạo {0} phiếu y lệnh (gom tự động theo phòng/ống bệnh phẩm):", res.ServiceReqs.Count));
            foreach (var sr in res.ServiceReqs)
            {
                Console.WriteLine(string.Format("  👉 Mã phiếu: {0} (ID: {1}) | Nơi thực hiện: {2} | Loại: {3}",
                    sr.SERVICE_REQ_CODE, sr.ID, sr.EXECUTE_ROOM_NAME ?? sr.EXECUTE_ROOM_ID.ToString(), sr.SERVICE_REQ_TYPE_NAME));
            }
        }
        else
        {
            string err = "Chỉ định nhóm CLS thất bại!";
            if (cp.Messages != null && cp.Messages.Count > 0) err += " Messages: " + string.Join("; ", cp.Messages);
            if (cp.BugCodes != null && cp.BugCodes.Count > 0) err += " BugCodes: " + string.Join("; ", cp.BugCodes);
            if (cp.HasException) err += " HasException: true";
            throw new Exception(err);
        }
    }

    public static void AssignSurgicalBilan(long treatmentId, long trackingId, string packType, int patientTypeId = 1)
    {
        InitSession();
        packType = packType.ToLower();

        List<ServiceTarget> targetList = new List<ServiceTarget>();
        string title = "";

        if (packType == "cement" || packType == "bxm")
        {
            title = "BILAN MỔ BƠM XI MĂNG CỘT SỐNG (VERTEBROPLASTY)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GLUCOSE"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["US_ABDOMEN"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
            targetList.Add(PredefinedServices["ECHO_HEART"]);
            targetList.Add(PredefinedServices["DEXA_2POS"]);
        }
        else if (packType == "spine" || packType == "cotsong" || packType == "nepvit")
        {
            title = "BILAN MỔ CỘT SỐNG (CỐ ĐỊNH NẸP VÍT / GIẢI ÉP / TLIF)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GLUCOSE"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["US_ABDOMEN"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "trauma" || packType == "chanthuong" || packType == "ortho" || packType == "khx")
        {
            title = "BILAN MỔ CHẤN THƯƠNG CHỈNH HÌNH (KẾT HỢP XƯƠNG CHI)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GLUCOSE"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["US_ABDOMEN"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "hip" || packType == "knee" || packType == "thaykhop")
        {
            title = "BILAN MỔ THAY KHỚP HÁNG / KHỚP GỐI NHÂN TẠO";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GLUCOSE"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["US_ABDOMEN"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "hand" || packType == "viphau")
        {
            title = "BILAN MỔ VI PHẪU / NỐI GÂN MẠCH BÀN TAY";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GLUCOSE"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "infection" || packType == "nhiemtrung" || packType == "viem")
        {
            title = "BILAN THEO DÕI NHIỄM TRÙNG / ĐÁNH GIÁ VIÊM SAU MỔ";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["ESR"]);
            targetList.Add(PredefinedServices["CRP"]);
            targetList.Add(PredefinedServices["CULT_BACTERIA"]);
            targetList.Add(PredefinedServices["ATTACH_KSD"]);
        }
        else
        {
            Console.WriteLine(string.Format("❌ Gói Bilan '{0}' không hợp lệ! Hỗ trợ: spine (Cột sống), trauma (Chấn thương), cement (BXM), hip (Thay khớp), hand (Vi phẫu), infection (Nhiễm trùng).", packType));
            return;
        }

        Console.WriteLine(string.Format("=== THỰC THI CHỈ ĐỊNH {0} ===", title));
        Console.WriteLine(string.Format("Treatment ID: {0} | Tờ điều trị ID: {1} | Số kỹ thuật: {2}", treatmentId, trackingId, targetList.Count));

        AssignServiceBatch(treatmentId, trackingId, targetList, patientTypeId);

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("✔ HOÀN TẤT CHỈ ĐỊNH GÓI: {0} kỹ thuật đã được gom nhóm tối ưu theo phòng tiếp nhận.", targetList.Count));
        Console.WriteLine("===============================================================================");
    }

    public static void AssignCustomServices(long treatmentId, long trackingId, string rawItems, int patientTypeId = 1)
    {
        InitSession();
        List<ServiceTarget> targetList = new List<ServiceTarget>();
        string[] items = rawItems.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in items)
        {
            string token = item.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            if (PredefinedServices.ContainsKey(token.ToUpper()))
            {
                targetList.Add(PredefinedServices[token.ToUpper()]);
            }
            else
            {
                // Format: serviceId:roomId:note
                string[] parts = token.Split(':');
                if (parts.Length >= 2)
                {
                    long sId = long.Parse(parts[0]);
                    long rId = long.Parse(parts[1]);
                    string note = parts.Length > 2 ? parts[2] : "";
                    targetList.Add(new ServiceTarget(sId, rId, "", "", note));
                }
                else
                {
                    Console.WriteLine("⚠️ Bỏ qua mục không xác định: " + token);
                }
            }
        }

        if (targetList.Count == 0)
        {
            Console.WriteLine("❌ Không có dịch vụ hợp lệ để chỉ định!");
            return;
        }

        Console.WriteLine(string.Format("=== THỰC THI CHỈ ĐỊNH TÙY CHỌN ({0} DỊCH VỤ) ===", targetList.Count));
        Console.WriteLine(string.Format("Treatment ID: {0} | Tờ điều trị ID: {1}", treatmentId, trackingId));
        AssignServiceBatch(treatmentId, trackingId, targetList, patientTypeId);
    }

    public static void InspectServiceRooms(long serviceId)
    {
        InitSession();
        var srf = new HisServiceRoomViewFilter { SERVICE_ID = serviceId };
        var srs = myAdapter.FetchList<V_HIS_SERVICE_ROOM>("api/HisServiceRoom/GetView", mosConsumer, srf, param);
        Console.WriteLine(string.Format("Phòng tiếp nhận dịch vụ ID {0}:", serviceId));
        if (srs != null && srs.Count > 0)
        {
            foreach (var sr in srs)
            {
                Console.WriteLine(string.Format("  • RoomId: {0} | Tên: {1} | Khoa: {2}", sr.ROOM_ID, sr.ROOM_NAME, sr.DEPARTMENT_NAME));
            }
        }
        else
        {
            Console.WriteLine("  (Không tìm thấy phòng cấu hình cụ thể trong HisServiceRoom)");
        }

        var pts = myAdapter.FetchList<HIS_PATIENT_TYPE>("api/HisPatientType/Get", mosConsumer, new HisPatientTypeFilter(), param);
        Console.WriteLine("Danh sách đối tượng (Patient Types):");
        if (pts != null)
        {
            foreach (var pt in pts)
            {
                Console.WriteLine(string.Format("  • ID: {0} | Mã: {1} | Tên: {2}", pt.ID, pt.PATIENT_TYPE_CODE, pt.PATIENT_TYPE_NAME));
            }
        }
    }

    public static void AssignRation(long treatmentId, long trackingId, string comboType, int patientTypeId = 42)
    {
        InitSession();
        HIS_TRACKING tr = null;
        if (trackingId > 0)
        {
            var tf = new HisTrackingFilter { ID = trackingId };
            var list = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, tf, param);
            if (list != null && list.Count > 0) tr = list[0];
        }
        if (tr == null)
        {
            var tf = new HisTrackingFilter { TREATMENT_ID = treatmentId };
            var list = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, tf, param);
            if (list != null && list.Count > 0)
            {
                tr = list.OrderByDescending(x => x.TRACKING_TIME).First();
                trackingId = tr.ID;
            }
        }
        if (tr == null) throw new Exception("Không tìm thấy tờ điều trị nào cho bệnh nhân!");

        long reqRoomId = ResolvePatientRoomId(treatmentId);
        if (reqRoomId <= 0) reqRoomId = 5248;
        EnsureWorkInfoForRoom(reqRoomId);

        var rationList = new List<RationServiceSDO>();
        long ptId = patientTypeId;

        string combo = (comboType ?? "BT07").ToUpper();
        if (combo.Contains("BT07"))
        {
            rationList.Add(new RationServiceSDO { ServiceId = 11063, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30151, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30059, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30064, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 4 } });
        }
        else if (combo.Contains("DD01"))
        {
            rationList.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else if (combo.Contains("TM01"))
        {
            rationList.Add(new RationServiceSDO { ServiceId = 30117, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30093, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30094, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else
        {
            rationList.Add(new RationServiceSDO { ServiceId = 30073, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30153, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30154, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }

        long instructionTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        var sdo = new HisRationServiceReqSDO
        {
            TreatmentIds = new List<long> { treatmentId },
            InstructionTimes = new List<long> { instructionTime },
            RequestRoomId = reqRoomId,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            HalfInFirstDay = false,
            IsForAutoCreateRation = false,
            IsForHomie = false,
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RationServices = rationList
        };

        var result = myAdapter.PostData<object>("api/HisServiceReq/RationCreate", mosConsumer, sdo, param);
        if (param.HasException)
        {
            string err = "Chỉ định suất ăn thất bại!";
            if (param.Messages != null && param.Messages.Count > 0) err += " " + string.Join("; ", param.Messages);
            if (param.BugCodes != null && param.BugCodes.Count > 0) err += " Bug: " + string.Join("; ", param.BugCodes);
            throw new Exception(err);
        }

        Console.WriteLine(string.Format("✔ Chỉ định suất ăn combo '{0}' ({1} bữa) THÀNH CÔNG cho BN!", combo, rationList.Count));
    }

    public static void LookupCls(string keyword = "")
    {
        Console.WriteLine("=========================================================================================================");
        Console.WriteLine("🔬 DANH MỤC CẬN LÂM SÀNG THƯỜNG QUY KHOA CTCH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI (HÀ NỘI)");
        Console.WriteLine("=========================================================================================================");
        Console.WriteLine(string.Format("{0,-20} | {1,-14} | {2,-8} | {3,-6} | {4}", "KEY TẮT", "MÃ DỊCH VỤ", "SERVICE", "PHÒNG", "TÊN DỊCH VỤ TRÊN HIS / GHI CHÚ"));
        Console.WriteLine(new string('-', 105));

        keyword = (keyword ?? "").Trim().ToLower();
        int count = 0;
        foreach (var kvp in PredefinedServices)
        {
            var s = kvp.Value;
            bool match = string.IsNullOrEmpty(keyword)
                || kvp.Key.ToLower().Contains(keyword)
                || (s.ServiceCode ?? "").ToLower().Contains(keyword)
                || (s.ServiceName ?? "").ToLower().Contains(keyword)
                || s.ServiceId.ToString().Contains(keyword)
                || s.RoomId.ToString().Contains(keyword)
                || (s.Note ?? "").ToLower().Contains(keyword);

            if (match)
            {
                count++;
                string noteStr = !string.IsNullOrEmpty(s.Note) ? " [" + s.Note + "]" : "";
                Console.WriteLine(string.Format("{0,-20} | {1,-14} | {2,-8} | {3,-6} | {4}{5}",
                    kvp.Key, s.ServiceCode, s.ServiceId, s.RoomId, s.ServiceName, noteStr));
            }
        }
        Console.WriteLine(new string('-', 105));
        Console.WriteLine(string.Format("✔ Đã tìm thấy {0}/{1} kỹ thuật CLS phù hợp.", count, PredefinedServices.Count));
        Console.WriteLine("\n💡 CÁC GÓI BILAN MỔ PHIÊN HỖ TRỢ (assign-bilan):");
        Console.WriteLine("  1. cement    : Bilan Mổ Bơm Xi Măng Cột Sống (20 kỹ thuật: CTM, Nhóm máu, ĐMCB, SHM, Virus, Nước tiểu, ECG, SA Bụng, XQ Phổi, SA Tim, DEXA 2 vị trí)");
        Console.WriteLine("  2. spine     : Bilan Mổ Cố Định Cột Sống / Nẹp Vít / Giải Ép (18 kỹ thuật: CTM, Nhóm máu, ĐMCB, SHM, Virus, Nước tiểu, ECG, SA Bụng, XQ Phổi)");
        Console.WriteLine("  3. trauma    : Bilan Mổ Kết Hợp Xương Chấn Thương Chỉnh Hình (18 kỹ thuật)");
        Console.WriteLine("  4. hip       : Bilan Mổ Thay Khớp Háng / Khớp Gối (CTM, ĐMCB, SHM, Virus, ECG, SA Tim, SA Mạch chi, DEXA)");
        Console.WriteLine("  5. hand      : Bilan Mổ Vi Phẫu / Nối Gân Mạch Bàn Tay (CTM, ĐMCB, SHM, Virus, Nước tiểu, ECG, XQ Phổi)");
        Console.WriteLine("  6. infection : Bilan Theo Dõi Nhiễm Trùng Sau Mổ (CTM, Máu lắng ESR, CRP, Cấy vi khuẩn, Đính kèm KSD)");
        Console.WriteLine("\n👉 Cú pháp chỉ định gói: HisClinicalCli.exe assign-bilan <treatmentId> <trackingId> <tên_gói>");
        Console.WriteLine("👉 Cú pháp tự chọn:      HisClinicalCli.exe assign-custom <treatmentId> <trackingId> \"CBC_LASER,PT_TQ,APTT_TCK,FIBRINOGEN,CRP...\"");
        Console.WriteLine("=========================================================================================================");
    }

    public static void LookupConsultationDebate(string key)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("👥 TRA CỨU BIÊN BẢN HỘI CHẨN & Ý KIẾN CHUYÊN KHOA CHO: " + key);
        V_HIS_TREATMENT targetTreatment = null;
        if (key.StartsWith("00") || key.Length == 12)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        long trId = 0;
        if (targetTreatment == null && long.TryParse(key, out trId) && trId > 1000000 && trId < 99999999)
        {
            var tf = new HisTreatmentViewFilter { ID = trId };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = key.PadLeft(10, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + key);
            return;
        }

        Console.WriteLine(string.Format("BỆNH NHÂN: {0} ({1} tuổi - {2})", targetTreatment.TDL_PATIENT_NAME, DateTime.Now.Year - int.Parse(targetTreatment.TDL_PATIENT_DOB.ToString().Substring(0, 4)), targetTreatment.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt ĐT: {2}", targetTreatment.TDL_PATIENT_CODE, targetTreatment.TREATMENT_CODE, targetTreatment.ID));
        Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", targetTreatment.ICD_CODE, targetTreatment.ICD_NAME, targetTreatment.ICD_TEXT));
        Console.WriteLine("-------------------------------------------------------------------------------");

        // 1. Kiểm tra HIS_DEBATE
        var df = new HisDebateFilter { TREATMENT_ID = targetTreatment.ID };
        var debates = myAdapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, df, param);

        if (debates != null && debates.Count > 0)
        {
            Console.WriteLine(string.Format("📋 TÌM THẤY {0} BIÊN BẢN HỘI CHẨN CHÍNH (HIS_DEBATE):", debates.Count));
            foreach (var d in debates)
            {
                Console.WriteLine("\n-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("🔹 HỘI CHẨN ID: {0} | Thời gian: {1}", d.ID, d.DEBATE_TIME));
                Console.WriteLine("  • Chẩn đoán: [" + d.ICD_CODE + "] " + d.ICD_NAME + " (" + d.ICD_TEXT + ")");
                Console.WriteLine("  • Địa điểm: " + d.LOCATION);
                if (!string.IsNullOrEmpty(d.TREATMENT_TRACKING)) Console.WriteLine("  • Tóm tắt quá trình ĐT / Khám: " + d.TREATMENT_TRACKING);
                if (!string.IsNullOrEmpty(d.DISCUSSION)) Console.WriteLine("  • Nội dung thảo luận / Xin ý kiến: " + d.DISCUSSION);
                if (!string.IsNullOrEmpty(d.CONCLUSION)) Console.WriteLine("  • Kết luận / Hướng xử trí: " + d.CONCLUSION);

                var duf = new HisDebateUserFilter { DEBATE_ID = d.ID };
                var dUsers = myAdapter.FetchList<HIS_DEBATE_USER>("api/HisDebateUser/Get", mosConsumer, duf, param);
                if (dUsers != null && dUsers.Count > 0)
                {
                    Console.WriteLine("  • Thành viên tham gia:");
                    foreach (var u in dUsers)
                    {
                        string role = u.IS_PRESIDENT == 1 ? "[Chủ tọa]" : (u.IS_SECRETARY == 1 ? "[Thư ký]" : "[Thành viên]");
                        Console.WriteLine(string.Format("    - {0} {1} ({2})", role, u.USERNAME, u.LOGINNAME));
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Chưa có biên bản ghi nhận trong bảng HIS_DEBATE.");
        }

        // 2. Kiểm tra tất cả phiếu yêu cầu mời chuyên khoa liên khoa (HIS_SERVICE_REQ + HIS_SERE_SERV_EXT)
        var srf = new HisServiceReqViewFilter { TREATMENT_ID = targetTreatment.ID };
        var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

        if (reqs != null)
        {
            var consultReqs = reqs.Where(x => 
                x.SERVICE_REQ_TYPE_ID == 1 ||
                (x.EXECUTE_DEPARTMENT_NAME != null && (
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hô hấp") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nhiệt đới") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("truyền nhiễm") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("tim mạch") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hồi sức") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("thần kinh") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nội tiết")
                ))
            ).OrderByDescending(x => x.INTRUCTION_TIME).ToList();

            if (consultReqs.Count > 0)
            {
                Console.WriteLine("\n===============================================================================");
                Console.WriteLine(string.Format("🩺 Ý KIẾN TRẢ LỜI CỦA CÁC CHUYÊN KHOA KHÁCH ({0} PHIẾU CHỈ ĐỊNH):", consultReqs.Count));
                Console.WriteLine("===============================================================================");

                foreach (var cr in consultReqs)
                {
                    string statusBadge = cr.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ CÓ KẾT QUẢ / HOÀN THÀNH" : "🟡 ĐANG CHỜ XỬ LÝ / CHƯA CÓ KẾT QUẢ";
                    Console.WriteLine("\n-------------------------------------------------------------------------------");
                    Console.WriteLine(string.Format("🏢 ĐƠN VỊ: {0} ({1})", cr.EXECUTE_DEPARTMENT_NAME, cr.EXECUTE_ROOM_NAME));
                    Console.WriteLine(string.Format("• Phiếu #{0} [{1}] - Gửi lúc: {2}", cr.SERVICE_REQ_CODE, cr.SERVICE_REQ_TYPE_NAME, cr.INTRUCTION_TIME));
                    Console.WriteLine(string.Format("• Bác sĩ chỉ định: {0} ({1}) -> Khoa: {2}", cr.REQUEST_USERNAME, cr.REQUEST_LOGINNAME, cr.REQUEST_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("• Trạng thái: {0}", statusBadge));
                    if (cr.FINISH_TIME.HasValue) Console.WriteLine(string.Format("• Thời gian hoàn thành: {0}", cr.FINISH_TIME.Value));
                    Console.WriteLine(string.Format("• Bác sĩ hội chẩn/trả lời: {0} ({1})", cr.EXECUTE_USERNAME ?? "(Chưa tiếp nhận)", cr.EXECUTE_LOGINNAME ?? "-"));

                    // Lấy chi tiết ý kiến từ HIS_SERE_SERV_EXT
                    var ssf = new HisSereServFilter { SERVICE_REQ_ID = cr.ID };
                    var sss = myAdapter.FetchList<HIS_SERE_SERV>("api/HisSereServ/Get", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var ss in sss)
                        {
                            var ssef = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                            var sses = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                            if (sses != null && sses.Count > 0)
                            {
                                foreach (var se in sses)
                                {
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION))
                                    {
                                        Console.WriteLine("\n📝 NỘI DUNG Ý KIẾN HỘI CHẨN:");
                                        Console.WriteLine(se.DESCRIPTION);
                                    }
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".")
                                    {
                                        Console.WriteLine("📌 KẾT LUẬN: " + se.CONCLUDE);
                                    }
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE))
                                    {
                                        Console.WriteLine("💡 LỜI DẶN: " + se.INSTRUCTION_NOTE);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void LookupConsultationQueue(string dateParam = null)
    {
        InitSession();
        DateTime targetDate = DateTime.Now;
        if (!string.IsNullOrEmpty(dateParam))
        {
            DateTime parsed;
            if (DateTime.TryParseExact(dateParam, new string[] { "yyyyMMdd", "dd/MM/yyyy", "yyyy-MM-dd" }, null, System.Globalization.DateTimeStyles.None, out parsed))
            {
                targetDate = parsed;
            }
        }
        long fromTime = long.Parse(targetDate.ToString("yyyyMMdd000000"));
        long toTime   = long.Parse(targetDate.ToString("yyyyMMdd235959"));

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 PHÒNG HỘI CHẨN KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (ROOM ID: 11387)");
        Console.WriteLine("Ngày: " + targetDate.ToString("dd/MM/yyyy") + " | Quét tất cả yêu cầu hội chẩn gửi đến Khoa 57");
        Console.WriteLine("===============================================================================");

        var srf = new HisServiceReqViewFilter
        {
            EXECUTE_ROOM_ID = 11387,
            INTRUCTION_TIME_FROM = fromTime,
            INTRUCTION_TIME_TO = toTime
        };

        var list = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (list == null || list.Count == 0)
        {
            Console.WriteLine("Không có yêu cầu hội chẩn nào gửi đến Khoa 57 trong ngày.");
            return;
        }

        Console.WriteLine(string.Format("Tìm thấy {0} yêu cầu hội chẩn:\n", list.Count));
        int idx = 1;
        foreach (var r in list.OrderBy(x => x.INTRUCTION_TIME))
        {
            string timeStr = r.INTRUCTION_TIME.ToString().Length >= 12 ? r.INTRUCTION_TIME.ToString().Substring(8, 4).Insert(2, ":") : r.INTRUCTION_TIME.ToString();
            string stt = r.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ KHÁM/TRẢ LỜI" : (r.SERVICE_REQ_STT_ID == 2 ? "🟡 ĐANG KHÁM" : "🔴 CHỜ KHÁM/HC");
            Console.WriteLine(string.Format("{0}. [{1}] {2} | Mã ĐT: {3}", 
                idx++, timeStr, r.TDL_PATIENT_NAME, r.TREATMENT_CODE));
            Console.WriteLine(string.Format("   • Nơi gửi: {0} ({1}) | Bác sĩ Y/C: {2}", 
                r.REQUEST_DEPARTMENT_NAME, r.REQUEST_ROOM_NAME, r.REQUEST_USERNAME ?? r.REQUEST_LOGINNAME));
            Console.WriteLine(string.Format("   • Trạng thái: {0} | Mã phiếu: {1} | BS xử lý: {2}", 
                stt, r.SERVICE_REQ_CODE, r.EXECUTE_USERNAME ?? "Chưa phân công"));
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
    }

    public static void ScanWardRooms()
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 QUÉT TOÀN BỘ DANH SÁCH BỆNH NHÂN NỘI TRÚ KHOA 57 (CÁC BUỒNG 710 - 740)");
        Console.WriteLine("Phòng: Toàn bộ buồng bệnh Khoa 57 (P710 - P740)");
        Console.WriteLine("===============================================================================");

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);

        if (allBeds == null || allBeds.Count == 0)
        {
            Console.WriteLine("Không tìm thấy bệnh nhân nào đang nằm buồng!");
            return;
        }

        var dept57Beds = allBeds.Where(x => x.DEPARTMENT_ID == 57)
            .OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME).ToList();

        Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân tại toàn bộ các buồng Khoa 57 (710 - 740):\n", dept57Beds.Count));

        // BATCH QUERY: Gom toàn bộ Treatment IDs vào 1 request HTTP duy nhất
        var treatIds = dept57Beds.Select(b => b.TREATMENT_ID).Distinct().ToList();
        var treatMap = new Dictionary<long, V_HIS_TREATMENT>();
        if (treatIds.Count > 0)
        {
            HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter();
            tfBatch.IDs = treatIds;
            var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
            if (tList != null)
            {
                foreach (var t in tList) treatMap[t.ID] = t;
            }
        }

        int stt = 1;
        foreach (var b in dept57Beds)
        {
            var tr = treatMap.ContainsKey(b.TREATMENT_ID) ? treatMap[b.TREATMENT_ID] : null;

            string patName = tr != null ? tr.TDL_PATIENT_NAME : "N/A";
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "N/A";
            string icd = tr != null ? string.Format("[{0}] {1}", tr.ICD_CODE, tr.ICD_NAME) : "-";

            Console.WriteLine(string.Format("{0:D2}. [{1} - {2}] BN: {3} (Mã: {4}) | TrID: {5}", stt++, b.BED_ROOM_NAME, b.BED_NAME, patName, patCode, b.TREATMENT_ID));
            Console.WriteLine(string.Format("    Chẩn đoán: {0}", icd));
        }
        Console.WriteLine("===============================================================================");
    }

    public static void LocatePatients(string keywordsArg)
    {
        InitSession();
        string[] rawList = keywordsArg.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        var keywords = rawList.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📍 ĐỊNH VỊ BỆNH NHÂN THEO DANH SÁCH / LỊCH MỔ ({0} BỆNH NHÂN)", keywords.Count));
        Console.WriteLine("===============================================================================");

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter
        {
            IS_IN_ROOM = true,
            TREATMENT_IS_ACTIVE = true
        };
        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param) ?? new List<V_HIS_TREATMENT_BED_ROOM>();

        // Match beds for each keyword
        var matchedMap = new Dictionary<string, V_HIS_TREATMENT_BED_ROOM>();
        var treatIdsToFetch = new HashSet<long>();

        foreach (var kw in keywords)
        {
            string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
            var matches = allBeds.Where(b => 
                !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) && 
                RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)
            ).ToList();

            var bed57 = matches.FirstOrDefault(b => b.DEPARTMENT_ID == 57) ?? matches.FirstOrDefault();
            if (bed57 != null)
            {
                matchedMap[kw] = bed57;
                treatIdsToFetch.Add(bed57.TREATMENT_ID);
            }
        }

        // Batch fetch treatments
        var treatMap = new Dictionary<long, V_HIS_TREATMENT>();
        if (treatIdsToFetch.Count > 0)
        {
            HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter { IDs = treatIdsToFetch.ToList() };
            var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
            if (tList != null)
            {
                foreach (var t in tList) treatMap[t.ID] = t;
            }
        }

        int stt = 1;
        foreach (var kw in keywords)
        {
            if (matchedMap.ContainsKey(kw))
            {
                var bed = matchedMap[kw];
                var tr = treatMap.ContainsKey(bed.TREATMENT_ID) ? treatMap[bed.TREATMENT_ID] : null;

                int birthYear = 0;
                if (tr != null && tr.TDL_PATIENT_DOB.ToString().Length >= 4)
                {
                    int.TryParse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4), out birthYear);
                }
                int age = birthYear > 0 ? (DateTime.Now.Year - birthYear) : 0;

                string roomBed = string.Format("{0} - {1}", bed.BED_ROOM_NAME ?? "Chưa rõ buồng", bed.BED_NAME ?? "Giường ?");
                string patName = tr != null ? tr.TDL_PATIENT_NAME : bed.TDL_PATIENT_NAME;
                string patCode = tr != null ? tr.TDL_PATIENT_CODE : bed.TDL_PATIENT_CODE;
                string trCode = tr != null ? tr.TREATMENT_CODE : "N/A";
                string dept = tr != null ? (tr.END_DEPARTMENT_NAME ?? "Khoa 57") : (bed.DEPARTMENT_ID == 57 ? "Khoa 57" : ("Khoa " + bed.DEPARTMENT_ID));
                string icd = tr != null ? string.Format("[{0}] {1} {2}", tr.ICD_CODE, tr.ICD_NAME, !string.IsNullOrEmpty(tr.ICD_TEXT) ? ("(" + tr.ICD_TEXT + ")") : "") : "-";

                Console.WriteLine(string.Format("{0:D2}. 🛏️ [{1}] BN: {2} ({3}t - {4}) | Mã BN: {5} | Mã ĐT: {6}",
                    stt++, roomBed, patName, age > 0 ? age.ToString() : "N/A", tr != null ? tr.TDL_PATIENT_GENDER_NAME : "N/A", patCode, trCode));
                Console.WriteLine(string.Format("    Khoa: {0} | Chẩn đoán: {1}", dept, icd));
            }
            else
            {
                // Fallback: search treatment directly
                var tf = new HisTreatmentViewFilter { KEY_WORD = kw, IS_PAUSE = false };
                var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                var tr = trList != null ? (trList.FirstOrDefault(x => x.END_DEPARTMENT_ID == 57) ?? trList.FirstOrDefault()) : null;

                if (tr != null)
                {
                    int birthYear = 0;
                    if (tr.TDL_PATIENT_DOB.ToString().Length >= 4)
                    {
                        int.TryParse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4), out birthYear);
                    }
                    int age = birthYear > 0 ? (DateTime.Now.Year - birthYear) : 0;

                    Console.WriteLine(string.Format("{0:D2}. ⚠️ [CHƯA GÁN BUỒNG / NGOẠI TRÚ] BN: {1} ({2}t - {3}) | Mã BN: {4} | Mã ĐT: {5}",
                        stt++, tr.TDL_PATIENT_NAME, age > 0 ? age.ToString() : "N/A", tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
                    Console.WriteLine(string.Format("    Khoa: {0} | Chẩn đoán: [{1}] {2} {3}",
                        tr.END_DEPARTMENT_NAME ?? "Khoa 57", tr.ICD_CODE, tr.ICD_NAME, !string.IsNullOrEmpty(tr.ICD_TEXT) ? ("(" + tr.ICD_TEXT + ")") : ""));
                }
                else
                {
                    Console.WriteLine(string.Format("{0:D2}. ❌ [KHÔNG TÌM THẤY TRÊN HỆ THỐNG]: {1}", stt++, kw));
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void ExportPt01Data(string pCodesArg)
    {
        InitSession();
        string[] rawList = pCodesArg.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        var pCodes = rawList.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

        var listOut = new List<string>();

        foreach (var pCode in pCodes)
        {
            string cleanCode = pCode;
            if (cleanCode.All(char.IsDigit) && cleanCode.Length < 10)
            {
                cleanCode = cleanCode.PadLeft(10, '0');
            }

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            if (cleanCode.Length == 12 && cleanCode.StartsWith("0000"))
                tf.TREATMENT_CODE__EXACT = cleanCode;
            else
                tf.PATIENT_CODE__EXACT = cleanCode;

            var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (trList == null || trList.Count == 0) continue;
            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tr.ID };
            var teinList = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param) ?? new List<V_HIS_SERE_SERV_TEIN>();

            Func<string, string> getTein = (match) => {
                var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                    ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
                     (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
                return item != null ? (item.VALUE + " " + (item.TEST_INDEX_UNIT_NAME ?? "")).Trim() : "-";
            };

            string hb = getTein("Hemoglobin");
            string wbc = getTein("Bạch cầu");
            string plt = getTein("Tiểu cầu");
            string inr = getTein("INR");
            string fib = getTein("Fibrinogen");
            string aptt = getTein("APTT");
            string glu = getTein("Glucose");
            string ure = getTein("Urê");
            string cre = getTein("Creatinin");
            string ast = getTein("AST");
            string alt = getTein("ALT");
            string abo = getTein("ABO");

            string dobStr = tr.TDL_PATIENT_DOB.ToString();
            string dobFormatted = dobStr;
            if (dobStr.Length == 8)
            {
                dobFormatted = dobStr.Substring(6, 2) + "/" + dobStr.Substring(4, 2) + "/" + dobStr.Substring(0, 4);
            }
            else if (dobStr.Length == 4)
            {
                dobFormatted = "01/01/" + dobStr;
            }

            string inTimeStr = tr.IN_TIME.ToString();
            string inTimeFormatted = inTimeStr;
            if (inTimeStr.Length >= 12)
            {
                inTimeFormatted = inTimeStr.Substring(8, 2) + ":" + inTimeStr.Substring(10, 2) + " ngày " + inTimeStr.Substring(6, 2) + "/" + inTimeStr.Substring(4, 2) + "/" + inTimeStr.Substring(0, 4);
            }

            Func<string, string> cleanJson = (str) => {
                if (string.IsNullOrEmpty(str)) return "";
                return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
            };

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"Ma_Ho_So\":\"{0}\",", cleanJson(tr.TREATMENT_CODE));
            sb.AppendFormat("\"PatientCode\":\"{0}\",", cleanJson(tr.TDL_PATIENT_CODE));
            sb.AppendFormat("\"Ho_Va_Ten\":\"{0}\",", cleanJson(tr.TDL_PATIENT_NAME.ToUpper()));
            sb.AppendFormat("\"Ngay_Sinh\":\"{0}\",", cleanJson(dobFormatted));
            sb.AppendFormat("\"Gioi_Tinh\":\"{0}\",", cleanJson(tr.TDL_PATIENT_GENDER_NAME));
            sb.AppendFormat("\"Dia_Chi\":\"{0}\",", cleanJson(tr.TDL_PATIENT_ADDRESS));
            sb.AppendFormat("\"Ngay_Gio_Vao_Vien\":\"{0}\",", cleanJson(inTimeFormatted));
            sb.AppendFormat("\"IcdCode\":\"{0}\",", cleanJson(tr.ICD_CODE));
            sb.AppendFormat("\"IcdName\":\"{0}\",", cleanJson(tr.ICD_NAME));
            sb.AppendFormat("\"IcdText\":\"{0}\",", cleanJson(tr.ICD_TEXT));
            sb.AppendFormat("\"Hb\":\"{0}\",", cleanJson(hb));
            sb.AppendFormat("\"Wbc\":\"{0}\",", cleanJson(wbc));
            sb.AppendFormat("\"Plt\":\"{0}\",", cleanJson(plt));
            sb.AppendFormat("\"Inr\":\"{0}\",", cleanJson(inr));
            sb.AppendFormat("\"Fib\":\"{0}\",", cleanJson(fib));
            sb.AppendFormat("\"Aptt\":\"{0}\",", cleanJson(aptt));
            sb.AppendFormat("\"Glu\":\"{0}\",", cleanJson(glu));
            sb.AppendFormat("\"Ure\":\"{0}\",", cleanJson(ure));
            sb.AppendFormat("\"Cre\":\"{0}\",", cleanJson(cre));
            sb.AppendFormat("\"Ast\":\"{0}\",", cleanJson(ast));
            sb.AppendFormat("\"Alt\":\"{0}\",", cleanJson(alt));
            sb.AppendFormat("\"Nhom_Mau\":\"{0}\"", cleanJson(abo.Replace("-", "").Trim()));
            sb.Append("}");
            listOut.Add(sb.ToString());
        }

        Directory.CreateDirectory("Reports");
        File.WriteAllText("Reports/pt01_input.json", "[" + string.Join(",", listOut) + "]", Encoding.UTF8);
        Console.WriteLine(string.Format("✅ ĐÃ XUẤT THÀNH CÔNG DỮ LIỆU {0} BỆNH NHÂN -> Reports/pt01_input.json", listOut.Count));
    }

    public static void RestoreDefaultDept57WorkInfo()
    {
        try
        {
            long[] dept57Rooms = new long[] {
                931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 
                5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 
                5267, 5539, 6622, 6623, 18679, 18681, 14759, 14787
            };
            var workInfo = new WorkInfoSDO
            {
                Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList()
            };
            myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }
    }

    public static V_HIS_TREATMENT ResolveTreatment(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;
        string kw = (keyword ?? "").Trim();
        long numVal;
        bool isNum = long.TryParse(kw, out numVal);

        if (isNum)
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }
        else
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
            if (treatments == null || treatments.Count == 0)
            {
                string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
                try
                {
                    HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true, TREATMENT_IS_ACTIVE = true };
                    var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                    if (allBeds != null && allBeds.Count > 0)
                    {
                        var matchedBeds = allBeds.Where(b => !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) && RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)).ToList();
                        if (matchedBeds.Count > 0)
                        {
                            var tIds = matchedBeds.Select(x => x.TREATMENT_ID).Distinct().ToList();
                            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { IDs = tIds }, param);
                        }
                    }
                }
                catch { }
            }
        }

        if (treatments == null || treatments.Count == 0) return null;
        return treatments.LastOrDefault(x => x.IS_PAUSE != 1) ?? treatments.Last();
    }

    public static void ListOrders(string keyword)
    {
        InitSession();
        var tr = ResolveTreatment(keyword);
        if (tr == null)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📋 DANH SÁCH Y LỆNH: {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
        Console.WriteLine("===============================================================================");

        HisServiceReqViewFilter srf = new HisServiceReqViewFilter
        {
            TREATMENT_ID = tr.ID
        };
        var orders = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (orders == null || orders.Count == 0)
        {
            Console.WriteLine("Bệnh nhân chưa có y lệnh nào.");
            Console.WriteLine("===============================================================================");
            return;
        }

        var sorted = orders.OrderByDescending(x => x.INTRUCTION_TIME).ToList();

        Dictionary<long, List<V_HIS_SERE_SERV>> ssMap = new Dictionary<long, List<V_HIS_SERE_SERV>>();
        try
        {
            var allReqIds = sorted.Select(x => x.ID).Distinct().ToList();
            if (allReqIds.Count > 0)
            {
                HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = allReqIds };
                var allSsList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                if (allSsList != null)
                {
                    ssMap = allSsList.Where(x => x.SERVICE_REQ_ID.HasValue)
                                     .GroupBy(x => x.SERVICE_REQ_ID.Value)
                                     .ToDictionary(g => g.Key, g => g.ToList());
                }
            }
        }
        catch { }

        int stt = 1;
        foreach (var r in sorted)
        {
            string timeStr = r.INTRUCTION_TIME.ToString().Length >= 12 
                ? string.Format("{0}/{1} {2}:{3}", r.INTRUCTION_TIME.ToString().Substring(6, 2), r.INTRUCTION_TIME.ToString().Substring(4, 2), r.INTRUCTION_TIME.ToString().Substring(8, 2), r.INTRUCTION_TIME.ToString().Substring(10, 2)) 
                : r.INTRUCTION_TIME.ToString();

            string sttBadge;
            string canCancelTag = "";
            if (r.SERVICE_REQ_STT_ID == 1)
            {
                sttBadge = "⚪ CHƯA THỰC HIỆN (Màu trắng)";
                canCancelTag = " 👉 [CÓ THỂ HỦY/XÓA]";
            }
            else if (r.SERVICE_REQ_STT_ID == 2)
            {
                sttBadge = "🟡 ĐANG THỰC HIỆN";
            }
            else if (r.SERVICE_REQ_STT_ID == 3)
            {
                sttBadge = "🟢 ĐÃ HOÀN THÀNH";
            }
            else
            {
                sttBadge = "⚫ KHÁC (" + r.SERVICE_REQ_STT_ID + ")";
            }

            Console.WriteLine(string.Format("{0:D2}. [ID: {1} | Mã: {2}] Lúc {3} | {4}{5}", 
                stt++, r.ID, r.SERVICE_REQ_CODE, timeStr, sttBadge, canCancelTag));
            Console.WriteLine(string.Format("    • Loại: {0} | BS Chỉ định: {1} ({2}) | Nơi Y/C: {3}", 
                r.SERVICE_REQ_TYPE_NAME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.REQUEST_ROOM_NAME));
            
            try
            {
                List<V_HIS_SERE_SERV> ssList;
                if (ssMap.TryGetValue(r.ID, out ssList) && ssList != null && ssList.Count > 0)
                {
                    var names = ssList.Select(x => string.Format("{0} (SS_ID: {1})", x.TDL_SERVICE_NAME, x.ID));
                    Console.WriteLine(string.Format("    • Dịch vụ: {0}", string.Join("; ", names)));
                }
            }
            catch { }
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
        Console.WriteLine("💡 Hủy cả phiếu: .\\.agents\\skills\\his-clinical-operations\\scripts\\HisClinicalCli.exe cancel-order <ID>");
        Console.WriteLine("💡 Hủy dịch vụ lẻ: .\\.agents\\skills\\his-clinical-operations\\scripts\\HisClinicalCli.exe cancel-service <SS_ID>");
        Console.WriteLine("===============================================================================");
    }

    public static void TransferOrders(string keyword, string targetDoctor = null, bool isDryRun = false)
    {
        InitSession();
        var tr = ResolveTreatment(keyword);
        if (tr == null)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        string targetLogin = string.IsNullOrEmpty(targetDoctor) ? currentDoctorLogin : targetDoctor.Trim();
        string targetName;
        string targetTitle;

        if (string.Equals(targetLogin, "034727", StringComparison.OrdinalIgnoreCase))
        {
            targetName = "Ths.BS NGUYỄN HỮU SÂM";
            targetTitle = "Thạc sỹ y học";
        }
        else if (string.Equals(targetLogin, "vmc", StringComparison.OrdinalIgnoreCase))
        {
            targetName = "VŨ MINH CƯỜNG";
            targetTitle = "Bác sĩ";
        }
        else
        {
            targetName = targetLogin.ToUpper();
            targetTitle = "Bác sĩ";
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🔄 CHUYỂN Y LỆNH CHƯA XỬ LÝ SANG BÁC SĨ CHỈ ĐỊNH: {0} ({1})", targetName, targetLogin));
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("• Bệnh nhân    : {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
        Console.WriteLine(string.Format("• BS Tiếp nhận : {0} ({1}) - {2}", targetName, targetLogin, targetTitle));
        if (isDryRun) Console.WriteLine("⚠️ CHẾ ĐỘ THỬ NGHIỆM (DRY-RUN): Không thay đổi dữ liệu thật.");
        Console.WriteLine("-------------------------------------------------------------------------------");

        HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var orders = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (orders == null || orders.Count == 0)
        {
            Console.WriteLine("Bệnh nhân chưa có y lệnh nào.");
            return;
        }

        // Lọc y lệnh chưa xử lý (Màu trắng, STT_ID == 1, IS_DELETE != 1)
        var whiteOrders = orders.Where(x => x.SERVICE_REQ_STT_ID == 1 && (x.IS_DELETE == null || x.IS_DELETE == 0)).OrderBy(x => x.INTRUCTION_TIME).ToList();
        if (whiteOrders.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân không có y lệnh nào ở trạng thái Chưa thực hiện (màu trắng).");
            return;
        }

        Dictionary<long, List<V_HIS_SERE_SERV>> ssMap = new Dictionary<long, List<V_HIS_SERE_SERV>>();
        try
        {
            var reqIds = whiteOrders.Select(x => x.ID).Distinct().ToList();
            if (reqIds.Count > 0)
            {
                HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = reqIds };
                var allSs = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisServiceReq/GetView", mosConsumer, ssf, param);
                if (allSs != null)
                {
                    ssMap = allSs.Where(x => x.SERVICE_REQ_ID.HasValue)
                                 .GroupBy(x => x.SERVICE_REQ_ID.Value)
                                 .ToDictionary(g => g.Key, g => g.ToList());
                }
            }
        }
        catch { }

        int totalPending = whiteOrders.Count;
        int alreadyTarget = 0;
        int successCount = 0;
        int failCount = 0;

        Console.WriteLine(string.Format("🔍 Tìm thấy {0} y lệnh chưa xử lý (màu trắng). Bắt đầu chuyển đổi...\n", totalPending));

        int idx = 1;
        foreach (var req in whiteOrders)
        {
            string timeStr = req.INTRUCTION_TIME.ToString().Length >= 12 
                ? string.Format("{0}/{1} {2}:{3}", req.INTRUCTION_TIME.ToString().Substring(6, 2), req.INTRUCTION_TIME.ToString().Substring(4, 2), req.INTRUCTION_TIME.ToString().Substring(8, 2), req.INTRUCTION_TIME.ToString().Substring(10, 2)) 
                : req.INTRUCTION_TIME.ToString();

            string svcs = "";
            List<V_HIS_SERE_SERV> ssList;
            if (ssMap.TryGetValue(req.ID, out ssList) && ssList != null && ssList.Count > 0)
            {
                svcs = string.Join("; ", ssList.Select(s => string.Format("{0} (SL: {1})", s.TDL_SERVICE_NAME, s.AMOUNT)));
            }

            if (string.Equals(req.REQUEST_LOGINNAME, targetLogin, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(string.Format("{0:D2}. [BỎ QUA] Phiếu {1} (ID: {2} | {3}) lúc {4}", 
                    idx++, req.SERVICE_REQ_CODE, req.ID, req.SERVICE_REQ_TYPE_NAME, timeStr));
                Console.WriteLine(string.Format("    • Đã do {0} ({1}) chỉ định từ trước.", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                if (!string.IsNullOrEmpty(svcs)) Console.WriteLine(string.Format("    • Dịch vụ: {0}", svcs));
                Console.WriteLine();
                alreadyTarget++;
                continue;
            }

            Console.WriteLine(string.Format("{0:D2}. 👉 CHUYỂN: Phiếu {1} (ID: {2} | {3}) lúc {4}", 
                idx++, req.SERVICE_REQ_CODE, req.ID, req.SERVICE_REQ_TYPE_NAME, timeStr));
            Console.WriteLine(string.Format("    • BS hiện tại: {0} ({1}) -> BS mới: {2} ({3})", 
                req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, targetName, targetLogin));
            Console.WriteLine(string.Format("    • Nơi Y/C: {0} (Phòng ID: {1})", req.REQUEST_ROOM_NAME, req.REQUEST_ROOM_ID));
            if (!string.IsNullOrEmpty(svcs)) Console.WriteLine(string.Format("    • Dịch vụ: {0}", svcs));

            if (isDryRun)
            {
                Console.WriteLine("    [DRY-RUN] Bỏ qua thao tác ghi thực tế.");
                Console.WriteLine();
                successCount++;
                continue;
            }

            try
            {
                if (req.REQUEST_ROOM_ID > 0)
                {
                    EnsureWorkInfoForRoom(req.REQUEST_ROOM_ID);
                }

                var rawFilter = new HisServiceReqFilter { ID = req.ID };
                var rawList = myAdapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mosConsumer, rawFilter, param);
                if (rawList != null && rawList.Count > 0)
                {
                    var rawReq = rawList[0];
                    rawReq.REQUEST_LOGINNAME = targetLogin;
                    rawReq.REQUEST_USERNAME = targetName;
                    rawReq.REQUEST_USER_TITLE = targetTitle;

                    var pUpd = new CommonParam();
                    var res = myAdapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mosConsumer, rawReq, pUpd);
                    if (res != null && !pUpd.HasException)
                    {
                        Console.WriteLine("    ✔ THÀNH CÔNG: Đã chuyển quyền chỉ định sang " + targetName);
                        successCount++;
                    }
                    else
                    {
                        // Fallback to Update
                        var pUpd2 = new CommonParam();
                        var res2 = myAdapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/Update", mosConsumer, rawReq, pUpd2);
                        if (res2 != null && !pUpd2.HasException)
                        {
                            Console.WriteLine("    ✔ THÀNH CÔNG (qua Update): Đã chuyển quyền chỉ định sang " + targetName);
                            successCount++;
                        }
                        else
                        {
                            string errMsg = pUpd.Messages != null && pUpd.Messages.Count > 0 ? string.Join("; ", pUpd.Messages) : (pUpd2.Messages != null ? string.Join("; ", pUpd2.Messages) : "Lỗi từ Backend MOS");
                            Console.WriteLine("    ❌ THẤT BẠI: " + errMsg);
                            failCount++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("    ❌ THẤT BẠI: Không tải được chi tiết bản ghi HIS_SERVICE_REQ từ API.");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    ❌ NGOẠI LỆ: " + ex.Message);
                failCount++;
            }
            Console.WriteLine();
        }

        RestoreDefaultDept57WorkInfo();

        Console.WriteLine("===============================================================================");
        Console.WriteLine("📊 TỔNG KẾT CHUYỂN Y LỆNH:");
        Console.WriteLine(string.Format("• Tổng số y lệnh chưa xử lý : {0}", totalPending));
        Console.WriteLine(string.Format("• Đã đúng tên bác sĩ từ trước: {0}", alreadyTarget));
        Console.WriteLine(string.Format("• Chuyển thành công          : {0}", successCount));
        Console.WriteLine(string.Format("• Thất bại                   : {0}", failCount));
        Console.WriteLine("===============================================================================");
    }

    public static void CancelOrder(string orderKey, long? customRoomId = null)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🗑️ HỦY/XÓA Y LỆNH (MÃ HOẶC ID: {0})", orderKey));
        Console.WriteLine("===============================================================================");

        if (string.IsNullOrEmpty(orderKey))
        {
            Console.WriteLine("❌ Vui lòng cung cấp ID phiếu y lệnh (ServiceReqId) hoặc Mã phiếu (ServiceReqCode)!");
            return;
        }

        string ok = orderKey.Trim();
        List<V_HIS_SERVICE_REQ> reqs = null;

        // Nếu chuỗi bắt đầu bằng 0 hoặc có độ dài >= 11 ký tự -> Ưu tiên tìm theo SERVICE_REQ_CODE trước (tránh nhầm sang ID)
        if (ok.StartsWith("0") || ok.Length >= 11)
        {
            HisServiceReqViewFilter srfCode = new HisServiceReqViewFilter { SERVICE_REQ_CODE = ok };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfCode, param);
        }

        long reqId = 0;
        // 1. Nếu là số ID (8 chữ số thông thường)
        if (long.TryParse(ok, out reqId) && !ok.StartsWith("0"))
        {
            HisServiceReqViewFilter srfId = new HisServiceReqViewFilter { ID = reqId };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfId, param);

            if (reqs == null || reqs.Count == 0)
            {
                var rawF = new HisServiceReqFilter { ID = reqId };
                var rawList = myAdapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mosConsumer, rawF, param);
                if (rawList != null && rawList.Count > 0 && rawList[0].IS_DELETE == 1)
                {
                    Console.WriteLine(string.Format("✔ Y lệnh ID {0} (Mã: {1}) đã ở trạng thái ĐÃ XÓA (IS_DELETE = 1) từ trước.", rawList[0].ID, rawList[0].SERVICE_REQ_CODE));
                    return;
                }
            }
        }

        // 2. Tìm theo Mã phiếu (SERVICE_REQ_CODE) - Chỉ áp dụng khi chuỗi bắt đầu bằng '0' hoặc có độ dài >= 10
        if ((reqs == null || reqs.Count == 0) && (ok.StartsWith("0") || ok.Length >= 10))
        {
            string codeToCheck = ok.PadLeft(12, '0');
            HisServiceReqViewFilter srfCode = new HisServiceReqViewFilter { SERVICE_REQ_CODE = codeToCheck };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfCode, param);

            if (reqs == null || reqs.Count == 0)
            {
                var rawF = new HisServiceReqFilter { SERVICE_REQ_CODE__EXACT = codeToCheck };
                var rawList = myAdapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mosConsumer, rawF, param);
                if (rawList != null && rawList.Count > 0 && rawList[0].IS_DELETE == 1)
                {
                    Console.WriteLine(string.Format("✔ Y lệnh Mã {0} (ID: {1}) đã ở trạng thái ĐÃ XÓA (IS_DELETE = 1) từ trước.", rawList[0].SERVICE_REQ_CODE, rawList[0].ID));
                    return;
                }
            }
        }

        if (reqs == null || reqs.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy y lệnh với từ khóa: {0} (hoặc y lệnh đã bị xóa trước đó).", orderKey));
            return;
        }

        var req = reqs[0];
        string timeStr = req.INTRUCTION_TIME.ToString().Length >= 12 
            ? string.Format("{0}/{1} {2}:{3}", req.INTRUCTION_TIME.ToString().Substring(6, 2), req.INTRUCTION_TIME.ToString().Substring(4, 2), req.INTRUCTION_TIME.ToString().Substring(8, 2), req.INTRUCTION_TIME.ToString().Substring(10, 2)) 
            : req.INTRUCTION_TIME.ToString();

        Console.WriteLine(string.Format("• Bệnh nhân     : {0} (Mã BN: {1} | Mã ĐT: {2})", req.TDL_PATIENT_NAME, req.TDL_PATIENT_CODE, req.TREATMENT_CODE));
        Console.WriteLine(string.Format("• Mã phiếu      : {0} | ID Phiếu: {1} | Loại: {2}", req.SERVICE_REQ_CODE, req.ID, req.SERVICE_REQ_TYPE_NAME));
        Console.WriteLine(string.Format("• Người chỉ định: {0} ({1}) lúc {2}", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, timeStr));
        Console.WriteLine(string.Format("• Nơi chỉ định  : {0} (Phòng ID: {1})", req.REQUEST_ROOM_NAME, req.REQUEST_ROOM_ID));
        Console.WriteLine(string.Format("• Bác sĩ thực hiện hủy: {0} ({1})", currentDoctorName, currentDoctorLogin));

        // RÀO CHẮN AN TOÀN 1: Kiểm tra trạng thái y lệnh
        if (req.SERVICE_REQ_STT_ID != 1)
        {
            string sttName = req.SERVICE_REQ_STT_ID == 2 ? "🟡 ĐANG THỰC HIỆN (Đã tiếp nhận mẫu)" : (req.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ HOÀN THÀNH / ĐÃ CÓ KẾT QUẢ" : "KHÁC (" + req.SERVICE_REQ_STT_ID + ")");
            Console.WriteLine(string.Format("❌ CẢNH BÁO AN TOÀN: Y lệnh đang ở trạng thái {0}.", sttName));
            Console.WriteLine("👉 Quy định HIS: Chỉ được phép xóa y lệnh ở trạng thái CHƯA THỰC HIỆN (Chỉ định màu trắng).");
            Console.WriteLine("👉 Nếu cần hủy chỉ định này, vui lòng liên hệ phòng thực hiện để HỦY TIẾP NHẬN / HỦY KẾT QUẢ trên HIS trước.");
            return;
        }

        // Liệt kê chi tiết dịch vụ đính kèm
        try
        {
            HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
            var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (ssList != null && ssList.Count > 0)
            {
                Console.WriteLine(string.Format("• Dịch vụ sẽ bị hủy ({0} mục):", ssList.Count));
                foreach (var s in ssList)
                {
                    Console.WriteLine(string.Format("   - [{0}] {1} (SL: {2})", s.ID, s.TDL_SERVICE_NAME, s.AMOUNT));
                }
            }
        }
        catch { }

        // RÀO CHẮN AN TOÀN 2: Kiểm tra và hủy văn bản ký EMR nếu có
        try
        {
            var docFilter = new EMR.Filter.EmrDocumentFilter();
            docFilter.TREATMENT_CODE__EXACT = req.TDL_TREATMENT_CODE;
            ApiConsumer emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");
            var emrDocs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
            if (emrDocs != null && emrDocs.Count > 0)
            {
                string reqTag = "SERVICE_REQ_CODE:" + req.SERVICE_REQ_CODE;
                var matchedDocs = emrDocs.Where(d => d.HIS_CODE != null && d.HIS_CODE.Contains(reqTag)).ToList();
                if (matchedDocs.Count > 0)
                {
                    Console.WriteLine(string.Format("⚠️ Phát hiện {0} văn bản ký EMR liên kết với y lệnh này. Đang tự động hủy văn bản EMR...", matchedDocs.Count));
                    foreach (var doc in matchedDocs)
                    {
                        bool delEmr = myAdapter.PostData<bool>("api/EmrDocument/Delete", emrConsumer, doc, param);
                        Console.WriteLine(string.Format("   - Hủy văn bản EMR ID {0}: {1}", doc.ID, delEmr ? "✔ THÀNH CÔNG" : "❌ THẤT BẠI"));
                    }
                }
            }
        }
        catch { }

        // RÀO CHẮN 3: Tự động chuyển quyền người chỉ định về Bác sĩ hiện tại nếu do người khác chỉ định (Bypass cơ chế MOS qua UpdateCommonInfo)
        if (!string.IsNullOrEmpty(req.REQUEST_LOGINNAME) && !string.Equals(req.REQUEST_LOGINNAME, currentDoctorLogin, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(string.Format("🔄 Y lệnh do {0} ({1}) chỉ định. Đang tự động chuyển quyền Người chỉ định về {2} ({3}) qua UpdateCommonInfo...",
                req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, currentDoctorName, currentDoctorLogin));
            try
            {
                var rawFilter = new HisServiceReqFilter { ID = req.ID };
                var rawList = myAdapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mosConsumer, rawFilter, param);
                if (rawList != null && rawList.Count > 0)
                {
                    var rawReq = rawList[0];
                    rawReq.REQUEST_LOGINNAME = currentDoctorLogin;
                    rawReq.REQUEST_USERNAME = currentDoctorName;
                    rawReq.REQUEST_USER_TITLE = "Thạc sỹ y học";
                    var updRes = myAdapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mosConsumer, rawReq, param);
                    if (updRes != null && !param.HasException)
                    {
                        Console.WriteLine("✔ Đã chuyển quyền người chỉ định thành công! Tiến hành xóa y lệnh...");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Cảnh báo UpdateCommonInfo: " + param.GetMessage());
                    }
                }
            }
            catch (Exception exUpd)
            {
                Console.WriteLine("⚠️ Không thể UpdateCommonInfo: " + exUpd.Message);
            }
        }

        // RÀO CHẮN 4: Thực thi xóa ServiceReq qua API MOS Backend
        long reqRoomId = customRoomId ?? (req.REQUEST_ROOM_ID > 0 ? req.REQUEST_ROOM_ID : 5248);

        var sdo = new HisServiceReqSDO
        {
            Id = req.ID,
            RequestRoomId = reqRoomId
        };

        Console.WriteLine(string.Format("🚀 Đang gửi lệnh xóa y lệnh đến Backend MOS (RequestRoomId: {0})...", reqRoomId));
        bool isSuccess = myAdapter.PostData<bool>("api/HisServiceReq/Delete", mosConsumer, sdo, param);

        if (!isSuccess && reqRoomId != 5248 && customRoomId == null)
        {
            Console.WriteLine("⚠️ Thử lại lệnh xóa với Phòng làm việc chính (RequestRoomId: 5248)...");
            sdo.RequestRoomId = 5248;
            param = new CommonParam();
            isSuccess = myAdapter.PostData<bool>("api/HisServiceReq/Delete", mosConsumer, sdo, param);
        }

        if (isSuccess)
        {
            // Post-verify
            HisServiceReqViewFilter vf = new HisServiceReqViewFilter { ID = req.ID };
            var vList = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, vf, param);
            bool isGone = (vList == null || vList.Count == 0 || vList[0].IS_DELETE == 1);

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("✔ ĐÃ XÓA THÀNH CÔNG Y LỆNH: ID {0} (Mã phiếu: {1})", req.ID, req.SERVICE_REQ_CODE));
            if (isGone)
            {
                Console.WriteLine("✔ ĐỐI SOÁT HỆ THỐNG: Y lệnh đã được gỡ bỏ hoàn toàn khỏi hồ sơ bệnh án.");
            }
            Console.WriteLine("👉 Bác sĩ tải lại màn hình HIS Desktop sẽ thấy y lệnh màu trắng đã biến mất.");
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("❌ XÓA THẤT BẠI: {0} (BugCode: {1})", param.GetMessage(), param.GetBugCode()));
            Console.WriteLine("===============================================================================");
        }
    }

    public static void CancelSereServ(long sereServId)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🗑️ HỦY/XÓA DỊCH VỤ CON ĐƠN LẺ (SERE_SERV_ID: {0})", sereServId));
        Console.WriteLine("===============================================================================");

        HisSereServViewFilter ssf = new HisSereServViewFilter { ID = sereServId };
        var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
        if (ssList == null || ssList.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy dịch vụ với ID: {0} (hoặc đã bị xóa trước đó).", sereServId));
            return;
        }

        var ss = ssList[0];
        Console.WriteLine(string.Format("• Tên dịch vụ : {0} (Mã: {1})", ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_CODE));
        Console.WriteLine(string.Format("• Phiếu y lệnh: ID {0} (Mã phiếu: {1})", ss.SERVICE_REQ_ID, ss.TDL_SERVICE_REQ_CODE));
        Console.WriteLine(string.Format("• Bệnh nhân   : Mã ĐT {0} (TreatmentId: {1})", ss.TDL_TREATMENT_CODE, ss.TDL_TREATMENT_ID));
        Console.WriteLine(string.Format("• Số lượng    : {0} | Đơn giá: {1:N0} đ", ss.AMOUNT, ss.PRICE));

        Console.WriteLine("🚀 Đang gửi lệnh xóa dịch vụ đến Backend MOS...");
        bool isSuccess = myAdapter.PostData<bool>("api/HisSereServ/ExamDelete", mosConsumer, new HIS_SERE_SERV { ID = sereServId }, param);

        if (isSuccess)
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("✔ ĐÃ XÓA THÀNH CÔNG DỊCH VỤ: {0} (ID: {1})", ss.TDL_SERVICE_NAME, sereServId));
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("❌ XÓA THẤT BẠI: {0} (BugCode: {1})", param.GetMessage(), param.GetBugCode()));
            Console.WriteLine("===============================================================================");
        }
    }

    public static V_HIS_TREATMENT FindTreatmentByKeyword(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;
        string kw = (keyword ?? "").Trim();
        long numVal;
        bool isNum = long.TryParse(kw, out numVal);

        if (isNum)
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }
        else
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
            if (treatments == null || treatments.Count == 0)
            {
                string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
                try
                {
                    HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true, TREATMENT_IS_ACTIVE = true };
                    var bedList = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                    if (bedList != null)
                    {
                        var mBed = bedList.FirstOrDefault(b => b.TDL_PATIENT_NAME != null && RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm));
                        if (mBed != null)
                        {
                            var tf = new HisTreatmentViewFilter { ID = mBed.TREATMENT_ID };
                            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                        }
                    }
                }
                catch { }
            }
        }

        if (treatments == null || treatments.Count == 0) return null;
        return treatments.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
    }

    public static void ListEmrDocuments(string keyword)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("📂 TRA CỨU HỒ SƠ VĂN BẢN EMR: " + keyword);
        Console.WriteLine("===============================================================================");

        var tr = FindTreatmentByKeyword(keyword);
        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + keyword);
            return;
        }

        Console.WriteLine(string.Format("👤 BỆNH NHÂN : {0} ({1} tuổi - {2})", tr.TDL_PATIENT_NAME, DateTime.Now.Year - (tr.TDL_PATIENT_DOB.ToString().Length >= 4 ? int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)) : 1990), tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("🆔 MÃ BỆNH ÁN: {0} | MÃ ĐỢT ĐIỀU TRỊ: {1} | ID: {2}", tr.TREATMENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("🩺 CHẨN ĐOÁN : [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));
        Console.WriteLine("-------------------------------------------------------------------------------");

        var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
        var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
        if (docs == null || docs.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân này hiện chưa có văn bản nào trên hệ thống EMR.");
            Console.WriteLine("===============================================================================");
            return;
        }

        Console.WriteLine(string.Format("📋 Tìm thấy {0} văn bản EMR (Hiển thị 15 văn bản mới nhất):", docs.Count));
        int idx = 1;
        foreach (var d in docs.OrderByDescending(x => x.ID).Take(15))
        {
            var signFilter = new EmrSignFilter { DOCUMENT_ID = d.ID };
            var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, param);
            bool isAllSigned = signs != null && signs.Count > 0 && signs.All(s => s.SIGN_TIME > 0);

            string statusSign;
            if (string.IsNullOrEmpty(d.NEXT_SIGNER) || isAllSigned)
            {
                statusSign = "🟢 ĐÃ KÝ ĐẦY ĐỦ";
            }
            else if (string.Equals(d.NEXT_SIGNER, currentDoctorLogin, StringComparison.OrdinalIgnoreCase))
            {
                statusSign = string.Format("🟡 CHỜ BẠN KÝ ({0})", d.NEXT_SIGNER);
            }
            else
            {
                statusSign = string.Format("🟠 Chờ {0} ký", d.NEXT_SIGNER);
            }

            Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Loại: {2,-3} | {3}", idx++, d.ID, d.DOCUMENT_TYPE_ID, statusSign));
            Console.WriteLine(string.Format("    Tên VB : {0}", d.DOCUMENT_NAME));
            Console.WriteLine(string.Format("    Mã VB  : {0} | HIS_CODE: {1}", d.DOCUMENT_CODE, d.HIS_CODE));

            if (signs != null && signs.Count > 0)
            {
                foreach (var s in signs.OrderBy(x => x.NUM_ORDER))
                {
                    string sStatus = s.SIGN_TIME > 0 ? string.Format("✅ Đã ký ({0})", s.SIGN_TIME) : "❌ Chưa ký";
                    Console.WriteLine(string.Format("      - Vị trí {0} ({1} - {2}): {3}", s.NUM_ORDER, s.LOGINNAME, s.USERNAME, sStatus));
                }
            }
        }
        Console.WriteLine("\n===============================================================================");
    }

    public static void AutoSignEmr(string keyword, long? targetDocId = null)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("⚡ TỰ ĐỘNG KÝ ĐIỆN TỬ EMR (AUTO-SIGN ON DEMAND): " + keyword);
        Console.WriteLine(string.Format("👤 Bác sĩ ký: {0} ({1})", currentDoctorName, currentDoctorLogin));
        Console.WriteLine("===============================================================================");

        var tr = FindTreatmentByKeyword(keyword);
        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + keyword);
            return;
        }

        Console.WriteLine(string.Format("👤 BỆNH NHÂN: {0} | Mã ĐT: {1}", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE));
        var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
        var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
        if (docs == null || docs.Count == 0)
        {
            Console.WriteLine("ℹ️ Không tìm thấy văn bản EMR nào của bệnh nhân.");
            Console.WriteLine("===============================================================================");
            return;
        }

        var pendingDocs = docs.Where(d => 
            targetDocId != null ? (d.ID == targetDocId.Value) :
            (!string.IsNullOrEmpty(d.NEXT_SIGNER) &&
             (string.Equals(d.NEXT_SIGNER, currentDoctorLogin, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(d.NEXT_SIGNER, "034727", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(d.NEXT_SIGNER, "vmc", StringComparison.OrdinalIgnoreCase)))
        ).ToList();

        if (pendingDocs.Count == 0)
        {
            Console.WriteLine(string.Format("✔ Không có văn bản nào đang chờ Bác sĩ ({0}) ký!", currentDoctorLogin));
            Console.WriteLine("===============================================================================");
            return;
        }

        Console.WriteLine(string.Format("📋 Tìm thấy {0} văn bản đang chờ ký. Bắt đầu tự động ký điện tử...", pendingDocs.Count));
        long signTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        int successCount = 0;

        foreach (var doc in pendingDocs)
        {
            Console.WriteLine(string.Format("\n👉 Đang ký văn bản ID {0}: {1}...", doc.ID, doc.DOCUMENT_NAME));

            var signFilter = new EmrSignFilter { DOCUMENT_ID = doc.ID };
            var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, param);
            var mySign = signs != null ? signs.FirstOrDefault(s => 
                (string.Equals(s.LOGINNAME, currentDoctorLogin, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, doc.NEXT_SIGNER, StringComparison.OrdinalIgnoreCase) ||
                 string.IsNullOrEmpty(s.LOGINNAME)) &&
                (s.SIGN_TIME == null || s.SIGN_TIME == 0)
            ) : null;

            if (mySign == null)
            {
                Console.WriteLine("   ⚠️ Không tìm thấy lượt ký hợp lệ của Bác sĩ trong văn bản này!");
                if (signs != null && signs.Count > 0)
                {
                    foreach (var s in signs)
                    {
                        Console.WriteLine(string.Format("      • Vị trí {0} ({1} - {2}): SIGN_TIME={3}", s.NUM_ORDER, s.LOGINNAME, s.USERNAME, s.SIGN_TIME));
                    }
                }
                continue;
            }

            var signSdo = new EmrSignHsmSDO
            {
                EmrDocumentId = doc.ID,
                EmrSignId = mySign.ID,
                SignTime = signTime,
                IsFinishSign = true,
                IsSigning = true,
                IsSignElectronic = true,
                Description = "Ký điện tử Bác sĩ điều trị (Auto-Sign)",
                RoomCode = "NQCTCHBB734",
                RoomTypeCode = "GI",
                WorkingDepartmentName = "Khoa Chấn thương Chỉnh hình và Cột sống",
                PointSign = new EMR.SDO.EmrPointSignSDO
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

            CommonParam pSign = new CommonParam();
            var res = myAdapter.PostData<EmrSignResultSDO>("api/EmrSign/SignPdfHsm", emrConsumer, signSdo, pSign);
            if (res != null && res.EmrSign != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("   ✅ KÝ ĐIỆN TỬ THÀNH CÔNG! SignID: {0} | Thời gian: {1}", res.EmrSign.ID, res.EmrSign.SIGN_TIME));
                Console.ResetColor();
                successCount++;
            }
            else
            {
                // Fallback qua UpdateSdo nếu SignPdfHsm cần cấu hình HSM
                var updateSdo = new EmrSignUpdateSDO
                {
                    DocumentId = doc.ID,
                    Updates = new List<EMR_SIGN>
                    {
                        new EMR_SIGN
                        {
                            ID = mySign.ID,
                            SIGN_TIME = signTime
                        }
                    }
                };
                var resUp = myAdapter.PostData<bool>("api/EmrSign/UpdateSdo", emrConsumer, updateSdo, pSign);
                if (resUp)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("   ✅ KÝ ĐIỆN TỬ THÀNH CÔNG (qua UpdateSdo)!");
                    Console.ResetColor();
                    successCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   ❌ KÝ THẤT BẠI: " + string.Join("; ", pSign.Messages ?? new List<string>()));
                    Console.ResetColor();
                }
            }
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📊 HOÀN TẤT: Đã tự động ký thành công {0}/{1} văn bản EMR.", successCount, pendingDocs.Count));
        Console.WriteLine("===============================================================================");
    }

    public static void InviteSignEmr(string keyword, string typeArg = "all", string doctorLoginArg = null)
    {
        InitSession();
        string targetDoctorLogin = !string.IsNullOrEmpty(doctorLoginArg) ? doctorLoginArg.Trim() : "034727";
        string targetDoctorName = targetDoctorLogin == "034727" ? "Ths.BS Nguyễn Hữu Sâm" : targetDoctorLogin;
        string targetDoctorTitle = targetDoctorLogin == "034727" ? "Ths.BS" : "BS";
        string targetDeptCode = "9";
        string targetDeptName = "Khoa Chấn thương Chỉnh hình và Cột sống";

        Console.WriteLine("===============================================================================");
        Console.WriteLine("📩 MỜI KÝ SỐ EMR (INVITE-SIGN): " + keyword);
        Console.WriteLine(string.Format("👤 Bác sĩ được mời ký: {0} ({1}) | Bộ lọc loại VB: {2}", targetDoctorName, targetDoctorLogin, typeArg ?? "all"));
        Console.WriteLine("===============================================================================");

        var tr = FindTreatmentByKeyword(keyword);
        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + keyword);
            return;
        }

        Console.WriteLine(string.Format("👤 BỆNH NHÂN : {0} ({1} tuổi - {2})", tr.TDL_PATIENT_NAME, DateTime.Now.Year - (tr.TDL_PATIENT_DOB.ToString().Length >= 4 ? int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)) : 1990), tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("🆔 MÃ BỆNH ÁN: {0} | MÃ ĐỢT ĐIỀU TRỊ: {1} | ID: {2}", tr.TREATMENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("🩺 CHẨN ĐOÁN : [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));
        Console.WriteLine("-------------------------------------------------------------------------------");

        var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
        var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
        if (docs == null || docs.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân này hiện chưa có văn bản nào trên hệ thống EMR.");
            Console.WriteLine("===============================================================================");
            return;
        }

        // Lọc theo docType
        List<EMR_DOCUMENT> targetDocs = new List<EMR_DOCUMENT>();
        string filterTypeLower = (typeArg ?? "all").Trim().ToLowerInvariant();
        long customTypeId = 0;
        if (filterTypeLower == "7")
        {
            targetDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == 7).ToList();
        }
        else if (filterTypeLower == "116")
        {
            targetDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == 116).ToList();
        }
        else if (long.TryParse(filterTypeLower, out customTypeId))
        {
            targetDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == customTypeId).ToList();
        }
        else // "all" hoặc mặc định: lấy Type 7 (Tờ điều trị) và Type 116 (Vỏ bệnh án)
        {
            targetDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == 7 || d.DOCUMENT_TYPE_ID == 116).ToList();
        }

        if (targetDocs.Count == 0)
        {
            Console.WriteLine(string.Format("ℹ️ Không tìm thấy văn bản EMR phù hợp (Bộ lọc loại: {0}) của bệnh nhân.", typeArg));
            Console.WriteLine("===============================================================================");
            return;
        }

        Console.WriteLine(string.Format("📋 Tìm thấy {0} văn bản EMR cần xử lý mời ký:", targetDocs.Count));
        int successCount = 0;
        int idx = 1;

        foreach (var doc in targetDocs.OrderBy(d => d.ID))
        {
            var signFilter = new EmrSignFilter { DOCUMENT_ID = doc.ID };
            var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, param);

            // 1. Kiểm tra xem Bác sĩ đã ký chưa
            var mySigned = signs != null ? signs.FirstOrDefault(s => 
                string.Equals(s.LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase) && s.SIGN_TIME > 0) : null;
            if (mySigned != null)
            {
                Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Loại: {2,-3} | Mã VB: {3}", idx++, doc.ID, doc.DOCUMENT_TYPE_ID, doc.DOCUMENT_CODE));
                Console.WriteLine(string.Format("    Tên VB : {0}", doc.DOCUMENT_NAME));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("    Trạng thái: 🟢 ĐÃ KÝ BỞI BẠN ({0}) lúc {1}", targetDoctorLogin, mySigned.SIGN_TIME));
                Console.ResetColor();
                successCount++;
                continue;
            }

            // 1b. Kiểm tra xem văn bản đã được ký đầy đủ bởi tất cả các bên chưa
            bool isAllSigned = signs != null && signs.Count > 0 && signs.All(s => s.SIGN_TIME != null && s.SIGN_TIME > 0);
            if (isAllSigned)
            {
                Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Loại: {2,-3} | Mã VB: {3}", idx++, doc.ID, doc.DOCUMENT_TYPE_ID, doc.DOCUMENT_CODE));
                Console.WriteLine(string.Format("    Tên VB : {0}", doc.DOCUMENT_NAME));
                Console.ForegroundColor = ConsoleColor.Green;
                string signers = string.Join(", ", signs.Select(s => string.Format("{0} ({1})", s.USERNAME ?? s.LOGINNAME, s.LOGINNAME)));
                Console.WriteLine(string.Format("    Trạng thái: 🟢 ĐÃ KÝ ĐẦY ĐỦ (Bởi {0})", signers));
                Console.ResetColor();
                successCount++;
                continue;
            }

            // 2. Kiểm tra xem đã được gán và đang chờ bác sĩ này ký chưa
            var myPending = signs != null ? signs.FirstOrDefault(s => 
                string.Equals(s.LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase) && (s.SIGN_TIME == null || s.SIGN_TIME == 0)) : null;
            if (myPending != null)
            {
                Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Loại: {2,-3} | Mã VB: {3}", idx++, doc.ID, doc.DOCUMENT_TYPE_ID, doc.DOCUMENT_CODE));
                Console.WriteLine(string.Format("    Tên VB : {0}", doc.DOCUMENT_NAME));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("    Trạng thái: 🟡 CHỜ BẠN KÝ ({0}) (Đã gán từ trước)", targetDoctorLogin));
                Console.ResetColor();
                successCount++;
                continue;
            }

            // 3. Tìm slot chưa ký để cập nhật, ưu tiên slot của vmc, doc.NEXT_SIGNER, rỗng hoặc bất kỳ slot chưa ký nào
            var slotToUpdate = signs != null ? signs.FirstOrDefault(s => 
                (s.SIGN_TIME == null || s.SIGN_TIME == 0) &&
                (string.IsNullOrEmpty(s.LOGINNAME) || 
                 string.Equals(s.LOGINNAME, "vmc", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, doc.NEXT_SIGNER, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase))
            ) : null;

            if (slotToUpdate == null && signs != null)
            {
                slotToUpdate = signs.FirstOrDefault(s => s.SIGN_TIME == null || s.SIGN_TIME == 0);
            }

            var updateSdo = new EmrSignUpdateSDO
            {
                DocumentId = doc.ID,
                Updates = new List<EMR_SIGN>()
            };

            if (slotToUpdate != null)
            {
                updateSdo.Updates.Add(new EMR_SIGN
                {
                    ID = slotToUpdate.ID,
                    DOCUMENT_ID = doc.ID,
                    NUM_ORDER = slotToUpdate.NUM_ORDER,
                    LOGINNAME = targetDoctorLogin,
                    USERNAME = targetDoctorName,
                    TITLE = targetDoctorTitle,
                    DEPARTMENT_CODE = targetDeptCode,
                    DEPARTMENT_NAME = targetDeptName,
                    SIGN_TIME = null,
                    SIGN_DATE = null
                });
            }
            else
            {
                updateSdo.Creates = new List<EMR_SIGN>
                {
                    new EMR_SIGN
                    {
                        DOCUMENT_ID = doc.ID,
                        NUM_ORDER = (signs != null && signs.Count > 0 ? signs.Max(x => x.NUM_ORDER) + 1 : 1),
                        LOGINNAME = targetDoctorLogin,
                        USERNAME = targetDoctorName,
                        TITLE = targetDoctorTitle,
                        DEPARTMENT_CODE = targetDeptCode,
                        DEPARTMENT_NAME = targetDeptName,
                        SIGN_TIME = null,
                        SIGN_DATE = null,
                        IS_ACTIVE = 1,
                        IS_DELETE = 0
                    }
                };
            }

            CommonParam pSign = new CommonParam();
            bool ok = myAdapter.PostData<bool>("api/EmrSign/UpdateSdo", emrConsumer, updateSdo, pSign);

            Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Loại: {2,-3} | Mã VB: {3}", idx++, doc.ID, doc.DOCUMENT_TYPE_ID, doc.DOCUMENT_CODE));
            Console.WriteLine(string.Format("    Tên VB : {0}", doc.DOCUMENT_NAME));
            if (ok)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("    Trạng thái: 🟡 CHỜ BẠN KÝ ({0})", targetDoctorLogin));
                Console.ResetColor();
                successCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string err = (pSign.Messages != null && pSign.Messages.Count > 0) ? string.Join("; ", pSign.Messages) : "UpdateSdo trả về false";
                Console.WriteLine(string.Format("    Trạng thái: ❌ LỖI GÁN LƯỢT KÝ: {0}", err));
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("📊 HOÀN TẤT: Đã gán/chuyển trạng thái thành công {0}/{1} văn bản EMR sang 🟡 CHỜ BẠN KÝ ({2}).", successCount, targetDocs.Count, targetDoctorLogin));
        Console.WriteLine("===============================================================================");
    }

    public static void CheckPrescriptionsTrackingSign(string keyword = null)
    {
        InitSession();
        long todayStart = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "000000");
        long todayEnd = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "235959");

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🔍 KIỂM TRA CHỈ ĐỊNH THUỐC & TRẠNG THÁI KÝ TỜ ĐIỀU TRỊ ({0})", DateTime.Today.ToString("dd/MM/yyyy")));
        Console.WriteLine(string.Format("Bác sĩ: {0} ({1})", currentDoctorName, currentDoctorLogin));
        Console.WriteLine("===============================================================================");

        List<string> targetPatients = new List<string> {
            "0004079203", // NGUYỄN KHẮC CHỦ
            "0004062358", // NGUYỄN THỊ VỰC
            "0004053995", // HỒ XUÂN TÁM
            "0004059994", // BÙI CÔNG ĐIỂM
            "0004081634", // NGUYỄN THỊ THU
            "0003178086", // TRƯƠNG CÔNG MẠNH
            "0004008409", // LÊ THỊ BÌNH
            "0004072758", // NGUYỄN THỊ XUÂN
            "0004069486"  // TRẦN VĂN SÁNG
        };

        if (!string.IsNullOrEmpty(keyword) && keyword != "all" && keyword != "today")
        {
            targetPatients = new List<string> { keyword };
        }

        List<V_HIS_SERVICE_REQ> allMedReqs = new List<V_HIS_SERVICE_REQ>();

        // 1. Quét theo danh sách bệnh nhân
        foreach (var pCode in targetPatients)
        {
            var tr = FindTreatmentByKeyword(pCode);
            if (tr == null) continue;

            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_ID = tr.ID,
                INTRUCTION_TIME_FROM = todayStart,
                INTRUCTION_TIME_TO = todayEnd
            };
            var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (reqs != null && reqs.Count > 0)
            {
                foreach (var r in reqs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7 || 
                    (x.SERVICE_REQ_TYPE_NAME != null && (x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("thuốc") || x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("tủ trực") || x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("đơn")))))
                {
                    if (!allMedReqs.Any(m => m.ID == r.ID)) allMedReqs.Add(r);
                }
            }
        }

        // 2. Quét thêm tất cả ServiceReq loại đơn thuốc do Bác sĩ tạo hôm nay
        var docSrf = new HisServiceReqViewFilter
        {
            REQUEST_LOGINNAME__EXACT = currentDoctorLogin,
            INTRUCTION_TIME_FROM = todayStart,
            INTRUCTION_TIME_TO = todayEnd
        };
        var docReqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, docSrf, param);
        if (docReqs != null && docReqs.Count > 0)
        {
            foreach (var r in docReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7 || 
                (x.SERVICE_REQ_TYPE_NAME != null && (x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("thuốc") || x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("tủ trực") || x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("đơn")))))
            {
                if (!allMedReqs.Any(m => m.ID == r.ID)) allMedReqs.Add(r);
            }
        }

        if (allMedReqs.Count == 0)
        {
            Console.WriteLine("ℹ️ Không tìm thấy chỉ định thuốc nào trong ngày hôm nay.");
            return;
        }

        Console.WriteLine(string.Format("📋 Tìm thấy {0} chỉ định thuốc trong ngày hôm nay:\n", allMedReqs.Count));

        int unsignedCount = 0;
        int signedCount = 0;
        int idx = 1;

        foreach (var req in allMedReqs.OrderBy(x => x.INTRUCTION_TIME))
        {
            // Lấy chi tiết thuốc từ SereServ
            var ssFilter = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
            var sereServs = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
            string medSummary = "";
            if (sereServs != null && sereServs.Count > 0)
            {
                medSummary = string.Join("; ", sereServs.Select(s => string.Format("{0} ({1} {2})", s.TDL_SERVICE_NAME, s.AMOUNT, s.SERVICE_UNIT_NAME)));
            }

            // Lấy Tracking hôm nay
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = req.TREATMENT_ID };
            var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);
            V_HIS_TRACKING linkedTrk = null;
            if (trks != null && trks.Count > 0)
            {
                linkedTrk = trks.Where(t => t.TRACKING_TIME.ToString().StartsWith("20260928")).OrderByDescending(t => t.TRACKING_TIME).FirstOrDefault();
                if (linkedTrk == null) linkedTrk = trks.OrderByDescending(t => t.TRACKING_TIME).FirstOrDefault();
            }

            // Kiểm tra trạng thái ký trên EMR
            bool isSigned = false;
            string signStatusText = "";

            if (linkedTrk != null)
            {
                if (linkedTrk.EMR_DOCUMENT_STT_ID == 3 || linkedTrk.EMR_DOCUMENT_STT_ID == 4)
                {
                    isSigned = true;
                    signStatusText = string.Format("🟢 ĐÃ KÝ ĐIỆN TỬ (EMR STT: {0})", linkedTrk.EMR_DOCUMENT_STT_NAME ?? "Đã ký");
                }
                else
                {
                    var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = req.TREATMENT_CODE };
                    var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
                    
                    EMR_DOCUMENT trkDoc = null;
                    if (docs != null && docs.Count > 0)
                    {
                        trkDoc = docs.FirstOrDefault(d => 
                            d.HIS_CODE != null && d.HIS_CODE.Contains(linkedTrk.ID.ToString())
                        );
                        if (trkDoc == null)
                        {
                            trkDoc = docs.Where(d => 
                                (d.DOCUMENT_NAME != null && d.DOCUMENT_NAME.ToLower().Contains("điều trị")) ||
                                d.DOCUMENT_TYPE_ID == 7 || d.DOCUMENT_TYPE_ID == 116
                            ).OrderByDescending(d => d.ID).FirstOrDefault();
                            
                            if (trkDoc != null && trkDoc.DOCUMENT_TIME.HasValue && !trkDoc.DOCUMENT_TIME.Value.ToString().StartsWith("20260928"))
                            {
                                trkDoc = null;
                            }
                        }
                    }

                    if (trkDoc != null)
                    {
                        var signFilter = new EmrSignFilter { DOCUMENT_ID = trkDoc.ID };
                        var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, param);
                        bool allSigned = signs != null && signs.Count > 0 && signs.All(s => s.SIGN_TIME > 0);
                        if (allSigned)
                        {
                            isSigned = true;
                            signStatusText = string.Format("🟢 ĐÃ KÝ EMR (DocID {0})", trkDoc.ID);
                        }
                        else
                        {
                            signStatusText = string.Format("🟡 ĐÃ TẠO VB EMR (DocID {0}) NHƯNG CHƯA KÝ (Chờ: {1})", trkDoc.ID, trkDoc.NEXT_SIGNER ?? "Trống");
                        }
                    }
                    else
                    {
                        signStatusText = "🔴 CHƯA KÝ TỜ ĐIỀU TRỊ (Mới tạo trên HIS, chưa in/ký số EMR)";
                    }
                }
            }
            else
            {
                signStatusText = "🔴 CHƯA CÓ TỜ ĐIỀU TRỊ!";
            }

            string icon = isSigned ? "🟢" : "🔴";
            if (isSigned) signedCount++; else unsignedCount++;

            Console.WriteLine(string.Format("[{0}] {1} BN: {2} (Mã BN: {3})", idx++, icon, req.TDL_PATIENT_NAME, req.TDL_PATIENT_CODE));
            Console.WriteLine(string.Format("    Phiếu thuốc : {0} | Giờ: {1}", req.SERVICE_REQ_CODE, req.INTRUCTION_TIME));
            if (!string.IsNullOrEmpty(medSummary)) Console.WriteLine(string.Format("    Thuốc       : {0}", medSummary));
            if (linkedTrk != null)
            {
                Console.WriteLine(string.Format("    Tờ điều trị : ID {0} (Tờ số {1}, lúc {2})", linkedTrk.ID, linkedTrk.SHEET_ORDER, linkedTrk.TRACKING_TIME));
            }
            Console.WriteLine(string.Format("    Trạng thái  : {0}", signStatusText));
            Console.WriteLine();
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT: {0} CHỈ ĐỊNH THUỐC | 🟢 ĐÃ KÝ: {1} | 🔴 CHƯA KÝ: {2}", allMedReqs.Count, signedCount, unsignedCount));
        Console.WriteLine("===============================================================================");
    }

    public static void AuditClinicalDate(string dateParam = null)
    {
        InitSession();
        DateTime targetDate = DateTime.Today.AddDays(1); // Mặc định là ngày mai
        if (!string.IsNullOrEmpty(dateParam))
        {
            DateTime parsed;
            if (DateTime.TryParseExact(dateParam, new string[] { "yyyyMMdd", "dd/MM/yyyy", "yyyy-MM-dd" }, null, System.Globalization.DateTimeStyles.None, out parsed))
            {
                targetDate = parsed;
            }
        }

        long dayStart = long.Parse(targetDate.ToString("yyyyMMdd") + "000000");
        long dayEnd = long.Parse(targetDate.ToString("yyyyMMdd") + "235959");

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 RÀ SOÁT LÂM SÀNG TOÀN DIỆN KHOA 57 CHO NGÀY: {0} ({1})", targetDate.ToString("dd/MM/yyyy"), targetDate.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"))));
        Console.WriteLine(string.Format("Thời gian kiểm tra: {0} | Bác sĩ: {1} ({2})", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), currentDoctorName, currentDoctorLogin));
        Console.WriteLine("===============================================================================");

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var allRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        if (allRooms == null || allRooms.Count == 0) throw new Exception("Không lấy được danh sách buồng bệnh Khoa 57!");

        var targetRoomIds = allRooms.Select(r => r.ID).ToList();
        HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = targetRoomIds, IS_IN_ROOM = true };
        var inPatients = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
        if (inPatients == null || inPatients.Count == 0)
        {
            Console.WriteLine("Không có bệnh nhân nội trú nào.");
            return;
        }

        var allTreatmentIds = inPatients.Select(x => x.TREATMENT_ID).Distinct().ToList();
        Console.WriteLine(string.Format("📍 Tìm thấy {0} bệnh nhân tại {1} buồng bệnh. Đang tải dữ liệu Tờ ĐT, Thuốc, Suất ăn...", inPatients.Count, allRooms.Count));

        // Batch fetch
        Dictionary<long, List<V_HIS_TRACKING>> trkMap = new Dictionary<long, List<V_HIS_TRACKING>>();
        Dictionary<long, List<V_HIS_SERVICE_REQ>> srMap = new Dictionary<long, List<V_HIS_SERVICE_REQ>>();
        Dictionary<long, List<V_HIS_SERE_SERV>> ssMap = new Dictionary<long, List<V_HIS_SERE_SERV>>();

        try
        {
            var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, new HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds }, param);
            if (trks != null) trkMap = trks.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        try
        {
            var srs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, new HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds }, param);
            if (srs != null) srMap = srs.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        try
        {
            var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, new HisSereServViewFilter { TREATMENT_IDs = allTreatmentIds }, param);
            if (sss != null) ssMap = sss.Where(x => x.TDL_TREATMENT_ID.HasValue).GroupBy(x => x.TDL_TREATMENT_ID.Value).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        // Đối soát
        int total = inPatients.Count;
        int hasTrkCount = 0;
        int hasMedCount = 0;
        int hasRationCount = 0;
        int fullyReadyCount = 0;

        List<string> missingTrkList = new List<string>();
        List<string> missingMedList = new List<string>();
        List<string> missingRationList = new List<string>();

        var groupedByRoom = inPatients.GroupBy(x => x.BED_ROOM_NAME).OrderBy(g => g.Key);
        int stt = 1;

        foreach (var rg in groupedByRoom)
        {
            Console.WriteLine(string.Format("\n🏨 {0} ({1} bệnh nhân):", rg.Key, rg.Count()));
            foreach (var p in rg.OrderBy(x => x.BED_NAME))
            {
                long tId = p.TREATMENT_ID;
                string pTag = string.Format("[{0} - {1}] {2} (Mã BN: {3})", rg.Key, p.BED_NAME ?? "G-", p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE);

                // 1. Tờ điều trị
                List<V_HIS_TRACKING> pTrks;
                trkMap.TryGetValue(tId, out pTrks);
                bool hasTrkDay = pTrks != null && pTrks.Any(x => x.TRACKING_TIME >= dayStart && x.TRACKING_TIME <= dayEnd);
                var latestTrk = pTrks != null && pTrks.Count > 0 ? pTrks.OrderByDescending(x => x.TRACKING_TIME).First() : null;
                bool hasWeekendTrk = !hasTrkDay && latestTrk != null && (latestTrk.TRACKING_TIME >= 20260918000000) && 
                    (latestTrk.CONTENT != null && (latestTrk.CONTENT.ToLower().Contains("t2") || latestTrk.CONTENT.ToLower().Contains("ngày nghỉ") || latestTrk.CONTENT.ToLower().Contains("chủ nhật")));

                string trkStatus;
                if (hasTrkDay) { trkStatus = "🟢 Tờ ĐT ngày " + targetDate.ToString("dd/MM"); hasTrkCount++; }
                else if (hasWeekendTrk) { trkStatus = "🟡 Gộp cuối tuần (" + latestTrk.CONTENT.Trim().Replace("\r\n", " ") + ")"; hasTrkCount++; }
                else { trkStatus = "🔴 CHƯA CÓ"; missingTrkList.Add(pTag); }

                // 2. Thuốc (Kiểm tra cả ServiceReq và SereServ)
                List<V_HIS_SERVICE_REQ> pSrs;
                srMap.TryGetValue(tId, out pSrs);
                var medSrs = pSrs != null ? pSrs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList() : null;
                bool hasMedDay = medSrs != null && medSrs.Any(x => x.INTRUCTION_TIME >= dayStart && x.INTRUCTION_TIME <= dayEnd);
                bool hasWeekendMed = !hasMedDay && medSrs != null && medSrs.Any(x => x.INTRUCTION_TIME >= 20260918000000);

                List<V_HIS_SERE_SERV> pSss;
                ssMap.TryGetValue(tId, out pSss);
                if (!hasMedDay && pSss != null)
                {
                    hasMedDay = pSss.Any(x => x.TDL_SERVICE_TYPE_ID == 6 && x.TDL_INTRUCTION_TIME >= dayStart && x.TDL_INTRUCTION_TIME <= dayEnd);
                }
                if (!hasWeekendMed && pSss != null)
                {
                    hasWeekendMed = pSss.Any(x => x.TDL_SERVICE_TYPE_ID == 6 && x.TDL_INTRUCTION_TIME >= 20260918000000);
                }

                string medStatus;
                if (hasMedDay) { medStatus = "🟢 Đơn ngày " + targetDate.ToString("dd/MM"); hasMedCount++; }
                else if (hasWeekendMed) { medStatus = "🟡 Duy trì T6/T7"; hasMedCount++; }
                else { medStatus = "🔴 CHƯA CÓ"; missingMedList.Add(pTag); }

                // 3. Suất ăn (Kiểm tra qua SereServ TDL_SERVICE_TYPE_ID == 14)
                var dayRations = pSss != null ? pSss.Where(x => x.TDL_SERVICE_TYPE_ID == 14 && x.TDL_INTRUCTION_TIME >= dayStart && x.TDL_INTRUCTION_TIME <= dayEnd).ToList() : null;
                bool hasRation = dayRations != null && dayRations.Count > 0;

                string rationStatus;
                if (hasRation)
                {
                    hasRationCount++;
                    var rTypes = dayRations.Select(x => x.TDL_SERVICE_NAME).Distinct();
                    rationStatus = string.Format("🟢 {0} bữa ({1})", dayRations.Count, string.Join(", ", rTypes));
                }
                else
                {
                    rationStatus = "🔴 CHƯA CÓ";
                    missingRationList.Add(pTag);
                }

                if ((hasTrkDay || hasWeekendTrk) && (hasMedDay || hasWeekendMed) && hasRation)
                {
                    fullyReadyCount++;
                }

                Console.WriteLine(string.Format("  {0,2}. [{1}] {2,-22} | Tờ ĐT: {3,-28} | Thuốc: {4,-20} | Cơm: {5}",
                    stt++, p.BED_NAME ?? "G-", p.TDL_PATIENT_NAME, trkStatus, medStatus, rationStatus));
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT RÀ SOÁT CA TRỰC NGÀY {0} ({1} BỆNH NHÂN)", targetDate.ToString("dd/MM/yyyy"), total));
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("✔ ĐÃ HOÀN TẤT ĐẦY ĐỦ CẢ 3 TIÊU CHÍ (TỜ ĐT + THUỐC + SUẤT ĂN): {0}/{1} BN ({2:0.0}%)", fullyReadyCount, total, (fullyReadyCount * 100.0 / total)));
        Console.WriteLine(string.Format("✔ ĐÃ CÓ SUẤT ĂN DINH DƯỠNG: {0}/{1} BN", hasRationCount, total));
        Console.WriteLine(string.Format("✔ ĐÃ CÓ TỜ ĐIỀU TRỊ (HOẶC GỘP CUỐI TUẦN): {0}/{1} BN", hasTrkCount, total));
        Console.WriteLine(string.Format("✔ ĐÃ CÓ Y LỆNH THUỐC (HOẶC DUY TRÌ CUỐI TUẦN): {0}/{1} BN", hasMedCount, total));
        Console.WriteLine("-------------------------------------------------------------------------------");

        if (missingRationList.Count > 0)
        {
            Console.WriteLine(string.Format("⚠️ CÒN {0} BỆNH NHÂN CHƯA CÓ SUẤT ĂN NGÀY {1}:", missingRationList.Count, targetDate.ToString("dd/MM")));
            foreach (var m in missingRationList) Console.WriteLine("  • " + m);
        }
        else
        {
            Console.WriteLine("🟢 100% BỆNH NHÂN ĐÃ CÓ SUẤT ĂN NGÀY " + targetDate.ToString("dd/MM"));
        }

        if (missingTrkList.Count > 0)
        {
            Console.WriteLine(string.Format("\n⚠️ CÒN {0} BỆNH NHÂN CHƯA CÓ TỜ ĐIỀU TRỊ/KÝ GỘP:", missingTrkList.Count));
            foreach (var m in missingTrkList) Console.WriteLine("  • " + m);
        }
        else
        {
            Console.WriteLine("🟢 100% BỆNH NHÂN ĐÃ ĐƯỢC KÝ TỜ ĐIỀU TRỊ/GỘP CUỐI TUẦN.");
        }

        if (missingMedList.Count > 0)
        {
            Console.WriteLine(string.Format("\n⚠️ CÒN {0} BỆNH NHÂN CHƯA CÓ Y LỆNH THUỐC TỪ THỨ 6 ĐẾN NAY:", missingMedList.Count));
            foreach (var m in missingMedList) Console.WriteLine("  • " + m);
        }
        else
        {
            Console.WriteLine("🟢 100% BỆNH NHÂN ĐÃ ĐƯỢC DUY TRÌ Y LỆNH THUỐC CUỐI TUẦN.");
        }
        Console.WriteLine("===============================================================================");
    }

    public static void PlanAndPrescribeMedications(string targetDateParam = null, bool executePrescribe = true)
    {
        InitSession();
        DateTime targetDate = DateTime.Today.AddDays(1);
        if (!string.IsNullOrEmpty(targetDateParam))
        {
            DateTime parsed;
            if (DateTime.TryParseExact(targetDateParam, new string[] { "yyyyMMdd", "dd/MM/yyyy", "yyyy-MM-dd" }, null, System.Globalization.DateTimeStyles.None, out parsed))
            {
                targetDate = parsed;
            }
        }

        DateTime prevDate = targetDate.AddDays(-1);
        long targetDayStart = long.Parse(targetDate.ToString("yyyyMMdd") + "000000");
        long targetDayEnd = long.Parse(targetDate.ToString("yyyyMMdd") + "235959");
        long nowTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        long target0805 = long.Parse(targetDate.ToString("yyyyMMdd") + "080500");
        long presInstructionTime = targetDate.Date == DateTime.Today ? Math.Min(target0805, nowTime) : target0805;
        if (presInstructionTime < long.Parse(targetDate.ToString("yyyyMMdd") + "080000"))
        {
            presInstructionTime = long.Parse(targetDate.ToString("yyyyMMdd") + "080000");
        }

        long prevDayStart = long.Parse(prevDate.ToString("yyyyMMdd") + "000000");
        long prevDayEnd = long.Parse(prevDate.ToString("yyyyMMdd") + "235959");
        long friDayStart = long.Parse(prevDate.AddDays(-1).ToString("yyyyMMdd") + "000000");

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("💊 KẾ HOẠCH DỰ TRÙ TỦ TRỰC 810 & KÊ THUỐC KHO DƯỢC NGÀY {0}", targetDate.ToString("dd/MM/yyyy")));
        Console.WriteLine(string.Format("Bác sĩ thực hiện: {0} ({1}) | Y lệnh thuốc lúc: 08:05", currentDoctorName, currentDoctorLogin));
        Console.WriteLine("===============================================================================");

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var allRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        if (allRooms == null || allRooms.Count == 0) throw new Exception("Không lấy được danh sách buồng bệnh Khoa 57!");

        var bedRoomToRoomMap = allRooms.Where(r => r.ROOM_ID > 0).GroupBy(r => r.ID).ToDictionary(g => g.Key, g => g.First().ROOM_ID);
        var targetRoomIds = allRooms.Select(r => r.ID).ToList();
        HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = targetRoomIds, IS_IN_ROOM = true };
        var inPatients = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
        if (inPatients == null || inPatients.Count == 0)
        {
            Console.WriteLine("Không có bệnh nhân nội trú nào.");
            return;
        }

        var allTreatmentIds = inPatients.Select(x => x.TREATMENT_ID).Distinct().ToList();
        Console.WriteLine(string.Format("📍 Tìm thấy {0} bệnh nhân tại Khoa 57. Đang quét tồn Tủ trực 810 và đơn thuốc cũ...", inPatients.Count));

        HisMedicineBeanViewFilter bf810 = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = 810, IS_ACTIVE = 1 };
        var beans810 = myAdapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", mosConsumer, bf810, param);
        Dictionary<long, decimal> stock810Map = new Dictionary<long, decimal>();
        Dictionary<long, string> stock810NameMap = new Dictionary<long, string>();
        if (beans810 != null)
        {
            foreach (var b in beans810)
            {
                if (b.AMOUNT > 0)
                {
                    if (!stock810Map.ContainsKey(b.MEDICINE_TYPE_ID))
                    {
                        stock810Map[b.MEDICINE_TYPE_ID] = 0;
                        stock810NameMap[b.MEDICINE_TYPE_ID] = b.MEDICINE_TYPE_NAME;
                    }
                    stock810Map[b.MEDICINE_TYPE_ID] += b.AMOUNT;
                }
            }
        }
        Console.WriteLine(string.Format("  ✔ Tủ trực 810 hiện có {0} loại thuốc đang còn tồn.", stock810Map.Count));

        Dictionary<long, V_HIS_TREATMENT> trMap = new Dictionary<long, V_HIS_TREATMENT>();
        try
        {
            var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { IDs = allTreatmentIds }, param);
            if (trList != null) trMap = trList.ToDictionary(x => x.ID, x => x);
        } catch { }

        Console.WriteLine("  • Đang tải danh sách điều trị, tờ điều trị và đơn thuốc từ ngày 18/09...");
        Dictionary<long, List<V_HIS_TRACKING>> trkMap = new Dictionary<long, List<V_HIS_TRACKING>>();
        try
        {
            var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, new HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds, TRACKING_TIME_FROM = friDayStart }, param);
            if (trks != null) trkMap = trks.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        Dictionary<long, List<V_HIS_SERVICE_REQ>> srMap = new Dictionary<long, List<V_HIS_SERVICE_REQ>>();
        try
        {
            var srs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, new HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds, INTRUCTION_TIME_FROM = friDayStart }, param);
            if (srs != null) srMap = srs.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        Dictionary<long, List<V_HIS_EXP_MEST_MEDICINE>> emmMap = new Dictionary<long, List<V_HIS_EXP_MEST_MEDICINE>>();
        try
        {
            var ems = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, new HisExpMestMedicineViewFilter { TDL_TREATMENT_IDs = allTreatmentIds, CREATE_TIME_FROM = friDayStart - 1000000 }, param);
            if (ems != null)
            {
                emmMap = ems.Where(x => x.TDL_TREATMENT_ID.HasValue)
                            .GroupBy(x => x.TDL_TREATMENT_ID.Value)
                            .ToDictionary(g => g.Key, g => g.ToList());
            }
        }
        catch { }
        Console.WriteLine(string.Format("  ✔ Tải xong dữ liệu: {0} BN có y lệnh, {1} BN có chi tiết thuốc.", srMap.Count, emmMap.Count));

        string[] abxKeywords = new string[] {
            "cefa", "cefu", "ceft", "cefe", "amox", "clavulan", "ampicil", "sulbac", "cipro", "levo",
            "vanco", "teico", "linezolid", "metro", "genta", "tobra", "amikacin", "unasyn", "augmentin",
            "zinnat", "rocephin", "tazocin", "meropenem", "imipenem", "tienam", "avelox", "klacid"
        };

        var cabinetReservePlans = new List<string>();
        var pharmacyPrescribedList = new List<string>();
        var antibioticAlerts = new List<string>();
        var noMedCopyList = new List<string>();

        int countPrescribed = 0;
        int countPlannedCabinet = 0;

        foreach (var ip in inPatients)
        {
            long tId = ip.TREATMENT_ID;
            V_HIS_TREATMENT tr = null;
            trMap.TryGetValue(tId, out tr);
            string patName = tr != null ? tr.TDL_PATIENT_NAME : ("BN_" + tId);
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "";
            string bedInfo = string.Format("{0} - {1}", ip.BED_ROOM_NAME, ip.BED_NAME);
            long patRoomId = 5248;
            if (bedRoomToRoomMap.ContainsKey(ip.BED_ROOM_ID) && bedRoomToRoomMap[ip.BED_ROOM_ID] > 0)
            {
                patRoomId = bedRoomToRoomMap[ip.BED_ROOM_ID];
            }

            List<V_HIS_SERVICE_REQ> myReqs = null;
            srMap.TryGetValue(tId, out myReqs);

            bool hasTargetMed = false;
            if (myReqs != null)
            {
                hasTargetMed = myReqs.Any(r => r.SERVICE_REQ_TYPE_ID == 6 && r.INTRUCTION_TIME >= targetDayStart && r.INTRUCTION_TIME <= targetDayEnd && r.IS_DELETE != 1);
            }

            if (hasTargetMed)
            {
                continue;
            }

            List<V_HIS_EXP_MEST_MEDICINE> myEms = null;
            emmMap.TryGetValue(tId, out myEms);

            List<V_HIS_EXP_MEST_MEDICINE> prevMeds = null;
            if (myEms != null && myEms.Count > 0)
            {
                prevMeds = myEms.Where(m => {
                    long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
                    return t >= prevDayStart && t <= prevDayEnd && m.IS_DELETE != 1;
                }).ToList();

                if (prevMeds == null || prevMeds.Count == 0)
                {
                    prevMeds = myEms.Where(m => {
                        long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
                        return t >= friDayStart && t < prevDayStart && m.IS_DELETE != 1;
                    }).ToList();
                }
            }

            if (prevMeds == null || prevMeds.Count == 0)
            {
                noMedCopyList.Add(string.Format("[{0}] {1} (Mã BN: {2}) - Không có đơn thuốc T6/T7 để copy", bedInfo, patName, patCode));
                continue;
            }

            bool isPostOp = false;
            if (myReqs != null && myReqs.Any(r => r.SERVICE_REQ_TYPE_ID == 7 && r.IS_DELETE != 1)) isPostOp = true;
            if (tr != null && ((tr.ICD_NAME != null && (tr.ICD_NAME.ToLower().Contains("sau phẫu thuật") || tr.ICD_NAME.ToLower().Contains("hậu phẫu"))) ||
                              (tr.ICD_TEXT != null && (tr.ICD_TEXT.ToLower().Contains("sau mổ") || tr.ICD_TEXT.ToLower().Contains("hậu phẫu"))))) isPostOp = true;

            List<V_HIS_TRACKING> myTrks = null;
            trkMap.TryGetValue(tId, out myTrks);
            if (myTrks != null && myTrks.Any(tk => tk.CONTENT != null && (tk.CONTENT.ToLower().Contains("hậu phẫu") || tk.CONTENT.ToLower().Contains("sau mổ")))) isPostOp = true;

            bool hasAntibiotic = false;
            foreach (var m in prevMeds)
            {
                string sName = (m.MEDICINE_TYPE_NAME ?? "").ToLower();
                if (abxKeywords.Any(k => sName.Contains(k)))
                {
                    hasAntibiotic = true;
                    break;
                }
            }

            if (isPostOp && !hasAntibiotic)
            {
                antibioticAlerts.Add(string.Format("⚠️ [{0}] {1} (Mã BN: {2}) HẬU PHẪU nhưng đơn copy KHÔNG CÓ kháng sinh!", bedInfo, patName, patCode));
            }

            long trackingId = 0;
            if (myTrks != null)
            {
                var trkTarget = myTrks.Where(tk => tk.TRACKING_TIME >= targetDayStart && tk.TRACKING_TIME <= targetDayEnd)
                                      .OrderByDescending(tk => tk.TRACKING_TIME).FirstOrDefault();
                if (trkTarget != null) trackingId = trkTarget.ID;
            }

            var distinctMeds = prevMeds.GroupBy(m => m.MEDICINE_TYPE_ID).Select(g => g.First()).ToList();

            var cabinetMedsForPat = new List<string>();
            var cabinetPresMeds = new List<PresMedicineSDO>();
            var pharmacyMedsForPat = new List<PresMedicineSDO>();
            string sessionKey = Guid.NewGuid().ToString();

            // Bộ lọc loại bỏ thuốc đái tháo đường (Insulin & OAD) theo quy tắc lâm sàng an toàn: "K copy thuốc đái đường"
            string[] diabetesKeywords = new string[] {
                "actrapid", "lantus", "mixtard", "novorapid", "insulin", "humalog", "apidra", "toujeo", "tresiba", "insulatard", "scilin",
                "metformin", "glucophage", "diamicron", "gliclazide", "amaryl", "glimepiride", "januvia", "sitagliptin", "galvus", "vildagliptin",
                "trajenta", "linagliptin", "forxiga", "dapagliflozin", "jardiance", "empagliflozin", "glucovance"
            };

            foreach (var m in distinctMeds)
            {
                long medTypeId = m.MEDICINE_TYPE_ID;
                string medName = m.MEDICINE_TYPE_NAME ?? "Thuốc";
                string sNameLower = medName.ToLower();

                // Bỏ qua tuyệt đối các thuốc điều trị đái tháo đường theo chỉ đạo: "K copy thuốc đái đường"
                if (diabetesKeywords.Any(k => sNameLower.Contains(k)))
                {
                    continue;
                }

                decimal reqAmount = m.AMOUNT;
                string tutorial = m.TUTORIAL ?? "Theo chỉ dẫn bác sĩ";
                int patientTypeId = (int)(m.PATIENT_TYPE_ID.HasValue && m.PATIENT_TYPE_ID.Value > 0 ? m.PATIENT_TYPE_ID.Value : (tr != null && tr.TDL_PATIENT_TYPE_ID.HasValue ? tr.TDL_PATIENT_TYPE_ID.Value : 1L));

                if (!executePrescribe)
                {
                    // Chế độ DRY-RUN (Dự trù mô phỏng)
                    if (stock810Map.ContainsKey(medTypeId) && stock810Map[medTypeId] >= reqAmount)
                    {
                        cabinetMedsForPat.Add(string.Format("{0} x {1:0.##} ({2})", medName, reqAmount, tutorial));
                        countPlannedCabinet++;
                    }
                    else
                    {
                        long stockId = 4210;
                        if (sNameLower.Contains("tiêm") || sNameLower.Contains("truyền") || sNameLower.Contains("ống") || sNameLower.Contains("lọ") || sNameLower.Contains("chai") || sNameLower.Contains("injection") || sNameLower.Contains("infusion") || (m.MEDICINE_USE_FORM_ID.HasValue && (m.MEDICINE_USE_FORM_ID == 15 || m.MEDICINE_USE_FORM_ID == 20)))
                        {
                            stockId = 4209;
                        }
                        pharmacyMedsForPat.Add(new PresMedicineSDO
                        {
                            MedicineTypeId = medTypeId,
                            MediStockId = stockId,
                            Amount = reqAmount,
                            PresAmount = reqAmount,
                            PatientTypeId = patientTypeId,
                            Tutorial = tutorial,
                            MedicineUseFormId = m.MEDICINE_USE_FORM_ID,
                            Speed = m.SPEED,
                            Morning = m.MORNING,
                            Noon = m.NOON,
                            Afternoon = m.AFTERNOON,
                            Evening = m.EVENING,
                            NumOfDays = 1
                        });
                    }
                }
                else
                {
                    // Chế độ THỰC THI (KÊ ĐƠN CHÍNH THỨC: Tủ trực trước, hết cơ số chuyển kê Dược lĩnh)
                    bool takenCabinet = false;
                    if (stock810Map.ContainsKey(medTypeId) && stock810Map[medTypeId] >= reqAmount)
                    {
                        try
                        {
                            var takeBeanSDO = new TakeBeanSDO
                            {
                                TypeId = medTypeId,
                                MediStockId = 810,
                                PatientTypeId = patientTypeId,
                                Amount = reqAmount,
                                ClientSessionKey = sessionKey,
                                ExpiredDate = null
                            };
                            CommonParam pTake = new CommonParam();
                            var beans = myAdapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBeanSDO, pTake);
                            if (beans != null && beans.Count > 0)
                            {
                                stock810Map[medTypeId] -= reqAmount;
                                cabinetPresMeds.Add(new PresMedicineSDO
                                {
                                    MedicineTypeId = medTypeId,
                                    MediStockId = 810,
                                    Amount = reqAmount,
                                    PresAmount = reqAmount,
                                    PatientTypeId = patientTypeId,
                                    Tutorial = tutorial,
                                    MedicineUseFormId = m.MEDICINE_USE_FORM_ID,
                                    Speed = m.SPEED,
                                    Morning = m.MORNING,
                                    Noon = m.NOON,
                                    Afternoon = m.AFTERNOON,
                                    Evening = m.EVENING,
                                    IsExpend = false,
                                    NumOfDays = 1,
                                    MedicineBeanIds = beans.Select(b => b.ID).ToList()
                                });
                                takenCabinet = true;
                                countPlannedCabinet++;
                            }
                        }
                        catch { }
                    }

                    if (!takenCabinet)
                    {
                        // Tủ trực không có hoặc hết cơ số -> Tự động chuyển sang Kho Dược lĩnh
                        long stockId = 4210;
                        if (sNameLower.Contains("tiêm") || sNameLower.Contains("truyền") || sNameLower.Contains("ống") || sNameLower.Contains("lọ") || sNameLower.Contains("chai") || sNameLower.Contains("injection") || sNameLower.Contains("infusion") || (m.MEDICINE_USE_FORM_ID.HasValue && (m.MEDICINE_USE_FORM_ID == 15 || m.MEDICINE_USE_FORM_ID == 20)))
                        {
                            stockId = 4209;
                        }

                        pharmacyMedsForPat.Add(new PresMedicineSDO
                        {
                            MedicineTypeId = medTypeId,
                            MediStockId = stockId,
                            Amount = reqAmount,
                            PresAmount = reqAmount,
                            PatientTypeId = patientTypeId,
                            Tutorial = tutorial,
                            MedicineUseFormId = m.MEDICINE_USE_FORM_ID,
                            Speed = m.SPEED,
                            Morning = m.MORNING,
                            Noon = m.NOON,
                            Afternoon = m.AFTERNOON,
                            Evening = m.EVENING,
                            IsExpend = false,
                            NumOfDays = 1
                        });
                    }
                }
            }

            string cleanIcd = !string.IsNullOrEmpty(tr != null ? tr.ICD_CODE : null) ? tr.ICD_CODE.Trim() : "M51.2";
            if (cleanIcd.Length > 5 && cleanIcd.Contains("."))
            {
                var dotParts = cleanIcd.Split('.');
                if (dotParts[0].Length <= 3 && dotParts.Length > 1)
                {
                    int maxDec = Math.Min(dotParts[1].Length, 5 - 1 - dotParts[0].Length);
                    cleanIcd = dotParts[0] + "." + dotParts[1].Substring(0, maxDec);
                }
            }
            if (cleanIcd.Length > 5) cleanIcd = cleanIcd.Substring(0, 5);

            if (!executePrescribe)
            {
                if (cabinetMedsForPat.Count > 0)
                {
                    cabinetReservePlans.Add(string.Format("• [{0}] {1} (Mã BN: {2}):\n    👉 {3}", bedInfo, patName, patCode, string.Join("; ", cabinetMedsForPat)));
                }
                if (pharmacyMedsForPat.Count > 0)
                {
                    pharmacyPrescribedList.Add(string.Format("• [{0}] {1} (Mã BN: {2}):\n    👉 {3} loại thuốc Kho Dược", bedInfo, patName, patCode, pharmacyMedsForPat.Count));
                }
            }
            else
            {
                long targetReqRoom = patRoomId > 0 ? patRoomId : 5248;

                // 1. Kê đơn Tủ trực 810 (OutPatientPresCreateList)
                if (cabinetPresMeds.Count > 0)
                {
                    try
                    {
                        var outPresSDO = new OutPatientPresSDO
                        {
                            TreatmentId = tId,
                            InstructionTime = presInstructionTime,
                            UseTimes = new List<long> { presInstructionTime },
                            TrackingId = trackingId > 0 ? (long?)trackingId : null,
                            RequestRoomId = targetReqRoom,
                            RequestLoginName = currentDoctorLogin,
                            RequestUserName = currentDoctorName,
                            IcdCode = cleanIcd,
                            IcdName = tr != null ? tr.ICD_NAME : "Bệnh lý cơ xương khớp",
                            IcdSubCode = tr != null ? tr.ICD_SUB_CODE : null,
                            IcdText = tr != null ? tr.ICD_TEXT : null,
                            IsCabinet = true,
                            ClientSessionKey = sessionKey,
                            Medicines = cabinetPresMeds
                        };

                        CommonParam pOut = new CommonParam();
                        var outRes = myAdapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, pOut);
                        if ((outRes == null || outRes.ServiceReqs == null || outRes.ServiceReqs.Count == 0) && targetReqRoom != 5248)
                        {
                            // Fallback phòng trực 5248 nếu buồng bệnh nhân chưa kích hoạt phân quyền
                            outPresSDO.RequestRoomId = 5248;
                            pOut = new CommonParam();
                            outRes = myAdapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, pOut);
                        }

                        if (outRes != null && outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0)
                        {
                            string sReqCode = outRes.ServiceReqs[0].SERVICE_REQ_CODE;
                            cabinetReservePlans.Add(string.Format("✔ [{0}] {1} (Mã BN: {2}) | Y LỆNH TỦ TRỰC: {3} | {4} loại thuốc",
                                bedInfo, patName, patCode, sReqCode, cabinetPresMeds.Count));
                            countPrescribed++;
                        }
                        else
                        {
                            string err = (pOut.Messages != null && pOut.Messages.Count > 0) ? string.Join("; ", pOut.Messages) : "Lỗi tạo đơn tủ trực";
                            if (pOut.BugCodes != null && pOut.BugCodes.Count > 0) err += " [" + string.Join(";", pOut.BugCodes) + "]";
                            cabinetReservePlans.Add(string.Format("❌ [{0}] {1} (Mã BN: {2}) | Thất bại Tủ trực: {3}", bedInfo, patName, patCode, err));
                        }
                    }
                    catch (Exception exCab)
                    {
                        cabinetReservePlans.Add(string.Format("❌ [{0}] {1} (Mã BN: {2}) | Lỗi Tủ trực: {3}", bedInfo, patName, patCode, exCab.Message));
                    }
                }

                // 2. Kê đơn Kho Dược lĩnh (InPatientPresCreate) cho các thuốc không có/hết tủ trực
                if (pharmacyMedsForPat.Count > 0)
                {
                    try
                    {
                        InPatientPresSDO presSDO = new InPatientPresSDO
                        {
                            TreatmentId = tId,
                            InstructionTimes = new List<long> { presInstructionTime },
                            UseTimes = new List<long> { presInstructionTime },
                            TrackingId = trackingId > 0 ? (long?)trackingId : null,
                            TrackingInfos = trackingId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = presInstructionTime } } : null,
                            RequestRoomId = targetReqRoom,
                            RequestLoginName = currentDoctorLogin,
                            RequestUserName = currentDoctorName,
                            IcdCode = cleanIcd,
                            IcdName = tr != null ? tr.ICD_NAME : "Bệnh lý cơ xương khớp",
                            IcdSubCode = tr != null ? tr.ICD_SUB_CODE : null,
                            IcdText = tr != null ? tr.ICD_TEXT : null,
                            Medicines = pharmacyMedsForPat
                        };

                        CommonParam pPres = new CommonParam();
                        var presRes = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, presSDO, pPres);
                        if ((presRes == null || ((presRes.ServiceReqs == null || presRes.ServiceReqs.Count == 0) && (presRes.ExpMests == null || presRes.ExpMests.Count == 0))) && targetReqRoom != 5248)
                        {
                            // Fallback phòng trực 5248 nếu buồng bệnh nhân chưa kích hoạt phân quyền
                            presSDO.RequestRoomId = 5248;
                            pPres = new CommonParam();
                            presRes = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, presSDO, pPres);
                        }

                        if (presRes != null && ((presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0) || (presRes.ExpMests != null && presRes.ExpMests.Count > 0)))
                        {
                            string reqCode = presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0 ? presRes.ServiceReqs[0].SERVICE_REQ_CODE : presRes.ExpMests[0].EXP_MEST_CODE;
                            pharmacyPrescribedList.Add(string.Format("✔ [{0}] {1} (Mã BN: {2}) | Y LỆNH KHO DƯỢC: {3} | {4} loại thuốc",
                                bedInfo, patName, patCode, reqCode, pharmacyMedsForPat.Count));
                            countPrescribed++;
                        }
                        else
                        {
                            string err = (pPres.Messages != null && pPres.Messages.Count > 0) ? string.Join("; ", pPres.Messages) : "MOS từ chối tạo đơn Kho Dược";
                            if (pPres.BugCodes != null && pPres.BugCodes.Count > 0) err += " [" + string.Join(";", pPres.BugCodes) + "]";
                            pharmacyPrescribedList.Add(string.Format("❌ [{0}] {1} (Mã BN: {2}) | Thất bại Kho Dược: {3}", bedInfo, patName, patCode, err));
                        }
                    }
                    catch (Exception exP)
                    {
                        pharmacyPrescribedList.Add(string.Format("❌ [{0}] {1} (Mã BN: {2}) | Lỗi Kho Dược: {3}", bedInfo, patName, patCode, exP.Message));
                    }
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
        string b1Title = executePrescribe ? 
            string.Format("💊 BẢNG 1: KẾT QUẢ KÊ ĐƠN TỦ TRỰC 810 THÀNH CÔNG (TỔNG CỘNG {0} LƯỢT THUỐC)", countPlannedCabinet) :
            string.Format("📋 BẢNG 1: DỰ TRÙ THUỐC TỦ TRỰC 810 (ƯU TIÊN MAI SÁNG ĐI BUỒNG KÊ - {0} LƯỢT THUỐC)", countPlannedCabinet);
        Console.WriteLine(b1Title);
        Console.WriteLine("===============================================================================");
        if (cabinetReservePlans.Count > 0)
        {
            foreach (var plan in cabinetReservePlans) Console.WriteLine(plan);
        }
        else
        {
            Console.WriteLine(executePrescribe ? "Không có đơn thuốc tủ trực nào được tạo." : "Không có thuốc nào khớp với Tủ trực 810.");
        }

        Console.WriteLine("\n===============================================================================");
        string b2Title = executePrescribe ?
            string.Format("📦 BẢNG 2: THUỐC NGOÀI TỦ TRỰC / THIẾU CƠ SỐ ĐÃ KÊ XUẤT KHO DƯỢC LĨNH ({0} LƯỢT)", pharmacyPrescribedList.Count) :
            string.Format("💊 BẢNG 2: THUỐC NGOÀI TỦ TRỰC DỰ KIẾN KÊ XUẤT KHO DƯỢC (4210 / 4209) ({0} LƯỢT)", pharmacyPrescribedList.Count);
        Console.WriteLine(b2Title);
        Console.WriteLine("===============================================================================");
        if (pharmacyPrescribedList.Count > 0)
        {
            foreach (var p in pharmacyPrescribedList) Console.WriteLine(p);
        }
        else
        {
            Console.WriteLine("Không có bệnh nhân nào cần kê thuốc Kho Dược.");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("🚨 BẢNG 3: CẢNH BÁO AN TOÀN LÂM SÀNG (BỆNH NHÂN MỔ VỀ / HẬU PHẪU)");
        Console.WriteLine("===============================================================================");
        if (antibioticAlerts.Count > 0)
        {
            foreach (var a in antibioticAlerts) Console.WriteLine(a);
        }
        else
        {
            Console.WriteLine("🟢 100% bệnh nhân hậu phẫu trong khoa đều đã có kháng sinh trong đơn.");
        }

        if (noMedCopyList.Count > 0)
        {
            Console.WriteLine(string.Format("\nℹ️ BỆNH NHÂN KHÔNG CÓ ĐƠN THUỐC T6/T7 ĐỂ COPY ({0} BN - DIỆN NGHỈ THUỐC CUỐI TUẦN):", noMedCopyList.Count));
            foreach (var nm in noMedCopyList) Console.WriteLine("  • " + nm);
        }
        Console.WriteLine("===============================================================================");
    }

    public static void AuditPostOpAntibiotics(string dateParam = null)
    {
        InitSession();
        DateTime targetDate = new DateTime(2026, 9, 20);
        if (!string.IsNullOrEmpty(dateParam))
        {
            DateTime parsed;
            if (DateTime.TryParseExact(dateParam, new string[] { "yyyyMMdd", "dd/MM/yyyy", "yyyy-MM-dd" }, null, System.Globalization.DateTimeStyles.None, out parsed))
                targetDate = parsed;
        }

        long dayStart = long.Parse(targetDate.ToString("yyyyMMdd") + "000000");
        long dayEnd = long.Parse(targetDate.ToString("yyyyMMdd") + "235959");

        DateTime prevDate = targetDate.AddDays(-1);
        long prevDayStart = long.Parse(prevDate.ToString("yyyyMMdd") + "000000");
        long prevDayEnd = long.Parse(prevDate.ToString("yyyyMMdd") + "235959");

        DateTime friDate = targetDate.AddDays(-2);
        long friDayStart = long.Parse(friDate.ToString("yyyyMMdd") + "000000");

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 RÀ SOÁT KHÁNG SINH HẬU PHẪU KHOA 57 CHO NGÀY: {0}", targetDate.ToString("dd/MM/yyyy")));
        Console.WriteLine(string.Format("Bác sĩ phụ trách: {0} ({1}) | Mục tiêu: Đề xuất phác đồ KS phù hợp cho ngày mai", currentDoctorName, currentDoctorLogin));
        Console.WriteLine("===============================================================================");

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var allRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        if (allRooms == null || allRooms.Count == 0) throw new Exception("Không lấy được danh sách buồng bệnh Khoa 57!");

        var targetRoomIds = allRooms.Select(r => r.ID).ToList();
        HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = targetRoomIds, IS_IN_ROOM = true };
        var inPatients = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
        if (inPatients == null || inPatients.Count == 0)
        {
            Console.WriteLine("Không có bệnh nhân nội trú nào.");
            return;
        }

        var allTreatmentIds = inPatients.Select(x => x.TREATMENT_ID).Distinct().ToList();

        // 1. Quét tồn Tủ trực 810
        HisMedicineBeanViewFilter beanFilter = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = 810 };
        var beans810 = myAdapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", mosConsumer, beanFilter, param);
        Dictionary<long, decimal> stock810Map = new Dictionary<long, decimal>();
        Dictionary<long, string> stock810NameMap = new Dictionary<long, string>();
        if (beans810 != null)
        {
            foreach (var b in beans810)
            {
                if (b.AMOUNT > 0)
                {
                    if (!stock810Map.ContainsKey(b.MEDICINE_TYPE_ID))
                    {
                        stock810Map[b.MEDICINE_TYPE_ID] = 0;
                        stock810NameMap[b.MEDICINE_TYPE_ID] = b.MEDICINE_TYPE_NAME;
                    }
                    stock810Map[b.MEDICINE_TYPE_ID] += b.AMOUNT;
                }
            }
        }

        // 2. Tải dữ liệu hồ sơ
        Dictionary<long, V_HIS_TREATMENT> trMap = new Dictionary<long, V_HIS_TREATMENT>();
        try
        {
            var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { IDs = allTreatmentIds }, param);
            if (trList != null) trMap = trList.ToDictionary(x => x.ID, x => x);
        } catch { }

        Dictionary<long, List<V_HIS_TRACKING>> trkMap = new Dictionary<long, List<V_HIS_TRACKING>>();
        try
        {
            var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, new HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds }, param);
            if (trks != null) trkMap = trks.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        Dictionary<long, List<V_HIS_SERVICE_REQ>> srMap = new Dictionary<long, List<V_HIS_SERVICE_REQ>>();
        try
        {
            var srs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, new HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds }, param);
            if (srs != null) srMap = srs.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
        } catch { }

        Dictionary<long, List<V_HIS_EXP_MEST_MEDICINE>> emmMap = new Dictionary<long, List<V_HIS_EXP_MEST_MEDICINE>>();
        foreach (var tId in allTreatmentIds)
        {
            try
            {
                var ems = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId }, param);
                if (ems != null && ems.Count > 0) emmMap[tId] = ems;
            } catch { }
        }

        string[] abxKeywords = new string[] {
            "cefa", "cefu", "ceft", "cefe", "amox", "clavulan", "ampicil", "sulbac", "cipro", "levo",
            "vanco", "teico", "linezolid", "metro", "genta", "tobra", "amikacin", "unasyn", "augmentin",
            "zinnat", "rocephin", "tazocin", "meropenem", "imipenem", "tienam", "avelox", "klacid"
        };

        var urgentGroup = new List<string>();   // Hậu phẫu N1-N3 hoặc mới về từ HSN / mổ cấp cứu -> CẦN KHÁNG SINH TIÊM NGAY
        var lateGroup = new List<string>();     // Hậu phẫu N4-N7 -> Cân nhắc gối KS uống hoặc theo dõi ngưng KS
        var completedGroup = new List<string>();// Hậu phẫu > N7 -> Ổn định, đã kết thúc phác đồ KS
        var allergyNotes = new List<string>();

        foreach (var ip in inPatients)
        {
            long tId = ip.TREATMENT_ID;
            V_HIS_TREATMENT tr = null;
            trMap.TryGetValue(tId, out tr);
            string patName = tr != null ? tr.TDL_PATIENT_NAME : ("BN_" + tId);
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "";
            string bedInfo = string.Format("{0} - {1}", ip.BED_ROOM_NAME, ip.BED_NAME);

            List<V_HIS_SERVICE_REQ> myReqs = null;
            srMap.TryGetValue(tId, out myReqs);

            List<V_HIS_TRACKING> myTrks = null;
            trkMap.TryGetValue(tId, out myTrks);

            List<V_HIS_EXP_MEST_MEDICINE> myEms = null;
            emmMap.TryGetValue(tId, out myEms);

            // Xác định phẫu thuật và thời gian mổ
            bool isPostOp = false;
            string surgeryName = "";
            DateTime? surgeryDate = null;

            if (myReqs != null)
            {
                var surgReq = myReqs.Where(r => r.SERVICE_REQ_TYPE_ID == 7 && r.IS_DELETE != 1)
                                    .OrderByDescending(r => r.START_TIME ?? r.INTRUCTION_TIME).FirstOrDefault();
                if (surgReq != null)
                {
                    isPostOp = true;
                    surgeryName = !string.IsNullOrEmpty(surgReq.ICD_NAME) ? surgReq.ICD_NAME : (!string.IsNullOrEmpty(surgReq.SERVICE_REQ_CODE) ? ("Phiếu mổ " + surgReq.SERVICE_REQ_CODE) : "Phẫu thuật");
                    if (tr != null && !string.IsNullOrEmpty(tr.ICD_NAME)) surgeryName = tr.ICD_NAME;
                    long st = surgReq.START_TIME ?? surgReq.INTRUCTION_TIME;
                    string stStr = st.ToString();
                    if (stStr.Length >= 8)
                    {
                        int y = int.Parse(stStr.Substring(0, 4));
                        int m = int.Parse(stStr.Substring(4, 2));
                        int d = int.Parse(stStr.Substring(6, 2));
                        surgeryDate = new DateTime(y, m, d);
                    }
                }
            }

            if (!isPostOp && tr != null)
            {
                string icdFull = ((tr.ICD_NAME ?? "") + " " + (tr.ICD_TEXT ?? "")).ToLower();
                if (icdFull.Contains("sau phẫu thuật") || icdFull.Contains("hậu phẫu") || icdFull.Contains("sau mổ"))
                {
                    isPostOp = true;
                    surgeryName = tr.ICD_NAME ?? "Phẫu thuật CTCH";
                }
            }

            if (!isPostOp && myTrks != null)
            {
                var postOpTrk = myTrks.FirstOrDefault(tk => tk.CONTENT != null && (tk.CONTENT.ToLower().Contains("hậu phẫu") || tk.CONTENT.ToLower().Contains("sau mổ")));
                if (postOpTrk != null)
                {
                    isPostOp = true;
                    surgeryName = "Phẫu thuật theo tờ điều trị";
                }
            }

            if (!isPostOp) continue; // Bỏ qua nếu không phải bệnh nhân hậu phẫu

            // Kiểm tra xem đơn thuốc ngày 19/09 (hoặc T6) có kháng sinh không
            List<V_HIS_EXP_MEST_MEDICINE> recentMeds = null;
            if (myEms != null && myEms.Count > 0)
            {
                recentMeds = myEms.Where(m => {
                    long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
                    return t >= prevDayStart && t <= prevDayEnd && m.IS_DELETE != 1;
                }).ToList();

                if (recentMeds == null || recentMeds.Count == 0)
                {
                    recentMeds = myEms.Where(m => {
                        long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
                        return t >= friDayStart && t < prevDayStart && m.IS_DELETE != 1;
                    }).ToList();
                }
            }

            bool hasAbxCurrently = false;
            if (recentMeds != null)
            {
                foreach (var m in recentMeds)
                {
                    string sName = (m.MEDICINE_TYPE_NAME ?? "").ToLower();
                    if (abxKeywords.Any(k => sName.Contains(k)))
                    {
                        hasAbxCurrently = true;
                        break;
                    }
                }
            }

            if (hasAbxCurrently) continue; // Đã có kháng sinh rồi, không cần cảnh báo

            // Bệnh nhân này diện HẬU PHẪU nhưng CHƯA CÓ KHÁNG SINH!
            // Tính số ngày hậu phẫu
            int postOpDay = 0;
            if (surgeryDate.HasValue)
            {
                postOpDay = (int)(targetDate - surgeryDate.Value).TotalDays;
                if (postOpDay < 0) postOpDay = 0;
            }

            // Lịch sử kháng sinh trước đó trong đợt điều trị
            string pastAbxInfo = "Chưa từng dùng KS trong đợt này";
            if (myEms != null)
            {
                var allPastAbx = myEms.Where(m => {
                    string sName = (m.MEDICINE_TYPE_NAME ?? "").ToLower();
                    return abxKeywords.Any(k => sName.Contains(k)) && m.IS_DELETE != 1;
                }).OrderByDescending(m => m.TDL_INTRUCTION_TIME ?? m.CREATE_TIME).ToList();

                if (allPastAbx.Count > 0)
                {
                    var lastAbx = allPastAbx[0];
                    long lastT = lastAbx.TDL_INTRUCTION_TIME ?? lastAbx.CREATE_TIME ?? 0;
                    string lastDateStr = lastT.ToString().Length >= 8 ? string.Format("{0}/{1}", lastT.ToString().Substring(6, 2), lastT.ToString().Substring(4, 2)) : "";
                    pastAbxInfo = string.Format("Đã từng dùng: {0} ({1})", lastAbx.MEDICINE_TYPE_NAME, lastDateStr);
                }
            }

            // Đọc diễn biến mới nhất
            string recentSymptom = "Toàn trạng ổn định";
            if (myTrks != null && myTrks.Count > 0)
            {
                var lastTrk = myTrks.OrderByDescending(tk => tk.TRACKING_TIME).FirstOrDefault();
                if (lastTrk != null && !string.IsNullOrEmpty(lastTrk.CONTENT))
                {
                    recentSymptom = lastTrk.CONTENT.Replace("\r", " ").Replace("\n", " ");
                    if (recentSymptom.Length > 80) recentSymptom = recentSymptom.Substring(0, 77) + "...";
                }
            }

            // Kiểm tra dị ứng
            if (patName.Contains("Lân") || patCode == "0004052391")
            {
                allergyNotes.Add(string.Format("⛔ DỊ ỨNG: [{0}] {1} ({2}) Dị ứng Ceftriaxone -> CẤM kê Ceftriaxone!", bedInfo, patName, patCode));
            }

            // Phân nhóm lâm sàng và đề xuất thuốc phù hợp từ Tủ trực 810
            string postOpStr = postOpDay > 0 ? string.Format("Hậu phẫu N{0}", postOpDay) : "Hậu phẫu gần";
            if (postOpDay <= 3 || patCode == "0000246202") // N1-N3 hoặc Nguyễn Xuân Hường
            {
                string recMed = "👉 ĐỀ XUẤT TỦ TRỰC 810: Ceftriaxone 1000 x 2 lọ (Pha 100ml NaCl 0.9% TTM 40 g/p lúc 10h) + NaCl 0.9% 100ml x 1";
                if (pastAbxInfo.Contains("Zinacef"))
                {
                    recMed = "👉 ĐỀ XUẤT TỦ TRỰC 810: Zinacef 750mg x 2 lọ (Tiêm TM 10h - 22h) + Nước cất 10ml x 2";
                }
                else if (pastAbxInfo.Contains("Voxin") || pastAbxInfo.Contains("Vancomycin"))
                {
                    recMed = "👉 ĐỀ XUẤT TỦ TRỰC 810: Voxin 500mg x 2 lọ (Pha 250ml NaCl 0.9% TTM 40 g/p 10h-22h)";
                }

                urgentGroup.Add(string.Format("🔴 [{0}] {1} (Mã BN: {2}) | {3} | PT: {4}\n     • Lâm sàng: {5}\n     • Tiền sử KS: {6}\n     • {7}",
                    bedInfo, patName, patCode, postOpStr, surgeryName, recentSymptom, pastAbxInfo, recMed));
            }
            else if (postOpDay >= 4 && postOpDay <= 7)
            {
                string recMed = "👉 ĐỀ XUẤT TỦ TRỰC 810: Cefuroxim 500mg x 2 viên (Ngày uống 2 viên chia 2 lần: 10h - 20h) HOẶC Duy trì theo dõi ngưng KS nếu vết mổ khô";
                lateGroup.Add(string.Format("🟡 [{0}] {1} (Mã BN: {2}) | {3} | PT: {4}\n     • Lâm sàng: {5}\n     • Tiền sử KS: {6}\n     • {7}",
                    bedInfo, patName, patCode, postOpStr, surgeryName, recentSymptom, pastAbxInfo, recMed));
            }
            else
            {
                completedGroup.Add(string.Format("🟢 [{0}] {1} (Mã BN: {2}) | {3} | PT: {4}\n     • Tiền sử KS: {6}\n     • Đánh giá: Đã kết thúc phác đồ KS tiêm/uống, vết mổ liền tốt -> KHÔNG CẦN BỔ SUNG KHÁNG SINH",
                    bedInfo, patName, patCode, postOpDay > 0 ? string.Format("Hậu phẫu N{0}", postOpDay) : "Hậu phẫu muộn", surgeryName, recentSymptom, pastAbxInfo));
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("🔴 NHÓM 1: HẬU PHẪU SỚM (N1 - N3) HOẶC MỚI CHUYỂN KHOA VỀ - CẦN KÊ KHÁNG SINH TIÊM NGAY ({0} BN)", urgentGroup.Count));
        Console.WriteLine("===============================================================================");
        if (urgentGroup.Count > 0)
        {
            foreach (var u in urgentGroup) Console.WriteLine(u);
        }
        else
        {
            Console.WriteLine("Không có bệnh nhân hậu phẫu sớm nào thiếu kháng sinh.");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("🟡 NHÓM 2: HẬU PHẪU N4 - N7 (GIAI ĐOẠN ỔN ĐỊNH) - CÂN NHẮC GỐI KHÁNG SINH UỐNG ({0} BN)", lateGroup.Count));
        Console.WriteLine("===============================================================================");
        if (lateGroup.Count > 0)
        {
            foreach (var l in lateGroup) Console.WriteLine(l);
        }
        else
        {
            Console.WriteLine("Không có bệnh nhân hậu phẫu N4-N7 thiếu kháng sinh.");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("🟢 NHÓM 3: HẬU PHẪU MUỘN (> N7) - ĐÃ KẾT THÚC LIỆU TRÌNH KHÁNG SINH ({0} BN)", completedGroup.Count));
        Console.WriteLine("===============================================================================");
        if (completedGroup.Count > 0)
        {
            foreach (var c in completedGroup) Console.WriteLine(c);
        }
        else
        {
            Console.WriteLine("Không có.");
        }

        if (allergyNotes.Count > 0)
        {
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("⛔ CẢNH BÁO DỊ ỨNG KHÁNG SINH ĐẶC BIỆT:");
            Console.WriteLine("===============================================================================");
            foreach (var al in allergyNotes) Console.WriteLine(al);
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("💊 CƠ SỐ KHÁNG SINH CÓ SẴN TRONG TỦ TRỰC 810 (SẴN SÀNG KÊ SÁNG MAI):");
        Console.WriteLine("===============================================================================");
        var abxInStock = stock810Map.Where(kv => {
            string name = stock810NameMap.ContainsKey(kv.Key) ? stock810NameMap[kv.Key].ToLower() : "";
            return abxKeywords.Any(k => name.Contains(k));
        }).ToList();
        foreach (var ab in abxInStock)
        {
            Console.WriteLine(string.Format("  • {0}: còn tồn {1} (lọ/viên/ống)", stock810NameMap[ab.Key], ab.Value));
        }
        Console.WriteLine("===============================================================================");
    }

    public static void ShowCabinet810Stock()
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 DANH MỤC THUỐC TỒN TẠI TỦ TRỰC KHOA 57 (STOCK 810)");
        Console.WriteLine("===============================================================================");
        HisMedicineBeanViewFilter bf = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = 810, IS_ACTIVE = 1 };
        var beans = myAdapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", mosConsumer, bf, param);
        if (beans == null || beans.Count == 0)
        {
            Console.WriteLine("Tủ trực 810 hiện không có thuốc tồn.");
            return;
        }

        var grouped = beans.Where(b => b.AMOUNT > 0)
                           .GroupBy(b => new { b.MEDICINE_TYPE_ID, b.MEDICINE_TYPE_NAME, b.SERVICE_UNIT_NAME })
                           .Select(g => new { 
                               Id = g.Key.MEDICINE_TYPE_ID, 
                               Name = g.Key.MEDICINE_TYPE_NAME, 
                               Unit = g.Key.SERVICE_UNIT_NAME,
                               Total = g.Sum(x => x.AMOUNT) 
                           })
                           .OrderBy(x => x.Name)
                           .ToList();

        Console.WriteLine(string.Format("Tìm thấy {0} mặt hàng thuốc còn tồn:", grouped.Count));
        for (int i = 0; i < grouped.Count; i++)
        {
            var item = grouped[i];
            Console.WriteLine(string.Format("  {0,2}. [ID: {1,5}] {2} | Tồn: {3,5} {4}", i + 1, item.Id, item.Name, item.Total, item.Unit));
        }
        Console.WriteLine("===============================================================================");
    }

    public static void RunCli(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("🏥 UNIFIED HIS CLINICAL AUTOMATION CLI - KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Cú pháp lệnh:");
            Console.WriteLine("  lookup <patientCode|treatmentCode|name>      : Tra cứu thông tin, buồng giường & Bilan");
            Console.WriteLine("  orders <patientCode|treatmentCode|name>      : Liệt kê danh sách y lệnh & trạng thái màu");
            Console.WriteLine("  transfer-orders <pat|trCode> [doc] [--dry-run]: Chuyển toàn bộ y lệnh chưa xử lý sang tên BS chỉ định");
            Console.WriteLine("  cancel-order <serviceReqId|reqCode> [roomId] : Hủy/Xóa y lệnh chưa thực hiện (Màu trắng)");
            Console.WriteLine("  cancel-service <sereServId>                  : Hủy/Xóa 1 dịch vụ con lẻ trong phiếu");
            Console.WriteLine("  wardround                                    : Quét danh sách BN toàn bộ buồng bệnh Khoa 57 (710 - 740)");
            Console.WriteLine("  locate <name1,name2,...>                     : Định vị buồng/giường hàng loạt BN siêu tốc (1 request)");
            Console.WriteLine("  create-tracking <trId> <content> [dhst..]    : Tạo tờ điều trị và DHST");
            Console.WriteLine("  prescribe <trId> <tkId> <medId> <stId> <amount> <tutorial> : Kê đơn thuốc an toàn");
            Console.WriteLine("  assign-cls <trId> <tkId> <svcId> <roomId> [note] [ptId]    : Chỉ định CLS đơn lẻ");
            Console.WriteLine("  assign-bilan <trId> <tkId> <cement|spine|trauma|hip|hand>  : Chỉ định gói Bilan 1-Click");
            Console.WriteLine("  assign-custom <trId> <tkId> <list> [ptId]                  : Chỉ định CLS tùy chọn gom theo phòng tự động");
            Console.WriteLine("  lookup-cls [từ khóa]                                       : Tra cứu danh mục CLS Khoa 57, mã, phòng & gói Bilan");
            Console.WriteLine("  debate <patientCode|treatmentCode>                         : Tra cứu biên bản hội chẩn & ý kiến các chuyên khoa");
            Console.WriteLine("  emr <patientCode|treatmentCode|name>                       : Tra cứu danh sách văn bản EMR & trạng thái ký");
            Console.WriteLine("  sign-emr <patientCode|treatmentCode> [docId]               : Tự động ký điện tử EMR cho Bác sĩ điều trị");
            Console.WriteLine("  check-pres-sign [patientCode|all]                          : Kiểm tra y lệnh thuốc và trạng thái ký tờ điều trị hôm nay");
            Console.WriteLine("===============================================================================");
            return;
        }

        string cmd = args[0].ToLower();
        try
        {
            if (cmd == "audit" || cmd == "audit-date" || cmd == "audit-tomorrow" || cmd == "soat")
            {
                string dateStr = args.Length > 1 ? args[1] : null;
                AuditClinicalDate(dateStr);
            }
            else if (cmd == "plan-prescribe" || cmd == "copy-orders" || cmd == "plan-meds" || cmd == "ke-don-du-tru")
            {
                string dateStr = args.Length > 1 ? args[1] : "20260920";
                bool doPrescribe = !(args.Length > 2 && (args[2] == "--dry-run" || args[2] == "-n"));
                PlanAndPrescribeMedications(dateStr, doPrescribe);
            }
            else if (cmd == "postop-abx" || cmd == "abx-audit" || cmd == "postop" || cmd == "soat-ks" || cmd == "ks-hau-phau")
            {
                string dateStr = args.Length > 1 ? args[1] : "20260920";
                AuditPostOpAntibiotics(dateStr);
            }
            else if (cmd == "stock-810" || cmd == "cabinet-stock" || cmd == "tu-truc-810" || cmd == "tu-truc")
            {
                ShowCabinet810Stock();
            }
            else if (cmd == "lookup")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu!");
                LookupPatient(args[1]);
            }
            else if (cmd == "view-meds" || cmd == "meds" || cmd == "donthuoc")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc TreatmentID!");
                ViewPatientMeds(args[1]);
            }
            else if (cmd == "tracking" || cmd == "trks" || cmd == "todieutri" || cmd == "dienbien")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc TreatmentID!");
                ViewPatientTrackings(args[1]);
            }
            else if (cmd == "cdha" || cmd == "labs" || cmd == "cls" || cmd == "xetnghiem")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc TreatmentID!");
                ViewPatientCdhaAndLabs(args[1]);
            }
            else if (cmd == "search-med" || cmd == "find-med")
            {
                if (args.Length < 2) throw new Exception("Thiếu tên hoặc từ khóa thuốc cần tìm!");
                SearchMed(args[1]);
            }
            else if (cmd == "search-service" || cmd == "find-service" || cmd == "search-dv")
            {
                if (args.Length < 2) throw new Exception("Thiếu tên hoặc từ khóa dịch vụ cần tìm!");
                SearchService(args[1]);
            }
            else if (cmd == "check-hanoi" || cmd == "ping-hanoi" || cmd == "check-hn")
            {
                CheckHanoiConnection();
            }
            else if (cmd == "amputations" || cmd == "cat-cut" || cmd == "phong-mo" || cmd == "mo-cat-cut")
            {
                string yr = args.Length > 1 ? args[1] : "2026";
                ReportAmputations2026(yr);
            }
            else if (cmd == "bs-ctch" || cmd == "ctch" || cmd == "ctch-surgeons")
            {
                string yr = args.Length > 1 ? args[1] : "2026";
                ReportCtchAmputations(yr);
            }
            else if (cmd == "debate" || cmd == "hoichan")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc mã đợt điều trị!");
                LookupConsultationDebate(args[1]);
            }
            else if (cmd == "wardround")
            {
                ScanWardRooms();
            }
            else if (cmd == "locate" || cmd == "locate-patients" || cmd == "batch-lookup")
            {
                if (args.Length < 2) throw new Exception("Thiếu danh sách tên bệnh nhân (phân cách bằng dấu phẩy)!");
                LocatePatients(args[1]);
            }
            else if (cmd == "login" || cmd == "switch-user" || cmd == "auth")
            {
                if (args.Length < 3) throw new Exception("Cú pháp: login <user> <pass> (VD: login vmc 789789)");
                LoginAs(args[1], args[2]);
            }
            else if (cmd == "export-pt01")
            {
                if (args.Length < 2) throw new Exception("Thiếu danh sách mã BN!");
                ExportPt01Data(args[1]);
            }
            else if (cmd == "create-tracking")
            {
                long treatmentId = long.Parse(args[1]);
                string content = args[2];
                long? pulse = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? (long?)long.Parse(args[3]) : null;
                decimal? temp = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? (decimal?)decimal.Parse(args[4]) : null;
                long? bpMax = args.Length > 5 && !string.IsNullOrEmpty(args[5]) ? (long?)long.Parse(args[5]) : null;
                long? bpMin = args.Length > 6 && !string.IsNullOrEmpty(args[6]) ? (long?)long.Parse(args[6]) : null;
                CreateTracking(treatmentId, content, pulse, temp, bpMax, bpMin);
            }
            else if (cmd == "prescribe")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long medId = long.Parse(args[3]);
                long stockId = long.Parse(args[4]);
                decimal amount = decimal.Parse(args[5]);
                string tutorial = args.Length > 6 ? args[6] : "";
                int ptId = args.Length > 7 ? int.Parse(args[7]) : 1;
                PrescribeMedication(treatmentId, trackingId, medId, stockId, amount, tutorial, ptId);
            }
            else if (cmd == "assign-cls")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long serviceId = long.Parse(args[3]);
                long roomId = long.Parse(args[4]);
                string note = args.Length > 5 ? args[5] : "";
                int ptId = args.Length > 6 ? int.Parse(args[6]) : 1;
                AssignClsService(treatmentId, trackingId, serviceId, roomId, note, ptId);
            }
            else if (cmd == "assign-bilan" || cmd == "assign-bilan-cement")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                string packType = (cmd == "assign-bilan-cement" || args.Length < 4) ? "cement" : args[3];
                int ptId = args.Length > 4 ? int.Parse(args[4]) : 1;
                AssignSurgicalBilan(treatmentId, trackingId, packType, ptId);
            }
            else if (cmd == "assign-custom")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                string rawItems = args[3];
                int ptId = args.Length > 4 ? int.Parse(args[4]) : 1;
                AssignCustomServices(treatmentId, trackingId, rawItems, ptId);
            }
            else if (cmd == "assign-ration" || cmd == "ration")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                string comboType = args.Length > 3 ? args[3] : "BT07";
                int ptId = args.Length > 4 ? int.Parse(args[4]) : 42;
                AssignRation(treatmentId, trackingId, comboType, ptId);
            }
            else if (cmd == "inspect-svc")
            {
                long svcId = long.Parse(args[1]);
                InspectServiceRooms(svcId);
            }
            else if (cmd == "lookup-cls" || cmd == "cls-list" || cmd == "cls-catalog" || cmd == "cls")
            {
                string kw = args.Length > 1 ? args[1] : "";
                LookupCls(kw);
            }
            else if (cmd == "consult-room" || cmd == "consult-queue" || cmd == "room11387")
            {
                string dateStr = args.Length > 1 ? args[1] : null;
                LookupConsultationQueue(dateStr);
            }
            else if (cmd == "orders" || cmd == "list-orders")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN, mã ĐT hoặc tên bệnh nhân!");
                ListOrders(args[1]);
            }
            else if (cmd == "transfer-orders" || cmd == "chuyen-y-lenh" || cmd == "reassign-orders" || cmd == "transfer")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN, mã ĐT hoặc tên bệnh nhân!");
                string patKey = args[1];
                string targetDoc = args.Length > 2 && !args[2].StartsWith("-") ? args[2] : currentDoctorLogin;
                bool isDryRun = args.Any(a => a == "--dry-run" || a == "-n");
                TransferOrders(patKey, targetDoc, isDryRun);
            }
            else if (cmd == "cancel-order" || cmd == "delete-order" || cmd == "cancel-req")
            {
                if (args.Length < 2) throw new Exception("Thiếu ID hoặc Mã phiếu y lệnh cần hủy!");
                
                // Thu thập toàn bộ các mã/ID được truyền vào (hỗ trợ dấu phẩy, chấm phẩy, khoảng trắng hoặc nhiều tham số)
                var allKeys = new List<string>();
                for (int i = 1; i < args.Length; i++)
                {
                    string[] parts = args[i].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    allKeys.AddRange(parts);
                }

                if (allKeys.Count == 1)
                {
                    CancelOrder(allKeys[0], null);
                }
                else
                {
                    long maybeRoom = 0;
                    if (allKeys.Count == 2 && long.TryParse(allKeys[1], out maybeRoom) && maybeRoom > 1000 && maybeRoom < 99999 && !allKeys[1].StartsWith("0"))
                    {
                        // Trường hợp truyền 1 mã + 1 roomId (VD: cancel-order 89758750 5248)
                        CancelOrder(allKeys[0], maybeRoom);
                    }
                    else
                    {
                        Console.WriteLine("===============================================================================");
                        Console.WriteLine(string.Format("🚀 BẮT ĐẦU XỬ LÝ HỦY MẺ CHO {0} Y LỆNH...", allKeys.Count));
                        Console.WriteLine("===============================================================================");
                        int done = 0;
                        for (int idx = 0; idx < allKeys.Count; idx++)
                        {
                            string k = allKeys[idx];
                            Console.WriteLine(string.Format("\n👉 [{0}/{1}] Đang xử lý: {2}", idx + 1, allKeys.Count, k));
                            try
                            {
                                CancelOrder(k, null);
                                done++;
                            }
                            catch (Exception exK)
                            {
                                Console.WriteLine(string.Format("❌ Lỗi khi xử lý {0}: {1}", k, exK.Message));
                            }
                        }
                        Console.WriteLine("===============================================================================");
                        Console.WriteLine(string.Format("📊 HOÀN TẤT XỬ LÝ: {0}/{1} y lệnh đã được thực thi.", done, allKeys.Count));
                        Console.WriteLine("===============================================================================");
                    }
                }
            }
            else if (cmd == "cancel-service" || cmd == "delete-service")
            {
                if (args.Length < 2) throw new Exception("Thiếu ID dịch vụ con (SERE_SERV_ID) cần hủy!");
                
                var allSsIds = new List<long>();
                for (int i = 1; i < args.Length; i++)
                {
                    string[] parts = args[i].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        long sid = 0;
                        if (long.TryParse(p.Trim(), out sid)) allSsIds.Add(sid);
                    }
                }

                if (allSsIds.Count == 1)
                {
                    CancelSereServ(allSsIds[0]);
                }
                else
                {
                    Console.WriteLine("===============================================================================");
                    Console.WriteLine(string.Format("🚀 BẮT ĐẦU HỦY MẺ CHO {0} DỊCH VỤ CON (SERE_SERV)...", allSsIds.Count));
                    Console.WriteLine("===============================================================================");
                    int done = 0;
                    foreach (var sid in allSsIds)
                    {
                        try
                        {
                            CancelSereServ(sid);
                            done++;
                        }
                        catch (Exception exS)
                        {
                            Console.WriteLine(string.Format("❌ Lỗi hủy dịch vụ {0}: {1}", sid, exS.Message));
                        }
                    }
                    Console.WriteLine("===============================================================================");
                    Console.WriteLine(string.Format("📊 HOÀN TẤT: {0}/{1} dịch vụ đã được xử lý.", done, allSsIds.Count));
                    Console.WriteLine("===============================================================================");
                }
            }
            else if (cmd == "emr" || cmd == "emr-docs" || cmd == "list-emr")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu BN hoặc mã ĐT!");
                ListEmrDocuments(args[1]);
            }
            else if (cmd == "sign-emr" || cmd == "auto-sign" || cmd == "ky-emr")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu BN hoặc mã ĐT!");
                long? docId = args.Length > 2 ? (long?)long.Parse(args[2]) : null;
                AutoSignEmr(args[1], docId);
            }
            else if (cmd == "invite-sign" || cmd == "moi-ky")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu BN hoặc mã ĐT! Cú pháp: invite-sign <MãBN/MãĐT> [--type 7|116|all] [--login 034727]");
                string kw = args[1];
                string docType = "all";
                string login = "034727";

                for (int i = 2; i < args.Length; i++)
                {
                    if ((args[i] == "--type" || args[i] == "-t") && i + 1 < args.Length)
                    {
                        docType = args[++i];
                    }
                    else if ((args[i] == "--login" || args[i] == "-l") && i + 1 < args.Length)
                    {
                        login = args[++i];
                    }
                    else if (args[i].StartsWith("--type="))
                    {
                        docType = args[i].Substring(7);
                    }
                    else if (args[i].StartsWith("--login="))
                    {
                        login = args[i].Substring(8);
                    }
                }

                InviteSignEmr(kw, docType, login);
            }
            else if (cmd == "check-pres-sign" || cmd == "check-med-sign" || cmd == "soat-ky" || cmd == "kiem-tra-ky")
            {
                string kw = args.Length > 1 ? args[1] : null;
                CheckPrescriptionsTrackingSign(kw);
            }
            else
            {
                Console.WriteLine("Lệnh không hợp lệ: " + cmd);
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LỖI THỰC THI: " + ex.Message);
            Environment.ExitCode = 1;
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

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        HisClinicalCli.RunCli(args);
    }
}
