# DISPATCH — Explorer PACS R1 (HisPacsUploader DICOM Query & Download)

## Assignment
You are Explorer PACS R1 for the HisPacsUploader.exe project.
Working Directory: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_pacs_r1`
Project Root: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`
Authoritative User Request: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (Focus on Follow-up section from line 40 onwards!)

## CRITICAL NOTICE
Do NOT investigate batch files or general query latency from the earlier section of ORIGINAL_REQUEST.md.
Your SOLE FOCUS is the **Follow-up section (lines 40-108) of ORIGINAL_REQUEST.md**, specifically **Requirement R1: Tra cứu & Tải ảnh PACS / DICOM**:
- "Công cụ nhận Mã BN (PatientId hoặc VisitId) làm tham số dòng lệnh."
- "Gọi API HIS / RIS Minerva (đã có trong codebase tại `.agents/skills/his-pacs-viewer/`) để lấy danh sách StudyInstanceUID và URL ảnh DICOM."
- "Tải toàn bộ file DICOM (.dcm) về thư mục tạm cục bộ, không lưu lâu dài (xoá sau khi upload xong)."

## Detailed Tasks
1. Inspect `.agents/skills/his-pacs-viewer/SKILL.md` and all files inside `.agents/skills/his-pacs-viewer/`.
2. Inspect `HisPacsCli.bat` and any related scripts or C# source files (`HisPacsCli.cs`, `HisClinicalCli.cs`, etc.).
3. Examine `HIS_AI_INTEGRATION_PLAYBOOK.md` section on PACS, RIS Minerva, and CĐHA.
4. Determine:
   - What API endpoints are called to get patient PACS/RIS studies? (e.g. `api/HisSereServ/GetView`, RIS Minerva URL format, Minerva PACS Web Viewer URLs).
   - What parameters and headers (tokens, cookies) are needed?
   - How are StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID, or DICOM image URLs obtained?
   - How can actual `.dcm` files be downloaded from RIS Minerva or the PACS WADO server (WADO-URI, WADO-RS, or HTTP GET)?
   - If RIS Minerva provides a web viewer link (`http://.../viewer?...`), what is the underlying DICOM storage/WADO endpoint? Can `.dcm` files be fetched directly, or does RIS Minerva expose a zip/download endpoint?
   - What happens when a patient has no PACS images or an invalid MaBN?
5. Write your findings, exact URLs, request payloads, sample C# download code, and data flows to:
   `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_pacs_r1\survey_report.md`
   and write a standard 5-component `handoff.md`.

## 2026-09-10T13:16:45Z
You are Explorer PACS R1 for the HisPacsUploader.exe project.
Your working directory is f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_pacs_r1
Read your instructions in f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_pacs_r1\DISPATCH.md
Read ORIGINAL_REQUEST.md at f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md (Specifically follow-up lines 40-108 on HisPacsUploader R1: PACS/DICOM download).
Investigate .agents/skills/his-pacs-viewer/, HisPacsCli.bat, HIS_AI_INTEGRATION_PLAYBOOK.md, and relevant C# code to uncover the exact API calls, endpoints, and DICOM download mechanisms.
Write survey_report.md and handoff.md in your working directory, then send a message to orchestrator.
