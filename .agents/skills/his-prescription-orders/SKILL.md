---
name: his-prescription-orders
description: >-
  Tự động kê đơn thuốc điều trị nội trú, đơn thuốc ra viện và sao chép y lệnh
  trên hệ thống HIS/MOS Bệnh viện Bạch Mai qua API chuẩn xác 100%.
  Bao gồm hệ thống phân định độc lập 2 luồng: Kê Tủ Trực (Cabinet) vs Kê Lĩnh Kho Dược (Warehouse),
  từ điển cách dùng chuẩn hóa lâm sàng (Ceftriaxone, Zinacef, Medivernol dồn sáng,
  pha dung môi, chia liều, tốc độ truyền), chống trùng lặp, và không bao giờ để bác sĩ phải sửa tay.
---

# HIS Prescription Orders & InPatient Medication Management

Skill này cung cấp quy trình và mã nguồn chuẩn hóa để **Kê đơn thuốc điều trị nội trú**, **Tiêm Insulin**, **Dinh dưỡng trước mổ (Leanpro)** và **Vật tư thay băng** trực tiếp lên máy chủ MOS Backend (`http://192.168.7.236:1608/`) Bệnh viện Bạch Mai với **ĐỘ CHÍNH XÁC TUYỆT ĐỐI 100% VỀ CẢ SỐ LƯỢNG VÀ CÁCH DÙNG NGAY TỪ LẦN GỌI ĐẦU TIÊN**, tuyệt đối không để bác sĩ phải thao tác chỉnh sửa tay trên giao diện.

---

## 0. NGUYÊN TẮC BẮT BUỘC: PHÂN TÁCH 2 HỆ THỐNG KÊ ĐƠN ĐỘC LẬP (CABINET vs WAREHOUSE)

Hệ thống HIS Inventec áp dụng 2 luồng xử lý xuất kho hoàn toàn khác biệt. **Agent TUYỆT ĐỐI KHÔNG ĐƯỢC NHẦM LẪN GIỮA 2 LUỒNG**:

```
                              ┌───────────────────────────────────────────────┐
                              │            BÁC SĨ CHỈ ĐỊNH THUỐC              │
                              └──────────────────────┬────────────────────────┘
                                                     │
                         ┌───────────────────────────┴───────────────────────────┐
                         ▼                                                       ▼
        ┌──────────────────────────────────┐                   ┌──────────────────────────────────┐
        │       HỆ 1: KÊ TỦ TRỰC           │                   │     HỆ 2: KÊ LĨNH KHO DƯỢC       │
        │  (Thuốc cấp cứu, tiêm Insulin,   │                   │  (Thuốc viên, ống lĩnh hàng ngày,│
        │   Leanpro 7787, Thay băng 810)   │                   │   SP dinh dưỡng điều trị 753)    │
        └────────────────┬─────────────────┘                   └────────────────┬─────────────────┘
                         │                                                      │
        ┌────────────────▼─────────────────┐                   ┌────────────────▼─────────────────┐
        │ • MCP: his_prescribe_cabinet     │                   │ • MCP: his_prescribe_warehouse   │
        │ • CLI: HisCabinetPrescribe.bat   │                   │ • CLI: HisWarehousePrescribe.bat │
        │ • IS_CABINET = 1                 │                   │ • IS_CABINET = 0                 │
        │ • Flow 2 bước:                   │                   │ • Flow 1 bước:                   │
        │   1. api/HisExpMest/TakeBeanSDO  │                   │   api/HisExpMest/                │
        │   2. OutPatientPresCreateList    │                   │   InPatientPresCreate            │
        │ • Kho HN: 810 (TT_KCTCHCS),      │                   │ • Kho HN: 4210 (Viên), 4209 (Ống)│
        │           7787 (TTSPDD_9)        │                   │           753 (Kho SP Dinh dưỡng)│
        │ • Kho NB: 5142 (3E), 5141 (3D)   │                   │ • Kho NB: 4854 (Kho dược chính)  │
        │ ❌ CẤM gọi InPatientPresCreate   │                   │ ❌ CẤM gọi TakeBeanSDO           │
        └──────────────────────────────────┘                   └──────────────────────────────────┘
```

---

## 1. CHI TIẾT 2 CÔNG CỤ MCP NATIVE KÊ ĐƠN

