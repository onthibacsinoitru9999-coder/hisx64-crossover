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

namespace FetchDebatePatients
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
            long roomIdHN = 11387; // Phòng hội chẩn CTCH & CS (Hà Nội)

            Console.WriteLine("===============================================================================");
            Console.WriteLine("   🏥 QUÉT BỆNH NHÂN TẠI PHÒNG HỘI CHẨN KHOA CTCH VÀ CS (NINH BÌNH & HÀ NỘI)");
            Console.WriteLine("===============================================================================");

            // 1. Quét ServiceReq tại Ninh Bình (RoomId 18686)
            Console.WriteLine("\n🔍 1. Quét yêu cầu dịch vụ tại Phòng hội chẩn Ninh Bình (RoomId: 18686)...");
            HisServiceReqViewFilter srfNB = new HisServiceReqViewFilter();
            srfNB.EXECUTE_ROOM_ID = roomIdNB;
            var reqsNB = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfNB, param);
            Console.WriteLine(string.Format("-> Kết quả: {0} yêu cầu.", reqsNB != null ? reqsNB.Count : 0));

            // 2. Quét ServiceReq tại Hà Nội (RoomId 11387)
            Console.WriteLine("\n🔍 2. Quét yêu cầu dịch vụ tại Phòng hội chẩn Hà Nội (RoomId: 11387)...");
            HisServiceReqViewFilter srfHN = new HisServiceReqViewFilter();
            srfHN.EXECUTE_ROOM_ID = roomIdHN;
            var reqsHN = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfHN, param);
            Console.WriteLine(string.Format("-> Kết quả: {0} yêu cầu.", reqsHN != null ? reqsHN.Count : 0));

            // 3. Quét các phòng hội chẩn khác tại Khoa Ngoại TH Ninh Bình (Dept 915)
            Console.WriteLine("\n🔍 3. Quét các phòng hội chẩn khác tại Khoa Ngoại TH Ninh Bình (Dept 915)...");
            long[] nbDebateRooms = new long[] { 18686, 18687, 18688, 18689, 18690, 15950 };
            List<V_HIS_SERVICE_REQ> allReqs = new List<V_HIS_SERVICE_REQ>();
            if (reqsNB != null) allReqs.AddRange(reqsNB);

            foreach (var rId in nbDebateRooms)
            {
                if (rId == roomIdNB) continue;
                HisServiceReqViewFilter f = new HisServiceReqViewFilter { EXECUTE_ROOM_ID = rId };
                var list = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, f, param);
                if (list != null && list.Count > 0)
                {
                    Console.WriteLine(string.Format("   - Phòng {0}: Có {1} yêu cầu", rId, list.Count));
                    allReqs.AddRange(list);
                }
            }

            var targetReqs = (reqsNB != null && reqsNB.Count > 0) ? reqsNB : allReqs;
            if (targetReqs.Count == 0 && reqsHN != null && reqsHN.Count > 0)
            {
                Console.WriteLine("\n[THÔNG BÁO] Không có yêu cầu tại Ninh Bình. Quét được tại CS1 Hà Nội.");
                targetReqs = reqsHN;
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("   📋 DANH SÁCH CHI TIẾT TỪNG BỆNH NHÂN ĐANG CÓ TÊN TẠI PHÒNG HỘI CHẨN");
            Console.WriteLine("===============================================================================");

            var grouped = targetReqs.GroupBy(x => x.TREATMENT_ID).ToList();
            Console.WriteLine(string.Format("Tổng số bệnh nhân: {0}", grouped.Count));

            foreach (var grp in grouped)
            {
                var repReq = grp.First();
                Console.WriteLine("\n-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("👤 BỆNH NHÂN: {0} | Giới tính: {1} | DOB: {2}", 
                    repReq.TDL_PATIENT_NAME, repReq.TDL_PATIENT_GENDER_NAME, repReq.TDL_PATIENT_DOB));
                Console.WriteLine(string.Format("   Mã BN: {0} | Mã ĐT: {1} (TreatmentId: {2})",
                    repReq.TDL_PATIENT_CODE, repReq.TREATMENT_CODE, repReq.TREATMENT_ID));
                Console.WriteLine(string.Format("   Địa chỉ: {0}", repReq.TDL_PATIENT_ADDRESS));
                Console.WriteLine(string.Format("   Chẩn đoán: {0} (ICD: {1})", repReq.ICD_NAME ?? repReq.ICD_TEXT, repReq.ICD_CODE));
                Console.WriteLine(string.Format("   Yêu cầu: {0} | Loại: {1} | Trạng thái: {2}", 
                    repReq.SERVICE_REQ_CODE, repReq.SERVICE_REQ_TYPE_NAME, repReq.SERVICE_REQ_STT_NAME ?? repReq.SERVICE_REQ_STT_ID.ToString()));
                Console.WriteLine(string.Format("   Bác sĩ chỉ định: {0} ({1}) | Thời gian: {2}",
                    repReq.REQUEST_USERNAME, repReq.REQUEST_LOGINNAME, repReq.INTRUCTION_TIME));
                Console.WriteLine(string.Format("   Khoa/Phòng chỉ định: {0} ({1})",
                    repReq.REQUEST_DEPARTMENT_NAME, repReq.REQUEST_ROOM_NAME));

                // 1. Hồ sơ đợt điều trị
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = repReq.TREATMENT_ID };
                var trts = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trts != null && trts.Count > 0)
                {
                    var t = trts.First();
                    Console.WriteLine(string.Format("   [Hồ sơ] Vào viện: {0} | Ra viện: {1} | Trạng thái: {2} | Khoa cuối: {3}",
                        t.IN_TIME, t.OUT_TIME, t.IS_PAUSE == 1 ? "Đã khóa/Tạm dừng" : "Đang điều trị", t.END_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("   [Chẩn đoán]: {0} [{1}] | Phụ: {2} [{3}]", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT, t.ICD_SUB_CODE));
                }

                // 2. Chi tiết dịch vụ & Ý kiến hội chẩn chuyên khoa
                HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = repReq.ID };
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                if (sss != null && sss.Count > 0)
                {
                    Console.WriteLine("   [Dịch vụ hội chẩn]:");
                    foreach (var s in sss)
                    {
                        Console.WriteLine(string.Format("     • {0} (Mã: {1}) | SL: {2} | Phòng TH: {3}",
                            s.TDL_SERVICE_NAME, s.TDL_SERVICE_CODE, s.AMOUNT, s.EXECUTE_ROOM_NAME));

                        // Lấy ý kiến từ HIS_SERE_SERV_EXT
                        var ssef = new HisSereServExtFilter { SERE_SERV_ID = s.ID };
                        var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                        if (sses != null && sses.Count > 0)
                        {
                            foreach (var se in sses)
                            {
                                if (!string.IsNullOrEmpty(se.DESCRIPTION)) Console.WriteLine("       📝 Ý kiến: " + se.DESCRIPTION);
                                if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".") Console.WriteLine("       📌 Kết luận: " + se.CONCLUDE);
                                if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE)) Console.WriteLine("       💡 Lời dặn: " + se.INSTRUCTION_NOTE);
                            }
                        }
                    }
                }

                // 3. Tờ điều trị gần nhất
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = repReq.TREATMENT_ID };
                var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                if (trackings != null && trackings.Count > 0)
                {
                    Console.WriteLine("   [Tờ điều trị gần nhất]:");
                    foreach (var trk in trackings.OrderByDescending(x => x.TRACKING_TIME).Take(2))
                    {
                        Console.WriteLine(string.Format("     📅 Thời gian: {0} | BS: {1} | Diễn biến: {2}", 
                            trk.TRACKING_TIME, trk.CREATOR, trk.CONTENT));
                    }
                }

                // 4. Biên bản hội chẩn HIS_DEBATE
                HisDebateFilter df = new HisDebateFilter { TREATMENT_ID = repReq.TREATMENT_ID };
                var debates = adapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, df, param);
                if (debates != null && debates.Count > 0)
                {
                    Console.WriteLine("   [Biên bản hội chẩn HIS_DEBATE]:");
                    foreach (var d in debates)
                    {
                        Console.WriteLine(string.Format("     🔹 HC ID: {0} | Thời gian: {1} | Địa điểm: {2}", d.ID, d.DEBATE_TIME, d.LOCATION));
                        if (!string.IsNullOrEmpty(d.ICD_NAME)) Console.WriteLine("        Chẩn đoán HC: " + d.ICD_NAME);
                        if (!string.IsNullOrEmpty(d.TREATMENT_TRACKING)) Console.WriteLine("        Tóm tắt ĐT: " + d.TREATMENT_TRACKING);
                        if (!string.IsNullOrEmpty(d.DISCUSSION)) Console.WriteLine("        Thảo luận: " + d.DISCUSSION);
                        if (!string.IsNullOrEmpty(d.CONCLUSION)) Console.WriteLine("        Kết luận: " + d.CONCLUSION);
                    }
                }
            }
        }
    }
}
