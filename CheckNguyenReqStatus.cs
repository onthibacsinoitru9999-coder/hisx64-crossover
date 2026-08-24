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

namespace CheckNguyenReqStatus
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
            tf.KEY_WORD = "0003975765";
            var t = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param).First();

            Console.WriteLine(string.Format("BN: {0} | Mã BN: {1} | TreatmentId: {2}", t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.ID));

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
            srf.TREATMENT_ID = t.ID;
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

            HisSereServViewFilter ssf = new HisSereServViewFilter();
            ssf.TREATMENT_ID = t.ID;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

            foreach (var req in srs.OrderBy(x => x.INTRUCTION_TIME))
            {
                Console.WriteLine(string.Format("\n[Y lệnh lúc {0}] Mã Y lệnh: {1} | Loại: {2} | Khoa YL: {3} | Phòng TH: {4} | Trạng thái: {5} (STT_ID: {6}) | Bác sĩ: {7}",
                    req.INTRUCTION_TIME, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_DEPARTMENT_NAME, req.EXECUTE_ROOM_NAME,
                    req.SERVICE_REQ_STT_NAME, req.SERVICE_REQ_STT_ID, req.EXECUTE_LOGINNAME));

                var childServices = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                foreach (var cs in childServices)
                {
                    Console.WriteLine(string.Format("   -> Dịch vụ: {0} (Mã: {1}) | Loại: {2} | IS_EXPEND: {3} | HEIN_CARD_NUMBER: {4}",
                        cs.TDL_SERVICE_NAME, cs.TDL_SERVICE_CODE, cs.SERVICE_TYPE_NAME, cs.IS_EXPEND, cs.HEIN_CARD_NUMBER));
                }
            }
        }
    }
}
