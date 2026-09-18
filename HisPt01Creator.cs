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

            // BN 1: TRẦN THẢO NHI
            list.Add(new PatientProfile {
                Stt = 1,
                PatientCode = "0004035437",
                PatientName = "TRẦN THẢO NHI",
                Dob = "2012",
                Gender = "Nữ",
                Address = "Thị trấn Chúc Sơn, Huyện Chương Mỹ, Hà Nội",
                InTime = "12/09/2026 16:00",
                Diagnosis = "Chấn thương gối trái: Gãy lún mâm chày trái , bong điểm bám dcct + dccs",
                History = "Trẻ em 14 tuổi, khỏe mạnh, không có bệnh lý mạn tính đặc biệt, sụn tiếp hợp đầu xương đang phát triển.",
                Course = "Ngày 12/09/2026 bị tai nạn giao thông ngã đập gối trái xuống đường. Sau tai nạn gối trái sưng đau dữ dội, không thể đứng hay tì chân xuống đất, được sơ cứu nẹp cố định chuyển vào BV Bạch Mai. Chụp CT và X-quang khớp gối xác định vỡ lún mâm chày ngoài và đầu trên xương chày trái, bong điểm bám dây chằng chéo trước và chéo sau, tràn dịch máu khớp gối -> vào viện hội chẩn mổ phiên kết hợp xương nẹp vít kết hợp nội soi hỗ trợ khâu dây chằng chéo.",
                HoiChanTime = "14 giờ 00 phút, ngày 18 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 84 ck/p, HA 105/65 mmHg, T° 36.7°C, SpO2 99%. Bệnh nhân tỉnh, tiếp xúc tốt, nét mặt đau. Khám gối trái: Khớp gối và 1/3 trên cẳng chân trái sưng nề to, bầm tím dưới da, sờ căng tức, không có vết thương hở (gãy kín). Điểm đau chói cố định tại mâm chày ngoài và đầu trên xương chày trái. Mất hoàn toàn cơ năng khớp gối trái, dấu hiệu bập bềnh xương bánh chè (+), ngăn kéo trước (+), ngăn kéo sau (+). Mạch mu chân và chày sau bắt rõ, ngón chân hồng ấm, CRT < 2s; cảm giác mu và gan bàn chân bình thường; các ngón chân gấp duỗi được; không có dấu hiệu chèn ép khoang cẳng chân. Tim đều, phổi thông khí rõ, bụng mềm, ngực vững.",
                FullCls = "- Công thức máu: WBC 7.5 G/L, RBC 4.30 T/L, HGB 125 g/L, PLT 270 G/L.\n- Đông máu: PT-INR 1.01, APTT 27.5s, Fibrinogen 3.2 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 3.9 mmol/L, Creatinin 55 µmol/L, AST 21 U/L, ALT 16 U/L, Điện giải Na/K/Cl (138/4.1/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: CT scanner khớp gối: Vỡ lún mâm chày ngoài và đầu trên xương chày trái ít di lệch, bong điểm bám dây chằng chéo trước và dây chằng chéo sau, tràn dịch máu gối trái. CT sọ não: Không thấy tổn thương nội sọ. X-quang ngực thẳng: Bình thường. X-quang gối: Vỡ mâm chày và đầu trên xương chày trái ít di lệch.",
                BloodGroup = "A Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Kết hợp xương nẹp vít + kết hợp nội soi hỗ trợ khâu dây chằng chéo trước+ chéo sau",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ)",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "08 giờ 00 phút, ngày 19 tháng 09 năm 2026",
                Risks = "Chảy máu, tụ máu khớp gối, hội chứng chèn ép khoang cẳng chân chu phẫu, tổn thương sụn tiếp hợp đầu xương ở tuổi vị thành niên, can lệch mặt khớp, lỏng nẹp vít, cứng khớp gối sau mổ, nhiễm trùng vết mổ sâu."
            });

            // BN 2: NGUYỄN THỊ HỢI
            list.Add(new PatientProfile {
                Stt = 2,
                PatientCode = "0003618705",
                PatientName = "NGUYỄN THỊ HỢI",
                Dob = "1961",
                Gender = "Nữ",
                Address = "Xã Bắc Tiên Hưng, Hưng Yên",
                InTime = "16/09/2026 14:19",
                Diagnosis = "Gãy cổ xương đùi trái / Tăng huyết áp vô căn (nguyên phát)",
                History = "Tăng huyết áp điều trị thuốc thường xuyên, huyết áp kiểm soát ổn định.",
                Course = "Bệnh nhân nữ 65 tuổi, trượt chân ngã đập hông trái xuống nền cứng tại nhà. Sau ngã đau chói vùng háng đùi trái dữ dội, mất hoàn toàn vận động chân trái, không thể tự đứng dậy hay đi lại được. Được gia đình đưa vào BV Bạch Mai cấp cứu. Chụp X-quang khung chậu và khớp háng xác định gãy dưới chỏm cổ xương đùi trái di lệch -> nhập Khoa CTCH & Cột sống phẫu thuật thay khớp háng.",
                HoiChanTime = "14 giờ 00 phút, ngày 18 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 130/85 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh, tiếp xúc tốt. Khám chi dưới trái: Chi trái xoay ngoài áp nhẹ, ngắn hơn bên phải ~1.5 cm, tam giác Scarpa đầy, ấn đau chói điểm mấu chuyển lớn xương đùi trái, gõ dồn từ gót chân đau buốt khớp háng trái. Bất lực vận động hoàn toàn chân trái. Mạch chày sau và mạch mu chân trái bắt rõ, ngón chân hồng ấm, cảm giác và vận động bàn ngón chân bình thường. Tim đều, T1 T2 rõ; Phổi thông khí tốt, không rale; Bụng mềm, không trướng.",
                FullCls = "- Công thức máu: WBC 11.5 G/L, RBC 4.35 T/L, HGB 130 g/L, PLT 283 G/L.\n- Đông máu: PT-INR 0.96, APTT 0.86 (27.5s), Fibrinogen 4.63 g/L.\n- Sinh hóa máu: Glucose 10.9 mmol/L (đã hội chẩn kiểm soát đường huyết chu phẫu), Ure 4.3 mmol/L, Creatinin 59 µmol/L, AST 22 U/L, ALT 30 U/L, Điện giải Na/K/Cl (137/3.9/100 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: X-quang khung chậu thẳng & X-quang xương đùi trái thẳng nghiêng: Hình ảnh gãy dưới chỏm cổ xương đùi trái di lệch, mất liên tục bè xương cổ xương đùi. X-quang ngực thẳng: Bình thường. Siêu âm Doppler tim, van tim: Chức năng tâm thu thất trái bảo tồn (EF 62%), dày đồng tâm thất trái nhẹ do THA. Siêu âm ổ bụng: Chưa thấy tổn thương tạng.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Thay khớp háng trái toàn phần",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ)",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "09 giờ 30 phút, ngày 19 tháng 09 năm 2026",
                Risks = "Chảy máu trong và sau mổ, trật khớp háng nhân tạo, gãy nứt xương quanh chuôi khớp nhân tạo, huyết khối tĩnh mạch sâu chi dưới gây thuyên tắc mạch phổi, tổn thương thần kinh ngồi, nhiễm trùng chu phẫu khớp nhân tạo."
            });

            // BN 3: PHẠM THỊ NGA
            list.Add(new PatientProfile {
                Stt = 3,
                PatientCode = "0004052694",
                PatientName = "PHẠM THỊ NGA",
                Dob = "1955",
                Gender = "Nữ",
                Address = "Xã Hải Tiến, Ninh Bình",
                InTime = "17/09/2026 08:07",
                Diagnosis = "Gãy cổ xương đùi trái/ TVDD L45 - Xẹp T12 đã BXM (Thoát vị đĩa đệm cột sống xác định khác)",
                History = "Loãng xương nặng, xẹp đốt sống T12 đã được phẫu thuật bơm xi măng sinh học (BXM) ổn định, thoái hóa cột sống thắt lưng, thoát vị đĩa đệm L4-L5.",
                Course = "Bệnh nhân nữ 71 tuổi, có tiền sử loãng xương và xẹp T12 đã BXM. Ngày 17/09/2026 sẩy chân trượt ngã đập mông và háng trái xuống nền cứng, sau ngã đau dữ dội khớp háng đùi trái, mất hoàn toàn khả năng vận động và đi lại. Được cố định tạm thời chuyển đến BV Bạch Mai. Chụp X-quang và CT xác định gãy hoàn toàn cổ xương đùi trái di lệch (Garden IV) trên nền xương loãng nặng -> nhập viện Khoa CTCH & Cột sống phẫu thuật thay toàn bộ khớp háng trái.",
                HoiChanTime = "14 giờ 00 phút, ngày 18 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 125/80 mmHg, T° 36.6°C, SpO2 98%. Bệnh nhân tỉnh, thể trạng già gầy. Khám chi dưới trái: Chi trái xoay ngoài áp nhẹ, ngắn hơn chân phải ~2cm. Điểm đau chói cố định vùng cổ xương đùi và mấu chuyển lớn trái, gõ dồn gót đau tăng vùng háng trái. Bất lực vận động hoàn toàn chi dưới trái. Mạch mu chân và chày sau 2 bên rõ, ngón chân cử động được, không tê bì mới, cảm giác nông ngọn chi bình thường. Vết chọc kim bơm xi măng T12 cũ liền sẹo tốt, không sưng đau. Tim đều, T1 T2 rõ; Phổi thông khí 2 bên đều, không rale; Bụng mềm, không trướng.",
                FullCls = "- Công thức máu: WBC 6.8 G/L, RBC 4.40 T/L, HGB 138 g/L, PLT 232 G/L.\n- Đông máu: PT-INR 1.05, APTT 0.95 (29.0s), Fibrinogen 4.44 g/L.\n- Sinh hóa máu: Glucose 6.6 mmol/L, Ure 5.8 mmol/L, Creatinin 76 µmol/L, AST 27 U/L, ALT 17 U/L, Điện giải Na/K/Cl (138/4.0/101 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (-), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: X-quang khung chậu & khớp háng trái: Hình ảnh gãy hoàn toàn cổ xương đùi trái di lệch nhiều (Garden IV), loãng xương nặng. Hình ảnh xi măng sinh học đọng tốt trong thân đốt sống T12 cũ. MRI CSTL: Thoát vị đĩa đệm L4/5 phồng nhẹ không hẹp nặng ống sống. Siêu âm Doppler tim: EF 60%, van tim thoái hóa nhẹ. Siêu âm Doppler mạch máu chi dưới: Hiện chưa thấy huyết khối tĩnh mạch sâu hai chi dưới.",
                BloodGroup = "O Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Thay toàn bộ khớp háng trái",
                Anesthesia = "Tê tủy sống (hoặc Mê NKQ)",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "11 giờ 00 phút, ngày 19 tháng 09 năm 2026",
                Risks = "Chảy máu trong và sau mổ, nguy cơ nứt vỡ xương đùi do loãng xương nặng trong quá trình đóng chuôi khớp, trật khớp háng nhân tạo sau mổ, huyết khối tĩnh mạch sâu chi dưới gây thuyên tắc phổi, tụ máu vết mổ, nhiễm trùng chu phẫu khớp nhân tạo."
            });

            // BN 4: KIM ĐÌNH Ý
            list.Add(new PatientProfile {
                Stt = 4,
                PatientCode = "0003346311",
                PatientName = "KIM ĐÌNH Ý",
                Dob = "12/04/1958",
                Gender = "Nam",
                Address = "Xã Tam Dương, Phú Thọ",
                InTime = "04/09/2026 08:51",
                Diagnosis = "Rách gân cơ gấp tự phát vai phải - Van động mạch chủ cơ học - Suy tim - Viêm gan B",
                History = "Thay van động mạch chủ cơ học, suy tim (đang dùng thuốc chống đông kháng vitamin K Sintrom/warfarin đã được chuyển gối sang Heparin trọng lượng phân tử thấp Gemapaxane chu phẫu), viêm gan virus B mạn tính, bệnh Gút mạn tính nhiều vị trí, tăng huyết áp.",
                Course = "Bệnh nhân nam 68 tuổi, mang van ĐMC cơ học nhiều năm. Khoảng 3 tuần nay xuất hiện đau nhức khớp vai phải tự phát tăng dần, đau tăng nhiều về đêm và khi vận động dạng, xoay ngoài khớp vai, cử động tay lên cao bị hạn chế nghiêm trọng. Đã được khám và chụp MRI khớp vai phải xác định rách toàn bộ bề dày chóp xoay (gân cơ trên gai/dưới gai) tự phát. Đã nhập viện điều trị nội khoa, hội chẩn Tim mạch - Dược lâm sàng để điều chỉnh đông máu (PT-INR kiểm soát an toàn trước mổ) -> chuyển mổ phiên nội soi khâu phục hồi chóp xoay.",
                HoiChanTime = "14 giờ 00 phút, ngày 18 tháng 09 năm 2026",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 98%. Bệnh nhân tỉnh táo, tiếp xúc tốt. Khám khớp vai phải: Ấn đau chói diện bám gân chóp xoay ở củ lớn xương cánh tay, nghiệm pháp Neer (+), Hawkins (+), Jobe test (+) đau yếu cơ trên gai; tầm vận động khớp vai phải hạn chế (gập 90 độ, dạng 80 độ, xoay ngoài hạn chế do đau). Cảm giác vùng cơ delta và bàn tay phải bình thường, mạch quay phải bắt rõ. Khám tim mạch: Tiếng click cơ học van ĐMC rõ, tim đều, không có tiếng thổi bệnh lý mới; không khó thở khi nằm, không phù chi. Phổi thông khí tốt; Bụng mềm, gan lách không to; Các hạt tophi gút rải rác khớp ngón chân/tay không loét.",
                FullCls = "- Công thức máu: WBC 6.6 G/L, RBC 3.95 T/L, HGB 121 g/L, PLT 278 G/L.\n- Đông máu: PT-INR 3.56 (đang điều chỉnh ngưng thuốc kháng VK và gối LMWH Gemapaxane để đưa INR về ngưỡng an toàn < 1.5 trước rạch da), APTT 1.07, Fibrinogen 4.42 g/L.\n- Sinh hóa máu: Glucose 5.6 mmol/L, Ure 6.6 mmol/L, Creatinin 116 µmol/L, AST 114 U/L, ALT 90 U/L (theo dõi men gan do viêm gan B mạn tính), Acid Uric 480 µmol/L, Điện giải Na/K/Cl (140/4.2/103 mmol/L).\n- Vi sinh & Miễn dịch: HBsAg (+), Anti-HCV (-), HIV (-).\n- Chẩn đoán hình ảnh: MRI khớp vai phải: Rách hoàn toàn bề dày gân cơ trên gai vai phải có tụt đầu gân Patte II, thoái hóa mỡ gân cơ dưới gai độ 1, viêm bao hoạt dịch dưới mỏm cùng vai. Siêu âm tim: Van động mạch chủ cơ học hoạt động tốt, chênh áp qua van trong giới hạn chấp nhận được, EF 55%, buồng tim không giãn nhiều. X-quang tim phổi: Chỉ số tim ngực trong giới hạn, hình ảnh vòng van cơ học cản quang.",
                BloodGroup = "B Rh(+)",
                BloodReserve = "350",
                SurgeryMethod = "Nội soi khâu chóp xoay",
                Anesthesia = "Mê nội khí quản",
                Surgeon = "BS. Hà Đức Cường",
                SurgeryTime = "13 giờ 30 phút, ngày 19 tháng 09 năm 2026",
                Risks = "Chảy máu trong và sau mổ do nguy cơ rối loạn đông máu trên bệnh nhân mang van tim cơ học, biến cố huyết khối kẹt van hoặc thuyên tắc mạch do dừng thuốc chống đông, tổn thương thần kinh nách, tụ dịch vết mổ, đứt lại gân sau khâu, cứng khớp vai sau mổ."
            });

            return list;
        }
    }
}
