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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "Bien_Ban_Thong_Qua_Mo_PT01_20260924_NB");
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

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) NGÀY 24/09/2026 — NINH BÌNH ===", targetList.Count));

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

            Console.WriteLine("\n🎉 HOÀN THÀNH TOÀN BỘ BIÊN BẢN HỘI CHẨN PT-01 NINH BÌNH 24/09 TẠI: " + outDirDated);

            // Đồng bộ trực tiếp lên Google Drive
            try
            {
                Console.WriteLine("\n☁️ Đang đồng bộ lên Google Drive (gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260924_NB)...");
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
                    Arguments = string.Format("copy \"{0}\" \"gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260924_NB\" --quiet", outDirDated),
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(30000);
                    Console.WriteLine("✔ Đã đồng bộ thành công lên Google Drive: gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260924_NB");
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
            // PHIÊN MỔ NINH BÌNH — THỨ NĂM 24/09/2026 — 2 CA
            // PTV chính: TS. Nguyễn Văn Trung
            // =========================================================

            // BN 1: ĐỖ THỊ LIÊN
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0004067770",
                PatientName = "ĐỖ THỊ LIÊN",
                Dob = "01/01/1944",
                Gender = "Nữ",
                Address = "Xã Đông Hưng, Hưng Yên",
                InTime = "22/09/2026 07:24",
                Diagnosis = "Chấn thương cột sống vỡ T11: loãng xương nặng, tăng huyết áp, tai biến mạch máu não cũ",
                History = "Loãng xương nặng sau mãn kinh; Tăng huyết áp; Tai biến mạch máu não cũ đã điều trị ổn định.",
                Course = "Bệnh nhân nữ 82 tuổi, tiền sử loãng xương nặng, THA, tai biến mạch máu não cũ. Cách vào viện khoảng 1 tuần, sau cử động mạnh, xuất hiện đau dữ dội vùng lưng-ngực thấp, đau tăng khi vận động, giảm khi nằm yên, đi lại hạn chế. Bệnh nhân được đưa đến viện khám, chụp phim MRI/CT cột sống phát hiện chấn thương vỡ đốt sống T11 do loãng xương nặng → nhập Khoa Ngoại tổng hợp Tầng 3 Nhà E BVBM CS2 Ninh Bình chỉ định phẫu thuật bơm xi măng sinh học có bóng tạo hình thân đốt sống T11.",
                HoiChanTime = "14 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 145/85 mmHg, T° 36.5°C, SpO2 97%. BN tỉnh, tiếp xúc được, thể trạng trung bình. Cột sống ngực-thắt lưng: Ấn đau chói gai sau và cạnh sống T11; hạn chế vận động do đau; không có hội chứng chèn ép tủy (cơ lực 2 chi dưới 4/5, cảm giác nông bình thường, tiểu tự chủ). Khám tim mạch: HA 145/85, nhịp tim đều; thần kinh khu trú: không có dấu hiệu bại liệt mới so với TBMMN cũ.",
                FullCls = "- Công thức máu: WBC 5.13 G/L, HGB 121 g/L, PLT 233 G/L.\n- Đông máu: PT-INR 0.97, APTT 21.9 giây, Fibrinogen 3.65 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 4.5 mmol/L, Creatinin 60 µmol/L, AST 73 U/L, ALT 80 U/L.\n- Nhóm máu: AB Rh(+).\n- Chẩn đoán hình ảnh: MRI/CT cột sống ngực: Hình ảnh vỡ thân đốt sống T11 (xẹp cấp tính, phù tủy xương), không có mảnh xương chèn vào ống sống; loãng xương nền lan tỏa. X-quang cột sống: giảm chiều cao thân đốt T11, không lệch trục.",
                BloodGroup = "AB Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học có bóng tạo hình thân đốt sống T11 (Balloon Kyphoplasty T11)",
                Anesthesia = "Gây mê nội khí quản hoặc tê tủy sống",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "7 giờ 30 phút, ngày 24 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, rò rỉ xi măng vào ống sống hoặc đĩa đệm gây liệt, phản ứng dị ứng xi măng, thuyên tắc phổi do xi măng, biến cố tim mạch (THA kịch phát, TBMMN tái phát), viêm phổi/nhiễm trùng vết mổ, tái xẹp đốt sống liền kề sau mổ trên nền loãng xương nặng."
            });

            // BN 2: THIỀU THỊ THANH
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0002629196",
                PatientName = "THIỀU THỊ THANH",
                Dob = "20/10/1950",
                Gender = "Nữ",
                Address = "Xã Nam Xang, Ninh Bình",
                InTime = "22/09/2026 09:00",
                Diagnosis = "Xẹp cấp L2, thoái hoá cột sống thắt lưng, rối loạn giấc ngủ",
                History = "Thoái hoá cột sống thắt lưng đã điều trị nội khoa nhiều năm; Rối loạn giấc ngủ.",
                Course = "Bệnh nhân nữ 76 tuổi, tiền sử thoái hoá cột sống thắt lưng, rối loạn giấc ngủ. Cách vào viện khoảng 10 ngày, sau ngã tư thế, xuất hiện đau dữ dội vùng thắt lưng, đau tăng khi đứng ngồi và vận động, đã điều trị giảm đau tại địa phương không đỡ. Vào Khoa Ngoại tổng hợp Tầng 3 Nhà E BVBM CS2 Ninh Bình, chụp MRI cột sống thắt lưng phát hiện xẹp cấp thân đốt sống L2 → chỉ định phẫu thuật bơm xi măng sinh học tạo hình thân đốt sống L2.",
                HoiChanTime = "14 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, tiếp xúc tốt, thể trạng trung bình. Cột sống thắt lưng: ấn đau chói gai sau và cạnh sống L2; hạn chế cúi-ưỡn do đau; Lasègue 2 bên âm tính; cơ lực 2 chi dưới 5/5, cảm giác nông bình thường; đại tiểu tiện tự chủ.",
                FullCls = "- Công thức máu: WBC 7.2 G/L, HGB 121 g/L, PLT 469 G/L.\n- Đông máu: Fibrinogen 4.68 g/L, APTT 24.6 giây (PT-INR chờ kết quả).\n- Sinh hóa máu: Glucose 6.7 mmol/L, Ure 5.9 mmol/L, Creatinin 80 µmol/L, AST 23 U/L, ALT 15 U/L.\n- Nhóm máu: Chưa có kết quả.\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Hình ảnh xẹp cấp thân đốt sống L2, phù tủy xương (giảm tín hiệu T1W, tăng STIR); thoái hoá đĩa đệm và mỏ xương nhiều tầng L2-L5; không chèn ép ống sống nặng. X-quang cột sống thắt lưng: Xẹp đốt sống L2, hẹp khe liên đốt L4-L5.",
                BloodGroup = "Chưa có",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học tạo hình thân đốt sống L2 (Vertebroplasty / Kyphoplasty L2)",
                Anesthesia = "Gây mê nội khí quản hoặc tê tủy sống",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "8 giờ 30 phút, ngày 24 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, rò rỉ xi măng vào ống sống hoặc tĩnh mạch gây liệt/thuyên tắc, biến cố tim mạch, nhiễm trùng vết mổ, tái xẹp đốt sống liền kề trên nền loãng xương."
            });

            return list;
        }
    }
}
