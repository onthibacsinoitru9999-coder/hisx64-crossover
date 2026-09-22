using System;
using System.IO;
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
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = e.Name.Split(',')[0];
            string cur = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = new string[]
            {
                Path.Combine(cur, "ReferencedAssemblies", shortName + ".dll"),
                Path.Combine(cur, "Plugins", "Module", shortName + ".dll"),
                Path.Combine(cur, shortName + ".dll")
            };
            foreach (var p in paths) if (File.Exists(p)) return Assembly.LoadFrom(p);
            return null;
        };

        Run();
    }

    static string ReadLiveToken()
    {
        string cur = AppDomain.CurrentDomain.BaseDirectory;
        string c = Path.Combine(cur, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(c, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            long len = fs.Length;
            int bufSize = (int)Math.Min(262144L, len);
            fs.Seek(len - bufSize, SeekOrigin.Begin);
            byte[] buf = new byte[bufSize];
            int read = fs.Read(buf, 0, bufSize);
            string chunk = Encoding.UTF8.GetString(buf, 0, read);
            int idx = chunk.LastIndexOf("TokenCode|");
            if (idx >= 0 && chunk.Length >= idx + 10 + 64)
            {
                return chunk.Substring(idx + 10, 64);
            }
        }
        return null;
    }

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        var stList = adapter.FetchList<HIS_SERVICE_TYPE>("api/HisServiceType/Get", mosConsumer, new HisServiceTypeFilter(), param);
        if (stList != null)
        {
            foreach (var st in stList)
            {
                Console.WriteLine("{0} - {1}: {2}", st.ID, st.SERVICE_TYPE_CODE, st.SERVICE_TYPE_NAME);
            }
        }

        // Also check what PTTT tables exist in MOS:
        // HisSereServPttt
        Console.WriteLine("\nTesting HisSereServPttt query...");
        var ssPtttFilter = new HisSereServPtttViewFilter();
        param = new CommonParam();
        param.Limit = 5;
        var ptttList = adapter.FetchList<V_HIS_SERE_SERV_PTTT>("api/HisSereServPttt/GetView", mosConsumer, ssPtttFilter, param);
        Console.WriteLine("HisSereServPttt count: " + (ptttList != null ? ptttList.Count : -1));
        if (ptttList != null && ptttList.Count > 0)
        {
            foreach (var p in ptttList)
            {
                Console.WriteLine("  PTTT: {0} | PtttGroup: {1}", p.PTTT_METHOD_NAME, p.PTTT_GROUP_NAME);
            }
        }
    }
}
