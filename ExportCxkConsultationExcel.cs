using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aspose.Cells;

namespace ExportCxkExcel
{
    class PatientItem
    {
        public int Stt { get; set; }
        public string SendTime { get; set; }
        public string RoomBed { get; set; }
        public string TreatmentCode { get; set; }
        public string PatientCode { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string RequestDoctor { get; set; }
        public string Diagnosis { get; set; }
        public string ReasonAndTracking { get; set; }
        public string ExactImaging { get; set; }
        public string OrthoPlan { get; set; }
        public string PlanCategory { get; set; }
        public string WardTransfer { get; set; }
        public string HisStatus { get; set; }
        public bool IsInpatient { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
                string p0 = Path.Combine(folderPath, name);
                if (File.Exists(p0)) return Assembly.LoadFrom(p0);
                string p1 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                if (File.Exists(p1)) return Assembly.LoadFrom(p1);
                string p2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
                if (File.Exists(p2)) return Assembly.LoadFrom(p2);
                return null;
            };

            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "ConsultationReports");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string xlsxPath = Path.Combine(outDir, "BaoCao_HoiChan_CoXuongKhop_20260903.xlsx");
            string csvPath = Path.Combine(outDir, "BaoCao_HoiChan_CoXuongKhop_20260903.csv");

            var list = GetPatientData();

            // 1. Tạo Workbook bằng Aspose.Cells
            Workbook wb = new Workbook();
            
            // Sheet 1: Nội trú CXK (18 BN)
            Worksheet ws1 = wb.Worksheets[0];
            ws1.Name = "NoiTru_CXK_DiBuong";
            FillSheet(wb, ws1, "BÁO CÁO HỘI CHẨN ĐI BUỒNG NỘI TRÚ - VIỆN CƠ XƯƠNG KHỚP GỬI KHOA 57", 
                list.Where(x => x.IsInpatient).ToList(), true);

            // Sheet 2: Ngoại trú CXK (10 BN)
            Worksheet ws2 = wb.Worksheets.Add("NgoaiTru_CXK");
            FillSheet(wb, ws2, "DANH SÁCH YÊU CẦU HỘI CHẨN NGOẠI TRÚ - VIỆN CƠ XƯƠNG KHỚP GỬI KHOA 57", 
                list.Where(x => !x.IsInpatient).ToList(), false);

            // Sheet 3: Toàn bộ 28 BN
            Worksheet ws3 = wb.Worksheets.Add("TongHop_TatCa_28BN");
            FillSheet(wb, ws3, "TỔNG HỢP TOÀN BỘ 28 CA HỘI CHẨN TỪ VIỆN CƠ XƯƠNG KHỚP (03/09/2026)", 
                list, false);

            wb.Save(xlsxPath, SaveFormat.Xlsx);
            Console.WriteLine("✅ Đã tạo file Excel thành công: " + xlsxPath);

            // 2. Xuất thêm file CSV chuẩn UTF-8 BOM để mở tức thì trên Google Sheets / Excel
            ExportCsv(list.Where(x => x.IsInpatient).ToList(), csvPath);
            Console.WriteLine("✅ Đã tạo file CSV thành công: " + csvPath);
        }

