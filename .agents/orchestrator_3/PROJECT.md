# Project: HisPacsUploader.exe

## Architecture
`HisPacsUploader.exe` is a high-performance, standalone C# (.NET Framework 4.8 / csc.exe x64) CLI tool integrated into the Bach Mai Hospital HIS ecosystem. It connects the hospital's internal PACS/RIS network with cloud sharing for clinicians:

```
[Clinician / Agent CLI]
         │
         ▼
[HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open]]
         │
         ├── 1. PacsClient:
         │      - Login to RIS Minerva (192.168.200.110/ris/rest/login)
         │      - Query patient studies (192.168.200.110/ris/rest/study?pid=VS.<MaBN>)
         │      - Stream study zip (192.168.200.107:8080/pacs/0/rest/{pacsAE}/studies/{studyIUID}?contentType=application/zip)
         │      - Extract .dcm files to %TEMP%\HisPacs_{MaBN}_{Timestamp}\dicom\
         │
         ├── 2. ViewerPackager:
         │      - Write embedded 24KB standalone DICOM Web Viewer (index.html) into temp folder
         │      - Generate manifest.json (list of slices, dimensions, series info)
         │
         ├── 3. DriveUploader:
         │      - Locate rclone.exe (WinGet / UserProfile / PATH)
         │      - Upload folder to Google Drive (target account: onthibacsinoitru9999@gmail.com)
         │      - Generate signed shareable link with configurable TTL (24h default / 7d)
         │      - Clean up local temporary files
         │
         └── 4. Output & Logging:
                - Log execution details to LogSystem.txt
                - Print progress to stderr / stdout
                - Output signed URL as final line of stdout
                - Optionally open URL in default browser (--open)
```

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Patient PACS Query | Query RIS Minerva by MaBN to fetch StudyInstanceUID and pacsAE | M1 | Survey R1 |
| 2 | DICOM Study Zip Download | Stream entire study zip from PACS server in 1 HTTP GET request | M1 | Survey R1 |
| 3 | Local Temp Extract & Cleanup | Extract .dcm files to %TEMP%, clean up on upload success | M1 | Survey R1 |
| 4 | No-Image & Invalid MaBN Guard | Graceful error output with exit code != 0, no crash | M1 | Survey R1 |
| 5 | Embedded DICOM Web Viewer | 24KB zero-dependency canvas viewer with zoom/pan/scroll/presets | M2 | Survey R3 |
| 6 | Viewer Manifest Generator | Generate manifest.json with series/slice ordering for zero-CORS viewing | M2 | Survey R3 |
| 7 | rclone Toolchain Auto-discovery | Auto-detect rclone.exe in WinGet or local paths | M3 | Survey R2 |
| 8 | Google Drive Folder Upload | Upload folder to onthibacsinoitru9999@gmail.com on Google Drive | M3 | Survey R2 |
| 9 | Signed Shareable Link with TTL | Generate signed link with 24h/7d expiration accessible without login | M3 | Survey R2 |
| 10 | Standard CLI Syntax | HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open] | M4 | Survey R4 |
| 11 | Stdout URL & HIS Logging | Last line stdout is the signed URL, log to LogSystem.txt | M4 | Survey R4 |
| 12 | End-to-End Verification | Verified against live patient / test study and Drive upload | M4 | Survey R4 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | M1: PACS/DICOM Engine | Query RIS Minerva & download/extract DICOM files | none | PLANNED |
| 2 | M2: DICOM Viewer Packager | Standalone HTML5 canvas viewer & manifest generator | none | PLANNED |
| 3 | M3: Google Drive & TTL Link | rclone upload & HMAC signed TTL link generation | M1, M2 | PLANNED |
| 4 | M4: CLI Integration & Verification | HisPacsUploader.exe entrypoint, logging, tests | M1, M2, M3 | PLANNED |

## Interface Contracts

### M1 (PacsClient) Contract
- Class: `PacsClient`
- Method: `StudyDownloadResult DownloadStudy(string maBn, string tempDir)`
- Result:
  ```csharp
  public class StudyDownloadResult {
      public bool Success { get; set; }
      public string ErrorMessage { get; set; }
      public string StudyInstanceUid { get; set; }
      public string StudyDescription { get; set; }
      public string StudyDate { get; set; }
      public List<string> DicomFiles { get; set; } // Full paths to .dcm files
  }
  ```

### M2 (ViewerPackager) Contract
- Class: `ViewerPackager`
- Method: `void PackageViewer(string folderPath, List<string> dicomFiles, string patientName, string maBn, string studyDate)`
- Output: writes `index.html` and `manifest.json` directly into `folderPath`.

### M3 (DriveUploader) Contract
- Class: `DriveUploader`
- Method: `DriveUploadResult UploadAndShare(string folderPath, string maBn, string studyDate, TimeSpan ttl)`
- Result:
  ```csharp
  public class DriveUploadResult {
      public bool Success { get; set; }
      public string ErrorMessage { get; set; }
      public string ShareableUrl { get; set; }
      public DateTime ExpirationTime { get; set; }
  }
  ```

### M4 (Main CLI) Contract
- Entry point: `HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open]`
- Exit codes: `0` = Success, `1` = Invalid arguments / MaBN not found, `2` = Download failed, `3` = Upload failed.
- Stdout: Status messages to stderr or prefix lines; **Final line of stdout MUST be the exact Shareable URL**.
- Logging: Appends timestamped log entry to `LogSystem.txt` with format `[YYYY-MM-DD HH:mm:ss] HisPacsUploader | MaBN={MaBN} | StudyUID={UID} | URL={URL} | Status=SUCCESS`.

## Code Layout
- Root: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`
- Source file: `HisPacsUploader.cs` (or modular parts linked during compilation)
- Binary executable: `HisPacsUploader.exe` in project root
- Helper script: `HisPacsUploader.bat` in project root
- Embedded Viewer HTML: embedded within C# or bundled as `ViewerAssets\index.html`
