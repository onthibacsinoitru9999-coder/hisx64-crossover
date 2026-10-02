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

namespace RvLookup
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
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

            string c = Path.Combine(baseDir, "doctor_hn.token");
            if (File.Exists(c))
            {
                var parts = File.ReadAllText(c, Encoding.UTF8).Trim().Split('|');
                if (parts.Length >= 2 && parts[0].Length == 64) token = parts[0];
            }

            var mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var adapter = new MyAdapter();
            var cp = new CommonParam();

            // Query trackings from 25/09/2026
            var tf = new HisTrackingViewFilter { DEPARTMENT_ID = 57, TRACKING_TIME_FROM = 20260925000000 };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, tf, cp);
            if (trks != null)
            {
                var rvs = trks.Where(x => (x.CONTENT ?? "").ToLower().Contains("ra viện") || (x.MEDICAL_INSTRUCTION ?? "").ToLower().Contains("ra viện") || (x.CONTENT ?? "").ToLower().Contains("rvsam")).Take(10).ToList();
                Console.WriteLine(string.Format("Tìm thấy {0} tờ điều trị ra viện:", rvs.Count));
                foreach (var r in rvs)
                {
                    Console.WriteLine("=================================================");
                    Console.WriteLine(string.Format("BN: {0} | Time: {1} | Creator: {2}", r.TDL_PATIENT_NAME, r.TRACKING_TIME, r.CREATOR));
                    Console.WriteLine("DIỄN BIẾN:\n" + r.CONTENT);
                    Console.WriteLine("CHĂM SÓC:\n" + r.CARE_INSTRUCTION);
                    Console.WriteLine("Y LỆNH:\n" + r.MEDICAL_INSTRUCTION);
                }
            }
        }
    }
}
