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

            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

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

            Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) CHI TIẾT ===", targetList.Count));

            foreach (var p in targetList)
            {
                Console.WriteLine(string.Format("\n[{0:D2}/12] Đang tạo biên bản cho BN: {1} ({2})...", p.Stt, p.PatientName, p.PatientCode));
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

                string safeName = p.PatientName.Replace(" ", "_");
                string fileName = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
                string savePath = Path.Combine(outDir, fileName);
                doc.Save(savePath);
                Console.WriteLine("  ✔ Đã xuất: " + fileName);
            }

            Console.WriteLine("\n🎉 HOÀN THÀNH TOÀN BỘ BIÊN BẢN HỘI CHẨN PT-01 TẠI: " + outDir);
        }

        static List<PatientProfile> GetPatients()
        {
            var list = new List<PatientProfile>();

            // BN 1
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0003406142",
                PatientName = "NGUYỄN THỊ HUYỀN",
                Dob = "1971",
                Gender = "Nữ",
                Address = "Phường Trung Phụng, Quận Đống Đa, Hà Nội",
                InTime = "16/09/2026 09:06",
                Diagnosis = "Trượt L4L5, thoái hóa đa tầng, thoát vị đĩa đệm hẹp ống sống thắt lưng, hở eo L5 trái",
                History = "Chưa phát hiện bệnh lý mạn tính đặc biệt.",
                Course = "Bệnh nhân đau cột sống thắt lưng lan xuống mông và cẳng bàn chân phải 9 tháng nay, điều trị nội khoa nhiều đợt không đỡ. Gần đây đau tăng nhiều khi đi lại và cúi ngửa, tê bì mặt ngoài cẳng chân và mu chân phải, khoảng cách đi bộ không đau giảm còn dưới 50m (cách hồi thần kinh) -> vào viện theo lịch mổ phiên để phẫu thuật.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 130/90 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh táo, tiếp xúc tốt. Cột sống thắt lưng: Ấn gai sau L4-L5 đau nhói, co cứng cơ cạnh sống hai bên, Schober 11/10 cm. Hội chứng rễ (+): Lasègue (P) (+) 45 độ, Lasègue (T) 75 độ; nghiệm pháp bấm chuông (+) lan chân phải; giảm cảm giác nông mu chân phải (rễ L5); cơ lực ngón cái chân phải 4/5, các nhóm cơ khác 5/5; phản xạ gân gót hai bên đều; không rối loạn cơ tròn (đại tiểu tiện tự chủ). Tim đều, phổi thông khí rõ, bụng mềm, ngực vững.",
                FullCls = "- Công thức máu: WBC 6.8 G/L, RBC 4.20 T/L, HGB 128 g/L, PLT 245 G/L.\n- Đông máu: PT-INR 1.01, APTT 28.5s, Fibrinogen 3.4 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 4.8 mmol/L, Creatinin 68 µmol/L, AST 22 U/L, ALT 18 U/L, Điện giải Na/K/Cl (138/4.0/102 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL: Thoái hóa đĩa đệm đa tầng. Trượt L4 ra trước độ I, hở eo L5 trái. Phồng thoát vị đĩa đệm L4/5, L5/S1 gây hẹp ống sống, chèn ép rễ thần kinh L4, L5, S1 hai bên (ưu thế rễ L5 phải). Rách vòng xơ đĩa đệm L2/3, L3/4, L5/S1. Phù khối khớp bên L4/5, L5/S1. X-quang CSTL: Trượt đốt sống L4 ra trước L5 độ I.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "TLIF L4-L5, L5-S1, CĐCS Nẹp bán động L3-L4, phong bế khớp cùng chậu (P)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "08 giờ 00 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu trong và sau mổ, rách màng tủy rò dịch não tủy, tổn thương rễ thần kinh L4-L5-S1 và chùm đuôi ngựa, tụ máu chèn ép thần kinh, nhiễm trùng vết mổ sâu, lỏng hoặc gãy nẹp vít sau mổ."
            });

            // BN 2
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0001140537",
                PatientName = "ĐẶNG THỊ CHUNG",
                Dob = "1952",
                Gender = "Nữ",
                Address = "Phường Minh Xuân, Thành phố Tuyên Quang",
                InTime = "08/09/2026 23:04",
                Diagnosis = "Xẹp cấp thân đốt sống T11, D9 cũ / Đái tháo đường typ 2, Rối loạn tiền đình, Rối loạn giấc ngủ",
                History = "Đái tháo đường typ 2 điều trị thuốc uống thường xuyên, nhịp nhanh xoang, thoái hóa CSTL, chóng mặt tiền đình mạn tính.",
                Course = "Bệnh nhân nữ 74 tuổi, có tiền sử ĐTĐ typ 2. Khoảng 10 ngày trước vào viện mệt mỏi, đi ngoài lỏng, chóng mặt chòng chành, đau chói vùng cột sống ngực thắt lưng khi thay đổi tư thế, đau tăng nhiều khi ngồi dậy hoặc ho, được điều trị ổn định tại Trung tâm Đột quỵ. Chụp MRI ngực và thắt lưng xác định xẹp cấp thân T11 còn phù tủy xương -> chuyển Khoa CTCH & Cột sống can thiệp ngoại khoa.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 68 ck/p, HA 110/70 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh táo, tiếp xúc tốt, GCS 15đ. Cột sống ngực: Ấn đau chói gai sau T11, gõ dồn dọc cột sống đau tăng tại T11. Hạn chế vận động cúi ngửa xoay thân do đau, không tự ngồi dậy được, nằm ngửa cố định. Không có dấu hiệu chèn ép tủy ngực hay rễ thần kinh; phản xạ gân xương chi dưới đều, Babinski (-) hai bên; cơ lực tứ chi 5/5, đại tiểu tiện tự chủ. Hội chứng tiền đình thuyên giảm. Tim đều, T1 T2 rõ; Phổi thông khí tốt, không rale; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 4.71 G/L, RBC 4.01 T/L, HGB 120 g/L, HCT 0.349, PLT 221 G/L.\n- Đông máu: PT-INR 0.98, APTT 27.1s, Fibrinogen 3.72 g/L.\n- Sinh hóa máu: Glucose 5.7 mmol/L (ĐMMM dao động 6.5-8.3 mmol/L), Ure 4.5 mmol/L, Creatinin 70 µmol/L, AST 30 U/L, ALT 19 U/L, Điện giải Na/K/Cl (135/3.6/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI cột sống ngực & thắt lưng: Xẹp cấp thân T11 còn phù tủy xương T11 rõ rệt không đẩy lồi tường sau, xẹp cũ D9. Phình nhẹ đĩa đệm L4/5, L5/S1 không chèn ép thần kinh. MRI sọ não: Thoái hóa myelin rải rác Fazekas 1. X-quang ngực: Dày tổ chức kẽ, xẹp D9, D11, vôi hóa quai ĐMC. Siêu âm tim: Hở van chủ nhẹ, EF bình thường.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Bơm xi măng sinh học thân đốt sống T11 (BXM)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Toàn",
                SurgeryTime = "08 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Thoát rò xi măng vào ống sống gây chèn ép tủy ngực, tràn xi măng vào tĩnh mạch cạnh sống gây thuyên tắc phổi (PE), tụ máu vết chọc kim, dao động đường huyết và huyết áp chu phẫu."
            });

            // BN 3
            list.Add(new PatientProfile {
                Stt = 3,
                PatientCode = "0004020895",
                PatientName = "ĐỖ THỊ VUI",
                Dob = "1987",
                Gender = "Nữ",
                Address = "Xã Thọ Xuân, Huyện Đan Phượng, Hà Nội",
                InTime = "11/09/2026 10:15",
                Diagnosis = "Thoát vị đĩa đệm L5-S1 lệch trái cấp tính chèn ép rễ S1 trái, phồng kèm rách vòng xơ đĩa đệm L4-5",
                History = "Chưa phát hiện bệnh lý mạn tính đặc biệt, không dị ứng thuốc.",
                Course = "Cách vào viện 2 tuần, sau khi mang vác vật nặng bệnh nhân đột ngột xuất hiện đau dữ dội vùng thắt lưng lan xuống mông, mặt sau đùi và gót chân trái. Đau buốt kiểu điện giật, tê bì dọc bờ ngoài bàn chân và ngón 5 chân trái, đi lại rất khó khăn (phải nghiêng người chống tay vào đùi). Đã điều trị giảm đau và châm cứu tuyến dưới không đỡ, đau tăng nhiều (VAS 8/10) -> vào viện theo lịch mổ phiên.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 115/75 mmHg, T° 36.6°C, SpO2 99%. Cột sống thắt lưng mất ưỡn sinh lý, vẹo nhẹ sang phải chống đau. Ấn điểm Valleix và cạnh sống L5-S1 bên trái đau buốt lan dọc chân trái (nghiệm pháp bấm chuông (+)). Lasègue chân trái (+) 35 độ, chân phải (-) 80 độ. Giảm cảm giác nông bờ ngoài bàn chân và ngón út trái (vùng rễ S1). Cơ lực nhóm cơ gấp lòng bàn chân trái 4/5 (đứng bằng mũi chân yếu), cơ lực gấp mu bàn chân 5/5. Phản xạ gân gót trái giảm so với bên phải. Cơ tròn bình thường. Tim phổi lồng ngực bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.2 G/L, RBC 4.35 T/L, HGB 131 g/L, PLT 250 G/L.\n- Đông máu: PT-INR 1.0, APTT 29.0s, Fibrinogen 3.1 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 4.2 mmol/L, Creatinin 65 µmol/L, AST 20 U/L, ALT 17 U/L, Điện giải Na/K/Cl (139/4.1/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL: Thoái hóa đĩa đệm đa tầng. Phồng kèm rách vòng xơ L4-5 chèn ép rễ L5 hai bên ngách bên. Phồng kèm thoát vị đĩa đệm L5-S1 chèn ép rễ thần kinh S1 hai bên (ưu thế nhiều bên trái đoạn ngách bên). Phù nề nhẹ phần mềm dưới da vùng lưng. X-quang CSTL: Thoái hóa cột sống thắt lưng.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật nội soi lấy nhân thoát vị đĩa đệm L5S1 trái",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "09 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh S1 gây tê bì yếu cơ kéo dài, tụ máu hố mổ, nhiễm trùng vết mổ nội soi, tái phát thoát vị đĩa đệm sau mổ."
            });

            // BN 4
            list.Add(new PatientProfile {
                Stt = 4,
                PatientCode = "0004036327",
                PatientName = "NGUYỄN THỊ CHỈNH",
                Dob = "1956",
                Gender = "Nữ",
                Address = "Xã Yên Đồng, Huyện Ý Yên, Tỉnh Nam Định",
                InTime = "10/09/2026 14:20",
                Diagnosis = "Loãng xương nặng, Xẹp cấp thân đốt sống L2 phù tủy xương, thoái hóa CSTL, phình đĩa đệm L1/2, L4/5",
                History = "Loãng xương phát hiện nhiều năm điều trị không đều, thoái hóa khớp gối hai bên, không có tiền sử THA hay bệnh tim mạch.",
                Course = "Cách vào viện 1 tuần, bệnh nhân trượt chân ngã ngồi đập mông xuống nền cứng. Sau ngã xuất hiện đau chói dữ dội thắt lưng trên, đau tăng khi cử động, ho hoặc trở mình, không thể ngồi dậy hay đi lại được. Chụp MRI xác định xẹp cấp thân L2 kèm phù tủy xương rõ rệt -> vào viện Khoa CTCH & CS để phẫu thuật bơm xi măng sinh học.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 98%. Thể trạng gầy, già yếu. Ấn điểm đau chói cố định tại gai sau L2, gõ dồn dọc cột sống đau nhói tại L2 (VAS 8/10). Co cứng khối cơ dựng sống hai bên. Vận động cột sống thắt lưng hạn chế tối đa, bệnh nhân chỉ nằm ngửa bất động. Thần kinh: Không có dấu hiệu chèn ép rễ hay tủy; cơ lực 2 chi dưới 5/5, cảm giác da bình thường; phản xạ gân bánh chè và gân gót đều 2 bên; đại tiểu tiện tự chủ. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 5.5 G/L, RBC 3.90 T/L, HGB 118 g/L, PLT 210 G/L.\n- Đông máu: PT-INR 1.02, APTT 28.0s, Fibrinogen 3.3 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 4.9 mmol/L, Creatinin 72 µmol/L, AST 25 U/L, ALT 21 U/L, Điện giải Na/K/Cl (137/3.9/100 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL: Thoái hóa đốt sống đĩa đệm cột sống thắt lưng. Phình đĩa đệm L1/2, L4/5, rách bao xơ đĩa đệm L4/5. Xẹp cấp đốt sống L2 kèm phù tủy xương. X-quang CSTL: Thoái hóa cột sống thắt lưng, xẹp L1 cũ, xẹp cấp L2.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Bơm xi măng sinh học thân đốt sống L2 (BXM)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Toàn",
                SurgeryTime = "10 giờ 00 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Thoát xi măng vào ống sống hoặc lỗ tiếp hợp gây chèn ép thần kinh, tràn xi măng vào tĩnh mạch cạnh sống gây thuyên tắc phổi, tụ máu vết chọc, gãy xẹp đốt sống lân cận do loãng xương nặng."
            });

            // BN 5
            list.Add(new PatientProfile {
                Stt = 5,
                PatientCode = "0003068447",
                PatientName = "NGUYỄN THỊ LŨ",
                Dob = "1945",
                Gender = "Nữ",
                Address = "Xã Thọ Diên, Huyện Thọ Xuân, Tỉnh Thanh Hóa",
                InTime = "12/09/2026 09:30",
                Diagnosis = "Xẹp cấp thân đốt sống T11 có phù tủy xương / Xẹp cũ T12, L2, L4, Loãng xương người già (81 tuổi), Phình đĩa đệm đa tầng",
                History = "81 tuổi, loãng xương nặng, tiền sử xẹp đốt sống T11 cũ đã can thiệp, xơ vữa quai động mạch chủ.",
                Course = "Bệnh nhân nữ 81 tuổi, khoảng 10 ngày trước xuất hiện đau thắt lưng ngực tăng dần sau khi cúi bê đồ, đau liên tục tăng dữ dội khi ngồi hoặc đứng, nằm yên đỡ đau. Mọi sinh hoạt phụ thuộc hoàn toàn vào người nhà. Khám chụp MRI phát hiện xẹp tiến triển thân T11 có phù tủy xương rõ rệt -> vào viện để can thiệp bơm xi măng bóng.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 135/85 mmHg, T° 36.5°C, SpO2 97%. Thể trạng già yếu, gù vẹo nhẹ cột sống ngực - thắt lưng. Ấn đau chói khu trú gai sau T11-T12, cơ cạnh sống co cứng nhiều. Cột sống ngực - thắt lưng hạn chế vận động hoàn toàn do đau. Thần kinh: Cử động hai chân trên giường bình thường, cơ lực 4/5 do đau, phản xạ gân xương bánh chè đều 2 bên, cảm giác nông chi dưới bình thường, Babinski (-) 2 bên, không rối loạn cơ tròn. Tim đều, T1 T2 rõ; Phổi thông khí đều 2 bên; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 5.1 G/L, RBC 3.80 T/L, HGB 115 g/L, PLT 198 G/L.\n- Đông máu: PT-INR 1.05, APTT 30.1s, Fibrinogen 3.6 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 5.6 mmol/L, Creatinin 80 µmol/L, AST 28 U/L, ALT 23 U/L, Điện giải Na/K/Cl (136/3.8/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL: Thoái hóa đốt sống đĩa đệm thắt lưng, vẹo nhẹ cột sống sang trái. Xẹp các thân đốt sống D11, D12, L2, L4, có phù nhẹ tủy xương đốt sống D11. Phình đa tầng đĩa đệm từ L1-2 đến L5-S1 gây hẹp ống sống và lỗ tiếp hợp hai bên, chèn ép rễ L5 hai bên. X-quang: Vài dải mờ kẽ rải rác hai phổi, quai ĐMC giãn và vôi hóa; xẹp thân L1, xẹp T11 đã đổ ciment.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Bơm xi măng sinh học có bóng thân đốt sống T11 (BXM có bóng)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "BS. Lê Đăng Toàn",
                SurgeryTime = "10 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Rò xi măng vào ống sống hoặc ngoài thân đốt, thuyên tắc mạch do xi măng/mỡ, biến cố tim mạch hô hấp ở người bệnh cao tuổi (81t), tụ máu vết chọc."
            });

            // BN 6
            list.Add(new PatientProfile {
                Stt = 6,
                PatientCode = "0002039303",
                PatientName = "PHẠM THỊ ANH",
                Dob = "1969",
                Gender = "Nữ",
                Address = "Xã Chí Tiên, Huyện Thanh Ba, Tỉnh Phú Thọ",
                InTime = "14/09/2026 11:00",
                Diagnosis = "Xẹp thân đốt sống ngực cao T3 / Loãng xương - Suy tuyến thượng thận do thuốc, Sỏi niệu quản phải 1/3 trên",
                History = "Tự dùng thuốc giảm đau corticoid kéo dài gây suy thượng thận thứ phát, loãng xương, sỏi tiết niệu.",
                Course = "Bệnh nhân đau nhức dữ dội vùng lưng trên (liên bả vai) 2 tuần nay, đau nhói lan ra trước ngực theo dây thần kinh liên sườn, đau tăng khi hít thở sâu hoặc ho, đã khám phát hiện xẹp đốt sống ngực T3 kèm sỏi niệu quản phải -> vào viện Khoa CTCH & CS điều trị phẫu thuật cột sống trước kết hợp bù hydrocortisone nội tiết.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 110/70 mmHg, T° 36.6°C, SpO2 98%. Thể trạng kiểu hình Cushing nhẹ (mặt tròn, da mỏng). Ấn đau chói gai sau đốt sống T3 gian bả vai, gõ dồn đau nhức rõ rệt. Không biến dạng gù vẹo cấp. Thần kinh: Phản xạ gân xương chi dưới bình thường, Babinski (-), không có dấu hiệu liệt vận động hay chèn ép tủy ngực, cơ lực 5/5, đại tiểu tiện tự chủ. Hệ tiết niệu: Chạm thận (-), bập bềnh thận (-), ấn niệu quản trên phải tức nhẹ. Tim đều, bóng tim to nhẹ; Phổi thông khí tốt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 7.2 G/L, RBC 4.10 T/L, HGB 122 g/L, PLT 230 G/L.\n- Đông máu: PT-INR 1.0, APTT 28.2s, Fibrinogen 3.2 g/L.\n- Sinh hóa máu: Glucose 5.8 mmol/L, Ure 5.0 mmol/L, Creatinin 75 µmol/L, Cortisol máu giảm thấp, AST 26 U/L, ALT 22 U/L, Na/K/Cl (136/3.9/102 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CT hệ tiết niệu: Giãn nhẹ đài bể thận - niệu quản phải do sỏi niệu quản đoạn 1/3 trên. X-quang cột sống ngực: Thoái hóa cột sống ngực, xẹp thân T3, xẹp cũ D11, D12 / loãng xương. X-quang ngực: Bóng tim to nhẹ, dày nhẹ tổ chức kẽ phổi hai bên.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Bơm xi măng sinh học thân đốt sống T3 (BXM)",
                Anesthesia = "Tiền mê + Tê tại chỗ",
                Surgeon = "TS. Nguyễn Văn Trung",
                SurgeryTime = "11 giờ 00 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Cơn suy thượng thận cấp trong mổ, rò xi măng vào ống sống ngực đe dọa chèn ép tủy ngực, tràn khí màng phổi do chọc kim gần màng phổi ngực, thuyên tắc mạch do xi măng."
            });

            // BN 7
            list.Add(new PatientProfile {
                Stt = 7,
                PatientCode = "0004030237",
                PatientName = "DƯƠNG THỊ THẬP",
                Dob = "1970",
                Gender = "Nữ",
                Address = "Thị trấn Thứa, Huyện Lương Tài, Tỉnh Bắc Ninh",
                InTime = "15/09/2026 08:30",
                Diagnosis = "Trượt đốt sống L3 ra trước độ I do hở eo, Thoát vị đĩa đệm và thoái hóa đa tầng L3-L4, L4-L5, L5-S1 chèn ép rễ L4, L5, S1 hai bên / Tăng huyết áp",
                History = "Tăng huyết áp điều trị đều bằng Amlodipine 5mg/ngày, HA kiểm soát ổn định 120-130/80 mmHg.",
                Course = "Đau thắt lưng lan đùi và mặt ngoài cẳng chân hai bên 6 tháng nay, đi bộ khoảng 30m là đau mỏi tê bì chân phải dừng nghỉ (cách hồi thần kinh). Điều trị nội khoa nhiều đợt không đỡ. Chụp MRI xác định trượt L3 hở eo, phồng thoát vị đĩa đệm chèn ép rễ nặng 3 tầng L3-S1 -> vào viện mổ chương trình.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 130/80 mmHg, T° 36.5°C, SpO2 98%. Cột sống thắt lưng: Dấu hiệu bậc thang L3-L4 (+), ấn gai sau L3-L4-L5 đau chói, cơ dựng sống co cứng, hạn chế cúi ngửa cột sống. Thần kinh: Lasègue hai bên (+) 50 độ; giảm cảm giác da mặt ngoài cẳng chân hai bên (rễ L4, L5). Cơ lực duỗi ngón cái hai bên 4/5, cơ lực gấp bàn chân 5/5. Phản xạ gân bánh chè giảm nhẹ hai bên, phản xạ gân gót bình thường. Không rối loạn cơ tròn. Tim đều, T1 T2 rõ; Phổi thông khí tốt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.1 G/L, RBC 4.25 T/L, HGB 126 g/L, PLT 240 G/L.\n- Đông máu: PT-INR 0.99, APTT 27.8s, Fibrinogen 3.5 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 4.6 mmol/L, Creatinin 71 µmol/L, AST 24 U/L, ALT 20 U/L, Điện giải Na/K/Cl (137/4.0/100 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL: Thoái hóa đốt sống đĩa đệm thắt lưng, trượt L3 ra trước độ I do hở eo, thoái hóa modic 1 đĩa tận L4, L5. Rách vòng xơ kèm phình các đĩa đệm L3/4, L4/5, L5/S1 gây chèn ép rễ L4, L5, S1 trong ngách bên hai bên, tiếp xúc chùm đuôi ngựa L3/4. X-quang CSTL thẳng nghiêng và động gập ưỡn: Trượt L3 ra trước độ I / Thoái hóa đốt sống thắt lưng.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "TLIF 3 tầng (L3-L4, L4-L5, L5-S1), cố định cột sống thắt lưng",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Nguyễn Đức Hoàng",
                SurgeryTime = "08 giờ 00 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu phẫu trường lớn (mổ 3 tầng), rách màng tủy rò dịch não tủy, tổn thương rễ thần kinh L3, L4, L5, tụ máu vết mổ chèn ép thần kinh, nhiễm trùng vết mổ sâu, lỏng hoặc nhổ vít xốp xương."
            });

            // BN 8
            list.Add(new PatientProfile {
                Stt = 8,
                PatientCode = "0001405855",
                PatientName = "NGUYỄN THỊ LAN",
                Dob = "1990",
                Gender = "Nữ",
                Address = "Xã Gia Tân, Huyện Gia Viễn, Tỉnh Ninh Bình",
                InTime = "15/09/2026 15:40",
                Diagnosis = "Hoại tử vô khuẩn chỏm xương đùi phải Ficat giai đoạn III / Khớp háng nhân tạo bên trái, Lupus ban đỏ hệ thống (SLE)",
                History = "Mắc Lupus ban đỏ hệ thống nhiều năm điều trị Medrol kéo dài; đã thay khớp háng nhân tạo bên trái năm 2024 kết quả tốt.",
                Course = "Sau thay khớp háng trái, bệnh nhân xuất hiện đau khớp háng phải tăng dần khi tì đè và đi lại. Gần đây đau nhức nhiều vùng bẹn phải lan xuống đùi và gối, đi khập khiễng, hạn chế gấp và xoay khớp háng, dùng thuốc giảm đau kém đáp ứng. X-quang xác định hoại tử biến dạng chỏm xương đùi phải Ficat III -> vào viện phẫu thuật thay khớp háng phải.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 115/70 mmHg, T° 36.6°C, SpO2 99%. Bệnh nhân tỉnh, thể trạng trung bình, niêm mạc hồng. Khám khớp háng phải: Ấn tam giác Scarpa bên phải đau chói, dồn gót đau khớp háng. Biên độ khớp háng phải hạn chế nhiều: Gấp 80°, duỗi 0°, dạng 20°, khép 15°, xoay trong và ngoài hạn chế nặng. Nghiệm pháp Patrick bên phải (+). Khớp háng bên trái: Liền sẹo tốt, khớp háng nhân tạo vận động tốt, không đau. Chiều dài 2 chi dưới tương đối cân đối. Mạch mu chân và chày sau bắt rõ. Tim phổi bình thường, bụng mềm, không có đợt bùng phát Lupus.",
                FullCls = "- Công thức máu: WBC 5.8 G/L, RBC 3.95 T/L, HGB 116 g/L, PLT 215 G/L.\n- Đông máu: PT-INR 1.02, APTT 29.5s, Fibrinogen 3.4 g/L.\n- Sinh hóa máu: Glucose 5.0 mmol/L, Ure 4.7 mmol/L, Creatinin 69 µmol/L, AST 23 U/L, ALT 19 U/L, Điện giải bình thường.\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: X-quang khung chậu & khớp háng: Biến dạng, xẹp chỏm xương đùi phải - theo dõi hoại tử vô khuẩn chỏm xương đùi phải Ficat III, gai xương bờ ổ chảo. Khớp háng nhân tạo bên trái ổn định, không lỏng khớp hay biến dạng vật liệu. X-quang ngực: Bình thường.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật thay khớp háng phải toàn bộ",
                Anesthesia = "Mê nội khí quản (hoặc Tê tủy sống)",
                Surgeon = "TS.BS. Hà Đức Cường",
                SurgeryTime = "09 giờ 00 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu trong và sau mổ trên nền Lupus, trật khớp háng nhân tạo sau mổ, tổn thương thần kinh hông to/thần kinh đùi, nứt vỡ xương chu phẫu, nhiễm trùng khớp háng nhân tạo, huyết khối tĩnh mạch sâu chi dưới."
            });

            // BN 9
            list.Add(new PatientProfile {
                Stt = 9,
                PatientCode = "0002635203",
                PatientName = "TRẦN VĂN QUANG",
                Dob = "1999",
                Gender = "Nam",
                Address = "Xã Gia Xuyên, Huyện Gia Lộc, Tỉnh Hải Dương",
                InTime = "15/09/2026 14:00",
                Diagnosis = "Đứt hoàn toàn dây chằng chéo trước khớp gối trái sau chấn thương thể thao, tràn dịch khớp gối trái",
                History = "Thanh niên 27 tuổi khỏe mạnh, thể thao thường xuyên, không bệnh lý nội khoa, không dị ứng thuốc.",
                Course = "Cách vào viện 3 tuần, trong khi đá bóng bị xoay vặn khớp gối trái đột ngột, nghe tiếng \"rắc\", gối mất vững sưng đau nhiều. Đã sơ cứu chườm đá bất động nẹp. Sau bớt sưng cảm giác lỏng khớp gối, bước hụt chân khi đi nhanh hoặc xuống thang. Chụp MRI xác định đứt hoàn toàn DCCT gối trái -> vào viện phẫu thuật nội soi tái tạo dây chằng.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 99%. Thể trạng khỏe mạnh, cơ bắp tốt. Khớp gối trái sưng nề nhẹ, dấu hiệu bập bềnh xương bánh chè (+/-). Tầm vận động gấp duỗi gối 0 - 120 độ. Nghiệm pháp độ vững khớp gối: Ngăn kéo trước (Anterior Drawer test) (+), Nghiệm pháp Lachman (+) độ II-III điểm dừng mềm, Pivot shift (+). Ngăn kéo sau (-), mở khe khớp trong và ngoài (-). Mạch mu chân và chày sau bắt rõ, cảm giác vận động bàn chân bình thường. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.5 G/L, RBC 4.80 T/L, HGB 145 g/L, PLT 260 G/L.\n- Đông máu: PT-INR 0.98, APTT 26.5s, Fibrinogen 3.0 g/L.\n- Sinh hóa máu: Glucose 4.9 mmol/L, Ure 4.4 mmol/L, Creatinin 78 µmol/L, AST 21 U/L, ALT 24 U/L, Điện giải Na/K/Cl (140/4.2/103 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI khớp gối trái: Đứt hoàn toàn dây chằng chéo trước khớp gối trái, tràn dịch khớp gối. X-quang khớp gối và ngực thẳng: Bình thường, không thấy gãy xương.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật nội soi tái tạo dây chằng chéo trước gối trái",
                Anesthesia = "Tê tủy sống",
                Surgeon = "TS.BS. Hà Đức Cường",
                SurgeryTime = "10 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu tụ máu ổ khớp gối sau mổ, tổn thương sụn khớp chu phẫu, nhiễm trùng khớp gối nội soi, lỏng mảnh ghép tái phát, hạn chế biên độ vận động khớp gối (cứng khớp) sau mổ."
            });

            // BN 10
            list.Add(new PatientProfile {
                Stt = 10,
                PatientCode = "0004042813",
                PatientName = "NGUYỄN VĂN NHỜ",
                Dob = "1980",
                Gender = "Nam",
                Address = "Xã Nam Hồng, Huyện Nam Trực, Tỉnh Nam Định",
                InTime = "13/09/2026 15:30",
                Diagnosis = "Hội chứng ống cổ tay 2 bên mức độ nặng (Phải > Trái), teo nhẹ cơ ô mô cái bàn tay phải",
                History = "Lao động thủ công dùng tay nhiều năm, đau dạ dày mạn tính, không có tiền sử THA hay ĐTĐ.",
                Course = "Tê bì các đầu ngón 1, 2, 3 và nửa ngón 4 của cả hai bàn tay khoảng 1 năm nay, bên phải nặng hơn bên trái. Tê nhiều về đêm và sáng sớm làm mất ngủ, phải thức dậy vẩy tay mới đỡ. Gần đây bàn tay phải yếu, cầm nắm đồ vật dễ rơi, khó cài cúc áo. Điện cơ (EMG) xác định tổn thương thần kinh giữa đoạn ống cổ tay 2 bên mức độ nặng -> vào viện phẫu thuật giải ép.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 120/80 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh, thể trạng tốt. Khám bàn ngón tay hai bên (P > T): Ấn nếp gấp cổ tay dấu hiệu Tinel (+) rõ rệt bên phải, nghiệm pháp Phalen (+) sau 30 giây (tê buốt dọc các ngón 1, 2, 3 bàn tay phải). Vận động: Teo nhẹ khối cơ ô mô cái bàn tay phải so với tay trái; động tác đối chiếu ngón cái với ngón út tay phải yếu (cơ lực 4/5). Cảm giác: Giảm cảm giác nông mặt gan các ngón 1, 2, 3 tay phải. Mạch quay và mạch trụ hai bên bắt rõ. Tim phổi bình thường, bụng mềm.",
                FullCls = "- Công thức máu: WBC 5.9 G/L, RBC 4.60 T/L, HGB 138 g/L, PLT 235 G/L.\n- Đông máu: PT-INR 1.0, APTT 28.0s, Fibrinogen 3.1 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 4.5 mmol/L, Creatinin 76 µmol/L, AST 22 U/L, ALT 25 U/L, Điện giải bình thường.\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Thăm dò chức năng & CĐHA: Điện cơ (EMG): Tổn thương dẫn truyền thần kinh giữa hai bên đoạn qua ống cổ tay mức độ nặng. X-quang ngực thẳng và cổ tay: Bình thường.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "Không",
                SurgeryMethod = "Phẫu thuật cắt dây chằng vòng giải ép ống cổ tay 2 bên",
                Anesthesia = "Tê tại chỗ (hoặc Mê tĩnh mạch)",
                Surgeon = "BS. Vũ Minh Cường",
                SurgeryTime = "11 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu tụ máu vết mổ gan tay, tổn thương nhánh vận động ô mô cái của thần kinh giữa, tổn thương cung mạch gan tay nông, nhiễm trùng vết mổ, đau sẹo mổ kéo dài (pillar pain)."
            });

            // BN 11
            list.Add(new PatientProfile {
                Stt = 11,
                PatientCode = "0004035437",
                PatientName = "TRẦN THẢO NHI",
                Dob = "2012",
                Gender = "Nữ",
                Address = "Thị trấn Chúc Sơn, Huyện Chương Mỹ, Hà Nội",
                InTime = "12/09/2026 16:00",
                Diagnosis = "Vỡ kín mâm chày và đầu trên xương chày trái ít di lệch, bong điểm bám dây chằng chéo trước khớp gối trái do Tai nạn giao thông",
                History = "Trẻ em 14 tuổi, khỏe mạnh, không bệnh mạn tính, sụn tiếp hợp đầu xương đang phát triển.",
                Course = "Ngày 12/9/2026 bị tai nạn giao thông ngã đập gối trái xuống đường. Sau tai nạn gối trái sưng to nhanh, đau dữ dội, không thể đứng hay tì chân xuống đất, được sơ cứu nẹp bất động chuyển vào BV Bạch Mai. Chụp CT và X-quang khớp gối xác định vỡ mâm chày và đầu trên xương chày trái, tràn máu khớp gối, bong điểm bám DCCT -> nhập viện phẫu thuật kết hợp xương.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 84 ck/p, HA 105/65 mmHg, T° 36.7°C, SpO2 99%. Bệnh nhân tỉnh, hợp tác, nét mặt đau. Khám chân trái: Khớp gối và 1/3 trên cẳng chân trái sưng nề to, bầm tím dưới da, sờ căng tức, không có vết thương hở (gãy kín). Điểm đau chói cố định tại mâm chày ngoài và đầu trên xương chày trái. Mất hoàn toàn cơ năng khớp gối trái, dấu hiệu bập bềnh xương bánh chè (+). Mạch mu chân và chày sau bắt rõ, ngón chân hồng ấm, CRT < 2s; cảm giác mu và gan bàn chân bình thường; các ngón chân gấp duỗi được; không có dấu hiệu chèn ép khoang cẳng chân. Sọ não, lồng ngực, ổ bụng không có tổn thương phối hợp.",
                FullCls = "- Công thức máu: WBC 7.5 G/L, RBC 4.30 T/L, HGB 125 g/L, PLT 270 G/L.\n- Đông máu: PT-INR 1.01, APTT 27.5s, Fibrinogen 3.2 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 3.9 mmol/L, Creatinin 55 µmol/L, AST 21 U/L, ALT 16 U/L, Điện giải bình thường.\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CT scanner khớp gối: Vỡ xương mâm chày và đầu trên xương chày trái ít di lệch, tràn dịch máu gối trái. CT sọ não: Không thấy tổn thương nội sọ. X-quang ngực thẳng: Bình thường. X-quang gối: Vỡ mâm chày và đầu trên xương chày trái ít di lệch.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Kết hợp xương mâm chày trái qua nội soi (KHX nội soi)",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ)",
                Surgeon = "BS. Lê Văn Luân",
                SurgeryTime = "13 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Chảy máu, hội chứng chèn ép khoang cẳng chân chu phẫu, tổn thương sụn tiếp hợp ở tuổi vị thành niên, cứng khớp gối sau mổ, can lệch mặt khớp, nhiễm trùng vết mổ."
            });

            // BN 12
            list.Add(new PatientProfile {
                Stt = 12,
                PatientCode = "0004018398",
                PatientName = "TRẦN THỊ VINH",
                Dob = "1958",
                Gender = "Nữ",
                Address = "Xã Tô Hiệu, Huyện Thường Tín, Hà Nội",
                InTime = "11/09/2026 15:00",
                Diagnosis = "U bao rễ thần kinh L5 bên trái trong lỗ tiếp hợp L5-S1 (Schwannoma rễ L5), Thoát vị đĩa đệm L4-5, L5-S1 / Đái tháo đường typ 2, Gan nhiễm mỡ",
                History = "Đái tháo đường typ 2 điều trị thuốc viên (đã hội chẩn Nội tiết kiểm soát đường huyết ổn định), gan nhiễm mỡ.",
                Course = "Đau vùng thắt lưng lan xuống hông và mặt ngoài cẳng chân trái kéo dài hơn 1 năm nay. Gần đây đau rát bỏng dữ dội vùng mông và chân trái, đau tăng về đêm gây mất ngủ nhiều, tê bì liên tục mu chân trái, điều trị nội khoa không đỡ. Chụp MRI CSTL có tiêm thuốc và CT ổ bụng phát hiện khối u ngấm thuốc mạnh vị trí lỗ tiếp hợp L5-S1 bên trái kích thước ~15x20mm (Schwannoma rễ L5) chèn ép rễ thần kinh -> vào viện phẫu thuật bóc u và cố định cột sống.",
                HoiChanTime = "14 giờ 00 phút, ngày 16 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh, tiếp xúc tốt. Cột sống thắt lưng: Điểm cạnh sống L5-S1 bên trái ấn đau buốt nhói lan xuống cẳng bàn chân trái, cơ dựng thắt lưng co cứng nhẹ. Thần kinh: Lasègue chân trái (+) 40 độ (đau buốt rễ thần kinh), Lasègue chân phải (-) 80 độ. Dấu hiệu Valleix (+) dọc dây tọa trái. Giảm cảm giác nông mu chân và ngón cái chân trái (rễ L5). Cơ lực duỗi ngón cái và gập mu bàn chân trái 4/5. Phản xạ gân bánh chè và gân gót bình thường. Cơ tròn kiểm soát tốt. Tim đều, T1 T2 rõ; Phổi thông khí tốt; Bụng mềm.",
                FullCls = "- Công thức máu: WBC 6.3 G/L, RBC 4.15 T/L, HGB 124 g/L, PLT 230 G/L.\n- Đông máu: PT-INR 1.0, APTT 28.1s, Fibrinogen 3.3 g/L.\n- Sinh hóa máu: Glucose 6.2 mmol/L (kiểm soát tốt), Ure 4.9 mmol/L, Creatinin 72 µmol/L, AST 27 U/L, ALT 32 U/L, Điện giải Na/K/Cl (138/4.0/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI CSTL có tiêm thuốc tương phản: Thoái hóa đốt sống đĩa đệm thắt lưng. Thoát vị đĩa đệm L4-5, L5-S1 có chèn ép rễ thần kinh. Khối vị trí lỗ tiếp hợp L5-S1 bên trái ngấm thuốc mạnh hướng tới Schwannoma. CT tầng trên ổ bụng: Gan nhiễm mỡ, khối ngấm thuốc trong lỗ tiếp hợp L5-S1 bên trái. CT ngực: Vài nốt vôi và nốt đặc thùy trên phổi trái ổn định.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Phẫu thuật bóc u rễ thần kinh L5 trái, cố định cột sống (Lấy U - CĐCS)",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Nguyễn Đức Hoàng",
                SurgeryTime = "14 giờ 30 phút, ngày 17 tháng 09 năm 2026",
                Risks = "Tổn thương rễ thần kinh L5 trong quá trình bóc u gây yếu liệt gập mu bàn chân, rách màng cứng rò dịch não tủy, chảy máu đám rối tĩnh mạch quanh lỗ tiếp hợp, tụ máu hố mổ chèn ép thần kinh, nhiễm trùng vết mổ."
            });

            return list;
        }
    }
}
