using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class InspectManhRealReqs
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
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
            string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        Run();
    }

    static void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
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

        long tId = 7139074; // ĐỖ XUÂN MẠNH
        HisServiceReqViewFilter f = new HisServiceReqViewFilter { TREATMENT_ID = tId, SERVICE_REQ_TYPE_ID = 17 };
        var list = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, f, cp);
        Console.WriteLine(string.Format("BN Đỗ Xuân Mạnh - ServiceReq Suất ăn (Type 17): {0}", list != null ? list.Count : 0));
        if (list != null)
        {
            foreach (var r in list.OrderBy(x => x.INTRUCTION_TIME))
            {
                Console.WriteLine(string.Format("  * Req: {0} | Code: {1} | Time: {2} | Room: {3} | Creator: {4} | CreateTime: {5}",
                    r.ID, r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.REQUEST_ROOM_NAME, r.CREATOR, r.CREATE_TIME));
            }
        }

        // Also check all service reqs of all types for Manh
        HisServiceReqViewFilter fAll = new HisServiceReqViewFilter { TREATMENT_ID = tId };
        var listAll = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, fAll, cp);
        Console.WriteLine(string.Format("\nTổng số tất cả ServiceReq của Đỗ Xuân Mạnh: {0}", listAll != null ? listAll.Count : 0));
        var types = listAll.GroupBy(x => x.SERVICE_REQ_TYPE_NAME).Select(g => g.Key + ": " + g.Count()).ToList();
        Console.WriteLine("Các loại: " + string.Join(", ", types));
    }
}
