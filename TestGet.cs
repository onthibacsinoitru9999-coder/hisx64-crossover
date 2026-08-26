using System;
using System.IO;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using System.Collections.Generic;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class TestGet
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        Run();
    }

    static void Run()
    {
        string logFile = @"Logs\LogSystem.txt";
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

        Console.WriteLine("Token: " + token);
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();
        HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
        bf.DEPARTMENT_ID = 57;

        MyAdapter adapter = new MyAdapter();
        var res = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, cp);
        Console.WriteLine(string.Format("GetView Result: {0} items | Bug: {1} | Msg: {2}",
            res != null ? res.Count.ToString() : "NULL",
            cp.GetBugCode(), cp.GetMessage()));

        if (res != null)
        {
            foreach (var r in res)
            {
                Console.WriteLine("Room: " + r.BED_ROOM_NAME + " (ID: " + r.ID + ", RoomId: " + r.ROOM_ID + ")");
            }
        }
    }
}
