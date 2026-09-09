@echo off
setlocal
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY call "%SCRIPT_DIR%set_env.bat"

where python >nul 2>nul
if %errorlevel% equ 0 (
    python "%SCRIPT_DIR%HisAiCli.py" %*
    exit /b %errorlevel%
)

where uv >nul 2>nul
if %errorlevel% equ 0 (
    uv run python "%SCRIPT_DIR%HisAiCli.py" %*
    exit /b %errorlevel%
)

if exist "%USERPROFILE%\.local\bin\uv.exe" (
    "%USERPROFILE%\.local\bin\uv.exe" run python "%SCRIPT_DIR%HisAiCli.py" %*
    exit /b %errorlevel%
)

echo [ERROR] Khong tim thay Python hoac UV tren he thong!
exit /b 1
