@echo off
setlocal
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY (
    if exist "%SCRIPT_DIR%set_env.bat" call "%SCRIPT_DIR%set_env.bat"
)

if exist "%SCRIPT_DIR%HisThoDuongHuyet.exe" (
    "%SCRIPT_DIR%HisThoDuongHuyet.exe" %*
    exit /b %ERRORLEVEL%
) else if exist "%SCRIPT_DIR%.agents\skills\his-clinical-operations\scripts\HisThoDuongHuyet.exe" (
    "%SCRIPT_DIR%.agents\skills\his-clinical-operations\scripts\HisThoDuongHuyet.exe" %*
    exit /b %ERRORLEVEL%
) else (
    echo [ERROR] Khong tim thay HisThoDuongHuyet.exe! Vui long bien dich lai.
    exit /b 1
)
