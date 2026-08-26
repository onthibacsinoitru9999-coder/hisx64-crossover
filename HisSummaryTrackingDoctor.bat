@echo off
setlocal
set SCRIPT_DIR=%~dp0

if exist "%SCRIPT_DIR%HisSummaryTrackingDoctor.exe" (
    "%SCRIPT_DIR%HisSummaryTrackingDoctor.exe" %*
) else (
    echo [ERROR] Khong tim thay HisSummaryTrackingDoctor.exe! Vui long bien dich lai.
    exit /b 1
)
