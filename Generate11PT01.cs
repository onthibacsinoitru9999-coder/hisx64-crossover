using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Generate11PT01
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

            // ==========================================
            // PHẦN 1: CỘT SỐNG (CS)
            // ==========================================

            // BN 1: NGUYỄN THỊ HẰNG
            patients.Add(new PatientDocData
            {
                FileName = "1. NGUYEN THI HANG - PT-01.docx",
                HoTen = "NGUYỄN THỊ HẰNG",
                NgaySinh = "04/10/1980",
                GioiTinh = "Nữ",
                DiaChi = "Phường Sông Trí, Thị xã Kỳ Anh, Tỉnh Hà Tĩnh",
                VaoVien = "07/09/2026 09:42",
                ChanDoan = "Trật khớp đốt sống cổ C1-C2, mất vững cột sống cổ C1-C2 (Trật đốt sống cổ C1C2 HOS) [S13.1]",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân đau vùng cột sống cổ kéo dài nhiều tháng, đau tăng dần kèm lan tê bì xuống hai tay, hạn chế vận động cúi ngửa và xoay cổ. Đã điều trị nội khoa nhiều đợt không đỡ, vào Bệnh viện Bạch Mai khám và nhập viện Khoa CTCH & Cột sống phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), da niêm mạc hồng, không phù, không sốt. Huyết động ổn định, tim đều rõ, phổi thông khí tốt không rales, bụng mềm, ngực chậu vững. Đau nhiều cột sống cổ lan xuống hai tay, hạn chế biên độ vận động cột sống cổ do đau. Cơ lực tứ chi 5/5, phản xạ gân xương bình thường, cảm giác nông sâu bình thường, không rối loạn cơ tròn.",
                Cls = "MRI và CLVT Cột sống cổ: Hình ảnh trật khớp đội trục (C1-C2), mất vững cột sống cổ cao C1-C2, hẹp ngách sau, chèn ép nhẹ bao màng tủy, chưa thấy phù tủy cổ. X-quang cột sống cổ động thẳng nghiêng: Mất vững C1-C2 rõ khi cúi ngửa. X-quang ngực thẳng bình thường. Bilan xét nghiệm máu: CTM, Đông máu, Sinh hóa cơ bản ổn định; Điện tim nhịp xoang đều; Nhóm máu, HIV, HBsAg, HCV âm tính.",
                Pppt = "Phẫu thuật nắn chỉnh và cố định cột sống cổ C1-C2 bằng vít qua cuống/khối bên kèm ghép xương (Cố định CS C1 C2)",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tổn thương động mạch đốt sống, tổn thương tủy cổ cao (liệt tứ chi, suy hô hấp ngừng thở), nhiễm trùng vết mổ, bung lỏng nẹp vít, khớp giả, tử vong trên bàn mổ.",
                BienPhap = "Dự trù máu trước mổ; Chuẩn bị bộ nẹp vít cột sống cổ cao C1-C2 chuyên dụng và màn huỳnh quang tăng sáng C-arm; Sử dụng máy theo dõi điện thế gợi thần kinh (IONM) nếu có; Bất động nẹp cổ cứng sau mổ; Kháng sinh dự phòng theo phác đồ."
            });

            // BN 2: PHẠM THỊ TUYẾT
            patients.Add(new PatientDocData
            {
                FileName = "2. PHAM THI TUYET - PT-01.docx",
                HoTen = "PHẠM THỊ TUYẾT",
                NgaySinh = "20/08/1966",
                GioiTinh = "Nữ",
                DiaChi = "Xã Quốc Oai, Huyện Quốc Oai, Thành phố Hà Nội",
                VaoVien = "07/09/2026 10:08",
                ChanDoan = "Xẹp cấp thân đốt sống L1 gù cột sống do loãng xương [M48.56]",
                TienSu = "Loãng xương, không có tiền sử dị ứng thuốc.",
                BenhSu = "Khoảng 1 tuần trước bệnh nhân bị trượt chân ngã ngồi đập mông xuống nền cứng, sau ngã đau dữ dội vùng thắt lưng, đau chói tăng khi ngồi dậy và thay đổi tư thế, nằm yên đỡ đau, điều trị giảm đau nội khoa không đỡ, vào Bệnh viện Bạch Mai phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không phù, không sốt, thể trạng trung bình. Bụng mềm, ngực chậu vững. Đau chói gai sau thân đốt sống L1 (VAS 4-5 điểm), đau tăng khi thay đổi tư thế và đi lại. Cơ lực hai chi dưới 5/5, cảm giác bình thường, không có dấu hiệu chèn ép rễ hay chùm đuôi ngựa, đại tiểu tiện tự chủ.",
                Cls = "MRI Cột sống thắt lưng: Hình ảnh phù tủy xương cấp tính thân đốt sống L1 giảm chiều cao, gù góc gập thân đốt L1, chưa có mảnh vỡ tường sau đẩy vào ống sống. X-quang CSTL thẳng nghiêng: L1 xẹp lún giảm chiều cao > 30%, loãng xương cột sống. X-quang tim phổi bình thường. Bilan xét nghiệm huyết học, đông máu, sinh hóa máu cơ bản trong giới hạn cho phép can thiệp.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống L1 bằng bơm xi măng sinh học có bóng qua da (Kyphoplasty L1 / Bơm xi măng L1 có bóng)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại II",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Rò xi măng sinh học vào ống sống gây chèn ép tủy/rễ, rò xi măng vào hệ tĩnh mạch gây thuyên tắc phổi do xi măng, tụ máu vị trí chọc kim, nhiễm trùng thân đốt sống, xẹp các đốt sống lân cận do loãng xương.",
                BienPhap = "Kiểm soát đường vào kim và quá trình nở bóng bơm xi măng dưới màn tăng sáng C-arm liên tục; Sử dụng xi măng sinh học có độ cản quang và độ nhớt cao; Theo dõi sát DHST và thần kinh chi dưới sau can thiệp; Bổ sung phác đồ điều trị loãng xương tích cực sau mổ."
            });

            // BN 3: KHƯƠNG QUỐC ĐẠI
            patients.Add(new PatientDocData
            {
                FileName = "3. KHUONG QUOC DAI - PT-01.docx",
                HoTen = "KHƯƠNG QUỐC ĐẠI",
                NgaySinh = "24/11/1989",
                GioiTinh = "Nam",
                DiaChi = "Phường Hoàng Mai, Quận Hoàng Mai, Thành phố Hà Nội",
                VaoVien = "06/09/2026 14:59",
                ChanDoan = "Thoát vị đĩa đệm cột sống thắt lưng L5-S1 thể sau bên chèn ép rễ thần kinh S1 [M51.2 / M54.45]",
                TienSu = "Đau thắt lưng mạn tính điều trị nội khoa nhiều đợt; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân đau cột sống thắt lưng lan tê bì xuống mông và mặt sau cẳng chân nhiều tháng nay, điều trị nội khoa không cải thiện. Đợt này đau dữ dội, đi lại khó khăn, tê bì nhiều chân, vào Bệnh viện Bạch Mai khám và nhập viện phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo (GCS 15 điểm), không sốt, huyết động ổn định (Mạch 90 l/p, HA 120/70 mmHg, SpO2 98%). Da niêm mạc hồng, tim đều, phổi thông khí rõ không rales, bụng mềm. Đau nhiều cột sống thắt lưng, ấn đau chói ngách bên L5-S1 lan theo đường đi dây thần kinh tọa (VAS 5-6 điểm). Nghiệm pháp Lasegue (+) 45 độ bên tổn thương. Cơ lực hai chi dưới 5/5, cảm giác giảm nhẹ theo dải bì rễ S1, đại tiểu tiện bình thường.",
                Cls = "MRI Cột sống thắt lưng: Hình ảnh thoát vị đĩa đệm L5-S1 thể sau bên gây chèn ép rễ thần kinh S1 trong ngách bên. X-quang CSTL: Thoái hóa cột sống L5-S1; X-quang ngực thẳng và ECG bình thường. XN: WBC 10.94 G/L, RBC 5.81 T/L, HGB 172 g/L, PLT 285 G/L, PT 100%, INR 1.00, APTT 32.4s, Fibrinogen 3.30 g/L, Nhóm máu O Rh(+), HIV âm tính, HBsAg âm tính.",
                Pppt = "Phẫu thuật nội soi cột sống lấy nhân thoát vị đĩa đệm L5-S1 giải ép rễ thần kinh (Nội soi lấy thoát vị)",
                VoCam = "Gây mê nội khí quản (hoặc Gây tê tủy sống)",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh S1, chảy máu khoang ngoài màng cứng, sót nhân đĩa đệm hoặc thoát vị tái phát, nhiễm trùng đĩa đệm đốt sống.",
                BienPhap = "Chuẩn bị hệ thống phẫu thuật nội soi cột sống độ phân giải cao; Màn tăng sáng C-arm định vị chính xác tầng L5-S1; Kháng sinh dự phòng trước mổ 30 phút; Hướng dẫn đeo đai lưng bảo vệ và phục hồi chức năng sớm sau mổ."
            });

            // BN 4: TẠ THỊ THÌN
            patients.Add(new PatientDocData
            {
                FileName = "4. TA THI THIN - PT-01.docx",
                HoTen = "TẠ THỊ THÌN",
                NgaySinh = "01/01/1952",
                GioiTinh = "Nữ",
                DiaChi = "Xã Trần Phú, Huyện An Lão, Thành phố Hải Phòng",
                VaoVien = "07/09/2026 07:53",
                ChanDoan = "Xẹp cấp thân đốt sống L5 do loãng xương nặng trên bệnh nhân đã bơm xi măng sinh học cũ [M48.50]",
                TienSu = "Loãng xương nặng, đã phẫu thuật bơm xi măng sinh học cột sống 2 lần (năm 2021 và 2023).",
                BenhSu = "Bệnh nhân xuất hiện đau thắt lưng tăng dần cách đây 2 tuần, điều trị nội khoa không đỡ. Đau tại chỗ cột sống thắt lưng tăng nhiều khi thay đổi tư thế, ngồi dậy và đứng đau nhiều, nằm nghỉ đỡ đau, không lan xuống chân, vào Bệnh viện Bạch Mai xin can thiệp.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân 74 tuổi, thể trạng gầy, tỉnh táo, tiếp xúc tốt, da niêm mạc kém hồng, không phù, không sốt. Huyết động ổn định, tim đều, phổi thông khí rõ, bụng mềm. Đau chói gai sau thân đốt sống L5, co cứng cơ cạnh sống thắt lưng (VAS 5 điểm). Cơ lực hai chi dưới 5/5, không tê bì, phản xạ gân xương bình thường, đại tiểu tiện tự chủ.",
                Cls = "MRI Cột sống thắt lưng: Hình ảnh xẹp cấp thân đốt sống L5 phù nề tủy xương; các thân đốt L1, L3 có bóng xi măng cũ ổn định. X-quang CSTL thẳng nghiêng: Thân đốt L5 giảm chiều cao, hình ảnh xi măng cũ các đốt trước đó, loãng xương nặng. Bilan máu cơ bản đủ điều kiện can thiệp.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống L5 bằng bơm xi măng sinh học không bóng qua da (Vertebroplasty L5 / BXM không bóng)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Loại II",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Tràn xi măng ra ngoài thân đốt, chèn ép rễ L5/chùm đuôi ngựa, thuyên tắc tĩnh mạch/phổi do xi măng, tụ máu vị trí chọc kim, xẹp gãy đốt sống lân cận do loãng xương nặng.",
                BienPhap = "Kiểm soát tiêm xi măng từng ml dưới C-arm liên tục; Pha xi măng đạt độ quánh tối ưu trước khi bơm; Bù dịch và theo dõi sát huyết động trong mổ; Phối hợp điều trị loãng xương chuyên khoa sau can thiệp."
            });

            // BN 5: TRẦN THỊ HIỀN
            patients.Add(new PatientDocData
            {
                FileName = "5. TRAN THI HIEN - PT-01.docx",
                HoTen = "TRẦN THỊ HIỀN",
                NgaySinh = "20/10/1957",
                GioiTinh = "Nữ",
                DiaChi = "Xã Phụ Dực, Huyện Quỳnh Phụ, Tỉnh Hưng Yên",
                VaoVien = "07/09/2026 07:14",
                ChanDoan = "Chấn thương xẹp vỡ đốt sống T12 gù mất vững / Não úng thủy thể tắc nghẽn [M48.50 / G91.0, G91.1]",
                TienSu = "Bệnh não úng thủy thể tắc nghẽn; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị ngã chấn thương vùng lưng, sau ngã đau nhiều cột sống đoạn ngực - thắt lưng, gù lưng, hạn chế vận động nhiều, đi lại khó khăn, điều trị tuyến dưới không đỡ, vào Bệnh viện Bạch Mai phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo (GCS 15 điểm), da niêm mạc hồng, không phù, không sốt, tự thở tốt. Đồng tử 2 bên đều 2mm, PXAS (+), hội chứng tăng áp lực nội sọ âm tính. Đau chói nhiều vùng bản lề ngực - thắt lưng T12, gù cột sống nhẹ, hạn chế cúi ngửa. Cơ lực hai chân 5/5, cảm giác bình thường, không rối loạn cơ tròn.",
                Cls = "MRI Cột sống ngực - thắt lưng: Hình ảnh xẹp vỡ thân đốt sống T12 gù góc bản lề, phù tủy xương tiến triển, mất vững cột sống. XN: WBC 7.0 G/L, RBC 4.66 T/L, HGB 131 g/L, PLT 417 G/L, HCT 0.413 L/L, Neut% 65.0%; PT 91%, INR 1.06, APTT 25.4s, Fibrinogen 3.61 g/L; Glucose 8.5 mmol/L, Ure 6.4 mmol/L, Creatinin 59 µmol/L, AST 29 U/L, ALT 16 U/L, Na 136, K 3.6, Cl 99; Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật cố định cột sống ngực - thắt lưng ít xâm lấn nẹp vít qua cuống (CĐCS ít xâm lấn)",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "BS Trịnh Minh Đức",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tổn thương rễ thần kinh/tủy ngực, rách màng cứng, nhiễm trùng vết mổ, bung lỏng nẹp vít, biến chứng thần kinh liên quan tiền sử não úng thủy.",
                BienPhap = "Chuẩn bị bộ nẹp vít cột sống qua da ít xâm lấn (MIS); Định vị C-arm 2 bình diện kiểm tra chính xác đường đi cuống sống T11, T12, L1; Kháng sinh dự phòng theo phác đồ; Hội chẩn Thần kinh đánh giá tình trạng não úng thủy ổn định."
            });

            // ==========================================
            // PHẦN 2: CHẤN THƯƠNG CHỈNH HÌNH (CTCH)
            // ==========================================

            // BN 6: NGUYỄN VĂN HUẤN
            patients.Add(new PatientDocData
            {
                FileName = "6. NGUYEN VAN HUAN - PT-01.docx",
                HoTen = "NGUYỄN VĂN HUÂN",
                NgaySinh = "24/09/1965",
                GioiTinh = "Nam",
                DiaChi = "Phường Hoàng Mai, Quận Hoàng Mai, Thành phố Hà Nội",
                VaoVien = "07/09/2026 08:38",
                ChanDoan = "Hoại tử vô khuẩn chỏm xương đùi hai bên giai đoạn muộn (Phải > Trái) / Đái tháo đường type 2 - Tăng lipid máu hỗn hợp - Suy thượng thận do thuốc [M87.00 / E78.2, E27.3, E11.9]",
                TienSu = "Đái tháo đường type 2 từ 2025; Thiểu năng vỏ thượng thận do thuốc (lạm dụng corticoid); Rối loạn lipid máu hỗn hợp.",
                BenhSu = "Bệnh nhân đau và hạn chế vận động khớp háng 2 bên nhiều tháng nay, tăng dần, 7 tháng nay không đi lại được, phụ thuộc hoàn toàn vào xe lăn, điều trị nội khoa nhiều nơi không đỡ, vào Bệnh viện Bạch Mai phẫu thuật thay khớp háng.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định. Teo nhiều khối cơ đùi hai bên, đau khớp háng hai bên (phải > trái), hạn chế vận động nặng các động tác gấp duỗi, dạng khép và xoay trong ngoài, chân phải ngắn hơn chân trái 1cm. ĐMMM theo dõi đạt 9.7 mmol/L. Mạch mu chân hai bên bắt rõ.",
                Cls = "X-quang khớp háng hai bên và khung chậu thẳng: Hình ảnh hoại tử tiêu chỏm xương đùi hai bên giai đoạn Ficat IV, xẹp vỡ diện khớp chỏm xương đùi phải, hẹp khe khớp háng nặng. Bilan điện giải, đông máu, công thức máu và đường huyết đang được kiểm soát ổn định chuẩn bị phẫu thuật.",
                Pppt = "Phẫu thuật thay toàn bộ khớp háng bên phải (Thay khớp háng)",
                VoCam = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS. Hà Đức Cường",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "III",
                NguyCo = "Sạch",
                BienChung = "Mất máu nhiều trong mổ, trật khớp háng nhân tạo, gãy xương quanh chuôi khớp, tổn thương thần kinh ngồi/đùi, huyết khối tĩnh mạch sâu (DVT), nhiễm trùng khớp nhân tạo, suy thượng thận cấp chu phẫu do stress mổ.",
                BienPhap = "Bổ sung Hydrocortisone phòng suy thượng thận cấp chu phẫu; Kiểm soát đường huyết mục tiêu 7-10 mmol/L; Chuẩn bị bộ khớp háng nhân tạo không xi măng chất lượng cao; Dự trù 2 đơn vị hồng cầu khối; Dự phòng huyết khối tĩnh mạch sâu bằng thuốc chống đông và tất áp lực sau mổ; Kháng sinh dự phòng theo phác đồ."
            });

            // BN 7: PHẠM CAO CƯỜNG
            patients.Add(new PatientDocData
            {
                FileName = "7. PHAM CAO CUONG - PT-01.docx",
                HoTen = "PHẠM CAO CƯỜNG",
                NgaySinh = "02/02/1991",
                GioiTinh = "Nam",
                DiaChi = "Phường Hà Đông, Quận Hà Đông, Thành phố Hà Nội",
                VaoVien = "07/09/2026 08:27",
                ChanDoan = "U trung mô mấu chuyển lớn xương đùi trái / Theo dõi loạn dưỡng cơ [D16.2]",
                TienSu = "Theo dõi loạn dưỡng cơ (đã đo điện cơ cực kim tại BV 103, men CK bình thường).",
                BenhSu = "Bệnh nhân đau tức vùng hông - mấu chuyển lớn xương đùi trái nhiều năm nay, điều trị nội khoa không đỡ. Đợt này đau tăng, sờ thấy khối vùng mấu chuyển lớn, vào Bệnh viện Bạch Mai khám và nhập viện phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng. Đau vùng háng và mấu chuyển lớn xương đùi trái (VAS 4 điểm), hạn chế nhẹ vận động dạng khép khớp háng. Mạch mu chân rõ, bàn ngón chân hồng ấm, cảm giác và vận động đầu chi bình thường.",
                Cls = "MRI và X-quang đùi trái: Hình ảnh tổn thương dạng u vùng mấu chuyển lớn xương đùi trái; Kết quả mô bệnh học sinh thiết trước đó: U trung mô vùng mấu chuyển lớn xương đùi trái. Bilan đông máu, CTM, sinh hóa máu cơ bản ổn định.",
                Pppt = "Phẫu thuật bóc tách lấy u vùng mấu chuyển lớn xương đùi trái, làm giải phẫu bệnh (Lấy u, GPB)",
                VoCam = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS Nguyễn Thế Luân",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tổn thương bó mạch thần kinh đùi/ngồi, gãy xương bệnh lý vùng mấu chuyển lớn trong và sau mổ, nhiễm trùng vết mổ, sót u tái phát.",
                BienPhap = "Phẫu tích cẩn trọng theo ranh giới vỏ u; Gửi mẫu bệnh phẩm làm mô bệnh học (GPB) thường quy và sinh thiết tức thì; Kháng sinh dự phòng; Hạn chế tì đè chân trái sau mổ nếu diện bóc tách sát vỏ xương."
            });

            // BN 8: TRẦN DANH DŨNG
            patients.Add(new PatientDocData
            {
                FileName = "8. TRAN DANH DUNG - PT-01.docx",
                HoTen = "TRẦN DANH DŨNG",
                NgaySinh = "11/03/1979",
                GioiTinh = "Nam",
                DiaChi = "Xã Hợp Thịnh, Huyện Hiệp Hòa, Tỉnh Bắc Ninh",
                VaoVien = "07/09/2026 10:15",
                ChanDoan = "Chấn thương khớp gối phải: Đứt hoàn toàn dây chằng chéo trước [M23.00]",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân chấn thương gối phải do tai nạn, sau ngã nghe tiếng 'rắc', gối sưng đau nhiều và hạn chế vận động, đi lại có cảm giác lỏng gối, trẹo gối, bất động nẹp không đỡ, vào Bệnh viện Bạch Mai phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo (GCS 15 điểm), huyết động ổn định, không sốt. Khớp gối phải sưng nề nhẹ, ấn đau khe khớp, hạn chế gấp duỗi gối do đau. Dấu hiệu ngăn kéo trước (+), Lachman (+), Pivot shift (+). Mạch mu chân rõ, chi ấm, vận động cảm giác bàn ngón chân tốt.",
                Cls = "MRI khớp gối phải: Hình ảnh đứt hoàn toàn dây chằng chéo trước (ACL), tràn dịch khớp gối, dập nhẹ sụn chêm trong. X-quang khớp gối: Không thấy gãy xương. Bilan xét nghiệm tiền phẫu đầy đủ trong giới hạn bình thường.",
                Pppt = "Phẫu thuật nội soi khớp gối tái tạo dây chằng chéo trước bằng gân tự thân (Nội soi tái tạo dây chằng)",
                VoCam = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS. Hà Đức Cường",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Tụ máu - tràn máu khớp gối, nhiễm trùng khớp gối, đứt/lỏng mảnh ghép dây chằng tái tạo, cứng khớp gối, tổn thương mạch khoeo hoặc thần kinh chày/mác.",
                BienPhap = "Dàn máy nội soi khớp gối chuyên dụng; Chuẩn bị bộ dụng cụ tái tạo dây chằng chéo và vật liệu cố định (vít tự tiêu / Endobutton); Kháng sinh dự phòng; Đeo nẹp Zimmer cố định gối sau mổ và hướng dẫn tập phục hồi chức năng sớm."
            });

            // BN 9: PHẠM THỊNH NUÔI
            patients.Add(new PatientDocData
            {
                FileName = "9. PHAM THINH NUOI - PT-01.docx",
                HoTen = "PHẠM THỊNH NUÔI",
                NgaySinh = "12/11/1957",
                GioiTinh = "Nam",
                DiaChi = "Phường Việt Hưng, Quận Long Biên, Thành phố Hà Nội",
                VaoVien = "03/09/2026 08:49",
                ChanDoan = "Chấn thương khớp gối phải: Đứt hoàn toàn dây chằng bánh chè - Trật xương bánh chè [M22.3]",
                TienSu = "Chấn thương cũ khớp gối, sẹo mổ cũ khớp gối liền tốt; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị ngã đập gối phải, sau ngã đau nhói mặt trước gối, mất hoàn toàn khả năng chủ động duỗi thẳng cẳng chân (mất duỗi gối), sờ thấy khuyết hõm dưới xương bánh chè, vào Bệnh viện Bạch Mai nhập viện phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định. Đau ít (VAS 1-2 điểm). Khớp gối phải mất động tác duỗi chủ động, dấu hiệu mất duỗi gối, sờ khuyết hõm cực dưới xương bánh chè, xương bánh chè bị co kéo lên trên. Mạch mu chân và chày sau bắt rõ, đầu chi ấm hồng.",
                Cls = "MRI khớp gối phải: Hình ảnh đứt hoàn toàn gân dây chằng bánh chè tại điểm bám cực dưới xương bánh chè, xương bánh chè di lệch lên cao (Patella alta), tràn dịch khớp gối. X-quang khớp gối: Xương bánh chè lên cao, không thấy gãy xương. Bilan xét nghiệm tiền phẫu đầy đủ.",
                Pppt = "Phẫu thuật mở khâu phục hồi và tái tạo dây chằng bánh chè khớp gối phải bằng chỉ siêu bền (tái tạo DC bánh chè)",
                VoCam = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS Nguyễn Thế Luân & BS Hà Đức Cường",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Đứt tái phát gân bánh chè, nhiễm trùng khớp gối, cứng khớp hạn chế biên độ gấp gối, hội chứng bánh chè đùi đau mạn tính, tụ máu vết mổ.",
                BienPhap = "Chuẩn bị bộ chỉ neo xương (suture anchor) và chỉ bện siêu bền FiberWire; Đặt nẹp cố định duỗi gối sau mổ trong 4-6 tuần; Kháng sinh dự phòng theo phác đồ; Hướng dẫn tập gồng cơ tứ đầu đùi sớm."
            });

            // BN 10: VI TRUNG HIẾU
            patients.Add(new PatientDocData
            {
                FileName = "10. VI TRUNG HIEU - PT-01.docx",
                HoTen = "VI TRUNG HIẾU",
                NgaySinh = "07/07/2007",
                GioiTinh = "Nam",
                DiaChi = "Phường Kim Liên, Quận Đống Đa, Thành phố Hà Nội",
                VaoVien = "04/09/2026 23:15",
                ChanDoan = "Gãy kín 1/3 giữa hai xương cẳng tay trái có di lệch [S52.90]",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị tai nạn ngã chống tay trái, sau ngã đau chói dữ dội và biến dạng cẳng tay trái, hạn chế vận động hoàn toàn cẳng tay và cổ tay trái, được sơ cứu cố định nẹp bột tạm thời và chuyển vào Bệnh viện Bạch Mai phẫu thuật.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân nam 19 tuổi, tỉnh táo (GCS 15 điểm), huyết động ổn định. Khung chậu vững, ngực bụng bình thường. Cẳng tay trái sưng nề, biến dạng gập góc nhẹ 1/3 giữa, điểm đau chói và mất liên tục xương quay và xương trụ. Mạch quay và mạch trụ bắt rõ, vận động gấp duỗi các ngón tay tốt, cảm giác đầu ngón tay bình thường, không có dấu hiệu chèn ép khoang.",
                Cls = "X-quang cẳng tay trái thẳng nghiêng: Hình ảnh gãy kín 1/3 giữa thân xương quay và xương trụ trái di lệch chồng ngắn và gập góc. X-quang ngực thẳng bình thường. XN: WBC 11.88 G/L, RBC 5.78 T/L, HGB 160 g/L, PLT 279 G/L, HCT 0.460 L/L, Neut% 71.6%; PT 78%, INR 1.19, APTT 34.0s, Fibrinogen 2.72 g/L; Glucose 5.1 mmol/L, Ure 6.8 mmol/L, Creatinin 87 µmol/L, AST 26 U/L, Na 134, K 3.8, Cl 98; Nhóm máu AB Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật mở nắn chỉnh và kết hợp xương hai xương cẳng tay trái bằng nẹp vít nén ép (KHX)",
                VoCam = "Gây mê nội khí quản (hoặc Gây tê đám rối thần kinh cánh tay)",
                PhauThuatVien = "BS Nguyễn Thế Luân",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Hội chứng chèn ép khoang cẳng tay sau mổ, tổn thương nhánh thần kinh quay/thần kinh gian cốt sau, nhiễm trùng vết mổ, chậm liền xương, khớp giả, hạn chế động tác sấp ngửa cẳng tay, gãy lại sau tháo nẹp.",
                BienPhap = "Chuẩn bị 2 bộ nẹp LC-DCP 3.5mm cho xương quay và xương trụ; Đặt dẫn lưu kín nếu cần; Treo tay cao sau mổ theo dõi sát các khoang cẳng tay; Kháng sinh dự phòng trước rạch da 30 phút."
            });

            // BN 11: ĐẶNG XUÂN TẤN
            patients.Add(new PatientDocData
            {
                FileName = "11. DANG XUAN TAN - PT-01.docx",
                HoTen = "ĐẶNG XUÂN TẤN",
                NgaySinh = "01/08/1978",
                GioiTinh = "Nam",
                DiaChi = "Phường Hà Nam, Thị xã Phủ Lý, Tỉnh Ninh Bình",
                VaoVien = "04/09/2026 13:42",
                ChanDoan = "Chấn thương khớp gối trái: Đứt hoàn toàn dây chằng chéo sau / Tiền sử viêm gan B, tán sỏi thận P [S83.5]",
                TienSu = "Viêm gan virus B mạn tính; Tán sỏi thận phải ngoài cơ thể 3 năm trước; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị tai nạn giao thông ngã xe máy va đập mạnh gối trái, sau tai nạn khớp gối trái sưng đau nhiều, đi lại thấy lỏng gối rõ, gối mất vững, vào Bệnh viện Bạch Mai khám và có chỉ định phẫu thuật tái tạo dây chằng.",
                ThoiGianHoiChan = "07/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định, tim phổi bình thường, bụng mềm, khung chậu vững. Khớp gối trái sưng đau, hạn chế vận động, lỏng khớp gối rõ rệt. Nghiệm pháp ngăn kéo sau (+++), dấu hiệu sụt lùi xương chày sau (Posterior sag sign) (+). Mạch mu chân và chày sau bắt rõ, đầu chi ấm hồng, vận động cảm giác bàn ngón chân tốt.",
                Cls = "MRI khớp gối trái: Đứt hoàn toàn dây chằng chéo sau (PCL) gối trái, tụ dịch bao khớp gối. X-quang khớp gối trái: Khe khớp bình thường, không gãy xương. XN: WBC 10.4 G/L, RBC 5.15 T/L, HGB 162 g/L, PLT 287 G/L, HCT 0.482 L/L, Neut% 64.7%; PT 94%, INR 1.05, APTT 28.4s, Fibrinogen 3.01 g/L; Glucose 9.6 mmol/L, Ure 4.6 mmol/L, Creatinin 80 µmol/L, AST 22 U/L, ALT 39 U/L, Na 140, K 3.2, Cl 103; Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật nội soi khớp gối tái tạo dây chằng chéo sau bằng gân tự thân (tái tạo dc chéo sau)",
                VoCam = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
                PhauThuatVien = "BS. Hà Đức Cường",
                NgayMo = "ngày 08 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, tổn thương động mạch khoeo/thần kinh khoeo (rất nguy hiểm trong phẫu thuật PCL), nhiễm trùng khớp gối, đứt lỏng mảnh ghép, hạn chế biên độ gấp duỗi gối sau mổ.",
                BienPhap = "Chuẩn bị hệ thống camera nội soi góc nghiêng và bộ dẫn đường tái tạo PCL chuyên dụng; Màn tăng sáng C-arm kiểm soát vị trí khoan đường hầm chày tránh thủng vỏ xương sau; Kháng sinh dự phòng; Bất động nẹp PCL chuyên dụng sau mổ."
            });

            // ==========================================
            // THỰC HIỆN XUẤT 11 FILE DOCX
            // ==========================================
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            byte[] templateBytes;
            using (var fs = new FileStream(templateDocx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                templateBytes = ms.ToArray();
            }

            Console.WriteLine("=== BẮT ĐẦU XUẤT 11 BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) ===");

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

                    // 1. Duyệt qua các đoạn văn bản trong body
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

            Console.WriteLine("\n=== TẠO THÀNH CÔNG TẤT CẢ 11 BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ PT-01 ===");
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
