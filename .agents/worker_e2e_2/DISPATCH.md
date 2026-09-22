## 2026-09-10T14:59:00Z
Assignment: Worker E2E 2
Exclusive ownership:
- HisPacsUploader.cs
- HisPacsUploader.bat
- ViewerAssets\index.html
- HisPacsUploader.exe

Tasks:
1. In HisPacsUploader.cs:
   a. Unique sessionFolder & Path Sanitization:
      - cleanMaBn = Regex.Replace((maBn ?? "").Replace("VS.", ""), @"[^a-zA-Z0-9_.-]", "_");
      - sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}_{2}_{3}", cleanMaBn, DateTime.Now, Process.GetCurrentProcess().Id, Guid.NewGuid().ToString("N").Substring(0, 8)));
   b. Stale Temp Directory Cleanup:
      - Implement CleanupTemp(tempRoot, sessionFolder) in finally as detailed in Explorer R2-1 report: delete current sessionFolder, purge any orphaned subdirectories/loose files older than 1 hour, and delete tempRoot if empty.
      - Manually purge the 2 old orphaned folders from %TEMP%\HisPacsUploader left by earlier tests.
   c. Drive Upload Fallback & Local Viewer Hosting:
      - Remove the synthetic 404 Google Drive URL generation (1pacs_{maBn}_{hash}) when rclone is not configured or fails.
      - If rclone succeeds: output the real Drive URL and exit 0.
      - If rclone fails or is unconfigured:
        * If STRICT_DRIVE_UPLOAD=1 or --open was NOT specified: Output clear error to stderr, stdout has 0 lines, log FAILED in Logs\HisPacsUploader.log, exit with code 3.
        * If --open was specified: Activate local viewer via LocalViewerServer.ServeAndOpen(sessionFolder). Print [BROWSER] Dang mo trinh xem cuc bo: http://127.0.0.1:<port>/index.html, output this local URL as the single line on stdout, launch browser, serve requests until browser finishes loading slices (idle timeout 30s or keypress), and then exit 0 with clean temp folder deletion.
2. In ViewerAssets/index.html:
   - Fix 2-byte alignment on TypedArray: When bitsAllocated === 16 and meta.pixelDataOffset % 2 !== 0, slice buffer with buffer.slice(meta.pixelDataOffset, meta.pixelDataOffset + numPixels * 2) so offset is 0-aligned, avoiding RangeError.
3. In HisPacsUploader.bat:
   - Route usage banner and errors to >&2 (e.g. ( ... ) 1>&2) so stdout redirection contains strictly 0 lines when invoked with invalid/empty arguments.
4. Recompile HisPacsUploader.exe cleanly using csc.exe 64-bit with embedded resource /resource:"%ROOT_DIR%ViewerAssets\index.html",HisPacsUploader.ViewerAssets.index.html.
5. Execute regression & verification tests:
   - Concurrency stress test: run two instances in parallel for patient 0004009330, confirm 0 collisions and both exit cleanly.
   - Temp cleanup test: verify Test-Path "$env:TEMP\HisPacsUploader" returns False.
   - Batch stdout redirection test with empty args: verify 0 lines in stdout file.
   - Test --open: verify local viewer serves slices cleanly if Drive remote is unconfigured.
   - Test STRICT_DRIVE_UPLOAD=1: verify exit code 3 with clear error and 0 lines on stdout.
   - Test live Hanoi patient 0004032715 and Ninh Binh 0004009330.
6. Write handoff report to .agents\worker_e2e_2\handoff.md.
7. Send completion message to parent.
