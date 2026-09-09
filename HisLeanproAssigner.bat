@echo off
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"
if not defined HIS_ENV_READY call "%ROOT_DIR%set_env.bat"

if "%~1"=="" (
    echo ===============================================================================
    echo  🥛 HIS LEANPRO PRESUR 12.5%% ASSIGNER [CHUẨN ERAS TRƯỚC MỔ]
    echo ===============================================================================
    echo  Cú pháp sử dụng:
    echo     HisLeanproAssigner.bat ^<MaBN_hoac_MaDT^>
    echo     HisLeanproAssigner.bat -p ^<MaBN1,MaBN2,...^>
    echo.
    echo  Ví dụ:
    echo     HisLeanproAssigner.bat 0003976907
    echo     HisLeanproAssigner.bat -p 0003976907,0003595506
    echo ===============================================================================
    exit /b 0
)

if exist "%ROOT_DIR%HisLeanproAssigner.exe" (
    "%ROOT_DIR%HisLeanproAssigner.exe" %*
    exit /b %ERRORLEVEL%
) else (
    echo [LỖI] Không tìm thấy HisLeanproAssigner.exe! Vui lòng biên dịch lại.
    exit /b 1
)
