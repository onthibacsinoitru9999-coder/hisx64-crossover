@echo off
setlocal
set SCRIPT_DIR=%~dp0

if exist "%SCRIPT_DIR%HisWardReportCreator.exe" (
    "%SCRIPT_DIR%HisWardReportCreator.exe" %*
) else (
    echo [ERROR] Khong tim thay HisWardReportCreator.exe! Vui long bien dich lai.
    exit /b 1
)

if exist "%SCRIPT_DIR%Reports\WardReports" (
    rclone copy "%SCRIPT_DIR%Reports\WardReports" "gdrive:BaoCaoBuongBenh_Khoa57" --quiet
)

