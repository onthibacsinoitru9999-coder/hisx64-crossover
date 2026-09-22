using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
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
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string name = new AssemblyName(a.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", name);
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            return null;
        };
        RealMain();
    }

    static void RealMain()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token", Encoding.UTF8).Split('|')[0];
        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        long[] tIds = new long[] { 7163469, 7163700, 7163879, 7200072, 7180230, 7160889, 7172565 };
        string[] names = new string[] { "VU DUC BINH", "NGUYEN VAN THO", "VI TRUNG HIEU", "DINH VAN SUA", "LE QUY DANG", "CAM VAN TICH", "TRUONG VAN BINH" };

        for (int i = 0; i < tIds.Length; i++)
        {
            long tid = tIds[i];
            Console.WriteLine(string.Format("\n=== {0}. {1} (TrID: {2}) ===", i + 1, names[i], tid));

            // DHST
            var df = new HisDhstViewFilter { TREATMENT_ID = tid, ORDER_FIELD = "EXECUTE_TIME", ORDER_DIRECTION = "DESC" };
            var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mos, df, param);
            if (dhsts != null && dhsts.Count > 0)
            {
                var d = dhsts[0];
                Console.WriteLine(string.Format("  DHST ({0}): Mạch: {1} l/p | HA: {2}/{3} mmHg | T: {4}°C | SpO2: {5}% | NT: {6} l/p | CN: {7} kg",
                    d.EXECUTE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.BREATH_RATE, d.WEIGHT));
            }

            // CDHA & PTTT
            var ssf = new HisSereServViewFilter { TREATMENT_ID = tid };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssf, param);
            if (sss != null)
            {
                var cdha = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8).ToList();
                Console.WriteLine("  CĐHA / Phẫu thuật / Thủ thuật: " + cdha.Count);
                foreach (var s in cdha)
                {
                    Console.WriteLine(string.Format("    - [{0}] {1} ({2})", s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME));
                }
            }
        }
    }
}