        static void FillSheet(Workbook wb, Worksheet ws, string title, List<PatientItem> items, bool highlightUrgent)
        {
            // Title
            ws.Cells.Merge(0, 0, 1, 16);
            Cell cTitle = ws.Cells[0, 0];
            cTitle.PutValue(title);
            Style sTitle = wb.CreateStyle();
            sTitle.Font.IsBold = true;
            sTitle.Font.Size = 14;
            sTitle.Font.Color = Color.White;
            sTitle.ForegroundColor = Color.FromArgb(27, 54, 93); // Navy Blue
            sTitle.Pattern = BackgroundType.Solid;
            sTitle.HorizontalAlignment = TextAlignmentType.Center;
            sTitle.VerticalAlignment = TextAlignmentType.Center;
            cTitle.SetStyle(sTitle);
            ws.Cells.SetRowHeight(0, 32);

            // Subtitle
            ws.Cells.Merge(1, 0, 1, 16);
            Cell cSub = ws.Cells[1, 0];
            cSub.PutValue("Ngày hội chẩn: 03/09/2026 | Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57) - Bệnh viện Bạch Mai | BS phụ trách: Ths.BS Nguyễn Hữu Sâm - BS Hà Đức Cường");
            Style sSub = wb.CreateStyle();
            sSub.Font.IsItalic = true;
            sSub.Font.Size = 10;
            sSub.Font.Color = Color.FromArgb(71, 85, 105);
            sSub.HorizontalAlignment = TextAlignmentType.Center;
            sSub.VerticalAlignment = TextAlignmentType.Center;
            cSub.SetStyle(sSub);
            ws.Cells.SetRowHeight(1, 20);

            // Header row
            string[] headers = new string[]
            {
                "STT", "Giờ Gửi", "Buồng Giường CXK", "Mã ĐT", "Mã BN", "Họ và Tên", "Tuổi", "Giới",
                "Bác Sĩ CXK Yêu Cầu", "Chẩn Đoán Lâm Sàng CXK", "Nội Dung Viện CXK Xin Hội Chẩn",
                "Chẩn Đoán Hình Ảnh Đích Danh (Rule 4)", "Đề Xuất Hướng Xử Trí CTCH & Cột Sống (Khoa 57)",
                "Phân Loại Xử Trí", "Kế Hoạch Tiếp Nhận", "Trạng Thái HIS"
            };

            Style sHeader = wb.CreateStyle();
            sHeader.Font.IsBold = true;
            sHeader.Font.Size = 11;
            sHeader.Font.Color = Color.White;
            sHeader.ForegroundColor = Color.FromArgb(37, 99, 235); // Royal Blue
            sHeader.Pattern = BackgroundType.Solid;
            sHeader.HorizontalAlignment = TextAlignmentType.Center;
            sHeader.VerticalAlignment = TextAlignmentType.Center;
            sHeader.IsTextWrapped = true;
            sHeader.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            sHeader.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Medium;
            sHeader.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            sHeader.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;

            ws.Cells.SetRowHeight(2, 28);
            for (int col = 0; col < headers.Length; col++)
            {
                Cell hc = ws.Cells[2, col];
                hc.PutValue(headers[col]);
                hc.SetStyle(sHeader);
            }

            // Body Styles
            Style sDataNormal = wb.CreateStyle();
            sDataNormal.Font.Size = 10;
            sDataNormal.IsTextWrapped = true;
            sDataNormal.VerticalAlignment = TextAlignmentType.Top;
            sDataNormal.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            sDataNormal.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
            sDataNormal.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            sDataNormal.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;

            Style sDataZebra = wb.CreateStyle();
            sDataZebra.Font.Size = 10;
            sDataZebra.IsTextWrapped = true;
            sDataZebra.VerticalAlignment = TextAlignmentType.Top;
            sDataZebra.ForegroundColor = Color.FromArgb(248, 250, 252);
            sDataZebra.Pattern = BackgroundType.Solid;
            sDataZebra.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            sDataZebra.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
            sDataZebra.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            sDataZebra.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;

            // Fill rows
            int rIdx = 3;
            int stt = 1;
            foreach (var p in items)
            {
                Style baseStyle = (rIdx % 2 == 0) ? sDataZebra : sDataNormal;

                ws.Cells[rIdx, 0].PutValue(stt++);
                ws.Cells[rIdx, 1].PutValue(p.SendTime);
                ws.Cells[rIdx, 2].PutValue(p.RoomBed);
                ws.Cells[rIdx, 3].PutValue(p.TreatmentCode);
                ws.Cells[rIdx, 4].PutValue(p.PatientCode);
                ws.Cells[rIdx, 5].PutValue(p.FullName);
                ws.Cells[rIdx, 6].PutValue(p.Age);
                ws.Cells[rIdx, 7].PutValue(p.Gender);
                ws.Cells[rIdx, 8].PutValue(p.RequestDoctor);
                ws.Cells[rIdx, 9].PutValue(p.Diagnosis);
                ws.Cells[rIdx, 10].PutValue(p.ReasonAndTracking);
                ws.Cells[rIdx, 11].PutValue(p.ExactImaging);
                ws.Cells[rIdx, 12].PutValue(p.OrthoPlan);
                ws.Cells[rIdx, 13].PutValue(p.PlanCategory);
                ws.Cells[rIdx, 14].PutValue(p.WardTransfer);
                ws.Cells[rIdx, 15].PutValue(p.HisStatus);

                for (int c = 0; c < 16; c++)
                {
                    ws.Cells[rIdx, c].SetStyle(baseStyle);
                }

                // Bold FullName
                Style sName = ws.Cells[rIdx, 5].GetStyle();
                sName.Font.IsBold = true;
                sName.Font.Color = Color.FromArgb(30, 64, 175);
                ws.Cells[rIdx, 5].SetStyle(sName);

                // Highlight Plan Category
                Style sPlan = ws.Cells[rIdx, 13].GetStyle();
                sPlan.Font.IsBold = true;
                if (p.PlanCategory.Contains("Bơm xi măng")) sPlan.Font.Color = Color.FromArgb(5, 122, 85);
                else if (p.PlanCategory.Contains("Mổ")) sPlan.Font.Color = Color.FromArgb(224, 36, 36);
                else if (p.PlanCategory.Contains("Sinh thiết")) sPlan.Font.Color = Color.FromArgb(147, 51, 234);
                else sPlan.Font.Color = Color.FromArgb(217, 119, 6);
                ws.Cells[rIdx, 13].SetStyle(sPlan);

                ws.Cells.SetRowHeight(rIdx, 65);
                rIdx++;
            }

            // Set Column Widths
            ws.Cells.SetColumnWidth(0, 6);   // STT
            ws.Cells.SetColumnWidth(1, 10);  // Giờ Gửi
            ws.Cells.SetColumnWidth(2, 22);  // Buồng Giường
            ws.Cells.SetColumnWidth(3, 15);  // Mã ĐT
            ws.Cells.SetColumnWidth(4, 13);  // Mã BN
            ws.Cells.SetColumnWidth(5, 24);  // Họ và Tên
            ws.Cells.SetColumnWidth(6, 6);   // Tuổi
            ws.Cells.SetColumnWidth(7, 7);   // Giới
            ws.Cells.SetColumnWidth(8, 20);  // Bác Sĩ Y/C
            ws.Cells.SetColumnWidth(9, 32);  // Chẩn Đoán
            ws.Cells.SetColumnWidth(10, 40); // Nội Dung Xin HC
            ws.Cells.SetColumnWidth(11, 45); // CĐHA Đích Danh
            ws.Cells.SetColumnWidth(12, 45); // Đề Xuất CTCH
            ws.Cells.SetColumnWidth(13, 24); // Phân Loại
            ws.Cells.SetColumnWidth(14, 20); // Kế Hoạch Nhận
            ws.Cells.SetColumnWidth(15, 14); // Trạng Thái HIS

            // Freeze top 3 rows
            ws.FreezePanes(3, 0, 3, 0);
        }

