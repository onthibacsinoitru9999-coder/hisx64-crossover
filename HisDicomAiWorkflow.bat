@echo off
chcp 65001 > nul
set "ROOT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT_DIR%HisDicomAiWorkflow.ps1" %*
