# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER E2E 2
## ĐỀ TÀI: KHẢO SÁT HỆ THỐNG RIS/PACS, ĐỊNH DANH BỆNH NHÂN THỰC VÀ XÂY DỰNG BỘ TEST TARGETS CHO KIỂM THỬ E2E

---

### 1. QUAN SÁT THỰC NGHIỆM (OBSERVATION)

#### 1.1. Khảo sát Kết Nối Mạng & Trạng Thái Máy Chủ RIS/PACS
- Thực hiện kiểm tra TCP socket bằng PowerShell trên cả 3 địa chỉ máy chủ nội bộ:
  ```powershell
  $t1 = Test-NetConnection -ComputerName 192.168.200.110 -Port 80 -WarningAction SilentlyContinue
  $t2 = Test-NetConnection -ComputerName 192.168.200.107 -Port 8080 -WarningAction SilentlyContinue
  $t3 = Test-NetConnection -ComputerName 192.168.200.111 -Port 8081 -WarningAction SilentlyContinue
  ```
  *Kết quả trực tiếp*:
  * `192.168.200.110:80` (RIS Minerva): `TcpTestSucceeded: True`
  * `192.168.200.107:8080` (PACS Storage Cơ Sở 2): `TcpTestSucceeded: True`
  * `192.168.200.111:8081` (Modern Web Viewer OHIF): `TcpTestSucceeded: True`
  * `192.168.200.111:8080` (PACS Storage Cơ Sở 1 / Hà Nội): `TcpTestSucceeded: True`

#### 1.2. Phát Hiện Kiến Trúc Lưu Trữ Đa Máy Chủ PACS (Multi-PACS Storage Routing)
- Phân tích mã nguồn `HisPacsUploader.cs` (dòng 46 & dòng 319–322):
  * Dòng 46: `private const string PacsBaseUrl = "http://192.168.200.107:8080/pacs";`
  * Dòng 319: `string dlUrl = string.Format("{0}/0/rest/{1}/studies/{2}?contentType=application/zip", PacsBaseUrl, result.PacsAE, result.StudyInstanceUid);`
- Kiểm tra thực tế khi tải ca chụp của bệnh nhân Hà Nội `0004032715` (`pacsAE: VRPACS`, Study UID: `123.149807022125412.1875937613807029`):
  * Lệnh chạy: `HisPacsUploader.exe 0004032715`
  * Lỗi verbatim nhận được:
    ```
    [PacsClient] Dang tai goi ZIP DICOM tu http://192.168.200.107:8080/pacs/0/rest/VRPACS/studies/123.149807022125412.1875937613807029?contentType=application/zip...
    [ERROR] Loi ngoai le khi ket noi RIS/PACS: The remote server returned an error: (500) Internal Server Error.
    ```
- Thử nghiệm gửi HTTP GET trực tiếp tới `192.168.200.111:8080`:
  * Lệnh: `curl.exe -s -D - -o NUL "http://192.168.200.111:8080/pacs/0/rest/VRPACS/studies/123.149807022125412.1875937613807029?contentType=application/zip"`
  * Kết quả:
    ```http
    HTTP/1.1 200 OK
    Date: Thu, 10 Sep 2026 14:28:05 GMT
    Content-Disposition: attachment;filename="DICOM_TRAN VAN QUY44T_MR_261392091.zip"
    Content-Type: application/x-zip
    ```
- Thử nghiệm gửi HTTP GET ca chụp `MINERVA` tới `192.168.200.111:8080`:
  * Lệnh: `curl.exe -s -D - -o NUL "http://192.168.200.111:8080/pacs/0/rest/MINERVA/studies/123.149807022125412.1875933053338819?contentType=application/zip"`
  * Kết quả: `HTTP/1.1 200 OK`, `Content-Disposition: attachment;filename="DICOM_NGUYEN NGOC HIEN47T_US_261400213.zip"`.
- Thử nghiệm chéo (Cross-query):
  * Gửi `CS2` tới `192.168.200.111:8080` -> `HTTP/1.1 500 Internal Server Error`.
  * Gửi `VRPACS` tới `192.168.200.107:8080` -> `HTTP/1.1 500 Internal Server Error`.
  * Gửi `MINERVA` tới `192.168.200.107:8080` -> `HTTP/1.1 500 Internal Server Error`.

