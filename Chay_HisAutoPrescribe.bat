@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not defined HIS_ENV_READY call "%~dp0set_env.bat"

title Ứng Dụng Kê Đơn Thuốc & Y Lệnh Tự Động (HisAutoPrescribe) - Khoa 57
echo ==============================================================================
echo  HỆ THỐNG KÊ ĐƠN THUỐC & Y LỆNH TỰ ĐỘNG (HIS / MOS)
echo  KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI
echo ==============================================================================
echo.
echo Đang khởi động ứng dụng...

if exist "%~dp0HisAutoPrescribe.exe" (
    start "" /d "%~dp0" "%~dp0HisAutoPrescribe.exe" %*
) else (
    echo [LỖI] Không tìm thấy file HisAutoPrescribe.exe! Vui lòng biên dịch lại.
    pause
    exit /b 1
)
exit /b 0
