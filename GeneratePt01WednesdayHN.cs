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
        public string Room;
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

            string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01", "PT01_20260930_HN");
            string outDirStd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(outDirDated)) Directory.CreateDirectory(outDirDated);
            if (!Directory.Exists(outDirStd)) Directory.CreateDirectory(outDirStd);

            List<PatientProfile> allList = BuildProfiles();

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) LỊCH MỔ THỨ TƯ 30/09/2026 — KHOA 57 HÀ NỘI ===", allList.Count));

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
                string fileNameDated = string.Format("{0:D2}_PT01_BienBanThongQuaMo_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
                string fileNameStd = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);

                string pathDated = Path.Combine(outDirDated, fileNameDated);
                string pathStd = Path.Combine(outDirStd, fileNameStd);

                doc.Save(pathDated);
                File.Copy(pathDated, pathStd, true);
                Console.WriteLine("  ✔ Đã xuất: " + fileNameDated);
            }

            Console.WriteLine(string.Format("\n🎉 XUẤT THÀNH CÔNG 100% {0} BIÊN BẢN PT-01 TẠI:\n📂 {1}", allList.Count, outDirDated));
        }

        static List<PatientProfile> BuildProfiles()
        {
            var list = new List<PatientProfile>();

            // =========================================================================
            // 1. NGUYỄN THỊ THÌN (74t) - P723 - G43 | PTV: TS. Nguyễn Văn Trung
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 1,
                Room = "Phòng mổ Cột sống (Khoa 57)",
                PatientCode = "0004062838",
                PatientName = "NGUYỄN THỊ THÌN",
                Dob = "01/01/1952",
                Gender = "Nữ",
                Address = "Xã Hoài Đức, Thành phố Hà Nội",
                InTime = "09:25 ngày 24/09/2026",
                Diagnosis = "Thoát vị đĩa đệm đa tầng cột sống cổ (C2-C6) - Hội chứng chèn ép tủy cổ / Tăng huyết áp vô căn",
                History = "Tăng huyết áp điều trị đều bằng thuốc hạ áp hàng ngày; Thoái hóa cột sống cổ nhiều năm.",
                Course = "Bệnh nhân nữ 74 tuổi, tiền sử tăng huyết áp, đau mỏi cổ gáy mạn tính nhiều năm. Khoảng 3 tháng gần đây xuất hiện đau cổ gáy tăng nhiều, lan xuống hai vai và hai tay, tê bì dị cảm bàn tay hai bên, cầm nắm đồ vật không khéo, đi lại có cảm giác không vững như đi trên đệm. Đã điều trị nội khoa và phục hồi chức năng nhiều đợt không thuyên giảm. Được người nhà đưa vào BV Bạch Mai khám, chụp MRI cột sống cổ phát hiện thoát vị đĩa đệm đa tầng từ C2 đến C6 chèn ép tủy cổ gây hẹp nặng ống sống -> Nhập Khoa 57 chỉ định phẫu thuật lối trước cắt đĩa đệm, giải ép tủy và ghép xương hàn khớp liên thân đốt ACDF.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 135/85 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, tiếp xúc tốt. Cột sống cổ: Hạn chế biên độ cúi ngửa xoay cổ do đau; ấn đau các gai sau C3-C6; dấu hiệu bấm chuông (+); Spurling (+); Hoffman (+) hai bên, tăng phản xạ gân xương chi trên và chi dưới hai bên; cơ lực chi trên 4/5, cơ lực chi dưới 4/5, đi lại loạng choạng, Babinski (-). Đại tiểu tiện tự chủ. Tim đều T1 T2 rõ; phổi thông khí tốt; bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.2 G/L, HGB 122 g/L, PLT 218 G/L.\n- Đông máu: PT-INR 1.01, APTT 0.90, Fibrinogen 2.95 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 4.9 mmol/L, Creatinin 68 µmol/L, AST 26 U/L, ALT 21 U/mL, Điện giải Na 139, K 4.0, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường (10 thông số âm tính).\n- CĐHA: MRI cột sống cổ: Thoái hóa và thoát vị đĩa đệm đa tầng C2-C3, C3-C4, C4-C5, C5-C6 gây hẹp nặng ống sống cổ tương ứng, chèn ép mặt trước tủy cổ kèm tăng tín hiệu tủy cổ trên T2W (tổn thương tủy cổ); gai xương thân đốt; X-quang cột sống cổ thẳng - nghiêng: Thoái hóa cột sống cổ, xơ đặc xương dưới sụn, hẹp khe đĩa đệm đa tầng.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật lối trước cắt đĩa đệm giải ép tủy ghép xương hàn khớp liên thân đốt cột sống cổ đa tầng (ACDF C2-C6) bằng nẹp vít và lồng hợp kim PEEK",
                Anesthesia = "Gây mê nội khí quản",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "08 giờ 00 phút, ngày 30/09/2026",
                Risks = "Tổn thương tủy cổ và rễ thần kinh gây liệt tứ chi hoặc suy hô hấp sau mổ, rách màng cứng rò dịch não tủy, tổn thương thực quản - khí quản gây thủng hoặc khó nuốt kéo dài, tổn thương thần kinh thanh quản quặt ngược gây khàn tiếng, tổn thương động mạch cảnh/đốt sống gây chảy máu ồ ạt, tụ máu chèn ép vùng cổ sau mổ gây suy hô hấp cấp, bung hoặc lún nẹp vít/lồng ghép, nhiễm trùng vết mổ."
            });

            // =========================================================================
            // 2. LÊ VĂN HÒA (37t) - P712 - G21 | PTV: TS. Nguyễn Văn Trung
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 2,
                Room = "Phòng mổ Cột sống (Khoa 57)",
                PatientCode = "0001530113",
                PatientName = "LÊ VĂN HÒA",
                Dob = "15/11/1989",
                Gender = "Nam",
                Address = "Xã Yên Cường, Ninh Bình",
                InTime = "08:35 ngày 18/09/2026",
                Diagnosis = "Thoát vị đĩa đệm cột sống cổ C6-C7 bên trái chèn ép rễ C7 trái / Tăng men gan (AST 117, ALT 238)",
                History = "Khỏe mạnh, không tiền sử dị ứng, tiền sử sử dụng rượu bia nhẹ dẫn đến tăng men gan.",
                Course = "Bệnh nhân nam 37 tuổi, khởi bệnh 1 tháng nay sau khi bê vác nặng xuất hiện đau dữ dội vùng cổ gáy lan xuống vai trái, cánh cẳng tay và ngón 2-3 bàn tay trái theo đường đi rễ C7, đau tăng khi ngửa cổ và quay đầu sang trái, kèm tê bì nhiều mặt sau cẳng tay và các ngón tay trái, lực bóp bàn tay trái giảm sút. Đã dùng thuốc giảm đau giãn cơ nội khoa không đỡ. Vào khám tại BV Bạch Mai, chụp MRI chẩn đoán TVĐĐ cột sống cổ C6-C7 thể cạnh trung tâm - lỗ liên hợp bên trái chèn ép rễ C7 -> Hội chẩn chỉ định phẫu thuật nội soi lấy nhân thoát vị đĩa đệm cổ C6-C7 giải ép rễ thần kinh.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.7°C, SpO2 99%. Thể trạng tốt, tiếp xúc nhanh nhẹn. Cột sống cổ: Co cứng cơ cạnh sống cổ trái; ấn điểm cạnh gai sau C6-C7 bên trái đau chói lan xuống ngón 2-3; Spurling (+) bên trái; Eaton (+) bên trái; giảm cảm giác ngón 2-3 bàn tay trái; cơ lực nhóm cơ duỗi cẳng tay và gập cổ tay trái 4/5; phản xạ gân cơ tam đầu cánh tay trái giảm so với bên phải. Không có dấu hiệu chèn ép tủy (Hoffman (-), Babinski (-)). Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.1 G/L, HGB 164 g/L, PLT 254 G/L.\n- Đông máu: PT-INR 0.97, APTT 0.99, Fibrinogen 2.94 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 6.4 mmol/L, Creatinin 100 µmol/L, AST 117 U/L (tăng nhẹ-vừa), ALT 238 U/mL (tăng vừa), Điện giải Na 138, K 4.1, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số âm tính.\n- CĐHA: MRI cột sống cổ: Khối thoát vị đĩa đệm tầng C6-C7 lệch trái thể cạnh trung tâm - lỗ liên hợp kích thước ~ 4.5 mm, chèn ép nặng rễ thần kinh C7 bên trái và mặt trước bên bao màng cứng; không phù tủy; X-quang cột sống cổ: Đường cong sinh lý giảm nhẹ, hẹp nhẹ khe gian đốt C6-C7.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật nội soi lấy nhân thoát vị đĩa đệm cột sống cổ C6-C7 bên trái giải ép rễ thần kinh qua lối sau (Posterior Cervical Endoscopic Discectomy / Foraminotomy C6-C7)",
                Anesthesia = "Gây mê nội khí quản (lưu ý bảo vệ gan chu phẫu)",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "09 giờ 30 phút, ngày 30/09/2026",
                Risks = "Tổn thương rễ thần kinh C7 gây tê bì yếu liệt cơ duỗi cánh tay/ngón tay, rách màng tủy rò dịch não tủy, chảy máu từ đám rối tĩnh mạch ngoài màng cứng, tái phát thoát vị đĩa đệm, tổn thương tủy cổ, chuyển mổ mở nếu nội soi khó khăn hoặc chảy máu khó cầm, biến chứng suy giảm chức năng gan do thuốc mê."
            });

            // =========================================================================
            // 3. PHAN THỊ TƯ (60t) - P723 - G41 | PTV: BS Lê Đăng Tân
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 3,
                Room = "Phòng mổ Cột sống (Khoa 57)",
                PatientCode = "0004079114",
                PatientName = "PHAN THỊ TƯ",
                Dob = "01/01/1966",
                Gender = "Nữ",
                Address = "Xã Phú Nghĩa, Thành phố Hà Nội",
                InTime = "05:00 ngày 26/09/2026",
                Diagnosis = "Xẹp cấp thân đốt sống L4 - Xẹp cũ L2, L3 - Loãng xương nặng - Suy thượng thận thứ phát do Corticoid",
                History = "Đau xương khớp mạn tính tự mua thuốc nam/corticoid uống kéo dài nhiều năm dẫn đến suy thượng thận thứ phát; Loãng xương nặng.",
                Course = "Bệnh nhân nữ 60 tuổi, tiền sử lạm dụng Corticoid điều trị khớp gây suy thượng thận và loãng xương nặng. Khoảng 2 tuần nay sau khi ngồi võng đứng dậy đột ngột xuất hiện đau dữ dội vùng cột sống thắt lưng, đau chói tại chỗ ngang L4, không thể đứng thẳng hay đi lại được, nằm thay đổi tư thế đau tăng dữ dội. Đã được gia đình đưa vào BV Bạch Mai khám, chụp MRI cột sống thắt lưng phát hiện xẹp cấp tính đốt sống L4 trên nền xẹp cũ L2, L3 và loãng xương nặng, mật độ xương T-score < -3.5 -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật cố định cột sống thắt lưng bằng nẹp vít nhồi xi măng cuống đốt sống.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 84 ck/p, HA 120/75 mmHg, T° 36.8°C, SpO2 98%. Kiểu hình Cushing nhẹ (mặt tròn, da mỏng, bầm tím dưới da). Cột sống thắt lưng: Biến dạng gù nhẹ vùng thắt lưng trên; ấn đau chói mỏm gai sau và cơ cạnh sống L4; cơ dựng sống thắt lưng co cứng mạnh; hạn chế vận động cột sống nghiêm trọng; Lasègue 2 bên 70 độ do đau lưng; cơ lực 2 chi dưới 4+/5; phản xạ gân xương bánh chè 2 bên đều; không có hội chứng đuôi ngựa (đại tiểu tiện tự chủ). Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 16.84 G/L (bạch cầu tăng do phản ứng viêm/corticoid), HGB 128 g/L, PLT 241 G/L.\n- Đông máu: PT-INR 0.85, APTT 0.74, Fibrinogen 3.50 g/L.\n- Sinh hóa máu: Glucose 4.8 mmol/L, Ure 7.1 mmol/L, Creatinin 67 µmol/L, AST 24 U/L, ALT 50 U/mL, Cortisol máu 8h giảm (suy thượng thận), Điện giải Na 137, K 3.9, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số trong giới hạn bình thường.\n- CĐHA: MRI cột sống thắt lưng: Hình ảnh xẹp lún cấp thân đốt sống L4 (phù tủy xương tăng tín hiệu mạnh trên chuỗi xung STIR, giảm trên T1W); kèm hình ảnh xẹp cũ xơ hóa thân đốt sống L2 và L3; loãng xương nặng lan tỏa; X-quang cột sống thắt lưng: Giảm chiều cao thân đốt sống L4 > 40%, hình chêm, vôi hóa xơ vữa động mạch chủ bụng.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật cố định cột sống thắt lưng bằng hệ thống nẹp vít nhồi xi măng sinh học có lỗ qua cuống đốt sống (Cement-Augmented Pedicle Screws) L3-L4-L5",
                Anesthesia = "Gây mê nội khí quản (hoặc Tê tủy sống, phối hợp bù Hydrocortisone chu phẫu)",
                Surgeon = "BS Lê Đăng Tân",
                SurgeryTime = "10 giờ 30 phút, ngày 30/09/2026",
                Risks = "Cơn suy thượng thận cấp chu phẫu (tụt huyết áp, trụy mạch do stress phẫu thuật - cần tiêm bù Corticoid liều stress), tràn xi măng sinh học vào tĩnh mạch gây thuyên tắc phổi hoặc vào ống sống gây chèn ép tủy/đuôi ngựa, tụt vít/nhổ vít do xương loãng nặng, mất máu chu phẫu, nhiễm trùng vết mổ sâu, xẹp các đốt sống kế cận sau mổ."
            });

            // =========================================================================
            // 4. BÙI THỊ THIỆN (63t) - P712A - G20A | PTV: BS Lê Đăng Tân
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 4,
                Room = "Phòng mổ Cột sống (Khoa 57)",
                PatientCode = "0004050211",
                PatientName = "BÙI THỊ THIỆN",
                Dob = "08/03/1963",
                Gender = "Nữ",
                Address = "Xã Cẩm Xuyên, Hà Tĩnh",
                InTime = "05:00 ngày 29/09/2026",
                Diagnosis = "Hẹp ống sống thắt lưng đa tầng L3-L4, L4-L5 - Trượt đốt sống L3-L4 ra trước độ I do thoái hóa - Đau cách hồi thần kinh",
                History = "Thoái hóa cột sống thắt lưng điều trị nội khoa nhiều đợt; Tăng huyết áp nhẹ.",
                Course = "Bệnh nhân nữ 63 tuổi, đau thắt lưng âm ỉ nhiều năm. Khoảng 6 tháng nay đau tăng dữ dội lan xuống mông và mặt ngoài hai đùi, cẳng chân, kèm theo tê bì nặng hai bàn chân. Bệnh nhân xuất hiện dấu hiệu đau cách hồi thần kinh điển hình: đi bộ được khoảng 20-30 mét là phải dừng lại ngồi nghỉ hoặc cúi gập người mới đỡ đau để đi tiếp, ảnh hưởng nghiêm trọng đến sinh hoạt hàng ngày. Khám tại BV Bạch Mai, chụp MRI phát hiện hẹp ống sống nặng tầng L3-L4 và L4-L5 kèm trượt L3-L4 độ I -> Chỉ định phẫu thuật nắn trượt, giải ép ống sống và hàn xương liên thân đốt TLIF 2 tầng.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. Tiếp xúc tốt, thể trạng trung bình. Cột sống thắt lưng: Biến dạng bậc thang vùng L3-L4; ấn đau chói khe liên gai sau L3-L4, L4-L5 và cơ cạnh sống 2 bên; hạn chế vận động cúi ngửa thắt lưng; nghiệm pháp căng rễ Lasègue 2 bên 60 độ; dấu hiệu bấm chuông (+); giảm cảm giác da vùng phân bố rễ L4, L5 hai bên; cơ lực ngón cái và bàn chân hai bên 4/5; phản xạ gân gót và gân bánh chè giảm nhẹ 2 bên; đại tiểu tiện tự chủ. Khám tim phổi bình thường.",
                FullCls = "- Công thức máu: WBC 6.8 G/L, HGB 126 g/L, PLT 230 G/L.\n- Đông máu: PT-INR 0.92, APTT 0.70, Fibrinogen 2.72 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 5.2 mmol/L, Creatinin 72 µmol/L, AST 28 U/L, ALT 24 U/mL, Điện giải Na 140, K 4.1, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: 10 thông số bình thường.\n- CĐHA: MRI cột sống thắt lưng: Dày dây chằng vàng, phì đại khớp mấu, thoái hóa lồi đĩa đệm gây hẹp nặng ống sống và lỗ liên hợp 2 tầng L3-L4 và L4-L5, chèn ép bao màng cứng và các chùm rễ thần kinh đuôi ngựa; hình ảnh trượt thân đốt L3 ra trước so với L4 độ I; X-quang cột sống thắt lưng thẳng - nghiêng - động (cúi/ngửa): Hình ảnh mất vững cột sống tầng L3-L4, trượt tăng khi gập.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật nắn chỉnh trượt, cắt bản sống giải ép rộng rãi ống sống và rễ thần kinh, ghép xương hàn khớp liên thân đốt qua lỗ liên hợp 2 tầng (TLIF L3-L4, L4-L5) kết hợp nẹp vít qua cuống cột sống thắt lưng",
                Anesthesia = "Gây mê nội khí quản",
                Surgeon = "BS Lê Đăng Tân",
                SurgeryTime = "11 giờ 30 phút, ngày 30/09/2026",
                Risks = "Tổn thương rễ thần kinh L3, L4, L5 hoặc chùm đuôi ngựa gây yếu liệt chi dưới, rối loạn cơ tròn bàng quang - trực tràng, rách màng cứng gây rò dịch não tủy, chảy máu từ đám rối tĩnh mạch ngoài màng cứng, không liền xương/khớp giả liên thân đốt, gãy nẹp vít/bung lồng PEEK, nhiễm trùng vết mổ sâu."
            });

            // =========================================================================
            // 5. VŨ THÀNH TRUNG (10t) - P716 - G04 | PTV: BS Lê Văn Luân
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 5,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0004089090",
                PatientName = "VŨ THÀNH TRUNG",
                Dob = "04/10/2016",
                Gender = "Nam",
                Address = "Tiên Cầu, Xã Hiệp Cường, Hưng Yên",
                InTime = "13:35 ngày 28/09/2026",
                Diagnosis = "Gãy kín 1/3 giữa 2 xương cẳng tay phải di lệch / Trẻ em 10 tuổi",
                History = "Trẻ khỏe mạnh, không tiền sử bệnh lý nội khoa, không dị ứng thuốc.",
                Course = "Bệnh nhi nam 10 tuổi, ngày 28/09/2026 khi đang chơi đá bóng bị ngã chống mạnh bàn tay phải xuống đất trong tư thế duỗi và sấp cẳng tay. Sau ngã đau chói dữ dội, cẳng tay phải biến dạng gập góc, sưng nề nhanh, không cử động được cẳng bàn tay. Được gia đình đưa ngay vào BV Bạch Mai cấp cứu, chụp X-quang chẩn đoán gãy kín 1/3 giữa thân xương quay và xương trụ cẳng tay phải di lệch nhiều -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật kết hợp xương kín bằng đinh nội tủy đàn hồi (TEN) dưới C-Arm.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 90 ck/p, HA 105/65 mmHg, T° 36.8°C, SpO2 99%. Trẻ tỉnh, quấy khóc do đau, thể trạng dinh dưỡng tốt (cân nặng 32 kg). Chi trên phải: Sưng nề, biến dạng gập góc rõ rệt vùng 1/3 giữa cẳng tay phải; sờ có điểm đau chói và lạo xạo xương; mất cơ năng cẳng tay phải; mạch quay và mạch trụ bắt rõ, đầu chi hồng ấm, thời gian làm đầy mao mạch (CRT) < 2 giây; cảm giác các ngón tay bình thường; không có dấu hiệu chèn ép khoang. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 18.2 G/L (tăng do phản ứng sau chấn thương/stress đau), HGB 114 g/L, PLT 334 G/L.\n- Đông máu: PT-INR 1.11, APTT 1.04, Fibrinogen 3.24 g/L.\n- Sinh hóa máu: Glucose 7.0 mmol/L, Ure 4.7 mmol/L, Creatinin 55 µmol/L, AST 38 U/L, ALT 15 U/mL, Điện giải đồ bình thường.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- CĐHA: X-quang cẳng tay phải thẳng - nghiêng: Hình ảnh gãy ngang 1/3 giữa thân xương quay và gãy chéo 1/3 giữa thân xương trụ cẳng tay phải, di lệch gập góc mở ra ngoài 25 độ và di lệch chồng ngắn 1 cm; chưa thấy tổn thương sụn phát triển ở đầu trên và đầu dưới hai xương.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật kết hợp xương kín 2 xương cẳng tay phải bằng đinh nội tủy đàn hồi Titanium (TEN - Titanium Elastic Nail) dưới hướng dẫn màn tăng sáng C-Arm",
                Anesthesia = "Gây mê nội khí quản (lưu ý gây mê nhi khoa an toàn)",
                Surgeon = "BS Lê Văn Luân",
                SurgeryTime = "08 giờ 00 phút, ngày 30/09/2026",
                Risks = "Hội chứng chèn ép khoang cẳng tay cấp chu phẫu, tổn thương nhánh thần kinh quay nông/nhánh gian cốt sau, đinh chồi gây kích thích da/nhiễm trùng điểm vào đinh, can lệch/hạn chế sấp ngửa cẳng tay, khớp giả/chậm liền xương, gãy lại sau khi rút đinh."
            });

            // =========================================================================
            // 6. PHẠM VĂN BỎNG (79t) - P740 - G62 | PTV: BS Hà Đức Cường
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 6,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0002840646",
                PatientName = "PHẠM VĂN BỒNG",
                Dob = "22/07/1947",
                Gender = "Nam",
                Address = "Tổ 6, Phường Minh Xuân, Tuyên Quang",
                InTime = "21:25 ngày 15/09/2026",
                Diagnosis = "Gãy kín liên mấu chuyển xương đùi Trái di lệch / Xuất huyết tiêu hóa do loét hành tá tràng Forrest IB đã can thiệp ổn định - Viêm phổi - Stent mạch vành - Suy tim - Tăng huyết áp - Theo dõi đợt cấp suy thận mạn - COPD",
                History = "Đặt Stent ĐMV 2021; Suy tim NYHA II-III; THA; COPD mạn; Xuất huyết tiêu hóa do loét hành tá tràng Forrest IB đã kẹp clip nội soi ngày 16/09 điều trị ổn định; Suy thận mạn nền.",
                Course = "Bệnh nhân nam 79 tuổi, nhiều bệnh lý nội khoa phức tạp phối hợp (tim mạch, hô hấp, tiêu hóa, thận mạn). Ngày 15/09/2026 bị ngã đập hông trái xuống sàn nhà, vào viện cấp cứu trong tình trạng gãy liên mấu chuyển xương đùi trái kèm xuất huyết tiêu hóa do ổ loét hành tá tràng Forrest IB và viêm phổi. Bệnh nhân đã được điều trị tích cực tại Khoa Tiêu hóa & HSTC: nội soi can thiệp kẹp clip cầm máu ổ loét, truyền máu, dùng kháng sinh điều trị viêm phổi, kiểm soát suy tim và suy thận. Hiện tại tình trạng xuất huyết tiêu hóa đã ổn định > 10 ngày (phân vàng, không nôn máu, Hb 102 g/L), chức năng tim phổi bù trừ tốt -> Hội chẩn liên chuyên khoa phê duyệt phẫu thuật kết hợp xương liên mấu chuyển đùi trái bằng đinh nội tủy PFNA.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 82 ck/p, HA 130/80 mmHg, T° 36.7°C, SpO2 97% (thở khí phòng). Thể trạng già yếu, da niêm mạc nhợt nhẹ. Khám chi dưới trái: Vùng háng và mấu chuyển lớn đùi trái sưng nề, bầm tím lan rộng, ấn đau chói vùng liên mấu chuyển; chân trái biến dạng ngắn chi và xoay ngoài ~ 45 độ; mất cơ năng chân trái hoàn toàn; mạch mu chân và mạch chày sau 2 bên bắt rõ; vận động cảm giác các ngón chân bình thường. Tim mạch: T1 T2 mờ, nhịp tim đều, không phù ngoại biên; Phổi: rales nổ rải rác đáy phổi giảm nhiều so với trước; Bụng mềm, không chướng, ấn thượng vị không đau, phân vàng.",
                FullCls = "- Công thức máu: WBC 12.82 G/L, HGB 102 g/L (thiếu máu nhẹ sau XHTH đã kiểm soát), PLT 300 G/L.\n- Đông máu: PT-INR 1.04, APTT 0.77, Fibrinogen 3.89 g/L.\n- Sinh hóa máu: Glucose 5.0 mmol/L, Ure 9.4 mmol/L, Creatinin 155 µmol/L (suy thận mạn có đợt cấp đang hồi phục), AST 42 U/L, ALT 29 U/mL, Điện giải Na 136, K 4.2, Cl 100 mmol/L, Troponin T hs 14.5 ng/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: A Rh(+).\n- TPT nước tiểu: Protein (+), trụ niệu (-).\n- CĐHA: X-quang khớp háng và đùi trái thẳng - nghiêng: Hình ảnh gãy kín liên mấu chuyển xương đùi trái không vững (AO 31-A2), di lệch mảnh rời mấu chuyển bé và gập góc cổ - thân xương đùi; X-quang ngực thẳng: Viêm phổi hai đáy đang thoái lui, bóng tim to; Siêu âm tim: EF 52%, giãn thất trái nhẹ, xơ vữa van tim.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "500",
                SurgeryMethod = "Phẫu thuật kết hợp xương kín liên mấu chuyển xương đùi trái bằng đinh nội tủy chốt đầu chỏm (PFNA / Gamma Nail) trên bàn chỉnh hình dưới hướng dẫn C-Arm",
                Anesthesia = "Gây tê tủy sống liều thấp (hoặc Gây mê nội khí quản có kiểm soát huyết động và theo dõi hô hấp tim mạch liên tục)",
                Surgeon = "BS Hà Đức Cường",
                SurgeryTime = "09 giờ 30 phút, ngày 30/09/2026",
                Risks = "Tái phát xuất huyết tiêu hóa do stress phẫu thuật, biến cố tim mạch cấp (nhồi máu cơ tim chu phẫu, suy tim cấp, rối loạn nhịp tim), suy hô hấp đợt cấp COPD/viêm phổi tiến triển, đợt cấp suy thận nặng lên cần lọc máu, thuyên tắc huyết khối tĩnh mạch sâu/thuyên tắc phổi, mất máu chu phẫu, gãy thêm thân xương đùi hoặc tụt đinh/cắt chỏm (cut-out) do xương loãng, nhiễm trùng vết mổ."
            });

            // =========================================================================
            // 7. NGUYỄN THỊ THU (87t) - P740 - G69 | PTV: BS Hà Đức Cường
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 7,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0004081634",
                PatientName = "NGUYỄN THỊ THU",
                Dob = "07/08/1939",
                Gender = "Nữ",
                Address = "Khối 4, Phường Hoàng Mai, Nghệ An",
                InTime = "13:07 ngày 26/09/2026",
                Diagnosis = "Gãy kín cổ xương đùi phải Garden IV - Xẹp cũ đốt sống D1, vỡ cũ L1 / Suy dinh dưỡng người già - Tăng huyết áp",
                History = "Tăng huyết áp người già điều trị đều; Loãng xương nặng, tiền sử xẹp đốt sống D1 và L1 điều trị bảo tồn; Suy dinh dưỡng tuổi già.",
                Course = "Bệnh nhân nữ 87 tuổi, tiền sử THA, loãng xương nặng. Ngày 26/09/2026 bệnh nhân trượt chân ngã ngồi đập mông và hông phải xuống sàn nhà, sau ngã đau chói dữ dội vùng khớp háng phải, không thể đứng dậy hay cử động chân phải. Được đưa vào BV Bạch Mai khám, chụp X-quang phát hiện gãy hoàn toàn cổ xương đùi phải di lệch Garden IV trên nền xẹp cũ D1, L1 -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật thay khớp háng nhân tạo bán phần Bipolar chuôi có xi măng bên phải nhằm phục hồi vận động sớm, tránh biến chứng nằm lâu.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 135/85 mmHg, T° 36.7°C, SpO2 97%. Thể trạng người già 87 tuổi gầy còm (suy dinh dưỡng nhẹ-vừa), tiếp xúc chậm nhưng định hướng đúng. Khám chi dưới phải: Vùng khớp háng phải sưng nhẹ, ấn đau chói vùng tam giác Scarpa và mấu chuyển lớn; chân phải xoay ngoài ~ 60 độ, ngắn chi tương đối 2 cm so với bên trái; gõ dồn gót truyền đau lên khớp háng phải; mất cơ năng vận động chủ động chân phải; mạch mu chân và chày sau phải bắt rõ, ngón chân ấm hồng. Cột sống ngực - thắt lưng: Gù vẹo do xẹp cũ D1, L1 nhưng không có điểm đau chói cấp. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 9.71 G/L, HGB 121 g/L, PLT 143 G/L.\n- Đông máu: PT-INR 0.97, APTT 0.89, Fibrinogen 4.72 g/L.\n- Sinh hóa máu: Glucose 7.4 mmol/L, Ure 4.1 mmol/L, Creatinin 54 µmol/L, AST 22 U/L, ALT 12 U/mL, Albumin 33 g/L, Protein toàn phần 62 g/L, Điện giải Na 138, K 4.0, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- CĐHA: X-quang khớp háng phải thẳng - nghiêng: Gãy hoàn toàn cổ chính danh xương đùi phải di lệch rời chỏm Garden IV, loãng xương nặng; X-quang cột sống thắt lưng: Hình ảnh xẹp cũ xơ hóa thân đốt sống D1 và L1; X-quang tim phổi: Xơ hóa rải rác hai phổi tuổi già, bóng tim không to.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật thay khớp háng bán phần nhân tạo chuôi có xi măng (Cemented Bipolar Hemiarthroplasty) bên phải",
                Anesthesia = "Gây tê tủy sống (hoặc Tê ngoài màng cứng / Gây mê nội khí quản)",
                Surgeon = "BS Hà Đức Cường",
                SurgeryTime = "11 giờ 00 phút, ngày 30/09/2026",
                Risks = "Hội chứng tụt huyết áp/ngừng tim do xi măng sinh học (BCIS - Bone Cement Implantation Syndrome), biến cố tim mạch và hô hấp cấp chu phẫu ở người 87 tuổi, trật khớp háng nhân tạo sau mổ, lỏng chuôi khớp do xương loãng nặng, gãy xương đùi quanh chuôi khớp, mất máu chu phẫu, thuyên tắc huyết khối tĩnh mạch sâu/thuyên tắc phổi, loét tì đè và nhiễm trùng vết mổ sâu."
            });

            // =========================================================================
            // 8. NGUYỄN THỊ NGỌC (50t) - Khoa 57 | PTV: BS Đặng Hoàng Giang
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 8,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0003837595",
                PatientName = "NGUYỄN THỊ NGỌC",
                Dob = "17/09/1976",
                Gender = "Nữ",
                Address = "Phường Phạm Sư Mạnh, Thành phố Hải Phòng",
                InTime = "08:30 ngày 29/09/2026",
                Diagnosis = "Rách hoàn toàn gân chóp xoay khớp vai phải (gân trên gai và gân dưới gai) - Hẹp khoang dưới mỏm cùng vai / Tiền sử Ung thư nội mạc tử cung đã phẫu thuật cắt tử cung hoàn toàn",
                History = "Tiền sử Ung thư nội mạc tử cung phát hiện và phẫu thuật cắt tử cung toàn bộ + 2 phần phụ năm 2023, khám định kỳ ổn định không tái phát.",
                Course = "Bệnh nhân nữ 50 tuổi, đau khớp vai phải âm ỉ 6 tháng nay, đau tăng nhiều về đêm làm mất ngủ, không thể nằm nghiêng bên phải. Gần đây đau chói khi giơ tay lên cao quá đầu, chải tóc hoặc với tay ra sau lưng, cảm giác vai phải yếu rõ rệt, không nâng được đồ vật. Đã tiêm nội khớp và điều trị nội khoa tại Hải Phòng không đỡ. Khám tại BV Bạch Mai, chụp MRI khớp vai phải phát hiện rách hoàn toàn gân trên gai và gân dưới gai chóp xoay vai phải, tụt đầu gân thoái hóa độ II, hẹp khoang dưới mỏm cùng vai -> Nhập viện Khoa 57 chỉ định phẫu thuật nội soi khâu phục hồi gân chóp xoay vai phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. BN tỉnh, thể trạng tốt. Khám khớp vai phải: Teo nhẹ cơ trên gai và dưới gai vai phải; ấn đau chói tại điểm bám gân củ lớn xương cánh tay phải; dấu hiệu chạm mỏm cùng vai Neer (+), Hawkins-Kennedy (+); nghiệm pháp Jobe (kiểm tra gân trên gai) (+), Patte (kiểm tra gân dưới gai) (+), Gerber (gân dưới vai) (-); biên độ vận động chủ động khớp vai phải hạn chế (dạng 80°, nâng trước 90°, xoay ngoài 30°), biên độ thụ động bình thường; mạch quay bắt rõ, cảm giác bàn tay bình thường. Tim phổi bình thường, sẹo mổ cũ đường trắng giữa dưới rốn lành tốt.",
                FullCls = "- Công thức máu: WBC 5.8 G/L, HGB 138 g/L, PLT 234 G/L.\n- Đông máu: PT-INR 1.16, APTT 1.06, Fibrinogen 4.08 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 5.4 mmol/L, Creatinin 80 µmol/L, AST 30 U/L, ALT 26 U/mL, Điện giải Na 139, K 4.1, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số âm tính.\n- CĐHA: MRI khớp vai phải: Hình ảnh rách hoàn toàn gân trên gai và gân dưới gai chóp xoay vai phải, đầu gân co rút ngang mức chỏm xương cánh tay (Patte độ II), thoái hóa mỡ cơ chóp xoay độ I (Goutallier); gai xương mỏm cùng vai hẹp khoang dưới mỏm cùng; tràn dịch khớp vai và bao hoạt dịch dưới mỏm cùng vai lượng vừa; X-quang khớp vai: Gai xương dưới mỏm cùng vai Type II.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật nội soi khớp vai phải tạo hình khoang dưới mỏm cùng vai, mài gai mỏm cùng và khâu phục hồi gân chóp xoay (gân trên gai, dưới gai) bằng chỉ neo sinh học có luồn chỉ (Suture Anchors)",
                Anesthesia = "Gây mê nội khí quản + Gây tê đám rối thần kinh cánh tay (giảm đau chu phẫu)",
                Surgeon = "BS Đặng Hoàng Giang",
                SurgeryTime = "13 giờ 30 phút, ngày 30/09/2026",
                Risks = "Rách tái phát gân chóp xoay sau mổ do chất lượng gân kém hoặc tuân thủ tập phục hồi chức năng không đúng, cứng khớp vai/đông cứng khớp vai (Frozen Shoulder), tổn thương thần kinh trên vai hoặc thần kinh nách, tụ dịch/nhiễm trùng khớp vai, bung neo chỉ xương, biến chứng liên quan tư thế mổ (ngồi bãi biển/nghiêng)."
            });

            // =========================================================================
            // 9. NGUYỄN KHẮC CHỦ (68t) - P711 - G28 | PTV: BS Hà Đức Cường
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 9,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0004079203",
                PatientName = "NGUYỄN KHẮC CHỦ",
                Dob = "07/03/1958",
                Gender = "Nam",
                Address = "Xã Thái Tân, Thành phố Hải Phòng",
                InTime = "09:18 ngày 25/09/2026",
                Diagnosis = "Gãy kín cổ xương đùi trái Garden IV di lệch / Bệnh nhân nam 68 tuổi",
                History = "Không có tiền sử bệnh lý mạn tính đặc biệt, không dị ứng.",
                Course = "Bệnh nhân nam 68 tuổi, ngày 25/09/2026 trượt ngã đập mạnh vùng mông và hông trái xuống sàn gạch. Sau ngã đau chói vùng khớp háng trái, bất lực vận động hoàn toàn chân trái. Được người nhà sơ cứu cố định tạm thời và chuyển ngay đến BV Bạch Mai. Chụp X-quang chẩn đoán gãy hoàn toàn cổ xương đùi trái di lệch Garden IV -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật thay khớp háng nhân tạo bên trái nhằm phục hồi chức năng đi lại sớm cho bệnh nhân.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 130/80 mmHg, T° 36.7°C, SpO2 98%. BN tỉnh táo, thể trạng tốt. Khám chi dưới trái: Sưng nề nhẹ vùng khớp háng trái, ấn đau chói diện khớp háng và mấu chuyển lớn; chân trái ở tư thế xoay ngoài ~ 50 độ, ngắn chi 2 cm; gõ dồn gót chân đau chói khớp háng trái; mất cơ năng khớp háng trái hoàn toàn; bắt rõ mạch mu chân và chày sau trái, cảm giác và vận động ngón chân bình thường. Tim mạch: T1, T2 đều rõ; Phổi: rì rào phế nang êm dịu; Bụng mềm, gan lách không to.",
                FullCls = "- Công thức máu: WBC 8.65 G/L, HGB 145 g/L, PLT 194 G/L.\n- Đông máu: PT-INR 1.00, APTT 0.87, Fibrinogen 6.59 g/L (tăng do phản ứng sau gãy xương).\n- Sinh hóa máu: Glucose 4.4 mmol/L, Ure 5.0 mmol/L, Creatinin 75 µmol/L, AST 40 U/L, ALT 30 U/mL, Điện giải Na 139, K 4.1, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: 10 thông số bình thường.\n- CĐHA: X-quang khớp háng trái thẳng - nghiêng: Hình ảnh gãy hoàn toàn cổ xương đùi trái qua phần giữa cổ chính danh, di lệch rời hoàn toàn góc cổ - thân (Garden IV); mật độ xương giảm nhẹ; X-quang tim phổi: Nhu mô phổi sáng, tim không to.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật thay khớp háng bán phần (hoặc toàn phần) nhân tạo bên trái (Hemiarthroplasty / Total Hip Arthroplasty Left)",
                Anesthesia = "Gây tê tủy sống",
                Surgeon = "BS Hà Đức Cường",
                SurgeryTime = "14 giờ 30 phút, ngày 30/09/2026",
                Risks = "Trật khớp háng nhân tạo sau mổ, mất máu chu phẫu cần truyền máu, tổn thương thần kinh tọa hoặc thần kinh đùi, gãy xương đùi hoặc vỡ ổ cối quanh khớp nhân tạo trong lúc đóng chuôi, thuyên tắc huyết khối tĩnh mạch sâu/thuyên tắc phổi, nhiễm trùng khớp háng nhân tạo sâu."
            });

            // =========================================================================
            // 10. NGUYỄN THỊ THẢO (81t) - P722 - G35 | PTV: BS Lê Văn Luân
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 10,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0004067971",
                PatientName = "NGUYỄN THỊ THẢO",
                Dob = "01/01/1945",
                Gender = "Nữ",
                Address = "TDP 20 Miêu Nha, Phường Xuân Phương, Thành phố Hà Nội",
                InTime = "05:00 ngày 28/09/2026",
                Diagnosis = "Gãy kín cổ xương đùi phải Garden IV - Thoái hóa khớp gối 2 bên / Tăng huyết áp vô căn - Viêm dạ dày tá tràng",
                History = "Tăng huyết áp điều trị thường xuyên; Viêm dạ dày mạn; Thoái hóa khớp gối 2 bên đi lại hạn chế nhiều năm.",
                Course = "Bệnh nhân nữ 81 tuổi, tiền sử THA, thoái hóa khớp gối. Ngày 28/09/2026 trượt ngã trong nhà tắm đập hông phải xuống nền cứng, sau ngã đau dữ dội khớp háng phải, không thể tự đứng dậy. Người nhà đưa vào BV Bạch Mai cấp cứu, chụp X-quang chẩn đoán gãy hoàn toàn cổ xương đùi phải di lệch Garden IV trên nền thoái hóa khớp gối 2 bên -> Nhập Khoa 57 hội chẩn chỉ định phẫu thuật thay khớp háng bán phần nhân tạo chuôi có xi măng bên phải nhằm giúp bệnh nhân ngồi dậy và phục hồi vận động sớm.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 135/80 mmHg, T° 36.6°C, SpO2 98%. Tiếp xúc tốt, thể trạng người già 81t. Khám chi dưới phải: Khớp háng phải sưng nề nhẹ, ấn đau chói diện khớp và mấu chuyển lớn; chân phải ngắn chi 2 cm, tư thế xoay ngoài ~ 50 độ; mất cơ năng khớp háng phải; khớp gối 2 bên biến dạng trục nhẹ, lạo xạo xương khi cử động; mạch mu chân và chày sau phải bắt rõ, đầu ngón chân hồng ấm. Tim phổi bình thường; bụng mềm, ấn thượng vị không đau.",
                FullCls = "- Công thức máu: WBC 7.5 G/L, HGB 118 g/L, PLT 210 G/L.\n- Đông máu: PT-INR 1.21, APTT 0.91, Fibrinogen 5.13 g/L.\n- Sinh hóa máu: Glucose 5.6 mmol/L, Ure 4.8 mmol/L, Creatinin 70 µmol/L, AST 27 U/L, ALT 20 U/mL, Điện giải Na 138, K 4.0, Cl 101 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- CĐHA: X-quang khớp háng phải thẳng - nghiêng: Gãy hoàn toàn cổ chính danh xương đùi phải di lệch Garden IV, loãng xương nặng; X-quang khớp gối 2 bên: Gai xương mâm chày và lồi cầu đùi, hẹp khe khớp gối độ III; X-quang tim phổi: Tim phổi trong giới hạn bình thường.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "250",
                SurgeryMethod = "Phẫu thuật thay khớp háng bán phần nhân tạo bên phải chuôi có xi măng (Cemented Bipolar Hemiarthroplasty Right)",
                Anesthesia = "Gây tê tủy sống",
                Surgeon = "BS Lê Văn Luân",
                SurgeryTime = "15 giờ 30 phút, ngày 30/09/2026",
                Risks = "Hội chứng tụt huyết áp do phản ứng xi măng sinh học (BCIS), mất máu chu phẫu, trật khớp háng nhân tạo, thuyên tắc huyết khối tĩnh mạch sâu chi dưới/thuyên tắc mạch phổi, lỏng chuôi khớp do xương loãng, nhiễm trùng vết mổ sâu, biến chứng tim mạch ở người 81 tuổi."
            });

            // =========================================================================
            // 11. TRẦN THỊ LƯỢNG (45t) - P712 - G25 | PTV: BS Ngô Đăng Quang
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 11,
                Room = "Phòng mổ Tiểu phẫu / Chấn thương (Khoa 57)",
                PatientCode = "0003863470",
                PatientName = "TRẦN THỊ LƯỢNG",
                Dob = "20/08/1981",
                Gender = "Nữ",
                Address = "Thôn Phù Long, Xã Gia Vân, Ninh Bình",
                InTime = "06:26 ngày 28/09/2026",
                Diagnosis = "Hội chứng ống cổ tay bên trái mức độ nặng (Hội chứng ống cổ tay 2 bên T > P)",
                History = "Lao động chân tay thủ công nhiều năm; Không có tiền sử bệnh lý mạn tính, không dị ứng.",
                Course = "Bệnh nhân nữ 45 tuổi, làm nghề may mặc thủ công. Khoảng 1 năm nay xuất hiện tê bì, dị cảm châm chích như kim châm ở vùng gan bàn tay và các ngón 1, 2, 3 và nửa ngoài ngón 4 bàn tay trái, tê tăng nhiều về đêm làm thức giấc, phải vẩy tay mới đỡ tê. Gần đây xuất hiện yếu tay, teo cơ mô cái bàn tay trái, khó cầm nắm đồ vật nhỏ hoặc cài cúc áo. Đã điều trị nội khoa và châm cứu nhiều đợt không đỡ. Khám tại BV Bạch Mai, đo điện cơ (EMG) chẩn đoán Hội chứng ống cổ tay 2 bên, bên trái mức độ nặng (tổn thương sợi trục và myelin thần kinh giữa tại ống cổ tay) -> Nhập Khoa 57 chỉ định phẫu thuật giải phóng thần kinh giữa ống cổ tay trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. BN tỉnh, thể trạng tốt. Khám bàn tay trái: Teo rõ rệt khối cơ mô cái bàn tay trái; giảm cảm giác da vùng phân bố thần kinh giữa (gan ngón 1, 2, 3 và nửa ngoài ngón 4); nghiệm pháp Phalen (+) sau 20 giây; nghiệm pháp Tinel (+) tại nếp gấp cổ tay trái lan xuống các ngón; nghiệm pháp Durkan (ấn trực tiếp ống cổ tay) (+); cơ lực đối ngón cái - ngón út bên trái giảm (3+/5); mạch quay và mạch trụ bắt rõ. Bàn tay phải có tê nhẹ nhưng chưa teo cơ. Tim phổi bình thường.",
                FullCls = "- Công thức máu: WBC 4.9 G/L, HGB 117 g/L, PLT 345 G/L.\n- Đông máu: PT-INR 0.98, APTT 0.92, Fibrinogen 2.63 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 4.0 mmol/L, Creatinin 73 µmol/L, AST 25 U/L, ALT 17 U/mL, Điện giải Na 139, K 4.1, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số bình thường.\n- CĐHA & TDCN: Điện cơ (EMG): Tổn thương nặng dẫn truyền cảm giác và vận động của dây thần kinh giữa qua ống cổ tay bên trái (kéo dài thời gian tiềm vận động, giảm biên độ vận động cơ mô cái, mất dẫn truyền cảm giác); bên phải tổn thương mức độ nhẹ-vừa; Siêu âm cổ tay: Phù nề thần kinh giữa đoạn trước ống cổ tay (diện tích cắt ngang CSA = 15 mm2), dây chằng vòng cổ tay ngang dày.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "0",
                SurgeryMethod = "Phẫu thuật mở giải phóng thần kinh giữa trong ống cổ tay trái (Cắt dây chằng vòng cổ tay ngang / Carpal Tunnel Release Left)",
                Anesthesia = "Gây tê tại chỗ (hoặc Gây tê đám rối thần kinh cánh tay / Gây tê vùng)",
                Surgeon = "BS Ngô Đăng Quang",
                SurgeryTime = "16 giờ 00 phút, ngày 30/09/2026",
                Risks = "Tổn thương nhánh vận động quặt ngược của thần kinh giữa chi phối cơ mô cái, tổn thương nhánh bì gan tay của thần kinh giữa gây đau bỏng rát mạn tính sẹo mổ, cắt không hết dây chằng ngang dẫn đến giải ép không hoàn toàn, chảy máu tạo máu tụ chèn ép, đau sẹo mổ (Pillar pain), nhiễm trùng vết mổ nông."
            });

            // =========================================================================
            // 12. NGUYỄN VĂN PHƯƠNG (59t) - P712 - G23 | PTV: BS Đặng Hoàng Giang
            // =========================================================================
            list.Add(new PatientProfile {
                Stt = 12,
                Room = "Phòng mổ Chấn thương (Khoa 57)",
                PatientCode = "0002740977",
                PatientName = "NGUYỄN VĂN PHƯƠNG",
                Dob = "10/11/1967",
                Gender = "Nam",
                Address = "Xã Yên Thành, Nghệ An",
                InTime = "16:11 ngày 25/09/2026",
                Diagnosis = "Thoái hóa khớp gối phải giai đoạn nặng (Kellgren-Lawrence độ IV) - Biến dạng vẹo trong khớp gối / Nốt đặc thùy trên phổi trái Lung-RADS 4X - Theo dõi u sụn màng hoạt dịch khớp gối",
                History = "Đau khớp gối 2 bên hơn 5 năm nay, điều trị nội khoa nhiều đợt; Phát hiện nốt đặc thùy trên phổi trái trên CT Scanner theo dõi.",
                Course = "Bệnh nhân nam 59 tuổi, đau khớp gối 2 bên âm ỉ tăng dần nhiều năm nay, bên phải đau nặng hơn bên trái. Bệnh nhân đau nhiều khi đi lại, đứng lâu, lên xuống cầu thang, kèm cứng khớp gối buổi sáng và biến dạng vẹo trong khớp gối phải (chân vòng kiềng), hạn chế biên độ gập duỗi gối nghiêm trọng. Đã điều trị nội khoa, tiêm Acid Hyaluronic nội khớp nhiều đợt không còn đáp ứng. Khám tại BV Bạch Mai, chụp X-quang khớp gối chẩn đoán thoái hóa khớp gối phải độ IV kèm hẹp khe khớp hoàn toàn; chụp CT lồng ngực kiểm tra thấy nốt đặc thùy trên phổi trái Lung-RADS 4X đã hội chẩn hô hấp đánh giá đủ điều kiện phẫu thuật -> Nhập Khoa 57 chỉ định phẫu thuật thay toàn bộ khớp gối nhân tạo bên phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.7°C, SpO2 99%. BN tỉnh táo, thể trạng tốt. Khám khớp gối phải: Biến dạng vẹo trong (Varus deformity) ~ 12 độ; sưng nhẹ diện khớp, ấn đau chói khe khớp bên trong và quanh xương bánh chè; dấu hiệu bào gỗ (+), lạo xạo khớp (+) rõ khi cử động; biên độ vận động gập duỗi gối phải hạn chế (gập tối đa 90°, duỗi thiếu 10°); nghiệm pháp ngăn kéo (-), Lachman (-); mạch mu chân và chày sau phải bắt rõ, không phù chi dưới. Tim mạch bình thường; Phổi: rì rào phế nang rõ, không rale; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.4 G/L, HGB 157 g/L, PLT 140 G/L.\n- Đông máu: PT-INR 1.00, APTT 0.96, Fibrinogen 2.51 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 4.8 mmol/L, Creatinin 115 µmol/L, AST 31 U/L, ALT 28 U/mL, Điện giải Na 140, K 4.0, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số âm tính.\n- CĐHA: X-quang khớp gối phải tư thế đứng chịu lực thẳng - nghiêng: Thoái hóa khớp gối phải độ IV (Kellgren-Lawrence), hẹp hoàn toàn khe khớp khoang trong, đặc xương dưới sụn mâm chày trong, gai xương lớn rìa lồi cầu và bánh chè, biến dạng vẹo trong mâm chày; CT lồng ngực: Nốt đặc thùy trên phổi trái kích thước 6 mm (Lung-RADS 4X), không có tổn thương đông đặc hay tràn dịch màng phổi, đã hội chẩn chuyên khoa hô hấp cho phép mổ và theo dõi CT sau mổ.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay toàn bộ khớp gối nhân tạo bên phải (Total Knee Replacement - TKR Right) có cắt lọc màng hoạt dịch",
                Anesthesia = "Gây tê tủy sống + Giảm đau ngoài màng cứng / Tê kênh cơ khép (Adductor Canal Block)",
                Surgeon = "BS Đặng Hoàng Giang",
                SurgeryTime = "16 giờ 30 phút, ngày 30/09/2026",
                Risks = "Mất máu chu phẫu cần truyền máu, thuyên tắc huyết khối tĩnh mạch sâu chi dưới (DVT) và thuyên tắc phổi (PE), tổn thương động mạch khoeo hoặc thần kinh mác chung, cứng khớp gối sau mổ, lỏng khớp hoặc lệch trục khớp gối nhân tạo, nhiễm trùng khớp gối nhân tạo sâu (biến chứng nguy hiểm nhất), gãy xương quanh khớp nhân tạo."
            });

            return list;
        }
    }
}
