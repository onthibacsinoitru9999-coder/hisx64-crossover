@echo off
:: ==============================================================================
:: set_env.bat: Bo nap moi truong tu dong cho he thong HIS Automation
:: Ho tro da tang (Multi-Tier Toolchain Resolution) - Khong hardcode o dia C:\, D:\, E:\
:: ==============================================================================

if defined HIS_ENV_READY goto :check_vars

:: ------------------------------------------------------------------------------
:: 1. Git (git.exe)
:: ------------------------------------------------------------------------------
where git >nul 2>nul
if %errorlevel% equ 0 (
    for /f "delims=" %%i in ('where git 2^>nul') do if not defined GIT set "GIT=%%i"
) else (
    for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\Git.MinGit*") do (
        if exist "%%p\cmd\git.exe" (
            set "PATH=%%p\cmd;%%p\mingw64\bin;%PATH%"
            set "GIT=%%p\cmd\git.exe"
            goto :git_found
        )
    )
    if exist "%ProgramFiles%\Git\cmd\git.exe" (
        set "PATH=%ProgramFiles%\Git\cmd;%PATH%"
        set "GIT=%ProgramFiles%\Git\cmd\git.exe"
        goto :git_found
    )
    if exist "%ProgramFiles(x86)%\Git\cmd\git.exe" (
        set "PATH=%ProgramFiles(x86)%\Git\cmd;%PATH%"
        set "GIT=%ProgramFiles(x86)%\Git\cmd\git.exe"
        goto :git_found
    )
    if exist "%LOCALAPPDATA%\Programs\Git\cmd\git.exe" (
        set "PATH=%LOCALAPPDATA%\Programs\Git\cmd;%PATH%"
        set "GIT=%LOCALAPPDATA%\Programs\Git\cmd\git.exe"
        goto :git_found
    )
    if exist "%~dp0Tool\Git\cmd\git.exe" (
        set "PATH=%~dp0Tool\Git\cmd;%PATH%"
        set "GIT=%~dp0Tool\Git\cmd\git.exe"
        goto :git_found
    )
)
:git_found

:: ------------------------------------------------------------------------------
:: 2. Python 3.12+ (python.exe) - Nap vao DAU PATH de chan WindowsApps 0-byte stub
:: ------------------------------------------------------------------------------
set "NEED_PYTHON=1"
where python >nul 2>nul
if %errorlevel% equ 0 (
    for /f "delims=" %%i in ('where python 2^>nul') do (
        echo %%i | findstr /i "WindowsApps" >nul
        if errorlevel 1 (
            if not defined PYTHON set "PYTHON=%%i"
            set "NEED_PYTHON=0"
        )
    )
)

if "%NEED_PYTHON%"=="1" (
    if exist "%~dp0.venv\Scripts\python.exe" (
        set "PATH=%~dp0.venv\Scripts;%PATH%"
        set "PYTHON=%~dp0.venv\Scripts\python.exe"
        goto :python_found
    )
    for /d %%p in ("%LOCALAPPDATA%\Programs\Python\Python3*") do (
        if exist "%%p\python.exe" (
            set "PATH=%%p;%%p\Scripts;%PATH%"
            set "PYTHON=%%p\python.exe"
            goto :python_found
        )
    )
    for /d %%p in ("%ProgramFiles%\Python3*") do (
        if exist "%%p\python.exe" (
            set "PATH=%%p;%%p\Scripts;%PATH%"
            set "PYTHON=%%p\python.exe"
            goto :python_found
        )
    )
    for /d %%p in ("%ProgramFiles(x86)%\Python3*") do (
        if exist "%%p\python.exe" (
            set "PATH=%%p;%%p\Scripts;%PATH%"
            set "PYTHON=%%p\python.exe"
            goto :python_found
        )
    )
    for /d %%p in ("%SystemDrive%\Python3*") do (
        if exist "%%p\python.exe" (
            set "PATH=%%p;%%p\Scripts;%PATH%"
            set "PYTHON=%%p\python.exe"
            goto :python_found
        )
    )
    where py >nul 2>nul
    if %errorlevel% equ 0 (
        for /f "delims=" %%i in ('py -3 -c "import sys; print(sys.executable)" 2^>nul') do (
            if exist "%%i" (
                set "PYTHON=%%i"
                for %%d in ("%%i") do set "PATH=%%~dpd;%%~dpdScripts;!PATH!"
                goto :python_found
            )
        )
    )
)
:python_found

