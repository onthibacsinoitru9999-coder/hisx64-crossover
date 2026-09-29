@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

set SCRIPT_DIR=%~dp0
set CLI_EXE=%SCRIPT_DIR%HisClinicalCli.exe

if not exist "%CLI_EXE%" (
    echo [ERROR] Khong tim thay file %CLI_EXE%!
    exit /b 1
)

"%CLI_EXE%" lookup-cls %*
