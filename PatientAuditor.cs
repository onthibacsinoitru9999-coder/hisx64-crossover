using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
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
}

public class Program
{
    static void Main()
    {
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
        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        string token = "";
        string logPath = @"LogSystem.txt";
        if (File.Exists(logPath))
        {
            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                string text = reader.ReadToEnd();
                int idx = text.LastIndexOf("TokenCode|");
                if (idx >= 0 && text.Length >= idx + 10 + 64)
                {
                    token = text.Substring(idx + 10, 64);
                }
            }
        }
        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var param = new CommonParam();

        string[] codes = new string[] { "0004033304", "0004048113", "0003380587", "0003002690", "0003863470", "0001344789", "0004093343", "0004093319" };

        foreach (var c in codes)
        {
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("KIEM TRA BENH NHAN: " + c);
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = c.PadLeft(10, '0') };
            var listT = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            if (listT == null || listT.Count == 0)
            {
                Console.WriteLine("KHONG TIM THAY HO SO!");
                continue;
            }
            var t = listT.OrderByDescending(x => x.IN_TIME).First();
            Console.WriteLine(string.Format("BN: {0} | Ma BN: {1} | Ma DT: {2} | ID: {3} | Khoa: {4}",
                t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.ID, t.LAST_DEPARTMENT_ID));
            Console.WriteLine("Chan doan: " + t.ICD_NAME + " - " + t.ICD_SUB_CODE + " - " + t.ICD_TEXT);

            // 1. Tracking
            var trf = new HisTrackingViewFilter { TREATMENT_ID = t.ID };
            var trs = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, trf, param);
            if (trs != null && trs.Count > 0)
            {
                var trSorted = trs.OrderByDescending(x => x.TRACKING_TIME).Take(2).ToList();
                foreach (var tr in trSorted)
                {
                    Console.WriteLine(string.Format("📄 TO DIEU TRI [{0}] (ID: {1}):\n{2}", tr.TRACKING_TIME, tr.ID, tr.CONTENT));
                }
            }
            else
            {
                Console.WriteLine("⚠️ CHUA CO TO DIEU TRI NAO!");
            }

            // 2. Prescriptions & Rations
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = t.ID };
            var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, param);
            if (reqs != null && reqs.Count > 0)
            {
                Console.WriteLine("💊 Y LENH GAN DAY:");
                var recentReqs = reqs.Where(x => {
                    string s = x.INTRUCTION_TIME.ToString();
                    return s.StartsWith("20260928") || s.StartsWith("20260929") || s.StartsWith("20260930");
                }).OrderBy(x => x.INTRUCTION_TIME).ToList();

                foreach (var r in recentReqs)
                {
                    Console.WriteLine(string.Format("  • [ID: {0} | Loai: {1} ({2}) | Luc: {3} | Ma: {4}]",
                        r.ID, r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_TYPE_NAME, r.INTRUCTION_TIME, r.SERVICE_REQ_CODE));

                    var ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var s in sss)
                        {
                            Console.WriteLine(string.Format("      - {0} | SL: {1} {2} | Gia: {3}",
                                s.TDL_SERVICE_NAME, s.AMOUNT, s.SERVICE_UNIT_NAME, s.PRICE));
                        }
                    }
                }
            }
        }
    }
}
