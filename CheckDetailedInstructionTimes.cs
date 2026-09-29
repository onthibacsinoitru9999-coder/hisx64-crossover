using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class Program
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

        long[] treatmentIds = new long[] { 7309184, 7252303, 7332069 };

        foreach (var tId in treatmentIds)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
            var tr = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp).FirstOrDefault();

            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🧑 BN: {0} ({1}) | TreatmentId: {2}", tr != null ? tr.TDL_PATIENT_NAME : "", tr != null ? tr.TDL_PATIENT_CODE : "", tId));

            HisExpMestMedicineViewFilter mf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId };
            var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, mf, cp);

            if (allMeds != null)
            {
                var groups = allMeds.GroupBy(x => (x.TDL_INTRUCTION_TIME ?? x.EXP_TIME ?? 0).ToString().Substring(0, 8)).OrderBy(g => g.Key);
                foreach (var g in groups)
                {
                    Console.WriteLine(string.Format("  📅 Instruction Date: {0} ({1} khoản thuốc)", g.Key, g.Count()));
                    foreach (var m in g)
                    {
                        Console.WriteLine(string.Format("     • {0} | SL: {1} | Stock: {2} ({3}) | InsTime: {4} | CreateTime: {5}",
                            m.MEDICINE_TYPE_NAME, m.AMOUNT, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.TDL_INTRUCTION_TIME, m.CREATE_TIME));
                    }
                }
            }
        }
    }
}
