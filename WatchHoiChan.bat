@echo off
chcp 65001 >nul
cd /d "%~dp0"
title HIS WATCHER - Theo Doi Hoi Chan ^& Moi Ky PT01

echo ================================================================================
echo   THEO DOI THAO TAC HOI CHAN ^& MOI KY PT-01 (LIVE MONITOR)
echo   - Lang nghe su kien tu Logs\LogSystem.txt
echo   - Bat tron Payload API Hoi chan, Moi ky EMR, ServiceReq
echo   - Ghi log truc tiep ra file: Logs\CapturedHoiChan_Live.txt
echo ================================================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WatchHoiChan.ps1"
pause
