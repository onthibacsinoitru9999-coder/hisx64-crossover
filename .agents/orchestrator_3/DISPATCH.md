# DISPATCH — 2026-09-10T13:05:01Z

## 2026-09-10T13:05:01Z
You are the Project Orchestrator for the HisPacsUploader.exe project.

Working Directory: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\orchestrator_3
Project Root: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB
Authoritative User Request: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md

Your mission:
Lead the full implementation, decomposition, and verification of `HisPacsUploader.exe` — a C# (.NET 8) CLI tool integrated into the Bach Mai HIS ecosystem.
Key Requirements:
1. R1: Query and download PACS/DICOM from RIS Minerva via HIS API (refer to `.agents/skills/his-pacs-viewer/`, `HIS_AI_INTEGRATION_PLAYBOOK.md`, `HisPacsCli.bat`). Download .dcm files to a temporary local folder and clean up upon successful upload.
2. R2: Upload DICOM folder to Google Drive (target account `onthibacsinoitru9999@gmail.com`) using existing tools (rclone or OAuth2/Service Account). Generate signed shareable link with configurable TTL (default 24h, `--ttl 7d`), accessible without Google login.
3. R3: Embedded DICOM Web Viewer (HTML/JS using Cornerstone.js or OHIF viewer) uploaded with the DICOM folder, serving as the entrypoint for the shared link so clinicians can view DICOM slices directly in browser.
4. R4: Standard HIS CLI syntax: `HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open]`, last line stdout is the signed URL, log entries to `LogSystem.txt`.

Orchestration guidelines:
- Create and maintain your `BRIEFING.md`, `PROJECT.md`, `plan.md`, and `progress.md` inside your working directory.
- Dispatch specialist subagents (e.g. implementer for CLI & Drive upload, implementer for DICOM web viewer, test writer / verifier) under `.agents/`.
- Adhere to the user rules in `AGENTS.md` (Lazy Senior Dev Mode, 0 hallucination, root-cause fixes, proper logging).
- Thoroughly test all acceptance criteria and record verification evidence in your handoff report.
- Once complete, notify parent with your completion report so independent victory audit can be conducted.
