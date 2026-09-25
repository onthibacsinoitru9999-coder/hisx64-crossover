@echo off
chcp 65001 >nul
cd /d "%~dp0"

title HIS LIVE WATCHER v2.0 - Ban phim + Chuot + API + Thoi gian cho

echo ================================================================================
echo   HIS LIVE BRANCH WATCHER ^& LEARNER v2.0
echo   - Theo doi LogSystem.txt (API, Token, WorkInfo, Loi)
echo   - Bat ban phim + chuot toan cuc (Low-Level Win32 Hook)
echo   - Do thoi gian cho giua moi thao tac
echo   - Ghi journal CSV: Logs\HisInputJournal.csv
echo ================================================================================
echo.

:: Kiểm tra quyền Admin (hook bàn phím cần quyền cao hơn)
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Chua chay voi quyen Admin. Tu dong nang cap quyen...
    powershell -Command "Start-Process cmd -ArgumentList '/c,cd /d ""%~dp0"" && ""%~f0""' -Verb RunAs"
    exit /b
)

echo [OK] Dang chay voi quyen Admin.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WatchBranchLearning.ps1"
pause
