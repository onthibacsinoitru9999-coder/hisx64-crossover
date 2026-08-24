# 🏥 CẨM NANG TOÀN DIỆN TÍCH HỢP HIS / MOS / EMR CHO AI AGENT
> **Mục đích**: Tài liệu hóa 100% kinh nghiệm, kiến trúc, cấu trúc DTO, các bẫy runtime (gotchas) và công cụ sẵn có trên hệ thống HIS Bệnh viện Bạch Mai. Một Agent mới ở bất kỳ máy nào chỉ cần đọc file này là có thể thực thi chính xác ngay lập tức mà **không cần thử lỗi hay phân tích ngược lại từ đầu**.

---

## 📑 MỤC LỤC
1. [Kiến Trúc Hệ Thống & Cổng Kết Nối](#1-kiến-trúc-hệ-thống--cổng-kết-nối)
2. [Bộ Thông Số Bác Sĩ & Khoa Phòng Mặc Định](#2-bộ-thông-số-bác-sĩ--khoa-phòng-mặc-định)
3. [Cơ Chế Xác Thực & Bẫy Khóa File Log (Critical Gotcha)](#3-cơ-chế-xác-thực--bẫy-khóa-file-log-critical-gotcha)
4. [Quy Trình Tra Cứu Bệnh Nhân & Buồng Giường](#4-quy-trình-tra-cứu-bệnh-nhân--buồng-giường)
5. [Phân Hệ 1: Tờ Điều Trị (Treatment Tracking)](#5-phân-hệ-1-tờ-điều-trị-treatment-tracking)
6. [Phân Hệ 2: Chỉ Định Suất Ăn Dinh Dưỡng Bệnh Lý](#6-phân-hệ-2-chỉ-định-suất-ăn-dinh-dưỡng-bệnh-lý)
7. [Phân Hệ 3: Kê Đơn Thuốc Nội Trú & Ra Viện](#7-phân-hệ-3-kê-đơn-thuốc-nội-trú--ra-viện)
8. [Phân Hệ 4: Ký Số Điện Tử & Mời Bác Sĩ Ký (EMR Sign)](#8-phân-hệ-4-ký-số-điện-tử--mời-bác-sĩ-ký-emr-sign)
9. [Bảng Tổng Hợp Sai Lầm & Bài Học Xương Máu](#9-bảng-tổng-hợp-sai-lầm--bài-học-xương-máu)
10. [Hướng Dẫn Biên Dịch & Chạy Công Cụ CLI Siêu Tốc](#10-hướng-dẫn-biên-dịch--chạy-công-cụ-cli-siêu-tốc)

---

## 1. KIẾN TRÚC HỆ THỐNG & CỔNG KẾT NỐI

Mọi giao tiếp đều đi qua mạng nội bộ bệnh viện (hoặc qua VPN/Tunnel):

| Dịch vụ | Địa chỉ IP & Cổng | Mục đích | Phương thức gọi |
| :--- | :--- | :--- | :--- |
| **MOS Backend** | `http://192.168.7.236:1608/` | Nghiệp vụ HIS chính (Tờ điều trị, Suất ăn, Thuốc, Dịch vụ) | REST API (JSON Body hoặc Base64 param) |
| **ACS Auth** | `http://192.168.7.200:1401/` | Xác thực đăng nhập, cấp/làm mới Token | `api/Token/Login`, `api/Token/Renew` |
| **SDA Data** | `http://192.168.7.200:1410/` | Danh mục hệ thống, cấu hình phân quyền | SDA Service APIs |
| **EMR Service** | `http://192.168.7.239:1415/` | Bệnh án điện tử, tạo văn bản, ký số, mời ký | EMR Document APIs |

---

## 2. BỘ THÔNG SỐ BÁC SĨ & KHOA PHÒNG MẶC ĐỊNH

Khi gửi request, sử dụng thông tin định danh của phiên làm việc bác sĩ:

* **Bác sĩ thực hiện**: `034727` - **Ths.BS NGUYỄN HỮU SÂM**
* **Khoa lâm sàng**: Khoa Chấn thương Chỉnh hình & Cột sống (`DEPARTMENT_ID = 57`, Mã Khoa: `9`)
* **Các buồng bệnh phụ trách**: Phòng 712, 714, 716, 724, 725 (Room ID tương ứng, ví dụ P724 có `BED_ROOM_ID = 780`, `ROOM_ID = 5257`)
* **IP Client gửi request**: `100.93.206.93`

---

## 3. CƠ CHẾ XÁC THỰC & BẪY KHÓA FILE LOG (CRITICAL GOTCHA)

### 3.1. Cách lấy Live Token từ phiên HIS đang mở
HIS Client ghi log chứa `TokenCode` vào file log cục bộ: `E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt`.

### ⚠️ Bẫy nghiêm trọng (File Lock Gotcha):
Ứng dụng HIS trên máy người dùng liên tục ghi vào `LogSystem.txt`. Nếu dùng `File.ReadAllLines`, `File.ReadAllText` hoặc PowerShell `Get-Content` không đúng cách, Windows sẽ trả về lỗi:
> `System.IO.IOException: The process cannot access the file because it is being used by another process.`

### ✅ Cách xử lý chuẩn (Bắt buộc dùng `FileShare.ReadWrite`):
```csharp
string logPath = @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
string token = "";
using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
using (var sr = new StreamReader(fs))
{
    string text = sr.ReadToEnd();
    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    for (int i = lines.Length - 1; i >= 0; i--)
    {
        if (lines[i].Contains("TokenCode|"))
        {
            int idx = lines[i].IndexOf("TokenCode|") + 10;
            if (lines[i].Length >= idx + 64)
            {
                token = lines[i].Substring(idx, 64);
                break;
            }
        }
    }
}
```

### 3.2. Headers bắt buộc khi gọi REST API trực tiếp
```http
TokenCode: <token_64_ký_tự>
Authorization: Bearer <token_64_ký_tự>
ClientIpAddress: 100.93.206.93
Content-Type: application/json; charset=utf-8
```

### 3.3. Gán phòng làm việc cho phiên (Bắt buộc để tránh lỗi `KhongCoThongTinPhongLamViec`)
Khi tạo phiên đăng nhập mới qua mã độc lập (CLI / Automation), MOS yêu cầu Token phải được gán danh sách phòng làm việc (`WorkInfoSDO`) trên máy chủ trước khi gọi các API nghiệp vụ lâm sàng (Tạo tờ điều trị, kê đơn, chỉ định CLS):
```csharp
var workInfo = new WorkInfoSDO
{
    Rooms = new List<RoomSDO>
    {
        new RoomSDO { RoomId = 5248 }, // Phòng 734 (Phòng trực/khám CTCH)
        new RoomSDO { RoomId = 5252 }, // Phòng 712 (Buồng bệnh)
        new RoomSDO { RoomId = 5251 }  // Phòng 714 (Buồng bệnh)
    }
};
var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkPlaceSDO = workPlaces;
HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkInfoSDO = workInfo;
```
*(Nếu thiếu bước này, MOS sẽ trả lời: `{"Success":false,"Param":{"MessageCodes":["KhongCoThongTinPhongLamViec"]}}`)*

---

## 4. QUY TRÌNH TRA CỨU BỆNH NHÂN & BUỒNG GIƯỜNG

Mỗi bệnh nhân có 2 mã quan trọng:
* **Mã bệnh nhân (Patient Code)**: Thường là 10 ký tự (VD: `0003969449`).
* **Mã hồ sơ bệnh án (Treatment Code)**: Thường là 12 ký tự (VD: `000007060449`).
* **Treatment ID (Khóa chính nội bộ)**: Số nguyên `Int64` (VD: `7060265`).

### 4.1. Tìm hồ sơ đang điều trị (`api/HisTreatment/Get`)
Gửi filter `MOS.Filter.HisTreatmentFilter`:
* Thử tìm theo `PATIENT_CODE__EXACT`. Nếu không có, tìm theo `TREATMENT_CODE__EXACT`.
* Lấy bản ghi cuối cùng có `IS_PAUSE == 0` (hoặc `IS_PAUSE == null`).
* Trích xuất: `ID` (Treatment ID), `ICD_CODE`, `ICD_NAME`, `ICD_SUB_CODE`, `ICD_TEXT`, `LAST_DEPARTMENT_ID`.

### 4.2. Lấy vị trí buồng / giường hiện tại (`api/HisTreatmentBedRoom/Get`)
Gửi filter `MOS.Filter.HisTreatmentBedRoomFilter` với `TREATMENT_IDs = [treatmentId]`:
* Lọc bản ghi có `REMOVE_TIME == null || REMOVE_TIME == 0`.
* Lấy `BED_ROOM_ID` (ID của phòng giường).
* Tra cứu `HisBedRoom` (`api/HisBedRoom/Get`) theo `BED_ROOM_ID` để lấy `ROOM_ID` thực tế gán vào `WorkingRoomId`.

---

## 5. PHÂN HỆ 1: TỜ ĐIỀU TRỊ (TREATMENT TRACKING) & DẤU HIỆU SINH TỒN

### 5.1. Kiến Trúc & Cặp Endpoint Backend MOS
Tờ điều trị trong HIS/EMR là trái tim của hồ sơ bệnh án nội trú, bao gồm 2 thành phần dữ liệu song hành:
1. **Tờ điều trị (Diễn biến & Y lệnh)**: `POST http://192.168.7.236:1608/api/HisTracking/Create`
   - DTO yêu cầu: **`MOS.SDO.HisTrackingSDO`**
2. **Dấu hiệu sinh tồn (DHST)**: `POST http://192.168.7.236:1608/api/HisDhst/Create`
   - DTO yêu cầu: **`MOS.EFMODEL.DataModels.HIS_DHST`** hoặc đóng gói qua SDO

---

### 5.2. Cấu Trúc Chi Tiết DTO `HIS_TRACKING` & Các Trường Bắt Buộc

| Tên Thuộc Tính | Kiểu Dữ Liệu | Ý Nghĩa Lâm Sàng & Quy Tắc Điền |
| :--- | :--- | :--- |
| **`TREATMENT_ID`** | `long` | ID khóa chính của đợt điều trị (Lấy từ `HisTreatment.ID`). |
| **`TRACKING_TIME`** | `long` | Thời gian tạo y lệnh theo định dạng số 14 chữ số **`yyyyMMddHHmmss`** (VD: `20260824080000` = 08:00:00 ngày 24/08/2026). |
| **`DEPARTMENT_ID`** | `long` | Khoa lâm sàng quản lý (Khoa CTCH & Cột sống = **`57`**). |
| **`ROOM_ID`** | `long` | ID phòng làm việc/buồng bệnh (Lấy từ `HisBedRoom.ROOM_ID` hoặc phòng trực **`5248`**). |
| **`ICD_CODE`** | `string` | Mã ICD-10 bệnh chính (VD: `M47.00†`, `S62.30`, `M51.2`, `M87.85`). |
| **`ICD_NAME`** | `string` | Tên chẩn đoán bệnh chính tương ứng với mã ICD. |
| **`ICD_SUB_CODE`** | `string` | Mã ICD-10 bệnh kèm theo / biến chứng (VD: `E11.9; I10`). |
| **`ICD_TEXT`** | `string` | Chẩn đoán chi tiết bằng văn bản tự do (VD: `Trượt L3 độ I / ĐTĐ type 2 - THA`). |
| **`CONTENT`** | `string` | **Diễn biến bệnh lý**: Ghi nhận toàn trạng, tri giác (Glasgow), triệu chứng cơ năng, khám chuyên khoa, tình trạng vết mổ, dẫn lưu, mạch ngọn chi. |
| **`MEDICAL_INSTRUCTION`**| `string` | **Y lệnh bác sĩ**: Tên thuốc, dịch truyền, thủ thuật, cận lâm sàng, chuẩn bị phẫu thuật, chuyển khoa, ra viện. *(⚠️ Tuyệt đối không nhầm thành `TREATMENT_INSTRUCTION`)* |
| **`CARE_INSTRUCTION`** | `string` | **Chăm sóc điều dưỡng**: Phân cấp chăm sóc (`CSCI`, `CSCII`, `CSCIII`), chế độ ăn (`BT01`, `DD01`, `TM01`), theo dõi DHST, thay băng, đếm đường máu mao mạch. |

---

### 5.3. Cấu Trúc DTO Dấu Hiệu Sinh Tồn (`HIS_DHST`)
Để các chỉ số sinh tồn hiển thị tương ứng trên bảng theo dõi của tờ điều trị, `EXECUTE_TIME` của DHST **phải khớp hoặc đồng bộ mốc giờ** với `TRACKING_TIME`:

```csharp
var dhst = new HIS_DHST();
dhst.TREATMENT_ID = treatmentId;
dhst.EXECUTE_TIME = trackingTime; // Cùng mốc yyyyMMddHHmmss
dhst.PULSE = 80;                 // Mạch (lần/phút)
dhst.TEMPERATURE = 36.8m;        // Nhiệt độ (°C)
dhst.BLOOD_PRESSURE_MAX = 120;   // Huyết áp tâm thu (mmHg)
dhst.BLOOD_PRESSURE_MIN = 80;    // Huyết áp tâm trương (mmHg)
dhst.BREATH_RATE = 18;           // Nhịp thở (lần/phút)
dhst.SPO2 = 0.98m;               // SpO2 (0.98 hoặc 98%)
dhst.WEIGHT = 60.0m;             // Cân nặng (kg)
```

---

### 5.4. ⚠️ Bẫy Nghiêm Trọng Về Cấu Trúc & Quy Trình Tạo (Critical Gotchas)

1. **Bẫy lớp bao bọc (SDO Wrapper)**:
   - Gửi trực tiếp `HIS_TRACKING` dạng phẳng sẽ bị Backend từ chối (`Success: false`).
   - Bắt buộc phải đóng gói trong `MOS.SDO.HisTrackingSDO`:
     ```csharp
     var sdo = new HisTrackingSDO();
     sdo.Tracking = tracking;
     sdo.WorkingRoomId = workingRoomId; // RoomId thực tế của buồng bệnh
     ```
2. **Bẫy liên kết Y lệnh thuốc & Cận lâm sàng (Instruction Linking)**:
   - Sau khi tạo thành công `HisTracking`, lấy `tracking.ID` trả về để truyền vào `InPatientPresSDO.TrackingId` hoặc `AssignServiceSDO.TrackingInfos`.
   - Nếu không truyền `TrackingId`, y lệnh thuốc / CLS sẽ bị tách rời, không hiển thị trong cột Y lệnh của Tờ điều trị trên EMR!
3. **Bẫy định dạng số 14 chữ số**:
   - `TRACKING_TIME` phải đủ 14 ký tự số (`yyyyMMddHHmmss`). Ví dụ: 8h sáng phải là `20260824080000` (không được ghi thiếu giây `202608240800`).

---

### 5.5. 5 Mẫu Tờ Điều Trị Chuẩn Lâm Sàng Khoa CTCH & Cột Sống (Khoa 57)

#### Mẫu 1: Tờ điều trị thông thường / Hàng ngày
```text
[CONTENT]:
Bệnh nhân tỉnh táo, tiếp xúc tốt (Glasgow 15đ)
Da niêm mạc hồng, không phù, không sốt
Tim đều, phổi thông khí rõ không rale, bụng mềm
Vết mổ / vị trí tổn thương: Đau ít (VAS 2-3đ), không sưng nóng đỏ
Đầu chi hồng ấm, cảm giác và vận động ngọn chi bình thường
Đại tiểu tiện tự chủ

[MEDICAL_INSTRUCTION]:
Thuốc dùng theo đơn đã kê
Bổ sung dịch truyền dinh dưỡng nếu ăn kém

[CARE_INSTRUCTION]:
Chăm sóc cấp II (CSII)
Chế độ ăn: BT01 (Ăn thường) / DD01 (ĐTĐ) / TM01 (Tim mạch)
Theo dõi mạch, nhiệt độ, huyết áp 2 lần/ngày
```

#### Mẫu 2: Sơ kết 3 - 5 ngày điều trị / Lãnh đạo khoa đi buồng
```text
[CONTENT]:
SƠ KẾT 3 - 5 NGÀY ĐIỀU TRỊ
Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng ổn định
Huyết động ổn định, không sốt
Vết mổ khô sạch, thấm ít dịch băng, đầu chi hồng ấm
Triệu chứng đau thuyên giảm (VAS 3/10)
Cơ lực 2 chân 5/5, không rối loạn cảm giác nông sâu

Ý KIẾN LÃNH ĐẠO KHOA ĐI BUỒNG:
- Thống nhất chẩn đoán và phác đồ điều trị hiện tại
- Thay băng chăm sóc vết thương vô khuẩn hàng ngày
- Tập phục hồi chức năng vận động theo hướng dẫn

[MEDICAL_INSTRUCTION]:
Duy trì phác đồ thuốc hiện tại
Hoàn thiện bilan xét nghiệm kiểm tra nếu có chỉ định

[CARE_INSTRUCTION]:
Chăm sóc cấp II (CSII) - Chế độ ăn theo bệnh lý
Hướng dẫn tập PHCN tại giường
```

#### Mẫu 3: Tờ điều trị tiền phẫu (Chuẩn bị trước mổ)
```text
[CONTENT]:
KHÁM BỆNH NHÂN TRƯỚC MỔ:
Bệnh nhân tỉnh táo, tiếp xúc tốt, tâm lý ổn định
Thể trạng trung bình, không sốt
Tim đều rõ, phổi thông khí tốt, không khó thở
Đã hoàn thiện đầy đủ bilan xét nghiệm tiền phẫu, X-quang, MRI/CT, Siêu âm tim
Đã giải thích rõ tình trạng bệnh, phương pháp phẫu thuật, nguy cơ và tai biến có thể xảy ra trong và sau mổ cho bệnh nhân và gia đình. Bệnh nhân và đại diện gia đình hiểu, đồng ý và đã ký cam kết phẫu thuật.

[MEDICAL_INSTRUCTION]:
Bột / nẹp rạch dọc kiểm tra
Dặn nhịn ăn uống hoàn toàn từ 00h đêm trước mổ
Kháng sinh dự phòng trước mổ 30 phút theo phác đồ
Thêm dịch truyền trước mổ nếu có chỉ định
Chuyển phòng mổ theo lịch

[CARE_INSTRUCTION]:
Chăm sóc cấp II (CSII)
Vệ sinh vùng mổ, thay trang phục mổ
Nhịn ăn uống tuyệt đối trước mổ
```

#### Mẫu 4: Tờ điều trị hậu phẫu (Sau mổ 24 giờ đầu)
```text
[CONTENT]:
BỆNH NHÂN PHẪU THUẬT VỀ KHOA (Bàn giao từ phòng Hồi tỉnh/GMHS):
Bệnh nhân tỉnh táo, tiếp xúc tốt, đã thoát mê / thoát tê hoàn toàn
Da niêm mạc hồng, tự thở êm, SpO2 98-99%
Huyết động ổn định: Mạch 80-85 l/p, HA 120/80 mmHg
Vết mổ nề nhẹ, băng thấm ít dịch máu
Dẫn lưu vết mổ ra ít dịch hồng (< 50ml), hoạt động tốt
Đầu chi hồng ấm, mạch ngoại vi bắt rõ, không tê liệt ngọn chi
Đau vết mổ mức độ vừa (VAS 3-4 điểm)

[MEDICAL_INSTRUCTION]:
Theo dõi sát toàn trạng và huyết động 24h sau mổ
Thuốc giảm đau, kháng sinh, chống phù nề theo biên bản bàn giao gây mê
Rút dẫn lưu sau 24 - 48h khi dịch ra < 30ml/24h

[CARE_INSTRUCTION]:
Chăm sóc cấp I / II (CSCI / CSII)
Kê cao chi mổ / nằm ngửa có gối đỡ tư thế chuẩn
Theo dõi mạch, huyết áp, nhiệt độ, SpO2 mỗi 3 - 6 giờ
Theo dõi màu sắc đầu chi và lượng dịch dẫn lưu
```

#### Mẫu 5: Tổng kết ra viện (Discharge Summary Tracking)
```text
[CONTENT]:
TỔNG KẾT BỆNH ÁN RA VIỆN:
- Chẩn đoán ra viện: [Ghi rõ chẩn đoán bệnh chính + bệnh kèm theo/phụ]
- Phương pháp điều trị / Phẫu thuật: [Ghi rõ tên phẫu thuật / thủ thuật đã thực hiện]
- Quá trình điều trị: Diễn biến thuận lợi, không tai biến sau mổ, vết mổ khô sạch liền sẹo tốt.
- Tình trạng hiện tại: Bệnh nhân tỉnh táo, hết sốt, huyết động ổn định, vết mổ khô sạch (đã cắt chỉ / liền sẹo), đỡ đau nhiều, đi lại và vận động phục hồi tốt, đại tiểu tiện tự chủ.
- Đủ điều kiện xuất viện.

[MEDICAL_INSTRUCTION]:
Cho bệnh nhân ra viện
Kê đơn thuốc điều trị ngoại trú
Hẹn khám lại sau 01 tháng (hoặc 4 tuần) kèm phim chụp kiểm tra
Dặn dò chế độ tập PHCN và dinh dưỡng tại nhà

[CARE_INSTRUCTION]:
Chăm sóc cấp II (CSII)
Hướng dẫn bệnh nhân và gia đình làm thủ tục thanh toán ra viện
```

---

### 5.6. Mã Nguồn C# Chuẩn Tạo Tờ Điều Trị & DHST Hoàn Chỉnh

```csharp
// 1. Khởi tạo và gán dữ liệu Tracking
var tracking = new HIS_TRACKING
{
    TREATMENT_ID = treatmentId,
    TRACKING_TIME = 20260824080000, // yyyyMMddHHmmss
    DEPARTMENT_ID = 57,              // Khoa CTCH & Cột sống
    ROOM_ID = workingRoomId,         // ID phòng bệnh thực tế
    ICD_CODE = icdCode,              // VD: "S62.30"
    ICD_NAME = icdName,
    ICD_SUB_CODE = icdSubCode,
    ICD_TEXT = icdText,
    CONTENT = "Bệnh nhân tỉnh táo, tiếp xúc tốt. Da niêm mạc hồng, không sốt. Vết mổ khô sạch, đầu chi ấm.",
    MEDICAL_INSTRUCTION = "Thuốc theo đơn. Kháng sinh + giảm đau. Thay băng chăm sóc vết mổ.",
    CARE_INSTRUCTION = "Chăm sóc cấp II. Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày."
};

// 2. Đóng gói vào SDO bắt buộc
var trackingSDO = new HisTrackingSDO
{
    Tracking = tracking,
    WorkingRoomId = workingRoomId
};

// 3. Gửi API tạo Tờ điều trị
var consumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
var commonParam = new CommonParam();
var trackingResult = consumer.Post<HisTrackingSDO>("api/HisTracking/Create", commonParam, trackingSDO, new object[0]);

// 4. Tạo đồng thời Dấu hiệu sinh tồn (DHST)
if (trackingResult != null && trackingResult.Tracking != null)
{
    var dhst = new HIS_DHST
    {
        TREATMENT_ID = treatmentId,
        EXECUTE_TIME = tracking.TRACKING_TIME,
        PULSE = 80,
        TEMPERATURE = 36.8m,
        BLOOD_PRESSURE_MAX = 120,
        BLOOD_PRESSURE_MIN = 80,
        BREATH_RATE = 18,
        SPO2 = 0.98m
    };
    consumer.Post<HIS_DHST>("api/HisDhst/Create", commonParam, dhst, new object[0]);
}
```

---

### 5.7. Ứng Dụng Toàn Diện: `HisTrackingCreator.exe` (GUI & CLI Siêu Tốc)
Được đóng gói tại thư mục gốc: `HisTrackingCreator.exe` (kèm file kích hoạt nhanh `Chay_Tao_ToDieuTri.bat`).
* **Tính năng Giao diện WinForms (GUI)**:
  - Tra cứu bệnh nhân tức thì theo Mã bệnh nhân / Mã điều trị (Treatment Code) / Treatment ID.
  - Hiển thị Patient Card trực quan: Tên BN, Giới tính, Tuổi, Đối tượng (BHYT/Viện phí), Buồng - Giường, Chẩn đoán ICD bệnh chính & bệnh kèm theo.
  - Hỗ trợ 1-Click nạp toàn bộ bệnh nhân nội trú Khoa CTCH & Cột sống (Khoa 57), lọc theo buồng bệnh, tìm kiếm nhanh.
  - 6 Mẫu lâm sàng chuẩn (Thông thường, Sơ kết 3-5 ngày, Tiền phẫu, Hậu phẫu 24h, Tổng kết ra viện, Hội chẩn bệnh nặng).
  - Chọn nhanh cấp chăm sóc (`CSCI`, `CSCII`, `CSCIII`), chế độ ăn (`BT01`, `DD01`, `TM01`...), tích chọn y lệnh điều dưỡng tự động tổng hợp text `CARE_INSTRUCTION`.
  - Tích hợp nhập Dấu hiệu sinh tồn (DHST: Mạch, HA Max/Min, Nhiệt độ, Nhịp thở, SpO2, Cân nặng) đồng bộ thời gian.
  - 1-Click Xuất Excel (CSV) và Copy báo cáo kết quả.
* **Tốc độ thực thi**: ~0.20 – 0.35 giây / 1 tờ điều trị.
* **Cú pháp dòng lệnh (CLI)**:
  ```powershell
  # Tạo tờ điều trị thường ngày:
  .\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "Bệnh nhân tỉnh táo, không sốt, vết mổ khô sạch đầu chi ấm." -med "Thuốc theo đơn đã kê" -care "Chăm sóc cấp II (CSII). Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày."
  
  # Tạo theo mẫu lâm sàng chuẩn (Mẫu 1..6):
  .\HisTrackingCreator.exe -p "0003969449,0003298895" -template 1 -time "08:00" -date "2026-08-25"
  
  # Tạo kèm chỉ số sinh tồn (DHST) tùy chỉnh:
  .\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "BN ổn định" -care "CSII, BT01" -pulse 78 -temp 36.6 -bpmax 120 -bpmin 80 -spo2 99
  ```

---

## 6. PHÂN HỆ 2: CHỈ ĐỊNH SUẤT ĂN DINH DƯỠNG BỆNH LÝ

### 6.1. Nguyên tắc cốt lõi: 1 Request = 1 Ngày
* Backend MOS chỉ ghi nhận **đúng 1 mốc thời gian đầu tiên** trong mảng `InstructionTimes`.
* Nếu kê cho $N$ ngày, **bắt buộc lặp $N$ lần gọi API**, mỗi lần truyền `InstructionTimes = [YYYYMMDD050000]`.

### 6.2. Từ điển Suất ăn:
* **Phòng thực hiện tiếp nhận**: `RoomId = 5809` (Trung tâm Dinh dưỡng Lâm sàng).
* **Đối tượng**: `PatientTypeId = 42` (Viện phí) hoặc lấy từ hồ sơ.

| Mã Combo | Bữa Sáng (06:00, RationTimeId: 1) | Bữa Trưa (11:00, RationTimeId: 3) | Bữa Chiều (17:00, RationTimeId: 5) |
| :--- | :--- | :--- | :--- |
| **BT01** (Thông thường/Ngoại khoa) | Mã: `BM17770` (ID: `30073`) | Mã: `BM18696` (ID: `30153`) | Mã: `BM18697` (ID: `30154`) |
| **DD01** (Đái tháo đường) | Mã: `CO02` (ID: `30180`) | Mã: `CS01` (ID: `30181`) | Mã: `BM18650` (ID: `30133`) |
| **TM01** (Tim mạch / THA) | Mã: `BM18075` (ID: `30117`) | Mã: `BM18039` (ID: `30093`) | Mã: `BM18040` (ID: `30094`) |

### 6.3. Endpoint & Cấu trúc DTO:
* **Endpoint**: `POST http://192.168.7.236:1608/api/HisServiceReq/RationCreate`
* **DTO**: `HisRationServiceReqSDO`

```json
{
  "TreatmentIds": [ 7060265 ],
  "InstructionTimes": [ 20260824050000 ],
  "RequestRoomId": 5257,
  "RequestLoginName": "034727",
  "RequestUserName": "NGUYỄN HỮU SÂM",
  "IcdCode": "M47.00†",
  "IcdName": "Viêm đốt sống...",
  "RationServices": [
    { "ServiceId": 30073, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [1] },
    { "ServiceId": 30153, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [3] },
    { "ServiceId": 30154, "PatientTypeId": 42, "RoomId": 5809, "Amount": 1.0, "RationTimeIds": [5] }
  ]
}
```

### 6.4. Công cụ sẵn có: `QuickRation.exe`
```powershell
.\QuickRation.exe -p 0003969449 -combo BT01 -date 20260824,20260825
```

---

## 7. PHÂN HỆ 3: KÊ ĐƠN THUỐC NỘI TRÚ & RA VIỆN

* **Kho thuốc viện mặc định**: `MediStockId = 4210` (hoặc `4209, 804, 4208`).
* **Đối tượng**: `PatientTypeId = 1` (BHYT) hoặc `42` (Viện phí).
* **Loại xuất**: `EXP_MEST_TYPE_ID = 15` (Đơn ra viện) / `14` (Đơn nội trú).
* **Endpoint**: `POST api/HisExpMest/CreateOutPatientPres` hoặc `CreateInPatientPres`.

---

## 8. PHÂN HỆ 4: KÝ SỐ ĐIỆN TỬ & MỜI BÁC SĨ KÝ (EMR SIGN)

* **Loại tài liệu**: `DOCUMENT_TYPE_ID = 7` (Tờ điều trị).
* **Ký chính (NumOrder = 1)**: Bác sĩ điều trị `034727` (Ths.BS Nguyễn Hữu Sâm).
* **Mời ký phối hợp (NumOrder = 2)**: Bác sĩ `ndh2` (BS Nguyễn Đức Hoàng).

---

## 9. BẢNG TỔNG HỢP SAI LẦM & BÀI HỌC XƯƠNG MÁU

| STT | Hiện tượng / Lỗi gặp phải | Nguyên nhân gốc rễ | Giải pháp chuẩn xác |
| :---: | :--- | :--- | :--- |
| **1** | `IOException: process cannot access file` khi đọc token | HIS client đang ghi `LogSystem.txt` với lock | Mở file bằng `FileStream` với `FileShare.ReadWrite` |
| **2** | `api/HisTracking/Create` trả về `Success: false` | Gửi JSON phẳng hoặc gửi trực tiếp `HIS_TRACKING` | Bắt buộc đóng gói vào `MOS.SDO.HisTrackingSDO` kèm `WorkingRoomId` |
| **3** | Không tìm thấy trường `TREATMENT_INSTRUCTION` | Tên trường trong database/EFModel thực tế là `MEDICAL_INSTRUCTION` | Dùng đúng property `MEDICAL_INSTRUCTION` |
| **4** | Suất ăn chỉ vào được 1 ngày dù truyền mảng nhiều ngày | Backend MOS chỉ xử lý `InstructionTimes[0]` | Dùng vòng lặp gọi API tách biệt cho từng ngày |
| **5** | Tràn bộ nhớ / treo lệnh khi query buồng giường | Filter `HisTreatmentBedRoom` sai tên trường lọc | Luôn lọc bằng `TREATMENT_IDs = [treatmentId]` |
| **6** | Lỗi định dạng giờ tờ điều trị | Truyền sai 14 số | Chuẩn hóa `yyyyMMddHHmmss` (VD: `20260823080000`) |
| **7** | Phần mềm báo không tồn tại mã ICD (VD: `B18.1`, `M60.0`...) | Hệ thống HIS/MOS áp dụng **danh mục ICD-10 5 ký tự chuẩn Bộ Y tế** mới; các mã 3-4 ký tự cũ đã chuyển `IsActive = 0` | Bắt buộc tra cứu và sử dụng mã ICD 5 ký tự đang khả dụng (`IsActive = 1`): VD dùng `B18.19` (thay vì `B18.1`), `M60.05` (thay vì `M60.0`), `M47.00†`, `S62.11`... |
| **8** | Rút ngắn/tóm tắt chung chung kết quả MRI, X-quang, CT (VD: ghi "xẹp lún các đốt sống", "gãy xương chi") | Tóm tắt sơ sài làm mất dữ liệu tầng đốt sống và vị trí giải phẫu cụ thể, gây nguy cơ chỉ định can thiệp nhầm vị trí (đặc biệt là phẫu thuật bơm xi măng sinh học BXM) | **Bắt buộc trích xuất đích danh, chính xác từng tầng/vị trí tổn thương:** VD ghi rõ `Xẹp cấp L2, L3, L5`, `Xẹp cũ T12`, `Trượt L4 ra trước độ I`, `Rách vòng xơ L4/5`, `Gãy xương tháp và xương thang cổ tay trái`... |
| **9** | Chỉ định Xét nghiệm BM02426 (Đường máu mao mạch) bị lỗi `Success: false` | 1) Thiếu `SampleTypeCode = "BP0042"` trên `ServiceReqDetailSDO` (bắt buộc cho BM02426). 2) `PrimaryPatientTypeId` gán = 1 (với BHYT phải là `null`). 3) `TrackingInfoSDO.IntructionTime` không khớp với `TRACKING_TIME` của tờ điều trị. 4) Chưa kích hoạt `UpdateWorkInfo` cho phòng làm việc. | 1) Luôn gán `SampleTypeCode = "BP0042"`. 2) Gán `PrimaryPatientTypeId = (PatientTypeId == 1 ? null : PatientTypeId)`. 3) Đồng bộ `InstructionTime`, `InstructionTimes`, `UseTimes`, `TrackingInfos[0].IntructionTime` bằng chính `tracking.TRACKING_TIME`. 4) Đăng ký danh sách phòng qua `UpdateWorkInfo`. |

---

### 9.1. Quy Tắc Bắt Buộc Về Mã ICD-10 (Hệ 5 Ký Tự Mới):
- **Bắt buộc dùng hệ mã ICD-10 chi tiết 5 ký tự (`IsActive = 1`)**:
  - Viêm gan B mạn: Dùng **`B18.19`** (*Bệnh viêm gan virus B mạn tính không có viêm gan D [tác nhân delta], giai đoạn khác và/hoặc không xác định*) hoặc **`B18.10`**. Tuyệt đối không dùng `B18.1` (đã deactive).
  - Áp xe cơ / Viêm cơ mủ vùng đùi mông: Dùng **`M60.05`** (*Viêm cơ do nhiễm trùng, vùng chậu và/hoặc đùi*). Không dùng `M60.0` (đã deactive).
  - Thoái hóa cột sống: Dùng **`M47.00†`**, **`M47.8`**, **`M51.2`**, **`M51.3`**...
  - Gãy xương cổ bàn tay / đòn: Dùng **`S62.11`**, **`S42.00`**, **`M84.04`**...
- Khi xây dựng công cụ tra cứu hoặc tạo bệnh án tự động: Luôn filter với điều kiện `IS_ACTIVE == 1` trong bảng `HIS_ICD` (`api/HisIcd/Get`).

### 9.2. Quy Tắc Trích Xuất Chẩn Đoán Hình Ảnh (Đích Danh Tầng & Vị Trí Tổn Thương):
- **Cột sống:** Phải nêu rõ từng tầng đốt sống bị tổn thương cấp (có phù tủy xương) và tổn thương cũ:
  - *Đúng chuẩn:* `Xẹp cấp L2, L3, L5 (phù tủy xương)`, `Xẹp cũ T12`, `Trượt L4 ra trước độ I kèm hẹp ống sống`, `Rách vòng xơ đĩa đệm L4/5`.
  - *Sai/Cấm kỵ:* `Xẹp lún phù tủy xương các đốt sống thắt lưng` (chung chung, thiếu an toàn lâm sàng).
- **Xương khớp chi:** Ghi rõ từng xương, vị trí đoạn gãy (1/3 trên, giữa, dưới), độ di lệch hoặc tên cụ thể từng gân đứt:
  - *Đúng chuẩn:* `Gãy xương tháp và xương thang cổ tay trái di lệch`, `Gãy 1/3 giữa xương đòn phải`, `Đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải`.

---

## 10. HƯỚNG DẪN BIÊN DỊCH & CHẠY CÔNG CỤ CLI SIÊU TỐC

Khi di chuyển sang máy mới có .NET Framework (mặc định có trên mọi Windows 10/11):

### 10.4. Ứng Dụng Chỉ Định Đường Máu Mao Mạch Tại Giường BM02426 (`HisGlucoseBedsideAssigner.exe`):
Ứng dụng chuyên dụng cho phép chỉ định hàng loạt cận lâm sàng **`BM02426`** (*Xét nghiệm đường máu mao mạch tại giường*) cho bệnh nhân nội trú với khả năng chọn linh hoạt nhiều khung giờ trong ngày:
- **Tập tin chạy**: `HisGlucoseBedsideAssigner.exe` (kèm file config `HisGlucoseBedsideAssigner.exe.config`)
- **Tập tin kích hoạt nhanh 1-Click**: `Chay_ChiDinh_BM02426.bat`
- **Mã dịch vụ**: `BM02426` (Service ID: `6217`)
- **Phòng thực hiện mặc định**: Room ID `931` (*Phòng Tiểu Phẫu Nhà Q - Khoa CTCH*) hoặc `531` (*P289*)
- **Tính năng nổi bật**:
  1. *Đa khung giờ*: Hỗ trợ chọn đồng thời `06:00`, `11:00`, `17:00`, `21:00`, `Hiện tại`, hoặc khung giờ tùy chỉnh `HH:mm`.
  2. *Nhập liệu siêu tốc*: Nhập trực tiếp danh sách mã BN/mã BA (copy paste từ Excel/Word/Text) hoặc 1-click tải toàn bộ bệnh nhân nội trú Khoa 57.
  3. *Tự động liên kết/tạo tờ điều trị*: Tự động dò tìm tờ điều trị trong ngày hoặc tạo tờ điều trị mới (`HIS_TRACKING`) khớp với giờ chỉ định để đảm bảo hệ số y lệnh hợp lệ 100%.
  4. *Báo cáo & Xuất file*: Xuất báo cáo kết quả ra file CSV/Excel và copy nhanh kết quả vào Clipboard.
  5. *Hỗ trợ CLI*: Chạy ngầm hoặc qua dòng lệnh với cú pháp:
     ```powershell
     .\HisGlucoseBedsideAssigner.exe -p "0003969449,0003298895" -time "06:00,11:00,17:00,21:00" -date "2026-08-25"
     ```

### 10.5. Ứng Dụng Tạo Tờ Điều Trị & Chế Độ Chăm Sóc Bệnh Nhân (`HisTrackingCreator.exe`):
Ứng dụng chuyên dụng tạo Tờ điều trị (`HIS_TRACKING`) và Dấu hiệu sinh tồn (`HIS_DHST`) cho bệnh nhân có mã bệnh nhân cụ thể, vào giờ cụ thể, nội dung cụ thể và chế độ chăm sóc cụ thể:
- **Tập tin chạy**: `HisTrackingCreator.exe` (kèm file config `HisTrackingCreator.exe.config`)
- **Tập tin kích hoạt nhanh 1-Click**: `Chay_Tao_ToDieuTri.bat`
- **Vị trí file mã nguồn & binary**: `HisTrackingCreator.exe`, `HisTrackingCreator.cs` (Thư mục gốc & `.agents/skills/his-clinical-operations/scripts/`)
- **Lệnh biên dịch siêu tốc (khi sang máy mới)**:
  ```powershell
  & "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HisTrackingCreator.exe /lib:.,ReferencedAssemblies,HisAutoPrescribe_Portable /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisTrackingCreator.cs
  ```
- **Cú pháp CLI**:
  ```powershell
  # Tạo tờ điều trị đơn lẻ:
  .\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "Bệnh nhân tỉnh táo, không sốt, vết mổ khô sạch." -care "Chăm sóc cấp II. Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày." -med "Thuốc theo đơn."
  
  # Tạo theo mẫu lâm sàng chuẩn 1..6:
  .\HisTrackingCreator.exe -p "0003969449,0003298895" -template 1 -time "08:00" -date "2026-08-25"
  ```

### 10.6. Ứng Dụng Tổng Hợp Báo Cáo Giao Ban Ca Trực & Bilan Phẫu Thuật (`HospitalShiftReporter.exe`):
Ứng dụng chuyên dụng phục vụ công tác giao ban, tổng hợp ca trực, lọc bệnh nhân vào khoa, bệnh nhân sau mổ về khoa, rà soát bilan 12 bệnh nhân dự kiến mổ và tra cứu bệnh nhân truyền máu:
- **Tập tin chạy**: `HospitalShiftReporter.exe` (kèm file config `HospitalShiftReporter.exe.config`)
- **Vị trí file mã nguồn & binary**: `HospitalShiftReporter.exe`, `HospitalShiftReporter.cs` (Thư mục gốc & `.agents/skills/his-clinical-operations/scripts/`)
- **Lệnh biên dịch siêu tốc (khi sang máy mới)**:
  ```powershell
  & "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HospitalShiftReporter.exe /lib:.,ReferencedAssemblies,HisAutoPrescribe_Portable /r:System.dll,System.Core.dll,System.Data.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HospitalShiftReporter.cs
  ```
- **Quy tắc tra cứu**:
  1. *Bệnh nhân vào khoa ca trực*: Query `V_HIS_DEPARTMENT_TRAN` theo `DEPARTMENT_ID == 57` và `DEPARTMENT_IN_TIME` trong khoảng thời gian trực.
  2. *Bệnh nhân mổ về ca trực*: Query `V_HIS_SERVICE_REQ` có `SERVICE_REQ_TYPE_ID == 6` và `FINISH_TIME` hoặc `INTRUCTION_TIME` trong ngày trực, kết hợp đối chiếu diễn biến hậu phẫu trên `V_HIS_TRACKING`.
  3. *Bệnh nhân dự kiến mổ*: Query hồ sơ bệnh án theo tên hoặc `TREATMENT_BED_ROOM` đang nằm, trích xuất toàn bộ CTM, Đông máu, Sinh hóa, Điện giải, Nhóm máu, Virus, CĐHA, MRI, CT.
  4. *Bệnh nhân truyền máu*: Query `V_HIS_SERE_SERV` có tên dịch vụ chứa "máu", "Khối hồng cầu", "Huyết tương", trích xuất chỉ số HGB/HCT các thời điểm trước và sau truyền.



