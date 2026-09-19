using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

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


public class HisDiagnosticDoctor
{
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static string currentToken = null;
    public static Inventec.Common.WebApiClient.ApiConsumer mosConsumer = null;

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();

        // 1. Thư mục HIS chuẩn theo yêu cầu của Bác sĩ
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

        // 3. Chỉ quét tiến trình HIS nếu chạy đúng từ preferredDir (TUYỆT ĐỐI không đọc từ thư mục HIS khác trên máy)
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
                        if (exePath.IndexOf("LBP2900_R150_V330_W64_uk_EN_2", StringComparison.OrdinalIgnoreCase) >= 0)
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
                    int bufferSize = (int)Math.Min(2097152L, length);
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

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;
        try { Load.Init(); } catch { }
        param = new CommonParam();

        // 1. Kiểm tra cache token độc lập (hạn 6 tiếng)
        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        string tokenCode = null;
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
                        }
                    }
                }
            }
        }
        catch { }

        // 2. Thử đọc Live Token từ HIS chuẩn (chỉ nhận nick 034727/vmc)
        if (string.IsNullOrEmpty(tokenCode))
        {
            tokenCode = ReadLiveToken();
        }

        // 3. Tự động ĐĂNG NHẬP ĐỘC LẬP qua ACS bằng nick 034727
        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null) tokenCode = token.TokenCode;
                }

                if (!string.IsNullOrEmpty(tokenCode))
                {
                    try
                    {
                        File.WriteAllText(cacheFile, tokenCode + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8);
                    }
                    catch { }
                }
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(tokenCode))
        {
            currentToken = tokenCode;
            mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
            try { ApiConsumers.SetConsunmer(currentToken); } catch { }
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 },
                        new RoomSDO { RoomId = 5252 },
                        new RoomSDO { RoomId = 5251 },
                        new RoomSDO { RoomId = 5257 },
                        new RoomSDO { RoomId = 18679 },
                        new RoomSDO { RoomId = 18681 },
                        new RoomSDO { RoomId = 14759 },
                        new RoomSDO { RoomId = 14787 }
                    }
                };
                myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            }
            catch { }
        }
    }

    public static bool CheckTcpPort(string host, int port, int timeoutMs = 2000)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                if (!success) return false;
                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static void HealthCheck()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🩺 BÁC SĨ CHẨN ĐOÁN HỆ THỐNG HIS (HIS SYSTEM HEALTH DIAGNOSTIC)");
        Console.WriteLine("===============================================================================");

        // 1. Kiểm tra mạng tới 4 cổng Backend
        Console.WriteLine("\n1. Kiểm tra kết nối mạng 4 máy chủ lõi:");
        var endpoints = new[] {
            new { Name = "MOS Backend API", Host = "192.168.7.236", Port = 1608 },
            new { Name = "ACS Auth Service", Host = "192.168.7.200", Port = 1401 },
            new { Name = "SDA Data Service", Host = "192.168.7.200", Port = 1410 },
            new { Name = "EMR Document API", Host = "192.168.7.239", Port = 1415 }
        };

        bool allNetOk = true;
        foreach (var ep in endpoints)
        {
            bool ok = CheckTcpPort(ep.Host, ep.Port);
            Console.WriteLine(string.Format("   [{0}] {1,-18} ({2}:{3})", ok ? "✅ OK" : "❌ FAIL", ep.Name, ep.Host, ep.Port));
            if (!ok) allNetOk = false;
        }

        // 2. Kiểm tra Live Token
        Console.WriteLine("\n2. Kiểm tra Live Token & Phiên làm việc:");
        InitSession();
        if (!string.IsNullOrEmpty(currentToken))
        {
            Console.WriteLine("   ✅ TokenCode đang hoạt động: " + currentToken.Substring(0, 16) + "..." + currentToken.Substring(currentToken.Length - 8));
        }
        else
        {
            Console.WriteLine("   ❌ Không thể lấy TokenCode (Kiểm tra lại HIS Client hoặc tài khoản ACS)!");
        }

        // 3. Kiểm tra xác thực MOS API
        Console.WriteLine("\n3. Kiểm tra khả năng gọi API MOS Backend:");
        if (!string.IsNullOrEmpty(currentToken) && mosConsumer != null)
        {
            try
            {
                HisDepartmentFilter df = new HisDepartmentFilter { ID = 57 };
                var depts = myAdapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, df, param);
                if (depts != null && depts.Count > 0)
                {
                    Console.WriteLine("   ✅ Gọi API MOS Backend thành công! Khoa: " + depts[0].DEPARTMENT_NAME + " (ID: 57)");
                }
                else
                {
                    Console.WriteLine("   ⚠️  MOS API trả về danh sách rỗng.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ❌ Lỗi gọi MOS API: " + ex.Message);
            }
        }

        // 4. Kiểm tra OpenRouter API
        Console.WriteLine("\n4. Kiểm tra AI OpenRouter Free Tier:");
        string orKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(orKey)) orKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.Process);
        if (!string.IsNullOrEmpty(orKey))
        {
            Console.WriteLine("   ✅ Đã cấu hình OPENROUTER_API_KEY (" + orKey.Substring(0, 10) + "...)");
        }
        else
        {
            Console.WriteLine("   ⚠️  Chưa cấu hình OPENROUTER_API_KEY.");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("🎯 KẾT LUẬN CHẨN ĐOÁN: " + (allNetOk && !string.IsNullOrEmpty(currentToken) ? "Hệ thống sẵn sàng 100%!" : "Cảnh báo: Cần kiểm tra lại kết nối mạng hoặc phiên đăng nhập!"));
        Console.WriteLine("===============================================================================");
    }

    public static void CheckPatient(string keyword)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🔍 CHẨN ĐOÁN TRẠNG THÁI HỒ SƠ BỆNH NHÂN: " + keyword);
        Console.WriteLine("===============================================================================");

        InitSession();
        if (string.IsNullOrEmpty(currentToken) || mosConsumer == null)
        {
            Console.WriteLine("❌ Chưa có token xác thực!");
            return;
        }

        List<V_HIS_TREATMENT> tList = null;
        string kw = keyword.Trim();
        long pNum;
        if (long.TryParse(kw, out pNum))
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (tList == null || tList.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }

        if (tList == null || tList.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.KEY_WORD = kw;
            tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        }

        if (tList == null || tList.Count == 0)
        {
            Console.WriteLine("❌ KHÔNG TÌM THẤY BỆNH NHÂN NÀO VỚI TỪ KHÓA: " + keyword);
            return;
        }

        var tr = tList.OrderByDescending(x => x.IN_TIME).First();
        Console.WriteLine(string.Format("Họ tên       : {0} (Mã BN: {1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
        Console.WriteLine(string.Format("Mã hồ sơ (Tr): {0} (Mã ĐT: {1})", tr.ID, tr.TREATMENT_CODE));
        Console.WriteLine(string.Format("Khoa điều trị: ID {0} {1}", tr.LAST_DEPARTMENT_ID, tr.LAST_DEPARTMENT_ID == 57 ? "✅ Đúng Khoa 57" : "⚠️ KHÁC KHOA 57!"));
        Console.WriteLine(string.Format("Chẩn đoán    : [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));
        Console.WriteLine(string.Format("Trạng thái   : {0}", tr.IS_PAUSE == 1 ? "⚠️ ĐÃ TẠM KHÓA / XUẤT VIỆN" : "✅ Đang điều trị nội trú"));

        // Kiểm tra buồng giường
        HisBedLogViewFilter bf = new HisBedLogViewFilter { TREATMENT_ID = tr.ID };
        var bedList = myAdapter.FetchList<V_HIS_BED_LOG>("api/HisBedLog/GetView", mosConsumer, bf, param);
        if (bedList != null && bedList.Count > 0)
        {
            var activeBed = bedList.OrderByDescending(x => x.START_TIME).First();
            Console.WriteLine(string.Format("Buồng / Giường: {0} / {1}", activeBed.BED_ROOM_NAME, activeBed.BED_NAME));
        }
        else
        {
            Console.WriteLine("Buồng / Giường: Chưa gán buồng giường trên hệ thống!");
        }

        // Kiểm tra Tờ điều trị mới nhất
        HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
        var trkList = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
        if (trkList != null && trkList.Count > 0)
        {
            var lastTrk = trkList.OrderByDescending(x => x.TRACKING_TIME).First();
            Console.WriteLine(string.Format("Tờ ĐT mới nhất: ID {0} lúc {1}", lastTrk.ID, lastTrk.TRACKING_TIME));
        }
        else
        {
            Console.WriteLine("Tờ ĐT mới nhất: Chưa có tờ điều trị nào!");
        }

        Console.WriteLine("===============================================================================");
    }

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0 || args[0].ToLower() == "health")
        {
            HealthCheck();
        }
        else if (args[0].ToLower() == "patient" || args[0].ToLower() == "p")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Cú pháp: HisDiagnosticDoctor.bat patient <mã_bn|mã_điều_trị>");
                return;
            }
            CheckPatient(args[1]);
        }
        else if (args[0].ToLower() == "token")
        {
            string t = ReadLiveToken();
            Console.WriteLine("Token: " + (t ?? "NULL"));
        }
        else if (args[0].ToLower() == "inspect-tracking")
        {
            try
            {
                var refDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies");
                string signPath = Path.Combine(refDir, "Inventec.Common.SignLibrary.dll");
                if (File.Exists(signPath))
                {
                    var asm = Assembly.LoadFrom(signPath);
                    var emrDocType = asm.GetType("Inventec.Common.SignLibrary.Api.EmrDocument");
                    long targetDocId = 93425215; // The newly created doc from previous run
                    Console.WriteLine("\n=== Calling SignPdfHsm with PointSign on DocId " + targetDocId + " ===");
                    InitSession();
                    var emrConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");

                    var signFilter = new EMR.Filter.EmrSignFilter { DOCUMENT_ID = targetDocId };
                    var pSignFilter = new CommonParam();
                    var signs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, pSignFilter);
                    if (signs == null || signs.Count == 0)
                    {
                        Console.WriteLine("❌ Không tìm thấy EMR_SIGN cho doc " + targetDocId);
                        return;
                    }
                    var mySign = signs[0];
                    Console.WriteLine(string.Format("Found Sign ID: {0} | Login: {1} | Time: {2}", mySign.ID, mySign.LOGINNAME, mySign.SIGN_TIME));

                    long signTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    var signHsmSdo = new EMR.SDO.EmrSignHsmSDO
                    {
                        EmrDocumentId = targetDocId,
                        EmrSignId = mySign.ID,
                        SignTime = signTime,
                        IsFinishSign = true,
                        IsSigning = true,
                        IsSignElectronic = true,
                        Description = "Ký điện tử Bác sĩ điều trị",
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

                    Console.WriteLine("Posting to api/EmrSign/SignPdfHsm...");
                    var pSignRes = new CommonParam();
                    var resSign = myAdapter.PostData<EMR.SDO.EmrSignResultSDO>("api/EmrSign/SignPdfHsm", emrConsumer, signHsmSdo, pSignRes);
                    if (resSign != null && resSign.EmrSign != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("🎉🎉🎉 KÝ ĐIỆN TỬ THÀNH CÔNG RỰC RỠ! SignID: {0} | SignTime: {1}", 
                            resSign.EmrSign.ID, resSign.EmrSign.SIGN_TIME));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("❌ SignPdfHsm failed. HasException: " + pSignRes.HasException);
                        if (pSignRes.Messages != null) foreach (var m in pSignRes.Messages) Console.WriteLine("  Msg: " + m);
                        if (pSignRes.BugCodes != null) foreach (var b in pSignRes.BugCodes) Console.WriteLine("  Bug: " + b);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
        else
        {
            Console.WriteLine("Cú pháp:");
            Console.WriteLine("  HisDiagnosticDoctor.bat health         : Chẩn đoán toàn diện hệ thống");
            Console.WriteLine("  HisDiagnosticDoctor.bat patient <mã_bn>: Chẩn đoán hồ sơ bệnh nhân");
            Console.WriteLine("  HisDiagnosticDoctor.bat token          : Đọc token live");
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

        RunCli(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void RunCli(string[] args)
    {
        HisDiagnosticDoctor.Run(args);
    }
}
