@echo off
chcp 65001 >nul
echo ==============================================================================
echo [HIS ROLLBACK] KHOI PHUC TRANG THAI AN TOAN TU TAG 'stable-release'
echo ==============================================================================

git status --porcelain >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Thu muc khong phai la git repository!
    pause
    exit /b 1
)

echo Dang khoi phuc cac file nhi phan va ma nguon cot loi ve 'stable-release'...
git checkout stable-release -- HisCabinetPrescribe.cs HisGlucoseMcpServer.cs .agents/skills/his-clinical-operations/scripts/HisGlucoseBedsideAssigner.cs
git checkout stable-release -- HisCabinetPrescribe.exe HisGlucoseBedsideAssigner.exe HisGlucoseMcpServer.exe
git checkout stable-release -- .agents/skills/his-clinical-operations/scripts/HisCabinetPrescribe.exe
git checkout stable-release -- .agents/skills/his-clinical-operations/scripts/HisGlucoseBedsideAssigner.exe
git checkout stable-release -- .agents/skills/his-clinical-operations/scripts/HisGlucoseMcpServer.exe
git checkout stable-release -- *.config .agents/skills/his-clinical-operations/scripts/*.config

echo.
echo ==============================================================================
echo [THANH CONG] Da phuc hoi 100%% cac cong cu ve phien ban stable-release duoc chung nhan!
echo ==============================================================================
pause
