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

        // 1. Get ServiceReq 000090132804
        var srf = new HisServiceReqViewFilter { SERVICE_REQ_CODE__EXACT = "000090132804" };
        var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, param);
        if (srs != null && srs.Count > 0)
        {
            var sr = srs[0];
            Console.WriteLine(string.Format("Phiếu Hội chẩn: {0} | Req: {1} | Exec: {2} | Descr: {3}",
                sr.SERVICE_REQ_CODE, sr.REQUEST_ROOM_NAME, sr.EXECUTE_ROOM_NAME, sr.DESCRIPTION));
        }

        // 2. Get SereServTein or SereServExt for MRI Cervical spine (BM00479.260119)
        var ssf = new HisSereServViewFilter { TREATMENT_ID = 7185273 };
        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssf, param);
        if (sss != null)
        {
            var mri = sss.FirstOrDefault(x => x.TDL_SERVICE_CODE == "BM00479.260119");
            if (mri != null)
            {
                Console.WriteLine(string.Format("\nMRI Cột sống cổ: ID {0} | Code: {1} | ReqCode: {2}", mri.ID, mri.TDL_SERVICE_CODE, mri.TDL_SERVICE_REQ_CODE));
                // Get SereServTein or SereServExt or check SereServFile
                var teinf = new HisSereServTeinViewFilter { SERE_SERV_ID = mri.ID };
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mos, teinf, param);
                if (teins != null && teins.Count > 0)
                {
                    foreach (var t in teins) Console.WriteLine("  Tein: " + t.TEST_INDEX_NAME + " = " + t.VALUE);
                }

                // Check SereServExt
                var extf = new HisSereServExtViewFilter { SERE_SERV_ID = mri.ID };
                var exts = adapter.FetchList<V_HIS_SERE_SERV_EXT>("api/HisSereServExt/GetView", mos, extf, param);
                if (exts != null && exts.Count > 0)
                {
                    foreach (var e in exts) Console.WriteLine("  Ext: " + e.DESCRIPTION + " | Concl: " + e.CONCLUDE);
                }
            }
        }
    }
}
