@echo off
chcp 65001 >nul
:: Chuyển thư mục làm việc về chính xác vị trí file .bat này
cd /d "%~dp0"

:: Nạp môi trường tự động (Git, Python, C++, C#)
if not defined HIS_ENV_READY call "%~dp0set_env.bat"

echo ===================================================
echo [HIS AI AGENT] DANG DAY CAP NHAT LEN GIT (PUSH)...
echo Thu muc hien tai: %CD%
echo ===================================================

:: Kiểm tra nếu chưa khởi tạo git repo thì tự động khởi tạo
if not exist ".git" (
    echo [THONG BAO] Thu muc chua co Git. Dang khoi tao tu dong...
    git init -b main
    git config user.name "onthibacsinoitru9999-coder"
    git config user.email "onthibacsinoitru9999@gmail.com"
    git remote add origin https://github.com/onthibacsinoitru9999-coder/hisx64-crossover.git
)

git add .gitignore AGENTS.md HIS_AI_INTEGRATION_PLAYBOOK.md HIS_INTEGRATION_AGENT_LOG.md .agents/ *.cs *.bat *.ps1
set /p msg="Nhap ghi chu cap nhat (Enter de lay mac dinh): "
if "%msg%"=="" set msg=update: dong bo playbook va tools moi nhat

git commit -m "%msg%"
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===================================================
    echo [THANH CONG] Da day thanh cong len Git Remote!
    echo Moi khung chat va may khac chi can chay sync_pull.bat de lay ve.
    echo ===================================================
) else (
    echo.
    echo ===================================================
    echo [CANH BAO] Co loi khi push!
    echo Hay kiem tra:
    echo 1. Ket noi mang.
    echo 2. Dang nhap tai khoan GitHub (hoac Token).
    echo ===================================================
)
pause
