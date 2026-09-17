# 🏥 HƯỚNG DẪN TỔNG HỢP TOÀN BỘ SKILL & CÔNG CỤ TỰ ĐỘNG HÓA HIS DÀNH CHO AGENT HERMES

> **Dành cho**: AI Agent (Hermes Agent / Subagents / External Agents)  
> **Hệ thống mục tiêu**: Inventec HIS / MOS / EMR - Bệnh viện Bạch Mai  
> **Mục đích**: Tổng hợp toàn bộ 5 Skill lâm sàng, danh mục mã chuẩn hóa, đường dẫn file thực thi và cú pháp lệnh CLI 1-Click để Agent có thể tra cứu và thực thi ngay lập tức.

---

## 📌 1. ĐƯỜNG DẪN THƯ MỤC CỐT LÕI (CORE PATHS)

* **Thư mục gốc dự án (Project Root)**: `.` hoặc `e:\his-x64-28-11fix GDYK\his-x64\` (hoặc `d:\his\his-x64-28-11fix GDYK\his-x64\`)
* **Thư mục chứa Skills chuẩn**: `.agents\skills\`
* **Thư mục chứa Binary/Scripts (.exe, .bat, .cs)**: `.agents\skills\his-clinical-operations\scripts\` và thư mục gốc `.\`
* **File Playbook chi tiết**: [`HIS_AI_INTEGRATION_PLAYBOOK.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HIS_AI_INTEGRATION_PLAYBOOK.md)
* **File Quy tắc Agent**: [`AGENTS.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/AGENTS.md)
* **File Log hệ thống HIS (để lấy TokenCode)**: `Logs\LogSystem.txt` (hoặc `Logs\HLSLogSystem.txt`)

---

## 🧭 2. MA TRẬN 5 SKILL LÂM SÀNG CỐT LÕI (CLINICAL SKILLS MATRIX)

| Tên Skill | Vị trí SKILL.md | Mô Tả & Nghiệp Vụ Chính |
| :--- | :--- | :--- |
| **`his-clinical-operations`** | [`.agents\skills\his-clinical-operations\SKILL.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-clinical-operations/SKILL.md) | Cẩm nang toàn diện: Đăng nhập, tra cứu BN, kê đơn thuốc, tạo tờ điều trị, chỉ định CLS, giám sát tồn đọng, ĐMMM tại giường, ma trận OpenRouter AI. |
| **`his-prescription-orders`** | [`.agents\skills\his-prescription-orders\SKILL.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-prescription-orders/SKILL.md) | Tự động kê đơn thuốc nội trú/ra viện, từ điển cách dùng chuẩn hóa lâm sàng (Ceftriaxone, Zinacef, Unasyn dồn sáng, chia liều), đơn tủ trực, đơn thay băng. |
| **`his-treatment-tracking`** | [`.agents\skills\his-treatment-tracking\SKILL.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-treatment-tracking/SKILL.md) | Tạo Tờ điều trị (`HIS_TRACKING`), ghi nhận diễn biến bệnh, y lệnh, sinh hiệu (`HIS_DHST`), quy trình ký số/mời ký EMR (Type 7). |
| **`his-service-and-ration-orders`** | [`.agents\skills\his-service-and-ration-orders\SKILL.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-service-and-ration-orders/SKILL.md) | Chỉ định Suất ăn dinh dưỡng bệnh lý (`BT01`, `DD01`, `TM01`) và Cận lâm sàng (Xét nghiệm, CĐHA, TDCN) chuẩn xác 100%, đối soát từng bữa ăn. |
| **`his-consultation-orders`** | [`.agents\skills\his-consultation-orders\SKILL.md`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-consultation-orders/SKILL.md) | Tạo Phiếu chỉ định hội chẩn & Trích biên bản hội chẩn (EMR Type 17 / Mps000019), mời ký Chủ tọa (`hdc`), Thư ký (`034727`/`vmc`), tra cứu ý kiến CK khách. |

---

## 🛠️ 3. BẢNG PHÂN CÔNG NHIỆM VỤ CLI & LỆNH THỰC THI (SINGLE RESPONSIBILITY MATRIX)

> ⚠️ **QUY TẮC BẮT BUỘC**: Mỗi công cụ được thiết kế độc lập. Tuyệt đối không gọi nhầm công cụ (Đặc biệt: **Khi tra cứu cấm dùng `HisAutoPrescribe.exe`**).

| Nghiệp Vụ Của Bác Sĩ | Công Cụ DUY NHẤT Được Phép Dùng | Đường Dẫn Thực Thi File | Cú Pháp Lệnh CLI Mẫu Chuẩn |
| :--- | :--- | :--- | :--- |
| 🔍 **Tra cứu BN, buồng, tiền sử, dịch vụ, đơn cũ** | `HisClinicalCli.exe` | `.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe` | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe lookup <MãBN>` |
| 👥 **Đọc Biên bản Hội chẩn & Ý kiến CK khách** | `HisClinicalCli.exe` | `.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe` | `.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe debate <MãBN>` |
| 💊 **Kê đơn thuốc / Tiêm Insulin / Tủ trực** | `HisAutoPrescribe.exe` | `.\HisAutoPrescribe.exe` | `.\HisAutoPrescribe.exe "000007060703" "8" "Actrapid" "chiều 17h"` |
| 📝 **Tạo tờ điều trị hàng ngày (Diễn biến + Y lệnh)** | `HisTrackingCreator.exe` | `.\HisTrackingCreator.exe` | `.\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "BN tỉnh..." -care "Chăm sóc cấp II" -med "Thuốc theo đơn"` |
| ⚡ **Tạo tờ điều trị nhanh (Quick)** | `QuickTracking.exe` | `.\QuickTracking.exe` | `.\QuickTracking.exe -p 0003969449 -time 08:00 -content "BN tỉnh táo không sốt"` |
| 📋 **Đối soát thiếu Sơ kết 3 ngày / 7 ngày** | `HisSummaryTrackingDoctor.exe` | `.\HisSummaryTrackingDoctor.bat` | `.\HisSummaryTrackingDoctor.bat "<TênBuồng>"` |
| 📄 **Tạo tờ Sơ kết 3 ngày / 7 ngày tự động** | `HisSummaryTrackingCreator.exe` | `.\HisSummaryTrackingCreator.bat` | `.\HisSummaryTrackingCreator.bat` |
| 🩸 **Chỉ định ĐMMM tại giường (`BM02426`)** | `HisGlucoseBedsideAssigner.exe` | `.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe` | `.\.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe -p "0003969449" -time "06:00,11:00,17:00,21:00"` |
| 🍲 **Chỉ định Suất ăn Dinh dưỡng bệnh lý** | `QuickRation.exe` / `HisRationAssigner.bat` | `.\QuickRation.exe` hoặc `.\HisRationAssigner.bat` | `.\QuickRation.exe -p 0003967577 -combo DD01 -date 20260829,20260830` |
| 🩹 **Kê đơn vật tư & dung dịch thay băng** | `HisDressingOrder.exe` | `.agents\skills\his-clinical-operations\scripts\HisDressingOrder.exe` | `.\.agents\skills\his-clinical-operations\scripts\HisDressingOrder.exe -t <MãĐT> -c "Vết mổ khô sạch"` |
| 👥 **Chỉ định & Biên bản Hội chẩn (EMR 17)** | `HisDebateCreator.exe` | `.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe` | `.\.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe -t <MãĐT> -s "Khoa Nội tiết" -m "Tóm tắt..."` |
| 📊 **Xuất Báo cáo buồng bệnh đồng bộ Drive** | `HisWardReport.bat` | `.\HisWardReport.bat` | `.\HisWardReport.bat --all` hoặc `.\HisWardReport.bat` |
| 🔬 **Giám sát CLS tồn đọng Khoa 57** | `HisClsCtchTracker.exe` | `.\HisClsCtchTracker.exe` | `.\HisClsCtchTracker.exe --cli` |
| 🩺 **Kiểm tra sức khỏe hệ thống / Auth** | `HisDiagnosticDoctor.bat` | `.\HisDiagnosticDoctor.bat` | `.\HisDiagnosticDoctor.bat health` |
| 🤖 **Điều phối AI OpenRouter miễn phí 100%** | `HisAiCli.bat` / `openrouter_client.py` | `.\HisAiCli.bat` | `.\HisAiCli.bat ask "Câu hỏi lâm sàng..."` |

---

## 🎯 4. HƯỚNG DẪN THỰC THI CHI TIẾT TỪNG NGHIỆP VỤ

### 4.1. Tra Cứu Bệnh Nhân & Đọc Biên Bản Hội Chẩn
```powershell
# 1. Tra cứu thông tin hồ sơ, chẩn đoán, giường nằm, đơn thuốc cũ:
.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe lookup 0003969449

# 2. Đọc toàn văn kết luận & ý kiến của chuyên khoa khách (Tim mạch, Hô hấp, Nội tiết...):
.\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe debate 0003969449
```

### 4.2. Tạo Tờ Điều Trị & Dấu Hiệu Sinh Tồn (Treatment Tracking)
```powershell
# Cách 1: HisTrackingCreator đầy đủ diễn biến, chế độ ăn, chăm sóc, thuốc:
.\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "Bệnh nhân tỉnh táo, không sốt, vết mổ khô sạch, dẫn lưu ra 20ml dịch hồng" -care "Chăm sóc cấp II. Ăn BT01. Theo dõi DHST 2 lần/ngày" -med "Thuốc theo đơn"

# Cách 2: QuickTracking nhanh gọn:
.\QuickTracking.exe -p 0003969449 -time 08:00 -content "BN tỉnh táo không sốt huyết động ổn"
```

### 4.3. Kê Đơn Thuốc & Tiêm Insulin
```powershell
# Kê đơn Insulin lẻ:
.\HisAutoPrescribe.exe "000007060703" "8" "Actrapid" "chiều 17h"

# Kê đơn hàng loạt từ file danh sách:
.\HisAutoPrescribe.exe "danh_sach_ke_don.csv"
```
* **Quy chuẩn tỷ lệ quy đổi liều Insulin**:
  * `Amount = UI / 1000.0m` (VD: `8 UI` $\rightarrow$ `0.0080 lọ`).
  * `MedicineUseFormId = 15` (*Tiêm*).
  * Khung giờ cữ tiêm: `MORNING` / `NOON` / `EVENING` = chuỗi 2 chữ số (VD: `"08"`).
  * **BẮT BUỘC kê từ Kho Tủ Trực Khoa 57 (`MediStockId = 810` - `TT_KCTCHCS`)**, TUYỆT ĐỐI KHÔNG kê từ Kho Dược (4209/4210).

### 4.4. Chỉ Định Đường Máu Mao Mạch Tại Giường (`BM02426`)
```powershell
# Chỉ định ĐMMM cho 1 hoặc nhiều bệnh nhân tại nhiều khung giờ:
.\.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe -p "0003969449,0003298895" -time "06:00,11:00,17:00,21:00"
```
* **Mã dịch vụ**: `BM02426` (Service ID: `6217`).
* **Phòng thực hiện**: `931` (Phòng Tiểu Phẫu Nhà Q) / `531` (P289 Khoa CTCH).

### 4.5. Chỉ Định Suất Ăn Dinh Dưỡng Bệnh Lý
```powershell
# Kê suất ăn cho BN ngày mai (mặc định combo Ngoại khoa BT01, 3 bữa):
.\QuickRation.exe -p 0003983111

# Kê suất ăn theo combo bệnh lý cho nhiều ngày:
.\QuickRation.exe -p 0003967577 -combo DD01 -date 20260829,20260830,20260831

# Kê suất ăn cho cả buồng bệnh:
.\QuickRation.exe -room 712 -combo BT01 -date 20260829,20260830
```

### 4.6. Chỉ Định & Soạn Biên Bản Hội Chẩn Chuyên Khoa
```powershell
# Mời Hội chẩn Chuyên khoa Tạo hình thẩm mỹ:
.\.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe -t 000007070917 -s "ck tạo hình thẩm mỹ" -m "Bn nam chẩn đoán Vết thương phức tạp mu bàn chân (P). Hiện tại vết lóc da có diện da hoại tử đen vạt ngược kích thước 4cm, xin ý kiến CK tạo hình xét nhận bệnh nhân điều trị / phối hợp." -l "Khoa Chấn thương Chỉnh hình và Cột sống"

# Mời Hội chẩn Viện Tim Mạch:
.\.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe -t 000007070917 -s "Viện Tim Mạch" -m "Bệnh nhân cao tuổi có tiền sử THA kiểm soát kém kèm cơn rung nhĩ kịch phát, xin ý kiến chuyên khoa tim mạch tối ưu hóa thuốc và đánh giá nguy cơ gây mê phẫu thuật." -l "Phòng 716 Khoa 57"
```

### 4.7. Kê Đơn Dung Dịch & Vật Tư Thay Băng Hàng Ngày
```powershell
.\.agents\skills\his-clinical-operations\scripts\HisDressingOrder.exe -t <MãBệnhÁn> -c "Vết mổ khô sạch, thay băng rửa vết thương hàng ngày"
```
* **Quy chuẩn**: Tự động kê 1 lọ Povidone 10% 125ml (`TH.POVI008`) + 1 chai NaCl 0.9% 500ml (`TH.NATR047`) từ Kho Tủ Trực 810 với cờ **Hao phí (`IsExpend = true`)**.

### 4.8. Báo Cáo Buồng Bệnh & Đồng Bộ Cloud Drive
```powershell
# Quét các buồng trọng điểm (712, 714, 716, 724, 725, 712A):
.\HisWardReport.bat

# Quét toàn bộ 22 buồng Khoa 57:
.\HisWardReport.bat --all

# Quét và mở file HTML trong trình duyệt:
.\HisWardReport.bat --open
```
* **Đồng bộ tự động**: Lưu vào `C:\Users\1995\OneDrive\BaoCaoBuongBenh_Khoa57\` và `Reports\WardReports\`.

---

## ⚡ 5. ĐẶC QUYỀN PROTOCOL: "THỢ CHO ĐƯỜNG HUYẾT"

Khi Bác sĩ gửi ảnh/bảng báo cáo đường huyết và nhắc **"thợ cho đường huyết"**, Agent kích hoạt tự động 3 tác vụ theo đúng **Thứ tự Tuần tự (Sequential Pipeline)**:
1. **Bước 1 (Bắt buộc chạy trước) - Tờ điều trị (`HisTrackingCreator.exe`)**: Tạo tờ điều trị ghi nhận kết quả ĐMMM và y lệnh tiêm insulin theo từng mốc giờ (17h, 21h, 6h).
2. **Bước 2 - Chỉ định CLS (`HisGlucoseBedsideAssigner.exe`)**: Chỉ định xét nghiệm ĐMMM tại giường **`BM02426`** cho các mốc giờ (mốc 06:00 tự động tính ngày hôm sau).
3. **Bước 3 (Bắt buộc chạy sau cùng) - Kê đơn Insulin (`HisAutoPrescribe.exe`)**: Kê đơn tiêm Insulin từ Kho Tủ Trực Khoa 57 (`MediStockId = 810`).
   * **Quy tắc lùi 5 phút (Timing Offset Rule)**: Kê đơn Insulin thực hiện SAU KHI ĐÃ CÓ TỜ ĐIỀU TRỊ. Thời gian y lệnh thuốc (`InstructionTime`) tự động **lùi +5 phút sau thời điểm Tờ điều trị** (`InstructionTime = TrackingTime + 5 phút`, vd: Tờ điều trị 17:00 $\rightarrow$ Đơn thuốc 17:05) để chống nhảy ngược vào tờ điều trị buổi sáng.
   * Ký hiệu viết tắt: `R` (Actrapid), `L` (Lantus), `M` (Mixtard).
   * Ví dụ: `8R` = 8 UI Actrapid, `12L` = 12 UI Lantus.

---

## 📚 6. BỘ DANH MỤC HỆ THỐNG CỐT LÕI (SYSTEM CATALOGS)

### 6.1. Danh Mục Kho Dược & Tủ Trực
* `4210` (`KT_KD15`): Kho thuốc viên.
* `4209` (`KT_KD14`): Kho thuốc ống (Kháng sinh tiêm, giảm đau truyền).
* `4208` (`KT_KD13`): Kho thuốc Hướng thần (Seduxen, Diazepam).
* `4207` (`KT_KD12`): Kho thuốc Gây nghiện (Morphin, Fentanyl).
* `804`: Kho Dịch truyền (Natri Clorid 0.9% 100ml, 250ml, 500ml).
* `810` (`TT_KCTCHCS`): **Tủ trực Khoa CTCH & Cột sống (Khoa 57)** *(Dùng cho đơn cấp cứu, tiêm Insulin, vật tư thay băng)*.
* `753` (`LA_TTDDLS`): Kho Sản phẩm Dinh dưỡng điều trị (Sữa chuyên biệt, đạm).

### 6.2. Danh Mục Combo Suất Ăn Dinh Dưỡng Bệnh Lý
* **Phòng thực hiện**: `5809` (Trung tâm Dinh dưỡng Lâm sàng) | `PatientTypeId = 42`.
* **Combo `BT01` (Ngoại khoa / Bình thường)**:
  * Sáng 06h: `BM17770` (Service ID: `30073`, RationTimeId: `1`)
  * Trưa 11h: `BM18696` (Service ID: `30153`, RationTimeId: `3`)
  * Chiều 17h: `BM18697` (Service ID: `30154`, RationTimeId: `5`)
* **Combo `DD01` (Đái tháo đường)**:
  * Sáng 06h: `CO02` (Service ID: `30180`, RationTimeId: `1`)
  * Trưa 11h: `CS01` (Service ID: `30181`, RationTimeId: `3`)
  * Chiều 17h: `BM18650` (Service ID: `30133`, RationTimeId: `5`)
* **Combo `TM01` (Tim mạch / Tăng huyết áp)**:
  * Sáng 06h: `BM18075` (Service ID: `30117`, RationTimeId: `1`)
  * Trưa 11h: `BM18039` (Service ID: `30093`, RationTimeId: `3`)
  * Chiều 17h: `BM18040` (Service ID: `30094`, RationTimeId: `5`)

### 6.3. Danh Mục Mã ICD-10 Chuẩn Hệ 5 Ký Tự Mới
> ⚠️ **BẮT BUỘC dùng mã 5 ký tự (`IS_ACTIVE = 1`), không dùng mã 3-4 ký tự cũ.**
* `B18.19`: Viêm gan virus B mạn tính giai đoạn khác/không xác định *(thay cho B18.1)*.
* `M60.05`: Viêm cơ do nhiễm trùng vùng chậu/đùi *(thay cho M60.0)*.
* `M48.50`: Xẹp lún thân đốt sống.
* `M51.2`: Thoát vị đĩa đệm thắt lưng khác.
* `S62.11`: Gãy xương tháp/cổ tay.
* `S42.00`: Gãy xương đòn.
* `E11.9`: Đái tháo đường type 2 không có biến chứng.

---

## 🛡️ 7. QUY TẮC AN TOÀN & CẮT CẦU DAO (STRICT CIRCUIT-BREAKER)

1. **Pre-flight Git Pull**: Mọi phiên làm việc bắt buộc chạy `git pull origin main` đầu tiên.
2. **Quy tắc 2 lần thử (Max 2 Attempts)**: Nếu lệnh/API thất bại lần 1, phân tích kỹ thuật và thử sửa lần 2. Nếu lần 2 vẫn lỗi $\rightarrow$ **CẮT CẦU DAO NGAY LẬP TỨC (HARD STOP)**, không lặp lại lần 3.
3. **Báo cáo bàn giao minh bạch**: Khi cắt cầu dao, xuất ngay:
   - Các phần việc ĐÃ TẠO THÀNH CÔNG.
   - Nguyên nhân kỹ thuật cụ thể (mã lỗi HTTP, exception).
   - Hướng dẫn Bác sĩ xử lý 1-click trên UI HIS.
4. **Trích xuất CĐHA đích danh**: Nêu rõ từng tầng tổn thương (VD: `Xẹp cấp L2, L3, L5`, `Gãy 1/3 giữa xương đòn phải`), tuyệt đối không ghi chung chung.
5. **Đọc Token không gây khóa file**: Luôn mở file log `Logs\LogSystem.txt` với chế độ `FileShare.ReadWrite`.
