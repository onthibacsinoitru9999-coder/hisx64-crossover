@echo off
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"

if exist "%ROOT_DIR%HisWarehousePrescribe.exe" (
    "%ROOT_DIR%HisWarehousePrescribe.exe" %*
    exit /b %ERRORLEVEL%
) else if exist "%ROOT_DIR%.agents\skills\his-clinical-operations\scripts\HisWarehousePrescribe.exe" (
    "%ROOT_DIR%.agents\skills\his-clinical-operations\scripts\HisWarehousePrescribe.exe" %*
    exit /b %ERRORLEVEL%
) else (
    echo [LOI] Khong tim thay HisWarehousePrescribe.exe!
    exit /b 1
)
