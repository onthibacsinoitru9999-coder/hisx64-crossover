# ==============================================================================
# QUY TẮC BẮT BUỘC DÀNH CHO AI AGENT (HIS AUTOMATION PROJECT RULES)
# ==============================================================================

Mọi Agent khi khởi động trong BẤT KỲ khung chat nào (khung chat mới tạo, khung chat cũ tiếp tục, trên máy ổ E:\ hay ổ D:\) BẮT BUỘC phải tuân thủ nghiêm ngặt các quy tắc sau mà KHÔNG CẦN người dùng nhắc nhở:


## 1. QUY TẮC ĐẦU PHIÊN CHO MỌI KHUNG CHAT MỚI (PRE-FLIGHT SYNC - BẮT BUỘC 100%)
* **Bất kỳ khung chat nào khi bắt đầu phiên làm việc**, trước khi xử lý yêu cầu của bác sĩ (kê đơn, tờ điều trị, chỉ định CLS, tra cứu, sửa code...), Agent PHẢI **CHỦ ĐỘNG CHẠY LỆNH PULL** bằng terminal để đồng bộ tri thức và công cụ mới nhất từ Git:
  ```powershell
  . .\set_env.ps1 ; git pull origin main
  ```
  *(Trường hợp thư mục mới chưa có `.git`, Agent tự động chạy lệnh tự phục hồi sau để liên kết ngay với repository: `git init ; git remote add origin https://github.com/onthibacsinoitru9999-coder/hisx64-crossover.git ; git fetch origin main ; git branch -M main ; git reset origin/main`)*