### 1.1. Hệ Kê Tủ Trực: `his_prescribe_cabinet`
* **Mục đích**: Kê thuốc, dịch truyền, tiêm Insulin, Leanpro trước mổ, hóa chất/vật tư thay băng từ tủ trực buồng bệnh / tủ trực khoa.
* **Tham số**:
  - `mode`: `"single"` (1 thuốc), `"multi"` (toa nhiều thuốc), `"insulin"` (tiem insulin), `"leanpro"` (dinh dưỡng trước mổ), `"dressing"` (vật tư thay băng), `"stock"` (xem tồn tủ trực).
  - `patientCode`: Mã bệnh nhân hoặc mã điều trị (VD: `"0001666593"`).
  - `stockId`: Mã kho tủ trực (Mặc định `810` - Hà Nội Khoa 57; `7787` - Tủ trực dinh dưỡng TTSPDD_9; `5142` - Ninh Bình Khu 3E; `5141` - Ninh Bình Khu 3D).
  - `medicineName`: Tên/mã thuốc khi `mode='single'`.
  - `amount`: Số lượng thuốc (hoặc số chai Leanpro).
  - `tutorial`: Hướng dẫn sử dụng.
  - `items`: Mảng chuỗi thuốc khi `mode='multi'` (`["TenThuoc|SoLuong|HDSD|[DuongDungId]"]`).
  - `insulinUnits`: Số đơn vị Insulin UI khi `mode='insulin'` (VD: `8`, `10`, `12`).
  - `insulinType`: `"R"` (Actrapid), `"L"` (Lantus), `"M"` (Mixtard).
  - `timeSlot`: Mốc giờ (`"17:00"`, `"21:00"`, `"06:00"`).
  - `keyword`: Từ khóa lọc danh mục tồn khi `mode='stock'`.

* **Ví dụ gọi**:
  ```json
  // Kê 1 lọ Ceftriaxone tủ trực 810:
  {"mode": "single", "patientCode": "0001666593", "medicineName": "Rocephin", "amount": 2, "stockId": 810, "tutorial": "Pha 2 lo truyen TM sang"}

  // Kê tiêm 8UI Actrapid cữ 17h:
  {"mode": "insulin", "patientCode": "0001666593", "insulinUnits": 8, "insulinType": "R", "timeSlot": "17:00", "stockId": 810}

  // Kê 6 chai Leanpro trước mổ từ tủ trực dinh dưỡng 7787:
  {"mode": "leanpro", "patientCode": "0001666593", "amount": 6, "stockId": 7787}

  // Kê gói hóa chất thay băng (Povidone + Muối rửa hao phí):
  {"mode": "dressing", "patientCode": "0001666593", "stockId": 810}

  // Tra cứu tồn tủ trực Khoa 57:
  {"mode": "stock", "stockId": 810, "keyword": "Insulin"}
  ```

### 1.2. Hệ Kê Lĩnh Kho Dược: `his_prescribe_warehouse`
* **Mục đích**: Kê đơn thuốc nội trú thường quy lĩnh theo ngày (thuốc viên uống, kháng sinh ống dồn liều, dịch truyền, thuốc hướng thần), sản phẩm dinh dưỡng điều trị kho 753, tra cứu danh mục Dược viện.
* **Tham số**:
  - `mode`: `"single"` (1 thuốc lĩnh), `"multi"` (toa nhiều thuốc lĩnh), `"nutrition"` (dinh dưỡng kho 753), `"search"` (tra cứu danh mục Dược).
  - `patientCode`: Mã bệnh nhân hoặc mã điều trị.
  - `stockId`: Mã kho cấp phát (Mặc định `4210` - Kho thuốc viên; `4209` - Kho thuốc ống; `753` - Kho SP dinh dưỡng; `4854` - Kho dược chính Ninh Bình).
  - `medicineName`: Tên/mã thuốc hoặc từ khóa tra cứu.
  - `amount`: Số lượng thuốc lĩnh.
  - `tutorial`: Hướng dẫn sử dụng.
  - `items`: Mảng chuỗi thuốc khi `mode='multi'` (`["Thuoc|SL|HDSD|[KhoId]|[DuongDungId]|[Cu Sang:Trua:Chieu:Toi]"]`).
  - `useFormId`: ID đường dùng (1: Uống, 15: Tiêm, 20: Truyền TM, 25: Dùng ngoài, 32: Dinh dưỡng).
  - `doses`: Cữ dùng `"Sang:Trua:Chieu:Toi"` (VD: `"01::01"` = Sáng 1, Chiều 1).

