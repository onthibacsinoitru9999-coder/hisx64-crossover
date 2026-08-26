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
       * **Quy chuẩn tỷ lệ quy đổi:** `Amount = UI / 1000.0m` (VD: `8 UI` -> `0.0080 lọ`), `MedicineUseFormId = 15` (*Tiêm*), cữ tiêm `MORNING`/`NOON`/`EVENING` = chuỗi 2 chữ số (VD: `"08"`), `IsExpend = false`.
       * Kê đơn tiêm Insulin (Actrapid / Lantus / Mixtard) đúng số đơn vị và hướng dẫn dùng chuẩn lâm sàng.
  3. **Quy tắc Kiểm soát thời gian xử lý (Max 2 Attempts - Tuyệt đối không loop lâu):**
     * Trong mọi tác vụ lâm sàng (đặc biệt khi bác sĩ đang trực tiếp xử lý bệnh nhân), nếu API backend từ chối hoặc trả `Success: false` quá 2 lần, Agent **PHẢI DỪNG VÒNG LẶP NGAY LẬP TỨC**.
     * Báo cáo ngay kết quả những phần việc ĐÃ TẠO THÀNH CÔNG (Tờ điều trị, Chỉ định CLS) và hướng dẫn Bác sĩ xử lý nhanh nhất trên giao diện HIS, tuyệt đối không được tự ý viết mã thử-sai kéo dài làm chậm trễ công việc của Bác sĩ.
  4. **Báo cáo kết quả:** In bảng tổng hợp đối soát kết quả rõ ràng, minh bạch (Thành công / Lỗi từng BN).

## 6. QUY TẮC MA TRẬN MÔ HÌNH OPENROUTER: ĐIỀU PHỐI ĐA TẦNG MIỄN PHÍ 100% (MULTI-TIER SMART FALLBACK)
* **Khóa xác thực**: Tự động nạp từ biến môi trường `OPENROUTER_API_KEY` (hoặc Windows Registry `HKCU\Environment`).
* **Bộ điều phối chuẩn hóa**: Sử dụng [`openrouter_client.py`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/openrouter_client.py) hoặc lệnh CLI [`HisAiCli.bat`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisAiCli.bat) để tự động chuyển tầng dự phòng khi gặp sự cố rate-limit/timeout mà không làm gián đoạn công việc của Bác sĩ.

### 🌟 Ma trận Phân công Mô hình theo Nghiệp vụ Lâm sàng:
| Phân nhóm Nghiệp vụ | Tầng 1 (Ưu tiên số 1) | Tầng 2 (Dự phòng 1) | Tầng 3 (Dự phòng 2) | Tầng 4 (Dự phòng 3) |
| :--- | :--- | :--- | :--- | :--- |
| **1. Đa phương thức & OCR (Ảnh ĐH, Phim X-quang/CT/MRI, Phiếu KQ)** | **`stealth/ox-alpha`** *(1M tokens, Reasoning, JSON)* | **`minimax/minimax-m3:free`** *(1M tokens, Multimodal)* | **`google/gemma-4-31b-it:free`** *(256K tokens, Multimodal)* | **`openrouter/free`** *(Auto Router)* |
| **2. Lập luận Bệnh án & Hội chẩn Chuyên khoa Phức tạp** | **`stealth/ox-alpha`** *(1M tokens, CoT)* | **`nvidia/nemotron-3-ultra-550b-a55b:free`** *(1M tokens, 550B MoE)* | **`z-ai/glm-5.2:free`** *(256K tokens, Deep Reasoning)* | **`minimax/minimax-m3:free`** |
| **3. Lập trình Script, Trích xuất JSON & Tool Calling** | **`stealth/ox-alpha`** *(1M tokens, Strict JSON)* | **`cohere/north-mini-code:free`** *(256K tokens, Code Expert)* | **`poolside/laguna-s-2.1:free`** *(262K tokens, Logic & Code)* | **`openrouter/free`** |

