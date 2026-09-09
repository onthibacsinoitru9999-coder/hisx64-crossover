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

namespace GenerateDebateReport
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    public class DebatePatientDto
    {
        public string PatientName { get; set; }
        public string Gender { get; set; }
        public string Dob { get; set; }
        public string PatientCode { get; set; }
        public string TreatmentCode { get; set; }
        public long TreatmentId { get; set; }
        public string Address { get; set; }
        public string DepartmentName { get; set; }
        public string RoomName { get; set; }
        public string RequestDoctor { get; set; }
        public string RequestTime { get; set; }
        public string StatusName { get; set; }
        public string MainDiagnosis { get; set; }
        public string IcdCode { get; set; }
        public string SubDiagnosis { get; set; }
        public string IsPause { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public List<string> ConsultationOpinions { get; set; }
        public List<string> ImagingResults { get; set; }
        public string LatestTracking { get; set; }

        public DebatePatientDto()
        {
            ConsultationOpinions = new List<string>();
            ImagingResults = new List<string>();
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

            Console.WriteLine("=== ĐANG TẢI DỮ LIỆU PHÒNG HỘI CHẨN CTCH & CS NINH BÌNH (ROOM 18686) ===");
            HisServiceReqViewFilter srfNB = new HisServiceReqViewFilter();
            srfNB.EXECUTE_ROOM_ID = roomIdNB;
            var reqsNB = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfNB, param);
            if (reqsNB == null || reqsNB.Count == 0)
            {
                Console.WriteLine("Không tìm thấy yêu cầu dịch vụ.");
                return;
            }

            var sorted = reqsNB.OrderByDescending(x => x.INTRUCTION_TIME).ToList();
            var recentReqs = sorted.Where(x => x.INTRUCTION_TIME >= 20260829000000).ToList();
            if (recentReqs.Count == 0) recentReqs = sorted.Take(15).ToList();

            var uniqueTreatmentIds = recentReqs.Select(x => x.TREATMENT_ID).Distinct().ToList();
            Console.WriteLine(string.Format("Đang xử lý {0} bệnh nhân gần nhất...", uniqueTreatmentIds.Count));

            List<DebatePatientDto> patientList = new List<DebatePatientDto>();
            int cur = 0;

            foreach (var tId in uniqueTreatmentIds)
            {
                cur++;
                var pReqs = recentReqs.Where(x => x.TREATMENT_ID == tId).ToList();
                var rep = pReqs.First();

                Console.WriteLine(string.Format("[{0}/{1}] {2} (Mã BN: {3})", cur, uniqueTreatmentIds.Count, rep.TDL_PATIENT_NAME, rep.TDL_PATIENT_CODE));

                var dto = new DebatePatientDto
                {
                    PatientName = rep.TDL_PATIENT_NAME,
                    Gender = rep.TDL_PATIENT_GENDER_NAME,
                    Dob = rep.TDL_PATIENT_DOB.ToString(),
                    PatientCode = rep.TDL_PATIENT_CODE,
                    TreatmentCode = rep.TREATMENT_CODE,
                    TreatmentId = rep.TREATMENT_ID,
                    Address = rep.TDL_PATIENT_ADDRESS,
                    DepartmentName = rep.REQUEST_DEPARTMENT_NAME,
                    RoomName = rep.REQUEST_ROOM_NAME,
                    RequestDoctor = rep.REQUEST_USERNAME,
                    RequestTime = rep.INTRUCTION_TIME.ToString(),
                    StatusName = rep.SERVICE_REQ_STT_NAME ?? rep.SERVICE_REQ_STT_ID.ToString(),
                    MainDiagnosis = rep.ICD_NAME ?? rep.ICD_TEXT,
                    IcdCode = rep.ICD_CODE
                };

                // Treatment info
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trtList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trtList != null && trtList.Count > 0)
                {
                    var t = trtList.First();
                    dto.IsPause = t.IS_PAUSE == 1 ? "🔴 Đã ra viện / Tạm khóa" : "🟢 Đang điều trị nội trú";
                    dto.InTime = t.IN_TIME.ToString();
                    dto.OutTime = t.OUT_TIME.HasValue ? t.OUT_TIME.Value.ToString() : "";
                    if (!string.IsNullOrEmpty(t.ICD_NAME)) dto.MainDiagnosis = t.ICD_NAME;
                    if (!string.IsNullOrEmpty(t.ICD_CODE)) dto.IcdCode = t.ICD_CODE;
                    dto.SubDiagnosis = t.ICD_TEXT;
                }

                // Consultation opinions
                foreach (var req in pReqs)
                {
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
                                    StringBuilder sb = new StringBuilder();
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION)) sb.Append(se.DESCRIPTION);
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".") sb.Append("\n[Kết luận]: " + se.CONCLUDE);
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE)) sb.Append("\n[Lời dặn]: " + se.INSTRUCTION_NOTE);
                                    if (sb.Length > 0) dto.ConsultationOpinions.Add(sb.ToString());
                                }
                            }
                        }
                    }
                }

                // CĐHA (Lấy 3 kết quả gần nhất)
                HisServiceReqViewFilter allSrf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, allSrf, param);
                if (allReqs != null)
                {
                    var cdhaReqs = allReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3).OrderByDescending(x => x.INTRUCTION_TIME).Take(4).ToList();
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
                                        string res = !string.IsNullOrEmpty(se2.CONCLUDE) && se2.CONCLUDE != "." ? se2.CONCLUDE : se2.DESCRIPTION;
                                        if (!string.IsNullOrEmpty(res))
                                        {
                                            dto.ImagingResults.Add(string.Format("[{0}] {1}: {2}", c.INTRUCTION_TIME, s2.TDL_SERVICE_NAME, res.Replace("\r\n", " ").Replace("\n", " ")));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Latest tracking
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                if (trks != null && trks.Count > 0)
                {
                    var latest = trks.OrderByDescending(x => x.TRACKING_TIME).First();
                    dto.LatestTracking = string.Format("[{0}] (BS: {1}): {2}", latest.TRACKING_TIME, latest.CREATOR, latest.CONTENT);
                }

                patientList.Add(dto);
            }

            // Xuất Markdown
            string outMd = @"e:\his-x64-28-11fix GDYK\his-x64\Reports\BaoCao_HoiChan_CTCH_NinhBinh.md";
            string outDir = Path.GetDirectoryName(outMd);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            StringBuilder md = new StringBuilder();
            md.AppendLine("# 🏥 BÁO CÁO TỔNG HỢP BỆNH ÁN TẠI PHÒNG HỘI CHẨN KHOA CTCH VÀ CỘT SỐNG (CƠ SỞ NINH BÌNH)");
            md.AppendLine(string.Format("*Thời gian xuất báo cáo: {0}*", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            md.AppendLine(string.Format("*Địa điểm: Phòng Hội chẩn Khoa Chấn thương chỉnh hình & Phẫu thuật cột sống - Cơ sở Ninh Bình (`RoomId = 18686`)*\n"));

            md.AppendLine("## 📊 TỔNG QUAN DANH SÁCH BỆNH NHÂN ĐANG CÓ TÊN TẠI PHÒNG HỘI CHẨN");
            md.AppendLine("| STT | Bệnh Nhân | Tuổi / Giới | Mã BN | Khoa / Buồng Chỉ Định | Chẩn Đoán Chính | Trạng Thái HC | Hướng Xử Trí / Kết Luận |");
            md.AppendLine("| :---: | :--- | :---: | :---: | :--- | :--- | :---: | :--- |");

            int idx = 1;
            foreach (var p in patientList)
            {
                string dobYear = p.Dob.Length >= 4 ? p.Dob.Substring(0, 4) : p.Dob;
                int y;
                int.TryParse(dobYear, out y);
                int age = y > 0 ? (2026 - y) : 0;
                string shortOp = p.ConsultationOpinions.Count > 0 ? p.ConsultationOpinions.First().Replace("\r\n", " ").Replace("\n", " ") : "Chờ hội chẩn";
                if (shortOp.Length > 80) shortOp = shortOp.Substring(0, 77) + "...";

                string sttBadge = p.StatusName.Contains("Chưa") ? "🟡 Chưa xử lý" : (p.StatusName.Contains("Đang") ? "🟠 Đang xử lý" : "🟢 Hoàn thành");

                md.AppendLine(string.Format("| {0} | **{1}** | {2}t ({3}) | `{4}` | {5} ({6}) | {7} | {8} | {9} |",
                    idx++, p.PatientName, age, p.Gender, p.PatientCode, p.DepartmentName, p.RoomName, p.MainDiagnosis, sttBadge, shortOp));
            }

            md.AppendLine("\n---\n");
            md.AppendLine("## 📝 TÓM TẮT BỆNH ÁN CHI TIẾT TỪNG BỆNH NHÂN\n");

            idx = 1;
            foreach (var p in patientList)
            {
                string dobYear = p.Dob.Length >= 4 ? p.Dob.Substring(0, 4) : p.Dob;
                int y2;
                int.TryParse(dobYear, out y2);
                int age = y2 > 0 ? (2026 - y2) : 0;

                md.AppendLine(string.Format("### {0}. Bệnh nhân: **{1}** ({2} tuổi - {3})", idx++, p.PatientName, age, p.Gender));
                md.AppendLine(string.Format("- **Mã Bệnh Nhân**: `{0}` | **Mã Đợt Điều Trị**: `{1}`", p.PatientCode, p.TreatmentCode));
                md.AppendLine(string.Format("- **Địa chỉ**: {0}", p.Address));
                md.AppendLine(string.Format("- **Khoa / Buồng hiện tại**: {0} - {1} (Bác sĩ chỉ định: {2})", p.DepartmentName, p.RoomName, p.RequestDoctor));
                md.AppendLine(string.Format("- **Thời gian gửi yêu cầu HC**: `{0}` | **Trạng thái**: **{1}**", p.RequestTime, p.StatusName));
                md.AppendLine(string.Format("- **Thời gian vào viện**: `{0}` | **Tình trạng đợt điều trị**: {1}", p.InTime, p.IsPause));
                md.AppendLine(string.Format("- **Chẩn đoán xác định**: **{0}** [`{1}`]", p.MainDiagnosis, p.IcdCode));
                if (!string.IsNullOrEmpty(p.SubDiagnosis))
                {
                    md.AppendLine(string.Format("- **Bệnh kèm theo / Tiền sử**: {0}", p.SubDiagnosis));
                }

                if (p.ConsultationOpinions.Count > 0)
                {
                    md.AppendLine("\n> [!IMPORTANT]");
                    md.AppendLine("> **Ý KIẾN & KẾT LUẬN HỘI CHẨN CHUYÊN KHOA CTCH & CỘT SỐNG:**");
                    foreach (var op in p.ConsultationOpinions)
                    {
                        var lines = op.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var l in lines)
                        {
                            md.AppendLine("> " + l);
                        }
                    }
                }
                else
                {
                    md.AppendLine("\n> [!NOTE]");
                    md.AppendLine("> **HỘI CHẨN:** Phiếu yêu cầu đang chờ Bác sĩ chuyên khoa CTCH & Cột sống tiếp nhận và đưa ra kết luận.");
                }

                if (p.ImagingResults.Count > 0)
                {
                    md.AppendLine("\n**Kết quả Chẩn đoán hình ảnh & Thăm dò:**");
                    foreach (var img in p.ImagingResults)
                    {
                        md.AppendLine("- " + img);
                    }
                }

                if (!string.IsNullOrEmpty(p.LatestTracking))
                {
                    md.AppendLine(string.Format("\n**Diễn biến lâm sàng mới nhất:**\n- {0}", p.LatestTracking));
                }

                md.AppendLine("\n---");
            }

            File.WriteAllText(outMd, md.ToString(), Encoding.UTF8);
            Console.WriteLine("\n[XUẤT BÁO CÁO THÀNH CÔNG] File saved at: " + outMd);
        }
    }
}
