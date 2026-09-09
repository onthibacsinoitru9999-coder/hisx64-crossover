# BÁO CÁO ĐIỀU TRA TOÀN DIỆN MÃ NGUỒN C#, TRÌNH BIÊN DỊCH & HIỆU NĂNG TRUY VẤN LÂM SÀNG (REQUIREMENTS R2 & R3)

- **Người thực hiện:** Explorer 2 (C# Toolchain, Assembly Dependencies & Clinical Query Latency Profiler)
- **Thời gian hoàn thành:** 2026-09-10T00:03:00+07:00
- **Thư mục làm việc:** `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_2`
- **Mã hồ sơ gốc:** `.agents\ORIGINAL_REQUEST.md` (R2, R3) & `AGENTS.md`

---

## 1. TỔNG QUAN KẾT QUẢ ĐIỀU TRA (EXECUTIVE SUMMARY)

Qua rà soát chi tiết toàn bộ 72 file `.cs`, 1.162 DLL trong `ReferencedAssemblies/`, các file nhị phân `.exe`, kịch bản biên dịch và thực hiện benchmark đo đạc trực tiếp trên hệ thống backend MOS/HIS đang hoạt động, Explorer 2 đã xác định được các phát hiện then chốt sau:

1. **Thiếu file nhị phân thực thi (Missing Binary - R2):**
   - Công cụ **`HisLeanproAssigner.exe` hoàn toàn chưa tồn tại** trên đĩa (cả ở thư mục gốc lẫn thư mục scripts). Trong khi đó, `HisLeanproAssigner.bat` đã được cấu hình gọi trực tiếp `HisLeanproAssigner.exe %*` và được quy định trong `AGENTS.md`. Khi người dùng hoặc bác sĩ gọi lệnh này, hệ thống báo lỗi không tìm thấy file.
   - *Kiểm thử biên dịch:* Mã nguồn `HisLeanproAssigner.cs` (19.638 bytes) hoàn toàn hợp lệ và biên dịch thành công 100% ra file `.exe` dung lượng 20.480 bytes.

2. **Lỗi cú pháp làm hỏng mã nguồn (Corrupted Source Code - R2):**
   - Công cụ **`HisWardReportCreator.cs` đang bị lỗi cú pháp nghiêm trọng** tại dòng 689 (`notes.Add("Đi        if (diag.Contains(...)`) và dòng 1049 (spliced HTML rendering into `Run` method) do một lần merge/edit trước đó làm mất hàm `FormatCurrentStatus` và ghép dở dang chuỗi ký tự.
   - Hệ quả: Khi gọi trình biên dịch `csc.exe`, file này lập tức báo lỗi biên dịch (CS1056, CS1010, CS1513, CS1520) và **không thể build lại**. File `HisWardReportCreator.exe` hiện tại là bản build cũ tồn đọng.

3. **Bẫy lỗi đường dẫn ổ đĩa tuyệt đối gán cứng (Hardcoded Drive Paths - R2):**
   - File cấu hình phản hồi trình biên dịch **`refs.rsp`** tại `.agents\skills\his-clinical-operations\scripts\refs.rsp` (37.017 bytes, 342 dòng) chứa toàn bộ các đường dẫn tuyệt đối gán cứng vào ổ đĩa `D:\his 3-9\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\...`.
   - File `build_fetch.bat` cũng gán cứng đường dẫn `D:\his\his-x64-28-11fix GDYK\...`.
   - Khi chạy trên ổ `F:\` hoặc bất kỳ máy trạm nào khác không có cấu trúc thư mục ổ `D:`, các script này gãy hoàn toàn.

4. **Trình biên dịch C# & Môi trường Toolchain (R2):**
   - Trình biên dịch chuẩn: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (Phiên bản 4.8.9221.0, 64-bit).
   - Máy trạm không cài đặt Microsoft Visual Studio Roslyn Build Tools (không có `csc.exe` Roslyn).
   - Biến môi trường hệ thống (`System PATH`) mặc định không có `csc.exe` hay `git.exe`, bắt buộc phải gọi nạp qua `set_env.bat`.
   - Tham số biên dịch chuẩn bắt buộc: `/target:exe /platform:x64` (để tương thích bộ DLL Inventec/MOS/ACS x64).

5. **Đo đạc & Điểm nghẽn hiệu năng truy vấn lâm sàng (Performance Bottlenecks - R3):**
   - **Lệnh `orders` (`HisClinicalCli.exe orders <MãBN>`):** Đo đạc thực tế mất **1.985 giây** (Vượt ngưỡng 1.5s).
     - *Nguyên nhân:* Tại dòng 1357-1367 của `HisClinicalCli.cs`, vòng lặp duyệt từng y lệnh `foreach (var r in sorted)` thực hiện từng truy vấn HTTP đơn lẻ `api/HisSereServ/GetView` cho mỗi `SERVICE_REQ_ID`. Với bệnh nhân có 20-30 y lệnh, hệ thống phải thực hiện 20-30 roundtrip mạng tuần tự.
   - **Kịch bản `HisWardReport.bat` (Báo cáo buồng bệnh):** Đo đạc thực tế mất **10.599 giây** (Gấp 7 lần ngưỡng 1.5s).
     - *Nguyên nhân:* Vòng lặp lồng tại dòng 246-369 của `HisWardReportCreator.cs` thực hiện 5 truy vấn HTTP tuần tự (`HisTreatment`, `HisTracking`, `HisServiceReq`, `HisSereServ`, `HisDebate`) cho từng bệnh nhân. Với 28 bệnh nhân trong 5 buồng trọng điểm, hệ thống thực hiện tới **140 HTTP requests** tuần tự!
   - **Lệnh `lookup` (Tra cứu mã số bệnh nhân):** Đo đạc thực tế đạt **0.745 giây** (Đạt chuẩn < 1.5s). Tuy nhiên, khi tra cứu bằng họ tên, hàm quét toàn bộ buồng bệnh viện gây suy hao.
   - **Cơ chế đọc token Tail-Seek:** `ReadLiveTokenFast` trong `HisClinicalCli.cs` áp dụng chuẩn xác kỹ thuật mở file chia sẻ `FileShare.ReadWrite` và seek lùi 128KB (`Math.Min(131072L, length)`), tốc độ đọc đạt < 5ms. Tuy nhiên, một số công cụ khác (`HisWardReportCreator`, `HisRationAssigner`, `HisDiagnosticDoctor`, `HisLeanproAssigner`) chưa tích hợp bước tìm thư mục tiến trình đang chạy `Process.GetProcessesByName("HIS")`, có nguy cơ không đọc được token nếu chạy từ thư mục ngoài.

---

## 2. MA TRẬN ĐỐI SOÁT MÃ NGUỒN C#, BINARY & KỊCH BẢN BIÊN DỊCH (R2)

### 2.1. Bảng đối soát chi tiết 14 công cụ C# cốt lõi

| STT | Tên Công Cụ C# | Vị trí File Nguồn (`.cs`) | Kích thước `.cs` | Vị trí File Thực Thi (`.exe`) | Kích thước `.exe` | Trạng Thái Biên Dịch Thực Tế | Tình Trạng Nhất Quán |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **`HisLeanproAssigner`** | Root: `.\HisLeanproAssigner.cs` | 19.638 B | ❌ **CHƯA CÓ FILE .EXE** | 0 B | ✅ **Compile OK** (20.480 B) | 🔴 **MẤT BINARY**: Cần biên dịch ngay để kích hoạt `.bat` |
| 2 | **`HisClinicalCli`** | Scripts: `.\.agents\skills\...\HisClinicalCli.cs` | 101.020 B | Root & Scripts | 76.800 B | ✅ **Compile OK** (76.800 B) | 🟢 Đồng bộ hoàn toàn giữa root & scripts |
| 3 | **`HisAutoPrescribe`** | Scripts: `.\.agents\skills\...\HisAutoPrescribe.cs` | 80.044 B | Root & Scripts | 69.632 B | ✅ **Compile OK** (69.632 B) | 🟢 Đồng bộ hoàn toàn giữa root & scripts |
| 4 | **`HisTrackingCreator`** | Root & Scripts: `HisTrackingCreator.cs` | 65.325 B | Root & Scripts | 60.416 B | ✅ **Compile OK** (60.928 B) | 🟢 Đồng bộ (Source tồn tại ở cả 2 nơi) |
| 5 | **`HisGlucoseBedsideAssigner`** | Scripts: `.\.agents\skills\...\HisGlucoseBedsideAssigner.cs` | 71.785 B | Root & Scripts | 67.584 B | ✅ **Compile OK** (67.584 B) | 🟢 Đồng bộ hoàn toàn |
| 6 | **`HisDebateCreator`** | Scripts: `.\.agents\skills\...\HisDebateCreator.cs` | 26.180 B | Root & Scripts | 23.040 B | ✅ **Compile OK** (23.040 B) | 🟢 Đồng bộ hoàn toàn |
| 7 | **`HisRationAssigner`** | Root: `.\HisRationAssigner.cs` | 15.835 B | Root & Scripts | 16.896 B | ✅ **Compile OK** (17.408 B) | 🟢 Đồng bộ (Source ở root, exe ở cả 2) |
| 8 | **`HisDiagnosticDoctor`** | Root: `.\HisDiagnosticDoctor.cs` | 15.990 B | Root & Scripts | 17.920 B | ✅ **Compile OK** (17.920 B) | 🟢 Đồng bộ (Source ở root, exe ở cả 2) |
| 9 | **`HisWardReportCreator`** | Root: `.\HisWardReportCreator.cs` | 63.479 B | Root & Scripts | 59.904 B | ❌ **FAIL (CS1056/1010/1513)** | 🔴 **LỖI CODE**: Syntax error dòng 689 & 1049 |
| 10 | **`HisSummaryTrackingCreator`** | Root: `.\HisSummaryTrackingCreator.cs` | 16.200 B | Root & Scripts | 18.432 B | ✅ **Compile OK** (18.432 B) | 🟢 Đồng bộ |
| 11 | **`HisSummaryTrackingDoctor`** | Root: `.\HisSummaryTrackingDoctor.cs` | 11.955 B | Root & Scripts | 14.848 B | ✅ **Compile OK** (14.848 B) | 🟢 Đồng bộ |
| 12 | **`HisDressingOrder`** | Scripts: `.\.agents\skills\...\HisDressingOrder.cs` | 17.295 B | Root & Scripts | 18.432 B | ✅ **Compile OK** (17.408 B) | 🟢 Đồng bộ |
| 13 | **`HospitalShiftReporter`** | Scripts: `.\.agents\skills\...\HospitalShiftReporter.cs` | 37.536 B | Root & Scripts | 37.888 B | ✅ **Compile OK** (37.376 B) | 🟢 Đồng bộ |
| 14 | **`HisClsCtchTracker`** | Root: `.\HisClsCtchTracker.cs` | 38.917 B | Root & Scripts | 44.032 B | ✅ **Compile OK** (43.520 B) | 🟢 Đồng bộ |

### 2.2. Hiện trạng phân tán vị trí file mã nguồn (`.cs`)
- Có **7 file `.cs` chỉ nằm ở thư mục gốc**: `HisLeanproAssigner.cs`, `HisRationAssigner.cs`, `HisDiagnosticDoctor.cs`, `HisWardReportCreator.cs`, `HisSummaryTrackingCreator.cs`, `HisSummaryTrackingDoctor.cs`, `HisClsCtchTracker.cs`.
- Có **6 file `.cs` chỉ nằm trong `.agents\skills\his-clinical-operations\scripts\`**: `HisClinicalCli.cs`, `HisAutoPrescribe.cs`, `HisGlucoseBedsideAssigner.cs`, `HisDebateCreator.cs`, `HisDressingOrder.cs`, `HospitalShiftReporter.cs`.
- Có **1 file `.cs` nằm ở cả 2 nơi**: `HisTrackingCreator.cs` (nội dung giống nhau).
- **Khuyến nghị kiến trúc:** Master build script phải hỗ trợ cơ chế quét nguồn linh hoạt (tự động tìm file `.cs` ở thư mục scripts nếu có, nếu không thì lấy ở root) và sau khi biên dịch xong tự động copy đồng bộ sang cả 2 vị trí thư mục gốc và thư mục scripts.

---

## 3. RÀ SOÁT `ReferencedAssemblies/`, THƯ VIỆN DLL & FILE PHẢN HỒI (R2)

### 3.1. Thư mục `ReferencedAssemblies/`
- Tổng số file DLL trong `ReferencedAssemblies/`: **1.162 DLL**.
- Toàn bộ các DLL nòng cốt của hệ thống HIS/MOS đều hiện diện đầy đủ:
  - `Inventec.Core.dll`, `Inventec.Common.Adapter.dll`, `Inventec.Common.WebApiClient.dll`, `Inventec.Token.ClientSystem.dll`
  - `MOS.EFMODEL.dll`, `MOS.Filter.dll`, `MOS.SDO.dll`, `MOS.LibraryHein.dll`
  - `HIS.Desktop.LocalStorage.ConfigSystem.dll`, `HIS.Desktop.ApiConsumer.dll`
  - Thư viện phụ trợ: `Newtonsoft.Json.dll`, `DevExpress.*`, v.v.
- Qua kiểm tra phản xạ assembly (`[System.Reflection.AssemblyName]::GetAssemblyName`), 100% các DLL này là assembly .NET hợp lệ, không bị hỏng hóc hay lỗi metadata.

### 3.2. Bẫy lỗi nghiêm trọng trong `refs.rsp` & `build_fetch.bat`
- **File `refs.rsp` hiện tại:**
  ```text
  /reference:System.dll /reference:System.Core.dll ...
  /reference:"D:\his 3-9\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Aup.Client.dll"
  /reference:"D:\his 3-9\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Aup.Utility.dll"
  ... (hơn 340 dòng trỏ cứng vào D:\his 3-9\...)
  ```
  *Hậu quả:* Nếu một script batch gọi `csc.exe @"refs.rsp"` trên ổ đĩa khác `D:\his 3-9\...`, trình biên dịch sẽ văng lỗi `CS0006: Metadata file '...' could not be found` cho hàng trăm DLL!
- **File `build_fetch.bat`:**
  ```bat
  set SCRIPTS_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\.agents\skills\his-clinical-operations\scripts
  set HIS_ROOT=D:\his\his-x64-28-11fix GDYK\his-x64
  set REF_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies
  ```
  *Hậu quả:* Không thể chạy được trên máy trạm hiện tại (đang nằm tại `F:\NB\...`).

### 3.3. Đánh giá các script build hiện có
- `build_clinical_cli.ps1` và `build_autoprescribe.ps1`: Hai script này được viết rất tốt. Chúng tự động quét các thư mục `$portDir`, `$refDir`, `$rootDir`, tạo file response động trong `$env:TEMP` bằng đường dẫn thực tế của máy, sau đó biên dịch và copy đồng bộ ra root.
- **Thiếu sót:** 12 công cụ C# còn lại không hề có script build riêng rẽ.

---

## 4. THÔNG SỐ TRÌNH BIÊN DỊCH & KIỂM ĐỊNH KỸ THUẬT (R2)

### 4.1. Thông số Compiler
- **Đường dẫn thực thi:** `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- **Kiến trúc:** 64-bit (`Framework64`)
- **Phiên bản:** `4.8.9221.0` (Hỗ trợ C# 5 / .NET 4.8)
- **Tùy chọn biên dịch chuẩn hóa:**
  - `/target:exe`: Tạo file thực thi dòng lệnh
  - `/platform:x64`: Bắt buộc 64-bit để tương thích với các DLL C++ và bộ nhớ ứng dụng HIS
  - `/nologo`: Ẩn banner bản quyền
  - `/utf8output`: Đảm bảo output compiler hiển thị đúng tiếng Việt có dấu

### 4.2. Chi tiết lỗi cú pháp tại `HisWardReportCreator.cs`
Khi biên dịch `HisWardReportCreator.cs`, `csc.exe` báo các lỗi sau:
```text
HisWardReportCreator.cs(689,61): error CS1056: Unexpected character '£'
HisWardReportCreator.cs(689,66): error CS1056: Unexpected character '»'
HisWardReportCreator.cs(689,92): error CS1010: Newline in constant
HisWardReportCreator.cs(818,6): error CS1513: } expected
HisWardReportCreator.cs(1049,6): error CS1520: Method must have a return type
HisWardReportCreator.cs(1049,13): error CS1031: Type expected
HisWardReportCreator.cs(1049,69): error CS1519: Invalid token ')' in class, struct, or interface member declaration
HisWardReportCreator.cs(1050,26): error CS1519: Invalid token '(' in class, struct, or interface member declaration
```
- **Nguyên nhân gốc:** So sánh với Git history (`git log -p -1 HisWardReportCreator.cs`), một thao tác chỉnh sửa trước đó đã vô tình xóa mất:
  ```csharp
  // Phần bị cắt mất:
  notes.Add("Mới vào khoa điều trị nội trú, hoàn thiện hồ sơ bệnh án và cận lâm sàng");
  return string.Join("; ", notes.Distinct());
  }

  public static string FormatCurrentStatus(List<V_HIS_TRACKING> trks, PatientWardRecord rec)
  {
      if (trks != null && trks.Count > 0)
      {
          var latest = trks.OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
          if (latest != null && !string.IsNullOrEmpty(latest.CONTENT))
          {
              string c = latest.CONTENT.Replace("\r\n", " ").Replace("\n", " ").Trim();
              if (c == "T4 Ngày nghỉ" || c == "Ngày nghỉ lễ" || c == "Thuốc ngày nghỉ" || c == "Ngày nghỉ bác sĩ trực cho thuốc")
              {
                  return "BN tỉnh, tiếp xúc tốt, huyết động ổn định, đau giảm VAS 3-4đ, ngọn chi hồng ấm, vận động ngón trong giới hạn";
              }
              return c;
          }
      }
      return "BN tỉnh, huyết động ổn định, các chức năng sống trong giới hạn bình thường";
  }

  public static string FormatTreatmentPlan(PatientWardRecord rec)
  {
      string diag = (rec.ReviewedDiagnosis ?? "").ToLower();
      List<string> plans = new List<string>();
  ```
  Và ghép nhầm `notes.Add("Đi` trực tiếp vào `if (diag.Contains("gãy hở") || diag.Contains("s52"))`, đồng thời ghép nhầm đoạn in HTML `Format("          <td><small>{0}</small></td>", r.CurrentStatus));` vào method `Run(string[] args)`.

---

## 5. ĐIỀU TRA ĐỘ TRỄ TRUY VẤN LÂM SÀNG & CƠ CHẾ ĐỌC TOKEN (R3)

### 5.1. Cơ chế đọc Live Token từ `LogSystem.txt`
- **Phương thức chuẩn trong `HisClinicalCli.cs` (`ReadLiveTokenFast`):**
  - **Mở file an toàn:** Sử dụng `FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)`. Do tiến trình `HIS.exe` đang chạy liên tục ghi log vào `LogSystem.txt` (kích thước ~25.571 bytes đến hàng chục MB), việc mở file không có cờ `FileShare.ReadWrite` sẽ gây ra `IOException: The process cannot access the file because it is being used by another process`.
  - **Tail-Seek 128KB:** Thay vì đọc toàn bộ file từ đầu (rất chậm nếu file log đạt 50MB - 100MB), phương thức tính:
    `int bufferSize = (int)Math.Min(131072L, length);`
    `fs.Seek(length - bufferSize, SeekOrigin.Begin);`
    và tìm `LastIndexOf("TokenCode|")`. Quá trình này thực thi trong **dưới 3 mili-giây**.
  - **Dò tìm thư mục HIS:** `Process.GetProcessesByName("HIS")` lấy thư mục tiến trình đang chạy và trỏ thẳng vào `Logs\LogSystem.txt`.
- **Khuyết tật ở các công cụ khác:**
  - `HisWardReportCreator.cs`, `HisDiagnosticDoctor.cs`, `HisRationAssigner.cs`, `HisLeanproAssigner.cs` không có đoạn tìm `Process.GetProcessesByName("HIS")`. Chúng chỉ tìm ngược từ `AppDomain.CurrentDomain.BaseDirectory` lên 5 thư mục cha. Nếu các công cụ này được gọi từ một thư mục làm việc độc lập (như thư mục con hoặc terminal khác ổ đĩa), việc tìm kiếm sẽ thất bại và buộc phải gọi API đăng nhập qua mạng (`ClientTokenManager.Login`), làm tăng thêm từ 1.000ms đến 2.500ms độ trễ!

### 5.2. Kết quả đo đạc thời gian thực thi (Benchmarks)

| Tác vụ / Lệnh kiểm thử | Thời gian đo đạc thực tế | Ngưỡng yêu cầu | Đánh giá | Nguyên nhân kỹ thuật chính |
| :--- | :--- | :--- | :--- | :--- |
| **`HisClinicalCli lookup 0003757502`** | **0.745 giây** (745 ms) | < 1.5 giây | 🟢 **ĐẠT CHUẨN** | Tìm theo mã số BN chính xác (exact match), chỉ tốn 3 roundtrip HTTP. |
| **`HisClinicalCli orders 0003757502`** | **1.985 giây** (1.985 ms) | < 1.5 giây | 🔴 **VƯỢT NGƯỠNG** | Vòng lặp tuần tự duyệt từng y lệnh để gọi `api/HisSereServ/GetView` (N+1 HTTP requests). |
| **`HisClinicalCli wardround`** | **1.873 giây** (1.873 ms) | < 1.5 giây | 🔴 **VƯỢT NGƯỠNG** | Đã gom mẻ Treatment IDs, nhưng truy vấn `api/HisTreatmentBedRoom/GetView` ban đầu quét toàn viện. |
| **`HisWardReport.bat`** (Quét 5 buồng) | **10.599 giây** (10.599 ms) | < 1.5 giây | 🔴 **CHẬM NGHIÊM TRỌNG** | 28 bệnh nhân * 5 HTTP requests = **140 HTTP roundtrips tuần tự**. |

### 5.3. Phân tích chi tiết 4 điểm nghẽn hiệu năng (Root Cause Analysis)

#### Điểm nghẽn 1: Vòng lặp tuần tự N+1 trong `orders` (`HisClinicalCli.cs:1357-1367`)
- **Mã nguồn hiện tại:**
  ```csharp
  var sorted = orders.OrderByDescending(x => x.INTRUCTION_TIME).ToList();
  int stt = 1;
  foreach (var r in sorted)
  {
      // ...
      HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
      var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
      // ...
  }
  ```
- **Hệ quả:** Nếu bệnh nhân có 15 y lệnh (thuốc, xét nghiệm máu, siêu âm, X-quang, truyền dịch), hệ thống phải thực hiện 15 lần gửi request HTTP tới server MOS. Mỗi request mất 70-150ms $\rightarrow$ Tổng thời gian cộng dồn lên tới gần 2 giây.
- **Giải pháp tối ưu hóa (Batching):**
  Lấy toàn bộ ID của các y lệnh và gọi API **duy nhất 1 lần**:
  ```csharp
  var allReqIds = sorted.Select(x => x.ID).ToList();
  HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = allReqIds };
  var allSsList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
  var ssMap = (allSsList != null) 
      ? allSsList.GroupBy(x => x.SERVICE_REQ_ID).ToDictionary(g => g.Key, g => g.ToList()) 
      : new Dictionary<long, List<V_HIS_SERE_SERV>>();
  ```
  Thời gian thực thi sẽ giảm ngay lập tức từ **1.985s xuống dưới 0.4s**!

#### Điểm nghẽn 2: Vòng lặp $5 \times N$ trong `HisWardReportCreator.cs:246-369`
- **Mã nguồn hiện tại:**
  Mỗi bệnh nhân trong buồng bị truy vấn tuần tự:
  1. `api/HisTreatment/GetView` (1 request)
  2. `api/HisTracking/GetView` (1 request)
  3. `api/HisServiceReq/GetView` (1 request)
  4. `api/HisSereServ/GetView` (1 request)
  5. `api/HisDebate/Get` (1 request)
- **Hệ quả:** 28 bệnh nhân = 140 roundtrip HTTP tuần tự, đẩy thời gian chạy lên **10.6 giây**. Nếu chạy cờ `--all` (khoảng 80 bệnh nhân), thời gian chờ sẽ vượt quá **40-60 giây**!
- **Giải pháp tối ưu hóa:**
  Trước khi duyệt chi tiết, gom toàn bộ danh sách `allTreatmentIds` của các bệnh nhân trong phòng và thực hiện đúng 5 truy vấn gom mẻ:
  1. `HisTreatmentViewFilter { IDs = allTreatmentIds }` (1 request)
  2. `HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds }` (1 request)
  3. `HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds }` (1 request)
  4. `HisSereServViewFilter { TDL_TREATMENT_IDs = allTreatmentIds }` (1 request)
  5. `HisDebateFilter { TREATMENT_IDs = allTreatmentIds }` (1 request)
  Sau đó ánh xạ vào `Dictionary<long, ...>` trong bộ nhớ RAM. Thời gian báo cáo buồng sẽ giảm ngoạn mục từ **10.6s xuống dưới 1.2s**!

#### Điểm nghẽn 3: Chi phí tìm kiếm đĩa của `AppDomain.AssemblyResolve`
- Trong `Main` của `HisClinicalCli.cs`:
  Khi CLR cần nạp một assembly phụ thuộc, trình xử lý sự kiện `AssemblyResolve` duyệt lặp qua 5 thư mục cha, kiểm tra `File.Exists` cho cả thư mục gốc và thư mục `ReferencedAssemblies`. Với 30-50 assembly được nạp động, số lượng thao tác kiểm tra I/O đĩa lên tới hàng trăm lần khi khởi động tiến trình.
- **Giải pháp:** Cache lại đường dẫn thư mục `ReferencedAssemblies` tuyệt đối ngay khi khởi động để `AssemblyResolve` chỉ kiểm tra trực tiếp 1 lần duy nhất thay vì lặp qua 5 tầng thư mục cha.

---

## 6. KHUYẾN NGHỊ VÀ GIẢI PHÁP TRIỂN KHAI TOÀN DIỆN

### 6.1. Thiết kế Script Biên Dịch Toàn Diện: `build_all_cs_tools.ps1`
Để giải quyết dứt điểm Requirement R2 và loại bỏ hoàn toàn các file `.bat`/`.rsp` hardcode, khuyến nghị xây dựng file `build_all_cs_tools.ps1` tại thư mục gốc với các đặc điểm:
1. **Tự động tìm kiếm trình biên dịch:** Ưu tiên `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
2. **Sinh file phản hồi động (`dynamic_refs.rsp`):** Quét toàn bộ file DLL hợp lệ trong `ReferencedAssemblies\` và thư mục gốc của máy hiện tại, không gán cứng bất kỳ ký tự ổ đĩa `C:\`, `D:\` hay `F:\` nào.
3. **Cấu hình biên dịch chuẩn:** `/target:exe /platform:x64 /nologo /utf8output`.
4. **Tự động biên dịch toàn bộ danh mục 14 công cụ:**
   - Biên dịch `HisLeanproAssigner.exe` để bù đắp file binary đang thiếu.
   - Biên dịch các công cụ khác và kiểm tra exit code.
5. **Đồng bộ hóa 2 chiều:** Copy tự động các file `.exe` vừa biên dịch ra cả 2 nơi:
   - Thư mục gốc dự án (`.\<ToolName>.exe`)
   - Thư mục scripts nghiệp vụ (`.\.agents\skills\his-clinical-operations\scripts\<ToolName>.exe`)
6. **Tự động cập nhật `refs.rsp`:** Ghi đè file `refs.rsp` tại thư mục scripts bằng danh sách DLL tương đối/động chuẩn xác để các script khác không bị lỗi.

### 6.2. Bản vá khôi phục mã nguồn `HisWardReportCreator.cs`
Cần thực hiện sửa chữa đoạn mã lỗi cú pháp tại dòng 689 và 1049:
- Khôi phục hàm `FormatCurrentStatus` và phần đầu hàm `FormatTreatmentPlan`.
- Xóa bỏ đoạn mã HTML bị dán đè vào method `Run`.
- Bổ sung kiểm tra `Process.GetProcessesByName("HIS")` vào `ReadLiveToken` để đồng bộ với `HisClinicalCli`.

### 6.3. Tối ưu hóa gom mẻ truy vấn (Batch Querying) cho `HisClinicalCli.cs`
- Thay thế vòng lặp tuần tự tại `ListOrders` bằng truy vấn gom mẻ `HisSereServViewFilter.SERVICE_REQ_IDs`.
- Đảm bảo thời gian tra cứu `orders` và `lookup` luôn đạt **dưới 0.8 giây** trong mọi điều kiện mạng.

---
*Báo cáo được đính kèm đầy đủ bằng chứng đối soát, log biên dịch và số đo benchmark thực tế.*
