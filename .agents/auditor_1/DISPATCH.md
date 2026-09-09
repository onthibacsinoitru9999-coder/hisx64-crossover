# Task Assignment — Forensic Auditor (Integrity Forensics & Quality Gate)

## Objective
Perform comprehensive independent forensic integrity audit across all changes implemented for Milestones M1, M2, M3, M4, M5 in the HIS Automation project.
Verify that:
1. No hardcoded test outputs or mock shortcuts were added to C# or script files.
2. All clinical logic, API integration, and batch querying are genuine.
3. No secret keys or credentials were inappropriately committed.
4. Protected assets (`ConfigSystem.xml`, `ReferencedAssemblies/`, `Logs/`, `*.exe.config`) remain 100% intact.
5. All 14 C# clinical tools are genuine 64-bit binaries compiled from real source.
6. Verify whether the verdict is CLEAN or INTEGRITY VIOLATION.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m1\handoff.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m2\handoff.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m3\handoff.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m4\handoff.md`

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md`, `PROJECT.md`, and all worker handoffs.
2. Check git diff and inspect modified source code (`HisClinicalCli.cs`, `HisWardReportCreator.cs`, `set_env.bat`, `build_all_cs_tools.ps1`, etc.):
   - Verify that logic changes are genuine algorithms and API integrations.
   - Verify that no static/mock returns were hardcoded.
   - Verify that token reading is genuine tail-seek with `FileShare.ReadWrite`.
3. Check binary integrity:
   - Verify that all 14 `.exe` binaries are valid PE x64 executables and correspond to their `.cs` sources.
4. Check asset protection:
   - Confirm that `ConfigSystem.xml`, all `*.exe.config` files, all 1,162 assemblies in `ReferencedAssemblies/`, and active runtime `Logs/` were not deleted or corrupted.
5. Write `handoff.md` in `.agents\auditor_1\` stating clearly the verdict (CLEAN or INTEGRITY VIOLATION) and send completion message back to parent.

## 2026-09-09T19:14:07Z
You are Forensic Auditor 1 for the HIS Automation project.
Your assigned working directory is:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\auditor_1`

Read your assignment in `DISPATCH.md` at that path.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md`.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`.
Read all completed worker handoff reports (`.agents\worker_m1\handoff.md`, `worker_m2\handoff.md`, `worker_m3\handoff.md`, `worker_m4\handoff.md`).

Execute Forensic Integrity Audit:
1. Check git diff and inspect all source modifications to verify they are authentic implementations without hardcoded strings, mock bypasses, or cheated test results.
2. Verify all 14 clinical C# executables are genuine PE x64 binaries matching their sources.
3. Confirm that all protected assets (`ConfigSystem.xml`, `ReferencedAssemblies/`, `Logs/`, `*.exe.config`) remain 100% intact.
4. Record verdict (CLEAN or INTEGRITY VIOLATION) in `handoff.md` in `.agents\auditor_1\` with evidence, and notify caller.
