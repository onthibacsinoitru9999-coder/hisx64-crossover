using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class Launcher
{
    public class MyAdapter : AdapterBase
    {
        public System.Collections.Generic.List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<System.Collections.Generic.List<T>>(uri, consumer, filter, param);
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
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
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

        long[] reqIds = new long[] { 89493442, 89493443, 89493444, 89458960 };
        foreach (var reqId in reqIds)
        {
            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { ID = reqId };
            var list = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            if (list != null && list.Count > 0)
            {
                var r = list[0];
                Console.WriteLine(string.Format("ReqId: {0} | Code: {1} | TdlPatientName: {2} | SttId: {3} | IsActive: {4} | IsDelete: {5} | InstructionTime: {6} | ReqRoomId: {7} | ReqRoomName: {8} | ExecRoomId: {9} | ExecRoomName: {10}",
                    r.ID, r.SERVICE_REQ_CODE, r.TDL_PATIENT_NAME, r.SERVICE_REQ_STT_ID, r.IS_ACTIVE, r.IS_DELETE, r.INTRUCTION_TIME, r.REQUEST_ROOM_ID, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_ID, r.EXECUTE_ROOM_NAME));
            }
            else
            {
                Console.WriteLine("ReqId " + reqId + " not found!");
            }
        }
    }
}