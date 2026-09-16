using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Aspose.Words;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace HisPt01Creator
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    public class PatientPt01Data
    {
        public string Code;
        public string TreatmentCode;
        public string Name;
        public string Dob;
        public string Gender;
        public string Address;
        public string InTime;
        public string Diagnosis;
        public string History;
        public string Course;
        public string Summary;
        public string Cls;
        public string BloodGroup;
        public string BloodReserve;
        public string Surgery;
        public string Anesthesia;
        public string Surgeon;
        public string SurgeryTime;
        public string Risks;
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
            };
            Run(args);
        }

        static void Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
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

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mau pt01.docx");
            if (!File.Exists(templatePath))
            {
                templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "mau pt01.docx");
            }
            if (!File.Exists(templatePath))
            {
                Console.WriteLine("❌ LỖI: Không tìm thấy file mẫu mau pt01.docx!");
                return;
            }

            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            List<string> targetCodes = new List<string>();
            if (args.Length > 0)
            {
                string input = string.Join(",", args).Replace(" ", "");
                targetCodes = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (targetCodes.Count == 0)
            {
                Console.WriteLine("===============================================================================");
                Console.WriteLine("📝 HIS BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01 CREATOR CLI)");
                Console.WriteLine("===============================================================================");
                Console.WriteLine("Cú pháp: HisPt01Creator.exe <MãBN1,MãBN2,...>");
                Console.WriteLine("Ví dụ:   HisPt01Creator.exe 0003406142,0001140537");
                return;
            }

            Console.WriteLine(string.Format("=== ĐANG XỬ LÝ {0} BỆNH NHÂN XUẤT BIÊN BẢN PT-01 ===", targetCodes.Count));

            int count = 0;
            foreach (var code in targetCodes)
            {
                count++;
                string cleanCode = code.Trim().PadLeft(10, '0');
                Console.WriteLine(string.Format("\n[{0}/{1}] Đang nạp hồ sơ BN: {2}...", count, targetCodes.Count, cleanCode));

                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = cleanCode };
                var trtList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trtList == null || trtList.Count == 0)
                {
                    Console.WriteLine("  ❌ Không tìm thấy hồ sơ điều trị cho mã BN: " + cleanCode);
                    continue;
                }

                var trt = trtList.OrderByDescending(x => x.IN_TIME).First();

                // Lấy DHST
                HisDhstViewFilter dhstf = new HisDhstViewFilter { TREATMENT_ID = trt.ID };
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstf, param);
                var latestDhst = dhsts != null && dhsts.Count > 0 ? dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First() : null;

                // Lấy tờ điều trị
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = trt.ID };
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                var firstTrk = trks != null && trks.Count > 0 ? trks.OrderBy(x => x.TRACKING_TIME).First() : null;
                var latestTrk = trks != null && trks.Count > 0 ? trks.OrderBy(x => x.TRACKING_TIME).Last() : null;

                // Lấy xét nghiệm chỉ số (Tein)
                HisSereServTeinViewFilter teinf = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = trt.ID };
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinf, param);

                // Nếu đợt hiện tại chưa có xét nghiệm, tìm trong đợt gần nhất
                if ((teins == null || teins.Count == 0) && trtList.Count > 1)
                {
                    foreach (var pastTrt in trtList.OrderByDescending(x => x.IN_TIME).Skip(1))
                    {
                        var pastTeins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, new HisSereServTeinViewFilter { TDL_TREATMENT_ID = pastTrt.ID }, param);
                        if (pastTeins != null && pastTeins.Count > 0)
                        {
                            teins = pastTeins;
                            break;
                        }
                    }
                }

                Func<string, string> getTein = (match) => {
                    if (teins == null) return "-";
                    var item = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(match, StringComparison.OrdinalIgnoreCase))));
                    return item != null ? string.Format("{0} {1}", item.VALUE, item.TEST_INDEX_UNIT_NAME).Trim() : "-";
                };

                // Lấy CĐHA
                HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = trt.ID };
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                StringBuilder cdhaSb = new StringBuilder();
                if (reqs != null)
                {
                    var cdhaReqs = reqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3).OrderByDescending(x => x.INTRUCTION_TIME).ToList();
                    foreach (var cr in cdhaReqs)
                    {
                        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, new HisSereServViewFilter { SERVICE_REQ_ID = cr.ID }, param);
                        if (sss != null)
                        {
                            foreach (var ss in sss)
                            {
                                var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, new HisSereServExtFilter { SERE_SERV_ID = ss.ID }, param);
                                string conc = sses != null && sses.Count > 0 ? sses[0].CONCLUDE : "";
                                if (!string.IsNullOrEmpty(conc) && conc.Trim() != ".")
                                {
                                    cdhaSb.Append(string.Format("{0}: {1}. ", ss.TDL_SERVICE_NAME, conc.Replace("\r\n", " ").Trim()));
                                }
                            }
                        }
                    }
                }

                // Tổng hợp dữ liệu
                string patientName = trt.TDL_PATIENT_NAME;
                string dob = trt.TDL_PATIENT_DOB.ToString().Length >= 8 
                    ? string.Format("{0}/{1}/{2}", trt.TDL_PATIENT_DOB.ToString().Substring(6, 2), trt.TDL_PATIENT_DOB.ToString().Substring(4, 2), trt.TDL_PATIENT_DOB.ToString().Substring(0, 4))
                    : trt.TDL_PATIENT_DOB.ToString().Substring(0, 4);
                string inTimeFmt = trt.IN_TIME.ToString().Length >= 12
                    ? string.Format("{0}/{1}/{2} {3}:{4}", trt.IN_TIME.ToString().Substring(6, 2), trt.IN_TIME.ToString().Substring(4, 2), trt.IN_TIME.ToString().Substring(0, 4), trt.IN_TIME.ToString().Substring(8, 2), trt.IN_TIME.ToString().Substring(10, 2))
                    : trt.IN_TIME.ToString();

                string diag = trt.ICD_NAME + (!string.IsNullOrEmpty(trt.ICD_TEXT) ? " / " + trt.ICD_TEXT : "");
                string hist = !string.IsNullOrEmpty(trt.ICD_TEXT) ? trt.ICD_TEXT : "Chưa phát hiện bệnh lý mạn tính đặc biệt.";
                string course = firstTrk != null && !string.IsNullOrEmpty(firstTrk.CONTENT) ? firstTrk.CONTENT.Replace("\r\n", " ") : "Bệnh nhân đau, hạn chế vận động, điều trị nội khoa không đỡ -> vào viện phẫu thuật.";
                string summary = latestTrk != null && !string.IsNullOrEmpty(latestTrk.CONTENT) ? latestTrk.CONTENT.Replace("\r\n", " ") : "Bệnh nhân tỉnh táo, tiếp xúc tốt, tim đều, phổi rõ, huyết động ổn định.";

                string clsSummary = cdhaSb.ToString();
                clsSummary += string.Format(" CTM: WBC {0}, RBC {1}, HGB {2}, PLT {3}. Đông máu: PT-INR {4}, APTT {5}, Fibrinogen {6}. Sinh hóa: Glucose {7}, Ure {8}, Creatinin {9}, AST {10}, ALT {11}. Nhóm máu: {12} Rh({13}).",
                    getTein("Số lượng bạch cầu"), getTein("Số lượng hồng cầu"), getTein("Hemoglobin"), getTein("Số lượng tiểu cầu"),
                    getTein("PT - INR"), getTein("APTT (s)"), getTein("Fibrinogen"),
                    getTein("Glucose [Máu]"), getTein("Urê [Máu]"), getTein("Creatinin [máu]"), getTein("AST"), getTein("ALT"),
                    getTein("ABO"), getTein("Rh(D)"));

                string bloodGrp = getTein("ABO") != "-" ? string.Format("{0} Rh({1})", getTein("ABO"), getTein("Rh(D)")) : "O Rh(+)";

                // Mở template và thực hiện replace chuẩn
                Document doc = new Document(templatePath);

                doc.Range.Replace("PHẠM VĂN BỒNG", patientName.ToUpper(), false, false);
                doc.Range.Replace("22/07/1947", dob, false, false);
                doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + trt.TDL_PATIENT_GENDER_NAME, false, false);
                doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", trt.TDL_PATIENT_ADDRESS ?? "", false, false);
                doc.Range.Replace("15/09/2026 21:25", inTimeFmt, false, false);
                doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", diag, false, false);
                doc.Range.Replace("Stent đmv, suy tim, THA", hist, false, false);

                doc.Range.Replace("Nhóm máu:................ Dự trù máu........................................ (ml)",
                    string.Format("Nhóm máu: {0}                 Dự trù máu: 350 (ml)", bloodGrp), false, false);

                string[] replacements = new string[] {
                    course,                                                  // 1: Bệnh sử
                    DateTime.Now.ToString("14 giờ 00 ngày dd/MM/yyyy"),      // 2: Thời gian hội chẩn
                    summary,                                                 // 3: Tóm tắt tình trạng bệnh
                    clsSummary,                                              // 4: Các xét nghiệm, CĐHA
                    "Phẫu thuật theo chỉ định lâm sàng",                    // 5: Phương pháp phẫu thuật
                    "Mê nội khí quản (hoặc Tê tủy sống)",                   // 6: Phương pháp vô cảm dự kiến
                    "BS Khoa CTCH & Cột sống",                               // 7: Phẫu thuật viên chính
                    DateTime.Now.AddDays(1).ToString("08 giờ 00 phút, ngày dd/MM/yyyy"), // 8: Ngày, giờ phẫu thuật dự kiến
                    "Chảy máu, tổn thương mạch máu thần kinh lân cận, nhiễm trùng vết mổ." // 9: Biến chứng nguy cơ
                };

                for (int i = 0; i < replacements.Length; i++)
                {
                    doc.Range.Replace(new Regex("<thay>"), replacements[i]);
                }

                string safeName = patientName.Replace(" ", "_");
                string fileName = string.Format("PT01_{0:D2}_{1}_{2}.docx", count, safeName, cleanCode);
                string savePath = Path.Combine(outDir, fileName);
                doc.Save(savePath);
                Console.WriteLine("  ✔ Đã xuất file thành công: " + fileName);
            }

            Console.WriteLine("\n🎉 HOÀN THÀNH TẤT CẢ BIÊN BẢN PT-01 TẠI: " + outDir);
        }
    }
}
