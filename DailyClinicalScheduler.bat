@echo off
:: ==============================================================================
:: DailyClinicalScheduler.bat: Master Routine Scheduler CLI Launcher
:: Ho tro Windows PowerShell 5.1 (xu ly UTF-8 emoji qua temp file) va PowerShell Core
:: ==============================================================================
setlocal enabledelayedexpansion
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

:: 1. Nap bo moi truong tu dong
if not defined HIS_ENV_READY (
    if exist "%SCRIPT_DIR%set_env.bat" (
        call "%SCRIPT_DIR%set_env.bat"
    )
)

:: 2. Tim kiem powershell.exe
set "PS_EXE="
if exist "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" (
    set "PS_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
) else (
    where powershell.exe >nul 2>nul
    if !ERRORLEVEL! equ 0 ( set "PS_EXE=powershell.exe" ) else (
        echo [ERROR] Khong tim thay powershell.exe!
        exit /b 1
    )
)

:: 3. Kiem tra script
if not exist "%SCRIPT_DIR%DailyClinicalScheduler.ps1" (
    echo [ERROR] Khong tim thay DailyClinicalScheduler.ps1
    exit /b 1
)

:: 4. Chay qua wrapper UTF-8 tranh loi emoji PS5.1
set "ARGS=%*"
"!PS_EXE!" -NoProfile -ExecutionPolicy Bypass -Command "& { $src='%SCRIPT_DIR%DailyClinicalScheduler.ps1'; $tmp=[IO.Path]::Combine($env:TEMP,'HIS_DCS_run.ps1'); $bytes=[IO.File]::ReadAllBytes($src); $txt=[Text.Encoding]::UTF8.GetString($bytes); [IO.File]::WriteAllText($tmp,$txt,[Text.Encoding]::UTF8); & $tmp %ARGS% }"
set "SCHED_EXIT=!ERRORLEVEL!"
exit /b !SCHED_EXIT!
