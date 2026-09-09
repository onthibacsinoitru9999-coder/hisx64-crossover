# Real-time HIS Log Monitor for Consultation / Hoi Chan
$ErrorActionPreference = "Continue"

$targetDirs = @(
    "$PSScriptRoot\Logs",
    "Logs",
    (Join-Path $PSScriptRoot "..\Logs"),
    (Join-Path (Get-Location).Path "Logs")
)

$logDir = $null
foreach ($d in $targetDirs) {
    if (Test-Path $d) {
        $logDir = $d
        break
    }
}

if (-not $logDir) {
    Write-Error "Khong tim thay thu muc Logs!"
    exit 1
}

$logPath = Join-Path $logDir "LogSystem.txt"
$captureFile = Join-Path $logDir "CapturedHoiChan_Live.txt"

# Clear or start new capture file
"=== BẮT ĐẦU THEO DÕI HỘI CHẨN CHUYÊN KHOA LÚC $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $captureFile -Encoding UTF8

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   🔥 HIS LIVE LOG MONITOR - THEO DÕI CHỈ ĐỊNH HỘI CHẨN CHUYÊN KHOA 🔥   " -ForegroundColor Yellow
Write-Host "   Target Log : $logPath" -ForegroundColor Gray
Write-Host "   Capture Out: $captureFile" -ForegroundColor Gray
Write-Host "=================================================================" -ForegroundColor Cyan

$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$fs.Seek(0, [System.IO.SeekOrigin]::End) | Out-Null
$sr = New-Object System.IO.StreamReader($fs, [System.Text.Encoding]::UTF8)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 🟢 ĐANG LẮNG NGHE THAO TÁC CỦA BÁC SĨ TRÊN GIAO DIỆN HIS..." -ForegroundColor Green
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Bác sĩ hãy bắt đầu thao tác tạo 'Chỉ định hội chẩn chuyên khoa' trên HIS ngay bây giờ!" -ForegroundColor Green

try {
    while ($true) {
        $line = $sr.ReadLine()
        if ($line -ne $null) {
            # Ghi toan bo ra file capture live
            $timestamp = Get-Date -Format "HH:mm:ss.fff"
            "[$timestamp] $line" | Out-File -FilePath $captureFile -Append -Encoding UTF8
            
            # Loc cac dong quan trong
            if ($line -match "WebApiClient\.Post\..*?api:([^_\s]+)" -or 
                $line -match "WebApiClient\.Get\..*?api:([^_\s]+)" -or 
                $line -match "SerializeObject data api: (.*)" -or
                $line -match "SERVICE_REQ" -or
                $line -match "DHST" -or
                $line -match "TRACKING" -or
                $line -match "CONSULTATION" -or
                $line -match "Consult" -or
                $line -match "HoiChan" -or
                $line -match "Assign" -or
                $line -match "ExecuteRoom" -or
                $line -match "Department" -or
                $line -match "TokenCode\|([a-f0-9]+)") {
                
                if ($line -match "SerializeObject data api: (.*)") {
                    Write-Host "[$timestamp] 📦 [REQUEST PAYLOAD]: $($matches[1])" -ForegroundColor Magenta
                } elseif ($line -match "WebApiClient\.Post\..*?api:([^_\s]+)") {
                    Write-Host "[$timestamp] 🚀 [POST API]: $($matches[1])" -ForegroundColor Yellow
                } elseif ($line -match "WebApiClient\.Get\..*?api:([^_\s]+)") {
                    Write-Host "[$timestamp] 🔍 [GET API]: $($matches[1])" -ForegroundColor Cyan
                } else {
                    Write-Host "[$timestamp] 📝 [LOG]: $line" -ForegroundColor Gray
                }
            }
        } else {
            Start-Sleep -Milliseconds 150
        }
    }
} finally {
    $sr.Close()
    $fs.Close()
}