* **Ví dụ gọi**:
  ```json
  // Kê 2 viên Augmentin 1g lĩnh kho viên 4210:
  {"mode": "single", "patientCode": "0001666593", "medicineName": "Augmentin 1g", "amount": 2, "stockId": 4210, "tutorial": "Uong 1 vien dau bua an sang va chieu", "doses": "01::01"}

  // Kê 6 chai Leanpro PreSur lĩnh từ Kho Dinh dưỡng Viện 753:
  {"mode": "nutrition", "patientCode": "0001666593", "medicineName": "Leanpro PreSur", "amount": 6, "stockId": 753, "tutorial": "Uong theo chi dinh chuyen khoa"}

  // Tra cứu thuốc trong Kho Dược viện:
  {"mode": "search", "medicineName": "Paracetamol"}
  ```

### 1.3. Router Điều Phối Tự Động: `his_prescribe_medicine`
* Công cụ fallback / router tương thích ngược: Nhận `stockId`, tự động điều phối:
  - Nếu `stockId` là `810`, `7787`, `5142`, `5141` $\rightarrow$ Tự động chuyển sang `ExecutePrescribeCabinet`.
  - Nếu `stockId` là `4210`, `4209`, `753`, `4854` $\rightarrow$ Tự động chuyển sang `ExecutePrescribeWarehouse`.

---

## 2. NGUYÊN TẮC BẮT BUỘC VỀ CÁCH DÙNG THUỐC (CLINICAL TUTORIAL STANDARDS)

*Khi sinh dữ liệu cho `InPatientPresSDO` hoặc kê đơn qua MCP/CLI, bắt buộc phải áp dụng chính xác từ điển cách dùng lâm sàng sau:*

### 2.1. Kháng sinh Tiêm truyền (Dồn liều & Dung môi chuẩn)
1. **Rocephin 1g (Ceftriaxone 1g)**:
   - **Số lượng**: 2 lọ/ngày.
   - **Chia liều**: **Dồn 1 lần duy nhất buổi sáng** (`Morning = "02"`, `Afternoon = null`, `Evening = null`).
   - **Cách dùng (`Tutorial`)**: `"Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 9h sáng"`
   - **Dung môi kèm theo**: `Sodium Chloride 0.9% 100ml` (1 chai) -> Tutorial: `"Dung môi pha Rocephin truyền TM sáng 9h"`.
2. **Zinacef 750mg (Cefuroxim 750mg)**:
   - **Số lượng**: 2 lọ/ngày.
   - **Chia liều**: **Dồn 1 lần duy nhất buổi sáng** (`Morning = "02"`, `Afternoon = null`, `Evening = null`).
   - **Cách dùng (`Tutorial`)**: `"Pha 02 lọ với 02 ống Nước cất tiêm 10ml (20ml), tiêm/truyền TM chậm lúc 9h sáng"`
   - **Dung môi kèm theo**: `Nước cất tiêm 10ml` (2 ống) -> Tutorial: `"Dung môi pha Zinacef tiêm TM sáng 9h"`.
3. **Medivernol 1g (Cefoperazon/Sulbactam)**:
   - **Số lượng**: 2 lọ/ngày.
   - **Chia liều**: **Dồn 1 lần duy nhất buổi sáng** (`Morning = "02"`, `Afternoon = null`, `Evening = null`).
   - **Cách dùng (`Tutorial`)**: `"Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30-40 giọt/phút lúc 9h sáng"`
   - **Dung môi kèm theo**: `Sodium Chloride 0.9% 100ml` (1 chai) -> Tutorial: `"Dung môi pha Medivernol truyền TM sáng 9h"`.
4. **Unasyn (1g + 0.5g) (Ampicillin/Sulbactam)**:
   - **Số lượng**: 2 lọ/ngày.
   - **Chia liều**: Sáng 1, Chiều 1 (`Morning = "01"`, `Afternoon = "01"`).
   - **Cách dùng (`Tutorial`)**: `"Pha mỗi lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 9h - 17h"`
   - **Dung môi kèm theo**: `Sodium Chloride 0.9% 100ml` (2 chai) -> Tutorial: `"Dung môi pha Unasyn truyền TM lúc 9h - 17h"`.
