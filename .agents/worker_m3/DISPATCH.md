# Task Assignment — Worker M3 (Clinical Query Latency & Batching)

## Objective
Execute Milestone M3 (Requirements R3: Features F7, F8, F9).
Optimize query performance in `HisClinicalCli.cs` and `HisWardReportCreator.cs` via batch querying (eliminating N+1 HTTP loops), standardize tail-seek 128KB `FileShare.ReadWrite` live token reading across clinical tools, recompile via master build script, and benchmark latency to ensure lookup/orders < 1.5s and ward report < 2.5s.

## Authoritative Inputs
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (MUST read first)
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\PROJECT.md`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_2\survey_report.md` (specifically Section 5: Performance Bottlenecks & RCA)

## Exclusively Owned Files
- `.agents\skills\his-clinical-operations\scripts\HisClinicalCli.cs`
- `HisWardReportCreator.cs`
- `HisRationAssigner.cs`
- `HisDiagnosticDoctor.cs`
- `HisLeanproAssigner.cs`
- Recompiled `.exe` binaries of modified tools in root and scripts directory

## Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Tasks
1. Read `ORIGINAL_REQUEST.md` and `explorer_survey_2/survey_report.md` Section 5.
2. Optimize `HisClinicalCli.cs` (Feature F7):
   - Locate the `orders <MãBN>` command handling around lines 1357-1367 where `foreach (var r in sorted)` sends sequential single queries `api/HisSereServ/GetView` with `SERVICE_REQ_ID = r.ID`.
   - Batch query optimization:
     ```csharp
     var allReqIds = sorted.Select(x => x.ID).ToList();
     HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = allReqIds };
     var allSsList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
     var ssMap = (allSsList != null)
         ? allSsList.GroupBy(x => x.SERVICE_REQ_ID).ToDictionary(g => g.Key, g => g.ToList())
         : new Dictionary<long, List<V_HIS_SERE_SERV>>();
     ```
   - In the display loop, retrieve items from `ssMap[r.ID]` instead of querying HTTP over the network.
3. Optimize `HisWardReportCreator.cs` (Feature F8):
   - Locate the patient iteration loop around lines 246-369 where each inpatient triggers 5 sequential HTTP calls (`HisTreatment`, `HisTracking`, `HisServiceReq`, `HisSereServ`, `HisDebate`).
   - Batch query optimization:
     Collect `allTreatmentIds = patients.Select(p => p.TREATMENT_ID).Distinct().ToList();`
     Perform 5 batch queries for the entire ward cohort:
     - `HisTreatmentViewFilter { IDs = allTreatmentIds }`
     - `HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds }`
     - `HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds }`
     - `HisSereServViewFilter { TDL_TREATMENT_IDs = allTreatmentIds }`
     - `HisDebateFilter { TREATMENT_IDs = allTreatmentIds }`
     Build lookup dictionaries (`treatmentMap`, `trackingMap`, `serviceReqMap`, `sereServMap`, `debateMap`) and populate `PatientWardRecord` from memory.
4. Standardize Live Token Reader Tail-Seek (Feature F9):
   - Ensure `FileShare.ReadWrite`, seek lùi 128KB (`Math.Min(131072L, length)`), and `Process.GetProcessesByName("HIS")` detection are present in token readers across `HisClinicalCli.cs`, `HisWardReportCreator.cs`, `HisRationAssigner.cs`, `HisDiagnosticDoctor.cs`, `HisLeanproAssigner.cs`.
5. Recompile and Synchronize:
   - Run `build_all_cs_tools.bat` to recompile the modified C# tools.
   - Verify exit code 0 and PE x64 architecture.
6. Benchmarking & Verification:
   - Benchmark `HisClinicalCli.exe lookup 0003757502` -> measure elapsed time, verify < 1.5s.
   - Benchmark `HisClinicalCli.exe orders 0003757502` -> measure elapsed time, verify < 1.5s (previously 1.985s).
   - Benchmark `HisWardReportCreator.exe` -> measure elapsed time, verify < 2.5s (previously 10.599s).
   - Run `HisDiagnosticDoctor.bat health` -> verify 100% Ready.
7. Write `handoff.md` and `progress.md` in `.agents\worker_m3\`.

## 2026-09-09T18:47:52Z
Caller: Parent Agent (92384618-4fc3-4925-a710-33d13faecd26)
Task: Execute Milestone M3: F7, F8, F9 optimizations, recompilation of 14 tools, benchmark verifications, handoff.

