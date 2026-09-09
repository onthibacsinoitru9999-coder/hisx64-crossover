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

namespace GetPatientDetails
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
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

            HisServiceReqViewFilter srfNB = new HisServiceReqViewFilter();
            srfNB.EXECUTE_ROOM_ID = roomIdNB;
            var reqsNB = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfNB, param);
            if (reqsNB == null) return;

            var sorted = reqsNB.OrderByDescending(x => x.INTRUCTION_TIME).ToList();

            // Lấy các ca từ 29/08/2026 đến nay (01/09/2026)
            var recentReqs = sorted.Where(x => x.INTRUCTION_TIME >= 20260829000000).ToList();
            var uniqueTreatments = recentReqs.Select(x => x.TREATMENT_ID).Distinct().ToList();

            Console.WriteLine(string.Format("=== TÌM THẤY {0} BỆNH NHÂN TRONG PHÒNG HỘI CHẨN CTCH&CS CSNB TỪ 29/08 ĐẾN 01/09/2026 ===", uniqueTreatments.Count));

            int count = 0;
            foreach (var tId in uniqueTreatments)
            {
                count++;
                var pReqs = recentReqs.Where(x => x.TREATMENT_ID == tId).ToList();
                var rep = pReqs.First();

                Console.WriteLine("\n===============================================================================");
                Console.WriteLine(string.Format("CA #{0}: {1} | Giới tính: {2} | Năm sinh: {3} | Mã BN: {4} | Mã ĐT: {5}",
                    count, rep.TDL_PATIENT_NAME, rep.TDL_PATIENT_GENDER_NAME, rep.TDL_PATIENT_DOB, rep.TDL_PATIENT_CODE, rep.TREATMENT_CODE));
                Console.WriteLine(string.Format("   Địa chỉ: {0}", rep.TDL_PATIENT_ADDRESS));

                // 1. Treatment
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trtList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trtList != null && trtList.Count > 0)
                {
                    var t = trtList.First();
                    Console.WriteLine(string.Format("   Vào viện: {0} | Ra viện: {1} | Trạng thái: {2} | Khoa cuối: {3}",
                        t.IN_TIME, t.OUT_TIME, t.IS_PAUSE == 1 ? "🔴 Đã khóa/ra viện" : "🟢 Đang điều trị", t.END_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("   Chẩn đoán chính: {0} [{1}]", t.ICD_NAME, t.ICD_CODE));
                    if (!string.IsNullOrEmpty(t.ICD_TEXT) || !string.IsNullOrEmpty(t.ICD_SUB_CODE))
                    {
                        Console.WriteLine(string.Format("   Bệnh kèm theo: {0} [{1}]", t.ICD_TEXT, t.ICD_SUB_CODE));
                    }
                }

                // 2. Yêu cầu hội chẩn
                foreach (var req in pReqs)
                {
                    Console.WriteLine(string.Format("   [YÊU CẦU HC] Mã: #{0} | Giờ gửi: {1} | Trạng thái: {2} | BS gửi: {3} ({4} - {5})",
                        req.SERVICE_REQ_CODE, req.INTRUCTION_TIME, req.SERVICE_REQ_STT_NAME ?? req.SERVICE_REQ_STT_ID.ToString(),
                        req.REQUEST_USERNAME, req.REQUEST_DEPARTMENT_NAME, req.REQUEST_ROOM_NAME));

                    HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var ss in sss)
                        {
                            var ssef = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                            var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                            if (sses != null && sses.Count > 0)
                            {
                                foreach (var se in sses)
                                {
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION)) Console.WriteLine("      📝 Ý KIẾN HỘI CHẨN:\n" + se.DESCRIPTION);
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".") Console.WriteLine("      📌 KẾT LUẬN HC: " + se.CONCLUDE);
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE)) Console.WriteLine("      💡 LỜI DẶN: " + se.INSTRUCTION_NOTE);
                                }
                            }
                            else
                            {
                                Console.WriteLine("      (Chưa có nội dung ý kiến trả lời trong HIS_SERE_SERV_EXT)");
                            }
                        }
                    }
                }

                // 3. CĐHA (CT, MRI, X-quang) & Xét nghiệm quan trọng
                HisServiceReqViewFilter allSrf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, allSrf, param);
                if (allReqs != null)
                {
                    var cdhaReqs = allReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3).OrderByDescending(x => x.INTRUCTION_TIME).ToList();
                    if (cdhaReqs.Count > 0)
                    {
                        Console.WriteLine("   📷 KẾT QUẢ CĐHA (X-QUANG, CT, MRI, SIÊU ÂM):");
                        foreach (var c in cdhaReqs)
                        {
                            var ssf2 = new HisSereServViewFilter { SERVICE_REQ_ID = c.ID };
                            var sss2 = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf2, param);
                            if (sss2 != null)
                            {
                                foreach (var s2 in sss2)
                                {
                                    var ssef2 = new HisSereServExtFilter { SERE_SERV_ID = s2.ID };
                                    var sses2 = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef2, param);
                                    if (sses2 != null && sses2.Count > 0)
                                    {
                                        foreach (var se2 in sses2)
                                        {
                                            Console.WriteLine(string.Format("      • [{0}] {1} (Mã: {2}):", c.INTRUCTION_TIME, s2.TDL_SERVICE_NAME, s2.TDL_SERVICE_CODE));
                                            if (!string.IsNullOrEmpty(se2.CONCLUDE) && se2.CONCLUDE != ".") Console.WriteLine("        -> KẾT LUẬN: " + se2.CONCLUDE.Replace("\r\n", " | ").Replace("\n", " | "));
                                            else if (!string.IsNullOrEmpty(se2.DESCRIPTION)) Console.WriteLine("        -> MÔ TẢ: " + se2.DESCRIPTION.Replace("\r\n", " | ").Replace("\n", " | "));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 4. Tờ điều trị mới nhất
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                if (trks != null && trks.Count > 0)
                {
                    Console.WriteLine("   📝 TỜ ĐIỀU TRỊ GẦN NHẤT:");
                    var latestTrk = trks.OrderByDescending(x => x.TRACKING_TIME).First();
                    Console.WriteLine(string.Format("      📅 [{0}] BS: {1} | Diễn biến: {2}", latestTrk.TRACKING_TIME, latestTrk.CREATOR, latestTrk.CONTENT));
                }
            }
        }
    }
}
