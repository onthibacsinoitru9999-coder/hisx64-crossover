# BÁO CÁO KHẢO SÁT CHUYÊN SÂU: REQUIREMENT R1
## Rà Soát, Đánh Giá & Tối Ưu Hóa 100% Batch Files & Toolchain Links
**Dự án**: HIS Automation & Clinical Intelligence Cross-Over  
**Người thực hiện**: Explorer 1  
**Thời gian khảo sát**: 2026-09-09T17:00:00Z  
**Thư mục làm việc**: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`

---

## I. TỔNG QUAN KHẢO SÁT (EXECUTIVE SUMMARY)

Thực hiện yêu cầu tại **Requirement R1** (từ `ORIGINAL_REQUEST.md` và `AGENTS.md`), Explorer 1 đã tiến hành rà soát toàn diện 100% các file kịch bản thực thi `.bat` trên toàn bộ cây thư mục gốc và thư mục con của dự án. 

### Các chỉ số cốt lõi:
- **Tổng số file `.bat` được phát hiện**: **27 file**
  - **18 file** tại thư mục gốc (Root Automation & CLI Tool wrappers).
  - **2 file** tại thư mục Skill & Build (`.agents\skills\his-clinical-operations\scripts\`).
  - **7 file** tại các thư mục thành phần phụ / Vendor Legacy (`Integrate\`, `Setup\`, `Tool\`).
- **Tỷ lệ nạp môi trường chuẩn (`set_env.bat`)**: Hiện chỉ có **5/27 file (18.5%)** chủ động gọi `set_env.bat` (`Cai_Dat_He_Thong_Support.bat`, `HisAiCli.bat`, `HisDiagnosticDoctor.bat`, `sync_pull.bat`, `sync_push.bat`). Còn lại **22 file (81.5%)** chưa nạp môi trường tự động!
- **Tỷ lệ có rủi ro đường dẫn gán cứng (Hardcoded Paths)**: Phát hiện nhiều điểm gán cứng ổ đĩa `D:\`, `E:\`, `C:\` nghiêm trọng khiến script gãy đổ ngay lập tức khi di chuyển sang máy khác hoặc ổ đĩa khác (điển hình: `build_fetch.bat`, `build_autoprescribe.bat`, `set_env.bat`, `copyDll.bat`, `generate_html_report.py`, `WatchBranchLearning.ps1`, `WatchHoiChan.ps1`).
- **Lỗi xử lý đường dẫn chứa khoảng trắng (Space in Paths)**: Thư mục dự án hiện tại có khoảng trắng (`HIS CSNB`). Trong `Cai_Dat_He_Thong_Support.bat` dòng 53-54 viết thiếu dấu ngoặc kép (`%SCRIPT_DIR%HisClinicalCli.exe`), khiến CMD báo lỗi cú pháp.
- **Thiếu `cd /d "%~dp0"`**: Nhiều script khởi động ứng dụng lâm sàng (`Chay_HisAutoPrescribe.bat`, `Chay_Tao_ToDieuTri.bat`, `HisPacsCli.bat`, v.v.) không chuyển thư mục làm việc về vị trí script, dẫn tới lỗi nạp file cấu hình `ConfigSystem.xml` hoặc log khi chạy từ shortcut ngoài Desktop hoặc shell khác.

---

## II. MA TRẬN ĐÁNH GIÁ CHI TIẾT 27 BATCH FILES

| STT | Đường dẫn File `.bat` | Nhóm chức năng | Gọi `set_env`? | Có `cd /d "%~dp0"`? | UTF-8 (`chcp 65001`) | Đường dẫn gán cứng / Bẫy lỗi phát hiện | Mức độ rủi ro |
| :--- | :--- | :--- | :---: | :---: | :---: | :--- | :---: |
| 1 | `set_env.bat` | Core Env Loader | - | ❌ | ❌ | Gán cứng `C:\Program Files\Git\cmd`, `C:\MinGW\bin`, `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. Thiếu `%SystemRoot%`, thiếu Python system-wide, thiếu rclone. | 🔴 Cao |
| 2 | `sync_pull.bat` | Git Sync Pull | ✅ | ✅ | ✅ | Không có rủi ro nghiêm trọng. Đã nạp môi trường tốt. | 🟢 Thấp |
| 3 | `sync_push.bat` | Git Sync Push | ✅ | ✅ | ✅ | Không có rủi ro nghiêm trọng. Đã nạp môi trường tốt. | 🟢 Thấp |
| 4 | `Cai_Dat_He_Thong_Support.bat` | System Init & Verify | ✅ | ⚠️ Lỗi khoảng trắng | ✅ | Dòng 9 `cd /d %SCRIPT_DIR%` và dòng 53-54 `%SCRIPT_DIR%HisClinicalCli.exe` không có dấu ngoặc kép `"`, vỡ lệnh khi đường dẫn có dấu cách. | 🔴 Cao |
| 5 | `HisAiCli.bat` | OpenRouter AI CLI | ✅ | ❌ | ❌ | Thiếu `cd /d "%SCRIPT_DIR%"`, thiếu `chcp 65001 >nul`. | 🟡 Vừa |
| 6 | `HisDiagnosticDoctor.bat` | Diagnostic Doctor | ✅ | ❌ | ❌ | Thiếu `cd /d "%SCRIPT_DIR%"`, thiếu `chcp 65001 >nul`. | 🟡 Vừa |
| 7 | `Chay_ChiDinh_BM02426.bat` | GUI Bedside Assigner | ❌ | ✅ | ✅ | Chưa gọi `set_env.bat`. | 🟡 Vừa |
| 8 | `Chay_HisAutoPrescribe.bat` | GUI Auto Prescribe | ❌ | ❌ | ❌ | Quá sơ sài (3 dòng), không `cd /d`, không `set_env`, gọi `start HisAutoPrescribe.exe` không kiểm tra file tồn tại. | 🔴 Cao |
| 9 | `Chay_Tao_ToDieuTri.bat` | GUI Tracking Creator | ❌ | ❌ | ✅ | Chưa gọi `set_env.bat`, thiếu `cd /d "%~dp0"`. | 🟡 Vừa |
| 10 | `HisBranchWatcher.bat` | Live Event Monitor | ❌ | ❌ | ✅ | Chưa gọi `set_env.bat`, thiếu `cd /d "%~dp0"`. Script PS1 con chứa hardcoded `D:\` và `E:\`. | 🔴 Cao |
| 11 | `HisConsultationReport.bat` | Consultation Report | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d`, gán cứng tên file `20260903.xlsx` và `20260903.csv`. Biến gán đường dẫn không bọc quotes (`set REPORT_HTML=%SCRIPT_DIR%...`). | 🔴 Cao |
| 12 | `HisLeanproAssigner.bat` | Leanpro ERAS Assigner | ❌ | ✅ | ✅ | Chưa gọi `set_env.bat`. Dòng 8 bị lỗi font unicode (`?? HIS LEANPRO...`). | 🟡 Vừa |
| 13 | `HisPacsCli.bat` | PACS/RIS Viewer CLI | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d "%~dp0"`, thiếu `chcp 65001`. | 🟡 Vừa |
| 14 | `HisRationAssigner.bat` | Clinical Ration CLI | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d "%SCRIPT_DIR%"`, thiếu `chcp 65001`. | 🟡 Vừa |
| 15 | `HisSummaryTrackingCreator.bat` | Summary Tracking CLI | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d "%SCRIPT_DIR%"`, thiếu `chcp 65001`. | 🟡 Vừa |
| 16 | `HisSummaryTrackingDoctor.bat` | Summary Doctor CLI | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d "%SCRIPT_DIR%"`, thiếu `chcp 65001`. | 🟡 Vừa |
| 17 | `HisWardReport.bat` | Ward Report CLI | ❌ | ❌ | ❌ | Chưa gọi `set_env.bat`, thiếu `cd /d`. Gọi lệnh `rclone copy` trực tiếp nhưng rclone không có trong PATH toàn cục và không kiểm tra file tồn tại. | 🔴 Cao |
| 18 | `Kiem_Tra_Ket_Noi_HIS.bat` | Tailscale/Network Check | ❌ | ❌ | ✅ | Chưa gọi `set_env.bat`, thiếu `cd /d "%~dp0"`. Gọi powershell thiếu cờ `-NoProfile`. | 🟡 Vừa |
| 19 | `.agents\...\build_autoprescribe.bat` | C# Build Script | ❌ | ❌ | ✅ | Gán cứng `set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. Không gọi `set_env.bat`. Lỗi lồng quotes `%OUT%`. | 🔴 Cao |
| 20 | `.agents\...\build_fetch.bat` | C# Build Script | ❌ | ❌ | ❌ | **Gán cứng toàn bộ đường dẫn máy cũ**: `D:\his\his-x64-28-11fix GDYK\his-x64...`, `set CSC=C:\Windows\...`. Không thể chạy được trên bất kỳ máy nào khác! | 🔴 Nghiêm trọng |
| 21 | `Integrate\Cache\...\service-install.bat` | Redis Service (Vendor) | ❌ | ❌ | ❌ | Script của bên thứ ba (Redis x86). Thiếu `cd /d "%~dp0"`. | ⚪ Legacy |
| 22 | `Integrate\Cache\...\uninstall-service.bat` | Redis Service (Vendor) | ❌ | ❌ | ❌ | Script của bên thứ ba (Redis x86). | ⚪ Legacy |
| 23 | `Integrate\EMR\copyDll.bat` | EMR Hot-Update (Vendor) | ❌ | ❌ | ❌ | Gán cứng đường dẫn máy lập trình viên Medilink: `D:\code\Medilink\EMR\EMRHIS1\bin\Debug\...`. Hoàn toàn không còn giá trị thực thi. | ⚪ Rác/Legacy |
| 24 | `Setup\Redis\x86\service-install.bat` | Redis Service (Vendor) | ❌ | ❌ | ❌ | Trùng lặp với file số 21. | ⚪ Legacy |
| 25 | `Setup\Redis\x86\uninstall-service.bat` | Redis Service (Vendor) | ❌ | ❌ | ❌ | Trùng lặp với file số 22. | ⚪ Legacy |
| 26 | `Tool\Anydesk\getAnydeskID.bat` | Anydesk Utility (Vendor) | ❌ | ❌ | ❌ | Script tiện ích lấy Anydesk ID cho kỹ thuật viên HIS. | ⚪ Legacy |
| 27 | `Tool\Anydesk\testbat.bat` | Anydesk Set Password | ❌ | ❌ | ❌ | Script tạm gán mật khẩu Anydesk `123456a@`. | ⚪ Rác/Cần dọn |

