# Plan — HisPacsUploader.exe E2E Verification and Hardening

## Overview
Orchestrate comprehensive verification, adversarial testing, code review, and forensic auditing for `HisPacsUploader.exe` against all Acceptance Criteria in `ORIGINAL_REQUEST.md`.

## Step-by-Step Plan

### Step 1: Pre-flight Inspection & Target Readiness (Explorer)
- Dispatch Explorers to inspect:
  - `HisPacsUploader.exe`, `HisPacsUploader.cs`, `HisPacsUploader.bat`.
  - Available patient records at Khoa 57 / 915 with PACS studies via `HisClinicalCli.exe lookup` / `HisPacsCli.bat`.
  - Environmental readiness: `rclone.exe`, Google Drive remote `gdrive:`, RIS Minerva connectivity (192.168.200.110 & 192.168.200.107), Chrome/Edge browser availability.
  - Produce an actionable verification strategy report with concrete test cases.

### Step 2: Implementation & E2E Test Execution (Worker)
- Dispatch Worker to:
  - Execute end-to-end tests covering:
    1. Valid patient query & study download (extract .dcm to temp).
    2. Zero-dependency DICOM web viewer packaging (`index.html` + `manifest.json`).
    3. Google Drive upload via rclone and public signed link generation with TTL (`--ttl 24h`, `--ttl 7d`).
    4. Temp directory cleanup after upload.
    5. Final stdout line is the public URL.
    6. Browser launch with `--open`.
    7. Clean error exit (code != 0, descriptive error, no crash) on invalid MaBN.
    8. HIS logging into `LogSystem.txt`.
  - If any issues arise, fix `HisPacsUploader.cs`, recompile cleanly using `csc.exe` x64, and re-verify.

### Step 3: Independent Review & Adversarial Stress Testing (Reviewers & Challengers)
- Dispatch 2 independent Reviewers to review code quality, edge cases, contracts, and security.
- Dispatch 2 Challengers to adversarially test limits (spaces in paths, corrupted studies, invalid arguments, network timeouts, TTL validity, browser console error inspection).

### Step 4: Forensic Integrity Audit (teamwork_preview_auditor)
- Dispatch Forensic Auditor to inspect source code, compiled binary, and test results for any hardcoding, mock facades, or shortcuts.
- Binary veto gate: Must be CLEAN.

### Step 5: Gate Evaluation & Victory Report
- Synthesize all results in `GATE_STATUS.md`.
- Write comprehensive `handoff.md`.
- Report to Sentinel via `send_message` with links to artifacts for Victory Auditor clearance.
