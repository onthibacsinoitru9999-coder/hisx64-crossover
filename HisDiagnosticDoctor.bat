@echo off
setlocal
set SCRIPT_DIR=%~dp0
call "%SCRIPT_DIR%set_env.bat"

if exist "%SCRIPT_DIR%HisDiagnosticDoctor.exe" (
    "%SCRIPT_DIR%HisDiagnosticDoctor.exe" %*
) else (
    echo [ERROR] Khong tim thay HisDiagnosticDoctor.exe! Vui long bien dich lai.
    exit /b 1
)
