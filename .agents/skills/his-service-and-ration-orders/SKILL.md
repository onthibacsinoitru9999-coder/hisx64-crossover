---
name: his-service-and-ration-orders
description: >-
  Quy trình và công cụ chuẩn hóa chỉ định Suất ăn dinh dưỡng bệnh lý (BT01, DD01, TM01...) và Cận lâm sàng 
  trên hệ thống HIS/MOS Bệnh viện Bạch Mai qua API. Ưu tiên độ chính xác tuyệt đối, chuẩn hóa tham số, 
  thực thi theo từng ngày độc lập và tự động đối soát 100% dữ liệu.
---

# HIS Paraclinical Tests & Diet / Ration Management (Chuẩn xác & Đối soát)

Skill này cung cấp quy trình chuẩn hóa, bộ thông số kỹ thuật và công cụ tự động để **Chỉ định Suất ăn dinh dưỡng bệnh lý** và **Chỉ định Cận lâm sàng** trực tiếp qua MOS API (`:1608`) với nguyên tắc **Độ chính xác là ưu tiên số 1**.

---

## 🎯 NGUYÊN TẮC CỐT LÕI (CORE PRINCIPLES)

1. **Chính xác tuyệt đối > Tốc độ**: Không được bỏ qua các bước xác thực và đối soát.
2. **Quy tắc 1 Request = 1 Ngày**: Hệ thống backend MOS chỉ ghi nhận **đúng 1 mốc thời gian** trong mảng `InstructionTimes` cho mỗi request. Tuyệt đối không gộp nhiều ngày vào chung một mảng.
3. **Quy trình 3 bước bắt buộc**:
   * **Bước 1 (Pre-check)**: Quét hồ sơ, chẩn đoán ICD, phòng/giường và kiểm tra các suất ăn đã có để tránh kê trùng.
   * **Bước 2 (Execute)**: Gửi y lệnh tách biệt cho từng ngày.
   * **Bước 3 (Post-verify)**: Truy vấn trực tiếp bảng `HIS_SERE_SERV_RATION` để đối soát xác nhận 100% từng bữa đã lưu thành công.

---

## 📋 TỪ ĐIỂN SUẤT ĂN & CHẾ ĐỘ DINH DƯỠNG

* **Nơi tiếp nhận chế biến (Phòng thực hiện)**: `RoomId = 5809` (Trung tâm Dinh dưỡng Lâm sàng).
* **Mã loại đối tượng**: `PatientTypeId = 42` (hoặc lấy từ `TDL_PATIENT_TYPE_ID` của hồ sơ).

### 1. Combo Ngoại khoa / Thông thường (`BT01`)
| Bữa ăn | Thời điểm | RationTimeId | Mã dịch vụ | Service ID | Tên dịch vụ trên HIS |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Sáng** | 06:00 | `1` | `BM17770` | `30073` | BT01-06- Sáng- E: 360-380kcal, P: 14-16g... |
| **Trưa** | 11:00 | `3` | `BM18696` | `30153` | BT01-11- Trưa- E: 880-900kcal, P: 45-50g... |
| **Chiều** | 17:00 | `5` | `BM18697` | `30154` | BT01-17- Chiều- E: 880-900kcal, P: 45-50g... |

### 2. Combo Đái tháo đường (`DD01`)
| Bữa ăn | Thời điểm | RationTimeId | Mã dịch vụ | Service ID | Tên dịch vụ trên HIS |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Sáng** | 06:00 | `1` | `CO02` | `30180` | DD01-06-Sáng-E:350 - 370 kcal, P: 15-17g... |
| **Trưa** | 11:00 | `3` | `CS01` | `30181` | DD01-11-Trưa-E:620 - 640 kcal, P: 33-35g... |
| **Chiều** | 17:00 | `5` | `BM18650` | `30133` | DD01-17- Chiều- E:580 - 600 kcal, P: 32-34g... |

### 3. Combo Tim mạch / Tăng huyết áp (`TM01`)
| Bữa ăn | Thời điểm | RationTimeId | Mã dịch vụ | Service ID | Tên dịch vụ trên HIS |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Sáng** | 06:00 | `1` | `BM18075` | `30117` | TM01-06-Sáng-E: 280-290kcal, P: 13-15g... |
| **Trưa** | 11:00 | `3` | `BM18039` | `30093` | TM01-11-Trưa-E: 645-655kcal, P: 20-32g... |
| **Chiều** | 17:00 | `5` | `BM18040` | `30094` | TM01-17-Chiều-E: 630-640kcal, P: 29-31g... |

