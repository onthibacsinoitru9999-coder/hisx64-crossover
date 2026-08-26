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

    public static string ReadLiveToken()
    {
        string[] candidates = new string[] {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "HLSLogSystem.txt"),
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };

        foreach (var logPath in candidates)
        {
            if (File.Exists(logPath))
            {
                try
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
        return null;
    }

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;
        param = new CommonParam();
        string tokenCode = ReadLiveToken();

        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null) tokenCode = token.TokenCode;
                }
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(tokenCode))
        {
            currentToken = tokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 },
                        new RoomSDO { RoomId = 5252 },
                        new RoomSDO { RoomId = 5251 },
                        new RoomSDO { RoomId = 5257 }
                    }
                };
                myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
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
        if (!string.IsNullOrEmpty(currentToken))
        {
            try
            {
                HisDepartmentFilter df = new HisDepartmentFilter { ID = 57 };
                var depts = myAdapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", ApiConsumers.MosConsumer, df, param);
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
        if (string.IsNullOrEmpty(currentToken))
        {
            Console.WriteLine("❌ Chưa có token xác thực!");
            return;
        }

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.KEY_WORD = keyword;

        var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
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
        var bedList = myAdapter.FetchList<V_HIS_BED_LOG>("api/HisBedLog/GetView", ApiConsumers.MosConsumer, bf, param);
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
        var trkList = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", ApiConsumers.MosConsumer, trkFilter, param);
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
