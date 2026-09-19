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
> **Chốt Ranh Giới Kỹ Thuật Ký Số**:
> - **Tờ điều trị (`DOCUMENT_TYPE_ID = 7` / `Mps000062` / `HIS_TRACKING`)**: **TỰ ĐỘNG HÓA KÝ SỐ 100%**. Đây là đối tượng duy nhất được tự động hóa hoàn toàn từ tạo tờ điều trị đến đóng dấu đỏ Cloud HSM qua API.
> - **Vỏ bệnh án ngoại khoa (`BENHANNGOAIKHOA`)**: Điền tự động dữ liệu vào Oracle EMR qua `HisEmrFiller.exe`. Bác sĩ ký 1-click trực tiếp trên giao diện EMR Desktop. Tuyệt đối không script ký API cho Vỏ bệnh án để tránh lệch engine báo cáo XtraReports của EMR Client.

---

## 1. Thông Số Hệ Thống & Đăng Nhập
- **ACS Auth URL**: `http://192.168.7.200:1401/`
- **MOS Backend URL**: `http://192.168.7.236:1608/`
- **EMR Document URL**: `http://192.168.7.239:1415/`
- **Tài khoản bác sĩ**: `034727` / `998199` (Ths.BS NGUYỄN HỮU SÂM - Khoa CTCH & Cột sống, Khoa 57 / Mã khoa `9`, Phòng 714 / 716 / P734 `5248`).

---

## 2. Công Cụ Thực Thi Siêu Tốc (`HisTrackingCreator.exe`)

Công cụ `HisTrackingCreator.exe` đặt trực tiếp tại thư mục gốc HIS (`e:\his-x64-28-11fix GDYK\his-x64\`). Thực thi tạo và tự động ký trong **< 1 giây**.

### Cú pháp lệnh chuẩn:
```powershell
# 1. Tạo tờ điều trị và TỰ ĐỘNG KÝ SỐ Cloud HSM luôn
.\HisTrackingCreator.exe -p 0004051068 -time 08:00 -content "Bệnh nhân tỉnh, đau lưng giảm, vết mổ khô" -care "Chăm sóc cấp II. Ăn BT01" -med "Thuốc theo đơn"

# 2. Kiểm tra trạng thái ký trên EMR
.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe emr 0004051068
```

---

## 3. Kiến Trúc 2 Bước Ký Số Tự Động (Cloud HSM Pipeline)

Khi `HisTrackingCreator.exe` tạo tờ điều trị:
1. **Bước 1: Tạo bản ghi MOS (`POST api/HisTracking/Create`)**:
   - DTO: `MOS.SDO.HisTrackingSDO` chứa `HIS_TRACKING` và `WorkingRoomId = 5248`.
   - Thuộc tính y lệnh bắt buộc là `MEDICAL_INSTRUCTION`.
   - Sinh ra `TRACKING_ID` và `SHEET_ORDER`.
2. **Bước 2: Tạo Lệnh In & Đóng Dấu Ký Số EMR (`AutoSignTrackingEmr`)**:
   - Gọi `POST api/EmrDocument/CreateByTdo`:
     * `DocumentTypeId = 7` (Phiếu yêu cầu in tờ điều trị).
     * `HisCode = "Mps000062 TREATMENT_CODE:{treatmentCode} HIS_TRACKING:{trackingId}"`.
     * `SignTDO`: Bác sĩ điều trị `034727` (ThS.BS NGUYỄN HỮU SÂM).
   - Gọi `POST api/EmrSign/SignPdfHsm`:
     * Tọa độ con dấu: `CoorXRectangle = 400.0f, CoorYRectangle = 100.0f, PageNumber = 1`.
     * Kết quả: Văn bản chuyển sang trạng thái `🟢 ĐÃ KÝ ĐẦY ĐỦ` ngay lập tức.

---

## 4. Kiểm Tra Đối Soát Sau Ký
Sử dụng lệnh CLI:
```powershell
.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe emr <MãBN>
```
Kết quả hiển thị:
```text
[1] DocID: 93372246 | Loại: 7   | 🟢 ĐÃ KÝ ĐẦY ĐỦ
    Tên VB : Phiếu yêu cầu in tờ điều trị (4)
    Mã VB  : 000093372352 | HIS_CODE: Mps000062 TREATMENT_CODE:... HIS_TRACKING:...
      - Vị trí 1 (034727 - NGUYỄN HỮU SÂM): ✅ Đã ký
```

---

## 5. Tự Động Hóa Soạn Thảo Diễn Biến Điều Trị Bằng AI
- Khi nhận thông tin thô từ buồng bệnh, điều dưỡng hoặc ca trực:
  ```powershell
  .\HisAiCli.bat ask "Chuẩn hóa diễn biến bệnh cho BN hậu phẫu nẹp vít cột sống ngày 2..."
  ```
- Mô hình AI đa tầng miễn phí (`minimax/minimax-m3:free` hoặc `openrouter/free`) sẽ chuẩn hóa format y khoa trước khi truyền vào `HisTrackingCreator.exe`.
