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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "Bien_Ban_Thong_Qua_Mo_PT01_20260923_NB");
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

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) NGÀY 23/09/2026 — NINH BÌNH ===", targetList.Count));

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

            Console.WriteLine("\n🎉 HOÀN THÀNH TOÀN BỘ BIÊN BẢN HỘI CHẨN PT-01 NINH BÌNH 23/09 TẠI: " + outDirDated);

            // Đồng bộ trực tiếp lên Google Drive
            try
            {
                Console.WriteLine("\n☁️ Đang đồng bộ lên Google Drive (gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260923_NB)...");
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
                    Arguments = string.Format("copy \"{0}\" \"gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260923_NB\" --quiet", outDirDated),
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(30000);
                    Console.WriteLine("✔ Đã đồng bộ thành công lên Google Drive: gdrive:Bien_Ban_Thong_Qua_Mo_PT01_20260923_NB");
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
            // PHIÊN MỔ 23/09/2026 — BỆNH VIỆN BẠCH MAI CƠ SỞ NINH BÌNH (KHOA 915)
            // =========================================================================

            // BN 1: ĐINH THỊ LÝ — TLIF L45 phải — PTV: TS. Nguyễn Văn Trung
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0000756322",
                PatientName = "ĐINH THỊ LÝ",
                Dob = "05/01/1951",
                Gender = "Nữ",
                Address = "Xã Giao Minh, Ninh Bình",
                InTime = "21/09/2026 08:22",
                Diagnosis = "Thoát vị đĩa đệm L4/5 ra sau thể di trú xuống dưới lệch phải gây chèn ép đuôi ngựa - Đái tháo đường typ 2 năm thứ 17 - Suy thượng thận do thuốc",
                History = "Đái tháo đường typ 2 điều trị 17 năm, đang dùng Insulin; Suy thượng thận do thuốc corticoid dài ngày; Không tiền sử phẫu thuật cột sống.",
                Course = "Bệnh nhân nữ 75 tuổi, tiền sử đái tháo đường typ 2 và suy thượng thận do thuốc. Cách vào viện khoảng 2 tuần, xuất hiện đau thắt lưng dữ dội lan xuống hai chân kèm tê bì, yếu cơ chi dưới hai bên tiến triển nhanh, đi lại khó khăn, rối loạn cơ tròn (tiểu khó, bí tiểu). Điều trị nội khoa không cải thiện; chụp MRI cột sống thắt lưng phát hiện thoát vị đĩa đệm L4/5 thể di trú xuống dưới lệch phải gây chèn ép nặng đuôi ngựa → nhập viện Khoa CTCH & Cột sống Ninh Bình cần phẫu thuật giải ép và cố định cột sống (TLIF L4-5) cấp cứu.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 130/80 mmHg, T° 36.7°C, SpO2 98%. Bệnh nhân tỉnh, thể trạng trung bình, đi lại phải có người dìu. Cột sống thắt lưng hạn chế vận động rõ do đau; ấn đau chói cạnh sống L4-L5; Hội chứng đuôi ngựa: Cơ lực gấp và duỗi cổ chân hai bên 3/5; Tê bì vùng mông, mặt sau đùi và bàn chân hai bên (anesthesia in saddle); Phản xạ gân gót hai bên giảm rõ; Bàng quang căng, bí tiểu, cần thông tiểu; Lasègue hai bên (+) 30-40 độ. Khám toàn thân: Tim đều; Phổi thông khí rõ; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 13.80 G/L, RBC 4.10 T/L, HGB 129 g/L, PLT 201 G/L.\n- Đông máu: PT-INR 1.05, APTT 28.5 giây, Fibrinogen 2.68 g/L.\n- Sinh hóa máu: Glucose 12.2 mmol/L (theo dõi đường huyết chu phẫu - đái tháo đường typ 2), Ure 12.3 mmol/L, Creatinin 107 µmol/L, AST 42 U/L, ALT 37 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: B Rh(+).\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Thoát vị đĩa đệm L4/5 thể đứt rời di trú xuống dưới lệch phải qua dây chằng dọc sau, chèn ép nặng đuôi ngựa và rễ thần kinh tầng L5-S1 phải; tầng L3-L4 phồng nhẹ. X-quang cột sống thắt lưng: Không thấy mất vững, gai thoái hóa các đốt sống thắt lưng.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "TLIF L4-5 phải (Giải ép và hàn liên thân đốt lối sau tầng L4-5)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "08 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Tổn thương rễ đuôi ngựa gây liệt chi dưới và rối loạn cơ tròn vĩnh viễn, rách màng cứng rò dịch não tủy, chảy máu chu phẫu, biến cố đường huyết chu phẫu trên nền đái tháo đường typ 2 (cần theo dõi glucose sát), suy thượng thận cấp chu phẫu (cần bổ sung corticoid ngắn ngày peri-op), nhiễm trùng vết mổ sâu, lỏng/tuột nẹp vít-cage."
            });

            // BN 2: ĐINH THỊ THÌN — BXM T10 — PTV: BS Lê Đăng Tân
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0004026349",
                PatientName = "ĐINH THỊ THÌN",
                Dob = "03/02/1940",
                Gender = "Nữ",
                Address = "Xã Xuân Trường, Ninh Bình",
                InTime = "16/09/2026 08:44",
                Diagnosis = "Xẹp cấp T10 / Loãng xương - Tăng huyết áp - Phình động mạch chủ bụng đoạn dưới thận",
                History = "Loãng xương sau mãn kinh kèm gãy xương bệnh lý nhiều vị trí; Tăng huyết áp vô căn đang điều trị; Phình động mạch chủ bụng không vỡ.",
                Course = "Bệnh nhân nữ 86 tuổi, tiền sử loãng xương nặng và phình ĐMC bụng chưa vỡ. Cách vào viện 1 tuần, sau khi gắng sức nhẹ tại nhà, xuất hiện đau nhói cột sống ngực lưng vùng D10, đau tăng mạnh khi hít thở sâu hoặc xoay người, không thể tự đứng dậy. Chụp MRI cột sống ngực xác nhận xẹp cấp thân đốt sống T10 phù nề tủy xương → nhập viện Khoa CTCH & Cột sống Ninh Bình phẫu thuật bơm xi măng sinh học tầng T10.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 140/85 mmHg, T° 36.5°C, SpO2 96%. Bệnh nhân tỉnh, thể trạng già yếu, gày. Cột sống ngực vùng T10 ấn đau chói tại gai sau và cạnh sống, gõ dồn gót chân đau dội; không có hội chứng chèn ép tuỷ (cơ lực hai chi dưới 5/5, cảm giác bình thường, đại tiểu tiện tự chủ). Tim đều, không tiếng thổi; Phổi thông khí hai bên, SpO2 96% thở khí phòng; Bụng mềm, ấn hạ sườn bên trái không đau (khối phình ĐMC chưa vỡ).",
                FullCls = "- Công thức máu: WBC - G/L, RBC - T/L, HGB - g/L, PLT - G/L (chưa cập nhật đủ bilan).\n- Đông máu: PT-INR 0.92, APTT 22.0 giây, Fibrinogen 3.57 g/L.\n- Sinh hóa máu: Glucose - mmol/L, Ure 9.5 mmol/L, Creatinin - µmol/L, AST - U/L, ALT - U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: B Rh(+).\n- Chẩn đoán hình ảnh: MRI cột sống ngực: Xẹp cấp thân đốt sống T10, giảm chiều cao ~25%, phù nề tủy xương tăng tín hiệu STIR và giảm T1W, chưa có mảnh rời chèn ép ống sống. Siêu âm bụng/CT bụng: Phình ĐMC bụng đoạn dưới thận đường kính ~3.4cm, thành ổn định, chưa có dấu hiệu vỡ. X-quang tim phổi: Không tràn dịch màng phổi.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học T10 (BXM T10)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Tân",
                SurgeryTime = "09 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu vết chọc kim, rò xi măng sinh học vào ống sống gây chèn ép tuỷ hoặc vào tĩnh mạch gây thuyên tắc phổi, suy hô hấp chu phẫu (SpO2 nền 96%), biến cố tim mạch trên người già 86 tuổi, vỡ phình ĐMC bụng do thay đổi huyết động đột ngột, gãy xẹp thứ phát các đốt sống kế cận."
            });

            // BN 3: LÊ THỊ HẢO — KHX gãy xương đòn trái — PTV: BS Phạm Ngọc Trưởng
            list.Add(new PatientProfile {
                Stt = 3,
                PatientCode = "0004065015",
                PatientName = "LÊ THỊ HẢO",
                Dob = "18/11/1990",
                Gender = "Nữ",
                Address = "Xã Liêm Hà, Ninh Bình",
                InTime = "21/09/2026 21:11",
                Diagnosis = "Gãy xương đòn trái - Theo dõi trật khớp vai trái",
                History = "Khỏe mạnh, không có bệnh lý mạn tính, không tiền sử phẫu thuật.",
                Course = "Bệnh nhân nữ 36 tuổi, khỏe mạnh. Tối 21/09/2026 bị tai nạn giao thông xe máy ngã đập vai trái xuống đường, sau ngã đau nhói vùng xương đòn trái, biến dạng vai trái rõ, không thể giơ tay lên. Được chuyển cấp cứu BV Bạch Mai cơ sở Ninh Bình, chụp X-quang xác nhận gãy 1/3 giữa xương đòn trái di lệch, nghi ngờ thêm trật khớp vai trái → nhập viện Khoa CTCH & Cột sống phẫu thuật kết hợp xương nẹp vít xương đòn trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 115/75 mmHg, T° 36.8°C, SpO2 99%. Bệnh nhân tỉnh, hợp tác tốt. Vùng 1/3 giữa xương đòn trái sưng nề bầm tím, gồ xương dưới da, ấn đau chói và có cử động bất thường; khớp vai trái giới hạn vận động rõ do đau, vùng đỉnh khớp không thấy hõm rõ (khớp vai không trật rõ). Chi trên trái: Cảm giác cánh tay ngoài tê nhẹ (nhánh thần kinh da cánh tay ngoài), cơ lực vai 4/5 do đau, bàn ngón tay cơ lực 5/5, mạch quay bắt rõ 2 bên, ngón tay hồng ấm. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 11.28 G/L, RBC 4.60 T/L, HGB 130 g/L, PLT 328 G/L.\n- Đông máu: PT-INR 0.97, APTT 30.0 giây, Fibrinogen 2.57 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 5.5 mmol/L, Creatinin 66 µmol/L, AST 21 U/L, ALT 13 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: O Rh(+).\n- Chẩn đoán hình ảnh: X-quang xương đòn và vai trái: Gãy 1/3 giữa xương đòn trái di lệch lên trên; khớp vai đúng vị trí (không trật). X-quang ngực: Bình thường, không tràn khí tràn máu màng phổi.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX nẹp vít xương đòn trái",
                Anesthesia = "Mê nội khí quản (hoặc Tê đám rối thần kinh cánh tay liên cơ bậc thang)",
                Surgeon = "BS. Phạm Ngọc Trưởng",
                SurgeryTime = "10 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Tổn thương bó mạch dưới đòn và đám rối thần kinh cánh tay, tràn khí/tràn máu màng phổi do đầu dụng cụ chọc đỉnh phổi, tụ máu vết mổ, gãy/bung nẹp vít sau mổ, nhiễm trùng vết mổ, can xương chậm hoặc không liền xương."
            });

            // BN 4: NGÔ THỊ LIÊN — BXM L2 — PTV: BS Lê Đăng Tân
            list.Add(new PatientProfile {
                Stt = 4,
                PatientCode = "0004064449",
                PatientName = "NGÔ THỊ LIÊN",
                Dob = "20/02/1958",
                Gender = "Nữ",
                Address = "Phường Trường Thi, Ninh Bình",
                InTime = "21/09/2026 19:30",
                Diagnosis = "Xẹp cấp L2 do chấn thương sinh hoạt / Tăng huyết áp",
                History = "Tăng huyết áp vô căn đang điều trị thuốc hạ áp; không có tiền sử phẫu thuật cột sống.",
                Course = "Bệnh nhân nữ 68 tuổi, tiền sử THA. Tối 21/09/2026, trong khi làm việc nặng tại nhà bị ngã ngồi xuống sàn cứng, sau ngã đau chói vùng thắt lưng không thể tự đứng dậy được. Được chuyển cấp cứu BV Bạch Mai cơ sở Ninh Bình, chụp MRI cột sống thắt lưng xác nhận xẹp cấp thân đốt sống L2 do chấn thương phù nề tủy xương → nhập viện Khoa CTCH & Cột sống Ninh Bình phẫu thuật bơm xi măng sinh học L2.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 145/90 mmHg (đang hạ áp), T° 36.6°C, SpO2 98%. Bệnh nhân tỉnh, thể trạng trung bình. Cột sống thắt lưng ấn đau chói tại gai sau L2 và cạnh sống L2 hai bên, gõ dồn gót chân đau dội vùng TL; không có hội chứng chèn ép rễ (Lasègue 2 bên âm tính, cơ lực chi dưới 5/5, cảm giác bình thường, đại tiểu tiện tự chủ). Tim đều; Phổi thông khí rõ; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.78 G/L, RBC 4.40 T/L, HGB 134 g/L, PLT 185 G/L.\n- Đông máu: PT-INR 0.96, APTT 26.3 giây, Fibrinogen 3.47 g/L.\n- Sinh hóa máu: Glucose - mmol/L, Ure 5.5 mmol/L, Creatinin 85 µmol/L, AST 28 U/L, ALT 18 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: chưa có kết quả.\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Xẹp cấp thân đốt sống L2 giảm chiều cao ~20-25% so với đốt sống kề, phù nề tủy xương tăng tín hiệu trên STIR và giảm T1W, không có mảnh rời chèn ép ống sống. X-quang CSTL: Xẹp đốt sống L2, loãng xương trung bình.",
                BloodGroup = "chưa xác định",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học L2 (BXM L2)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Tân",
                SurgeryTime = "11 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu vết chọc kim, rò xi măng sinh học vào ống sống gây chèn ép rễ thần kinh, tắc mạch phổi do xi măng rò vào tĩnh mạch, tụt huyết áp phản xạ chu phẫu, tăng huyết áp kịch phát, nhiễm trùng vết chọc, gãy xẹp thứ phát đốt sống kề L1 hoặc L3."
            });

            // BN 5: NGUYỄN THỊ LAN — BXM T12L2 — PTV: TS. Nguyễn Văn Trung
            list.Add(new PatientProfile {
                Stt = 5,
                PatientCode = "0004046492",
                PatientName = "NGUYỄN THỊ LAN",
                Dob = "30/10/1948",
                Gender = "Nữ",
                Address = "Xã Hiệp Cường, Hưng Yên",
                InTime = "15/09/2026 16:37",
                Diagnosis = "Loãng xương nặng xẹp cấp L2 / Đau thần kinh tọa phải - Tăng huyết áp - Đái tháo đường typ 2 - Tiền sử chấn thương gãy xương bánh chè trái",
                History = "Loãng xương nặng (T-score -3.7); Tăng huyết áp đang điều trị; Đái tháo đường typ 2 đang điều trị; Tiền sử gãy xương bánh chè trái đã điều trị bảo tồn.",
                Course = "Bệnh nhân nữ 78 tuổi, tiền sử loãng xương nặng, THA, ĐTĐ typ 2. Cách vào viện khoảng 1 tuần, sau sinh hoạt bình thường xuất hiện đau thắt lưng lan xuống chân phải dữ dội, không thể đứng hoặc đi lại được. Chụp MRI phát hiện xẹp cấp thân đốt sống T12 và L2 phù nề tủy xương. Đã hội chẩn nội tiết kiểm soát đường huyết và huyết áp. Hiện đủ điều kiện phẫu thuật bơm xi măng sinh học T12 và L2.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 135/85 mmHg (đang hạ áp và kiểm soát đường huyết), T° 36.5°C, SpO2 97%. Bệnh nhân tỉnh, thể trạng già yếu. Cột sống ngực-thắt lưng ấn đau chói tại gai sau T12 và L2, gõ dồn từ gót chân đau dội; không có hội chứng chèn ép rễ nặng (Lasègue phải (+) 50 độ - đau thần kinh tọa mạn); cơ lực hai chân 4/5; cảm giác bình thường; đại tiểu tiện tự chủ. Tim đều; Phổi thông khí; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.4 G/L, RBC 4.40 T/L, HGB 140 g/L, PLT 267 G/L.\n- Đông máu: PT-INR 0.95, APTT 25.3 giây, Fibrinogen 4.28 g/L.\n- Sinh hóa máu: Glucose - mmol/L (kiểm soát đường huyết theo HC nội tiết), Ure - mmol/L, Creatinin 67 µmol/L, AST 20 U/L, ALT 10 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: B Rh(+).\n- Đo mật độ xương DEXA: T-score cột sống thắt lưng -3.7 (Loãng xương nặng).\n- Chẩn đoán hình ảnh: MRI cột sống ngực-thắt lưng: Xẹp cấp thân đốt sống T12 và L2, phù nề tủy xương tăng tín hiệu STIR rõ ở cả hai tầng; không có mảnh rời chèn ép ống sống. Đĩa đệm L4/5 thoái hóa phồng nhẹ.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Bơm xi măng sinh học T12 và L2 (BXM T12L2)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "13 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu vết chọc kim, rò xi măng sinh học vào ống sống hoặc tĩnh mạch (nguy cơ cao do loãng xương nặng T-3.7), tắc mạch phổi do xi măng, biến cố đường huyết chu phẫu (ĐTĐ typ 2), tăng huyết áp kịch phát, nhiễm trùng vết chọc, gãy xẹp thứ phát các đốt sống kề T11, L1, L3."
            });

            // BN 6: PHẠM VĂN THỦY — BXM/CĐCS L1 — PTV: BS Lê Đăng Tân
            list.Add(new PatientProfile {
                Stt = 6,
                PatientCode = "0004060357",
                PatientName = "PHẠM VĂN THỦY",
                Dob = "01/09/1972",
                Gender = "Nam",
                Address = "Phường Nam Hoa Lư, Ninh Bình",
                InTime = "20/09/2026 18:41",
                Diagnosis = "Vỡ xẹp L1 sau ngã cao 3m",
                History = "Khỏe mạnh trước chấn thương; Không có bệnh lý mạn tính đặc biệt.",
                Course = "Bệnh nhân nam 54 tuổi, khỏe mạnh. Chiều 20/09/2026 bị tai nạn lao động ngã từ độ cao 3m, đập lưng xuống mặt đất. Sau ngã đau thắt lưng dữ dội, bất lực vận động chi dưới, được chuyển cấp cứu BV Bạch Mai cơ sở Ninh Bình. CT Scanner và MRI cột sống thắt lưng phát hiện vỡ xẹp thân đốt sống L1 có mảnh xương rời sau. Hội chẩn khẩn: Nếu có chèn ép tủy → CĐCS ít xâm lấn; nếu chưa chèn ép nặng → BXM có bóng (kyphoplasty) giữ chiều cao.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 84 ck/p, HA 135/85 mmHg (phản ứng đau), T° 37.0°C, SpO2 99%. Bệnh nhân tỉnh, đau nhiều, thể trạng tốt. Cột sống thắt lưng ấn đau chói tại gai sau L1, gõ dồn đau dội mạnh; vùng L1 căng cứng cơ cạnh sống. Thần kinh: Cơ lực hai chi dưới 4/5 (giảm nhẹ do đau cấp tính, không liệt hoàn toàn); Cảm giác đau tê lan xuống hai chân nhẹ; Lasègue hai bên (+) 50 độ; không rối loạn cơ tròn. Tim phổi bình thường.",
                FullCls = "- Công thức máu: WBC 9.50 G/L, RBC 5.20 T/L, HGB 154 g/L, PLT 178 G/L.\n- Đông máu: PT-INR 1.13, APTT 27.6 giây, Fibrinogen 2.96 g/L.\n- Sinh hóa máu: Glucose 9.8 mmol/L (tăng phản ứng stress), Ure 6.0 mmol/L, Creatinin 84 µmol/L, AST 40 U/L, ALT 26 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: O Rh(+).\n- Chẩn đoán hình ảnh: CT Scanner cột sống thắt lưng: Vỡ thân đốt sống L1 loại burst fracture, mảnh xương sau di lệch vào ống sống <30%, giảm chiều cao thân đốt ~40%. MRI cột sống thắt lưng: Phù nề tuỷ xương L1 cấp tính, không tăng tín hiệu phù tuỷ sống tầng tổn thương; đĩa đệm L1/2 và T12/L1 chưa tổn thương rõ.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "BXM có bóng (Kyphoplasty) L1 / hoặc CĐCS ít xâm lấn tùy đánh giá trong mổ",
                Anesthesia = "Tiền mê + Tê tại chỗ (hoặc Mê nội khí quản nếu cần CĐCS)",
                Surgeon = "BS. Lê Đăng Tân",
                SurgeryTime = "14 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Rò xi măng vào ống sống làm nặng thêm chèn ép (đặc biệt khi đã có mảnh xương sau), tổn thương tuỷ sống và rễ thần kinh, tắc mạch phổi do xi măng, chảy máu chu phẫu, chuyển mổ mở CĐCS nếu trong mổ phát hiện chèn ép nặng hơn đánh giá lâm sàng, nhiễm trùng vết mổ."
            });

            // BN 7: PHẠM VĂN VIỆT — Nối gân Achilles phải — PTV: BS Lê Văn Luân
            list.Add(new PatientProfile {
                Stt = 7,
                PatientCode = "0003997648",
                PatientName = "PHẠM VĂN VIỆT",
                Dob = "20/11/1981",
                Gender = "Nam",
                Address = "Xã Giao Phúc, Ninh Bình",
                InTime = "21/09/2026 08:11",
                Diagnosis = "Đứt gân Achille phải tháng thứ 3 + Viêm khuỷu phải",
                History = "Khỏe mạnh; không có bệnh lý mạn tính; không tiền sử phẫu thuật.",
                Course = "Bệnh nhân nam 45 tuổi, khỏe mạnh. Cách đây 3 tháng bị đứt gân Achille phải trong lúc chơi thể thao (nghe tiếng 'bộp' vùng gân gót phải, sau đó không thể kiễng chân). Điều trị bảo tồn bó bột nhưng sau tháo bột vẫn đi khập khiễng rõ, không kiễng chân được → khám lại xác nhận đứt gân Achille phải giai đoạn mạn tính 3 tháng cần phẫu thuật tái tạo. Kèm theo viêm mỏm trên lồi cầu ngoài khuỷu phải mạn tính.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.7°C, SpO2 99%. Bệnh nhân tỉnh, thể trạng tốt. Khám gân gót phải: Vùng gân Achille phải sờ thấy khe hở ~3cm so với đường bình thường của gân; Thompson test (+) mạnh (bóp bắp chân không thấy cử động gấp lòng bàn chân); không thể kiễng chân phải đứng đơn độc; sẹo cũ vùng gân gót phải mềm (do chấn thương cũ). Khám khuỷu phải: Ấn đau chói điểm mỏm trên lồi cầu ngoài, đau tăng khi duỗi thụ động cổ tay và ngón tay có kháng lực. Mạch chày sau và mu chân phải bắt rõ, cảm giác bàn ngón chân bình thường.",
                FullCls = "- Công thức máu: WBC 4.63 G/L, RBC 5.30 T/L, HGB 155 g/L, PLT 265 G/L.\n- Đông máu: PT-INR 0.97, APTT 27.6 giây, Fibrinogen 2.83 g/L.\n- Sinh hóa máu: Glucose 5.6 mmol/L, Ure 5.7 mmol/L, Creatinin 93 µmol/L, AST 30 U/L, ALT 29 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: O Rh(+).\n- Chẩn đoán hình ảnh: Siêu âm gân Achille phải: Đứt hoàn toàn gân Achille phải tầng 3 tháng, hai đầu gân co rút cách nhau ~3cm, không có dấu hiệu liền tự nhiên. X-quang cổ chân phải: Không gãy xương.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Nối gân Achilles phải (phẫu thuật tái tạo gân Achille đứt mạn tính)",
                Anesthesia = "Tê tủy sống",
                Surgeon = "BS. Lê Văn Luân",
                SurgeryTime = "08 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu, tổn thương thần kinh hiển ngoài (cảm giác mặt ngoài bàn chân), nhiễm trùng vết mổ gân, đứt lại hoặc lỏng đường khâu nối gân chu phẫu sớm (nguy cơ cao do gân teo teo sau 3 tháng đứt mạn), cứng khớp cổ chân hạn chế gấp lưng sau mổ, liền gân chậm."
            });

            // BN 8: TRẦN VĂN THƯỞNG — KHX gãy 1/3 dưới xương chày phải — PTV: BS Lê Văn Luân
            list.Add(new PatientProfile {
                Stt = 8,
                PatientCode = "0004060343",
                PatientName = "TRẦN VĂN THƯỞNG",
                Dob = "22/11/1983",
                Gender = "Nam",
                Address = "Xã Nam Cường, Hưng Yên",
                InTime = "20/09/2026 18:30",
                Diagnosis = "Gãy phức tạp 1/3 dưới xương chày phải",
                History = "Khỏe mạnh trước chấn thương; không có bệnh lý mạn tính.",
                Course = "Bệnh nhân nam 43 tuổi, khỏe mạnh. Tối 20/09/2026 bị tai nạn giao thông, đập cẳng chân phải trực tiếp xuống vật cứng. Sau tai nạn đau nhói dữ dội 1/3 dưới cẳng chân phải, biến dạng rõ, không thể cử động. Được chuyển cấp cứu BV Bạch Mai cơ sở Ninh Bình, X-quang xác nhận gãy phức tạp nhiều mảnh 1/3 dưới xương chày phải → nhập viện Khoa CTCH phẫu thuật kết hợp xương.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 82 ck/p, HA 125/80 mmHg, T° 36.9°C, SpO2 99%. Bệnh nhân tỉnh, đau nhiều, thể trạng tốt. Khám cẳng chân phải: Vùng 1/3 dưới cẳng chân phải sưng nề tím bầm rõ, biến dạng góc; ấn đau chói và có tiếng lạo xạo xương; da vùng trên ổ gãy căng nhưng chưa có phỏng nước; không vết thương hở (gãy kín). Mạch mu chân phải bắt rõ, CRT <2s, ngón chân hồng ấm; cảm giác mu chân và lòng bàn chân bình thường; gấp duỗi ngón chân còn làm được; không dấu hiệu chèn ép khoang sớm (khoang cẳng chân mềm). Siêu âm mạch máu cẳng chân phải: chưa có chèn ép mạch.",
                FullCls = "- Công thức máu: WBC 10.7 G/L, RBC 5.10 T/L, HGB 149 g/L, PLT 253 G/L.\n- Đông máu: PT-INR 0.99, APTT 27.9 giây, Fibrinogen 2.68 g/L.\n- Sinh hóa máu: Glucose 6.6 mmol/L, Ure 4.8 mmol/L, Creatinin 73 µmol/L, AST 42 U/L, ALT 40 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: O Rh(+).\n- Chẩn đoán hình ảnh: X-quang cẳng chân phải: Gãy phức tạp nhiều mảnh 1/3 dưới thân xương chày phải di lệch; xương mác phải nguyên vẹn. CT Scanner cẳng chân (nếu cần): đánh giá chi tiết các mảnh gãy vùng siêu âm.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX đinh nội tuỷ hoặc nẹp vít xương chày phải (tùy đánh giá trong mổ)",
                Anesthesia = "Tê tủy sống",
                Surgeon = "BS. Lê Văn Luân",
                SurgeryTime = "10 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu chu phẫu, hội chứng chèn ép khoang cẳng chân sau mổ, tổn thương mạch máu thần kinh cẳng chân (thần kinh mác nông/sâu), nhiễm trùng vết mổ kết hợp xương, can lệch hoặc chậm liền xương do ổ gãy nhiều mảnh, nứt vỡ thêm do đóng đinh nội tủy."
            });

            // BN 9: TRỊNH THỊ BẮC — KHX bánh chè — PTV: BS Phạm Ngọc Trưởng
            list.Add(new PatientProfile {
                Stt = 9,
                PatientCode = "0004059455",
                PatientName = "TRỊNH THỊ BẮC",
                Dob = "25/12/1979",
                Gender = "Nữ",
                Address = "Xã Thanh Bình, Ninh Bình",
                InTime = "20/09/2026 09:30",
                Diagnosis = "Gãy xương bánh chè / Viêm khớp dạng thấp - Viêm dạ dày",
                History = "Viêm khớp dạng thấp đang điều trị DMARDs (Methotrexate, Hydroxychloroquine); Viêm dạ dày mạn tính đang điều trị.",
                Course = "Bệnh nhân nữ 47 tuổi, tiền sử viêm khớp dạng thấp. Cách vào viện 2 ngày, bị ngã đập đầu gối trái xuống nền cứng, sau ngã đau nhói vùng xương bánh chè trái dữ dội, sưng nề rõ, không thể duỗi thẳng gối ra được. Được chuyển đến BV Bạch Mai cơ sở Ninh Bình, chụp X-quang xác nhận gãy xương bánh chè trái di lệch → nhập viện Khoa CTCH phẫu thuật kết hợp xương bánh chè.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 120/80 mmHg, T° 36.6°C, SpO2 99%. Bệnh nhân tỉnh, thể trạng trung bình. Khám gối trái: Sưng nề rõ vùng bánh chè, bầm tím rộng mặt trước gối; sờ thấy khe hở xương bánh chè ~1.5cm; ấn đau chói; Không thể duỗi thẳng chân trái chủ động (mất liên tục bộ máy duỗi); Không tràn máu khớp gối rõ. Mạch mu chân trái bắt rõ, ngón chân hồng ấm, cảm giác bình thường. Toàn thân: Tim đều; Phổi thông khí; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 12.43 G/L, RBC 4.50 T/L, HGB 133 g/L, PLT 343 G/L.\n- Đông máu: PT-INR 0.89, APTT 24.1 giây, Fibrinogen 3.42 g/L.\n- Sinh hóa máu: Glucose 5.6 mmol/L, Ure 4.9 mmol/L, Creatinin 57 µmol/L, AST 21 U/L, ALT 28 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: O Rh(+).\n- Chẩn đoán hình ảnh: X-quang gối trái: Gãy ngang xương bánh chè trái di lệch ~15mm, hai mảnh tách rời. Thang đánh giá chức năng: Mất khả năng duỗi chủ động gối.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "KHX xương bánh chè trái (buộc chỉ thép kiểu số 8 / nẹp vít)",
                Anesthesia = "Tê tủy sống",
                Surgeon = "BS. Phạm Ngọc Trưởng",
                SurgeryTime = "09 giờ 00 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu, nhiễm trùng vết mổ (nguy cơ tăng trên nền VKDT - điều trị ức chế miễn dịch), tụ máu khớp gối sau mổ, gãy/bung chỉ thép hoặc vít cố định, can xương chậm do VKDT, cứng khớp gối hạn chế gấp sau mổ, viêm loét dạ dày cấp do thuốc kháng viêm chu phẫu."
            });

            // BN 10: ZHANG, JIE — Tháo vít cổ chân phải — PTV: BS Lê Văn Luân
            list.Add(new PatientProfile {
                Stt = 10,
                PatientCode = "0003857730",
                PatientName = "ZHANG, JIE",
                Dob = "09/02/1978",
                Gender = "Nam",
                Address = "Phường Duy Hà, Ninh Bình",
                InTime = "21/09/2026 08:56",
                Diagnosis = "Còn nẹp vít cổ chân phải - Sau mổ KHX tháng thứ 2",
                History = "Tiền sử gãy xương cổ chân phải đã phẫu thuật KHX nẹp vít cách 2 tháng; xương liền tốt; đủ điều kiện tháo phương tiện kết hợp xương.",
                Course = "Bệnh nhân nam 48 tuổi. Cách đây 2 tháng bị gãy xương cổ chân phải đã được phẫu thuật kết hợp xương nẹp vít tại BV Bạch Mai cơ sở Ninh Bình. Hiện khớp cổ chân phải liền xương tốt trên X-quang kiểm tra; bệnh nhân đau tức vùng nẹp vít khi vận động; X-quang xác nhận nẹp vít còn nguyên vị trí → nhập viện Khoa CTCH phẫu thuật tháo phương tiện kết hợp xương cổ chân phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 22 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 70 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. Bệnh nhân tỉnh, thể trạng tốt. Khám cổ chân phải: Sẹo mổ cũ mặt ngoài cổ chân phải liền sẹo tốt, mềm không đỏ; sờ thấy nẹp và vít dưới da; ấn đau nhẹ vùng nẹp; tầm vận động cổ chân phải hạn chế nhẹ (gấp lưng 10 độ, gấp lòng 40 độ). Mạch mu chân phải bắt rõ, ngón chân hồng ấm, cảm giác bình thường. Tim phổi bụng bình thường.",
                FullCls = "- Công thức máu: WBC 5.85 G/L, RBC 5.60 T/L, HGB 169 g/L, PLT 211 G/L.\n- Đông máu: PT-INR 0.89, APTT 27.8 giây, Fibrinogen 2.13 g/L.\n- Sinh hóa máu: Glucose 4.0 mmol/L, Ure 5.2 mmol/L, Creatinin 110 µmol/L, AST 24 U/L, ALT 36 U/L.\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Nhóm máu: chưa có kết quả.\n- Chẩn đoán hình ảnh: X-quang cổ chân phải: Xương cổ chân phải liền tốt, không còn đường gãy; nẹp vít còn nguyên vị trí tốt, không lỏng tuột vít.",
                BloodGroup = "chưa xác định",
                BloodReserve = "0",
                SurgeryMethod = "Tháo vít / Tháo phương tiện KHX cổ chân phải",
                Anesthesia = "Tê tủy sống (hoặc Tê vùng cổ chân)",
                Surgeon = "BS. Lê Văn Luân",
                SurgeryTime = "11 giờ 30 phút, ngày 23 tháng 09 năm 2026",
                Risks = "Chảy máu vết mổ, gãy xương khi tháo vít (nếu xương chưa đủ vững), tổn thương thần kinh mác nông khi bóc tách, nhiễm trùng vết mổ, vít bị khóa khó tháo do rỉ sét/bã xương mọc phủ đầu vít."
            });

            return list;
        }
    }
}

