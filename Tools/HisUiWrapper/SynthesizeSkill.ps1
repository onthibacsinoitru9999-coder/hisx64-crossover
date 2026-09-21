# =============================================================================
# SYNTHESIZE AGENT SKILL FROM HIS UI RECORDING (AUTO SKILL FACTORY)
# Tự động chuyển đổi thư mục ghi nhận thao tác của HisUiWrapper thành Agent Skill
# =============================================================================
param (
    [Parameter(Mandatory=$false)]
    [string]$RecordingFolder = "",

    [Parameter(Mandatory=$false)]
    [string]$SkillName = "",

    [Parameter(Mandatory=$false)]
    [string]$SkillTitle = ""
)

$baseDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if (-not (Test-Path (Join-Path $baseDir "Logs\ui_recordings"))) {
    $baseDir = Get-Location
}

$recordingsRoot = Join-Path $baseDir "Logs\ui_recordings"

Write-Host "=============================================================================" -ForegroundColor Cyan
Write-Host " ⚡ HIS UI WRAPPER: RECORDING TO AGENT SKILL SYNTHESIZER                      " -ForegroundColor Cyan
Write-Host "=============================================================================" -ForegroundColor Cyan

# 1. Nếu chưa chỉ định folder, liệt kê các folder có sẵn trong Logs/ui_recordings
if ([string]::IsNullOrWhiteSpace($RecordingFolder)) {
    $folders = Get-ChildItem -Path $recordingsRoot -Directory
    if ($folders.Count -eq 0) {
        Write-Host "[!] Không tìm thấy thư mục ghi nhận nào trong: $recordingsRoot" -ForegroundColor Red
        return
    }
    
    Write-Host "[?] Danh sách các thư mục ghi nhận thao tác có sẵn:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $folders.Count; $i++) {
        Write-Host "    [$($i+1)] $($folders[$i].Name)" -ForegroundColor Green
    }
    
    $sel = Read-Host "Nhập số thứ tự hoặc tên thư mục cần tổng hợp thành Skill"
    if ($sel -match '^\d+$' -and [int]$sel -le $folders.Count) {
        $targetDir = $folders[[int]$sel - 1].FullName
    } else {
        $targetDir = Join-Path $recordingsRoot $sel
    }
} else {
    if (Test-Path $RecordingFolder) {
        $targetDir = (Resolve-Path $RecordingFolder).Path
    } else {
        $targetDir = Join-Path $recordingsRoot $RecordingFolder
    }
}

if (-not (Test-Path $targetDir)) {
    Write-Host "[!] Thư mục không tồn tại: $targetDir" -ForegroundColor Red
    return
}

$folderName = Split-Path $targetDir -Leaf
Write-Host "[+] Phân tích thư mục ghi nhận: $folderName" -ForegroundColor Cyan

# 2. Tìm các file thành phần
$workflowFiles = Get-ChildItem -Path $targetDir -Filter "*_workflow.md"
$jsonlFiles = Get-ChildItem -Path $targetDir -Filter "*.jsonl"
$replayFiles = Get-ChildItem -Path $targetDir -Filter "*_replay.cs"

if ($workflowFiles.Count -eq 0) {
    Write-Host "[!] Không tìm thấy file *_workflow.md trong thư mục $targetDir" -ForegroundColor Red
    return
}

$workflowPath = $workflowFiles[0].FullName
$jsonlPath = if ($jsonlFiles.Count -gt 0) { $jsonlFiles[0].FullName } else { $null }
$replayPath = if ($replayFiles.Count -gt 0) { $replayFiles[0].FullName } else { $null }

# 3. Chuẩn hóa Tên Skill (kebab-case)
if ([string]::IsNullOrWhiteSpace($SkillName)) {
    $raw = $folderName.Normalize([System.Text.NormalizationForm]::FormD) -replace '\p{M}', ''
    $clean = $raw.ToLower() -replace '[^a-z0-9]+', '-' -replace '^-|-$', ''
    $SkillName = "his-$clean"
}

if ([string]::IsNullOrWhiteSpace($SkillTitle)) {
    $SkillTitle = "Kỹ Năng $($folderName.ToUpper())"
}

$skillsRoot = Join-Path $baseDir ".agents\skills"
$targetSkillDir = Join-Path $skillsRoot $SkillName
$targetRefsDir = Join-Path $targetSkillDir "references"
$targetScriptsDir = Join-Path $targetSkillDir "scripts"

