---
name: his-pacs-viewer
description: >-
  Tra cứu, truy xuất và tự động tạo link mở ảnh PACS/RIS (MRI, CT Scanner, X-quang, Siêu âm)
  trực tiếp trên hệ thống RIS Minerva và Web Viewer PACS Bệnh viện Bạch Mai. Hỗ trợ 1-click mở tab ảnh
  trên trình duyệt máy trạm không cần đăng nhập lại.
---

# HIS / RIS / PACS Imaging Viewer Integration Skill

Hệ thống tài liệu và công cụ tự động hóa tra cứu, trích xuất và mở ảnh chẩn đoán hình ảnh (MRI, CT, X-quang, Siêu âm) từ hệ thống RIS / PACS Bệnh viện Bạch Mai qua API và Web Viewer trực tiếp.

---

## 1. Kiến Trúc Mạng & Cấu Hình PACS / RIS

- **RIS Portal:** `http://192.168.200.110/ris` (Minerva RIS)
  - Đăng nhập: `http://192.168.200.110/ris/account/login`
  - Tài khoản mặc định: `ctch` / Mật khẩu: `ctchCS2026!`
  - API endpoint tìm ca chụp: `http://192.168.200.110/ris/rest/study`
- **Modern Web DICOM Viewer (OHIF):** `http://192.168.200.111:8081`
  - URL phiên xem ảnh: `http://192.168.200.111:8081/viewer?session=<session_id>&mobile_support=1`
  - Token phiên được RIS backend tạo tự động khi gọi `http://192.168.200.110/ris/viewer?study=<studyIUID>` (HTTP 302 redirect mang theo session).
- **Máy chủ Lưu trữ PACS (Storage & WADO):**
  - **CS2:** `192.168.200.107:8080` (WADO: `http://192.168.200.107:8080/pacs/CS2/wado`)
  - **VRPACS / IMPORT2:** `192.168.200.111:8080` (WADO: `http://192.168.200.111:8080/pacs/IMPORT2/wado`)
  - **MINERVACS2:** `10.0.0.32:8080`

---

## 2. Công Cụ CLI Tự Động Hóa Duy Nhất: `HisPacsCli.bat` / `HisPacsCli.ps1`

Công cụ CLI được đặt ngay tại thư mục gốc dự án:
- `.\HisPacsCli.bat <MãBN>`
- `.\HisPacsCli.bat <MãBN> -Open` (Tự động bật tab xem ảnh trên Chrome)
- `.\HisPacsCli.ps1 -AccessionNo <MãPhiếu> -Open`
- `.\HisPacsCli.ps1 -PatientName "<TênBN>"`

### 🎯 Các Tham Số Đầu Vào:
| Tham số | Ý nghĩa | Ví dụ |
| :--- | :--- | :--- |
| `PatientId` (hoặc vị trí 0) | Mã bệnh nhân (hệ thống tự chuẩn hóa tiền tố `VS.` và 10 chữ số) | `0004009330` -> `VS.0004009330` |
| `-AccessionNo` | Mã số phiếu chỉ định hoặc số accession | `000089828831` hoặc `261381993` |
| `-PatientName` | Họ tên bệnh nhân (không dấu hoặc có dấu) | `LE THI LO` |
| `-Open` | Tự động mở trực tiếp các đường link xem ảnh trên trình duyệt máy trạm | `-Open` |

---

## 3. Quy Trình Trích Xuất & Mở Ảnh (3 Bước Chuẩn)

### Bước 1: Xác thực phiên RIS
1. `GET http://192.168.200.110/ris/account/login` -> Trích xuất chuỗi `validKey` trong script HTML (`pageData.validKey`).
2. `POST http://192.168.200.110/ris/account/login` với dữ liệu form:
   - `account=ctch`
   - `password=ctchCS2026!`
   - `isLocal=true`
   - `validKey=<validKey>`
   - Lưu cookie phiên (`slim_session`).

### Bước 2: Truy vấn danh sách ca chụp
- Gọi `GET http://192.168.200.110/ris/rest/study?pid=VS.<MãBN>&status=all&dateFrom=<TừNgày>&dateTo=<ĐếnNgày>`
- Trích xuất:
  - `studyIUID`: Mã định danh ca chụp (Study Instance UID)
  - `date`: Ngày giờ chụp
  - `modalityName` & `pacsAE`: Máy chụp và trạm lưu PACS
  - `diagnosis`: Tên dịch vụ CĐHA, Bác sĩ đọc kết quả, trạng thái ký số

### Bước 3: Tạo Link Web Viewer & Mở Trình Duyệt
- Gửi yêu cầu `GET http://192.168.200.110/ris/viewer?study=<studyIUID>` kèm cookie phiên.
- Bắt header `Location` từ HTTP 302:
  `http://192.168.200.111:8081/viewer?session=<UUID>&mobile_support=1`
- Nếu có cờ `-Open`, thực thi `Start-Process <DirectViewerURL>` để bác sĩ tương tác trực tiếp với giao diện phóng to/thu nhỏ, đo đạc góc Cobb, điều chỉnh cửa sổ xương/phần mềm.

---

## 4. Các Bẫy Lỗi Đã Giải Quyết (Gotchas & Lessons Learned)

1. **Tiền tố Mã Bệnh nhân RIS (`VS.`):**
   - Trên HIS mã BN là `0004009330`.
   - Trên RIS Minerva, mã BN bắt buộc phải có tiền tố `VS.` (thành `VS.0004009330`). Nếu truyền `0004009330` sẽ trả về 0 kết quả.
2. **Biến môi trường `$PID` của PowerShell:**
   - Trong PowerShell, `$PID` là biến tự động chỉ Process ID (Read-only). Tuyệt đối không đặt tên biến `$pid = ...`, phải dùng `$patientCode` hoặc `$pIdStr`.
3. **PowerShell Array vs String trong `-match`:**
   - Khi chạy `curl.exe`, output trả về dạng mảng string `[Object[]]`.
   - Toán tử `-match` trên mảng chỉ lọc phần tử, KHÔNG thiết lập biến `$matches`. Bắt buộc phải join mảng thành chuỗi đơn `($res -join "`n") -match ...` trước khi truy cập `$matches[1]`.
4. **Không cần đăng nhập lại tại máy chủ Web Viewer:**
   - Đường dẫn `http://192.168.200.111:8081/viewer?session=...` đã chứa sẵn session token được sinh bởi RIS. Trình duyệt mở link này trực tiếp mà không bị chặn xác thực.
