# ==============================================================================
# QUY TẮC BẮT BUỘC DÀNH CHO AI AGENT (HIS AUTOMATION PROJECT RULES)
# ==============================================================================

Mọi Agent khi khởi động trong BẤT KỲ khung chat nào (khung chat mới tạo, khung chat cũ tiếp tục, trên máy ổ E:\ hay ổ D:\) BẮT BUỘC phải tuân thủ nghiêm ngặt các quy tắc sau mà KHÔNG CẦN người dùng nhắc nhở:

## 1. QUY TẮC ĐẦU PHIÊN CHO MỌI KHUNG CHAT MỚI (PRE-FLIGHT SYNC - BẮT BUỘC 100%)
* **Bất kỳ khung chat nào khi bắt đầu phiên làm việc**, trước khi xử lý yêu cầu của bác sĩ (kê đơn, tờ điều trị, chỉ định CLS, tra cứu, sửa code...), Agent PHẢI **CHỦ ĐỘNG CHẠY LỆNH PULL** bằng terminal để đồng bộ tri thức và công cụ mới nhất từ Git:
  ```powershell
  git pull origin main
  ```
* **Nạp tri thức:** Sau khi pull, Agent tự động cập nhật ngữ cảnh từ [`HIS_AI_INTEGRATION_PLAYBOOK.md`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/HIS_AI_INTEGRATION_PLAYBOOK.md) và thư mục `.agents/skills/his-clinical-operations/` để đảm bảo nắm được toàn bộ danh mục thuốc, mã kho, bẫy lỗi và cấu trúc DTO mới nhất.

## 2. QUY TẮC THỰC THI (CLI-FIRST & KHÔNG VIẾT MÃ THÔ)
* **Tuyệt đối không tự viết mã cào log hay gửi HTTP thô** nếu tác vụ đó đã được đóng gói trong các công cụ có sẵn:
  - [`HisClinicalCli.exe`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-clinical-operations/scripts/HisClinicalCli.exe): Tra cứu bệnh nhân, tạo tờ điều trị, chỉ định CLS, ký số EMR.
  - [`HisAutoPrescribe.exe`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-clinical-operations/scripts/HisAutoPrescribe.exe): Kê đơn thuốc viên, ống, dinh dưỡng, tủ trực.
