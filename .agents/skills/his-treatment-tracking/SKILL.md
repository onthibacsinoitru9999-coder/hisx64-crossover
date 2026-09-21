---
name: his-treatment-tracking
description: >-
  Tự động tạo Tờ điều trị (Treatment Tracking), ghi nhận diễn biến bệnh, y lệnh,
  sinh hiệu (DHST), và quy trình ký số/ký điện tử Cloud HSM EMR tự động 100%
  trên hệ thống HIS/MOS/EMR Bệnh viện Bạch Mai qua API không cần thao tác UI.
---

# 📝 HIS Treatment Tracking & Cloud HSM EMR Digital Signature (Chuẩn Hóa 100%)

Skill này cung cấp quy trình và kịch bản thực thi tự động tạo **Tờ điều trị**, **Diễn biến bệnh**, **Y lệnh**, và **Ký số Cloud HSM EMR tự động 100%** trực tiếp qua API hệ thống HIS bệnh viện (Backend MOS `:1608`, EMR `:1415`, ACS `:1401`).

> [!IMPORTANT]
> **Quy Chuẩn Ký Số & In Ấn**:
> - ⚠️ **GỠ BỎ HOÀN TOÀN TỰ ĐỘNG KÝ SỐ QUA API**: Toàn bộ chức năng ký số ngầm qua API (`SignPdfHsm`, `AutoSignTrackingEmr`, sinh PDF upload) đã bị **GỠ BỎ 100%**.
> - Hệ thống tập trung tối đa vào việc tạo bản ghi tờ điều trị chuẩn xác, đầy đủ diễn biến lâm sàng trên HIS/MOS (`api/HisTracking/Create`).
> - **In và ký**: Bác sĩ in và ký trực tiếp Tờ điều trị trên giao diện **HIS / EMR Desktop Client** tại máy trạm khoa phòng. Không can thiệp API ký ngầm.

---

## 1. Thông Số Hệ Thống & Đăng Nhập
- **ACS Auth URL**: `http://192.168.7.200:1401/`
- **MOS Backend URL**: `http://192.168.7.236:1608/`
- **EMR Document URL**: `http://192.168.7.239:1415/`
- **Tài khoản bác sĩ**: `034727` (Ths.BS NGUYỄN HỮU SÂM - Mật khẩu đọc từ `$env:HIS_PASSWORD` hoặc fallback `981`).

---

## 2. Công Cụ Thực Thi Siêu Tốc (`HisTrackingCreator.exe`)

Công cụ `HisTrackingCreator.exe` đặt trực tiếp tại thư mục gốc HIS. Thực thi tạo tờ điều trị trong **< 1 giây**.

### Cú pháp lệnh chuẩn:
```powershell
# 1. Tạo tờ điều trị sạch sẽ trên MOS
.\HisTrackingCreator.exe -p 0004051068 -time 08:00 -content "Bệnh nhân tỉnh, đau lưng giảm, vết mổ khô" -care "Chăm sóc cấp II. Ăn BT01" -med "Thuốc theo đơn"

# 2. Kiểm tra văn bản EMR (Chỉ đọc)
.\HisClinicalCli.exe emr 0004051068
```

---

## 3. Kiến Trúc Tạo Tờ Điều Trị MOS Chuẩn

Khi `HisTrackingCreator.exe` tạo tờ điều trị:
1. **Tạo bản ghi MOS (`POST api/HisTracking/Create`)**:
   - DTO: `MOS.SDO.HisTrackingSDO` chứa `HIS_TRACKING` và `WorkingRoomId = 5248`.
   - Thuộc tính y lệnh bắt buộc là `MEDICAL_INSTRUCTION`.
   - Sinh ra `TRACKING_ID` và `SHEET_ORDER` chuẩn xác.
2. **Ký và In ấn**:
   - Bác sĩ mở bệnh án trên UI Desktop, bấm In và Ký trực tiếp. Template `062-Tờ điều trị chuẩn.xlsx` tự nạp 100% dữ liệu.

---

## 4. Kiểm Tra Danh Sách Văn Bản EMR (Chỉ Đọc)
Sử dụng lệnh CLI:
```powershell
.\HisClinicalCli.exe emr <MãBN>
```

---

## 5. Tự Động Hóa Soạn Thảo Diễn Biến Điều Trị Bằng AI
- Khi nhận thông tin thô từ buồng bệnh, điều dưỡng hoặc ca trực:
  ```powershell
  .\HisAiCli.bat ask "Chuẩn hóa diễn biến bệnh cho BN hậu phẫu nẹp vít cột sống ngày 2..."
  ```
- Mô hình AI đa tầng miễn phí (`minimax/minimax-m3:free` hoặc `openrouter/free`) sẽ chuẩn hóa format y khoa trước khi truyền vào `HisTrackingCreator.exe`.

---

## 6. Thực Thi Tạo Tờ Điều Trị Qua HIS MCP Server (Chống Rác File Trong /goal)
Trong các phiên chạy tự động `/goal`, Agent **BẮT BUỘC** gọi qua công cụ MCP:
- `his_create_tracking(patientCode, progressNote, pulse, bloodPressure, temperature, spO2, instructionTime)`
- Mọi diễn biến lâm sàng được truyền qua tham số trực tiếp, tuyệt đối KHÔNG tạo script tạm.

