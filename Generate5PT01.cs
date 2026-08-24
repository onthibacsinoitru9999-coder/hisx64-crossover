using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

namespace GeneratePT01
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
        public string LoaiPhauThuat { get; set; } // Dac biet, Loai I, Loai II, Loai III
        public string Asa { get; set; } // I, II, III
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

            // BN 1: HOÀNG HỮU CƯỜNG
            patients.Add(new PatientDocData
            {
                FileName = "1. HOANG HUU CUONG - PT-01.docx",
                HoTen = "HOÀNG HỮU CƯỜNG",
                NgaySinh = "27/07/1975",
                GioiTinh = "Nam",
                DiaChi = "Thôn Nà Nàng, Phường Bắc Kạn, TP Thái Nguyên",
                VaoVien = "06/08/2026 21:18",
                ChanDoan = "Vỡ lún L3 kiểu A(D), gãy xương gót 2 bên / Gãy xương đòn (P) đã KHX, viêm phổi, trầm cảm, lao phổi cũ, thiếu máu",
                TienSu = "Lao phổi cũ điều trị 15 năm trước; Gãy xương đòn phải đã phẫu thuật KHX nẹp vít ngày 04/08/2026; Rối loạn trầm cảm ngắn",
                BenhSu = "Bệnh nhân ngã cao ngày 06/08/2026 sau chấn thương đau dữ dội thắt lưng và 2 gót chân, hạn chế vận động, bí tiểu, đã điều trị nội khoa hô hấp, tâm thần và bất động tại BV Bạch Mai, nay chuyển Khoa CTCH phẫu thuật.",
                ThoiGianHoiChan = "21/08/2026",
                TomTat = "Bệnh nhân tỉnh, tiếp xúc tốt, hết sốt, HA 114/81 mmHg, Mạch 110 l/p, SpO2 99%. Tim đều, phổi thông khí cải thiện không rales, bụng mềm. Đau nhiều cột sống L3, sưng đau 2 gót chân đang bó bột. Cơ lực 2 chân 4/5, cảm giác bình thường.",
                Cls = "MRI/CT: Vỡ lún thân đốt sống L3 giảm chiều cao, vỡ tường sau đẩy nhẹ vào ống sống; X-quang: Vỡ xương gót 2 bên phức tạp, gãy xương sườn phải, nẹp vít xương đòn phải vững. XN: WBC 6.5 G/L, HGB 101 g/L, PLT 870 G/L, PT 55%, INR 1.55, APTT 38.6s, Creatinin 81 µmol/L, Glucose 7.7 mmol/L, Nhóm máu B Rh(+).",
                Pppt = "Phẫu thuật mở nắn chỉnh, cố định cột sống thắt lưng bằng nẹp vít qua cuống, giải ép bản sống L3 - Kết hợp xương gót 2 bên",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "Bs Cường H",
                NgayMo = "ngày 21 tháng 08 năm 2026",
                LoaiPhauThuat = "Đặc biệt",
                Asa = "III"
            });

            // BN 2: NGUYỄN THỊ SỆT
            patients.Add(new PatientDocData
            {
                FileName = "2. NGUYEN THI SET - PT-01.docx",
                HoTen = "NGUYỄN THỊ SỆT",
                NgaySinh = "20/03/1954",
                GioiTinh = "Nữ",
                DiaChi = "Xã Thượng Phúc, Thành phố Hà Nội",
                VaoVien = "20/08/2026 07:57",
                ChanDoan = "Xẹp đốt sống L3-L4 do loãng xương (T-score -4.6)",
                TienSu = "Loãng xương nặng (T-score -4.6), không có tiền sử dị ứng thuốc",
                BenhSu = "Bệnh nhân đau vùng cột sống thắt lưng tăng dần 1 tháng nay, đau lan xuống đùi và chân 2 bên kèm tê bì, đau tăng khi ngồi dậy và đi lại, điều trị tuyến dưới không đỡ, vào viện xin phẫu thuật.",
                ThoiGianHoiChan = "21/08/2026",
                TomTat = "Bệnh nhân tỉnh, tiếp xúc tốt, da niêm mạc hồng, không sốt, HA 120/75 mmHg, Mạch 76 l/p. Tim đều, phổi rõ, bụng mềm, ngực vững. Đau chói gai sau L3, L4, co cứng cơ cạnh sống, VAS 4/10. Cơ lực 2 chân 5/5, không rối loạn cơ tròn.",
                Cls = "MRI CSDL: Xẹp phù nề tủy xương thân đốt sống L3 và L4, thoái hóa cột sống thắt lưng; X-quang: Xẹp L3-L4, loãng xương nặng (T-score -4.6). XN: PT 95%, INR 1.04, APTT 26.9s, Fibrinogen 4.39 g/L, Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính, X-quang ngực thẳng tim phổi bình thường.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống L3, L4 bằng bơm xi măng sinh học có bóng qua da (Kyphoplasty / BXM L3, L4)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau",
                PhauThuatVien = "Bs Cường H",
                NgayMo = "ngày 21 tháng 08 năm 2026",
                LoaiPhauThuat = "Loại II",
                Asa = "II"
            });

            // BN 3: PHẠM THÙY DUNG
            patients.Add(new PatientDocData
            {
                FileName = "3. PHAM THUY DUNG - PT-01.docx",
                HoTen = "PHẠM THÙY DUNG",
                NgaySinh = "01/06/1991",
                GioiTinh = "Nữ",
                DiaChi = "Xã Định Hóa, Tỉnh Thái Nguyên",
                VaoVien = "18/08/2026 07:34",
                ChanDoan = "Ngón tay lò xo (ngón tay cò súng) ngón I tay Trái / Viêm gân gấp ngón I tay Trái điều trị nội khoa không đáp ứng",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính",
                BenhSu = "Bệnh nhân đau tức và kẹt cứng ngón I bàn tay trái nhiều tuần nay, khi gấp vào khó duỗi ra, duỗi có tiếng bật và đau chói tại khớp bàn ngón, đã điều trị nội khoa không đỡ, vào viện xin phẫu thuật.",
                ThoiGianHoiChan = "21/08/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, HA 115/70 mmHg, Mạch 72 l/p. Tim đều, phổi thông khí tốt, bụng mềm. Gốc ngón I bàn tay trái sờ thấy nốt xơ dày bao gân pully A1 ấn đau tức, dấu hiệu ngón tay lò xo dương tính rõ khi gấp duỗi. Vận động ngón II-V bình thường, mạch quay trụ rõ.",
                Cls = "Siêu âm bàn tay: Dày bao gân và ổ giảm âm quanh gân gấp ngón I tay trái. XN: WBC 5.7 G/L, HGB 146 g/L, PLT 240 G/L, PT 95%, INR 1.04, APTT 31.0s, Fibrinogen 3.50 g/L, Glucose 5.3 mmol/L, Creatinin 54 µmol/L, AST 34 U/L, ALT 33 U/L, Nhóm máu O Rh(+), HIV/HBsAg/HCV âm tính, X-quang ngực thẳng và xương bàn tay bình thường.",
                Pppt = "Phẫu thuật mở cắt giải phóng hãm gân gấp (ròng rọc pully A1) ngón I bàn tay trái",
                VoCam = "Gây tê tại chỗ",
                PhauThuatVien = "Bs Cường H",
                NgayMo = "ngày 21 tháng 08 năm 2026",
                LoaiPhauThuat = "Loại III",
                Asa = "I"
            });

            // BN 4: TRẦN VĂN HẢI
            patients.Add(new PatientDocData
            {
                FileName = "4. TRAN VAN HAI - PT-01.docx",
                HoTen = "TRẦN VĂN HẢI",
                NgaySinh = "07/08/1981",
                GioiTinh = "Nam",
                DiaChi = "Xã Bình Lục, Tỉnh Ninh Bình",
                VaoVien = "20/08/2026 05:36",
                ChanDoan = "U bao rễ thần kinh (theo dõi Schwannoma) vùng khuỷu tay phải",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính",
                BenhSu = "Bệnh nhân phát hiện khối u vùng khuỷu tay phải nhiều năm nay to dần, gần đây đau tức và tê bì dọc mặt trong cẳng tay lan xuống ngón IV, V bàn tay phải, vào viện khám và điều trị phẫu thuật.",
                ThoiGianHoiChan = "21/08/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, HA 120/75 mmHg, Mạch 74 l/p. Tim đều, phổi rõ, bụng mềm. Mặt trong khuỷu tay phải có khối u kích thước ~2x3cm, mật độ chắc, ranh giới rõ, gõ có dấu hiệu Tinel tê dọc thần kinh trụ xuống ngón IV, V. Vận động khuỷu, cổ bàn ngón tay tốt.",
                Cls = "MRI Khuỷu tay phải: Hình ảnh khối u phần mềm ranh giới rõ nằm dọc đường đi dây thần kinh trụ khuỷu tay phải (TD Schwannoma). XN: WBC 6.86 G/L, HGB 135 g/L, PLT 265 G/L, PT 96%, INR 1.03, APTT 32.5s, Glucose 5.5 mmol/L, Creatinin 80 µmol/L, AST 20 U/L, ALT 11 U/L, Nhóm máu AB Rh(+), HIV/HBsAg/HCV âm tính, X-quang ngực thẳng bình thường.",
                Pppt = "Phẫu thuật mở bóc u bao dây thần kinh vùng khuỷu tay phải dưới kính vi phẫu/lúp phóng đại, bảo tồn thân dây thần kinh, gửi GPB",
                VoCam = "Gây tê đám rối thần kinh cánh tay (hoặc Mê tĩnh mạch/Mask thanh quản)",
                PhauThuatVien = "Bs Cường H",
                NgayMo = "ngày 21 tháng 08 năm 2026",
                LoaiPhauThuat = "Loại II",
                Asa = "I"
            });

            // BN 5: TRẦN THỊ TƠM
            patients.Add(new PatientDocData
            {
                FileName = "5. TRAN THI TOM - PT-01.docx",
                HoTen = "TRẦN THỊ TƠM",
                NgaySinh = "05/07/1940",
                GioiTinh = "Nữ",
                DiaChi = "Tổ Dân Phố Thạch Hạ, Phường Trần Phú, Tỉnh Hà Tĩnh",
                VaoVien = "20/08/2026 06:25",
                ChanDoan = "Xẹp đốt sống L1, T10 do loãng xương (T-score -4.6), phình đĩa đệm L3-L4, L4-L5, L5-S1, hẹp ống sống / Tăng huyết áp",
                TienSu = "Tuổi cao (86 tuổi), Tăng huyết áp đang điều trị, Loãng xương nặng (T-score -4.6)",
                BenhSu = "Bệnh nhân bị ngã đập mông xuống nền cứng cách vào viện 5 ngày, sau ngã đau dữ dội vùng cột sống ngực - thắt lưng, đau tăng chói khi ngồi dậy hoặc cử động, không tự đi lại được, điều trị tuyến tỉnh không đỡ, chuyển Bạch Mai.",
                ThoiGianHoiChan = "21/08/2026",
                TomTat = "Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng già yếu (86 tuổi), không sốt, HA 130/80 mmHg, Mạch 78 l/p, SpO2 97%. Tim đều, phổi thông khí rõ, bụng mềm, đại tiểu tiện tự chủ. Ấn đau chói gai sau T10 và L1, co cứng cơ cạnh sống, hạn chế vận động do đau. Cơ lực 2 chân 5/5, cảm giác bình thường.",
                Cls = "MRI/X-quang CSTL: Xẹp phù nề mới thân đốt sống L1 và T10; Phình đĩa đệm đa tầng L3-S1 gây hẹp ống sống; Loãng xương nặng (T-score -4.6). XN: WBC 6.42 G/L, HGB 119 g/L, PLT 302 G/L, PT 119%, INR 0.90, APTT 21.1s, Fibrinogen 4.51 g/L, Glucose 5.5 mmol/L, Ure 9.9, Creatinin 83 µmol/L, Nhóm máu B Rh(+), HIV/HBsAg/HCV âm tính, X-quang ngực thẳng tim phổi bình thường.",
                Pppt = "Phẫu thuật tạo hình thân đốt sống T10, L1 bằng bơm xi măng sinh học có bóng qua da (Kyphoplasty / BXM T10, L1)",
                VoCam = "Gây tê tại chỗ kết hợp tiền mê giảm đau",
                PhauThuatVien = "Bs Cường H",
                NgayMo = "ngày 21 tháng 08 năm 2026",
                LoaiPhauThuat = "Loại II",
                Asa = "II"
            });

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            foreach (var pData in patients)
            {
                string targetFilePath = Path.Combine(outputDir, pData.FileName);
                File.Copy(templateDocx, targetFilePath, true);

                using (ZipArchive archive = ZipFile.Open(targetFilePath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry docEntry = archive.GetEntry("word/document.xml");
                    string docXmlText = "";
                    using (var reader = new StreamReader(docEntry.Open(), Encoding.UTF8))
                    {
                        docXmlText = reader.ReadToEnd();
                    }

                    XDocument doc = XDocument.Parse(docXmlText);

                    // 1. Duyet qua cac doan van de sua
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
                    }

                    // Ghi de vao docx
                    docEntry.Delete();
                    ZipArchiveEntry newDocEntry = archive.CreateEntry("word/document.xml");
                    using (var writer = new StreamWriter(newDocEntry.Open(), Encoding.UTF8))
                    {
                        doc.Save(writer);
                    }
                }

                Console.WriteLine("Tao thanh cong: " + targetFilePath);
            }

            Console.WriteLine("=== HOAN TAT XUAT 5 FILE VAO FOLDER PT-01 ===");
        }
    }
}
