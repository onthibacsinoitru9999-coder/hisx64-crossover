@echo off
REM HisEmrFiller.bat — Build HisEmrFiller.cs
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set EMR=Integrate\EMR
set REF=ReferencedAssemblies

echo [BUILD] HisEmrFiller.exe ...
"%CSC%" ^
  /target:exe ^
  /platform:x64 ^
  /optimize ^
  /out:HisEmrFiller.exe ^
  /r:"%EMR%\MDB.dll" ^
  /r:"%EMR%\EMR_MAIN.Library.dll" ^
  /r:"%EMR%\Oracle.ManagedDataAccess.dll" ^
  /r:"%REF%\Inventec.Common.Adapter.dll" ^
  /r:"%REF%\Inventec.Common.WebApiClient.dll" ^
  /r:"%REF%\Inventec.Core.dll" ^
  /r:"%REF%\MOS.EFMODEL.dll" ^
  /r:"%REF%\MOS.Filter.dll" ^
  /r:"%REF%\Newtonsoft.Json.dll" ^
  /r:System.dll ^
  /r:System.Core.dll ^
  /r:System.Net.Http.dll ^
  HisEmrFiller.cs

if %ERRORLEVEL%==0 (
    echo [OK] Build thanh cong: HisEmrFiller.exe
) else (
    echo [FAIL] Build that bai, xem loi o tren.
    exit /b 1
)
endlocal