---

## III. PHÂN TÍCH CHUYÊN SÂU BỘ NẠP MÔI TRƯỜNG `set_env.bat`

### 1. Hiện trạng mã nguồn `set_env.bat`:
```cmd
@echo off
:: set_env.bat: Tu dong nap Git, Python, MinGW C++, .NET C# vao PATH
if defined HIS_ENV_READY goto :eof

:: 1. Git
for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\Git.MinGit*") do if exist "%%p\cmd\git.exe" set "PATH=%%p\cmd;%PATH%"
if exist "C:\Program Files\Git\cmd\git.exe" set "PATH=C:\Program Files\Git\cmd;%PATH%"

:: 2. Python 3.12+
for /d %%p in ("%LOCALAPPDATA%\Programs\Python\Python*") do if exist "%%p\python.exe" set "PATH=%%p;%%p\Scripts;%PATH%"

:: 3. C++ MinGW (GCC/G++)
for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs*") do if exist "%%p\mingw64\bin\g++.exe" set "PATH=%%p\mingw64\bin;%PATH%"
if exist "C:\MinGW\bin\g++.exe" set "PATH=C:\MinGW\bin;%PATH%"

:: 4. C# (csc.exe)
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319;%PATH%"

set HIS_ENV_READY=1
```

### 2. Các điểm nghẽn & lỗ hổng kỹ thuật phát hiện:

