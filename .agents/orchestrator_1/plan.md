# Master Project Plan — HIS Automation Audit, Optimization, Compilation & Validation

## Objective
Quét toàn diện toàn bộ codebase hệ thống HIS Automation, phát hiện và sửa chữa triệt để mọi điểm nghẽn, liên kết môi trường, cấu hình compiler, mã hóa ký tự, và tối ưu hóa các kịch bản thực thi để toàn bộ hệ thống hoạt động mượt mà, trơn tru 100%.

## Architecture & Work Tracks
1. **Survey Track (Phase 0)**:
   - Explorer 1: Batch files, toolchain, environment loaders (`set_env.bat`, `*.bat`), path dependencies (R1).
   - Explorer 2: C# tools, compiler configurations, DLL references (`ReferencedAssemblies/`), source vs binary sync (R2, R3).
   - Explorer 3: PowerShell scripts (`*.ps1`), encoding (UTF-8 / UTF-8 BOM), garbage/temp cleanup, test validation pipeline (`HisDiagnosticDoctor.bat`, `HisAiCli.bat`, `HisDiabetesOrchestrator.ps1`) (R4, R5).

2. **Synthesis & Decomposition (Phase 1)**:
   - Synthesize survey findings into `PROJECT.md` & Feature Inventory.
   - Decompose into Milestones (M1: Batch & Environment Toolchain; M2: C# Compiler & Assembly Linkage; M3: Query Latency Optimization; M4: Garbage Cleanup & Script Encoding; M5: Full-system E2E Verification & Git Push).
   - Establish Dual Track: Implementation Track + E2E Testing Track (`TEST_INFRA.md`).

3. **Execution Track (Phase 2)**:
   - Milestone execution via iteration loops (Worker -> Reviewer -> Challenger -> Forensic Auditor -> Gate).
   - E2E test suite execution.

4. **Delivery & Handoff (Phase 3)**:
   - Final audit verification.
   - Handoff report and communication to Sentinel.
