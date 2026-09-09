@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not defined HIS_ENV_READY call "%~dp0set_env.bat"

title Ứng Dụng Tạo Tờ Điều Trị & Chế Độ Chăm Sóc Bệnh Nhân (HIS / MOS) - Bệnh Viện Bạch Mai
echo ==============================================================================
echo  HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHẾ ĐỘ CHĂM SÓC BỆNH NHÂN NỘI TRÚ
echo  KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI
echo ==============================================================================
echo.
echo Đang khởi động giao diện điều khiển...

if exist "%~dp0HisTrackingCreator.exe" (
    start "" /d "%~dp0" "%~dp0HisTrackingCreator.exe"
) else if exist "%~dp0.agents\skills\his-clinical-operations\scripts\HisTrackingCreator.exe" (
    start "" /d "%~dp0.agents\skills\his-clinical-operations\scripts" "%~dp0.agents\skills\his-clinical-operations\scripts\HisTrackingCreator.exe"
) else (
    echo [LỖI] Không tìm thấy file HisTrackingCreator.exe!
    pause
    exit /b 1
)
exit /b 0