Write-Host "[+] Tên Skill: $SkillName" -ForegroundColor Green
Write-Host "[+] Thư mục đích: $targetSkillDir" -ForegroundColor Green

# 4. Khởi tạo thư mục đích
New-Item -ItemType Directory -Force -Path $targetSkillDir | Out-Null
New-Item -ItemType Directory -Force -Path $targetRefsDir | Out-Null
New-Item -ItemType Directory -Force -Path $targetScriptsDir | Out-Null

# 5. Sao chép các tệp tham chiếu
Copy-Item $workflowPath (Join-Path $targetRefsDir "workflow_recording.md") -Force
if ($replayPath) {
    Copy-Item $replayPath (Join-Path $targetScriptsDir "ReplayWorkflow.cs") -Force
}

# 6. Đọc nội dung workflow và trích xuất bảng phân tích
$lines = Get-Content $workflowPath -Encoding UTF8
$stepLines = $lines | Where-Object { $_ -match '^\d+\.\s+🏥' }

Write-Host "[+] Đã trích xuất $($stepLines.Count) bước thao tác lâm sàng" -ForegroundColor Cyan

# 7. Sinh nội dung SKILL.md (nếu chưa có hoặc tạo mới)
$targetSkillMd = Join-Path $targetSkillDir "SKILL.md"
if (-not (Test-Path $targetSkillMd)) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("name: $SkillName")
    [void]$sb.AppendLine("description: >-")
    [void]$sb.AppendLine("  Hướng dẫn và tự động hóa quy trình $folderName trên phần mềm HIS và EMR.")
    [void]$sb.AppendLine("  Được tổng hợp trực tiếp từ phiên ghi thao tác lâm sàng thực tế của bác sĩ.")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("# HIS / EMR Automation Skill: $SkillTitle")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Tài liệu này cung cấp hướng dẫn từng bước, ma trận phím tắt, đối soát điều khiển giao diện (UI controls)")
    [void]$sb.AppendLine("cho nghiệp vụ **$folderName** trên hệ thống Bệnh viện Bạch Mai.")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## 1. Thông Tin Phiên Làm Việc Gốc")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("- **Nghiệp vụ**: $folderName")
    [void]$sb.AppendLine("- **Thư mục ghi nhận**: `Logs/ui_recordings/$folderName`")
    [void]$sb.AppendLine("- **Tổng số thao tác**: $($stepLines.Count) bước")
    [void]$sb.AppendLine("- **Tài liệu chi tiết**: Xem [workflow_recording.md](references/workflow_recording.md)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## 2. Quy Trình Thao Tác Chi Tiết")
    [void]$sb.AppendLine("")
    foreach ($step in $stepLines) {
        [void]$sb.AppendLine($step)
        [void]$sb.AppendLine("")
    }
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## 3. Các Phím Tắt & Thao Tác Quan Trọng")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("- **`Ctrl + S`**: Lưu dữ liệu / Chọn phòng làm việc")
    [void]$sb.AppendLine("- **`Ctrl + A`**: Bổ sung danh mục thuốc / vật tư")
    [void]$sb.AppendLine("- **`Ctrl + E`**: Kết thúc ca xử lý")
    [void]$sb.AppendLine("- **`Enter`**: Xác nhận các hộp thoại thông báo")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## 4. Tệp Phát Lại Tự Động (Replay Script)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Mã nguồn C# tự động phát lại quy trình qua UI Automation:")
    [void]$sb.AppendLine("- Vị trí: `scripts/ReplayWorkflow.cs`")
    
    [System.IO.File]::WriteAllText($targetSkillMd, $sb.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "[+] Đã tạo mới file: $targetSkillMd" -ForegroundColor Green
} else {
    Write-Host "[i] File SKILL.md đã tồn tại, giữ nguyên nội dung tùy biến chuyên sâu." -ForegroundColor Yellow
}

Write-Host "=============================================================================" -ForegroundColor Green
Write-Host " [V] ĐÃ TỔNG HỢP HOÀN TẤT AGENT SKILL: $SkillName" -ForegroundColor Green
Write-Host "     - Tài liệu: $targetSkillDir\SKILL.md" -ForegroundColor Green
Write-Host "     - Tham chiếu: $targetRefsDir\workflow_recording.md" -ForegroundColor Green
if ($replayPath) {
    Write-Host "     - Script phát lại: $targetScriptsDir\ReplayWorkflow.cs" -ForegroundColor Green
}
Write-Host "=============================================================================" -ForegroundColor Green
