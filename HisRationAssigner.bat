@echo off
setlocal
set SCRIPT_DIR=%~dp0

if exist "%SCRIPT_DIR%HisRationAssigner.exe" (
    "%SCRIPT_DIR%HisRationAssigner.exe" %*
) else (
    echo [ERROR] Khong tim thay HisRationAssigner.exe! Vui long bien dich lai.
    exit /b 1
)