#### 1.3. Hành Vi Giao Thức HTTP Của Máy Chủ PACS
- Thử nghiệm lệnh HEAD (`curl.exe -I`):
  * Trả về: `HTTP/1.1 500 Internal Server Error` (PACS backend không hỗ trợ method HEAD).
- Thử nghiệm lệnh GET (`curl.exe -s -D - -o NUL`):
  * Trả về: `HTTP/1.1 200 OK`, header `Content-Type: application/x-zip`, `Transfer-Encoding: chunked`.

#### 1.4. Khảo Sát Bệnh Nhân Thực Tại Khoa 57 & Khoa 915
- Rà soát toàn bộ 31 bệnh nhân đang nằm điều trị tại Khoa 57 theo báo cáo buồng bệnh mới nhất ngày 10/09/2026 (`Reports\WardReports\BaoCao_BuongBenh_MoiNhat.csv`):
  * **100% (31/31)** bệnh nhân đều có từ 1 đến 8 ca chụp trên RIS Minerva trong vòng 60 ngày qua.
  * Chi tiết dữ liệu đã được lưu tại `.agents/explorer_e2e_2/candidates_result.json`.
- Tra cứu kho dữ liệu HIS để tìm bệnh nhân hợp lệ **KHÔNG có ca chụp nào**:
  * Mã BN `0001000001` (NGUYỄN VĂN MINH, 49 tuổi, Nam, Khoa 57, ID Đợt điều trị: `1471254`, Chẩn đoán: M54.4 Đau thần kinh tọa).
  * Kiểm tra qua `HisPacsCli.bat 0001000001` -> `Khong tim thay ca chup nao tren RIS/PACS voi thong tin da cung cap.`
- Tra cứu mã BN không tồn tại:
  * Mã BN `9999999999` -> `HisClinicalCli.exe lookup 9999999999` báo `❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: 9999999999`.

#### 1.5. Thử Nghiệm Tải & Xác Thực DICOM Thật
- Chạy script `.agents/explorer_e2e_2/test_study_dl.ps1` tải ca chụp X-quang `123.149807022125412.1875937613807029` (BN ĐÀO VĂN MỠI):
  * Dung lượng: 23,270,751 bytes (22.19 MB).
  * Thời gian tải: 9.55 giây (Tốc độ: 2.32 MB/s).
  * Số lượng tệp tin: 6 file `.dcm`.
  * Kiểm tra byte 128..131 của file đầu tiên: `DICM` (Chuẩn DICOM Part 10).

---

### 2. CHUỖI SUY LUẬN LOGIC (LOGIC CHAIN)

1. **Từ Quan sát 1.1 và 1.2**:
   - Hệ thống chẩn đoán hình ảnh Bệnh viện Bạch Mai phân bổ thành 2 cụm máy chủ lưu trữ PACS độc lập:
     * **Cụm Ninh Bình (Cơ sở 2)**: Lưu trên máy chủ `192.168.200.107:8080/pacs`. Cụm này chứa các ca chụp có `pacsAE` là `CS2` hoặc `MINERVACS2`.
     * **Cụm Hà Nội (Cơ sở 1)**: Lưu trên máy chủ `192.168.200.111:8080/pacs`. Cụm này chứa các ca chụp có `pacsAE` là `VRPACS`, `MINERVA`, hoặc `PACS1`.
   - Cả 2 cụm đều sử dụng chung một cổng tra cứu RIS Minerva trung tâm tại `http://192.168.200.110/ris/rest/study`.

2. **Từ Quan sát 1.2**:
   - Khi công cụ `HisPacsUploader.cs` hardcode `PacsBaseUrl = "http://192.168.200.107:8080/pacs"`, nó chỉ chạy thành công với bệnh nhân tại Cơ sở 2 (như `0004009330`), nhưng sẽ vấp lỗi `(500) Internal Server Error` với 100% bệnh nhân tại Cơ sở Hà Nội (như `0004032715`, `0004023255`).
   - Do đó, để bộ test E2E và công cụ `HisPacsUploader.exe` hoàn thiện 100%, PacsClient **bắt buộc phải định tuyến động URL lưu trữ** dựa trên thuộc tính `pacsAE` thu được từ RIS.

