@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"

if "%~1"=="" (
    echo ===============================================================================
    echo  [HIS EMERGENCY SURGERY REGISTRATION - MO CAP CUU HA NOI ^& NINH BINH]
    echo ===============================================================================
    echo  Cu phap:
    echo     HisEmergencySurgery.bat ^<MaBN^> "^<CachThucMo^>" [HN^|NB] [--submit] [--dry-run]
    echo.
    echo  Vi du:
    echo     HisEmergencySurgery.bat 0004060486 "Phau thuat KHX kim Kirschner ngon 5 tay phai" HN --dry-run
    echo     HisEmergencySurgery.bat 0004060486 "Phau thuat KHX ngon 5 tay phai" NB --submit
    echo ===============================================================================
    exit /b 0
)

set "PYTHON_EXE=python"
if exist "C:\Users\HP\AppData\Local\Programs\Python\Python312\python.exe" (
    set "PYTHON_EXE=C:\Users\HP\AppData\Local\Programs\Python\Python312\python.exe"
)

"%PYTHON_EXE%" "%ROOT_DIR%.agents\skills\his-emergency-surgery\scripts\his_emergency_surgery.py" %*
exit /b %ERRORLEVEL%
