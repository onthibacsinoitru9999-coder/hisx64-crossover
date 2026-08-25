using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Generate6PT01
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
        public string Asa { get; set; } // "I", "II", "III"
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
            string templateDocx = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01.docx";
            string outputDir = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01";

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            List<PatientDocData> patients = new List<PatientDocData>();

            // 1. NGUYỄN ANH QUANG
            patients.Add(new PatientDocData
            {
                FileName = "1. NGUYEN ANH QUANG - PT-01.docx",
                HoTen = "NGUYỄN ANH QUANG",
                NgaySinh = "06/10/1981",
                GioiTinh = "Nam",
                DiaChi = "Xã Đồng Thái, Huyện Yên Mô, Tỉnh Ninh Bình",
                VaoVien = "24/08/2026 09:31",
                ChanDoan = "Thoát vị đĩa đệm cột sống thắt lưng L4-5, L5-S1 thể trung tâm, hẹp ống sống thắt lưng nặng chèn ép chùm rễ đuôi ngựa và rễ L4, L5 hai bên / Viêm gan B mạn, Di chứng cắt cụt 1/2 bàn tay phải do TNLĐ cũ",
                TienSu = "Viêm gan B mạn tính (HBsAg dương tính); Cắt cụt 1/2 bàn tay phải do tai nạn lao động năm 1994; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân đau cột sống thắt lưng lan tê bì chân phải nhiều năm nay điều trị nội khoa không đỡ. Gần đây đau tăng nhiều lan tê bì cả 2 chân, đi bộ cách hồi thần kinh < 300m, hạn chế vận động nhiều, vào Bệnh viện Bạch Mai khám và nhập viện phẫu thuật.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh, tiếp xúc tốt, da niêm mạc hồng, không sốt, HA 120/80 mmHg, Mạch 78 l/p. Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Đau vùng cột sống thắt lưng L4-S1, ấn đau chói ngách bên và cạnh sống, đau tê bì lan mặt ngoài đùi cẳng chân 2 bên (VAS 4-5 điểm). Đau cách hồi thần kinh < 300m. Cơ lực 2 chân 5/5, phản xạ gân xương bình thường, không rối loạn cơ tròn.",
                Cls = "MRI CSTL: Phình đĩa đệm L3-4; Thoát vị đĩa đệm L4-5, L5-S1 thể trung tâm gây hẹp ống sống nặng, chèn ép chùm rễ đuôi ngựa và rễ L4, L5 trong ngách bên hai bên. X-quang: Cột sống thắt lưng thẳng nghiêng, động gập ưỡn mất vững nhẹ L4-5, L5-S1; X-quang ngực thẳng bình thường. XN: WBC 7.7 G/L, Neut% 52.8%, HGB 147 g/L, PLT 344 G/L, PT 102%, INR 0.98, APTT 28.5s, Fibrinogen 3.84 g/L, Glucose 4.8 mmol/L, Ure 4.9 mmol/L, Creatinin 46 µmol/L, AST 18 U/L, ALT 25 U/L, Na 140, K 3.3 mmol/L (đang bù K+), Cl 104, Nhóm máu A Rh(+), HBsAg (+), HIV/HCV âm tính.",
                Pppt = "Phẫu thuật mở giải ép ống sống, cắt đĩa đệm, ghép xương liên thân đốt và cố định cột sống bằng nẹp vít qua cuống 2 tầng L4-L5, L5-S1 (TLIF 2 tầng L4-5 L5-S1)",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng vết mổ, rách màng cứng, rò dịch não tủy, tổn thương rễ thần kinh, bung nẹp vít / thất bại hàn xương, mổ lại nhiều lần, dị ứng thuốc, tử vong trên bàn mổ.",
                BienPhap = "Bù Kali máu trước mổ (đích K+ >= 3.5 mmol/L); Dự trù 02 đơn vị hồng cầu lắng cùng nhóm; Kháng sinh dự phòng theo phác đồ; Áo nẹp cột sống thắt lưng sau mổ."
            });

            // 2. PHẠM THỊ HIẾU
            patients.Add(new PatientDocData
            {
                FileName = "2. PHAM THI HIEU - PT-01.docx",
                HoTen = "PHẠM THỊ HIẾU",
                NgaySinh = "16/03/1974",
                GioiTinh = "Nữ",
                DiaChi = "Phường Tân Mai, Thành phố Vinh, Tỉnh Nghệ An",
                VaoVien = "24/08/2026 09:29",
                ChanDoan = "Trượt đốt sống L4-L5 ra trước độ I do thoái hóa, hẹp ống sống thắt lưng chèn ép rễ thần kinh L5 (P) / Thoái hóa khớp gối 2 bên",
                TienSu = "Thoái hóa khớp gối 2 bên; Không có tiền sử dị ứng thuốc; Không có tiền sử bệnh lý tim mạch, nội tiết khác.",
                BenhSu = "Bệnh nhân đau cột sống thắt lưng tăng dần nhiều tháng nay, đau lan xuống mông và mặt ngoài cẳng chân phải kèm tê bì, đau tăng khi đi lại hoặc đứng lâu, đau cách hồi thần kinh < 100m, điều trị nội khoa không đỡ, vào viện xin phẫu thuật.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, HA 135/90 mmHg, Mạch 86 ck/p, thể trạng trung bình (cao 150cm, nặng 65kg). Tim đều, phổi rõ, bụng mềm. Ấn đau chói khe liên gai sau L4-L5, co cứng cơ cạnh sống thắt lưng (VAS 3-4 điểm), tê bì lan chân phải. Đau cách hồi thần kinh < 100m. Cơ lực 2 chân 5/5, không teo cơ, đại tiểu tiện tự chủ.",
                Cls = "MRI/X-quang CSTL: Hình ảnh trượt thân đốt sống L4 ra trước trên L5 độ I, mất vững cột sống trên phim động gập ưỡn; Hẹp ống sống tầng L4-L5 chèn ép rễ L5 phải; Thoái hóa khớp gối 2 bên. X-quang ngực thẳng: Bình thường. XN: WBC 7.1 G/L, Neut% 59.3%, HGB 141 g/L, PLT 403 G/L, PT 90%, INR 1.07, APTT 32.2s, Fib 3.70 g/L, Glucose 4.6 mmol/L, Ure 3.8 mmol/L, Creatinin 37 µmol/L, AST 29 U/L, ALT 29 U/L, Na 139, K 4.0, Cl 102, Nhóm máu A Rh(+), HIV/HBsAg/HCV âm tính, nước tiểu bình thường.",
                Pppt = "Phẫu thuật mở nắn chỉnh trượt, giải ép cuống rễ thần kinh, hàn xương liên thân đốt và cố định cột sống bằng nẹp vít qua cuống L4-L5 (TLIF L4-5)",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Loại I",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng vết mổ, tổn thương rễ L5, rách màng cứng, tụ máu chèn ép sau mổ, bung nẹp vít / trượt tái phát, phản vệ dị ứng, tử vong trên bàn mổ.",
                BienPhap = "Kiểm soát huyết áp trong và sau mổ; Kháng sinh dự phòng theo phác đồ; Dự trù máu 01 đơn vị; Áo nẹp cột sống thắt lưng hỗ trợ sau mổ."
            });

            // 3. NGUYỄN THỊ HOA
            patients.Add(new PatientDocData
            {
                FileName = "3. NGUYEN THI HOA - PT-01.docx",
                HoTen = "NGUYỄN THỊ HOA",
                NgaySinh = "17/04/1967",
                GioiTinh = "Nữ",
                DiaChi = "Xã Cẩm Xuyên, Huyện Cẩm Xuyên, Tỉnh Hà Tĩnh",
                VaoVien = "07/08/2026 07:41",
                ChanDoan = "Thoát vị đĩa đệm cột sống thắt lưng đa tầng L3/4, L4/5, L5/S1 rách vòng xơ chèn ép rễ thần kinh L5, S1 hai bên / Đái tháo đường type 2 - Suy tuyến thượng thận do thuốc (kiểu hình Cushing) - Tăng huyết áp - Tăng lipid máu - Gan nhiễm mỡ độ II, tăng men gan - Loãng xương",
                TienSu = "Suy tuyến thượng thận 10 năm điều trị Hydrocortisone 10mg (sáng 1.5v, chiều 0.5v); ĐTĐ type 2 điều trị Insulin Novomix + Linagliptin 5mg; Tăng huyết áp 6-7 năm; Rối loạn lipid máu; Loãng xương; Đã phẫu thuật thay TTT mắt phải, đục TTT mắt trái.",
                BenhSu = "Bệnh nhân đau CSTL kèm tê yếu 2 chân, mệt mỏi, vào BV Bạch Mai điều trị nội khoa tại Khoa Nội tiết - ĐTĐ kiểm soát đường huyết, bù điện giải, điều chỉnh liều Hydrocortisone và hạ men gan. Sau khi tình trạng nội khoa ổn định, bệnh nhân được chuyển Khoa CTCH hội chẩn phẫu thuật cột sống.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh, GCS 15 điểm, thể trạng kiểu hình Cushing, da niêm mạc hồng nhạt, không phù, không sốt, HA 130/80 mmHg, Mạch 84 ck/p. Tim đều rõ, phổi thông khí tốt không rales, bụng mềm. Đau cột sống thắt lưng (VAS 3 điểm), tê bì lan dọc 2 chân (trái > phải). Cơ lực 2 chân 5/5, đại tiểu tiện tự chủ. Đường máu mao mạch kiểm soát ổn định (6-10 mmol/L). Đã soi đáy mắt kiểm tra trước mổ tư thế nằm sấp: không tổn thương vi mạch võng mạc tiến triển.",
                Cls = "MRI CSTL: Thoái hóa đa tầng, phồng và rách vòng xơ, thoát vị đĩa đệm L3/4, L4/5, L5/S1 gây chèn ép rễ thần kinh L5, S1 hai bên; Thoát vị nội xốp L3, L5; Loãng xương. X-quang CSTL thẳng nghiêng và động gập ưỡn; X-quang ngực thẳng; Khám chuyên khoa Mắt / Soi đáy mắt: Đục TTT mắt trái, đã thay TTT mắt phải, không tổn thương võng mạc ĐTĐ tiến triển; Điện tim và Siêu âm tim (đã hội chẩn Viện Tim Mạch). XN: WBC 7.2 G/L, Neut% 62.5%, HGB 128 g/L, PLT 250 G/L, PT 138%, INR 0.83, APTT 23.1s, Fib 3.77 g/L, Glucose máu 6.6 - 8.3 mmol/L, Creatinin 52 µmol/L, AST 86 U/L, ALT 157 U/L (đang giảm dần), Na 141, K 3.4 mmol/L, Ca 2.21 mmol/L, Troponin Ths 53.2 (đã hội chẩn Tim mạch cho phép mổ), Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật mở giải ép bản sống và lỗ tiếp hợp 3 tầng L3-L4, L4-L5, L5-S1, hàn xương liên thân đốt đa tầng (TLIF 3 tầng L3-4, L4-5, L5-S1) và cố định cột sống bằng nẹp vít cuống sống",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "III",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng vết mổ (nguy cơ cao do ĐTĐ/Cushing), suy thượng thận cấp chu phẫu, rối loạn đường huyết / hạ đường huyết, tăng men gan, biến chứng tim mạch, rách màng cứng, rò DNT, tổn thương rễ thần kinh, bung vít loãng xương, tử vong trên bàn mổ.",
                BienPhap = "Phác đồ bù Hydrocortisone liều stress chu phẫu (Hydrocortisone 100mg tiêm trước khởi mê và duy trì hậu phẫu); Kiểm soát đường huyết bằng Insulin truyền tĩnh mạch/tiêm ngắt quãng (đích ĐH 7-10 mmol/L); Dự trù 02 đơn vị khối hồng cầu; Sử dụng vít xi măng / vít nở tăng cường độ vững loãng xương; Kháng sinh dự phòng phổ rộng; Theo dõi sát điện giải K+, Na+, huyết áp và điện tim liên tục."
            });

            // 4. NGUYỄN THỊ HẰNG*
            patients.Add(new PatientDocData
            {
                FileName = "4. NGUYEN THI HANG - PT-01.docx",
                HoTen = "NGUYỄN THỊ HẰNG",
                NgaySinh = "04/10/1980",
                GioiTinh = "Nữ",
                DiaChi = "Phường Sông Trí, Thị xã Kỳ Anh, Tỉnh Hà Tĩnh",
                VaoVien = "20/08/2026 07:17",
                ChanDoan = "Bán trật khớp đội - trục C1-C2, mất vững cột sống cổ cao C1-C2 gây hẹp ống sống ngang mức chèn ép tủy cổ / Dày vôi hóa dây chằng đội trục",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân đau mỏi và hạn chế vận động cột sống cổ tăng dần kèm tê bì dọc 2 tay lan xuống bàn tay nhiều tháng nay, đau tăng khi cúi ngửa và xoay đầu cổ, điều trị nội khoa tuyến dưới không đỡ, vào Bệnh viện Bạch Mai khám chuyên khoa Cột sống có chỉ định nhập viện phẫu thuật.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh, tiếp xúc tốt, da niêm mạc hồng, không sốt, HA 120/75 mmHg, Mạch 76 ck/p. Tim đều rõ, phổi thông khí tốt, bụng mềm, ngực vững. Đau tức và hạn chế vận động cột sống cổ, ấn đau chói vùng gai sau C1-C2, tê bì lan 2 tay (VAS 3-4 điểm). Cơ lực tứ chi 5/5, phản xạ gân xương tứ chi bình thường, không có dấu hiệu tháp (Hoffmann âm tính, Babinski âm tính), không rối loạn cơ tròn, đầu chi ấm hồng.",
                Cls = "CLVT (CT Scanner) Cột sống cổ: Hình ảnh mất vững khớp đội trục, dày và vôi hóa dây chằng đội trục gây hẹp ống sống ngang mức C1-C2; Thoái hóa cột sống cổ. X-quang: Cột sống cổ thẳng nghiêng và X-quang ngực thẳng tim phổi bình thường; Tổng phân tích nước tiểu bình thường. XN: WBC 6.2 G/L, Neut% 63.5%, HGB 132 g/L, PLT 278 G/L, PT 89%, INR 1.08, APTT 31.2s, Fib 3.04 g/L, Glucose 6.7 mmol/L, Ure 3.6 mmol/L, Creatinin 44 µmol/L, AST 17 U/L, ALT 16 U/L, Na 136, K 3.7, Cl 99, Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật mở nắn chỉnh, giải ép bản sống sau C1, cố định cột sống cổ cao C1-C2 bằng nẹp vít khối bên C1 và vít cuống C2 (kỹ thuật Harms/Goel) kết hợp ghép xương tự thân/đồng loại (CĐ C1-C2, ghép xương)",
                VoCam = "Gây mê nội khí quản (lưu ý đặt nội khí quản bằng ống mềm / video mềm tránh ngửa cổ quá mức)",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu (nguy cơ tổn thương động mạch đốt sống V3), tổn thương tủy cổ cao gây suy hô hấp/liệt tứ chi, rách màng cứng, rò dịch não tủy, nhiễm trùng vết mổ, bung nẹp vít / thất bại hàn xương, mổ lại nhiều lần, tử vong trên bàn mổ.",
                BienPhap = "Đặt ống nội khí quản bằng ống mềm có camera / đèn soi video có kiểm soát; Chuẩn bị hệ thống định vị C-arm / Navigation; Dự trù 02 đơn vị khối hồng cầu; Chuẩn bị nẹp cổ cứng Philadelphia sau mổ."
            });

            // 5. TRẦN THỊ THU*
            patients.Add(new PatientDocData
            {
                FileName = "5. TRAN THI THU - PT-01.docx",
                HoTen = "TRẦN THỊ THU",
                NgaySinh = "27/02/1963",
                GioiTinh = "Nữ",
                DiaChi = "Xã Mỹ Đức, Huyện Mỹ Đức, Thành phố Hà Nội",
                VaoVien = "23/08/2026 15:24",
                ChanDoan = "Chấn thương cột sống: Vỡ xẹp thân đốt sống L1 do ngã cao (gãy vững kiểu A1) / Tăng huyết áp - Cường giáp đang điều trị duy trì Thyrozol 5mg/ngày",
                TienSu = "Tăng huyết áp đang điều trị; Cường giáp điều trị thường xuyên duy trì Thyrozol 5mg/ngày; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị tai nạn sinh hoạt ngã từ độ cao khoảng 1.5m đập mông xuống sàn cứng lúc 8h ngày 22/08/2026. Sau ngã đau dữ dội vùng cột sống thắt lưng L1, không thể ngồi dậy hay đi lại được, được sơ cứu cố định tại BV Mỹ Đức rồi chuyển Bệnh viện Bạch Mai.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh, GCS 15 điểm, da niêm mạc hồng, thể trạng trung bình, không sốt, HA 130/80 mmHg, Mạch 80 ck/p. Tim đều, tuyến giáp to độ I không thổi, phổi rõ không rales, bụng mềm, ngực chậu vững. Đau chói nhiều vùng gai sau và cạnh sống ngang mức L1 (VAS 4-5/10), co cứng cơ dựng sống thắt lưng, gõ dồn đau chói. Cơ lực 2 chi dưới 5/5, cảm giác bình thường, không tê bì, đại tiểu tiện tự chủ.",
                Cls = "MRI & CT Scanner CSTL: Hình ảnh vỡ xẹp kèm đường vỡ thân đốt sống L1 giảm chiều cao ~30%, phù nề tủy xương thân đốt mới chấn thương (gãy vững, thành sau thân đốt còn nguyên vẹn, không đẩy lồi vào ống sống); Thoái hóa CSTL, phình đĩa đệm L2-S1; Loãng xương. X-quang: CSTL thẳng nghiêng và X-quang ngực thẳng tim phổi bình thường. XN: WBC 10.4 G/L, Neut% 46.9%, HGB 145 g/L, PLT 140 G/L, PT 101%, INR 0.99, APTT 26.0s, Fib 3.41 g/L, Glucose 5.5 mmol/L, Ure 3.6 mmol/L, Creatinin 50 µmol/L, AST 43 U/L, ALT 27 U/L, Na 140, K 3.4, Cl 104, HIV âm tính, HBsAg/HCV âm tính. Siêu âm tim: Chức năng tâm thu thất trái EF 62%, không rối loạn vận động vùng.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống L1 bằng bơm xi măng sinh học có bóng qua da dưới hướng dẫn của C-arm (Kyphoplasty / BXM L1)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau (hoặc Mê tĩnh mạch)",
                PhauThuatVien = "BS.CKII Nguyễn Tuấn Hoành",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Loại II",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng, xi măng sinh học rò rỉ vào ống sống chèn ép thần kinh hoặc rò vào tĩnh mạch gây thuyên tắc mạch phổi, phản vệ dị ứng với thuốc cản quang / xi măng, gãy xẹp đốt sống lân cận thứ phát, tử vong trên bàn mổ.",
                BienPhap = "Kiểm tra bóng tạo hình và độ nhớt của xi măng sinh học dưới màn huỳnh quang tăng sáng C-arm; Bổ sung đo mật độ xương (DEXA) đánh giá mức độ loãng xương; Duy trì thuốc điều trị cường giáp Thyrozol và thuốc huyết áp; Áo nẹp cột sống thắt lưng khi ngồi dậy sau mổ."
            });

            // 6. TRẦN THỊ CHINH
            patients.Add(new PatientDocData
            {
                FileName = "6. TRAN THI CHINH - PT-01.docx",
                HoTen = "TRẦN THỊ CHINH",
                NgaySinh = "01/01/1950",
                GioiTinh = "Nữ",
                DiaChi = "TDP 20, Phường Bình Thuận, TP Tuyên Quang, Tỉnh Tuyên Quang",
                VaoVien = "22/08/2026 14:41",
                ChanDoan = "Xẹp thân đốt sống ngực T12 mới do loãng xương và ngã chấn thương / Viêm đa khớp dạng thấp điều trị mạn tính",
                TienSu = "Tuổi cao (76 tuổi); Viêm đa khớp dạng thấp điều trị duy trì thường xuyên nhiều năm; Loãng xương; Không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân bị tai nạn sinh hoạt ngã cầu thang đập lưng và vùng chẩm xuống nền cứng lúc 12h ngày 22/08/2026. Sau ngã đau chói nhiều vùng cột sống ngực - thắt lưng T12, hạn chế vận động cúi ngửa xoay trở, sưng nề nhẹ đỉnh chẩm phải, được đưa vào BV Bạch Mai cấp cứu và chuyển Khoa CTCH điều trị.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh, GCS 15 điểm, thể trạng già yếu (76 tuổi), da niêm mạc hồng, không sốt, HA 130/75 mmHg, Mạch 78 ck/p. Tim đều, phổi thông khí rõ, bụng mềm, khung chậu vững. Sưng nề nhẹ dưới da vùng đỉnh chẩm phải (đồng tử 2 bên đều 2mm, PXAS dương tính, CT sọ não không tổn thương). Ấn đau chói gai sau và cạnh sống mức đốt sống T12, co cứng cơ cạnh sống, không tự ngồi dậy được do đau. Cơ lực 2 chân 5/5, cảm giác bình thường, không tê bì yếu liệt, đại tiểu tiện tự chủ.",
                Cls = "MRI CSTL: Hình ảnh xẹp thân đốt sống T12 mới, phù nề tủy xương thân đốt mạnh trên T2W/STIR, thành sau thân đốt vững không chèn ép ống sống; Thoái hóa cột sống ngực thắt lưng; Loãng xương. CT Scanner Sọ não: Không thấy ổ tụ máu hay tổn thương nội sọ. X-quang: CSTL thẳng nghiêng và X-quang ngực thẳng tim phổi bóng tim bình thường, cung sườn không gãy. Siêu âm tim: EF 60%, không rối loạn vận động vùng. XN: WBC 7.3 G/L, Neut% 73.1%, HGB 131 g/L, PLT 246 G/L, PT 116%, INR 0.91, APTT 27.9s, Fib 3.11 g/L, Glucose 5.7 mmol/L, Ure 5.2 mmol/L, Creatinin 69 µmol/L, AST 28 U/L, ALT 11 U/L, Na 138, K 4.1, Cl 106, Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống T12 bằng bơm xi măng sinh học có bóng qua da dưới hướng dẫn của C-arm (Kyphoplasty / BXM T12)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau",
                PhauThuatVien = "TS.BS Hoàng Gia Trung",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "II",
                LoaiPhauThuat = "Loại II",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng, xi măng sinh học rò rỉ vào ống sống hoặc tĩnh mạch gây thuyên tắc mạch phổi, dị ứng phản vệ, tụ máu vùng đỉnh chẩm, gãy xẹp đốt sống lân cận thứ phát do loãng xương nặng, tử vong trên bàn mổ.",
                BienPhap = "Theo dõi tri giác và dấu hiệu thần kinh sọ não; Kiểm tra bóng tạo hình và độ nhớt xi măng dưới màn tăng sáng C-arm; Bổ sung đo mật độ xương (DEXA); Phục hồi chức năng và áo nẹp cột sống ngực thắt lưng sau mổ."
            });

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            // Đọc template ra bộ nhớ an toàn (FileShare.ReadWrite)
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

                Console.WriteLine("✔ Đã tạo hoàn hảo biên bản: " + targetFilePath);
            }

            Console.WriteLine("\n=== HOÀN TẤT TẤT CẢ 6 BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ PT-01 ===");
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