5. **Voxin 500mg (Vancomycin 500mg)**:
   - **Số lượng**: 3 lọ/ngày (Tổng liều 1.5g).
   - **Chia liều**: Sáng 1.5 lọ, Tối 1.5 lọ (`Morning = "1.5"`, `Evening = "1.5"`).
   - **Cách dùng (`Tutorial`)**: `"Pha mỗi lần 1.5 lọ (750mg) với 250ml NaCl 0.9%, truyền TM chậm 40 giọt/phút trong ít nhất 60 phút lúc 9h - 21h"`
   - **Dung môi kèm theo**: `Sodium Chloride Injection 250ml` (2 chai) -> Tutorial: `"Dung môi pha Voxin truyền TM lúc 9h - 21h"`.

### 2.2. Thuốc Giảm đau / Hạ sốt / Kháng viêm
1. **Paracetamol Kabi AD 1g/100ml (Truyền TM)**:
   - **Số lượng**: 2 chai/ngày.
   - **Chia liều**: Sáng 1, Chiều 1 (`Morning = "01"`, `Afternoon = "01"` hoặc `Evening = "01"`).
   - **Cách dùng (`Tutorial`)**: `"Truyền TM 30-40 giọt/phút lúc 10h - 18h khi đau/sốt"`
2. **Augmentin 1g (Uống)**:
   - **Số lượng**: 2 viên/ngày (`Morning = "01"`, `Afternoon = "01"`).
   - **Cách dùng (`Tutorial`)**: `"Uống 1 viên ngay đầu bữa ăn sáng (8h) và chiều (18h)"`
3. **Tramadol/Paracetamol Normon 37.5mg/325mg (Uống)**:
   - **Số lượng**: 2 viên/ngày (`Morning = "01"`, `Afternoon = "01"`).
   - **Cách dùng (`Tutorial`)**: `"Uống 1 viên sau ăn sáng (9h) và chiều (18h) khi đau"`
4. **Celebrex 200mg / Arcoxia 60mg (NSAID Uống)**:
   - **Cách dùng (`Tutorial`)**: `"Uống 1 viên sau ăn no lúc 9h - 18h"`

### 2.3. Thuốc Chống đông & An thần
1. **Gemapaxane 4000IU/0.4ml / Heparine 25.000IU**:
   - **Chia liều**: Tối 1 (`Evening = "01"` hoặc `Evening = "1"`).
   - **Cách dùng (`Tutorial`)**: `"Tiêm dưới da thành bụng 1 bơm lúc 20h"`
2. **Seduxen 5mg (Diazepam)**:
   - **Chia liều**: Tối 1 (`Evening = "01"` hoặc `Evening = "1"`).
   - **Cách dùng (`Tutorial`)**: `"Uống 1 viên lúc 21h trước khi đi ngủ"`

---

## 3. BẢN ĐỒ KHO & TỦ TRỰC TOÀN HỆ THỐNG

### 3.1. Kho Dược Viện (Lĩnh định kỳ theo ngày - `his_prescribe_warehouse`):
- `4209`: **Kho thuốc ống** (Kháng sinh tiêm, dịch pha, giảm đau truyền, chống đông: Ceftriaxone, Zinacef, Unasyn, Medivernol, Voxin, Paracetamol Kabi, Nước cất, Heparine, Gemapaxane).
- `4210`: **Kho thuốc viên** (Thuốc uống: Augmentin, Tramadol/Para, Celebrex, Arcoxia, Tolperison).
- `753`: **Kho SP Dinh dưỡng Viện** (Leanpro PreSur, sữa non, sản phẩm dinh dưỡng điều trị).
- `804`: **Kho Dịch truyền 2024** (Natri Clorid 0.9% 100ml, 250ml, 500ml).
- `4208`: **Kho Hướng thần** (Seduxen 5mg).
- `4854`: **Kho Dược chính Cơ sở 2 - Ninh Bình**.

### 3.2. Tủ Trực Khoa / Buồng Bệnh (Kê dùng ngay - `his_prescribe_cabinet`):
- `810`: **Tủ trực Khoa CTCH & Cột sống - Khoa 57** (`TT_KCTCHCS` - Hà Nội).
- `7787`: **Tủ trực Sản phẩm Dinh dưỡng** (`TTSPDD_9` - Dinh dưỡng trước mổ Leanpro).
- `5142`: **Tủ trực Khu 3E - Khoa Ngoại tổng hợp** (`TTT_NBKP05.02` - Ninh Bình).
- `5141`: **Tủ trực Khu 3D - Khoa Ngoại tổng hợp** (`TTT_NBKP05.01` - Ninh Bình).

---

