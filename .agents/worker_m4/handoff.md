# Handoff Report — Worker M4: Milestone M4 (Script Encoding & Workspace Hygiene)

## 1. Observation
- **PowerShell AST Parsing Failure on `WatchHoiChan.ps1`**:
  Initial check using `[System.Management.Automation.Language.Parser]::ParseFile` on `WatchHoiChan.ps1` returned verbatim 9 parse errors:
  ```
  File                        Errors
  ----                        ------
  WatchHoiChan.ps1                 9
  ```
  Inspection showed `WatchHoiChan.ps1` lacked a UTF-8 Byte Order Mark (BOM), causing Windows PowerShell 5.1 to parse multibyte Vietnamese characters and emojis under ANSI (Windows-1252), corrupting double-quote string delimiters and generating syntax errors.
- **Hardcoded Drive Paths in Watcher Scripts**:
  - `WatchHoiChan.ps1` lines 7-8:
    ```powershell
    "D:\his\his-x64-28-11fix GDYK\his-x64\Logs",
    "E:\his-x64-28-11fix GDYK\his-x64\Logs"
    ```
  - `WatchBranchLearning.ps1` lines 9-10:
    ```powershell
    "E:\his-x64-28-11fix GDYK\his-x64\Logs",
    "D:\his-x64-28-11fix GDYK\his-x64\Logs"
    ```
- **Static `$cscPath` in Build Scripts**:
  - `build_autoprescribe.ps1` line 7:
    `$cscPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'`
  - `build_clinical_cli.ps1` line 7:
    `$cscPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'`
- **UTF-16 LE Encoding Corruption in `FetchPatient.cs`**:
  Inspection of `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs` showed:
  - File size: 3,360 bytes
  - Byte 0 & 1: `0xFF 0xFE` (UTF-16 LE BOM)
  - `view_file` rejected the file with verbatim error: `unsupported mime type text/plain; charset=utf-16le`.
- **Target Temporary Files on Disk**:
  Verified existence of temporary/scratch files:
  - `output_3e.txt`: 74,792 bytes
  - `Test20211012.txt`: 3 bytes
  - `readmebk.txt`: 10 bytes
  - `__pycache__/`: containing 2 `.pyc` files (`generate_html_report.cpython-312.pyc`, `openrouter_client.cpython-312.pyc`)
- **Strictly Protected Assets Status**:
  - `ConfigSystem.xml`: Exists (8,367 bytes)
  - `ReferencedAssemblies/`: Exists with 1,162 `.dll` assemblies
  - `Logs/LogSystem.txt`: Exists and actively recording
  - 109 `*.exe.config` / `*.exe.config_` files verified intact across the workspace.

## 2. Logic Chain
1. **Resolving `WatchHoiChan.ps1` 9 Parse Errors**:
   From Observation 1, Windows PowerShell 5.1 requires a UTF-8 BOM (`0xEF, 0xBB, 0xBF`) to correctly interpret non-ASCII characters without reverting to system ANSI code page. By saving `WatchHoiChan.ps1` and all other `.ps1` files with `[System.Text.UTF8Encoding]::new($true)`, `[System.Management.Automation.Language.Parser]::ParseFile` accurately decoded all multibyte strings and returned 0 parse errors across all 8 scripts (and `set_env.ps1`).
2. **Decoupling Hardcoded Paths**:
   From Observation 2, hardcoded references to `D:\` and `E:\` break execution when the project runs on drive `F:\` or other deployment environments. Replacing them with `$PSScriptRoot\Logs`, `Logs`, and parent paths ensures seamless portability.
3. **Dynamic Compiler Resolution**:
   From Observation 3, relying solely on `C:\Windows` fails in custom Windows installations or containerized runners. Using `$env:CSC` first, then `$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, guarantees robust resolution across environments. Testing `build_clinical_cli.ps1` confirmed successful compilation of `HisClinicalCli.exe` (75 KB, exit code 0).
4. **Normalizing `FetchPatient.cs`**:
   From Observation 4, `FetchPatient.cs` was encoded in UTF-16 LE (2 bytes per character with `0xFF, 0xFE` BOM). Reading via `[System.Text.Encoding]::Unicode` and writing via `[System.Text.UTF8Encoding]::new($false)` reduced file size from 3,360 to 1,679 bytes and restored standard UTF-8 readability.
5. **Workspace Hygiene without Asset Regression**:
   From Observation 5 and 6, targeting only `output_3e.txt`, `Test20211012.txt`, `readmebk.txt`, `__pycache__/`, and temporary orchestrator logs safely purges clutter without affecting `ConfigSystem.xml`, `ReferencedAssemblies/`, or `Logs/`.

## 3. Caveats
- `set_env.ps1` (introduced by Worker M1) was also standardized with UTF-8 BOM and verified for 0 AST errors, ensuring the entire `.ps1` family is uniform.
- `FetchPatient.cs` was converted to UTF-8 without BOM, matching standard C# source conventions.
- No other files outside the designated scope were altered or removed.

## 4. Conclusion
Milestone M4 is 100% complete and fully verified:
- All 8 `.ps1` files (plus `set_env.ps1`) have UTF-8 BOM and pass AST parsing with 0 errors.
- Hardcoded `D:\...` and `E:\...` paths in `WatchHoiChan.ps1` and `WatchBranchLearning.ps1` are completely eliminated.
- `$cscPath` in `build_autoprescribe.ps1` and `build_clinical_cli.ps1` supports multi-tier dynamic resolution.
- `FetchPatient.cs` is converted from UTF-16 LE to standard UTF-8 and loads cleanly.
- Target scratch files were deleted; all critical configurations, assembly dependencies, and live log files remain fully intact.

## 5. Verification Method
To independently verify Milestone M4:
1. **PowerShell AST & BOM Validation**:
   ```powershell
   $files = @(
       "WatchHoiChan.ps1", "WatchBranchLearning.ps1", "build_autoprescribe.ps1",
       "build_clinical_cli.ps1", "check_his_tunnel.ps1", "HisDiabetesOrchestrator.ps1",
       "HisPacsCli.ps1", "install_git.ps1"
   )
   foreach ($f in $files) {
       $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $f).Path)
       $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
       $tokens = $null; $errors = $null
       [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $f).Path, [ref]$tokens, [ref]$errors)
       [PSCustomObject]@{ File = $f; HasBom = $hasBom; Errors = $errors.Count }
   }
   ```
   *Expected*: All entries show `HasBom: True` and `Errors: 0`.
2. **`FetchPatient.cs` Encoding Verification**:
   ```powershell
   $b = [System.IO.File]::ReadAllBytes(".agents\skills\his-clinical-operations\scripts\FetchPatient.cs")
   -not ($b[0] -eq 0xFF -and $b[1] -eq 0xFE)
   ```
   *Expected*: `True` (File size: 1,679 bytes).
3. **Workspace Hygiene & Asset Protection**:
   ```powershell
   -not (Test-Path "output_3e.txt") -and
   -not (Test-Path "Test20211012.txt") -and
   -not (Test-Path "readmebk.txt") -and
   -not (Test-Path "__pycache__") -and
   (Test-Path "ConfigSystem.xml") -and
   (Test-Path "ReferencedAssemblies") -and
   (Test-Path "Logs\LogSystem.txt")
   ```
   *Expected*: `True`.
4. **Compilation Verification**:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\build_clinical_cli.ps1
   ```
   *Expected*: Exit code 0, `[OK] BUILD THANH CONG`.
