using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace CheckKhoaDetails
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Load.Init();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            CommonParam param = new CommonParam();
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.KEY_WORD = "0001624719";
            var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            var t = treats.OrderByDescending(x => x.IN_TIME).First();

            Console.WriteLine("BN: " + t.TDL_PATIENT_NAME + " | Mã BA: " + t.TREATMENT_CODE);

            HisSereServViewFilter ssFilter = new HisSereServViewFilter();
            ssFilter.TREATMENT_ID = t.ID;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);

            Console.WriteLine("\n--- TOÀN BỘ DỊCH VỤ CỦA BN LƯU VĂN KHOA ---");
            foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
            {
                Console.WriteLine(string.Format("[{0}] {1} (Loại: {2}, Mã: {3}) | Phòng: {4}",
                    s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.TDL_SERVICE_CODE, s.EXECUTE_ROOM_NAME));
            }
        }
    }
}
