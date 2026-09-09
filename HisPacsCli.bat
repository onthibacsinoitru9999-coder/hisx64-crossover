@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0HisPacsCli.ps1" %*
