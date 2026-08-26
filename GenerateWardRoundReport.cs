using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WardRoundFormatter
{
    class Program
    {
        static string FormatDate(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length < 8) return raw;
            if (raw.Length >= 12)
            {
                return string.Format("{0}/{1}/{2} {3}:{4}", raw.Substring(6, 2), raw.Substring(4, 2), raw.Substring(0, 4), raw.Substring(8, 2), raw.Substring(10, 2));
            }
            return string.Format("{0}/{1}/{2}", raw.Substring(6, 2), raw.Substring(4, 2), raw.Substring(0, 4));
        }

        static int CalculateAge(string dobStr)
        {
            if (string.IsNullOrEmpty(dobStr) || dobStr.Length < 4) return 0;
            int y = int.Parse(dobStr.Substring(0, 4));
            return 2026 - y;
        }

        static string ResolveDoctorName(string login)
        {
            if (string.IsNullOrEmpty(login)) return "";
            login = login.Trim();
            switch (login.ToLower())
            {
                case "tmd2":
                case "tmd":
                    return "BS. Trịnh Minh Đức (tmd2)";
                case "034727":
                    return "Ths.BS. Nguyễn Hữu Sâm (034727)";
                case "ndh2":
                    return "BS. Nguyễn Đức Hoàng (ndh2)";
                case "vmc":
                    return "BS. Vũ Minh Cường (vmc)";
                case "lvl12":
                    return "BS. Lê Văn Lượng (lvl12)";
                case "ldt":
                    return "BS. Lê Đăng Toàn (ldt)";
                case "dhg":
                    return "BS. Đinh Hoàng Giang (dhg)";
                case "pnt2":
                    return "BS. Phạm Ngọc Thắng (pnt2)";
                case "ddb":
                    return "BS. Đỗ Đăng Bình (ddb)";
                default:
                    return "BS. " + login;
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string text = File.ReadAllText("ward_round_714_712_724_725_data.txt", Encoding.UTF8);

            // Split by patient header
            string[] rawBlocks = Regex.Split(text, @"={50,}\r?\nBỆNH NHÂN\s+(\d+):\s+(.*?)\s+\|\s+BUỒNG:\s+(.*?)\s+\|\s+GIƯỜNG:\s+(.*?)\r?\n={50,}");

            StreamWriter sw = new StreamWriter("ward_round_comprehensive_data.txt", false, Encoding.UTF8);

            for (int i = 1; i < rawBlocks.Length; i += 5)
            {
                string idx = rawBlocks[i].Trim();
                string name = rawBlocks[i + 1].Trim();
                string room = rawBlocks[i + 2].Trim();
                string bed = rawBlocks[i + 3].Trim();
                string block = rawBlocks[i + 4].Trim();

                string pCode = Regex.Match(block, @"Mã BN:\s*([0-9]+)").Groups[1].Value;
                string tCode = Regex.Match(block, @"Mã BA:\s*([0-9]+)").Groups[1].Value;
                string gender = Regex.Match(block, @"Giới tính:\s*(.*?)\s*\|").Groups[1].Value;
                string dobRaw = Regex.Match(block, @"Ngày sinh:\s*([0-9]+)").Groups[1].Value;
                string inTimeRaw = Regex.Match(block, @"Thời gian vào viện:\s*([0-9]+)").Groups[1].Value;
                string icdIn = Regex.Match(block, @"Chẩn đoán vào viện:\s*(.*?)\r?\n").Groups[1].Value;
                string icdCur = Regex.Match(block, @"Chẩn đoán hiện tại:\s*(.*?)\r?\n").Groups[1].Value;
                string icdSub = Regex.Match(block, @"Chẩn đoán kèm theo:\s*(.*?)\r?\n").Groups[1].Value;
                string address = Regex.Match(block, @"Địa chỉ:\s*(.*?)\r?\n").Groups[1].Value;

                int age = CalculateAge(dobRaw);
                string inTimeFmt = FormatDate(inTimeRaw);
                string dobFmt = FormatDate(dobRaw);

                sw.WriteLine("================================================================================");
                sw.WriteLine(string.Format("BỆNH NHÂN #{0}: {1} | {2} - {3}", idx, name, room, bed));
                sw.WriteLine("================================================================================");
                sw.WriteLine(string.Format("• Hành chính: {0} ({1}, {2} tuổi - NS: {3}) | Mã BN: {4} | Mã BA: {5}", name, gender, age, dobFmt, pCode, tCode));
                sw.WriteLine(string.Format("• Quê quán: {0}", address));
                sw.WriteLine(string.Format("• Ngày giờ vào viện: {0}", inTimeFmt));
                sw.WriteLine(string.Format("• Chẩn đoán hiện tại: {0}", icdCur));
                if (!string.IsNullOrEmpty(icdSub) && icdSub.Trim() != "-")
                    sw.WriteLine(string.Format("• Chẩn đoán kèm theo: {0}", icdSub));
                sw.WriteLine(string.Format("• Chẩn đoán lúc vào: {0}", icdIn));

                // Latest Trackings
                var trkMatches = Regex.Matches(block, @"\[TỜ ĐIỀU TRỊ:\s*(\d+)\]\s*Phòng:\s*(.*?)\s*\|\s*Bác sĩ:\s*(.*?)\r?\n(.*?)(?=\n\s*\[TỜ ĐIỀU TRỊ:|\n---|\Z)", RegexOptions.Singleline);
                sw.WriteLine("\n--- DIỄN BIẾN LÂM SÀNG & TỜ ĐIỀU TRỊ GẦN NHẤT ---");
                if (trkMatches.Count > 0)
                {
                    int startT = Math.Max(0, trkMatches.Count - 2);
                    for (int t = startT; t < trkMatches.Count; t++)
                    {
                        var tm = trkMatches[t];
                        string tTime = FormatDate(tm.Groups[1].Value);
                        string tRoom = tm.Groups[2].Value.Trim();
                        string tDoc = ResolveDoctorName(tm.Groups[3].Value.Trim());
                        string tContent = tm.Groups[4].Value.Trim();
                        sw.WriteLine(string.Format("  [Tờ điều trị lúc {0} - {1} ({2})]", tTime, tDoc, tRoom));
                        sw.WriteLine("    " + tContent.Replace("\r\n", "\n    "));
                    }
                }
                else
                {
                    sw.WriteLine("  (Chưa có tờ điều trị ghi nhận)");
                }

                // Key Surgeries / Interventions & Imaging
                sw.WriteLine("\n--- PHẪU THUẬT, THỦ THUẬT & HÌNH ẢNH (CT, MRI, X-QUANG, SIÊU ÂM) ---");
                var ssLines = Regex.Matches(block, @"•\s*\[(\d+)\]\s*\[(.*?)\]\s*(.*?)(?=\r?\n\s*•|\r?\n---|\r?\n=|\Z)", RegexOptions.Singleline);
                List<string> surgList = new List<string>();
                List<string> imgList = new List<string>();
                List<string> consultList = new List<string>();

                foreach (Match m in ssLines)
                {
                    string sTime = FormatDate(m.Groups[1].Value);
                    string sType = m.Groups[2].Value.Trim();
                    string sContent = m.Groups[3].Value.Trim().Replace("\r\n", " ");

                    if (sType == "Phẫu thuật" || sType == "Thủ thuật")
                    {
                        surgList.Add(string.Format("[{0}] [{1}] {2}", sTime, sType, sContent));
                    }
                    else if (sType == "Chẩn đoán hình ảnh" || sType == "Siêu âm" || sType == "Thăm dò chức năng")
                    {
                        imgList.Add(string.Format("[{0}] [{1}] {2}", sTime, sType, sContent));
                    }
                }

                if (surgList.Count > 0)
                {
                    sw.WriteLine("  * PHẪU THUẬT / THỦ THUẬT / HỘI CHẨN:");
                    foreach (var s in surgList) sw.WriteLine("    • " + s);
                }
                if (imgList.Count > 0)
                {
                    sw.WriteLine("  * HÌNH ẢNH & THĂM DÒ CHỨC NĂNG:");
                    foreach (var img in imgList) sw.WriteLine("    • " + img);
                }

                // Detailed Key Labs
                sw.WriteLine("\n--- KẾT QUẢ XÉT NGHIỆM ĐÁNG CHÚ Ý ---");
                var teinSection = Regex.Match(block, @"--- KẾT QUẢ XÉT NGHIỆM CHI TIẾT ---\r?\n(.*?)--- CÁC PHIẾU Y LỆNH", RegexOptions.Singleline);
                if (teinSection.Success)
                {
                    var lines = teinSection.Groups[1].Value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> labItems = new List<string>();
                    foreach (var l in lines)
                    {
                        if (l.Contains("WBC") || l.Contains("HGB") || l.Contains("PLT") || l.Contains("NEUT") ||
                            l.Contains("PT") || l.Contains("APTT") || l.Contains("Fibrinogen") || l.Contains("INR") ||
                            l.Contains("Creatinin") || l.Contains("Urê") || l.Contains("Ure") || l.Contains("Glucose") ||
                            l.Contains("AST") || l.Contains("ALT") || l.Contains("Natri") || l.Contains("Kali") || l.Contains("Clo") ||
                            l.Contains("CRP") || l.Contains("Procalcitonin") || l.Contains("Troponin") || l.Contains("đường máu mao mạch") ||
                            l.Contains("HBsAg") || l.Contains("HCV") || l.Contains("HIV") || l.Contains("Albumin") || l.Contains("Protein") ||
                            l.Contains("Vancomycin"))
                        {
                            labItems.Add(l.Trim());
                        }
                    }

                    // Group by test and keep last 2 results
                    var grouped = labItems.GroupBy(x => {
                        var m = Regex.Match(x, @"\)\s*(.*?)\s*\(");
                        return m.Success ? m.Groups[1].Value.Trim() : x;
                    });

                    foreach (var g in grouped)
                    {
                        var list = g.ToList();
                        int startIdx = Math.Max(0, list.Count - 2);
                        for (int k = startIdx; k < list.Count; k++)
                        {
                            sw.WriteLine("  • " + list[k]);
                        }
                    }
                }
                else
                {
                    sw.WriteLine("  (Chưa có kết quả xét nghiệm)");
                }

                // Recent Prescriptions
                sw.WriteLine("\n--- ĐƠN THUỐC & Y LỆNH HIỆN TẠI ---");
                var medSection = Regex.Match(block, @"--- CHI TIẾT THUỐC ĐÃ KÊ / XUẤT DƯỢC ---\r?\n(.*?)==========================================================================", RegexOptions.Singleline);
                if (medSection.Success)
                {
                    var mLines = medSection.Groups[1].Value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    // Filter unique recent medicines
                    var uMeds = mLines.GroupBy(x => {
                        var m = Regex.Match(x, @"\]\s*(.*?)\s*\(");
                        return m.Success ? m.Groups[1].Value.Trim() : x;
                    }).Select(g => g.First()).ToList();

                    foreach (var m in uMeds.Take(12))
                    {
                        sw.WriteLine("  • " + m.Trim());
                    }
                }
                else
                {
                    sw.WriteLine("  (Chưa có danh sách thuốc)");
                }

                sw.WriteLine("\n\n");
            }

            sw.Close();
            Console.WriteLine("Comprehensive data generated successfully with resolved doctor names!");
        }
    }
}
