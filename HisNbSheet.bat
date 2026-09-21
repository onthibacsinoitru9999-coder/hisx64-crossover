@echo off
chcp 65001 >nul
python "%~dp0Tools\nb_sheet_client.py" %*