1. **Gán cứng ổ đĩa `C:\Windows` cho C# Compiler (`csc.exe`)**:
   - `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`: Nếu hệ điều hành Windows cài trên ổ đĩa khác hoặc biến `%SystemRoot%` khác `C:\Windows`, lệnh này sẽ bỏ sót.
   - Không có cơ chế fallback sang bản 32-bit (`Framework\v4.0.30319\csc.exe`) hoặc bản Roslyn/MSBuild mới nếu máy trạm là Windows Server/32-bit.
   - **Cách sửa chuẩn hóa**: Sử dụng `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319` và kiểm tra fallback `%SystemRoot%\Microsoft.NET\Framework\v4.0.30319`.

2. **Gán cứng `C:\Program Files\Git\cmd\git.exe`**:
   - Bỏ sót các vị trí cài đặt phổ biến:
     - `%ProgramFiles%\Git\cmd`
     - `%ProgramFiles(x86)%\Git\cmd`
     - `%LOCALAPPDATA%\Programs\Git\cmd`
     - Portable Git trong thư mục `Tool\Git` hoặc ổ di động.

3. **Thuật toán dò Python còn hạn chế**:
   - Chỉ quét `%LOCALAPPDATA%\Programs\Python\Python*`.
   - Bỏ sót Python cài đặt System-wide (`%ProgramFiles%\Python*`, `C:\Python3*`), môi trường ảo `.venv\Scripts`, hoặc trình khởi chạy Windows Python Launcher `py.exe` (vốn nằm sẵn trong `%SystemRoot%\py.exe`).
   - Nguy cơ: Nếu máy chưa chạy `set_env.bat`, lệnh `python` sẽ gọi vào Windows Store stub (`C:\Users\HP\AppData\Local\Microsoft\WindowsApps\python.exe`) và gây mở Microsoft Store thay vì chạy script!

