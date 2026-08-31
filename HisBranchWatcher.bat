@echo off
chcp 65001 >nul
title HIS LIVE BRANCH WATCHER - CS2 NINH BINH
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WatchBranchLearning.ps1"
pause
