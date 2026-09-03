@echo off
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0

echo ===============================================================================
echo [HIS-KHOA 57] BAO CAO HOI CHAN LIEN KHOA VA DONG BO GOOGLE DRIVE
echo ===============================================================================

set REPORT_HTML=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.html
set REPORT_MD=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.md
set REPORT_XLSX=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_CoXuongKhop_20260903.xlsx
set REPORT_CSV=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_CoXuongKhop_20260903.csv

if exist "%REPORT_HTML%" echo [OK] Bao cao HTML: %REPORT_HTML%
if exist "%REPORT_MD%"   echo [OK] Bao cao MD  : %REPORT_MD%
if exist "%REPORT_XLSX%" echo [OK] Bao cao Excel: %REPORT_XLSX%
if exist "%REPORT_CSV%"  echo [OK] Bao cao CSV  : %REPORT_CSV%

if exist "%SCRIPT_DIR%rclone.exe" (
    "%SCRIPT_DIR%rclone.exe" listremotes 2>nul | findstr /i "gdrive:" >nul
    if !errorlevel! equ 0 (
        echo [SYNC] Dang dong bo len thu muc 'HC BM' tren Google Drive...
        "%SCRIPT_DIR%rclone.exe" mkdir "gdrive:HC BM" 2>nul
        "%SCRIPT_DIR%rclone.exe" copy "%REPORT_CSV%" "gdrive:HC BM" --drive-import-formats csv --drive-allow-import-name-change --quiet
        "%SCRIPT_DIR%rclone.exe" copy "%REPORT_XLSX%" "gdrive:HC BM" --quiet
        "%SCRIPT_DIR%rclone.exe" copy "%SCRIPT_DIR%Reports\ConsultationReports" "gdrive:HC BM" --quiet
        echo [SYNC] Dong bo Google Drive thanh cong vao thu muc 'HC BM'!
    ) else (
        echo [INFO] Chua cau hinh remote 'gdrive'. File da duoc luu tai thu muc Reports\ConsultationReports.
    )
)

if "%1"=="--excel" (
    if exist "%REPORT_XLSX%" start "" "%REPORT_XLSX%"
) else if "%1"=="--sheet" (
    if exist "%REPORT_XLSX%" start "" "%REPORT_XLSX%"
) else if "%1"=="--open" (
    if exist "%REPORT_HTML%" start "" "%REPORT_HTML%"
) else if "%1"=="" (
    if exist "%REPORT_XLSX%" (
        start "" "%REPORT_XLSX%"
    ) else if exist "%REPORT_HTML%" (
        start "" "%REPORT_HTML%"
    )
)

echo ===============================================================================
echo Hoan tat!
