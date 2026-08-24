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

namespace CheckExactKhoa
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

            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
            tbrf.BED_ROOM_ID = 780; // Phong 724
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

            var khoa = inPatients.FirstOrDefault(x => x.TDL_PATIENT_NAME.Contains("KHOA"));
            if (khoa == null)
            {
                Console.WriteLine("Khong tim thay BN Khoa trong phong 724");
                return;
            }

            Console.WriteLine(string.Format("BN: {0} | Giuong: {1} | Ma BA: {2} | TreatmentId: {3}",
                khoa.TDL_PATIENT_NAME, khoa.BED_NAME, khoa.TREATMENT_CODE, khoa.TREATMENT_ID));

            long tId = khoa.TREATMENT_ID;

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
            srf.TREATMENT_ID = tId;
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

            HisSereServViewFilter ssf = new HisSereServViewFilter();
            ssf.TREATMENT_ID = tId;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

            Console.WriteLine("\n--- TOAN BO SERVICE REQ CUA BN LUU VAN KHOA ---");
            foreach (var req in srs.OrderBy(x => x.INTRUCTION_TIME))
            {
                string stt = "";
                if (req.SERVICE_REQ_STT_ID == 1) stt = "🔴 CHƯA XỬ LÝ (MỚI CHỈ ĐỊNH)";
                else if (req.SERVICE_REQ_STT_ID == 2) stt = "🟡 ĐANG XỬ LÝ / ĐÃ TIẾP ĐÓN";
                else if (req.SERVICE_REQ_STT_ID == 3) stt = "🟢 HOÀN THÀNH (ĐÃ CÓ KẾT QUẢ)";
                else stt = req.SERVICE_REQ_STT_NAME;

                Console.WriteLine(string.Format("[{0}] Mã YL: {1} | Loại: {2} | Khoa YL: {3} | Phòng TH: {4} | TT: {5}",
                    req.INTRUCTION_TIME, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_DEPARTMENT_NAME, req.EXECUTE_ROOM_NAME, stt));

                var child = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                foreach (var c in child)
                {
                    Console.WriteLine(string.Format("    -> Dịch vụ: {0} ({1}) | Loại: {2}",
                        c.TDL_SERVICE_NAME, c.TDL_SERVICE_CODE, c.SERVICE_TYPE_NAME));
                }
            }
        }
    }
}
