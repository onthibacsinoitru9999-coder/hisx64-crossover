# BÁO CÁO KHẢO SÁT CHUYÊN SÂU: YÊU CẦU R4 & R5
## HIS AUTOMATION CODEBASE AUDIT & OPTIMIZATION PROJECT
**Explorer Agent**: Explorer 3  
**Working Directory**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_3`  
**Workspace Root**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`  
**Timestamp**: 2026-09-10T00:01:00+07:00 (2026-09-09T17:01:00Z)

---

## 1. TỔNG QUAN ĐIỀU HÀNH (EXECUTIVE SUMMARY)

Thực hiện nhiệm vụ khảo sát độc lập cho Yêu cầu **R4** (*Dọn dẹp File Rác, File Tạm & Chuẩn Hóa Cấu Trúc, Mã Hóa Ký Tự*) và **R5** (*Kiểm Thử Khép Kín & Kiểm Định Tự Động Toàn Hệ Thống*), Explorer 3 đã tiến hành rà soát 100% cây thư mục, phân tích tĩnh toàn bộ tập lệnh PowerShell, kiểm toán byte-level bảng mã ký tự (Character Encoding), định danh chính xác các file rác/tạm so với tài sản cấu hình bất khả xâm phạm, đo lường các mốc kiểm thử trọng yếu và phân tích cấu trúc Git repository.

### Các Phát Hiện Cốt Lõi (Key Discoveries):
1. **Phát hiện lỗi cú pháp nghiêm trọng trong PowerShell do thiếu UTF-8 BOM (`R4/R5`)**:
   - Tệp lệnh `WatchHoiChan.ps1` (3,677 bytes) chứa văn bản tiếng Việt có dấu và ký tự Emoji nhưng được lưu ở định dạng UTF-8 **không có BOM** (No BOM).
   - Khi được phân tích bởi bộ phân tích chuẩn của Windows PowerShell 5.1 (`[System.Management.Automation.Language.Parser]::ParseFile`), trình biên dịch diễn giải theo mã trang ANSI (Windows-1252), dẫn tới việc các chuỗi multibyte bị hiểu nhầm thành ký tự đóng ngoặc/ngắt chuỗi, gây ra **9 lỗi cú pháp (Syntax Errors)** chết người (`CS1056/ParseError`).
   - Khi nạp dưới dạng UTF-8 thuần bằng `ParseInput`, file có **0 lỗi cú pháp**. Giải pháp khắc phục: Ghi lại file với UTF-8 BOM (`EF BB BF`).
2. **Phát hiện tệp mã nguồn C# bị hỏng mã hóa byte và vỡ cú pháp biên dịch (`R2/R4`)**:
   - Tệp `HisWardReportCreator.cs` (63,479 bytes) chứa một byte lỗi đơn độc `0xE1` tại offset 35,224 (dòng 689) nằm giữa chuỗi `"Điề"` và câu lệnh `if (diag.Contains...)`.
   - Hậu quả: Trình biên dịch `csc.exe` 64-bit báo lỗi `error CS1056: Unexpected character '£'` và không thể biên dịch mã nguồn này thành `.exe`.
3. **Phát hiện mã nguồn lưu sai bảng mã UTF-16 LE (`R4`)**:
   - Tệp `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs` (3,360 bytes) được lưu ở định dạng **UTF-16 LE** thay vì UTF-8, gây xung đột với các công cụ đọc mã chuẩn và trình phân tích văn bản UTF-8.
4. **Định danh danh mục file rác/tồn đọng từ các lần chạy thử (`R4`)**:
   - Các file log sinh ra từ `HisDiabetesOrchestrator.ps1`: `diabetes_orchestrator_20260909_2341.log`, `2343.log`, `2356.log`.
   - File trung gian y lệnh: `insulin_orders_temp.csv` (2,288 bytes).
   - File dump stdout thừa: `output_3e.txt` (74,792 bytes).
   - File scratch cũ: `Test20211012.txt` (3 bytes), `readmebk.txt` (10 bytes), thư mục `__pycache__/`.
5. **Thiết lập danh mục tài sản cấu hình và runtime bất khả xâm phạm (Protected Assets)**:
   - `ConfigSystem.xml` (3 bản sao: root, scripts, Integrate/LOG.VPlus).
   - 66 tệp `*.exe.config` và `*.exe.config_` trên toàn bộ cây thư mục.
   - Thư mục `ReferencedAssemblies/` chứa 1,177 DLLs và phông chữ của hệ thống HIS.
   - Thư mục `Logs/` chứa dòng log sống (`LogSystem.txt`, `HLSLogSystem.txt`) cung cấp TokenCode thời gian thực.
