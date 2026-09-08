using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Generate4PT01
{
    class PatientDocData
    {
        public string FileName { get; set; }
        public string HoTen { get; set; }
        public string NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string DiaChi { get; set; }
        public string VaoVien { get; set; }
        public string ChanDoan { get; set; }
        public string TienSu { get; set; }
        public string BenhSu { get; set; }
        public string ThoiGianHoiChan { get; set; }
        public string TomTat { get; set; }
        public string Cls { get; set; }
        public string Pppt { get; set; }
        public string VoCam { get; set; }
        public string PhauThuatVien { get; set; }
        public string NgayMo { get; set; }
        public string LoaiPhauThuat { get; set; } // "Đặc biệt", "Loại I", "Loại II", "Loại III"
        public string Asa { get; set; } // "I", "II", "III", "IV"
        public string Mallampati { get; set; } // "I", "II", "III", "IV"
        public string NguyCo { get; set; } // "Sạch", "Sạch nhiễm", "Nhiễm", "Bẩn"
        public string BienChung { get; set; }
        public string BienPhap { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string templateDocx = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PT-01.docx");
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PT-01");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            List<PatientDocData> patients = new List<PatientDocData>();

            // BN 1: BÙI THỊ NGỌC
            patients.Add(new PatientDocData
            {
                FileName = "1. BUI THI NGOC - PT-01.docx",
                HoTen = "BÙI THỊ NGỌC",
                NgaySinh = "08/01/1969",
                GioiTinh = "Nữ",
                DiaChi = "Phường Tây Hoa Lư, Thành phố Ninh Bình, Tỉnh Ninh Bình",
                VaoVien = "07/09/2026 05:00",
                ChanDoan = "U nang vùng khuỷu tay phải - Tổn thương chèn ép dây thần kinh trụ đoạn khuỷu [G56.2]",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân xuất hiện teo nhẹ cơ ô mô cái và gian cốt bàn tay phải, tê buốt nhiều ngón 4, 5 và nửa ngoài bàn tay phải kéo dài hơn 1 tháng nay, triệu chứng tăng dần khi gập khuỷu hoặc tỳ đè vùng khuỷu, điều trị nội khoa không thuyên giảm. Bệnh nhân đến khám tại Bệnh viện Bạch Mai, được siêu âm và đo điện cơ chẩn đoán U nang vùng khuỷu chèn ép thần kinh trụ, có chỉ định nhập viện phẫu thuật.",
                ThoiGianHoiChan = "08/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), da niêm mạc hồng, không phù, không sốt, thể trạng trung bình. Huyết động ổn định (Mạch 76 ck/p, HA 120/80 mmHg, SpO2 99%). Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Tại chỗ: Tê buốt và giảm cảm giác nông ngón 4, 5 bàn tay phải, teo nhẹ cơ ô mô cái và nhóm cơ gian cốt bàn tay phải (VAS 3 điểm). Khám vùng khuỷu tay phải: Rãnh thần kinh trụ có khối mềm ranh giới rõ kích thước ~10x5mm, ấn tức nhẹ, dấu hiệu Tinel khuỷu tay dương tính (gõ rãnh ròng rọc khuỷu gây tê lan xuống ngón 4, 5). Mạch quay và mạch trụ bắt rõ, đầu chi hồng ấm, vận động gấp duỗi cổ tay và ngón 1, 2, 3 bình thường.",
                Cls = "Siêu âm phần mềm vùng khuỷu tay phải: Hình ảnh u nang vùng khuỷu kích thước 10x5mm gây chèn ép cơ học vào rãnh thần kinh trụ. Điện cơ (EMG): Tổn thương dây thần kinh trụ đoạn khuỷu và dưới khuỷu tay phải. X-quang khớp khuỷu phải thẳng nghiêng: Không thấy tổn thương xương khớp khuỷu. X-quang ngực thẳng: Tim phổi bình thường. Bilan XN máu: WBC 5.2 G/L, Neut% 51.9%, HGB 136 g/L, RBC 4.50 T/L, HCT 0.421 L/L, PLT 277 G/L; PT% 133%, INR 0.85, APTT 26.5s, Fibrinogen 3.88 g/L; Glucose 5.4 mmol/L, Ure 5.7 mmol/L, Creatinin 51 µmol/L, AST 24 U/L, ALT 30 U/L; Điện giải đồ: Na 139, K 3.7, Cl 102 mmol/L; Nhóm máu: B Rh(+); HIV âm tính, HBsAg âm tính, HCV âm tính; Tổng phân tích nước tiểu bình thường.",
                Pppt = "Phẫu thuật mở bóc u nang vùng khuỷu, giải ép và chuyển giường dây thần kinh trụ ra trước (Chuyển giường TK)",
                VoCam = "Gây tê đám rối thần kinh cánh tay (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS Đặng Nhật Quang",
                NgayMo = "ngày 09 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại II",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tụ máu vùng khuỷu, nhiễm trùng vết mổ, tổn thương dây thần kinh trụ trong phẫu thuật (tê bì kéo dài, liệt vận động bàn tay), dính thần kinh sau mổ, tái phát u nang, hội chứng đau vùng phức hợp (CRPS), dị ứng thuốc gây tê/mê, tử vong trên bàn mổ.",
                BienPhap = "Kháng sinh dự phòng trước rạch da 30 phút; Sử dụng kính lúp vi phẫu bộc lộ và phẫu tích tỉ mỉ bảo tồn tối đa bao bó thần kinh trụ và các nhánh vận động; Tạo giường thần kinh mới vững chắc ở mặt trước nhóm cơ gấp; Cố định nẹp bột cẳng bàn tay gập nhẹ khuỷu 2-3 tuần sau mổ; Dịch dinh dưỡng trước mổ Leanpro PreSur 12.5% theo phác đồ ERAS."
            });

            // BN 2: TÔ XUÂN HÒA
            patients.Add(new PatientDocData
            {
                FileName = "2. TO XUAN HOA - PT-01.docx",
                HoTen = "TÔ XUÂN HÒA",
                NgaySinh = "24/09/1961",
                GioiTinh = "Nam",
                DiaChi = "Xã Thường Tín, Thành phố Hà Nội",
                VaoVien = "07/09/2026 09:30",
                ChanDoan = "Hoại tử vô mạch chỏm xương đùi phải giai đoạn muộn (Ficat IV) / Tăng huyết áp vô căn [M87.05, I10]",
                TienSu = "Tăng huyết áp vô căn điều trị thường xuyên; Không có tiền sử đái tháo đường hay bệnh lý tim mạch thiếu máu cục bộ; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân đau nhức vùng khớp háng phải âm ỉ tăng dần nhiều tháng nay, đau tăng nhiều khi đi lại, đứng lâu hoặc tỳ đè trọng lực, giảm khi nghỉ ngơi, đi khập khiễng, tầm vận động khớp háng phải hạn chế nhiều, điều trị giảm đau nội khoa không đỡ. Bệnh nhân vào Bệnh viện Bạch Mai khám chuyên khoa Chấn thương Chỉnh hình, có chỉ định phẫu thuật thay khớp háng.",
                ThoiGianHoiChan = "08/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), da niêm mạc hồng, không sốt, thể trạng trung bình. Huyết áp kiểm soát ổn định 130/80 mmHg, Mạch 78 ck/p. Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Tại chỗ: Ấn đau chói vùng tam giác Scarpa và mấu chuyển lớn bên phải (VAS 4-5 điểm), hạn chế biên độ vận động khớp háng phải (gấp < 90 độ, xoay trong và dạng hạn chế rõ), nghiệm pháp Patrick (P) dương tính, dáng đi khập khiễng do đau. Cơ lực chi dưới 5/5, cảm giác đầu chi bình thường, mạch mu chân và mạch chày sau 2 bên bắt rõ hồng ấm.",
                Cls = "X-quang khớp háng và khung chậu thẳng - nghiêng: Hình ảnh hoại tử chỏm xương đùi phải diện rộng, tiêu xương dưới sụn, dẹt và biến dạng chỏm xương đùi, hẹp khe khớp háng phải (giai đoạn Ficat IV). Siêu âm khớp háng: Không thấy ổ tụ dịch khu trú ở cơ chậu. X-quang ngực thẳng: Tim phổi bình thường. Bilan XN máu: WBC 6.35 G/L, Neut% 63.8%, HGB 116 g/L, RBC 3.98 T/L, HCT 0.366 L/L, PLT 492 G/L; PT% 111%, INR 0.94, APTT 28.5s, Fibrinogen 2.83 g/L; Glucose 5.8 mmol/L, Ure 5.0 mmol/L, Creatinin 60 µmol/L, AST 25 U/L, ALT 17 U/L, CRP hs 0.5 mg/L, máu lắng ổn định; Điện giải: Na 138, K 3.1 mmol/L (đang bù Kali), Cl 104 mmol/L; Nhóm máu O Rh(+); HIV âm tính, HBsAg âm tính, HCV âm tính; Tổng phân tích nước tiểu bình thường.",
                Pppt = "Phẫu thuật thay khớp háng toàn phần bên phải (Thay khớp háng phải toàn phần)",
                VoCam = "Gây tê tủy sống (hoặc Gây tê ngoài màng cứng / Gây mê nội khí quản)",
                PhauThuatVien = "BS Lê Văn Luân",
                NgayMo = "ngày 09 tháng 09 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Loại I",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu trong và sau mổ, tụ máu vết mổ, nhiễm trùng nông/sâu khớp háng nhân tạo, trật khớp háng nhân tạo chu phẫu, tổn thương thần kinh ngồi hoặc thần kinh đùi, vỡ/nứt xương quanh chuôi khớp nhân tạo, thuyên tắc huyết khối tĩnh mạch sâu chi dưới và thuyên tắc phổi (DVT/PE), lỏng khớp nhân tạo, phản vệ dị ứng, tử vong trên bàn mổ.",
                BienPhap = "Bù Kali máu trước mổ (đích K+ >= 3.5 mmol/L); Kiểm soát huyết áp chu phẫu; Kháng sinh dự phòng đường tĩnh mạch trước rạch da 30 phút; Dự trù 02 đơn vị khối hồng cầu cùng nhóm; Chuẩn bị bộ khớp háng nhân tạo toàn phần tương thích kích thước và hệ thống C-arm; Dự phòng thuyên tắc huyết khối bằng thuốc chống đông (Enoxaparin) và vớ áp lực sau mổ; Dịch dinh dưỡng Leanpro PreSur 12.5% theo phác đồ ERAS."
            });

            // BN 3: NGUYỄN VĂN ĐỨC
            patients.Add(new PatientDocData
            {
                FileName = "3. NGUYEN VAN DUC - PT-01.docx",
                HoTen = "NGUYỄN VĂN ĐỨC",
                NgaySinh = "04/10/1998",
                GioiTinh = "Nam",
                DiaChi = "Xã Sóc Sơn, Thành phố Hà Nội",
                VaoVien = "08/09/2026 10:04",
                ChanDoan = "Còn nẹp vít xương đòn phải sau phẫu thuật kết hợp xương 3 năm (can xương liền tốt) [Z96.6]",
                TienSu = "Tiền sử phẫu thuật kết hợp xương đòn phải bằng nẹp vít cách đây 3 năm; Khỏe mạnh, không có bệnh lý nội khoa mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân nam 28 tuổi có tiền sử gãy 1/3 giữa xương đòn phải đã được phẫu thuật KHX bằng nẹp vít 3 năm trước. Hiện tại xương đòn liền tốt, hết đau, vận động khớp vai tốt, bệnh nhân vào Bệnh viện Bạch Mai xin phẫu thuật tháo bỏ dụng cụ kết hợp xương.",
                ThoiGianHoiChan = "08/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), thể trạng tốt (cao 172cm, nặng 66kg), da niêm mạc hồng, không phù, không sốt. Huyết động ổn định (Mạch 72 ck/p, HA 120/66 mmHg, SpO2 99%). Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Tại chỗ: Vùng xương đòn phải có sẹo mổ cũ dài khoảng 8cm liền phẳng đẹp, không sưng đỏ, ấn không đau tức. Tầm vận động khớp vai phải bình thường. Mạch quay bắt rõ, đầu chi hồng ấm, vận động và cảm giác bàn tay phải bình thường.",
                Cls = "X-quang khớp vai và xương đòn phải: Hình ảnh còn nẹp và các vít kim loại xương đòn phải, can xương đã liền đặc hoàn toàn, trục xương thẳng vững chắc. X-quang ngực thẳng: Tim phổi bình thường, các cung sườn không tổn thương. Bilan XN máu: WBC 7.2 G/L, Neut% 63.1%, HGB 154 g/L, RBC 5.30 T/L, HCT 0.455 L/L, PLT 316 G/L; PT% 104%, INR 0.97, APTT 28.4s, Fibrinogen 2.30 g/L; Glucose 5.1 mmol/L, Ure 6.0 mmol/L, Creatinin 81 µmol/L, AST 30 U/L, ALT 26 U/L; Điện giải đồ: Na 138, K 3.6, Cl 106 mmol/L; Nhóm máu B Rh(+); HIV âm tính, HBsAg âm tính, HCV âm tính; Tổng phân tích nước tiểu bình thường.",
                Pppt = "Phẫu thuật tháo bỏ nẹp vít xương đòn phải (Tháo nẹp)",
                VoCam = "Gây tê đám rối thần kinh cánh tay / Gây tê thần kinh trên đòn (hoặc Tiền mê giảm đau / Gây mê nội khí quản)",
                PhauThuatVien = "BS Đỗ Đức Bình",
                NgayMo = "ngày 09 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại II",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tụ máu vết mổ, nhiễm trùng vết mổ, gãy vít / kẹt vít trong xương, tổn thương bó mạch thần kinh dưới đòn, tràn khí màng phổi do kim tê hoặc bộc lộ sâu, gãy lại xương đòn sau tháo nẹp nếu chịu lực mạnh sớm, dị ứng thuốc, tử vong trên bàn mổ.",
                BienPhap = "Kháng sinh dự phòng trước rạch da 30 phút; Chuẩn bị bộ dụng cụ tháo nẹp vít chuyên dụng đầy đủ các đầu tuốc-nơ-vít và màn huỳnh quang tăng sáng C-arm; Hướng dẫn đeo đai số 8 hoặc treo tay cố định nhẹ nhàng sau mổ 1-2 tuần, tránh vận động mang vác nặng trong 4-6 tuần tránh nguy cơ gãy xương lại; Dịch dinh dưỡng Leanpro PreSur 12.5% theo phác đồ ERAS."
            });

            // BN 4: SÙNG Y NÔNG
            patients.Add(new PatientDocData
            {
                FileName = "4. SUNG Y NONG - PT-01.docx",
                HoTen = "SÙNG Y NÔNG",
                NgaySinh = "01/04/2006",
                GioiTinh = "Nữ",
                DiaChi = "Xã Pà Cò, Phú Thọ",
                VaoVien = "08/09/2026 08:58",
                ChanDoan = "Đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải do vết thương dao cắt [M24.84]",
                TienSu = "Khỏe mạnh, không có bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị vết thương mặt gan bàn tay phải cách đây 1.5 tháng do dao cắt trúng. Được xử trí khâu vết thương phần mềm tại y tế cơ sở, sau cắt chỉ sẹo liền tốt nhưng các ngón 3, 4, 5 không gập chủ động được, cầm nắm đồ vật khó khăn. Bệnh nhân đến Bệnh viện Bạch Mai khám, siêu âm chẩn đoán đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải và được chỉ định nhập viện phẫu thuật tạo hình gân gấp.",
                ThoiGianHoiChan = "08/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), thể trạng trung bình (cao 162cm, nặng 48kg), da niêm mạc hồng, không phù, không sốt. Huyết động ổn định (Mạch 78 ck/p, HA 130/70 mmHg, SpO2 99%). Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Tại chỗ: Mặt gan bàn tay ngón 3, 4, 5 ngang vị trí khớp liên đốt gần có vết sẹo cũ kích thước nhỏ đã liền sẹo tốt. Mất vận động gấp chủ động khớp liên đốt xa ngón 3, 4, 5 bàn tay phải (ngón tay ở tư thế duỗi), hạn chế chức năng cầm nắm. Đầu ngón hồng ấm, mạch quay và mạch trụ bắt rõ, cảm giác nông sâu các đầu ngón còn tốt, không tổn thương thần kinh mạch máu ngón.",
                Cls = "Siêu âm phần mềm bàn ngón tay phải: Hình ảnh mất liên tục bó gân gấp sâu ngón 3, 4, 5 bàn tay phải ngang mức khớp liên đốt gần, hai đầu gân co rút nhẹ (đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải). X-quang bàn tay phải thẳng nghiêng: Không thấy gãy xương hay dị vật cản quang. X-quang ngực thẳng: Tim phổi bình thường. Bilan XN máu: WBC 6.38 G/L, Neut% 60.2%, HGB 149 g/L, RBC 5.26 T/L, HCT 0.442 L/L, PLT 223 G/L; PT% 92%, INR 1.05, APTT 31.7s, Fibrinogen 2.35 g/L; Glucose 4.9 mmol/L, Ure 3.8 mmol/L, Creatinin 76 µmol/L, AST 38 U/L, ALT 53 U/L; Điện giải: Na 139, K 4.1, Cl 104 mmol/L; Nhóm máu A Rh(+); HIV âm tính, HBsAg âm tính, HCV âm tính; Tổng phân tích nước tiểu bình thường.",
                Pppt = "Phẫu thuật mở bộc lộ, giải phóng đầu gân và khâu nối tạo hình gân gấp sâu ngón 3, 4, 5 bàn tay phải (Tạo hình gân gấp)",
                VoCam = "Gây tê đám rối thần kinh cánh tay / Gây tê chọn lọc thần kinh tại cổ tay (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS Đặng Nhật Quang",
                NgayMo = "ngày 09 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại II",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tụ máu vùng mổ, nhiễm trùng vết mổ, tổn thương bó mạch thần kinh ngón tay (tê bì ngón, hoại tử vạt da/đầu ngón), đứt lại mối nối gân sau mổ, dính gân gấp gây cứng khớp hạn chế biên độ vận động ngón tay, hội chứng đau vùng phức hợp (CRPS), dị ứng thuốc, tử vong trên bàn mổ.",
                BienPhap = "Kháng sinh dự phòng trước rạch da 30 phút; Sử dụng garo hơi áp lực kiểm soát và kính lúp vi phẫu; Sử dụng chỉ khâu gân chuyên dụng không tiêu sợi bện (Ethibond / Prolene 3-0, 4-0) theo kỹ thuật Kessler cải tiến kết hợp khâu viền biểu mô gân; Bất động nẹp bột cẳng bàn tay tư thế cổ tay gấp nhẹ, khớp bàn ngón gấp 60-70 độ, khớp liên đốt duỗi (nẹp bột bảo vệ gân gấp); Hướng dẫn bệnh nhân tập phục hồi chức năng vận động thụ động sớm theo phác đồ Kleinert để chống dính gân; Dịch dinh dưỡng Leanpro PreSur 12.5% theo phác đồ ERAS."
            });

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            byte[] templateBytes;
            using (var fs = new FileStream(templateDocx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                templateBytes = ms.ToArray();
            }

            foreach (var pData in patients)
            {
                string targetFilePath = Path.Combine(outputDir, pData.FileName);
                File.WriteAllBytes(targetFilePath, templateBytes);

                using (ZipArchive archive = ZipFile.Open(targetFilePath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry docEntry = archive.GetEntry("word/document.xml");
                    string docXmlText = "";
                    using (var reader = new StreamReader(docEntry.Open(), Encoding.UTF8))
                    {
                        docXmlText = reader.ReadToEnd();
                    }

                    XDocument doc = XDocument.Parse(docXmlText);

                    // 1. Duyệt và thay thế nội dung các đoạn văn bản (paragraphs)
                    foreach (var p in doc.Descendants(w + "p"))
                    {
                        string pText = "";
                        foreach (var t in p.Descendants(w + "t"))
                        {
                            pText += t.Value;
                        }

                        if (pText.Contains("Họ và tên người bệnh"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Họ và tên người bệnh: " + pData.HoTen)
                            ));
                        }
                        else if (pText.Contains("Ngày sinh:") && pText.Contains("Giới tính:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", string.Format("Ngày sinh: {0}   Giới tính:  {1}", pData.NgaySinh, pData.GioiTinh))
                            ));
                        }
                        else if (pText.StartsWith("Địa chỉ:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Địa chỉ: " + pData.DiaChi)
                            ));
                        }
                        else if (pText.StartsWith("Vào viện:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Vào viện: " + pData.VaoVien)
                            ));
                        }
                        else if (pText.StartsWith("Chẩn đoán:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Chẩn đoán: " + pData.ChanDoan)
                            ));
                        }
                        else if (pText.StartsWith("Tiền sử:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Tiền sử: " + pData.TienSu)
                            ));
                        }
                        else if (pText.StartsWith("Bệnh sử:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Bệnh sử: " + pData.BenhSu)
                            ));
                        }
                        else if (pText.Contains("Thời gian hội chẩn:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Thời gian hội chẩn:    " + pData.ThoiGianHoiChan)
                            ));
                        }
                        else if (pText.StartsWith("Tóm tắt tình trạng bệnh"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Tóm tắt tình trạng bệnh: ")
                            ));
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.TomTat)
                            ));
                        }
                        else if (pText.StartsWith("Các xét nghiệm, chẩn đoán hình ảnh"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Các xét nghiệm, chẩn đoán hình ảnh: ")
                            ));
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.Cls)
                            ));
                        }
                        else if (pText.StartsWith("Phương pháp phẫu thuật"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Phương pháp phẫu thuật: ")
                            ));
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.Pppt)
                            ));
                        }
                        else if (pText.StartsWith("Phương pháp vô cảm dự kiến"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Phương pháp vô cảm dự kiến: ")
                            ));
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.VoCam)
                            ));
                        }
                        else if (pText.Contains("Phẫu  thuật  viên  chính:") || pText.Contains("Phẫu thuật viên chính:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Phẫu  thuật  viên  chính: " + pData.PhauThuatVien)
                            ));
                        }
                        else if (pText.Contains("Ngày, giờ phẫu thuật dự kiến"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Ngày, giờ phẫu thuật dự kiến: " + pData.NgayMo)
                            ));
                        }
                        else if (pText.Contains("Chảy máu , nhiễm trùng, mổ đi mổ lại nhiều lần"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.BienChung)
                            ));
                        }
                        else if (pText.Contains("Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt:"))
                        {
                            p.Descendants(w + "t").Remove();
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "b", new XAttribute(w + "val", "1")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", "Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt:\n")
                            ));
                            p.Add(new XElement(w + "r",
                                new XElement(w + "rPr",
                                    new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                                    new XElement(w + "sz", new XAttribute(w + "val", "24"))
                                ),
                                new XElement(w + "t", pData.BienPhap)
                            ));
                        }
                    }

                    // 2. Cập nhật các ô đánh dấu trong Bảng đánh giá GMHS (Table 1)
                    var tables = doc.Descendants(w + "tbl").ToList();
                    if (tables.Count >= 2)
                    {
                        var tbl1 = tables[1]; // Bảng đánh giá GMHS
                        var rows = tbl1.Elements(w + "tr").ToList();

                        // Hàng 0: Mallampati
                        if (rows.Count > 0)
                        {
                            var cells = rows[0].Elements(w + "tc").ToList();
                            SetCellText(cells, 1, pData.Mallampati == "I" ? "x" : "", w);
                            SetCellText(cells, 3, pData.Mallampati == "II" ? "x" : "", w);
                            SetCellText(cells, 5, pData.Mallampati == "III" ? "x" : "", w);
                            SetCellText(cells, 7, pData.Mallampati == "IV" ? "x" : "", w);
                        }

                        // Hàng 1: Loại phẫu thuật
                        if (rows.Count > 1)
                        {
                            var cells = rows[1].Elements(w + "tc").ToList();
                            SetCellText(cells, 1, pData.LoaiPhauThuat == "Đặc biệt" ? "x" : "", w);
                            SetCellText(cells, 3, pData.LoaiPhauThuat == "Loại I" ? "x" : "", w);
                            SetCellText(cells, 5, pData.LoaiPhauThuat == "Loại II" ? "x" : "", w);
                            SetCellText(cells, 7, pData.LoaiPhauThuat == "Loại III" ? "x" : "", w);
                        }

                        // Hàng 2: Phân loại ASA
                        if (rows.Count > 2)
                        {
                            var cells = rows[2].Elements(w + "tc").ToList();
                            SetCellText(cells, 1, pData.Asa == "I" ? "x" : "", w);
                            SetCellText(cells, 3, pData.Asa == "II" ? "x" : "", w);
                            SetCellText(cells, 5, pData.Asa == "III" ? "x" : "", w);
                            SetCellText(cells, 7, pData.Asa == "IV" ? "x" : "", w);
                            SetCellText(cells, 9, pData.Asa == "V" ? "x" : "", w);
                        }

                        // Hàng 3: Phân loại nguy cơ
                        if (rows.Count > 3)
                        {
                            var cells = rows[3].Elements(w + "tc").ToList();
                            SetCellText(cells, 1, pData.NguyCo == "Sạch" ? "x" : "", w);
                            SetCellText(cells, 3, pData.NguyCo == "Sạch nhiễm" ? "x" : "", w);
                            SetCellText(cells, 5, pData.NguyCo == "Nhiễm" ? "x" : "", w);
                            SetCellText(cells, 7, pData.NguyCo == "Bẩn" ? "x" : "", w);
                        }
                    }

                    // Ghi đè vào document.xml
                    docEntry.Delete();
                    ZipArchiveEntry newDocEntry = archive.CreateEntry("word/document.xml");
                    using (var writer = new StreamWriter(newDocEntry.Open(), Encoding.UTF8))
                    {
                        doc.Save(writer);
                    }
                }

                Console.WriteLine("✔ Đã tạo hoàn tất biên bản PT-01: " + targetFilePath);
            }

            Console.WriteLine("\n=== TẠO THÀNH CÔNG TẤT CẢ 4 BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ PT-01 ===");
        }

        static void SetCellText(List<XElement> cells, int index, string text, XNamespace w)
        {
            if (index < cells.Count)
            {
                var cell = cells[index];
                cell.Descendants(w + "t").Remove();
                var p = cell.Element(w + "p");
                if (p == null)
                {
                    p = new XElement(w + "p");
                    cell.Add(p);
                }
                if (!string.IsNullOrEmpty(text))
                {
                    p.Add(new XElement(w + "r",
                        new XElement(w + "rPr",
                            new XElement(w + "rFonts", new XAttribute(w + "ascii", "Times New Roman"), new XAttribute(w + "hAnsi", "Times New Roman")),
                            new XElement(w + "b", new XAttribute(w + "val", "1")),
                            new XElement(w + "sz", new XAttribute(w + "val", "24"))
                        ),
                        new XElement(w + "t", text)
                    ));
                }
            }
        }
    }
}
