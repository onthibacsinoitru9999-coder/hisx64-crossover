@echo off
chcp 65001 >nul
echo ===============================================================================
echo   ĐỒNG BỘ THƯ VIỆN DLL SỬA LỖI KÝ TỜ ĐIỀU TRỊ (MOS.SDO & MOS.Filter)
echo ===============================================================================
echo.

tasklist /FI "IMAGENAME eq HIS.exe" 2>NUL | find /I /N "HIS.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [CANH BAO] HIS.exe đang chạy! File trong ReferencedAssemblies có thể bị khóa.
    echo Vui lòng đóng phần mềm HIS Desktop trước khi chạy bản vá này để đạt hiệu quả 100%.
    echo.
)

echo Đang copy MOS.SDO.dll và MOS.Filter.dll vào thư mục gốc...
copy /Y ".\MOS.SDO.dll" ".\MOS.SDO.dll" >nul 2>&1
if exist ".\MOS.SDO.dll" (
    echo   [OK] Đã có MOS.SDO.dll (bản mới 959KB) tại thư mục gốc.
)

echo Đang copy vào ReferencedAssemblies...
copy /Y ".\MOS.SDO.dll" ".\ReferencedAssemblies\MOS.SDO.dll" >nul 2>&1
if "%ERRORLEVEL%"=="0" (
    echo   [OK] Đã cập nhật ReferencedAssemblies\MOS.SDO.dll thành công!
) else (
    echo   [CHU Y] Không thể ghi đè ReferencedAssemblies\MOS.SDO.dll do HIS.exe đang mở.
    echo          File tại thư mục gốc đã sẵn sàng để HIS nạp khi khởi động lại!
)

copy /Y ".\MOS.Filter.dll" ".\ReferencedAssemblies\MOS.Filter.dll" >nul 2>&1
if "%ERRORLEVEL%"=="0" (
    echo   [OK] Đã cập nhật ReferencedAssemblies\MOS.Filter.dll thành công!
)

echo.
echo ===============================================================================
echo   HOÀN TẤT: Khởi động lại HIS.exe để áp dụng bản sửa lỗi nút 'Lưu & Ký'!
echo ===============================================================================
pause
