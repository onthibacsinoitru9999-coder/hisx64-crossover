@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo  BIEN DICH HIS UI INTERACTION WRAPPER ^& RECORDER (HisUiWrapper.exe)
echo ===============================================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC%" (
    echo [LOI] Khong tim thay csc.exe tren he thong .NET Framework!
    exit /b 1
)

set WPF_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF
if not exist "%WPF_DIR%\UIAutomationClient.dll" (
    set WPF_DIR=C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF
)

set SCRIPT_DIR=%~dp0
set ROOT_DIR=%SCRIPT_DIR%..\..
set OUT_EXE=%ROOT_DIR%\HisUiWrapper.exe

echo [+] CSC Compiler: %CSC%
echo [+] WPF Libs:    %WPF_DIR%
echo [+] Output Target: %OUT_EXE%

"%CSC%" /nologo /target:exe /optimize+ /platform:anycpu ^
    /r:System.dll ^
    /r:System.Core.dll ^
    /r:System.Data.dll ^
    /r:System.Drawing.dll ^
    /r:System.Windows.Forms.dll ^
    /r:"%WPF_DIR%\UIAutomationClient.dll" ^
    /r:"%WPF_DIR%\UIAutomationTypes.dll" ^
    /r:"%WPF_DIR%\WindowsBase.dll" ^
    /out:"%OUT_EXE%" ^
    "%SCRIPT_DIR%HisUiaInspector.cs" ^
    "%SCRIPT_DIR%HisUiHookEngine.cs" ^
    "%SCRIPT_DIR%HisUiSessionWriter.cs" ^
    "%SCRIPT_DIR%HisFloatingHud.cs" ^
    "%SCRIPT_DIR%HisUiWrapper.cs"

if %ERRORLEVEL% equ 0 (
    echo ===============================================================================
    echo [THANH CONG] Da tao thanh cong HisUiWrapper.exe tai thu muc goc!
    echo ===============================================================================
) else (
    echo [LOI] Bien dich that bai voi ma loi %ERRORLEVEL%!
    exit /b %ERRORLEVEL%
)