## 4. QUY TẮC AN TOÀN BẢO HIỂM & LÂM SÀNG (BẮT BUỘC GHI NHỚ)

### 4.1. Quy tắc Lùi +5 Phút Sau Tờ Điều Trị (5-Minute Timing Offset Rule):
- Kê đơn thuốc BẮT BUỘC thực hiện **SAU KHI ĐÃ CÓ TỜ ĐIỀU TRỊ**.
- Thời gian y lệnh thuốc (`InstructionTime`) tự động **lùi +5 phút sau thời điểm Tờ điều trị** (`InstructionTime = TrackingTime + 5 phút`).
- Ví dụ: Tờ điều trị lúc 17:00 $\rightarrow$ Y lệnh thuốc lúc 17:05. Chống triệt để việc đơn thuốc nhảy ngược vào tờ điều trị buổi sáng (10h).

### 4.2. Quy tắc Chẩn đoán Bệnh nền (Tăng huyết áp, Đái tháo đường):
- Trước khi kê thuốc điều trị bệnh nền (Amlodipin, Metformin...):
  - Phải kiểm tra xem hồ sơ điều trị đã có **Mã ICD chẩn đoán chính thức** (`ICD_CODE`, `ICD_SUB_CODE`, `ICD_TEXT`) hay chưa.
  - Nếu chỉ mới có trong tiền sử mà chưa được gán mã ICD chính thức, **phải chủ động đề xuất Bác sĩ bổ sung chẩn đoán bệnh nền** trước khi kê đơn.

### 4.3. Quy tắc Kê thuốc nhóm PPI (Nexium, Pantoloc, Esomeprazole...):
- BHYT yêu cầu chỉ định PPI phải có chẩn đoán kèm theo là **Viêm/loét dạ dày tá tràng (`K29`, `K25`, `K27`...)** hoặc **Trào ngược dạ dày thực quản (`K21`)**.
- Nếu hồ sơ bệnh nhân chưa có mã chẩn đoán dạ dày, **tuyệt đối không tự ý kê PPI** mà phải đề xuất Bác sĩ bổ sung mã chẩn đoán phụ.

### 4.4. Quy Chuẩn Kê Vật Tư Thay Băng (`mode = 'dressing'`):
- **1 lọ Povidone 10% 125ml** (`TH.POVI008` - ID `17385`): Số lượng `1`, đường dùng *Dùng ngoài* (`25`), cách dùng `"thay băng"`, **BẮT BUỘC bật cờ Hao phí (`IsExpend = true`)**.
- **1 chai Muối rửa Natri clorid 0.9% 500ml** (`TH.NATR047` - ID `27127`): Số lượng `1`, đường dùng *Dùng ngoài* (`25`), cách dùng `"thay băng"`, **BẮT BUỘC bật cờ Hao phí (`IsExpend = true`)**.
- Gắn vào `TrackingId` ngày tương ứng, ký số tờ điều trị.
- Lệnh gọi nhanh:
  * MCP: `his_prescribe_cabinet(mode="dressing", patientCode="...", stockId=810)`
  * CLI: `.\HisCabinetPrescribe.bat dressing <MãBN> 1 1 810`

---

## 5. CÔNG CỤ THỰC THI (CLI FALLBACK)

Khi không thể gọi qua MCP Server, Agent dùng các lệnh CLI biên dịch sẵn (tuyệt đối không tạo file mới):

```powershell
# Nạp biến môi trường
. .\set_env.ps1

# --- HỆ 1: KÊ TỦ TRỰC (IS_CABINET = 1) ---
.\HisCabinetPrescribe.bat single <MãBN> "Rocephin" 2 810 "Pha 2 lo truyen TM sang"
.\HisCabinetPrescribe.bat insulin <MãBN> 8 R "17:00" 810
.\HisCabinetPrescribe.bat leanpro <MãBN> 6
.\HisCabinetPrescribe.bat dressing <MãBN> 1 1 810
.\HisCabinetPrescribe.bat stock 810 "Insulin"

# --- HỆ 2: KÊ LĨNH KHO DƯỢC (IS_CABINET = 0) ---
.\HisWarehousePrescribe.bat single <MãBN> "Augmentin 1g" 2 4210 "Uong 1 vien sang 1 vien chieu" 1 "01::01"
.\HisWarehousePrescribe.bat nutrition <MãBN> "Leanpro PreSur" 6 753 "Uong theo chi dinh chuyen khoa"
.\HisWarehousePrescribe.bat search "Paracetamol"
```
