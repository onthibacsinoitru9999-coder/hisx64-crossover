@echo off
chcp 65001 >nul
:: Chuyển thư mục làm việc về chính xác vị trí file .bat này
cd /d "%~dp0"

:: Đảm bảo Git có trong PATH
set "PATH=C:\Program Files\Git\cmd;%PATH%"

echo ===================================================
echo [HIS AI AGENT] DANG DONG BO DU LIEU TU GIT (PULL)...
echo Thu muc hien tai: %CD%
echo ===================================================

:: Kiểm tra nếu chưa khởi tạo git repo thì tự động khởi tạo
if not exist ".git" (
    echo [THONG BAO] Thu muc chua co Git. Dang khoi tao va ket noi tu dong...
    git init -b main
    git config user.name "onthibacsinoitru9999-coder"
    git config user.email "onthibacsinoitru9999@gmail.com"
    git remote add origin https://github.com/onthibacsinoitru9999-coder/hisx64-crossover.git
)

git pull origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===================================================
    echo [THANH CONG] Da dong bo toan bo Playbook va Skills moi nhat!
    echo ===================================================
) else (
    echo.
    echo ===================================================
    echo [CANH BAO] Co loi khi pull du lieu hoac chua co ket noi mang!
    echo ===================================================
)
pause
