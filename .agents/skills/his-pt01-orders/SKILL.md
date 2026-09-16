---
name: his-pt01-orders
description: >-
  Tự động lập Biên bản Hội chẩn thông qua mổ (Biểu mẫu MS: PT-01) từ file Word mẫu mau pt01.docx,
  trích xuất dữ liệu lâm sàng, đối soát hồ sơ, khám lâm sàng logic và cận lâm sàng (CTM, Đông máu, Sinh hóa,
  CĐHA MRI/CT/X-quang chi tiết) xuất file docx chuẩn cho các ca phẫu thuật chương trình/phiên Khoa 57.
---

# HIS Surgical Consultation & Approval Protocol (Biên Bản Hội Chẩn Thông Qua Mổ MS: PT-01)

Skill này chuẩn hóa quy trình tạo **Biên bản Hội chẩn thông qua mổ (MS: PT-01)** từ file mẫu chuẩn `mau pt01.docx` (hoặc `Templates\mau pt01.docx`), tích hợp đầy đủ hồ sơ bệnh án nội trú Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57), trích xuất và điền chính xác vào từng trường dữ liệu mà không làm xô lệch định dạng Word.

---

## 1. NGUYÊN TẮC THAY THẾ KHÔNG PHÁ VỠ FORMAT MẪU WORD (`mau pt01.docx`)

1. **Thông tin hành chính (Direct Match)**:
   - Họ và tên người bệnh: In hoa toàn bộ (VD: `NGUYỄN THỊ HUYỀN`)
   - Ngày sinh: Năm sinh hoặc ngày/tháng/năm
   - Giới tính: `Nam` hoặc `Nữ`
   - Địa chỉ: Trích xuất từ hành chính thẻ BHYT / hồ sơ
   - Vào viện: Ngày giờ vào viện `dd/MM/yyyy HH:mm`
   - Chẩn đoán: Mã ICD và tên chẩn đoán chi tiết
   - Tiền sử: Bệnh nền mạn tính, thuốc đang dùng, phẫu thuật cũ

2. **9 Thẻ `<thay>` Thay Thế Theo Ngữ Cảnh Đoạn Văn (Contextual Paragraph Replacement)**:
   - `Bệnh sử: <thay>`: Diễn biến khởi phát, vị trí đau, tính chất đau (VAS), thời gian kéo dài, yếu tố tăng giảm, điều trị nội khoa trước đó và lý do vào mổ.
   - `Thời gian hội chẩn: ..<thay>`: Ngày giờ hội chẩn (VD: `14 giờ 00 phút, ngày 16 tháng 09 năm 2026`).
   - `Tóm tắt tình trạng bệnh: <thay>`: Dấu hiệu sinh tồn (DHST Mạch, HA, T°, SpO2) + Khám chuyên khoa cơ xương khớp/thần kinh logic với chẩn đoán (Hội chứng cột sống, rễ, nghiệm pháp Lasègue, Valleix, bấm chuông, cơ lực, gõ dồn, Patrick, Tinel...) + Khám toàn thân tim phổi bụng.
   - `Các xét nghiệm, chẩn đoán hình ảnh`:
     * Công thức máu (WBC, RBC, HGB, PLT)
     * Đông máu (PT-INR, APTT, Fibrinogen)
     * Sinh hóa máu (Glucose, Ure, Creatinin, AST, ALT, Điện giải Na/K/Cl)
     * Vi sinh & Miễn dịch (HBsAg, HCV, HIV)
     * Chẩn đoán hình ảnh: Trích xuất đích danh từng tầng tổn thương từ MRI/CT/X-quang (TUYỆT ĐỐI không ghi chung chung).
   - `Nhóm máu & Dự trù máu`: Điền nhóm máu (VD: `O Rh(+)`) và lượng máu dự trù (VD: `350 ml`).
   - `Phương pháp phẫu thuật`: Khớp chính xác với lịch mổ phiên đã xếp.
   - `Phương pháp vô cảm dự kiến`: Mê nội khí quản / Tiền mê + Tê tại chỗ / Tê tủy sống.
   - `Phẫu thuật viên chính`: Tên PTV từ lịch mổ phiên.
   - `Ngày, giờ phẫu thuật dự kiến`: Thời gian mổ dự kiến (VD: `08 giờ 00 phút, ngày 17 tháng 09 năm 2026`).
   - `Các biến chứng, nguy cơ, khó khăn đặc biệt cần lưu ý`: Chảy máu, rách màng tủy rò DNT, tổn thương thần kinh/mạch máu, tụ máu vết mổ, nhiễm trùng vết mổ sâu.

---

## 2. CÔNG CỤ THỰC THI CLI (`HisPt01Creator.exe`)

Công cụ được đóng gói sẵn tại thư mục gốc dự án:
`.\HisPt01Creator.exe`

### Cú pháp lệnh:
```powershell
# Tạo biên bản cho toàn bộ danh sách bệnh nhân phiên mổ
.\HisPt01Creator.exe

# Hoặc tạo riêng cho các mã bệnh nhân cụ thể:
.\HisPt01Creator.exe 0003406142,0001140537,0004020895
```

### Thư mục lưu kết quả:
Tất cả các file Word xuất ra được lưu tại:
`Reports\BienBanHoiChan_PT01\PT01_<STT>_<TênBN>_<MãBN>.docx`