* **Tương thích đa máy:** Không hardcode cố định ổ đĩa `E:\` hay `D:\`. Khi cần đọc log `LogSystem.txt`, sử dụng đường dẫn tương đối từ thư mục gốc dự án hoặc tự động dò tìm vị trí thư mục đang chạy.

## 3. QUY TẮC ĐÓNG GÓI & ĐẨY TRI THỨC (CONTINUOUS LEARNING PUSH)
* Khi Agent giải quyết được một **bẫy lỗi mới (Gotcha)**, cập nhật danh mục, sửa code công cụ `.cs` hoặc bổ sung tính năng mới:
  1. Ghi lại nguyên nhân và giải pháp vào mục **Bảng Tổng Hợp Sai Lầm & Bài Học Xương Máu** trong `HIS_AI_INTEGRATION_PLAYBOOK.md`.
  2. Biên dịch lại file `.exe` (nếu có sửa file `.cs`).
  3. Tự động commit và đẩy lên Git để các khung chat khác và máy kia nhận được ngay lập tức:
     ```powershell
     git add HIS_AI_INTEGRATION_PLAYBOOK.md .agents/ *.cs *.bat *.ps1 AGENTS.md .gitignore
     git commit -m "fix/feat: <mô tả ngắn gọn nội dung sửa/cập nhật tri thức>"
     git push origin main
     ```

## 4. QUY TẮC BẮT BUỘC VỀ CHẨN ĐOÁN HÌNH ẢNH (ĐÍCH DANH VỊ TRÍ & TẦNG TỔN THƯƠNG)
* **Tuyệt đối không tóm tắt chung chung** các kết quả MRI, CT Scanner, X-quang trong tóm tắt bệnh án, biên bản hội chẩn hay tờ điều trị (như ghi "xẹp lún các đốt sống", "gãy xương", "thoái hóa đĩa đệm").
* **Bắt buộc trích xuất đích danh, chính xác từng tầng/vị trí tổn thương:**
  - *Ví dụ về cột sống:* Phải ghi rõ **`Xẹp cấp L2, L3, L5`** (để phục vụ chỉ định bơm xi măng chính xác), **`Xẹp cũ T12`**, **`Rách vòng xơ đĩa đệm L4/5`**, **`Trượt đốt sống L4 ra trước độ I`**, **`Hẹp ống sống tầng L3-L4-L5`**.
  - *Ví dụ về xương chi:* Phải ghi rõ **`Gãy xương tháp và xương thang cổ tay trái di lệch`**, **`Gãy 1/3 giữa xương đòn phải`**, **`Đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải`**.
* **Ý nghĩa an toàn:** Tránh nguy cơ phẫu thuật sai vị trí hoặc bỏ sót tổn thương cấp cần can thiệp.

## 5. QUY TẮC ĐẶC QUYỀN BÍ DANH: "THỢ CHO ĐƯỜNG HUYẾT" (DIABETES 1-CLICK PROTOCOL)
* **Bí danh kích hoạt:** Bất cứ khi nào bác sĩ nhắn tin hoặc gửi ảnh báo cáo đường huyết và gọi/nhắc đến **"thợ cho đường huyết"**, Agent PHẢI tự động nhận diện và kích hoạt ngay luồng xử lý toàn diện mà **KHÔNG CẦN HỎI LẠI HAY TINH CHỈNH GÌ THÊM**:
  1. **Tự đọc & trích xuất dữ liệu:** Phân tích trực tiếp ảnh/bảng dữ liệu gửi kèm (Mã BN, Họ tên, ĐH các mốc 17h, 21h, 6h sáng hôm sau, liều Insulin tương ứng).
     - 💡 **Quy chuẩn ký hiệu viết tắt của Điều dưỡng (Bắt buộc ghi nhớ):**
       * **`R`** (VD: **`6R`**, **`8R`**, **`4R`**): là **Actrapid** (Insulin Regular tác dụng nhanh). Ví dụ `6R` = `6 đơn vị Actrapid`.
       * **`L`** (VD: **`10L`**, **`12L`**, **`14L`**): là **Lantus** (Insulin Glargine nền kéo dài). Ví dụ `10L` = `10 đơn vị Lantus`.
       * **`M`** (VD: **`8M`**, **`10M`**, **`12M`**): là **Mixtard** (Insulin hỗn hợp / Mix). Ví dụ `8M` = `8 đơn vị Mixtard`.
     - 🔍 **Mặc định đối chiếu Bệnh nhân tại Khoa CTCH & Cột sống (Khoa 57):**
       * Trừ khi có chỉ định khác, luôn tìm kiếm và đối chiếu hồ sơ bệnh nhân đang điều trị nội trú tại **Khoa 57 (`DEPARTMENT_ID = 57`)**.
       * Trường hợp không tìm thấy bệnh nhân tại Khoa 57, Agent PHẢI báo lại ngay cho Bác sĩ.
  2. **Thực thi đồng thời 3 tác vụ y lệnh cho 100% bệnh nhân:**
     - **Tác vụ 1 - Tờ điều trị (`HisTrackingCreator.exe`):** Tạo tờ điều trị ghi nhận kết quả ĐMMM và y lệnh tiêm insulin theo từng mốc giờ (17h, 21h, 6h).
     - **Tác vụ 2 - Chỉ định CLS (`HisGlucoseBedsideAssigner.exe`):** Chỉ định xét nghiệm đường máu mao mạch tại giường **`BM02426`** cho các mốc giờ (mốc 06:00 tự động tính ngày hôm sau).
     - **Tác vụ 3 - Kê đơn Insulin (`HisAutoPrescribe.exe --batch` hoặc CLI):**
       * ⚠️ **BẮT BUỘC chỉ định từ Kho Tủ Trực Khoa 57 (`MediStockId = 810` - `TT_KCTCHCS`)**, **TUYỆT ĐỐI KHÔNG kê từ Kho Dược (4209/4210)**.
       * Kê đơn tiêm Insulin (Actrapid / Lantus / Mixtard) đúng số đơn vị và hướng dẫn dùng chuẩn lâm sàng.
  3. **Báo cáo kết quả:** In bảng tổng hợp đối soát kết quả rõ ràng, minh bạch (Thành công / Lỗi từng BN).

