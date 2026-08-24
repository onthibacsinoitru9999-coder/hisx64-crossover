@echo off
chcp 65001 >nul
echo ===================================================
echo [HIS AI AGENT] DANG DONG BO DU LIEU TU GIT (PULL)...
echo ===================================================

git pull origin main

if %ERRORLEVEL% EQU 0 (
    echo [THANH CONG] Da dong bo toan bo Playbook va Skills moi nhat!
) else (
    echo [CANH BAO] Co loi khi pull du lieu hoac chua cau hinh Remote!
)
pause
