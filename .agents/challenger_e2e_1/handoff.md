# Handoff Report — Challenger E2E 1

- **Date**: 2026-09-10T21:55:00+07:00
- **Evaluator**: Challenger E2E 1 (critic, specialist)
- **Subject**: `HisPacsUploader.exe` & `HisPacsUploader.bat` End-to-End Stress Testing & Adversarial Verification
- **Target Verdict**: **REJECT**

---

## 1. Observation

### Observation 1: Concurrency Collision & Unhandled IOException (CRITICAL BUG)
- **Code Inspection**: In `HisPacsUploader.cs` (lines 1011-1013):
  ```csharp
  string tempRoot = Path.Combine(Path.GetTempPath(), "HisPacsUploader");
  string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));
  ```
  The folder name template `Pacs_{MaBN}_{yyyyMMdd_HHmmss}` does NOT contain Process ID (`Process.GetCurrentProcess().Id`), does NOT contain GUID, and has only 1-second timestamp resolution (`HHmmss`).
- **Empirical Execution**: Executed `powershell -NoProfile -ExecutionPolicy Bypass -File .\test_concurrency.ps1` launching two simultaneous instances of `HisPacsUploader.exe` for patient `0004009330` at the exact same sub-second.
- **Verbatim Result**:
  - Process 1: ExitCode = 0, Stdout has valid signed URL.
  - Process 2: ExitCode = 1, Stdout has 0 lines.
  - Process 2 Verbatim Stderr:
    ```
    [ERROR] Loi ngoai le khi ket noi RIS/PACS: The process cannot access the file '1.3.12.2.1107.5.3.63.33108.12.202609081111395551.dcm' because it is being used by another process.
    ```
  - Process 1 and Process 2 extracted DICOM files into the identical folder path `.../Pacs_0004009330_20260910_215310/dicom/`, resulting in Windows OS file-locking collision.

---

### Observation 2: CLI Syntax & TTL Edge Cases
Ran 12 test cases in Suite 1 (`run_adversarial_suite.ps1`):
1. **TC-A01 (Exe Empty Args)**:
   - Command: `HisPacsUploader.exe`
   - Result: ExitCode `1`. Stdout `0` lines. Stderr prints `[ERROR] Thieu tham so MaBN!` and usage. (PASS)
2. **TC-A02 (Bat Empty Args)**:
   - Command: `cmd.exe /c HisPacsUploader.bat`
   - Result: ExitCode `1`.
   - **Flaw**: Stderr `0` lines, but **Stdout has 10 lines** of banner text because `echo` in `HisPacsUploader.bat` lines 50-60 is not redirected to stderr (`>&2`).
3. **TC-A03 (Unknown Flag)**:
   - Command: `HisPacsUploader.exe 0004009330 --unknown`
   - Result: ExitCode `0`. Silently ignores `--unknown`.
4. **TC-A04 (Invalid TTL Text)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl invalid`
   - Result: ExitCode `0`. Silently falls back to default `24h` without error or warning.
5. **TC-A05 (Large TTL Unbounded)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl 100d`
   - Result: ExitCode `0`. Produces signed link with `ttl=100d&exp=1797691879` (100 days). Specification called for 24h - 7d bounds, but no ceiling is enforced.
6. **TC-A06 (Invalid TTL Unit)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl 0s`
   - Result: ExitCode `0`. Silently falls back to 24h.
7. **TC-A07 (Zero Duration TTL)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl 0d`
   - Result: ExitCode `0`. Produces signed URL with `ttl=0h&exp=1789051889`. Link is instantly expired upon creation!
8. **TC-A08 (Negative Duration TTL)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl -5h`
   - Result: ExitCode `0`. Produces signed URL with `ttl=-5h&exp=1789033894`. Link was expired 5 hours in the past!
9. **TC-A09 (Dangling Flag)**:
   - Command: `HisPacsUploader.exe 0004009330 --ttl` (trailing flag without value)
   - Result: ExitCode `0`. Gracefully handled without `IndexOutOfRangeException`.
10. **TC-A10 (Whitespace in MaBN)**:
    - Command: `HisPacsUploader.exe "  0004009330  "`
    - Result: ExitCode `0`. Properly trimmed to `0004009330`.
11. **TC-A11 (Whitespace in TTL)**:
    - Command: `HisPacsUploader.exe 0004009330 --ttl " 7d "`
    - Result: ExitCode `0`. Properly parsed as `7d`.
12. **TC-A12 (Help Flag)**:
    - Command: `HisPacsUploader.exe --help`
    - Result: ExitCode `0`. Stdout `0` lines. Stderr prints usage instructions.

---

### Observation 3: Invalid MaBN Formats & Sanitization
Ran 8 test cases in Suite 2 (`run_adversarial_suite.ps1`):
1. **TC-B01 (Letters)**: `ABCD1234EF` -> ExitCode `1`, Stdout `0` lines, Stderr: `[ERROR] Khong tim thay ca chup nao tren RIS/PACS cho benh nhan ABCD1234EF`.
2. **TC-B02 (Symbols)**: `!@#$%^&*()` -> ExitCode `1`, Stdout `0` lines, Stderr: `[ERROR] Khong tim thay ca chup nao...`.
3. **TC-B03 (Non-existent 10-digit ID)**: `9999999999` -> ExitCode `1`, Stdout `0` lines, Stderr: `[ERROR] Khong tim thay ca chup nao...`.
4. **TC-B04 (All Zeroes ID)**: `0000000000` -> ExitCode `1`, Stdout `0` lines, Stderr: `[ERROR] Khong tim thay ca chup nao...`.
5. **TC-B05 (Whitespace Only)**: `"   "` -> ExitCode `1`, Stdout `0` lines, Stderr: `[ERROR] Thieu tham so MaBN!`.
6. **TC-B06 (Prefix VS.)**: `VS.0004009330` -> ExitCode `0`, Stdout `1` line (valid URL).
7. **TC-B07 (Unknown Prefix)**: `INVALID.0004009330` -> ExitCode `1`, Stdout `0` lines, graceful error.
8. **TC-B08 (Short Numeric ID)**: `4009330` -> ExitCode `0`, auto-padded to `VS.0004009330`, Stdout `1` line (valid URL).

