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
    static void Main(string[] args)
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
                if (parts.Length >= 1 && parts[0].Length == 64)
                    currentToken = parts[0];
            }
            if (string.IsNullOrEmpty(currentToken))
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
                if (File.Exists(logFile))
                {
                    string text = File.ReadAllText(logFile);
                    int idx = text.LastIndexOf("TokenCode|");
                    if (idx >= 0 && text.Length >= idx + 10 + 64)
                        currentToken = text.Substring(idx + 10, 64);
                }
            }

            TokenClient.SetToken(currentToken);

            long[] tIds = new long[] { 7361797, 7320121 }; // Đặng Minh Đông & Nguyễn Thị Huế

            foreach (var tId in tIds)
            {
                var tf = new HisTreatmentViewFilter { ID = tId };
                var trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trs == null || trs.Count == 0) continue;
                var tr = trs[0];

                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("🏥 BỆNH NHÂN: {0} ({1} tuổi - {2})", tr.TDL_PATIENT_NAME, 2026 - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)), tr.TDL_PATIENT_GENDER_NAME));
                Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt điều trị: {2}", tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
                Console.WriteLine(string.Format("Khoa hiện tại: {0} | Buồng: {1} | Giường: {2}", tr.END_DEPARTMENT_NAME, tr.TDL_PATIENT_ROOM_NAME, tr.BED_NAME));
                Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
                Console.WriteLine(string.Format("Thời gian vào viện: {0}", tr.IN_TIME));
                Console.WriteLine("===============================================================================");

                // 1. Service requests & Services
                var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

                Console.WriteLine("\n--- TẤT CẢ CÁC PHIẾU CHỈ ĐỊNH LÂM SÀNG & CẬN LÂM SÀNG ---");
                if (reqs != null)
                {
                    foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("\n📋 Phiếu [{0}] lúc {1} | Loại: {2} (ID: {3}) | BS: {4} | Nơi TH: {5} | STT: {6}",
                            r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.SERVICE_REQ_TYPE_NAME, r.SERVICE_REQ_TYPE_ID, r.REQUEST_USERNAME, r.EXECUTE_ROOM_NAME, r.SERVICE_REQ_STT_NAME));

                        var ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
                        var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                        if (sss != null)
                        {
                            foreach (var s in sss)
                            {
                                Console.WriteLine(string.Format("   • Dịch vụ: {0} (Mã: {1} | SS_ID: {2})", s.TDL_SERVICE_NAME, s.TDL_SERVICE_CODE, s.ID));

                                // If CDHA, fetch ext
                                if (r.SERVICE_REQ_TYPE_ID == 2 || r.SERVICE_REQ_TYPE_ID == 3)
                                {
                                    var extFilter = new HisSereServExtFilter { SERE_SERV_ID = s.ID };
                                    var exts = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, extFilter, param);
                                    if (exts != null && exts.Count > 0)
                                    {
                                        if (!string.IsNullOrEmpty(exts[0].CONCLUDE))
                                            Console.WriteLine("     👉 KẾT LUẬN CĐHA: " + exts[0].CONCLUDE.Replace("\r\n", " ").Trim());
                                        if (!string.IsNullOrEmpty(exts[0].DESCRIPTION))
                                            Console.WriteLine("     👉 MÔ TẢ CĐHA: " + exts[0].DESCRIPTION.Replace("\r\n", " ").Trim());
                                    }
                                }
                            }
                        }
                    }
                }

                // 2. All Lab test results
                Console.WriteLine("\n--- TẤT CẢ KẾT QUẢ XÉT NGHIỆM CHI TIẾT (SERE_SERV_TEIN) ---");
                var teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tId };
                var teins = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);
                if (teins != null && teins.Count > 0)
                {
                    var grp = teins.GroupBy(x => x.TDL_SERVICE_REQ_ID ?? 0).OrderBy(g => g.Key);
                    foreach (var g in grp)
                    {
                        var req = reqs != null ? reqs.FirstOrDefault(r => r.ID == g.Key) : null;
                        string reqName = req != null ? (req.SERVICE_REQ_CODE + " (" + req.SERVICE_REQ_TYPE_NAME + " - " + req.EXECUTE_ROOM_NAME + ")") : ("Req ID " + g.Key);
                        Console.WriteLine("\n📋 Phiếu " + reqName + ":");
                        foreach (var t in g.OrderBy(x => x.TEST_INDEX_NAME))
                        {
                            if (!string.IsNullOrEmpty(t.VALUE))
                            {
                                Console.WriteLine(string.Format("   - {0,-35}: {1} {2} (BT: {3})", 
                                    t.TEST_INDEX_NAME ?? t.TEST_INDEX_CODE, t.VALUE, t.TEST_INDEX_UNIT_NAME, t.TEST_INDEX_RANGE));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Chưa có kết quả xét nghiệm.");
                }

                // 3. Tracking
                var trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trackings = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                Console.WriteLine("\n--- TỜ ĐIỀU TRỊ GẦN NHẤT ---");
                if (trackings != null && trackings.Count > 0)
                {
                    foreach (var tk in trackings.OrderByDescending(x => x.TRACKING_TIME).Take(2))
                    {
                        Console.WriteLine(string.Format("\n[Tờ ĐT lúc {0}] ID: {1} | BS: {2}\n{3}",
                            tk.TRACKING_TIME, tk.ID, tk.CREATOR, tk.CONTENT));
                    }
                }

                Console.WriteLine("\n\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
