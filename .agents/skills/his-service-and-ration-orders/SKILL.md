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

---

## 🔬 TỪ ĐIỂN CẬN LÂM SÀNG & ĐỊA CHỈ PHÒNG THỰC HIỆN CHUẨN KHOA 57 (GROUND TRUTH)

Khi chỉ định Cận lâm sàng cho Khoa Chấn thương Chỉnh hình & Cột sống, hệ thống backend tự động gom nhóm theo Phòng thực hiện. Bắt buộc sử dụng đúng mã dịch vụ và phòng tiếp nhận sau:

| STT | Phân Loại / Phòng Thực Hiện | Tên Kỹ Thuật Chuẩn | Mã DV (`SERVICE_CODE`) | Service ID | Đối Tượng |
|:---:|:---|:---|:---:|:---:|:---:|
| **1** | **XN Huyết Học Tế Bào** (`1772`) | Tổng phân tích tế bào máu ngoại vi (máy laser) | `BM00110` | `5745` | BHYT (`1`) |
| | *(Phòng XN Huyết Học Tế Bào)* | Máu lắng (bằng máy tự động) (ESR) | `BM00138` | `5686` | BHYT (`1`) |
| **2** | **XN Đông Máu** (`626`) | Thời gian Prothrombin (PT / TQ) bằng máy tự động | `BM00531` | `5713` | BHYT (`1`) |
| | *(Phòng Xét Nghiệm Đông Máu)* | Thời gian APTT (TCK) bằng máy tự động | `BM260527.52` | `63622` | BHYT (`1`) |
| | | Định lượng Fibrinogen bằng máy tự động | `BM00542` | `5716` | BHYT (`1`) |
| **3** | **XN Truyền Máu** (`1464`) | Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động) | `BM01700` | `5783` | BHYT (`1`) |
| **4** | **XN Sinh Hóa** (`410`) | Định lượng CRP (C-Reactive Protein) | `BM02180` | `5995` | BHYT (`1`) |
| | *(Phòng Xét Nghiệm Sinh Hóa)* | Định lượng Urê [Máu] | `BM02304` | `5923` | BHYT (`1`) |
| | | Định lượng Creatinin (máu) *(bắt buộc trước tiêm CQ)* | `BM01361` | `5934` | BHYT (`1`) |
| | | Đo hoạt độ AST (GOT) | `BM01352` | `5834` | BHYT (`1`) |
| | | Đo hoạt độ ALT (GPT) | `BM01347` | `5833` | BHYT (`1`) |
| | | Định lượng Glucose | `BM10249` | `5864` | BHYT (`1`) |
| | | Điện giải đồ (Na, K, Cl) | `BM00132` | `5853` | BHYT (`1`) |
| **5** | **XN Virus Miễn Dịch** (`871`) | HIV Ag/Ab miễn dịch tự động | `BM00871` | `6020` | BHYT (`1`) |
| | *(Phòng XN Virus Miễn Dịch - Vi Sinh)*| HBsAg miễn dịch tự động | `BM00859` | `6135` | BHYT (`1`) |
| | | HCV Ag/Ab miễn dịch tự động | `BM26341` | `34801` | BHYT (`1`) |
| **6** | **XN Nước Tiểu** (`566`) | Tổng phân tích nước tiểu (Bằng máy tự động) | `BM02998` | `5950` | BHYT (`1`) |
| **7** | **XN Lao - TB-IGRA** (`9645`)| Mycobacterium tuberculosis Quantiferon *(gửi BV Phổi TW)* | `BM26362` | `36522` | Yêu Cầu (`43`) |
| **8** | **XN Vi Sinh & Cấy Máu** (`4374`)| Vi khuẩn nuôi cấy và định danh hệ thống tự động | `BM01691` | `6054` | BHYT (`1`) |
| | *(Phòng XN Vi Khuẩn - Vi Nấm)*| ⚠️ **Đính Kèm KSD** *(Bắt buộc kèm khi cấy máu)* | `BMDK01` | `38374` | Dịch Vụ (`42`) |
| **9** | **CLVT Nội Trú** (`17549`) | Chụp CLVT phổi HRCT [đến 32 dãy không thuốc CQ] | `BM00338.260119` | `58181` | BHYT (`1`) |
| | *(Phòng tiếp đón CLVT Nội trú)*| Chụp CLVT cột sống thắt lưng không tiêm thuốc CQ | `BM00400.260119` | `58191` | BHYT (`1`) |
| **10**| **Cộng Hưởng Từ (MRI)** (`17548`)| Chụp cộng hưởng từ cột sống thắt lưng – cùng [Không in phim] | `BM00482.260119` | `58292` | BHYT (`1`) |
| **11**| **Thăm Dò Chức Năng (TDCN)** | Điện tim thường *(Thực hiện tại: P.Tiểu phẫu Nhà Q - Khoa 57)* | `BM04258` | `920` | BHYT (`1`) |
| | | Đo mật độ xương DEXA [1 vị trí] *(P202 - Nhà K2 - Room 6462)* | `BM08084` | `160` | BHYT (`1`) |
| | | Siêu âm Doppler tim, van tim *(P112 T1 Nhà K2 - Room 16987)* | `BM00201` | `5569` | BHYT (`1`) |

---

## ⚡ QUY TRÌNH CHỈ ĐỊNH CLS TRỰC TIẾP BYPASS UI & GOM ỐNG 1-BARCODE

* **Endpoint**: `POST http://192.168.7.236:1608/api/HisServiceReq/AssignServiceByInstructionTimes`
* **Cơ chế gom ống**: Gửi toàn bộ các kỹ thuật trong mảng `ServiceReqDetails`. MOS Backend tự động gom các dịch vụ cùng `RoomId` (Phòng thực hiện) thành **1 `HIS_SERVICE_REQ` duy nhất (1 Barcode / 1 Ống máu)**, tuyệt đối không gửi vòng lặp lẻ từng dịch vụ.
* **Quy tắc an toàn**: `RequestRoomId` phải lấy từ `BED_ROOM.ROOM_ID` nơi bệnh nhân nằm điều trị, và đã được kích hoạt qua `POST api/Token/UpdateWorkInfo`.


