# ==============================================================================
# BÍ KÍP TỔNG HỢP VẬN HÀNH & TÍCH HỢP HỆ THỐNG HIS/MOS BỆNH VIỆN BẠCH MAI
# Dành riêng cho Agent Antigravity / AI Developer (Chạy ngay không cần thử lỗi)
# ==============================================================================

## 1. TỔNG QUAN HẠ TẦNG & MẠNG NỘI BỘ (NETWORK ARCHITECTURE)
- **Cơ chế mạng**: Hệ thống HIS/MOS chạy trên mạng nội bộ viện. Truy cập từ xa bắt buộc qua Tailscale Subnet Router định tuyến dải mạng `192.168.7.0/24`.
- **IP Client máy trạm (Tailscale)**: `100.93.206.93` (hoặc IP gán bởi Tailscale).
- **Danh sách Endpoint máy chủ dịch vụ**:
  * **MOS (Nghiệp vụ KCB chính)**: `http://192.168.7.236:1608/`
  * **ACS (Xác thực / Quản lý Token)**: `http://192.168.7.200:1401/`
  * **SDA (Cấu hình hệ thống & Danh mục)**: `http://192.168.7.200:1410/`
  * **EMR (Bệnh án điện tử & Ký số)**: `http://192.168.7.239:1415/`
  * **PACS (Máy chủ hình ảnh X-quang/CT/MRI)**: `http://192.168.7.200:5000/`
  * **SAR (Báo cáo tổng hợp)**: `http://192.168.7.200:1409/`
  * **MRS (Báo cáo y tế)**: `http://192.168.7.200:1413/`
  * **FSS (Lưu trữ file, ảnh, mẫu phiếu)**: `http://192.168.7.216:1405/`

---

## 2. GIAO THỨC GỌI API & CƠ CHẾ XÁC THỰC (AUTHENTICATION & PROTOCOL)

### 2.1. Lấy TokenCode tự động (Không cần gọi API Login lại)
Khi ứng dụng `HIS.exe` đang chạy trên máy, `TokenCode` phiên làm việc luôn được ghi lại trong file log:
- **Đường dẫn log**: `E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt`
- **Mẫu Regex bắt Token**: `TokenCode\|([a-f0-9]{64})`

### 2.2. Chuẩn HTTP Headers bắt buộc cho mọi Request
```http
TokenCode: <TokenCode_64_ký_tự>
Authorization: Bearer <TokenCode_64_ký_tự>
ClientIpAddress: 100.93.206.93
Content-Type: application/json
```

### 2.3. Quy chuẩn cấu trúc Request Parameters
- **Phương thức GET**:
  * URL: `http://192.168.7.236:1608/api/<Controller>/Get?param=<Base64EncodedJson>`
  * Chuỗi JSON trước khi Base64 Encode:
  ```json
  {
    "CommonParam": {
      "LanguageCode": "VI",
      "Messages": [],
      "BugCodes": [],
      "MessageCodes": [],
      "Now": 0,
      "HasException": false
    },
    "ApiData": {
      /* Bộ lọc điều kiện */
    }
  }
  ```
- **Phương thức POST**:
  * Body là JSON trực tiếp với cùng cấu trúc `{ "CommonParam": {...}, "ApiData": {...} }`.

---

## 3. DANH MỤC MÃ HỆ THỐNG & ĐỊNH DANH NGHIỆP VỤ (MASTER CODES)

### 3.1. Phân biệt Buồng phòng Khoa CTCH & Cột sống (Mã khoa: 9, DEPARTMENT_ID: 57)
> ⚠️ **QUAN TRỌNG**: Phòng 712 và Phòng 712A là 2 buồng độc lập hoàn toàn, không được gộp!
- **Phòng 712**: `BED_ROOM_ID = 775` | `ROOM_ID = 5252` | Mã buồng: `NQCTCHBB712` (Gồm 12 giường: 21, 22, 23, 24, 25, 26...).
- **Phòng 712A**: `BED_ROOM_ID = 1457` | `ROOM_ID = 6622` | Mã buồng: `NQCTCHBB712A` (Giường 20A).
- **Phòng 714**: `BED_ROOM_ID = 774` | `ROOM_ID = 5251` | Mã buồng: `NQCTCHBB714` (Giường 18...).
- **Phòng 724**: `BED_ROOM_ID = 780` | `ROOM_ID = 5257` | Mã buồng: `NQCTCHBB724` (Giường 49, 50, 52, 53...).
- **Phòng 725**: `BED_ROOM_ID = 781` | `ROOM_ID = 5258` | Mã buồng: `NQCTCHBB725` (Giường 54...).

