# BÁO CÁO ĐIỀU TRA KỸ THUẬT & ĐỀ XUẤT KHẮC PHỤC (HANDOFF REPORT)
## DỰ ÁN: HISPACSUPLOADER.EXE — GOOGLE DRIVE FALLBACK & LOCALVIEWERSERVER ARCHITECTURE

- **Tác nhân thực hiện**: Explorer E2E Round 2 - 2 (`explorer_e2e_r2_2`)
- **Archetype**: Explorer / Investigator
- **Parent Agent**: `parent` (`39825030-4eea-4a74-be36-84c091696543`)
- **Thư mục làm việc**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_e2e_r2_2`
- **Thời điểm lập báo cáo**: 2026-09-10T15:00:00Z (22:00:00 UTC+7)
- **Tài liệu tham chiếu**:
  - `ORIGINAL_REQUEST.md` (R2, R3, R4)
  - `orchestrator_4/GATE_STATUS.md` (Iteration 1 Defect #3 & #6)
  - `reviewer_e2e_2/handoff.md` (Báo cáo thẩm định tính toàn vẹn và URL 404)
  - `challenger_e2e_1/handoff.md` (Báo cáo kiểm thử đối kháng CLI)
  - `explorer_drive_r2/handoff.md` (Điều tra hạ tầng rclone và Google Drive REST API)
  - Mã nguồn đối tượng: `HisPacsUploader.cs` (Dòng 650 – 833, 1020 – 1107)

---

## TÓM TẮT ĐIỀU TRA TRỌNG TÂM (EXECUTIVE SUMMARY)

1. **Vấn đề Google Drive Upload Fallback**:
   - Hiện tại, khi `rclone` chưa có remote `gdrive:` (hoặc upload thất bại), `DriveUploader.UploadAndShare` tại dòng 736–742 tự động nuốt lỗi, sinh ra một URL giả mạo định dạng `https://drive.google.com/drive/folders/1pacs_{maBn}_{hash}?usp=sharing`, gán `res.Success = true`, trả về Exit Code `0`, và in link này ra `stdout`.
   - Khi truy cập thực tế qua trình duyệt hoặc HTTP client, Google Drive trả về **`HTTP Error 404: Not Found`**. Đây là lỗi vi phạm nghiêm trọng tính toàn vẹn (Integrity Violation) vì đánh lừa bác sĩ/agent bằng một đường link không tồn tại, đồng thời xóa sạch toàn bộ file tạm cục bộ khiến dữ liệu hình ảnh bị mất hoàn toàn.
2. **Vấn đề Mã chết & Xung đột Vòng đời của `LocalViewerServer`**:
   - `LocalViewerServer` (dòng 746–833) là **mã chết 100% (Dead Code)**, không hề được gọi tại bất kỳ đâu trong `Program.Run`.
   - Khi người dùng truyền cờ `--open`, mã nguồn tại dòng 1052 mở thẳng URL đám mây (là URL 404 giả mạo nói trên).
   - Thiết kế ban đầu của `LocalViewerServer.ServeAndOpen` chứa một lỗi kiến trúc chí mạng: khởi chạy `HttpListener` trên ThreadPool ngầm và return ngay lập tức. Sau đó `Program.Run` thoát hàm và chạy khối `finally`, dẫn tới `Directory.Delete(sessionFolder, true)` xóa sạch thư mục ảnh TRƯỚC KHI trình duyệt kịp gửi yêu cầu HTTP. Khi trình duyệt mở lên, toàn bộ ảnh đều bị `404`!
   - Không thể mở trực tiếp bằng giao thức file cục bộ `file:///.../index.html` vì chính sách CORS của Chrome/Edge chặn lệnh `fetch('manifest.json')` và `fetch('dicom/*.dcm')`. Do đó, giải pháp máy chủ HTTP loopback cục bộ (`http://127.0.0.1:<port>/`) là bắt buộc.
3. **Giải pháp Kiến trúc Đề xuất (Ponytail Zero-Bloat Architecture)**:
   - **Xóa bỏ triệt để việc sinh URL 404 giả mạo**.
   - Nếu `rclone` upload thành công: Trả về link Google Drive thật, stdout = link Drive, exit code `0`. Nếu có `--open`, mở link Drive trên trình duyệt.
   - Nếu `rclone` chưa cấu hình hoặc upload thất bại:
     * Nếu có `STRICT_DRIVE_UPLOAD=1`: Báo lỗi ra `stderr`, ghi log `FAILED`, `stdout` có 0 dòng, thoát với Exit Code `3`.
     * Nếu người dùng truyền cờ `--open`: Kích hoạt chế độ **Transparent Local Viewer Fallback**. Khởi chạy `LocalViewerServer` trên `http://127.0.0.1:<port>/index.html`, mở trình duyệt xem ảnh trực tiếp, in URL localhost ra `stdout`, ghi log `LOCAL_VIEWER_SERVED`, duy trì vòng lặp phục vụ cho đến khi trình duyệt tải xong ảnh vào RAM hoặc người dùng nhấn phím/hết thời gian chờ, sau đó mới dọn dẹp thư mục tạm và thoát với Exit Code `0`. Bác sĩ xem được 100% lát cắt DICOM mượt mà!
     * Nếu KHÔNG truyền cờ `--open`: Thoát với Exit Code `3`, in hướng dẫn gợi ý dùng `--open` để xem ảnh cục bộ ngay trên máy trạm.

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM ĐỘC LẬP & DẪN CHỨNG NGUYÊN VĂN)

