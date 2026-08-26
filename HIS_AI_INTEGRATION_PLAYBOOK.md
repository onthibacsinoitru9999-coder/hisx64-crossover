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
11. [Quy Chuẩn Lâm Sàng Bắt Buộc (Mã ICD-10 5 Ký Tự & Mô Tả CĐHA)](#11-quy-chuẩn-lâm-sàng-bắt-buộc-mã-icd-10-5-ký-tự--mô-tả-cđha)
12. [Bảng Tổng Hợp 15+ Sai Lầm & Bài Học Xương Máu (Gotchas Matrix)](#12-bảng-tổng-hợp-15-sai-lầm--bài-học-xương-máu-gotchas-matrix)
13. [Hướng Dẫn Biên Dịch & Chạy Công Cụ CLI Tức Thì](#13-hướng-dẫn-biên-dịch--chạy-công-cụ-cli-tức-thì)
14. [Cơ Chế Đồng Bộ Tri Thức 1-Click Giữa Máy Bàn & Laptop](#14-cơ-chế-đồng-bộ-tri-thức-1-click-giữa-máy-bàn--laptop)

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
  - 💉 **Quy tắc 2 (Kho Thuốc Insulin)**: Đơn thuốc Insulin theo dõi đường huyết **BẮT BUỘC chỉ định từ Kho Tủ Trực Khoa 57 (`MediStockId = 810` - `TT_KCTCHCS`)**, **TUYỆT ĐỐI KHÔNG kê từ Kho Dược (4209/4210)**.
* **Quy chuẩn ký hiệu viết tắt Insulin của Điều dưỡng**:
  - **`R`** (VD: **`6R`**, **`8R`**, **`4R`**): là **Actrapid** (Insulin Regular tác dụng nhanh). Ví dụ `6R` = `6 đơn vị Actrapid`.
  - **`L`** (VD: **`10L`**, **`12L`**, **`14L`**): là **Lantus** (Insulin Glargine nền kéo dài). Ví dụ `10L` = `10 đơn vị Lantus`.
  - **`M`** (VD: **`8M`**, **`10M`**, **`12M`**): là **Mixtard** (Insulin hỗn hợp / Mix). Ví dụ `8M` = `8 đơn vị Mixtard`.
* **Bộ công cụ cốt lõi**:
  - Script điều phối: [`HisDiabetesOrchestrator.ps1`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/HisDiabetesOrchestrator.ps1)
  - Parser thị giác: [`parse_glucose_image.py`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/parse_glucose_image.py)
  - Công cụ kê đơn hàng loạt: [`HisAutoPrescribe.exe --batch`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/HisAutoPrescribe.exe)
  - Công cụ chỉ định ĐMMM: [`HisGlucoseBedsideAssigner.exe`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-clinical-operations/scripts/HisGlucoseBedsideAssigner.exe)
  - Công cụ tạo tờ điều trị: [`HisTrackingCreator.exe`](file:///d:/his/his-x64-28-11fix%20GDYK/his-x64/.agents/skills/his-clinical-operations/scripts/HisTrackingCreator.exe)

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

## 11. QUY CHUẨN LÂM SÀNG BẮT BUỘC (MÃ ICD-10 5 KÝ TỰ & MÔ TẢ CĐHA)

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

---

## 13. HƯỚNG DẪN BIÊN DỊCH & CHẠY CÔNG CỤ CLI TỨC THÌ

Khi di chuyển sang máy mới có .NET Framework (mặc định có trên mọi Windows 10/11):

### 13.1. Đường dẫn trình biên dịch C#:
`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

> 💡 **Lưu ý về thông báo C# 5:** Khi chạy `csc.exe`, hệ thống sẽ hiện thông báo bản quyền `...for C# 5... This compiler is provided as part of the Microsoft (R) .NET Framework...`. Đây là thông báo mặc định của Windows .NET Framework 4.8 (không phải lỗi). Thêm `/nologo` vào lệnh biên dịch để ẩn thông báo này.

### 13.2. Lệnh biên dịch chuẩn cho mọi công cụ:
```powershell
# Biên dịch HisTrackingCreator:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:HisTrackingCreator.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisTrackingCreator.cs

# Biên dịch HisGlucoseBedsideAssigner:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HisGlucoseBedsideAssigner.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HisGlucoseBedsideAssigner.cs

# Biên dịch HospitalShiftReporter:
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:HospitalShiftReporter.exe /lib:.,ReferencedAssemblies /r:System.dll,System.Core.dll,System.Data.dll,Inventec.Core.dll,Inventec.Token.ClientSystem.dll,Inventec.Token.Core.dll,Inventec.Common.Adapter.dll,Inventec.Common.WebApiClient.dll,HIS.Desktop.LocalStorage.ConfigSystem.dll,HIS.Desktop.LocalStorage.LocalData.dll,HIS.Desktop.ApiConsumer.dll,MOS.Filter.dll,MOS.SDO.dll,MOS.EFMODEL.dll HospitalShiftReporter.cs
```

### 13.3. Cơ chế Nạp Assembly Động (Assembly Resolve Hook):
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

## 14. CƠ CHẾ ĐỒNG BỘ TRI THỨC 1-CLICK GIỮA MÁY BÀN & LAPTOP

Dự án đã được trang bị sẵn 2 kịch bản tự động hóa 1-click:

* **Đẩy cập nhật lên Git (`sync_push.bat`)**:
  - Tự động kiểm tra git, stage toàn bộ tài liệu Markdown, Skills, mã nguồn C#, Script BAT/PS1.
  - Tự động loại trừ các file binary DLL/Cache/DB theo `.gitignore`.
  - Thực hiện commit và push lên `origin main`.
* **Kéo cập nhật về máy (`sync_pull.bat`)**:
  - 1-click tự động kéo toàn bộ tri thức, kỹ năng, cẩm nang mới nhất từ máy kia về.

---
*Tài liệu Cẩm Nang Hợp Nhất được biên soạn, xác thực và lưu giữ tự động bởi AI Agent.*
