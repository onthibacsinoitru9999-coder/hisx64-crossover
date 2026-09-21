# -*- coding: utf-8 -*-
import json, zipfile, os, sys, xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

template_file = 'Templates/mau_bbhc .docx'
out_dir = 'Reports/BienBanThongQuaMo_PT01_16092026'
os.makedirs(out_dir, exist_ok=True)

patients_data = [
    {
        'idx': '01',
        'code': '0003369139',
        'treatment_code': '000007205580',
        'name': 'PHẠM THỊ LIÊN',
        'dob': '09/10/1991',
        'gender': 'Nữ',
        'address': 'Phường Hoàng Mai, Nghệ An',
        'in_time': '07:06 ngày 10/09/2026',
        'diag': 'Viêm đốt sống đĩa đệm + Hẹp ống sống L2-L3 (M48.30)',
        'tien_su': 'Chưa phát hiện bệnh lý mạn tính đặc biệt.',
        'benh_su': 'Đau thắt lưng âm ỉ kéo dài nhiều tháng, đợt này đau tăng dữ dội, hạn chế cúi ngửa, kèm theo dấu hiệu chèn ép rễ L3 bên trái lan xuống mặt trước trong đùi, không sốt, không rối loạn cơ tròn.',
        'tom_tat': 'Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng trung bình. Co cứng cơ cạnh sống thắt lưng, ấn đau chói vùng gai sau và cạnh sống L2 - L3. Vận động: Cơ lực 2 chân 5/5, cảm giác tê bì mặt trước trong đùi trái. Phản xạ gân xương bình thường, Lasegue (-) 2 bên. Tim đều, phổi không rale, bụng mềm, đại tiểu tiện tự chủ. Huyết động ổn định.',
        'cdha': '• MRI Cột sống thắt lưng (10/09/2026): Đĩa đệm L2/3 giảm chiều cao, giảm tín hiệu trên xung STIR. Thân đốt sống L2, L3 tín hiệu không đồng nhất, có tổn thương viêm hình soi gương qua khe liên đốt, phát triển ra phần mềm trước sống và khoang ngoài màng cứng, gây hẹp ống sống ngang mức (đường kính trước - sau 7.7 mm), chèn ép bao màng cứng/chùm đuôi ngựa; chèn ép rễ L3 trong ngách bên và lỗ tiếp hợp bên trái. Phình kèm rách vòng xơ đĩa đệm L5-S1.\n• CT Scanner Cột sống thắt lưng (14/09/2026): Thân đốt sống L2-L3 có tổn thương tiêu và đặc xương lan tỏa, không thấy lùi tường sau.\n• X-Quang động CSTL gập ưỡn (15/09/2026): Hình ảnh ổ đặc xương không đều thân L2, L3; hẹp khe liên đốt L2/3.\n• Điện tim: Nhịp xoang đều, tần số 73 chu kỳ/phút, trục trung gian.\n• Huyết học: Hb: 134 g/L, Bạch cầu: 11.04 G/L, Tiểu cầu: 395 G/L.\n• Đông máu: PT-INR: 0.98, Fibrinogen: 5.32 g/L, APTT: 22.5 giây.\n• Sinh hóa: Glucose: 5.6 mmol/L, Ure: 5.9 mmol/L, Creatinin: 58 umol/L, AST: 31 U/L, ALT: 36 U/L.',
        'nhom_mau': 'O Rh(+)',
        'du_tru_mau': '350',
        'pp_pt': 'Phẫu thuật cố định cột sống lối sau bằng nẹp vít qua cuống, giải ép rễ L3, hàn xương liên thân đốt L2-L3 (Cố định cột sống giải ép)',
        'vo_cam': 'Mê nội khí quản',
        'mallampati_col': 1, # Loại I
        'loai_pt_col': 3,    # Loại I
        'asa_col': 3,        # Loại II
        'nguy_co_col': 1,    # Sạch
        'ptv': 'TS.BS. Nguyễn Văn Trung'
    },
    {
        'idx': '02',
        'code': '0004018669',
        'treatment_code': '000007173948',
        'name': 'NGÔ VĂN XƯNG',
        'dob': '28/10/1944',
        'gender': 'Nam',
        'address': 'Thôn Thanh Bản 2, Xã Vạn Xuân, Hưng Yên',
        'in_time': '05:59 ngày 07/09/2026',
        'diag': 'Xẹp cấp L1 / Loãng xương - Đái tháo đường típ 2 - Tăng huyết áp - Tiền sử phẫu thuật u phì đại tuyến tiền liệt (M48.50)',
        'tien_su': 'Tiền sử mổ u phì đại tuyến tiền liệt cách 2 tuần có gây tê tủy sống. Đái tháo đường típ 2, Tăng huyết áp đang điều trị theo đơn.',
        'benh_su': 'Cách vào viện 1 tuần, bệnh nhân ngã ngồi đập mông xuống nền cứng, xuất hiện đau dữ dội vùng cột sống thắt lưng, đau tăng mạnh khi vận động thay đổi tư thế, kèm theo giật cơ cạnh sống, không tê yếu chân.',
        'tom_tat': 'Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng già yếu, da niêm mạc hồng, không phù. Ấn đau chói vùng cột sống ngực - thắt lưng (ngang mức L1), co cứng cơ cạnh sống, VAS 4/10. Không tê chân, cơ lực 2 chi dưới 5/5, Lasegue (-) 2 bên. Tim đều, không rale phổi, huyết áp 130/80 mmHg.',
        'cdha': '• DEXA đo mật độ xương (11/09/2026): Loãng xương nặng (T-score < -2.5).\n• MRI Cột sống thắt lưng (12/09/2026): Hình ảnh xẹp cấp kèm phù tủy xương thân đốt sống L1 tăng tín hiệu trên xung STIR.\n• X-quang ngực & CSTL (07/09/2026): Thoái hóa đốt sống ngực và thắt lưng.\n• Siêu âm tim (15/09/2026): Kích thước và chức năng tâm thu thất trái trong giới hạn bình thường (EF 64%), hở van 3 lá nhẹ, không rối loạn vận động vùng.\n• Điện tim (07/09/2026): Nhịp xoang đều, tần số 75 ck/ph, trục trung gian, không ST chênh.\n• Huyết học: Hb: 153 g/L, Bạch cầu: 8.9 G/L, Tiểu cầu: 223 G/L.\n• Đông máu: PT-INR: 1.00, Fibrinogen: 2.80 g/L, APTT: 29.7 giây.\n• Sinh hóa: Glucose: 8.9 mmol/L, Ure: 4.5 mmol/L, Creatinin: 71 umol/L, AST: 32 U/L, ALT: 54 U/L.',
        'nhom_mau': 'O Rh(+)',
        'du_tru_mau': '0',
        'pp_pt': 'Bơm xi măng sinh học tạo hình thân đốt sống L1 qua cuống (BXM L1)',
        'vo_cam': 'Tiền mê + Tê tại chỗ',
        'mallampati_col': 3, # Loại II
        'loai_pt_col': 3,    # Loại I
        'asa_col': 5,        # Loại III (82 tuổi, ĐTĐ típ 2, THA, mổ TTL gần đây)
        'nguy_co_col': 1,    # Sạch
        'ptv': 'TS.BS. Nguyễn Văn Trung'
    },
    {
        'idx': '03',
        'code': '0004035396',
        'treatment_code': '000007217332',
        'name': 'NGUYỄN THỊ ÚT',
        'dob': '01/08/1950',
        'gender': 'Nữ',
        'address': 'Xã Nam Tiên Hưng, Hưng Yên',
        'in_time': '17:05 ngày 11/09/2026',
        'diag': 'Xẹp cấp L2, L4 - Xẹp cũ L5 / Loãng xương nặng - Tăng huyết áp (M48.50)',
        'tien_su': 'Tăng huyết áp, đau cột sống thắt lưng nhiều năm, tiền sử dùng thuốc nam kéo dài (kiểu hình Cushing nhẹ).',
        'benh_su': 'Khoảng 1 tuần nay đau thắt lưng dữ dội tăng dần, đau tăng khi ngồi dậy hoặc đi lại, nằm nghỉ đỡ đau, không yếu liệt 2 chân. Đã chụp MRI tại BVĐK Thái Bình phát hiện xẹp phù tủy xương chuyển Bệnh viện Bạch Mai.',
        'tom_tat': 'Bệnh nhân tỉnh, thể trạng gầy (cân nặng 39 kg), kiểu hình Cushing (+/-). Đau cột sống thắt lưng nhiều, ấn đau chói mỏm gai L2 và L4, VAS 4/10. Lasegue 2 bên 90 độ, cơ lực 2 chân 5/5, không rối loạn cảm giác nông sâu. Tim đều, phổi sáng, HA 115/70 mmHg, Mạch 75 ck/ph.',
        'cdha': '• MRI Cột sống thắt lưng (11/09/2026): Đường cong sinh lý giảm, vẹo sang trái đỉnh L2 (góc Cobb 15 độ). Xẹp cấp tính thân đốt sống L2, L4 có phù tủy xương tăng tín hiệu trên STIR, không đẩy lồi tường sau, không gây hẹp ống sống. Xẹp mạn tính thân đốt sống L5 (không phù tủy). Phình đĩa đệm L2/3, L3/4, L4/5 gây hẹp ống sống (đường kính trước - sau 7.0 mm), chèn ép rễ L3 - L5 hai bên.\n• X-Quang CSTL thẳng nghiêng (15/09/2026): Giảm chiều cao kèm giảm đậm độ xương các thân đốt sống L2, L4, L5.\n• Siêu âm tim (15/09/2026): Kích thước và chức năng tâm thu thất trái bình thường (EF 62%), hở van 3 lá nhẹ.\n• X-Quang ngực thẳng (15/09/2026): Tim phổi bình thường.\n• Huyết học: Hb: 136 g/L, Bạch cầu: 6.81 G/L, Tiểu cầu: 323 G/L.\n• Đông máu: PT-INR: 0.93, Fibrinogen: 4.33 g/L, APTT: 35.0 giây.\n• Sinh hóa: Glucose: 4.0 mmol/L, Ure: 6.6 mmol/L, Creatinin: 49 umol/L, AST: 37 U/L, ALT: 19 U/L.',
        'nhom_mau': 'O Rh(+)',
        'du_tru_mau': '0',
        'pp_pt': 'Bơm xi măng sinh học tạo hình thân đốt sống L2, L4 qua cuống (BXM L2, L4)',
        'vo_cam': 'Tiền mê + Tê tại chỗ',
        'mallampati_col': 3, # Loại II
        'loai_pt_col': 3,    # Loại I
        'asa_col': 3,        # Loại II
        'nguy_co_col': 1,    # Sạch
        'ptv': 'BS. Trịnh Minh Đức'
    },
    {
        'idx': '04',
        'code': '0004036373',
        'treatment_code': '000007219593',
        'name': 'BÙI THỊ RỊU',
        'dob': '09/02/1950',
        'gender': 'Nữ',
        'address': 'Xã Xuân Hưng, Ninh Bình',
        'in_time': '07:09 ngày 12/09/2026',
        'diag': 'Xẹp cấp L1, L3 / Xẹp L2 cũ - Phình đĩa đệm L2/3, L3/4 và L4/5 chèn ép rễ - Thoái hóa cột sống thắt lưng / Đái tháo đường típ 2 (M48.50)',
        'tien_su': 'Đái tháo đường típ 2 điều trị thường xuyên, thoái hóa cột sống thắt lưng nhiều năm.',
        'benh_su': 'Cách vào viện 1 tuần xuất hiện đau cột sống thắt lưng tăng dữ dội, đau lan xuyên sang bụng và tê bì lan xuống hai chân, hạn chế vận động cúi nghiêng.',
        'tom_tat': 'Bệnh nhân tỉnh, tiếp xúc tốt, da niêm mạc kém hồng. Cột sống thắt lưng: Ấn đau chói các mỏm gai sau L1 và L3, co rút cơ thắt lưng. Cơ lực chi dưới 5/5, tê bì lan mặt ngoài đùi hai bên, đại tiểu tiện tự chủ. Mạch 83 ck/ph, HA 130/80 mmHg, tim phổi ổn định.',
        'cdha': '• MRI Cột sống thắt lưng (12/09/2026): Xẹp thân đốt sống L1, L3 cấp tính có phù tủy xương rõ trên STIR, không đẩy lồi tường sau, không hẹp ống sống. Xẹp mạn tính thân đốt sống L2 (không phù tủy). Thoái hóa Modic I, II thân L4, L5. Phình đĩa đệm L2/3, L3/4 và L4/5 ra xung quanh, kèm dày dây chằng vàng và thoái hóa diện khớp, gây hẹp ống sống (đường kính trước sau ~6.0 mm), chèn ép rễ L3 đến L5 trong ngách bên hai bên.\n• DEXA đo mật độ xương (15/09/2026): Loãng xương nặng.\n• X-Quang CSTL (15/09/2026): Hình ảnh thoái hóa cột sống thắt lưng, xẹp thân L1, L2, L3.\n• Siêu âm tim (15/09/2026): Chức năng tâm thu thất trái bình thường, hở van 3 lá nhẹ.\n• Huyết học: Hb: 117 g/L, Bạch cầu: 9.74 G/L, Tiểu cầu: 493 G/L.\n• Đông máu: PT-INR: 0.97, Fibrinogen: 5.55 g/L, APTT: 32.7 giây.\n• Sinh hóa: Glucose: 6.9 mmol/L, Ure: 6.8 mmol/L, Creatinin: 51 umol/L, AST: 18 U/L, ALT: 11 U/L.',
        'nhom_mau': 'A Rh(+)',
        'du_tru_mau': '0',
        'pp_pt': 'Bơm xi măng sinh học tạo hình thân đốt sống L1, L3 qua cuống (BXM L1, L3)',
        'vo_cam': 'Tiền mê + Tê tại chỗ',
        'mallampati_col': 1, # Loại I
        'loai_pt_col': 3,    # Loại I
        'asa_col': 3,        # Loại II
        'nguy_co_col': 1,    # Sạch
        'ptv': 'BS. Trịnh Minh Đức'
    },
    {
        'idx': '05',
        'code': '0003860421',
        'treatment_code': '000007231784',
        'name': 'ĐOÀN THỊ LỰU',
        'dob': '18/05/1953',
        'gender': 'Nữ',
        'address': 'Phường Trường Thi, Ninh Bình',
        'in_time': '08:35 ngày 14/09/2026',
        'diag': 'Thoát vị đĩa đệm cột sống L3-4, L4-5 - Hẹp ống sống nặng / Tăng huyết áp (M51.2)',
        'tien_su': 'Tăng huyết áp điều trị đều, đau cột sống thắt lưng nhiều năm điều trị nội khoa nhiều đợt.',
        'benh_su': 'Đợt này đau thắt lưng dữ dội tăng nhiều, đau buốt lan dọc xuống mông và mặt sau hai chân (bên phải nhiều hơn bên trái), tê bì châm chích bàn chân, đi lại quãng ngắn phải dừng lại nghỉ (dấu hiệu đi khập khiễng cách hồi thần kinh).',
        'tom_tat': 'Bệnh nhân tỉnh, thể trạng trung bình, đau CSTL VAS 3-4 điểm. Đau lan buốt và tê xuống 2 chân, nghiệm pháp căng rễ Lasegue (+) bên phải 60 độ. Cơ lực 2 chân 5/5, chưa có liệt cơ duỗi ngón cái, phản xạ gân gót bên phải giảm so với bên trái, không rối loạn cơ tròn. Tim đều, HA 125/80 mmHg.',
        'cdha': '• MRI Cột sống thắt lưng: Thoát vị đĩa đệm L3-4, L4-5 và L5-S1 dưới dây chằng dọc sau kèm phì đại diện khớp và dày dây chằng vàng. Hẹp nặng ống sống ngang mức L4/5 (đường kính trước - sau chỉ còn 4.5 mm), chèn ép mạnh bao màng cứng và chùm đuôi ngựa, chèn ép rễ thần kinh ngang mức và rễ đi xuống tiếp dưới.\n• Điện cơ chi dưới: Kéo dài thời gian tiềm, giảm biên độ vận động dây TK chày bên phải, mất phản xạ H bên phải -> Tổn thương dây TK chày (P) - Tổn thương rễ thần kinh S1 bên phải.\n• Siêu âm mạch chi dưới: Xơ vữa rải rác hệ ĐM chi dưới 2 bên, không có hẹp tắc có ý nghĩa huyết động.\n• Huyết học: Hb: 128 g/L, Bạch cầu: 7.08 G/L, Tiểu cầu: 265 G/L.\n• Đông máu: PT-INR: 0.96, Fibrinogen: 3.12 g/L, APTT: 24.4 giây.\n• Sinh hóa: Glucose: 8.4 mmol/L, Ure: 4.6 mmol/L, Creatinin: 61 umol/L, AST: 43 U/L, ALT: 41 U/L.',
        'nhom_mau': 'B Rh(+)',
        'du_tru_mau': '350',
        'pp_pt': 'Phẫu thuật cố định cột sống bằng nẹp vít qua cuống, hàn xương liên thân đốt lối sau qua lỗ liên hợp 2 tầng L3-4, L4-5, giải ép thần kinh (TLIF L3-4, L4-5)',
        'vo_cam': 'Mê nội khí quản',
        'mallampati_col': 3, # Loại II
        'loai_pt_col': 1,    # Đặc biệt
        'asa_col': 3,        # Loại II
        'nguy_co_col': 1,    # Sạch
        'ptv': 'TS.BS. Nguyễn Văn Trung'
    },
    {
        'idx': '06',
        'code': '0004031278',
        'treatment_code': '000007206021',
        'name': 'TRẦN THÁI DƯƠNG',
        'dob': '12/12/1947',
        'gender': 'Nam',
        'address': 'Xã Kiến Xương, Hưng Yên',
        'in_time': '07:21 ngày 10/09/2026',
        'diag': 'Xẹp thân đốt sống L3 / Loãng xương nặng (M48.50) - Phì đại tuyến tiền liệt, Viêm bàng quang, Tăng huyết áp, Tiền sử phẫu thuật thay khớp háng trái năm 2025 do gãy cổ xương đùi',
        'tien_su': 'Tăng huyết áp (Exforge 5/80), mổ thay khớp háng trái năm 2025. Phì đại tuyến tiền liệt, viêm bàng quang điều trị ngoại trú.',
        'benh_su': 'Cách vào viện 1 ngày, xuất hiện đau cột sống thắt lưng và cột sống cổ dữ dội, đau lan mặt sau chân trái, hạn chế vận động đi lại, không yếu liệt chi.',
        'tom_tat': 'Tỉnh, tiếp xúc tốt, nghe kém, thể trạng trung bình. Cột sống thắt lưng: Ấn đau chói mỏm gai L3, hạn chế vận động xoay cúi, đau hông trái VAS 3-4/10; Lasegue (Trái) 70 độ. Cột sống cổ: Đau mỏi cơ cạnh sống cổ, hội chứng màng não (-). Khớp háng trái sẹo mổ cũ liền tốt, vận động trong giới hạn cho phép. Mạch 73 ck/ph, HA 116/70 mmHg.',
        'cdha': '• MRI Cột sống thắt lưng (10/09/2026): Xẹp kèm phù tủy xương thân đốt sống L3 cấp tính, không đẩy lồi tường sau. Phình đĩa đệm L3-4, L4-5; thoát vị đĩa đệm L5-S1 thể trung tâm gây hẹp ống sống (đường kính trước sau 8.6 - 9.5 mm), tiếp xúc rễ L5 trái và S1 phải trong ngách bên. Phù nề phần mềm thắt lưng L2-L3.\n• DEXA đo mật độ xương (10/09/2026): Loãng xương nặng.\n• X-Quang Cột sống cổ (10/09/2026): Thoái hóa cột sống cổ C4-C6, hẹp nhẹ lỗ tiếp hợp C4/5 phải, C3/4 và C4/5 trái.\n• Siêu âm tim (10/09/2026): Kích thước và chức năng tâm thu thất trái bình thường, hở hai lá nhẹ.\n• Siêu âm ổ bụng (10/09/2026): Tiền liệt tuyến phì đại (~42 gram), thành bàng quang dày 7mm.\n• Huyết học: Hb: 123 g/L, Bạch cầu: 6.82 G/L, Tiểu cầu: 295 G/L.\n• Đông máu: PT-INR: 1.06, Fibrinogen: 3.51 g/L, APTT: 35.8 giây.\n• Sinh hóa: Glucose: 5.3 mmol/L, Ure: 7.1 mmol/L, Creatinin: 115 umol/L, AST: 16 U/L, ALT: 9 U/mL.',
        'nhom_mau': 'O Rh(+)',
        'du_tru_mau': '0',
        'pp_pt': 'Bơm xi măng sinh học tạo hình thân đốt sống L3 qua cuống (BXM L3)',
        'vo_cam': 'Tiền mê + Tê tại chỗ',
        'mallampati_col': 1, # Loại I
        'loai_pt_col': 3,    # Loại I
        'asa_col': 3,        # Loại II
        'nguy_co_col': 1,    # Sạch
        'ptv': 'BS. Trịnh Minh Đức'
    }
]

