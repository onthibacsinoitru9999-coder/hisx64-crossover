@echo off
setlocal enabledelayedexpansion
set SCRIPTS_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\.agents\skills\his-clinical-operations\scripts
set HIS_ROOT=D:\his\his-x64-28-11fix GDYK\his-x64
set REF_DIR=D:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC="%SCRIPTS_DIR%\FetchPatient.cs"
set OUT="%HIS_ROOT%\FetchPatient.exe"

set REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Data.dll /reference:System.Xml.dll /reference:System.Net.Http.dll

for %%f in ("%REF_DIR%\*.dll") do (
    set REFS=!REFS! /reference:"%%f"
)
for %%f in ("%HIS_ROOT%\Inventec.*.dll" "%HIS_ROOT%\HIS.*.dll" "%HIS_ROOT%\MOS.*.dll" "%HIS_ROOT%\Newtonsoft.Json.dll") do (
    set REFS=!REFS! /reference:"%%f"
)

"%CSC%" /target:exe /platform:x64 /out:%OUT% !REFS! %SRC%
