# Task Assignment — Worker M5 (System-wide E2E Testing & Git Synchronization)

## Objective
Execute Milestone M5 (Requirements R5: Features F13, F14).
Run the complete 4-Tier E2E verification test suite (`tests\test_e2e_suite.ps1`), verify `HisDiagnosticDoctor.bat health` (100% Ready), verify `HisAiCli.bat models` (< 2.0s), verify `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm` (4 stages, 0 errors), document lessons learned in `HIS_AI_INTEGRATION_PLAYBOOK.md`, and synchronize all changes to Git `origin main`.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\TEST_INFRA.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\tests\test_e2e_suite.ps1`

## Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md`, `PROJECT.md`, `TEST_INFRA.md`.
2. Execute the comprehensive 4-Tier E2E test harness:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File tests\test_e2e_suite.ps1 -Tier All
   ```
   Verify 100% pass across Tier 1, Tier 2, Tier 3, and Tier 4.
3. Verify Acceptance Criteria specific commands:
   - `cmd /c HisDiagnosticDoctor.bat health` -> verify "🎯 KẾT LUẬN CHẨN ĐOÁN: Hệ thống sẵn sàng 100%!".
   - `powershell -Command "$sw=[Diagnostics.Stopwatch]::StartNew(); cmd.exe /c HisAiCli.bat models; $sw.Stop(); $sw.ElapsedMilliseconds"` -> verify < 2000 ms.
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm` -> verify 4 stages (Tracking, Bedside BM02426, Insulin prescription) complete with 0 errors.
   - Verify all `.ps1` files pass AST parser with 0 errors.
4. Update `HIS_AI_INTEGRATION_PLAYBOOK.md` in "Bảng Tổng Hợp Sai Lầm & Bài Học Xương Máu" with lessons learned from this audit & optimization:
   - Lesson on CRLF line endings for batch files containing non-ASCII/Unicode.
   - Lesson on dynamic toolchain resolution in `set_env.bat` and `set_env.ps1`.
   - Lesson on master compiler and portable relative assembly references in `refs.rsp`.
   - Lesson on query batching (`SERVICE_REQ_IDs`, cohort `TREATMENT_IDs` with `Parallel.Invoke`) reducing latency by 60-80%.
   - Lesson on UTF-8 BOM requirement for Windows PowerShell 5.1 AST parser.
5. Git Synchronization (Rule 3 in AGENTS.md):
   - Stage all updated and tracked assets:
     `git add HIS_AI_INTEGRATION_PLAYBOOK.md .agents/ *.cs *.bat *.ps1 AGENTS.md .gitignore tests/`
   - Commit:
     `git commit -m "fix/feat: comprehensive audit, optimization, compilation and validation of HIS Automation platform"`
   - Push:
     `git push origin main`
6. Write `handoff.md` and `progress.md` in `.agents\worker_m5\`.

## 2026-09-09T19:14:07Z
Execute Milestone M5:
1. Run the comprehensive 4-Tier E2E test harness:
   `powershell -NoProfile -ExecutionPolicy Bypass -File tests\test_e2e_suite.ps1 -Tier All`
   Verify 100% test pass rate.
2. Verify Acceptance Criteria specific commands:
   - `cmd /c HisDiagnosticDoctor.bat health` -> verify 100% ready.
   - Benchmark `HisAiCli.bat models` -> verify < 2000 ms.
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm` -> verify 4 stages, 0 errors.
   - Verify all `.ps1` files pass AST parser with 0 errors.
3. Update `HIS_AI_INTEGRATION_PLAYBOOK.md` in "Bảng Tổng Hợp Sai Lầm & Bài Học Xương Máu" with lessons learned (CRLF line endings for batch files, multi-tier toolchain resolution in set_env, master compiler with dynamic relative rsp, query batching with Parallel.Invoke, and UTF-8 BOM for PowerShell 5.1).
4. Synchronize all changes to Git `origin main`:
   - Stage all tracked and updated assets (`git add HIS_AI_INTEGRATION_PLAYBOOK.md .agents/ *.cs *.bat *.ps1 AGENTS.md .gitignore tests/`).
   - Commit (`git commit -m "fix/feat: comprehensive audit, optimization, compilation and validation of HIS Automation platform"`).
   - Push (`git push origin main`).
5. Write `handoff.md` and `progress.md` in `.agents\worker_m5\`, and send completion message to caller.
