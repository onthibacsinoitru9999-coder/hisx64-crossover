# Task Assignment — Worker M4 (Script Encoding & Workspace Hygiene)

## Objective
Execute Milestone M4 (Requirements R4: Features F10, F11, F12).
Add UTF-8 BOM to all 8 `.ps1` files, decoupling hardcoded paths in `.ps1`, convert `FetchPatient.cs` from UTF-16 LE to UTF-8, and perform safe workspace cleanup of verified temporary files while strictly protecting all configuration, assembly, and log assets.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_3\survey_report.md`

## Exclusively Owned Files
- All 8 `.ps1` files in workspace:
  - `WatchHoiChan.ps1`
  - `WatchBranchLearning.ps1`
  - `build_autoprescribe.ps1`
  - `build_clinical_cli.ps1`
  - `check_his_tunnel.ps1`
  - `HisDiabetesOrchestrator.ps1`
  - `HisPacsCli.ps1`
  - `install_git.ps1`
- `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs`
- Temporary files targeted for deletion:
  - `diabetes_orchestrator_20260909_2341.log`
  - `diabetes_orchestrator_20260909_2343.log`
  - `diabetes_orchestrator_20260909_2356.log`
  - `insulin_orders_temp.csv`
  - `output_3e.txt`
  - `Test20211012.txt`
  - `readmebk.txt`
  - `__pycache__/`

## Strictly Protected Assets (DO NOT TOUCH OR DELETE)
- `ConfigSystem.xml` (all copies)
- Any `*.exe.config` or `*.exe.config_` (66 files)
- `ReferencedAssemblies/` (all 1,177 DLLs and fonts)
- `Logs/` (`LogSystem.txt`, `HLSLogSystem.txt`, etc.)
- Any `.cs`, `.exe`, `.bat`, `.md`, `.json` core files

## Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md` and `explorer_survey_3/survey_report.md`.
2. Decouple hardcoded paths in `WatchHoiChan.ps1` and `WatchBranchLearning.ps1`:
   - Replace hardcoded `D:\his...` and `E:\his...` paths with dynamic resolution using `$PSScriptRoot\Logs`, `Logs`, etc.
   - In `build_autoprescribe.ps1` and `build_clinical_cli.ps1`, update `$cscPath` to check `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` or `$env:CSC`.
3. Standardize UTF-8 BOM on all 8 `.ps1` files:
   - Use PowerShell `[System.Text.UTF8Encoding]::new($true)` to write all 8 `.ps1` files with UTF-8 BOM.
   - Run AST parser validation using `[System.Management.Automation.Language.Parser]::ParseFile` on each file.
   - Ensure `WatchHoiChan.ps1` has 0 errors (resolving the prior 9 parse errors in Windows PowerShell 5.1).
   - Ensure all other 7 `.ps1` files also have 0 errors.
4. Convert `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs` from UTF-16 LE to UTF-8 without BOM (or standard UTF-8). Verify readability.
5. Workspace Hygiene:
   - Carefully delete ONLY the designated temporary/scratch files listed above.
   - Verify that all protected assets remain intact.
6. Verification:
   - Run AST validation loop across all `.ps1` files and document the result.
   - 7. Write `handoff.md` and `progress.md` in `.agents\worker_m4\`.

## 2026-09-09T18:25:33Z
You are Worker M4 for the HIS Automation project.
Your assigned working directory is:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m4`

Read your assignment in `DISPATCH.md` at that path.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md`.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_3\survey_report.md`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Execute Milestone M4:
1. Decouple hardcoded paths in `WatchHoiChan.ps1` and `WatchBranchLearning.ps1` (replace `D:\...` and `E:\...` with dynamic `$PSScriptRoot\Logs`).
2. Update `$cscPath` in `build_autoprescribe.ps1` and `build_clinical_cli.ps1` to use `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` or `$env:CSC`.
3. Save all 8 `.ps1` files with UTF-8 BOM (`[System.Text.UTF8Encoding]::new($true)`).
4. Validate all `.ps1` files with `[System.Management.Automation.Language.Parser]::ParseFile` to guarantee 0 AST errors (fixing the 9 parse errors on `WatchHoiChan.ps1`).
5. Convert `.agents\skills\his-clinical-operations\scripts\FetchPatient.cs` from UTF-16 LE to UTF-8.
6. Clean up temporary files (`diabetes_orchestrator_*.log`, `insulin_orders_temp.csv`, `output_3e.txt`, `Test20211012.txt`, `readmebk.txt`, `__pycache__/`) while strictly protecting `ConfigSystem.xml`, `*.exe.config`, `ReferencedAssemblies/`, and `Logs/`.
7. Keep `progress.md` updated, write a comprehensive `handoff.md` in `.agents\worker_m4\`, and send a completion message back to the caller.
