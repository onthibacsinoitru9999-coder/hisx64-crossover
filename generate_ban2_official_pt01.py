import json, zipfile, os, sys, xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

template_file = "Templates/mau_bbhc .docx"
out_dir = "Reports/BienBanThongQuaMo_PT01"
os.makedirs(out_dir, exist_ok=True)

with open("Reports/pt01_input.json", "r", encoding="utf-8-sig") as f:
    all_patients = json.load(f)

ban2_data = {
    "0001900923": { # TRẦN THỊ THU
        "idx": "03",
        "ptv": "BS Hoàng",
        "pp_pt": "Phẫu thuật hàn xương liên thân đốt lối sau L4-5, L5-S1 (TLIF L4-5, L5S1)",
        "vo_cam": "Mê nội khí quản",
        "mallampati_col": 3, # Loại II
        "loai_pt_col": 1,    # Đặc biệt
        "asa_col": 3,        # Loại II
        "nguy_co_col": 1,    # Sạch
        "du_tru_mau": "350",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân đau CSTL âm ỉ kéo dài nhiều năm, đợt này đau nhói lan mặt ngoài đùi và cẳng chân phải, tê bì, hạn chế đi lại.",
        "tien_su": "Suy thượng thận do dùng corticoid kéo dài, đau dạ dày.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng trung bình. Co cứng cơ cạnh sống, ấn đau chói gai sau L4-L5, Lasegue (+) 60 độ chân phải, không rối loạn cơ tròn. Tim đều, phổi không rale, huyết động ổn định.",
        "cdha": "X-quang và MRI cột sống thắt lưng: Trượt đốt sống L4 ra trước độ I, hẹp ống sống tầng L4-L5, thoái hóa đĩa đệm L4-5, L5-S1. Huyết học: Hb: 136 g/L, Bạch cầu: 7.37 G/L, Tiểu cầu: 307 G/L. Đông máu: PT-INR: 1.09, Fibrinogen: 3.10 g/L, APTT: 0.97. Sinh hóa: Glucose: 4.6 mmol/L, Ure: 3.7 mmol/L, Creatinin: 54 umol/L, AST: 21 U/L, ALT: 21 U/L."
    },
    "0004022317": { # HOÀNG THỊ THÂN
        "idx": "07",
        "ptv": "BS Giang",
        "pp_pt": "Phẫu thuật thay toàn bộ khớp háng bên phải bằng khớp nhân tạo",
        "vo_cam": "Tê tủy sống",
        "mallampati_col": 3, # Loại II
        "loai_pt_col": 1,    # Đặc biệt
        "asa_col": 5,        # Loại III
        "nguy_co_col": 1,    # Sạch
        "du_tru_mau": "700",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân đau khớp háng phải tăng dần nhiều năm nay, đi khập khiễng, đợt này đau dữ dội khi dồn trọng lượng, hạn chế gấp duỗi khớp háng phải.",
        "tien_su": "Viêm khớp dạng thấp huyết thanh (+), tăng huyết áp, hẹp mạch vành, nhiễm khuẩn tiết niệu cũ.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Khớp háng phải biến dạng xoay ngoài nhẹ, hạn chế biên độ vận động khớp háng phải rõ rệt (gấp < 70 độ, xoay trong ngoài hạn chế), mạch bẹn và mu chân bắt rõ.",
        "cdha": "X-quang khớp háng: Thoái hóa khớp háng phải độ IV (hẹp khe khớp, xơ đặc xương dưới sụn và gai xương lớn ổ cối). Huyết học: Hb: 133 g/L, Bạch cầu: 14.35 G/L, Tiểu cầu: 451 G/L. Đông máu: PT-INR: 0.99, Fibrinogen: 4.59 g/L, APTT: 0.89. Sinh hóa: Glucose: 4.7 mmol/L, Ure: 5.6 mmol/L, Creatinin: 63 umol/L, AST: 27 U/L, ALT: 28 U/L."
    },
    "0002242870": { # NGUYỄN VĂN NGHĨA
        "idx": "08",
        "pp_pt": "Phẫu thuật kết hợp xương mâm chày phải bằng nẹp vít khóa (KHX mâm chày phải)",
        "vo_cam": "Tê tủy sống",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 3,    # Loại I
        "asa_col": 3,        # Loại II
        "nguy_co_col": 3,    # Sạch nhiễm
        "du_tru_mau": "350",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân tai nạn giao thông xe máy ngã đập gối phải, đau chói, sưng nề biến dạng gối phải, bất lực vận động chân phải.",
        "tien_su": "Tăng huyết áp, theo dõi chấn thương cột sống cổ (đã chụp CT kiểm tra loại trừ).",
        "tom_tat": "Gối phải sưng nề nhiều, bầm tím, dấu hiệu bập bềnh xương bánh chè (+), đau chói mâm chày ngoài, mạch mu chân và chày sau bắt rõ, cảm giác ngọn chi bình thường.",
        "cdha": "X-quang và CT Scanner khớp gối: Gãy phức tạp 1/3 trên xương chày phải (gãy lún mâm chày Schatzker II-III). Huyết học: Hb: 122 g/L, Bạch cầu: 18.26 G/L, Tiểu cầu: 309 G/L. Đông máu: PT-INR: 0.98, Fibrinogen: 3.92 g/L, APTT: 0.87. Sinh hóa: Glucose: 8.1 mmol/L, Ure: 5.1 mmol/L, Creatinin: 82 umol/L, AST: 20 U/L, ALT: 20 U/L."
    },
    "0004023186": { # PHẠM NGỌC HÒA
        "idx": "09",
        "pp_pt": "Phẫu thuật bóc u cuộn mạch (Glomus tumor) búp ngón 2 tay phải",
        "vo_cam": "Tê tại chỗ",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 5,    # Loại II
        "asa_col": 1,        # Loại I
        "nguy_co_col": 1,    # Sạch
        "du_tru_mau": "0",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân xuất hiện nốt đau chói ở búp ngón 2 tay phải kéo dài 6 tháng nay, đau tăng dữ dội khi chạm nhẹ hoặc khi tiếp xúc nước lạnh.",
        "tien_su": "Khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Búp ngón 2 tay phải có điểm đau chói khu trú kích thước ~4mm, nghiệm pháp Love (+), nghiệm pháp Hildreth (+), đầu chi hồng ấm.",
        "cdha": "Siêu âm mô mềm ngón tay: Hình ảnh khối giảm âm giới hạn rõ búp ngón 2 tay phải, giàu mạch máu trên Doppler (u cuộn mạch). Các xét nghiệm tiền phẫu trong giới hạn bình thường."
    },
    "0004034654": { # HOÀNG MINH ĐẰNG
        "idx": "10",
        "pp_pt": "Phẫu thuật kết hợp xương hai xương cẳng chân phải bằng đinh nội tủy có chốt (KHX)",
        "vo_cam": "Tê tủy sống",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 3,    # Loại I
        "asa_col": 3,        # Loại II
        "nguy_co_col": 3,    # Sạch nhiễm
        "du_tru_mau": "350",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân ngã bậc tam cấp trượt chân đập cẳng chân phải xuống đất, sau tai nạn đau chói, biến dạng cẳng chân, không đứng dậy được.",
        "tien_su": "Tăng huyết áp nhẹ điều trị không thường xuyên.",
        "tom_tat": "Cẳng chân phải sưng nề 1/3 giữa, biến dạng gập góc nhẹ, có điểm đau chói và lạo xạo xương, không có vết thương hở (gãy kín), mạch mu chân bắt rõ.",
        "cdha": "X-quang cẳng chân phải thẳng nghiêng: Gãy 1/3 giữa hai xương cẳng chân phải di lệch. Huyết học: Hb: 153 g/L, Bạch cầu: 5.17 G/L, Tiểu cầu: 157 G/L. Đông máu: PT-INR: 0.98, Fibrinogen: 2.85 g/L, APTT: 1.06. Sinh hóa: Glucose: 7.6 mmol/L, Ure: 4.8 mmol/L, Creatinin: 98 umol/L, AST: 21 U/L, ALT: 9 U/L."
    },
    "0004011066": { # NGUYỄN THỊ LIÊN
        "idx": "11",
        "pp_pt": "Phẫu thuật giải phóng thần kinh giữa - cắt dây chằng ngang cổ tay hai bên",
        "vo_cam": "Tê tại chỗ",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 5,    # Loại II
        "asa_col": 1,        # Loại I
        "nguy_co_col": 1,    # Sạch
        "du_tru_mau": "0",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân tê buốt 3 ngón tay rưỡi phía bờ quay 2 bàn tay nhiều tháng nay, tê nhiều về đêm thức giấc phải vẫy tay mới đỡ, vụng về khi làm việc.",
        "tien_su": "Khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh. Teo nhẹ mô cái 2 bên, giảm cảm giác nông ngón 1, 2, 3 bàn tay 2 bên, nghiệm pháp Tinel (+) cổ tay 2 bên, Phalen (+) 30 giây. Huyết động bình thường.",
        "cdha": "Điện cơ (EMG): Hội chứng ống cổ tay hai bên mức độ trung bình - nặng. Huyết học: Hb: 126 g/L, Bạch cầu: 5.19 G/L, Tiểu cầu: 236 G/L. Đông máu: PT-INR: 0.92, Fibrinogen: 2.91 g/L, APTT: 1.02. Sinh hóa bình thường."
    },
    "0004035826": { # MAI HỮU TÀI
        "idx": "12",
        "pp_pt": "Phẫu thuật kết hợp xương cẳng chân trái bằng đinh nội tủy có chốt (KHX cẳng chân trái)",
        "vo_cam": "Tê tủy sống",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 3,    # Loại I
        "asa_col": 1,        # Loại I
        "nguy_co_col": 3,    # Sạch nhiễm
        "du_tru_mau": "350",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân bị va chạm xe máy, chân trái va đập mạnh vào xe, sau va chạm đau chói cẳng chân trái, bất lực vận động.",
        "tien_su": "Thanh niên khỏe mạnh.",
        "tom_tat": "Bệnh nhân tỉnh, tiếp xúc tốt. Cẳng chân trái sưng nề biến dạng gập góc 1/3 giữa dưới, cử động bất thường, mạch mu chân và chày sau trái bắt tốt, cảm giác đầu chi bình thường.",
        "cdha": "X-quang cẳng chân trái: Gãy 1/3 giữa dưới xương chày và xương mác trái di lệch. Huyết học: Hb: 145 g/L, Bạch cầu: 8.4 G/L, Tiểu cầu: 235 G/L. Đông máu: PT-INR: 1.07, Fibrinogen: 2.59 g/L, APTT: 0.89. Sinh hóa: Glucose: 6.7 mmol/L, Ure: 3.2 mmol/L, Creatinin: 66 umol/L, AST: 34 U/L, ALT: 44 U/L."
    },
    "0004037901": { # NGUYỄN THỊ LAN ANH
        "idx": "13",
        "pp_pt": "Phẫu thuật kết hợp xương 1/3 trên và giữa xương chày, 1/3 trên xương mác trái bằng nẹp vít khóa (KHX)",
        "vo_cam": "Tê tủy sống",
        "mallampati_col": 1, # Loại I
        "loai_pt_col": 3,    # Loại I
        "asa_col": 1,        # Loại I
        "nguy_co_col": 3,    # Sạch nhiễm
        "du_tru_mau": "350",
        "nhom_mau": "O Rh(+)",
        "benh_su": "Bệnh nhân trượt chân cầu thang ngã đập cẳng chân trái, đau dữ dội, không thể đứng hay vận động chân trái.",
        "tien_su": "Khỏe mạnh, không có bệnh lý nền.",
        "tom_tat": "Bệnh nhân tỉnh. Cẳng chân trái sưng nề nhiều vùng 1/3 trên và giữa, đau chói chày mác, gãy kín, mạch mu chân trái bắt rõ, không chèn ép khoang.",
        "cdha": "X-quang cẳng chân trái: Gãy 1/3 trên và giữa xương chày, gãy 1/3 trên xương mác trái. Huyết học: Hb: 124 g/L, Bạch cầu: 9.9 G/L, Tiểu cầu: 150 G/L. Đông máu: PT-INR: 1.06, Fibrinogen: 2.29 g/L, APTT: 0.82. Sinh hóa: Ure: 5.3 mmol/L, Creatinin: 73 umol/L, AST: 31 U/L, ALT: 11 U/L."
    }
}

