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

public class CheckRationSchedule
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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
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

        // 1. Check Binh and Tho
        long[] tIds = new long[] { 7163469, 7163700 };
        foreach (var tId in tIds)
        {
            HisRationScheduleViewFilter f = new HisRationScheduleViewFilter { TREATMENT_ID__EXACT = tId };
            var list = adapter.FetchList<V_HIS_RATION_SCHEDULE>("api/HisRationSchedule/GetView", mos, f, cp);
            Console.WriteLine(string.Format("Treatment ID: {0} | Ration Schedules count: {1}", tId, list != null ? list.Count : 0));
            if (list != null)
            {
                foreach (var s in list)
                {
                    Console.WriteLine(string.Format("  - ScheduleId: {0} | Meal: {1} | Service: {2} ({3}) | From: {4} | To: {5} | LastAssign: {6}",
                        s.ID, s.RATION_TIME_NAME, s.SERVICE_NAME, s.SERVICE_CODE, s.FROM_TIME, s.TO_TIME, s.LAST_ASSIGN_DATE));
                }
            }
        }

        // 2. Check Dept 57 active ration schedules
        Console.WriteLine("\n--- Check Dept 57 active ration schedules ---");
        HisRationScheduleViewFilter df = new HisRationScheduleViewFilter { LAST_DEPARTMENT_ID = 57, IS_PAUSE = false };
        var deptSchedules = adapter.FetchList<V_HIS_RATION_SCHEDULE>("api/HisRationSchedule/GetView", mos, df, cp);
        Console.WriteLine("Dept 57 active ration schedules: " + (deptSchedules != null ? deptSchedules.Count : 0));
        if (deptSchedules != null)
        {
            foreach (var s in deptSchedules.Take(10))
            {
                Console.WriteLine(string.Format("  * BN: {0} (TrID: {1}) | Meal: {2} | Svc: {3} | From: {4} | To: {5}",
                    s.TDL_PATIENT_NAME, s.TREATMENT_ID, s.RATION_TIME_NAME, s.SERVICE_CODE, s.FROM_TIME, s.TO_TIME));
            }
        }
    }
}
