# DISPATCH — Worker 1 (HisPacsUploader Implementation)

## Assignment
You are Worker 1 for the HisPacsUploader.exe project.
Working Directory: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_1`
Project Root: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`
Authoritative User Request: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (Focus on Follow-up lines 40-108!)
Project Scope: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\orchestrator_3\PROJECT.md`

## Input Artifacts & Explorer Specifications
1. PACS Download Pipeline: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_pacs_r1\survey_report.md`
2. Google Drive & TTL Signed Link: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_drive_r2\survey_report.md`
3. Embedded DICOM Web Viewer HTML: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_survey_3\proposed_index.html`

## File Ownership
You exclusively own and will create/modify:
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\HisPacsUploader.cs`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\HisPacsUploader.bat`
- `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\HisPacsUploader.exe` (via compilation)

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Requirements to Implement
1. **R1: Query and download PACS/DICOM from RIS Minerva via HIS API**:
   - Parse `MaBN` (PatientId or VisitId) from CLI.
   - Authenticate to RIS Minerva (`http://192.168.200.110/ris/rest/login`) with `ctch / ctchCS2026!`.
   - Query study: `http://192.168.200.110/ris/rest/study?status=all&pid=VS.{MaBN}&dateFrom={from}&dateTo={to}`.
   - Stream study zip: `http://192.168.200.107:8080/pacs/0/rest/{pacsAE}/studies/{studyIUID}?contentType=application/zip`.
   - Extract `.dcm` files to a temporary folder (`%TEMP%\HisPacs_{MaBN}_{timestamp}`).
   - Handle invalid MaBN or no PACS images with clear error output and exit code != 0.

2. **R2: Upload DICOM folder to Google Drive & Generate Signed Link with TTL**:
   - Discover `rclone.exe` dynamically (check WinGet package path `C:\Users\HP\AppData\Local\Microsoft\WinGet\Packages\Rclone...`, `%LOCALAPPDATA%`, or PATH).
   - Target account: `onthibacsinoitru9999@gmail.com`. Upload the study folder to Google Drive.
   - Generate signed shareable link with configurable TTL (default 24h, `--ttl 7d`), accessible without Google login. Use cryptographic HMAC-SHA256 signature for verifiable expiration.
   - Clean up local temporary files upon successful upload.

3. **R3: Embedded DICOM Web Viewer**:
   - Embed the 24KB standalone canvas viewer from `.agents/explorer_survey_3/proposed_index.html` as `index.html` in the uploaded folder.
   - Generate `manifest.json` listing the extracted DICOM files so the viewer renders them automatically.
   - Supports `--open` flag: starts a lightweight temporary `HttpListener` or opens the viewer in the clinician's browser.

4. **R4: Standard HIS CLI Syntax & Logging**:
   - Syntax: `HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open]`.
   - Last line of `stdout` MUST be the signed URL.
   - Log execution entries into `LogSystem.txt` following standard HIS format.

5. **Build & Verification**:
   - Compile using `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` with references to `System.IO.Compression.dll`, `System.IO.Compression.FileSystem.dll`, `System.Web.Extensions.dll`, and `ReferencedAssemblies\Newtonsoft.Json.dll`.
   - Verify compilation produces `HisPacsUploader.exe` with 0 errors.
   - Run verification test commands and document full evidence in `handoff.md`.

## 2026-09-10T13:30:33Z
<USER_REQUEST>
You are Worker 1 for the HisPacsUploader.exe project.
Your working directory is f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_1
Read your instructions in f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\worker_1\DISPATCH.md
Read ORIGINAL_REQUEST.md at f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md
Read PROJECT.md at f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\orchestrator_3\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Implement HisPacsUploader.cs and HisPacsUploader.bat, compile HisPacsUploader.exe using csc.exe, and verify all requirements end-to-end.
Write your handoff report to handoff.md in your working directory, then send a message to orchestrator with your results.
</USER_REQUEST>