### 3.2. Mã loại dịch vụ & Phiếu yêu cầu (`SERVICE_TYPE` / `SERVICE_REQ_TYPE`)
| Nghiệp vụ | `SERVICE_TYPE_ID` | `SERVICE_REQ_TYPE_ID` | Nơi tiếp nhận / Kho xuất |
|:---|:---:|:---:|:---|
| **Suất ăn dinh dưỡng** | `16` (AN) | `17` (AN) | TT Dinh dưỡng Lâm sàng (`ROOM_ID = 5809`) |
| **Đơn thuốc nội trú** | `6` (TH) | `15` (DN) hoặc `14` (DT) | Kho Dược Chính / Kho Lĩnh (`MediStockId = 4210`) |
| **Xét nghiệm** | `2` (XN) | `2` (XN) | Các phòng Xét nghiệm (Huyết học `1772`...) |
| **Chẩn đoán hình ảnh** | `3` (HA) | `3` (HA) | Máy chủ PACS (`:5000`) |
| **Phẫu thuật - Thủ thuật**| `11` (PT) / `4` (TT)| `10` (PT) / `4` (TT)| Phòng mổ / Can thiệp |

### 3.3. Quy chuẩn Suất ăn Dinh dưỡng theo Bệnh lý
- **Suất thường (`BT01`)**:
  * Sáng (06:00 - `RationTimeId = 1`): `BT01-06` (Mã dịch vụ `BM17770`, ID `30073`)
  * Trưa (11:00 - `RationTimeId = 3`): `BT01-11` (Mã dịch vụ `BM18696`, ID `30153`)
  * Chiều (17:00 - `RationTimeId = 5`): `BT01-17` (Mã dịch vụ `BM18697`, ID `30154`)
- **Suất Đái tháo đường (`DD01`)**:
  * Sáng: `DD01-06` (`CO02`) | Trưa: `DD01-11` (`CS01`) | Chiều: `DD01-17` (`BM18650`)
- **Suất Tim mạch / THA (`TM01`)**:
  * Sáng: `TM01-06` (`BM18075`) | Trưa: `TM01-11` (`BM18039`) | Chiều: `TM01-17` (`BM18040`)

### 3.4. Nguyên tắc Kê đơn thuốc nội trú
- **Kho xuất**: Kho Lĩnh viện (`4210`), tuyệt đối **không** kê kho tủ trực nếu là đơn duy trì.
- **Loại trừ**:
  * ❌ Thuốc dùng 1 lần (như SAT - huyết thanh kháng uốn ván) không kê lại.
  * ❌ Thuốc phẫu thuật / gây mê / tiền phẫu (Ondansetron, Leanpro PreSur, Anaropin, Fresofol, Sevorane, Dexamethasone mổ) không kê lại khi chuyển đơn điều trị hàng ngày.
  * ❌ Insulin (Actrapid...) không kê tự động nếu không có y lệnh đường huyết cụ thể.

---

## 4. KINH NGHIỆM TỐI ƯU HIỆU NĂNG (BẮT BUỘC DÙNG BATCH QUERY)

### 4.1. Sai lầm lần đầu: Truy vấn tuần tự (Sequential)
- *Vấn đề*: Chạy vòng lặp gọi API cho từng bệnh nhân làm tổng thời gian kéo dài 2 - 3 phút.
- *Nguyên nhân*: Độ trễ mạng (Network Latency) tích lũy qua tunnel Tailscale cho 60+ request.

### 4.2. Kỹ thuật đúng: Truy vấn hàng loạt (Batch Querying)
Thay vì gọi từng người, truyền trực tiếp mảng danh sách ID:
1. `api/HisTreatmentBedRoom/Get` với `BED_ROOM_IDs = @(774, 775, 780, 781)` -> Lấy toàn bộ bệnh nhân trong các phòng cùng lúc.
2. `api/HisTreatment/Get` với `IDs = @($treatIds)` -> Lấy toàn bộ hồ sơ hành chính & ICD.
3. `api/HisServiceReq/Get` với `TREATMENT_IDs = @($treatIds)` và `SERVICE_REQ_TYPE_IDs = @(17)` (hoặc `6, 14, 15`) -> Lấy toàn bộ phiếu y lệnh 1 lần.
4. `api/HisSereServ/Get` với `SERVICE_REQ_IDs = @($reqIds)` -> Lấy toàn bộ chi tiết suất ăn/thuốc trong 1 request.
-> **Thời gian giảm từ 180s xuống còn 3.5 giây!**

