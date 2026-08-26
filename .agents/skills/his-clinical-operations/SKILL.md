---
name: his-clinical-operations
description: >-
  Comprehensive guide and automation suite for clinical operations on Inventec HIS/MOS system.
  Use when creating tracking sheets (tờ điều trị), vital signs (dấu hiệu sinh tồn),
  prescribing medications (kê đơn thuốc nội trú/kho/tủ trực/hướng thần/gây nghiện/dinh dưỡng),
  assigning paraclinical tests (chỉ định CLS: Xét nghiệm, CĐHA, TDCN), and looking up clinical patterns.
---

# HIS/MOS Clinical Operations Automation Guide

Hệ thống tài liệu và hướng dẫn toàn diện dành cho Agent nhằm tự động hóa 100% các nghiệp vụ lâm sàng trên hệ thống Bệnh viện Inventec HIS / MOS API Consumer, hỗ trợ bác sĩ thao tác nhanh chóng và chính xác qua mã nguồn ngầm.

---

## 1. Kiến trúc & Thông tin Đăng nhập Hệ thống

- **MOS Backend Endpoint:** `http://192.168.7.236:1608/`
- **Tài khoản bác sĩ mặc định:**
  - LoginName: `vmc` / Password: `789789` / Version: `2.390.0`
  - Bác sĩ: **BS. VŨ MINH CƯỜNG**
  - Khoa làm việc: **Khoa Chấn thương Chỉnh hình & Cột sống** (Department ID: `57`)
  - Phòng làm việc: **Phòng 734** (Room ID: `5248`)

---

## 2. Danh Mục Các Kho Thuốc, Dinh Dưỡng, Gây Nghiện - Hướng Thần & Tủ Trực

Hệ thống quản lý dược phẩm và vật tư nội trú được phân bổ theo các kho chuyên biệt:

| Phân nhóm kho | Tên Kho Dược / Vật tư | MediStock ID | Mã Kho | Mục đích & Loại dược phẩm |
| :--- | :--- | :--- | :--- | :--- |
| **Kho Dược Chính** | **Kho thuốc viên** | **`4210`** | `KT_KD15` | Thuốc viên nén, viên nang, viên sủi, gói uống |
| | **Kho thuốc ống** | **`4209`** | `KT_KD14` | Thuốc tiêm ống, lọ tiêm, dung dịch tiêm truyền |
| | **Kho thuốc Hướng thần** | **`4208`** | `KT_KD13` | Thuốc hướng thần, an thần, chống trầm cảm (Diazepam, Midazolam, Rotunda...) |
| | **Kho thuốc Gây nghiện** | **`4207`** | `KT_KD12` | Thuốc giảm đau gây nghiện, tiền chất (Morphin, Fentanyl, Pethidine, Codein...) |
| | **Kho Vắc xin** | **`4168`** | `KT_KD10` | Huyết thanh, Vắc xin phòng uốn ván (SAT, VAT...) |
| | **Kho Dịch truyền Lọc màng bụng** | **`5837`** | `KDT_KDNT` | Dịch truyền chuyên biệt |
| | **Kho Thuốc Chương trình** | **`236`** | `KTCT` | Thuốc chương trình y tế quốc gia |
| **Kho Dinh Dưỡng** | **Kho Sản phẩm Dinh dưỡng điều trị** | **`753`** | `LA_TTDDLS` | Sữa chuyên biệt, đạm, dịch nuôi ăn qua sonde, súp dinh dưỡng cao năng lượng |
| | **Kho Dinh Dưỡng chung** | **`268`** | `DD` | Thực phẩm và chế phẩm dinh dưỡng |
| | **Tủ trực SP Dinh dưỡng Khoa 57** | **`7787`** | `TTSPDD_9` | Sản phẩm dinh dưỡng cấp phát tại chỗ của Khoa CTCH & Cột sống |
| **Khoa CTCH (Khoa 57)**| **Tủ trực Khoa CTCH & Cột sống** | **`810`** | `TT_KCTCHCS` | Thuốc cấp cứu, thuốc dùng trong phiên trực tại khoa |
| | **Kho Vật tư Khoa CTCH & Cột sống** | **`796`** | `KVT_KCTCGCS` | Băng bột, nẹp, chỉ phẫu thuật, gạc, dẫn lưu, VTTH |
| | **Labo Khoa CTCH & Cột sống** | **`701`** | `LA_KCTCHCS` | Vật tư labo / xét nghiệm nhanh tại khoa |

