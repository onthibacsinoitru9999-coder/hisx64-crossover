@echo off
chcp 65001 >nul
echo =============================================================================
echo  [+] HIS UI WRAPPER: SYNTHESIZE AGENT SKILL FROM UI RECORDINGS
echo =============================================================================
powershell -ExecutionPolicy Bypass -File "%~dp0SynthesizeSkill.ps1" %*
pause
