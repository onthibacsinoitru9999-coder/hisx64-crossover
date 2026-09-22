# BÁO CÁO ĐIỀU TRA KỸ THUẬT & KHUYẾN NGHỊ KHẮC PHỤC
## DỰ ÁN: HISPACSUPLOADER.EXE — CONCURRENCY COLLISION & STALE TEMP CLEANUP
- **Tác nhân thực hiện**: Explorer E2E Round 2 - 1 (`explorer_e2e_r2_1`)
- **Parent Agent**: `parent` (`39825030-4eea-4a74-be36-84c091696543`)
- **Thư mục làm việc**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_e2e_r2_1`
- **Thời gian hoàn thành**: 2026-09-10T21:58:00+07:00 (14:58:00 UTC)
- **Tài liệu tham chiếu**:
  - `HisPacsUploader.cs` (Dòng 1011–1093)
  - `concurrency_test_results.json` & `test_concurrency.ps1`
  - `.agents\reviewer_e2e_1\handoff.md`
  - `.agents\challenger_e2e_1\handoff.md`
  - `.agents\orchestrator_4\GATE_STATUS.md`

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM ĐỘC LẬP)

### 1.1. Lỗi Xung đột Tiến trình Đồng thời (Concurrency Collision Bug)
Tại `HisPacsUploader.cs` (Dòng 1011–1013):
```csharp
1011:             string tempRoot = Path.Combine(Path.GetTempPath(), "HisPacsUploader");
1012:             string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));
1013: 
```

**Chi tiết khiếm khuyết:**
- Định dạng thư mục phiên `Pacs_{maBn}_{yyyyMMdd_HHmmss}` chỉ sử dụng độ phân giải thời gian đến hàng giây (`HHmmss`).
- Không chứa Process ID (`Process.GetCurrentProcess().Id`), không chứa GUID ngẫu nhiên, không có nano/mili-giây.
- Khi hai tiến trình `HisPacsUploader.exe` được kích hoạt song song trong cùng 1 giây cho cùng một bệnh nhân (ví dụ: điều phối đa agent, click đúp chuột, hoặc webhook tự động), cả hai tiến trình cùng nhận một chuỗi `sessionFolder` y hệt nhau.

**Hậu quả thực nghiệm (Trích xuất nguyên văn từ `concurrency_test_results.json`):**
- Lệnh chạy: `powershell -NoProfile -ExecutionPolicy Bypass -File .\test_concurrency.ps1`
- Tiến trình 1: `ExitCode = 0`, hoàn thành sau 5.2s.
- Tiến trình 2: `ExitCode = 1`, gặp ngoại lệ `IOException`:
```
[ERROR] Loi ngoai le khi ket noi RIS/PACS: The process cannot access the file '1.3.12.2.1107.5.3.63.33108.12.202609081111395551.dcm' because it is being used by another process.
```
- **Nguyên nhân tại tầng I/O:**
  Tại `HisPacsUploader.cs:426-431`:
  ```csharp
  string dicomDir = Path.Combine(tempDir, "dicom");
  if (Directory.Exists(dicomDir)) Directory.Delete(dicomDir, true);
  Directory.CreateDirectory(dicomDir);
  ZipFile.ExtractToDirectory(zipPath, dicomDir);
  ```
  Khi Tiến trình 1 đang giải nén hoặc ghi các file `.dcm`, Tiến trình 2 gọi `Directory.Delete(dicomDir, true)` hoặc `ZipFile.ExtractToDirectory(zipPath, dicomDir)` với các file trùng tên. Hệ điều hành Windows NTFS khóa file độc quyền (`FileShare.None` hoặc `FileMode.CreateNew`), dẫn đến crash `IOException`.
- **Nguy cơ xóa lấn phiên (Premature Destruction):**
  Nếu Tiến trình 1 hoàn tất trước, khối `finally` tại dòng 1078–1081 gọi `Directory.Delete(sessionFolder, true)`. Khi đó toàn bộ thư mục phiên bị xóa sổ ngay khi Tiến trình 2 đang đọc file để đóng gói Web Viewer hoặc upload Drive, làm Tiến trình 2 văng `DirectoryNotFoundException`.

---

### 1.2. Lỗi Tồn đọng Rác và Không Xóa Thư mục Gốc `%TEMP%\HisPacsUploader`
Tại `HisPacsUploader.cs` (Dòng 1074–1092):
```csharp
1074:             finally
1075:             {
1076:                 try
1077:                 {
1078:                     if (!string.IsNullOrEmpty(sessionFolder) && Directory.Exists(sessionFolder))
1079:                     {
1080:                         Directory.Delete(sessionFolder, true);
1081:                         Console.Error.WriteLine("[CLEANUP] Da xoa thu muc tam cuc bo.");
1082:                     }
1083:                     if (!string.IsNullOrEmpty(tempRoot) && Directory.Exists(tempRoot))
1084:                     {
1085:                         if (Directory.GetFileSystemEntries(tempRoot).Length == 0)
1086:                         {
1087:                             Directory.Delete(tempRoot, false);
1088:                         }
1089:                     }
1090:                 }
1091:                 catch { }
1092:             }
```

**Bằng chứng thực nghiệm từ Reviewer E2E 1 (`reviewer_e2e_1\handoff.md:32-35`):**
- Thư mục `%TEMP%\HisPacsUploader` bị sót 2 phiên mồ côi:
  1. `Pacs_0004009330_20260910_204717` (dung lượng 107 KB)
  2. `Pacs_0004009330_20260910_212857` (dung lượng 16.8 MB)
- `Test-Path "$env:TEMP\HisPacsUploader"` trả về `True` (vi phạm tiêu chí xóa sạch rác 100%).

**Cơ chế lỗi:**
1. Khối `finally` hiện tại chỉ xóa duy nhất `sessionFolder` của phiên hiện tại.
2. Nếu trong quá khứ có bất kỳ tiến trình nào bị tắt đột ngột (killed, crash unhandled, mất điện, debug giữa chừng), thư mục phiên đó trở thành "mồ côi" vĩnh viễn.
3. Khi tiến trình mới chạy xong và xóa `sessionFolder` của mình, nó kiểm tra:
   `Directory.GetFileSystemEntries(tempRoot).Length == 0`
   Vì có thư mục mồ côi cũ tồn tại, `Length > 0` luôn đúng. Lệnh `Directory.Delete(tempRoot, false)` không bao giờ được thực thi.
4. Hệ quả tích lũy: Thư mục tạm `%TEMP%\HisPacsUploader` sẽ phình to theo thời gian, chứa hàng trăm MB đến nhiều GB ảnh DICOM cũ, và không bao giờ được dọn sạch tự động.

---

### 1.3. Lỗ hổng Bổ trợ: Nguy cơ Path Traversal qua `maBn`
Tại dòng 1012:
`string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));`
Nếu `maBn` chứa các ký tự điều hướng như `../../` hoặc ký tự đặc biệt (`\`, `:`, `*`, `?`), `Path.Combine` có thể thoát ra ngoài `tempRoot` hoặc tạo tên thư mục không hợp lệ trên Windows.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN GIẢI PHÁP)

1. **Từ Quan sát 1.1**:
   - `sessionFolder` phụ thuộc vào `maBn` và `DateTime.Now:yyyyMMdd_HHmmss`.
   - Trong môi trường thực tế, hai lệnh gọi cho cùng một bệnh nhân kích hoạt trong cùng 1 giây sẽ sinh ra cùng một đường dẫn thư mục.
   - Khi đó, cả hai tiến trình cùng tranh chấp ghi, xóa, giải nén và khóa file DICOM trong thư mục con `dicom\`, dẫn đến lỗi `IOException: The process cannot access the file ... because it is being used by another process`.
   - Khi một tiến trình hoàn thành, lệnh `Directory.Delete(sessionFolder, true)` sẽ hủy hoại dữ liệu đang được tiến trình kia sử dụng.
   - **Suy luận logic**: Bắt buộc phải gắn định danh duy nhất của tiến trình (`Process.GetCurrentProcess().Id`) và một chuỗi ngẫu nhiên entropy cao (`Guid.NewGuid()`) vào tên thư mục phiên. PID đảm bảo tính độc nhất tuyệt đối giữa các tiến trình đang chạy đồng thời trên cùng OS, còn GUID đảm bảo tính độc nhất ngay cả khi PID được tái sử dụng trong tương lai.

2. **Từ Quan sát 1.2**:
   - Khối `finally` chỉ xóa phiên hiện tại và kiểm tra `GetFileSystemEntries(tempRoot).Length == 0`.
   - Bất kỳ phiên mồ côi nào từ quá khứ (do crash hoặc tắt ngang) sẽ làm `Length > 0`, vô hiệu hóa vĩnh viễn việc xóa `tempRoot`.
   - Các công cụ không có cơ chế quét dọn các phiên cũ.
   - **Suy luận logic**:
     - Cần bổ sung logic quét dọn (garbage collection) trong khối `finally` cho toàn bộ `tempRoot`.
     - Tiêu chuẩn nhận diện phiên mồ côi: Thư mục con hoặc file trong `tempRoot` có thời gian sửa đổi cuối (`LastWriteTime`) hoặc thời gian tạo (`CreationTime`) cũ hơn **1 giờ** (`TimeSpan.FromHours(1)`).
     - Quy trình PACS upload từ lúc tải đến lúc sinh link chỉ mất dưới 60 giây. Mọi thư mục tạm tồn tại trên 1 giờ chắc chắn là rác mồ côi của tiến trình đã chết.
     - Sau khi dọn các thư mục mồ côi cũ và xóa thư mục phiên hiện tại, nếu `tempRoot` không còn mục nào (`Length == 0`), tiến trình sẽ xóa luôn `tempRoot`.

3. **Từ Quan sát 1.3**:
   - Ký tự lạ trong `maBn` cần được chuẩn hóa bằng Regex để chỉ giữ lại các ký tự an toàn cho hệ thống file NTFS: `[a-zA-Z0-9_.-]`.

---

## 3. CAVEATS (ĐIỂM LƯU Ý & GIẢ ĐỊNH KỸ THUẬT)

1. **Tiến trình đang hoạt động của phiên khác (< 1 giờ)**:
   - Logic quét dọn không được chạm vào bất kỳ thư mục nào có tuổi thọ nhỏ hơn 1 giờ, nhằm tránh can thiệp vào các tiến trình khác đang chạy song song trên cùng máy trạm.
2. **Khóa file ngoài ý muốn khi dọn dẹp**:
   - Quá trình xóa thư mục con mồ côi phải được bọc trong `try-catch` riêng lẻ cho từng thư mục/file. Nếu một file đang bị antivirus hoặc tiến trình khác giữ khóa tạm thời, lệnh quét dọn sẽ bỏ qua file đó một cách an toàn mà không làm sập chương trình chính.
3. **Nguyên tắc Ponytail (Lazy Senior Dev Mode)**:
   - Không tạo thêm thư viện ngoài, không viết thêm service nền (daemon/cron) phức tạp. Toàn bộ logic giải quyết gọn gàng trong 1 hàm tĩnh `CleanupTemp(string tempRoot, string sessionFolder)` tái sử dụng BCL của .NET Framework (`System.IO`).

---

## 4. CONCLUSION & RECOMMENDED FIX SPECIFICATIONS (KHUYẾN NGHỊ KHẮC PHỤC CHI TIẾT)

### 4.1. Khắc phục Concurrency Collision & Path Sanitization

Thay thế đoạn code tại `HisPacsUploader.cs:1011-1013`:

**BEFORE (Hiện tại - Dễ xung đột & dính path traversal):**
```csharp
string tempRoot = Path.Combine(Path.GetTempPath(), "HisPacsUploader");
string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));
```

**AFTER (Đề xuất chuẩn hóa - Miễn nhiễm xung đột 100%):**
```csharp
string tempRoot = Path.Combine(Path.GetTempPath(), "HisPacsUploader");
string cleanMaBn = Regex.Replace((maBn ?? "").Replace("VS.", ""), @"[^a-zA-Z0-9_.-]", "_");
int pid = Process.GetCurrentProcess().Id;
string guid8 = Guid.NewGuid().ToString("N").Substring(0, 8);
string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}_{2}_{3}", cleanMaBn, DateTime.Now, pid, guid8));
```

**Phân tích độ an toàn:**
- `cleanMaBn`: Ngăn chặn hoàn toàn Path Traversal (loại bỏ `..`, `/`, `\`, `:`).
- `DateTime.Now:yyyyMMdd_HHmmss`: Giữ định dạng thời gian trực quan cho người quản trị khi xem thư mục tạm.
- `pid`: Đảm bảo 100% tính duy nhất giữa các tiến trình chạy đồng thời trong cùng 1 thời điểm trên hệ điều hành.
- `guid8`: 8 ký tự hex ngẫu nhiên ($16^8 \approx 4.29$ tỷ tổ hợp) triệt tiêu nguy cơ trùng lặp ngay cả khi PID được cấp phát lại hoặc khi chạy đa luồng.

---

### 4.2. Khắc phục Stale Temp Folder Cleanup trong `finally`

Thay thế khối `finally` tại `HisPacsUploader.cs:1074-1092` bằng phương thức dọn dẹp chuyên dụng:

**AFTER (Đề xuất khối finally và hàm bổ trợ):**
```csharp
            finally
            {
                CleanupTemp(tempRoot, sessionFolder);
            }
