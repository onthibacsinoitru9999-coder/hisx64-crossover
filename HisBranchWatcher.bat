@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not defined HIS_ENV_READY call "%~dp0set_env.bat"

title HIS LIVE BRANCH WATCHER - CS2 NINH BINH
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WatchBranchLearning.ps1"
pause
