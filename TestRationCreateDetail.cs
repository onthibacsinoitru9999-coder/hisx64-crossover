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
using MOS.SDO;

public class TestRationCreateDetail
{
    public class MyAdapter : AdapterBase
    {
        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
        }
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

        // 1. Check patient treatment info
        long tId = 7163469; // Binh
        HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
        var tr = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp)[0];

        // 2. Build SDO for date 20260909050000
        long instructionTime = 20260909050000;
        var services = new List<RationServiceSDO>
        {
            new RationServiceSDO { ServiceId = 30073, PatientTypeId = 42, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } },
            new RationServiceSDO { ServiceId = 30153, PatientTypeId = 42, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } },
            new RationServiceSDO { ServiceId = 30154, PatientTypeId = 42, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } }
        };

        var sdo = new HisRationServiceReqSDO
        {
            TreatmentIds = new List<long> { tId },
            InstructionTimes = new List<long> { instructionTime },
            RequestRoomId = 5248,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            HalfInFirstDay = false,
            IsForAutoCreateRation = false,
            IsForHomie = false,
            RationServices = services
        };

        CommonParam callParam = new CommonParam();
        var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, callParam);

        Console.WriteLine("HasException: " + callParam.HasException);
        Console.WriteLine("BugCode: " + callParam.GetBugCode());
        Console.WriteLine("Message: " + callParam.GetMessage());
        if (callParam.Messages != null)
        {
            foreach (var m in callParam.Messages) Console.WriteLine("Msg: " + m);
        }
        Console.WriteLine("Result object: " + (res != null ? res.ToString() : "NULL"));
        if (res != null)
        {
            foreach (var prop in res.GetType().GetProperties())
            {
                Console.WriteLine(string.Format("  {0} = {1}", prop.Name, prop.GetValue(res, null)));
            }
        }
    }
}
