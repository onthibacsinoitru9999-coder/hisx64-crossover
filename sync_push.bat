@echo off
chcp 65001 >nul
echo ===================================================
echo [HIS AI AGENT] DANG DAY CAP NHAT LEN GIT (PUSH)...
echo ===================================================

git add .gitignore AGENTS.md HIS_AI_INTEGRATION_PLAYBOOK.md HIS_INTEGRATION_AGENT_LOG.md .agents/ *.cs *.bat *.ps1
set /p msg="Nhap ghi chu cap nhat (Enter de lay mac dinh): "
if "%msg%"=="" set msg=update: dong bo playbook va tools moi nhat

git commit -m "%msg%"
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo [THANH CONG] Da day thanh cong len Git Remote! Moi khung chat va may khac chi can chay sync_pull.bat de lay ve.
) else (
    echo [CANH BAO] Co loi khi push. Kiem tra lai ket noi hoac token Git.
)
pause
