using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GeneratePT01_PhungMinhHa
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string templateDocx = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01.docx";
            string outputDir = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01";
            string targetFilePath = Path.Combine(outputDir, "PHUNG MINH HA - PT-01.docx");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var pData = new
            {
                HoTen = "PHÙNG MINH HÀ",
                NgaySinh = "08/04/1985",
                GioiTinh = "Nam",
                DiaChi = "Phường Phương Liên, Quận Đống Đa, Thành phố Hà Nội",
                VaoVien = "24/08/2026 11:54",
                ChanDoan = "Gãy kín đầu dưới xương mác / mắt cá ngoài cổ chân phải do ngã chấn thương [S82.80]",
                TienSu = "Khỏe mạnh, không có tiền sử bệnh lý mạn tính, không có tiền sử dị ứng thuốc.",
                BenhSu = "Khoảng 18h ngày 18/08/2026 bệnh nhân đi bộ bị trượt ngã, sau ngã đau nhiều và hạn chế vận động cổ chân phải, sưng nề mặt ngoài cổ chân. Bệnh nhân điều trị nội khoa tại nhà không đỡ, ngày 24/08/2026 đến khám tại Trung tâm Cấp cứu A9 Bệnh viện Bạch Mai, được chụp CLVT và chuyển Khoa CTCH & Cột sống phẫu thuật.",
                ThoiGianHoiChan = "24/08/2026",
                TomTat = "Bệnh nhân tỉnh táo, tiếp xúc tốt (GCS 15 điểm), da niêm mạc hồng, thể trạng trung bình (cao 159cm, nặng 66kg), không sốt, HA 130/80 mmHg, Mạch 80 ck/p, SpO2 98%. Tim đều rõ, phổi thông khí tốt không rales, bụng mềm, ngực chậu vững. Cổ chân phải sưng nề, bầm tím nhẹ, ấn đau chói mắt cá ngoài chân phải (VAS 3-4 điểm), hạn chế vận động gấp duỗi cổ chân do đau. Đầu chi hồng ấm, mạch mu chân và mạch chày sau bắt rõ, cảm giác và vận động các ngón chân bình thường, không tê bì liệt.",
                Cls = "CLVT & X-quang cổ chân phải: Hình ảnh đường gãy đầu dưới xương mác / mắt cá ngoài cổ chân phải di lệch nhẹ, phù nề phần mềm xung quanh. X-quang ngực thẳng: Tim phổi bình thường. Siêu âm ổ bụng & Siêu âm tim: Bình thường. XN máu: WBC 8.0 G/L, Neut% 68.7%, HGB 147 g/L, PLT 258 G/L; PT 99%, INR 1.01, APTT 33.7s, Fib 2.84 g/L; Glucose 5.4 mmol/L, Ure 6.4 mmol/L, Creatinin 79 µmol/L, AST 20 U/L, ALT 12 U/L; Na 139, K 3.7, Cl 103; HIV âm tính, HBsAg âm tính, HCV âm tính; Nước tiểu bình thường.",
                Pppt = "Phẫu thuật mở nắn chỉnh và kết hợp xương mắt cá ngoài / đầu dưới xương mác cổ chân phải bằng nẹp vít (KHX)",
                VoCam = "Gây tê tủy sống (hoặc Gây tê đám rối thần kinh / Gây mê nội khí quản)",
                PhauThuatVien = "BS Giang",
                NgayMo = "ngày 25 tháng 08 năm 2026",
                Mallampati = "I",
                LoaiPhauThuat = "Loại I",
                Asa = "I",
                NguyCo = "Sạch",
                BienChung = "Chảy máu, nhiễm trùng vết mổ, tổn thương thần kinh mác nông / thần kinh bì mu chân, chèn ép khoang, chậm liền xương, khớp giả, bung lỏng nẹp vít, hội chứng đau vùng phức hợp (CRPS), dị ứng thuốc, tử vong trên bàn mổ.",
                BienPhap = "Kháng sinh dự phòng theo phác đồ trước rạch da 30 phút; Chuẩn bị bộ nẹp vít giải phẫu mắt cá ngoài và màn huỳnh quang tăng sáng C-arm; Bất động nẹp bột cẳng bàn chân tư thế cơ năng sau mổ; Hướng dẫn kê cao chân và tập vận động ngón chân sớm tránh phù nề và huyết khối tĩnh mạch sâu."
            };

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            byte[] templateBytes;
            using (var fs = new FileStream(templateDocx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                templateBytes = ms.ToArray();
            }

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

                // Duyệt qua các đoạn văn bản
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

                // Cập nhật các ô đánh dấu trong Bảng đánh giá GMHS (Table 1)
                var tables = doc.Descendants(w + "tbl").ToList();
                if (tables.Count >= 2)
                {
                    var tbl1 = tables[1];
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

            Console.WriteLine("✔ ĐÃ TẠO THÀNH CÔNG BIÊN BẢN PT-01 CHO BN: " + targetFilePath);
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
