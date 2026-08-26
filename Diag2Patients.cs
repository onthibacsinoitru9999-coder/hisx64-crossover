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

class Diag2Patients
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
        }
    }

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
        string token = "";
        using (var fs = new FileStream(@"Logs\LogSystem.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // 1. Check Pham Van Them (7023543)
        Console.WriteLine("=== DIAG PHAM VAN THEM (7023543) ===");
        HisExpMestMedicineViewFilter f1 = new HisExpMestMedicineViewFilter();
        f1.TDL_TREATMENT_ID = 7023543;
        var meds1 = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, f1, cp);
        if (meds1 != null)
        {
            foreach (var m in meds1.Where(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().StartsWith("20260826")))
            {
                Console.WriteLine(string.Format("Med: {0} (ID {1}) | Stock: {2} (ID {3}) | Unit: {4}",
                    m.MEDICINE_TYPE_NAME, m.MEDICINE_TYPE_ID, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.SERVICE_UNIT_NAME));
            }
        }

        // 2. Check Nguyen Trong Te (7068104)
        Console.WriteLine("\n=== DIAG NGUYEN TRONG TE (7068104) ===");
        HisExpMestMedicineViewFilter f2 = new HisExpMestMedicineViewFilter();
        f2.TDL_TREATMENT_ID = 7068104;
        var meds2 = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, f2, cp);
        if (meds2 != null)
        {
            foreach (var m in meds2.Where(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().StartsWith("20260826")))
            {
                Console.WriteLine(string.Format("Med: {0} (ID {1}) | Stock: {2} (ID {3}) | Unit: {4}",
                    m.MEDICINE_TYPE_NAME, m.MEDICINE_TYPE_ID, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.SERVICE_UNIT_NAME));
            }
        }
    }
}
