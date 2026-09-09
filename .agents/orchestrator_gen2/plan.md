# Execution Plan — Project Orchestrator Gen 2

## Objective
Fulfill all requirements (R1-R5) and acceptance criteria of the HIS Automation Codebase Audit & Optimization project across 5 milestones.

## Milestone Breakdown & Execution Strategy

### Milestone M1: Batch Files & Toolchain Links (R1: F1, F2, F3)
- **Scope**:
  - Overhaul `set_env.bat` and create companion `set_env.ps1`.
  - Fix all 27 `.bat` files: eliminate hardcoded drive letters (`D:\`, `E:\`, `C:\`), ensure `cd /d "%~dp0"`, safe quoting for paths with spaces, and guard `rclone`.
  - Fix `generate_html_report.py`, `WatchBranchLearning.ps1`, `WatchHoiChan.ps1` hardcoded paths.
- **Worker**: Dispatch `worker_m1` (`teamwork_preview_worker`).
- **Gate**: Run Tier 1/2 tests on batch wrappers. Reviewer & Auditor sign-off.

### Milestone M4: Script Encoding & Workspace Hygiene (R4: F10, F11, F12)
- **Scope** (can run concurrently or in parallel with M1):
  - Add UTF-8 BOM to all 8 `.ps1` files (`WatchHoiChan.ps1`, `build_*.ps1`, `HisPacsCli.ps1`, etc.) ensuring 0 AST parser errors.
  - Convert `.agents/skills/his-clinical-operations/scripts/FetchPatient.cs` from UTF-16 LE to UTF-8.
  - Purge scratch/garbage files (`diabetes_orchestrator_*.log`, `insulin_orders_temp.csv`, `output_3e.txt`, `Test20211012.txt`, `readmebk.txt`, `__pycache__`) while strictly preserving protected configs and DLLs.
- **Worker**: Dispatch `worker_m4` (`teamwork_preview_worker`).
- **Gate**: AST parse all `.ps1` files (0 errors). Check file hygiene. Reviewer & Auditor sign-off.

### Milestone M2: C# Syntax Repair & Compilation Infrastructure (R2: F4, F5, F6)
- **Scope**:
  - Repair syntax corruption in `HisWardReportCreator.cs` at line 689 (`notes.Add("Đi...`) and line 1049 (`FormatCurrentStatus` restoration).
  - Update `refs.rsp` to portable relative paths or dynamic generation.
  - Create master compilation script `build_all_cs_tools.ps1` (and `.bat`).
  - Compile missing `HisLeanproAssigner.exe` (20,480 B) and synchronize all 14 `.exe` binaries across root and `.agents/skills/his-clinical-operations/scripts/`.
- **Worker**: Dispatch `worker_m2` (`teamwork_preview_worker`).
- **Gate**: Verify all 14 `.exe` binaries exist, PE x64, successfully compiled. Reviewer & Auditor sign-off.

### Milestone M3: Clinical Query Latency & Batching (R3: F7, F8, F9)
- **Scope**:
  - Optimize `HisClinicalCli.cs`: batch `SERVICE_REQ_IDs` in `orders` command (replace N+1 HTTP loop with single batch query).
  - Optimize `HisWardReportCreator.cs`: batch `TREATMENT_IDs` for 5 core queries across all ward patients (reducing ~140 HTTP requests to 5).
  - Standardize tail-seek 128KB `FileShare.ReadWrite` token reader and process detection across clinical tools.
  - Benchmark `lookup <MãBN>` and `orders <MãBN>` < 1.5s, ward report < 2.5s.
  - Recompile modified C# tools using master build script.
- **Worker**: Dispatch `worker_m3` (`teamwork_preview_worker`).
- **Gate**: Latency benchmark measurements < 1.5s. Reviewer & Auditor sign-off.

### Milestone M5: System-wide E2E Testing & Git Synchronization (R5: F13, F14)
- **Scope**:
  - Run comprehensive E2E test runner `tests\test_e2e_suite.ps1` across all 4 Tiers.
  - Run `HisDiagnosticDoctor.bat health` -> verify 100% Ready.
  - Run `HisAiCli.bat models` -> verify < 2s.
  - Run `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm` -> verify 4 stages, 0 errors.
  - Verify AST parse 0 errors on all `.ps1`.
  - Update `HIS_AI_INTEGRATION_PLAYBOOK.md` with lessons learned.
  - Stage, commit, and push changes to Git `origin main`.
- **Worker**: Dispatch `worker_m5` (`teamwork_preview_worker`) and verification agents.
- **Gate**: Full E2E suite passes 100%. Forensic Auditor verify clean integrity.
