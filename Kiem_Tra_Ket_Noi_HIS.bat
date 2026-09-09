@echo off
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY call "%SCRIPT_DIR%set_env.bat"

title KIEM TRA KET NOI HIS QUA TAILSCALE
echo Dang kiem tra ket noi toi may chu HIS tai vien...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%check_his_tunnel.ps1"
echo.
pause
