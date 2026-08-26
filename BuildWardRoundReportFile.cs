using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string mdPath = @"e:\his-x64-28-11fix GDYK\his-x64\Bao_Cao_Di_Buong_Khoa57_2026_08_26.md";
        string htmlPath = @"e:\his-x64-28-11fix GDYK\his-x64\Bao_Cao_Di_Buong_Khoa57_2026_08_26.html";

        string mdContent = File.ReadAllText(@"e:\his-x64-28-11fix GDYK\his-x64\ward_round_comprehensive_data.txt", Encoding.UTF8);

        StringBuilder sbMd = new StringBuilder();
        sbMd.AppendLine("# BÁO CÁO ĐI BUỒNG LÂM SÀNG ĐẦU GIỜ");
        sbMd.AppendLine("**Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57) - Bệnh viện Bạch Mai**");
        sbMd.AppendLine("*Thời gian báo cáo: 06:30 Ngày 26/08/2026*");
        sbMd.AppendLine("*Phạm vi buồng bệnh: **712, 712A, 714, 724, 725** | Tổng số: **23 Bệnh nhân***\n");
        sbMd.AppendLine("---");
        sbMd.AppendLine("## 🎯 TỔNG QUAN DANH SÁCH & TRỌNG TÂM THEO DÕI\n");
        sbMd.AppendLine("| Buồng | Giường | Họ và tên | Tuổi | Chẩn đoán xác định / Tổn thương đích danh | Trọng tâm theo dõi & Xử trí |");
        sbMd.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
        sbMd.AppendLine("| **712** | 21 | **Lê Doãn Nguyên** | 19 | Gãy kín xương bàn ngón IV, V bàn tay phải | Hậu phẫu N3 KHX nẹp vít; Vết mổ khô, tập vận động ngón. |");
        sbMd.AppendLine("| **712** | 21 | **Bùi Văn Điền** | 45 | Gãy xương tháp & xương thang cổ tay (T) di lệch / VT bàn tay đã khâu | **Lịch mổ hôm nay (26/08)**: KHX cổ tay (T). Nhịn ăn uống sáng. |");
        sbMd.AppendLine("| **712** | 22 | **Ngô Văn Nghị** | 50 | Trật khớp cùng vai đòn (P) - Tràn khí màng phổi (P) / TD Chấn thương sọ não | Theo dõi hô hấp, bão hòa oxy, tri giác; Đã HC Ngoại Lồng ngực & CTCH. |");
        sbMd.AppendLine("| **712** | 23 | **Quách Minh Thành** | 21 | Gãy hở độ IIIA 1/3 dưới 2 xương cẳng chân (P) | Hậu phẫu N8 KHX; Mép vết khâu có điểm thâm đen hoại tử da, HC Phẫu thuật Tạo hình. |");
        sbMd.AppendLine("| **712** | 23 | **Nguyễn Văn Kiểm** | 62 | Hoại tử vô khuẩn chỏm xương đùi (T) / Khớp háng nhân tạo (P) | Đã giải thích & hội chẩn lịch mổ Thay toàn bộ khớp háng trái. |");
        sbMd.AppendLine("| **712** | 24 | **Tạ Duy Hiếu** | 50 | Gãy kín 1/3 dưới 2 xương cẳng chân (T) / HBsAg (+) | **Lịch mổ hôm nay (26/08)**: KHX cẳng chân (T). Nhịn ăn sáng. |");
        sbMd.AppendLine("| **712** | 24 | **Lê Quang Minh** | 18 | Vết thương phức tạp lóc da mu bàn chân (P) | Theo dõi thiểu dưỡng mép vạt da mu chân; Đã thay nẹp bột cẳng bàn chân (P). |");
        sbMd.AppendLine("| **712** | 25 | **Hồ Hữu Môn** | 44 | Đứt gân Achilles chân (T) / Gút mạn | Hậu phẫu N3 nối gân; Nẹp bột bất động tư thế gập lòng, dẫn lưu ra ít dịch. |");
        sbMd.AppendLine("| **712** | 25 | **Nguyễn Văn Vạn Tường** | 34 | VT mu tay (T) đứt gân duỗi / Loạn thần do rượu - Sảng rượu | Hậu phẫu N3 nối gân duỗi; Tâm thần ổn định, hội chẩn Viện SKTT. |");
        sbMd.AppendLine("| **712** | 26 | **Nguyễn Văn Cường** | 47 | Rách chóp xoay vai (T) / Thoái hóa CSTL (TS mổ KHX đòn P) | Đau vai (T), Jobe (+); Điều trị nội khoa, chờ lịch phẫu thuật nội soi. |");
        sbMd.AppendLine("| **712** | 26 | **Phùng Minh Hà** | 41 | Gãy 1/3 dưới xương mác và đầu dưới xương chày (P) | Đã chụp CT khớp cổ chân, hoàn thiện bilan chuẩn bị phẫu thuật KHX. |");
        sbMd.AppendLine("| **712A** | 20A | **Mai Thị Thùy Linh** | 39 | Gãy 1/3 giữa xương cánh tay (P) | Hậu phẫu N2 KHX đinh/nẹp; Giảm đau PCA Morphin tĩnh mạch, tê bì tay (P). |");
        sbMd.AppendLine("| **712A** | 19A | **Trần Thị Thu** | 63 | Vỡ xẹp cấp thân đốt sống L1 / Tăng huyết áp - Cường giáp | **Hậu phẫu N1 Bơm Cement sinh học có bóng L1 (25/08)**: Đau VAS 4, HA 170/100, KS Zinacef. |");
        sbMd.AppendLine("| **714** | 17 | **Nguyễn Thị Ký** | 74 | Xẹp cấp đốt sống ngực T11 | Đau nhiều lưng ngực sau ngã; ĐMMM 17h: 7.9, 21h: 3.8. Chuẩn bị bơm Cement. |");
        sbMd.AppendLine("| **714** | 17 | **Đỗ Thị Thuân** | 78 | Áp xe ngoài màng cứng L3 gây liệt 2 chi dưới / THA | **Bệnh nặng CS cấp I**: Liệt 2 chân, KS liều cao, Corticoid, kiểm soát HA, đệm chống loét. |");
        sbMd.AppendLine("| **724** | 49 | **Lê Văn Chiến** | 58 | Viêm mủ ĐSĐĐ T4-T5 áp xe ngoài màng cứng, AIS(A) / ĐTĐ 2 - Viêm gan C | **Hậu phẫu N6 mổ cấp cứu đêm 19-rạng sáng 20/08**: Giải ép & nẹp vít ngực; Vancomycin (trough 9.76), CRP 35.4, ĐMMM 6.8-10.8, tập PHCN tại giường. |");
        sbMd.AppendLine("| **724** | 50 | **Phùng Văn Nầng** | 70 | Áp xe - Viêm xương tủy ngón II tay (T) / Tắc ĐM gian cốt - THA | Hậu phẫu N4 tháo ngón II tay (T); Mỏm cụt khô, kiểm soát HA. |");
        sbMd.AppendLine("| **724** | 50 | **Nguyễn Thị Nguyệt** | 60 | Nhiễm trùng khớp vai (P) sau mổ nội soi khâu chóp xoay | Hậu phẫu N5 nội soi làm sạch; Vancomycin (11.2 mg/L), CRP 22.5, DL ra 10ml dịch. |");
        sbMd.AppendLine("| **724** | 51 | **Mai Minh Hoàng** | 19 | Gãy xương cánh tay (P) | Tạm hoãn mổ, nẹp bột cánh bàn tay (P), vận động ngón tốt, chờ xếp lịch mổ. |");
        sbMd.AppendLine("| **724** | 52 | **Đinh Phú Xướng** | 65 | Áp xe lớn khối cơ mông - đùi (P) do *S. aureus* / ĐTĐ 2 - Xơ gan | **Điều trị kéo dài (vào 28/07)**: Dẫn lưu ổ mủ dưới SA, KS Vancomycin, ĐMMM 8.7-9.2, bổ sung Albumin. |");
        sbMd.AppendLine("| **724** | 53 | **Nguyễn Trọng Tề** | 74 | Trượt thân L3 - Xẹp thân D12 / ĐTĐ 2 - Nhồi máu cơ tim cũ | Đã bơm Cement D12 ngày 21/08; Đã phong bế khớp cùng chậu, ĐMMM 5.2-10.7 mmol/L. |");
        sbMd.AppendLine("| **724** | 54 | **Nguyễn Thị Vận** | 83 | Đau thần kinh tọa do thoái hóa CSTL - Loãng xương nặng / TD VKDT | Đau buốt lan 2 chân VAS 5/10, Lasegue (+) 60°, DEXA loãng xương nặng; Điều trị nội khoa. |");
        sbMd.AppendLine("| **725** | 55 | **Hà Ngọc Phương** | 30 | Gãy kín xương đòn (T) | **Lịch mổ hôm nay (26/08)**: Đã bù Kali (3.0 -> 3.6 mmol/L), HC Lồng ngực, nhịn ăn sáng. |");
        sbMd.AppendLine("\n---\n");
        sbMd.AppendLine("## ⚠️ DANH SÁCH BỆNH NHÂN CẦN LƯU Ý ĐẶC BIỆT TRONG NGÀY\n");
        sbMd.AppendLine("### 1. Danh sách Phẫu thuật trong ngày hôm nay (26/08/2026):");
        sbMd.AppendLine("1. **BN Bùi Văn Điền (Phòng 712 - Giường 21)**: KHX xương tháp/thang cổ tay trái. *(Đã nhịn ăn uống)*.");
        sbMd.AppendLine("2. **BN Tạ Duy Hiếu (Phòng 712 - Giường 24)**: KHX 1/3 dưới 2 xương cẳng chân trái. *(Lưu ý HBsAg dương tính)*.");
        sbMd.AppendLine("3. **BN Hà Ngọc Phương (Phòng 725 - Giường 55)**: KHX đòn trái. *(Đã bù Kali máu đạt 3.6 mmol/L)*.\n");
        sbMd.AppendLine("### 2. Bệnh nhân nặng / Cần theo dõi sát lâm sàng:");
        sbMd.AppendLine("- **BN Đỗ Thị Thuân (Phòng 714 - Giường 17)**: Áp xe ngoài màng cứng L3 gây liệt 2 chân, phù, rale ẩm phổi bên trái. Chăm sóc cấp I, lăn trở chống loét, hội chẩn mổ cấp cứu giải ép tủy.");
        sbMd.AppendLine("- **BN Quách Minh Thành (Phòng 712 - Giường 23)**: Mép da khâu cẳng chân phải có điểm thâm đen hoại tử; cần theo dõi sát ý kiến hội chẩn của Phẫu thuật Tạo hình.");
        sbMd.AppendLine("- **BN Ngô Văn Nghị (Phòng 712 - Giường 22)**: Tràn khí màng phổi phải kèm trật khớp cùng vai đòn; theo dõi hô hấp và tri giác.");
        sbMd.AppendLine("- **BN Lê Văn Chiến (Phòng 724 - Giường 49)** & **BN Đinh Phú Xướng (Phòng 724 - Giường 52)**: Nhiễm trùng mô mềm/cột sống nặng, đang dùng Vancomycin TDM và theo dõi đường huyết mao mạch nhiều cữ.");
        sbMd.AppendLine("- **BN Trần Thị Thu (Phòng 712A - Giường 19A)**: Hậu phẫu N1 Bơm cement L1; cần kiểm soát tốt cơn tăng huyết áp (170/100 mmHg).\n");
        sbMd.AppendLine("---");
        sbMd.AppendLine("## 📋 HỒ SƠ CHI TIẾT 23 BỆNH NHÂN (TRÍCH XUẤT TỪ HỆ THỐNG HIS/MOS)\n");
        sbMd.AppendLine(mdContent);

        File.WriteAllText(mdPath, sbMd.ToString(), Encoding.UTF8);

        // Generate clean HTML
        StringBuilder sbHtml = new StringBuilder();
        sbHtml.AppendLine("<!DOCTYPE html>");
        sbHtml.AppendLine("<html lang=\"vi\">");
        sbHtml.AppendLine("<head>");
        sbHtml.AppendLine("  <meta charset=\"UTF-8\">");
        sbHtml.AppendLine("  <title>Báo Cáo Đi Buồng - Khoa 57 CTCH & Cột Sống - 26/08/2026</title>");
        sbHtml.AppendLine("  <style>");
        sbHtml.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; max-width: 1200px; margin: 0 auto; padding: 20px; background-color: #f8fafc; }");
        sbHtml.AppendLine("    .header { background: linear-gradient(135deg, #1e3a8a, #0284c7); color: white; padding: 25px; border-radius: 10px; margin-bottom: 25px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }");
        sbHtml.AppendLine("    .header h1 { margin: 0 0 10px 0; font-size: 24px; text-transform: uppercase; letter-spacing: 0.5px; }");
        sbHtml.AppendLine("    .header p { margin: 3px 0; font-size: 14px; opacity: 0.9; }");
        sbHtml.AppendLine("    .section-title { color: #1e3a8a; border-left: 5px solid #0284c7; padding-left: 12px; margin: 30px 0 15px 0; font-size: 20px; }");
        sbHtml.AppendLine("    table { width: 100%; border-collapse: collapse; margin-bottom: 25px; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.05); font-size: 13px; }");
        sbHtml.AppendLine("    th { background-color: #0f172a; color: white; text-align: left; padding: 10px 12px; font-weight: 600; }");
        sbHtml.AppendLine("    td { padding: 9px 12px; border-bottom: 1px solid #e2e8f0; vertical-align: top; }");
        sbHtml.AppendLine("    tr:nth-child(even) { background-color: #f8fafc; }");
        sbHtml.AppendLine("    tr:hover { background-color: #f1f5f9; }");
        sbHtml.AppendLine("    .card { background: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.05); border: 1px solid #e2e8f0; }");
        sbHtml.AppendLine("    .card-header { font-size: 16px; font-weight: bold; color: #0369a1; border-bottom: 2px solid #e0f2fe; padding-bottom: 8px; margin-bottom: 12px; }");
        sbHtml.AppendLine("    .badge { display: inline-block; padding: 3px 8px; border-radius: 4px; font-size: 11px; font-weight: bold; }");
        sbHtml.AppendLine("    .badge-danger { background-color: #fee2e2; color: #b91c1c; }");
        sbHtml.AppendLine("    .badge-warning { background-color: #fef3c7; color: #b45309; }");
        sbHtml.AppendLine("    .badge-success { background-color: #dcfce7; color: #15803d; }");
        sbHtml.AppendLine("    .badge-info { background-color: #e0f2fe; color: #0369a1; }");
        sbHtml.AppendLine("    .alert-box { padding: 15px; border-radius: 8px; margin-bottom: 15px; }");
        sbHtml.AppendLine("    .alert-danger { background-color: #fff1f2; border-left: 4px solid #e11d48; color: #9f1239; }");
        sbHtml.AppendLine("    .alert-warning { background-color: #fffbeb; border-left: 4px solid #f59e0b; color: #92400e; }");
        sbHtml.AppendLine("    pre { background: #f1f5f9; padding: 12px; border-radius: 6px; font-size: 12px; overflow-x: auto; white-space: pre-wrap; font-family: Consolas, monospace; }");
        sbHtml.AppendLine("  </style>");
        sbHtml.AppendLine("</head>");
        sbHtml.AppendLine("<body>");
        sbHtml.AppendLine("  <div class=\"header\">");
        sbHtml.AppendLine("    <h1>Báo Cáo Đi Buồng Lâm Sàng Đầu Giờ</h1>");
        sbHtml.AppendLine("    <p><strong>Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57) - Bệnh viện Bạch Mai</strong></p>");
        sbHtml.AppendLine("    <p>Thời gian: 06:30 Ngày 26/08/2026 | Buồng: <strong>712, 712A, 714, 724, 725</strong> | Quy mô: <strong>23 Bệnh nhân nội trú</strong></p>");
        sbHtml.AppendLine("  </div>");

        sbHtml.AppendLine("  <h2 class=\"section-title\">⚠️ Trọng Tâm Đi Buồng & Phẫu Thuật Trong Ngày</h2>");
        sbHtml.AppendLine("  <div class=\"alert-box alert-warning\">");
        sbHtml.AppendLine("    <h3 style=\"margin-top:0;\">1. Danh sách Phẫu thuật hôm nay (26/08/2026):</h3>");
        sbHtml.AppendLine("    <ul>");
        sbHtml.AppendLine("      <li><strong>BN Bùi Văn Điền (P.712 - G.21)</strong>: KHX xương tháp, xương thang cổ tay trái. (Đã nhịn ăn uống).</li>");
        sbHtml.AppendLine("      <li><strong>BN Tạ Duy Hiếu (P.712 - G.24)</strong>: KHX 1/3 dưới 2 xương cẳng chân trái. (Lưu ý: HBsAg dương tính).</li>");
        sbHtml.AppendLine("      <li><strong>BN Hà Ngọc Phương (P.725 - G.55)</strong>: KHX xương đòn trái. (Đã bù Kali đạt 3.6 mmol/L).</li>");
        sbHtml.AppendLine("    </ul>");
        sbHtml.AppendLine("  </div>");

        sbHtml.AppendLine("  <div class=\"alert-box alert-danger\">");
        sbHtml.AppendLine("    <h3 style=\"margin-top:0;\">2. Bệnh nhân nặng / Cần theo dõi sát:</h3>");
        sbHtml.AppendLine("    <ul>");
        sbHtml.AppendLine("      <li><strong>BN Đỗ Thị Thuân (P.714 - G.17)</strong>: Áp xe ngoài màng cứng L3 gây liệt 2 chân, phù, rale ẩm phổi (T). CS cấp I, lăn trở chống loét, HC mổ cấp cứu.</li>");
        sbHtml.AppendLine("      <li><strong>BN Quách Minh Thành (P.712 - G.23)</strong>: Mép da khâu cẳng chân (P) thâm đen hoại tử; theo dõi HC Phẫu thuật Tạo hình.</li>");
        sbHtml.AppendLine("      <li><strong>BN Ngô Văn Nghị (P.712 - G.22)</strong>: Tràn khí màng phổi (P) + Trật khớp cùng vai đòn; theo dõi bão hòa oxy & tri giác.</li>");
        sbHtml.AppendLine("      <li><strong>BN Lê Văn Chiến (P.724 - G.49) & BN Đinh Phú Xướng (P.724 - G.52)</strong>: Nhiễm trùng nặng (Vancomycin TDM, ĐMMM nhiều cữ).</li>");
        sbHtml.AppendLine("      <li><strong>BN Trần Thị Thu (P.712A - G.19A)</strong>: Hậu phẫu N1 Bơm cement L1; theo dõi cơn tăng huyết áp 170/100 mmHg.</li>");
        sbHtml.AppendLine("    </ul>");
        sbHtml.AppendLine("  </div>");

        sbHtml.AppendLine("  <h2 class=\"section-title\">📊 Bảng Tổng Hợp 23 Bệnh Nhân</h2>");
        sbHtml.AppendLine("  <table>");
        sbHtml.AppendLine("    <thead><tr><th>Buồng</th><th>Giường</th><th>Họ và tên</th><th>Tuổi</th><th>Chẩn đoán xác định / Vị trí tổn thương</th><th>Trọng tâm theo dõi & Xử trí</th></tr></thead>");
        sbHtml.AppendLine("    <tbody>");
        
        string[] rows = new string[] {
            "<tr><td><strong>712</strong></td><td>21</td><td><strong>Lê Doãn Nguyên</strong></td><td>19</td><td>Gãy kín xương bàn ngón IV, V bàn tay phải</td><td>Hậu phẫu N3 KHX; Vết mổ khô, tập vận động ngón.</td></tr>",
            "<tr><td><strong>712</strong></td><td>21</td><td><strong>Bùi Văn Điền</strong></td><td>45</td><td>Gãy xương tháp & thang cổ tay (T) di lệch</td><td><span class=\"badge badge-danger\">Lịch mổ 26/08</span> KHX cổ tay (T). Nhịn ăn sáng.</td></tr>",
            "<tr><td><strong>712</strong></td><td>22</td><td><strong>Ngô Văn Nghị</strong></td><td>50</td><td>Trật khớp cùng vai đòn (P) - Tràn khí MP (P) / TD CTSN</td><td>Theo dõi hô hấp, bão hòa oxy; Đã HC Ngoại Lồng ngực.</td></tr>",
            "<tr><td><strong>712</strong></td><td>23</td><td><strong>Quách Minh Thành</strong></td><td>21</td><td>Gãy hở độ IIIA 1/3 dưới 2 xương cẳng chân (P)</td><td><span class=\"badge badge-warning\">Mép da hoại tử</span> HC Phẫu thuật Tạo hình.</td></tr>",
            "<tr><td><strong>712</strong></td><td>23</td><td><strong>Nguyễn Văn Kiểm</strong></td><td>62</td><td>Hoại tử vô khuẩn chỏm xương đùi (T) / Khớp háng NT (P)</td><td>Đã giải thích & HC lịch mổ Thay khớp háng (T).</td></tr>",
            "<tr><td><strong>712</strong></td><td>24</td><td><strong>Tạ Duy Hiếu</strong></td><td>50</td><td>Gãy kín 1/3 dưới 2 xương cẳng chân (T) / HBsAg (+)</td><td><span class=\"badge badge-danger\">Lịch mổ 26/08</span> KHX cẳng chân (T). Nhịn ăn sáng.</td></tr>",
            "<tr><td><strong>712</strong></td><td>24</td><td><strong>Lê Quang Minh</strong></td><td>18</td><td>Vết thương phức tạp lóc da mu bàn chân (P)</td><td>Thiểu dưỡng da mu chân; Đã thay nẹp bột cẳng bàn chân (P).</td></tr>",
            "<tr><td><strong>712</strong></td><td>25</td><td><strong>Hồ Hữu Môn</strong></td><td>44</td><td>Đứt gân Achilles chân (T) / Gút mạn</td><td>Hậu phẫu N3 nối gân; Nẹp bột gấp lòng, rút DL hôm nay.</td></tr>",
            "<tr><td><strong>712</strong></td><td>25</td><td><strong>Nguyễn Văn Vạn Tường</strong></td><td>34</td><td>VT mu tay (T) đứt gân duỗi / Loạn thần do rượu</td><td>Hậu phẫu N3 nối gân duỗi; Tâm thần ổn định, HC Viện SKTT.</td></tr>",
            "<tr><td><strong>712</strong></td><td>26</td><td><strong>Nguyễn Văn Cường</strong></td><td>47</td><td>Rách chóp xoay vai (T) / Thoái hóa CSTL</td><td>Đau vai (T), Jobe (+); Điều trị nội khoa, chờ mổ nội soi.</td></tr>",
            "<tr><td><strong>712</strong></td><td>26</td><td><strong>Phùng Minh Hà</strong></td><td>41</td><td>Gãy 1/3 dưới xương mác và đầu dưới xương chày (P)</td><td>Đã chụp CT khớp cổ chân, chuẩn bị phẫu thuật KHX.</td></tr>",
            "<tr><td><strong>712A</strong></td><td>20A</td><td><strong>Mai Thị Thùy Linh</strong></td><td>39</td><td>Gãy 1/3 giữa xương cánh tay (P)</td><td>Hậu phẫu N2 KHX; PCA Morphin TM, theo dõi tê bì tay (P).</td></tr>",
            "<tr><td><strong>712A</strong></td><td>19A</td><td><strong>Trần Thị Thu</strong></td><td>63</td><td>Vỡ xẹp cấp thân L1 / Tăng huyết áp - Cường giáp</td><td><span class=\"badge badge-success\">Bơm Cement L1 (25/08)</span> Đau VAS 4, HA 170/100, KS Zinacef.</td></tr>",
            "<tr><td><strong>714</strong></td><td>17</td><td><strong>Nguyễn Thị Ký</strong></td><td>74</td><td>Xẹp cấp đốt sống ngực T11</td><td>Đau nhiều sau ngã; ĐMMM 17h: 7.9, 21h: 3.8. Chuẩn bị bơm Cement.</td></tr>",
            "<tr><td><strong>714</strong></td><td>17</td><td><strong>Đỗ Thị Thuân</strong></td><td>78</td><td>Áp xe ngoài màng cứng L3 liệt 2 chân / THA</td><td><span class=\"badge badge-danger\">Nặng - CS cấp I</span> Liệt 2 chân, KS liều cao, Corticoid, lăn trở.</td></tr>",
            "<tr><td><strong>724</strong></td><td>49</td><td><strong>Lê Văn Chiến</strong></td><td>58</td><td>Viêm mủ ĐSĐĐ T4-T5 áp xe NMC, AIS(A) / ĐTĐ 2 - VGC</td><td>Hậu phẫu N6 mổ cấp cứu đêm 19-rạng sáng 20/08 (Giải ép & nẹp vít ngực); Vancomycin (9.76), CRP 35.4, tập PHCN.</td></tr>",
            "<tr><td><strong>724</strong></td><td>50</td><td><strong>Phùng Văn Nầng</strong></td><td>70</td><td>Áp xe - Viêm xương tủy ngón II tay (T) / Tắc ĐM gian cốt</td><td>Hậu phẫu N4 tháo ngón II tay (T); Mỏm cụt khô, kiểm soát HA.</td></tr>",
            "<tr><td><strong>724</strong></td><td>50</td><td><strong>Nguyễn Thị Nguyệt</strong></td><td>60</td><td>Nhiễm trùng khớp vai (P) sau nội soi chóp xoay</td><td>Hậu phẫu N5 làm sạch; Vancomycin (11.2), CRP 22.5, rút DL vai.</td></tr>",
            "<tr><td><strong>724</strong></td><td>51</td><td><strong>Mai Minh Hoàng</strong></td><td>19</td><td>Gãy xương cánh tay (P)</td><td>Tạm hoãn mổ, nẹp bột cánh bàn tay (P), vận động ngón tốt.</td></tr>",
            "<tr><td><strong>724</strong></td><td>52</td><td><strong>Đinh Phú Xướng</strong></td><td>65</td><td>Áp xe cơ mông - đùi (P) do S. aureus / ĐTĐ 2 - Xơ gan</td><td>Dẫn lưu ổ mủ dưới SA, Vancomycin, ĐMMM 8.7-9.2, bù Albumin.</td></tr>",
            "<tr><td><strong>724</strong></td><td>53</td><td><strong>Nguyễn Trọng Tề</strong></td><td>74</td><td>Trượt thân L3 - Xẹp D12 / ĐTĐ 2 - Nhồi máu cơ tim cũ</td><td>Đã bơm Cement D12 (21/08); Phong bế KCC, ĐMMM 5.2-10.7.</td></tr>",
            "<tr><td><strong>724</strong></td><td>54</td><td><strong>Nguyễn Thị Vận</strong></td><td>83</td><td>Đau TK tọa do thoái hóa CSTL - Loãng xương nặng</td><td>Đau buốt 2 chân VAS 5/10, Lasegue (+) 60°, DEXA T-score -3.2.</td></tr>",
            "<tr><td><strong>725</strong></td><td>55</td><td><strong>Hà Ngọc Phương</strong></td><td>30</td><td>Gãy kín xương đòn (T)</td><td><span class=\"badge badge-danger\">Lịch mổ 26/08</span> Bù Kali (3.0->3.6), HC Lồng ngực, nhịn ăn.</td></tr>"
        };

        foreach (var r in rows) {
            sbHtml.AppendLine("      " + r);
        }

        sbHtml.AppendLine("    </tbody>");
        sbHtml.AppendLine("  </table>");

        sbHtml.AppendLine("  <h2 class=\"section-title\">📑 Dữ Liệu Bệnh Án & Y Lệnh Chi Tiết Từng Bệnh Nhân</h2>");
        sbHtml.AppendLine("  <pre>" + mdContent + "</pre>");
        sbHtml.AppendLine("</body>");
        sbHtml.AppendLine("</html>");

        File.WriteAllText(htmlPath, sbHtml.ToString(), Encoding.UTF8);

        Console.WriteLine("SUCCESS: Regenerated MD and HTML files with corrected clinical surgery details.");
    }
}
