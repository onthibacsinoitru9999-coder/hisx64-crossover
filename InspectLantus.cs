using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        CommonParam param = new CommonParam();
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        ApiConsumers.SetConsunmer(token.TokenCode);
        MyAdapter adapter = new MyAdapter();

        Console.WriteLine("\n--- KIỂM TRA TỒN KHO THUỐC (MEDICINE BEAN) ---");
        HisMedicineBeanViewFilter mbf = new HisMedicineBeanViewFilter();
        mbf.MEDI_STOCK_ID = 810; // Tủ trực Khoa CTCH & Cột sống
        mbf.IS_ACTIVE = 1;
        var beans = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", ApiConsumers.MosConsumer, mbf, param);
        Console.WriteLine("Total medicine beans in Tu truc 810: " + (beans != null ? beans.Count : 0));
        if (beans != null) {
            var grouped = beans.GroupBy(b => b.MEDICINE_TYPE_NAME).Take(15);
            foreach (var g in grouped) {
                decimal total = g.Sum(x => x.AMOUNT);
                var first = g.First();
                Console.WriteLine(string.Format("   - {0} ({1}) | TypeId: {2} | Tồn: {3} {4} | HSD: {5}",
                    first.MEDICINE_TYPE_NAME, first.MEDICINE_TYPE_CODE, first.MEDICINE_TYPE_ID, total, first.SERVICE_UNIT_NAME, first.EXPIRED_DATE));
            }
        }
        return;
    }
}
