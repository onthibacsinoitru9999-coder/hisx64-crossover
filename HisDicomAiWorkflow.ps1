<#
.SYNOPSIS
    HisDicomAiWorkflow - Workflow Tự Động Hóa Phân Tích DICOM & Bóc Tách Tổn Thương Bằng AI
.DESCRIPTION
    Tích hợp toàn diện:
    1. Chế độ 1-Click theo Mã Bệnh Nhân: Tự tải ảnh từ PACS Bạch Mai -> Bắn lên Colab Pro -> Cắt tổn thương -> Báo chuông & Mở báo cáo.
    2. Chế độ Thư mục Giám sát (Watcher): Tự động quét thư mục DicomInbox, phát hiện file mới là xử lý ngay.
    3. Chế độ Kéo thả File (.dcm): Kéo thả file trực tiếp vào batch file.
.EXAMPLE
    .\HisDicomAiWorkflow.bat 0004009330
    .\HisDicomAiWorkflow.bat "D:\XRay\film.dcm"
    .\HisDicomAiWorkflow.bat --watch
#>
param(
    [Parameter(Position = 0)]
    [string]$Target,

    [Parameter()]
    [switch]$Watch,

    [Parameter()]
    [string]$SetUrl,

    [Parameter()]
    [switch]$Test
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ConfigFile = Join-Path $ScriptDir "colab_endpoint.txt"
$InboxDir = Join-Path $ScriptDir "DicomInbox"
$ProcessedDir = Join-Path $InboxDir "Processed"
$ReportsDir = Join-Path $ScriptDir "Reports\DicomAiReports"
$ImagesDir = Join-Path $ReportsDir "Images"

# Tạo các thư mục cần thiết
foreach ($dir in @($InboxDir, $ProcessedDir, $ReportsDir, $ImagesDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# Hàm thông báo âm thanh và pop-up Windows
function Send-DoctorAlert {
    param(
        [string]$Title,
        [string]$Message,
        [string]$HtmlReportPath
    )
    # 1. Phát âm thanh hệ thống
    [System.Media.SystemSounds]::Asterisk.Play()

    # 2. Hiển thị thông báo góc phải màn hình (Balloon Tip)
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $notify = New-Object System.Windows.Forms.NotifyIcon
        $notify.Icon = [System.Drawing.SystemIcons]::Information
        $notify.BalloonTipTitle = $Title
        $notify.BalloonTipText = $Message
        $notify.Visible = $true
        $notify.ShowBalloonTip(6000)
        Start-Sleep -Milliseconds 500
    } catch {}

    # 3. Mở báo cáo HTML trên trình duyệt
    if ($HtmlReportPath -and (Test-Path $HtmlReportPath)) {
        Start-Process $HtmlReportPath
    }
}

# 1. Cấu hình URL nếu có tham số -SetUrl
if ($SetUrl) {
    & "$ScriptDir\HisDicomAiDoctor.bat" -SetUrl $SetUrl
    exit 0
}

# 2. Chế độ Test kết nối
if ($Test) {
    & "$ScriptDir\HisDicomAiDoctor.bat" -Test
    exit 0
}

# Kiểm tra file cấu hình
if (-not (Test-Path $ConfigFile)) {
    Write-Host "`n[!] CHUA CAU HINH CONG KET NOI COLAB PRO!" -ForegroundColor Yellow
    Write-Host "Vui long chay: .\HisDicomAiWorkflow.bat -SetUrl <URL_Colab_Tunnel>" -ForegroundColor Cyan
    exit 1
}

# Hàm xử lý 1 file DICOM đơn lẻ
function Process-SingleDicom {
    param([string]$FilePath)

    $f = Get-Item $FilePath
    Write-Host "`n" + ("=" * 75) -ForegroundColor Cyan
    Write-Host "  🚀 BẮT ĐẦU WORKFLOW TỰ ĐỘNG XỬ LÝ: $($f.Name)" -ForegroundColor Yellow
    Write-Host ("=" * 75) -ForegroundColor Cyan

    $tempOutput = & "$ScriptDir\HisDicomAiDoctor.bat" -File $FilePath -NoOpen 2>&1
    $tempOutput | ForEach-Object { Write-Host $_ }

    # Tìm file báo cáo HTML vừa tạo mới nhất
    $latestReport = Get-ChildItem -Path $ReportsDir -Filter "Report_*.html" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($latestReport) {
        $msg = "Đã bóc tách tổn thương và lập báo cáo xong cho file: $($f.Name)"
        Write-Host "`n[+] HOÀN TẤT TOÀN BỘ WORKFLOW!" -ForegroundColor Green
        Write-Host "[+] Báo cáo: $($latestReport.FullName)" -ForegroundColor White

        Send-DoctorAlert -Title "AI ĐỌC X-QUANG ĐÃ HOÀN TẤT!" -Message $msg -HtmlReportPath $latestReport.FullName
    } else {
        Write-Host "[!] Không tìm thấy báo cáo xuất ra. Vui lòng kiểm tra log lỗi!" -ForegroundColor Red
    }
}

# ==============================================================================
# CHẾ ĐỘ 1: THƯ MỤC GIÁM SÁT (WATCHER MODE)
# ==============================================================================
if ($Watch -or $Target -eq "--watch") {
    Write-Host "`n" + ("=" * 75) -ForegroundColor Green
    Write-Host "  👀 ĐANG CHẠY CHẾ ĐỘ GIÁM SÁT THƯ MỤC TỰ ĐỘNG (WATCHER MODE)" -ForegroundColor Yellow
    Write-Host "  Thư mục theo dõi: $InboxDir" -ForegroundColor White
    Write-Host "  👉 Bác sĩ/KTV chỉ cần copy hoặc kéo thả file .dcm vào thư mục trên." -ForegroundColor Cyan
    Write-Host "     Hệ thống sẽ TỰ ĐỘNG phân tích, cắt tổn thương và mở báo cáo ngay!" -ForegroundColor Cyan
    Write-Host "  (Nhấn Ctrl + C để dừng giám sát)" -ForegroundColor DarkGray
    Write-Host ("=" * 75) + "`n" -ForegroundColor Green

    while ($true) {
        $incomingFiles = Get-ChildItem -Path $InboxDir -Filter "*.dcm" -File -ErrorAction SilentlyContinue
        foreach ($dcm in $incomingFiles) {
            Write-Host "`n[*] Phát hiện file mới: $($dcm.Name)" -ForegroundColor Green
            Start-Sleep -Seconds 1 # Đợi file copy xong hoàn toàn
            
            Process-SingleDicom -FilePath $dcm.FullName

            # Chuyển file sang thư mục Processed
            $destPath = Join-Path $ProcessedDir ($dcm.BaseName + "_" + (Get-Date -Format "yyyyMMdd_HHmmss") + $dcm.Extension)
            Move-Item -Path $dcm.FullName -Destination $destPath -Force
            Write-Host "[+] Đã lưu trữ file gốc vào: $destPath" -ForegroundColor DarkGray
        }
        Start-Sleep -Seconds 2
    }
    exit 0
}

# ==============================================================================
# CHẾ ĐỘ 2: TRUYỀN ĐƯỜNG DẪN FILE TRỰC TIẾP
# ==============================================================================
if ($Target -and (Test-Path $Target)) {
    Process-SingleDicom -FilePath $Target
    exit 0
}

# ==============================================================================
# CHẾ ĐỘ 3: TRUYỀN MÃ BỆNH NHÂN (LẤY TỪ PACS HOẶC DỰ ÁN)
# ==============================================================================
if ($Target -and ($Target -match "^\d+$" -or $Target -match "^VS\.\d+$")) {
    $cleanPid = $Target.Replace("VS.", "").Trim()
    Write-Host "`n[*] Đang tìm kiếm ảnh DICOM của Bệnh nhân $cleanPid từ PACS/Dự án..." -ForegroundColor Cyan

    # Tìm file dcm có sẵn trong máy hoặc tải qua HisPacsUploader
    $found = Get-ChildItem -Path $ScriptDir -Filter "*$cleanPid*.dcm" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1

    if (-not $found -and (Test-Path "$ScriptDir\HisPacsUploader.exe")) {
        Write-Host "[*] Đang kéo ca chụp từ hệ thống PACS Bệnh viện Bạch Mai..." -ForegroundColor Yellow
        $tempDcmDir = Join-Path $env:TEMP "Pacs_Fetch_$cleanPid"
        if (-not (Test-Path $tempDcmDir)) { New-Item -ItemType Directory -Path $tempDcmDir -Force | Out-Null }
        
        # Gọi PacsClient tải về
        & "$ScriptDir\HisPacsUploader.exe" $cleanPid --ttl 24h 2>&1 | Out-Null
        $found = Get-ChildItem -Path $env:TEMP -Filter "*$cleanPid*.dcm" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
    }

    if ($found) {
        Process-SingleDicom -FilePath $found.FullName
        exit 0
    } else {
        Write-Host "[!] Không tìm thấy file DICOM của Mã BN $cleanPid trên máy." -ForegroundColor Red
        Write-Host "    Gợi ý: Bác sĩ có thể kéo thả trực tiếp file .dcm vào lệnh:" -ForegroundColor Yellow
        Write-Host "    .\HisDicomAiWorkflow.bat `"C:\duong_dan\film.dcm`"" -ForegroundColor Cyan
        exit 1
    }
}

# Menu hướng dẫn nếu không có tham số
Write-Host "`n===============================================================================" -ForegroundColor Cyan
Write-Host "  WORKFLOW TỰ ĐỘNG PHÂN TÍCH X-QUANG & BÓC TÁCH TỔN THƯƠNG AI" -ForegroundColor Yellow
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "Cách sử dụng cực kỳ đơn giản:" -ForegroundColor White
Write-Host "  1. Phân tích 1 file:         .\HisDicomAiWorkflow.bat `"C:\path\to\film.dcm`"" -ForegroundColor Cyan
Write-Host "  2. Bật giám sát thư mục:     .\HisDicomAiWorkflow.bat --watch" -ForegroundColor Cyan
Write-Host "  3. Theo mã bệnh nhân:        .\HisDicomAiWorkflow.bat 0004009330" -ForegroundColor Cyan
Write-Host "  4. Lưu URL Colab Pro:        .\HisDicomAiWorkflow.bat -SetUrl <URL>" -ForegroundColor Gray
Write-Host "  5. Kiểm tra kết nối Colab:   .\HisDicomAiWorkflow.bat -Test" -ForegroundColor Gray
Write-Host "`nThư mục nhận file tự động (Watcher): $InboxDir`n" -ForegroundColor DarkGray
