using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class ReadPatientTrackings
{
    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies");
        string asmName = new AssemblyName(args.Name).Name + ".dll";
        string path = Path.Combine(folder, asmName);
        if (File.Exists(path)) return Assembly.LoadFrom(path);
        return null;
    }

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        Run();
    }

    static void Run()
    {
        string token = "";
        string logPath = @"Logs\LogSystem.txt";
        if (!File.Exists(logPath)) logPath = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\Logs\LogSystem.txt";
        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            byte[] buf = new byte[Math.Min(65536L, fs.Length)];
            fs.Seek(fs.Length - buf.Length, SeekOrigin.Begin);
            fs.Read(buf, 0, buf.Length);
            string s = Encoding.UTF8.GetString(buf);
            int idx = s.LastIndexOf("TokenCode|");
            if (idx >= 0) token = s.Substring(idx + 10, 64);
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var param = new CommonParam();
        var adapter = new CustomAdapter();

        long treatId = 7319928;
        var tf = new HisTrackingViewFilter { TREATMENT_ID = treatId };
        var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, tf, param);
        if (trks == null || trks.Count == 0) { Console.WriteLine("No trackings found."); return; }

        Console.WriteLine("Found " + trks.Count + " trackings:");
        foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
        {
            Console.WriteLine(string.Format("ID: {0} | Time: {1} | Dept: {2} | Room: {3} | ICD: {4}", t.ID, t.TRACKING_TIME, t.DEPARTMENT_ID, t.ROOM_ID, t.ICD_NAME));
            Console.WriteLine("CONTENT: " + (t.CONTENT ?? "").Replace("\r\n", " // "));
            Console.WriteLine("CARE: " + t.CARE_INSTRUCTION);
            Console.WriteLine("MED: " + t.MEDICAL_INSTRUCTION);
            Console.WriteLine(new string('-', 60));
        }
    }
}

public class CustomAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}