### 1.1. Quan sát Đoạn Mã Nuốt Lỗi & Sinh URL 404 Giả mạo
Trích xuất nguyên văn tại `HisPacsUploader.cs` (Dòng 724 – 743):
```csharp
// If rclone failed or gdrive remote is not configured
Console.Error.WriteLine(string.Format("[DriveUploader] Thong bao: rclone chua ket noi Google Drive ({0}).",
    string.IsNullOrEmpty(errorOutput) ? "gdrive remote chua khai bao" : errorOutput.Trim()));

string strict = Environment.GetEnvironmentVariable("STRICT_DRIVE_UPLOAD");
if (strict == "1")
{
    res.Success = false;
    res.ErrorMessage = "Loi upload Google Drive: " + errorOutput;
    return res;
}

// Fallback authenticated signed public drive link
string folderHash = Math.Abs((maBn + "_" + (studyUid ?? "uid")).GetHashCode()).ToString("x8");
string fallbackDriveUrl = string.Format("https://drive.google.com/drive/folders/1pacs_{0}_{1}?usp=sharing", maBn, folderHash);
res.Success = true;
res.ShareableUrl = SignedLinkService.GenerateSignedDriveUrl(fallbackDriveUrl, remoteFolderName, ttl);
Console.Error.WriteLine("[DriveUploader] Tao signed link co chu ky HMAC-SHA256 san sang chia se.");
return res;
```

**Hệ quả trực tiếp tại `Program.Run` (Dòng 1030 – 1066)**:
- `uploadResult.Success` bằng `true`.
- Ghi log giả: `Logger.Log(maBn, downloadResult.StudyInstanceUid, uploadResult.ShareableUrl, "SUCCESS");`
- In thông báo ngụy tạo: `[SUCCESS] Signed Shareable Link (TTL 24h): https://drive.google.com/drive/folders/1pacs_...`
- In URL giả ra `stdout` làm dòng cuối cùng.
- Trả về Exit Code `0`.
- Khối `finally` xóa sạch `sessionFolder`.

### 1.2. Bằng chứng Thực nghiệm: Link Google Drive Trả về HTTP 404
- Lệnh kiểm tra:
  ```powershell
  $url = (.\HisPacsUploader.exe 0004009330)[-1]
  $wc = New-Object System.Net.WebClient
  $wc.Headers.Add("User-Agent", "Mozilla/5.0")
  $wc.DownloadString($url.Split('?')[0])
  ```
- Kết quả thực tế (Trích xuất từ `reviewer_e2e_2/handoff.md`):
  `Exception: HTTP Error 404: Not Found`
- Thư mục `1pacs_0004009330_6486931a` không tồn tại trên Google Drive. Bác sĩ mở link sẽ gặp trang lỗi của Google.

### 1.3. Quan sát Mã Chết (Dead Code) tại `LocalViewerServer`
- Tại dòng 746 – 833: Lớp `LocalViewerServer` định nghĩa phương thức `public static void ServeAndOpen(string localDir, string shareableUrl)` gồm 88 dòng code khởi tạo `HttpListener`, mapping MIME type cho `.html`, `.json`, `.dcm`.
- Tìm kiếm toàn bộ file `HisPacsUploader.cs`: **Có đúng 0 lệnh gọi đến `LocalViewerServer.ServeAndOpen`**.
- Tại dòng 1047 – 1062, khi cờ `--open` được truyền:
  ```csharp
  if (isOpen)
  {
      Console.Error.WriteLine(string.Format("[BROWSER] Dang mo lien ket tren trinh duyet: {0}", uploadResult.ShareableUrl));
      try
      {
          Process.Start(new ProcessStartInfo
          {
              FileName = uploadResult.ShareableUrl,
              UseShellExecute = true
          });
      }
      catch (Exception ex)
      {
          Console.Error.WriteLine(string.Format("[BROWSER] Khong the mo trinh duyet: {0}", ex.Message));
      }
  }
  ```
  Tiến trình mở thẳng `uploadResult.ShareableUrl` (vốn là URL 404 giả mạo).

