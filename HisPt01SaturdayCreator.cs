using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Aspose.Words;

namespace HisPt01Creator
{
    public class PatientProfile
    {
        public int Stt;
        public string Room; // PM 5 hay PM 2
        public string PatientCode;
        public string PatientName;
        public string Dob;
        public string Gender;
        public string Address;
        public string InTime;
        public string Diagnosis;
        public string History;
        public string Course;
        public string HoiChanTime;
        public string ExamSummary;
        public string FullCls;
        public string BloodGroup;
        public string BloodReserve;
        public string SurgeryMethod;
        public string Anesthesia;
        public string Surgeon;
        public string SurgeryTime;
        public string Risks;
    }

    public class JsonPatient
    {
        public string Ma_Ho_So;
        public string PatientCode;
        public string Ho_Va_Ten;
        public string Ngay_Sinh;
        public string Gioi_Tinh;
        public string Dia_Chi;
        public string Ngay_Gio_Vao_Vien;
        public string IcdCode;
        public string IcdName;
        public string IcdText;
        public string Hb;
        public string Wbc;
        public string Plt;
        public string Inr;
        public string Fib;
        public string Aptt;
        public string Glu;
        public string Ure;
        public string Cre;
        public string Ast;
        public string Alt;
        public string Nhom_Mau;
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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01", "PT01_20260926_HN");
            string outDirStd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(outDirDated)) Directory.CreateDirectory(outDirDated);
            if (!Directory.Exists(outDirStd)) Directory.CreateDirectory(outDirStd);

