@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
if not defined HIS_ENV_READY call "%SCRIPT_DIR%set_env.bat"

echo ===============================================================================
echo [HIS-KHOA 57] BAO CAO HOI CHAN LIEN KHOA VA DONG BO GOOGLE DRIVE
echo ===============================================================================

set "REPORT_HTML=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.html"
set "REPORT_MD=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.md"
set "REPORT_XLSX="
set "REPORT_CSV="

for /f "delims=" %%f in ('dir /b /o-d "%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_*.xlsx" 2^>nul') do (
    if not defined REPORT_XLSX set "REPORT_XLSX=%SCRIPT_DIR%Reports\ConsultationReports\%%f"
)
for /f "delims=" %%f in ('dir /b /o-d "%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_*.csv" 2^>nul') do (
    if not defined REPORT_CSV set "REPORT_CSV=%SCRIPT_DIR%Reports\ConsultationReports\%%f"
)

if not defined REPORT_XLSX set "REPORT_XLSX=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.xlsx"
if not defined REPORT_CSV  set "REPORT_CSV=%SCRIPT_DIR%Reports\ConsultationReports\BaoCao_HoiChan_LienKhoa_Khoa57.csv"

if exist "%REPORT_HTML%" echo [OK] Bao cao HTML: %REPORT_HTML%
if exist "%REPORT_MD%"   echo [OK] Bao cao MD  : %REPORT_MD%
if exist "%REPORT_XLSX%" echo [OK] Bao cao Excel: %REPORT_XLSX%
if exist "%REPORT_CSV%"  echo [OK] Bao cao CSV  : %REPORT_CSV%

set "RCLONE_EXE="
if exist "%SCRIPT_DIR%rclone.exe" (
    set "RCLONE_EXE=%SCRIPT_DIR%rclone.exe"
) else (
    where rclone >nul 2>nul
    if !errorlevel! equ 0 set "RCLONE_EXE=rclone"
)

if defined RCLONE_EXE (
    "%RCLONE_EXE%" listremotes 2>nul | findstr /i "gdrive:" >nul
    if !errorlevel! equ 0 (
        echo [SYNC] Dang dong bo len thu muc 'HC BM' tren Google Drive...
        "%RCLONE_EXE%" mkdir "gdrive:HC BM" 2>nul
        if exist "%REPORT_CSV%" "%RCLONE_EXE%" copy "%REPORT_CSV%" "gdrive:HC BM" --drive-import-formats csv --drive-allow-import-name-change --quiet
        if exist "%REPORT_XLSX%" "%RCLONE_EXE%" copy "%REPORT_XLSX%" "gdrive:HC BM" --quiet
        if exist "%SCRIPT_DIR%Reports\ConsultationReports" "%RCLONE_EXE%" copy "%SCRIPT_DIR%Reports\ConsultationReports" "gdrive:HC BM" --quiet
        echo [SYNC] Dong bo Google Drive thanh cong vao thu muc 'HC BM'!
    ) else (
        echo [INFO] Chua cau hinh remote 'gdrive'. File da duoc luu tai thu muc Reports\ConsultationReports.
    )
) else (
    echo [INFO] rclone chua duoc cai dat tren he thong. Bo qua dong bo Google Drive.
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