4. **Thiếu hỗ trợ công cụ đồng bộ Cloud Drive (`rclone`)**:
   - `HisWardReport.bat` và `HisConsultationReport.bat` đều cần `rclone` để đẩy báo cáo buồng lên Google Drive (`gdrive:BaoCaoBuongBenh_Khoa57` và `gdrive:HC BM`).
   - `set_env.bat` hiện tại hoàn toàn không dò tìm hoặc thiết lập PATH cho `rclone.exe`.

5. **Vấn đề lệch phiên giữa Batch (CMD) và PowerShell**:
   - Khi chạy từ PowerShell (`powershell.exe` hoặc `pwsh`), các lệnh `set PATH=...` trong file `.bat` con **chỉ tồn tại trong process CMD con** và **không thể tác động ngược lại** biến `$env:PATH` của phiên PowerShell đang chạy.
   - Do đó, khi bác sĩ hoặc Agent mở PowerShell và gõ trực tiếp `git pull` hoặc `csc`, PowerShell sẽ báo lỗi `The term 'git' is not recognized`.
   - **Giải pháp kiến trúc**: Cần xây dựng song song file `set_env.ps1` (hoặc module nạp tự động) để PowerShell có thể nạp môi trường nhất quán 100% với Batch.

---

## IV. RÀ SOÁT CÁC ĐƯỜNG DẪN GÁN CỨNG (HARDCODED PATH AUDIT)

Dưới đây là danh sách chi tiết các file chứa đường dẫn tuyệt đối gán cứng đe dọa trực tiếp khả năng di động và chạy đa máy của hệ thống:

