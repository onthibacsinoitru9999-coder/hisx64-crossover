@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
echo ===============================================================================
echo   🏥 KHỞI TẠO & KÍCH HOẠT HỆ THỐNG SUPPORT AUTOMATION TRÊN HIS MỚI
echo ===============================================================================

set SCRIPT_DIR=%~dp0
cd /d %SCRIPT_DIR%

echo [1/3] Kiểm tra môi trường HIS...
if exist ReferencedAssemblies\Inventec.Core.dll (
    echo   ✔ Đã tìm thấy thư mục ReferencedAssemblies chuẩn.
) else if exist Inventec.Core.dll (
    echo   ✔ Đã tìm thấy các thư viện lõi Inventec tại thư mục gốc.
) else (
    echo   ⚠ CẢNH BÁO: Không tìm thấy Inventec.Core.dll! Hãy đảm bảo bạn đã giải nén
    echo               gói này vào đúng thư mục gốc chứa phần mềm HIS client.
)

echo.
echo [2/3] Kiểm tra kết nối máy chủ và phiên đăng nhập (Health Check)...
call %SCRIPT_DIR%HisDiagnosticDoctor.bat health

echo.
echo [3/3] Thử nghiệm tra cứu nhanh thông tin bệnh nhân...
if exist %SCRIPT_DIR%HisClinicalCli.exe (
    %SCRIPT_DIR%HisClinicalCli.exe lookup 0003985947
)

echo.
echo ===============================================================================
echo   🎉 HOÀN TẤT CÀI ĐẶT! HỆ THỐNG SUPPORT ĐÃ SẴN SÀNG SỬ DỤNG.
echo ===============================================================================
pause