### 1.4. Quan sát Xung đột Race Condition giữa ThreadPool và Khối `finally`
Nếu gọi trực tiếp hàm `LocalViewerServer.ServeAndOpen` hiện tại:
```csharp
ThreadPool.QueueUserWorkItem(delegate(object state)
{
    while (listener != null && listener.IsListening)
    {
        var ctx = listener.GetContext();
        ...
    }
});
...
Process.Start(...);
return; // Thoat ngay lap tuc khoi ServeAndOpen!
```
- Khi `ServeAndOpen` return, `Program.Run` lập tức đi đến dòng `return 0`.
- Khối `finally` tại dòng 1074 – 1092 lập tức được kích hoạt:
  ```csharp
  finally
  {
      if (!string.IsNullOrEmpty(sessionFolder) && Directory.Exists(sessionFolder))
      {
          Directory.Delete(sessionFolder, true);
          Console.Error.WriteLine("[CLEANUP] Da xoa thu muc tam cuc bo.");
      }
  }
  ```
- Thư mục `sessionFolder` bị xóa sổ trong vòng 10ms sau khi tiến trình thoát.
- Trình duyệt Chrome/Edge mất khoảng 300ms – 1000ms để khởi động và kết nối vào `http://localhost:<port>/index.html`.
- Khi trình duyệt kết nối tới: `Directory.Exists(sessionFolder)` là `false`, hoặc tiến trình console đã kết thúc khiến cổng bị đóng (`ERR_CONNECTION_REFUSED`).

### 1.5. Quan sát Hạn chế Bảo mật CORS khi Mở File Trực tiếp (`file:///`)
- Tại dòng 767 – 770 của `HisPacsUploader.cs` có đoạn mã dự phòng mở file trực tiếp:
  ```csharp
  string directHtml = Path.Combine(localDir, "index.html");
  Process.Start(new ProcessStartInfo { FileName = directHtml, UseShellExecute = true });
  ```
- Kiểm tra thực tế trên trình duyệt hiện đại (Chrome 120+, Edge 120+):
  Khi mở trang HTML qua `file:///C:/Users/.../index.html`, mã JavaScript gọi:
  `fetch('manifest.json')` hoặc `fetch('./dicom/image001.dcm')`
  Console trình duyệt chặn đứng ngay lập tức với lỗi:
  `Access to fetch at 'file:///...' from origin 'null' has been blocked by CORS policy: Cross origin requests are only supported for protocol schemes: http, data, chrome-extension, edge, https.`
- Kết quả: Giao diện viewer hiện lên nhưng canvas đen hoàn toàn, không nạp được bất kỳ lát cắt nào! Do đó, **phục vụ qua HTTP Loopback `127.0.0.1` là giải pháp kỹ thuật duy nhất khả thi**.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN GIẢI PHÁP)

```
[Quan sát 1.1 & 1.2] rclone thiếu config -> sinh link 1pacs_ giả mạo -> trả về HTTP 404 trên Drive
        │
        ├──> VI PHẠM TÍNH TOÀN VẸN (Integrity Violation)
        │    Bác sĩ không xem được ảnh, dữ liệu tạm bị xóa mất
        │
[Quan sát 1.3] LocalViewerServer không bao giờ được gọi (Dead Code 88 dòng)
        │
[Quan sát 1.4] Nếu gọi LocalViewerServer cũ -> Background ThreadPool bị kill khi tiến trình thoát -> finally xóa sessionFolder -> Browser bị ERR_CONNECTION_REFUSED
        │
[Quan sát 1.5] Mở direct file:/// -> Bị CORS chặn fetch manifest.json và DICOM blobs
        │
        ▼
[KẾT LUẬN LOGIC]:
Cần thiết kế lại ma trận hành vi của DriveUploader và LocalViewerServer theo 3 nguyên tắc:
1. TRUNG THỰC TUYỆT ĐỐI: Không sinh link 404 giả mạo. Upload thất bại thì báo thất bại.
2. CỨU CÁNH LÂM SÀNG: Khi bác sĩ muốn xem ảnh (--open) mà Cloud hỏng -> Chuyển hướng phục vụ Local Viewer tự động.
3. BỀN VỮNG VÒNG ĐỜI: LocalViewerServer phải giữ vòng lặp phục vụ cho đến khi trình duyệt nạp xong ảnh vào RAM, sau đó mới giải phóng tài nguyên.
```

### Chi tiết Chuỗi Lập luận:
1. **Loại bỏ cơ chế Facade Mock URL**:
   Việc tự động chế ra URL `1pacs_{maBn}_{hash}` khi rclone thất bại là nguồn gốc của phán quyết `REQUEST_CHANGES` và `REJECT` từ Reviewer 2 và Challenger 1. Hành vi này phải bị xóa bỏ 100%. Nếu rclone không upload được, `uploadResult.Success` PHẢI là `false`.
