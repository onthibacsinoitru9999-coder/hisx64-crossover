using System;
using System.IO;
using System.Reflection;
using System.Text;
using Aspose.Words;

namespace HisPt01Creator
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
            };
            Run();
        }

        static void Run()
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
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (!Directory.Exists(outDirDated)) Directory.CreateDirectory(outDirDated);
            if (!Directory.Exists(outDirStd)) Directory.CreateDirectory(outDirStd);

            Document doc = new Document(templatePath);

            string patientName = "NGUYỄN THỊ NGỌC";
            string dob = "17/09/1976";
            string gender = "Nữ";
            string address = "Phường Phạm Sư Mạnh, Thành phố Hải Phòng";
            string inTime = "08:30 ngày 29/09/2026";
            string diagnosis = "Rách hoàn toàn gân chóp xoay khớp vai phải (gân trên gai và gân dưới gai) - Hẹp khoang dưới mỏm cùng vai / Tiền sử Ung thư nội mạc tử cung đã phẫu thuật cắt tử cung hoàn toàn";
            string history = "Tiền sử Ung thư nội mạc tử cung phát hiện và phẫu thuật cắt tử cung toàn bộ + 2 phần phụ năm 2023, khám định kỳ ổn định không tái phát.";
            string course = "Bệnh nhân nữ 50 tuổi, đau khớp vai phải âm ỉ 6 tháng nay, đau tăng nhiều về đêm làm mất ngủ, không thể nằm nghiêng bên phải. Gần đây đau chói khi giơ tay lên cao quá đầu, chải tóc hoặc với tay ra sau lưng, cảm giác vai phải yếu rõ rệt, không nâng được đồ vật. Đã tiêm nội khớp và điều trị nội khoa tại Hải Phòng không đỡ. Khám tại BV Bạch Mai, chụp MRI khớp vai phải phát hiện rách hoàn toàn gân trên gai và gân dưới gai chóp xoay vai phải, tụt đầu gân thoái hóa độ II, hẹp khoang dưới mỏm cùng vai -> Nhập viện Khoa 57 chỉ định phẫu thuật nội soi khâu phục hồi gân chóp xoay vai phải.";
            string hoiChanTime = "14 giờ 00 phút, ngày 29 tháng 09 năm 2026";
            string examSummary = "DHST: Mạch 76 ck/p, HA 120/75 mmHg, T° 36.6°C, SpO2 99%. BN tỉnh, thể trạng tốt. Khám khớp vai phải: Teo nhẹ cơ trên gai và dưới gai vai phải; ấn đau chói tại điểm bám gân củ lớn xương cánh tay phải; dấu hiệu chạm mỏm cùng vai Neer (+), Hawkins-Kennedy (+); nghiệm pháp Jobe (kiểm tra gân trên gai) (+), Patte (kiểm tra gân dưới gai) (+), Gerber (gân dưới vai) (-); biên độ vận động chủ động khớp vai phải hạn chế (dạng 80°, nâng trước 90°, xoay ngoài 30°), biên độ thụ động bình thường; mạch quay bắt rõ, cảm giác bàn tay bình thường. Tim phổi bình thường, sẹo mổ cũ đường trắng giữa dưới rốn lành tốt.";
            string fullCls = "- Công thức máu: WBC 5.8 G/L, HGB 138 g/L, PLT 234 G/L.\n- Đông máu: PT-INR 1.16, APTT 1.06, Fibrinogen 4.08 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 5.4 mmol/L, Creatinin 80 µmol/L, AST 30 U/L, ALT 26 U/mL, Điện giải Na 139, K 4.1, Cl 102 mmol/L.\n- Vi sinh: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số âm tính.\n- CĐHA: MRI khớp vai phải: Hình ảnh rách hoàn toàn gân trên gai và gân dưới gai chóp xoay vai phải, đầu gân co rút ngang mức chỏm xương cánh tay (Patte độ II), thoái hóa mỡ cơ chóp xoay độ I (Goutallier); gai xương mỏm cùng vai hẹp khoang dưới mỏm cùng; tràn dịch khớp vai và bao hoạt dịch dưới mỏm cùng vai lượng vừa; X-quang khớp vai: Gai xương dưới mỏm cùng vai Type II.";
            string bloodGroup = "O Rh(+)";
            string bloodReserve = "0";
            string surgeryMethod = "Phẫu thuật nội soi khớp vai phải tạo hình khoang dưới mỏm cùng vai, mài gai mỏm cùng và khâu phục hồi gân chóp xoay (gân trên gai, dưới gai) bằng chỉ neo sinh học có luồn chỉ (Suture Anchors)";
            string anesthesia = "Gây mê nội khí quản + Gây tê đám rối thần kinh cánh tay (giảm đau chu phẫu)";
            string surgeon = "BS Đặng Hoàng Giang";
            string surgeryTime = "13 giờ 30 phút, ngày 30/09/2026";
            string risks = "Rách tái phát gân chóp xoay sau mổ do chất lượng gân kém hoặc tuân thủ tập phục hồi chức năng không đúng, cứng khớp vai/đông cứng khớp vai (Frozen Shoulder), tổn thương thần kinh trên vai hoặc thần kinh nách, tụ dịch/nhiễm trùng khớp vai, bung neo chỉ xương, biến chứng liên quan tư thế mổ (ngồi bãi biển/nghiêng).";

            // 1. Thay thế thông tin hành chính
            doc.Range.Replace("PHẠM VĂN BỒNG", patientName.ToUpper(), false, false);
            doc.Range.Replace("22/07/1947", dob, false, false);
            doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + gender, false, false);
            doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", address, false, false);
            doc.Range.Replace("15/09/2026 21:25", inTime, false, false);
            doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", diagnosis, false, false);
            doc.Range.Replace("Stent đmv, suy tim, THA", history, false, false);

            string bloodStr = string.Format("Nhóm máu: {0}                 Dự trù máu: {1} (ml)", bloodGroup, bloodReserve);
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
                    para.Range.Replace("<thay>", course, false, false);
                }
                else if (t.StartsWith("Thời gian hội chẩn:"))
                {
                    para.Range.Replace("<thay>", hoiChanTime, false, false);
                }
                else if (t.StartsWith("Tóm tắt tình trạng bệnh:"))
                {
                    para.Range.Replace("<thay>", examSummary, false, false);
                }
                else if (prevT.Contains("Các xét nghiệm, chẩn đoán hình ảnh"))
                {
                    para.Range.Replace("<thay>", fullCls, false, false);
                }
                else if (prevT.Contains("Phương pháp phẫu thuật:"))
                {
                    para.Range.Replace("<thay>", surgeryMethod, false, false);
                }
                else if (prevT.Contains("Phương pháp vô cảm dự kiến:"))
                {
                    para.Range.Replace("<thay>", anesthesia, false, false);
                }
                else if (prevT.Contains("Phẫu thuật viên chính:"))
                {
                    para.Range.Replace("<thay>", surgeon, false, false);
                }
                else if (prevT.Contains("Ngày, giờ phẫu thuật dự kiến:"))
                {
                    para.Range.Replace("<thay>", surgeryTime, false, false);
                }
                else if (prevT.Contains("Các biến chứng, nguy cơ, khó khăn đặc biệt"))
                {
                    para.Range.Replace("<thay>", risks, false, false);
                }
            }

            string outPath1 = Path.Combine(outDirDated, "08_PT01_BienBanThongQuaMo_NGUYỄN_THỊ_NGỌC_0003837595.docx");
            string outPath2 = Path.Combine(outDirStd, "PT08_NGUYỄN_THỊ_NGỌC_0003837595.docx");
            string outPathDesktop = Path.Combine(desktopDir, "PT01_NGUYỄN_THỊ_NGỌC_0003837595.docx");

            doc.Save(outPath1);
            doc.Save(outPath2);
            try { doc.Save(outPathDesktop); } catch { }

            Console.WriteLine("✔ ĐÃ XUẤT THÀNH CÔNG BIÊN BẢN PT-01 CHO BN NGUYỄN THỊ NGỌC:");
            Console.WriteLine("  📂 1. " + outPath1);
            Console.WriteLine("  📂 2. " + outPath2);
            Console.WriteLine("  📂 3. " + outPathDesktop);
        }
    }
}
