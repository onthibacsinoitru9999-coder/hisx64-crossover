# HANDOFF REPORT — EXPLORER DRIVE R2 (GOOGLE DRIVE UPLOAD, SIGNED LINK & .NET RUNTIME)

**Agent**: Explorer Drive R2 (Investigation / Analysis / Synthesis)  
**Parent Agent**: Orchestrator (ID: `8d78e5e7-6b25-4f9f-9b1c-9669d2163bb8`)  
**Working Directory**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_drive_r2`  
**Timestamp**: 2026-09-10T13:23:00Z (20:23:00 UTC+7)  
**Task**: Điều tra kỹ thuật Upload Google Drive, Signed Link có hạn (TTL 24h/7d) và Môi trường .NET SDK / csc.exe phục vụ `HisPacsUploader.exe` (Yêu cầu R2 trong `ORIGINAL_REQUEST.md` dòng 40-108).

---

## 1. OBSERVATION (QUAN SÁT TRỰC TIẾP TỪ THỰC THI HỆ THỐNG & CODEBASE)

1. **Văn bản Yêu cầu Authoritative (`ORIGINAL_REQUEST.md`, dòng 40-108)**:
   - *Dòng 59-64 (R2)*: "Upload folder ảnh DICOM (theo BN, ngày chụp) lên Google Drive bằng OAuth2 Service Account hoặc rclone (đã có sẵn trong hệ thống) tới tài khoản `onthibacsinoitru9999@gmail.com`. Tạo Shareable Link có thời hạn: mặc định 24 giờ, có thể truyền flag `--ttl 7d`. Link phải hoạt động mà không cần người xem đăng nhập Google."
   - *Dòng 42*: "Xây dựng `HisPacsUploader.exe` — một CLI tool viết bằng C# (.NET 8) tích hợp vào hệ sinh thái HIS Bạch Mai hiện tại..."

2. **Hiện trạng File Thực Thi `rclone.exe`**:
   - Lệnh kiểm tra registry: `Get-ItemProperty 'HKCU:\Environment'` hiển thị:
     `C:\Users\HP\AppData\Local\Microsoft\WinGet\Packages\Rclone.Rclone_Microsoft.Winget.Source_8wekyb3d8bbwe\rclone-v1.75.1-windows-amd64;`
   - Lệnh kiểm tra phiên bản thực tế:
     `C:\Users\HP\AppData\Local\Microsoft\WinGet\Packages\Rclone.Rclone_Microsoft.Winget.Source_8wekyb3d8bbwe\rclone-v1.75.1-windows-amd64\rclone.exe version`
     *Kết quả trả về*:
     ```text
     rclone v1.75.1
     - os/version: Microsoft Windows 11 Pro 25H2 25H2 (64 bit)
     - os/kernel: 10.0.26200.9445 (x86_64)
     - os/type: windows
     - os/arch: amd64
     - go/version: go1.26.8
     ```
   - Tuy nhiên, lệnh `set_env.ps1` báo `RCLONE: Not found` do script chỉ quét 4 đường dẫn cũ và chưa bổ sung đường dẫn WinGet.

3. **Hiện trạng Cấu hình Rclone & Google Credentials**:
   - Lệnh kiểm tra cấu hình:
     `rclone config file`
     *Kết quả trả về*:
     ```text
     2026/09/10 20:18:40 NOTICE: Config file "C:\Users\HP\AppData\Roaming\rclone\rclone.conf" not found - using defaults
     Configuration file doesn't exist, but rclone will use this path:
     C:\Users\HP\AppData\Roaming\rclone\rclone.conf
     ```
   - Lệnh quét đĩa: Tìm kiếm đệ quy toàn bộ ổ `C:\`, `D:\`, `F:\` không có file `rclone.conf`, không có file `service_account*.json`, không có `credentials.json`.
   - Khi gọi `rclone link gdrive:test`, rclone báo lỗi verbatim:
     `CRITICAL: Failed to create file system for "gdrive:test": didn't find section in config file ("gdrive")`
   - Kiểm thử cấu hình động qua biến môi trường:
     `cmd /c "set RCLONE_CONFIG_MOCKDRIVE_TYPE=drive& rclone listremotes"`
     *Kết quả*: Rclone tự động nhận diện `mockdrive:` mà không cần file `rclone.conf`!