---

## 3. Quy trình 1: Kê Đơn Thuốc Nội Trú / Kho Chuyên Biệt

### Endpoint: `api/HisServiceReq/InPatientPresCreate`
### Payload: `MOS.SDO.InPatientPresSDO`

```csharp
InPatientPresSDO presSDO = new InPatientPresSDO
{
    TreatmentId = tId,
    InstructionTime = trackingTime,
    InstructionTimes = new List<long> { trackingTime },
    UseTimes = new List<long> { trackingTime },
    TrackingId = trackingId,
    TrackingInfos = new List<TrackingInfoSDO>
    {
        new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
    },
    RequestRoomId = 5248, // Phòng 734
    RequestLoginName = "vmc",
    RequestUserName = "Vũ Minh Cường",
    IcdCode = "T01.9",
    IcdName = "Đa chấn thương",
    PresMedicines = new List<PresMedicineSDO>
    {
        new PresMedicineSDO
        {
            MedicineTypeId = 3388, // ID loại thuốc / dinh dưỡng
            MediStockId = 4210,    // Kho tương ứng (4210: Thuốc viên, 4209: Ống, 4208: Hướng thần, 4207: Gây nghiện, 753: Dinh dưỡng)
            Amount = 1.0m,
            PatientTypeId = 1,     // 1: BHYT, 2: Viện phí
            Tutorial = "Cách dùng chi tiết..."
        }
    }
};

var res = adapter.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, presSDO, param);
```

### Kê Đơn Thuốc Tủ Trực / Ngoại Trú (AssignPrescriptionPK - 2 Bước):
1. **Bước 1 (Giữ thuốc/khóa lô - Take Bean):**
   - Endpoint: `POST api/HisMedicineBean/Take`
   - Payload: `TakeBeanSDO { TypeId = medicineTypeId, MediStockId = stockId, PatientTypeId = 1, Amount = amount, ClientSessionKey = sessionKey, ExpiredDate = now }`
   - Nhận về: `List<HIS_MEDICINE_BEAN>` chứa `ID` của các bean tạm giữ (hỗ trợ tách nhiều bean tự động nếu liều thuốc chia lô lẻ).
2. **Bước 2 (Tạo đơn thuốc - OutPatientPresCreateList):**
   - Endpoint: `POST api/HisServiceReq/OutPatientPresCreateList`
   - Payload: `List<OutPatientPresSDO>` với `ClientSessionKey = sessionKey`, `IsCabinet = true`, `MedicineBeanIds = takenBeans.Select(b => b.ID).ToList()`
   - Nhận về: `OutPatientPresResultSDO` chứa `ExpMests`, `ServiceReqs`, `Medicines`.

### Công cụ Tự Động Kê Đơn Hàng Loạt Đóng Gói Sẵn (`HisAutoPrescribe.exe`):
- **Vị trí file:** `.agents/skills/his-clinical-operations/scripts/HisAutoPrescribe.exe` hoặc thư mục gốc `HisAutoPrescribe.exe`.
- **Giao diện bảng (GUI):** Nhấp đúp để mở bảng nhập liệu (4 cột: Mã BN/Mã ĐT, Số liều UI, Thuốc Actrapid/Lantus, Thời gian dùng). Hỗ trợ phím tắt `Ctrl+V` dán trực tiếp từ Excel!
- **Dòng lệnh (CLI siêu tốc ~1-2 giây):**
  ```powershell
  # Kê đơn đơn lẻ:
  .\HisAutoPrescribe.exe "000007060703" "8" "Actrapid" "chiều 17h"
  
  # Kê đơn hàng loạt từ file txt/csv:
  .\HisAutoPrescribe.exe "danh_sach_ke_don.csv"
  ```

---

## 4. Công cụ 1-Click Theo Dõi Cận Lâm Sàng Tồn Đọng Khoa CTCH & Cột Sống (`HisClsCtchTracker.exe`)

Phần mềm tự động quét và giám sát 100% các chỉ định Cận Lâm Sàng (Xét nghiệm, X-quang, CT, MRI, Siêu âm, Thăm dò chức năng, Giải phẫu bệnh) của toàn bộ bệnh nhân nội trú Khoa CTCH & Cột sống (Khoa 57) đang ở trạng thái **Chưa xử lý (01)** và **Đang xử lý (02)**.

