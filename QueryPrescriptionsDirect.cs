using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
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

class Program
{
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

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
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
        if (rootDir != null) Directory.SetCurrentDirectory(rootDir.FullName);

        string token = ReadLiveToken();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        long[] treatmentIds = new long[] { 7163469, 7163700, 7163879, 7136089, 7149475 };
        string[] names = new string[] { 
            "VŨ ĐỨC BÌNH (P714 - G17)", 
            "NGUYỄN VĂN THƠ (P714 - G18)", 
            "VI TRUNG HIẾU (P717 - G7)", 
            "NGUYỄN XUÂN CHÍN (P717 - G8)", 
            "NGUYỄN VĂN TRỌNG (P717 - G9)" 
        };

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🏥 BỆNH NHÂN: {0} (TreatmentID: {1})", names[i], tId));
            Console.WriteLine("===============================================================================");

            // 1. Tờ điều trị & Diễn biến ngày 05/09 và 06/09
            var trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
            if (trks != null && trks.Count > 0)
            {
                var targetTrks = trks.Where(x => x.TRACKING_TIME.ToString().StartsWith("20260905") || x.TRACKING_TIME.ToString().StartsWith("20260906") || x.TRACKING_TIME.ToString().StartsWith("20260904")).OrderByDescending(x => x.TRACKING_TIME).ToList();
                Console.WriteLine("📝 TỜ ĐIỀU TRỊ GẦN ĐÂY:");
                foreach (var tk in targetTrks)
                {
                    string tStr = tk.TRACKING_TIME.ToString();
                    string dStr = string.Format("{0}/{1} {2}:{3}", tStr.Substring(6, 2), tStr.Substring(4, 2), tStr.Substring(8, 2), tStr.Substring(10, 2));
                    Console.WriteLine(string.Format("  • Ngày {0}: Diễn biến: \"{1}\"", dStr, tk.CONTENT != null ? tk.CONTENT.Replace("\r\n", " ").Replace("\n", " ") : ""));
                }
            }

            // 2. Service Requests
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (srs != null)
            {
                var presSrs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList();
                Console.WriteLine(string.Format("\n💊 CÁC PHIẾU ĐƠN THUỐC (Tổng số: {0}):", presSrs.Count));
                foreach (var ps in presSrs.OrderByDescending(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("  • [{0}] Ngày y lệnh: {1} | Loại: {2} | Mã phiếu: {3} | Trạng thái: {4}", 
                        ps.ID, ps.INTRUCTION_TIME, ps.SERVICE_REQ_TYPE_NAME, ps.SERVICE_REQ_CODE, ps.SERVICE_REQ_STT_NAME));
                }
            }

            // 3. SereServ (Medicines)
            var ssf = new HisSereServViewFilter { TREATMENT_ID = tId };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null)
            {
                var medItems = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 6).ToList();
                Console.WriteLine(string.Format("\n💊 CHI TIẾT THUỐC TRONG SERE_SERV (Tổng số: {0}):", medItems.Count));
                var grp = medItems.GroupBy(x => x.TDL_INTRUCTION_TIME.ToString().Substring(0, 8)).OrderByDescending(g => g.Key);
                foreach (var g in grp)
                {
                    string dStr = string.Format("{0}/{1}/{2}", g.Key.Substring(6, 2), g.Key.Substring(4, 2), g.Key.Substring(0, 4));
                    Console.WriteLine(string.Format("  📅 Ngày {0} ({1} thuốc):", dStr, g.Count()));
                    foreach (var item in g)
                    {
                        Console.WriteLine(string.Format("     - {0} | SL: {1:0.##}", item.TDL_SERVICE_NAME, item.AMOUNT));
                    }
                }
            }

            Console.WriteLine();
        }
    }
}
