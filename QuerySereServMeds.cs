using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
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
            Console.WriteLine(string.Format("🧑 {0} (TreatmentID: {1}):", names[i], tId));

            // Service Req for Prescriptions
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (srs != null)
            {
                var presSrs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList();
                Console.WriteLine(string.Format("   Tổng số phiếu đơn thuốc (ServiceReq): {0}", presSrs.Count));
                foreach (var ps in presSrs.OrderByDescending(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("     • Ngày y lệnh: {0} | Loại: {1} | Mã: {2} | Kho: {3} | STT: {4}", 
                        ps.INTRUCTION_TIME, ps.SERVICE_REQ_TYPE_NAME, ps.SERVICE_REQ_CODE, ps.REQUEST_ROOM_NAME, ps.SERVICE_REQ_STT_NAME));
                }
            }

            // SereServ for Medicines
            var ssf = new HisSereServViewFilter { TREATMENT_ID = tId };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null)
            {
                var medItems = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 6).ToList();
                Console.WriteLine(string.Format("   Tổng số mục thuốc (SereServ): {0}", medItems.Count));
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
            Console.WriteLine();
        }
    }
}
