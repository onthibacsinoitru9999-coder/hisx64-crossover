@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
set EMR=Integrate\EMR
set REF=ReferencedAssemblies

"%CSC%" /target:exe /platform:x86 /optimize /out:HisEmrFiller.exe /r:"%EMR%\MDB.dll" /r:"%EMR%\EMR_MAIN.dll" /r:"%EMR%\EMR_MAIN.Library.dll" /r:"%EMR%\Oracle.ManagedDataAccess.dll" /r:"%REF%\Inventec.Common.Adapter.dll" /r:"%REF%\Inventec.Common.WebApiClient.dll" /r:"%REF%\Inventec.Core.dll" /r:"%REF%\MOS.EFMODEL.dll" /r:"%REF%\MOS.Filter.dll" /r:"%REF%\Newtonsoft.Json.dll" /r:System.dll /r:System.Core.dll /r:System.Net.Http.dll HisEmrFiller.cs

if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Bien dich that bai! Vui long kiem tra ma loi o tren.
    exit /b 1
)

echo [OK] Bien dich thanh cong!
echo.
HisEmrFiller.exe %*
endlocal