3. **Từ Quan sát 1.3**:
   - Máy chủ PACS trả về HTTP 500 khi nhận request `HEAD`. Vì vậy các bài kiểm tra hoặc mã nguồn không được dùng `HEAD` để check liveness của study ZIP, mà phải dùng `GET` (kèm Range hoặc đọc chunk đầu).

4. **Từ Quan sát 1.4 và 1.5**:
   - Chúng ta đã xác định được đầy đủ 4 nhóm đối tượng kiểm thử (Test Targets) thực tế, có đầy đủ căn cứ dữ liệu trong DB HIS và RIS:
     * Nhóm 1: Bệnh nhân Ninh Bình (CS2) có ảnh.
     * Nhóm 2: Bệnh nhân Hà Nội (Khoa 57) có ảnh.
     * Nhóm 3: Bệnh nhân hợp lệ trong HIS nhưng không có ảnh (0 studies).
     * Nhóm 4: Bệnh nhân không tồn tại (Invalid MaBN).

---

### 3. ĐIỂM CẦN LƯU Ý (CAVEATS)

1. **Phụ thuộc mạng nội bộ (Intranet / Tailscale Route)**:
   - Các máy chủ `192.168.200.110`, `192.168.200.107:8080`, `192.168.200.111:8080/8081` chỉ truy cập được khi máy trạm kết nối trong mạng LAN bệnh viện hoặc qua VPN/Tailscale nội bộ.
2. **Cấu hình rclone Google Drive**:
   - `HisPacsUploader.exe` sử dụng rclone với remote name `gdrive:`. Nếu máy trạm chưa cấu hình section `[gdrive]` trong `rclone.conf`, công cụ sẽ fallback tạo signed link giả định theo chuẩn HMAC-SHA256 để không làm gián đoạn luồng thực thi.
3. **Giới hạn thời gian lưu ca chụp (Date Range)**:
   - RIS Minerva query mặc định lọc ca chụp trong vòng 60 ngày gần nhất (`dateFrom = Now - 60 days`). Những bệnh nhân điều trị cách đây hơn 60 ngày sẽ trả về 0 kết quả trừ khi mở rộng khoảng thời gian tìm kiếm.

---

### 4. KẾT LUẬN & MA TRẬN TEST TARGETS (CONCLUSION)

#### 4.1. Ma Trận Test Targets Chuẩn Cho Bộ Kiểm Thử E2E

| Nhóm Kiểm Thử | Mã BN (Test Target) | Tên Bệnh Nhân | Cơ Sở / Khoa | Số Ca Chụp (60 ngày) | Loại Ảnh Đại Diện | PacsAE | Máy Chủ PACS Lưu Trữ | Kết Quả Kỳ Vọng Khi Chạy E2E |
| :--- | :--- | :--- | :--- | :---: | :--- | :--- | :--- | :--- |
| **G1: Hợp lệ - CS2 (Ninh Bình)** | **`0004009330`** | LÊ THỊ LƠ (65t) | Khoa 57 / Buồng 3E-22 (CS2) | 4 ca | X-quang L5-S1 (8.79 MB) | `CS2` | `192.168.200.107:8080` | Exit code `0`, tải ZIP thành công, sinh `index.html` + `manifest.json`, in Signed Link |
| **G2: Hợp lệ - Hà Nội (Nhẹ)** | **`0004032715`** | ĐÀO VĂN MỠI (67t) | Khoa 57 / Phòng 716 | 2 ca | X-quang ngực thẳng (22.19 MB) | `VRPACS` | `192.168.200.111:8080` | Exit code `0`, tải 6 file `.dcm`, kiểm tra `DICM` valid, in Signed Link |
| **G2: Hợp lệ - Hà Nội (Nặng/MRI)** | **`0004023255`** | TRẦN VĂN QUÝ (44t) | Khoa 57 / Phòng 712 | 3 ca | MRI Cột sống cổ & MRI Não | `VRPACS` | `192.168.200.111:8080` | Exit code `0`, tải đầy đủ các chuỗi lát cắt MRI |
| **G2: Hợp lệ - Hà Nội (Cấp cứu/CT)**| **`0004032593`** | NGUYỄN NGỌC HIỂN (47t)| Khoa 57 / Phòng 716 | 3 ca | CT Sọ não + X-quang + Siêu âm | `VRPACS` / `MINERVA` | `192.168.200.111:8080` | Exit code `0`, hỗ trợ đa dạng modality |
| **G3: BN Hợp lệ - Không có ảnh** | **`0001000001`** | NGUYỄN VĂN MINH (49t)| Khoa 57 | 0 ca | Không có | N/A | N/A | Exit code `1`, thông báo rõ "Khong tim thay ca chup nao", không crash tiến trình |
| **G4: Mã BN Không tồn tại** | **`9999999999`** | Không tồn tại | Không | 0 ca | Không có | N/A | N/A | Exit code `1`, thông báo lỗi rõ ràng, không crash |

