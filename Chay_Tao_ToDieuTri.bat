@echo off
chcp 65001 >nul
title Ứng Dụng Tạo Tờ Điều Trị & Chế Độ Chăm Sóc Bệnh Nhân (HIS / MOS) - Bệnh Viện Bạch Mai
echo ==============================================================================
echo  HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHẾ ĐỘ CHĂM SÓC BỆNH NHÂN NỘI TRÚ
echo  KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI
echo ==============================================================================
echo.
echo Đang khởi động giao diện điều khiển...

if exist "%~dp0HisTrackingCreator.exe" (
    start "" "%~dp0HisTrackingCreator.exe"
) else if exist "%~dp0.agents\skills\his-clinical-operations\scripts\HisTrackingCreator.exe" (
    start "" "%~dp0.agents\skills\his-clinical-operations\scripts\HisTrackingCreator.exe"
) else (
    echo [LỖI] Không tìm thấy file HisTrackingCreator.exe!
    pause
)
exit /b 0
