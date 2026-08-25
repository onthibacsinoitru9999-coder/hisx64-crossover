@echo off
chcp 65001 >nul
title KIEM TRA KET NOI HIS QUA TAILSCALE
echo Dang kiem tra ket noi toi may chu HIS tai vien...
powershell.exe -ExecutionPolicy Bypass -File "%~dp0check_his_tunnel.ps1"
echo.
pause