- **Vị trí file:** `HisClsCtchTracker.exe` (Thư mục gốc) hoặc `.agents/skills/his-clinical-operations/scripts/HisClsCtchTracker.exe`.
- **Giao diện 1-Click (GUI):**
  - Nhấp đúp vào `HisClsCtchTracker.exe` -> Tự động đăng nhập và tải ngay danh sách toàn bộ CLS tồn đọng.
  - Bộ lọc tức thì: Lọc theo Trạng thái (🔴 Chưa xử lý / 🟡 Đang xử lý), Phân loại CLS (Xét nghiệm, CĐHA, Siêu âm, TDCN, GPB), Buồng bệnh (Phòng 711, 712, 715, 740...).
  - Tìm kiếm thông minh: Gõ tên BN, mã BN, tên xét nghiệm, phòng thực hiện... lọc tức thì.
  - Tự động quét (Auto-refresh 60 giây).
  - 1-Click **Xuất Excel (CSV)** & 1-Click **Copy Giao Ban Ca Trực** (dán Zalo/Viber).
- **Dòng lệnh (CLI):**
  ```powershell
  .\HisClsCtchTracker.exe --cli
  ```

---

## 5. Quy trình 2: Tạo Tờ Điều Trị & Dấu Hiệu Sinh Tồn (`HisTrackingCreator.exe`)

### Endpoint: `api/HisTracking/Create` và `api/HisDhst/Create`
- `HisTrackingSDO`: Ghi nhận diễn biến bệnh (`Content`), chẩn đoán ICD, khoa điều trị (`DepartmentId: 57`), phòng làm việc (`WorkingRoomId`).
- `HIS_DHST`: Lưu các chỉ số Mạch, Nhiệt độ, Huyết áp (`BLOOD_PRESSURE_MAX`/`MIN`), Nhịp thở, SpO2, Cân nặng.
- **Ứng dụng đóng gói sẵn**: `HisTrackingCreator.exe` (Thư mục gốc & `.agents/skills/his-clinical-operations/scripts/HisTrackingCreator.exe`) kèm `Chay_Tao_ToDieuTri.bat`.
- **Cú pháp CLI**:
  ```powershell
  # Tạo tờ điều trị cho mã BN cụ thể:
  .\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "BN tỉnh táo, không sốt, vết mổ khô sạch đầu chi ấm" -care "Chăm sóc cấp II. Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày" -med "Thuốc theo đơn"
  ```

---

## 5. Quy trình 3: Chỉ Định Cận Lâm Sàng (Xét nghiệm, CĐHA, TDCN)

### Endpoint: `api/HisServiceReq/AssignServiceByInstructionTimes`
### Payload: `MOS.SDO.AssignServiceSDO`
- **`SessionCode = null`** (Bắt buộc với y lệnh mới).
- **`MultipleExecute = 1`** & **`EkipInfos = new List<EkipSDO>()`**.
- `InstructionTime`, `InstructionTimes`, `UseTimes`, `TrackingInfos` đồng bộ tuyệt đối với `TRACKING_TIME`.
- **Lưu ý chuyên khoa CTCH & Cột sống & Quy tắc Bilan Tiền Phẫu:**
  - ⚠️ **Các ca xẹp đốt sống dự kiến phẫu thuật bơm xi măng (BXM / Kyphoplasty / Vertebroplasty):** BẮT BUỘC phải rà soát và có kết quả **Đo mật độ xương (DEXA - T-score)** kèm theo MRI cột sống trước khi thông qua mổ. Khi soát bilan, nếu chưa có DEXA phải chủ động cảnh báo nhắc bác sĩ bổ sung ngay.
  - 🫀 **Chỉ định Siêu âm tim (Echocardiography) trước mổ:**
    - **BN > 60 tuổi:** BẮT BUỘC rà soát / chỉ định **Siêu âm tim** đánh giá chức năng tim trước phẫu thuật.
    - **BN > 50 tuổi có bệnh lý nền liên quan tim mạch** (Tăng huyết áp, Đái tháo đường, Bệnh mạch vành/Nhồi máu cơ tim cũ, Suy tim, Rối loạn nhịp...): BẮT BUỘC rà soát / chỉ định **Siêu âm tim**.
    - Khi soát bilan hoặc soạn biên bản thông qua mổ: Tự động đối chiếu tuổi và tiền sử bệnh nền của BN để cảnh báo bổ sung Siêu âm tim nếu chưa có.
  - 👁️ **Khám chuyên khoa Mắt tiền phẫu (Soi đáy mắt):**
    - **Áp dụng cho:** Bệnh nhân phẫu thuật có **tư thế nằm sấp** (Phẫu thuật cột sống ngực, thắt lưng, cố định cột sống, giải ép, bơm xi măng thân đốt...) có **tiền sử Đái tháo đường (ĐTĐ type 1 / type 2)**.
    - **Mục đích:** Sàng lọc bệnh võng mạc đái tháo đường, tổn thương vi mạch đáy mắt, dự phòng biến chứng thiếu máu thị thần kinh do tư thế nằm sấp kéo dài.
    - Khi soát bilan hoặc tạo biên bản thông qua mổ: Tự động cảnh báo và yêu cầu **Hội chẩn / Khám chuyên khoa Mắt** nếu chưa có.

