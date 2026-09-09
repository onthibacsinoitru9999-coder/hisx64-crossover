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
            return null;
        };

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string tokenCode = null;
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            string p = Path.Combine(cur.FullName, "Logs", "LogSystem.txt");
            if (File.Exists(p))
            {
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buf = new byte[Math.Min(131072L, fs.Length)];
                    fs.Seek(fs.Length - buf.Length, SeekOrigin.Begin);
                    fs.Read(buf, 0, buf.Length);
                    string str = Encoding.UTF8.GetString(buf);
                    int idx = str.LastIndexOf("TokenCode|");
                    if (idx >= 0) { tokenCode = str.Substring(idx + 10, 64); break; }
                }
            }
            cur = cur.Parent;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        // Kích hoạt WorkInfo phòng 5248
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }

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
            Console.WriteLine(string.Format("🧑 {0} (TreatmentID: {1}):", names[i], tId));

            // SereServ for Medicines
            var ssf = new HisSereServViewFilter { TREATMENT_ID = tId };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null)
            {
                var medItems = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 6).ToList();
                Console.WriteLine(string.Format("   Tổng số mục thuốc: {0}", medItems.Count));
                if (medItems.Count == 0)
                {
                    Console.WriteLine("   ❌ Chưa có mục thuốc nào trong SereServ.");
                }
                var grp = medItems.GroupBy(x => x.TDL_INTRUCTION_TIME.ToString().Substring(0, 8)).OrderByDescending(g => g.Key);
                foreach (var g in grp)
                {
                    string dStr = string.Format("{0}/{1}/{2}", g.Key.Substring(6, 2), g.Key.Substring(4, 2), g.Key.Substring(0, 4));
                    Console.WriteLine(string.Format("     📅 Ngày y lệnh {0} ({1} thuốc):", dStr, g.Count()));
                    foreach (var item in g)
                    {
                        Console.WriteLine(string.Format("        - {0} | SL: {1:0.##}", item.TDL_SERVICE_NAME, item.AMOUNT));
                    }
                }
            }
            else
            {
                Console.WriteLine("   (sss is null. Msg: " + (param.Messages != null ? string.Join(", ", param.Messages) : "none") + ")");
            }

            // Tracking
            var trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
            if (trks != null && trks.Count > 0)
            {
                Console.WriteLine("   📝 Các tờ điều trị gần đây:");
                foreach (var tk in trks.OrderByDescending(x => x.TRACKING_TIME).Take(3))
                {
                    string tStr = tk.TRACKING_TIME.ToString();
                    string timeFormatted = tStr.Length >= 12 ? string.Format("{0}/{1} {2}:{3}", tStr.Substring(6, 2), tStr.Substring(4, 2), tStr.Substring(8, 2), tStr.Substring(10, 2)) : tStr;
                    Console.WriteLine(string.Format("     • [{0}] Diễn biến: {1}", timeFormatted, tk.CONTENT != null ? tk.CONTENT.Replace("\r\n", " ").Replace("\n", " ") : ""));
                }
            }
            Console.WriteLine();
        }
    }
}