---

## 🛠️ QUY TRÌNH THỰC THI CHUẨN XÁC

### 1. Khảo sát & Lấy thông tin điều trị
* **Endpoint lấy hồ sơ**: `POST/GET api/HisTreatment/GetView`
  * *Bộ lọc bắt buộc*: `TREATMENT_CODE__EXACT` hoặc `PATIENT_CODE__EXACT`, kèm `IS_PAUSE = false`.
* **Endpoint lấy vị trí buồng/giường**: `api/HisTreatmentBedRoom/GetView`
  * *Bộ lọc*: `TREATMENT_ID = treatmentId`, `IS_IN_ROOM = true`.
  * *Lấy*: `ROOM_ID` (gán vào `RequestRoomId`), `BED_NAME`, `BED_ROOM_NAME`.
* **Endpoint kiểm tra suất ăn hiện có**: `api/HisSereServRation/GetView`
  * *Bộ lọc*: `TREATMENT_ID = treatmentId`, `IS_INCLUDE_DELETED = false`.
  * Xác định chính xác ngày nào, bữa nào (`RATION_TIME_ID`: 1=Sáng, 3=Trưa, 5=Chiều) đã có để chỉ kê các bữa còn thiếu.

---

### 2. Gửi y lệnh chỉ định suất ăn (`HisServiceReq/RationCreate`)
* **Endpoint**: `http://192.168.7.236:1608/api/HisServiceReq/RationCreate`
* **Cấu trúc DTO `HisRationServiceReqSDO`**:
```json
{
  "TreatmentIds": [ 7092075 ],
  "InstructionTimes": [ 20260824050000 ],
  "RequestRoomId": 5252,
  "RequestLoginName": "034727",
  "RequestUserName": "NGUYỄN HỮU SÂM",
  "IcdCode": "S91.0",
  "IcdName": "Đứt gân Achilles chân trái",
  "IcdSubCode": "M10.00",
  "IcdText": "Gout",
  "HalfInFirstDay": false,
  "IsForAutoCreateRation": false,
  "IsForHomie": false,
  "RationServices": [
    { "ServiceId": 30073, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [1] },
    { "ServiceId": 30153, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [3] },
    { "ServiceId": 30154, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [5] }
  ]
}
```
> ⚠️ **Lưu ý đặc biệt**: Nếu kê cho $N$ ngày, phải thực hiện vòng lặp $N$ lần gọi API, mỗi lần tương ứng với `InstructionTimes = [YYYYMMDD050000]`.

---

### 3. Bắt buộc đối soát 100% dữ liệu (Post-Verification)
Sau khi gửi y lệnh, gọi lại `api/HisSereServRation/GetView` để kiểm tra:
* Kiểm tra `INTRUCTION_TIME` khớp với ngày yêu cầu.
* Kiểm tra đầy đủ 3 bữa `RATION_TIME_ID` (1, 3, 5).
* Lấy mã phiếu chỉ định `SERVICE_REQ_CODE` được hệ thống cấp để hiển thị cho người dùng.

---

## ⚡ CÔNG CỤ THỰC THI SẴN CÓ (`QuickRation.exe`)

Công cụ `QuickRation.exe` đặt tại thư mục gốc của HIS (`e:\his-x64-28-11fix GDYK\his-x64\QuickRation.exe`).

### Cách sử dụng:
```powershell
# Kê suất ăn cho 1 bệnh nhân ngày mai (mặc định combo BT01, 3 bữa)
.\QuickRation.exe -p 0003983111

# Kê suất ăn theo combo bệnh lý cho nhiều ngày
.\QuickRation.exe -p 0003967577 -combo DD01 -date 20260824,20260825,20260826

# Kê suất ăn cho nhiều bệnh nhân cùng lúc
.\QuickRation.exe -p 0003967577,0003288519 -combo DD01 -date 20260825,20260826

# Kê suất ăn cho cả phòng
.\QuickRation.exe -room 712 -combo BT01 -date 20260824,20260825,20260826
```
