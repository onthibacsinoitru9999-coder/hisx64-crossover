@echo off
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

:: Thu thap reference DLLs
set REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Data.dll /reference:System.Xml.dll /reference:System.Net.Http.dll

:: Them DLLs tu ReferencedAssemblies
for %%f in ("%SCRIPTS_DIR%\ReferencedAssemblies\*.dll") do (
    set REFS=!REFS! /reference:"%%f"
)

:: Fallback: lay tu chinh thu muc scripts
for %%f in ("%SCRIPTS_DIR%\Inventec.*.dll" "%SCRIPTS_DIR%\HIS.*.dll" "%SCRIPTS_DIR%\MOS.*.dll") do (
    set REFS=!REFS! /reference:"%%f"
)

"%CSC%" /target:winexe /platform:x64 /out:%OUT% %REFS% %SRC%

if %errorlevel% equ 0 (
    echo.
    echo [OK] Bien dich thanh cong: HisAutoPrescribe.exe
) else (
    echo.
    echo [LOI] Bien dich that bai! Kiem tra lai code.
)

pause
