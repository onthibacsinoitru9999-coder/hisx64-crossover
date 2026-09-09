# E2E Test Infra: HIS Automation

## Test Philosophy
- Requirement-driven, opaque-box and contract-level verification.
- Validates batch toolchains, compiler output, PowerShell AST syntax, AI CLI latency, clinical query responsiveness, and end-to-end diabetes orchestration dry runs.

## Feature Inventory & Test Mapping
| # | Feature | Source | Tier 1 (Isolated) | Tier 2 (Boundary) | Tier 3 (Cross-Feature) |
|---|---------|--------|:-----------------:|:-----------------:|:---------------------:|
| 1 | F1-F3: Batch & Toolchain | R1, ORIGINAL_REQUEST | Run batch wrappers from arbitrary cwd | Spaces in paths, missing toolchain | Invoking C# tool from batch |
| 2 | F4-F6: C# Compilation | R2, ORIGINAL_REQUEST | Verify each .exe binary exists & executes | Verify timestamps, x64 platform | Multi-tool invocation pipeline |
| 3 | F7-F9: Query Latency | R3, ORIGINAL_REQUEST | Benchmark lookup < 1.5s, orders < 1.5s | Empty orders, large patient records | Ward round batch report generation |
| 4 | F10-F12: Encodings & Cleanup | R4, ORIGINAL_REQUEST | AST parse each .ps1 (0 errors) | Vietnamese/emoji text in scripts | No temp files left behind after run |
| 5 | F13-F14: E2E Health & Git | R5, ORIGINAL_REQUEST | DiagnosticDoctor health = 100% | AiCli models < 2s | Full Diabetes Orchestrator dry run |

## Real-World Application Scenarios (Tier 4)
| # | Scenario | Features Exercised | Expected Outcome |
|---|----------|--------------------|------------------|
| 1 | Fresh Shell Diagnostic Health Check | F1, F2, F6, F9, F13 | `HisDiagnosticDoctor.bat health` succeeds with 100% Ready in any directory |
| 2 | High-Speed AI Model Discovery | F1, F13 | `HisAiCli.bat models` completes under 2.0 seconds with valid JSON/model list |
| 3 | PowerShell Script AST Integrity Audit | F10 | `[System.Management.Automation.Language.Parser]::ParseFile` reports 0 errors on all `.ps1` |
| 4 | Clinical Multi-Patient Ward Query | F7, F8, F9 | `HisWardReport.bat` or equivalent ward query completes in under 2.5 seconds |
| 5 | Full Diabetes Orchestration Dry Run | F1, F6, F7, F9, F10, F13 | `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm` executes 4 stages with 0 errors |

## Test Runner Architecture
- Dedicated test harness script: `tests\test_e2e_suite.ps1`
- Pass/Fail semantics: All tests must return exit code 0 and meet latency SLAs.
