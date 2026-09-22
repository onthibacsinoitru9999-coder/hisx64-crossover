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

        long tid = 7185273; // Tran Van Quy

        var ssf = new HisSereServViewFilter { TREATMENT_ID = tid };
        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssf, param);

        Console.WriteLine("===============================================================================");
        Console.WriteLine("TỔNG SỐ DỊCH VỤ ĐÃ CHỈ ĐỊNH CHO TRẦN VĂN QUÝ: " + (sss != null ? sss.Count : 0));
        Console.WriteLine("===============================================================================");

        if (sss != null)
        {
            var grp = sss.GroupBy(x => x.SERVICE_TYPE_NAME ?? "Khác").OrderBy(g => g.Key);
            foreach (var g in grp)
            {
                Console.WriteLine(string.Format("\n▶ NHÓM: {0} ({1} dịch vụ):", g.Key, g.Count()));
                foreach (var s in g.OrderBy(x => x.TDL_INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("  - [{0}] {1,-50} | Time: {2} | Khoa: {3} | Room: {4} | YL: {5}",
                        s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.TDL_INTRUCTION_TIME, s.REQUEST_DEPARTMENT_NAME, s.REQUEST_ROOM_NAME, s.TDL_SERVICE_REQ_CODE));
                }
            }
        }
    }
}
