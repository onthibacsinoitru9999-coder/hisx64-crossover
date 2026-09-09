using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace FastFetchDebatePatients
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
        public T FetchSingle<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<T>(uri, consumer, filter, param);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
            };
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string logPath = @"e:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
            string token = null;
            if (File.Exists(logPath))
            {
                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string text = sr.ReadToEnd();
                    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        if (lines[i].Contains("TokenCode|"))
                        {
                            token = lines[i].Substring(lines[i].IndexOf("TokenCode|") + 10, 64);
                            break;
                        }
                    }
                }
            }

            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var adapter = new MyAdapter();

            long roomIdNB = 18686; // Phòng hội chẩn CTCH & PTCS (CSNB)

            Console.WriteLine("===============================================================================");
            Console.WriteLine("   🏥 DANH SÁCH YÊU CẦU TẠI PHÒNG HỘI CHẨN CTCH VÀ CS (CSNB - ROOM 18686)");
            Console.WriteLine("===============================================================================");

            HisServiceReqViewFilter srfNB = new HisServiceReqViewFilter();
            srfNB.EXECUTE_ROOM_ID = roomIdNB;
            var reqsNB = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfNB, param);
            
            if (reqsNB == null || reqsNB.Count == 0)
            {
                Console.WriteLine("Không có yêu cầu nào.");
                return;
            }

            Console.WriteLine(string.Format("Tổng số yêu cầu trong lịch sử phòng: {0}", reqsNB.Count));

            // Sắp xếp giảm dần theo thời gian chỉ định
            var sortedReqs = reqsNB.OrderByDescending(x => x.INTRUCTION_TIME).ToList();

            // In 30 yêu cầu gần nhất
            Console.WriteLine("\n--- 30 YÊU CẦU GẦN ĐÂY NHẤT TẠI PHÒNG HỘI CHẨN CTCH & CS (CSNB) ---");
            for (int i = 0; i < Math.Min(30, sortedReqs.Count); i++)
            {
                var r = sortedReqs[i];
                Console.WriteLine(string.Format("[{0,2}] Time: {1} | BN: {2,-22} | Code: {3} | Treat: {4} | STT: {5} | Doctor: {6} | Dept: {7} | Room: {8}",
                    i + 1, r.INTRUCTION_TIME, r.TDL_PATIENT_NAME, r.TDL_PATIENT_CODE, r.TREATMENT_CODE, r.SERVICE_REQ_STT_NAME ?? r.SERVICE_REQ_STT_ID.ToString(),
                    r.REQUEST_LOGINNAME, r.REQUEST_DEPARTMENT_NAME, r.REQUEST_ROOM_NAME));
            }

            // Lấy danh sách bệnh nhân duy nhất trong 20 yêu cầu gần nhất hoặc có thời gian >= 20260801000000
            var recentReqs = sortedReqs.Where(x => x.INTRUCTION_TIME >= 20260801000000).ToList();
            if (recentReqs.Count == 0) recentReqs = sortedReqs.Take(10).ToList();

            var uniqueTreatments = recentReqs.Select(x => x.TREATMENT_ID).Distinct().ToList();
            Console.WriteLine(string.Format("\n=== CHI TIẾT {0} BỆNH NHÂN GẦN ĐÂY CÓ YÊU CẦU HỘI CHẨN ===", uniqueTreatments.Count));

            foreach (var tId in uniqueTreatments)
            {
                var bReqs = recentReqs.Where(x => x.TREATMENT_ID == tId).ToList();
                var rep = bReqs.First();

                // Lấy thông tin điều trị
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trtList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                V_HIS_TREATMENT trt = (trtList != null && trtList.Count > 0) ? trtList.First() : null;

                Console.WriteLine("\n===============================================================================");
                Console.WriteLine(string.Format("👤 BỆNH NHÂN: {0} ({1}) | Năm sinh: {2} | Mã BN: {3} | Mã ĐT: {4}",
                    rep.TDL_PATIENT_NAME, rep.TDL_PATIENT_GENDER_NAME, rep.TDL_PATIENT_DOB, rep.TDL_PATIENT_CODE, rep.TREATMENT_CODE));
                Console.WriteLine(string.Format("   🏠 Địa chỉ: {0}", rep.TDL_PATIENT_ADDRESS));
                if (trt != null)
                {
                    Console.WriteLine(string.Format("   🏥 Vào viện: {0} | Ra viện: {1} | Trạng thái: {2} | Khoa: {3}",
                        trt.IN_TIME, trt.OUT_TIME, trt.IS_PAUSE == 1 ? "🔴 Đã ra viện / Tạm khóa" : "🟢 Đang điều trị nội trú", trt.END_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("   🩺 Chẩn đoán chính: {0} [{1}]", trt.ICD_NAME, trt.ICD_CODE));
                    if (!string.IsNullOrEmpty(trt.ICD_TEXT) || !string.IsNullOrEmpty(trt.ICD_SUB_CODE))
                    {
                        Console.WriteLine(string.Format("   🩺 Bệnh kèm theo/Mô tả: {0} [{1}]", trt.ICD_TEXT, trt.ICD_SUB_CODE));
                    }
                }

                // Chi tiết các phiếu yêu cầu hội chẩn của BN này
                foreach (var req in bReqs)
                {
                    Console.WriteLine(string.Format("   📋 Phiếu YC #{0} | Giờ: {1} | Trạng thái: {2} | BS gửi: {3} ({4} - {5})",
                        req.SERVICE_REQ_CODE, req.INTRUCTION_TIME, req.SERVICE_REQ_STT_NAME ?? req.SERVICE_REQ_STT_ID.ToString(),
                        req.REQUEST_USERNAME, req.REQUEST_DEPARTMENT_NAME, req.REQUEST_ROOM_NAME));

                    // Dịch vụ và ý kiến chuyên khoa
                    HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var ss in sss)
                        {
                            Console.WriteLine(string.Format("      • Dịch vụ: {0} (Mã: {1})", ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_CODE));
                            var ssef = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                            var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                            if (sses != null)
                            {
                                foreach (var se in sses)
                                {
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION)) Console.WriteLine("        📝 Ý KIẾN HỘI CHẨN:\n" + se.DESCRIPTION);
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".") Console.WriteLine("        📌 KẾT LUẬN: " + se.CONCLUDE);
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE)) Console.WriteLine("        💡 LỜI DẶN: " + se.INSTRUCTION_NOTE);
                                }
                            }
                        }
                    }
                }

                // Lấy tất cả CLS (CĐHA, Xét nghiệm) của bệnh nhân
                HisServiceReqViewFilter allSrf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var allPatientReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, allSrf, param);
                if (allPatientReqs != null)
                {
                    var clsReqs = allPatientReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3 || x.SERVICE_REQ_TYPE_ID == 4 || x.SERVICE_REQ_TYPE_ID == 5 || x.SERVICE_REQ_TYPE_ID == 9).OrderByDescending(x => x.INTRUCTION_TIME).Take(10).ToList();
                    if (clsReqs.Count > 0)
                    {
                        Console.WriteLine("   🔬 CẬN LÂM SÀNG GẦN NHẤT:");
                        foreach (var c in clsReqs)
                        {
                            Console.WriteLine(string.Format("      - [{0}] {1}: {2} (STT: {3})", 
                                c.INTRUCTION_TIME, c.SERVICE_REQ_TYPE_NAME, c.TDL_PATIENT_NAME, c.SERVICE_REQ_STT_NAME ?? c.SERVICE_REQ_STT_ID.ToString()));
                            var ssf2 = new HisSereServViewFilter { SERVICE_REQ_ID = c.ID };
                            var sss2 = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf2, param);
                            if (sss2 != null)
                            {
                                foreach (var s2 in sss2)
                                {
                                    Console.WriteLine(string.Format("        + {0} ({1})", s2.TDL_SERVICE_NAME, s2.TDL_SERVICE_CODE));
                                    var ssef2 = new HisSereServExtFilter { SERE_SERV_ID = s2.ID };
                                    var sses2 = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef2, param);
                                    if (sses2 != null)
                                    {
                                        foreach (var se2 in sses2)
                                        {
                                            if (!string.IsNullOrEmpty(se2.DESCRIPTION)) Console.WriteLine("          * Mô tả: " + se2.DESCRIPTION.Replace("\r\n", " | ").Replace("\n", " | "));
                                            if (!string.IsNullOrEmpty(se2.CONCLUDE) && se2.CONCLUDE != ".") Console.WriteLine("          * Kết luận: " + se2.CONCLUDE);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Tờ điều trị gần nhất
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                if (trks != null && trks.Count > 0)
                {
                    Console.WriteLine("   📝 TỜ ĐIỀU TRỊ GẦN NHẤT:");
                    foreach (var tr in trks.OrderByDescending(x => x.TRACKING_TIME).Take(2))
                    {
                        Console.WriteLine(string.Format("      📅 [{0}] BS: {1}\n         Diễn biến: {2}", tr.TRACKING_TIME, tr.CREATOR, tr.CONTENT));
                    }
                }
            }
        }
    }
}
