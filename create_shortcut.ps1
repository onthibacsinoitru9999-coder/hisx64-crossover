$target = "D:\his 3-9\his-x64-28-11fix GDYK\his-x64\Reports\BienBanHoiChan_PT01\PT01_20260930_HN"
$desktop = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Desktop)
$shortcutPath = [System.IO.Path]::Combine($desktop, "BienBan_PT01_30092026_Khoa57.lnk")

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $target
$shortcut.WorkingDirectory = $target
$shortcut.Description = "Thu muc chua 12 Bien ban Hoi chan PT-01 Ngay 30/09/2026"
$shortcut.Save()

Write-Host "Created shortcut successfully at: $shortcutPath"
