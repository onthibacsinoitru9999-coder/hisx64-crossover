@echo off
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"

if exist "%ROOT_DIR%HisCabinetPrescribe.exe" (
    "%ROOT_DIR%HisCabinetPrescribe.exe" %*
    exit /b %ERRORLEVEL%
) else if exist "%ROOT_DIR%.agents\skills\his-clinical-operations\scripts\HisCabinetPrescribe.exe" (
    "%ROOT_DIR%.agents\skills\his-clinical-operations\scripts\HisCabinetPrescribe.exe" %*
    exit /b %ERRORLEVEL%
) else (
    echo [LOI] Khong tim thay HisCabinetPrescribe.exe!
    exit /b 1
)
