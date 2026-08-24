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

namespace CheckKhoaPending
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

            long treatmentId = 7081410; // Luu Van Khoa

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.ID = treatmentId;
            var t = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param).First();

            Console.WriteLine("==========================================================================");
            Console.WriteLine(string.Format("BỆNH NHÂN: {0} | MÃ BN: {1} | MÃ BA: {2}", t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
            Console.WriteLine("==========================================================================");

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
            srf.TREATMENT_ID = treatmentId;
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

            HisSereServViewFilter ssf = new HisSereServViewFilter();
            ssf.TREATMENT_ID = treatmentId;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

            foreach (var req in srs.OrderBy(x => x.INTRUCTION_TIME))
            {
                string sttDesc = "";
                if (req.SERVICE_REQ_STT_ID == 1) sttDesc = "🔴 CHƯA XỬ LÝ (MỚI CHỈ ĐỊNH)";
                else if (req.SERVICE_REQ_STT_ID == 2) sttDesc = "🟡 ĐANG XỬ LÝ / ĐÃ TIẾP ĐÓN";
                else if (req.SERVICE_REQ_STT_ID == 3) sttDesc = "🟢 HOÀN THÀNH (ĐÃ CÓ KẾT QUẢ)";
                else sttDesc = req.SERVICE_REQ_STT_NAME;

                Console.WriteLine(string.Format("\n[Y lệnh lúc {0}] Mã: {1} | Loại: {2} | Khoa YL: {3} | Phòng TH: {4} | TRẠNG THÁI: {5}",
                    req.INTRUCTION_TIME, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_DEPARTMENT_NAME, req.EXECUTE_ROOM_NAME, sttDesc));

                var child = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                foreach (var c in child)
                {
                    Console.WriteLine(string.Format("    -> Dịch vụ: {0} (Mã: {1}) | Loại: {2}",
                        c.TDL_SERVICE_NAME, c.TDL_SERVICE_CODE, c.SERVICE_TYPE_NAME));
                }
            }
        }
    }
}
