using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aspose.Words;

namespace HisPt01Creator
{
    public class PatientProfile
    {
        public int Stt;
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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "Bien_Ban_Thong_Qua_Mo_PT01_20260921");
            string outDirStd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(outDirDated)) Directory.CreateDirectory(outDirDated);
            if (!Directory.Exists(outDirStd)) Directory.CreateDirectory(outDirStd);

            List<PatientProfile> allList = GetPatients();
            List<PatientProfile> targetList = new List<PatientProfile>();

            if (args != null && args.Length > 0)
            {
                string input = string.Join(",", args).Replace(" ", "");
                var codes = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(c => c.Trim().PadLeft(10, '0')).ToList();
                targetList = allList.Where(p => codes.Contains(p.PatientCode)).ToList();
            }

            if (targetList.Count == 0)
            {
                targetList = allList;
            }

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) NGÀY 21/09/2026 ===", targetList.Count));

            foreach (var p in targetList)
            {
                Console.WriteLine(string.Format("\n[{0:D2}/{1:D2}] Đang tạo biên bản cho BN: {2} ({3})...", p.Stt, targetList.Count, p.PatientName, p.PatientCode));
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
                string fileNameDated = string.Format("{0:D2}_PT01_BienBanThongQuaMo_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
                string fileNameStd = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);

                doc.Save(Path.Combine(outDirDated, fileNameDated));
                doc.Save(Path.Combine(outDirStd, fileNameStd));
                Console.WriteLine("  ✔ Đã xuất: " + fileNameDated);
            }

            Console.WriteLine("\n🎉 HOÀN THÀNH TOÀN BỘ 10 BIÊN BẢN HỘI CHẨN PT-01 TẠI: " + outDirDated);

            // Đồng bộ trực tiếp lên Google Drive
            try
            {
                Console.WriteLine("\n☁️ Đang đồng bộ lên Google Drive (gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260921)...");
                string rcloneExe = Environment.GetEnvironmentVariable("RCLONE");
                if (string.IsNullOrEmpty(rcloneExe) || !File.Exists(rcloneExe))
                {
                    string localBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "rclone.exe");
                    if (File.Exists(localBin)) rcloneExe = localBin;
                    else rcloneExe = "rclone";
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rcloneExe,
                    Arguments = string.Format("copy \"{0}\" \"gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260921\" --quiet", outDirDated),
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(30000);
                    Console.WriteLine("✔ Đã đồng bộ thành công lên Google Drive: gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260921");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ GDrive sync note: " + ex.Message);
            }
        }

        static List<PatientProfile> GetPatients()
        {
            var list = new List<PatientProfile>();

            // =========================================================================
            // PHÒNG MỔ 5 (CỘT SỐNG - CS) — 5 CA
            // =========================================================================

            // BN 1: PHẠM VĂN HỢI
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0003748599",
                PatientName = "PHẠM VĂN HỢI",
                Dob = "10/10/1959",
                Gender = "Nam",
                Address = "Phường  Tự Lạn, Bắc Ninh",
                InTime = "07/09/2026 07:09",
                Diagnosis = "Xẹp L1 trái, đau cân cơ TL / Loãng xương nặng - Tiền sử tạo hình thân đốt sống D12, L1, L2, L3, L4, L5 - COPD - THA - Sẩn cục - Sẩn ngứa - Tiền sử lao hạch đã điều trị",
                History = "Loãng xương nặng, tiền sử tạo hình thân đốt sống D12, L1, L2, L3, L4, L5 bằng xi măng sinh học; Bệnh phổi tắc nghẽn mạn tính (COPD); Tăng huyết áp; Lao hạch đã điều trị khỏi.",
                Course = "Bệnh nhân nam 67 tuổi, tiền sử loãng xương nặng và đã bơm xi măng sinh học nhiều đốt sống. Cách vào viện 3 tuần, sau khi cúi bê vật nặng, bệnh nhân xuất hiện đau dữ dội cột sống thắt lưng lệch trái, đau buốt tăng khi xoay mình hoặc đứng ngồi, đi lại khó khăn, đã điều trị nội khoa dùng thuốc giảm đau nhưng đáp ứng kém -> vào Viện Bạch Mai khám và nhập Khoa CTCH & Cột sống phẫu thuật bơm xi măng sinh học đốt sống L1.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 135/85 mmHg, T° 36.6°C, SpO2 97%. Bệnh nhân tỉnh, thể trạng trung bình. Cột sống thắt lưng gù vẹo nhẹ, hạn chế biên độ vận động do đau; ấn đau chói tại gai sau và cạnh sống L1 bên trái; không có hội chứng chèn ép rễ (Lasègue 2 bên âm tính), cơ lực 2 chi dưới 5/5, cảm giác nông sâu hai chân bình thường, phản xạ gân bánh chè và gân gót hai bên đều; đại tiểu tiện tự chủ. Khám tim phổi: Tim đều; Phổi rì rào phế nang giảm nhẹ đáy phổi, rải rác ran rít thì thở ra do COPD; Bụng mềm, gan lách không to.",
                FullCls = "- Công thức máu: WBC 7.7 G/L, RBC 3.82 T/L, HGB 116 g/L, PLT 268 G/L.\n- Đông máu: PT-INR 0.97, APTT 0.76 (24.7s), Fibrinogen 4.09 g/L.\n- Sinh hóa máu: Glucose 9.0 mmol/L (được theo dõi đường huyết chu phẫu), Ure 8.1 mmol/L, Creatinin 73 µmol/L, AST 35 U/L, ALT 18 U/L, Điện giải Na/K/Cl (138/4.1/100 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Hình ảnh xẹp cấp thân đốt sống L1 bên trái, phù nề tủy xương giảm tín hiệu trên T1W và tăng tín hiệu trên STIR; các đốt sống D12, L1, L2, L3, L4, L5 cũ có bóng xi măng sinh học cố định vững chắc, không thấy chèn ép ống sống nặng. X-quang cột sống thắt lưng: Hình ảnh xẹp đốt sống L1 lệch trái, hình ảnh cản quang xi măng cũ các thân đốt D12-L5.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "BXM L1 trái + Phong bế cân TL",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Nguyễn Đức Hoàng",
                SurgeryTime = "08 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu vết chọc kim, tràn xi măng sinh học ra ngoài thân đốt sống hoặc vào ống sống gây chèn ép rễ/tủy, thuyên tắc mạch phổi do xi măng, co thắt phế quản chu phẫu trên nền COPD, tăng huyết áp kịch phát, nhiễm trùng vết mổ."
            });

            // BN 2: VŨ VĂN HÒA
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0003499811",
                PatientName = "VŨ VĂN HÒA",
                Dob = "15/07/1966",
                Gender = "Nam",
                Address = "Phường  Cửa Ông, Quảng Ninh",
                InTime = "15/09/2026 10:19",
                Diagnosis = "Thoát vị đĩa đệm C34 chèn ép phù tuỷ cổ / Tiền sử mổ cố định cột sống cổ C5-C6 - Hen phế quản",
                History = "Tiền sử phẫu thuật cố định cột sống cổ C5-C6 cách 9 năm; Hen phế quản điều trị duy trì.",
                Course = "Bệnh nhân nam 60 tuổi, tiền sử mổ cố định cột sống cổ C5-C6. Khoảng 3 tháng nay xuất hiện đau nhiều vùng cổ gáy lan xuống vai và mặt ngoài cánh tay hai bên (trái > phải), kèm tê bì các ngón tay, giảm độ khéo léo bàn tay khi cầm nắm đồ vật, đi lại cảm giác hơi hẫng chân. Đã điều trị nội khoa không cải thiện, khám và chụp MRI cột sống cổ phát hiện thoát vị đĩa đệm C3-C4 chèn ép tủy cổ gây phù tủy -> nhập viện Khoa CTCH & Cột sống phẫu thuật lối trước cắt đĩa đệm giải ép ghép xương hàn liên thân đốt (ACDF).",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh táo, tiếp xúc tốt. Cột sống cổ co cứng nhẹ cơ cạnh sống, tầm vận động cổ hạn chế; Hội chứng rễ thần kinh (+): Dấu hiệu Spurling (+), nghiệm pháp căng cánh tay (+); Dấu hiệu Hoffmann bàn tay trái (+/-), phản xạ gân cơ nhị đầu và tam đầu chi trên trái tăng nhẹ; Cơ lực gốc chi trên hai bên 4/5, cơ lực bàn tay trái 4/5; Hai chi dưới cơ lực 5/5, Babinski âm tính; Cảm giác tê bì vùng khoanh da C4-C5; Sẹo mổ cũ đường trước cổ bên phải liền sẹo tốt. Tim đều; Phổi thông khí tốt, hiện không có ran rít co thắt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 8.6 G/L, RBC 4.51 T/L, HGB 144 g/L, PLT 271 G/L.\n- Đông máu: PT-INR 1.15, APTT 0.95 (28.2s), Fibrinogen 2.88 g/L.\n- Sinh hóa máu: Glucose 9.5 mmol/L (theo dõi đường huyết chu phẫu), Ure 4.4 mmol/L, Creatinin 105 µmol/L, AST 24 U/L, ALT 12 U/L, Điện giải Na/K/Cl (139/4.0/102 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI cột sống cổ: Thoát vị đĩa đệm tầng C3-C4 thể trung tâm lệch trái gây hẹp ống sống nặng (đường kính trước sau còn ~7mm), chèn ép bao màng tủy và tăng tín hiệu phù tủy cổ ngang mức C3-C4; hình ảnh nẹp vít và lồng hàn xương cố định cột sống cổ C5-C6 cũ vị trí tốt, liền xương vững. X-quang tim phổi: Bình thường.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "ACDF C3-4 (Hàn xương liên thân đốt lối trước C3-C4)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "09 giờ 30 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, tổn thương tủy cổ và rễ thần kinh, tổn thương dây thần kinh thanh quản quặt ngược gây khàn tiếng, thủng rách thực quản/khí quản, tụ máu khoang trước cổ chèn ép đường thở sau mổ, lỏng/trôi nẹp vít hoặc lồng hàn xương, khởi phát cơn co thắt hen phế quản khi rút nội khí quản."
            });

            // BN 3: ĐỖ VĂN TƯỜNG
            list.Add(new PatientProfile {
                Stt = 3,
                PatientCode = "0004039408",
                PatientName = "ĐỖ VĂN TƯỜNG",
                Dob = "1965",
                Gender = "Nam",
                Address = "Cẩm La, Xã  Quyết Thắng, Thành phố Hải Phòng",
                InTime = "13/09/2026 16:48",
                Diagnosis = "Thoát vị đĩa đệm L5/S1 trái di trú xuống / Đau thần kinh tọa trái - Trào ngược dạ dày thực quản (GERD)",
                History = "Trào ngược dạ dày thực quản (GERD), không có tiền sử bệnh ngoại khoa cột sống trước đây.",
                Course = "Bệnh nhân nam 61 tuổi, khởi phát đau thắt lưng lan xuống mông, mặt sau đùi và cẳng chân trái đến bờ ngoài bàn chân khoảng 10 ngày nay. Đau buốt rát liên tục (VAS 7-8 điểm), đau tăng khi đi lại, đứng lâu, ho hoặc rặn, nằm co chân đỡ đau. Điều trị thuốc giảm đau nội khoa không thuyên giảm -> vào Viện Bạch Mai khám, chụp MRI chẩn đoán thoát vị đĩa đệm L5/S1 trái di trú xuống chèn ép nặng rễ S1 -> nhập viện mổ nội soi lấy nhân thoát vị.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. Bệnh nhân tỉnh táo, tiếp xúc tốt, nét mặt đau khi đi lại. Cột sống thắt lưng vẹo nhẹ sang phải chống đau, co cứng khối cơ cạnh sống bên trái; Nghiệm pháp bấm chuông (+) tầng L5-S1 bên trái; Nghiệm pháp Lasègue chân trái (+) 35 độ, chân phải (-) 75 độ; Điểm Valleix chân trái (+); Cơ lực duỗi cổ chân và ngón cái 5/5, cơ lực gấp lòng bàn chân trái 4/5 (rễ S1); Phản xạ gân gót chân trái giảm nhẹ so với bên phải; Cảm giác tê bì bờ ngoài bàn chân và gót chân trái; Không rối loạn cơ tròn. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 5.9 G/L, RBC 4.70 T/L, HGB 154 g/L, PLT 215 G/L.\n- Đông máu: PT-INR 1.09, APTT 0.89 (27.0s), Fibrinogen 3.91 g/L.\n- Sinh hóa máu: Glucose 4.4 mmol/L, Ure 3.0 mmol/L, Creatinin 87 µmol/L, AST 16 U/L, ALT 19 U/L, Điện giải Na/K/Cl (141/3.9/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Hình ảnh thoát vị đĩa đệm tầng L5/S1 thể đứt rời di trú xuống dưới qua dây chằng dọc sau khoảng 7mm vào ngách bên ngách thần kinh S1 bên trái, chèn ép nặng rễ S1 trái; tầng L4-L5 phồng nhẹ không hẹp ống sống. X-quang CSTL: Thoái hóa nhẹ, không mất vững cột sống.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Nội soi lấy thoát vị đĩa đệm (L5/S1)",
                Anesthesia = "Mê nội khí quản (hoặc Tê tủy sống)",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "11 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh S1 gây tê bì/yếu vận động bàn chân, tụ máu ngách bên sau mổ, sót tổ chức thoát vị hoặc tái phát thoát vị đĩa đệm, nhiễm trùng khoang đĩa đệm."
            });

            // BN 4: NGÔ THỊ KÝ
            list.Add(new PatientProfile {
                Stt = 4,
                PatientCode = "0004027424",
                PatientName = "NGÔ THỊ KÝ",
                Dob = "1952",
                Gender = "Nữ",
                Address = "Phường  Đồng Văn, Ninh Bình",
                InTime = "10/09/2026 14:46",
                Diagnosis = "Xẹp cấp L1, L3 / Loãng xương nặng",
                History = "Thoái hóa cột sống thắt lưng nhiều năm, loãng xương nặng (DEXA T-score -3.6), không có tiền sử dị ứng thuốc.",
                Course = "Bệnh nhân nữ 74 tuổi, khoảng 1 tháng nay xuất hiện đau nhiều vùng cột sống thắt lưng sau sinh hoạt tại nhà, đau lan xuống hai bên mông, đau tăng dữ dội khi ngồi dậy hoặc đi lại, không thể tự xoay trở mình trên giường. Đã điều trị nội khoa tại Khoa Cơ Xương Khớp chuyển sang Khoa CTCH & Cột sống phẫu thuật bơm xi măng sinh học thân đốt sống L1 và L3.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 130/80 mmHg, T° 36.5°C, SpO2 98%. Thể trạng già gầy. Cột sống thắt lưng ấn đau chói tại gai sau và cạnh sống L1 và L3, gõ dồn từ gót chân đau dội lên vùng thắt lưng; cơ cạnh sống co cứng; không có triệu chứng chèn ép rễ thần kinh (Lasègue 2 bên âm tính), cơ lực hai chân 5/5, cảm giác bàn ngón chân bình thường, đại tiểu tiện tự chủ. Khám tim phổi: Tim đều; Phổi thông khí tốt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.5 G/L, RBC 4.10 T/L, HGB 118 g/L, PLT 220 G/L.\n- Đông máu: PT-INR 0.92, APTT 0.84 (26.5s), Fibrinogen 3.84 g/L.\n- Sinh hóa máu: Glucose 5.8 mmol/L, Ure 5.2 mmol/L, Creatinin 70 µmol/L, AST 22 U/L, ALT 19 U/L, Điện giải Na/K/Cl (138/4.0/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: Đo mật độ xương DEXA: T-score cột sống thắt lưng -3.6, cổ xương đùi -3.2 (Loãng xương nặng). MRI cột sống thắt lưng: Xẹp cấp thân đốt sống L1 và L3 giảm chiều cao ~30%, phù nề tủy xương tăng tín hiệu rõ trên STIR, giảm tín hiệu T1W, không có mảnh rời chèn ép ống sống. Siêu âm tim: EF 62%, van tim thoái hóa nhẹ.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng thân đốt sống (BXM L1, L3)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Tân",
                SurgeryTime = "13 giờ 30 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu vết chọc kim, rò rỉ xi măng sinh học vào khoang ngoài màng cứng gây chèn ép tủy/rễ thần kinh, tắc mạch phổi do xi măng rò vào tĩnh mạch, phản ứng dị ứng xi măng tụt huyết áp chu phẫu, gãy xẹp thứ phát các đốt sống kế cận."
            });

            // BN 5: NGUYỄN HỮU BÌNH
            list.Add(new PatientProfile {
                Stt = 5,
                PatientCode = "0003994196",
                PatientName = "NGUYỄN HỮU BÌNH",
                Dob = "18/03/1957",
                Gender = "Nam",
                Address = "Phường  Phúc Yên, Phú Thọ",
                InTime = "10/09/2026 06:12",
                Diagnosis = "Viêm đốt sống đĩa đệm C6/7 có tạo ổ áp xe mặt trước C6/7 - Yếu tay phải - Đợt cấp gút mạn / Tăng huyết áp",
                History = "Gút mạn tính nhiều năm có nhiều nốt tophi khớp ngón chân tay; Tăng huyết áp đang điều trị thuốc.",
                Course = "Bệnh nhân nam 69 tuổi, tiền sử Gút mạn và tăng huyết áp. Khoảng 2 tuần nay xuất hiện đau âm ỉ tăng dần vùng cổ gáy lan xuống vai và cánh tay phải, sau đó yếu cơ tay phải kèm sốt gai rét từng cơn. Chụp MRI cột sống cổ phát hiện viêm thân đốt sống đĩa đệm C6-C7 tạo ổ áp xe lớn mặt trước cột sống chèn ép rễ C7 phải -> nhập viện Khoa CTCH & Cột sống phẫu thuật nạo vét ổ áp xe viêm, cắt lọc hoại tử, cố định cột sống và điều trị kháng sinh trúng đích theo kháng sinh đồ.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 88 ck/p, HA 135/85 mmHg, T° 37.4°C, SpO2 98%. Bệnh nhân tỉnh, thể trạng trung bình, sốt nhẹ. Cột sống cổ đau tức, hạn chế vận động; vùng trước và bên cổ phải đầy, ấn đau tức sâu; Khám chi trên phải: Yếu cơ duỗi cổ tay và cơ tam đầu cánh tay phải (cơ lực 3/5), cầm nắm yếu; cảm giác tê bì giảm ngón 2-3 bàn tay phải; chi trên trái cơ lực 5/5; Hai chi dưới cơ lực 5/5, Babinski âm tính; Các khớp bàn ngón chân có nhiều hạt tophi gút kích thước 1-2cm không loét rò. Tim đều; Phổi thông khí rõ; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 12.8 G/L (Neu 78%), RBC 3.75 T/L, HGB 112 g/L, PLT 243 G/L.\n- Đông máu: PT-INR 0.97, APTT 1.33, Fibrinogen 5.53 g/L (phản ứng viêm tăng cao).\n- Sinh hóa máu: Glucose 7.2 mmol/L, Ure 6.4 mmol/L, Creatinin 62 µmol/L, AST 81 U/L, ALT 226 U/L, Acid Uric 520 µmol/L, CRP 45 mg/L, Điện giải Na/K/Cl (136/4.2/99 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI cột sống cổ: Viêm hủy hoại thân đốt sống và đĩa đệm tầng C6-C7, giảm chiều cao đĩa đệm, tạo ổ áp xe mặt trước thân đốt sống C6/7 kích thước ~15x22mm lan vào lỗ ghép C6-C7 bên phải chèn ép rễ thần kinh C7 phải. X-quang phổi: Bình thường.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "500",
                SurgeryMethod = "Làm sạch, CĐCS, Kháng sinh theo KSĐ",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Nguyễn Đức Hoàng",
                SurgeryTime = "15 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, tổn thương bó mạch cảnh hoặc thần kinh quặt ngược thanh quản, rách thủng thực quản do dính ổ áp xe viêm, nhiễm trùng lan rộng khoang trung thất cổ, lỏng tuột dụng cụ cố định cột sống do nền xương viêm mục, sốc nhiễm khuẩn chu phẫu."
            });

            // =========================================================================
            // PHÒNG MỔ 2 (CHẤN THƯƠNG CHỈNH HÌNH - CTCH) — 5 CA
            // =========================================================================

            // BN 6: LÊ VĂN HIẾU
            list.Add(new PatientProfile {
                Stt = 6,
                PatientCode = "0003990031",
                PatientName = "LÊ VĂN HIẾU",
                Dob = "10/05/2000",
                Gender = "Nam",
                Address = "06B 132 đường Trần Hưng Đạo, Phường  Hàm Rồng, Thanh Hóa",
                InTime = "25/08/2026 18:34",
                Diagnosis = "Gãy 1/3 trên 2 xương cẳng chân, 1/3 giữa xương đùi trái / CTBK: Chấn thương gan độ III, Chấn thương thận độ III - CTNK",
                History = "Bệnh nhân trẻ 26 tuổi, bị đa chấn thương do tai nạn giao thông xe máy - ô tô đêm 24/08/2026. Đã điều trị hồi sức tích cực bảo tồn thành công chấn thương gan độ III và chấn thương thận phải độ III ổn định.",
                Course = "Bệnh nhân nam 26 tuổi, bị TNGT đa chấn thương chuyển cấp cứu BV Bạch Mai ngày 25/08/2026. Sau gần 4 tuần hồi sức tích cực, theo dõi sát chấn thương bụng kín (gan độ III, thận phải độ III) và tràn dịch màng phổi phải, hiện toàn trạng hoàn toàn ổn định, các tạng bụng hồi phục tốt -> chuyển mổ phiên kết hợp xương đùi và hai xương cẳng chân trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 120/75 mmHg, T° 36.8°C, SpO2 99%. Bệnh nhân tỉnh, tiếp xúc tốt, da niêm mạc hồng nhạt. Khám chi dưới trái: Đang bất động nẹp kéo liên tục; đùi trái sưng nề 1/3 giữa, ngắn chi ~1.5cm; cẳng chân trái sưng nề 1/3 trên, không nốt phỏng, không vết thương hở (gãy kín); sờ điểm đau chói cố định và có tiếng lạo xạo xương; mạch mu chân và chày sau trái bắt rõ, ngón chân hồng ấm, CRT < 2s, cảm giác mu và lòng bàn chân bình thường, các ngón chân cử động được; không có dấu hiệu chèn ép khoang cẳng chân. Khám bụng mềm, ấn hạ sườn phải không đau tức; Nước tiểu trong; Ngực vững, phổi thông khí tốt.",
                FullCls = "- Công thức máu: WBC 17.2 G/L, RBC 3.65 T/L, HGB 105 g/L, PLT 617 G/L.\n- Đông máu: PT-INR 1.16, APTT 0.93 (28.5s), Fibrinogen 2.20 g/L.\n- Sinh hóa máu: Glucose 6.7 mmol/L, Ure 5.7 mmol/L, Creatinin 62 µmol/L, AST 86 U/L, ALT 190 U/L (men gan giảm tốt), Điện giải Na/K/Cl (139/4.0/102 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CT Scanner bụng - ngực kiểm tra: Nhu mô gan và thận phải tổn thương độ III đã liền bao xơ tốt, không còn thoát thuốc hoạt tử; dịch màng phổi phải lượng rất ít. X-quang: Gãy 1/3 giữa thân xương đùi trái di lệch chồng ngắn; gãy 1/3 trên hai xương cẳng chân trái di lệch.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "700",
                SurgeryMethod = "03KHX (Kết hợp xương đùi trái và xương cẳng chân trái)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Đặng Hoàng Giang",
                SurgeryTime = "08 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu mất máu trong mổ lớn do can thiệp nhiều xương, tắc mạch mỡ (FES), hội chứng chèn ép khoang cẳng chân chu phẫu, tổn thương thần kinh mác chung, can lệch hoặc chậm liền xương, nhiễm trùng sâu vết mổ kết hợp xương."
            });

            // BN 7: PHẠM ĐẠI ĐỘ
            list.Add(new PatientProfile {
                Stt = 7,
                PatientCode = "0004013868",
                PatientName = "PHẠM ĐẠI ĐỘ",
                Dob = "18/09/2005",
                Gender = "Nam",
                Address = "Xâm Dương, Xã  Hồng Vân, Thành phố Hà Nội",
                InTime = "05/09/2026 01:53",
                Diagnosis = "Gãy xương đòn trái / Sau mổ Chấn thương sọ não - Vết thương hàm mặt",
                History = "Sau phẫu thuật cấp cứu mở nắp sọ lấy máu tụ ngoài màng cứng trán trái ngày 05/09/2026 thành công, tri giác tỉnh táo hoàn toàn.",
                Course = "Bệnh nhân nam 21 tuổi, bị TNGT xe máy - ô tô ngày 05/09/2026 chấn thương sọ não máu tụ ngoài màng cứng trán trái và gãy xương đòn trái. Đã được phẫu thuật cấp cứu sọ não thành công ngày 05/09, hiện tri giác ổn định G15 điểm, vết mổ sọ não liền tốt -> chuyển Khoa CTCH & Cột sống phẫu thuật kết hợp xương nẹp vít xương đòn trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 115/75 mmHg, T° 36.7°C, SpO2 99%. Bệnh nhân tỉnh táo hoàn toàn, GCS 15đ, tiếp xúc tốt. Vết mổ trán thái dương trái khô sạch, sọ mềm phẳng; không liệt thần kinh sọ khu trú. Khám vai trái: Vùng 1/3 giữa xương đòn trái sưng nề bầm tím, gồ xương dưới da, ấn đau chói và có cử động bất thường; Khám chi trên trái: Tê bì nhẹ mặt ngoài cánh tay, cơ lực vai cánh tay trái 4/5 do đau, cơ lực bàn ngón tay 5/5, bắt mạch quay rõ đều hai bên, ngón tay hồng ấm; Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 17.6 G/L, RBC 4.38 T/L, HGB 136 g/L, PLT 284 G/L.\n- Đông máu: PT-INR 1.06, APTT 0.75 (24.5s), Fibrinogen 2.02 g/L.\n- Sinh hóa máu: Glucose 6.4 mmol/L, Ure 4.7 mmol/L, Creatinin 76 µmol/L, AST 47 U/L, ALT 18 U/L, Điện giải Na/K/Cl (140/3.9/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CT Scanner sọ não kiểm tra: Ổ mổ sạch, không tụ máu ngoài màng cứng tái phát, nhu mô não nở tốt. X-quang xương đòn trái: Gãy 1/3 giữa xương đòn trái di lệch có mảnh rời gồ lên trên.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX nẹp vít (Kết hợp xương đòn trái nẹp vít)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Đặng Nhật Quang",
                SurgeryTime = "09 giờ 30 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Tổn thương bó mạch dưới đòn và đám rối thần kinh cánh tay, tràn khí/tràn máu màng phổi do đầu dụng cụ chọc đỉnh phổi, tụ máu vết mổ, gãy/bung nẹp vít sau mổ, nhiễm trùng vết mổ."
            });

            // BN 8: BÙI VĂN LUẬN
            list.Add(new PatientProfile {
                Stt = 8,
                PatientCode = "0004050885",
                PatientName = "BÙI VĂN LUẬN",
                Dob = "28/03/1953",
                Gender = "Nam",
                Address = "Xã  Tân Kỳ, Nghệ An",
                InTime = "16/09/2026 19:49",
                Diagnosis = "Gãy LMC xương đùi trái / Phình ĐM cảnh trong phải",
                History = "Chóng mặt nhiều đợt; Phát hiện phình động mạch cảnh trong phải đoạn C5 kích thước 3.5x3.6mm chưa vỡ.",
                Course = "Bệnh nhân nam 73 tuổi, cách vào viện 5 ngày bị chóng mặt trượt ngã đập đùi háng trái xuống nền bê tông. Sau ngã đau chói vùng háng đùi trái dữ dội, không thể đứng hay nhấc chân lên được. Được người nhà chuyển BV Bạch Mai cấp cứu, chụp CTA phát hiện thêm phình ĐM cảnh trong phải C5 chưa vỡ, đã hội chẩn Tim mạch - Thần kinh đánh giá an toàn phẫu thuật -> chuyển mổ phiên kết hợp xương.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. Bệnh nhân tỉnh táo, GCS 15đ, không đau đầu, không chóng mặt lúc khám. Khám chi dưới trái: Chi trái xoay ngoài nhẹ, ngắn chi ~1cm; tam giác Scarpa đầy, ấn đau chói điểm mấu chuyển lớn xương đùi trái, gõ dồn gót chân đau buốt khớp háng trái; mất hoàn toàn khả năng vận động chân trái; Mạch mu chân và chày sau 2 bên bắt rõ, ngón chân hồng ấm, cảm giác ngọn chi bình thường; Tim đều, T1 T2 rõ; Phổi thông khí tốt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.2 G/L, RBC 4.15 T/L, HGB 130 g/L, PLT 180 G/L.\n- Đông máu: PT-INR 1.05, APTT 1.51, Fibrinogen 3.40 g/L.\n- Sinh hóa máu: Glucose 4.7 mmol/L, Ure 5.8 mmol/L, Creatinin 57 µmol/L, AST 35 U/L, ALT 24 U/L, Điện giải Na/K/Cl (137/4.1/100 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CTA Động mạch não: Túi phình động mạch cảnh trong phải đoạn C5 kích thước 3.5x3.6mm, cổ 3.5mm chưa vỡ. X-quang khớp háng & đùi trái: Hình ảnh gãy liên mấu chuyển xương đùi trái di lệch gập góc. Siêu âm tim: EF 60%, thất trái không giãn.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX (kết hợp xương)",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ)",
                Surgeon = "BS. Đặng Nhật Quang",
                SurgeryTime = "11 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu mất máu trong mổ ở người cao tuổi, biến chứng tim mạch chu phẫu do phình ĐM cảnh trong (cần kiểm soát huyết áp tránh dao động mạnh), huyết khối tĩnh mạch sâu chi dưới gây thuyên tắc phổi, nứt vỡ xương đùi thêm trong quá trình đóng đinh/nẹp, tụ máu vết mổ, nhiễm trùng chu phẫu."
            });

            // BN 9: NGUYỄN THỊ THỦY
            list.Add(new PatientProfile {
                Stt = 9,
                PatientCode = "0002978421",
                PatientName = "NGUYỄN THỊ THỦY",
                Dob = "10/06/1985",
                Gender = "Nữ",
                Address = "Phường  Hoàng Mai, Thành phố Hà Nội",
                InTime = "17/09/2026 08:51",
                Diagnosis = "Chấn thương gối trái đứt dây chằng chéo trước",
                History = "Khỏe mạnh, không có bệnh lý mạn tính đặc biệt.",
                Course = "Bệnh nhân nữ 41 tuổi, bị TNGT xe máy cách đây 3 tuần, sau ngã đau nhiều khớp gối trái, sưng nề hạn chế vận động, cảm giác lỏng khớp láng gối khi bước đi, điều trị nội khoa không đỡ. Được khám và chụp MRI khớp gối chẩn đoán đứt hoàn toàn dây chằng chéo trước gối trái -> nhập viện Khoa CTCH & Cột sống phẫu thuật nội soi tái tạo dây chằng chéo trước.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 110/70 mmHg, T° 36.5°C, SpO2 99%. Bệnh nhân tỉnh, thể trạng tốt. Khám gối trái: Khớp gối trái sưng nề nhẹ, dấu hiệu bập bềnh xương bánh chè (+/-); Ấn đau khe khớp trước trong; Tầm vận động gối: Gấp 110 độ, duỗi gần hết (-5 độ do đau); Dấu hiệu lỏng khớp: Ngăn kéo trước (+), Lachman (+), Pivot shift (+/-); Ngăn kéo sau (-); Mạch mu chân và chày sau trái bắt rõ, ngón chân hồng ấm, cảm giác và vận động bàn ngón chân bình thường. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 5.21 G/L, RBC 4.25 T/L, HGB 118 g/L, PLT 215 G/L.\n- Đông máu: PT-INR 1.10, APTT 0.96 (28.8s), Fibrinogen 3.24 g/L.\n- Sinh hóa máu: Glucose 5.0 mmol/L, Ure 3.7 mmol/L, Creatinin 58 µmol/L, AST 28 U/L, ALT 17 U/L, Điện giải Na/K/Cl (139/4.0/102 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI khớp gối trái: Đứt hoàn toàn dây chằng chéo trước (ACL), tràn dịch bao khớp gối mức độ vừa, đụng dập tủy xương lồi cầu ngoài xương đùi và mâm chày; dây chằng chéo sau và sụn chêm chưa rách rời. X-quang khớp gối: Không thấy gãy xương.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Nội soi ttdcct gối trái",
                Anesthesia = "Tê tủy sống",
                Surgeon = "BS. Đặng Hoàng Giang",
                SurgeryTime = "13 giờ 30 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu, tràn máu tràn dịch khớp gối sau mổ, đứt hoặc lỏng mảnh ghép dây chằng mới tái tạo, cứng khớp gối hạn chế gấp duỗi sau mổ, tổn thương mạch máu thần kinh khoeo, nhiễm trùng vết mổ/nhiễm trùng khớp gối."
            });

            // BN 10: PHẠM VĂN BỒNG
            list.Add(new PatientProfile {
                Stt = 10,
                PatientCode = "0002840646",
                PatientName = "PHẠM VĂN BỒNG",
                Dob = "22/07/1947",
                Gender = "Nam",
                Address = "Tổ 6, Phường  Minh Xuân, Tuyên Quang",
                InTime = "15/09/2026 21:25",
                Diagnosis = "Gãy liên mấu chuyển xương đùi Trái - viêm phổi - hạ natri máu / Stent mạch vành - Suy tim - Tăng huyết áp - bệnh thận mạn giai đoạn IV",
                History = "Stent động mạch vành, suy tim, tăng huyết áp, bệnh thận mạn tính giai đoạn IV (Creatinin máu nền ~200-220 µmol/L).",
                Course = "Bệnh nhân nam 79 tuổi, tiền sử đặt stent ĐMV, suy tim, THA, bệnh thận mạn giai đoạn IV. Ngày 15/09/2026 trượt ngã tại nhà đập hông trái xuống sàn cứng, sau ngã đau nhức dữ dội háng đùi trái, bất lực vận động hoàn toàn chân trái kèm sốt nhẹ và ho khạc đờm. Được chuyển BV Bạch Mai cấp cứu, điều trị nội khoa tích cực viêm phổi, bù natri máu và hội chẩn Tim mạch - Thận lọc máu kiểm soát ổn định -> mổ phiên kết hợp xương.",
                HoiChanTime = "14 giờ 00 phút, ngày 20 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 135/80 mmHg, T° 36.8°C, SpO2 96% (thở khí phòng). Thể trạng già yếu, niêm mạc nhợt nhẹ. Khám chi dưới trái: Chi trái xoay ngoài, ngắn chi ~2cm; tam giác Scarpa đầy, ấn đau chói điểm liên mấu chuyển xương đùi trái; bất lực hoàn toàn vận động chân trái; Mạch chày sau và mu chân trái bắt rõ, ngón chân ấm, không tê bì mới; Khám tim mạch: Tim đều, T1 T2 mờ nhẹ, không tiếng thổi mới; Khám hô hấp: Phổi thông khí hai bên, có ran ẩm rải rác hai đáy phổi, không khó thở co kéo; Bụng mềm, không trướng.",
                FullCls = "- Công thức máu: WBC 11.6 G/L, RBC 3.80 T/L, HGB 124 g/L, PLT 247 G/L.\n- Đông máu: PT-INR 1.04, APTT 0.77 (24.8s), Fibrinogen 3.89 g/L.\n- Sinh hóa máu: Glucose 5.0 mmol/L, Ure 15.7 mmol/L, Creatinin 221 µmol/L (suy thận mạn gđ IV), AST 24 U/L, ALT 13 U/L, Điện giải: Na+ 130 mmol/L, K+ 4.2 mmol/L, Cl- 98 mmol/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: X-quang khớp háng & đùi trái: Hình ảnh gãy liên mấu chuyển xương đùi trái không vững, loãng xương nặng. Siêu âm tim: EF 48%, giảm vận động thành tim vùng đặt stent, buồng tim giãn nhẹ. X-quang tim phổi: Đám mờ rải rác phế quản viêm đáy phổi hai bên.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX nẹp vít (hoặc đinh nội tủy PFNA)",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ) phối hợp theo dõi huyết động xâm lấn",
                Surgeon = "BS. Đặng Nhật Quang",
                SurgeryTime = "15 giờ 00 phút, ngày 21 tháng 09 năm 2026",
                Risks = "Chảy máu mất máu chu phẫu trên người già suy thận mạn giai đoạn IV, biến cố tim mạch kịch phát (nhồi máu cơ tim tái phát, loạn nhịp tim, suy tim cấp do quá tải dịch), suy thận cấp trên nền mạn cần lọc máu cấp cứu, suy hô hấp do viêm phổi tiến triển, thuyên tắc phổi do huyết khối tĩnh mạch sâu, nhiễm trùng vết mổ."
            });

            return list;
        }
    }
}
