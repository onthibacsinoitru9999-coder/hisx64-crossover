using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
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

        // 1. Get SereServ for Nguyet (TreatmentId 7082063) on 20260824
        var cpGet = new CommonParam();
        var ss = mosConsumer.Get<List<HIS_SERE_SERV>>("api/HisSereServ/Get", cpGet, new { TDL_TREATMENT_ID = 7082063, IS_INCLUDE_DELETED = false }, new object[0]);
        var req88163332Meds = ss.Where(x => x.SERVICE_REQ_ID == 88163332).ToList();
        Console.WriteLine(string.Format("SereServs in req 88163332: {0}", req88163332Meds.Count));

        foreach (var s in req88163332Meds)
        {
            var cpDel = new CommonParam();
            bool res = mosConsumer.Post<bool>("api/HisSereServ/ExamDelete", cpDel, new HIS_SERE_SERV { ID = s.ID }, new object[0]);
            Console.WriteLine(string.Format("ExamDelete SereServ {0} ({1}): {2} (Bug: {3}, Msg: {4})", s.ID, s.TDL_SERVICE_NAME, res, cpDel.GetBugCode(), cpDel.GetMessage()));
        }

        // 2. Delete ServiceReq 88163332
        var cpReq = new CommonParam();
        bool resReq = mosConsumer.Post<bool>("api/HisServiceReq/Delete", cpReq, new MOS.SDO.HisServiceReqSDO { Id = 88163332, RequestRoomId = 5252 }, new object[0]);
        Console.WriteLine(string.Format("HisServiceReq/Delete 88163332: {0} (Bug: {1}, Msg: {2})", resReq, cpReq.GetBugCode(), cpReq.GetMessage()));
    }
}