6. **Kiểm chứng hiệu năng và sẵn sàng của các chốt kiểm định (`R5`)**:
   - `HisDiagnosticDoctor.bat health`: Chạy thành công, kết nối 4 máy chủ lõi OK, xác thực TokenCode hợp lệ, kết luận: *"Hệ thống sẵn sàng 100%!"*.
   - `HisAiCli.bat models`: Phản hồi tức thì trong **145 ms** (đáp ứng xuất sắc tiêu chí < 2.0s).
   - `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm`: Hoàn thành 4/4 công đoạn điều phối ĐTĐ, 9 y lệnh giả lập đạt kết quả 100% thành công với 0 lỗi.

---

## 2. DANH MỤC VÀ KIỂM ĐỊNH TOÀN DIỆN TẬP LỆNH POWERSHELL (`*.ps1`)

Hệ thống có tổng cộng **8 tệp lệnh PowerShell** chính tại thư mục gốc dự án:

| STT | Tên Tệp Lệnh (`*.ps1`) | Kích thước | Dòng | Bảng Mã (BOM) | Tham số đầu vào (Parameters) | Mục đích chức năng | Lỗi AST PS 5.1 (`ParseFile`) |
| :---: | :--- | :---: | :---: | :---: | :--- | :--- | :---: |
| 1 | `build_autoprescribe.ps1` | 2,656 B | 67 | No BOM (Non-ASCII) | Không | Biên dịch `HisAutoPrescribe.cs` sang `.exe` bằng `csc.exe` x64 | **0** |
| 2 | `build_clinical_cli.ps1` | 2,442 B | 61 | No BOM (Non-ASCII) | Không | Biên dịch `HisClinicalCli.cs` sang `.exe` bằng `csc.exe` x64 | **0** |
| 3 | `check_his_tunnel.ps1` | 4,920 B | 109 | No BOM (ASCII) | Không | Kiểm tra subnet router Tailscale & ping 9 port máy chủ HIS | **0** |
| 4 | `HisDiabetesOrchestrator.ps1` | 23,504 B | 554 | **UTF-8 BOM** | `ImagePath`, `JsonPath`, `Date`, `OpenRouterApiKey`, `OpenRouterModel`, `GeminiApiKey`, `DryRun`, `SkipConfirm`, `SkipTracking`, `SkipBedside`, `SkipInsulin` | Script điều phối trung tâm tự động hóa luồng ĐTĐ (Vision OCR -> Tờ ĐT -> BM02426 -> Kê đơn Insulin) | **0** |
| 5 | `HisPacsCli.ps1` | 5,079 B | 156 | No BOM (Non-ASCII) | `PatientId`, `AccessionNo`, `PatientName`, `Open`, `RisHost`, `Account`, `Password` | Tra cứu ca chụp PACS/RIS Minerva và mở Web Viewer 1-Click | **0** |
| 6 | `install_git.ps1` | 955 B | 19 | No BOM (ASCII) | Không | Tải và cài đặt tự động Git 64-bit silent mode | **0** |
| 7 | `WatchBranchLearning.ps1` | 4,734 B | 100 | **UTF-8 BOM** | Không | Lắng nghe trực tiếp sự kiện đăng nhập, TokenCode và chuyển đổi cơ sở Ninh Bình từ `LogSystem.txt` | **0** |
| 8 | `WatchHoiChan.ps1` | 3,677 B | 84 | **No BOM (Non-ASCII)** | Không | Giám sát trực tiếp sự kiện tạo chỉ định hội chẩn chuyên khoa trên HIS | **9 LỖI** ❌ |

