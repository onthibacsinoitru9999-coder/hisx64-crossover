# ==============================================================================
# 🔥 HIS LIVE BRANCH WATCHER & LEARNER (NINH BÌNH / MULTI-BRANCH ADAPTIVE MONITOR)
# ==============================================================================
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
        $logDir = (Get-Item $d).FullName
        break
    }
}

if (-not $logDir) {
    Write-Host "[ERROR] Không tìm thấy thư mục Logs!" -ForegroundColor Red
    exit 1
}

$logPath = Join-Path $logDir "LogSystem.txt"
$sessionLogPath = Join-Path $logDir "LogSession.txt"
$captureFile = Join-Path $logDir "Captured_Branch_Learning.txt"

# Khởi tạo file log
"=== BẮT ĐẦU THEO DÕI & HỌC TẬP CƠ SỞ 2 (NINH BÌNH) LÚC $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $captureFile -Encoding UTF8

Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "   🔥 TRÌNH THEO DÕI & HỌC TẬP TỰ ĐỘNG - CƠ SỞ 2 NINH BÌNH / ĐA CƠ SỞ 🔥   " -ForegroundColor Yellow
Write-Host "   Target Log : $logPath" -ForegroundColor Gray
Write-Host "   Capture Out: $captureFile" -ForegroundColor Gray
Write-Host "===============================================================================" -ForegroundColor Cyan

# Kiểm tra nếu file chưa tồn tại thì tạo file rỗng để stream
if (-not (Test-Path $logPath)) {
    New-Item -ItemType File -Path $logPath -Force | Out-Null
}

$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
if ($fs.Length -gt 10000) {
    $fs.Seek(-10000, [System.IO.SeekOrigin]::End) | Out-Null
}
$sr = New-Object System.IO.StreamReader($fs, [System.Text.Encoding]::UTF8)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 🟢 TRÌNH THEO DÕI ĐANG CHẠY TRỰC TIẾP!" -ForegroundColor Green
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 👉 Bác sĩ hãy đăng nhập hoặc chuyển sang Cơ sở 2 (Ninh Bình) trên HIS." -ForegroundColor Green
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 👂 Đang lắng nghe luồng sự kiện từ LogSystem.txt..." -ForegroundColor Cyan

$lastToken = ""
$lastBranch = ""

while ($true) {
    $line = $sr.ReadLine()
    if ($line -ne $null) {
        $timestamp = Get-Date -Format "HH:mm:ss.fff"
        "[$timestamp] $line" | Out-File -FilePath $captureFile -Append -Encoding UTF8
        
        # 1. Phát hiện TokenCode
        if ($line -match "TokenCode\|([a-fA-F0-9]{64})") {
            $token = $Matches[1]
            if ($token -ne $lastToken) {
                $lastToken = $token
                Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] 🔑 [PHÁT HIỆN TOKEN MỚI] $token" -ForegroundColor Yellow
                "TOKEN_CAPTURED: $token" | Out-File -FilePath (Join-Path $logDir "ActiveToken.txt") -Encoding UTF8
            }
        }
        
        # 2. Phát hiện UpdateWorkInfo hoặc Branch / Room / Department
        if ($line -match "UpdateWorkInfo" -or $line -match "WorkInfoSDO" -or $line -match "DEPARTMENT_ID" -or $line -match "BRANCH_ID" -or $line -match "ROOM_ID") {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 🏢 [CẤU HÌNH PHÒNG/KHOA/CƠ SỞ] $line" -ForegroundColor Magenta
        }
        
        # 3. Phát hiện gọi API
        if ($line -match "WebApiClient\.(Post|Get)\..*?api:([^_\s]+)") {
            $apiMethod = $Matches[1]
            $apiName = $Matches[2]
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 🌐 [API GỌI] $apiMethod -> $apiName" -ForegroundColor Cyan
        }
        
        # 4. Phát hiện SerializeObject data liên quan đến bệnh nhân / khoa phòng / đơn thuốc / tờ điều trị
        if ($line -match "SerializeObject data api: (.*)") {
            $dataSnippet = $Matches[1]
            if ($dataSnippet.Length -gt 120) { $dataSnippet = $dataSnippet.Substring(0, 120) + "..." }
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 📦 [DỮ LIỆU GỬI] $dataSnippet" -ForegroundColor Gray
        }

        # 5. Phát hiện lỗi hệ thống
        if ($line -match "Exception" -or $line -match "Error" -or $line -match "Fail") {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ⚠️ [CẢNH BÁO/LỖI] $line" -ForegroundColor Red
        }
    } else {
        Start-Sleep -Milliseconds 300
    }
}
