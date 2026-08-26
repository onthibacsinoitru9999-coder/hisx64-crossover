@echo off
setlocal
set SCRIPT_DIR=%~dp0

if exist "%SCRIPT_DIR%HisSummaryTrackingCreator.exe" (
    "%SCRIPT_DIR%HisSummaryTrackingCreator.exe" %*
) else (
    echo [ERROR] Khong tim thay HisSummaryTrackingCreator.exe! Vui long bien dich lai.
    exit /b 1
)
