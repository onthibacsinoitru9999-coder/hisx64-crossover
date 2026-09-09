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

public class InspectReqFields
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

        // Check Req 89458893 (vmc) and Req 89625624 (our API)
        long[] reqIds = new long[] { 89458893, 89625624 };
        foreach (var id in reqIds)
        {
            HisServiceReqViewFilter f = new HisServiceReqViewFilter { ID = id };
            var list = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, f, cp);
            if (list != null && list.Count > 0)
            {
                var r = list[0];
                Console.WriteLine("================================================================================");
                Console.WriteLine(string.Format("REQ ID: {0} | Code: {1} | Creator: {2}", r.ID, r.SERVICE_REQ_CODE, r.CREATOR));
                Console.WriteLine(string.Format("  InstructionTime: {0} | CreateTime: {1}", r.INTRUCTION_TIME, r.CREATE_TIME));
                Console.WriteLine(string.Format("  ReqRoomId: {0} ({1}) | ExeRoomId: {2} ({3})", r.REQUEST_ROOM_ID, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_ID, r.EXECUTE_ROOM_NAME));
                Console.WriteLine(string.Format("  ReqDeptId: {0} ({1}) | ExeDeptId: {2} ({3})", r.REQUEST_DEPARTMENT_ID, r.REQUEST_DEPARTMENT_NAME, r.EXECUTE_DEPARTMENT_ID, r.EXECUTE_DEPARTMENT_NAME));
                Console.WriteLine(string.Format("  TrackingId: {0} | TreatmentId: {1}", r.TRACKING_ID, r.TREATMENT_ID));
                Console.WriteLine(string.Format("  ServiceReqTypeId: {0} ({1})", r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_TYPE_NAME));
                Console.WriteLine(string.Format("  IsDelete: {0} | IsActive: {1} | StatusId: {2}", r.IS_DELETE, r.IS_ACTIVE, r.SERVICE_REQ_STT_ID));
            }
        }
    }
}
