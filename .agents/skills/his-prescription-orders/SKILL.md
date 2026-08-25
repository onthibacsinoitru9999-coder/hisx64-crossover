---
name: his-prescription-orders
description: >-
  Tự động kê đơn thuốc điều trị nội trú, đơn thuốc ra viện và sao chép y lệnh
  trên hệ thống HIS/MOS Bệnh viện Bạch Mai qua API chuẩn xác 100%.
  Bao gồm từ điển cách dùng chuẩn hóa lâm sàng (Ceftriaxone, Zinacef, Medivernol dồn sáng,
  pha dung môi, chia liều, tốc độ truyền), chống trùng lặp, và không bao giờ để bác sĩ phải sửa tay.
---

# HIS Prescription Orders & InPatient Medication Management

Skill này cung cấp quy trình và mã nguồn chuẩn hóa để **Tạo đơn thuốc điều trị nội trú** và **Sao chép đơn thuốc qua các ngày** trực tiếp lên máy chủ MOS Backend (`http://192.168.7.236:1608/`) Bệnh viện Bạch Mai với **ĐỘ CHÍNH XÁC TUYỆT ĐỐI 100% VỀ CẢ SỐ LƯỢNG VÀ CÁCH DÙNG NGAY TỪ LẦN GỌI ĐẦU TIÊN**, tuyệt đối không để bác sĩ phải thao tác chỉnh sửa tay trên giao diện.

---

## 1. NGUYÊN TẮC BẮT BUỘC VỀ CÁCH DÙNG THUỐC (CLINICAL TUTORIAL STANDARDS)

*Khi sinh dữ liệu cho `InPatientPresSDO`, bắt buộc phải áp dụng chính xác từ điển cách dùng lâm sàng sau:*

### 1.1. Kháng sinh Tiêm truyền (Dồn liều & Dung môi chuẩn)
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

### 1.2. Thuốc Giảm đau / Hạ sốt / Kháng viêm
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

### 1.3. Thuốc Chống đông & An thần
1. **Gemapaxane 4000IU/0.4ml / Heparine 25.000IU**:
   - **Chia liều**: Tối 1 (`Evening = "01"` hoặc `Evening = "1"`).
   - **Cách dùng (`Tutorial`)**: `"Tiêm dưới da thành bụng 1 bơm lúc 20h"`
2. **Seduxen 5mg (Diazepam)**:
   - **Chia liều**: Tối 1 (`Evening = "01"` hoặc `Evening = "1"`).
   - **Cách dùng (`Tutorial`)**: `"Uống 1 viên lúc 21h trước khi đi ngủ"`

---

## 2. BẢN ĐỒ KHO XUẤT DƯỢC VIỆN
- `4209`: **Kho thuốc ống** (Kháng sinh tiêm, dịch pha, giảm đau truyền, chống đông: Ceftriaxone, Zinacef, Unasyn, Medivernol, Voxin, Paracetamol Kabi, Nước cất, Heparine, Gemapaxane).
- `4210`: **Kho thuốc viên** (Thuốc uống: Augmentin, Tramadol/Para, Celebrex, Arcoxia, Tolperison).
- `804`: **Kho Dịch truyền 2024** (Natri Clorid 0.9% 100ml, 250ml, 500ml).
- `4208`: **Kho Hướng thần** (Seduxen 5mg).

---

## 3. QUY TRÌNH KÊ ĐƠN TỰ ĐỘNG CHUẨN XÁC 100%
1. **Pre-check**: Luôn kiểm tra `api/HisExpMest/GetView` trước. Nếu ngày đó đã có đơn thì **bỏ qua ngay** (chống trùng đơn).
2. **Điền đầy đủ thông tin chuẩn**: Đảm bảo mọi dòng thuốc trong `Medicines` (`PresMedicineSDO`) đều có:
   - `Tutorial` chi tiết theo đúng từ điển trên.
   - `Morning`, `Noon`, `Afternoon`, `Evening` được phân bổ chính xác.
   - `NumOfDays = 1`.
3. **Kiểm tra phản hồi**: Đánh giá thành công qua `result.ExpMests != null && result.ExpMests.Count > 0`.