* **Nạp tri thức:** Sau khi pull, Agent tự động cập nhật ngữ cảnh từ [`HIS_AI_INTEGRATION_PLAYBOOK.md`](file:///f:/NB/LBP2900_R150_V330_W64_uk_EN_2/x64/MISC/ANIMIMG/his/HIS%20CSNB/HIS_AI_INTEGRATION_PLAYBOOK.md) và thư mục `.agents/skills/his-clinical-operations/` để đảm bảo nắm được toàn bộ danh mục thuốc, mã kho, bẫy lỗi và cấu trúc DTO mới nhất.

### 🌟 QUY TẮC CỨNG: BẮT BUỘC KHAI BÁO & NHẬN DIỆN CƠ SỞ ĐẦU PHIÊN (FACILITY PRE-FLIGHT DECLARATION)
* **BẮT BUỘC 100%**: Ngay khi khởi động phiên làm việc mới (hoặc trước khi thực hiện bất kỳ y lệnh lâm sàng nào), Agent PHẢI **XÁC ĐỊNH & KHAI BÁO RÕ RÀNG** đang làm việc tại cơ sở nào:
  - 🏥 **Cơ sở Hà Nội (`ha-noi` / `HN`)**:
    * **Khoa**: Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57 - `DEPARTMENT_ID = 57`)
    * **Branch**: Bệnh viện Bạch Mai - Hà Nội (`BRANCH_ID = 1`)
    * **Buồng bệnh**: P712, P714, P716, P724, P725...
    * **Phòng làm việc / Tiểu phẫu**: P734 (`RoomId = 5248`) hoặc Tiểu phẫu Nhà Q (`ExecuteRoomId = 931`)
    * **Tủ trực thuốc**: **`810` (`TT_KCTCHCS`)**
    * **Dịch vụ ĐMMM tại giường**: **`BM02426`** (Service ID: **`6217`**)
    * 🌟 **GOOGLE SHEET TOÀN KHOA HÀ NỘI**:
      - **Link Google Sheet**: [Sổ Đi Buồng & Giao Ban Khoa CTCH & CS Hà Nội](https://docs.google.com/spreadsheets/d/1z8Stz0XnEA4-s2AxKSzMijoiZxLxxOlkbYfqSiRwU28/edit?usp=drivesdk)
      - **Tab ĐI BUỒNG**: `gid=360892229` | **CSV Export**: `https://docs.google.com/spreadsheets/d/1z8Stz0XnEA4-s2AxKSzMijoiZxLxxOlkbYfqSiRwU28/export?format=csv&gid=360892229`
      - **Tab GIAO BAN**: `gid=340181132`
      - **Quy tắc cốt lõi**: Khác với Ninh Bình bệnh nhân phân tán, tại Hà Nội **ƯU TIÊN RÀ SOÁT TRÊN HIS NẾU HỎI DANH SÁCH TOÀN KHOA**; khi cần tra cứu phân công, ghi chú đi buồng, y lệnh lãnh đạo hoặc trạng thái mổ phiên/mổ cấp cứu thì đối chiếu trực tiếp tab `ĐI BUỒNG` trên Google Sheet này.
  - 🏥 **Cơ sở Ninh Bình (`ninh-binh` / `NB`)**:
    * **Khoa**: Khoa Ngoại tổng hợp - Tầng 3 Nhà E (Khoa 915 - `DEPARTMENT_ID = 915`)
    * **Branch**: Bệnh viện Bạch Mai Cơ sở 2 - Ninh Bình (`BRANCH_ID = 81`)
    * **Buồng bệnh**: Khu 3E (Phòng 3E-01 đến 3E-39), Khu 3D (Phòng 3D-01 đến 3D-38)
    * **Phòng làm việc / Thực hiện CLS**: P3E-05 (`18679` - Phòng TT Khoa CTCH & CS) hoặc P3D-05 (`18681` - Phòng TT Khoa PT tiêu hóa)
    * **Tủ trực thuốc**: **`5142` (`TTT_NBKP05.02` - Tủ trực khu 3E)** hoặc **`5141` (`TTT_NBKP05.01` - Tủ trực khu 3D)**
    * **Dịch vụ ĐMMM tại giường**: **`NB260620.6231`** (Service ID: **`74281`** - "Định lượng Glucose [Máu] mao mạch")
    * 🌟 **QUY TẮC CỨNG (HARD FORK) - ĐỐI CHIẾU DANH SÁCH BỆNH NHÂN NINH BÌNH**:
      - **Link Google Sheet**: [Danh Sách Bệnh Nhân Ninh Bình](https://docs.google.com/spreadsheets/d/1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM/edit?usp=drivesdk)
      - **Tab Danh Sách BN**: `gid=914015390` | **CSV Export Trực Tiếp**: `https://docs.google.com/spreadsheets/d/1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM/export?format=csv&gid=914015390`
      - **Bản chất nghiệp vụ khác biệt**: Khác với Hà Nội bệnh nhân tập trung từ 710 - 740 là xong, tại Ninh Bình bệnh nhân nằm rải rác, lẻ tẻ nhiều phòng khác nhau (Khu 3E & 3D). **Khi nhắc tới bệnh nhân ở Ninh Bình, Agent BẮT BUỘC phải đối chiếu với danh sách trong Google Sheet này trước tiên** để xác định đúng buồng, giường, mã BN, mã ĐT và bác sĩ phụ trách.
* **Cơ chế nhận diện & Điều phối tự động:**
  1. Nếu Bác sĩ khai báo cụ thể ("ở Ninh Bình", "3E-24", "tủ trực NB", "ở Hà Nội", "phòng 714"): Kích hoạt ngay cấu hình cơ sở tương ứng.
  2. Nếu Bác sĩ chưa khai báo: Agent kiểm tra branch Git hiện tại (`git branch --show-current`). Nếu ở `ninh-binh` thì chạy cấu hình Ninh Bình; nếu ở `ha-noi` thì chạy cấu hình Hà Nội.
  3. TUYỆT ĐỐI CẤM tự ý áp dụng catalog Hà Nội cho bệnh nhân Ninh Bình (sẽ gây lỗi `Success: false` do mã `BM02426` không có trong hợp đồng BHYT Ninh Bình) hoặc ngược lại.

## 2. QUY TẮC PHÂN ĐỊNH RÕ RÀNG NHIỆM VỤ CÁC PHẦN MỀM CON (SINGLE RESPONSIBILITY CLI MATRIX)
Mỗi công cụ `.exe` / `.bat` được thiết kế ĐỘC LẬP cho 1 mục đích chuyên biệt. **TUYỆT ĐỐI KHÔNG GỌI NHẦM CÔNG CỤ (Đặc biệt: Khi tra cứu thông tin CẤM gọi `HisAutoPrescribe.exe`)**:

| Mục Đích / Yêu Cầu Của Bác Sĩ | Công Cụ DUY NHẤT Được Phép Gọi | Lệnh Mẫu Chuẩn | TUYỆT ĐỐI CẤM DÙNG |
| :--- | :--- | :--- | :--- |
| 🔍 **Tra cứu thông tin BN, buồng, tiền sử, dịch vụ, đơn cũ** | **`HisClinicalCli.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe lookup <MãBN>` | ❌ **`HisAutoPrescribe.exe`** |
| 📋 **Xem danh sách y lệnh & trạng thái màu sắc (trắng/vàng/xanh)** | **`HisClinicalCli.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe orders <MãBN>` | ❌ Không tự cào DB |
| 🗑️ **Hủy/Xóa y lệnh chưa thực hiện (chỉ định màu trắng)** | **`HisClinicalCli.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe cancel-order <ID>` | ❌ Không xóa y lệnh đã làm |
| 🗑️ **Hủy/Xóa dịch vụ con đơn lẻ trong phiếu y lệnh** | **`HisClinicalCli.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe cancel-service <SS_ID>` | ❌ Không xóa y lệnh đã làm |
| 👥 **Đọc Biên bản Hội chẩn & Ý kiến Chuyên khoa khách** | **`HisClinicalCli.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe debate <MãBN>` | ❌ Không đoán mò |
| 💊 **Kê đơn thuốc, tiêm Insulin, tủ trực, dinh dưỡng** | **`HisAutoPrescribe.exe`** | `.\HisAutoPrescribe.exe single ...` hoặc `--batch` | ❌ Không dùng tra cứu |
| 📝 **Tạo tờ điều trị hàng ngày (Ghi diễn biến + y lệnh)** | **`HisTrackingCreator.exe`** | `.\HisTrackingCreator.exe` (Tích hợp OpenRouter AI) | ❌ Không dùng kê đơn |
| 📋 **Đối soát & kiểm tra thiếu Sơ kết 3 ngày / 7 ngày** | **`HisSummaryTrackingDoctor.exe`** | `.\HisSummaryTrackingDoctor.bat "<Buồng>"` | ❌ Không tự cào log |
| 📄 **Tạo tờ Sơ kết 3 ngày / 7 ngày tự động** | **`HisSummaryTrackingCreator.exe`** | `.\HisSummaryTrackingCreator.bat` | ❌ Không dùng kê đơn |
| 🩸 **Chỉ định ĐMMM tại giường (`BM02426`)** | **`HisGlucoseBedsideAssigner.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe` | ❌ Không dùng kê thuốc |
| 🍲 **Chỉ định Suất ăn dinh dưỡng (`BT01, DD01, TM01`)** | **`HisRationAssigner.exe`** | `.\HisRationAssigner.bat "<Buồng>"` | ❌ Không dùng kê thuốc |
| 🥛 **Chỉ định Dịch Dinh dưỡng trước mổ (Leanpro PreSur)** | **`HisLeanproAssigner.exe`** | `.\HisLeanproAssigner.bat "<MãBN1,MãBN2>"` | ❌ Không kê người >= 70t / ĐTĐ |
| 👥 **Hội chẩn chuyên khoa & Ký số EMR (Type 17 / Mps000019)** | **`HisDebateCreator.exe`** | `.\.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe` | ❌ Không dùng đơn lẻ |
| 📊 **Xuất Báo cáo buồng bệnh đồng bộ Drive** | **`HisWardReport.bat`** | `.\HisWardReport.bat` | ❌ Không dùng sửa dữ liệu |
| 🩺 **Kiểm tra sức khỏe hệ thống & Ping máy chủ** | **`HisDiagnosticDoctor.bat`** | `.\HisDiagnosticDoctor.bat health` | ❌ Không đoán mò |
| 🖼️ **Mở ảnh PACS / RIS (MRI, CT, X-Quang, Siêu âm)** | **`HisPacsCli.bat`** | `.\HisPacsCli.bat <MãBN> -Open` | ❌ Không đoán mò link |

* **Tăng tốc với OpenRouter AI:** Các công cụ tạo nội dung (Tờ điều trị, Sơ kết đợt điều trị, Báo cáo buồng) tự động nhúng `Tools\OpenRouterAiClient.cs` hoặc `openrouter_client.py` để sinh diễn biến lâm sàng siêu tốc (Model `minimax/minimax-m3:free` 1M tokens) mà không làm chậm Antigravity.
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
     - 🔍 **Đối chiếu Bệnh nhân theo Cơ sở Đã Khai Báo / Nhận Diện:**
       * **Tại Hà Nội**: Tìm kiếm và đối chiếu hồ sơ tại **Khoa CTCH & Cột sống (`DEPARTMENT_ID = 57`)**.
       * **Tại Ninh Bình**: Tìm kiếm và đối chiếu hồ sơ tại **Khoa Ngoại tổng hợp - Tầng 3 Nhà E (`DEPARTMENT_ID = 915`)**.
       * Trường hợp không tìm thấy bệnh nhân tại khoa tương ứng, Agent PHẢI báo lại ngay cho Bác sĩ.
  2. **Thực thi đồng thời 3 tác vụ y lệnh cho 100% bệnh nhân:**
     - **Tác vụ 1 - Tờ điều trị (`HisTrackingCreator.exe`):** Tạo tờ điều trị ghi nhận kết quả ĐMMM và y lệnh tiêm insulin theo từng mốc giờ (17h, 21h, 6h).
     - **Tác vụ 2 - Chỉ định CLS (`HisGlucoseBedsideAssigner.exe`):**
       * **Tại Hà Nội**: Chỉ định mã **`BM02426`** (Service ID: `6217`), Phòng thực hiện `5248` (P734) hoặc `931` (Tiểu phẫu nhà Q).
       * **Tại Ninh Bình**: Chỉ định mã **`NB260620.6231`** (Service ID: `74281` - "Định lượng Glucose [Máu] mao mạch"), Phòng thực hiện `18679` (P3E-05) hoặc `18681` (P3D-05).
     - **Tác vụ 3 - Kê đơn Insulin (`HisAutoPrescribe.exe --batch` hoặc CLI):**
       * ⚠️ **Kho Tủ Trực Bắt Buộc**:
         - **Tại Hà Nội**: Kê từ Tủ trực Khoa 57 (**`MediStockId = 810` - `TT_KCTCHCS`**).
         - **Tại Ninh Bình**: Kê từ Tủ trực Khu 3E (**`MediStockId = 5142` - `TTT_NBKP05.02`**) hoặc Khu 3D (**`5141`**).
         - **TUYỆT ĐỐI KHÔNG kê từ Kho Dược (4209/4210)**.
       * **Quy chuẩn tỷ lệ quy đổi:** `Amount = UI / 1000.0m` (VD: `8 UI` -> `0.0080 lọ`), `MedicineUseFormId = 15` (*Tiêm*), cữ tiêm `MORNING`/`NOON`/`EVENING` = chuỗi 2 chữ số (VD: `"08"`), `IsExpend = false`.
       * Kê đơn tiêm Insulin (Actrapid / Lantus / Mixtard) đúng số đơn vị và hướng dẫn dùng chuẩn lâm sàng.
  3. **Quy chuẩn Báo cáo Y Lệnh & Hiển Thị UI (BẮT BUỘC):**
     * **MÃ PHIẾU Y LỆNH LÂM SÀNG (`ServiceReqCode`)**: Bắt buộc in đậm `ServiceReqCode` (VD: `000090054138`) trên bảng kết quả. TUYỆT ĐỐI KHÔNG báo mã xuất kho dược `ExpMestCode` (VD: `000028492583`) làm bác sĩ hoang mang không tìm thấy trên EMR.
     * **BỘ LỌC HIS UI**: Luôn nhắc Bác sĩ kiểm tra bộ lọc trên giao diện HIS là **"Tất cả bác sĩ"** (thay vì "Bác sĩ hiện tại") để xem trọn vẹn y lệnh do tài khoản liên thông (`vmc` / `034727`) tạo.
  4. **Quy tắc Kiểm soát thời gian xử lý (Max 2 Attempts - Tuyệt đối không loop lâu):**
     * Trong mọi tác vụ lâm sàng (đặc biệt khi bác sĩ đang trực tiếp xử lý bệnh nhân), nếu API backend từ chối hoặc trả `Success: false` quá 2 lần, Agent **PHẢI DỪNG VÒNG LẶP NGAY LẬP TỨC**.
     * Báo cáo ngay kết quả những phần việc ĐÃ TẠO THÀNH CÔNG (Tờ điều trị, Chỉ định CLS) và hướng dẫn Bác sĩ xử lý nhanh nhất trên giao diện HIS, tuyệt đối không được tự ý viết mã thử-sai kéo dài làm chậm trễ công việc của Bác sĩ.
  5. **Báo cáo kết quả:** In bảng tổng hợp đối soát kết quả rõ ràng, minh bạch (Thành công / Lỗi từng BN).

## 6. QUY TẮC MA TRẬN MÔ HÌNH OPENROUTER: ĐIỀU PHỐI ĐA TẦNG MIỄN PHÍ 100% (MULTI-TIER SMART FALLBACK)
* **Khóa xác thực**: Tự động nạp từ biến môi trường `OPENROUTER_API_KEY` (hoặc Windows Registry `HKCU\Environment`).
* **Bộ điều phối chuẩn hóa**: Sử dụng [`openrouter_client.py`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/openrouter_client.py) hoặc lệnh CLI [`HisAiCli.bat`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisAiCli.bat) để tự động chuyển tầng dự phòng khi gặp sự cố rate-limit/timeout mà không làm gián đoạn công việc của Bác sĩ.

### 🌟 Ma trận Phân công Mô hình theo Nghiệp vụ Lâm sàng:
| Phân nhóm Nghiệp vụ | Tầng 1 (Ưu tiên số 1) | Tầng 2 (Dự phòng 1) | Tầng 3 (Dự phòng 2) | Tầng 4 (Dự phòng 3) |
| :--- | :--- | :--- | :--- | :--- |
| **1. Đa phương thức & OCR (Ảnh ĐH, Phim X-quang/CT/MRI, Phiếu KQ)** | **`minimax/minimax-m3:free`** *(1M tokens, Reasoning, JSON)* | **`google/gemma-4-31b-it:free`** *(256K tokens, Multimodal)* | **`z-ai/glm-5.2:free`** *(256K tokens)* | **`openrouter/free`** *(Auto Router)* |
| **2. Lập luận Bệnh án & Hội chẩn Chuyên khoa Phức tạp** | **`minimax/minimax-m3:free`** *(1M tokens, Deep CoT)* | **`nvidia/nemotron-3.5-lightning:free`** *(1M tokens)* | **`z-ai/glm-5.2:free`** *(256K tokens, Deep Reasoning)* | **`openrouter/free`** |
| **3. Lập trình Script, Trích xuất JSON & Tool Calling** | **`minimax/minimax-m3:free`** *(1M tokens, Strict JSON)* | **`poolside/laguna-s-2.1:free`** *(262K tokens, Code)* | **`z-ai/glm-5.2:free`** *(256K tokens, Structured)* | **`openrouter/free`** |

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

## 8. QUY TẮC BÁO CÁO BUỒNG BỆNH & ĐỒNG BỘ CLOUD DRIVE (WARD REPORT PROTOCOL)
* **Kích hoạt tự động**: Khi Bác sĩ nhắn tin hoặc yêu cầu "báo cáo buồng", "tình hình buồng bệnh", "đi buồng":
  1. **Thực thi 1-Click**: Agent chạy ngay công cụ [`HisWardReport.bat`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisWardReport.bat) (mặc định quét các buồng trọng điểm `712, 714, 716, 724, 725, 712A` hoặc thêm `--all` để quét toàn bộ Khoa 57).
  2. **Trích xuất đa chiều**:
     - Buồng - Giường, Mã BN, Mã ĐT, Họ tên, Tuổi, Giới tính.
     - Chẩn đoán chi tiết & mã ICD-10 (Đích danh tầng xẹp đốt sống, loại gãy xương, bệnh nền).
     - DHST mới nhất (Mạch, Huyết áp, Nhiệt độ, SpO2).
     - Tình trạng Tờ điều trị hôm nay (Giờ tạo, tóm tắt diễn biến).
     - Tình trạng Kê đơn thuốc, Kháng sinh, Suất ăn dinh dưỡng hôm nay.
     - Cảnh báo tự động: 🔴 Chưa tạo tờ ĐT, 🔴 Chưa kê đơn, 🟠 BN Đái tháo đường cần theo dõi ĐH/Insulin.
  3. **Tự động lưu & Đồng bộ Google Drive**:
     - **Google Drive (`onthibacsinoitru9999@gmail.com`)**: Tự động tải lên thư mục `gdrive:BaoCaoBuongBenh_Khoa57` qua `rclone`.
     - **Thư mục Dự án**: `Reports\WardReports\BaoCao_BuongBenh_YYYYMMDD_HHmmss.html` và `.md`.
  4. **Phản hồi Bác sĩ**: In bảng tổng quan Markdown trực tiếp trong chat kèm link mở file HTML trực quan (hỗ trợ tìm kiếm, lọc buồng và in PDF khổ A4).

## 9. QUY TẮC BẢO GÌ LÀM NẤY - TRỰC DIỆN, SIÊU TỐC & TUYỆT ĐỐI KHÔNG LAN MAN (STRICT SCOPE & FAST RESPONSE PROTOCOL)
* **Tuyệt đối tuân thủ đúng phạm vi yêu cầu (Strict Scope)**:
  - Bác sĩ hỏi buồng nào (VD: `Phòng 714`), Agent CHỈ kiểm tra và trả lời đích danh buồng đó.
  - **TUYỆT ĐỐI CẤM** tự ý quét lan sang các buồng khác (712, 715, 716, 724...), không tự ý tra cứu lịch sử bệnh nhân cũ đã ra viện/chuyển đi từ các ngày trước, không tự động chạy quét toàn viện (`--all`) khi không có yêu cầu.
* **Tốc độ phản hồi tức thì (Fast Response - Dưới 15 giây)**:
  - Nếu buồng rỗng/không có bệnh nhân: Phải báo ngay lập tức: **"Phòng [X] hiện đang trống (0 bệnh nhân)"**, kết thúc phản hồi ngắn gọn trong 1-2 câu. TUYỆT ĐỐI KHÔNG mất vài phút đào bới thông tin thừa.
* **Nguyên tắc "Đúng trọng tâm, ngắn gọn, súc tích"**:
  - Không in các bảng dữ liệu ngoài phạm vi câu hỏi.
  - Mọi thao tác kiểm tra phải dứt điểm, tiết kiệm tối đa thời gian của Bác sĩ trong ca trực.

## 10. QUY TẮC BẤT KHẢ XÂM PHẠM: CHỐNG ẢO GIÁC & CHỈ BÁO CÁO DỮ LIỆU ĐỐI SOÁT THẬT (ZERO HALLUCINATION & EVIDENCE-ONLY REPORTING)
* **Bản chất sai lầm cần triệt tiêu:** Khi bị thúc ép thời gian hoặc khi người dùng chất vấn, Agent có xu hướng tự sinh (bịa) số phiếu y lệnh, ID hoặc khẳng định "đã thành công" trong khi lệnh nền chưa hoàn tất hoặc script bị lỗi ngầm. Đây là điều **CẤM KỴ TUYỆT ĐỐI** trong môi trường lâm sàng y tế vì đe dọa trực tiếp an toàn người bệnh và phá hủy hoàn toàn niềm tin của Bác sĩ.
* **4 Rào chắn kỹ thuật bắt buộc (4 Mandatory Technical Guardrails):**
  1. **Không có Log thật = Không có Lời (Evidence-Based Output):** Tuyệt đối không tự gõ bất kỳ số phiếu (`ServiceReqCode`), ID hay kết quả nào vào tin nhắn nếu chuỗi ký tự đó KHÔNG nằm trong STDOUT của lệnh vừa thực thi dứt điểm. Cấm in bảng kết quả dự kiến khi chưa chạy lệnh thật.
  2. **Quy trình 3 bước Bắt buộc (Pre-check -> Execute -> Post-verify):**
     - **Bước 1 (Pre-check):** Tra cứu dữ liệu hiện tại bằng `GetView` (để tránh kê trùng và xác định chính xác cái gì còn thiếu).
     - **Bước 2 (Execute):** Thực thi lệnh tạo mới (POST API).
     - **Bước 3 (Post-verify - BẮT BUỘC):** Truy vấn lại trực tiếp bảng cơ sở dữ liệu (`HIS_SERE_SERV_RATION`, `HIS_SERVICE_REQ`, `HIS_SERE_SERV`) để lấy chính xác các bản ghi vừa được chèn vào DB và in mã phiếu từ DB ra.
  3. **Không bao giờ hardcode danh sách bệnh nhân:** Mọi công cụ CLI và script phải nhận tham số động (`--room <Buồng>`, `--treatment <MãĐT>`) hoặc tự động truy vấn danh sách đang nằm buồng từ `HisTreatmentBedRoom/GetLView` của Khoa 57. Tuyệt đối không gán cứng mảng ID trong code `.cs`.
  4. **Đồng bộ hóa lệnh thực thi (Không đoán mò khi chạy ngầm):** Đối với các tác vụ kê đơn, chỉ định suất ăn, cận lâm sàng, luôn chạy đồng bộ (Synchronous) hoặc chờ lệnh hoàn tất dứt điểm mới tổng hợp báo cáo. Tuyệt đối không vừa bấm lệnh vừa tự bịa kết quả để trả lời trước.

## 11. QUY TẮC BẢO MẬT & ĐỐI SOÁT GOOGLE SHEET BÁO CÁO HOẠT ĐỘNG & HÀNH CHÍNH
### 11.1. Cơ Sở Hà Nội: Báo Cáo Hoạt Động & Hành Chính Khoa CTCH & Cột Sống (Khoa 57)
* **File nguồn chuẩn hóa**: [Báo Cáo Hoạt Động & Hành Chính Khoa CTCH & Cột Sống - Cơ Sở Hà Nội](https://docs.google.com/spreadsheets/d/1z8Stz0XnEA4-s2AxKSzMijoiZxLxxOlkbYfqSiRwU28/edit?gid=340181132#gid=340181132)
* **Quy tắc cứng BẤT KHẢ XÂM PHẠM (STRICT READ-ONLY - 100%)**:
  - **TUYỆT ĐỐI CẤM** ghi chép, chỉnh sửa, xóa, thêm hàng/cột, cập nhật hay can thiệp bất kỳ dữ liệu nào vào file Google Sheet này dưới mọi hình thức.
  - File này là tài liệu điều hành và báo cáo hoạt động hành chính của Khoa CTCH & Cột Sống cơ sở Hà Nội.
  - Agent CHỈ ĐƯỢC PHÉP QUÉT, ĐỐI SOÁT và THAM CHIẾU (Read-Only) để nắm bắt bức tranh toàn cảnh lâm sàng, hỗ trợ Bác sĩ tra cứu và kiểm tra thông tin.
* **Cấu trúc 9 Sheet chuyên biệt của Hệ thống**:
  1. 📊 **`DASHBOARD`** (`gid=340181132`): Bảng điều khiển giao ban, thống kê tổng BN nội trú, phân loại bệnh nhân (hậu phẫu ổn định, điều trị nội khoa, mổ phiên, làm chẩn đoán, đã xếp lịch mổ, chưa xếp lịch...), phẫu thuật, vào/ra viện hôm nay và biểu đồ tự động.
  2. 👥 **`DANH SÁCH BN`** (`gid=520846210`): Danh sách bệnh nhân chi tiết (Mã BN, Mã ĐT, Buồng, Giường, Chẩn đoán chính, Tiền sử, Bệnh sử, Khám, Bilan mổ, Link phim, Link bệnh án, PTV chính/phụ, BS điều trị, SĐT người nhà, Phân loại chăm sóc).
  3. 🩺 **`ĐI BUỒNG`** (`gid=360892229`): Bảng theo dõi đi buồng lâm sàng theo Buồng/Giường, hướng xử trí của Bác sĩ, y lệnh lãnh đạo đi buồng, ĐD bàn giao theo dõi.
  4. 🚨 **`MỔ CẤP CỨU`** (`gid=1791790800`): Danh sách bệnh nhân mổ cấp cứu tại khoa hoặc hội chẩn từ khoa khác (CC Lưu, Hồi sức Ngoại P...), tình trạng mổ, PTV.
  5. 📝 **`THÔNG QUA MỔ HÀNG NGÀY`** (`gid=282218154`): Danh sách bệnh nhân thông qua mổ hàng ngày của khoa.
  6. 📅 **`LỊCH MỔ NGÀY`** (`gid=1532702217`): Lịch mổ chi tiết theo phòng mổ (Phòng 5 CS, Phòng 2 CTCH), kíp mổ, giờ dự kiến, thời lượng mổ.
  7. 📋 **`BÁO CÁO TRỰC`** (`gid=779922548`): Báo cáo số liệu giao ban của kíp trực (Bác sĩ trực, Điều dưỡng trực, tổng số bệnh nhân).
  8. 📚 **`DANH MỤC`** (`gid=258386294`): Danh mục chuẩn hóa phân loại BN, bilan mổ, phương pháp mổ, danh sách PTV, phân loại chăm sóc, phòng mổ.
  9. 📂 **`RA VIỆN`** (`gid=351678517`): Lưu trữ dữ liệu hồ sơ bệnh nhân đã ra viện / chuyển khoa / chuyển viện.

### 11.2. Cơ Sở Ninh Bình: Danh Sách Bệnh Nhân Khoa Ngoại Tổng Hợp (Khoa 915) — 🌟 QUY TẮC CỨNG (HARD FORK)
* **File nguồn chuẩn hóa**: [Danh Sách Bệnh Nhân Ninh Bình (Google Sheet)](https://docs.google.com/spreadsheets/d/1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM/edit?usp=drivesdk)
  - **Tab Danh Sách BN**: `gid=914015390`
  - **CSV Export Trực Tiếp (1-Click, không cần API key)**: `https://docs.google.com/spreadsheets/d/1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM/export?format=csv&gid=914015390`
  - **Form điền tiếp nhận BN mới**: [Google Form Tiếp Nhận BN Ninh Bình](https://docs.google.com/forms/d/e/1FAIpQLSeWar3VqZElU1unVs_JQL68BYUi_R7zgGJ8RYhWzi-OInMO2A/viewform)
* **Quy tắc cứng BẤT KHẢ XÂM PHẠM (HARD FORK - 100%)**:
  1. **Đặc thù buồng bệnh Ninh Bình**: Khác với Hà Nội bệnh nhân tập trung trong dãy buồng `710 - 740` (Khoa 57), tại Cơ sở 2 Ninh Bình bệnh nhân phân bố **lẻ tẻ, rải rác trên rất nhiều phòng** thuộc Khu 3E (`3E-16` đến `3E-39`) và Khu 3D (`3D-13` đến `3D-38`).
  2. **Bắt buộc đối chiếu trước khi thực thi**: **Bất cứ khi nào Bác sĩ nhắc tới hoặc xử lý bệnh nhân ở Ninh Bình, Agent BẮT BUỘC phải đối chiếu với danh sách trong Google Sheet này trước tiên** để:
     - Xác định chính xác bệnh nhân có đang nằm trong danh sách quản lý của Khoa không.
     - Lấy đích danh Buồng bệnh (`3E-XX`) và Giường bệnh (`GXX`) thật.
     - Đối soát Mã BN (`TDL_PATIENT_CODE`), Mã ĐT (`TREATMENT_CODE`) và Chẩn đoán chính để không bị nhầm lẫn bệnh nhân trùng tên.
  3. **Thao tác thêm BN mới vào danh sách**: Sử dụng script `submit_form_ninh_binh.py` hoặc POST trực tiếp `formResponse` của Google Form Ninh Bình (`1FAIpQLSeWar3VqZElU1unVs_JQL68BYUi_R7zgGJ8RYhWzi-OInMO2A`) để dữ liệu tự động cập nhật ngay vào Google Sheet.

## 12. QUY TẮC XỬ LÝ DẤU TIẾNG VIỆT KHI TRA CỨU TÊN BỆNH NHÂN: "HÒA" VS "HOÀ" (VIETNAMESE ACCENT & TONE VARIANT SEARCH PROTOCOL)
* **Bản chất kỹ thuật (Gotcha)**:
  - Hệ thống HIS / Cơ sở dữ liệu Oracle phân biệt nghiêm ngặt các kiểu đặt dấu thanh tiếng Việt kiểu truyền thống (dấu trên âm đệm) và kiểu mới (dấu trên nguyên âm chính).
  - *Ví dụ thực tế:* **`hòa`** (ò + a) khác biệt hoàn toàn với **`hoà`** (o + à); tương tự: **`hóa`** / **`hoá`**, **`thủy`** / **`thuỷ`**, **`khỏe`** / **`khoẻ`**...
  - Khi điều dưỡng nhập tên vào hệ thống: có bệnh nhân lưu là `TÔ XUÂN HÒA` (kiểu cũ), nhưng bệnh nhân khác lại lưu là `NGUYỄN VĂN HOÀ` (kiểu mới). Nếu chỉ tìm kiếm chính xác một kiểu gõ có dấu, hệ thống sẽ bỏ sót hoàn toàn bệnh nhân thuộc kiểu gõ kia!
* **Quy trình tra cứu bắt buộc (Mandatory Protocol)**:
  1. **Thử tìm kiếm không dấu (Unaccented)**: Khi tìm kiếm theo tên, luôn ưu tiên thử biến thể không dấu (VD: tìm `hoa` thay vì chỉ `hòa` hay `hoà`) hoặc quét in-memory danh sách buồng bệnh Khoa 57 bằng hàm `RemoveDiacritics()` để vét cạn toàn bộ các biến thể dấu.
  2. **Thử hoán đổi cả 2 kiểu dấu**: Nếu tìm kiếm có dấu, phải tự động thử cả biến thể kiểu cũ và kiểu mới (`òa` ⟷ `oà`, `óa` ⟷ `oá`, `ủy` ⟷ `uỷ`...).
  3. **Đối chiếu đa chiều (Cross-Reference)**: Khi có nhiều bệnh nhân trùng tên/trùng từ khóa (VD: `TÔ XUÂN HÒA` và `NGUYỄN VĂN HOÀ`), tuyệt đối không được tự ý chọn ngầm bản ghi cuối cùng (`LastOrDefault`). Bắt buộc liệt kê toàn bộ danh sách khớp kèm Mã BN, Mã ĐT, Buồng/Giường, Năm sinh và Chẩn đoán để Bác sĩ đối soát chính xác, tránh nhầm lẫn y lệnh.

## 13. QUY TẮC HỦY/XÓA Y LỆNH & DỊCH VỤ CHƯA THỰC HIỆN: CHỈ ĐỊNH MÀU TRẮNG (CANCEL ORDER PROTOCOL)
* **Bản chất kỹ thuật & Cơ chế 2 bước chuẩn của HIS (Bypass Protocol)**:
  - Khi xem y lệnh do Bác sĩ khác chỉ định, nút xóa (thùng rác) bị khóa ở cả Client lẫn Backend MOS (Backend chặn với mã lỗi `DuLieuDoNguoiKhacTaoKhongChoPhepXoa` nếu Token xóa khác với `REQUEST_LOGINNAME`).
  - **Cơ chế hoạt động chuẩn của hệ thống**:
    * **Bước 1 (Chuyển quyền người chỉ định)**: Hệ thống HIS Desktop (Module `HIS.Desktop.Plugins.ServiceReqUpdateInstruction.dll`) cho phép Bác sĩ chỉnh sửa Người chỉ định qua API **`POST api/HisServiceReq/UpdateCommonInfo`**, chuyển `REQUEST_LOGINNAME` về tài khoản của mình (`034727` - Ths.BS Nguyễn Hữu Sâm). Backend MOS cho phép nghiệp vụ này mà không chặn.
    * **Bước 2 (Xóa y lệnh)**: Sau khi `REQUEST_LOGINNAME` trùng với phiên làm việc hiện tại, lệnh **`POST api/HisServiceReq/Delete`** được Backend phê duyệt và đổi `IS_DELETE = 1` thành công 100%.
  - Công cụ **`HisClinicalCli.exe cancel-order`** đã tích hợp tự động cơ chế 2 bước này: tự kiểm tra người chỉ định, nếu khác thì tự động gọi `UpdateCommonInfo` rồi gọi `Delete`, giúp Bác sĩ hủy mọi chỉ định nhầm/thừa (màu trắng) tức thì 1-click!
* **Quy trình thực thi chuẩn (Mandatory Workflow)**:
  1. **Xem danh sách & màu sắc y lệnh**:
     ```powershell
     .\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe orders <MãBN|MãĐT|Tên>
     ```
     - ⚪ **Màu trắng (`SERVICE_REQ_STT_ID == 1`)**: Chưa thực hiện 👉 **Được phép hủy/xóa**.
     - 🟡 **Màu vàng (`SERVICE_REQ_STT_ID == 2`)**: Đang thực hiện / đã tiếp nhận mẫu 👉 **TUYỆT ĐỐI KHÔNG xóa** (phải liên hệ phòng thực hiện hủy tiếp nhận trước).
     - 🟢 **Màu xanh (`SERVICE_REQ_STT_ID == 3`)**: Đã hoàn thành / có kết quả 👉 **TUYỆT ĐỐI KHÔNG xóa**.
  2. **Hủy toàn bộ phiếu y lệnh**:
     ```powershell
     .\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe cancel-order <ServiceReqId|ServiceReqCode>
     ```
     - Script tự động kiểm tra rào chắn trạng thái trắng, tự động hủy văn bản ký EMR liên kết (nếu có), và gọi API `api/HisServiceReq/Delete` với `RequestRoomId` của khoa 57.
  3. **Hủy dịch vụ con đơn lẻ trong phiếu**:
     ```powershell
     .\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe cancel-service <SereServId>
     ```
* **Ý nghĩa an toàn lâm sàng**: Giúp Bác sĩ xử lý ngay các chỉ định thừa/nhầm lẫn trong phiên trực mà không bị gián đoạn công việc hay vi phạm quy chế hồ sơ bệnh án.

## 14. QUY TẮC BẮT BUỘC: BỘ 10 TIÊU CHUẨN BILAN CƠ BẢN TIỀN PHẪU (ROUTINE PRE-OP 10-CHECKLIST)
Mọi Agent khi nhận yêu cầu "soát bilan", "kiểm tra bilan", "đối soát bilan mổ", "thiếu bilan gì", hoặc chuẩn bị thông qua mổ (PT-01) **BẮT BUỘC PHẢI RÀ SOÁT TỐI THIỂU ĐẦY ĐỦ 10 TIÊU CHUẨN CƠ BẢN NÀY** mà KHÔNG ĐƯỢC PHÉP BỎ SÓT BẤT KỲ MỤC NÀO:

| STT | Tên Cận Lâm Sàng Cơ Bản | Mã Dịch Vụ / Dịch Vụ Trên HIS | Điều Kiện & Quy Chuẩn Lâm Sàng Bắt Buộc |
| :---: | :--- | :--- | :--- |
| **1** | 🩸 **Tổng phân tích tế bào máu** | `BM00110` (Laser 1772) | Bắt buộc 100% (Hb, WBC, PLT). |
| **2** | 🩸 **Định nhóm máu hệ ABO, Rh(D)** | `BM01700` (Gelcard/Scangel 1464) | Bắt buộc 100% (Xác định rõ ABO nhóm gì, Rh(D) Dương/Âm). |
| **3** | ⏱️ **Đông máu cơ bản** | `BM00531`, `BM260527.52`, `BM00542` | Bắt buộc 100% đủ bộ 3: PT/TQ, APTT/TCK, Fibrinogen. |
| **4** | 🧪 **Sinh hóa máu** | `BM02304`, `BM01361`, `BM10249`, `BM01352`, `BM01347`, `BM00132` | Bắt buộc 100% đủ 6 chỉ số: Ure, Creatinin, Glucose, AST/GOT, ALT/GPT, Điện giải đồ (Na-K-Cl). |
| **5** | 🦠 **Bộ 3 Vi sinh** | `BM00871`, `BM00859`, `BM00837` | Bắt buộc 100% đủ bộ 3: HIV Ag/Ab, HBsAg, HCV Ab (Báo cáo rõ Âm tính/Dương tính). |
| **6** | 🩺 **Siêu âm ổ bụng tổng quát** | `BM00199` (P.17547) | **BẮT BUỘC 100%** (Đánh giá gan mật, tụy, lách, thận, bàng quang; phát hiện bệnh lý ổ bụng tiềm ẩn trước gây mê). |
| **7** | 🩻 **X-quang ngực thẳng số hóa** | `BM21074` / `BM00338` (P.17552) | Bắt buộc 100% (Đánh giá bóng tim, nhu mô phổi trước gây mê/phẫu thuật). |
| **8** | 🧪 **Tổng phân tích nước tiểu** | `BM02998` (P.566) | Bắt buộc 100% (10 thông số máy tự động, sàng lọc nhiễm trùng tiết niệu, protein niệu, glucose niệu). |
| **9** | ⚡ **Điện tim thường (ECG)** | `BM04258` (P.931) | Bắt buộc 100% (Điện tâm đồ 12 chuyển đạo thường quy trước mổ). |
| **10** | 🫀 **Siêu âm Doppler tim, van tim** | `BM00201` (P.1715) | ⚠️ **QUY TẮC CỨNG:**<br>• **BẮT BUỘC với người bệnh ≥ 60 tuổi** (100%).<br>• **BẮT BUỘC với người bệnh > 50 tuổi VÀ có bệnh lý tim mạch** (Tăng huyết áp, ĐTĐ, bệnh mạch vành, rối loạn nhịp, suy tim, bệnh van tim...).<br>• Các trường hợp khác: Tùy chỉ định lâm sàng. |

* **Quy chuẩn Báo cáo Đối Soát Bilan (Output Protocol):**
  - Khi xuất kết quả đối soát bilan, Agent **BẮT BUỘC in bảng 10 mục** với 3 cột rõ ràng:
    1. Tên mục & Mã dịch vụ.
    2. Trạng thái (🟢 Đã có KQ kèm số liệu/kết luận, 🟡 Đang làm/Chờ duyệt, 🔴 Chưa chỉ định).
    3. Cảnh báo hành động (nếu thiếu bất kỳ mục nào trong 10 mục trên, phải bôi đỏ và đề xuất lệnh bổ sung ngay).
  - **TUYỆT ĐỐI KHÔNG BAO GIỜ BỎ QUÊN:** Siêu âm ổ bụng (`BM00199`), Siêu âm tim (`BM00201`), Nhóm máu (`BM01700`) và Vi sinh!

## 15. QUY TẮC BẮT BUỘC: TỰ ĐỘNG GÁN Y LỆNH THUỐC VÀO TỜ ĐIỀU TRỊ (MANDATORY PRESCRIPTION-TRACKING LINKAGE PROTOCOL)
* **BẮT BUỘC 100% - KHÔNG ĐƯỢC PHÉP QUÊN:**
  - Bất cứ khi nào Agent hoặc phần mềm thực hiện kê thuốc cho bệnh nhân (dù là **TỦ TRỰC** hay **LĨNH KHO DƯỢC**), **BẮT BUỘC PHẢI TỰ ĐỘNG GÁN Y LỆNH ĐÓ VÀO TỜ ĐIỀU TRỊ (`HIS_TRACKING`) CỦA NGÀY HÔM ĐÓ**.
  - **KHÔNG TẠO PHẦN MỀM MỚI:** Logic này đã được tích hợp trực tiếp ngay trong mã nguồn của chính các công cụ kê đơn cốt lõi (`HisCabinetPrescribe.cs` và `HisWarehousePrescribe.cs`).
* **Bản chất Kỹ thuật & Nghiệp vụ HIS/EMR**:
  - Khi API kê đơn (`OutPatientPresCreateList` hoặc `InPatientPresCreate`) tạo phiếu thuốc thành công, hệ thống chỉ mới sinh bản ghi `HIS_SERVICE_REQ` và `HIS_EXP_MEST`.
  - Nếu không gọi cập nhật Tờ điều trị, y lệnh thuốc sẽ **KHÔNG HIỂN THỊ** trong cột "Y lệnh" trên giao diện Tờ điều trị của phần mềm HIS và phần mềm EMR Bệnh án điện tử.
* **Cơ chế Thực thi Chuẩn hóa (`api/HisTracking/Update`)**:
  1. **Tìm Tờ điều trị trong ngày**: Quét danh sách Tờ điều trị của đợt điều trị (`HisTrackingViewFilter { TREATMENT_ID = ... }`). Lấy tờ điều trị mới nhất trong ngày (hoặc tạo mới nếu chưa có).
  2. **Gán ServiceReqId vào Tracking SDO**:
     ```csharp
     var sdo = new HisTrackingSDO();
     sdo.Tracking = trk;
     sdo.WorkingRoomId = roomId > 0 ? roomId : 5248;
     if (dhsts != null && dhsts.Count > 0) sdo.Dhst = dhsts[0];
     sdo.UsedForServiceReqIds = new List<long> { serviceReqId };
     sdo.ServiceReqs = new List<TrackingServiceReq>
     {
         new TrackingServiceReq
         {
             ServiceReqId = serviceReqId,
             IsNotShowMedicine = false,
             IsNotShowMaterial = false,
             IsNotShowOutMedi = false,
             IsNotShowOutMate = false
         }
     };
     ```
  3. **Nối diễn giải y lệnh vào `MEDICAL_INSTRUCTION`**: Tự động ghép tên thuốc, số lượng và hướng dẫn sử dụng vào nội dung chỉ định điều trị để Tờ điều trị hiển thị trực quan đầy đủ cả text lẫn liên kết dữ liệu hệ thống.
  4. **Gọi API cập nhật**: `POST api/HisTracking/Update` với `ApiConsumers.MosConsumer`.



