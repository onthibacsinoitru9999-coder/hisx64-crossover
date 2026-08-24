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

namespace GetKhoaAllRecent
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

            long tId = 7081410; // Luu Van Khoa

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
            srf.TREATMENT_ID = tId;
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

            HisSereServViewFilter ssf = new HisSereServViewFilter();
            ssf.TREATMENT_ID = tId;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

            Console.WriteLine("==========================================================================");
            Console.WriteLine("DANH SÁCH TOÀN BỘ CĐHA, SIÊU ÂM, TDCN VÀ XÉT NGHIỆM CẬP NHẬT MỚI NHẤT");
            Console.WriteLine("==========================================================================");

            foreach (var req in srs.Where(x => x.SERVICE_REQ_TYPE_ID != 6 && x.SERVICE_REQ_TYPE_ID != 15).OrderBy(x => x.INTRUCTION_TIME))
            {
                string stt = "";
                if (req.SERVICE_REQ_STT_ID == 1) stt = "🔴 CHƯA XỬ LÝ (CHỈ ĐỊNH)";
                else if (req.SERVICE_REQ_STT_ID == 2) stt = "🟡 ĐANG XỬ LÝ / TIẾP ĐÓN";
                else if (req.SERVICE_REQ_STT_ID == 3) stt = "🟢 HOÀN THÀNH (ĐÃ CÓ KẾT QUẢ)";
                else stt = req.SERVICE_REQ_STT_NAME;

                var child = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                foreach (var c in child)
                {
                    Console.WriteLine(string.Format("[{0}] Dịch vụ: {1} ({2}) | Phòng: {3} | TRẠNG THÁI: {4}",
                        req.INTRUCTION_TIME, c.TDL_SERVICE_NAME, c.SERVICE_TYPE_NAME, req.EXECUTE_ROOM_NAME, stt));
                }
            }
        }
    }
}
