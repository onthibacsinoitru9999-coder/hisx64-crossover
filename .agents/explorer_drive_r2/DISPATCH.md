# DISPATCH — Explorer Drive R2 (Google Drive Upload & Signed Link with TTL)

## Assignment
You are Explorer Drive R2 for the HisPacsUploader.exe project.
Working Directory: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_drive_r2`
Project Root: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB`
Authoritative User Request: `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\ORIGINAL_REQUEST.md` (Focus on Follow-up section from line 40 onwards!)

## CRITICAL NOTICE
Do NOT investigate batch files or general C# compilation issues from the earlier section of ORIGINAL_REQUEST.md.
Your SOLE FOCUS is the **Follow-up section (lines 40-108) of ORIGINAL_REQUEST.md**, specifically **Requirement R2: Upload lên Google Drive & Sinh Signed Link có hạn**:
- "Upload folder ảnh DICOM (theo BN, ngày chụp) lên Google Drive bằng OAuth2 Service Account hoặc rclone (đã có sẵn trong hệ thống) tới tài khoản `onthibacsinoitru9999@gmail.com`."
- "Tạo Shareable Link có thời hạn: mặc định 24 giờ, có thể truyền flag `--ttl 7d`."
- "Link phải hoạt động mà không cần người xem đăng nhập Google."
- Also check target runtime (.NET 8 SDK vs .NET Framework 4.8 / csc.exe) on this machine!

## Detailed Tasks
1. Investigate how Google Drive upload is intended to work in this ecosystem:
   - Check where `rclone.exe` is located on the entire machine (`Get-ChildItem -Path C:\, D:\, F:\ -Filter rclone.exe -Recurse -ErrorAction SilentlyContinue`, check user profile, `C:\Users\HP\`, `C:\Program Files`, etc.).
   - If `rclone` is installed, check `rclone config show`, remotes configured, whether `gdrive` or `onthibacsinoitru9999@gmail.com` remote exists, and how `rclone link` works.
   - If rclone is NOT installed, check if Google Service Account JSON credentials exist, or OAuth2 client secrets exist in repo or user profile (`.rclone.conf`, `credentials.json`, `client_secret*.json`, etc.).
   - Check if python environment (`python -c "import googleapiclient"`) or curl or PowerShell or a C# Google Drive client can be used, or if a portable rclone binary can be acquired or exists.
2. Investigate Shareable Link with TTL (24h / 7d):
   - How does Google Drive API support link expiration? (e.g. `permissions.create` with `expirationTime` ISO timestamp for `anyoneWithLink` / `reader` role).
   - How does rclone support link expiration? (`rclone link --expire 24h` or `rclone link --expire 7d`).
   - How does a shared link look and behave when opened without Google login?
3. Check .NET runtime environment:
   - Run `dotnet --version` and check if .NET 8 SDK is installed on this machine.
   - If .NET 8 SDK is installed, can we build a .NET 8 C# project (`dotnet build / dotnet publish`)?
   - If only .NET Framework 4.8 / `csc.exe` is installed, can `HisPacsUploader` be built as a .NET Framework 4.8 standalone CLI with `csc.exe` or does .NET 8 work? Document the exact compiler command.
4. Write your findings, exact CLI commands or C# code snippets for Drive upload & link generation with TTL, and environment status to:
   `f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\.agents\explorer_drive_r2\survey_report.md`
   and write a standard 5-component `handoff.md`.
5. Send a message to orchestrator when finished.