### 1. File `.agents\skills\his-clinical-operations\scripts\build_fetch.bat`:
```cmd
Line 3: set SCRIPTS_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\.agents\skills\his-clinical-operations\scripts
Line 4: set HIS_ROOT=D:\his\his-x64-28-11fix GDYK\his-x64
Line 5: set REF_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies
Line 6: set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```
*Tác hại*: Toàn bộ 4 dòng đều gán cứng ổ `D:\` và `C:\`. Khi chạy tại thư mục hiện tại (`F:\NB\...`), file này báo lỗi 100% không tìm thấy thư mục và không thể biên dịch `FetchPatient.cs`.

### 2. File `.agents\skills\his-clinical-operations\scripts\build_autoprescribe.bat`:
```cmd
Line 13: set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```
*Tác hại*: Gán cứng ổ C:\ cho csc.exe và không gọi `set_env.bat`.

### 3. File `Integrate\EMR\copyDll.bat`:
```cmd
Line 5: xcopy "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\copyDll.bat" ...
Line 6: move /y "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\NewUpdate\*" "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\"
Line 7: cd "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\NewUpdate\"
Line 11: del "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\NewUpdate\*"  / s / f / q
Line 12: cd "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\"
Line 13: start "" "D:\code\Medilink\EMR\EMRHIS1\bin\Debug\EMR.exe"
```
*Tác hại*: Script cũ từ bên thứ ba (Medilink), gán cứng toàn bộ mã nguồn ổ D.

### 4. File Python `generate_html_report.py`:
```python
Line 7: md_path = r"e:\his-x64-28-11fix GDYK\his-x64\Reports\BaoCao_HoiChan_CTCH_NinhBinh.md"
Line 8: html_path = r"e:\his-x64-28-11fix GDYK\his-x64\Reports\BaoCao_HoiChan_CTCH_NinhBinh.html"
Line 9: drive_dir = r"C:\Users\1995\OneDrive\BaoCaoBuongBenh_Khoa57"
```
*Tác hại*: Gán cứng ổ `e:\` và đường dẫn OneDrive cá nhân `C:\Users\1995\...`.

### 5. Các kịch bản PowerShell (`WatchBranchLearning.ps1`, `WatchHoiChan.ps1`, `build_*.ps1`):
- `WatchBranchLearning.ps1`:
  ```powershell
  $targetDirs = @(
      "$PSScriptRoot\Logs",
      "Logs",
      "E:\his-x64-28-11fix GDYK\his-x64\Logs",
      "D:\his\his-x64-28-11fix GDYK\his-x64\Logs"
  )
  ```
- `WatchHoiChan.ps1`:
  ```powershell
  $targetDirs = @(
      "$PSScriptRoot\Logs",
      "Logs",
      "D:\his\his-x64-28-11fix GDYK\his-x64\Logs",
      "E:\his-x64-28-11fix GDYK\his-x64\Logs"
  )
  ```
- `build_clinical_cli.ps1` & `build_autoprescribe.ps1`:
  ```powershell
  $cscPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
  ```

---

## V. ĐÁNH GIÁ TƯƠNG TÁC ĐA SHELL (CMD, POWERSHELL, GIT BASH)

1. **CMD (Command Prompt)**:
   - Là shell thực thi tự nhiên của các file `.bat`.
   - Lỗi phổ biến nhất: Quên lệnh `call` khi một file `.bat` gọi một file `.bat` khác. Nếu không có `call`, CMD sẽ chuyển hẳn luồng điều khiển và không bao giờ quay lại file gọi ban đầu. Hiện tại `Cai_Dat_He_Thong_Support.bat` đã dùng `call` đúng cách, nhưng các file tiện ích lâm sàng khác chưa tái sử dụng được nhau.
   - Vấn đề khoảng trắng: Thư mục chứa khoảng trắng (`.../his/HIS CSNB/`) bắt buộc mọi biến tham chiếu thư mục (`%~dp0`, `%SCRIPT_DIR%`, `%ROOT_DIR%`) phải được bọc trong cặp ngoặc kép `""`.

2. **Windows PowerShell (5.1 & Core)**:
   - Khi PowerShell chạy file `.bat`, PowerShell gọi `cmd.exe /c "script.bat"`. Biến môi trường chỉ có tác dụng trong phiên CMD con.
   - Các file `.bat` gọi ngược lại PowerShell (như `HisBranchWatcher.bat`, `HisPacsCli.bat`, `Kiem_Tra_Ket_Noi_HIS.bat`):
     - `HisBranchWatcher.bat`: `powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WatchBranchLearning.ps1"` -> Tốt.
     - `HisPacsCli.bat`: `powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0HisPacsCli.ps1" %*` -> Tốt.
     - `Kiem_Tra_Ket_Noi_HIS.bat`: `powershell.exe -ExecutionPolicy Bypass -File "%~dp0check_his_tunnel.ps1"` -> Thiếu `-NoProfile`, có thể bị chậm do nạp PowerShell profile cá nhân.