---

## 6. Quy Chuẩn Danh Mục Mã ICD-10 (Hệ 5 Ký Tự Mới - Bắt Buộc)

⚠️ **Nguyên tắc cốt lõi:** Bệnh viện đã cập nhật và áp dụng toàn diện danh mục mã ICD-10 hệ 5 ký tự chi tiết theo chuẩn Bộ Y tế. Các mã 3-4 ký tự cũ đã được chuyển trạng thái ngừng sử dụng (**`IS_ACTIVE = 0`**). Khi AI Agent tra cứu, tạo y lệnh hoặc tư vấn mã ICD, BẮT BUỘC phải sử dụng các mã 5 ký tự đang hoạt động (**`IS_ACTIVE = 1`**):

- **Viêm gan virus B:**
  - ✅ **`B18.19`**: Bệnh viêm gan virus B mạn tính không có viêm gan D [tác nhân delta], giai đoạn khác và/hoặc không xác định *(Mã chuẩn đang dùng)*
  - ✅ **`B18.10`**: Bệnh viêm gan virus B mạn tính không có viêm gan D, giai đoạn dung nạp miễn dịch
  - ❌ *Tuyệt đối không dùng `B18.1` hoặc `B18.0` (Hệ thống sẽ báo không tồn tại).*
- **Viêm cơ mủ / Áp xe cơ:**
  - ✅ **`M60.05`**: Viêm cơ do nhiễm trùng, vùng chậu và/hoặc đùi *(Mã chuẩn cho áp xe cơ đùi/mông)*
  - ❌ *Không dùng `M60.0` (đã deactive).*
- **Chấn thương xương khớp & Cột sống thường gặp:**
  - ✅ `M47.00†`, `M47.8`, `M51.2`, `M51.3` (Thoái hóa/thoát vị đĩa đệm cột sống)
  - ✅ `M48.50` (Xẹp lún thân đốt sống)
  - ✅ `S62.11` (Gãy xương tháp/cổ tay), `S42.00` (Gãy xương đòn), `S33.0` (Rách vòng xơ đĩa đệm thắt lưng)
  - ✅ `E11.9` (Đái tháo đường type 2), `K74.0` (Xơ hóa gan), `B95.6` (Tụ cầu vàng)
- **Quy tắc cho code C#:** Khi tra cứu `api/HisIcd/Get`, luôn lọc `IS_ACTIVE == 1` để đảm bảo 100% mã hợp lệ trên giao diện HIS/MOS.

---

## 7. Quy Chuẩn Trích Xuất Chẩn Đoán Hình Ảnh (Bắt Buộc Đích Danh Tầng & Vị Trí)

⚠️ **Nguyên tắc an toàn phẫu thuật:** Tuyệt đối không mô tả rút ngắn chung chung các kết quả MRI, X-quang, CT Scanner. Phải nêu đích danh từng tầng giải phẫu, tình trạng cấp/cũ để phục vụ chính xác cho việc thông qua mổ và can thiệp ngoại khoa:

- **Tổn thương Cột sống:**
  - Bắt buộc ghi rõ từng tầng đốt sống bị **xẹp cấp (có phù tủy xương)** và **xẹp cũ**:
    - ✅ **Đúng:** `Xẹp cấp L2, L3, L5 (phù tủy xương)`, `Xẹp cũ T12`, `Trượt L4 ra trước độ I kèm hẹp ống sống`, `Rách vòng xơ đĩa đệm L4/5`.
    - ❌ **Cấm kỵ:** `Xẹp lún phù tủy xương các đốt sống thắt lưng` (mơ hồ, gây nguy cơ can thiệp hoặc bơm xi măng nhầm vị trí).
