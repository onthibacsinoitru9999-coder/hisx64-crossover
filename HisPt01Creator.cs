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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "Bien_Ban_Thong_Qua_Mo_PT01_20260925_NB");
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

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) NINH BÌNH ===", targetList.Count));

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

                string safeName = p.PatientName.Trim().Replace(" ", "_").Replace("*", "");
                string fileNameDated = string.Format("{0:D2}_PT01_BienBanThongQuaMo_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
                string fileNameStd = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);

                doc.Save(Path.Combine(outDirDated, fileNameDated));
                doc.Save(Path.Combine(outDirStd, fileNameStd));
                Console.WriteLine("  ✔ Đã xuất: " + fileNameDated);
            }

            Console.WriteLine("\n🎉 HOÀN THÀNH TOÀN BỘ 7 BIÊN BẢN HỘI CHẨN PT-01 NINH BÌNH TẠI: " + outDirDated);

            // Đồng bộ trực tiếp lên Google Drive
            try
            {
                Console.WriteLine("\n☁️ Đang đồng bộ lên Google Drive (gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260925_NB)...");
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
                    Arguments = string.Format("copy \"{0}\" \"gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260925_NB\" --quiet", outDirDated),
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(30000);
                    Console.WriteLine("✔ Đã đồng bộ thành công lên Google Drive: gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260925_NB");
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

            // =========================================================
            // PHIÊN MỔ NINH BÌNH — DANH SÁCH 7 BỆNH NHÂN THÔNG QUA MỔ
            // =========================================================

            // BN 1: LƯƠNG THỊ THÁI
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0004068529",
                PatientName = "LƯƠNG THỊ THÁI",
                Dob = "15/06/1958",
                Gender = "Nữ",
                Address = "Xã Gia Trấn, Huyện Gia Viễn, Tỉnh Ninh Bình",
                InTime = "23/09/2026 18:38",
                Diagnosis = "Đứt đầu dài gân cơ nhị đầu - Hẹp khoang dưới mỏm cùng vai trái / Tăng huyết áp, Đái tháo đường type 2, Suy giáp (M75.1)",
                History = "Tăng huyết áp, Đái tháo đường type 2, Suy giáp đang điều trị theo đơn thuốc nội khoa hàng ngày.",
                Course = "Bệnh nhân nữ 68 tuổi, tiền sử THA, ĐTĐ type 2, suy giáp. Đau tức âm ỉ vùng vai trái nhiều tháng, đợt này đau tăng dữ dội khi giơ tay lên cao hoặc dạng xoay vai, kèm cảm giác yếu cơ và lục cục trong khớp, điều trị nội khoa không đỡ. Vào Khoa Ngoại tổng hợp Tầng 3 Nhà E BVBM CS2 Ninh Bình chụp MRI khớp vai phát hiện đứt đầu dài gân nhị đầu cánh tay, hẹp khoang dưới mỏm cùng vai trái -> Chỉ định phẫu thuật nội soi tạo hình khoang dưới mỏm cùng vai và xử trí gân nhị đầu.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng trung bình. Khám vai trái: Ấn đau chói điểm bám gân nhị đầu và khoang dưới mỏm cùng vai; Speed test (+), Yergason (+), Neer test (+), Hawkins-Kennedy (+); biên độ vận động dạng - khép, xoay trong - xoay ngoài khớp vai trái bị hạn chế do đau; mạch quay bắt rõ, cảm giác nông sâu bình thường. Tim đều, phổi không rale, bụng mềm.",
                FullCls = "- MRI Khớp vai trái: Đứt hoàn toàn đầu dài gân cơ nhị đầu cánh tay trái kèm co rút nhẹ; hẹp khoang dưới mỏm cùng vai (mỏm cùng vai type II), thoái hóa diện khớp cùng đòn; rách bán phần gân trên gai; tràn dịch bao khớp và bao gân nhị đầu.\n- X-Quang khớp vai trái: Gai xương mỏm cùng vai, thoái hóa khớp cùng đòn nhẹ.\n- Công thức máu: WBC 6.8 G/L, HGB 128 g/L, PLT 265 G/L.\n- Đông máu: PT-INR 0.98, APTT 28.5 giây, Fibrinogen 3.42 g/L.\n- Sinh hóa máu: Glucose 6.8 mmol/L, Ure 5.2 mmol/L, Creatinin 68 µmol/L, AST 24 U/L, ALT 22 U/L.\n- Điện tim: Nhịp xoang đều, tần số 74 ck/phút, không rối loạn tái cực.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật nội soi tạo hình khoang dưới mỏm cùng vai, cố định/cắt gân nhị đầu (Nội soi tạo hình khoang dưới mỏm cùng vai, gân nhị đầu)",
                Anesthesia = "Gây mê nội khí quản kết hợp gây tê đám rối thần kinh cánh tay",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "08 giờ 00 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, tụ máu khớp vai, nhiễm trùng vết mổ/khớp vai, tổn thương thần kinh nách hoặc mạch máu nách, đông cứng khớp vai sau mổ, đau tồn dư, biến động đường huyết/huyết áp chu phẫu."
            });

            // BN 2: NGUYỄN VĂN SỸ
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0004032591",
                PatientName = "NGUYỄN VĂN SỸ",
                Dob = "10/04/1985",
                Gender = "Nam",
                Address = "Phường Ninh Khánh, TP. Ninh Bình, Tỉnh Ninh Bình",
                InTime = "22/09/2026 17:34",
                Diagnosis = "Đa chấn thương: Chấn thương cột sống cổ vỡ C1, trật C5C6 - gãy đầu dưới xương quay trái / CTNK đã DLMP - CT bụng kín chấn thương gan độ III - CTSN G15 điểm (S12.0, S13.1, S52.5)",
                History = "Đa chấn thương sau TNGT: Chấn thương ngực kín đã đặt dẫn lưu màng phổi ổn định, Chấn thương bụng kín (chấn thương gan độ III điều trị bảo tồn ổn định), CTSN tri giác G15 điểm.",
                Course = "Bệnh nhân nam 41 tuổi, bị TNGT đa chấn thương nặng được sơ cấp cứu dẫn lưu màng phổi và hồi sức bảo tồn chấn thương gan ổn định tại tuyến dưới chuyển BVBM CS2. Hiện tại toàn trạng ổn định, khám phát hiện chấn thương cột sống cổ mất vững (vỡ C1, trật C5C6) kèm gãy đầu dưới xương quay trái -> Hội chẩn chỉ định phẫu thuật Thì 2: Hàn xương liên thân đốt lối trước cổ C5-C6 (ACDF) kết hợp phẫu thuật kết hợp xương đầu dưới xương quay trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 82 ck/p, HA 125/80 mmHg, T° 36.8°C, SpO2 98% (thở khí phòng). BN tỉnh, tiếp xúc tốt, Glasgow 15 điểm. Đang cố định nẹp cổ cứng, ấn đau chói vùng gai sau C5-C6; cơ lực 2 tay 4/5 (yếu nhẹ nắm tay bên trái), cơ lực 2 chân 5/5, cảm giác nông tê bì nhẹ ngón 1-2 tay trái (rễ C6); phản xạ gân xương bình thường. Cổ tay trái sưng nề, biến dạng gãy đầu dưới xương quay, ấn đau chói, hạn chế vận động cổ bàn tay. Ngực: Dẫn lưu màng phổi ra ít dịch hồng, phổi thông khí rõ. Bụng mềm, không đau.",
                FullCls = "- CT Scanner Cột sống cổ: Vỡ cung trước và khối bên C1 vững; trật khớp đốt sống C5/C6 ra trước độ II, rách đĩa đệm C5-C6, hẹp ống sống cổ ngang mức gây chèn ép tủy cổ và rễ C6 hai bên.\n- X-Quang & CT Cổ tay trái: Gãy nội khớp đầu dưới xương quay trái kiểu Pouteau-Colles di lệch, kèm gãy mỏm trâm trụ.\n- CT Ngực - Bụng: Chấn thương gan độ III ổn định không thoát mạch; khoang màng phổi còn ít khí dịch đã dẫn lưu tốt.\n- Công thức máu: WBC 9.4 G/L, HGB 112 g/L, PLT 215 G/L.\n- Đông máu: PT-INR 1.05, APTT 30.2 giây, Fibrinogen 3.85 g/L.\n- Sinh hóa máu: Glucose 5.8 mmol/L, Ure 6.1 mmol/L, Creatinin 76 µmol/L, AST 45 U/L, ALT 52 U/L.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật hàn xương liên thân đốt và cố định nẹp vít cột sống cổ lối trước (ACDF C5-C6) + Kết hợp xương đầu dưới xương quay trái bằng nẹp vít khóa (ACDF - KHX quay tay trái)",
                Anesthesia = "Gây mê nội khí quản",
                Surgeon = "BS. Nguyễn Đức Hoàng - BS. Bình",
                SurgeryTime = "08 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, tổn thương tủy cổ/rễ thần kinh gây liệt tứ chi, tổn thương thực quản/khí quản, khàn tiếng do tổn thương thần kinh quặt ngược thanh quản, tràn khí/máu màng phổi tái phát, chảy máu ổ chấn thương gan, nhiễm trùng vết mổ, không liền xương."
            });

            // BN 3: TRƯƠNG THỊ HỘI
            list.Add(new PatientProfile {
                Stt = 3,
                PatientCode = "0004071061",
                PatientName = "TRƯƠNG THỊ HỘI",
                Dob = "20/08/1962",
                Gender = "Nữ",
                Address = "Xã Ninh Giang, Huyện Hoa Lư, Tỉnh Ninh Bình",
                InTime = "23/09/2026 18:46",
                Diagnosis = "Xẹp đốt sống L1, L2, L5 cấp tính / Đau dây thần kinh hông to, Đau nhiều vị trí của cột sống, Loãng xương nặng (M48.50)",
                History = "Loãng xương nhiều năm, đau cột sống thắt lưng mạn tính điều trị nội khoa nhiều đợt.",
                Course = "Bệnh nhân nữ 64 tuổi, tiền sử loãng xương nhiều năm. Cách vào viện khoảng 10 ngày sau ngã ngồi đập mông xuất hiện đau thắt lưng dữ dội tăng dần, đau nhiều vị trí cột sống ngực - thắt lưng, đau tăng mạnh khi ngồi dậy hoặc đi lại, nằm nghỉ đỡ đau, đau lan xuống mặt sau đùi hai bên, hạn chế vận động. Chụp MRI phát hiện xẹp cấp phù tủy xương các thân đốt sống L1, L2, L5 -> Chỉ định phẫu thuật bơm xi măng sinh học tạo hình thân đốt sống L1, L2, L5.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 135/85 mmHg, T° 36.5°C, SpO2 98%. BN tỉnh, thể trạng trung bình. Cột sống thắt lưng: Co cứng cơ cạnh sống, ấn đau chói rõ tại gai sau và cạnh sống L1, L2, L5; VAS 7/10; hạn chế cúi ngửa hoàn toàn do đau; Lasègue (+) 60 độ 2 bên; cơ lực 2 chân 5/5, cảm giác tê bì nhẹ mặt sau đùi và cẳng chân 2 bên; phản xạ gân xương bình thường, đại tiểu tiện tự chủ.",
                FullCls = "- MRI Cột sống thắt lưng: Hình ảnh xẹp cấp tính thân đốt sống L1, L2, L5 có phù tủy xương rõ trên STIR (giảm T1W, tăng mạnh STIR); không đẩy lồi tường sau vào ống sống; phình thoái hóa đĩa đệm nhiều tầng L1-S1 kèm thoái hóa diện khớp; loãng xương thân đốt lan tỏa.\n- DEXA đo mật độ xương: Loãng xương nặng (T-score = -3.2 SD).\n- X-Quang CSTL: Giảm chiều cao thân đốt L1, L2, L5 hình chêm, loãng xương rõ.\n- Công thức máu: WBC 6.5 G/L, HGB 122 g/L, PLT 240 G/L.\n- Đông máu: PT-INR 0.96, APTT 29.1 giây, Fibrinogen 3.60 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 5.8 mmol/L, Creatinin 64 µmol/L, AST 21 U/L, ALT 18 U/L.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học tạo hình thân đốt sống L1, L2, L5 qua cuống (BXM L1, L2, L5)",
                Anesthesia = "Tiền mê kết hợp gây tê tại chỗ (hoặc Tê tủy sống)",
                Surgeon = "BS. Nguyễn Đức Hoàng",
                SurgeryTime = "09 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Rò rỉ xi măng vào ống sống gây chèn ép tủy/rễ thần kinh, tràn xi măng vào tĩnh mạch cạnh sống gây thuyên tắc phổi do xi măng, chảy máu, nhiễm trùng vết mổ, tụt huyết áp phản ứng xi măng, đau tồn dư, nguy cơ xẹp đốt sống liền kề trên nền loãng xương nặng."
            });

            // BN 4: CAO VĂN ĐỊNH
            list.Add(new PatientProfile {
                Stt = 4,
                PatientCode = "0004055650",
                PatientName = "CAO VĂN ĐỊNH",
                Dob = "12/03/1984",
                Gender = "Nam",
                Address = "Xã Khánh Hòa, Huyện Yên Khánh, Tỉnh Ninh Bình",
                InTime = "22/09/2026 17:26",
                Diagnosis = "Nang hoạt dịch dây chằng chéo trước gối trái / Rách sụn chêm ngoài - Đứt bán phần ACL gối trái (M71.2, S83.2)",
                History = "Chấn thương khớp gối trái khi chơi thể thao cách 6 tháng, đau tức âm ỉ tái diễn.",
                Course = "Bệnh nhân nam 42 tuổi, đau tức và kẹt khớp gối trái tăng dần trong 2 tháng nay, đau nhiều khi gấp gối tối đa hoặc leo cầu thang, có cảm giác lỏng gối nhẹ. Vào viện khám và chụp MRI khớp gối phát hiện nang hoạt dịch lớn xuất phát từ bao dây chằng chéo trước kèm rách sụn chêm ngoài -> Chỉ định phẫu thuật nội soi làm sạch khớp gối trái và bóc nang hoạt dịch dây chằng chéo trước.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. Thể trạng tốt. Gối trái: Sưng nề nhẹ khoang gian lồi cầu; ấn đau khe khớp ngoài và vùng trước dây chằng chéo; dấu hiệu bập bềnh xương bánh chè (+/-); Lachman (+/-), Ngăn kéo trước (+/-), McMurray (+/-); biên độ vận động gối trái: duỗi 0° - gấp 110° (đau khi gấp tối đa); mạch mu chân bắt rõ 2 bên.",
                FullCls = "- MRI Khớp gối trái: Cấu trúc nang hoạt dịch ranh giới rõ vùng gian lồi cầu bao quanh dây chằng chéo trước (kích thước ~ 18x25 mm), tăng tín hiệu trên T2W/STIR; rách đứt bán phần dây chằng chéo trước; rách sừng sau sụn chêm ngoài độ II-III; tràn dịch bao khớp gối lượng ít.\n- X-Quang khớp gối trái: Khe khớp bình thường, không gãy xương.\n- Công thức máu: WBC 7.1 G/L, HGB 148 g/L, PLT 235 G/L.\n- Đông máu: PT-INR 0.99, APTT 27.8 giây, Fibrinogen 3.10 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 4.9 mmol/L, Creatinin 82 µmol/L, AST 26 U/L, ALT 28 U/L.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật nội soi làm sạch khớp gối trái, bóc nang hoạt dịch dây chằng chéo trước, xử trí tổn thương sụn chêm (Nội soi làm sạch khớp gối trái)",
                Anesthesia = "Gây tê tủy sống",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "10 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Chảy máu trong khớp gối (hemarthrosis), tụ dịch bao hoạt dịch tái phát, nhiễm trùng khớp gối, tổn thương bó mạch - thần kinh khoeo, đứt hoàn toàn dây chằng chéo trước trong mổ, cứng khớp gối sau mổ."
            });

            // BN 5: TRẦN THỊ HUỆ
            list.Add(new PatientProfile {
                Stt = 5,
                PatientCode = "0004056677",
                PatientName = "TRẦN THỊ HUỆ",
                Dob = "05/01/1942",
                Gender = "Nữ",
                Address = "Phường Nam Bình, TP. Ninh Bình, Tỉnh Ninh Bình",
                InTime = "19/09/2026 06:45",
                Diagnosis = "Gãy cổ xương đùi trái Garden IV / Suy tim NYHA II (EF 48%), Tăng huyết áp, Loãng xương người già (S72.0, I50)",
                History = "Tăng huyết áp, suy tim mạn tính EF 48% đang dùng thuốc nội khoa duy trì; loãng xương nặng sau mãn kinh.",
                Course = "Bệnh nhân nữ 84 tuổi, tiền sử THA, suy tim EF 48%, loãng xương. Cách vào viện 5 ngày bị trượt chân ngã đập hông trái xuống sàn nhà, sau ngã đau chói vùng khớp háng trái, bất lực vận động hoàn toàn chân trái, không ngồi dậy hay đi lại được. Vào viện cấp cứu chụp X-quang chẩn đoán gãy rời cổ xương đùi trái di lệch nhiều (Garden IV) -> Chỉ định phẫu thuật thay khớp háng trái toàn phần.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 135/80 mmHg, T° 36.7°C, SpO2 97%. Thể trạng người già 84 tuổi. Khám khớp háng trái: Chân trái ngắn hơn chân phải ~ 2 cm, tư thế xoay ngoài áp sát mặt giường; sưng nề nhẹ mấu chuyển lớn, ấn đau chói tam giác Scarpa; mất cơ năng hoàn toàn chi dưới trái; mạch mu chân và chày sau bắt rõ. Tim: T1, T2 rõ, nhịp đều; Phổi: Thông khí đều, rải rác ít rale ẩm đáy phổi (đã hướng dẫn tập thổi bóng).",
                FullCls = "- X-Quang Khung chậu & Khớp háng trái: Gãy hoàn toàn cổ chính danh xương đùi trái di lệch nhiều (Garden IV), loãng xương nặng khung chậu.\n- Siêu âm tim: Chức năng tâm thu thất trái giảm nhẹ (EF = 48%), dày đồng quy thất trái, hở van 2 lá và 3 lá nhẹ, không có huyết khối buồng tim.\n- Siêu âm Doppler mạch máu chi dưới: Không có huyết khối tĩnh mạch sâu chi dưới.\n- Công thức máu: WBC 7.8 G/L, HGB 108 g/L, PLT 210 G/L.\n- Đông máu: PT-INR 1.02, APTT 29.5 giây, Fibrinogen 3.90 g/L.\n- Sinh hóa máu: Glucose 5.9 mmol/L, Ure 6.5 mmol/L, Creatinin 72 µmol/L, AST 28 U/L, ALT 22 U/L.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay khớp háng trái toàn phần (Thay khớp háng trái toàn phần)",
                Anesthesia = "Gây tê tủy sống hoặc Gây mê nội khí quản",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "13 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Mất máu chu phẫu, biến cố tim mạch (suy tim cấp, tụt huyết áp), thuyên tắc huyết khối tĩnh mạch sâu (DVT) và thuyên tắc phổi (PTE), trật khớp háng nhân tạo sau mổ, vỡ/nứt xương đùi khi đặt chuôi, nhiễm trùng khớp háng nhân tạo, viêm phổi ứ đọng ở người già."
            });

            // BN 6: VIÊN THỊ BẰNG
            list.Add(new PatientProfile {
                Stt = 6,
                PatientCode = "0004062524",
                PatientName = "VIÊN THỊ BẰNG",
                Dob = "10/02/1942",
                Gender = "Nữ",
                Address = "Xã Ninh Khang, Huyện Hoa Lư, Tỉnh Ninh Bình",
                InTime = "22/09/2026 01:21",
                Diagnosis = "Gãy cổ xương đùi trái Garden IV / Nhiễm khuẩn tiết niệu (NKTN), Loãng xương nặng (S72.0, N39.0)",
                History = "Đái buốt đái rắt từng đợt (NKTN), loãng xương nặng sau mãn kinh.",
                Course = "Bệnh nhân nữ 84 tuổi, tiền sử loãng xương. Cách vào viện 2 ngày bị ngã đập hông trái xuống sàn nhà, xuất hiện đau dữ dội vùng khớp háng trái, bất lực vận động hoàn toàn chân trái. Vào viện được chụp X-quang chẩn đoán gãy cổ xương đùi trái di lệch rời (Garden IV), kèm nhiễm khuẩn tiết niệu (đã điều trị kháng sinh ổn định) -> Chỉ định phẫu thuật thay khớp háng trái toàn phần.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 130/75 mmHg, T° 36.8°C, SpO2 98%. Thể trạng người già 84 tuổi (Phòng 3E-16, Giường G47). Khám chi thể: Chân trái ngắn hơn chân phải 1.5 cm, tư thế xoay ngoài; ấn đau chói vùng cổ - mấu chuyển xương đùi trái; không tự nâng nhấc chân lên được; mạch mu chân bắt rõ. Tim phổi ổn định; nước tiểu vàng trong qua sonde.",
                FullCls = "- X-Quang Khớp háng trái: Gãy cổ xương đùi trái thể dưới chỏm di lệch rời (Garden IV), loãng xương nặng.\n- Tổng phân tích nước tiểu: Bạch cầu niệu (+), Nitrit (-) sau điều trị kháng sinh.\n- Siêu âm tim: Chức năng tâm thu thất trái bình thường (EF 60%), xơ hóa van tim người già.\n- Công thức máu: WBC 8.2 G/L, HGB 115 g/L, PLT 255 G/L.\n- Đông máu: PT-INR 0.98, APTT 28.0 giây, Fibrinogen 4.10 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 5.6 mmol/L, Creatinin 65 µmol/L, AST 25 U/L, ALT 20 U/L.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay khớp háng trái toàn phần (Thay khớp háng trái toàn phần)",
                Anesthesia = "Gây tê tủy sống hoặc Gây mê nội khí quản",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "14 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, nhiễm trùng huyết từ ổ nhiễm khuẩn tiết niệu lan vào khớp nhân tạo, thuyên tắc huyết khối tĩnh mạch sâu chi dưới, trật khớp háng nhân tạo, gãy/nứt xương đùi quanh chuôi khớp, viêm phổi thở máy/ứ đọng ở người già."
            });

            // BN 7: TRẦN ĐỨC HƯỞNG
            list.Add(new PatientProfile {
                Stt = 7,
                PatientCode = "0004071288",
                PatientName = "TRẦN ĐỨC HƯỞNG",
                Dob = "18/09/1985",
                Gender = "Nam",
                Address = "Phường Vân Giang, TP. Ninh Bình, Tỉnh Ninh Bình",
                InTime = "24/09/2026 08:00",
                Diagnosis = "Nang hoạt dịch cổ chân trái (M71.3)",
                History = "Chưa phát hiện bệnh lý mạn tính đặc biệt.",
                Course = "Bệnh nhân nam 41 tuổi, xuất hiện khối phồng vùng mặt trước ngoài cổ chân trái khoảng 4 tháng nay, khối tăng dần kích thước, căng tức nhiều khi đi lại hoặc vận động khớp cổ chân, hạn chế đi lại và sinh hoạt. Đã khám và siêu âm/MRI cổ chân chẩn đoán u nang bao hoạt dịch cổ chân trái -> Chỉ định phẫu thuật bóc u nang hoạt dịch cổ chân trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 24 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 120/80 mmHg, T° 36.5°C, SpO2 99%. Thể trạng tốt. Khám cổ chân trái: Khối phồng mặt trước ngoài cổ chân kích thước ~ 2.5 x 3.0 cm, bề mặt nhẵn, mật độ căng chắc, ranh giới rõ, ấn tức nhẹ, không nóng đỏ, dính vào bao khớp/bao gân duỗi; biên độ vận động cổ chân bình thường; mạch mu chân bắt rõ, cảm giác nông sâu bình thường. Tim phổi không có bệnh lý.",
                FullCls = "- Siêu âm & MRI Cổ chân trái: Cấu trúc dạng nang chứa dịch đồng nhất mặt trước ngoài cổ chân trái, kích thước ~ 23 x 28 mm, thành mỏng đều, có cuống thông với bao khớp cổ chân/bao gân duỗi các ngón; xương khớp cổ chân không tổn thương.\n- X-Quang cổ chân: Khung xương khớp cổ chân bình thường.\n- Công thức máu: WBC 6.4 G/L, HGB 152 g/L, PLT 245 G/L.\n- Đông máu: PT-INR 0.98, APTT 28.2 giây, Fibrinogen 3.05 g/L.\n- Sinh hóa máu: Glucose 5.0 mmol/L, Ure 4.8 mmol/L, Creatinin 78 µmol/L, AST 22 U/L, ALT 24 U/L.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật bóc u nang bao hoạt dịch cổ chân trái (Bóc nang hoạt dịch cổ chân trái)",
                Anesthesia = "Gây tê tại chỗ kết hợp tiền mê (hoặc Tê tủy sống)",
                Surgeon = "BS. Bình",
                SurgeryTime = "15 giờ 30 phút, ngày 25 tháng 09 năm 2026",
                Risks = "Chảy máu, tụ dịch vết mổ, tổn thương nhánh thần kinh mác nông gây tê bì mu bàn chân, tổn thương gân duỗi cổ chân, nhiễm trùng vết mổ, tái phát u nang sau mổ do sót cuống nang."
            });

            return list;
        }
    }
}
