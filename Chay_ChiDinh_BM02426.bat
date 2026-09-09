@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not defined HIS_ENV_READY call "%~dp0set_env.bat"

title Ứng Dụng Chỉ Định Đường Máu Mao Mạch Tại Giường (BM02426) - Bệnh Viện Bạch Mai
echo ==============================================================================
echo  HỆ THỐNG TỰ ĐỘNG CHỈ ĐỊNH ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG (BM02426)
echo  KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI
echo ==============================================================================
echo.
echo Đang khởi động giao diện điều khiển...

if exist "%~dp0HisGlucoseBedsideAssigner.exe" (
    start "" /d "%~dp0" "%~dp0HisGlucoseBedsideAssigner.exe"
) else if exist "%~dp0.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe" (
    start "" /d "%~dp0.agents\skills\his-clinical-operations\scripts" "%~dp0.agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe"
) else (
    echo [LỖI] Không tìm thấy file HisGlucoseBedsideAssigner.exe!
    pause
)
exit /b 0