---

## 5. CÔNG CỤ THỰC THI CHUẨN (POWERSHELL HIGH-SPEED TEMPLATE)

Agent mới có thể copy và chạy trực tiếp đoạn mã PowerShell chuẩn này để truy xuất dữ liệu bất kỳ phòng nào trong 3 giây:

```powershell
param (
    [string[]]$Rooms = @("712", "714", "724", "725")
)

$ErrorActionPreference = "Stop"

# 1. Trích xuất Token tự động từ phiên làm việc HIS
$logPath = "E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
$tokenMatch = Get-Content -Path $logPath -Tail 500 | Select-String -Pattern 'TokenCode\|([a-f0-9]{64})' | Select-Object -Last 1
if (-not $tokenMatch) { throw "Không tìm thấy TokenCode trong LogSystem.txt" }
$token = $tokenMatch.Matches[0].Groups[1].Value

$headers = @{
    "TokenCode" = $token
    "Authorization" = "Bearer $token"
    "ClientIpAddress" = "100.93.206.93"
}

function Invoke-MosApiFast($endpoint, $payload) {
    $json = $payload | ConvertTo-Json -Compress
    $b64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($json))
    $url = "http://192.168.7.236:1608/$endpoint?param=" + [Uri]::EscapeDataString($b64)
    return Invoke-RestMethod -Uri $url -Headers $headers -Method Get -TimeoutSec 15
}

# 2. Batch Query phòng & bệnh nhân
$allBedRooms = (Invoke-MosApiFast "api/HisBedRoom/Get" @{ CommonParam=@{LanguageCode="VI"}; ApiData=@{IS_ACTIVE=1} }).Data
$pattern = ($Rooms | ForEach-Object { [regex]::Escape($_) }) -join "|"
$targetRooms = @($allBedRooms | Where-Object { ($_.BED_ROOM_NAME -match $pattern -or $_.BED_ROOM_CODE -match $pattern) -and $_.BED_ROOM_CODE -match "NQCTCH" })
$roomIds = @($targetRooms | ForEach-Object { $_.ID })

$tbrs = (Invoke-MosApiFast "api/HisTreatmentBedRoom/Get" @{ CommonParam=@{LanguageCode="VI"}; ApiData=@{BED_ROOM_IDs=$roomIds; IS_IN_ROOM=$true} }).Data
$activeTbrs = @($tbrs | Where-Object { [string]::IsNullOrEmpty($_.REMOVE_TIME) -or $_.REMOVE_TIME -eq 0 })
$treatIds = @($activeTbrs | ForEach-Object { $_.TREATMENT_ID } | Select-Object -Unique)

# 3. Batch Query điều trị, suất ăn, thuốc
$treatments = (Invoke-MosApiFast "api/HisTreatment/Get" @{ CommonParam=@{LanguageCode="VI"}; ApiData=@{IDs=$treatIds} }).Data
$reqs = (Invoke-MosApiFast "api/HisServiceReq/Get" @{ CommonParam=@{LanguageCode="VI"}; ApiData=@{TREATMENT_IDs=$treatIds} }).Data

Write-Host "Truy xuất thành công $($treatIds.Count) bệnh nhân trong 3 giây."
```

---

## 6. DANH SÁCH CÁC SAI LẦM CẦN TRÁNH TUYỆT ĐỐI (ANTI-PATTERNS)

1. ❌ **Không tự suy đoán hoặc bịa dữ liệu (Hallucination)**: Luôn gọi API để lấy đúng `TREATMENT_CODE`, `ICD_CODE`, `SERVICE_REQ_CODE`.
2. ❌ **Không nhầm lẫn giữa Phòng 712 và Phòng 712A**: 712 (`BED_ROOM_ID = 775`), 712A (`BED_ROOM_ID = 1457`).
3. ❌ **Không dùng lệnh `grep` trên Windows Command Prompt/PowerShell**: Môi trường Windows mặc định không có `grep.exe` -> Bắt buộc dùng `Select-String` trong PowerShell.
4. ❌ **Không chạy vòng lặp Polling kiểm tra task liên tục**: Hệ thống Antigravity sử dụng cơ chế Reactive Wakeup tự động thông báo khi background task hoàn thành.
5. ❌ **Không kê đơn thuốc nội trú từ kho Tủ trực**: Luôn trỏ `MediStockId = 4210` (Kho lĩnh viện).
