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
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
        {
            string name = new AssemblyName(a.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", name);
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR", name);
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        Run();
    }

    static void Run()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string token = null;

        string[] candidates = new string[] {
            Path.Combine(baseDir, "doctor_hn.token"),
            Path.Combine(baseDir, "doctor_standalone.token"),
            Path.Combine(baseDir, "Logs", "LogSystem.txt")
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                if (c.EndsWith(".token"))
                {
                    var parts = File.ReadAllText(c, Encoding.UTF8).Trim().Split('|');
                    if (parts.Length >= 2 && parts[0].Length == 64)
                    {
                        token = parts[0];
                        break;
                    }
                }
                else
                {
                    using (var fs = new FileStream(c, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        long len = fs.Length;
                        int bufSize = (int)Math.Min(131072L, len);
                        fs.Seek(len - bufSize, SeekOrigin.Begin);
                        byte[] buf = new byte[bufSize];
                        int r = fs.Read(buf, 0, bufSize);
                        string chunk = Encoding.UTF8.GetString(buf, 0, r);
                        int idx = chunk.LastIndexOf("TokenCode|");
                        if (idx >= 0 && chunk.Length >= idx + 10 + 64)
                        {
                            token = chunk.Substring(idx + 10, 64);
                            break;
                        }
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Token not found");
            return;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var cp = new CommonParam();

        string[] pCodes = new string[] { "0004061261", "0004062280", "0004093319" };

        foreach (var pc in pCodes)
        {
            Console.WriteLine("===============================================================================");
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pc };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, cp);
            if (trs == null || trs.Count == 0)
            {
                Console.WriteLine("Không tìm thấy BN: " + pc);
                continue;
            }
            var tr = trs.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
            Console.WriteLine(string.Format("BỆNH NHÂN: {0} | Mã BN: {1} | Mã ĐT: {2} | ID: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
            Console.WriteLine(string.Format("Vào viện: {0} | Ra viện: {1} | IsPause: {2} | Khoa: {3} ({4})",
                tr.IN_TIME, tr.OUT_TIME, tr.IS_PAUSE, tr.END_DEPARTMENT_NAME, tr.END_DEPARTMENT_ID));
            Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_SUB_CODE));

            // 1. Check Trackings
            var trkF = new HisTrackingFilter { TREATMENT_ID = tr.ID };
            var trks = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkF, cp) ?? new List<HIS_TRACKING>();
            Console.WriteLine(string.Format("\n--- TỜ ĐIỀU TRỊ ({0} tờ) ---", trks.Count));
            foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
            {
                string cSnippet = (t.CONTENT ?? "").Replace("\r", " ").Replace("\n", " ");
                if (cSnippet.Length > 80) cSnippet = cSnippet.Substring(0, 80) + "...";
                Console.WriteLine(string.Format("  • [{0}] ID:{1} | Dept:{2} | Content: {3}", t.TRACKING_TIME, t.ID, t.DEPARTMENT_ID, cSnippet));
            }

            // 2. Check Service Reqs
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, cp) ?? new List<V_HIS_SERVICE_REQ>();
            Console.WriteLine(string.Format("\n--- Y LỆNH DỊCH VỤ / THUỐC ({0} phiếu) ---", srs.Count));
            var whiteOrders = srs.Where(x => x.SERVICE_REQ_STT_ID == 1).ToList();
            Console.WriteLine(string.Format("  • Tổng số y lệnh TRẮNG (chưa thực hiện): {0}", whiteOrders.Count));
            foreach (var wo in whiteOrders)
            {
                Console.WriteLine(string.Format("    - ID:{0} | Mã:{1} | Type:{2} ({3}) | Time:{4} | ReqDoc:{5} ({6}) | ReqRoom:{7}",
                    wo.ID, wo.SERVICE_REQ_CODE, wo.SERVICE_REQ_TYPE_ID, wo.SERVICE_REQ_TYPE_NAME, wo.INTRUCTION_TIME, wo.REQUEST_USERNAME, wo.REQUEST_LOGINNAME, wo.REQUEST_ROOM_NAME));
            }

            // 3. Check Surgery / PTTT SereServ
            var ssf = new HisSereServViewFilter { TREATMENT_ID = tr.ID };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, cp) ?? new List<V_HIS_SERE_SERV>();
            var pttt = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 4 || (x.TDL_SERVICE_NAME ?? "").ToLower().Contains("phẫu thuật") || (x.TDL_SERVICE_NAME ?? "").ToLower().Contains("thủ thuật")).ToList();
            Console.WriteLine(string.Format("\n--- PTTT / PHẪU THUẬT / THỦ THUẬT ({0} dịch vụ) ---", pttt.Count));
            foreach (var p in pttt)
            {
                Console.WriteLine(string.Format("    - Service: [{0}] {1} | ReqTime:{2}",
                    p.TDL_SERVICE_CODE, p.TDL_SERVICE_NAME, p.TDL_INTRUCTION_TIME));
            }
        }
    }
}
