using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine(baseDir, shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        Run();
    }

    static void Run()
    {
        string logFile = @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
        string token = "";
        using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64)
                    token = line.Substring(idx + 10, 64);
            }
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        // Test delete single redundant service req (e.g. 88163332)
        var cp = new CommonParam();
        var sdo = new HisServiceReqSDO { Id = 88163332 };
        
        // Try api/HisServiceReq/Delete
        bool res1 = mosConsumer.Post<bool>("api/HisServiceReq/Delete", cp, sdo, new object[0]);
        Console.WriteLine(string.Format("HisServiceReq/Delete: {0} (Bug: {1}, Msg: {2})", res1, cp.GetBugCode(), cp.GetMessage()));

        // Try api/HisServiceReq/InPatientPresDelete
        var cp2 = new CommonParam();
        bool res2 = mosConsumer.Post<bool>("api/HisServiceReq/InPatientPresDelete", cp2, sdo, new object[0]);
        Console.WriteLine(string.Format("InPatientPresDelete: {0} (Bug: {1}, Msg: {2})", res2, cp2.GetBugCode(), cp2.GetMessage()));
    }
}
