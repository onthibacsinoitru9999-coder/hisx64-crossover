@echo off
chcp 65001 > nul
set ROOT_DIR=%~dp0
cd /d "%ROOT_DIR%"

if "%~1"=="" (
    echo ===============================================================================
    echo  ?? HIS LEANPRO PRESUR 12.5% ASSIGNER (CHUAN ERAS TRUOC MO)
    echo ===============================================================================
    echo  Cu phap su dung:
    echo     HisLeanproAssigner.bat ^<MaBN_hoac_MaDT^>
    echo     HisLeanproAssigner.bat -p ^<MaBN1,MaBN2,...^>
    echo.
    echo  Vi du:
    echo     HisLeanproAssigner.bat 0003976907
    echo     HisLeanproAssigner.bat -p 0003976907,0003595506
    echo ===============================================================================
    exit /b 0
)

"%ROOT_DIR%HisLeanproAssigner.exe" %*
