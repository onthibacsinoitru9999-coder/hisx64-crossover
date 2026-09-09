## 2026-09-09T17:03:58Z
You are Test Writer for the HIS Automation E2E Testing Track.

Your assigned working directory is:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\test_writer_e2e`

Workspace root:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`

Authoritative User Request:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md`

Test Infra Plan:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\TEST_INFRA.md`

Survey Reports:
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_1\survey_report.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_2\survey_report.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_3\survey_report.md`

Project Rules:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\AGENTS.md`

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership:
You exclusively own:
- `tests\` directory (e.g. `tests\test_e2e_suite.ps1`)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\TEST_READY.md`

## Your Tasks:
1. Read `TEST_INFRA.md` and `ORIGINAL_REQUEST.md`.
2. Design and implement a standalone, comprehensive E2E test runner script: `tests\test_e2e_suite.ps1`.
   The runner must cover:
   - Tier 1: Feature Isolation (test batch wrappers, c# tool help/version, AI CLI models, diagnostic health, AST syntax parsing).
   - Tier 2: Boundary & Corner Cases (invocation from outside directories, paths with spaces, missing environment variables, error code propagation).
   - Tier 3: Cross-Feature Integration (batch calling C# tool, reading token from `LogSystem.txt`, querying HIS API).
   - Tier 4: Real-World Workload Scenarios (`HisDiagnosticDoctor.bat health` -> 100% Ready, `HisAiCli.bat models` < 2s, `HisDiabetesOrchestrator.ps1 -DryRun -SkipConfirm`).
3. Run the test suite, measure and document baseline results. Ensure any tests that depend on uncompleted milestones fail gracefully with clear diagnostic messages, while implemented features pass.
4. Publish `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\TEST_READY.md` summarizing the test suite, how to run it, and the test coverage matrix across all 4 tiers.
5. Write `handoff.md` and send a completion message to your caller.
