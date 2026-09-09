# Original User Request

## Initial Request — 2026-09-09T16:52:58Z

Quét toàn diện toàn bộ codebase hệ thống HIS Automation, phát hiện và sửa chữa triệt để mọi điểm nghẽn, liên kết môi trường, cấu hình compiler, mã hóa ký tự, và tối ưu hóa các kịch bản thực thi để toàn bộ hệ thống hoạt động mượt mà, trơn tru 100%. Triển khai đầy đủ đội ngũ tác nhân đa chuyên biệt (C#, Python/AI, PowerShell/Batch, Kiểm định y lệnh lâm sàng). Ưu tiên tối thượng: chính xác, bền bỉ và tốc độ cao.

Working directory: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB
Integrity mode: development

## Requirements

### R1. Rà soát & Tối ưu hóa 100% Batch Files & Toolchain Links
Kiểm tra tất cả file `.bat` trong thư mục gốc và thư mục con để đảm bảo 100% không còn file nào hardcode đường dẫn cũ (`C:\Program Files\Git\cmd`, đường dẫn Python cố định, v.v.). Đảm bảo mọi script đều liên kết qua bộ nạp môi trường tự động `set_env.bat`, vận hành trơn tru trên mọi terminal (CMD, PowerShell, Git Bash) và không phụ thuộc vào vị trí thư mục sâu.

### R2. Tự Động Quét, Đối Soát & Biên Dịch Đồng Bộ Các Công Cụ C# (.cs -> .exe)
Kiểm tra tính nhất quán giữa file mã nguồn `.cs` và file nhị phân thực thi `.exe` (`HisClinicalCli.exe`, `HisTrackingCreator.exe`, `HisAutoPrescribe.exe`, `HisGlucoseBedsideAssigner.exe`, `HisRationAssigner.exe`, `HisDebateCreator.exe`, `HisDiagnosticDoctor.exe`, `HisWardReportCreator.exe`). Nếu phát hiện file `.cs` mới hơn file `.exe` hoặc thiếu file `.exe`, tự động nạp danh mục DLL trong `ReferencedAssemblies/` và biên dịch bằng `csc.exe` 64-bit mà không gây lỗi tham chiếu.

### R3. Tối Ưu Hóa Tốc Độ & Độ Trễ Truy Vấn Lâm Sàng (Zero Friction)
Rà soát thuật toán truy vấn dữ liệu bệnh nhân và đi buồng (`HisClinicalCli lookup`, `HisClinicalCli wardround`, `HisWardReport.bat`). Đảm bảo áp dụng cơ chế gom mẻ (batch query) theo mảng ID, kỹ thuật đọc token tail-seek 128KB với `FileShare.ReadWrite`, loại bỏ hoàn toàn các điểm nghẽn vòng lặp tuần tự để thời gian tra cứu đạt dưới 1.5 giây.

### R4. Dọn Dẹp File Rác, File Tạm & Chuẩn Hóa Cấu Trúc
Rà soát cây thư mục dự án, dọn dẹp các file rác, file log tạm thời (`*.tmp`, stdout thừa, output kiểm thử không cần thiết) mà không ảnh hưởng đến các file cấu hình quan trọng (`ConfigSystem.xml`, `*.exe.config`, `ReferencedAssemblies/`). Đảm bảo toàn bộ file script lưu chuẩn UTF-8 (hoặc UTF-8 BOM cho `.ps1`) để Windows PowerShell 5.1 không bị lỗi font hoặc lỗi cú pháp.

### R5. Kiểm Thử Khép Kín & Kiểm Định Tự Động Toàn Hệ Thống
Chạy bộ kiểm tra sức khỏe toàn diện (`HisDiagnosticDoctor.bat health`), kiểm thử cú pháp toàn bộ `.ps1`, kiểm tra CLI models AI (`HisAiCli.bat models`), và chạy thử nghiệm luồng điều phối ĐTĐ (`HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm`) để xác nhận 100% hệ thống hoạt động không có lỗi.

## Acceptance Criteria

### Tính Nhất Quán & Sẵn Sàng Công Cụ
- [ ] 100% các file `.bat` đều được cấu hình nạp môi trường tự động, không còn đường dẫn tuyệt đối gán cứng.
- [ ] Mọi công cụ C# cốt lõi đều có file `.exe` đồng bộ mới nhất với mã nguồn `.cs`, dung lượng và timestamp hợp lệ.
- [ ] Toàn bộ file `.ps1` vượt qua bộ phân tích cú pháp `[System.Management.Automation.Language.Parser]::ParseFile` với 0 lỗi.

### Hiệu Năng & Độ Ổn Định
- [ ] `HisDiagnosticDoctor.bat health` chạy thành công và báo kết luận "Hệ thống sẵn sàng 100%".
- [ ] `HisAiCli.bat models` in danh mục mô hình OpenRouter thành công trong dưới 2 giây.
- [ ] Thư mục làm việc sạch sẽ, không còn file rác kiểm thử tồn đọng.
- [ ] Phiên bản mã nguồn và tri thức được đóng gói và đồng bộ lên Git `origin main`.