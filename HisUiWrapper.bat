@echo off
setlocal
cd /d "%~dp0"

echo ===============================================================================
echo  KHOI DONG BO GHI ^& HOC THAO TAC GIAO DIEN HIS (HIS UI RECORDER)
echo ===============================================================================

if not exist "HisUiWrapper.exe" (
    echo [*] Dang bien dich HisUiWrapper.exe...
    call "Tools\HisUiWrapper\build_wrapper.bat"
)

start "" "HisUiWrapper.exe" %*