* **Đặc tính kỹ thuật cốt lõi**:
  - **100% Free Tier ($0 Input / $0 Output)**.
  - **Cửa sổ ngữ cảnh siêu lớn**: Lên đến **1,048,576 tokens (1M tokens)** xử lý toàn bộ bệnh án và dữ liệu lịch sử điều trị mà không lo tràn bộ nhớ.
  - **Tự động chuyển tầng (Transparent Failover)**: Nếu mô hình Tầng 1 bận hoặc rate limit, hệ thống tự động nhảy sang Tầng 2/3/4 với cùng cấu trúc đầu ra.

## 7. QUY TẮC CẮT CẦU DAO & CHỐNG VÒNG LẶP (ANTI-LOOP & CIRCUIT-BREAKER PROTOCOL)
Mọi Agent khi thực hiện bất kỳ tác vụ nào (kê đơn, chỉ định CLS, tạo tờ điều trị, gọi API, sửa code, biên dịch) BẮT BUỘC phải tuân thủ nghiêm ngặt nguyên tắc **CẮT CẦU DAO TỰ ĐỘNG**:

* **Nguyên tắc "Tối đa 2 lần thử" (Strict 2-Attempt Circuit Breaker)**:
  - Nếu một lệnh hoặc API thất bại **lần 1**: Agent được phép phân tích nguyên nhân kỹ thuật và thử khắc phục **1 lần duy nhất** (Lần 2).
  - Nếu **lần 2 vẫn thất bại**: **BẮT BUỘC CẮT CẦU DAO NGAY LẬP TỨC (HARD STOP)**. TUYỆT ĐỐI CẤM tiếp tục sửa mã thử-sai mù quáng hoặc lặp lại lệnh lỗi lần thứ 3.

* **Quy trình Chẩn đoán 4 Tầng (Pre-Flight Diagnostic Ladder) - TUYỆT ĐỐI KHÔNG ĐOÁN MÒ**:
  Trước khi sửa code hoặc chạy lại, Agent phải kiểm tra tuần tự 4 tầng sau (hoặc chạy [`HisDiagnosticDoctor.bat health`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisDiagnosticDoctor.bat)):
  1. **Tầng 1 - Xác thực (Auth)**: TokenCode còn hạn không? Đọc từ `LogSystem.txt` bằng `FileShare.ReadWrite`.
  2. **Tầng 2 - Phòng làm việc (WorkInfo)**: Tài khoản đã kích hoạt `WorkInfo` phòng làm việc (`RoomId = 5248` - P734) chưa?
  3. **Tầng 3 - Hồ sơ Bệnh nhân (Patient Status)**: Bệnh nhân có thuộc Khoa 57 (`DEPARTMENT_ID = 57`) không? Hồ sơ có bị tạm khóa/đã ra viện (`IS_PAUSE = 1`) không?
  4. **Tầng 4 - Cấu trúc DTO & Danh mục**: Tra cứu DLL thật (`InspectApiConsumer.cs` / `refs.rsp`) hoặc `HIS_AI_INTEGRATION_PLAYBOOK.md`, TUYỆT ĐỐI KHÔNG tự bịa tên thuộc tính.

* **Quy trình Bàn giao Thoát Lặp (Loop Exit & Handoff Protocol)**:
  Khi cắt cầu dao ở lần 2, Agent PHẢI xuất ngay Bảng Bàn Giao Minh Bạch gồm 3 phần:
  1. **Những phần việc ĐÃ TẠO THÀNH CÔNG** (VD: Đã tạo xong Tờ điều trị ID: 12345, Chỉ định CLS thành công).
  2. **Nguyên nhân kỹ thuật chính xác** (Trích xuất mã lỗi HTTP, Exception message thật).
  3. **Hướng dẫn Bác sĩ xử lý 1-Click trên UI HIS** (để Bác sĩ không bị gián đoạn công việc khám chữa bệnh).




