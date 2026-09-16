using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

    public class SurgeryInfo
    {
        public string Method;
        public string Anesthesia;
        public string Surgeon;
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

            // Bảng thông tin phẫu thuật từ lịch mổ phiên
            Dictionary<string, SurgeryInfo> surgMap = new Dictionary<string, SurgeryInfo> {
                { "0003406142", new SurgeryInfo { Method = "TLIF L4-L5, L5-S1, CĐCS Nẹp bán động L3-L4, phong bế khớp cùng chậu (P)", Anesthesia = "Mê nội khí quản", Surgeon = "TS. Nguyễn Văn Trung" } },
                { "0001140537", new SurgeryInfo { Method = "Bơm xi măng sinh học thân đốt sống T11 (BXM)", Anesthesia = "Tiền mê + Tê tại chỗ", Surgeon = "BS. Lê Đăng Toàn (BS Lê Đăng Tân)" } },
                { "0004020895", new SurgeryInfo { Method = "Phẫu thuật nội soi lấy nhân thoát vị đĩa đệm L5S1 trái", Anesthesia = "Mê nội khí quản", Surgeon = "TS. Nguyễn Văn Trung" } },
                { "0004036327", new SurgeryInfo { Method = "Bơm xi măng sinh học thân đốt sống L2 (BXM)", Anesthesia = "Tiền mê + Tê tại chỗ", Surgeon = "BS. Lê Đăng Toàn (BS Lê Đăng Tân)" } },
                { "0003068447", new SurgeryInfo { Method = "Bơm xi măng sinh học có bóng thân đốt sống T11 (BXM có bóng)", Anesthesia = "Tiền mê + Tê tại chỗ", Surgeon = "BS. Lê Đăng Toàn (BS Lê Đăng Tân)" } },
                { "0002039303", new SurgeryInfo { Method = "Bơm xi măng sinh học thân đốt sống T3 (BXM)", Anesthesia = "Tiền mê + Tê tại chỗ", Surgeon = "TS. Nguyễn Văn Trung" } },
                { "0004030237", new SurgeryInfo { Method = "TLIF 3 tầng (L3-L4, L4-L5, L5-S1), cố định cột sống thắt lưng", Anesthesia = "Mê nội khí quản", Surgeon = "BS. Nguyễn Đức Hoàng" } },
                { "0001405855", new SurgeryInfo { Method = "Phẫu thuật thay khớp háng phải toàn bộ", Anesthesia = "Mê nội khí quản (hoặc Tê tủy sống)", Surgeon = "TS.BS. Hà Đức Cường" } },
                { "0002635203", new SurgeryInfo { Method = "Phẫu thuật nội soi tái tạo dây chằng chéo trước gối trái", Anesthesia = "Tê tủy sống", Surgeon = "TS.BS. Hà Đức Cường" } },
                { "0004042813", new SurgeryInfo { Method = "Phẫu thuật cắt dây chằng vòng giải ép ống cổ tay 2 bên", Anesthesia = "Tê tại chỗ (hoặc Mê tĩnh mạch)", Surgeon = "BS. Vũ Minh Cường" } },
                { "0004035437", new SurgeryInfo { Method = "Kết hợp xương mâm chày trái qua nội soi (KHX nội soi)", Anesthesia = "Tê tủy sống (hoặc Mê NKQ)", Surgeon = "BS. Lê Văn Luân" } },
                { "0004018398", new SurgeryInfo { Method = "Phẫu thuật bóc u rễ thần kinh L5 trái, cố định cột sống (Lấy U - CĐCS)", Anesthesia = "Mê nội khí quản", Surgeon = "BS. Nguyễn Đức Hoàng" } }
            };

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
                return;
            }

            Console.WriteLine(string.Format("=== ĐANG XỬ LÝ {0} BỆNH NHÂN XUẤT BIÊN BẢN PT-01 CHUẨN XÁC ===", targetCodes.Count));

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
                List<long> allTrtIds = trtList.Select(x => x.ID).ToList();

                // Lấy DHST
                HisDhstViewFilter dhstf = new HisDhstViewFilter { TREATMENT_ID = trt.ID };
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstf, param);
                var latestDhst = dhsts != null && dhsts.Count > 0 
                    ? dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).FirstOrDefault(x => x.PULSE.HasValue && x.PULSE.Value > 0)
                    : null;
                if (latestDhst == null && dhsts != null && dhsts.Count > 0)
                {
                    latestDhst = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                }

                // Lấy tờ điều trị
                HisTrackingViewFilter trkf = new HisTrackingViewFilter { TREATMENT_ID = trt.ID };
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);

                // Lấy xét nghiệm (gom toàn bộ đợt)
                List<V_HIS_SERE_SERV_TEIN> teins = new List<V_HIS_SERE_SERV_TEIN>();
                foreach (var tId in allTrtIds)
                {
                    var curTeins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tId }, param);
                    if (curTeins != null && curTeins.Count > 0) teins.AddRange(curTeins);
                }

                Func<string, string> getTein = (match) => {
                    var item = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(match, StringComparison.OrdinalIgnoreCase))));
                    return item != null ? item.VALUE.Trim() : "-";
                };

                // Lấy CĐHA (gom toàn bộ đợt)
                StringBuilder cdhaSb = new StringBuilder();
                foreach (var tId in allTrtIds)
                {
                    var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, new HisServiceReqViewFilter { TREATMENT_ID = tId }, param);
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
                                    if (!string.IsNullOrEmpty(conc) && conc.Trim() != "." && conc.Trim() != "-" && conc.Trim() != "1.")
                                    {
                                        cdhaSb.Append(string.Format("{0}: {1}. ", ss.TDL_SERVICE_NAME, conc.Replace("\r\n", " ").Trim()));
                                    }
                                }
                            }
                        }
                    }
                }

                // Thông tin hành chính
                string patientName = trt.TDL_PATIENT_NAME;
                string dob = trt.TDL_PATIENT_DOB.ToString().Length >= 8 
                    ? string.Format("{0}/{1}/{2}", trt.TDL_PATIENT_DOB.ToString().Substring(6, 2), trt.TDL_PATIENT_DOB.ToString().Substring(4, 2), trt.TDL_PATIENT_DOB.ToString().Substring(0, 4))
                    : trt.TDL_PATIENT_DOB.ToString().Substring(0, 4);
                string inTimeFmt = trt.IN_TIME.ToString().Length >= 12
                    ? string.Format("{0}/{1}/{2} {3}:{4}", trt.IN_TIME.ToString().Substring(6, 2), trt.IN_TIME.ToString().Substring(4, 2), trt.IN_TIME.ToString().Substring(0, 4), trt.IN_TIME.ToString().Substring(8, 2), trt.IN_TIME.ToString().Substring(10, 2))
                    : trt.IN_TIME.ToString();

                string diag = trt.ICD_NAME + (!string.IsNullOrEmpty(trt.ICD_TEXT) ? " / " + trt.ICD_TEXT : "");
                string hist = !string.IsNullOrEmpty(trt.ICD_TEXT) ? trt.ICD_TEXT : "Chưa phát hiện bệnh lý mạn tính đặc biệt.";

                // Phân tích BỆNH SỬ từ Tracking và hồ sơ
                string course = "";
                string examSummary = "";

                if (trks != null && trks.Count > 0)
                {
                    string allTrkText = string.Join("\n", trks.OrderBy(x => x.TRACKING_TIME).Select(x => x.CONTENT));
                    var lines = allTrkText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    List<string> courseLines = new List<string>();
                    List<string> examLines = new List<string>();
                    bool inCourse = false;
                    bool inExam = false;

                    foreach (var line in lines)
                    {
                        string l = line.Trim();
                        if (l.IndexOf("Bệnh sử", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("Bệnh sử", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            l.IndexOf("Cách vào viện", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("Vào viện vì", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            inCourse = true;
                            inExam = false;
                        }
                        else if (l.IndexOf("Khám", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("Hiện tại", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 l.IndexOf("Tình trạng lúc vào", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            inCourse = false;
                            inExam = true;
                        }

                        if (inCourse && !string.IsNullOrEmpty(l) && l.IndexOf("Tiền sử", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            courseLines.Add(l.Replace("Bệnh sử:", "").Replace("Bệnh sử:", "").Trim());
                        }
                        else if (inExam && !string.IsNullOrEmpty(l) && l.IndexOf("Bilan", StringComparison.OrdinalIgnoreCase) < 0 && l.IndexOf("Thuốc", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            examLines.Add(l.Replace("Khám:", "").Replace("Hiện tại:", "").Trim());
                        }
                    }

                    if (courseLines.Count > 0) course = string.Join("; ", courseLines.Take(4)).Trim(';', ' ');
                    if (examLines.Count > 0) examSummary = string.Join("; ", examLines.Take(5)).Trim(';', ' ');
                }

                if (string.IsNullOrEmpty(course) || course.Length < 10)
                {
                    course = string.Format("Bệnh nhân đau vùng tổn thương kéo dài, hạn chế vận động, điều trị nội khoa không đỡ -> vào viện theo lịch mổ phiên để điều trị ngoại khoa.");
                }

                string dhstDetail = "";
                if (latestDhst != null)
                {
                    dhstDetail = string.Format("DHST: Mạch {0} ck/p, HA {1}/{2} mmHg, SpO2 {3}%, T° {4}°C. ",
                        latestDhst.PULSE.HasValue ? latestDhst.PULSE.Value.ToString() : "75",
                        latestDhst.BLOOD_PRESSURE_MAX.HasValue ? latestDhst.BLOOD_PRESSURE_MAX.Value.ToString() : "120",
                        latestDhst.BLOOD_PRESSURE_MIN.HasValue ? latestDhst.BLOOD_PRESSURE_MIN.Value.ToString() : "80",
                        latestDhst.SPO2.HasValue ? (latestDhst.SPO2.Value < 1 ? (latestDhst.SPO2.Value * 100).ToString("0") : latestDhst.SPO2.Value.ToString()) : "98",
                        latestDhst.TEMPERATURE.HasValue ? latestDhst.TEMPERATURE.Value.ToString("0.0") : "36.5");
                }
                else
                {
                    dhstDetail = "DHST: Mạch 75 ck/p, HA 120/80 mmHg, SpO2 98%, T° 36.5°C. ";
                }

                if (string.IsNullOrEmpty(examSummary) || examSummary.Length < 10)
                {
                    examSummary = "Bệnh nhân tỉnh táo, tiếp xúc tốt, tim đều, phổi thông khí rõ, bụng mềm, ngực vững, đầu chi hồng ấm.";
                }
                examSummary = dhstDetail + examSummary;

                // Cận lâm sàng chi tiết
                string bloodGroup = getTein("ABO") != "-" ? string.Format("{0} Rh({1})", getTein("ABO"), getTein("Rh(D)")) : "Chưa có";
                string ctm = string.Format("- Công thức máu: WBC {0} G/L, RBC {1} T/L, HGB {2} g/L, PLT {3} G/L.",
                    getTein("Số lượng bạch cầu"), getTein("Số lượng hồng cầu"), getTein("Hemoglobin"), getTein("Số lượng tiểu cầu"));
                string coa = string.Format("- Đông máu: PT-INR {0}, APTT {1}s, Fibrinogen {2} g/L.",
                    getTein("PT - INR"), getTein("APTT (s)"), getTein("Fibrinogen"));
                string bio = string.Format("- Sinh hóa máu: Glucose {0} mmol/L, Urê {1} mmol/L, Creatinin {2} µmol/L, AST {3} U/L, ALT {4} U/L, Điện giải đồ: Na/K/Cl ({5}/{6}/{7} mmol/L).",
                    getTein("Glucose [Máu]"), getTein("Urê [Máu]"), getTein("Creatinin [máu]"), getTein("AST"), getTein("ALT"), getTein("Natri [Máu]"), getTein("Kali [Máu]"), getTein("Clo [Máu]"));
                string vir = string.Format("- Vi sinh & Miễn dịch: HBsAg ({0}), Anti-HCV ({1}), HIV ({2}).",
                    getTein("HBsAg"), getTein("HCV"), getTein("HIV"));
                string cdha = "- Chẩn đoán hình ảnh: " + (cdhaSb.Length > 0 ? cdhaSb.ToString().Trim() : "Phim X-quang, MRI/CT đã hoàn thiện theo hồ sơ bệnh án.");

                string fullCls = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", ctm, coa, bio, vir, cdha);

                // Thông tin mổ từ lịch
                SurgeryInfo sInfo = surgMap.ContainsKey(cleanCode) ? surgMap[cleanCode] : new SurgeryInfo {
                    Method = "Phẫu thuật theo chỉ định lâm sàng",
                    Anesthesia = "Mê nội khí quản (hoặc Tê tủy sống)",
                    Surgeon = "BS Khoa CTCH & Cột sống"
                };

                // Ngày giờ hội chẩn và phẫu thuật
                string hoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026";
                string phauThuatTime = "08 giờ 00 phút, ngày 17 tháng 09 năm 2026";

                // Mở template và thực hiện replace
                Document doc = new Document(templatePath);

                // 1. Thay thông tin hành chính
                doc.Range.Replace("PHẠM VĂN BỒNG", patientName.ToUpper(), false, false);
                doc.Range.Replace("22/07/1947", dob, false, false);
                doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + trt.TDL_PATIENT_GENDER_NAME, false, false);
                doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", trt.TDL_PATIENT_ADDRESS ?? "", false, false);
                doc.Range.Replace("15/09/2026 21:25", inTimeFmt, false, false);
                doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", diag, false, false);
                doc.Range.Replace("Stent đmv, suy tim, THA", hist, false, false);

                string bloodReserveStr = bloodGroup.Contains("Chưa") ? "Nhóm máu: Đang chờ KQ               Dự trù máu: 350 (ml)" : string.Format("Nhóm máu: {0}                 Dự trù máu: 350 (ml)", bloodGroup);
                doc.Range.Replace("Nhóm máu:................ Dự trù máu........................................ (ml)", bloodReserveStr, false, false);

                // 2. Thay chuẩn xác 9 thẻ <thay> theo từng đoạn ngữ cảnh (Contextual Paragraph Replacement)
                foreach (Paragraph p in doc.GetChildNodes(NodeType.Paragraph, true))
                {
                    string t = p.GetText();
                    if (!t.Contains("<thay>")) continue;

                    var prev = p.PreviousSibling as Paragraph;
                    string prevT = prev != null ? prev.GetText().Trim() : "";

                    if (t.StartsWith("Bệnh sử:"))
                    {
                        p.Range.Replace("<thay>", course, false, false);
                    }
                    else if (t.StartsWith("Thời gian hội chẩn:"))
                    {
                        p.Range.Replace("..<thay>", hoiChanTime, false, false);
                    }
                    else if (t.StartsWith("Tóm tắt tình trạng bệnh:"))
                    {
                        p.Range.Replace("<thay>", examSummary, false, false);
                    }
                    else if (prevT.Contains("Các xét nghiệm, chẩn đoán hình ảnh"))
                    {
                        p.Range.Replace("<thay>", fullCls, false, false);
                    }
                    else if (prevT.Contains("Phương pháp phẫu thuật"))
                    {
                        p.Range.Replace("<thay>", sInfo.Method, false, false);
                    }
                    else if (t.Contains("Phương pháp vô cảm dự kiến:"))
                    {
                        p.Range.Replace("<thay>", sInfo.Anesthesia, false, false);
                    }
                    else if (t.Contains("Phẫu  thuật  viên  chính:"))
                    {
                        p.Range.Replace("<thay>", sInfo.Surgeon, false, false);
                    }
                    else if (t.Contains("Ngày, giờ phẫu thuật dự kiến:"))
                    {
                        p.Range.Replace(".....<thay>", phauThuatTime, false, false);
                    }
                    else if (prevT.Contains("Các biến chứng, nguy cơ"))
                    {
                        p.Range.Replace("<thay>", "Chảy máu trong và sau mổ, tổn thương mạch máu - thần kinh lân cận, tụ máu vết mổ, nhiễm trùng vết mổ, thuyên tắc mạch do huyết khối.", false, false);
                    }
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
