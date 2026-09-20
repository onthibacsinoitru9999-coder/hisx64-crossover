@echo off
setlocal
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0build_mcp_server.ps1"
exit /b %ERRORLEVEL%
