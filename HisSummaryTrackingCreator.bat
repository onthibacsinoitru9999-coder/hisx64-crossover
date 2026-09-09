@echo off
setlocal
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY call "%SCRIPT_DIR%set_env.bat"

if exist "%SCRIPT_DIR%HisSummaryTrackingCreator.exe" (
    "%SCRIPT_DIR%HisSummaryTrackingCreator.exe" %*
    exit /b %ERRORLEVEL%
) else (
    echo [ERROR] Khong tim thay HisSummaryTrackingCreator.exe! Vui long bien dich lai.
    exit /b 1
)
