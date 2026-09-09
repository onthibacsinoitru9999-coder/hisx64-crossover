# Task Assignment — Worker M2 (C# Syntax Repair & Compilation Infrastructure)

## Objective
Execute Milestone M2 (Requirements R2: Features F4, F5, F6).
Repair syntax corruption in `HisWardReportCreator.cs`, update `refs.rsp` to portable relative paths, create master build script `build_all_cs_tools.ps1` (and `build_all_cs_tools.bat`), compile missing `HisLeanproAssigner.exe`, compile all 14 C# clinical tools with 64-bit `csc.exe`, and synchronize `.exe` files across root and `.agents\skills\his-clinical-operations\scripts\`.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_2\survey_report.md`

## Exclusively Owned Files
- `HisWardReportCreator.cs`
- `refs.rsp` and `.agents\skills\his-clinical-operations\scripts\refs.rsp`
- `build_all_cs_tools.ps1`
- `build_all_cs_tools.bat`
- All 14 compiled `.exe` binary outputs in root and scripts directory:
  1. `HisLeanproAssigner.exe`
  2. `HisClinicalCli.exe`
  3. `HisAutoPrescribe.exe`
  4. `HisTrackingCreator.exe`
  5. `HisGlucoseBedsideAssigner.exe`
  6. `HisDebateCreator.exe`
  7. `HisRationAssigner.exe`
  8. `HisDiagnosticDoctor.exe`
  9. `HisWardReportCreator.exe`
  10. `HisSummaryTrackingCreator.exe`
  11. `HisSummaryTrackingDoctor.exe`
  12. `HisDressingOrder.exe`
  13. `HospitalShiftReporter.exe`
  14. `HisClsCtchTracker.exe`

## Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md` and `explorer_survey_2/survey_report.md`.
2. Repair `HisWardReportCreator.cs`:
   - Locate corruption at line 689 (`notes.Add("Đi        if (diag.Contains(...)`) and line 1049.
   - Restore the missing methods / closing blocks as documented in Section 4.2 of `explorer_survey_2/survey_report.md`:
     - Restore proper closure of previous block:
       `notes.Add("Mới vào khoa điều trị nội trú, hoàn thiện hồ sơ bệnh án và cận lâm sàng");`
       `return string.Join("; ", notes.Distinct());`
       `}`
     - Restore `FormatCurrentStatus(List<V_HIS_TRACKING> trks, PatientWardRecord rec)`:
       handling rest day strings ("T4 Ngày nghỉ", "Ngày nghỉ lễ", etc.) and default status.
     - Restore `FormatTreatmentPlan(PatientWardRecord rec)` method header:
       `public static string FormatTreatmentPlan(PatientWardRecord rec)`
       `{`
       `string diag = (rec.ReviewedDiagnosis ?? "").ToLower();`
       `List<string> plans = new List<string>();`
     - Clean up line 1049 where HTML formatting was accidentally spliced into `Run`.
   - Verify `HisWardReportCreator.cs` compiles cleanly with 0 errors via 64-bit `csc.exe`.
3. Update `refs.rsp`:
   - Replace all hardcoded `D:\his 3-9\...` paths in `refs.rsp` (and `.agents\skills\his-clinical-operations\scripts\refs.rsp`) with relative paths `.\ReferencedAssemblies\...` or generate dynamic response file during build.
4. Create Master Build Script (`build_all_cs_tools.ps1` and `build_all_cs_tools.bat`):
   - Resolves `csc.exe` 64-bit via `$env:CSC` or `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
   - Compiler flags: `/target:exe /platform:x64 /nologo /utf8output`.
   - Discovers source files across root and `.agents\skills\his-clinical-operations\scripts\`.
   - Compiles all 14 tools cleanly.
   - Specifically compiles `HisLeanproAssigner.cs` to generate the missing `HisLeanproAssigner.exe`.
   - Synchronizes all 14 `.exe` binaries to BOTH project root and `.agents\skills\his-clinical-operations\scripts\`.
5. Verification:
   - Run `build_all_cs_tools.ps1` and verify all 14 executables build with exit code 0.
   - Verify all 14 `.exe` files exist in project root and in scripts directory.
   - Verify PE x64 architecture for all 14 binaries.
   - Test running `HisLeanproAssigner.bat` to verify the new binary executes properly.
   - Test running `HisDiagnosticDoctor.bat health` to verify system health.
6. Write `handoff.md` and `progress.md` in `.agents\worker_m2\`.

## 2026-09-09T18:33:51Z
You are Worker M2 for the HIS Automation project.
Your assigned working directory is:
`f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_m2`

Read your assignment in `DISPATCH.md` at that path.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md`.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`.
Read `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_2\survey_report.md`.

Execute Milestone M2:
1. Repair syntax corruption in `HisWardReportCreator.cs` at line 689 and line 1049 (restore missing `FormatCurrentStatus` and `FormatTreatmentPlan` headers and clean up method closures as documented in Section 4.2 of explorer_survey_2 report).
2. Update `refs.rsp` (both root and scripts) from hardcoded `D:\his 3-9\...` to relative `.\ReferencedAssemblies\...` or dynamic rsp generation.
3. Create master compiler script `build_all_cs_tools.ps1` (and companion `build_all_cs_tools.bat`).
4. Compile the missing `HisLeanproAssigner.exe` and compile all 14 clinical C# tools via 64-bit `csc.exe` (`/platform:x64`).
5. Synchronize all 14 `.exe` binaries between project root and `.agents\skills\his-clinical-operations\scripts\`.
6. Verify all 14 binaries exist, are PE x64, run without missing assembly errors, and `HisLeanproAssigner.bat` functions properly.
7. Keep `progress.md` updated, write a comprehensive `handoff.md` in `.agents\worker_m2\`, and send a completion message back to the caller.
