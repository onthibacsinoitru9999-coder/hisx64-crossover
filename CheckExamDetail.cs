using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

namespace CheckExam
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();
            var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var adapter = new BackendAdapter();
            var cp = new CommonParam();

            // 1. Kiểm tra HIS_TREATMENT chi tiết
            var tf = new HisTreatmentViewFilter { ID = 7324914 };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, cp);
            if (trs != null && trs.Count > 0)
            {
                var t = trs[0];
                Console.WriteLine("TREATMENT INFO:");
                Console.WriteLine("  IN_TIME: " + t.IN_TIME);
                Console.WriteLine("  CLINICAL_IN_TIME: " + t.CLINICAL_IN_TIME);
                Console.WriteLine("  ICD_CODE: " + t.ICD_CODE);
                Console.WriteLine("  ICD_NAME: " + t.ICD_NAME);
                Console.WriteLine("  ICD_SUB_CODE: " + t.ICD_SUB_CODE);
                Console.WriteLine("  ICD_TEXT: " + t.ICD_TEXT);
            }

            // 2. Kiểm tra các phiếu khám hoặc dịch vụ đã chỉ định
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = 7324914 };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            if (srs != null)
            {
                Console.WriteLine("\nSERVICE REQS (" + srs.Count + "):");
                foreach (var s in srs)
                {
                    Console.WriteLine(string.Format("  - [{0}] {1} | Type: {2} | Room: {3} | ReqTime: {4}", 
                        s.SERVICE_REQ_CODE, s.SERVICE_REQ_TYPE_NAME, s.SERVICE_REQ_TYPE_ID, s.REQUEST_ROOM_NAME, s.INTRUCTION_TIME));
                }
            }

            // 3. Kiểm tra Tracking hiện tại
            var trkf = new HisTrackingViewFilter { TREATMENT_ID = 7324914 };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, trkf, cp);
            Console.WriteLine("\nTRACKINGS (" + (trks != null ? trks.Count : 0) + "):");
            if (trks != null)
            {
                foreach (var k in trks)
                {
                    Console.WriteLine(string.Format("  - ID: {0} | Time: {1} | Content: {2}", k.ID, k.TRACKING_TIME, k.CONTENT));
                }
            }
        }
    }
}