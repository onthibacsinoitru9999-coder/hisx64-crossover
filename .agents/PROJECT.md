# Project: HIS Automation Codebase Audit, Optimization, Compilation & Validation

## Architecture
- **Environment & Toolchain**: `set_env.bat`, `set_env.ps1`, batch wrappers.
- **C# Core CLI & Clinical Tools**: `HisClinicalCli.cs`, `HisTrackingCreator.cs`, `HisAutoPrescribe.cs`, `HisGlucoseBedsideAssigner.cs`, `HisRationAssigner.cs`, `HisDebateCreator.cs`, `HisDiagnosticDoctor.cs`, `HisWardReportCreator.cs`, `HisLeanproAssigner.cs`.
- **Compiler Infrastructure**: `refs.rsp`, 64-bit `csc.exe`, `ReferencedAssemblies/`.
- **PowerShell Pipelines & Orchestration**: `HisDiabetesOrchestrator.ps1`, `WatchHoiChan.ps1`, `WatchBranchLearning.ps1`, diagnostic tools.
- **E2E Testing & Quality Assurance**: AST parser, diagnostic doctor, AI CLI benchmark, dry-run simulation, git synchronization.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | F1: Environment Loader | Modernize `set_env.bat` and create `set_env.ps1` with multi-tier toolchain resolution (`csc`, `git`, `python`) | M1 | Survey Explorer 1 |
| 2 | F2: Path Decoupling | Eliminate hardcoded paths (`D:\...`, `E:\...`) in all 27 `.bat` files and python scripts | M1 | Survey Explorer 1 |
| 3 | F3: Quoting & Shell Safety | Fix quoting on paths with spaces (`HIS CSNB`), add `cd /d "%~dp0"`, guard `rclone` in `HisWardReport.bat` | M1 | Survey Explorer 1 |
| 4 | F4: C# Syntax Repair | Fix syntax corruption in `HisWardReportCreator.cs` at lines 689 & 1049 | M2 | Survey Explorer 2 & 3 |
| 5 | F5: Portable Assembly References | Update `refs.rsp` from hardcoded `D:\...` to relative `.\ReferencedAssemblies\...` | M2 | Survey Explorer 2 |
| 6 | F6: Synchronized Compilation | Master build script `build_all_cs_tools.bat`/`.ps1`, compile missing `HisLeanproAssigner.exe` and sync 14 binaries via 64-bit `csc` | M2 | Survey Explorer 2 |
| 7 | F7: Order Query Batching | Batch query optimization in `HisClinicalCli.cs` (`orders <MãBN>` via `SERVICE_REQ_IDs`) | M3 | Survey Explorer 2 |
| 8 | F8: Ward Report Batching | Batch query optimization in `HisWardReportCreator.cs` (reduce 140 sequential HTTP requests to ~7) | M3 | Survey Explorer 2 |
| 9 | F9: Token Reader Tail-Seek | Standardize tail-seek 128KB with `FileShare.ReadWrite` and process detection across all clinical tools | M3 | Survey Explorer 2 |
| 10 | F10: PowerShell UTF-8 BOM | Standardize UTF-8 BOM on all `.ps1` files, eliminating 9 AST parser errors in `WatchHoiChan.ps1` | M4 | Survey Explorer 3 |
| 11 | F11: Source Encoding Normalization | Convert `FetchPatient.cs` from UTF-16 LE to UTF-8 | M4 | Survey Explorer 3 |
| 12 | F12: Workspace Sanitation | Clean temporary/garbage files (`diabetes_orchestrator_*.log`, `output_3e.txt`, test logs) while preserving configs and DLLs | M4 | Survey Explorer 3 |
| 13 | F13: E2E Verification & Benchmarks | Execute full verification pipeline (`HisDiagnosticDoctor.bat health`, AST parser, `HisAiCli.bat models` < 2s, dry-run simulation) | M5 | Survey Explorer 3 |
| 14 | F14: Git Synchronization | Package and synchronize code, tools, and playbook knowledge to Git `origin main` | M5 | Survey Explorer 3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Batch Files & Toolchain Links | F1, F2, F3: Modernize `set_env.bat`/`.ps1`, fix hardcoded paths & quoting across all 27 `.bat` files | none | IN_PROGRESS |
| M2 | C# Syntax Repair & Compilation | F4, F5, F6: Repair `HisWardReportCreator.cs`, update `refs.rsp`, build master compiler script, generate missing `HisLeanproAssigner.exe` and sync all 14 `.exe` tools | M1 | PLANNED |
| M3 | Clinical Query Latency & Batching | F7, F8, F9: Batch HTTP requests in `HisClinicalCli.cs` and `HisWardReportCreator.cs`, standardize tail-seek token reading, bring latency < 1.5s | M2 | PLANNED |
| M4 | Script Encoding & Workspace Cleanup | F10, F11, F12: Add UTF-8 BOM to all `.ps1`, convert `FetchPatient.cs` to UTF-8, purge garbage/temp files, protect core configs | M1, M2 | PLANNED |
| M5 | System-wide E2E Testing & Git Sync | F13, F14: Validate 100% health, 0 AST errors, < 2s AI CLI, dry run execution, commit & push to Git | M1, M2, M3, M4 | PLANNED |

## Code Layout
- Root directory: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`
- Core C# sources: `*.cs` at project root
- Compiled binaries: `*.exe` at project root
- Batch scripts: `*.bat` at project root
- PowerShell scripts: `*.ps1` at project root
- Assembly libraries: `ReferencedAssemblies/*.dll`
- Configuration: `ConfigSystem.xml`, `*.exe.config`, `refs.rsp`
- Agent metadata: `.agents/`
