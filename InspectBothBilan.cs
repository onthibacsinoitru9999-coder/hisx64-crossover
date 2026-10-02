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

            long[] tIds = { 7361797, 7320121 }; // Đặng Minh Đông, Nguyễn Thị Huế

            foreach (var tId in tIds)
            {
                var tf = new HisTreatmentViewFilter { ID = tId };
                var tr = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param).FirstOrDefault();
                if (tr == null) continue;

                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("PATIENT: {0} ({1}t - {2}) | Mã BN: {3} | TrID: {4}", tr.TDL_PATIENT_NAME, 2026 - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)), tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.ID));
                Console.WriteLine("Buồng: " + tr.TDL_PATIENT_ROOM_NAME + " | Chẩn đoán: " + tr.ICD_CODE + " - " + tr.ICD_NAME + " (" + tr.ICD_TEXT + ")");

                // Check ECG
                var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                if (reqs != null)
                {
                    var ecgReq = reqs.Where(r => r.SERVICE_REQ_TYPE_ID == 3 || r.SERVICE_REQ_TYPE_ID == 4 || r.SERVICE_REQ_TYPE_NAME.Contains("Thủ thuật") || r.SERVICE_REQ_TYPE_NAME.Contains("Thăm dò")).ToList();
                    Console.WriteLine("\nCÁC THỦ THUẬT / THĂM DÒ ĐÃ CHỈ ĐỊNH:");
                    foreach (var er in ecgReq)
                    {
                        Console.WriteLine(string.Format("  • [{0}] {1} | Nơi TH: {2} | STT: {3}", er.INTRUCTION_TIME, er.SERVICE_REQ_TYPE_NAME, er.EXECUTE_ROOM_NAME, er.SERVICE_REQ_STT_NAME));
                    }
                }
                Console.WriteLine("\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
