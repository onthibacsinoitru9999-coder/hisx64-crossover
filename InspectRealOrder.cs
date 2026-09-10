using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
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

class InspectRealOrder
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
        Load.Init();
        string tokenCode = null;
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs))
        {
            string text = sr.ReadToEnd();
            var matches = Regex.Matches(text, @"TokenCode\|([a-f0-9]{64})");
            if (matches.Count > 0) tokenCode = matches[matches.Count - 1].Groups[1].Value;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
        MyAdapter adapter = new MyAdapter();

        // Query Order 90038241
        HisServiceReqViewFilter srf = new HisServiceReqViewFilter { ID = 90038241L };
        var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (reqs != null && reqs.Count > 0)
        {
            var r = reqs[0];
            Console.WriteLine("=== SERVICE REQ 90038241 ===");
            Console.WriteLine(string.Format("ID: {0} | Code: {1} | Type: {2} | Time: {3}", r.ID, r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_ID, r.INTRUCTION_TIME));
            Console.WriteLine(string.Format("RequestRoomId: {0} ({1}) | ExecuteRoomId: {2} ({3})", r.REQUEST_ROOM_ID, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_ID, r.EXECUTE_ROOM_NAME));
            Console.WriteLine(string.Format("TrackingId: {0} | TreatmentId: {1}", r.TRACKING_ID, r.TREATMENT_ID));

            // Query SereServ
            HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = 90038241L };
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null)
            {
                foreach (var ss in sss)
                {
                    Console.WriteLine(string.Format("  SS ID: {0} | Service: {1} ({2}) | RoomId: {3} | PatientType: {4}",
                        ss.ID, ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_CODE, ss.TDL_EXECUTE_ROOM_ID, ss.PATIENT_TYPE_ID));
                }
            }
        }
    }
}
