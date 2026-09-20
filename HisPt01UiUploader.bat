@echo off
chcp 65001 >nul
title HIS PT-01 UI AUTOMATION RUNNER
"%~dp0HisPt01UiUploader.exe" %*
exit /b %ERRORLEVEL%
