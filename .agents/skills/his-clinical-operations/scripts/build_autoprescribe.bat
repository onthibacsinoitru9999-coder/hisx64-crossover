@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
echo ===============================================================================
echo   Bien dich lai HisAutoPrescribe.exe (Dynamic Discovery x64)
echo ===============================================================================

set SCRIPTS_DIR=%~dp0
set SCRIPTS_DIR=%SCRIPTS_DIR:~0,-1%
set ROOT_DIR=%SCRIPTS_DIR%\..\..\..\..
for %%i in ("%ROOT_DIR%") do set ROOT_DIR=%%~fi

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC="%SCRIPTS_DIR%\HisAutoPrescribe.cs"
set OUT="%SCRIPTS_DIR%\HisAutoPrescribe.exe"

if not exist "%CSC%" (
    echo [LOI] Khong tim thay csc.exe tai: %CSC%
    exit /b 1
)

echo Dang bien dich tu %ROOT_DIR%...

echo /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Data.dll /reference:System.Xml.dll /reference:System.Net.Http.dll > "%SCRIPTS_DIR%\refs.rsp"

for %%f in ("%ROOT_DIR%\ReferencedAssemblies\Inventec.*.dll" "%ROOT_DIR%\ReferencedAssemblies\HIS.*.dll" "%ROOT_DIR%\ReferencedAssemblies\MOS.*.dll") do (
    echo /reference:"%%f" >> "%SCRIPTS_DIR%\refs.rsp"
)

"%CSC%" /target:exe /platform:x64 /out:%OUT% @"%SCRIPTS_DIR%\refs.rsp" %SRC%

if %errorlevel% equ 0 (
    echo.
    echo [OK] Bien dich thanh cong: %OUT%
    copy /y %OUT% "%ROOT_DIR%\HisAutoPrescribe.exe" > nul
    echo [OK] Da dong bo vao thu muc goc: %ROOT_DIR%\HisAutoPrescribe.exe
) else (
    echo.
    echo [LOI] Bien dich that bai! Kiem tra lai code.
    exit /b 1
)
