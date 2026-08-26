# 🏥 CẨM NANG TOÀN DIỆN TÍCH HỢP HIS / MOS / EMR CHO AI AGENT (MASTER PLAYBOOK)
> **Phiên bản Hợp nhất Tối thượng (Desktop & Laptop Unified Master Edition)**
> **Mục đích**: Tài liệu hóa 100% kinh nghiệm thực chiến, kiến trúc, cấu trúc DTO, các bẫy runtime (gotchas), từ điển lâm sàng chuẩn hóa và toàn bộ kho công cụ tự động hóa trên hệ thống HIS Bệnh viện Bạch Mai. Một Agent ở bất kỳ máy tính nào chỉ cần đọc duy nhất tài liệu này là có thể thực thi chính xác 100% ngay lập tức mà **không cần thử lỗi hay phân tích ngược lại từ đầu**.

---

## 📑 MỤC LỤC
1. [Kiến Trúc Hệ Thống & Cổng Kết Nối](#1-kiến-trúc-hệ-thống--cổng-kết-nối)
2. [Bộ Thông Số Bác Sĩ & Khoa Phòng Mặc Định](#2-bộ-thông-số-bác-sĩ--khoa-phòng-mặc-định)
3. [Cơ Chế Xác Thực, Đăng Nhập & Kích Hoạt Phòng Làm Việc](#3-cơ-chế-xác-thực-đăng-nhập--kích-hoạt-phòng-làm-việc)
4. [Quy Trình Tra Cứu Bệnh Nhân, Buồng Giường & Bilan Tiền Phẫu](#4-quy-trình-tra-cứu-bệnh-nhân-buồng-giường--bilan-tiền-phẫu)
5. [Phân Hệ 1: Tờ Điều Trị & Chế Độ Chăm Sóc (Treatment Tracking & DHST)](#5-phân-hệ-1-tờ-điều-trị--chế-độ-chăm-sóc-treatment-tracking--dhst)
6. [Phân Hệ 2: Chỉ Định Suất Ăn Dinh Dưỡng Bệnh Lý (Diet / Ration Orders)](#6-phân-hệ-2-chỉ-định-suất-ăn-dinh-dưỡng-bệnh-lý-diet--ration-orders)
7. [Phân Hệ 3: Kê Đơn Thuốc Nội Trú & Ra Viện Chuẩn Lâm Sàng 100%](#7-phân-hệ-3-kê-đơn-thuốc-nội-trú--ra-viện-chuẩn-lâm-sàng-100)
8. [Phân Hệ 4: Chỉ Định Cận Lâm Sàng & Đường Máu Mao Mạch Tại Giường (BM02426)](#8-phân-hệ-4-chỉ-định-cận-lâm-sàng--đường-máu-mao-mạch-tại-giường-bm02426)
9. [Phân Hệ 5: Báo Cáo Giao Ban Ca Trực & Bilan Phẫu Thuật Hậu Phẫu](#9-phân-hệ-5-báo-cáo-giao-ban-ca-trực--bilan-phẫu-thuật-hậu-phẫu)
10. [Phân Hệ 6: Ký Số Điện Tử & Mời Bác Sĩ Ký (EMR Sign)](#10-phân-hệ-6-ký-số-điện-tử--mời-bác-sĩ-ký-emr-sign)
11. [Phân Hệ 7: Chỉ Định & Biên Bản Hội Chẩn Chuyên Khoa (Debate Diagnostic & Consultation)](#11-phân-hệ-7-chỉ-định--biên-bản-hội-chẩn-chuyên-khoa-debate-diagnostic--consultation)
12. [Quy Chuẩn Lâm Sàng Bắt Buộc (Mã ICD-10 5 Ký Tự & Mô Tả CĐHA)](#12-quy-chuẩn-lâm-sàng-bắt-buộc-mã-icd-10-5-ký-tự--mô-tả-cđha)
13. [Bảng Tổng Hợp 28+ Sai Lầm & Bài Học Xương Máu (Gotchas Matrix)](#13-bảng-tổng-hợp-28-sai-lầm--bài-học-xương-máu-gotchas-matrix)
14. [Hướng Dẫn Biên Dịch & Chạy Công Cụ CLI Tức Thì](#14-hướng-dẫn-biên-dịch--chạy-công-cụ-cli-tức-thì)
15. [Cơ Chế Đồng Bộ Tri Thức 1-Click Giữa Máy Bàn & Laptop](#15-cơ-chế-đồng-bộ-tri-thức-1-click-giữa-máy-bàn--laptop)

---

## 1. KIẾN TRÚC HỆ THỐNG & CỔNG KẾT NỐI

Mọi giao tiếp đều đi qua mạng nội bộ bệnh viện (hoặc qua VPN/Tunnel an toàn):

| Dịch vụ | Địa chỉ IP & Cổng | Mục đích | Phương thức giao tiếp |
| :--- | :--- | :--- | :--- |
| **MOS Backend** | `http://192.168.7.236:1608/` | Nghiệp vụ HIS chính (Tờ điều trị, Suất ăn, Thuốc, Dịch vụ, CLS) | REST API (JSON Body hoặc Base64 param) |
| **ACS Auth** | `http://192.168.7.200:1401/` | Xác thực tài khoản bác sĩ, cấp và gia hạn TokenCode | `api/Token/Login`, `api/Token/Renew` |
| **SDA Data** | `http://192.168.7.200:1410/` | Danh mục hệ thống, phân quyền, cấu hình giao diện | SDA Service APIs |
| **EMR Service** | `http://192.168.7.239:1415/` | Bệnh án điện tử, tạo văn bản y khoa, ký số, mời ký | EMR Document APIs |

---

## 2. BỘ THÔNG SỐ BÁC SĨ & KHOA PHÒNG MẶC ĐỊNH

Khi gửi request hoặc xây dựng kịch bản y lệnh, sử dụng thông tin định danh của phiên làm việc bác sĩ:

* **Bác sĩ điều trị chính**: `034727` - **Ths.BS NGUYỄN HỮU SÂM** (Pass mặc định hệ thống: `9981`)
* **Bác sĩ phối hợp / Mời ký**: `ndh2` - **BS NGUYỄN ĐỨC HOÀNG**
* **Tài khoản bác sĩ phụ trợ**: `vmc` - **BS VŨ MINH CƯỜNG** (Pass: `789789`)
* **Bác sĩ Khoa 57 / PTV**: 
  - `tmd2` / `tmd`: **BS TRỊNH MINH ĐỨC** *(Tuyệt đối không tự đoán tên từ ký hiệu viết tắt)*
  - `lvl12`: **BS LÊ VĂN LƯỢNG**
  - `ldt`: **BS LÊ ĐĂNG TOÀN**
  - `dhg`: **BS ĐINH HOÀNG GIANG**
  - `pnt2`: **BS PHẠM NGỌC THẮNG**
  - `ddb`: **BS ĐỖ ĐĂNG BÌNH** (hoặc BS ĐOÀN ĐỨC BÁCH)
* **Khoa lâm sàng**: Khoa Chấn thương Chỉnh hình & Cột sống (`DEPARTMENT_ID = 57`, Mã Khoa: `9`)
* **Các buồng bệnh phụ trách**: Phòng 712, 714, 716, 724, 725 (Room ID tương ứng, ví dụ P724 có `BED_ROOM_ID = 780`, `ROOM_ID = 5257`; Phòng 734 có `ROOM_ID = 5248`)
* **IP Client gửi request**: `100.93.206.93`

---

## 3. CƠ CHẾ XÁC THỰC, ĐĂNG NHẬP & KÍCH HOẠT PHÒNG LÀM VIỆC

### 3.1. Cách 1: Đọc Live Token từ HIS Client đang chạy (Kèm xử lý File Lock)
HIS Client ghi log chứa `TokenCode` vào file log cục bộ: `E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt` (hoặc `Logs/HLSLogSystem.txt`).

> ⚠️ **Bẫy nghiêm trọng (File Lock Gotcha):**
> Ứng dụng HIS trên máy liên tục ghi vào file log. Nếu dùng `File.ReadAllLines`, `File.ReadAllText` hoặc PowerShell `Get-Content` không đúng cách, Windows sẽ báo lỗi:
> `System.IO.IOException: The process cannot access the file because it is being used by another process.`

✅ **Cách xử lý chuẩn xác 100% (Bắt buộc dùng `FileShare.ReadWrite`):**
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

### 3.3. Cách 2: Đăng nhập tự động & [BẮT BUỘC] Kích hoạt WorkInfo phòng làm việc
Khi chạy công cụ độc lập (CLI / Daemon), sau khi login lấy TokenCode, **bắt buộc phải kích hoạt danh sách phòng làm việc (`UpdateWorkInfo`)** trước khi thực hiện bất kỳ y lệnh lâm sàng nào để tránh lỗi `KhongCoThongTinPhongLamViec`:

```csharp
// 1. Khởi tạo cấu hình hệ thống
HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();

// 2. Đăng nhập lấy Token
ClientTokenManager tokenManager = new ClientTokenManager("HIS");
CommonParam param = new CommonParam();
var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
ApiConsumers.SetConsunmer(token.TokenCode);

// 3. [BẮT BUỘC] Kích hoạt thông tin phòng làm việc
var workInfo = new WorkInfoSDO
{
    DepartmentId = 57,
    BranchId = 1,
    Rooms = new List<RoomSDO>
    {
        new RoomSDO { RoomId = 5248 }, // Phòng 734 (Phòng giao ban/khám)
        new RoomSDO { RoomId = 5252 }, // Phòng 712
        new RoomSDO { RoomId = 5251 }, // Phòng 714
        new RoomSDO { RoomId = 5257 }  // Phòng 724
    }
};
var myAdapter = new MyAdapter();
var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkPlaceSDO = workPlaces;
HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkInfoSDO = workInfo;
```

---

## 4. QUY TRÌNH TRA CỨU BỆNH NHÂN, BUỒNG GIƯỜNG & BILAN TIỀN PHẪU

Mỗi bệnh nhân có các định danh quan trọng:
* **Mã bệnh nhân (Patient Code)**: 10 ký tự (VD: `0003969449`).
* **Mã hồ sơ bệnh án (Treatment Code)**: 12 ký tự (VD: `000007060449`).
* **Treatment ID (Khóa chính nội bộ)**: Số nguyên `Int64` (VD: `7060265`).

### 4.1. Tìm hồ sơ đang điều trị (`api/HisTreatment/GetView`)
```csharp
var filter = new HisTreatmentViewFilter();
filter.KEY_WORD = patientCode; // hoặc TREATMENT_CODE__EXACT
var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, filter, param);
var currentTreatment = treatments.LastOrDefault(x => x.IS_PAUSE != 1);
```

### 4.2. Lấy vị trí buồng / giường hiện tại (`api/HisTreatmentBedRoom/GetLView`)
```csharp
var bedFilter = new HisTreatmentBedRoomLViewFilter();
bedFilter.TREATMENT_IDs = new List<long> { currentTreatment.ID };
bedFilter.IS_IN_ROOM = true;
var bedRooms = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, bedFilter, param);
var currentBed = bedRooms.LastOrDefault(x => x.REMOVE_TIME == null || x.REMOVE_TIME == 0);
long requestRoomId = currentBed != null ? (currentBed.BED_ROOM_ID ?? 5248) : 5248;
```

### 4.3. Trích xuất toàn bộ Bilan Xét nghiệm & CĐHA (`api/HisSereServTein/GetView`)
```csharp
var teinFilter = new HisSereServTeinViewFilter();
teinFilter.TDL_TREATMENT_ID = currentTreatment.ID;
var teinList = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);

Func<string, string> getTein = (match) => {
    var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
    return item != null ? item.VALUE + " " + item.TEST_INDEX_UNIT_NAME : "-";
};

string hgb = getTein("Hemoglobin");
string wbc = getTein("Số lượng bạch cầu");
string plt = getTein("Số lượng tiểu cầu");
string inr = getTein("PT - INR");
string glucose = getTein("Glucose [Máu]");
string creatinin = getTein("Creatinin [máu]");
string bloodGroup = getTein("ABO") + " Rh " + getTein("Rh(D)");
```

---

## 5. PHÂN HỆ 1: TỜ ĐIỀU TRỊ & CHẾ ĐỘ CHĂM SÓC (TREATMENT TRACKING & DHST)

### 5.1. Endpoint Backend MOS
* **URL**: `http://192.168.7.236:1608/api/HisTracking/Create`
* **Loại đối tượng yêu cầu**: `MOS.SDO.HisTrackingSDO`

> ⚠️ **Bẫy DTO nghiêm trọng:**
> 1. **Sai tên trường**: Trường y lệnh trong `HIS_TRACKING` tên là **`MEDICAL_INSTRUCTION`** (không phải `TREATMENT_INSTRUCTION`).
> 2. **Sai lớp bao bọc**: Không gửi `HIS_TRACKING` dạng phẳng. Phải bọc vào `MOS.SDO.HisTrackingSDO`:
>    - `Tracking`: Đối tượng `HIS_TRACKING`
>    - `WorkingRoomId`: `Int64` (ID phòng làm việc)

### 5.2. Cấu trúc C# chuẩn tạo Tờ điều trị & DHST:
```csharp
var tracking = new HIS_TRACKING();
tracking.TREATMENT_ID = treatmentId;
tracking.TRACKING_TIME = 20260825080000; // yyyyMMddHHmmss
tracking.ICD_CODE = icdCode;             // VD: "M47.00†"
tracking.ICD_NAME = icdName;
tracking.ICD_SUB_CODE = icdSubCode;     // VD: "E11.9"
tracking.ICD_TEXT = icdText;
tracking.CONTENT = "Bệnh nhân tỉnh, tiếp xúc tốt, không sốt, vết mổ khô sạch đầu chi ấm.";
tracking.MEDICAL_INSTRUCTION = "Chăm sóc cấp II. Chế độ ăn BT01. Thuốc theo đơn. Theo dõi DHST 2 lần/ngày.";
tracking.DEPARTMENT_ID = 57;
tracking.ROOM_ID = workingRoomId;

var sdo = new HisTrackingSDO();
sdo.Tracking = tracking;
sdo.WorkingRoomId = workingRoomId;

var result = adapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
```

### 5.3. Công cụ sẵn có:
* `HisTrackingCreator.exe` (Kèm file kích hoạt nhanh `Chay_Tao_ToDieuTri.bat`):
  ```powershell
  # Tạo tờ điều trị đơn lẻ:
  .\HisTrackingCreator.exe -p "0003969449" -time "08:00" -content "Bệnh nhân tỉnh táo, vết mổ khô" -care "Chăm sóc cấp II. Ăn BT01" -med "Thuốc theo đơn"

  # Tạo theo mẫu lâm sàng chuẩn 1..6:
  .\HisTrackingCreator.exe -p "0003969449,0003298895" -template 1 -time "08:00" -date "2026-08-25"
  ```
* `QuickTracking.exe`:
  ```powershell
  .\QuickTracking.exe -p 0003969449 -time 08:00 -content "bn tỉnh không sốt huyết động ổn"
  ```

### 5.4. Quy trình Xóa Tờ Điều Trị (Delete Tracking):
- **Endpoint**: `POST api/HisTracking/Delete`
- **Consumer**: `ApiConsumers.MosConsumer`
- **Payload**: `Int64` (chính là `trackingId`, truyền trực tiếp dạng số nguyên: `adapter.Post<bool>("api/HisTracking/Delete", ApiConsumers.MosConsumer, trackingId, param)`).
- **Phân quyền**: Đăng nhập bằng tài khoản Bác sĩ tạo tờ điều trị (hoặc Bác sĩ được ủy quyền trong khoa) kèm cập nhật `UpdateWorkInfo`.

---

## 6. PHÂN HỆ 2: CHỈ ĐỊNH SUẤT ĂN DINH DƯỠNG BỆNH LÝ (DIET / RATION ORDERS)

### 6.1. Nguyên tắc cốt lõi: 1 Request = 1 Ngày
* Backend MOS chỉ ghi nhận **đúng 1 mốc thời gian đầu tiên** trong mảng `InstructionTimes`.
* Nếu kê cho $N$ ngày, **bắt buộc lặp $N$ lần gọi API**, mỗi lần truyền `InstructionTimes = [YYYYMMDD050000]`.
* **Quy trình 3 bước**: Pre-check (`api/HisSereServRation/GetView`) -> Execute -> Post-verify đối soát 100%.

### 6.2. Từ điển Suất ăn Dinh dưỡng Lâm sàng:
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
  "InstructionTimes": [ 20260825050000 ],
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
.\QuickRation.exe -p 0003969449 -combo BT01 -date 20260825,20260826
.\QuickRation.exe -room 712 -combo DD01 -date 20260825,20260826
```

---

## 7. PHÂN HỆ 3: KÊ ĐƠN THUỐC NỘI TRÚ & RA VIỆN CHUẨN LÂM SÀNG 100%

### 7.1. Cấu hình Bản đồ Kho Dược & Loại xuất:
* **Kho thuốc ống (tiêm, truyền, dịch, chống đông)**: `MediStockId = 4209` (Ceftriaxone, Zinacef, Unasyn, Medivernol, Voxin, Paracetamol Kabi, Gemapaxane, Heparine...)
* **Kho thuốc viên (uống)**: `MediStockId = 4210` (Augmentin 1g, Tramadol/Para, Celebrex, Arcoxia, Lyrica, Tolperison, Nexium...)
* **Kho Dịch truyền**: `MediStockId = 804` (Natri Clorid 0.9% 100ml, 250ml, 500ml...)
* **Kho Hướng thần / Gây nghiện**: `MediStockId = 4208` (Seduxen 5mg...)
* **Đối tượng**: `PatientTypeId = 1` (BHYT) hoặc `42` (Viện phí).
* **Loại xuất**: `EXP_MEST_TYPE_ID = 14` (Đơn nội trú) / `15` (Đơn ra viện).
* **Endpoint**: `POST api/HisServiceReq/InPatientPresCreate`

### 7.2. ⚠️ TỪ ĐIỂN CÁCH DÙNG THUỐC CHUẨN LÂM SÀNG (BẮT BUỘC 100% KHÔNG ĐỂ BS SỬA TAY):

| Mã thuốc | Tên biệt dược | Hoạt chất & Hàm lượng | Chia cữ (S/Tr/C/T) | Hướng dẫn sử dụng chuẩn lâm sàng (`TUTORIAL`) | Dung môi kèm theo |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `TH.MEDI007` | **Medivernol 1g** | Ceftriaxone 1g | `2` / `-` / `-` / `-` | Pha 02 lọ vào 100ml NaCl 0.9%, truyền TM 30-40 giọt/phút lúc 9h sáng. | NaCl 0.9% 100ml (1 chai) |
| `TH.ROCE003` | **Rocephin 1g** | Ceftriaxon 1g | `2` / `-` / `-` / `-` | Pha 02 lọ Ceftriaxone với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 9h sáng. | NaCl 0.9% 100ml (1 chai) |
| `TH.ZINA002` | **Zinacef 750mg** | Cefuroxim 750mg | `2` / `-` / `-` / `-` | Pha 02 lọ với 02 ống Nước cất 10ml, tiêm/truyền TM chậm lúc 9h sáng. | Nước cất tiêm 10ml (2 ống) |
| `TH.UNAS005` | **Unasyn 1.5g** | Ampicilin + Sulbactam | `1` / `-` / `1` / `-` | Pha mỗi lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 9h và 17h. | NaCl 0.9% 100ml (2 chai) |
| `TH.VOXI003` | **Voxin 500mg** | Vancomycin 500mg | `1.5` / `-` / `-` / `1.5` | Pha mỗi lần 1.5 lọ (750mg) với 250ml NaCl 0.9%, truyền TM chậm >= 60 phút lúc 9h - 21h. | NaCl 0.9% 250ml (2 chai) |
| `TH.PARA004` | **Paracetamol Kabi 1g** | Paracetamol 1g/100ml | `1` / `-` / `-` / `1` | Truyền TM 30-40 giọt/phút lúc 10h - 18h khi đau/sốt (cách nhau >= 4-6h). | Không |
| `TH.AUGM008` | **Augmentin 1g** | Amoxicilin + Clavulanic | `1` / `-` / `-` / `1` | Uống 1 viên ngay đầu bữa ăn sáng (8h) và chiều (18h). | Không |
| `TH.TRAM001` | **Tramadol/Para Normon** | Tramadol 37.5mg + Para 325mg | `1` / `-` / `-` / `1` | Uống 1 viên sau ăn sáng (9h) và chiều (18h) khi đau. | Không |
| `TH.ARCO045` | **Arcoxia 60mg** | Etoricoxib 60mg | `1` / `-` / `-` / `-` | Uống 1 viên buổi sáng lúc 9h sau ăn no. | Không |
| `TH.CELE003` | **Celebrex 200mg** | Celecoxib 200mg | `1` / `-` / `-` / `-` | Uống 1 viên buổi sáng lúc 8h-9h sau ăn no. | Không |
| `TH.ACUP002` | **Acupan 20mg/2ml** | Nefopam HCl 20mg | `1` / `-` / `1` / `-` | Pha 1 ống vào 100ml NaCl 0.9% truyền TM 100ml/h lúc 10h-16h khi đau. | NaCl 0.9% 100ml |
| `TH.LYRI003` | **Lyrica 75mg** | Pregabalin 75mg | `-` / `-` / `-` / `1` | Uống 1 viên buổi tối lúc 21h (giảm đau thần kinh). | Không |
| `TH.NEUT001` | **Neutrifore 3B** | Vitamin B1 + B6 + B12 | `1` / `-` / `-` / `1` | Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên lúc 9h - 19h. | Không |
| `TH.PHAR001` | **Pharmaclofen 10mg** | Baclofen 10mg | `1` / `-` / `-` / `1` | Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên sau ăn (giãn cơ). | Không |
| `TH.TOLP001` | **Tolperison 150mg** | Tolperison HCl 150mg | `1` / `-` / `-` / `1` | Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên sau ăn. | Không |
| `TH.NEXI011` | **Nexium Mups 20mg** | Esomeprazol 20mg | `1` / `-` / `-` / `-` | Uống 1 viên buổi sáng trước ăn 30 phút (8h). | Không |
| `TH.GEMA009` | **Gemapaxane 4000IU** | Enoxaparin natri 4000IU | `-` / `-` / `-` / `1` | Tiêm dưới da thành bụng 1 bơm lúc 20h, theo dõi chảy máu (dự phòng VTE). | Không |
| `TH.ACTR004` | **Actrapid 1000IU** | Insulin Human 100IU/ml | `4-6` / `4-6` / `4-6` / `-` | Tiêm dưới da trước các bữa ăn 30 phút theo phác đồ đường máu mao mạch. | Không |
| `TH.LANT001` | **Lantus 100IU/ml** | Insulin Glargine | `-` / `-` / `-` / `10-14` | Tiêm dưới da buổi tối lúc 21h (Insulin nền kéo dài). | Không |
| `TH.AMLO004` | **Amlor 5mg** | Amlodipin 5mg | `1` / `-` / `-` / `-` | Uống 1 viên buổi sáng lúc 8h (huyết áp). | Không |
| `TH.SEDU004` | **Seduxen 5mg (!)** | Diazepam 5mg | `-` / `-` / `-` / `1` | Uống 1 viên buổi tối lúc 21h30 (khi khó ngủ, đánh giá tri giác trước dùng). | Không |
| `TH.BRIO002` | **Briozcal (Calci+D3)**| Calci carbonat 500mg+D3 | `1` / `-` / `1` / `-` | Ngày uống 2 viên chia 2 lần, sáng: 1 viên, chiều: 1 viên lúc 9h-15h sau ăn. | Không |
| `SPBM25651` | **Leanpro PreSur 12.5%**| Dung dịch Carbohydrate | `-` / `-` / `-` / `4` | Uống 4 chai tối 20h trước mổ, 2 chai sáng hôm sau trước mổ 2h (chuẩn ERAS). | Không |
| `TH.POVI008` | **Povidone 10% 125ml** | Povidon Iodin 10% | `1` / `-` / `-` / `-` | Dùng ngoài, sát khuẩn vết mổ và thay băng rửa vết thương hàng ngày. | Không |

*(Xem toàn bộ từ điển 130 loại thuốc tại `MEDICATION_CLINICAL_TUTORIALS.md`)*.

### 7.4. QUY CHUẨN KÊ VẬT TƯ & DUNG DỊCH TIÊU HAO THAY BĂNG (DRESSING CONSUMABLES PROTOCOL):
Khi kê y lệnh thay băng rửa vết thương, chăm sóc vết mổ hàng ngày cho bệnh nhân:

* **Kho cấp y lệnh**: BẮT BUỘC kê từ **Tủ trực Khoa 57 (`MediStockId = 810` - `TT_KCTCHCS`)** *(Tuyệt đối không kê từ Kho Dược 4209/4210)*.
* **Bộ thông số DTO bắt buộc (`InPatientPresCreateSDO` / `OutPatientPresCreateList`)**:
  - `IsCabinet = true` (Đơn tủ trực).
  - `IsExpend = true` (**BẮT BUỘC** bật cờ Hao phí tiêu hao để không tính trùng hoặc xuất nhầm viện phí).
  - `MedicineUseFormId = 25` (Đường dùng: *Dùng ngoài*, `HtuText = "Dùng ngoài"`).
  - `Tutorial = "thay băng"` (Hướng dẫn thực hiện).
  - `PrescriptionTypeId = 1` (Đơn nội trú).
* **Danh mục dung dịch & vật tư thay băng thường quy**:
  1. 🧴 **Povidone 10% 125ml** (Dung dịch sát khuẩn Betadine):
     - Mã thuốc: **`TH.POVI008`** (ID: `17385`) | Đơn vị: `Chai` | Số lượng: `1` | Hướng dẫn: `"thay băng"`.
  2. 💧 **Muối rửa Natri clorid 0.9% 500ml** (Nước muối rửa vô khuẩn):
     - Mã thuốc: **`TH.NATR047`** (ID: `27127`) | Đơn vị: `Chai` | Số lượng: `1` | Hướng dẫn: `"thay băng"`.
  3. 🩹 **Vật tư tiêu hao kèm theo** (Gạc củ ấu, gạc miếng vô khuẩn, băng cuộn, băng dính):
     - Kê từ danh mục `Materials` thuộc Tủ trực `810` với cờ `IsExpend = true`, `Tutorial = "thay băng"`.

---

## 8. PHÂN HỆ 4: CHỈ ĐỊNH CẬN LÂM SÀNG & ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG (BM02426)

### 8.1. Chỉ Định Đường Máu Mao Mạch Tại Giường (`HisGlucoseBedsideAssigner.exe`)
* **Mã dịch vụ**: `BM02426` (Service ID: `6217`)
* **Phòng thực hiện**: `931` (Phòng Tiểu Phẫu Nhà Q) / `531` (P289 Khoa CTCH)
* **Khung giờ hỗ trợ**: `06:00`, `11:00`, `17:00`, `21:00`, hoặc giờ tùy chỉnh.
* **Tự động liên kết Tờ điều trị**: Tự động dò tìm tờ điều trị trong ngày hoặc tạo tờ điều trị mới (`HIS_TRACKING`) khớp giờ chỉ định để đảm bảo y lệnh hợp lệ 100%.
* **Kích hoạt 1-Click**: `Chay_ChiDinh_BM02426.bat` hoặc dòng lệnh:
  ```powershell
  .\HisGlucoseBedsideAssigner.exe -p "0003969449,0003298895" -time "06:00,11:00,17:00,21:00" -date "2026-08-25"
  ```

### 8.2. Theo Dõi & Đôn Đốc Tiến Độ Cận Lâm Sàng (`HisClsCtchTracker.exe`)
* Theo dõi trực quan trạng thái cận lâm sàng: 🔴 **Chưa xử lý** (chưa có kết quả) / 🟡 **Đang xử lý** / 🟢 **Đã hoàn thành**.
* Lọc nhanh theo Buồng bệnh, Phân loại (Xét nghiệm, CĐHA, Siêu âm, TDCN, GPB).
* 1-Click xuất báo cáo Excel (CSV) và Copy bản tin giao ban dán Zalo/Viber.

### 8.3. Bảng Quy Tắc Bilan Tiền Phẫu & Cảnh Báo An Toàn Bắt Buộc:
1. ⚠️ **Bơm xi măng thân đốt sống (BXM / Kyphoplasty / Vertebroplasty):** BẮT BUỘC phải có kết quả **Đo mật độ xương (DEXA - T-score)** kèm MRI cột sống trước khi thông qua mổ.
2. 🫀 **Chỉ định Siêu âm tim (Echocardiography) trước mổ:**
   - **BN > 60 tuổi:** BẮT BUỘC có Siêu âm tim.
   - **BN > 50 tuổi có tiền sử tim mạch / THA / ĐTĐ / NMCT cũ:** BẮT BUỘC có Siêu âm tim.
3. 👁️ **Khám chuyên khoa Mắt (Soi đáy mắt):**
   - Áp dụng cho bệnh nhân phẫu thuật có **tư thế nằm sấp** (Cột sống ngực, thắt lưng, giải ép, CĐCS, BXM) có **tiền sử Đái tháo đường**.
   - Mục đích sàng lọc bệnh võng mạc đái tháo đường, dự phòng thiếu máu thị thần kinh.

### 8.4. Luồng Tự Động Hóa 1-Click: Bí Danh "Thợ Cho Đường Huyết" (HisDiabetesOrchestrator)
* **Bí danh đặc quyền**: `"thợ cho đường huyết"` (Hoặc `"tho cho duong huyet"`).
* **Mục tiêu**: Bác sĩ chỉ cần gửi ảnh báo cáo đường huyết của điều dưỡng (hoặc bảng text) kèm lời gọi bí danh, Agent Antigravity tự động trích xuất và thực thi 100% cả 3 tác vụ y lệnh cho toàn bộ bệnh nhân mà **không cần hỏi lại hay bắt bác sĩ chỉnh sửa gì thêm**:
  1. **Bước 1 (Tờ điều trị)**: Gọi `HisTrackingCreator.exe` ghi nhận kết quả ĐMMM lúc 17h, 21h, 6h và ghi y lệnh tiêm insulin.
  2. **Bước 2 (Chỉ định CLS BM02426)**: Gọi `HisGlucoseBedsideAssigner.exe` chỉ định xét nghiệm đường máu mao mạch tại giường theo từng mốc giờ (mốc 06:00 tự động tính sang ngày hôm sau).
  3. **Bước 3 (Kê đơn Insulin)**: Gọi `HisAutoPrescribe.exe --batch` (hoặc CLI) kê đơn thuốc tiêm Insulin từ **Tủ trực Khoa 57 (`MediStockId = 810`)**.
* **Hai Quy Tắc Cốt Lõi Bắt Buộc Ghi Nhớ**:
  - 🔍 **Quy tắc 1 (Đối chiếu Bệnh nhân)**: Mặc định lọc, đối chiếu bệnh nhân đang nằm điều trị nội trú tại **Khoa Chấn thương Chỉnh hình & Cột sống (`DEPARTMENT_ID = 57`)**. Nếu không tìm thấy bệnh nhân tại Khoa 57, báo lại ngay cho Bác sĩ.

### 8.5. Từ Điển Mã Dịch Vụ & Phòng Thực Hiện Chuẩn Khoa CTCH & Cột Sống (Khoa 57) - Ground Truth Lâm Sàng:
| STT | Phân Loại / Phòng Thực Hiện | Tên Kỹ Thuật Chuẩn | Mã DV (`SERVICE_CODE`) | Service ID | Đối Tượng |
|:---:|:---|:---|:---:|:---:|:---:|
| **1** | **XN Huyết Học Tế Bào** (`1772`) | Tổng phân tích tế bào máu ngoại vi (máy laser) | `BM00110` | `5745` | BHYT (`1`) |
| | *(Phòng XN Huyết Học Tế Bào)* | Máu lắng (bằng máy tự động) (ESR) | `BM00138` | `5686` | BHYT (`1`) |
| **2** | **XN Đông Máu** (`626`) | Thời gian Prothrombin (PT / TQ) bằng máy tự động | `BM00531` | `5713` | BHYT (`1`) |
| | *(Phòng Xét Nghiệm Đông Máu)* | Thời gian APTT (TCK) bằng máy tự động | `BM260527.52` | `63622` | BHYT (`1`) |
| | | Định lượng Fibrinogen bằng máy tự động | `BM00542` | `5716` | BHYT (`1`) |
| **3** | **XN Truyền Máu** (`1464`) | Định nhóm máu hệ ABO, Rh(D) (kỹ thuật Gelcard tự động) | `BM01700` | `5783` | BHYT (`1`) |
| **4** | **XN Sinh Hóa** (`410`) | Định lượng CRP (C-Reactive Protein) | `BM02180` | `5995` | BHYT (`1`) |
| | *(Phòng Xét Nghiệm Sinh Hóa)* | Định lượng Urê [Máu] | `BM02304` | `5923` | BHYT (`1`) |
| | | Định lượng Creatinin (máu) *(bắt buộc trước tiêm cản quang)* | `BM01361` | `5934` | BHYT (`1`) |
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

### 8.6. Quy Tắc Bắt Buộc: Gom Nhóm Y Lệnh Xét Nghiệm Tránh Nhân Bản Ống Máu (Specimen & Tube Bundling Rule):
* ⚠️ **Nguyên nhân cốt lõi**: Mỗi một `SERVICE_REQ` khi được tạo sẽ sinh ra **một mã barcode / tem phiếu xét nghiệm riêng biệt** trên hệ thống LIS. Điều dưỡng buồng bệnh sẽ dựa vào số lượng barcode để dán tem và lấy số ống nghiệm / bệnh phẩm tương ứng.
* 🚨 **Cảnh báo lỗi nghiêm trọng**: Nếu lặp vòng lặp tạo lẻ từng xét nghiệm sinh hóa, huyết học hoặc vi sinh thành nhiều `SERVICE_REQ` riêng biệt $\rightarrow$ Hệ thống in ra $N$ tem barcode khác nhau $\rightarrow$ **Bệnh nhân sẽ bị lấy $N$ ống máu/mẫu bệnh phẩm riêng biệt**, gây đau đớn, lãng phí ống nghiệm và quá tải phòng xét nghiệm.
* 🎯 **Quy tắc Gom Y Lệnh Chuẩn Lâm Sàng (1 Request = 1 Ống / 1 Bệnh Phẩm / 1 Phòng Tiếp Nhận)**:
  1. 🟣 **Ống EDTA (Nắp tím) - Phòng 1772 (Huyết học Tế bào)**: Gom CTM (`5745`) + Máu lắng (`5686`) vào chung **1 `SERVICE_REQ`** $\rightarrow$ Lấy 1 ống máu EDTA.
  2. 🔵 **Ống Citrate (Nắp xanh lam) - Phòng 626 (Đông máu)**: Gom PT/TQ (`5713`) + APTT/TCK (`63622`) + Fibrinogen (`5716`) vào chung **1 `SERVICE_REQ`** $\rightarrow$ Lấy 1 ống máu Citrate.
  3. 🔴 **Ống Serum/Heparin (Nắp đỏ/vàng) - Phòng 410 (Sinh hóa)**: Gom toàn bộ Ure (`5923`) + Creatinin (`5934`) + AST (`5834`) + ALT (`5833`) + Glucose (`5864`) + Điện giải đồ (`5853`) + CRP (`5995`) vào chung **1 `SERVICE_REQ`** $\rightarrow$ Lấy 1 ống máu Sinh hóa.
  4. 🟡 **Ống Miễn Dịch (Nắp vàng/đỏ) - Phòng 871 (Virus Miễn dịch)**: Gom HIV (`6020`) + HBsAg (`6135`) + HCV (`34801`) vào chung **1 `SERVICE_REQ`** $\rightarrow$ Lấy 1 ống máu Miễn dịch.
  5. 🧪 **Lọ Nước Tiểu - Phòng 566 (Nước tiểu)**: Tổng phân tích nước tiểu 10 thông số (`5950`) $\rightarrow$ 1 `SERVICE_REQ` lấy 1 lọ nước tiểu.
  6. 🧫 **Bộ Bệnh Phẩm Vi Sinh / Cấy Máu / Dịch Mủ - Phòng 4374 (Vi khuẩn - Vi nấm)**: BẮT BUỘC gom Cấy vi khuẩn định danh (`6054`) + Đính kèm KSD (`38374`) vào chung **1 `SERVICE_REQ`** $\rightarrow$ Để chung 1 mã bệnh phẩm và không phát sinh chi phí thừa.
  7. 🫁 **Bộ Ống Chuyên Dụng QuantiFERON - Phòng 9645 (Chuyển BV Phổi TW)**: `36522` $\rightarrow$ 1 `SERVICE_REQ` riêng (Đối tượng Yêu Cầu `43`).
  8. 🖥️ **Chẩn Đoán Hình Ảnh CLVT - Phòng 17549 (Tiếp đón CLVT Nội trú)**: Gom các kỹ thuật CLVT chỉ định cùng đợt (CLVT Phổi `58181` + CLVT CSTL `58191`) vào chung **1 `SERVICE_REQ`**.

---

## 9. PHÂN HỆ 5: BÁO CÁO GIAO BAN CA TRỰC & BILAN PHẪU THUẬT HẬU PHẪU

Công cụ chuyên dụng `HospitalShiftReporter.exe` (Mã nguồn: `HospitalShiftReporter.cs`):
1. **Bệnh nhân vào khoa ca trực**: Query `V_HIS_DEPARTMENT_TRAN` theo `DEPARTMENT_ID == 57` và `DEPARTMENT_IN_TIME` trong ca trực.
2. **Bệnh nhân mổ về ca trực**: Query `V_HIS_SERVICE_REQ` có `SERVICE_REQ_TYPE_ID == 6` và `FINISH_TIME` trong ngày trực, kết hợp đối chiếu diễn biến hậu phẫu trên `V_HIS_TRACKING`.
3. **Bệnh nhân dự kiến mổ**: Rà soát đầy đủ Bilan CTM, Đông máu, Sinh hóa, Nhóm máu, DEXA, Siêu âm tim, Khám mắt.
4. **Bệnh nhân truyền máu**: Query `V_HIS_SERE_SERV` dịch vụ máu/hồng cầu, trích xuất HGB/HCT trước và sau truyền.

---

## 10. PHÂN HỆ 6: KÝ SỐ ĐIỆN TỬ & MỜI BÁC SĨ KÝ (EMR SIGN)

* **Loại tài liệu**: `DOCUMENT_TYPE_ID = 7` (Tờ điều trị).
* **Ký chính (`NumOrder = 1`)**: Bác sĩ điều trị `034727` (Ths.BS Nguyễn Hữu Sâm).
* **Mời ký phối hợp (`NumOrder = 2`)**: Bác sĩ `ndh2` (BS Nguyễn Đức Hoàng).
* **Endpoint EMR**: `http://192.168.7.239:1415/`

---

## 11. PHÂN HỆ 7: CHỈ ĐỊNH & BIÊN BẢN HỘI CHẨN CHUYÊN KHOA (DEBATE DIAGNOSTIC & CONSULTATION)

### 11.1. Kiến Trúc Phân Hệ & Module Giao Diện:
* **Module HIS Client**: `HIS.Desktop.Plugins.DebateDiagnostic` (Form: `FormDebateDiagnostic`).
* **Mục đích lâm sàng**: Tạo yêu cầu mời hội chẩn liên chuyên khoa (Tạo hình thẩm mỹ, Tim mạch, Nội tiết, Hô hấp, Huyết học...) hoặc hội chẩn toàn viện, thông qua chẩn đoán bệnh án, đồng thời tự động cập nhật diễn biến vào Tờ điều trị và sinh biểu mẫu trích biên bản hội chẩn.
* **Cơ chế lưu trữ Backend MOS**:
  - Thực thể chính: `MOS.EFMODEL.DataModels.HIS_DEBATE`
  - Danh sách bác sĩ tham gia: `MOS.EFMODEL.DataModels.HIS_DEBATE_USER` (`IS_PRESIDENT = 1` - Chủ tọa, `IS_SECRETARY = 1` - Thư ký).
  - Bác sĩ được mời: `MOS.EFMODEL.DataModels.HIS_DEBATE_INVITE_USER`.

### 11.2. Danh Sách Endpoint MOS & Phương Thức Gọi:
| Phương thức | Endpoint URI | Mục đích & Nghiệp vụ |
| :--- | :--- | :--- |
| `POST` | `api/HisDebate/CreateAutoTracking` | **Tạo mới hội chẩn** đồng thời tự động sinh/liên kết Tờ điều trị (`HIS_TRACKING`) |
| `POST` | `api/HisDebate/UpdateWithTracking` | **Cập nhật nội dung hội chẩn** và đồng bộ nội dung diễn biến tờ điều trị |
| `GET` | `api/HisDebate/GetView` | Tra cứu danh sách & chi tiết biên bản hội chẩn theo `TREATMENT_ID` / `TREATMENT_CODE` |
| `GET` | `api/HisDebateInviteUser/Get` | Tra cứu danh sách bác sĩ được mời tham gia hội chẩn |

### 11.3. Cấu Trúc DTO Chuẩn Khi Tạo Hội Chẩn (`HIS_DEBATE`):
```json
{
  "ID": 0,
  "TREATMENT_ID": 7070733,
  "ICD_CODE": "S91.0",
  "ICD_NAME": "Vết thương phức tạp mu bàn chân (P)",
  "DEPARTMENT_ID": 57,
  "DEBATE_TIME": 20260826101400,
  "REQUEST_LOGINNAME": "034727",
  "REQUEST_USERNAME": "NGUYỄN HỮU SÂM",
  "TREATMENT_TRACKING": "Bn nam chẩn đoán Vết thương phức tạp mu bàn chân (P). hiện tại vết lóc da có diện da hoại tử đen vạt ngược kích thước 4cm, xin ý kiến CK tạo hình xét nhận bệnh nhân điều trị / phối hợp",
  "TREATMENT_FROM_TIME": 20260819230800,
  "TREATMENT_TO_TIME": null,
  "TREATMENT_METHOD": "",
  "LOCATION": "Khoa Chấn thương Chỉnh hình và Cột sống",
  "REQUEST_CONTENT": "ck tạo hình thẩm mỹ",
  "DISCUSSION": "xin ý kiến CK tạo hình xét nhận bệnh nhân điều trị / phối hợp",
  "CONTENT_TYPE": 1,
  "HIS_DEBATE_USER": [
    {
      "ID": 0,
      "LOGINNAME": "hdc",
      "USERNAME": "HÀ ĐỨC CƯỜNG",
      "IS_PRESIDENT": 1,
      "IS_SECRETARY": null
    },
    {
      "ID": 0,
      "LOGINNAME": "034727",
      "USERNAME": "NGUYỄN HỮU SÂM",
      "IS_PRESIDENT": null,
      "IS_SECRETARY": 1
    }
  ]
}
```

### 11.4. Biểu Mẫu In & Định Danh EMR:
* **Mã mẫu in**: `Mps000019` - `HC_TrichBienBanHoiChan___CT_001.xlsx` (**Trích biên bản hội chẩn**).
* **Mã loại văn bản EMR (`EMR_DOCUMENT_TYPE_CODE`)**: `17` (Phục vụ ký số & lưu hồ sơ bệnh án điện tử).

### 11.5. Công Cụ CLI Tự Động Hóa 1-Click:
```powershell
# Tạo chỉ định hội chẩn chuyên khoa:
.\.agents\skills\his-clinical-operations\scripts\HisDebateCreator.exe --treatment 000007070917 --specialist "ck tạo hình thẩm mỹ" --summary "Vết lóc da hoại tử đen vạt ngược 4cm, xin ý kiến CK tạo hình phối hợp"
```

---

## 12. QUY CHUẨN LÂM SÀNG BẮT BUỘC (MÃ ICD-10 5 KÝ TỰ & MÔ TẢ CĐHA)

### 11.1. Quy Tắc Mã ICD-10 (Hệ 5 Ký Tự Mới - `IS_ACTIVE = 1`):
Bệnh viện đã chuyển đổi toàn bộ danh mục sang hệ 5 ký tự chi tiết theo Bộ Y tế. Các mã 3-4 ký tự cũ đã ngừng hoạt động (`IS_ACTIVE = 0`):
* **Viêm gan virus B:** Dùng **`B18.19`** hoặc **`B18.10`** *(Cấm dùng `B18.1`)*.
* **Viêm cơ mủ / Áp xe cơ đùi mông:** Dùng **`M60.05`** *(Cấm dùng `M60.0`)*.
* **Thoái hóa / Thoát vị đĩa đệm:** Dùng `M47.00†`, `M47.8`, `M51.2`, `M51.3`.
* **Xẹp lún thân đốt sống:** Dùng `M48.50`.
* **Gãy xương chi:** Dùng `S62.11` (Gãy xương tháp/thang), `S42.00` (Gãy xương đòn).
* **Quy tắc Code:** Khi query `api/HisIcd/Get`, luôn lọc `IS_ACTIVE == 1`.

### 11.2. Quy Tắc Trích Xuất Chẩn Đoán Hình Ảnh (Đích Danh Tầng & Vị Trí):
* **Cột sống:** Nêu rõ từng tầng đốt sống bị **xẹp cấp (có phù tủy xương)** và **xẹp cũ**:
  - *Chuẩn:* `Xẹp cấp L2, L3, L5 (phù tủy xương)`, `Xẹp cũ T12`, `Trượt L4 ra trước độ I kèm hẹp ống sống`, `Rách vòng xơ đĩa đệm L4/5`.
  - *Cấm kỵ:* `Xẹp lún phù tủy xương các đốt sống thắt lưng` (mơ hồ, nguy cơ can thiệp nhầm tầng).
* **Xương khớp chi:** Ghi rõ từng xương, vị trí đoạn gãy (1/3 trên, giữa, dưới), độ di lệch, tình trạng đứt gân:
  - *Chuẩn:* `Gãy di lệch xương tháp và xương thang cổ tay trái`, `Gãy 1/3 giữa xương đòn phải`, `Đứt cũ gân gấp sâu ngón 3, 4, 5 bàn tay phải`.

---

## 12. BẢNG TỔNG HỢP 15+ SAI LẦM & BÀI HỌC XƯƠNG MÁU (GOTCHAS MATRIX)

| STT | Hiện tượng / Báo lỗi | Nguyên nhân gốc rễ | Giải pháp chuẩn xác 100% |
| :---: | :--- | :--- | :--- |
| **1** | `IOException: process cannot access file` khi đọc log token | HIS client đang ghi `LogSystem.txt` với lock | Mở file bằng `FileStream` với `FileShare.ReadWrite` |
| **2** | `api/HisTracking/Create` trả về `Success: false` | Gửi JSON phẳng hoặc gửi trực tiếp `HIS_TRACKING` | Bắt buộc đóng gói vào `MOS.SDO.HisTrackingSDO` kèm `WorkingRoomId` |
| **3** | Không tìm thấy trường `TREATMENT_INSTRUCTION` | Tên thuộc tính trong DTO là `MEDICAL_INSTRUCTION` | Sử dụng đúng trường `MEDICAL_INSTRUCTION` |
| **4** | Suất ăn chỉ vào được 1 ngày dù truyền mảng nhiều ngày | Backend MOS chỉ xử lý `InstructionTimes[0]` | Dùng vòng lặp gọi API tách biệt cho từng ngày |
| **5** | Đơn thuốc bị từ chối / thiếu thông tin | Thiếu hướng dẫn dùng (`Tutorial`) và chia cữ | Điền đầy đủ `Tutorial` chuẩn lâm sàng và chia cữ S/Tr/C/T theo từ điển |
| **6** | Lỗi `KhongCoThongTinPhongLamViec` khi chạy CLI độc lập | Token chưa được gán phòng làm việc trên server | Gọi `api/Token/UpdateWorkInfo` (`WorkInfoSDO`) ngay sau khi login |
| **7** | Tràn bộ nhớ / treo lệnh khi query buồng giường | Filter `HisTreatmentBedRoom` không gán ID điều trị | Luôn lọc bằng `TREATMENT_IDs = [treatmentId]` và `IS_IN_ROOM = true` |
| **8** | Lỗi định dạng giờ tờ điều trị | Truyền sai 14 chữ số | Chuẩn hóa `yyyyMMddHHmmss` (VD: `20260825080000`) |
| **9** | Chỉ định CLS bị x4 hoặc nhân đôi y lệnh | Client tự động retry khi gặp timeout mạng | Tuyệt đối không retry mù quáng; query lại `V_HIS_SERVICE_REQ` để kiểm tra |
| **10**| Bị từ chối mã ICD khi lưu bệnh án | Dùng mã 3-4 ký tự đã deactive (`B18.1`, `M60.0`) | Luôn dùng mã 5 ký tự đang hoạt động (`B18.19`, `M60.05`) có `IS_ACTIVE == 1` |
| **11**| Thiếu bilan quan trọng khi thông qua mổ xẹp đốt sống | Quên rà soát mật độ xương DEXA | Bắt buộc cảnh báo bổ sung DEXA (T-score) cho ca dự kiến BXM |
| **12**| Quên chỉ định Siêu âm tim tiền phẫu | Bệnh nhân > 60 tuổi hoặc có tiền sử tim mạch | Tự động quét tuổi > 60 hoặc bệnh nền để cảnh báo Siêu âm tim |
| **13**| Thiếu khám mắt trước mổ cột sống | Bệnh nhân ĐTĐ mổ tư thế nằm sấp | Cảnh báo yêu cầu Hội chẩn Mắt (soi đáy mắt) dự phòng thiếu máu thị thần kinh |
| **14**| Không tìm thấy DLL khi biên dịch độc lập trên máy mới | Thiếu cơ chế nạp Assembly tự động | Gắn hook `AssemblyResolve` trỏ về thư mục gốc và `ReferencedAssemblies` |
| **15**| Merge conflict Git khi đồng bộ giữa Laptop và Máy Bàn | Khác biệt lịch sử commit hoặc track file binary | Cấu hình `.gitignore` chuẩn, dùng `sync_push.bat` / `sync_pull.bat` |
| **16**| `error CS0006: Metadata file MOS.EFMODEL.dll could not be found` | `MOS.EFMODEL.dll` nằm ở thư mục gốc còn các DLL khác nằm ở `ReferencedAssemblies\` | Dùng `/lib:.,ReferencedAssemblies` và copy dự phòng `MOS.EFMODEL.dll` vào `ReferencedAssemblies\` |
| **17**| Đề xuất thuốc bệnh nền khi hồ sơ chưa có mã ICD chính thức (VD: Tăng huyết áp) | Bác sĩ cấp cứu chỉ ghi nhận tiền sử mà chưa gán mã ICD chính/phụ trên HIS | Xem kỹ hồ sơ; nếu chưa có mã ICD chính thức, **phải đề xuất Bác sĩ bổ sung chẩn đoán bệnh nền** trước khi kê đơn |
| **18**| Kê thuốc nhóm PPI (Nexium, Pantoloc...) bị xuất toán BHYT | Hồ sơ bệnh án thiếu chẩn đoán phụ bệnh lý dạ dày (`K29`, `K25`, `K21`...) | Nếu chưa có chẩn đoán dạ dày, **chưa được tự ý kê PPI** mà phải **đề xuất Bác sĩ bổ sung chẩn đoán phụ** trước khi kê đơn |
| **19**| Bệnh nhân mới vào viện buổi chiều bị chậm thuốc sáng hôm sau | Thuốc kê từ Kho Dược (4209/4210/804) sau 14h chiều thì 10h sáng mai mới duyệt trả | **Chia đơn làm 2 phần**: Phần không có tủ trực kê từ Kho Dược; Phần có trong Tủ trực 57 (810) **đề xuất sáng mai BS vào kê tủ trực dùng ngay cữ sáng** |
| **20**| Tự đoán / Suy diễn sai họ tên Bác sĩ / PTV từ mã login viết tắt | Login trên HIS là mã viết tắt (VD `tmd2` = Trịnh Minh Đức), Agent tự đoán chữ cái dẫn đến bịa tên bác sĩ | **Tuyệt đối KHÔNG tự đoán tên từ mã viết tắt**. Bắt buộc đọc từ trường chữ ký/chức danh, EMR Signature, `ACS_USER` hoặc đối chiếu bảng danh mục Bác sĩ. Ghi nhớ: `tmd2` / `tmd` = **BS TRỊNH MINH ĐỨC**. |
| **21**| Bệnh nhân bị chọc nhiều mũi / lấy thừa nhiều ống máu khi chỉ định xét nghiệm | Chỉ định tách rời các xét nghiệm cùng nhóm mẫu/phòng thành nhiều `SERVICE_REQ` riêng biệt khiến LIS sinh nhiều barcode | **BẮT BUỘC gom nhóm các xét nghiệm cùng phòng/loại mẫu vào chung 1 `SERVICE_REQ`** (1 phiếu = 1 mã ống máu EDTA/Citrate/Serum/Nước tiểu/Vi sinh). Cấy vi khuẩn và Kháng sinh đồ `BMDK01` phải chung 1 phiếu. |
| **22**| Bộ xét nghiệm Đông máu bị từ chối / sai phòng thực hiện | Chọn mã gộp chung `BM00119` gửi phòng 1772 | **BẮT BUỘC tách lẻ 3 xét nghiệm thành phần gửi về Phòng 626 (Đơn nguyên Đông máu)**: `BM00542` (Fibrinogen - ID 5716), `BM00531` (PT/TQ - ID 5713), `BM260527.52` (APTT/TCK - ID 63622) tại Phòng 626. |
| **23**| Định nhóm máu Gelcard bị từ chối khi chỉ định | Gửi nhầm về các phòng 1772, 1773, 1771, 2993 | Chỉ định đích danh **`BM01700`** (ID `5783` - Định nhóm máu ABO, Rh(D) Gelcard tự động) gửi về **Phòng 1464**. |
| **24**| Chỉ định HbA1c BHYT bị MOS chặn (lỗi điều kiện thanh toán) | Thiếu `SERVICE_CONDITION_ID` theo quy định BHYT | Bắt buộc truyền **`SERVICE_CONDITION_ID = 4723`** khi chỉ định **`BM01429`** (ID `5870` - Định lượng HbA1c) tại **Phòng 410**. |
| **25**| Siêu âm tim nội trú chỉ định sai phòng | Gửi nhầm về phòng 16987 (P112 Viện Tim Mạch) | Chỉ định **`BM00201`** (ID `5569` - Siêu âm Doppler tim, van tim) gửi đích danh về **Phòng 1715 (Siêu âm tim nội trú)** kèm ghi chú `"điều dưỡng đưa bằng cáng - cs ii"`. |
| **26**| Đo mật độ xương DEXA chọn sai mã dịch vụ | Chọn mã 1 vị trí `BM08084` (ID 160) | Chỉ định đúng mã **`BM08085`** (ID `161` - DEXA 2 vị trí) gửi về **Phòng 6462 (P202 Nhà K2)** kèm ghi chú `"điều dưỡng đưa bằng cáng - cs ii"`. |
| **27**| Mã dịch vụ Ure và Điện giải đồ bị lệch danh mục | Chọn các mã tạm hoặc mã ngoài danh mục thường quy | Urê máu chọn **`BM02304`** (ID `5923`), Điện giải đồ Na/K/Cl chọn **`BM00132`** (ID `5853`), gửi về **Phòng 410**. |
| **28**| Tạo Hội chẩn chuyên khoa bị thiếu Tờ điều trị hoặc sai DTO | Gọi `api/HisDebate/Create` phẳng hoặc thiếu `HIS_DEBATE_USER` | **BẮT BUỘC gọi `api/HisDebate/CreateAutoTracking`** với DTO `HIS_DEBATE` chứa `CONTENT_TYPE = 1`, `DEBATE_TIME` (`yyyyMMddHHmmss`), và mảng `HIS_DEBATE_USER` gắn cờ `IS_PRESIDENT = 1` (Chủ tọa), `IS_SECRETARY = 1` (Thư ký). Biểu mẫu trích biên bản là `Mps000019` (EMR Type 17). |
| **29**| Kê vật tư tiêu hao thay băng bị tính sai viện phí hoặc nhầm kho | Kê từ Kho Dược (4209/4210) hoặc quên bật cờ hao phí | **BẮT BUỘC kê từ Tủ trực Khoa 57 (`MediStockId = 810`)**, đặt `IsCabinet = true`, `IsExpend = true`, `MedicineUseFormId = 25` (*Dùng ngoài*) và `Tutorial = "thay băng"`. Các mục chuẩn: **Povidone 10% 125ml (`TH.POVI008` - ID 17385)** và **Muối rửa NaCl 0.9% 500ml (`TH.NATR047` - ID 27127)**. |
| **30**| Kê đơn Insulin (Actrapid/Lantus/Mixtard) từ Tủ Trực 810 bị từ chối lượng tồn kho | Truyền `Amount` là số nguyên UI (VD: `8.0`) khiến MOS hiểu là 8 lọ (8000 IU) | **Tỷ lệ quy đổi bắt buộc: `Amount = UI / 1000.0m`** (VD `8 UI` = `0.0080 lọ`, `9 UI` = `0.0090 lọ`). `MedicineTypeId = 27727` (`TH.ACTR004`), `MediStockId = 810`, `MedicineUseFormId = 15` (*Tiêm*), các cữ tiêm `MORNING`/`NOON`/`EVENING` = chuỗi 2 chữ số (VD `"08"`), `IsExpend = false`. |
| **31**| Agent rơi vào vòng lặp thử-sai (trial-and-error loop) quá lâu khi API backend từ chối | Tự ý viết script test liên tiếp khi API trả `Success: false` âm thầm làm BS phải chờ đợi | **Quy tắc giới hạn 2 lần (Max 2 Attempts)**: Nếu sau 2 lần gọi API mà backend từ chối không rõ mã lỗi, Agent PHẢI DỪNG NGAY LẬP TỨC. Báo cáo minh bạch các tác vụ ĐÃ XONG (Tờ điều trị, Chỉ định CLS) và bàn giao lại để BS thao tác nhanh trên UI, tuyệt đối không để ảnh hưởng tiến độ khám chữa bệnh. |

---

## 14. HƯỚNG DẪN BIÊN DỊCH & CHẠY CÔNG CỤ CLI TỨC THÌ

Khi di chuyển sang máy mới có .NET Framework (mặc định có trên mọi Windows 10/11):

### 14.1. Đường dẫn trình biên dịch C#:
`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

> 💡 **Lưu ý về thông báo C# 5:** Khi chạy `csc.exe`, hệ thống sẽ hiện thông báo bản quyền `...for C# 5... This compiler is provided as part of the Microsoft (R) .NET Framework...`. Đây là thông báo mặc định của Windows .NET Framework 4.8 (không phải lỗi). Thêm `/nologo` vào lệnh biên dịch để ẩn thông báo này.

### 14.2. Lệnh biên dịch chuẩn cho mọi công cụ:
```powershell
# Biên dịch HisTrackingCreator:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:HisTrackingCreator.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisTrackingCreator.cs

# Biên dịch HisDebateCreator (Hội chẩn chuyên khoa):
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:HisDebateCreator.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisDebateCreator.cs

# Biên dịch HisGlucoseBedsideAssigner:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HisGlucoseBedsideAssigner.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisGlucoseBedsideAssigner.cs

# Biên dịch HospitalShiftReporter:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HospitalShiftReporter.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HospitalShiftReporter.cs
```

### 14.3. Cơ chế Nạp Assembly Động (Assembly Resolve Hook):
```csharp
AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
{
    string folderPath = AppDomain.CurrentDomain.BaseDirectory;
    string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
    string path1 = Path.Combine(folderPath, name);
    if (File.Exists(path1)) return Assembly.LoadFrom(path1);
    string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
    if (File.Exists(path2)) return Assembly.LoadFrom(path2);
    return null;
};
```

---

## 15. CƠ CHẾ ĐỒNG BỘ TRI THỨC 1-CLICK GIỮA MÁY BÀN & LAPTOP

Dự án đã được trang bị sẵn 2 kịch bản tự động hóa 1-click:

* **Đẩy cập nhật lên Git (`sync_push.bat`)**:
  - Tự động kiểm tra git, stage toàn bộ tài liệu Markdown, Skills, mã nguồn C#, Script BAT/PS1.
  - Tự động loại trừ các file binary DLL/Cache/DB theo `.gitignore`.
  - Thực hiện commit và push lên `origin main`.
* **Kéo cập nhật về máy (`sync_pull.bat`)**:
  - 1-click tự động kéo toàn bộ tri thức, kỹ năng, cẩm nang mới nhất từ máy kia về.

---
*Tài liệu Cẩm Nang Hợp Nhất được biên soạn, xác thực và lưu giữ tự động bởi AI Agent.*