---

### Observation 4: Stdout Redirection Purity
Ran 4 test cases in Suite 3 (`run_adversarial_suite.ps1`):
1. **TC-C01 (Exe Valid MaBN)**:
   - Stdout: **Strictly 1 line** matching `^https:\/\/drive\.google\.com\/drive\/folders\/1pacs_.*$`.
   - Stderr: 100% of diagnostic banners, timing, DICOM PS 3.10 verification, and cleanup notices. Zero diagnostic text in stdout.
2. **TC-C02 (Bat Valid MaBN)**:
   - Stdout: **Strictly 1 line** (the URL). Zero diagnostic lines.
3. **TC-C03 (Exe Failure)**:
   - Stdout: **Strictly 0 lines**. Zero leakage.
4. **TC-C04 (Bat Failure)**:
   - Stdout: **Strictly 0 lines**. Zero leakage.

---

### Observation 5: DICOM Web Viewer Quality
- Inspected `ViewerAssets\index.html` (30KB):
  - Pure zero-dependency HTML5 / Canvas implementation.
  - Implements DICOM PS 3.10 binary parser (`parseDicomP10`), Window/Leveling (`renderSlice`), Presets (Bone, Brain, Lung, Soft Tissue), Invert, Zoom, Pan, Slices navigation.
  - Embedded as executable resource `/resource:"ViewerAssets\index.html",HisPacsUploader.ViewerAssets.index.html`.

---

## 2. Logic Chain

1. **Premise 1**: The task specification explicitly mandated checking whether temp folder generation includes process and timestamp uniqueness to resist concurrency collision:
   `"Concurrency / temp folder collision resilience: check if temp folder generation includes process/timestamp uniqueness."`
2. **Premise 2**: In `HisPacsUploader.cs` line 1012, `sessionFolder` is defined as:
   `string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));`
   This lacks `Process.GetCurrentProcess().Id` and lacks `Guid.NewGuid()`. The timestamp resolution is 1 second (`HHmmss`).
3. **Premise 3**: In an automated hospital HIS environment with multiple background agents, automated workflows, or multiple doctors querying the same patient at the same second:
   Multiple processes concurrently download and extract to the exact same directory: `%TEMP%\HisPacsUploader\Pacs_{MaBN}_{Timestamp}\dicom\`.
4. **Empirical Proof**: When two instances were launched concurrently in `test_concurrency.ps1`, Process 2 crashed with exit code 1 due to `System.IO.IOException: The process cannot access the file ... because it is being used by another process`.
5. **Premise 4**: In `HisPacsUploader.bat`, running with no arguments outputs 10 lines of banner text to `stdout` rather than `stderr`, polluting stdout streams for automated callers.
6. **Premise 5**: TTL parsing permits `0d` (instantly expired links), `-5h` (expired in the past), and unbounded days (`100d`), and silently defaults invalid strings (`--ttl invalid`) without alerting the caller.
7. **Conclusion**: While the core single-process PACS download, DICOM parsing, and stdout purity on success are well executed, the system fails the mandatory concurrency resilience test and has TTL validation gaps.

---

## 3. Caveats

- In a single-user, sequential CLI usage pattern, the temp folder race condition will not be triggered.
- Google Drive upload via `rclone` currently falls back to authenticated signed public URLs because the local `rclone` config does not yet have a configured `gdrive` remote on this workstation; this fallback is working as designed via HMAC-SHA256 URL signing.

---

## 4. Conclusion & Actionable Fixes

**VERDICT: REJECT**

### Required Actionable Fixes:
1. **Fix Temp Folder Concurrency Collision (`HisPacsUploader.cs:1012`)**:
   Change:
   ```csharp
   string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}", maBn.Replace("VS.", ""), DateTime.Now));
   ```
   To:
   ```csharp
   string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}_{2}_{3}",
       maBn.Replace("VS.", ""),
       DateTime.Now,
       Process.GetCurrentProcess().Id,
       Guid.NewGuid().ToString("N").Substring(0, 6)));
   ```
2. **Fix `HisPacsUploader.bat` Usage Output Redirection (lines 50-60)**:
   Redirect the usage echo lines in the batch script to `stderr` (`>&2 echo ...`), ensuring `stdout` remains strictly 0 lines when invoked without arguments.
3. **Add Bounds & Validation to `--ttl`**:
   Reject negative or zero TTL (`ttl <= TimeSpan.Zero`), enforce maximum ceiling (e.g. 7 days as per specification), and print a warning or error to `Console.Error` if `--ttl` value is unrecognizable instead of silently falling back.

---

## 5. Verification Method

To independently reproduce the findings:
1. **Verify Concurrency Collision Bug**:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\test_concurrency.ps1
   ```
   Inspect `concurrency_test_results.json`: Process 2 will exit with code `1` and error `The process cannot access the file ... because it is being used by another process`.
2. **Verify Full Adversarial Test Suite (24 Test Cases)**:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\run_adversarial_suite.ps1
   ```
   Inspect `adversarial_test_results.json`.
3. **Verify Batch Stdout Pollution on Empty Args**:
   ```powershell
   cmd.exe /c "HisPacsUploader.bat" > stdout.txt 2> stderr.txt
   Get-Content stdout.txt
   ```
   Observe 10 lines of banner text leaked into `stdout.txt`.
