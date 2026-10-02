using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
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
        try
        {
            ConfigSystem.Load.Init();
            var mosConsumer = ApiConsumers.MosConsumer;
            var param = new CommonParam();
            var myAdapter = new MyAdapter();

            string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
            string currentToken = null;
            if (File.Exists(cacheFile))
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 1 && parts[0].Length == 64) currentToken = parts[0];
            }
            TokenClient.SetToken(currentToken);

            long treatmentId = 7348631;

            // 1. Service Requests
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = treatmentId };
            var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            Console.WriteLine("=== DANH SÁCH TẤT CẢ CÁC Y LỆNH / CHỈ ĐỊNH ĐÃ TẠO TRÊN HIS (TREATMENT " + treatmentId + ") ===");
            if (reqs != null)
            {
                foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("[{0}] Mã: {1} | Loại: {2} (Type: {3}) | Khoa: {4} | Nơi YC: {5} | Nơi TH: {6} | BS: {7} | STT: {8}",
                        r.INTRUCTION_TIME, r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_NAME, r.SERVICE_REQ_TYPE_ID, r.REQUEST_DEPARTMENT_NAME, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_NAME, r.REQUEST_USERNAME, r.SERVICE_REQ_STT_NAME));
                }
            }

            // 2. Sere Servs
            var ssf = new HisSereServViewFilter { TDL_TREATMENT_ID = treatmentId };
            var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            Console.WriteLine("\n=== CHI TIẾT TẤT CẢ DỊCH VỤ / XÉT NGHIỆM / CĐHA ===");
            if (sss != null)
            {
                foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("[{0}] Mã: {1,-12} | Tên: {2,-50} | Loại: {3,-15} | Nơi TH: {4}",
                        s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