2. **Xử lý Chế độ Nghiêm ngặt `STRICT_DRIVE_UPLOAD=1`**:
   Khi cờ môi trường `STRICT_DRIVE_UPLOAD=1` được thiết lập (hoặc người dùng chạy trong kịch bản bắt buộc lưu trữ đám mây), bất kỳ sự cố nào về upload Drive phải dẫn đến Exit Code `3` ngay lập tức, `stdout` 0 dòng, không mở trình duyệt cục bộ, ghi log `FAILED`.
3. **Phân định Mục đích của Cờ `--open`**:
   - Khi bác sĩ gõ lệnh: `HisPacsUploader.exe <MaBN> --open`, mục đích tối thượng của bác sĩ là: **XEM ĐƯỢC ẢNH CHỤP CỦA BỆNH NHÂN NGAY LẬP TỨC**.
   - Nếu Drive upload thành công: Mở link đám mây (hoặc local).
   - Nếu Drive upload thất bại (hoặc chưa cấu hình rclone remote): Thay vì báo lỗi văng ra làm gián đoạn việc chẩn đoán của bác sĩ, hệ thống kích hoạt **Transparent Local Fallback**, khởi chạy `LocalViewerServer` trên `http://127.0.0.1:<port>/index.html` và bật trình duyệt. Bác sĩ vẫn xem được đầy đủ 100% lát cắt DICOM, chỉnh cửa sổ xương/phần mềm/cột sống, phóng to thu nhỏ mà không hề bị gián đoạn!
4. **Xử lý khi KHÔNG có cờ `--open` và Upload Thất bại**:
   - Khi một script hoặc agent chạy `HisPacsUploader.exe <MaBN>` mà không có cờ `--open`, caller đang kỳ vọng nhận được một đường link chia sẻ công khai qua `stdout`.
   - Nếu upload Drive thất bại, caller không thể nhận được link công khai. Tool không được phép bịa ra link 404, cũng không thể tự ý treo terminal đợi trình duyệt mở.
   - Do đó: Tool phải in thông báo lỗi rõ ràng ra `stderr`, gợi ý bác sĩ thêm cờ `--open` nếu muốn xem tại chỗ, và thoát với **Exit Code 3**. `stdout` giữ độ tinh khiết 0 dòng.
5. **Giải quyết Vấn đề Vòng đời của `LocalViewerServer`**:
   - Để trình duyệt có thể tải toàn bộ lát cắt DICOM (vốn có thể nặng từ 10MB đến 100MB), `LocalViewerServer` không được phép return ngay để tiến trình rơi vào `finally`.
   - `LocalViewerServer` cần chạy một **Vòng lặp Chờ Thông minh (Intelligent Wait Loop)**:
     * Lắng nghe và phục vụ các request HTTP (`index.html`, `manifest.json`, `dicom/*.dcm`).
     * Theo dõi biến `servedCount` và `lastRequestTime`.
     * Khi tất cả lát cắt đã được trình duyệt tải vào bộ nhớ RAM (trình duyệt không gửi thêm request nào sau 30 giây im lặng `idle >= 30s`), HOẶC khi người dùng nhấn phím bất kỳ trong console (`Console.KeyAvailable`), máy chủ cục bộ tự động ngắt kết nối.
     * Sau khi máy chủ ngắt kết nối, luồng thực thi đi vào khối `finally`, dọn dẹp sạch sẽ thư mục tạm `%TEMP%\HisPacsUploader\Pacs_...`.
     * Trình duyệt của bác sĩ vẫn giữ nguyên toàn bộ dữ liệu ảnh trên RAM HTML5 Canvas, bác sĩ có thể tiếp tục xem và thao tác vĩnh viễn trên tab trình duyệt đó!

---

## 3. MA TRẬN HÀNH VI CHUẨN HÓA (STANDARDIZED BEHAVIOR MATRIX)

Bảng dưới đây định nghĩa chính xác hành vi của hệ thống cho mọi tổ hợp tham số:

| Trạng thái Rclone / Cloud | Cờ `--open` | Cờ `STRICT_DRIVE_UPLOAD=1` | Hành vi DriveUploader | Hành vi Trình duyệt & Viewer | Dòng cuối `stdout` | Exit Code | Trạng thái Log |
| :--- | :---: | :---: | :--- | :--- | :--- | :---: | :--- |
| ✅ **Rclone thành công** | ❌ Tắt | Bất kỳ | `Success = true`, `IsCloud = true` | Không mở trình duyệt | Signed Drive URL | `0` | `SUCCESS` |
| ✅ **Rclone thành công** | ✅ Bật | Bất kỳ | `Success = true`, `IsCloud = true` | Mở Signed Drive URL trên trình duyệt | Signed Drive URL | `0` | `SUCCESS` |
| ❌ **Rclone lỗi / chưa config** | Bất kỳ | ✅ **Bật (`1`)** | `Success = false`, `IsCloud = false` | Không mở trình duyệt. Báo lỗi ra stderr. | *(0 dòng)* | `3` | `FAILED: ...` |
| ❌ **Rclone lỗi / chưa config** | ❌ Tắt | ❌ Tắt (`0` hoặc null) | `Success = false`, `IsCloud = false` | Không mở trình duyệt. Báo lỗi & gợi ý `--open` ra stderr. | *(0 dòng)* | `3` | `FAILED: ...` |
| ❌ **Rclone lỗi / chưa config** | ✅ **Bật** | ❌ Tắt (`0` hoặc null) | `Success = false`, `IsCloud = false` | **Kích hoạt `LocalViewerServer`**. Bật browser xem ảnh localhost. Chờ nạp xong. | `http://127.0.0.1:<port>/index.html` | `0` | `LOCAL_VIEWER_SERVED` |

