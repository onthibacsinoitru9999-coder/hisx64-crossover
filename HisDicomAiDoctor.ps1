<#
.SYNOPSIS
    HisDicomAiDoctor - Tự động đẩy file DICOM lên Colab Pro, chạy AI cắt tổn thương và xuất báo cáo lâm sàng
.DESCRIPTION
    Kết nối máy trạm với máy chủ AI Colab Pro qua Cloudflare Tunnel:
    - Tải file DICOM (.dcm) lên máy chủ AI.
    - Nhận ảnh cắt vùng tổn thương (Cropped patch) & Ảnh toàn cảnh khoanh viền đỏ.
    - Nhận mô tả tổn thương chi tiết (Findings), Kết luận (Impression) và Hướng xử trí.
    - Tự động lưu và mở báo cáo HTML trực quan trên trình duyệt máy trạm.
.EXAMPLE
    .\HisDicomAiDoctor.ps1 -SetUrl "https://xxx.trycloudflare.com"
    .\HisDicomAiDoctor.ps1 -File "D:\XRay\film.dcm"
    .\HisDicomAiDoctor.ps1 -PatientId "0004009330"
#>
param(
    [Parameter(Position = 0)]
    [string]$File,

    [Parameter()]
    [string]$SetUrl,

    [Parameter()]
    [string]$PatientId,

    [Parameter()]
    [switch]$Test,

    [Parameter()]
    [switch]$NoOpen
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ConfigFile = Join-Path $ScriptDir "colab_endpoint.txt"
$ReportsDir = Join-Path $ScriptDir "Reports\DicomAiReports"
$ImagesDir = Join-Path $ReportsDir "Images"

# 1. Xử lý lưu URL cấu hình
if ($SetUrl) {
    $cleanUrl = $SetUrl.Trim().TrimEnd('/')
    $cleanUrl | Out-File -FilePath $ConfigFile -Encoding utf8 -Force
    Write-Host "`n[SUCCESS] Da luu cau hinh cong Colab Pro endpoint vao: $ConfigFile" -ForegroundColor Green
    Write-Host "URL: $cleanUrl`n" -ForegroundColor Cyan
    exit 0
}

# 2. Đọc URL cấu hình
if (-not (Test-Path $ConfigFile)) {
    Write-Host "`n[!] CHUA CAU HINH CONG KET NOI COLAB PRO!" -ForegroundColor Yellow
    Write-Host "Vui long lay URL tu man hinh Colab (vi du: https://xxx.trycloudflare.com)" -ForegroundColor Gray
    Write-Host "va chay lenh thiet lap:" -ForegroundColor Gray
    Write-Host "   .\HisDicomAiDoctor.bat -SetUrl `"https://xxx.trycloudflare.com`"`n" -ForegroundColor Cyan
    exit 1
}

$ColabUrl = (Get-Content $ConfigFile -Encoding utf8).Trim().TrimEnd('/')
if ([string]::IsNullOrWhiteSpace($ColabUrl)) {
    Write-Host "[!] File cau hinh $ConfigFile rong! Vui long chay: .\HisDicomAiDoctor.bat -SetUrl <URL>" -ForegroundColor Red
    exit 1
}

# 3. Chế độ kiểm tra kết nối (-Test)
if ($Test) {
    Write-Host "[*] Dang kiem tra ket noi toi Colab Pro tai: $ColabUrl/health ..." -ForegroundColor Cyan
    try {
        $res = curl.exe -s -m 10 "$ColabUrl/health" | ConvertFrom-Json
        Write-Host "[+] KET NOI THANH CONG!" -ForegroundColor Green
        Write-Host "  - Trang thai: $($res.status)" -ForegroundColor White
        Write-Host "  - Thiet bi:   $($res.device)" -ForegroundColor White
        Write-Host "  - Florence-2: $($res.florence)" -ForegroundColor White
    }
    catch {
        Write-Host "[!] Khong the ket noi toi Colab Pro. Kiem tra xem Notebook tren Colab con dang chay khong!" -ForegroundColor Red
    }
    exit 0
}

# 4. Xác định file DICOM cần gửi
$TargetDicom = $File

if (-not $TargetDicom -and $PatientId) {
    Write-Host "[*] Dang tim kiem file DICOM cho Ma BN $PatientId tu thu muc du an..." -ForegroundColor Cyan
    # Tìm file dcm tương ứng với mã BN trong thư mục tạm hoặc PACS download
    $foundFiles = Get-ChildItem -Path $ScriptDir -Filter "*$PatientId*.dcm" -Recurse -ErrorAction SilentlyContinue
    if ($foundFiles -and $foundFiles.Count -gt 0) {
        $TargetDicom = $foundFiles[0].FullName
        Write-Host "[+] Da tim thay file: $TargetDicom" -ForegroundColor Green
    } else {
        Write-Host "[!] Khong tim thay file DICOM san co cho Ma BN $PatientId tren may." -ForegroundColor Yellow
        Write-Host "    Vui long truyen duong dan file: .\HisDicomAiDoctor.bat -File <duong_dan_dcm>" -ForegroundColor Gray
        exit 1
    }
}

if (-not $TargetDicom) {
    Write-Host "`n===============================================================================" -ForegroundColor Cyan
    Write-Host "  HE THONG AI PHAN TICH DICOM & BOC TACH TON THUONG - KHOA 57 BACH MAI" -ForegroundColor Yellow
    Write-Host "===============================================================================" -ForegroundColor Cyan
    Write-Host "Cu phap su dung:" -ForegroundColor White
    Write-Host "  1. Thiet lap URL Colab:  .\HisDicomAiDoctor.bat -SetUrl <URL>" -ForegroundColor Gray
    Write-Host "  2. Gui file DICOM:       .\HisDicomAiDoctor.bat -File `"C:\path\to\film.dcm`"" -ForegroundColor Gray
    Write-Host "  3. Kiem tra ket noi:     .\HisDicomAiDoctor.bat -Test" -ForegroundColor Gray
    Write-Host "`nURL hien tai: $ColabUrl`n" -ForegroundColor DarkGray
    exit 0
}

if (-not (Test-Path $TargetDicom)) {
    Write-Host "[!] Khong ton tai file tai: $TargetDicom" -ForegroundColor Red
    exit 1
}

# 5. Tạo thư mục báo cáo nếu chưa có
if (-not (Test-Path $ImagesDir)) {
    New-Item -ItemType Directory -Path $ImagesDir -Force | Out-Null
}

$fileInfo = Get-Item $TargetDicom
$fileSizeMb = [math]::Round($fileInfo.Length / 1MB, 2)
Write-Host "`n[*] File: $($fileInfo.Name) ($fileSizeMb MB)" -ForegroundColor White
Write-Host "[*] Dang day file len Colab Pro Vision AI Server ($ColabUrl)..." -ForegroundColor Cyan

# 6. Gửi file DICOM lên Colab bằng curl.exe
$tempJsonPath = [System.IO.Path]::GetTempFileName()
$startTime = Get-Date

try {
    $curlOut = curl.exe -s -X POST "$ColabUrl/api/analyze-dicom" `
        -F "file=@`"$TargetDicom`"" `
        -o "$tempJsonPath" `
        --connect-timeout 15 `
        --max-time 180

    if (-not (Test-Path $tempJsonPath) -or (Get-Item $tempJsonPath).Length -eq 0) {
        throw "Colab khong tra ve du lieu!"
    }

    $jsonRaw = Get-Content -Path $tempJsonPath -Raw -Encoding utf8
    $result = $jsonRaw | ConvertFrom-Json

    if (-not $result.success) {
        Write-Host "[!] AI phan tich that bai: $($result.error)" -ForegroundColor Red
        if ($result.detail) { Write-Host $result.detail -ForegroundColor DarkRed }
        exit 1
    }

    $duration = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)
    Write-Host "[+] AI xu ly thanh cong trong $duration giay!" -ForegroundColor Green

    # 7. Trích xuất thông tin
    $pId = $result.metadata.patient_id
    if (-not $pId -or $pId -eq "UNKNOWN") { $pId = "BN_" + (Get-Date -Format "yyyyMMdd_HHmmss") }
    $pName = $result.metadata.patient_name
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

    # Lưu ảnh cắt tổn thương (cropped patch)
    $cropFileName = "Crop_${pId}_${timestamp}.jpg"
    $cropFilePath = Join-Path $ImagesDir $cropFileName
    if ($result.cropped_image_base64) {
        $cropBytes = [System.Convert]::FromBase64String($result.cropped_image_base64)
        [System.IO.File]::WriteAllBytes($cropFilePath, $cropBytes)
    }

    # Lưu ảnh toàn cảnh có khung đỏ (annotated)
    $annotFileName = "Annot_${pId}_${timestamp}.jpg"
    $annotFilePath = Join-Path $ImagesDir $annotFileName
    if ($result.annotated_image_base64) {
        $annotBytes = [System.Convert]::FromBase64String($result.annotated_image_base64)
        [System.IO.File]::WriteAllBytes($annotFilePath, $annotBytes)
    }

    # Lưu báo cáo HTML
    $reportHtmlPath = Join-Path $ReportsDir "Report_${pId}_${timestamp}.html"
    $result.report_html | Out-File -FilePath $reportHtmlPath -Encoding utf8 -Force

    # Lưu báo cáo Markdown
    $reportMdPath = Join-Path $ReportsDir "Report_${pId}_${timestamp}.md"
    $result.report_markdown | Out-File -FilePath $reportMdPath -Encoding utf8 -Force

    # 8. In kết quả lâm sàng tóm tắt ra Terminal
    Write-Host "`n===============================================================================" -ForegroundColor Green
    Write-Host "  KET QUA PHAN TICH HINH ANH X-QUANG AI (KHOA 57 BACH MAI)" -ForegroundColor Yellow
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host "  - Benh nhan:    $pName (Ma BN: $pId)" -ForegroundColor White
    Write-Host "  - Vung chup:    $($result.metadata.body_part) ($($result.metadata.modality))" -ForegroundColor White
    Write-Host "  - Ngay chup:    $($result.metadata.study_date)" -ForegroundColor White
    Write-Host "-------------------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "  [TOA DO TON THUONG - CROP ROI]:" -ForegroundColor Cyan
    Write-Host "    Box: [$($result.bbox -join ', ')]" -ForegroundColor Gray
    Write-Host "    Anh crop: $cropFilePath" -ForegroundColor DarkCyan
    Write-Host "-------------------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "  [KET LUAN (IMPRESSION)]:" -ForegroundColor Yellow
    Write-Host "    $($result.impression)" -ForegroundColor White
    Write-Host "    Ma ICD-10: $($result.icd10)" -ForegroundColor Red
    Write-Host "-------------------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "  [BAO CAO DA LUU]:" -ForegroundColor Green
    Write-Host "    HTML: $reportHtmlPath" -ForegroundColor White
    Write-Host "    MD:   $reportMdPath" -ForegroundColor Gray
    Write-Host "===============================================================================`n" -ForegroundColor Green

    # 9. Tự động mở báo cáo HTML trên trình duyệt máy trạm
    if (-not $NoOpen) {
        Write-Host "[*] Dang mo bao cao tren trinh duyet..." -ForegroundColor Cyan
        Start-Process $reportHtmlPath
    }

}
finally {
    if (Test-Path $tempJsonPath) {
        Remove-Item $tempJsonPath -Force -ErrorAction SilentlyContinue
    }
}
