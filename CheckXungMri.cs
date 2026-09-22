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

        // Kiểm tra tất cả CĐHA của Ngô Văn Xưng TID 7173948
        long tid = 7173948;
        var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = tid }, param);
        if (reqs != null)
        {
            foreach (var r in reqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3))
            {
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = r.ID }, param);
                if (sss != null)
                {
                    foreach (var s in sss)
                    {
                        var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = s.ID }, param);
                        if (sses != null)
                        {
                            foreach (var se in sses)
                            {
                                Console.WriteLine(string.Format("YL: {0} | Dịch vụ: {1} | STT: {2}", r.SERVICE_REQ_CODE, s.TDL_SERVICE_NAME, r.SERVICE_REQ_STT_NAME));
                                Console.WriteLine("DESCRIPTION: " + se.DESCRIPTION);
                                Console.WriteLine("CONCLUDE: " + se.CONCLUDE);
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("YL: {0} | Dịch vụ: {1} | STT: {2} | Không có SERE_SERV_EXT", r.SERVICE_REQ_CODE, s.TDL_SERVICE_NAME, r.SERVICE_REQ_STT_NAME));
                        }
                    }
                }
            }
        }
    }
}