:: ------------------------------------------------------------------------------
:: 3. C++ MinGW (GCC/G++)
:: ------------------------------------------------------------------------------
where g++ >nul 2>nul
if %errorlevel% neq 0 (
    for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs*") do (
        if exist "%%p\mingw64\bin\g++.exe" (
            set "PATH=%%p\mingw64\bin;%PATH%"
            goto :mingw_found
        )
    )
    if exist "%SystemDrive%\MinGW\bin\g++.exe" (
        set "PATH=%SystemDrive%\MinGW\bin;%PATH%"
        goto :mingw_found
    )
    if exist "%ProgramFiles%\mingw-w64\bin\g++.exe" (
        set "PATH=%ProgramFiles%\mingw-w64\bin;%PATH%"
        goto :mingw_found
    )
)
:mingw_found

:: ------------------------------------------------------------------------------
:: 4. C# Compiler (csc.exe) - Uu tien 64-bit Framework, sau do 32-bit va Roslyn
:: ------------------------------------------------------------------------------
if not defined SystemRoot set "SystemRoot=C:\Windows"

if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    set "PATH=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319;%PATH%"
    goto :csc_found
)
if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    set "PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319;%PATH%"
    goto :csc_found
)
for /d %%v in ("%ProgramFiles%\Microsoft Visual Studio\*\*" "%ProgramFiles(x86)%\Microsoft Visual Studio\*\*") do (
    if exist "%%v\MSBuild\Current\Bin\Roslyn\csc.exe" (
        set "CSC=%%v\MSBuild\Current\Bin\Roslyn\csc.exe"
        set "PATH=%%v\MSBuild\Current\Bin\Roslyn;%PATH%"
        goto :csc_found
    )
)
where csc >nul 2>nul
if %errorlevel% equ 0 (
    for /f "delims=" %%i in ('where csc 2^>nul') do if not defined CSC set "CSC=%%i"
)
:csc_found

:: ------------------------------------------------------------------------------
:: 5. Cloud Sync Tool (rclone.exe)
:: ------------------------------------------------------------------------------
if exist "%~dp0rclone.exe" (
    set "RCLONE=%~dp0rclone.exe"
    set "PATH=%~dp0;%PATH%"
) else (
    where rclone >nul 2>nul
    if %errorlevel% equ 0 (
        for /f "delims=" %%i in ('where rclone 2^>nul') do if not defined RCLONE set "RCLONE=%%i"
    ) else if exist "%LOCALAPPDATA%\Programs\rclone\rclone.exe" (
        set "RCLONE=%LOCALAPPDATA%\Programs\rclone\rclone.exe"
        set "PATH=%LOCALAPPDATA%\Programs\rclone;%PATH%"
    ) else if exist "%ProgramFiles%\rclone\rclone.exe" (
        set "RCLONE=%ProgramFiles%\rclone\rclone.exe"
        set "PATH=%ProgramFiles%\rclone;%PATH%"
    )
)

set HIS_ENV_READY=1

:check_vars
if not defined CSC (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    ) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    ) else (
        where csc >nul 2>nul && for /f "delims=" %%i in ('where csc 2^>nul') do if not defined CSC set "CSC=%%i"
    )
)
if not defined GIT (
    where git >nul 2>nul && for /f "delims=" %%i in ('where git 2^>nul') do if not defined GIT set "GIT=%%i"
)
if not defined PYTHON (
    where python >nul 2>nul && for /f "delims=" %%i in ('where python 2^>nul') do (
        echo %%i | findstr /i "WindowsApps" >nul
        if errorlevel 1 if not defined PYTHON set "PYTHON=%%i"
    )
)
if not defined RCLONE (
    where rclone >nul 2>nul && for /f "delims=" %%i in ('where rclone 2^>nul') do if not defined RCLONE set "RCLONE=%%i"
    if not defined RCLONE if exist "%~dp0rclone.exe" set "RCLONE=%~dp0rclone.exe"
)
(call )
exit /b 0
