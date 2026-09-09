@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
echo ===============================================================================
echo   🏥 KHỞI TẠO & KÍCH HOẠT HỆ THỐNG SUPPORT AUTOMATION TRÊN HIS MỚI
echo ===============================================================================

set SCRIPT_DIR=%~dp0
cd /d %SCRIPT_DIR%
call "%SCRIPT_DIR%set_env.bat"

echo [1/5] Kiểm tra môi trường HIS...
if exist "ReferencedAssemblies\Inventec.Core.dll" (
    echo   ✔ Đã tìm thấy thư mục ReferencedAssemblies chuẩn.
) else if exist "Inventec.Core.dll" (
    echo   ✔ Đã tìm thấy các thư viện lõi Inventec tại thư mục gốc.
) else (
    echo   ⚠ CẢNH BÁO: Không tìm thấy Inventec.Core.dll! Hãy đảm bảo bạn đã giải nén
    echo               gói này vào đúng thư mục gốc chứa phần mềm HIS client.
)

echo.
echo [2/5] Kiểm tra và thiết lập Git đồng bộ tri thức cho Agent...
if not exist ".git" (
    where git >nul 2>nul
    if !errorlevel! equ 0 (
        echo   ⚡ Đang khởi tạo và kết nối Git Repository...
        git init >nul 2>nul
        git remote add origin https://github.com/onthibacsinoitru9999-coder/hisx64-crossover.git >nul 2>nul
        git fetch origin main >nul 2>nul
        git branch -M main >nul 2>nul
        git reset origin/main >nul 2>nul
        echo   ✔ Đã kết nối Git origin main thành công! Agent sẽ tự động git pull bình thường.
    ) else (
        echo   ⚠ Git chưa được cài trên PATH. Bác sĩ vẫn dùng các công cụ .exe/.bat bình thường.
    )
) else (
    echo   ✔ Thư mục Git đã tồn tại và sẵn sàng đồng bộ.
)

echo.
echo [3/5] Kiểm tra bộ công cụ phát triển (Python, C++, C#)...
where python >nul 2>nul && (echo   ✔ Python: Đã nhận diện) || (echo   ⚠ Python: Chưa tìm thấy)
where g++ >nul 2>nul && (echo   ✔ C++ Compiler: Đã nhận diện [g++]) || (echo   ⚠ C++: Chưa tìm thấy)
where csc >nul 2>nul && (echo   ✔ C# Compiler: Đã nhận diện [csc]) || (echo   ⚠ C#: Chưa tìm thấy)

echo.
echo [4/5] Kiểm tra kết nối máy chủ và phiên đăng nhập (Health Check)...
call "%SCRIPT_DIR%HisDiagnosticDoctor.bat" health

echo.
echo [5/5] Thử nghiệm tra cứu nhanh thông tin bệnh nhân...
if exist %SCRIPT_DIR%HisClinicalCli.exe (
    %SCRIPT_DIR%HisClinicalCli.exe lookup 0003985947
)

echo.
echo ===============================================================================
echo   🎉 HOÀN TẤT CÀI ĐẶT! HỆ THỐNG SUPPORT ĐÃ SẴN SÀNG SỬ DỤNG.
echo ===============================================================================
pause
