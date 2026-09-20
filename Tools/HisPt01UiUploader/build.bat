@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo  BIEN DICH HIS PT-01 UI AUTOMATION RUNNER (HisPt01UiUploader.exe)
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

set ZIP_FS=C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.IO.Compression.FileSystem\v4.0_4.0.0.0__b77a5c561934e089\System.IO.Compression.FileSystem.dll
set ZIP_CORE=C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.IO.Compression\v4.0_4.0.0.0__b77a5c561934e089\System.IO.Compression.dll

set SCRIPT_DIR=%~dp0
set ROOT_DIR=%SCRIPT_DIR%..\..
set OUT_EXE=%ROOT_DIR%\HisPt01UiUploader.exe

echo [+] CSC Compiler: %CSC%
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
    /r:"%ZIP_FS%" ^
    /r:"%ZIP_CORE%" ^
    /out:"%OUT_EXE%" ^
    "%SCRIPT_DIR%HisUiDriver.cs" ^
    "%SCRIPT_DIR%WordCleaner.cs" ^
    "%SCRIPT_DIR%HisPt01UiUploader.cs"

if %ERRORLEVEL% equ 0 (
    echo ===============================================================================
    echo [THANH CONG] Da tao thanh cong HisPt01UiUploader.exe tai thu muc goc!
    echo ===============================================================================
) else (
    echo [LOI] Bien dich that bai voi ma loi %ERRORLEVEL%!
    exit /b %ERRORLEVEL%
)