ns = {
    'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
    'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
}
ET.register_namespace('w', ns['w'])
ET.register_namespace('r', ns['r'])

def set_p_text(p, label, val_text="", bold_val=False):
    pPr = p.find('w:pPr', ns)
    for child in list(p):
        if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr':
            p.remove(child)
    # Label run
    r1 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
    t1 = ET.SubElement(r1, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
    t1.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    t1.text = label
    # Value run
    if val_text:
        r2 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
        if bold_val:
            rPr = ET.SubElement(r2, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}rPr')
            ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}b')
        t2 = ET.SubElement(r2, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
        t2.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        t2.text = val_text

def set_p_twoparts(p, l1, v1, l2, v2, bold1=False, bold2=False):
    pPr = p.find('w:pPr', ns)
    for child in list(p):
        if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr':
            p.remove(child)
    # Part 1
    r1 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
    t1 = ET.SubElement(r1, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
    t1.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    t1.text = l1
    if v1:
        rv1 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
        if bold1:
            rPr = ET.SubElement(rv1, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}rPr')
            ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}b')
        tv1 = ET.SubElement(rv1, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
        tv1.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        tv1.text = v1
    # Part 2
    r2 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
    t2 = ET.SubElement(r2, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
    t2.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    t2.text = l2
    if v2:
        rv2 = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
        if bold2:
            rPr = ET.SubElement(rv2, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}rPr')
            ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}b')
        tv2 = ET.SubElement(rv2, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
        tv2.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        tv2.text = v2

def set_cell_text(cell, lines):
    p = cell.find('w:p', ns)
    if p is None:
        p = ET.SubElement(cell, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p')
    for child in list(p):
        if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr':
            p.remove(child)
    r = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
    for i, line in enumerate(lines):
        if i > 0:
            ET.SubElement(r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}br')
        t = ET.SubElement(r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
        t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        t.text = line

def set_table1_checkbox(tbl, row_idx, target_col):
    rows = tbl.findall('.//w:tr', ns)
    r = rows[row_idx]
    cells = r.findall('.//w:tc', ns)
    checkbox_cols = [1, 3, 5, 7]
    if row_idx == 2:
        checkbox_cols = [1, 3, 5, 7, 9]
    for c_idx in checkbox_cols:
        if c_idx < len(cells):
            c = cells[c_idx]
            p = c.find('w:p', ns)
            if p is not None:
                for child in list(p):
                    if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr':
                        p.remove(child)
                r_elem = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
                t_elem = ET.SubElement(r_elem, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
                t_elem.text = "x" if c_idx == target_col else ""

generated_files = []

for p in all_patients:
    p_code = p["PatientCode"]
    if p_code not in ban2_data:
        continue
    
    info = ban2_data[p_code]
    idx_str = info["idx"]
    safe_name = p.get("Ho_Va_Ten", "").strip().replace(" ", "_")
    out_filename = f"{idx_str}_PT01_BienBanThongQuaMo_{safe_name}_{p_code}.docx"
    out_filepath = os.path.join(out_dir, out_filename)
    
    dob_raw = p.get("Ngay_Sinh", "")
    dob_fmt = dob_raw
    if len(dob_raw) >= 8:
        dob_fmt = f"{dob_raw[6:8]}/{dob_raw[4:6]}/{dob_raw[0:4]}"

    with zipfile.ZipFile(template_file, "r") as zin:
        xml_content = zin.read("word/document.xml")
        root = ET.fromstring(xml_content)
        body = root.find('w:body', ns)
        
        # Table 0: Add Treatment code to header Cell 2
        tbl0 = body[1]
        c2 = tbl0.findall('.//w:tc', ns)[2]
        set_cell_text(c2, ["MS: PT -01", f"Mã: {p.get('Ma_Ho_So', '')}"])
        
        # P06: Họ và tên người bệnh:
        set_p_text(body[6], "Họ và tên người bệnh: ", p.get("Ho_Va_Ten", "").strip().upper(), bold_val=True)
        
        # P07: Ngày sinh & Giới tính
        set_p_twoparts(body[7], "Ngày sinh: ", dob_fmt, "                                   Giới tính: ", p.get("Gioi_Tinh", ""))
        
        # P08: Địa chỉ
        set_p_text(body[8], "Địa chỉ: ", p.get("Dia_Chi", ""))
        
        # P09: Vào viện:
        set_p_text(body[9], "Vào viện: ", p.get("Ngay_Gio_Vao_Vien", ""))
        
        # P10: Chẩn đoán:
        diag_full = (p.get("IcdName", "") + (" (" + p.get("IcdText", "") + ")" if p.get("IcdText") else "")).strip()
        set_p_text(body[10], "Chẩn đoán: ", diag_full)
        
        # P11: Tiền sử:
        set_p_text(body[11], "Tiền sử: ", info.get("tien_su", ""))
        
        # P12: Bệnh sử:
        set_p_text(body[12], "Bệnh sử: ", info.get("benh_su", ""))
        
        # P13: Thời gian hội chẩn:
        set_p_text(body[13], "Thời gian hội chẩn: ", "14 giờ 00 ngày 13/09/2026")
        
        # P14: Tóm tắt tình trạng bệnh:
        set_p_text(body[14], "Tóm tắt tình trạng bệnh: ", info.get("tom_tat", ""))
        
        # P15: Các xét nghiệm, CĐHA:
        set_p_text(body[15], "Các xét nghiệm, chẩn đoán hình ảnh (ghi kết quả CLS có giá trị về: chẩn đoán bệnh, đánh giá tình trạng người bệnh hiện tại, xét nghiệm tiền phẫu): ", info.get("cdha", ""))
        
        # P16: Nhóm máu & Dự trù máu
        set_p_twoparts(body[16], "Nhóm máu: ", info.get("nhom_mau", "O Rh(+)"), "        Dự trù máu: ", f"{info.get('du_tru_mau', '0')} (ml)")
        
        # P17: Phương pháp phẫu thuật:
        set_p_text(body[17], "Phương pháp phẫu thuật (Mổ hở, mổ nội soi, phương pháp khác): ", info.get("pp_pt", ""))
        
        # P18: Phương pháp vô cảm dự kiến:
        set_p_text(body[18], "Phương pháp vô cảm dự kiến: (Mê nội khí quản, mê mask thanh quản, tiền mê, tê ngoài màng cứng, mê tĩnh mạch, tê tủy sống, tê khoang cùng, tê tại chỗ): ", info.get("vo_cam", ""))
        
        # Table 1: Đánh giá điều kiện GMHS (body[21])
        tbl1 = body[21]
        set_table1_checkbox(tbl1, 0, info["mallampati_col"]) # Mallampati
        set_table1_checkbox(tbl1, 1, info["loai_pt_col"])    # Loại phẫu thuật
        set_table1_checkbox(tbl1, 2, info["asa_col"])        # ASA
        set_table1_checkbox(tbl1, 3, info["nguy_co_col"])    # Phân loại nguy cơ
        
        # P22: Nhiễm khuẩn vết mổ:
        set_p_text(body[22], "Nhiễm khuẩn vết mổ: ", "Không")
        
        # P23: Kháng sinh dự phòng:
        set_p_text(body[23], "Kháng sinh dự phòng: ", "CÓ (Cefazolin 2g tiêm TM trước rạch da 30 phút)")
        
        # P24: Phẫu thuật viên chính:
        ptv_name = info.get("ptv", "BS Giang")
        set_p_text(body[24], "Phẫu thuật viên chính: ", ptv_name)
        
        # P25: Ngày, giờ phẫu thuật dự kiến:
        set_p_text(body[25], "Ngày, giờ phẫu thuật dự kiến: ", "08 giờ 00 phút, ngày 14/09/2026")
        
        # P26: Các biến chứng, nguy cơ, khó khăn đặc biệt cần lưu ý:
        set_p_text(body[26], "Các biến chứng, nguy cơ, khó khăn đặc biệt cần lưu ý:")
        
        # P27: Chi tiết biến chứng
        set_p_text(body[27], "Chảy máu, nhiễm trùng, đau sau mổ, tổn thương mạch máu thần kinh, dị ứng, phản vệ, tử vong.")
        
        # P29: Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt:
        set_p_text(body[29], "Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt: ", "Theo dõi sát dấu hiệu sinh tồn, chuẩn bị hộp chống sốc, giải thích kỹ thân nhân và người bệnh.")
        
        # Table 2: Signatures (body[30])
        tbl2 = body[30]
        sig_cells = tbl2.findall('.//w:tc', ns)
        set_cell_text(sig_cells[0], ["Bác sỹ phẫu thuật", "(Ký, ghi rõ họ tên)", "", "", "", ptv_name])
        set_cell_text(sig_cells[1], ["Bác sỹ gây mê", "(Ký, ghi rõ họ tên)", "", "", "", "BS Khoa PT - GMHS"])
        set_cell_text(sig_cells[2], ["Lãnh đạo khoa lâm sàng", "(Ký, ghi rõ họ tên)", "", "", "", "TS.BS. Nguyễn Văn Trung"])
        set_cell_text(sig_cells[3], ["Lãnh đạo duyệt mổ/ KHTH", "(Ký, ghi rõ họ tên)", "", "", "", "TS.BS. Nguyễn Văn Trung"])
        
        # Serialize back to XML
        xml_out = ET.tostring(root, encoding="utf-8")
        
        with zipfile.ZipFile(out_filepath, "w", zipfile.ZIP_DEFLATED) as zout:
            for item in zin.infolist():
                if item.filename == "word/document.xml":
                    zout.writestr(item, xml_out)
                else:
                    zout.writestr(item, zin.read(item.filename))
                    
        generated_files.append(out_filepath)
        print(f"[{idx_str}] Created official PT01 format: {out_filename}")

print(f"\n✅ Hoàn tất tạo {len(generated_files)} biên bản Bàn 2 theo chuẩn 100% mẫu gốc PT-01!")
