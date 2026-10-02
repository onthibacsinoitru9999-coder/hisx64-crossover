using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

class CheckActrapidStock810
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, r) =>
        {
            string n = new System.Reflection.AssemblyName(r.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, n);
            if (File.Exists(p1)) return System.Reflection.Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", n);
            if (File.Exists(p2)) return System.Reflection.Assembly.LoadFrom(p2);
            return null;
        };
        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = "";
        if (File.Exists("doctor_standalone.token")) token = File.ReadAllText("doctor_standalone.token").Split('|')[0].Trim();
        else if (File.Exists("doctor_hn.token")) token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();

        var cp = new CommonParam();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new BackendAdapter(cp);

        var filter = new HisMedicineStockFilter
        {
            MEDI_STOCK_ID = 810
        };
        var stocks = adapter.Get<List<HIS_MEDICINE_STOCK>>("api/HisMedicineStock/Get", mosConsumer, filter, cp);
        if (stocks != null)
        {
            Console.WriteLine(string.Format("Tổng số loại thuốc trong kho 810: {0}", stocks.Count));
            var typeIds = stocks.Select(s => s.MEDICINE_TYPE_ID).Distinct().ToList();
            var types = adapter.Get<List<HIS_MEDICINE_TYPE>>("api/HisMedicineType/Get", mosConsumer, new HisMedicineTypeFilter { IDs = typeIds }, cp);
            if (types != null)
            {
                foreach (var t in types.Where(x => x.MEDICINE_TYPE_NAME.ToLower().Contains("actrapid") || x.MEDICINE_TYPE_NAME.ToLower().Contains("insulin")))
                {
                    Console.WriteLine(string.Format("Thuốc: ID={0} | Code={1} | Name={2} | Concentra={3}", t.ID, t.MEDICINE_TYPE_CODE, t.MEDICINE_TYPE_NAME, t.CONCENTRA));
                }
            }
        }
    }
}
