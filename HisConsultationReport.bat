@echo off
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0

echo ===============================================================================
echo [HIS-KHOA 57] BAO CAO HOI CHAN LIEN KHOA VA DONG BO GOOGLE DRIVE
echo ===============================================================================

set REPORT_HTML=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.html
set REPORT_MD=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.md

if not exist "%REPORT_HTML%" (
    echo [ERROR] Chua co file bao cao: %REPORT_HTML%
    exit /b 1
)

echo [OK] Bao cao HTML: %REPORT_HTML%
echo [OK] Bao cao MD  : %REPORT_MD%

if exist "%SCRIPT_DIR%rclone.exe" (
    "%SCRIPT_DIR%rclone.exe" listremotes 2>nul | findstr /i "gdrive:" >nul
    if !errorlevel! equ 0 (
        echo [SYNC] Dang dong bo len Google Drive...
        "%SCRIPT_DIR%rclone.exe" copy "%SCRIPT_DIR%Reports\ConsultationReports" "gdrive:BaoCaoHoiChan_Khoa57" --quiet
        echo [SYNC] Dong bo Google Drive thanh cong!
    ) else (
        echo [INFO] Chua cau hinh remote 'gdrive'. Bao cao duoc luu tai thu muc Reports\ConsultationReports.
    )
)

if "%1"=="--open" (
    start "" "%REPORT_HTML%"
) else if "%1"=="" (
    start "" "%REPORT_HTML%"
)

echo ===============================================================================
echo Hoan tat!