- **Tổn thương Xương khớp chi:**
  - Bắt buộc ghi rõ từng xương, đoạn gãy, độ di lệch, tình trạng gân cơ:
    - ✅ **Đúng:** `Gãy di lệch xương tháp và xương thang cổ tay trái`, `Gãy 1/3 giữa xương đòn phải`, `Đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải`.
    - ❌ **Cấm kỵ:** `Gãy xương cổ tay`, `Gãy xương đòn`, `Đứt gân bàn tay`.

---

## 8. Công Cụ Chỉ Định Đường Máu Mao Mạch Tại Giường (`HisGlucoseBedsideAssigner.exe`)

Công cụ chuyên dụng cho phép chỉ định hàng loạt cận lâm sàng **`BM02426`** (*Xét nghiệm đường máu mao mạch tại giường*) cho bệnh nhân nội trú:
- **Tập tin chạy**: `HisGlucoseBedsideAssigner.exe` (kèm file config `HisGlucoseBedsideAssigner.exe.config` và file kích hoạt nhanh `Chay_ChiDinh_BM02426.bat`)
- **Mã dịch vụ**: `BM02426` (Service ID: `6217`)
- **Phòng thực hiện**: `931` (Phòng Tiểu Phẫu Nhà Q) / `531` (P289 Khoa CTCH)
- **Tính năng nổi bật**:
  - Hỗ trợ chọn đồng thời nhiều khung giờ (`06:00`, `11:00`, `17:00`, `21:00`, `Hiện tại`, Giờ tùy chỉnh `HH:mm`).
  - Nhập mã BN từ clipboard / Excel hoặc 1-click tải toàn bộ bệnh nhân nội trú Khoa 57.
  - Tự động liên kết hoặc tạo mới tờ điều trị `HIS_TRACKING` tương ứng với giờ chỉ định.
  - Xuất báo cáo CSV / Excel và Copy kết quả tiện lợi.
  - Chạy giao diện WinForms trực quan hoặc CLI:
    ```powershell
    .\HisGlucoseBedsideAssigner.exe -p "0003969449,0003298895" -time "06:00,11:00,17:00,21:00"
    ```

---

## 9. Tích Hợp AI Trích Xuất Dữ Liệu & Ma Trận Mô Hình OpenRouter Free Tier (Multi-Tier Smart Fallback)

Khi xử lý trích xuất y lệnh từ hình ảnh bảng điều dưỡng, tóm tắt bệnh án, đối soát đơn thuốc hoặc chuyển đổi dữ liệu lâm sàng sang cấu trúc JSON:
- **Ma trận 7 Tầng Tự Động Chuyển Tầng (100% Free Tier - $0 Input / $0 Output)**:
  * **Tier 1 (Vua Đa phương thức & Suy luận 1M Context)**: `stealth/ox-alpha` (Ưu tiên số 1)
  * **Tier 2 (Dự phòng Đa phương thức 1M Context)**: `minimax/minimax-m3:free`
  * **Tier 3 (Dự phòng Đa phương thức 256K Context Google)**: `google/gemma-4-31b-it:free`
  * **Tier 4 (Siêu mô hình Suy luận 550B MoE 1M Context)**: `nvidia/nemotron-3-ultra-550b-a55b:free`
  * **Tier 5 (Chuyên sâu Lập trình & Logic 256K Context)**: `cohere/north-mini-code:free` & `poolside/laguna-s-2.1:free`
  * **Tier 6 (Mô hình Ngôn ngữ Lớn 256K Context)**: `z-ai/glm-5.2:free`
  * **Tier 7 (Bộ định tuyến Tự động)**: `openrouter/free`
- **Công cụ điều phối tự động**:
  * **CLI nhanh**: [`HisAiCli.bat`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisAiCli.bat) (`ask`, `ocr`, `json`, `models`).
  * **Module Python**: [`openrouter_client.py`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/openrouter_client.py) (`generate_with_fallback`, `extract_json_structured`).
  * **OCR Đường Huyết & Insulin**: [`parse_glucose_image.py`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/parse_glucose_image.py) & [`HisDiabetesOrchestrator.ps1`](file:///e:/his-x64-28-11fix%20GDYK/his-x64/HisDiabetesOrchestrator.ps1).



