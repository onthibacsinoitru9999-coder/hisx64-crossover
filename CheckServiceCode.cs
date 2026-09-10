using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class CheckServiceCode
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
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

    static void Run()
    {
        CommonParam param = new CommonParam();
        HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        string tokenCode = null;
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs))
        {
            string text = sr.ReadToEnd();
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"TokenCode\|([a-f0-9]{64})");
            if (matches.Count > 0) tokenCode = matches[matches.Count - 1].Groups[1].Value;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
        MyAdapter adapter = new MyAdapter();

        HisServiceViewFilter svf = new HisServiceViewFilter { SERVICE_CODE__EXACT = "NB260620.6231" };
        var svcs = adapter.FetchList<V_HIS_SERVICE>("api/HisService/GetView", mosConsumer, svf, param);
        if (svcs != null && svcs.Count > 0)
        {
            Console.WriteLine(string.Format("Service ID: {0} | Code: {1} | Name: {2}", svcs[0].ID, svcs[0].SERVICE_CODE, svcs[0].SERVICE_NAME));
        }
    }
}