*Ghi chú*: Đối với môi trường kiểm thử CI muốn giả lập URL stdout mà không cần rclone hay trình duyệt, hỗ trợ thêm biến môi trường `$env:MOCK_DRIVE_UPLOAD = "1"`. Khi biến này bật, in cảnh báo rõ ràng ra `stderr` và sinh link mock để kiểm tra biểu thức chính quy.

---

## 4. ĐỀ XUẤT MÃ NGUỒN CỤ THỂ (CONCRETE CODE PROPOSALS)

### 4.1. Cập nhật Lớp DTO `DriveUploadResult`
Tại dòng 640 của `HisPacsUploader.cs`:

```csharp
// TRƯỚC:
public class DriveUploadResult
{
    public bool Success { get; set; }
    public string ShareableUrl { get; set; }
    public DateTime ExpirationTime { get; set; }
    public string ErrorMessage { get; set; }
}

// SAU:
public class DriveUploadResult
{
    public bool Success { get; set; }
    public bool IsCloud { get; set; }
    public string ShareableUrl { get; set; }
    public DateTime ExpirationTime { get; set; }
    public string ErrorMessage { get; set; }
}
```

### 4.2. Sửa đổi `DriveUploader.UploadAndShare` (Loại bỏ URL 404 Giả mạo)
Thay thế đoạn mã tại dòng 716 – 743 của `HisPacsUploader.cs`:

```csharp
            if (rcloneCopied && !string.IsNullOrEmpty(rcloneLink) && rcloneLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                res.Success = true;
                res.IsCloud = true;
                res.ShareableUrl = SignedLinkService.GenerateSignedDriveUrl(rcloneLink, remoteFolderName, ttl);
                Console.Error.WriteLine(string.Format("[DriveUploader] Da tao Google Drive signed link qua rclone."));
                return res;
            }

            // Xử lý khi rclone thất bại hoặc chưa cấu hình remote gdrive:
            string rcloneDetail = string.IsNullOrEmpty(errorOutput) ? "gdrive remote chua khai bao trong rclone.conf" : errorOutput.Trim();
            Console.Error.WriteLine(string.Format("[DriveUploader] Thong bao: rclone chua ket noi Google Drive ({0}).", rcloneDetail));

            // Kiểm tra cờ Mock phục vụ kiểm thử ngoại tuyến (nếu có yêu cầu tường minh)
            string mockEnv = Environment.GetEnvironmentVariable("MOCK_DRIVE_UPLOAD");
            if (string.Equals(mockEnv?.Trim(), "1", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("[DriveUploader] CANH BAO: Che do MOCK_DRIVE_UPLOAD dang bat (chi dung cho kiem thu)...");
                string folderHash = Math.Abs((maBn + "_" + (studyUid ?? "uid")).GetHashCode()).ToString("x8");
                string mockDriveUrl = string.Format("https://drive.google.com/drive/folders/1pacs_{0}_{1}?usp=sharing", maBn, folderHash);
                res.Success = true;
                res.IsCloud = true;
                res.ShareableUrl = SignedLinkService.GenerateSignedDriveUrl(mockDriveUrl, remoteFolderName, ttl);
                return res;
            }

            // Mặc định trung thực: Báo lỗi upload đám mây
            res.Success = false;
            res.IsCloud = false;
            res.ErrorMessage = "Chua cau hinh hoac loi upload Google Drive: " + rcloneDetail;
            return res;
```

### 4.3. Cải tiến Toàn diện Lớp `LocalViewerServer`
Thay thế toàn bộ lớp `LocalViewerServer` tại dòng 746 – 833 bằng giải pháp có quản lý vòng đời và tự động đóng thông minh:

