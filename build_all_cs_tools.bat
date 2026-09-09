@echo off
setlocal
cd /d "%~dp0"
call "%~dp0set_env.bat"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_all_cs_tools.ps1" %*
exit /b %ERRORLEVEL%
