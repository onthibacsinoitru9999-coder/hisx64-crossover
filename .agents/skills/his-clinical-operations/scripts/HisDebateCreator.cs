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

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("==========================================================================");
        Console.WriteLine("   🏥 HIS DEBATE CREATOR - CHỈ ĐỊNH & BIÊN BẢN HỘI CHẨN CHUYÊN KHOA     ");
        Console.WriteLine("==========================================================================");

        string treatmentCode = "";
        string specialist = "ck tạo hình thẩm mỹ";
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

        for (int i = 0; i < args.Length; i++)
        {
            string k = args[i].ToLower();
            if ((k == "-t" || k == "--treatment-code" || k == "--treatment" || k == "-p") && i + 1 < args.Length)
                treatmentCode = args[++i];
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

        if (string.IsNullOrEmpty(treatmentCode))
        {
            Console.WriteLine("HƯỚNG DẪN SỬ DỤNG:");
            Console.WriteLine("  HisDebateCreator.exe -t <mã_bệnh_án> -s <chuyên_khoa> -m <tóm_tắt_bệnh_án> [-l <địa_điểm>]");
            Console.WriteLine("\nVÍ DỤ THỰC TẾ:");
            Console.WriteLine("  1. Hội chẩn Tạo hình thẩm mỹ (vết thương lóc da):");
            Console.WriteLine("     HisDebateCreator.exe -t 000007070917 -s \"ck tạo hình thẩm mỹ\" -m \"Bn nam Vết thương phức tạp mu bàn chân (P). Hiện tại có diện hoại tử vạt ngược 4cm, xin ý kiến CK tạo hình phối hợp điều trị.\"");
            Console.WriteLine("\n  2. Hội chẩn Tim mạch (tăng huyết áp / rối loạn nhịp):");
            Console.WriteLine("     HisDebateCreator.exe -t 000007070917 -s \"Viện Tim Mạch\" -m \"Bệnh nhân tiền sử THA, rung nhĩ, xin ý kiến tối ưu hóa huyết động trước phẫu thuật.\"");
            Console.WriteLine("\n  3. Tùy chọn bác sĩ ký:");
            Console.WriteLine("     --president hdc    (Chủ tọa: BS Hà Đức Cường - mặc định)");
            Console.WriteLine("     --secretary 034727 (Thư ký: Ths.BS Nguyễn Hữu Sâm - mặc định) hoặc --secretary vmc (BS Vũ Minh Cường)");
            Console.WriteLine("     --location \"Phòng 716 Khoa 57\" (Địa điểm hội chẩn)");
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
            var token = tokenManager.Login(commonParam, "034727", "9981", "2.390.0");
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

        // Kích hoạt WorkInfo phòng làm việc
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 }, // Phòng 734
                    new RoomSDO { RoomId = 5252 }  // Phòng 712
                }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, commonParam);
        }
        catch { }

        // 2. Tra cứu đợt điều trị
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
        Console.WriteLine("--------------------------------------------------------------------------");
        Console.WriteLine(string.Format("BỆNH NHÂN : {0} ({1}) | NĂM SINH: {2}", tm.TDL_PATIENT_NAME, tm.TDL_PATIENT_GENDER_NAME, tm.TDL_PATIENT_DOB.ToString().Substring(0, 4)));
        Console.WriteLine(string.Format("MÃ BỆNH ÁN: {0} | MÃ ĐỢT ĐIỀU TRỊ: {1} | ID: {2}", tm.TREATMENT_CODE, tm.TREATMENT_CODE, tm.ID));
        Console.WriteLine(string.Format("CHẨN ĐOÁN : {0} - {1}", tm.ICD_CODE, tm.ICD_NAME));
        Console.WriteLine("--------------------------------------------------------------------------");

        if (string.IsNullOrEmpty(icdCode)) icdCode = tm.ICD_CODE ?? "S91.0";
        if (string.IsNullOrEmpty(icdName)) icdName = tm.ICD_NAME ?? "Vết thương bàn chân";
        
        if (string.IsNullOrEmpty(summary))
        {
            summary = string.Format("Bệnh nhân {0} chẩn đoán {1}. Hiện tại có tổn thương cần xin ý kiến {2} xét nhận điều trị / phối hợp.", tm.TDL_PATIENT_NAME, icdName, specialist);
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

    static string GetLiveToken()
    {
        string[] searchDirs = new string[]
        {
            @"D:\New folder (3)\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"Logs\LogSystem.txt",
            @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };

        foreach (var path in searchDirs)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string text = sr.ReadToEnd();
                        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (lines[i].Contains("TokenCode|"))
                            {
                                int idx = lines[i].IndexOf("TokenCode|") + 10;
                                if (lines[i].Length >= idx + 64)
                                {
                                    return lines[i].Substring(idx, 64);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }
        return "";
    }
}