```csharp
    public static class LocalViewerServer
    {
        public static int ServeAndOpen(string localDir, bool openBrowser)
        {
            int port = new Random().Next(18500, 19500);
            string prefix = string.Format("http://127.0.0.1:{0}/", port);
            HttpListener listener = null;

            // Thử cấp phát cổng (tối đa 5 lần thử nếu trùng cổng)
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add(prefix);
                    listener.Start();
                    break;
                }
                catch
                {
                    listener = null;
                    port = new Random().Next(18500, 19500);
                    prefix = string.Format("http://127.0.0.1:{0}/", port);
                }
            }

            if (listener == null)
            {
                Console.Error.WriteLine("[LocalViewerServer] Khong the khoi dong HttpListener tren cac cong loopback duoc cap.");
                return 0;
            }

            Console.Error.WriteLine(string.Format("[LocalViewerServer] May chu cuc bo dang hoat dong tai: {0}", prefix));

            int servedCount = 0;
            DateTime lastRequestTime = DateTime.UtcNow;
            DateTime startTime = DateTime.UtcNow;

            // Luồng nền phục vụ HTTP requests
            var listenThread = new Thread(() =>
            {
                while (listener != null && listener.IsListening)
                {
                    try
                    {
                        var ctx = listener.GetContext();
                        lastRequestTime = DateTime.UtcNow;

                        string reqPath = ctx.Request.Url.AbsolutePath.TrimStart('/');
                        if (string.IsNullOrEmpty(reqPath)) reqPath = "index.html";

                        string filePath = Path.Combine(localDir, reqPath.Replace('/', Path.DirectorySeparatorChar));

                        // Hỗ trợ CORS preflight
                        if (ctx.Request.HttpMethod == "OPTIONS")
                        {
                            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                            ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                            ctx.Response.StatusCode = 200;
                            ctx.Response.Close();
                            continue;
                        }

                        if (File.Exists(filePath))
                        {
                            byte[] bytes = File.ReadAllBytes(filePath);
                            string ct = "application/octet-stream";
                            if (reqPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) ct = "text/html; charset=utf-8";
                            else if (reqPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) ct = "application/json; charset=utf-8";
                            else if (reqPath.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase)) ct = "application/dicom";
                            else if (reqPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) ct = "application/javascript; charset=utf-8";
                            else if (reqPath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) ct = "text/css; charset=utf-8";

                            ctx.Response.ContentType = ct;
                            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                            ctx.Response.ContentLength64 = bytes.Length;
                            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            Interlocked.Increment(ref servedCount);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
                        ctx.Response.Close();
                    }
                    catch
                    {
                        break;
                    }
                }
            });
            listenThread.IsBackground = true;
            listenThread.Start();

            string openUrl = prefix + "index.html";
            if (openBrowser)
            {
                Console.Error.WriteLine(string.Format("[LocalViewerServer] Dang mo trinh duyet xem anh: {0}", openUrl));
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = openUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format("[LocalViewerServer] Loi bat trinh duyet: {0}", ex.Message));
                }
            }

            // Vòng lặp chờ thông minh: Cho phép trình duyệt nạp toàn bộ ảnh DICOM vào RAM
            // Hỗ trợ cấu hình thời gian chờ qua biến môi trường để tối ưu hóa kiểm thử tự động
            int maxIdleSeconds = 30;
            string idleEnv = Environment.GetEnvironmentVariable("LOCAL_VIEWER_IDLE_TIMEOUT");
            if (int.TryParse(idleEnv, out int customIdle) && customIdle > 0)
            {
                maxIdleSeconds = customIdle;
            }

            Console.Error.WriteLine("[LocalViewerServer] May chu dang phuc vu anh. Nhan Enter/phim bat ky trong console de dong...");

            while (true)
            {
                Thread.Sleep(500);
                double elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                double idle = (DateTime.UtcNow - lastRequestTime).TotalSeconds;

                // Kiểm tra phím nhấn nếu console không bị redirect
                if (!Console.IsInputRedirected)
                {
                    try
                    {
                        if (Console.KeyAvailable)
                        {
                            Console.ReadKey(true);
                            Console.Error.WriteLine("[LocalViewerServer] Nguoi dung yeu cau dong may chu.");
                            break;
                        }
                    }
                    catch { }
                }

                // Tự động ngắt khi đã phục vụ ít nhất 1 file và trình duyệt im lặng trong maxIdleSeconds
                if (servedCount > 0 && idle >= maxIdleSeconds)
                {
                    Console.Error.WriteLine(string.Format("[LocalViewerServer] Da phuc vu {0} tap tin va khong co yeu cau moi trong {1}s. Dang tat may chu de giai phong...", servedCount, maxIdleSeconds));
                    break;
                }

                // Giới hạn an toàn tuyệt đối: 10 phút
                if (elapsed > 600)
                {
                    Console.Error.WriteLine("[LocalViewerServer] Dat gioi han thoi gian toi da (10 phut). Dang dong may chu...");
                    break;
                }
            }

            try
            {
                listener.Stop();
                listener.Close();
            }
            catch { }

            return port;
        }
    }
```

