@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY call "%SCRIPT_DIR%set_env.bat"

if exist "%SCRIPT_DIR%HisWardReportCreator.exe" (
    "%SCRIPT_DIR%HisWardReportCreator.exe" %*
    set "REPORT_ERR=!ERRORLEVEL!"
) else (
    echo [ERROR] Khong tim thay HisWardReportCreator.exe! Vui long bien dich lai.
    exit /b 1
)

set "RCLONE_EXE="
if defined RCLONE (
    set "RCLONE_EXE=%RCLONE%"
) else if exist "%SCRIPT_DIR%rclone.exe" (
    set "RCLONE_EXE=%SCRIPT_DIR%rclone.exe"
) else (
    where rclone >nul 2>nul
    if !ERRORLEVEL! equ 0 set "RCLONE_EXE=rclone"
)

if defined RCLONE_EXE (
    if exist "%SCRIPT_DIR%Reports\WardReports" (
        "%RCLONE_EXE%" copy "%SCRIPT_DIR%Reports\WardReports" "gdrive:BaoCaoBuongBenh_Khoa57" --quiet
    )
) else (
    echo [INFO] rclone chua duoc cai dat tren he thong. Bo qua dong bo Google Drive.
)

exit /b !REPORT_ERR!