*(Ghi chú: Có thêm 2 tệp `.ps1` tạm thuộc thư mục `.agents\explorer_survey_2\` do Explorer 2 tạo ra để đo kiểm: `audit_r2_r3.ps1` và `test_compilation.ps1`).*

### Phân Tích Chuyên Sâu Lỗi Cú Pháp Của `WatchHoiChan.ps1`:
- **Nguyên nhân kỹ thuật**:
  Tệp `WatchHoiChan.ps1` sử dụng các ký tự tiếng Việt có dấu (`ĐANG LẮNG NGHE...`, `Bác sĩ hãy bắt đầu...`) và các ký tự emoji Unicode đa byte (`🔥`, `🟢`, `👉`, `📦`, `🚀`, `🔍`, `📝`). Tệp được lưu dưới dạng UTF-8 không có byte BOM (`EF BB BF`).
  Khi thực thi lệnh kiểm định chuẩn của PowerShell:
  ```powershell
  $tokens = $null; $errors = $null
  [System.Management.Automation.Language.Parser]::ParseFile('WatchHoiChan.ps1', [ref]$tokens, [ref]$errors)
  ```
  Trình phân tích cú pháp của PowerShell 5.1 đọc file theo mặc định mã ANSI (Windows-1252). Các chuỗi byte UTF-8 đa byte bị phân rã, dẫn đến ký tự trích dẫn `"` bị ngắt quãng, tạo ra 9 lỗi:
  - `Missing type name after '['.`
  - `The string is missing the terminator: ".`
  - `Missing closing '}' in statement block or type definition.` (x5)
  - `The Try statement is missing its Catch or Finally block.`
- **Bằng chứng đối chứng**:
  Khi nạp tệp bằng luồng UTF-8 tường minh:
  ```powershell
  $content = [System.IO.File]::ReadAllText('WatchHoiChan.ps1', [System.Text.Encoding]::UTF8)
  $ast = [System.Management.Automation.Language.Parser]::ParseInput($content, [ref]$null, [ref]$errors)
  # Kết quả: $errors.Count = 0 (Hoàn toàn hợp lệ!)
  ```
- **Hành động đề xuất**: Bổ sung UTF-8 BOM cho `WatchHoiChan.ps1`, cũng như đồng bộ bổ sung UTF-8 BOM cho toàn bộ các file `.ps1` còn lại để phòng ngừa triệt để lỗi khi chạy trong PowerShell 5.1.

---

## 3. KIỂM TOÁN MÃ HÓA KÝ TỰ TOÀN DIỆN (CHARACTER ENCODING AUDIT)

Đã quét toàn diện **166 tệp** thuộc các định dạng chỉ định trên toàn bộ workspace:
- `.md`: 45 tệp
- `.cs`: 82 tệp
- `.ps1`: 10 tệp
- `.bat`: 27 tệp
- `.json`: 2 tệp

### Bảng Thống Kê Các Tệp Bị Lỗi Mã Hóa / Bất Thường:

| Đường Dẫn Tệp | Định Dạng | Kích Thước | BOM Hiện Tại | UTF-8 Hợp Lệ? | Chi Tiết Lỗi / Hiện Tượng | Tác Động Hệ Thống |
| :--- | :---: | :---: | :---: | :---: | :--- | :--- |
| `WatchHoiChan.ps1` | `.ps1` | 3,677 B | None | Có | Thiếu UTF-8 BOM khi có ký tự Unicode tiếng Việt/Emoji | Khiến `[Parser]::ParseFile` văng 9 lỗi cú pháp trong PS 5.1 |
| `HisWardReportCreator.cs` | `.cs` | 63,479 B | None | **KHÔNG** ❌ | Byte lỗi `0xE1` tại byte 35,224; Dòng 689 bị mất đoạn văn bản | **Không thể biên dịch** bằng `csc.exe` (Báo lỗi `CS1056`) |
| `.agents\skills\...\FetchPatient.cs` | `.cs` | 3,360 B | **UTF-16 LE** ❌ | **KHÔNG** ❌ | Tệp lưu dạng Little-Endian Unicode (2 bytes/char) | Bị lỗi định dạng MIME khi xem bằng công cụ UTF-8 chuẩn |
| `build_autoprescribe.ps1` | `.ps1` | 2,656 B | None | Có | Chứa tiếng Việt không dấu/có dấu trong chuỗi Write-Host | Nguy cơ lỗi encoding trên console tiếng Anh |
| `build_clinical_cli.ps1` | `.ps1` | 2,442 B | None | Có | Chứa tiếng Việt không dấu/có dấu trong chuỗi Write-Host | Nguy cơ lỗi encoding trên console tiếng Anh |
| `HisPacsCli.ps1` | `.ps1` | 5,079 B | None | Có | Chứa comment tiếng Việt có dấu nhưng chưa có UTF-8 BOM | Cần chuẩn hóa UTF-8 BOM |

### Phân Tích Sự Cố Tệp `HisWardReportCreator.cs`:
Tại vị trí offset 35,224, dòng 689 của tệp `HisWardReportCreator.cs` hiển thị:
```csharp
Line 685:         if (notes.Count == 0)
Line 686:         {
Line 687:             if (trks != null && trks.Count > 1)
Line 688:             {
Line 689:                 notes.Add("Điề        if (diag.Contains("gãy hở") || diag.Contains("s52"))
Line 690:         {
Line 691:             plans.Add("1. Cắt lọc, rửa và xử trí vô khuẩn vết thương gãy hở độ I");
```
Chuỗi `"Điề"` bị cụt dở, thiếu dấu đóng ngoặc `")` và dấu chấm phẩy `;`, tiếp theo là câu lệnh `if (diag.Contains...)` bị dính liền vào cùng một dòng.
Khi chạy thử biên dịch:
```powershell
csc.exe /target:exe /platform:x64 HisWardReportCreator.cs ...
# Báo lỗi: HisWardReportCreator.cs(689,61): error CS1056: Unexpected character '£'
```
File nhị phân `HisWardReportCreator.exe` hiện tại trên đĩa được biên dịch trước khi mã nguồn bị lỗi sửa dở này. Để thỏa mãn Acceptance Criteria của R2 và R4, tệp `.cs` này **bắt buộc phải được sửa phục hồi dòng 689** và lưu chuẩn UTF-8.

### Kiểm Tra Các Tệp `.bat`:
Trong 27 tệp `.bat`, các tệp chứa chuỗi tiếng Việt như `Cai_Dat_He_Thong_Support.bat`, `Chay_ChiDinh_BM02426.bat`, `Chay_Tao_ToDieuTri.bat`, `sync_pull.bat`, `sync_push.bat` đều đã có chỉ thị `chcp 65001 >nul` ở đầu tệp, đảm bảo bảng mã UTF-8 được kích hoạt khi chạy trong CMD.

---

## 4. PHÂN ĐỊNH FILE RÁC, FILE TẠM VS TÀI SẢN BẢO VỆ (WORKSPACE HYGIENE)

### 4.1. Danh Mục File Rác & File Tạm Cần Dọn Dẹp (Safe to Delete)

Tất cả các tệp dưới đây là kết quả của quá trình chạy thử nghiệm, dump log hoặc tệp sao lưu tạm thời. Việc dọn dẹp các tệp này không ảnh hưởng đến hoạt động của hệ thống:

| STT | Tên Tệp / Thư Mục | Vị Trí | Dung Lượng | Nguồn Gốc Sinh Ra | Quy Tắc Xử Lý |
| :---: | :--- | :--- | :---: | :--- | :--- |
| 1 | `diabetes_orchestrator_20260909_2341.log` | Thư mục gốc | 332 B | Log chạy thử `HisDiabetesOrchestrator.ps1` | Xóa an toàn |
| 2 | `diabetes_orchestrator_20260909_2343.log` | Thư mục gốc | 332 B | Log chạy thử `HisDiabetesOrchestrator.ps1` | Xóa an toàn |
| 3 | `diabetes_orchestrator_20260909_2356.log` | Thư mục gốc | 332 B | Log chạy thử `HisDiabetesOrchestrator.ps1` | Xóa an toàn |
| 4 | `insulin_orders_temp.csv` | Thư mục gốc | 2,288 B | File trung gian sinh ra từ Bước 3 của Orchestrator | Xóa an toàn |
| 5 | `output_3e.txt` | Thư mục gốc | 74,792 B | File stdout dump từ lệnh kiểm tra `Query3EAdmissions.exe` | Xóa an toàn |
| 6 | `Test20211012.txt` | Thư mục gốc | 3 B | File test tồn đọng từ năm 2021 chứa nội dung rác (`123`) | Xóa an toàn |
| 7 | `readmebk.txt` | Thư mục gốc | 10 B | File sao lưu trùng lặp với `readme.txt` | Xóa an toàn |
| 8 | `__pycache__/` | Thư mục gốc | Thư mục | Bytecode cache sinh ra khi thực thi Python | Xóa an toàn |

*(Lưu ý về `glucose_data.json`: Tệp này được `HisDiabetesOrchestrator.ps1` tự động tạo ra làm dữ liệu mẫu khi chạy tham số `-DryRun -SkipConfirm`. Sau khi kiểm thử hoàn tất, tệp này có thể dọn dẹp hoặc giữ lại làm mẫu kiểm thử).*

### 4.2. Danh Mục Tài Sản Cấu Hình & Runtime Bất Khả Xâm Phạm (STRICT PROTECTED ASSETS)

**TUYỆT ĐỐI KHÔNG ĐƯỢC XÓA HOẶC LÀM SAI LỆCH CÁC TÀI NGUYÊN SAU**:

1. **Tệp Cấu Hình Hệ Thống `ConfigSystem.xml`**:
   - `ConfigSystem.xml` (Gốc: 8,367 bytes)
   - `.agents\skills\his-clinical-operations\scripts\ConfigSystem.xml` (8,367 bytes)
   - `Integrate\LOG.VPlus\ConfigSystem.xml` (8,367 bytes)
   - *Ý nghĩa*: Chứa toàn bộ địa chỉ IP máy chủ HIS (`192.168.7.200`, `192.168.7.236`, `192.168.7.239`), cấu hình cổng dịch vụ, tên cơ sở dữ liệu và mã chi nhánh. Thiếu file này, 100% ứng dụng HIS và công cụ AI bị tê liệt.
2. **Hệ Thống Cấu Hình Ứng Dụng `*.exe.config` và `*.exe.config_` (66 tệp)**:
   - Các tệp cấu hình cho toàn bộ công cụ C#: `HisClinicalCli.exe.config`, `HisAutoPrescribe.exe.config`, `HisTrackingCreator.exe.config`, `HisGlucoseBedsideAssigner.exe.config`, `HisRationAssigner.exe.config`, `HisDebateCreator.exe.config`, `HisDiagnosticDoctor.exe.config`, `HisWardReportCreator.exe.config`, `HospitalShiftReporter.exe.config`.
   - Các tệp cấu hình nền tảng HIS: `HIS.exe.config`, `ATU.exe.config`, `HLS.WCFClient.exe.config`, `EMR.exe.Config`, v.v.
   - *Ý nghĩa*: Cấu hình binding chuyển hướng assembly (.NET Assembly Binding Redirects), cấu hình WCF endpoint, timeout kết nối HTTP.
3. **Thư Mục Lắp Ráp Tham Chiếu `ReferencedAssemblies/`**:
   - Chứa **1,177 tệp DLL và phông chữ** của nhà cung cấp Inventec.
   - *Ý nghĩa*: Cung cấp DTO EFMODEL, Filter, SDO, thư viện giao tiếp WCF cho `csc.exe` biên dịch.
4. **Thư Mục Nhật Ký Hoạt Động Trực Tiếp `Logs/`**:
   - `LogSystem.txt` (712+ KB), `HLSLogSystem.txt`, `LogSession.txt`, `LogAction.txt`.
   - *Ý nghĩa*: Chứa luồng dữ liệu sống của phiên làm việc bác sĩ, đặc biệt là chuỗi **`TokenCode`** 64 ký tự hex. Nếu xóa file này, công cụ AI không thể xác thực vào máy chủ MOS.
5. **Các Nhị Phân & DLL Runtime Cốt Lõi**:
   - `HIS.exe`, `HIS1.exe`, `HIS2.exe`, `HIS4.5.exe`, `ATU.exe`, `MOS.EFMODEL.dll`, `LIS.EFMODEL.dll`, `System.Data.SQLite.dll`, `WebView2Loader.dll`.
   - Các thư mục nghiệp vụ: `DB/`, `Tmp/`, `Tool/`, `Setup/`, `Plugins/`, `ModuleDesign/`, `Font/`, `Img/`, `Voice/`, `x64/`, `x86/`, `de/`, `en/`, `vi/`, `my/`, `Integrate/`, `PDFNet/`, `Resources/`, `Everest/`.
6. **Bảo Mật & Thông Tin Xác Thực (Credentials Audit)**:
   - Remote Git Origin lưu Personal Access Token (PAT) trong cấu hình Git:
     `https://onthibacsinoitru9999-coder:gho_...q7qgx@github.com/onthibacsinoitru9999-coder/hisx64-crossover.git`.
   - `HisPacsCli.ps1` chứa thông tin đăng nhập tài khoản RIS Minerva nội bộ (`Account = "ctch"`, `Password = "ctchCS2026!"`).
   - `OPENROUTER_API_KEY` và `GEMINI_API_KEY` được nạp từ biến môi trường của hệ điều hành.

---

## 5. KẾT QUẢ ĐO KIỂM CÁC CHỐT KIỂM THỬ TRỌNG YẾU (`R5`)

Explorer 3 đã trực tiếp thực thi các mục tiêu kiểm định tự động theo Yêu cầu R5 với kết quả cụ thể:

### 5.1. Kiểm Tra Sức Khỏe Toàn Diện: `HisDiagnosticDoctor.bat health`
- **Lệnh thực thi**: `cmd.exe /c HisDiagnosticDoctor.bat health`
- **Thời gian phản hồi**: ~1.2 giây.
- **Chi tiết kết quả quan sát**:
  - `[✅ OK] MOS Backend API (192.168.7.236:1608)`
  - `[✅ OK] ACS Auth Service (192.168.7.200:1401)`
  - `[✅ OK] SDA Data Service (192.168.7.200:1410)`
  - `[✅ OK] EMR Document API (192.168.7.239:1415)`
  - `✅ TokenCode đang hoạt động: 8b03f4490c1d49a9...04b2b80a`
  - `✅ Gọi API MOS Backend thành công! Khoa: Khoa Chấn thương Chỉnh hình và Cột sống (ID: 57)`
  - `⚠️ Chưa cấu hình OPENROUTER_API_KEY.`
- **Kết luận**: **`🎯 KẾT LUẬN CHẨN ĐOÁN: Hệ thống sẵn sàng 100%!`** (Đạt Acceptance Criteria).

### 5.2. Kiểm Tra Tốc Độ Danh Mục Mô Hình AI: `HisAiCli.bat models`
- **Lệnh thực thi**: `powershell -Command "$sw = [System.Diagnostics.Stopwatch]::StartNew(); cmd.exe /c HisAiCli.bat models; $sw.Stop(); $sw.ElapsedMilliseconds"`
- **Thời gian phản hồi**: **145 ms** (Thấp hơn rất nhiều so với ngưỡng quy định 2,000 ms).
- **Chi tiết kết quả**: In đầy đủ danh mục mô hình Free Tier 1M tokens đã phân loại theo 3 nhóm nghiệp vụ:
  1. *Vision/OCR*: `minimax/minimax-m3:free`, `google/gemma-4-31b-it:free`, v.v.
  2. *Reasoning*: `minimax/minimax-m3:free`, `nvidia/nemotron-3.5-lightning:free`, `z-ai/glm-5.2:free`.
  3. *Code/JSON*: `minimax/minimax-m3:free`, `poolside/laguna-s-2.1:free`, `z-ai/glm-5.2:free`.
- **Kết luận**: **ĐẠT 100%**.

### 5.3. Kiểm Định Luồng Điều Phối Đường Huyết: `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm`
- **Lệnh thực thi**: `powershell -ExecutionPolicy Bypass -File .\HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm`
- **Chi tiết kết quả quan sát**:
  - Tự động nhận diện 3 công cụ: `HisTrackingCreator.exe`, `HisGlucoseBedsideAssigner.exe`, `HisAutoPrescribe.exe` trong thư mục scripts.
  - Tự động khởi tạo dữ liệu mẫu kiểm thử chuẩn cho bệnh nhân `0003969449` gồm 3 phiên đo đường huyết (17h, 21h, 6h).
  - Chuẩn bị 3 y lệnh Tờ điều trị, 3 y lệnh chỉ định BM02426, 3 y lệnh kê đơn tiêm Insulin (Actrapid, Lantus).
  - Chạy mô phỏng Dry-Run thành công 9/9 y lệnh.
  - Tổng kết: `Tờ điều trị: 3/0 | Chỉ định BM02426: 3/0 | Kê đơn Insulin: 3/0 | TỔNG: 9/0`.
- **Kết luận**: **HOÀN THÀNH TOÀN BỘ, 0 LỖI**.

---

## 6. PHÂN TÍCH TÌNH TRẠNG GIT REPOSITORY & QUY TẮC BỎ QUA (`.gitignore`)

### 6.1. Trạng Thái Repository:
- **Nhánh làm việc**: `main`
- **Commit hiện tại**: `62a30dd feat(pacs): add his-pacs-viewer skill and HisPacsCli automation tool` (Đồng bộ với `origin/main`).
- **Remote URL**: `https://onthibacsinoitru9999-coder:gho_...q7qgx@github.com/onthibacsinoitru9999-coder/hisx64-crossover.git`
- **File chưa theo dõi (Untracked)**:
  - Chỉ gồm các tài liệu và thư mục làm việc của Agent: `.agents/ORIGINAL_REQUEST.md`, `.agents/explorer_survey_1/`, `.agents/explorer_survey_2/`, `.agents/explorer_survey_3/`, `.agents/orchestrator_1/`, `.agents/sentinel_1/`.
- **Lưu ý môi trường**: Lệnh `git` cần được nạp qua `set_env.bat` vì MinGit được cài đặt trong thư mục WinGet LocalAppData (`C:\Users\HP\AppData\Local\Microsoft\WinGet\Packages\Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe\cmd\git.exe`) và chưa được đưa vĩnh viễn vào System PATH.

### 6.2. Cơ Chế Hoạt Động Của `.gitignore`:
Tệp `.gitignore` áp dụng chiến lược **Block-All + Explicit Whitelist**:
1. Dòng 6: `/*` -> Bỏ qua toàn bộ nội dung trong thư mục gốc theo mặc định.
2. Dòng 26-33: Mở quyền theo dõi tường minh cho các tệp tri thức và mã nguồn:
   ```gitignore
   !.gitignore
   !*.md
   !*.cs
   !*.bat
   !*.ps1
   !*.py
   !.agents/
   !.agents/**
   ```
Chiến lược này rất hiệu quả vì tự động ngăn ngừa việc vô tình commit các DLL (1,177 files), nhị phân `.exe`, logs hoặc cấu hình cục bộ nhạy cảm lên Git.

---

## 7. QUY TẮC DỌN DẸP & ĐẶC TẢ BỘ KIỂM ĐỊNH TỰ ĐỘNG (CLEANUP RULES & TEST HARNESS SPEC)

### 7.1. Quy Tắc Dọn Dẹp File Rác (Cleanup Rules):

Quy trình dọn dẹp cần được thực hiện thông qua script tự động an toàn với các bước kiểm tra nghiêm ngặt:
1. **Chỉ nhắm mục tiêu (Target Whitelist)**:
   - Các file có mẫu tên: `diabetes_orchestrator_*.log`, `insulin_orders_temp.csv`, `output_3e.txt`.
   - Các file rác chỉ định: `Test20211012.txt`, `readmebk.txt`.
   - Thư mục cache: `__pycache__` (và các `*.pyc`).
2. **Rào chắn bảo vệ (Safety Assertion)**:
   - Trước khi xóa, kiểm tra tệp **không** nằm trong danh mục bảo vệ (`ConfigSystem.xml`, `*.exe.config`, `ReferencedAssemblies/`, `Logs/`, `*.dll`, `*.cs`, `*.bat`, `*.ps1`, `*.py`, `*.md`).
3. **Đoạn mã dọn dẹp chuẩn hóa (PowerShell snippet)**:
   ```powershell
   $garbageFiles = @(
       "output_3e.txt",
       "insulin_orders_temp.csv",
       "Test20211012.txt",
       "readmebk.txt"
   )
   $garbagePatterns = @("diabetes_orchestrator_*.log")

   foreach ($f in $garbageFiles) {
       if (Test-Path $f) { Remove-Item -Force $f; Write-Host "Đã xóa: $f" }
   }
   foreach ($pat in $garbagePatterns) {
       Get-ChildItem -Path . -Filter $pat | ForEach-Object {
           Remove-Item -Force $_.FullName; Write-Host "Đã xóa log: $($_.Name)"
       }
   }
   if (Test-Path "__pycache__") { Remove-Item -Recurse -Force "__pycache__" }
   ```

### 7.2. Đặc Tả Bộ Kiểm Thử Tự Động Toàn Diện (Automated Test Harness Specification - `test_harness.ps1`):

Để đáp ứng đầy đủ Yêu cầu R5 và Acceptance Criteria, bộ kiểm thử tự động khép kín cần thực thi tuần tự 4 giai đoạn:

```powershell
<#
================================================================================
HIS AUTOMATION SYSTEM-WIDE VERIFICATION TEST HARNESS (R5 SPECIFICATION)
================================================================================
#>
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " 🚀 BẮT ĐẦU KIỂM THỬ KHÉP KÍN TOÀN BỘ HỆ THỐNG HIS AUTOMATION 🚀 " -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan

# GIAI ĐOẠN 1: KIỂM TRA CÚ PHÁP TOÀN BỘ SCRIPT POWERSHELL (*.ps1)
Write-Host "`n[TEST 1/4] Kiểm tra cú pháp AST toàn bộ file *.ps1..." -ForegroundColor Cyan
$psFiles = Get-ChildItem -Path . -Filter "*.ps1"
$psFailCount = 0
foreach ($ps in $psFiles) {
    $tokens = $null; $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($ps.FullName, [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) {
        Write-Host "  ❌ $($ps.Name): $($errors.Count) lỗi cú pháp!" -ForegroundColor Red
        $psFailCount++
    } else {
        Write-Host "  ✅ $($ps.Name): Cú pháp 100% chuẩn xác." -ForegroundColor Green
    }
}
if ($psFailCount -gt 0) { throw "Test 1 thất bại: Có $psFailCount script PowerShell lỗi cú pháp!" }

# GIAI ĐOẠN 2: KIỂM TRA SỨC KHỎE HỆ THỐNG & KẾT NỐI MÁY CHỦ (HEALTH DIAGNOSTIC)
Write-Host "`n[TEST 2/4] Chạy HisDiagnosticDoctor.bat health..." -ForegroundColor Cyan
$healthOutput = cmd.exe /c HisDiagnosticDoctor.bat health
$healthReady = ($healthOutput | Where-Object { $_ -match "Hệ thống sẵn sàng 100%" }).Count -gt 0
if (-not $healthReady) {
    throw "Test 2 thất bại: HisDiagnosticDoctor không đạt kết luận sẵn sàng 100%!"
}
Write-Host "  ✅ Health check đạt: Hệ thống sẵn sàng 100%." -ForegroundColor Green

# GIAI ĐOẠN 3: KIỂM TRA ĐỘ TRỄ PHẢN HỒI CLI MÔ HÌNH AI (AI CLI LATENCY)
Write-Host "`n[TEST 3/4] Đo thời gian phản hồi HisAiCli.bat models..." -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$modelsOutput = cmd.exe /c HisAiCli.bat models
$sw.Stop()
$elapsedMs = $sw.ElapsedMilliseconds
Write-Host "  ℹ️ Thời gian phản hồi: $elapsedMs ms" -ForegroundColor Gray
if ($elapsedMs -gt 2000) {
    throw "Test 3 thất bại: HisAiCli.bat models vượt quá 2,000 ms ($elapsedMs ms)!"
}
Write-Host "  ✅ AI CLI phản hồi siêu tốc ($elapsedMs ms < 2000 ms)." -ForegroundColor Green

# GIAI ĐOẠN 4: KIỂM TRA ĐIỀU PHỐI ĐƯỜNG HUYẾT ĐTĐ (DIABETES ORCHESTRATOR DRY-RUN)
Write-Host "`n[TEST 4/4] Chạy HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm..." -ForegroundColor Cyan
$orchOutput = powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm
$orchSuccess = ($orchOutput | Where-Object { $_ -match "HOÀN THÀNH TOÀN BỘ! Không có lỗi" }).Count -gt 0
if (-not $orchSuccess) {
    throw "Test 4 thất bại: HisDiabetesOrchestrator gặp lỗi trong luồng điều phối!"
}
Write-Host "  ✅ Điều phối ĐTĐ mô phỏng thành công 100% (9/9 y lệnh)." -ForegroundColor Green

# TỰ ĐỘNG DỌN DẸP LOG VÀ CSV SAU KHI TEST ORCHESTRATOR
Get-ChildItem -Path . -Filter "diabetes_orchestrator_*.log" | Remove-Item -Force
if (Test-Path "insulin_orders_temp.csv") { Remove-Item -Force "insulin_orders_temp.csv" }
if (Test-Path "glucose_data.json") { Remove-Item -Force "glucose_data.json" }
Write-Host "  🧹 Đã dọn dẹp sạch sẽ các tệp sinh ra trong quá trình kiểm thử." -ForegroundColor Gray

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " 🎉 CHÚC MỪNG: TOÀN BỘ 4 CHỐT KIỂM ĐỊNH ĐÃ VƯỢT QUA 100%! 🎉 " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan
```

---

## 8. BẢNG PHÂN CÔNG KHUYẾN NGHỊ CHO CÁC TÁC NHÂN TIẾP THEO

1. **Dành cho Agent Chỉnh Sửa & Tối Ưu (Implementer/Optimizer)**:
   - Sửa dòng 689 trong `HisWardReportCreator.cs` để khôi phục cú pháp hợp lệ và lưu dưới bảng mã UTF-8.
   - Thêm UTF-8 BOM (`[System.Text.Encoding]::UTF8`) cho `WatchHoiChan.ps1`, `build_autoprescribe.ps1`, `build_clinical_cli.ps1`, `HisPacsCli.ps1`.
   - Chuyển mã `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs` từ UTF-16 LE sang UTF-8.
   - Thực thi quy trình dọn dẹp 8 tệp rác/tồn đọng đã liệt kê tại Mục 4.1.
2. **Dành cho Agent Kiểm Định (Verifier/Sentinel)**:
   - Sử dụng đặc tả bộ kiểm thử `test_harness.ps1` tại Mục 7.2 để chạy kiểm định khép kín và xác nhận 100% hệ thống sẵn sàng trước khi đóng gói bàn giao.
