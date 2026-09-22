## 2026-09-10T14:41:06Z
You are Challenger E2E 2.
Working Directory: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\challenger_e2e_2
You MUST read ORIGINAL_REQUEST.md first: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md
Also read Project Specification: f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\orchestrator_3\PROJECT.md

Task:
1. Adversarially stress test the DICOM Web Viewer (`ViewerAssets\index.html` and embedded in `HisPacsUploader.exe`).
2. Test:
   - Offline zero-dependency verification: verify that loading `index.html` makes zero HTTP calls to external CDNs, fonts, or third-party servers.
   - TTL expiration behavior: verify that passing `?exp=<past_timestamp>` immediately displays the expired banner and blocks image loading, while `?exp=<future_timestamp>` allows viewing.
   - DICOM parsing robustness: test with DICOM files containing standard PS 3.10 headers and corrupted/non-DICOM files.
   - Manifest.json parsing and dropzone fallback under `file://` protocol.
3. Document each test case, inputs, outputs, and results.
4. Render an unambiguous verdict: APPROVE or REJECT.
5. Write your complete handoff report to `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\challenger_e2e_2\handoff.md`.
6. Send a message to parent notifying that you have completed.