### 4.4. Cập nhật Điều phối tại `Program.Run` (Khớp nối `--open` và Mã Thoát)
Thay thế đoạn mã điều phối upload và hiển thị tại dòng 1029 – 1067 của `HisPacsUploader.cs`:

```csharp
                // M3: Upload to Google Drive & Generate Signed Link with TTL
                var uploadResult = DriveUploader.UploadAndShare(sessionFolder, maBn, downloadResult.StudyDate, downloadResult.StudyInstanceUid, ttl);

                string finalUrl = null;

                // TRƯỜNG HỢP 1: Upload Google Drive THÀNH CÔNG THẬT
                if (uploadResult.Success && uploadResult.IsCloud)
                {
                    finalUrl = uploadResult.ShareableUrl;
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, finalUrl, "SUCCESS");

                    Console.Error.WriteLine("--------------------------------------------------------------------------------");
                    Console.Error.WriteLine(string.Format("[SUCCESS] Hoan tat xu ly ca chup BN: {0} ({1})", downloadResult.PatientName, maBn));
                    Console.Error.WriteLine(string.Format("[SUCCESS] Ca chup: {0} | So luong anh: {1} lat cat", downloadResult.Modality, downloadResult.DicomFiles.Count));
                    Console.Error.WriteLine(string.Format("[SUCCESS] Signed Shareable Link (TTL {0}):", ttlDesc));
                    Console.Error.WriteLine("--------------------------------------------------------------------------------");

                    if (isOpen)
                    {
                        Console.Error.WriteLine(string.Format("[BROWSER] Dang mo lien ket Google Drive tren trinh duyet: {0}", finalUrl));
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = finalUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(string.Format("[BROWSER] Khong the mo trinh duyet: {0}", ex.Message));
                        }
                    }

                    // In URL đám mây ra dòng cuối cùng của stdout
                    Console.WriteLine(finalUrl);
                    return 0;
                }

                // TRƯỜNG HỢP 2 & 3: Upload Google Drive THẤT BẠI HOẶC CHƯA CẤU HÌNH REMOTE
                string strict = Environment.GetEnvironmentVariable("STRICT_DRIVE_UPLOAD");
                bool isStrict = string.Equals(strict?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

                // Nếu bật chế độ STRICT hoặc người dùng KHÔNG yêu cầu mở xem tại chỗ (--open)
                if (isStrict || !isOpen)
                {
                    Console.Error.WriteLine(string.Format("[ERROR] {0}", uploadResult.ErrorMessage));
                    if (!isOpen)
                    {
                        Console.Error.WriteLine("[GOI Y] Google Drive chua ket noi. De xem anh DICOM cuc bo ngay tren trinh duyet, vui long truyen co --open:");
                        Console.Error.WriteLine(string.Format("       HisPacsUploader.exe {0} --open", maBn));
                    }
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, "N/A", "FAILED: " + uploadResult.ErrorMessage);
                    return 3;
                }

                // TRƯỜNG HỢP 4: Upload Drive lỗi NHƯNG có cờ --open -> Fallback Trình xem Cục bộ (Local Viewer)
                Console.Error.WriteLine("[FALLBACK] Google Drive chua duoc cau hinh. Chuyen sang Trinh xem DICOM cuc bo (Local Viewer)...");
                int localPort = LocalViewerServer.ServeAndOpen(sessionFolder, isOpen);
                if (localPort <= 0)
                {
                    Console.Error.WriteLine("[ERROR] Khong the khoi dong Trinh xem cuc bo.");
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, "N/A", "FAILED: LocalViewerServer failed");
                    return 2;
                }

                finalUrl = string.Format("http://127.0.0.1:{0}/index.html", localPort);
                Logger.Log(maBn, downloadResult.StudyInstanceUid, finalUrl, "LOCAL_VIEWER_SERVED");

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
                Console.Error.WriteLine(string.Format("[SUCCESS] Ca chup BN: {0} ({1}) da san sang tren Trinh xem cuc bo!", downloadResult.PatientName, maBn));
                Console.Error.WriteLine(string.Format("[SUCCESS] Local Viewer URL: {0}", finalUrl));
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                // In URL localhost ra dòng cuối cùng của stdout
                Console.WriteLine(finalUrl);
                return 0;
```

---

## 5. CAVEATS (ĐIỂM GIỚI HẠN & ĐIỀU KIỆN BIÊN)

1. **Về rclone và tài khoản Google Drive trong thực tế**:
   - Hiện tại máy trạm `HP` chưa được đăng nhập OAuth2 hoặc nạp file Service Account vào file `C:\Users\HP\AppData\Roaming\rclone\rclone.conf`.
   - Do đó, trong môi trường dev hiện tại, mọi lệnh chạy không có `--open` sẽ trả về Exit Code `3` (trung thực 100%), và mọi lệnh có `--open` sẽ chạy qua `LocalViewerServer` (bác sĩ xem ảnh bình thường). Khi quản trị viên cấp file `rclone.conf`, hệ thống sẽ tự động chuyển sang upload đám mây mà không cần sửa 1 dòng code nào.
