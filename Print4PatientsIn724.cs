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

namespace Print4PatientsIn724
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

            HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
            bf.DEPARTMENT_ID = 57;
            var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
            var room724 = bList.FirstOrDefault(x => x.BED_ROOM_NAME.Contains("724"));

            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
            tbrf.BED_ROOM_ID = room724.ID;
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

            foreach (var p in inPatients)
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.ID = p.TREATMENT_ID;
                var t = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param).First();

                Console.WriteLine("==========================================================================");
                Console.WriteLine(string.Format("GIƯỜNG: {0} | HỌ TÊN: {1} | MÃ BN: {2} | MÃ BA: {3} | TREATMENT_ID: {4}",
                    p.BED_NAME, t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.ID));
                Console.WriteLine("CHẨN ĐOÁN: " + t.ICD_NAME + " - " + t.ICD_TEXT);

                HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                srf.TREATMENT_ID = p.TREATMENT_ID;
                var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

                HisSereServViewFilter ssf = new HisSereServViewFilter();
                ssf.TREATMENT_ID = p.TREATMENT_ID;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);

                var clsReqs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3 || x.SERVICE_REQ_TYPE_ID == 4 || x.SERVICE_REQ_TYPE_ID == 8 || x.SERVICE_REQ_TYPE_ID == 9 || x.SERVICE_REQ_TYPE_ID == 11).OrderBy(x => x.INTRUCTION_TIME).ToList();
                Console.WriteLine(string.Format("TỔNG SỐ Y LỆNH CLS: {0}", clsReqs.Count));
                foreach (var req in clsReqs)
                {
                    string stt = "";
                    if (req.SERVICE_REQ_STT_ID == 1) stt = "🔴 CHƯA XỬ LÝ (CHỈ ĐỊNH)";
                    else if (req.SERVICE_REQ_STT_ID == 2) stt = "🟡 ĐANG XỬ LÝ / TIẾP ĐÓN";
                    else if (req.SERVICE_REQ_STT_ID == 3) stt = "🟢 HOÀN THÀNH (ĐÃ CÓ KẾT QUẢ)";
                    else stt = req.SERVICE_REQ_STT_NAME;

                    var child = sss.Where(x => x.SERVICE_REQ_ID == req.ID).ToList();
                    foreach (var c in child)
                    {
                        Console.WriteLine(string.Format("   [{0}] {1} (Loại: {2}) | Phòng: {3} | TRẠNG THÁI: {4}",
                            req.INTRUCTION_TIME, c.TDL_SERVICE_NAME, c.SERVICE_TYPE_NAME, req.EXECUTE_ROOM_NAME, stt));
                    }
                }
            }
        }
    }
}
