# Task Assignment — Worker M1 (Batch & Toolchain Links)

## Objective
Execute Milestone M1 (Requirements R1: Features F1, F2, F3).
Modernize `set_env.bat`, create `set_env.ps1`, decouple hardcoded paths across all 27 `.bat` files and python scripts, fix quoting for paths with spaces (`HIS CSNB`), and ensure robust multi-shell compatibility.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_1\survey_report.md`

## Exclusively Owned Files
- `set_env.bat`, `set_env.ps1`
- All 27 `.bat` files in workspace (root, `.\.agents\skills\his-clinical-operations\scripts\`, `Integrate\`, `Setup\`, `Tool\`)
- `generate_html_report.py`

## Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md` and `explorer_survey_1/survey_report.md`.
2. Upgrade `set_env.bat`:
   - Replace `C:\Windows` with `%SystemRoot%`.
   - Implement multi-tier search for `csc.exe` (x64 first: `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, fallback to Framework 32-bit). Set `%CSC%`.
   - Multi-tier search for `git.exe` (WinGet MinGit packages in `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Git.MinGit*`, `%ProgramFiles%\Git\cmd`, `%ProgramFiles(x86)%\Git\cmd`, `%LOCALAPPDATA%\Programs\Git\cmd`). Set `%GIT%`.
   - Multi-tier search for `python.exe` (`%LOCALAPPDATA%\Programs\Python\Python*`, `%ProgramFiles%\Python*`, `C:\Python3*`, `.venv\Scripts`, and `py.exe`). Set `%PYTHON%`.
   - Search for `rclone.exe` (PATH, `%LOCALAPPDATA%\Programs\rclone`, `%ProgramFiles%\rclone`, or `%~dp0rclone.exe`). Set `%RCLONE%`.
   - Ensure `HIS_ENV_READY=1`.
3. Create `set_env.ps1` companion script for PowerShell environments:
   - Sets `$env:PATH` with discovered paths for `csc`, `git`, `python`, `rclone`.
   - Sets `$env:CSC`, `$env:GIT`, `$env:PYTHON`.
4. Audit and update all 27 `.bat` files:
   - Call `set_env.bat` if not already defined:
     `if not defined HIS_ENV_READY call "%~dp0set_env.bat"`
   - Ensure `cd /d "%~dp0"` or `cd /d "%SCRIPT_DIR%"` is present at the beginning of each tool runner.
   - Add `chcp 65001 >nul` to ensure UTF-8 output.
   - Quote all directory variables (`"%~dp0"`, `"%SCRIPT_DIR%"`, etc.) to handle spaces in `HIS CSNB`.
   - Specifically fix `Cai_Dat_He_Thong_Support.bat` lines 9 and 53-54 where `%SCRIPT_DIR%HisClinicalCli.exe` was unquoted.
   - Fix `build_fetch.bat` and `build_autoprescribe.bat` in `.agents\skills\his-clinical-operations\scripts\`: remove all hardcoded `D:\his...` paths, use relative paths from `%~dp0..\..\..\..` or `%SCRIPT_DIR%` and use `%CSC%`.
   - In `HisWardReport.bat`, guard `rclone copy` with `where rclone >nul 2>nul` to avoid crash if rclone is not installed.
   - In `generate_html_report.py`, remove hardcoded `e:\...` and `C:\Users\1995\...` paths; use relative paths from `__file__`.
5. Verification:
   - Run `cmd /c set_env.bat` and verify environment variables.
   - Run `powershell -ExecutionPolicy Bypass -File .\set_env.ps1` and verify.
   - Run `cmd /c HisAiCli.bat models` and verify exit code 0.
   - Run `cmd /c HisDiagnosticDoctor.bat health` and verify exit code 0.
   - Test invoking batch files from another directory (e.g. `cd %TEMP%` and call batch with full path).
6. Write `handoff.md` and `progress.md` in `.agents\worker_m1\`.

## 2026-09-09T18:25:33Z
Execute Milestone M1:
1. Modernize `set_env.bat` with multi-tier toolchain resolution (%SystemRoot%, csc.exe, git.exe, python.exe, rclone.exe, export %CSC%, %GIT%, %PYTHON%, %RCLONE%, HIS_ENV_READY=1).
2. Create companion `set_env.ps1`.
3. Eliminate hardcoded paths (`D:\`, `E:\`, `C:\Program Files\Git...`) in all 27 `.bat` files and `generate_html_report.py`.
4. Fix quoting on paths with spaces (`HIS CSNB`, `%SCRIPT_DIR%`), add `cd /d "%~dp0"`, add `chcp 65001 >nul`.
5. Guard `rclone copy` in `HisWardReport.bat`.
6. Run build/test verification commands (test set_env.bat, test set_env.ps1, test HisAiCli.bat models, test HisDiagnosticDoctor.bat health, test calling batch from another cwd).
7. Keep `progress.md` updated with timestamps, write a comprehensive `handoff.md` in `.agents\worker_m1\`, and send a completion message back to the caller.

