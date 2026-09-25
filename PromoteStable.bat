@echo off
chcp 65001 >nul
echo ==============================================================================
echo [HIS PROMOTE] KIEM TRA & DONG DANG KY BAN ON DINH (STABLE RELEASE)
echo ==============================================================================

echo 1. Chay Smoke Test kiem tra ket noi Tu truc 810...
HisCabinetPrescribe.exe stock 810 >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Smoke Test that bai! Khong the dong bang ban hien tai vi tool chua on dinh.
    pause
    exit /b 1
)
echo [PASS] Smoke Test thanh cong!

echo 2. Tao ban sao luu .stable.exe cho cac cong cu cot loi...
copy /Y HisCabinetPrescribe.exe HisCabinetPrescribe.stable.exe >nul
copy /Y HisGlucoseBedsideAssigner.exe HisGlucoseBedsideAssigner.stable.exe >nul
if exist HisTrackingCreator.exe copy /Y HisTrackingCreator.exe HisTrackingCreator.stable.exe >nul

copy /Y HisCabinetPrescribe.exe .agents\skills\his-clinical-operations\scripts\HisCabinetPrescribe.exe >nul
copy /Y HisCabinetPrescribe.exe .agents\skills\his-clinical-operations\scripts\HisCabinetPrescribe.stable.exe >nul

copy /Y HisGlucoseBedsideAssigner.exe .agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.exe >nul
copy /Y HisGlucoseBedsideAssigner.exe .agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.stable.exe >nul

if exist HisTrackingCreator.exe (
    copy /Y HisTrackingCreator.exe .agents\skills\his-clinical-operations\scripts\HisTrackingCreator.exe >nul
    copy /Y HisTrackingCreator.exe .agents\skills\his-clinical-operations\scripts\HisTrackingCreator.stable.exe >nul
)

echo 3. Gan Git Tag 'stable-release'...
git tag -f stable-release
git push origin stable-release -f

echo.
echo ==============================================================================
echo [HOAN TAT] Da thang hang va khoa ban STABLE thanh cong tren ca may tram va Git!
echo ==============================================================================
pause