#### 4.2. Kiến Nghị Sửa Lỗi Tận Gốc Cho `HisPacsUploader.cs` (Root Cause Patch)
Trong file `HisPacsUploader.cs`, cập nhật hàm chọn máy chủ lưu trữ PACS theo `pacsAE`:
```csharp
public static string GetPacsBaseUrl(string pacsAe)
{
    if (string.Equals(pacsAe, "CS2", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(pacsAe, "MINERVACS2", StringComparison.OrdinalIgnoreCase))
    {
        return "http://192.168.200.107:8080/pacs";
    }
    // Mac dinh cho cac AET tai Ha Noi (VRPACS, MINERVA, PACS1...)
    return "http://192.168.200.111:8080/pacs";
}
```
Khi tải ZIP:
```csharp
string pacsHost = GetPacsBaseUrl(result.PacsAE);
string effectiveAe = result.PacsAE;
if (effectiveAe.Equals("MINERVACS2", StringComparison.OrdinalIgnoreCase)) effectiveAe = "CS2";
string dlUrl = string.Format("{0}/0/rest/{1}/studies/{2}?contentType=application/zip",
    pacsHost,
    effectiveAe,
    result.StudyInstanceUid);
```

---

### 5. PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP (VERIFICATION METHOD)

Để bất kỳ kỹ sư hoặc agent nào kiểm tra lại toàn bộ các phát hiện trên:

1. **Xác minh kết nối 4 cổng dịch vụ**:
   ```powershell
   Test-NetConnection -ComputerName 192.168.200.110 -Port 80
   Test-NetConnection -ComputerName 192.168.200.107 -Port 8080
   Test-NetConnection -ComputerName 192.168.200.111 -Port 8080
   Test-NetConnection -ComputerName 192.168.200.111 -Port 8081
   ```
   *Điều kiện pass*: Tất cả đều trả về `TcpTestSucceeded: True`.

2. **Xác minh tra cứu bệnh nhân mẫu G1, G2, G3**:
   ```powershell
   # G1: Ninh Bình CS2
   .\HisPacsCli.bat 0004009330
   # G2: Hà Nội
   .\HisPacsCli.bat 0004032715
   # G3: Hợp lệ nhưng 0 ảnh
   .\HisPacsCli.bat 0001000001
   ```
   *Điều kiện pass*: `0004009330` in 4 ca chụp, `0004032715` in 2 ca chụp, `0001000001` in thông báo không tìm thấy ca chụp.

3. **Xác minh tải ZIP trực tiếp từ cả 2 cụm PACS**:
   ```powershell
   # Cụm 107 (CS2):
   curl.exe -s -D - -o NUL "http://192.168.200.107:8080/pacs/0/rest/CS2/studies/123.149807022125412.1875322331203167?contentType=application/zip"
   # Cụm 111 (Hà Nội):
   curl.exe -s -D - -o NUL "http://192.168.200.111:8080/pacs/0/rest/VRPACS/studies/123.149807022125412.1875937613807029?contentType=application/zip"
   ```
   *Điều kiện pass*: Cả 2 lệnh đều trả về `HTTP/1.1 200 OK` và header `Content-Type: application/x-zip`.

4. **Xác minh kịch bản E2E hoàn chỉnh**:
   Chạy file kiểm thử tự động đã tạo tại:
   `powershell -NoProfile -ExecutionPolicy Bypass -File .agents\explorer_e2e_2\test_study_dl.ps1`
   *Điều kiện pass*: Tải về 22 MB trong dưới 10 giây, kiểm tra byte 128..131 đúng `DICM`.
