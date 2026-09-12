---
name: his-treatment-tracking
description: >-
  Tự động tạo Tờ điều trị (Treatment Tracking), ghi nhận diễn biến bệnh, y lệnh,
  sinh hiệu (DHST), và quy trình ký số/ký điện tử EMR (kèm mời bác sĩ cùng ký)
  trên hệ thống HIS/MOS/EMR Bệnh viện Bạch Mai qua API không cần thao tác UI.
---

# HIS Treatment Tracking & EMR Digital Signature

Skill này cung cấp quy trình và kịch bản thực thi tự động tạo **Tờ điều trị**, **Diễn biến bệnh**, **Y lệnh**, và **Ký điện tử / Mời ký EMR** trực tiếp qua API hệ thống HIS bệnh viện (Backend MOS `:1608`, EMR `:1415`, ACS `:1401`).

## 1. Thông số Hệ thống & Đăng nhập
- **ACS Auth URL**: `http://192.168.7.200:1401/`
- **MOS Backend URL**: `http://192.168.7.236:1608/`
- **EMR Document URL**: `http://192.168.7.239:1415/`
- **Tài khoản bác sĩ**: `034727` / `998199` (Ths.BS NGUYỄN HỮU SÂM - Khoa CTCH & Cột sống, Mã khoa `9`, Phòng 714 / 716).

## 2. Công cụ thực thi siêu tốc (`HisTrackingCreator.exe` / `QuickTracking.exe`)

Công cụ `HisTrackingCreator.exe` và `QuickTracking.exe` đặt trực tiếp tại thư mục gốc HIS (`e:\his-x64-28-11fix GDYK\his-x64\`). Thực thi trong **< 1 giây**.

### Cách sử dụng:
```powershell
# Tạo tờ điều trị thời điểm hiện tại
.\QuickTracking.exe -p 0003969449 -content "bn tỉnh không sốt huyết động ổn"

# Tạo tờ điều trị với mốc giờ cụ thể trong ngày
.\QuickTracking.exe -p 0003969449 -time 08:00 -content "bn tỉnh không sốt huyết động ổn"

# Tạo bằng HisTrackingCreator kèm chế độ chăm sóc và thuốc
.\HisTrackingCreator.exe -p 0003969449 -time 08:00 -content "Bệnh nhân tỉnh, vết mổ khô" -care "Chăm sóc cấp II. Ăn BT01" -med "Thuốc theo đơn"
```

---

## 3. Cấu trúc API Backend MOS (`POST api/HisTracking/Create`)
- **Endpoint**: `http://192.168.7.236:1608/api/HisTracking/Create`
- **DTO**: `MOS.SDO.HisTrackingSDO` (lớp vỏ chứa `Tracking: HIS_TRACKING` và `WorkingRoomId: long`).
- **Lưu ý thuộc tính**: Trường y lệnh là `MEDICAL_INSTRUCTION` (không phải `TREATMENT_INSTRUCTION`).

---

## 4. Quy trình Ký Điện Tử & Mời Bác Sĩ Cùng Ký (EMR Sign Flow)
- **Tạo EMR Document**: `DOCUMENT_TYPE_ID = 7` (Tờ điều trị).
- **Ký chính (`NumOrder = 1`)**: Bác sĩ điều trị `034727` (Ths.BS Nguyễn Hữu Sâm).
- **Mời ký phối hợp (`NumOrder = 2`)**: Bác sĩ `ndh2` (BS Nguyễn Đức Hoàng).

---

## 5. Tự Động Hóa Soạn Thảo Diễn Biến Điều Trị bằng AI OpenRouter Multi-Tier

Khi nhận bàn giao ca trực hoặc kết quả thăm khám thô của điều dưỡng/bác sĩ phụ:
- Sử dụng CLI: `.\HisAiCli.bat ask "Chuẩn hóa diễn biến bệnh cho BN hậu phẫu nẹp vít cột sống ngày 2..."`
- Hệ thống tự động dùng **`stealth/ox-alpha`** (Ưu tiên số 1) hoặc **`minimax/minimax-m3:free`** để chuẩn hóa diễn biến bệnh và y lệnh lâm sàng trước khi chuyển tiếp vào `HisTrackingCreator.exe`.