ns = {
    'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
    'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
}
ET.register_namespace('w', ns['w'])
ET.register_namespace('r', ns['r'])

def add_run(p, text, font_name='Times New Roman', sz='24', bold=False, italic=False):
    r = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}r')
    rPr = ET.SubElement(r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}rPr')
    
    rFonts = ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}rFonts')
    rFonts.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}ascii', font_name)
    rFonts.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}hAnsi', font_name)
    rFonts.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}cs', font_name)
    rFonts.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}eastAsia', font_name)
    
    sz_el = ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}sz')
    sz_el.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val', str(sz))
    szCs_el = ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}szCs')
    szCs_el.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val', str(sz))
    
    if bold:
        ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}b')
        ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}bCs')
    if italic:
        ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}i')
        ET.SubElement(rPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}iCs')
        
    t = ET.SubElement(r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
    t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    t.text = text
    return r

def clear_p(p):
    for child in list(p):
        if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr':
            p.remove(child)

def set_p_text(p, label, val_text='', bold_val=False, sz='24', bold_label=False):
    clear_p(p)
    add_run(p, label, font_name='Times New Roman', sz=sz, bold=bold_label)
    if val_text:
        lines = val_text.split('\n')
        for idx, line in enumerate(lines):
            if idx > 0:
                last_r = p[-1]
                ET.SubElement(last_r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}br')
                t = ET.SubElement(last_r, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t')
                t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
                t.text = line
            else:
                add_run(p, line, font_name='Times New Roman', sz=sz, bold=bold_val)

def set_p_twoparts(p, l1, v1, l2, v2, bold1=False, bold2=False, sz='24'):
    clear_p(p)
    add_run(p, l1, font_name='Times New Roman', sz=sz, bold=False)
    if v1:
        add_run(p, v1, font_name='Times New Roman', sz=sz, bold=bold1)
    add_run(p, l2, font_name='Times New Roman', sz=sz, bold=False)
    if v2:
        add_run(p, v2, font_name='Times New Roman', sz=sz, bold=bold2)

def rebuild_cell_paragraphs(cell, para_list):
    for child in list(cell):
        if child.tag != '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}tcPr':
            cell.remove(child)
    for p_info in para_list:
        p = ET.SubElement(cell, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p')
        pPr = ET.SubElement(p, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pPr')
        align = p_info.get('align', 'center')
        jc = ET.SubElement(pPr, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}jc')
        jc.set('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}val', align)
        
        text = p_info.get('text', '')
        sz = p_info.get('sz', '24')
        bold = p_info.get('bold', False)
        italic = p_info.get('italic', False)
        
        if text:
            add_run(p, text, font_name='Times New Roman', sz=sz, bold=bold, italic=italic)
        else:
            add_run(p, ' ', font_name='Times New Roman', sz=sz)

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
            if p is None:
                p = ET.SubElement(c, '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p')
            clear_p(p)
            val = 'x' if c_idx == target_col else ''
            add_run(p, val, font_name='Times New Roman', sz='24', bold=True)

generated_files = []

for item in patients_data:
    idx_str = item['idx']
    p_code = item['code']
    safe_name = item['name'].strip().replace(' ', '_')
    out_filename = f'{idx_str}_PT01_BienBanThongQuaMo_{safe_name}_{p_code}.docx'
    out_filepath = os.path.join(out_dir, out_filename)
    
    with zipfile.ZipFile(template_file, 'r') as zin:
        xml_content = zin.read('word/document.xml')
        root = ET.fromstring(xml_content)
        body = root.find('w:body', ns)
        
        # Table 0: Header (body[1])
        tbl0 = body[1]
        c0 = tbl0.findall('.//w:tc', ns)[0]
        rebuild_cell_paragraphs(c0, [
            {'text': 'BỆNH VIỆN BẠCH MAI', 'sz': '26', 'bold': False, 'align': 'center'},
            {'text': 'Khoa Ngoại tổng hợp - CS Ninh Bình', 'sz': '26', 'bold': True, 'align': 'center'}
        ])
        
        c1 = tbl0.findall('.//w:tc', ns)[1]
        rebuild_cell_paragraphs(c1, [
            {'text': 'CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM', 'sz': '24', 'bold': True, 'align': 'center'},
            {'text': 'Độc lập - Tự do - Hạnh phúc', 'sz': '26', 'bold': False, 'align': 'center'}
        ])
        
        c2 = tbl0.findall('.//w:tc', ns)[2]
        rebuild_cell_paragraphs(c2, [
            {'text': 'MS: PT -01', 'sz': '24', 'bold': False, 'align': 'center'},
            {'text': f'Mã: {item["treatment_code"]}', 'sz': '24', 'bold': True, 'align': 'center'}
        ])
        
        # P06: Họ và tên người bệnh
        set_p_text(body[6], 'Họ và tên người bệnh: ', item['name'].upper(), bold_val=True, sz='26')
        
        # P07: Ngày sinh & Giới tính
        set_p_twoparts(body[7], 'Ngày sinh: ', item['dob'], '                                   Giới tính: ', item['gender'], sz='24')
        
        # P08: Địa chỉ
        set_p_text(body[8], 'Địa chỉ: ', item['address'], sz='24')
        
        # P09: Vào viện
        set_p_text(body[9], 'Vào viện: ', item['in_time'], sz='24')
        
        # P10: Chẩn đoán
        set_p_text(body[10], 'Chẩn đoán: ', item['diag'], bold_val=True, sz='24')
        
        # P11: Tiền sử
        set_p_text(body[11], 'Tiền sử: ', item['tien_su'], sz='24')
        
        # P12: Bệnh sử
        set_p_text(body[12], 'Bệnh sử: ', item['benh_su'], sz='24')
        
        # P13: Thời gian hội chẩn
        set_p_text(body[13], 'Thời gian hội chẩn: ', '14 giờ 00 phút, ngày 15 tháng 09 năm 2026', sz='24')
        
        # P14: Tóm tắt tình trạng bệnh
        set_p_text(body[14], 'Tóm tắt tình trạng bệnh: ', item['tom_tat'], sz='24')
        
        # P15: Các xét nghiệm, CĐHA
        set_p_text(body[15], 'Các xét nghiệm, chẩn đoán hình ảnh (ghi kết quả CLS có giá trị về: chẩn đoán bệnh, đánh giá tình trạng người bệnh hiện tại, xét nghiệm tiền phẫu):\n', item['cdha'], sz='24')
        
        # P16: Nhóm máu & Dự trù máu
        set_p_twoparts(body[16], 'Nhóm máu: ', item['nhom_mau'], '        Dự trù máu: ', f'{item["du_tru_mau"]} (ml)', bold1=True, bold2=True, sz='24')
        
        # P17: Phương pháp phẫu thuật
        set_p_text(body[17], 'Phương pháp phẫu thuật (Mổ hở, mổ nội soi, phương pháp khác): ', item['pp_pt'], bold_val=True, sz='24')
        
        # P18: Phương pháp vô cảm dự kiến
        set_p_text(body[18], 'Phương pháp vô cảm dự kiến: (Mê nội khí quản, mê mask thanh quản, tiền mê, tê ngoài màng cứng, mê tĩnh mạch, tê tủy sống, tê khoang cùng, tê tại chỗ): ', item['vo_cam'], bold_val=True, sz='24')
        
        # Table 1: Đánh giá điều kiện GMHS (body[21])
        tbl1 = body[21]
        set_table1_checkbox(tbl1, 0, item['mallampati_col']) # Mallampati
        set_table1_checkbox(tbl1, 1, item['loai_pt_col'])    # Loại phẫu thuật
        set_table1_checkbox(tbl1, 2, item['asa_col'])        # ASA
        set_table1_checkbox(tbl1, 3, item['nguy_co_col'])    # Phân loại nguy cơ
        
        # P22: Nhiễm khuẩn vết mổ
        set_p_text(body[22], 'Nhiễm khuẩn vết mổ: ', 'Không', sz='24')
        
        # P23: Kháng sinh dự phòng
        set_p_text(body[23], 'Kháng sinh dự phòng: ', 'CÓ (Cefazolin 2g tiêm TM trước rạch da 30 phút)', bold_val=True, sz='24')
        
        # P24: Phẫu thuật viên chính
        set_p_text(body[24], 'Phẫu thuật viên chính: ', item['ptv'], bold_val=True, sz='24')
        
        # P25: Ngày, giờ phẫu thuật dự kiến
        set_p_text(body[25], 'Ngày, giờ phẫu thuật dự kiến: ', '08 giờ 00 phút, ngày 16/09/2026', bold_val=True, sz='24')
        
        # P26: Các biến chứng, nguy cơ, khó khăn đặc biệt cần lưu ý
        set_p_text(body[26], 'Các biến chứng, nguy cơ, khó khăn đặc biệt cần lưu ý:', sz='24')
        
        # P27: Chi tiết biến chứng
        set_p_text(body[27], 'Chảy máu, nhiễm trùng vết mổ, tổn thương màng cứng - rò dịch não tủy, tổn thương rễ thần kinh/tủy sống, tràn xi măng vào ống sống/tĩnh mạch, đau tồn dư sau mổ, thuyên tắc huyết khối tĩnh mạch sâu (DVT), dị ứng, phản vệ.', sz='22')
        
        # P29: Các biện pháp thay thế hoặc chuẩn bị đặc biệt
        set_p_text(body[29], 'Các biện pháp thay thế hoặc các yêu cầu chuẩn bị đặc biệt: ', 'Theo dõi sát dấu hiệu sinh tồn và tri giác, giải thích kỹ thân nhân và người bệnh trước can thiệp, chuẩn bị đầy đủ dụng cụ phẫu thuật, C-arm và thuốc cấp cứu.', sz='24')
        
        # Table 2: Signatures (body[30])
        tbl2 = body[30]
        sig_cells = tbl2.findall('.//w:tc', ns)
        rebuild_cell_paragraphs(sig_cells[0], [
            {'text': 'Bác sỹ phẫu thuật', 'sz': '24', 'bold': True, 'align': 'center'},
            {'text': '(Ký, ghi rõ họ tên)', 'sz': '24', 'italic': True, 'align': 'center'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': item['ptv'], 'sz': '24', 'bold': True, 'align': 'center'}
        ])
        rebuild_cell_paragraphs(sig_cells[1], [
            {'text': 'Bác sỹ gây mê', 'sz': '24', 'bold': True, 'align': 'center'},
            {'text': '(Ký, ghi rõ họ tên)', 'sz': '24', 'italic': True, 'align': 'center'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': 'BS Khoa PT - GMHS', 'sz': '24', 'bold': True, 'align': 'center'}
        ])
        rebuild_cell_paragraphs(sig_cells[2], [
            {'text': 'Lãnh đạo khoa lâm sàng', 'sz': '24', 'bold': True, 'align': 'center'},
            {'text': '(Ký, ghi rõ họ tên)', 'sz': '24', 'italic': True, 'align': 'center'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': 'TS.BS. Nguyễn Văn Trung', 'sz': '24', 'bold': True, 'align': 'center'}
        ])
        rebuild_cell_paragraphs(sig_cells[3], [
            {'text': 'Lãnh đạo duyệt mổ/ KHTH', 'sz': '24', 'bold': True, 'align': 'center'},
            {'text': '(Ký, ghi rõ họ tên)', 'sz': '24', 'italic': True, 'align': 'center'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': '', 'sz': '24'},
            {'text': 'TS.BS. Nguyễn Văn Trung', 'sz': '24', 'bold': True, 'align': 'center'}
        ])
        
        # Serialize and verify XML
        xml_out = ET.tostring(root, encoding='utf-8')
        try:
            ET.fromstring(xml_out)
        except Exception as ex:
            print(f'❌ ERROR: XML validation failed for {safe_name}: {ex}')
            sys.exit(1)
            
        with zipfile.ZipFile(out_filepath, 'w', zipfile.ZIP_DEFLATED) as zout:
            for item_zip in zin.infolist():
                if item_zip.filename == 'word/document.xml':
                    zout.writestr(item_zip, xml_out)
                else:
                    zout.writestr(item_zip, zin.read(item_zip.filename))
                    
        generated_files.append(out_filepath)
        print(f'[{idx_str}/06] Created & Verified OK: {out_filename}')

print(f'\n🎉 100% HOÀN TẤT TẠO {len(generated_files)} BIÊN BẢN HỘI CHẨN PT-01 CHUẨN ĐẸP!')
