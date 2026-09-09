@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
echo ===============================================================================
echo   Bien dich FetchPatient.exe (Dynamic Discovery x64)
echo ===============================================================================

set "SCRIPTS_DIR=%~dp0"
if "%SCRIPTS_DIR:~-1%"=="\" set "SCRIPTS_DIR=%SCRIPTS_DIR:~0,-1%"
cd /d "%SCRIPTS_DIR%"
for %%i in ("%SCRIPTS_DIR%\..\..\..\..") do set "HIS_ROOT=%%~fi"
set "REF_DIR=%HIS_ROOT%\ReferencedAssemblies"

if exist "%HIS_ROOT%\set_env.bat" (
    if not defined HIS_ENV_READY call "%HIS_ROOT%\set_env.bat"
)

if not defined CSC (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    ) else (
        where csc >nul 2>nul && for /f "delims=" %%c in ('where csc 2^>nul') do if not defined CSC set "CSC=%%c"
    )
)

if not exist "%CSC%" (
    echo [LOI] Khong tim thay csc.exe! Vui long cai dat .NET Framework 4.5+ hoac kiem tra PATH.
    exit /b 1
)

set "SRC=%SCRIPTS_DIR%\FetchPatient.cs"
set "OUT=%HIS_ROOT%\FetchPatient.exe"

echo Dang bien dich tu %HIS_ROOT%...

echo /reference:System.dll /reference:System.Core.dll /reference:System.Data.dll /reference:System.Xml.dll /reference:System.Net.Http.dll > "%SCRIPTS_DIR%\refs_fetch.rsp"

for %%f in ("%REF_DIR%\Inventec.*.dll" "%REF_DIR%\HIS.*.dll" "%REF_DIR%\MOS.*.dll" "%REF_DIR%\Newtonsoft.Json.dll" "%REF_DIR%\*.EFMODEL.dll") do (
    if exist "%%~f" echo /reference:"%%~f" >> "%SCRIPTS_DIR%\refs_fetch.rsp"
)
for %%f in ("%HIS_ROOT%\Inventec.*.dll" "%HIS_ROOT%\HIS.*.dll" "%HIS_ROOT%\MOS.*.dll" "%HIS_ROOT%\Newtonsoft.Json.dll" "%HIS_ROOT%\*.EFMODEL.dll") do (
    if exist "%%~f" echo /reference:"%%~f" >> "%SCRIPTS_DIR%\refs_fetch.rsp"
)

"%CSC%" /target:exe /platform:x64 /out:"%OUT%" @"%SCRIPTS_DIR%\refs_fetch.rsp" "%SRC%"

if %errorlevel% equ 0 (
    echo.
    echo [OK] Bien dich thanh cong: %OUT%
) else (
    echo.
    echo [LOI] Bien dich that bai! Kiem tra lai code.
    exit /b 1
)
