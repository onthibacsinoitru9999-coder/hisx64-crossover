using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class Launcher
{
    public class MyAdapter : AdapterBase
    {
        public System.Collections.Generic.List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<System.Collections.Generic.List<T>>(uri, consumer, filter, param);
        }
    }

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
        Console.OutputEncoding = Encoding.UTF8;
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        string token = "";
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = 89493442 };
        var ss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssf, cp);
        Console.WriteLine("SereServ for Req 89493442 (Suat an): " + (ss != null ? ss.Count : 0));
        if (ss != null)
        {
            foreach (var s in ss)
            {
                Console.WriteLine(string.Format("ID: {0} | Code: {1} | Name: {2} | SvcTypeId: {3} | IsDelete: {4} | Price: {5}",
                    s.ID, s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.TDL_SERVICE_TYPE_ID, s.IS_DELETE, s.PRICE));
            }
        }
        
        // Also check HisSereServRation for Req 89493442
        HisSereServRationViewFilter rf = new HisSereServRationViewFilter { SERVICE_REQ_ID = 89493442 };
        var rr = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
        Console.WriteLine("SereServRation for Req 89493442: " + (rr != null ? rr.Count : 0));
        if (rr != null)
        {
            foreach (var r in rr)
            {
                Console.WriteLine(string.Format("Ration ID: {0} | ServiceName: {1} | SereServId: {2} | Time: {3} | IsDelete: {4}",
                    r.ID, r.SERVICE_NAME, r.SERE_SERV_ID, r.INTRUCTION_TIME, r.IS_DELETE));
            }
        }
    }
}