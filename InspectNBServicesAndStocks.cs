using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        CommonParam param = new CommonParam();
        Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        if (token == null) token = tokenManager.Login(param, "034727", "9981", "2.390.0");
        ApiConsumers.SetConsunmer(token.TokenCode);
        MyAdapter adapter = new MyAdapter();

        // 1. Service Rooms for Service 6217 (BM02426)
        Console.WriteLine("=== SERVICE ROOMS FOR BM02426 (SERVICE ID 6217) ===");
        var srf = new HisServiceRoomViewFilter { SERVICE_ID = 6217, IS_ACTIVE = 1 };
        var sRooms = adapter.FetchList<V_HIS_SERVICE_ROOM>("api/HisServiceRoom/GetView", ApiConsumers.MosConsumer, srf, param);
        if (sRooms != null)
        {
            foreach (var sr in sRooms.Where(x => x.DEPARTMENT_ID == 915 || x.BRANCH_ID == 81 || (x.ROOM_NAME != null && x.ROOM_NAME.Contains("3E"))))
            {
                Console.WriteLine(string.Format("  Room: {0} (ID: {1}) | Dept: {2} (ID: {3}) | Branch: {4}",
                    sr.ROOM_NAME, sr.ROOM_ID, sr.DEPARTMENT_NAME, sr.DEPARTMENT_ID, sr.BRANCH_ID));
            }
        }

        // 2. All Medi Stocks in Dept 915
        Console.WriteLine("\n=== MEDI STOCKS IN DEPT 915 ===");
        var stockFilter = new HisMediStockViewFilter { DEPARTMENT_ID = 915, IS_ACTIVE = 1 };
        var stocks = adapter.FetchList<V_HIS_MEDI_STOCK>("api/HisMediStock/GetView", ApiConsumers.MosConsumer, stockFilter, param);
        if (stocks != null)
        {
            foreach (var s in stocks)
            {
                Console.WriteLine(string.Format("  Stock: {0} (ID: {1}) | Code: {2} | RoomId: {3} | Cabinet: {4}",
                    s.MEDI_STOCK_NAME, s.ID, s.MEDI_STOCK_CODE, s.ROOM_ID, s.IS_CABINET));
            }
        }

        // 3. Check Medicine Types for Actrapid
        Console.WriteLine("\n=== MEDICINE TYPES FOR ACTRAPID ===");
        var mtf = new HisMedicineTypeViewFilter { KEY_WORD = "Actrapid", IS_ACTIVE = 1 };
        var mTypes = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, mtf, param);
        if (mTypes != null)
        {
            foreach (var m in mTypes)
            {
                Console.WriteLine(string.Format("  MedType: {0} (ID: {1} | Code: {2} | Concentra: {3})",
                    m.MEDICINE_TYPE_NAME, m.ID, m.MEDICINE_TYPE_CODE, m.CONCENTRA));
            }
        }

        // 4. Check available inventory for Stock 5142, 5141 or Dept 915 stocks
        if (stocks != null)
        {
            Console.WriteLine("\n=== BEANS IN DEPT 915 STOCKS ===");
            foreach (var st in stocks)
            {
                var bf = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = st.ID, IS_ACTIVE = 1 };
                var bList = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", ApiConsumers.MosConsumer, bf, param);
                Console.WriteLine(string.Format("  Stock {0} ({1}) has {2} beans", st.ID, st.MEDI_STOCK_NAME, bList != null ? bList.Count : 0));
                if (bList != null && bList.Count > 0)
                {
                    foreach (var b in bList.Where(x => x.MEDICINE_TYPE_NAME != null && (x.MEDICINE_TYPE_NAME.IndexOf("Actrapid", StringComparison.OrdinalIgnoreCase) >= 0 || x.MEDICINE_TYPE_NAME.IndexOf("Insulin", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        Console.WriteLine(string.Format("    👉 Actrapid/Insulin bean: MedId {0} | TypeId {1} | Name {2} | Amount {3}",
                            b.MEDICINE_ID, b.MEDICINE_TYPE_ID, b.MEDICINE_TYPE_NAME, b.AMOUNT));
                    }
                }
            }
        }
    }
}
