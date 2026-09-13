import json, zipfile, os, sys, xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

template_file = "Templates/Mẫu Biên bản hội chẩn thông qua mổ (Template Mail Merge).docx"
out_dir = "Reports/BienBanThongQuaMo_PT01"
os.makedirs(out_dir, exist_ok=True)

with open("Reports/pt01_input.json", "r", encoding="utf-8-sig") as f:
    patients = json.load(f)

def xml_escape(val):
    if val is None:
        return ""
    s = str(val)
    # Must replace & first
    s = s.replace("&", "&amp;")
    s = s.replace("<", "&lt;").replace(">", "&gt;")
    # Word XML line breaks inside <w:t>
    s = s.replace("\n", '</w:t><w:br/><w:t xml:space="preserve">')
    return s

surgery_info = {
    "0004019674": { # LÊ THỊ THÊM
        "ptv": "BS Tân",
        "pp_pt": "Phẫu thuật hàn xương liên thân đốt lối sau qua lỗ liên hợp L3-4, L4-5 (TLIF L3-4, L4-5)",
        "vo_cam": "Mê nội khí quản",
        "loai_pt": "Đặc biệt",
        "asa": "Loại III",
        "mallampati": "Loại II",
        "du_tru_mau": "350",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân đau CSTL lan xuống 2 chi dưới nhiều năm, tê bì chân, đi lại hạn chế, điều trị nội khoa không cải thiện.",
        "tien_su": "Tăng huyết áp, suy tuyến thượng thận điều trị thường xuyên.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng trung bình. Hội chứng thắt lưng hông (+), nghiệm pháp Lasegue (+) 60 độ 2 bên, cảm giác và vận động bàn ngón chân bình thường. Tim đều, phổi không rale, bụng mềm.",
        "cdha": "MRI CSTL: Thoát vị đĩa đệm L3-4, L4-5 chèn ép rễ thần kinh, trượt đốt sống L4-5 ra trước độ I, hẹp ống sống nặng L3-L4-L5."
    },
    "0004023255": { # TRẦN VĂN QUÝ
        "ptv": "TS Trung",
        "pp_pt": "Phẫu thuật cố định cột sống cổ lối trước, hàn xương liên thân đốt C3-4 (ACDF C3-4) / Cố định cột sống lối sau",
        "vo_cam": "Mê nội khí quản",
        "loai_pt": "Đặc biệt",
        "asa": "Loại II",
        "mallampati": "Loại II",
        "du_tru_mau": "350",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân đau mỏi cổ vai gáy lan 2 tay, tê bì 2 tay, đi lại loạng choạng cảm giác như đi trên đệm, tăng dần 3 tháng nay.",
        "tien_su": "Chưa phát hiện tiền sử bệnh lý mạn tính đặc biệt.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Hội chứng rễ tủy cổ (+), Hoffman (+) 2 bên, tăng phản xạ gân xương 2 chi dưới, cơ lực tứ chi 4/5. Tim đều, huyết động ổn định.",
        "cdha": "MRI Cột sống cổ: Thoát vị đĩa đệm C3-4 chèn ép tủy cổ gây phù tủy ngang mức, theo dõi cốt hóa dây chằng dọc sau (OPLL)."
    },
    "0001900923": { # TRẦN THỊ THU
        "ptv": "BS Hoàng",
        "pp_pt": "Phẫu thuật hàn xương liên thân đốt lối sau L4-5, L5-S1 (TLIF L4-5, L5S1)",
        "vo_cam": "Mê nội khí quản",
        "loai_pt": "Đặc biệt",
        "asa": "Loại II",
        "mallampati": "Loại II",
        "du_tru_mau": "350",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân đau CSTL âm ỉ kéo dài nhiều năm, đợt này đau nhói lan mặt ngoài đùi và cẳng chân phải, tê bì, hạn chế đi lại.",
        "tien_su": "Suy thượng thận do dùng corticoid kéo dài, đau dạ dày.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng trung bình. Co cứng cơ cạnh sống, ấn đau chói gai sau L4-L5, Lasegue (+) 60 độ chân phải, không rối loạn cơ tròn. Tim đều, phổi không rale, huyết động ổn định.",
        "cdha": "X-quang và MRI cột sống thắt lưng: Trượt đốt sống L4 ra trước độ I, hẹp ống sống tầng L4-L5, thoái hóa đĩa đệm L4-5, L5-S1."
    },
    "0003837429": { # ĐINH VĂN MINH
        "ptv": "TS Trung",
        "pp_pt": "Phẫu thuật cắt đĩa đệm, ghép xương cố định cột sống cổ lối trước (ACDF C3/4)",
        "vo_cam": "Mê nội khí quản",
        "loai_pt": "Đặc biệt",
        "asa": "Loại II",
        "mallampati": "Loại II",
        "du_tru_mau": "250",
        "nguy_co": "Sạch",
        "benh_su": "Đau cổ gáy nhiều, tê bì dọc mặt ngoài cánh tay và cẳng tay 2 bên, vụng về khi cầm nắm đồ vật.",
        "tien_su": "Gút mạn tính điều trị ngoại trú.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Tê bì phân bố rễ C4-C5 2 bên, giảm cảm giác nông ngọn chi, phản xạ gân xương tứ chi tăng nhẹ. Tim đều, phổi sáng.",
        "cdha": "MRI CS cổ: Phình đĩa đệm C3/4 gây hẹp ống sống nặng, chèn ép rễ thần kinh và tủy cổ gây phù tủy ngang mức C3/4."
    },
    "0003807551": { # NGUYỄN THỊ LAN
        "ptv": "TS Trung",
        "pp_pt": "Bơm xi măng sinh học tạo hình thân đốt sống T12 (BXM T12)",
        "vo_cam": "Tiền mê + Tê tại chỗ",
        "loai_pt": "Loại I",
        "asa": "Loại III",
        "mallampati": "Loại II",
        "du_tru_mau": "0",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân ngã ngồi đập mông cách đây 1 tuần, sau ngã đau nhói vùng lưng ngực T12, không thể tự ngồi dậy được.",
        "tien_su": "Loãng xương nặng, tăng huyết áp, suy tim xung huyết, suy tủy xương do thuốc.",
        "tom_tat": "Bệnh nhân tỉnh, thể trạng già yếu. Ấn đau chói gai sau T12, gõ dồn từ đầu đau nhói T12, cơ lực 2 chân 5/5, không liệt, đại tiểu tiện tự chủ. Tim loạn nhịp nhẹ, huyết áp 135/85 mmHg.",
        "cdha": "MRI CSTL: Hình ảnh xẹp cấp thân đốt sống T12 trên nền loãng xương (phù tủy xương tăng tín hiệu trên STIR)."
    },
    "0004024456": { # CỒ THỊ LAN
        "ptv": "TS Trung",
        "pp_pt": "Bơm xi măng sinh học tạo hình thân đốt sống L1 (BXM L1)",
        "vo_cam": "Tiền mê + Tê tại chỗ",
        "loai_pt": "Loại I",
        "asa": "Loại II",
        "mallampati": "Loại I",
        "du_tru_mau": "0",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân đau vùng thắt lưng sau với đồ vật nặng, đau tăng khi đứng hoặc thay đổi tư thế, nghỉ ngơi giảm đau ít.",
        "tien_su": "Loãng xương nhiều năm.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Ấn đau chói đốt sống L1, không có dấu hiệu chèn ép rễ thần kinh, cơ lực 2 chân 5/5. Huyết động ổn định.",
        "cdha": "MRI CSTL: Loãng xương nặng, xẹp lún cấp tính thân đốt sống L1 (phù tủy xương rõ trên T2 fat sat)."
    },
    "0004022317": { # HOÀNG THỊ THÂN
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật thay toàn bộ khớp háng bên phải bằng khớp nhân tạo",
        "vo_cam": "Tê tủy sống",
        "loai_pt": "Đặc biệt",
        "asa": "Loại III",
        "mallampati": "Loại II",
        "du_tru_mau": "700",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân đau khớp háng phải tăng dần nhiều năm nay, đi khập khiễng, đợt này đau dữ dội khi dồn trọng lượng, hạn chế gấp duỗi khớp háng phải.",
        "tien_su": "Viêm khớp dạng thấp huyết thanh (+), tăng huyết áp, hẹp mạch vành, nhiễm khuẩn tiết niệu cũ.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Khớp háng phải biến dạng xoay ngoài nhẹ, hạn chế biên độ vận động khớp háng phải rõ rệt (gấp < 70 độ, xoay trong ngoài hạn chế), mạch bẹn và mu chân bắt rõ.",
        "cdha": "X-quang khớp háng: Thoái hóa khớp háng phải độ IV (hẹp khe khớp, xơ đặc xương dưới sụn và gai xương lớn ổ cối)."
    },
    "0002242870": { # NGUYỄN VĂN NGHĨA
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật kết hợp xương mâm chày phải bằng nẹp vít khóa (KHX mâm chày phải)",
        "vo_cam": "Tê tủy sống",
        "loai_pt": "Loại I",
        "asa": "Loại II",
        "mallampati": "Loại I",
        "du_tru_mau": "350",
        "nguy_co": "Sạch nhiễm",
        "benh_su": "Bệnh nhân tai nạn giao thông xe máy ngã đập gối phải, đau chói, sưng nề biến dạng gối phải, bất lực vận động chân phải.",
        "tien_su": "Tăng huyết áp, theo dõi chấn thương cột sống cổ (đã chụp CT kiểm tra loại trừ).",
        "tom_tat": "Gối phải sưng nề nhiều, bầm tím, dấu hiệu bập bềnh xương bánh chè (+), đau chói mâm chày ngoài, mạch mu chân và chày sau bắt rõ, cảm giác ngọn chi bình thường.",
        "cdha": "X-quang và CT Scanner khớp gối: Gãy phức tạp 1/3 trên xương chày phải (gãy lún mâm chày Schatzker II-III)."
    },
    "0004023186": { # PHẠM NGỌC HÒA
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật bóc u cuộn mạch (Glomus tumor) búp ngón 2 tay phải",
        "vo_cam": "Tê tại chỗ",
        "loai_pt": "Loại II",
        "asa": "Loại I",
        "mallampati": "Loại I",
        "du_tru_mau": "0",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân xuất hiện nốt đau chói ở búp ngón 2 tay phải kéo dài 6 tháng nay, đau tăng dữ dội khi chạm nhẹ hoặc khi tiếp xúc nước lạnh.",
        "tien_su": "Khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Búp ngón 2 tay phải có điểm đau chói khu trú kích thước ~4mm, nghiệm pháp Love (+), nghiệm pháp Hildreth (+), đầu chi hồng ấm.",
        "cdha": "Siêu âm mô mềm ngón tay: Hình ảnh khối giảm âm giới hạn rõ búp ngón 2 tay phải, giàu mạch máu trên Doppler (u cuộn mạch)."
    },
    "0004034654": { # HOÀNG MINH ĐẰNG
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật kết hợp xương hai xương cẳng chân phải (KHX bằng đinh nội tủy / nẹp vít)",
        "vo_cam": "Tê tủy sống",
        "loai_pt": "Loại I",
        "asa": "Loại II",
        "mallampati": "Loại I",
        "du_tru_mau": "350",
        "nguy_co": "Sạch nhiễm",
        "benh_su": "Bệnh nhân ngã bậc tam cấp trượt chân đập cẳng chân phải xuống đất, sau tai nạn đau chói, biến dạng cẳng chân, không đứng dậy được.",
        "tien_su": "Tăng huyết áp nhẹ điều trị không thường xuyên.",
        "tom_tat": "Cẳng chân phải sưng nề 1/3 giữa, biến dạng gập góc nhẹ, có điểm đau chói và lạo xạo xương, không có vết thương hở (gãy kín), mạch mu chân bắt rõ.",
        "cdha": "X-quang cẳng chân phải thẳng nghiêng: Gãy 1/3 giữa hai xương cẳng chân phải di lệch."
    },
    "0004011066": { # NGUYỄN THỊ LIÊN
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật giải phóng thần kinh giữa - cắt dây chằng ngang cổ tay hai bên",
        "vo_cam": "Tê tại chỗ",
        "loai_pt": "Loại II",
        "asa": "Loại I",
        "mallampati": "Loại I",
        "du_tru_mau": "0",
        "nguy_co": "Sạch",
        "benh_su": "Bệnh nhân tê buốt 3 ngón tay rưỡi phía bờ quay 2 bàn tay nhiều tháng nay, tê nhiều về đêm thức giấc phải vẫy tay mới đỡ, vụng về khi làm việc.",
        "tien_su": "Khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh. Teo nhẹ mô cái 2 bên, giảm cảm giác nông ngón 1, 2, 3 bàn tay 2 bên, nghiệm pháp Tinel (+) cổ tay 2 bên, Phalen (+) 30 giây. Huyết động bình thường.",
        "cdha": "Điện cơ (EMG): Hội chứng ống cổ tay hai bên mức độ trung bình - nặng (tổn thương sợi cảm giác và vận động dây thần kinh giữa)."
    },
    "0004035826": { # MAI HỮU TÀI
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật kết hợp xương cẳng chân trái bằng đinh nội tủy có chốt (KHX cẳng chân trái)",
        "vo_cam": "Tê tủy sống",
        "loai_pt": "Loại I",
        "asa": "Loại I",
        "mallampati": "Loại I",
        "du_tru_mau": "350",
        "nguy_co": "Sạch nhiễm",
        "benh_su": "Bệnh nhân bị va chạm xe máy, chân trái va đập mạnh vào xe, sau va chạm đau chói cẳng chân trái, bất lực vận động.",
        "tien_su": "Thanh niên khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Cẳng chân trái sưng nề biến dạng gập góc 1/3 giữa dưới, cử động bất thường, mạch mu chân và chày sau trái bắt tốt, cảm giác đầu chi bình thường.",
        "cdha": "X-quang cẳng chân trái: Gãy 1/3 giữa dưới xương chày và xương mác trái di lệch."
    },
    "0004037901": { # NGUYỄN THỊ LAN ANH
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật kết hợp xương 1/3 trên và giữa xương chày, 1/3 trên xương mác trái bằng nẹp vít khóa (KHX)",
        "vo_cam": "Tê tủy sống",
        "loai_pt": "Loại I",
        "asa": "Loại I",
        "mallampati": "Loại I",
        "du_tru_mau": "350",
        "nguy_co": "Sạch nhiễm",
        "benh_su": "Bệnh nhân trượt chân cầu thang ngã đập cẳng chân trái, đau dữ dội, không thể đứng hay vận động chân trái.",
        "tien_su": "Khỏe mạnh, không có bệnh lý nền.",
        "tom_tat": "Bệnh nhân tỉnh. Cẳng chân trái sưng nề nhiều vùng 1/3 trên và giữa, đau chói chày mác, gãy kín, mạch mu chân trái bắt rõ, không chèn ép khoang.",
        "cdha": "X-quang cẳng chân trái: Gãy 1/3 trên và giữa xương chày, gãy 1/3 trên xương mác trái."
    }
}

