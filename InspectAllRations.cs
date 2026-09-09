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

public class InspectAllRations
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

        long[] tIds = new long[] { 7163469, 7163700 };
        foreach (var tId in tIds)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            var tr = trList != null && trList.Count > 0 ? trList[0] : null;

            Console.WriteLine("================================================================================");
            Console.WriteLine(string.Format("BN: {0} | TrID: {1} | Mã BN: {2}", tr != null ? tr.TDL_PATIENT_NAME : "", tId, tr != null ? tr.TDL_PATIENT_CODE : ""));

            HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
            var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
            Console.WriteLine(string.Format("Tổng số SereServRation: {0}", rList != null ? rList.Count : 0));
            if (rList != null)
            {
                foreach (var r in rList.OrderBy(x => x.INTRUCTION_TIME).ThenBy(x => x.RATION_TIME_ID))
                {
                    Console.WriteLine(string.Format("  - ID: {0} | ServiceReqId: {1} | Meal: {2} ({3}) | Time: {4} | CreateTime: {5} | Svc: {6}",
                        r.ID, r.SERVICE_REQ_ID, r.RATION_TIME_ID, r.RATION_TIME_NAME, r.INTRUCTION_TIME, r.CREATE_TIME, r.SERVICE_NAME));
                }
            }
        }
    }
}
