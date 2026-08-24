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
