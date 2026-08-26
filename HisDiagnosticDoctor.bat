@echo off
setlocal
set SCRIPT_DIR=%~dp0

if exist "%SCRIPT_DIR%HisDiagnosticDoctor.exe" (
    "%SCRIPT_DIR%HisDiagnosticDoctor.exe" %*
) else (
    echo [ERROR] Khong tim thay HisDiagnosticDoctor.exe! Vui long bien dich lai.
    exit /b 1
)
