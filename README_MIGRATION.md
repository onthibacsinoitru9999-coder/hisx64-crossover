# HƯỚNG DẪN DI DỜI & CÀI ĐẶT HỆ THỐNG SUPPORT AUTOMATION SANG HIS MỚI

## 1. Bản chất của gói di dời (Migration Package)
Gói này chứa toàn bộ **Bộ Core Hỗ Trợ Lâm Sàng & AI Automation** đã được chắt lọc tinh gọn 100%:
- Đã loại bỏ hoàn toàn gần 500MB log cũ (Logs/).
- Đã loại bỏ các file nháp/debug tạm thời.
- Giữ lại toàn bộ 10 công cụ thực thi x64 đã được tối ưu tốc độ, bền bỉ và chuẩn xác.
- Toàn bộ tri thức .agents/ (5 skills lâm sàng, danh mục mã kho, bẫy lỗi Gotchas 1-42).

---

## 2. Các bước triển khai trên thư mục HIS mới nguyên bản:
1. **Sao chép file nén**: Copy file HIS_Support_Core_Migration.zip vào thư mục gốc của phiên bản HIS mới (nơi chứa file HIS.Desktop.exe hoặc thư mục ReferencedAssemblies).
2. **Giải nén trực tiếp**: Nhấp chuột phải vào file zip -> Chọn **Extract Here** (Giải nén ngay tại thư mục gốc này).
3. **Kích hoạt & Kiểm tra 1-Click**:
   - Chạy file: **Cai_Dat_He_Thong_Support.bat**
   - Hệ thống sẽ tự động:
     * Kiểm tra các DLL liên kết.
     * Chạy chẩn đoán sức khỏe 4 tầng (MOS, ACS, SDA, EMR, Live Token).
     * Chạy thử nghiệm tra cứu hồ sơ để xác nhận sẵn sàng 100%.

---

## 3. Danh mục các công cụ 1-Click hàng ngày:
| Nghiệp vụ | Lệnh / File Batch | Chức năng |
| :--- | :--- | :--- |
| 🩺 **Kiểm tra kết nối & Token** | HisDiagnosticDoctor.bat health | Kiểm tra 4 server lõi & Live Token |
| 🔍 **Tra cứu bệnh nhân & buồng** | HisClinicalCli.exe lookup <MãBN> | Tra cứu siêu tốc, tính tuổi chuẩn, bilan CLS |
| 👥 **Đọc biên bản hội chẩn** | HisClinicalCli.exe debate <MãBN> | Đọc ý kiến các chuyên khoa khách |
| 💊 **Kê đơn thuốc & Insulin** | HisAutoPrescribe.exe hoặc --batch | Tự động đổi liều UI/1000, tủ trực 810 |
| 🩸 **Chỉ định ĐMMM BM02426** | HisGlucoseBedsideAssigner.exe | Chỉ định ĐMMM (mốc 06:00 tự sang ngày mai) |
| 📝 **Tạo tờ điều trị hàng ngày** | HisTrackingCreator.exe | Tạo diễn biến lâm sàng nhúng OpenRouter AI |
| 📋 **Đối soát sơ kết 3/7 ngày** | HisSummaryTrackingDoctor.bat 712 | Rà soát các ca đến hạn sơ kết |
| 📄 **Tạo tờ sơ kết 3/7 ngày** | HisSummaryTrackingCreator.bat | Tự động tạo tờ sơ kết đợt điều trị |
| 🍲 **Bổ sung suất ăn dinh dưỡng** | HisRationAssigner.bat | Chỉ định suất ăn BT01, DD01 tự động |
| 📊 **Báo cáo buồng & Drive** | HisWardReport.bat | Xuất báo cáo đi buồng & đồng bộ Google Drive |
