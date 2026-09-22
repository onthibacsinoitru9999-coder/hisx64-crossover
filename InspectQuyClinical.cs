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

        // 1. Check Trackings
        var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, new HisTrackingViewFilter { TREATMENT_ID = tid }, param);
        Console.WriteLine("Trackings (" + (trks != null ? trks.Count : 0) + "):");
        if (trks != null)
        {
            foreach (var tk in trks.OrderByDescending(x => x.TRACKING_TIME))
            {
                Console.WriteLine(string.Format("  * Time: {0} | BS: {1} ({2})", tk.TRACKING_TIME, tk.CREATOR, tk.ROOM_NAME));
                Console.WriteLine("    Content: " + (tk.CONTENT != null ? tk.CONTENT.Replace("\r\n", " | ").Replace("\n", " | ") : ""));
            }
        }

        // 2. Check Debate
        var debFilter = new HisDebateFilter { TREATMENT_ID = tid };
        var debs = adapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mos, debFilter, param);
        Console.WriteLine("\nDebates (" + (debs != null ? debs.Count : 0) + "):");
        if (debs != null)
        {
            foreach (var d in debs)
            {
                Console.WriteLine(string.Format("  - Debate ID: {0} | Time: {1} | Conclusion: {2}", d.ID, d.DEBATE_TIME, d.CONCLUSION));
                Console.WriteLine("    Content: " + d.CONTENT_DEBATE);
            }
        }
    }
}