3. **Git Bash (MSYS2 / MinGW)**:
   - Người dùng hoặc Agent trong Git Bash gọi `./sync_pull.bat` hoặc `./HisAiCli.bat`:
     - Git Bash sẽ ủy quyền cho CMD thực thi file `.bat`.
     - Tuy nhiên, nếu script `.bat` cố gắng gọi các đường dẫn dạng Unix `/f/NB/...` thay vì `F:\NB\...`, CMD sẽ báo lỗi. Sử dụng `%~dp0` luôn trả về đường dẫn chuẩn Windows (ổ đĩa + dấu gạch chéo ngược `\`), đảm bảo tương thích 100% với CMD kể cả khi gọi từ Git Bash.

---

## VI. LIÊN KẾT TOOLCHAIN & TÍNH TOÀN VẸN CỦA BỘ BIÊN DỊCH (CSC, GIT, PYTHON, RCLONE)

### 1. Trình biên dịch C# (`csc.exe`):
- Để biên dịch các công cụ lâm sàng (`.cs -> .exe`), hệ thống yêu cầu trình biên dịch `csc.exe` x64 của .NET Framework 4.5+ (`v4.0.30319`).
- Hiện tại, cả 4 script biên dịch (`build_autoprescribe.bat`, `build_fetch.bat`, `build_autoprescribe.ps1`, `build_clinical_cli.ps1`) đều tự định nghĩa cứng đường dẫn `csc.exe` thay vì lấy từ PATH hoặc dùng cơ chế nạp tập trung.
- Khi chuẩn hóa: `set_env.bat` sẽ đưa thư mục chứa `csc.exe` vào PATH. Mọi script biên dịch chỉ cần gọi `csc.exe` trực tiếp hoặc trỏ tới biến `%CSC%` được `set_env.bat` chuẩn hóa.

### 2. Trình quản lý phiên bản Git (`git.exe`):
- Khi nạp qua WinGet, Git nằm tại `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Git.MinGit*`.
- `sync_pull.bat` và `sync_push.bat` đã nạp môi trường qua `set_env.bat` và hoạt động trơn tru.
- Tuy nhiên, khi Agent làm việc trực tiếp trên terminal (theo quy tắc Pre-flight Sync của `AGENTS.md`), nếu terminal chưa có Git trong PATH thì lệnh `git pull origin main` sẽ thất bại.
- Cần có `set_env.ps1` để Agent hoặc Bác sĩ có thể dot-source `. .\set_env.ps1` ngay đầu phiên PowerShell.

### 3. Trình thông dịch Python (`python.exe`):
- Python 3.12 được cài tại `%LOCALAPPDATA%\Programs\Python\Python312`.
- Trong `HisAiCli.bat`, script đã kiểm tra:
  ```cmd
  where python >nul 2>nul
  where uv >nul 2>nul
  if exist "%USERPROFILE%\.local\bin\uv.exe"
  ```
  Đây là cơ chế kiểm tra đa tầng rất tốt, cần được nhân rộng sang các script khác nếu có tương tác Python.

### 4. Công cụ đồng bộ Cloud (`rclone.exe`):
- Hệ thống hiện tại chưa có file nhị phân `rclone.exe` (quét trong `%LOCALAPPDATA%` và thư mục dự án đều không có).
- Trong `HisWardReport.bat`:
  ```cmd
  if exist "%SCRIPT_DIR%Reports\WardReports" (
      rclone copy "%SCRIPT_DIR%Reports\WardReports" "gdrive:BaoCaoBuongBenh_Khoa57" --quiet
  )
  ```
  Lệnh này sẽ gây lỗi `'rclone' is not recognized` nếu máy tính chưa cài đặt rclone.
- Cần bổ sung kiểm tra an toàn: `where rclone >nul 2>nul` hoặc kiểm tra `if exist "%SCRIPT_DIR%rclone.exe"`, in cảnh báo hướng dẫn thay vì ném lỗi văng ra màn hình.

---

## VII. ĐỀ XUẤT GIẢI PHÁP VÀ KẾ HOẠCH CHUẨN HÓA (ACTIONABLE RECOMMENDATIONS)

Để đáp ứng 100% tiêu chí nghiệm thu của **Requirement R1**, Explorer 1 đề xuất kế hoạch thực thi 4 bước chuẩn hóa như sau:

### Bước 1: Nâng cấp toàn diện `set_env.bat` (Bulletproof Multi-Tier Environment Loader)
Nâng cấp `set_env.bat` với các tính năng:
1. Sử dụng `%SystemRoot%` thay cho `C:\Windows`.
2. Dò tìm `csc.exe` đa tầng: `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319`, `%SystemRoot%\Microsoft.NET\Framework\v4.0.30319`, và MSBuild Roslyn.
3. Dò tìm `git.exe` mở rộng: WinGet MinGit, `%ProgramFiles%\Git\cmd`, `%ProgramFiles(x86)%\Git\cmd`, `%LOCALAPPDATA%\Programs\Git\cmd`.
4. Dò tìm Python mở rộng: `%LOCALAPPDATA%\Programs\Python\Python*`, `%ProgramFiles%\Python*`, `C:\Python3*`, `.venv\Scripts`.
5. Dò tìm `rclone.exe`: `%LOCALAPPDATA%\Programs\rclone`, `%ProgramFiles%\rclone`, hoặc ngay tại `%~dp0rclone.exe`.
6. Tự động thiết lập cờ `HIS_ENV_READY=1` và xuất biến `%CSC%`, `%PYTHON%`, `%GIT%` để các script khác sử dụng trực tiếp nếu muốn.

### Bước 2: Tạo mới file đồng hành `set_env.ps1` (PowerShell Companion Loader)
Cung cấp một file `set_env.ps1` để khi làm việc trong PowerShell:
- Tự động nạp các đường dẫn của Git, Python, csc vào `$env:PATH` của phiên PowerShell hiện tại.
- Cho phép Agent và Bác sĩ gọi `.\set_env.ps1` hoặc nạp tự động trong các script `.ps1`.

### Bước 3: Chuẩn hóa 100% các file `.bat` theo Mẫu Chuẩn (Standardized Blueprint)
Mọi file `.bat` trong thư mục gốc và thư mục con phải tuân theo cấu trúc 5 khối thống nhất:

```cmd
@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

:: 1. Xác định thư mục gốc tuyệt đối an toàn với khoảng trắng
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

:: 2. Nạp môi trường tự động
if exist "%SCRIPT_DIR%set_env.bat" (
    call "%SCRIPT_DIR%set_env.bat"
) else if exist "%SCRIPT_DIR%..\..\..\..\set_env.bat" (
    call "%SCRIPT_DIR%..\..\..\..\set_env.bat"
)

:: 3. Tiêu đề và thông tin ứng dụng
title <Tên Ứng Dụng Chuẩn Hóa>

:: 4. Kiểm tra file thực thi / tham số
if not exist "%SCRIPT_DIR%<TenFile.exe>" (
    echo [LỖI] Không tìm thấy %SCRIPT_DIR%<TenFile.exe>! Vui lòng kiểm tra lại.
    exit /b 1
)

:: 5. Thực thi ứng dụng với đầy đủ tham số
"%SCRIPT_DIR%<TenFile.exe>" %*
exit /b %ERRORLEVEL%
```

### Bước 4: Sửa chữa triệt để các file cá biệt & Dọn dẹp Legacy
1. **`Cai_Dat_He_Thong_Support.bat`**: Bọc toàn bộ các biến `%SCRIPT_DIR%` trong dấu ngoặc kép `""` ở các dòng 8, 9, 10, 49, 53, 54.
2. **`build_fetch.bat`**: Thay thế toàn bộ các đường dẫn cứng `D:\his\...` bằng tính toán tương đối từ `%~dp0`:
   ```cmd
   set "SCRIPTS_DIR=%~dp0"
   set "SCRIPTS_DIR=%SCRIPTS_DIR:~0,-1%"
   for %%i in ("%SCRIPTS_DIR%\..\..\..\..") do set "ROOT_DIR=%%~fi"
   set "REF_DIR=%ROOT_DIR%\ReferencedAssemblies"
   ```
3. **`build_autoprescribe.bat`**: Gọi `set_env.bat`, sử dụng biến `%CSC%` được chuẩn hóa, sửa lỗi lồng quotes khi copy.
4. **`HisWardReport.bat`**: Thêm kiểm tra `where rclone >nul 2>nul` trước khi gọi lệnh đồng bộ Drive.
5. **`HisConsultationReport.bat`**: Chuẩn hóa tên file báo cáo động theo ngày hiện tại thay vì gán cứng ngày `20260903`.
6. **`generate_html_report.py`**: Thay các đường dẫn gán cứng `e:\...` và `C:\Users\1995\...` bằng `Path(__file__).resolve().parent`.
7. **`Tool\Anydesk\testbat.bat`** & **`Integrate\EMR\copyDll.bat`**: Đánh dấu hoặc dọn dẹp theo quy chuẩn dọn dẹp file rác của Requirement R4.

---

## VIII. KẾT LUẬN & KIỂM ĐỊNH TỰ ĐỘNG (VERIFICATION PLAN)

Sau khi đội ngũ triển khai (Implementer) áp dụng các chỉnh sửa trên:
1. **Kiểm tra cú pháp & tính độc lập thư mục**:
   - Mở terminal tại thư mục bất kỳ (ví dụ `C:\Windows\Temp`), gọi thử các file `.bat` bằng đường dẫn tuyệt đối có chứa dấu cách: `"F:\NB\...\his\HIS CSNB\HisAiCli.bat" models` -> Phải chạy thành công.
2. **Kiểm tra khả năng nhận diện Toolchain**:
   - Chạy `set_env.bat` rồi kiểm tra `where git`, `where python`, `where csc` -> 100% tìm thấy đúng phiên bản mong muốn.
3. **Kiểm tra biên dịch C# không phụ thuộc ổ đĩa**:
   - Chạy thử `build_autoprescribe.bat` và `build_fetch.bat` -> Biên dịch thành công và đồng bộ `.exe` ra thư mục gốc mà không báo lỗi đường dẫn.
4. **Kiểm tra sức khỏe hệ thống**:
   - Chạy `HisDiagnosticDoctor.bat health` -> Hoàn tất với kết luận "Hệ thống sẵn sàng 100%".

---
*Báo cáo được khởi tạo và lưu trữ tại: `.agents\explorer_survey_1\survey_report.md`*
