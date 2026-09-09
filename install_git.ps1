# PowerShell script cai dat Git for Windows tu dong
Write-Host "Dang kiem tra Git..." -ForegroundColor Cyan
if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Host "Git da duoc cai dat: $(git --version)" -ForegroundColor Green
    Exit
}

Write-Host "Chua co Git. Dang tai Git 64-bit cho Windows..." -ForegroundColor Yellow
$installerPath = "$env:TEMP\GitInstaller.exe"
$url = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/Git-2.44.0-64-bit.exe"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $url -OutFile $installerPath

Write-Host "Dang cai dat Git ngam (Silent Install)..." -ForegroundColor Yellow
Start-Process -FilePath $installerPath -ArgumentList "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS" -Wait

Write-Host "Cai dat hoan tat! Vui long khoi dong lai Terminal hoac IDE." -ForegroundColor Green