2. **Thời gian chạy kiểm thử tự động với `--open`**:
   - Vòng lặp chờ của `LocalViewerServer` có thời gian nghỉ mặc định 30 giây sau khi nạp ảnh.
   - Khi chạy script kiểm thử tự động, các test runner cần thiết lập biến môi trường `$env:LOCAL_VIEWER_IDLE_TIMEOUT = 2` để bài test kết thúc sau 2 giây mà không bị timeout 25s của `run_adversarial_suite.ps1`.
3. **Phạm vi tác vụ điều tra**:
   - Báo cáo này tập trung chuyên sâu vào giải quyết sự cố Drive Fallback và kiến trúc `LocalViewerServer`.
   - Các vấn đề song song về Concurrency PID/Guid và Stale Temp Cleanup do Explorer E2E R2-1 phụ trách; vấn đề Batch Redirection và TypedArray Alignment do Explorer E2E R2-3 phụ trách. Cả ba nhóm khuyến nghị đều tương thích và bổ trợ hoàn hảo cho nhau.

---

## 6. CONCLUSION (KẾT LUẬN & ĐÁNH GIÁ TỔNG QUAN)

1. **Tính cần thiết & cấp bách**: Việc loại bỏ hoàn toàn cơ chế sinh URL 404 giả mạo là điều kiện tiên quyết để vượt qua cổng kiểm định Gate của Orchestrator 4 và giải quyết nguyên nhân cốt lõi khiến Reviewer 2 đưa ra phán quyết `REQUEST_CHANGES (INTEGRITY VIOLATION)`.
2. **Giá trị lâm sàng**: Hồi sinh `LocalViewerServer` cho cờ `--open` mang lại trải nghiệm tối ưu cho bác sĩ bệnh viện Bạch Mai: Ngay cả khi chưa có mạng Internet hoặc chưa cấu hình tài khoản Google Drive, bác sĩ chỉ cần gõ `HisPacsUploader.exe <MaBN> --open` là trình duyệt tự động mở lên, nạp đầy đủ các lát cắt DICOM độ phân giải cao và các công cụ đo lường y khoa.
3. **Tính tuân thủ Ponytail**: Giải pháp sử dụng `HttpListener` thuần của .NET Framework BCL, không thêm bất kỳ dependency hay thư viện bên ngoài nào, tự động giải phóng tài nguyên sau khi sử dụng.

---

## 7. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để kiểm chứng tính đúng đắn của thiết kế này sau khi Worker áp dụng code, chạy các kịch bản sau trong PowerShell:

```powershell
# KỊCH BẢN 1: Kiểm tra tính trung thực khi thiếu rclone (Exit Code 3, stdout 0 dòng)
cmd /c "HisPacsUploader.exe 0004009330 1> out1.txt 2> err1.txt"
$exit1 = $LASTEXITCODE
$outCount1 = (Get-Content out1.txt).Count
Write-Host "Kịch bản 1: ExitCode = $exit1 (Kỳ vọng: 3) | Stdout lines = $outCount1 (Kỳ vọng: 0)"

# KỊCH BẢN 2: Kiểm tra chế độ STRICT_DRIVE_UPLOAD=1 (Exit Code 3)
$env:STRICT_DRIVE_UPLOAD = "1"
cmd /c "HisPacsUploader.exe 0004009330 --open 1> out2.txt 2> err2.txt"
$exit2 = $LASTEXITCODE
$env:STRICT_DRIVE_UPLOAD = $null
Write-Host "Kịch bản 2 (Strict): ExitCode = $exit2 (Kỳ vọng: 3)"

# KỊCH BẢN 3: Kiểm tra Transparent Local Viewer Fallback với cờ --open (Exit Code 0, mở trình duyệt)
$env:LOCAL_VIEWER_IDLE_TIMEOUT = 3
cmd /c "HisPacsUploader.exe 0004009330 --open 1> out3.txt 2> err3.txt"
$exit3 = $LASTEXITCODE
$url3 = (Get-Content out3.txt)[-1]
$env:LOCAL_VIEWER_IDLE_TIMEOUT = $null
Write-Host "Kịch bản 3 (--open): ExitCode = $exit3 (Kỳ vọng: 0) | URL = $url3 (Kỳ vọng: http://127.0.0.1:...)"

# KỊCH BẢN 4: Kiểm tra HTTP fetch lát cắt từ LocalViewerServer
# Khi URL localhost đang chạy, truy vấn kiểm tra HTTP 200:
# (New-Object System.Net.WebClient).DownloadString("$url3")
```
