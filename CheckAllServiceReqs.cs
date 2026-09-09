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
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
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

        long[] tIds = new long[] { 7163469, 7163700 };
        foreach (var tId in tIds)
        {
            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var list = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            Console.WriteLine("================================================================================");
            Console.WriteLine("TreatmentId: " + tId);
            if (list != null)
            {
                Console.WriteLine("Total ServiceReqs: " + list.Count);
                foreach (var r in list.OrderByDescending(x => x.INTRUCTION_TIME))
                {
                    if (r.SERVICE_REQ_TYPE_ID == 12 || r.SERVICE_REQ_TYPE_ID == 17 || (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("suất ăn")) || (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn")))
                    {
                        Console.WriteLine(string.Format("  -> [SUẤT ĂN] Code: {0} | Name: {1} | TypeId: {2} | Time: {3} | Room: {4}",
                            r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_NAME, r.SERVICE_REQ_TYPE_ID, r.INTRUCTION_TIME, r.REQUEST_ROOM_NAME));
                    }
                }
            }
        }
    }
}