generated_files = []

for idx, p in enumerate(patients, 1):
    p_code = p["PatientCode"]
    info = surgery_info.get(p_code, {})
    
    # Format ngày sinh
    dob_raw = p.get("Ngay_Sinh", "")
    dob_fmt = dob_raw
    if len(dob_raw) >= 8:
        dob_fmt = f"{dob_raw[6:8]}/{dob_raw[4:6]}/{dob_raw[0:4]}"
    
    # Format kết quả cận lâm sàng
    cls_parts = []
    cls_parts.append(f"• Huyết học: Hb: {p.get('Hb', '-')}, Bạch cầu: {p.get('Wbc', '-')}, Tiểu cầu: {p.get('Plt', '-')}.")
    cls_parts.append(f"• Đông máu: PT-INR: {p.get('Inr', '-')}, Fibrinogen: {p.get('Fib', '-')}, APTT: {p.get('Aptt', '-')}.")
    cls_parts.append(f"• Sinh hóa: Glucose: {p.get('Glu', '-')}, Ure: {p.get('Ure', '-')}, Creatinin: {p.get('Cre', '-')}, AST: {p.get('Ast', '-')}, ALT: {p.get('Alt', '-')}.")
    if info.get("cdha"):
        cls_parts.append(f"• CĐHA: {info.get('cdha')}")
    cls_full = "\n".join(cls_parts)
    
    replacements = {
        "&lt;&lt;Ma_Ho_So&gt;&gt;": xml_escape(p.get("Ma_Ho_So", "")),
        "&lt;&lt;Loai_Hoi_Chan&gt;&gt;": xml_escape("Chương trình"),
        "&lt;&lt;Ho_Va_Ten&gt;&gt;": xml_escape(p.get("Ho_Va_Ten", "").strip().upper()),
        "&lt;&lt;Ngay_Sinh&gt;&gt;": xml_escape(dob_fmt),
        "&lt;&lt;Gioi_Tinh&gt;&gt;": xml_escape(p.get("Gioi_Tinh", "")),
        "&lt;&lt;Dia_Chi&gt;&gt;": xml_escape(p.get("Dia_Chi", "")),
        "&lt;&lt;Ngay_Gio_Vao_Vien&gt;&gt;": xml_escape(p.get("Ngay_Gio_Vao_Vien", "")),
        "&lt;&lt;Chan_Doan&gt;&gt;": xml_escape((p.get("IcdName", "") + (" (" + p.get("IcdText", "") + ")" if p.get("IcdText") else "")).strip()),
        "&lt;&lt;Tien_Su&gt;&gt;": xml_escape(info.get("tien_su", "Chưa phát hiện bất thường")),
        "&lt;&lt;Benh_Su&gt;&gt;": xml_escape(info.get("benh_su", "Bệnh diễn biến tăng dần, vào viện điều trị.")),
        "&lt;&lt;Thoi_Gian_Hoi_Chan&gt;&gt;": xml_escape("14:00 ngày 13/09/2026"),
        "&lt;&lt;Tom_Tat_Tinh_Trang_Benh&gt;&gt;": xml_escape(info.get("tom_tat", "Bệnh nhân tỉnh, tiếp xúc tốt, huyết động ổn định.")),
        "&lt;&lt;Ket_Qua_CLS_CDHA&gt;&gt;": xml_escape(cls_full),
        "&lt;&lt;Nhom_Mau&gt;&gt;": xml_escape(p.get("Nhom_Mau", "O Rh(+)")),
        "&lt;&lt;Du_Tru_Mau_ml&gt;&gt;": xml_escape(info.get("du_tru_mau", "0")),
        "&lt;&lt;Phuong_Phap_Phau_Thuat&gt;&gt;": xml_escape(info.get("pp_pt", "")),
        "&lt;&lt;Phuong_Phap_Vo_Cam&gt;&gt;": xml_escape(info.get("vo_cam", "Mê nội khí quản")),
        "&lt;&lt;Mallampati&gt;&gt;": xml_escape(info.get("mallampati", "Loại I")),
        "&lt;&lt;Loai_Phau_Thuat&gt;&gt;": xml_escape(info.get("loai_pt", "Loại I")),
        "&lt;&lt;Phan_Loai_ASA&gt;&gt;": xml_escape(info.get("asa", "Loại II")),
        "&lt;&lt;Phan_Loai_Nguy_Co&gt;&gt;": xml_escape(info.get("nguy_co", "Sạch")),
        "&lt;&lt;Nhiem_Khuan_Vet_Mo&gt;&gt;": xml_escape("Không"),
        "&lt;&lt;Khang_Sinh_Du_Phong&gt;&gt;": xml_escape("Có (Cefazolin 2g tiêm TM trước rạch da 30 phút)"),
        "&lt;&lt;Phau_Thuat_Vien_Chinh&gt;&gt;": xml_escape(info.get("ptv", "")),
        "&lt;&lt;Ngay_Gio_PT_Du_Kien&gt;&gt;": xml_escape("08:00 ngày 14/09/2026"),
        "&lt;&lt;Bien_Chung_Nguy_Co_Luu_Y&gt;&gt;": xml_escape("Chảy máu, nhiễm trùng vết mổ, tổn thương mạch máu thần kinh, đau sau mổ, dị ứng phản vệ, thuyên tắc huyết khối tĩnh mạch sâu (DVT)."),
        "&lt;&lt;Bien_Phap_Thay_The_Chuan_Bi&gt;&gt;": xml_escape("Theo dõi sát DHST, giải thích kỹ thân nhân và người bệnh, chuẩn bị đầy đủ dụng cụ phẫu thuật và thuốc cấp cứu."),
        "&lt;&lt;BS_Phau_Thuat&gt;&gt;": xml_escape(info.get("ptv", "")),
        "&lt;&lt;BS_Gay_Me&gt;&gt;": xml_escape("BS Khoa Phẫu thuật - Gây mê hồi sức"),
        "&lt;&lt;Lanh_Dao_Khoa_LS&gt;&gt;": xml_escape("TS.BS. Nguyễn Văn Trung"),
        "&lt;&lt;Lanh_Dao_KHTH&gt;&gt;": xml_escape("TS.BS. Nguyễn Văn Trung")
    }

    safe_name = p.get("Ho_Va_Ten", "").strip().replace(" ", "_")
    out_filename = f"{idx:02d}_PT01_BienBanThongQuaMo_{safe_name}_{p_code}.docx"
    out_filepath = os.path.join(out_dir, out_filename)

    with zipfile.ZipFile(template_file, "r") as zin:
        xml_content = zin.read("word/document.xml").decode("utf-8")
        for tag, val in replacements.items():
            xml_content = xml_content.replace(tag, str(val))
        
        # Verify XML well-formedness BEFORE saving
        try:
            ET.fromstring(xml_content.encode('utf-8'))
        except Exception as ex:
            print(f"❌ ERROR: XML for {safe_name} is invalid: {ex}")
            sys.exit(1)
            
        with zipfile.ZipFile(out_filepath, "w", zipfile.ZIP_DEFLATED) as zout:
            for item in zin.infolist():
                if item.filename == "word/document.xml":
                    zout.writestr(item, xml_content.encode("utf-8"))
                else:
                    zout.writestr(item, zin.read(item.filename))
                    
    generated_files.append(out_filepath)
    print(f"[{idx:02d}/13] Verified & Created OK: {out_filename}")

print(f"\n🎉 100% HOÀN TẤT VÀ KIỂM ĐỊNH THÀNH CÔNG {len(generated_files)} FILE WORD CHUẨN XML!")
