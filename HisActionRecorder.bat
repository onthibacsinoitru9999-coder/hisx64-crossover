@echo off
chcp 65001 >nul
cd /d "%~dp0"
title HIS Action Recorder v3.0

:: Kiểm tra quyền Admin (cần thiết để hook chuột/phím xuyên suốt các cửa sổ HIS)
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process cmd -ArgumentList '/c,cd /d ""%~dp0"" && start HisActionRecorder.exe' -Verb RunAs"
    exit /b
)

start HisActionRecorder.exe
exit
