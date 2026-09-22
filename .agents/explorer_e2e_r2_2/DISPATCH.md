## 2026-09-10T14:54:45Z
Task:
1. Investigate the Google Drive upload fallback issue in `DriveUploader.UploadAndShare`:
   - Currently, if rclone lacks a configured `gdrive:` remote, it generates a synthetic 404 Google Drive URL and swallows errors.
   - Investigate how `DriveUploader` should behave:
     a. If rclone succeeds, use the real Drive sharing link.
     b. If rclone is unconfigured / fails, and `STRICT_DRIVE_UPLOAD=1`, exit with code 3.
     c. If `--open` is passed and cloud upload is unavailable or failed, how should the viewer be opened? Either serve via `LocalViewerServer` (listening until browser finishes or timeout) or open local file, so the clinician actually sees the DICOM slices instead of a 404.
2. Provide concrete recommendations for resolving the dead code in `LocalViewerServer` vs `--open` cloud link.
3. Write your complete fix recommendations to `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_e2e_r2_2\handoff.md`.
4. Send a completion message to parent via send_message.
