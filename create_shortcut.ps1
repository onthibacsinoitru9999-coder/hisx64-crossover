$desktop = [Environment]::GetFolderPath('Desktop')
$wsh = New-Object -ComObject WScript.Shell
$scPath = Join-Path $desktop "Ghi Thao Tac HIS - Action Recorder.lnk"
$target = "D:\New folder\his-x64-28-11fix GDYK\his-x64\HisActionRecorder.bat"
$workDir = "D:\New folder\his-x64-28-11fix GDYK\his-x64"
$iconPath = "D:\New folder\his-x64-28-11fix GDYK\his-x64\HisActionRecorder.exe"

$sc = $wsh.CreateShortcut($scPath)
$sc.TargetPath = $target
$sc.WorkingDirectory = $workDir
$sc.Description = "HIS Action Recorder v3.5 - Ghi thao tac UI tren Web va Win de hoc ky nang PT-01"
$sc.IconLocation = "$iconPath,0"
$sc.Save()

try {
    $bytes = [System.IO.File]::ReadAllBytes($scPath)
    $bytes[0x15] = $bytes[0x15] -bor 0x20
    [System.IO.File]::WriteAllBytes($scPath, $bytes)
    Write-Host "Set Run-As-Admin flag: SUCCESS"
} catch {
    Write-Host "Set Run-As-Admin flag: SKIPPED"
}

Write-Host "SHORTCUT_PATH: $scPath"
Write-Host "SHORTCUT_EXISTS: $(Test-Path $scPath)"
