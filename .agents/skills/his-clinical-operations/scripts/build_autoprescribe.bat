@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
echo ===============================================================================
echo   Bien dich lai HisAutoPrescribe.exe (Them Batch Mode)
echo ===============================================================================

set SCRIPTS_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\.agents\skills\his-clinical-operations\scripts
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC="%SCRIPTS_DIR%\HisAutoPrescribe.cs"
set OUT="%SCRIPTS_DIR%\HisAutoPrescribe.exe"

if not exist "%CSC%" (
    echo [LOI] Khong tim thay csc.exe tai: %CSC%
    pause
    exit /b 1
)

echo Dang bien dich...

echo /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Data.dll /reference:System.Xml.dll /reference:System.Net.Http.dll > "%SCRIPTS_DIR%\refs.rsp"

set ROOT_DIR=D:\his\his-x64-28-11fix GDYK\his-x64
for %%f in ("%ROOT_DIR%\ReferencedAssemblies\Inventec.*.dll" "%ROOT_DIR%\ReferencedAssemblies\HIS.*.dll" "%ROOT_DIR%\ReferencedAssemblies\MOS.*.dll") do (
    echo /reference:"%%f" >> "%SCRIPTS_DIR%\refs.rsp"
)

"%CSC%" /target:winexe /platform:x64 /out:%OUT% @"%SCRIPTS_DIR%\refs.rsp" %SRC%

if %errorlevel% equ 0 (
    echo.
    echo [OK] Bien dich thanh cong: HisAutoPrescribe.exe
) else (
    echo.
    echo [LOI] Bien dich that bai! Kiem tra lai code.
)

pause
