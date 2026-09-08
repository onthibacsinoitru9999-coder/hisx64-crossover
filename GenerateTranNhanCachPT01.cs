using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GenerateTranNhanCachPT01
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
        public string LoaiPhauThuat { get; set; }
        public string Asa { get; set; }
        public string Mallampati { get; set; }
        public string NguyCo { get; set; }
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

            PatientDocData pData = new PatientDocData
            {
                FileName = "5. TRAN NHAN CACH - PT-01.docx",
                HoTen = "TRẦN NHÂN CÁCH",
                NgaySinh = "10/11/1978",
                GioiTinh = "Nam",
                DiaChi = "Xã Đại Xuyên, Thành phố Hà Nội",
                VaoVien = "04/09/2026 10:17",
                ChanDoan = "Sau mổ giải ép lấy thoát vị đĩa đệm L4-L5 cách 3 tuần / Thoát vị đĩa đệm cột sống thắt lưng đa tầng L3-L4, L4-L5, L5-S1 [M51.1]",
                TienSu = "Tiền sử phẫu thuật chấn thương sọ não cách đây 6 năm; tiền sử mổ mở giải ép lấy nhân thoát vị đĩa đệm L4-L5 cách đây 3 tuần tại Bệnh viện Bạch Mai; không có bệnh lý nội khoa mạn tính (không tăng huyết áp, không đái tháo đường); không có tiền sử dị ứng thuốc.",
                BenhSu = "Bệnh nhân nam 48 tuổi, có tiền sử mổ mở giải ép thoát vị đĩa đệm L4-L5 cách đây 3 tuần, sau mổ đỡ đau được một thời gian ngắn. Khoảng vài ngày gần đây bệnh nhân xuất hiện đau tái phát dữ dội vùng cột sống thắt lưng, đau buốt chói lan dọc mông xuống mặt sau ngoài đùi, cẳng chân và bàn chân trái (theo rễ L5), mức độ đau nhiều (VAS 7-8 điểm), kèm tê bì nhiều chân trái, hạn chế đi lại và cúi ngửa, dùng các thuốc giảm đau không đỡ. Bệnh nhân xin nhập viện Khoa Chấn thương Chỉnh hình và Cột sống - Bệnh viện Bạch Mai để hội chẩn và phẫu thuật tiếp.",
                ThoiGianHoiChan = "08/09/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), da niêm mạc hồng, không phù, không sốt (NĐ 36.8°C), thể trạng trung bình. Huyết động ổn định: Mạch 76 ck/p, Huyết áp 120/80 mmHg, SpO2 98%. Tim đều rõ, phổi thông khí tốt không rales, bụng mềm, khung chậu vững. Khám chuyên khoa Cột sống: Vùng thắt lưng có vết mổ cũ đường giữa dài ~4cm ngang mức L4-L5 đã liền sẹo tốt khô sạch, ấn đau chói cạnh sống L4-L5 bên trái (VAS 7-8 điểm), co cứng nhẹ khối cơ cạnh sống thắt lưng, hạn chế biên độ vận động cúi - ngửa - nghiêng cột sống thắt lưng. Dấu hiệu căng rễ (Lasègue) bên trái (+) 45 độ, bấm chuông cạnh sống L4-L5 lệch trái lan xuống chân (+). Cảm giác nông: Tê bì và giảm cảm giác mặt ngoài cẳng chân và mu bàn chân trái (vùng rễ L5). Vận động: Cơ lực chi trên 5/5, cơ lực chi dưới: bên phải 5/5, bên trái cơ gấp mu bàn chân và ngón cái 4/5 do đau. Phản xạ gân bánh chè và gân gót 2 bên bình thường cân đối. Phản xạ bệnh lý bó tháp: Hoffmann (-), Babinski (-). Không rối loạn cơ tròn (tiểu tiện tự chủ). Dị ứng da nhẹ 2 đùi đã khám chuyên khoa Da liễu, dùng thuốc bôi ngoài da ổn định.",
                Cls = "MRI cột sống thắt lưng - cùng: Hình ảnh khuyết cung sau L4-5 sau phẫu thuật cũ; thoát vị đĩa đệm L4-L5 tái phát/tồn dư lệch trái chèn ép ngách bên và rễ thần kinh L5 trái; thoái hóa đĩa đệm rải rác tầng L3-4, L5-S1; hẹp ống sống thắt lưng tương đối tầng L4-5; không thấy tụ dịch hay nhiễm trùng vùng mổ cũ. X-quang cột sống thắt lưng L5-S1 thẳng - nghiêng & động gập - ưỡn: Thoái hóa cột sống thắt lưng, hẹp khoang gian đốt L4-L5, mất vững cột sống nhẹ tầng L4-L5 trên phim động gập ưỡn. X-quang ngực thẳng: Tim phổi bình thường. Bilan XN máu: WBC 5.56 G/L, Neut% 62.4%, HGB 122 g/L, RBC 3.89 T/L, HCT 0.365 L/L, PLT 176 G/L; PT% 99%, INR 1.01, APTT 31.4s, Fibrinogen 5.09 g/L; Glucose 5.8 mmol/L, Ure 6.2 mmol/L, Creatinin 85 µmol/L, AST 28 U/L, ALT 32 U/L; Điện giải: Na 137, K 3.8, Cl 103 mmol/L; Nhóm máu B Rh(+); HIV âm tính, HBsAg âm tính, HCV âm tính; Tổng phân tích nước tiểu bình thường.",
                Pppt = "Phẫu thuật hàn xương liên thân đốt thắt lưng qua lỗ liên hợp ít xâm lấn (MIS-TLIF L4-L5), lấy nhân đĩa đệm giải ép thần kinh và cố định cột sống thắt lưng bằng nẹp vít qua cuống qua da (MIS-TLIF)",
                VoCam = "Gây mê nội khí quản",
                PhauThuatVien = "BS Nguyễn Đức Hoàng",
                NgayMo = "ngày 09 tháng 09 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "II",
                NguyCo = "Sạch",
                BienChung = "Rách màng cứng gây rò dịch não tủy (nguy cơ tăng cao do mổ lại trên nền mô xơ dính), tổn thương rễ thần kinh L4, L5, S1 gây liệt vận động hoặc rối loạn cơ tròn, chảy máu đám rối tĩnh mạch ngoài màng cứng hoặc tụ máu chèn ép khoang tủy sau mổ, lỏng hoặc gãy nẹp vít xê dịch lồng hàn xương (cage), không liền xương (khớp giả), nhiễm trùng vết mổ nông/sâu hoặc viêm đĩa đệm đốt sống, thuyên tắc huyết khối tĩnh mạch sâu chi dưới và thuyên tắc phổi (DVT/PE), phản vệ dị ứng thuốc mê/kháng sinh, tử vong chu phẫu.",
                BienPhap = "Kháng sinh dự phòng phổ rộng đường tĩnh mạch trước rạch da 30 phút; Chuẩn bị hệ thống ống banh nong vi phẫu ít xâm lấn (tubular retractor), kính lúp vi phẫu / kính hiển vi phẫu thuật; Sử dụng màn huỳnh quang tăng sáng C-arm đa bình diện để kiểm soát chính xác vị trí bắt vít qua cuống L4-L5 và đặt lồng liên thân đốt; Chuẩn bị dụng cụ bóc tách mô sẹo tỉ mỉ, sáp xương cầm máu, keo sinh học và chỉ Prolene 5-0 vá màng cứng nếu có rách màng cứng; Đặt dẫn lưu kín vết mổ áp lực âm theo dõi chảy máu; Bổ sung dịch dinh dưỡng trước mổ Leanpro PreSur 12.5% theo phác đồ ERAS (tối 4 chai lúc 20h, sáng 2 chai lúc 6h); Đeo đai lưng cố định và tập phục hồi chức năng vận động sớm sau mổ."
            };

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            string[] targetFiles = new string[]
            {
                Path.Combine(outputDir, pData.FileName),
                Path.Combine(outputDir, "TRAN NHAN CACH - PT-01.docx")
            };

            foreach (var targetFilePath in targetFiles)
            {
                string modifiedDocXml = "";

                using (var srcZip = ZipFile.OpenRead(templateDocx))
                {
                    var entry = srcZip.GetEntry("word/document.xml");
                    using (var r = new StreamReader(entry.Open(), Encoding.UTF8))
                    {
                        XDocument doc = XDocument.Parse(r.ReadToEnd(), LoadOptions.PreserveWhitespace);

                        foreach (var p in doc.Descendants(w + "p"))
                        {
                            string pText = string.Concat(p.Descendants(w + "t").Select(t => t.Value));

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
                                    new XElement(w + "t", "Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt: ")
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

                        var tables = doc.Descendants(w + "tbl").ToList();
                        if (tables.Count >= 2)
                        {
                            var tbl1 = tables[1];
                            var rows = tbl1.Elements(w + "tr").ToList();

                            if (rows.Count > 0)
                            {
                                var cells = rows[0].Elements(w + "tc").ToList();
                                SetCellText(cells, 1, pData.Mallampati == "I" ? "x" : "", w);
                                SetCellText(cells, 3, pData.Mallampati == "II" ? "x" : "", w);
                                SetCellText(cells, 5, pData.Mallampati == "III" ? "x" : "", w);
                                SetCellText(cells, 7, pData.Mallampati == "IV" ? "x" : "", w);
                            }

                            if (rows.Count > 1)
                            {
                                var cells = rows[1].Elements(w + "tc").ToList();
                                SetCellText(cells, 1, pData.LoaiPhauThuat == "Đặc biệt" ? "x" : "", w);
                                SetCellText(cells, 3, pData.LoaiPhauThuat == "Loại I" ? "x" : "", w);
                                SetCellText(cells, 5, pData.LoaiPhauThuat == "Loại II" ? "x" : "", w);
                                SetCellText(cells, 7, pData.LoaiPhauThuat == "Loại III" ? "x" : "", w);
                            }

                            if (rows.Count > 2)
                            {
                                var cells = rows[2].Elements(w + "tc").ToList();
                                SetCellText(cells, 1, pData.Asa == "I" ? "x" : "", w);
                                SetCellText(cells, 3, pData.Asa == "II" ? "x" : "", w);
                                SetCellText(cells, 5, pData.Asa == "III" ? "x" : "", w);
                                SetCellText(cells, 7, pData.Asa == "IV" ? "x" : "", w);
                                SetCellText(cells, 9, pData.Asa == "V" ? "x" : "", w);
                            }

                            if (rows.Count > 3)
                            {
                                var cells = rows[3].Elements(w + "tc").ToList();
                                SetCellText(cells, 1, pData.NguyCo == "Sạch" ? "x" : "", w);
                                SetCellText(cells, 3, pData.NguyCo == "Sạch nhiễm" ? "x" : "", w);
                                SetCellText(cells, 5, pData.NguyCo == "Nhiễm" ? "x" : "", w);
                                SetCellText(cells, 7, pData.NguyCo == "Bẩn" ? "x" : "", w);
                            }
                        }

                        using (var ms = new MemoryStream())
                        {
                            using (var writer = new StreamWriter(ms, new UTF8Encoding(false)))
                            {
                                doc.Save(writer, SaveOptions.DisableFormatting);
                            }
                            modifiedDocXml = Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                }

                if (File.Exists(targetFilePath)) File.Delete(targetFilePath);

                using (var srcZip = ZipFile.OpenRead(templateDocx))
                using (var destFile = new FileStream(targetFilePath, FileMode.Create))
                using (var destZip = new ZipArchive(destFile, ZipArchiveMode.Create))
                {
                    foreach (var entry in srcZip.Entries)
                    {
                        var newEntry = destZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                        using (var destStream = newEntry.Open())
                        {
                            if (entry.FullName == "word/document.xml")
                            {
                                byte[] bytes = new UTF8Encoding(false).GetBytes(modifiedDocXml);
                                destStream.Write(bytes, 0, bytes.Length);
                            }
                            else
                            {
                                using (var srcStream = entry.Open())
                                {
                                    srcStream.CopyTo(destStream);
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("✔ Đã tạo hoàn tất biên bản PT-01: " + targetFilePath);
            }

            Console.WriteLine("\n=== TẠO THÀNH CÔNG BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ PT-01 CHO TRẦN NHÂN CÁCH ===");
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