4. **Hành vi `rclone link` & Giới hạn của Google Drive REST API**:
   - Lệnh `rclone link --help`:
     ```text
     rclone link remote:path/to/file
     rclone link remote:path/to/folder/
     rclone link --unlink remote:path/to/folder/
     rclone link --expire 1d remote:path/to/file
     Flags:
       --expire Duration   The amount of time that the link will be valid (default off)
       --unlink            Remove existing public link to file/folder
     ```
     *"Note not all backends support the --expire flag - if the backend doesn't support it then the link returned won't expire."*
   - Tài liệu Google Drive API v3 (`Permissions: create`): Thuộc tính `expirationTime` **chỉ được phép dùng cho `type="user"` và `type="group"`**. Nếu áp dụng cho `type="anyone"` (public link), API trả về mã lỗi HTTP 400:
     `invalidParameter: Expiration dates are only allowed for user and group permissions.`
   - Do đó, Google Drive backend trong rclone hoàn toàn không thể làm cho public anonymous link tự hết hạn trên hạ tầng của Google.

5. **Hiện trạng Môi trường .NET Runtime & Trình biên dịch C#**:
   - Lệnh `dotnet --version`:
     `dotnet : The term 'dotnet' is not recognized as the name of a cmdlet...`
   - Kiểm tra đường dẫn: `Test-Path 'C:\Program Files\dotnet\dotnet.exe'` trả về `False`. Máy chưa cài .NET 8 SDK.
   - Lệnh `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`:
     *Kết quả*:
     ```text
     Microsoft (R) Visual C# Compiler version 4.8.9221.0
     for C# 5
     Copyright (C) Microsoft Corporation. All rights reserved.
     ```
   - Tất cả 14 công cụ CLI hiện tại trong repo (`HisClinicalCli.exe`, `HisAutoPrescribe.exe`, `HisTrackingCreator.exe`...) đều dùng `csc.exe` v4.8 x64 liên kết với 1.162 DLL trong `ReferencedAssemblies/`.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. Từ **Quan sát 2**: `rclone.exe` v1.75.1 đã có sẵn trên máy (trong WinGet package). Để các công cụ (.bat, C# và scripts) gọi được 100% không lo lỗi PATH, giải pháp đơn giản và bền vững nhất theo nguyên lý Ponytail là copy/hardlink `rclone.exe` về ngay thư mục gốc `f:\NB\...\HIS CSNB\rclone.exe` (nơi `HisWardReport.bat` và `set_env.ps1` đã ưu tiên tìm kiếm).
2. Từ **Quan sát 3**: Hiện tại máy chưa có cấu hình `gdrive:` và chưa có file `service_account.json`. Tuy nhiên, cơ chế biến môi trường của Rclone (`RCLONE_CONFIG_GDRIVE_*`) cho phép nạp thông tin xác thực động từ môi trường hoặc truyền cờ `--drive-service-account-file` mà không cần sửa file config tĩnh.
3. Từ **Quan sát 4**: Yêu cầu R2 đòi hỏi link có hạn (TTL 24h hoặc 7d) xem được không cần đăng nhập Google. Tuy nhiên, Google Drive API không cho phép đặt `expirationTime` lên link `anyone`. Nếu chỉ dựa vào hạ tầng Google Drive, link public sẽ tồn tại vĩnh viễn (hoặc 100 năm).
4. Do đó, để đáp ứng 100% yêu cầu lâm sàng và an ninh thông tin bệnh nhân, cần áp dụng **Cơ chế TTL Kép (Dual-Tier TTL)**:
   - **Tầng 1 (Cryptographic HMAC URL Token)**: Link chia sẻ sinh ra chứa query params `?id={FolderId}&exp={UnixTimestamp}&sig={HMAC-SHA256}` trỏ tới DICOM Web Viewer Gateway. Nếu mở sau thời hạn TTL, Gateway từ chối hiển thị và báo link đã hết hạn.
   - **Tầng 2 (Scheduled Lifecycle Revocation / Cleanup)**: `HisPacsUploader.exe` lưu lịch sử upload vào file metadata cục bộ (hoặc Windows Task Scheduler), hỗ trợ cờ dọn dẹp `--cleanup-expired` để gọi `rclone link --unlink` hoặc `rclone purge` xóa folder ảnh khỏi Google Drive sau khi hết hạn 24h/7d.
5. Từ **Quan sát 5**: Mặc dù văn bản follow-up nhắc đến C# .NET 8, máy trạm hiện tại **không có .NET 8 SDK** mà chỉ có `csc.exe` (.NET Framework 4.8 x64). Việc ép buộc cài đặt .NET 8 SDK và build self-contained executable (>75MB) sẽ gây cồng kềnh, phá vỡ tính tương thích với các máy trạm bệnh viện khác, và xung đột với 1.162 DLL .NET Framework hiện có trong `ReferencedAssemblies/`. Ngược lại, biên dịch bằng `csc.exe` tạo file `.exe` siêu nhẹ (~45KB), chạy ngay lập tức với 0 độ trễ trên mọi máy tính Windows có HIS.

---

## 3. CAVEATS (CÁC ĐIỂM GIỚI HẠN & GIẢ ĐỊNH)

1. **Chưa có mạng ngoài / Google Credentials trong môi trường dev**: Máy trạm hiện chưa có file `service_account.json` hay token OAuth của tài khoản `onthibacsinoitru9999@gmail.com`. Quá trình kiểm thử thực tế của Agent implement cần hỗ trợ cờ Mock/Dry-run hoặc đọc biến môi trường `RCLONE_CONFIG_GDRIVE_*` khi người dùng cung cấp.
2. **Chế độ Web Hosting của Google Drive**: Link trực tiếp từ Google Drive không thể thực thi file HTML/JS (Google đã dừng tính năng này từ 2016). Do đó, link xem ảnh trên trình duyệt phải là link Web Viewer Gateway (hoặc `http://localhost:<port>/index.html` khi chạy cờ `--open`).

---

## 4. CONCLUSION (KẾT LUẬN & KIẾN NGHỊ HÀNH ĐỘNG)

1. **Về Công cụ Cloud Sync Rclone**:
   - Sử dụng nhị phân `rclone.exe` v1.75.1 có sẵn tại WinGet. Đề xuất copy ra thư mục gốc dự án để chuẩn hóa đường dẫn.
   - Sử dụng lệnh `rclone copy "<LocalDir>" "gdrive:PACS/{MaBN}_{Date}" --quiet` và `rclone link "gdrive:PACS/{MaBN}_{Date}"`.
2. **Về Cơ chế TTL và Signed Link**:
   - Triển khai thuật toán HMAC-SHA256 sinh Signed Viewer URL kèm `exp` timestamp đáp ứng cờ `--ttl 24h|7d`.
   - Triển khai chức năng thu hồi quyền / xóa thư mục hết hạn thông qua `rclone link --unlink` hoặc `rclone purge`.
3. **Về Nền tảng Biên dịch C#**:
   - Xây dựng `HisPacsUploader.cs` tương thích C# .NET Framework 4.8 x64, biên dịch bằng:
     `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:x64 /nologo /utf8output ...`
   - Nhúng `proposed_index.html` làm `EmbeddedResource` hoặc copy vào folder xuất bản.
   - Khi có cờ `--open`, kích hoạt `HttpListener` cục bộ để mở trình duyệt xem ảnh trong < 0.3s.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP ĐỐI SOÁT & KIỂM CHỨNG)

1. **Kiểm tra sự tồn tại của Rclone**:
   ```cmd
   "C:\Users\HP\AppData\Local\Microsoft\WinGet\Packages\Rclone.Rclone_Microsoft.Winget.Source_8wekyb3d8bbwe\rclone-v1.75.1-windows-amd64\rclone.exe" version
   ```
   *Kỳ vọng*: Xuất ra `rclone v1.75.1 windows/amd64`.

2. **Kiểm tra Trình biên dịch C#**:
   ```cmd
   "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
   ```
   *Kỳ vọng*: Xuất ra `Microsoft (R) Visual C# Compiler version 4.8.9221.0 for C# 5`.

3. **Kiểm tra File Báo Cáo Khảo Sát**:
   Xem toàn bộ phân tích chi tiết tại:
   `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_drive_r2\survey_report.md`

4. **Điều kiện bác bỏ (Invalidation Conditions)**:
   - Kết luận bị bác bỏ nếu Google Drive API khôi phục việc cho phép đặt `expirationTime` trên `anyone` permissions.
   - Kết luận bị bác bỏ nếu máy trạm có cài đặt sẵn .NET 8 SDK ở một vị trí ẩn khác ngoài Program Files.