            List<PatientProfile> allList = BuildProfiles();

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) LỊCH MỔ THỨ BẢY 26/09/2026 — KHOA 57 HÀ NỘI ===", allList.Count));

            foreach (var p in allList)
            {
                Console.WriteLine(string.Format("\n[{0:D2}/{1:D2}] Đang tạo biên bản cho BN: {2} ({3}) | {4} | PTV: {5}...", 
                    p.Stt, allList.Count, p.PatientName, p.PatientCode, p.Room, p.Surgeon));
                
                Document doc = new Document(templatePath);

                // 1. Thay thế thông tin hành chính
                doc.Range.Replace("PHẠM VĂN BỒNG", p.PatientName.ToUpper(), false, false);
                doc.Range.Replace("22/07/1947", p.Dob, false, false);
                doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + p.Gender, false, false);
                doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", p.Address, false, false);
                doc.Range.Replace("15/09/2026 21:25", p.InTime, false, false);
                doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", p.Diagnosis, false, false);
                doc.Range.Replace("Stent đmv, suy tim, THA", p.History, false, false);

                string bloodStr = string.Format("Nhóm máu: {0}                 Dự trù máu: {1} (ml)", p.BloodGroup, p.BloodReserve);
                doc.Range.Replace("Nhóm máu:................ Dự trù máu........................................ (ml)", bloodStr, false, false);

                // 2. Điền chuẩn xác 9 thẻ <thay> theo từng đoạn ngữ cảnh
                foreach (Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
                {
                    string t = para.GetText();
                    if (!t.Contains("<thay>")) continue;

                    var prev = para.PreviousSibling as Paragraph;
                    string prevT = prev != null ? prev.GetText().Trim() : "";

                    if (t.StartsWith("Bệnh sử:"))
                    {
                        para.Range.Replace("<thay>", p.Course, false, false);
                    }
                    else if (t.StartsWith("Thời gian hội chẩn:"))
                    {
                        para.Range.Replace("..<thay>", " " + p.HoiChanTime, false, false);
                    }
                    else if (t.StartsWith("Tóm tắt tình trạng bệnh:"))
                    {
                        para.Range.Replace("<thay>", p.ExamSummary, false, false);
                    }
                    else if (prevT.Contains("Các xét nghiệm, chẩn đoán hình ảnh"))
                    {
                        para.Range.Replace("<thay>", p.FullCls, false, false);
                    }
                    else if (prevT.Contains("Phương pháp phẫu thuật"))
                    {
                        para.Range.Replace("<thay>", p.SurgeryMethod, false, false);
                    }
                    else if (t.Contains("Phương pháp vô cảm dự kiến:"))
                    {
                        para.Range.Replace("<thay>", p.Anesthesia, false, false);
                    }
                    else if (t.Contains("Phẫu  thuật  viên  chính:"))
                    {
                        para.Range.Replace("<thay>", p.Surgeon, false, false);
                    }
                    else if (t.Contains("Ngày, giờ phẫu thuật dự kiến:"))
                    {
                        para.Range.Replace(".....<thay>", " " + p.SurgeryTime, false, false);
                    }
                    else if (prevT.Contains("Các biến chứng, nguy cơ"))
                    {
                        para.Range.Replace("<thay>", p.Risks, false, false);
                    }
                }

                string safeName = p.PatientName.Trim().Replace(" ", "_");
                string fileNameDated = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
                string pathDated = Path.Combine(outDirDated, fileNameDated);
                string pathStd = Path.Combine(outDirStd, fileNameDated);

                doc.Save(pathDated);
                CleanDocxWatermark(pathDated);

                File.Copy(pathDated, pathStd, true);
                Console.WriteLine("  ✔ Đã xuất và làm sạch: " + fileNameDated);
            }

            Console.WriteLine(string.Format("\n🎉 XUẤT THÀNH CÔNG 100% {0} BIÊN BẢN PT-01 TẠI:\n📂 {1}", allList.Count, outDirDated));
        }

        static void CleanDocxWatermark(string docxPath)
        {
            try
            {
                using (ZipArchive zip = ZipFile.Open(docxPath, ZipArchiveMode.Update))
                {
                    var entry = zip.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        string xmlContent;
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            xmlContent = reader.ReadToEnd();
                        }

                        if (xmlContent.IndexOf("Evaluation Only. Created with Aspose.Words", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string cleanedXml = Regex.Replace(xmlContent, 
                                @"<w:p\b[^>]*>(?:(?!</w:p>).)*?Evaluation Only\. Created with Aspose\.Words.*?</w:p>", 
                                string.Empty, 
                                RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            if (cleanedXml.IndexOf("Evaluation Only", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                cleanedXml = cleanedXml.Replace("Evaluation Only. Created with Aspose.Words. Copyright 2003-2011 Aspose Pty Ltd.", "");
                            }

                            cleanedXml = Regex.Replace(cleanedXml, @"<!--\s*Generated by Aspose\.Words.*?-->", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            entry.Delete();
                            var newEntry = zip.CreateEntry("word/document.xml", CompressionLevel.Optimal);
                            using (var writer = new StreamWriter(newEntry.Open(), Encoding.UTF8))
                            {
                                writer.Write(cleanedXml);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        static List<PatientProfile> BuildProfiles()
        {
            var list = new List<PatientProfile>();

            // =========================================================================
            // PHÒNG MỔ 5 (CS) — 6 CA
            // =========================================================================

            // CA 01: LÊ THIỆU TƯỜNG (82t) - PTV: BS Nguyễn Thế Hoành
            list.Add(new PatientProfile {
                Stt = 1,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0004073176",
                PatientName = "LÊ THIỆU TƯỜNG",
                Dob = "19/02/1944",
                Gender = "Nam",
                Address = "TDP Minh Tân 10, Phường Yên Bái, Lào Cai",
                InTime = "08:46 ngày 23/09/2026",
                Diagnosis = "Loãng xương nặng - Xẹp cấp thân đốt sống D11 - Tăng huyết áp",
                History = "Tăng huyết áp điều trị đều; Loãng xương tuổi già nặng.",
                Course = "Bệnh nhân nam 82 tuổi, tiền sử THA, loãng xương. Cách vào viện 1 tuần, sau khi với đồ nặng, xuất hiện đau dữ dội vùng cột sống ngực thấp (ngang mức D11), đau nhói khi ho, hắt hơi và thay đổi tư thế, đi lại hạn chế. Được gia đình đưa vào BV Bạch Mai khám, chụp X-quang và MRI cột sống ngực phát hiện xẹp cấp D11 phù tủy xương do loãng xương nặng -> Nhập Khoa 57 chỉ định phẫu thuật bơm xi măng sinh học tạo hình thân đốt sống.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 135/80 mmHg, T° 36.6°C, SpO2 97%. Thể trạng người già 82t, tiếp xúc tốt. Cột sống ngực: Ấn đau chói mỏm gai sau và cạnh sống D11; cơ cạnh sống co cứng nhẹ; hạn chế cúi xoay người do đau; không có dấu hiệu chèn ép tủy ngực (cơ lực 2 chi dưới 5/5, phản xạ gân xương bình thường, đại tiểu tiện tự chủ). Tim mạch: T1, T2 rõ, không tiếng thổi; Phổi: rì rào phế nang êm dịu, không rale; Bụng mềm, gan lách không to.",
                FullCls = "- Công thức máu: WBC 4.81 G/L, HGB 107 g/L, PLT 212 G/L.\n- Đông máu: PT-INR 1.06, APTT 0.95, Fibrinogen 3.06 g/L.\n- Sinh hóa máu: Glucose 6.5 mmol/L, Ure 3.5 mmol/L, Creatinin 78 µmol/L, AST 25 U/L, ALT 14 U/mL, CRP 4.2 mg/L, Calci toàn phần 2.21 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- Tổng phân tích nước tiểu: Bình thường (10 thông số âm tính).\n- CĐHA: X-quang cột sống ngực và MRI cột sống ngực: Xẹp cấp thân đốt sống D11 mức độ vừa (giảm 30% chiều cao thân đốt), phù tủy xương cấp tính (tín hiệu giảm trên T1W, tăng mạnh trên STIR); không vỡ tường sau, không chèn ép ống sống; loãng xương nặng lan tỏa.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học thân đốt sống ngực D11 (Vertebroplasty / Balloon Kyphoplasty D11)",
                Anesthesia = "Tiền mê + Tê tại chỗ (hoặc Gây mê nội khí quản)",
                Surgeon = "BS Nguyễn Thế Hoành",
                SurgeryTime = "08 giờ 00 phút, ngày 26 tháng 09 năm 2026",
                Risks = "Tràn xi măng sinh học vào tĩnh mạch gây thuyên tắc phổi, tràn xi măng vào ống sống gây chèn ép tủy/liệt hai chi dưới, tụt huyết áp/sốc phản vệ do monomer xi măng, tăng huyết áp kịch phát chu phẫu, nhiễm trùng vết mổ, xẹp đốt sống lân cận do loãng xương nền nặng."
            });

            // CA 02: NGÔ THỊ THANH (77t) - PTV: BS Lê Văn Luân
            list.Add(new PatientProfile {
                Stt = 2,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0004076519",
                PatientName = "NGÔ THỊ THANH",
                Dob = "28/12/1949",
                Gender = "Nữ",
                Address = "Phường Hoàng Mai, Thành phố Hà Nội",
                InTime = "12:06 ngày 24/09/2026",
                Diagnosis = "Gãy kín 1/3 giữa xương cánh tay phải - Theo dõi liệt thần kinh quay / Tăng huyết áp - ĐTĐ típ 2",
                History = "Tăng huyết áp điều trị đều; Đái tháo đường típ 2 dùng thuốc.",
                Course = "Bệnh nhân nữ 77 tuổi, tiền sử THA, ĐTĐ tip 2. Ngày 24/09/2026 bị trượt chân ngã đập vùng cánh tay phải xuống nền cứng, sau ngã đau chói dữ dội, mất cơ năng cánh tay phải, cẳng bàn tay rủ nhẹ, tê bì mu tay. Được đưa vào BV Bạch Mai cấp cứu, chụp X-quang chẩn đoán gãy kín 1/3 giữa xương cánh tay phải, theo dõi tổn thương thần kinh quay -> Nhập viện Khoa 57 hội chẩn chỉ định phẫu thuật kết hợp xương nẹp vít và thăm dò giải phóng thần kinh quay.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 140/85 mmHg, T° 36.7°C, SpO2 98%. BN tỉnh, thể trạng trung bình. Chi trên phải: Sưng nề, bầm tím 1/3 giữa cánh tay phải, ấn có điểm đau chói, lạo xạo xương và cử động bất thường; Cổ bàn tay có dấu hiệu rủ ('bàn tay rủ cổ cò' nhẹ), giảm cảm giác vùng khoang gian đốt 1-2 mu tay, duỗi cổ tay và ngón tay yếu (cơ lực 3/5); mạch quay và mạch trụ bắt rõ. Tim phổi không rale; bụng mềm.",
                FullCls = "- Công thức máu: WBC 9.84 G/L, HGB 125 g/L, PLT 215 G/L.\n- Đông máu: PT-INR 1.07, APTT 0.83, Fibrinogen 3.29 g/L.\n- Sinh hóa máu: Glucose 10.9 mmol/L, Ure 10.7 mmol/L, Creatinin 128 µmol/L, AST 62 U/L, ALT 47 U/mL, Điện giải Na 137, K 4.1, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: A Rh(+).\n- TPT nước tiểu: Protein (-), Glucose (+).\n- CĐHA: X-quang xương cánh tay phải thẳng nghiêng: Hình ảnh gãy kín chéo vát 1/3 giữa thân xương cánh tay phải di lệch gập góc và chồng ngắn 1.5 cm; X-quang ngực thẳng: Quai ĐMC vồng, nhu mô phổi sáng.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật kết hợp xương cánh tay phải bằng nẹp vít AO + Thăm dò giải phóng thần kinh quay",
                Anesthesia = "Gây mê nội khí quản (hoặc Tê đám rối thần kinh cánh tay)",
                Surgeon = "BS Lê Văn Luân",
                SurgeryTime = "09 giờ 30 phút, ngày 26 tháng 09 năm 2026",
                Risks = "Tổn thương làm đứt hoặc dập thần kinh quay trong phẫu tích, chảy máu chu phẫu, tổn thương động mạch cánh tay sâu, chậm liền xương, khớp giả, nhiễm trùng vết mổ sâu, biến chứng tăng đường huyết và tăng huyết áp dao động chu phẫu."
            });

            // CA 03: TRẦN THỊ NGỌC DIỆP (61t) - PTV: BS Trịnh Minh Đức
            list.Add(new PatientProfile {
                Stt = 3,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0003000402",
                PatientName = "TRẦN THỊ NGỌC DIỆP",
                Dob = "18/08/1965",
                Gender = "Nữ",
                Address = "Phường Lê Chân, Thành phố Hải Phòng",
                InTime = "09:59 ngày 18/09/2026",
                Diagnosis = "Xẹp cấp thân đốt sống L2 / Thoát vị đĩa đệm L3-L4-L5-S1 - Lupus ban đỏ hệ thống - Theo dõi Lơ-xê-mi kinh dòng mono",
                History = "Lupus ban đỏ hệ thống điều trị Medrol kéo dài; Theo dõi bệnh lý huyết học dòng mono; Thiếu máu mạn; Tăng huyết áp thứ phát.",
                Course = "Bệnh nhân nữ 61 tuổi, tiền sử Lupus ban đỏ hệ thống dùng Corticoid nhiều năm, theo dõi Lơ-xê-mi mono. Khoảng 2 tuần nay đau thắt lưng dữ dội tăng dần, không cúi gập được, ngồi đau chói vùng thắt lưng trên (L2), đau lan nhẹ xuống mông. Đã điều trị nội khoa tại Hải Phòng không đỡ, nhập Viện Bạch Mai khám, chụp MRI phát hiện xẹp cấp thân đốt sống L2 trên nền loãng xương thứ phát do Corticoid, kèm TVĐĐ L3-S1 -> Hội chẩn chuyên khoa chỉ định phẫu thuật bơm xi măng sinh học L2.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 82 ck/p, HA 130/80 mmHg, T° 36.8°C, SpO2 98%. Da niêm mạc nhợt nhẹ, kiểu hình cushing do thuốc. Cột sống thắt lưng: Ấn đau chói gai sau L2; co cứng cơ cạnh sống thắt lưng 2 bên; hạn chế vận động cột sống; Lasègue 2 bên 70 độ; cơ lực 2 chi dưới 4/5 do đau; phản xạ gân xương bánh chè bình thường; đại tiểu tiện tự chủ. Khám tim phổi bình thường; gan lách không to.",
                FullCls = "- Công thức máu: WBC 11.2 G/L (tăng nhẹ mono), HGB 64 g/L (thiếu máu mức độ vừa/nặng, đã truyền máu nâng Hb lên > 95 g/L), PLT 115 G/L.\n- Đông máu: PT-INR 1.45, APTT 1.33, Fibrinogen 2.22 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 7.2 mmol/L, Creatinin 140 µmol/L, AST 18 U/L, ALT 5 U/mL, Bổ thể C3 0.68 g/L (giảm nhẹ).\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Protein niệu vết, hồng cầu niệu (-).\n- CĐHA: MRI cột sống thắt lưng: Hình ảnh xẹp cấp thân đốt sống L2 (phù tủy xương STIR tăng mạnh); thoái hóa thoát vị đĩa đệm tầng L3/4, L4/5, L5/S1 lồi ra sau chèn ép rễ nhẹ; loãng xương thứ phát do Corticoid.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Bơm xi măng sinh học tạo hình thân đốt sống L2 (Vertebroplasty L2)",
                Anesthesia = "Tiền mê + Tê tại chỗ (theo dõi sát huyết động)",
                Surgeon = "BS Trịnh Minh Đức",
                SurgeryTime = "11:00 ngày 26 tháng 09 năm 2026",
                Risks = "Nguy cơ chảy máu và bầm tụ máu tại chỗ tiêm do rối loạn đông máu/tiểu cầu thấp, rò xi măng vào ống sống, nguy cơ nhiễm trùng vết mổ cơ hội trên nền suy giảm miễn dịch do Lupus và Corticoid kéo dài, suy thận cấp tăng nặng chu phẫu."
            });

            // CA 04: LÊ TRÍ QUYẾN (59t) - PTV: BS Trịnh Minh Đức
            list.Add(new PatientProfile {
                Stt = 4,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0004074524",
                PatientName = "LÊ TRÍ QUYẾN",
                Dob = "20/09/1967",
                Gender = "Nam",
                Address = "Xóm 3, Dạ Trạch, Huyện Khoái Châu, Hưng Yên",
                InTime = "21:43 ngày 23/09/2026",
                Diagnosis = "Chấn thương cột sống: lún vỡ thân đốt sống L2 - vỡ xương gót Trái",
                History = "Khỏe mạnh, không có bệnh lý mạn tính.",
                Course = "Bệnh nhân nam 59 tuổi, không có tiền sử bệnh nền. Ngày 23/09/2026 ngã từ giàn giáo cao khoảng 2.5m đập gót chân và mông xuống nền cứng. Sau ngã đau chói dữ dội vùng thắt lưng và gót chân trái, không đứng dậy được. Được sơ cứu chuyển cấp cứu BV Bạch Mai, chụp CT cột sống phát hiện lún vỡ thân đốt sống L2 mất vững (AO Spine type A3), kèm vỡ xương gót chân trái -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật cố định cột sống thắt lưng ít xâm lấn (nẹp vít qua da).",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 99%. BN tỉnh, tiếp xúc tốt. Cột sống thắt lưng: Ấn đau chói vùng gai sau L2, co cứng cơ lưng rõ rệt, bầm tím nhẹ dưới da; Khám thần kinh: Cơ lực 2 chân 5/5, cảm giác nông sâu bình thường, phản xạ gân xương bánh chè gót bình thường, đại tiểu tiện tự chủ (Frankel E). Cổ chân - gót trái: Sưng nề bầm tím vùng gót, đau chói, cử động khớp sên gót hạn chế, mạch mu chân bắt rõ. Tim phổi bình thường.",
                FullCls = "- Công thức máu: WBC 8.4 G/L, HGB 148 g/L, PLT 247 G/L.\n- Đông máu: PT-INR 0.97, APTT 0.99, Fibrinogen 3.48 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 4.0 mmol/L, Creatinin 88 µmol/L, AST 61 U/L, ALT 28 U/mL, Điện giải đồ bình thường.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: AB Rh(+).\n- TPT nước tiểu: Bình thường.\n- CĐHA: CLVT cột sống thắt lưng: Hình ảnh vỡ vụn lún thân đốt sống L2, giảm chiều cao thân đốt > 40%, vỡ một phần tường sau nhưng chưa chèn ép nặng ống sống (mất vững cơ học); X-quang xương gót trái: Vỡ xương gót trái ít di lệch.",
                BloodGroup = "AB Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật cố định cột sống thắt lưng ít xâm lấn qua da (MIS Pedicle Screws L1-L3) nắn chỉnh gù vỡ L2",
                Anesthesia = "Gây mê nội khí quản",
                Surgeon = "BS Trịnh Minh Đức",
                SurgeryTime = "12:30 ngày 26 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, bắt vít lệch vào ống sống gây tổn thương rễ thần kinh/màng cứng, rò dịch não tủy, tụ máu vết mổ chèn ép thần kinh muộn, nhiễm trùng vết mổ sâu, gãy vít hoặc lỏng vít nẹp xốp xương."
            });

            // CA 05: CHU VĂN TUẤN (63t) - PTV: BS Đặng Nhật Quang
            list.Add(new PatientProfile {
                Stt = 5,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0004055395",
                PatientName = "CHU VĂN TUẤN",
                Dob = "04/04/1963",
                Gender = "Nam",
                Address = "Phường Thái Hòa, Tỉnh Nghệ An",
                InTime = "07:42 ngày 18/09/2026",
                Diagnosis = "Hội chứng ống cổ tay hai bên mức độ nặng / Bệnh Gút vô căn nhiều vị trí",
                History = "Gút mạn tính nhiều năm có hạt tophi rải rác; Tê bì 2 bàn tay kéo dài.",
                Course = "Bệnh nhân nam 63 tuổi, tiền sử bệnh Gút mạn tính. Khoảng 1 năm nay xuất hiện tê bì dị cảm các ngón 1, 2, 3 và nửa ngón 4 hai bàn tay, đau tăng về đêm khiến mất ngủ, phải thức dậy vẩy tay mới đỡ đau; gần đây teo cơ ô mô cái nhẹ hai bên, cầm nắm đồ vật hay rơi. Đi khám được đo điện cơ chẩn đoán Hội chứng ống cổ tay 2 bên mức độ nặng (tổn thương sợi trục và myelin dây thần kinh giữa) -> Nhập viện chỉ định phẫu thuật giải phóng thần kinh giữa.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng tốt. Hai bàn tay: Dấu hiệu Tinel (+) rõ tại ống cổ tay 2 bên; Nghiệm pháp Phalen (+) sau 30 giây; Teo nhẹ cơ ô mô cái 2 bên, cơ lực đối chiếu ngón cái 4/5; Giảm cảm giác nông ngón 1, 2, 3 bàn tay 2 bên; Mạch quay bắt rõ; Các khớp cổ tay có hạt tophi nhỏ không viêm loét. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 11.4 G/L, HGB 152 g/L, PLT 289 G/L.\n- Đông máu: PT-INR 0.82, APTT 0.99, Fibrinogen 3.92 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 4.2 mmol/L, Creatinin 85 µmol/L, Acid Uric 480 µmol/L (tăng), AST 23 U/L, ALT 24 U/mL.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- Điện tâm đồ: Nhịp xoang đều 74 ck/p; X-quang ngực thẳng: Bình thường; Siêu âm ổ bụng: Gan nhiễm mỡ nhẹ.\n- Thăm dò chức năng: Điện cơ chi trên: Giảm nặng tốc độ dẫn truyền vận động và cảm giác dây thần kinh giữa đoạn qua cổ tay 2 bên, thời gian tiềm tàng vận động kéo dài (Hội chứng ống cổ tay 2 bên mức độ nặng).",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật cắt dây chằng vòng cổ tay, giải phóng thần kinh giữa hai bên",
                Anesthesia = "Gây tê tại chỗ (hoặc Tiền mê + Tê đám rối)",
                Surgeon = "BS Đặng Nhật Quang",
                SurgeryTime = "14:00 ngày 26 tháng 09 năm 2026",
                Risks = "Tổn thương nhánh quặt ngược vận động ngón cái thần kinh giữa, tổn thương cung mạch gan tay nông, tụ máu vết mổ, sẹo đau phì đại vùng gan tay, hội chứng đau vùng phức hợp (CRPS), nhiễm trùng vết mổ nông."
            });

            // CA 06: NGUYỄN HỮU TÀI (22t) - PTV: BS Đặng Nhật Quang
            list.Add(new PatientProfile {
                Stt = 6,
                Room = "Phòng mổ 5 (CS)",
                PatientCode = "0001923208",
                PatientName = "NGUYỄN HỮU TÀI",
                Dob = "23/02/2004",
                Gender = "Nam",
                Address = "Xã Kim Anh, Thành phố Hà Nội",
                InTime = "05:00 ngày 24/09/2026",
                Diagnosis = "Nang hoạt dịch mu cổ tay trái",
                History = "Khỏe mạnh, thanh niên trẻ 22 tuổi.",
                Course = "Bệnh nhân nam 22 tuổi, không có tiền sử bệnh mạn tính. Khoảng 4 tháng nay phát hiện một khối tròn vùng mu cổ tay trái, khối to dần, ấn tức nhẹ khi chống tay hoặc gấp cổ tay tối đa, ảnh hưởng sinh hoạt và thẩm mỹ. Khám chuyên khoa chẩn đoán nang hoạt dịch mu tay trái -> Chỉ định phẫu thuật cắt trọn u nang bao hoạt dịch khớp cổ tay.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 115/70 mmHg, T° 36.5°C, SpO2 99%. Thể trạng thanh niên khỏe mạnh. Cổ tay trái: Vùng mu cổ tay có khối tròn kích thước khoảng 2x2.5 cm, ranh giới rõ, bề mặt nhẵn, mật độ căng chắc, không nóng đỏ đau, ấn tức nhẹ, di động theo gân duỗi ít; cử động gấp duỗi cổ tay bình thường; mạch quay và cảm giác các ngón tay bình thường. Tim phổi không rale, bụng mềm.",
                FullCls = "- Công thức máu: WBC 4.6 G/L, HGB 153 g/L, PLT 253 G/L.\n- Đông máu: PT-INR 1.11, APTT 1.25, Fibrinogen 2.15 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 3.8 mmol/L, Creatinin 81 µmol/L, AST 19 U/L, ALT 16 U/mL.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Tim phổi bình thường.\n- CĐHA: Siêu âm phần mềm cổ tay trái: Hình ảnh khối nang giảm âm đồng nhất kích thước 22x18 mm tại mu cổ tay, có cuống thông với bao khớp quay cổ tay, thành mỏng, dịch trong.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật lấy bỏ trọn u bao hoạt dịch mu cổ tay trái đến tận cuống bao khớp",
                Anesthesia = "Gây tê tại chỗ",
                Surgeon = "BS Đặng Nhật Quang",
                SurgeryTime = "15:00 ngày 26 tháng 09 năm 2026",
                Risks = "Tổn thương nhánh nông thần kinh quay, đứt hoặc dập gân duỗi cổ tay/duỗi ngón, rách bao khớp rộng, chảy máu tụ dịch mu tay, nhiễm trùng vết mổ, tái phát nang hoạt dịch sau mổ nếu không bóc hết cuống."
            });

            // =========================================================================
            // PHÒNG MỔ 2 (CTCH) — 6 CA
            // =========================================================================

            // CA 07: NGUYỄN THỊ XUÂN (73t) - PTV: BS. Hà Đức Cường
            list.Add(new PatientProfile {
                Stt = 7,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0004072758",
                PatientName = "NGUYỄN THỊ XUÂN",
                Dob = "20/12/1953",
                Gender = "Nữ",
                Address = "Xã Gia Hanh, Tỉnh Hà Tĩnh",
                InTime = "07:31 ngày 23/09/2026",
                Diagnosis = "Thoái hóa khớp gối hai bên giai đoạn IV - Biến dạng vẹo trong",
                History = "Thoái hóa khớp gối nhiều năm, điều trị tiêm khớp nội khoa nhiều đợt.",
                Course = "Bệnh nhân nữ 73 tuổi, tiền sử thoái hóa khớp gối 2 bên hơn 6 năm. Đau nhức khớp gối 2 bên liên tục tăng dần, đau nhiều khi đứng và đi bộ, cứng khớp buổi sáng, gần đây biến dạng chân vẹo trong (chân vòng kiềng), đi lại rất khó khăn, đã dùng NSAID và tiêm acid hyaluronic không còn đáp ứng. Chụp X-quang chẩn đoán thoái hóa khớp gối 2 bên độ IV (Kellgren-Lawrence) -> Nhập viện chỉ định phẫu thuật thay khớp gối toàn phần.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. Thể trạng trung bình. Hai khớp gối: Sưng biến dạng vẹo trong (Varus 10 độ), dày bao khớp, có tiếng lạo xạo xương khớp khi vận động; ấn đau nhiều diện khớp trong 2 bên; biên độ vận động khớp gối gập 90 độ, duỗi thiếu 10 độ; dấu hiệu ngăn kéo âm tính; mạch mu chân và ống gót bắt rõ 2 bên; cảm giác bàn chân bình thường. Tim phổi không tiếng bệnh lý, bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.16 G/L, HGB 148 g/L, PLT 283 G/L.\n- Đông máu: PT-INR 0.99, APTT 0.98, Fibrinogen 3.74 g/L.\n- Sinh hóa máu: Glucose 4.9 mmol/L, Ure 6.3 mmol/L, Creatinin 73 µmol/L, AST 19 U/L, ALT 14 U/mL.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Bình thường; Điện tim: Nhịp xoang đều 76 ck/p; Siêu âm tim: Hở van 2 lá nhẹ, chức năng tâm thu thất trái EF 64%.\n- CĐHA: X-quang khớp gối thẳng nghiêng 2 bên: Hẹp khe khớp gối trong hoàn toàn, đặc xương dưới sụn, gai xương lớn quanh mâm chày và lồi cầu đùi, biến dạng trục vẹo trong (Thoái hóa khớp gối độ IV).",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay khớp gối toàn phần (Total Knee Arthroplasty - TKA)",
                Anesthesia = "Gây tê tủy sống (hoặc Tê ngoài màng cứng giảm đau sau mổ)",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "08 giờ 00 phút, ngày 26 tháng 09 năm 2026",
                Risks = "Mất máu chu phẫu, tổn thương động mạch khoeo/thần kinh hông khoeo ngoài, huyết khối tĩnh mạch sâu chi dưới (DVT) và thuyên tắc phổi (PE), nhiễm trùng khớp nhân tạo sâu (PJI), lỏng khớp nhân tạo, cứng khớp gối sau mổ."
            });

            // CA 08: LÊ THỊ BÌNH (72t) - PTV: BS. Hà Đức Cường
            list.Add(new PatientProfile {
                Stt = 8,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0004008409",
                PatientName = "LÊ THỊ BÌNH",
                Dob = "12/07/1954",
                Gender = "Nữ",
                Address = "Xã Trung Chính, Tỉnh Thanh Hóa",
                InTime = "16:31 ngày 21/09/2026",
                Diagnosis = "Thoái hóa khớp gối phải giai đoạn IV - Tăng huyết áp",
                History = "Tăng huyết áp điều trị đều bằng Coversyl; Thoái hóa khớp gối nhiều năm.",
                Course = "Bệnh nhân nữ 72 tuổi, tiền sử THA điều trị ổn định. Đau khớp gối phải kéo dài nhiều năm, đau nhức tăng khi đi lại và leo cầu thang, khớp gối biến dạng vẹo trục, biên độ vận động giảm dần. Bệnh nhân đã điều trị nội khoa nhiều đợt không thuyên giảm, đi lại phải chống gậy. Vào BV Bạch Mai chụp X-quang khớp gối chẩn đoán thoái hóa khớp gối phải giai đoạn IV -> Chỉ định phẫu thuật thay khớp gối toàn phần bên phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 135/85 mmHg, T° 36.5°C, SpO2 98%. BN tỉnh, thể trạng béo vừa (BMI 24.5). Khớp gối phải: Biến dạng trục vẹo trong, sưng nề nhẹ không nóng đỏ, ấn đau chói khe khớp bên trong; dấu hiệu bào khớp (+); biên độ vận động gập 95 độ, duỗi thiếu 5 độ; dây chằng bên vững; mạch mu chân và chày sau bắt rõ; cảm giác bàn chân bình thường. Tim mạch: T1, T2 rõ, không tiếng thổi; Phổi trong.",
                FullCls = "- Công thức máu: WBC 9.38 G/L, HGB 118 g/L, PLT 392 G/L.\n- Đông máu: PT-INR 0.99, APTT 0.95, Fibrinogen 3.26 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 7.1 mmol/L, Creatinin 67 µmol/L, AST 23 U/L, ALT 14 U/mL, Albumin 42 g/L, Điện giải bình thường.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Cung ĐMC vồng nhẹ, phổi sáng; Điện tâm đồ: Nhịp xoang 75 ck/p, dày thất trái nhẹ.\n- CĐHA: X-quang khớp gối phải: Hẹp nặng khe khớp gian lồi cầu trong, đặc xương xơ hóa diện dưới sụn, chồi gai xương rìa khớp lớn (Thoái hóa khớp gối giai đoạn IV).",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay khớp gối toàn phần bên Phải (TKA)",
                Anesthesia = "Gây tê tủy sống",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "09:30 ngày 26 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, huyết khối tĩnh mạch sâu (DVT), thuyên tắc mạch phổi, nhiễm trùng khớp gối nhân tạo, lỏng implant, cứng khớp sau phẫu thuật, biến cố tăng huyết áp kịch phát trong mổ."
            });

            // CA 09: TRẦN THỊ THO (75t) - PTV: BS Đặng Hoàng Giang
            list.Add(new PatientProfile {
                Stt = 9,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0004071508",
                PatientName = "TRẦN THỊ THO",
                Dob = "31/07/1951",
                Gender = "Nữ",
                Address = "Tổ 1, Phường Cam Đường, Tỉnh Lào Cai",
                InTime = "00:08 ngày 23/09/2026",
                Diagnosis = "Gãy cổ xương đùi phải / Suy tủy xương - Suy tim - Tăng huyết áp - ĐTĐ típ 2",
                History = "Suy tủy xương điều trị huyết học; Suy tim EF 48%; Tăng huyết áp; Đái tháo đường típ 2.",
                Course = "Bệnh nhân nữ 75 tuổi, nhiều bệnh nền phức tạp (suy tủy xương, suy tim, THA, ĐTĐ tip 2). Ngày 22/09 bị ngã đập mông và háng phải xuống sàn cứng, sau ngã đau chói vùng háng phải, mất hoàn toàn cơ năng chân phải, không đứng dậy được. Chuyển tuyến BV Bạch Mai cấp cứu, chụp X-quang chẩn đoán gãy cổ xương đùi phải (Garden IV) -> Nhập Khoa 57 hội chẩn đa chuyên khoa (Tim mạch, Huyết học, Nội tiết) chỉ định phẫu thuật thay khớp háng bán phần.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 88 ck/p, HA 140/90 mmHg, T° 36.7°C, SpO2 96%. Da niêm mạc nhợt nhẹ, thể trạng gầy yếu. Chi dưới phải: Chân phải nằm xoay ngoài, ngắn chi khoảng 2 cm; ấn đau chói vùng tam giác Scarpa và mấu chuyển lớn xương đùi phải; bầm tím nhẹ vùng khớp háng; cử động khớp háng phải mất hoàn toàn do đau; mạch mu chân và chày sau bắt được; cảm giác ngọn chi bình thường. Tim: Nhịp tim nhanh đều, T3 nhẹ mỏm tim; Phổi rale ẩm rải rác đáy phổi; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.49 G/L, HGB 113 g/L, PLT 180 G/L (tiểu cầu vón, chức năng đông máu chấp nhận được).\n- Đông máu: PT-INR 1.26, APTT 0.74, Fibrinogen 2.44 g/L.\n- Sinh hóa máu: Glucose 13.8 mmol/L (đang chỉnh insulin liều chuẩn), Ure 11.2 mmol/L, Creatinin 94 µmol/L, AST 70 U/L, ALT 65 U/mL, Troponin Ths 18 ng/L, NT-proBNP 1450 pg/mL (suy tim ổn định).\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Glucose (+), Protein (-).\n- CĐHA: X-quang khung chậu và khớp háng phải: Hình ảnh gãy hoàn toàn cổ xương đùi phải, di lệch xoay ngoài và chồng ngắn (Garden IV); X-quang ngực: Bóng tim to, dày phế nang đáy phổi.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "500",
                SurgeryMethod = "Phẫu thuật thay khớp háng bán phần chuôi không xi măng bên Phải (Bipolar Hemiarthroplasty)",
                Anesthesia = "Gây tê tủy sống (hoặc Gây mê nội khí quản nếu huyết động không ổn định)",
                Surgeon = "BS Đặng Hoàng Giang",
                SurgeryTime = "11:00 ngày 26 tháng 09 năm 2026",
                Risks = "Nguy cơ rất cao suy tim cấp chu phẫu, tụt huyết áp kéo dài, chảy máu khó cầm do bệnh lý suy tủy, trật khớp háng nhân tạo, thuyên tắc mỡ/thuyên tắc phổi, nhiễm trùng vết mổ sâu, biến cố hạ/tăng đường huyết kịch phát."
            });

            // CA 10: NGUYỄN THỊ NHUNG (55t) - PTV: BS Đặng Hoàng Giang
            list.Add(new PatientProfile {
                Stt = 10,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0003987361",
                PatientName = "NGUYỄN THỊ NHUNG",
                Dob = "14/06/1971",
                Gender = "Nữ",
                Address = "Xã Tiên Du, Tỉnh Bắc Ninh",
                InTime = "07:50 ngày 24/09/2026",
                Diagnosis = "Hội chứng chóp xoay giai đoạn III - Rách hoàn toàn gân trên gai vai phải / Suy giáp sau can thiệp",
                History = "Suy giáp đang dùng Levothyroxin; Viêm loét dạ dày.",
                Course = "Bệnh nhân nữ 55 tuổi, tiền sử suy giáp. Đau khớp vai phải âm ỉ kéo dài hơn 6 tháng, đau tăng nhiều khi giơ tay lên cao hoặc với ra sau lưng, đau về đêm không nằm nghiêng bên phải được, lực giạng vai yếu dần. Đã điều trị vật lý trị liệu và tiêm nội khớp không đỡ. Vào Viện Bạch Mai chụp MRI khớp vai phát hiện đứt hoàn toàn gân trên gai (chóp xoay độ III) -> Chỉ định phẫu thuật nội soi khâu phục hồi gân chóp xoay vai phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. Thể trạng trung bình, tiếp xúc tốt. Khớp vai phải: Teo nhẹ cơ trên gai và dưới gai vai phải; ấn đau chói điểm bám gân trên gai mấu động lớn; Nghiệm pháp Neer (+), Hawkins (+), Jobe (+) cơ lực 3/5; Hạn chế chủ động động tác giạng và xoay ngoài cánh tay, vận động thụ động gần như bình thường; mạch quay và cảm giác bàn tay tốt. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 4.94 G/L, HGB 131 g/L, PLT 227 G/L.\n- Đông máu: PT-INR 1.03, APTT 0.95, Fibrinogen 3.58 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 3.3 mmol/L, Creatinin 69 µmol/L, AST 28 U/L, ALT 32 U/mL.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Bình thường; Điện tim: Nhịp xoang 72 ck/p.\n- CĐHA: MRI khớp vai phải: Hình ảnh rách hoàn toàn bề dày gân trên gai kích thước khoảng 1.5 cm, đầu gân co rút nhẹ mức độ Patte I; thoái hóa mỏm cùng vai gai xương chèn ép khoang dưới mỏm cùng; không teo mỡ cơ nặng (Goutallier độ I).",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật nội soi khâu phục hồi chóp xoay vai phải bằng neo sinh học (Suture Anchor)",
                Anesthesia = "Gây mê nội khí quản + Tê đám rối thần kinh cánh tay",
                Surgeon = "BS Đặng Hoàng Giang",
                SurgeryTime = "12:30 ngày 26 tháng 09 năm 2026",
                Risks = "Tổn thương thần kinh nách hoặc thần kinh trên vai trong phẫu thuật nội soi, chảy máu vào khớp vai, tụt neo sinh học, đứt rách tái phát gân chóp xoay sau mổ, cứng khớp vai thứ phát, nhiễm trùng khoang khớp vai."
            });

            // CA 11: NGUYỄN THỊ KIỀU TRANG (22t) - PTV: BS Đặng Hoàng Giang
            list.Add(new PatientProfile {
                Stt = 11,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0004077425",
                PatientName = "NGUYỄN THỊ KIỀU TRANG",
                Dob = "25/09/2004",
                Gender = "Nữ",
                Address = "Xã Hải Xuân, Tỉnh Ninh Bình",
                InTime = "19:59 ngày 24/09/2026",
                Diagnosis = "Gãy kín mâm chày ngoài gối trái (Schatzker II)",
                History = "Khỏe mạnh, thanh niên trẻ 22 tuổi.",
                Course = "Bệnh nhân nữ 22 tuổi, không có tiền sử bệnh lý. Chiều 24/09/2026 bị tai nạn giao thông xe máy va chạm, ngã đập trực tiếp khớp gối trái xuống đường. Sau tai nạn khớp gối trái sưng nề biến dạng, tràn dịch máu khớp gối dữ dội, mất hoàn toàn cơ năng vận động chân trái. Được đưa vào BV Bạch Mai cấp cứu, chụp X-quang và CT-scanner khớp gối chẩn đoán gãy mâm chày ngoài gối trái lún sụn khớp (Schatzker II) -> Nhập viện chỉ định phẫu thuật kết hợp xương mâm chày trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 110/70 mmHg, T° 36.5°C, SpO2 99%. Thể trạng thanh niên tốt. Khớp gối trái: Sưng nề to, bầm tím lan rộng, dấu hiệu bập bềnh xương bánh chè (+), tràn dịch máu trong khớp; ấn đau chói bờ ngoài mâm chày trái; cử động khớp gối hạn chế tối đa do đau; cẳng chân không có dấu hiệu chèn ép khoang (bắp chân mềm, cử động ngón chân tốt); mạch chày sau và mu chân bắt rõ; cảm giác mu chân bình thường. Tim phổi không rale, bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.54 G/L, HGB 137 g/L, PLT 198 G/L.\n- Đông máu: PT-INR 1.11, APTT 0.91, Fibrinogen 3.32 g/L.\n- Sinh hóa máu: Glucose 3.9 mmol/L, Ure 3.7 mmol/L, Creatinin 67 µmol/L, AST 25 U/L, ALT 17 U/mL.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: AB Rh(+).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Bình thường; Điện tim: Nhịp xoang 78 ck/p.\n- CĐHA: Chụp CT-scanner khớp gối trái: Hình ảnh gãy vỡ lún mâm chày ngoài khớp gối trái, lún sụn khớp mâm chày sâu khoảng 4mm, tách rời bờ ngoài mâm chày (Phân loại Schatzker II); không tổn thương mạch máu lớn quanh khoeo.",
                BloodGroup = "AB Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật nâng diện lún mâm chày, ghép xương nhân tạo và kết hợp xương nẹp vít mâm chày ngoài gối trái",
                Anesthesia = "Gây tê tủy sống",
                Surgeon = "BS Đặng Hoàng Giang",
                SurgeryTime = "14:00 ngày 26 tháng 09 năm 2026",
                Risks = "Tổn thương thần kinh hông khoeo ngoài gây liệt bàn chân rủ, hội chứng chèn ép khoang cẳng chân sau mổ, tổn thương rách sụn chêm/dây chằng chéo phối hợp, nhiễm trùng vết mổ sâu, cứng khớp gối hoặc thoái hóa khớp thứ phát sau chấn thương."
            });

            // CA 12: NGUYỄN THỊ HÒA (85t) - PTV: BS Lê Văn Luân
            list.Add(new PatientProfile {
                Stt = 12,
                Room = "Phòng mổ 2 (CTCH)",
                PatientCode = "0004076916",
                PatientName = "NGUYỄN THỊ HÒA",
                Dob = "08/03/1941",
                Gender = "Nữ",
                Address = "Thôn Thạch Đồng, Xã Đào Xá, Tỉnh Phú Thọ",
                InTime = "15:37 ngày 24/09/2026",
                Diagnosis = "Gãy kín liên mấu chuyển xương đùi trái / Stent mạch vành - Đang mang máy tạo nhịp tim - Tăng huyết áp",
                History = "Tăng huyết áp điều trị nhiều năm; Đã đặt Stent động mạch vành; Đang mang máy tạo nhịp tim vĩnh viễn.",
                Course = "Bệnh nhân nữ 85 tuổi, nhiều bệnh tim mạch nặng (stent ĐMV, máy tạo nhịp tim, THA). Ngày 24/09/2026 bị trượt chân ngã đập vùng hông đùi trái xuống nền cứng, sau ngã đau nhói dữ dội vùng háng đùi trái, mất hoàn toàn cơ năng chân trái. Được gia đình chuyển cấp cứu Viện Bạch Mai, chụp X-quang khung chậu chẩn đoán gãy liên mấu chuyển xương đùi trái -> Nhập Khoa 57 hội chẩn đa chuyên khoa (Tim mạch, Can thiệp tim mạch, GMHS) chỉ định phẫu thuật kết hợp xương đinh nội tủy nẹp nén.",
                HoiChanTime = "14 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 70 ck/p (nhịp máy tạo nhịp), HA 140/85 mmHg, T° 36.6°C, SpO2 97%. Cụ bà 85 tuổi thể trạng gầy, tiếp xúc chậm. Chi dưới trái: Ngắn chi trái khoảng 1.5 cm, bàn chân xoay ngoài; sưng nề bầm tím vùng mấu chuyển lớn xương đùi trái, ấn có điểm đau chói dữ dội; không thể nâng nhấc chân; mạch mu chân và chày sau bắt được; cảm giác ngọn chi bình thường. Khám lồng ngực: Vùng dưới đòn trái có sẹo mổ cấy máy tạo nhịp tim lành sẹo; Tim: Tiếng T1, T2 rõ, nhịp tim theo máy tạo nhịp đều 70 ck/p; Phổi rale ẩm rải rác 2 đáy phổi; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.66 G/L, HGB 91 g/L (thiếu máu mức độ vừa, dự trù truyền khối hồng cầu), PLT 315 G/L.\n- Đông máu: Đã làm phản ứng hòa hợp và định nhóm máu tại labo (PT, APTT, Fibrinogen trong giới hạn can thiệp).\n- Sinh hóa máu: Ure 11.9 mmol/L, Creatinin 92 µmol/L, Glucose 6.2 mmol/L, AST 34 U/L, ALT 23 U/mL, Điện giải Na 136, K 4.2, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: Đang hòa hợp mẫu máu (Nhóm máu O Rh(+)).\n- TPT nước tiểu: Bình thường; X-quang ngực thẳng: Bóng tim to, có hình ảnh dây điện cực và máy tạo nhịp tim dưới đòn trái; CĐHA: X-quang khung chậu và khớp háng trái: Hình ảnh gãy kín vùng liên mấu chuyển xương đùi trái nhiều mảnh di lệch (Phân loại Evans III).",
                BloodGroup = "O Rh(+)",
                BloodReserve = "500",
                SurgeryMethod = "Phẫu thuật kết hợp xương liên mấu chuyển xương đùi trái bằng đinh nội tủy chốt đầu (PFNA / Gamma Nail)",
                Anesthesia = "Gây tê tủy sống (hoặc Gây mê nội khí quản phối hợp chuyên khoa Tim mạch điều chỉnh máy tạo nhịp)",
                Surgeon = "BS Lê Văn Luân",
                SurgeryTime = "15:00 ngày 26 tháng 09 năm 2026",
                Risks = "Rối loạn nhịp tim / nhiễu máy tạo nhịp do dao điện phẫu thuật (cần chuyển chế độ máy tạo nhịp không đồng bộ trước rạch da), nguy cơ nhồi máu cơ tim chu phẫu, suy tim cấp, mất máu chu phẫu trên bệnh nhân cao tuổi (85t), tụt huyết áp kéo dài, huyết khối tĩnh mạch sâu/thuyên tắc phổi, nhiễm trùng vết mổ sâu."
            });

            return list;
        }
    }
}