        static void ExportCsv(List<PatientItem> items, string path)
        {
            StringBuilder sb = new StringBuilder();
            // BOM for UTF-8
            sb.Append('\uFEFF');
            sb.AppendLine("STT,GioGui,BuongGiuong,MaDT,MaBN,HoVaTen,Tuoi,Gioi,BacSiYeuCau,ChanDoan,NoiDungHoiChan,CDHA_DichDanh_Rule4,DeXuat_CTCH_Khoa57,PhanLoaiXuTri,KeHoachTiepNhan,TrangThaiHIS");

            int idx = 1;
            foreach (var p in items)
            {
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",\"{15}\"",
                    idx++,
                    p.SendTime,
                    EscapeCsv(p.RoomBed),
                    p.TreatmentCode,
                    p.PatientCode,
                    EscapeCsv(p.FullName),
                    p.Age,
                    p.Gender,
                    EscapeCsv(p.RequestDoctor),
                    EscapeCsv(p.Diagnosis),
                    EscapeCsv(p.ReasonAndTracking),
                    EscapeCsv(p.ExactImaging),
                    EscapeCsv(p.OrthoPlan),
                    EscapeCsv(p.PlanCategory),
                    EscapeCsv(p.WardTransfer),
                    EscapeCsv(p.HisStatus)
                ));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ");
        }

        static List<PatientItem> GetPatientData()
        {
            var list = new List<PatientItem>();

            // --- NỘI TRÚ (18 BN) ---
            // 1. NGUYỄN THỊ OANH
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "07:04",
                RoomBed = "Phòng 507 - Giường 32",
                TreatmentCode = "000007124259",
                PatientCode = "0003109956",
                FullName = "NGUYỄN THỊ OANH",
                Age = 49,
                Gender = "Nữ",
                RequestDoctor = "BS BÙI THỊ HƯỜNG",
                Diagnosis = "[M25.00] Áp xe bàn chân trái do tụ cầu vàng - TS mổ tháo xương ngón II chân trái",
                ReasonAndTracking = "Mụn nhọt mu chân T sưng đau, đã chích hút ra 1ml dịch mủ. Hiện ổ mủ đã tự vỡ thoát dịch vàng ra gạc, đỡ sưng đau. Xin ý kiến CTCH & PT Tạo hình thẩm mỹ.",
                ExactImaging = "MRI bàn chân: Ổ tụ dịch dưới da mu chân 50 x 7 mm, phù nề viêm mô tế bào nông, xương bàn ngón bình thường, mỏm cụt ngón II cũ ổn định.",
                OrthoPlan = "Điều trị bảo tồn nội khoa tại CXK: Thay băng vô khuẩn hàng ngày, tách mép dẫn lưu mủ, rửa NaCl 0.9% + Betadine pha loãng, duy trì KS chống tụ cầu. Chưa cần mổ lớn.",
                PlanCategory = "🟡 Điều trị bảo tồn",
                WardTransfer = "Theo dõi tại CXK",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 2. NGUYỄN NGỌC HƯNG
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "07:13",
                RoomBed = "Phòng 511 - Giường 53",
                TreatmentCode = "000007141049",
                PatientCode = "0001634901",
                FullName = "NGUYỄN NGỌC HƯNG",
                Age = 64,
                Gender = "Nam",
                RequestDoctor = "BS BÙI THỊ HƯỜNG",
                Diagnosis = "[M54.20] HC cổ vai cánh tay - Thoát vị C3/4, C4/5, C5/6, C6/7 chèn ép nặng & phù tủy cổ C3/4",
                ReasonAndTracking = "Đau cổ vai cánh tay dữ dội 2 bên, tê bì lan tay. Xin ý kiến can thiệp cấp thiết.",
                ExactImaging = "MRI Cột sống cổ: Thoát vị C3/4 thể trung tâm gây HẸP NẶNG ỐNG SỐNG (đường kính trước sau 4mm) + CHÈN ÉP VÀ PHÙ TỦY CỔ NGANG MỨC C3/4; chèn ép rễ C5, C6 (P), C7 (T).",
                OrthoPlan = "BỆNH LÝ TỦY CỔ CẤP THIẾT - NGUY CƠ LIỆT TỨ CHI: Chỉ định Phẫu thuật giải ép tủy và hàn xương liên thân đốt lối trước (ACDF C3/4). Đeo ngay nẹp cổ cứng Philadelphia, chống phù tủy.",
                PlanCategory = "🚨 Mổ cấp thiết tủy cổ",
                WardTransfer = "Chuyển gấp Khoa 57",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 3. NGUYỄN THỊ MINH NGUYỆT
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "07:31",
                RoomBed = "Phòng 507 - Giường 31",
                TreatmentCode = "000007138586",
                PatientCode = "0004003123",
                FullName = "NGUYỄN THỊ MINH NGUYỆT",
                Age = 46,
                Gender = "Nữ",
                RequestDoctor = "BS BÙI THỊ HƯỜNG",
                Diagnosis = "[M05.80] Thoát vị kén khoeo chân phải - Viêm khớp dạng thấp - Suy giáp",
                ReasonAndTracking = "Đau tức nhiều vùng khoeo và cẳng chân phải trên nền VKDT, suy giáp điều trị thường xuyên. Xin ý kiến mổ bóc kén.",
                ExactImaging = "MRI cẳng chân: Thoát vị kén Baker khoeo chân P kích thước lớn lan 1/3 trên cẳng chân, dịch không đồng nhất, phù nề mô kẽ lân cận (theo dõi vỡ kén/bội nhiễm).",
                OrthoPlan = "Ưu tiên điều trị nội khoa kiểm soát đợt cấp VKDT (Medrol, DMARDs) và chỉnh liều suy giáp. Nếu căng tức: chọc hút dịch dưới siêu âm + tiêm corticoid. Chưa có chỉ định mổ mở bóc kén.",
                PlanCategory = "🟡 Nội khoa VKDT",
                WardTransfer = "Điều trị tại CXK",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 4. PHẠM THỊ LƠ
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "08:33",
                RoomBed = "Phòng 510 - Giường 48",
                TreatmentCode = "000007114733",
                PatientCode = "0003992323",
                FullName = "PHẠM THỊ LƠ",
                Age = 80,
                Gender = "Nữ",
                RequestDoctor = "BS TRẦN HUYỀN TRANG",
                Diagnosis = "[L02.4] Áp xe 1/2 dưới mặt ngoài cẳng chân trái do MRSA",
                ReasonAndTracking = "Áp xe cẳng chân - cổ chân trái do tụ cầu vàng MRSA đã rạch hút tại Việt Tiệp nhưng không đỡ, hiện vết mổ rỉ mủ vàng thấm băng. Xin ý kiến CTCH chỉ định phẫu thuật nạo vét.",
                ExactImaging = "MRI cẳng chân: Các ổ áp xe phần mềm sâu trong cơ và giữa các lớp cơ mặt sau cẳng chân, kích thước ổ lớn nhất 125 x 10 mm có đường rò ra da 1/3 dưới; ổ áp xe cổ bàn chân 29x11mm; phù xương sên, gót.",
                OrthoPlan = "CÓ CHỈ ĐỊNH PHẪU THUẬT: Mở rộng, nạo vét, cắt lọc tổ chức hoại tử và tháo mủ ổ áp xe sâu giữa các lớp cơ cẳng chân (12,5 cm) và cổ chân trái. Duy trì Vancomycin tĩnh mạch.",
                PlanCategory = "🔴 Mổ nạo áp xe sâu",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 5. NGUYỄN THỊ ĐỈNH
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:02",
                RoomBed = "Phòng 510 - Giường 50",
                TreatmentCode = "000007135233",
                PatientCode = "0004001185",
                FullName = "NGUYỄN THỊ ĐỈNH",
                Age = 78,
                Gender = "Nữ",
                RequestDoctor = "BS TRẦN HUYỀN TRANG",
                Diagnosis = "[M80.05] Xẹp cấp thân đốt sống D12 - Phình đĩa đệm L3/4, L4/5 lệch trái hẹp ống sống",
                ReasonAndTracking = "Ngã ngồi do TNGT cách 1 tuần, đau dữ dội CSTL (VAS 4-5/10). Lãnh đạo Viện CXK (PGS.TS Nguyễn Văn Hùng) chỉ đạo: HC điện quang hoặc CTCH.",
                ExactImaging = "MRI CSTL: Xẹp mới thân đốt sống D12 (T12) có phù tủy xương STIR rõ rệt; phình đĩa đệm L3/4, L4/5, thoát vị L5/S1 lệch trái gây hẹp ống sống chèn rễ L5 hai bên.",
                OrthoPlan = "Chỉ định Bơm xi măng sinh học thân đốt sống qua da (Vertebroplasty - BXM) đốt D12 (T12) giúp giảm đau tức thì và phòng ngừa gù gập cột sống. Chờ DEXA, làm bilan mổ BXM.",
                PlanCategory = "🟢 Bơm xi măng D12",
                WardTransfer = "Nhận Khoa 57 can thiệp",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 6. HÀ THỊ HƯỜNG
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:14",
                RoomBed = "Phòng 507 - Giường 29",
                TreatmentCode = "000007130951",
                PatientCode = "0003400670",
                FullName = "HÀ THỊ HƯỜNG",
                Age = 68,
                Gender = "Nữ",
                RequestDoctor = "BS NGUYỄN THỊ KIM ANH",
                Diagnosis = "[M48.54] Theo dõi viêm đốt sống đĩa đệm chưa loại trừ lao / Loãng xương nặng xẹp D8, suy thận độ 3",
                ReasonAndTracking = "Đau CSTL và ngực nhiều, hạn chế vận động. Chẩn đoán theo dõi viêm đĩa đệm đốt sống do lao / suy thận 3. Xin hội chẩn Khoa 57 xét sinh thiết đốt sống.",
                ExactImaging = "MRI CSTL: Tổn thương phù tủy xương gây xẹp thân đốt sống D7-D9, nhiều nhất D8 gây gù cột sống, lồi tường sau hẹp ống sống (d=8.1mm), dày thâm nhiễm cạnh sống theo dõi lao; phình L2-S1.",
                OrthoPlan = "CHỈ ĐỊNH SINH THIẾT ĐỐT SỐNG D8 dưới hướng dẫn C-arm/CT để chẩn đoán xác định căn nguyên (Lao vs Vi khuẩn thường). Hội chẩn chuyên khoa Lao & Thận nhân tạo trước can thiệp.",
                PlanCategory = "🟣 Sinh thiết đốt sống D8",
                WardTransfer = "Khoa 57 làm sinh thiết",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 7. NGUYỄN XUÂN CHÍN
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:49",
                RoomBed = "Phòng 508 - Giường 38",
                TreatmentCode = "000007136273",
                PatientCode = "0003944205",
                FullName = "NGUYỄN XUÂN CHÍN",
                Age = 50,
                Gender = "Nam",
                RequestDoctor = "BS TRỊNH HOÀI PHƯƠNG",
                Diagnosis = "[M25.50] Nhiễm khuẩn khớp gối trái sau tiêm hút dịch nhiều lần",
                ReasonAndTracking = "2 tháng gần đây sưng đau khớp gối T nhiều lần, đã tiêm hút dịch nhiều lần (lần gần nhất 11/08), ngày qua bắt đầu gai sốt, khớp gối sưng nóng đỏ đau VAS 4đ. Xin ý kiến điều trị.",
                ExactImaging = "MRI Khớp gối trái: Viêm màng hoạt dịch khớp gối trái, dày màng hoạt dịch, tràn dịch khớp. Phù dây chằng chéo trước. Thoái hóa, rách độ 3 sụn chêm trong, độ 2 sụn chêm ngoài. Phù tủy xương đùi/chày.",
                OrthoPlan = "CHỈ ĐỊNH NỘI SOI KHỚP GỐI BƠM RỬA LÀM SẠCH, CẮT LỌC MÀNG HOẠT DỊCH VIÊM MỦ, dẫn lưu khớp gối. Lấy dịch khớp làm PCR và kháng sinh đồ. Điều trị KS tĩnh mạch theo KS đồ.",
                PlanCategory = "🔴 Mổ nội soi rửa khớp gối",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 8. ĐINH VĂN ĐIỆP
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:53",
                RoomBed = "Phòng 513 - Giường 75",
                TreatmentCode = "000007134908",
                PatientCode = "0003485139",
                FullName = "ĐINH VĂN ĐIỆP",
                Age = 40,
                Gender = "Nam",
                RequestDoctor = "BS NGỌ CÔNG MINH",
                Diagnosis = "[M54.36] Đau CSTL - Nhiễm trùng phần mềm cẳng chân - Trượt L5 độ II, phình L4/5 - Gút mạn, ĐTĐ 2, Suy thượng thận",
                ReasonAndTracking = "Đau lưng nhiều kèm nhiễm trùng phần mềm cẳng chân trên nền gút, đái tháo đường, suy tuyến thượng thận do dùng corticoid. Xin ý kiến CTCH.",
                ExactImaging = "MRI CSTL: Thoái hóa cột sống thắt lưng. Trượt thân đốt sống L5 ra trước độ I/II do hở eo. Giảm chiều cao D12, L3. Rách vòng xơ L4/5, phình L3-S1 chưa chèn ép rễ thần kinh.",
                OrthoPlan = "Ưu tiên điều trị kiểm soát dứt điểm nhiễm trùng phần mềm cẳng chân và điều chỉnh suy thượng thận/ĐTĐ tại CXK. Về cột sống: hiện chưa có chèn ép rễ cấp, cho mang đai nẹp cột sống thắt lưng, chưa mổ cột sống.",
                PlanCategory = "🟡 Điều trị nội khoa",
                WardTransfer = "Điều trị tại CXK",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 9. NÔNG THỊ CHẤN
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:56",
                RoomBed = "Phòng 505 - Giường 8",
                TreatmentCode = "000007075564",
                PatientCode = "0003974123",
                FullName = "NÔNG THỊ CHẤN",
                Age = 50,
                Gender = "Nữ",
                RequestDoctor = "BS NGỌ CÔNG MINH",
                Diagnosis = "[L02.9] Áp xe phần mềm đa ổ: sau đùi trái, thành ngực phải, nách trái, cơ dưới vai trái / ĐTĐ 2, Nhịp nhanh trên thất",
                ReasonAndTracking = "Áp xe đa ổ nhiều nơi tái phát, đã mổ áp xe vai, gối tháng 7-8/2026. Nay sưng đau mạn sườn P, nách T (ổ tụ dịch 100x54mm). Đã hoàn thiện bilan mổ, xin CTCH xét can thiệp.",
                ExactImaging = "MRI lồng ngực & đùi: Ổ áp xe lớn phần mềm thành ngực P (66x21mm), cơ dưới vai T. Siêu âm: Ổ tụ dịch nách T (100x54mm). MRI đùi T: Ổ tụ dịch trong cơ mặt ngoài 1/3 dưới đùi (27x6mm).",
                OrthoPlan = "CÓ CHỈ ĐỊNH PHẪU THUẬT: Mở rộng tháo mủ, nạo vét làm sạch tổ chức hoại tử ổ áp xe lớn nách trái (10cm) và thành ngực phải, đặt dẫn lưu áp lực âm VAC. Phối hợp PT Lồng Ngực và kiểm soát ĐTĐ, tim mạch.",
                PlanCategory = "🔴 Mổ tháo mủ áp xe đa ổ",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 10. NGUYỄN THỊ XUYẾN
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:59",
                RoomBed = "Phòng 510 - Giường 45",
                TreatmentCode = "000007138790",
                PatientCode = "0002353127",
                FullName = "NGUYỄN THỊ XUYẾN",
                Age = 61,
                Gender = "Nữ",
                RequestDoctor = "BS NGUYỄN THỊ HIỀN",
                Diagnosis = "[M54.30] Hẹp lỗ tiếp hợp L3/4 trái có chèn ép rễ - Viêm gan B - Basedow / TS mổ nẹp vít L4-L5 (2009)",
                ReasonAndTracking = "Đau CSTL lan chân trái. Tiền sử mổ nẹp vít L4-L5 năm 2009, Basedow 6 năm bình giáp. Xin ý kiến CTCH điều trị.",
                ExactImaging = "MRI CSTL: Thoái hóa, vẹo cột sống thắt lưng. Hẹp lỗ tiếp hợp L3/4 bên trái có chèn ép rễ L3. Hình ảnh nẹp vít L4-L5 cũ vững chắc.",
                OrthoPlan = "Chỉ định Phong bế rễ thần kinh chọn lọc L3/L4 (Selective Nerve Root Block - SNRB) dưới hướng dẫn C-arm hoặc điều trị giảm đau nội khoa thần kinh (Pregabalin/Gabapentin). Chưa có chỉ định mổ mở lại.",
                PlanCategory = "🟢 Phong bế rễ / Bảo tồn",
                WardTransfer = "Theo dõi tại CXK",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 11. DƯƠNG THỊ LỨU
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "09:59",
                RoomBed = "Phòng 513 - Giường 78",
                TreatmentCode = "000007129882",
                PatientCode = "0003998446",
                FullName = "DƯƠNG THỊ LỨU",
                Age = 89,
                Gender = "Nữ",
                RequestDoctor = "BS NGỌ CÔNG MINH",
                Diagnosis = "[M51.1] Xẹp cấp thân đốt sống L4-L5 - Hẹp nặng ống sống / Loãng xương nặng, Đau thắt ngực",
                ReasonAndTracking = "Cụ bà 89 tuổi, tự ngã tại nhà đau dữ dội CSTL, đau tăng khi thay đổi tư thế. Đau ngực sau xương ức. Xin ý kiến CTCH can thiệp cột sống.",
                ExactImaging = "MRI CSTL: Xẹp cấp thân đốt sống L4, L5 có phù tủy xương STIR, hẹp nặng ống sống ngang mức L4/5; loãng xương sau mãn kinh nặng.",
                OrthoPlan = "ĐÁNH GIÁ CHỈ ĐỊNH BƠM XI MĂNG SINH HỌC L4, L5. Tuy nhiên do BN 89 tuổi có đau thắt ngực không ổn định, bắt buộc kiểm tra men tim (Troponin T), điện tim, hội chẩn Tim mạch trước can thiệp.",
                PlanCategory = "🟢 Bơm xi măng L4, L5 (Cần HC Tim mạch)",
                WardTransfer = "Xem xét nhận Khoa 57",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 12. NGUYỄN DUY SÁU
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:08",
                RoomBed = "Phòng 505 - Giường 12",
                TreatmentCode = "000007140157",
                PatientCode = "0003908119",
                FullName = "NGUYỄN DUY SÁU",
                Age = 64,
                Gender = "Nam",
                RequestDoctor = "BS HOÀNG HUẾ ANH",
                Diagnosis = "[L03.1] Áp xe cẳng chân phải & gối phải - Đợt cấp gút mạn - ĐTĐ 2 - Suy thượng thận - Viêm phổi",
                ReasonAndTracking = "Gút 26 năm, sưng nóng đỏ đau cẳng chân P, chọc hút ra 10ml mủ đục. Khớp gối và cẳng chân phù nhiều. Xin ý kiến CTCH.",
                ExactImaging = "MRI Gối & Cẳng chân P: Các ổ áp xe phần mềm mặt sau cẳng chân, mặt sau khớp gối và trước xương bánh chè. Viêm khớp gối, tổn thương viêm xương đầu dưới xương đùi. Phù nề rách bán phần ACL.",
                OrthoPlan = "CÓ CHỈ ĐỊNH PHẪU THUẬT: Rạch mở rộng tháo mủ, nạo vét tổ chức hoại tử và dẫn lưu mủ ổ áp xe cẳng chân và quanh gối phải. Bơm rửa khớp gối, bảo tồn khớp. Kháng sinh phổ rộng chống tụ cầu/E.coli.",
                PlanCategory = "🔴 Mổ tháo mủ áp xe gối - cẳng chân",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 13. LÊ VĂN HUY
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:17",
                RoomBed = "Phòng 506 - Giường 14",
                TreatmentCode = "000007121494",
                PatientCode = "0003995150",
                FullName = "LÊ VĂN HUY",
                Age = 70,
                Gender = "Nam",
                RequestDoctor = "BS PHẠM KHÁNH MINH",
                Diagnosis = "[A49.0] Nhiễm trùng rò mủ hạt tophi cổ tay trái sau mổ tuyến dưới - Gút mạn - Viêm phổi",
                ReasonAndTracking = "Vỡ chảy dịch tophi cổ tay T, mổ tuyến dưới 2 tuần không đỡ, chảy dịch mủ kèm sốt rét run. Xin ý kiến CTCH.",
                ExactImaging = "MRI Cổ bàn tay: Viêm khớp cổ tay, dày màng hoạt dịch. Ổ tụ dịch quanh khoang gân gấp ngón cái và gân gấp chung. Viêm xương đầu dưới xương quay, trụ và xương cổ tay. Theo dõi hoại tử xương thuyền, nguyệt.",
                OrthoPlan = "CÓ CHỈ ĐỊNH PHẪU THUẬT: Mở rộng nạo vét làm sạch ổ tophi nhiễm trùng hoại tử, làm sạch bao gân gấp cổ bàn tay trái, nạo tổ chức viêm xương tiêu hủy, dẫn lưu mủ và nẹp bột cố định cổ tay.",
                PlanCategory = "🔴 Mổ nạo tophi hoại tử cổ tay",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 14. DƯƠNG VĂN ĐỐC
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:19",
                RoomBed = "Phòng 508 - Giường 33",
                TreatmentCode = "000007071973",
                PatientCode = "0003505307",
                FullName = "DƯƠNG VĂN ĐỐC",
                Age = 41,
                Gender = "Nam",
                RequestDoctor = "BS PHẠM KHÁNH MINH",
                Diagnosis = "[M46.47] Viêm đĩa đệm đốt sống L4-L5 mạn tính tái phát",
                ReasonAndTracking = "Đau CSTL L4-L5 tái phát sau 5 tháng. Đã sinh thiết T4/2026 là viêm mạn tính, Quantiferon (-). Chụp CLVT theo ý kiến HC lần 1. Xin ý kiến mổ thông qua Khoa 57.",
                ExactImaging = "CLVT & MRI CSTL: Tổn thương phá hủy bề mặt thân đốt sống L4, L5 và khe đĩa đệm L4/5, phù nề mô mềm cạnh sống mức độ vừa, không thấy ổ áp xe lớn ngoài màng cứng.",
                OrthoPlan = "Nếu điều trị nội khoa kháng sinh đủ liều mà lâm sàng còn đau nhiều hoặc có mất vững cột sống: Chỉ định Phẫu thuật nạo vét đĩa đệm viêm, giải ép và hàn xương liên thân đốt lối sau (TLIF/PLIF) nẹp vít L4-L5.",
                PlanCategory = "🔴 Mổ hàn xương L4-L5",
                WardTransfer = "Xem xét nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 15. LẠI THỊ KHA
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:25",
                RoomBed = "Phòng 507 - Giường 28",
                TreatmentCode = "000007133397",
                PatientCode = "0001620834",
                FullName = "LẠI THỊ KHA",
                Age = 79,
                Gender = "Nữ",
                RequestDoctor = "BS PHẠM THỊ THÙY TRANG",
                Diagnosis = "[M51.2] Loãng xương nặng - Lún xẹp đa tầng đốt sống - TS mổ TVĐĐ 2017 - Đau lan 2 chân",
                ReasonAndTracking = "Đau CSTL mạn tính tăng dần lan 2 bàn chân kèm tê bì, đi lại đau tăng, chóng mặt tiền đình. Xin ý kiến CTCH điều trị.",
                ExactImaging = "MRI CSTL: Thoái hóa, vẹo CSTL sang phải. Phình và rách vòng xơ đĩa đệm từ L1-2 đến L5-S1. Thoát vị đĩa đệm L5/S1 gây hẹp ống sống, chèn ép bao màng cứng đuôi ngựa.",
                OrthoPlan = "Ưu tiên điều trị bảo tồn nội khoa tích cực (giảm đau thần kinh Gabapentin/Pregabalin, thuốc chống loãng xương, vật lý trị liệu nhẹ nhàng). Chưa có chỉ định mổ mở lại trên BN 79 tuổi thể trạng yếu.",
                PlanCategory = "🟡 Điều trị bảo tồn",
                WardTransfer = "Điều trị tại CXK",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 16. NGUYỄN VĂN DŨNG
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:27",
                RoomBed = "Phòng 508 - Giường 35",
                TreatmentCode = "000007133965",
                PatientCode = "0004000470",
                FullName = "NGUYỄN VĂN DŨNG",
                Age = 21,
                Gender = "Nam",
                RequestDoctor = "BS PHẠM KHÁNH MINH",
                Diagnosis = "[M25.50] Đau cẳng chân trái - TD cốt tủy viêm - Áp xe dưới màng xương chày trái",
                ReasonAndTracking = "Nam 21 tuổi, sưng đau cẳng chân T 5 tháng nay, mặt trước có khối 5cm chắc không di động. MRI tư: Cốt tủy viêm 2/3 trên xương chày, áp xe dưới màng xương 14x15x68mm. Xin HC hướng xử trí.",
                ExactImaging = "MRI Cẳng chân T: Viêm xương tủy xương chày trái đoạn 2/3 trên, ổ áp xe dưới màng xương ở mặt trong đoạn 1/3 giữa xương chày trái kích thước 14 x 15 x 68 mm.",
                OrthoPlan = "BỆNH LÝ NGOẠI KHOA TUYỆT ĐỐI: CÓ CHỈ ĐỊNH PHẪU THUẬT MỞ CỬA SỔ XƯƠNG NẠO VÉT Ổ CỐT TỦY VIÊM, tháo mủ áp xe dưới màng xương chày trái, đục xương chết, đặt dẫn lưu tưới rửa liên tục, làm KS đồ.",
                PlanCategory = "🔴 Mổ nạo cốt tủy viêm xương chày",
                WardTransfer = "Chuyển Khoa 57 mổ sớm",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 17. ĐOÀN TRỌNG HÀ
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "10:46",
                RoomBed = "Phòng 508 - Giường 38",
                TreatmentCode = "000007112655",
                PatientCode = "0003991552",
                FullName = "ĐOÀN TRỌNG HÀ",
                Age = 63,
                Gender = "Nam",
                RequestDoctor = "BS PHÙNG VĂN ANH ĐỨC",
                Diagnosis = "[M00.00] Áp xe ngón 3 tay phải do MRSA sau chấn thương chuôi dao đập - Tăng huyết áp",
                ReasonAndTracking = "Vết thương ngón 3 do chuôi dao đập 15 ngày trước, nay sưng tím tê bì ngón 3 lan mu tay, sốt rét run. Cầu khuẩn Gram (+) 2(+), dịch viêm mủ. Xin ý kiến CTCH điều trị.",
                ExactImaging = "MRI Bàn tay P: Phù tủy xương chỏm xương đốt bàn ngón I, II, III. Tụ dịch trong bao gân gấp ngón sâu đốt ngón III. Viêm gân duỗi ngón II, III. Dày da, tụ dịch mủ dưới da đốt ngón III và mu tay.",
                OrthoPlan = "CÓ CHỈ ĐỊNH PHẪU THUẬT CẤP: Rạch mở rộng bao gân gấp ngón 3 tháo mủ và cắt lọc tổ chức hoại tử, dẫn lưu mủ mu bàn tay phải để cứu gân gấp ngón tay, chống hoại tử đứt gân và cứng khớp.",
                PlanCategory = "🔴 Mổ dẫn lưu áp xe bao gân gấp",
                WardTransfer = "Nhận Khoa 57 mổ",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // 18. NGUYỄN VĂN MINH
            list.Add(new PatientItem
            {
                IsInpatient = true,
                SendTime = "11:09",
                RoomBed = "Phòng 506 - Giường 21",
                TreatmentCode = "000007137214",
                PatientCode = "0004002368",
                FullName = "NGUYỄN VĂN MINH",
                Age = 59,
                Gender = "Nam",
                RequestDoctor = "BS NGUYỄN ĐỨC PHONG",
                Diagnosis = "[M51.2] Áp xe cơ thắt lưng chậu (Psoas) & cơ hình lê 2 bên - NK huyết do E.coli - Thoát vị L4/5, L5/S1",
                ReasonAndTracking = "Sốt 39 độ C, đau CSTL dữ dội lan 2 đùi, nhiễm khuẩn huyết E.coli, bí tiểu đặt sonde. Đang truyền Ciprobay + Vancomycin. Xin ý kiến CTCH điều trị áp xe cơ thắt lưng.",
                ExactImaging = "MRI CSTL & Khung chậu: Các ổ áp xe lớn cơ thắt lưng chậu (Psoas) và cơ hình lê hai bên, thâm nhiễm phù nề lan tỏa khoang sau phúc mạc. Thoát vị đĩa đệm L4/5 chèn ép rễ L5.",
                OrthoPlan = "BỆNH NẶNG NGUY CƠ NHIỄM TRÙNG HUYẾT NẶNG: Chỉ định Chọc hút dẫn lưu ổ áp xe cơ thắt lưng chậu dưới hướng dẫn CT/Siêu âm; duy trì kháng sinh tĩnh mạch liều cao theo KS đồ; kiểm soát đường huyết chặt chẽ.",
                PlanCategory = "🔴 Dẫn lưu áp xe cơ Psoas dưới CT/SA",
                WardTransfer = "Phối hợp CĐHA can thiệp",
                HisStatus = "🔴 Chờ khám/HC"
            });

            // --- NGOẠI TRÚ (10 BN) ---
            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "06:23",
                RoomBed = "PK Cơ Xương Khớp 1 (P.407 K1)",
                TreatmentCode = "000007133180",
                PatientCode = "0004000091",
                FullName = "LÊ VĂN NGỪNG",
                Age = 55,
                Gender = "Nam",
                RequestDoctor = "BS TRẦN THỊ THU HUYỀN",
                Diagnosis = "[M17.0] Thoái hóa khớp gối nguyên phát hai bên",
                ReasonAndTracking = "Khám ngoại trú chuyển khám chuyên khoa khớp CTCH",
                ExactImaging = "X-quang khớp gối: Hẹp khe khớp, gai xương rìa khớp",
                OrthoPlan = "Khám tư vấn tiêm nội khớp / nội soi khớp tại PK 424 Nhà K1",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🟡 Đang khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "06:40",
                RoomBed = "PK Cơ Xương Khớp (P.427 K1)",
                TreatmentCode = "000007134973",
                PatientCode = "0004001055",
                FullName = "VÕ QUÝ BÁ",
                Age = 62,
                Gender = "Nam",
                RequestDoctor = "BS PHÙNG VĂN ANH ĐỨC",
                Diagnosis = "[M54.5] Đau thắt lưng - Thoát vị đĩa đệm cột sống thắt lưng",
                ReasonAndTracking = "Chuyển khám chuyên khoa cột sống",
                ExactImaging = "MRI CSTL: Thoát vị đĩa đệm L4/5 thể trung tâm lệch trái",
                OrthoPlan = "Khám và kê đơn điều trị ngoại trú tại PK 425 Nhà K1",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 425 Nhà K1",
                HisStatus = "🟡 Đang khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "07:33",
                RoomBed = "PK Cơ Xương Khớp 1 (P.407 K1)",
                TreatmentCode = "000007144694",
                PatientCode = "0004005882",
                FullName = "PHẠM THỊ OANH",
                Age = 58,
                Gender = "Nữ",
                RequestDoctor = "BS TRẦN THỊ THU HUYỀN",
                Diagnosis = "[M17.1] Thoái hóa khớp gối sau chấn thương",
                ReasonAndTracking = "Chuyển khám chuyên khoa khớp",
                ExactImaging = "X-quang khớp gối",
                OrthoPlan = "Tư vấn điều trị ngoại trú",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🟡 Đang khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "08:34",
                RoomBed = "PK Cơ Xương Khớp 2 (P.408 K1)",
                TreatmentCode = "000007142154",
                PatientCode = "0004004739",
                FullName = "TRỊNH KẾ THÔNG",
                Age = 67,
                Gender = "Nam",
                RequestDoctor = "BS NGUYỄN ĐỨC PHONG",
                Diagnosis = "[M16.0] Thoái hóa khớp háng nguyên phát",
                ReasonAndTracking = "Chuyển khám chuyên khoa khớp xét chỉ định thay khớp",
                ExactImaging = "X-quang khung chậu: Thoái hóa khớp háng độ III",
                OrthoPlan = "Tư vấn kế hoạch phẫu thuật thay khớp háng nhân tạo",
                PlanCategory = "🟡 Tư vấn thay khớp",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🟡 Đang khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "09:10",
                RoomBed = "PK Cơ xương khớp (P410 K1)",
                TreatmentCode = "000007145556",
                PatientCode = "0004006277",
                FullName = "NGUYỄN THỊ XUÂN",
                Age = 54,
                Gender = "Nữ",
                RequestDoctor = "BS NGUYỄN THỊ BẢO THOA",
                Diagnosis = "[M75.1] Hội chứng chèn ép khoang dưới mỏm cùng vai - Rách gân trên gai",
                ReasonAndTracking = "Chuyển khám chuyên khoa khớp CTCH",
                ExactImaging = "MRI khớp vai: Rách bán phần gân trên gai vai phải",
                OrthoPlan = "Tư vấn nội soi khâu phục hồi gân chóp xoay khớp vai",
                PlanCategory = "🟡 Tư vấn nội soi vai",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🟡 Đang khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "09:18",
                RoomBed = "PK Cơ Xương Khớp (P.427 K1)",
                TreatmentCode = "000007139884",
                PatientCode = "0004003712",
                FullName = "PHẠM VĂN SẤN",
                Age = 60,
                Gender = "Nam",
                RequestDoctor = "BS PHÙNG VĂN ANH ĐỨC",
                Diagnosis = "[M54.4] Đau dây thần kinh tọa do thoát vị đĩa đệm",
                ReasonAndTracking = "Chuyển khám chuyên khoa cột sống",
                ExactImaging = "MRI CSTL: Thoát vị đĩa đệm L5/S1 chèn rễ S1",
                OrthoPlan = "Tư vấn điều trị nội khoa hoặc phẫu thuật ít xâm lấn",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 425 Nhà K1",
                HisStatus = "🔴 Chờ khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "09:56",
                RoomBed = "PK Cơ Xương Khớp (P.428 K1)",
                TreatmentCode = "000007144350",
                PatientCode = "0004005721",
                FullName = "NINH THỊ NHIÊN",
                Age = 51,
                Gender = "Nữ",
                RequestDoctor = "BS NGUYỄN THỊ NGA",
                Diagnosis = "[M77.1] Viêm lồi cầu ngoài xương cánh tay (Tennis elbow)",
                ReasonAndTracking = "Chuyển khám chuyên khoa khớp",
                ExactImaging = "Siêu âm khuỷu tay",
                OrthoPlan = "Tư vấn tiêm huyết tương giàu tiểu cầu (PRP) / tiêm nội khớp",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🔴 Chờ khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "10:29",
                RoomBed = "PK Cơ Xương Khớp (P.427 K1)",
                TreatmentCode = "000007143449",
                PatientCode = "0004005330",
                FullName = "ĐỖ THỊ HUYỀN",
                Age = 47,
                Gender = "Nữ",
                RequestDoctor = "BS PHÙNG VĂN ANH ĐỨC",
                Diagnosis = "[M54.2] Đau cột sống cổ - Thoái hóa đĩa đệm cột sống cổ",
                ReasonAndTracking = "Chuyển khám chuyên khoa cột sống",
                ExactImaging = "MRI CS Cổ: Phình đĩa đệm C4/5, C5/6",
                OrthoPlan = "Khám và điều trị bảo tồn ngoại trú tại PK 425",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 425 Nhà K1",
                HisStatus = "🔴 Chờ khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "11:03",
                RoomBed = "PK Cơ Xương Khớp (P.427 K1)",
                TreatmentCode = "000007143655",
                PatientCode = "0004005421",
                FullName = "NGUYỄN THỊ CHÉN",
                Age = 65,
                Gender = "Nữ",
                RequestDoctor = "BS PHÙNG VĂN ANH ĐỨC",
                Diagnosis = "[M17.0] Thoái hóa khớp gối - Tràn dịch khớp gối",
                ReasonAndTracking = "Chuyển khám chuyên khoa khớp",
                ExactImaging = "X-quang khớp gối",
                OrthoPlan = "Tư vấn hút dịch, tiêm khớp tại PK 424 Nhà K1",
                PlanCategory = "🟡 Khám ngoại trú",
                WardTransfer = "PK 424 Nhà K1",
                HisStatus = "🔴 Chờ khám"
            });

            list.Add(new PatientItem
            {
                IsInpatient = false,
                SendTime = "11:07",
                RoomBed = "PK Cơ xương khớp (P410 K1)",
                TreatmentCode = "000007144839",
                PatientCode = "0004005953",
                FullName = "TRẦN THỊ NHUẦN",
                Age = 58,
                Gender = "Nữ",
                RequestDoctor = "BS NGUYỄN THỊ BẢO THOA",
                Diagnosis = "[M65.3] Ngón tay lò xo (Trigger finger) ngón 1 tay phải",
                ReasonAndTracking = "Chuyển khám chuyên khoa chấn thương chỉnh hình",
                ExactImaging = "Lâm sàng: Ngón tay lò xo kẹt độ III",
                OrthoPlan = "Chỉ định phẫu thuật giải phóng ròng rọc A1 ngón tay tại Phòng tiểu phẫu Nhà Q",
                PlanCategory = "🟢 Tiểu phẫu ngón tay lò xo",
                WardTransfer = "Phòng tiểu phẫu Nhà Q",
                HisStatus = "🔴 Chờ khám"
            });

            return list;
        }
    }
}