```

Và bổ sung phương thức tĩnh `CleanupTemp` vào lớp `Program` (hoặc ngay trước hàm `Run`):

```csharp
        /// <summary>
        /// Don dep thu muc phien hien tai, dong thoi quet va xoa cac thu muc phien mo coi (>1 gio).
        /// Neu tempRoot hoan toan trong, tien hanh xoa sach tempRoot khoi may cuc bo.
        /// </summary>
        static void CleanupTemp(string tempRoot, string sessionFolder)
        {
            try
            {
                // 1. Xoa thu muc session cua phien hien tai
                if (!string.IsNullOrEmpty(sessionFolder) && Directory.Exists(sessionFolder))
                {
                    try
                    {
                        Directory.Delete(sessionFolder, true);
                        Console.Error.WriteLine("[CLEANUP] Da xoa thu muc tam cuc bo: " + Path.GetFileName(sessionFolder));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(string.Format("[CLEANUP] Canh bao: Chua the xoa sessionFolder ({0}).", ex.Message));
                    }
                }

                // 2. Quet va don dep cac phien cu mo coi (> 1 gio) trong tempRoot
                if (!string.IsNullOrEmpty(tempRoot) && Directory.Exists(tempRoot))
                {
                    try
                    {
                        // Quet cac thu muc con
                        string[] subDirs = Directory.GetDirectories(tempRoot);
                        DateTime threshold = DateTime.Now.AddHours(-1);

                        foreach (string dir in subDirs)
                        {
                            try
                            {
                                var di = new DirectoryInfo(dir);
                                if (di.LastWriteTime < threshold || di.CreationTime < threshold)
                                {
                                    Directory.Delete(dir, true);
                                    Console.Error.WriteLine(string.Format("[CLEANUP] Da don dep phien cu mo coi (>1h): {0}", di.Name));
                                }
                            }
                            catch
                            {
                                // Bo qua neu dang bi khoa boi tien trinh khac
                            }
                        }

                        // Quet cac file rac con sot lai trong tempRoot
                        string[] looseFiles = Directory.GetFiles(tempRoot);
                        foreach (string file in looseFiles)
                        {
                            try
                            {
                                var fi = new FileInfo(file);
                                if (fi.LastWriteTime < threshold || fi.CreationTime < threshold)
                                {
                                    File.Delete(file);
                                }
                            }
                            catch { }
                        }

                        // 3. Neu tempRoot hoan toan rong, xoa luon tempRoot
                        if (Directory.GetFileSystemEntries(tempRoot).Length == 0)
                        {
                            Directory.Delete(tempRoot, false);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
```

---

## 5. INDEPENDENT VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để kiểm chứng tính đúng đắn và hiệu quả của các giải pháp trên sau khi Worker áp dụng:

### 5.1. Kiểm thử Xung đột Đồng thời (Concurrency Stress Test)
Chạy script kiểm thử song song đã xây dựng:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test_concurrency.ps1
```
**Tiêu chí thành công:**
- Cả 2 tiến trình (`Process 1` và `Process 2`) đều có `ExitCode = 0`.
- Cả 2 tiến trình đều in ra đúng duy nhất 1 dòng URL trên `stdout`.
- Không có bất kỳ lỗi `IOException: The process cannot access the file ...` nào xuất hiện trong `stderr`.

### 5.2. Kiểm thử Tự động Quét dọn Phiên Cũ Mồ côi (Orphaned Temp Cleanup Test)
Chạy kịch bản kiểm tra dọn dẹp bằng PowerShell:
```powershell
# 1. Tao gia lap 1 thu muc mo coi cu (> 2 gio truoc)
$tempRoot = "$env:TEMP\HisPacsUploader"
$staleDir = Join-Path $tempRoot "Pacs_STALE_TEST_DIR"
New-Item -ItemType Directory -Path $staleDir -Force | Out-Null
Set-Content -Path (Join-Path $staleDir "dummy.dcm") -Value "DUMMY_DICOM_DATA"
(Get-Item $staleDir).LastWriteTime = (Get-Date).AddHours(-2)
(Get-Item $staleDir).CreationTime = (Get-Date).AddHours(-2)

# 2. Chay HisPacsUploader binh thuong
.\HisPacsUploader.exe 0004009330 --ttl 24h

# 3. Kiem tra thu muc tam goc
Test-Path "$env:TEMP\HisPacsUploader"
# Ky vong tra ve: False (Thu muc cu da bi xoa va tempRoot duoc xoa sach vi khong con gi)
```

### 5.3. Kiểm thử Biên dịch & Kiểm tra cú pháp
```powershell
.\HisPacsUploader.bat rebuild
```
**Kỳ vọng:** Biên dịch thành công với `0 Warning(s), 0 Error(s)`. File thực thi sinh ra có kích thước hợp lệ (~65 KB).

---
*Báo cáo được đệ trình bởi Explorer E2E Round 2 - 1 để làm cơ sở triển khai chính thức cho Worker trong Vòng lặp 2.*
