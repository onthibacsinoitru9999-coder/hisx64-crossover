@echo off
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"
if not defined HIS_ENV_READY call "%ROOT_DIR%set_env.bat"

rem 1. Check if compilation is needed or explicitly requested
set "NEED_BUILD=0"
if not exist "%ROOT_DIR%HisPacsUploader.exe" set "NEED_BUILD=1"
if /i "%~1"=="build" set "NEED_BUILD=1"
if /i "%~1"=="rebuild" set "NEED_BUILD=1"
if /i "%~1"=="--build" set "NEED_BUILD=1"
if /i "%~1"=="--rebuild" set "NEED_BUILD=1"

if "%NEED_BUILD%"=="0" (
    for /f %%I in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "if ((Get-Item '%ROOT_DIR%HisPacsUploader.cs').LastWriteTime -gt (Get-Item '%ROOT_DIR%HisPacsUploader.exe').LastWriteTime -or ((Test-Path '%ROOT_DIR%ViewerAssets\index.html') -and ((Get-Item '%ROOT_DIR%ViewerAssets\index.html').LastWriteTime -gt (Get-Item '%ROOT_DIR%HisPacsUploader.exe').LastWriteTime))) { Write-Output '1' } else { Write-Output '0' }"') do set "NEED_BUILD=%%I"
)

if "%NEED_BUILD%"=="1" (
    1>&2 echo [BUILD] Dang bien dich HisPacsUploader.exe...
    if not defined CSC set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if not exist "%CSC%" (
        if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
            set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
        )
    )
    "%CSC%" /target:exe /platform:x64 /nologo /utf8output /out:"%ROOT_DIR%HisPacsUploader.exe" ^
      /reference:System.dll ^
      /reference:System.Core.dll ^
      /reference:System.Net.Http.dll ^
      /reference:System.Web.Extensions.dll ^
      /reference:System.IO.Compression.dll ^
      /reference:System.IO.Compression.FileSystem.dll ^
      /reference:"%ROOT_DIR%ReferencedAssemblies\Newtonsoft.Json.dll" ^
      /resource:"%ROOT_DIR%ViewerAssets\index.html",HisPacsUploader.ViewerAssets.index.html ^
      "%ROOT_DIR%HisPacsUploader.cs" 1>&2
    if errorlevel 1 (
        1>&2 echo [ERROR] Bien dich HisPacsUploader.exe that bai!
        exit /b 1
    )
    1>&2 echo [BUILD] Bien dich thanh cong HisPacsUploader.exe!
    if /i "%~1"=="build" exit /b 0
    if /i "%~1"=="rebuild" exit /b 0
    if /i "%~1"=="--build" exit /b 0
    if /i "%~1"=="--rebuild" exit /b 0
)

rem 2. If no argument provided, display usage
if "%~1"=="" (
    (
        echo ===============================================================================
        echo  BACH MAI PACS UPLOADER ^& WEB VIEWER CLI
        echo ===============================================================================
        echo  Cu phap su dung:
        echo     HisPacsUploader.bat ^<MaBN^> [--ttl 24h^|7d] [--open]
        echo.
        echo  Vi du:
        echo     HisPacsUploader.bat 0004009330
        echo     HisPacsUploader.bat 0004009330 --ttl 7d --open
        echo     HisPacsUploader.bat VS.0004009330
        echo ===============================================================================
    ) 1>&2
    exit /b 1
)

rem 3. Execute HisPacsUploader.exe
"%ROOT_DIR%HisPacsUploader.exe" %*
exit /b %ERRORLEVEL%
