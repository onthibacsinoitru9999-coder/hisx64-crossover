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

namespace CheckExecutionStatus
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

            long[] treatIds = new long[] { 7081410, 7073289 }; // 7081410: Luu Van Khoa, 7073289: Le Doan Nguyen

            foreach (var tId in treatIds)
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.ID = tId;
                var t = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param).First();
                Console.WriteLine("==========================================================================");
                Console.WriteLine(string.Format("BN: {0} | Mã BN: {1} | TreatmentId: {2}", t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.ID));
                Console.WriteLine("==========================================================================");

                HisSereServViewFilter ssf = new HisSereServViewFilter();
                ssf.TREATMENT_ID = tId;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

                // ServiceReq de xem trang thai thuc hien
                HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                srf.TREATMENT_ID = tId;
                var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

                foreach (var req in srs.OrderBy(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("\n[Y lệnh lúc {0}] Mã Y lệnh: {1} | Loại: {2} | Khoa Y lệnh: {3} | Phòng TH: {4} | Trạng thái: {5} (STT_ID: {6})",
                        req.INTRUCTION_TIME, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_DEPARTMENT_NAME, req.EXECUTE_ROOM_NAME,
                        req.SERVICE_REQ_STT_NAME, req.SERVICE_REQ_STT_ID));

                    var childServices = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                    foreach (var cs in childServices)
                    {
                        Console.WriteLine(string.Format("   -> Dịch vụ: {0} ({1}) | Loại: {2} | Đã hoàn thành/kết quả: {3}",
                            cs.TDL_SERVICE_NAME, cs.TDL_SERVICE_CODE, cs.SERVICE_TYPE_NAME, cs.IS_ACTIVE == 1 ? "Active" : "Not Active"));
                    }
                }
            }
        }
    }
}
