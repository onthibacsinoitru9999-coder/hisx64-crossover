<#
.SYNOPSIS
    HisDiabetesOrchestrator.ps1 - Điều phối tự động luồng theo dõi đường huyết ĐTĐ
    
.DESCRIPTION
    Script điều phối trung tâm: nhận ảnh báo cáo điều dưỡng → phân tích Gemini Vision
    → thực thi 3 tác vụ song song cho tất cả bệnh nhân:
      1. Tờ điều trị (HisTrackingCreator.exe)
      2. Chỉ định BM02426 - Đường máu mao mạch (HisGlucoseBedsideAssigner.exe)
      3. Kê đơn Insulin (HisAutoPrescribe.exe --batch)

.PARAMETER ImagePath
    Đường dẫn đến ảnh báo cáo điều dưỡng (JPG/PNG/PDF)

.PARAMETER JsonPath
    Nếu đã có file JSON từ parse_glucose_image.py thì bỏ qua bước phân tích ảnh
    
.PARAMETER Date
    Ngày báo cáo (yyyy-MM-dd), mặc định là hôm nay
    
.PARAMETER OpenRouterApiKey
    OpenRouter API Key để phân tích ảnh qua mô hình miễn phí stealth/ox-alpha (hoặc set biến OPENROUTER_API_KEY)

.PARAMETER OpenRouterModel
    Tên mô hình OpenRouter (mặc định: stealth/ox-alpha - Free Tier 1M context)

.PARAMETER GeminiApiKey
    Gemini API Key để phân tích ảnh (hoặc set biến môi trường GEMINI_API_KEY)

.PARAMETER DryRun
    Chỉ in lệnh sẽ chạy, không thực thi thật

.PARAMETER SkipConfirm
    Bỏ qua bước xác nhận dữ liệu (tự động tiếp tục)

.PARAMETER SkipTracking
    Bỏ qua bước tạo tờ điều trị

.PARAMETER SkipBedside
    Bỏ qua bước chỉ định BM02426

.PARAMETER SkipInsulin
    Bỏ qua bước kê đơn Insulin

.EXAMPLE
    .\HisDiabetesOrchestrator.ps1 -ImagePath "C:\Users\dr\Desktop\bao_cao_dh_17h.jpg"
    
.EXAMPLE
    .\HisDiabetesOrchestrator.ps1 -ImagePath "report.png" -OpenRouterModel "stealth/ox-alpha"
    
.EXAMPLE
    .\HisDiabetesOrchestrator.ps1 -JsonPath "glucose_data.json" -DryRun
#>

param(
    [string]$ImagePath = "",
    [string]$JsonPath = "",
    [string]$Date = (Get-Date -Format "yyyy-MM-dd"),
    [string]$OpenRouterApiKey = $env:OPENROUTER_API_KEY,
    [string]$OpenRouterModel = "stealth/ox-alpha",
    [string]$GeminiApiKey = $env:GEMINI_API_KEY,
    [switch]$DryRun,
    [switch]$SkipConfirm,
    [switch]$SkipTracking,
    [switch]$SkipBedside,
    [switch]$SkipInsulin
)


# ==============================================================================
# THIẾT LẬP ĐƯỜNG DẪN
# ==============================================================================

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User") + ";" + $env:Path
}
# Tự dò tìm thư mục scripts dù chạy từ ổ D: hay E:
$ScriptsDir = Join-Path $ScriptDir ".agents\skills\his-clinical-operations\scripts"
if (-not (Test-Path $ScriptsDir)) {
    $ScriptsDir = $ScriptDir  # fallback: cùng thư mục gốc
}

$TrackingExe   = Join-Path $ScriptsDir "HisTrackingCreator.exe"
$BedsideExe    = Join-Path $ScriptsDir "HisGlucoseBedsideAssigner.exe"
$InsulinExe    = Join-Path $ScriptsDir "HisAutoPrescribe.exe"
$ParseScript   = Join-Path $ScriptDir "parse_glucose_image.py"
$TempJsonPath  = Join-Path $ScriptDir "glucose_data.json"
$LogFile       = Join-Path $ScriptDir ("diabetes_orchestrator_" + (Get-Date -Format "yyyyMMdd_HHmm") + ".log")

# ==============================================================================
# HÀM TIỆN ÍCH
# ==============================================================================

function Write-Header {
    param([string]$Title)
    $line = "=" * 80
    Write-Host "`n$line" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "$line" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Step, [string]$Msg, [string]$Color = "Yellow")
    Write-Host "  [$Step] $Msg" -ForegroundColor $Color
    Add-Content -Path $LogFile -Value "[$([datetime]::Now.ToString('HH:mm:ss'))] [$Step] $Msg"
}

function Write-Success { param([string]$Msg) Write-Host "  ✅ $Msg" -ForegroundColor Green; Add-Content -Path $LogFile -Value "[OK] $Msg" }
function Write-Fail    { param([string]$Msg) Write-Host "  ❌ $Msg" -ForegroundColor Red; Add-Content -Path $LogFile -Value "[ERR] $Msg" }
function Write-Info    { param([string]$Msg) Write-Host "  ℹ️  $Msg" -ForegroundColor Cyan }

function Run-Tool {
    param(
        [string]$ExePath,
        [string[]]$Arguments,
        [string]$Label
    )
    
    $cmdLine = "`"$ExePath`" " + ($Arguments -join " ")
    
    if ($DryRun) {
        Write-Host "  [DRY-RUN] $cmdLine" -ForegroundColor Magenta
        return $true
    }
    
    Write-Step $Label "Chạy: $([System.IO.Path]::GetFileName($ExePath)) $($Arguments[0..2] -join ' ')..."
    
    try {
        $process = Start-Process -FilePath $ExePath -ArgumentList $Arguments `
            -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$env:TEMP\his_stdout.txt"
        
        $stdout = if (Test-Path "$env:TEMP\his_stdout.txt") { Get-Content "$env:TEMP\his_stdout.txt" -Raw } else { "" }
        Add-Content -Path $LogFile -Value $stdout
        
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq $null) {
            Write-Success "$Label hoàn tất"
            return $true
        } else {
            Write-Fail "$Label thất bại (Exit: $($process.ExitCode))"
            return $false
        }
    }
    catch {
        Write-Fail "$Label lỗi: $_"
        return $false
    }
}

function Format-TrackingContent {
    param([psobject]$Session, [string]$PatientCode)
    $timeLabel = switch ($Session.time) {
        "17:00" { "17h" }
        "21:00" { "21h" }
        "06:00" { "6h sáng" }
        default  { $Session.time }
    }
    
    $glucoseStr = if ($null -ne $Session.glucose_mmol) { "$($Session.glucose_mmol) mmol/l" } else { "chưa đo" }
    $content = "Đường máu mao mạch $timeLabel`: $glucoseStr."
    
    if ($Session.inject -and $Session.insulin_dose_ui -gt 0) {
        $content += " Tiêm $($Session.insulin_name) $($Session.insulin_dose_ui) đv dưới da."
    } else {
        $content += " Không tiêm Insulin (đường huyết trong giới hạn hoặc không có chỉ định)."
    }
    
    return $content
}

function Format-MedInstruction {
    param([psobject]$Session)
    if ($Session.inject -and $Session.insulin_dose_ui -gt 0) {
        $timeLabel = switch ($Session.time) {
            "17:00" { "trước ăn chiều 17h" }
            "21:00" { "21h trước ngủ" }
            "06:00" { "trước ăn sáng 6h" }
            default  { $Session.time }
        }
        return "Tiêm dưới da $($Session.insulin_dose_ui) đơn vị $timeLabel."
    }
    return ""
}

# ==============================================================================
# BƯỚC 0: KIỂM TRA CÔNG CỤ
# ==============================================================================

Write-Header "HIS DIABETES ORCHESTRATOR - Tự Động Hóa Theo Dõi Đường Huyết ĐTĐ"
Write-Info "Ngày thực thi: $(Get-Date -Format 'dd/MM/yyyy HH:mm')"
Write-Info "Log: $LogFile"
if ($DryRun) { Write-Host "`n  [CHẾ ĐỘ DRY-RUN - Không thực thi thật]" -ForegroundColor Magenta }

$toolsOk = $true
foreach ($exe in @($TrackingExe, $BedsideExe, $InsulinExe)) {
    if (-not (Test-Path $exe)) {
        Write-Fail "Không tìm thấy: $exe"
        $toolsOk = $false
    } else {
        Write-Info "✔ $(Split-Path -Leaf $exe)"
    }
}
if (-not $toolsOk) {
    Write-Fail "Thiếu công cụ. Kiểm tra lại thư mục scripts."
    exit 1
}

# ==============================================================================
# BƯỚC 1: ĐỌC DỮ LIỆU JSON (từ file có sẵn hoặc phân tích ảnh)
# ==============================================================================

Write-Header "BƯỚC 1: Chuẩn Bị Dữ Liệu"

if ($JsonPath -and (Test-Path $JsonPath)) {
    Write-Info "Dùng file JSON có sẵn: $JsonPath"
    $dataJson = Get-Content $JsonPath -Raw -Encoding UTF8
    $TempJsonPath = $JsonPath
}
elseif ($ImagePath -and (Test-Path $ImagePath)) {
    Write-Step "OCR" "Phân tích ảnh: $ImagePath"
    
    if (-not $OpenRouterApiKey) {
        $OpenRouterApiKey = [Environment]::GetEnvironmentVariable("OPENROUTER_API_KEY", "User")
    }
    if (-not $GeminiApiKey) {
        $GeminiApiKey = [Environment]::GetEnvironmentVariable("GEMINI_API_KEY", "User")
    }
    
    if (-not $OpenRouterApiKey -and -not $GeminiApiKey) {
        Write-Fail "Thiếu API Key! Vui lòng set OPENROUTER_API_KEY (ưu tiên stealth/ox-alpha Free Tier) hoặc GEMINI_API_KEY."
        exit 1
    }
    
    $previewFlag = if (-not $SkipConfirm) { "--preview" } else { "" }
    $openrouterFlag = if ($OpenRouterApiKey) { "--openrouter-key `"$OpenRouterApiKey`" --model `"$OpenRouterModel`"" } else { "" }
    $geminiFlag = if ($GeminiApiKey) { "--api-key `"$GeminiApiKey`"" } else { "" }
    $pyArgs = "`"$ImagePath`" --output `"$TempJsonPath`" --date $Date $openrouterFlag $geminiFlag $previewFlag"
    
    # Kiểm tra python hoặc uv
    $hasPython = (Get-Command python -ErrorAction SilentlyContinue)
    $hasUv = (Get-Command uv -ErrorAction SilentlyContinue)
    
    if ($hasPython) {
        $pyProcess = Start-Process -FilePath "python" -ArgumentList "`"$ParseScript`" $pyArgs" -Wait -NoNewWindow -PassThru
    } elseif ($hasUv) {
        $pyProcess = Start-Process -FilePath "uv" -ArgumentList "run python `"$ParseScript`" $pyArgs" -Wait -NoNewWindow -PassThru
    } else {
        Write-Fail "Không tìm thấy python hoặc uv trên hệ thống!"
        exit 1
    }
    
    if ($pyProcess.ExitCode -ne 0) {
        Write-Fail "Phân tích ảnh thất bại! Kiểm tra lại ảnh và API key."
        exit 1
    }
    
    if (-not (Test-Path $TempJsonPath)) {
        Write-Fail "Không tìm thấy file JSON đầu ra: $TempJsonPath"
        exit 1
    }
    
    $dataJson = Get-Content $TempJsonPath -Raw -Encoding UTF8
}
else {
    # Chế độ thủ công: tạo JSON mẫu và mở editor
    Write-Info "Không có ảnh hoặc JSON. Tạo file mẫu để nhập tay..."
    $sampleDate = $Date
    $nextDate = ([datetime]::ParseExact($Date, "yyyy-MM-dd", $null)).AddDays(1).ToString("yyyy-MM-dd")
    
    $sampleData = @{
        report_date = $sampleDate
        parse_note  = "Nhập thủ công - chưa OCR từ ảnh"
        patients    = @(
            @{
                patient_code = "0003969449"
                patient_name = "Nguyễn Văn A"
                bed          = "Phòng 711 - Giường 01"
                note         = ""
                sessions     = @(
                    @{ time="17:00"; date=$sampleDate; glucose_mmol=12.5; insulin_name="Actrapid"; insulin_dose_ui=8; inject=$true;  raw_text="" }
                    @{ time="21:00"; date=$sampleDate; glucose_mmol=9.2;  insulin_name="Actrapid"; insulin_dose_ui=6; inject=$true;  raw_text="" }
                    @{ time="06:00"; date=$nextDate;   glucose_mmol=$null; insulin_name="Lantus"; insulin_dose_ui=10; inject=$true; raw_text="" }
                )
            }
        )
    } | ConvertTo-Json -Depth 10
    
    Set-Content -Path $TempJsonPath -Value $sampleData -Encoding UTF8
    
    Write-Info "Đã tạo mẫu: $TempJsonPath"
    Write-Host "`n  Vui lòng chỉnh sửa file JSON rồi chạy lại với: -JsonPath `"$TempJsonPath`"" -ForegroundColor Yellow
    
    if (-not $SkipConfirm) {
        $answer = Read-Host "  Nhấn ENTER để mở Notepad chỉnh sửa (hoặc gõ 'skip' để tiếp tục với dữ liệu mẫu)"
        if ($answer -ne "skip") {
            Start-Process notepad.exe -ArgumentList $TempJsonPath -Wait
        }
    }
    
    $dataJson = Get-Content $TempJsonPath -Raw -Encoding UTF8
}

# Parse JSON
try {
    $data = $dataJson | ConvertFrom-Json
} catch {
    Write-Fail "JSON không hợp lệ: $_"
    exit 1
}

$patients = $data.patients
$reportDate = $data.report_date
Write-Success "Đọc dữ liệu: $($patients.Count) bệnh nhân | Ngày: $reportDate"

# ==============================================================================
# BƯỚC 2: XÁC NHẬN DỮ LIỆU VỚI BÁC SĨ
# ==============================================================================

Write-Header "BƯỚC 2: Xác Nhận Dữ Liệu"

# In bảng tóm tắt
Write-Host ""
Write-Host ("  {0,-4} {1,-14} {2,-20} {3,-8} {4,-10} {5,-14} {6,-10}" -f "STT", "Mã BN", "Tên BN", "Giờ", "ĐH(mmol)", "Insulin", "Liều(UI)") -ForegroundColor White
Write-Host "  " + "-" * 82 -ForegroundColor Gray

$idx = 1
$allSessions = @()

foreach ($pat in $patients) {
    foreach ($sess in $pat.sessions) {
        $glucose = if ($null -ne $sess.glucose_mmol) { "$($sess.glucose_mmol)" } else { "Chưa đo" }
        $injectFlag = if ($sess.inject) { "✅" } else { "❌" }
        $color = if ($idx % 2 -eq 0) { "Gray" } else { "White" }
        
        Write-Host ("  {0,-4} {1,-14} {2,-20} {3,-8} {4,-10} {5,-14} {6,-10} {7}" -f `
            $idx, $pat.patient_code, ($pat.patient_name -replace ".{18}$","..."), `
            $sess.time, $glucose, $sess.insulin_name, $sess.insulin_dose_ui, $injectFlag) `
            -ForegroundColor $color
        
        $allSessions += @{
            Patient  = $pat
            Session  = $sess
        }
        $idx++
    }
}

Write-Host ""
$injectCount = ($allSessions | Where-Object { $_.Session.inject -eq $true }).Count
Write-Info "Tổng: $($allSessions.Count) dòng | Có tiêm Insulin: $injectCount | Không tiêm: $($allSessions.Count - $injectCount)"

if (-not $SkipConfirm) {
    Write-Host ""
    $confirm = Read-Host "  ❓ Dữ liệu đúng chưa? Tiếp tục thực thi 3 bước? [y/N]"
    if ($confirm -notmatch "^(y|yes|có|co)$") {
        Write-Host "  ⛔ Hủy bỏ bởi người dùng." -ForegroundColor Red
        exit 0
    }
}

# ==============================================================================
# BƯỚC 3: CHUẨN BỊ FILE CSV TRUNG GIAN
# ==============================================================================

Write-Header "BƯỚC 3: Tạo File Lệnh Trung Gian"

$trackingOrders = @()  # Dữ liệu cho HisTrackingCreator
$bedsideOrders  = @{}  # Dữ liệu cho HisGlucoseBedsideAssigner (gom theo ngày+giờ)
$insulinOrders  = @()  # Dữ liệu cho HisAutoPrescribe --batch

foreach ($entry in $allSessions) {
    $pat  = $entry.Patient
    $sess = $entry.Session
    
    $content   = Format-TrackingContent -Session $sess -PatientCode $pat.patient_code
    $medInstr  = Format-MedInstruction -Session $sess
    $careInstr = "Chăm sóc cấp II. Chế độ ăn ĐTĐ. Theo dõi đường huyết."
    
    # --- Tờ điều trị ---
    if (-not $SkipTracking) {
        $trackingOrders += [PSCustomObject]@{
            PatientCode  = $pat.patient_code
            Time         = $sess.time
            Date         = $sess.date
            Content      = $content
            MedInstr     = $medInstr
            CareInstr    = $careInstr
        }
    }
    
    # --- Chỉ định BM02426: gom theo ngày+giờ để gọi 1 lần/nhóm ---
    if (-not $SkipBedside) {
        $key = "$($sess.date)|$($sess.time)"
        if (-not $bedsideOrders.ContainsKey($key)) {
            $bedsideOrders[$key] = @{
                Date     = $sess.date
                Time     = $sess.time
                Patients = @()
            }
        }
        if ($pat.patient_code) {
            $bedsideOrders[$key].Patients += $pat.patient_code
        }
    }
    
    # --- Kê đơn Insulin (chỉ khi có chỉ định tiêm) ---
    if (-not $SkipInsulin -and $sess.inject -and $sess.insulin_dose_ui -gt 0) {
        $tutorialMap = @{
            "17:00" = "Tiêm dưới da trước ăn chiều 17h"
            "21:00" = "Tiêm dưới da trước ngủ 21h"
            "06:00" = "Tiêm dưới da sáng 6h"
        }
        $tutorial = $tutorialMap[$sess.time] -replace "", "Theo chỉ dẫn bác sĩ"
        if (-not $tutorial) { $tutorial = "Tiêm dưới da $($sess.time)" }
        
        $insulinOrders += [PSCustomObject]@{
            patient_code  = $pat.patient_code
            dose          = $sess.insulin_dose_ui
            medicine      = $sess.insulin_name
            tutorial      = $tutorial
            time          = $sess.time
            date          = $sess.date
        }
    }
}

# Ghi file CSV Insulin
$insulinCsvPath = Join-Path $ScriptDir "insulin_orders_temp.csv"
if ($insulinOrders.Count -gt 0) {
    $csvLines = @("patient_code,dose,medicine,tutorial,time,date")
    foreach ($order in $insulinOrders) {
        $csvLines += "`"$($order.patient_code)`",$($order.dose),`"$($order.medicine)`",`"$($order.tutorial)`",$($order.time),$($order.date)"
    }
    Set-Content -Path $insulinCsvPath -Value ($csvLines -join "`n") -Encoding UTF8
    Write-Success "Tạo insulin_orders_temp.csv ($($insulinOrders.Count) y lệnh)"
}

Write-Success "Chuẩn bị xong: $($trackingOrders.Count) tờ điều trị | $($bedsideOrders.Count) nhóm BM02426 | $($insulinOrders.Count) kê Insulin"

# ==============================================================================
# BƯỚC 4: THỰC THI 3 TÁC VỤ
# ==============================================================================

Write-Header "BƯỚC 4: Thực Thi Tự Động"

$stats = @{
    TrackingOk = 0; TrackingFail = 0
    BedsideOk  = 0; BedsideFail  = 0
    InsulinOk  = 0; InsulinFail  = 0
}

# ─────────────────────────────────────────────────────
# TÁC VỤ 1: Tờ Điều Trị (HisTrackingCreator.exe)
# ─────────────────────────────────────────────────────
if (-not $SkipTracking -and $trackingOrders.Count -gt 0) {
    Write-Step "TỜ ĐT" "Tạo tờ điều trị ($($trackingOrders.Count) mục)..."
    
    foreach ($order in $trackingOrders) {
        if (-not $order.PatientCode) {
            Write-Fail "Bỏ qua: thiếu mã BN"
            $stats.TrackingFail++
            continue
        }
        
        $args = @(
            "-p", "`"$($order.PatientCode)`"",
            "-time", "`"$($order.Time)`"",
            "-content", "`"$($order.Content)`"",
            "-care", "`"$($order.CareInstr)`"",
            "-med", "`"$($order.MedInstr)`""
        )
        
        $ok = Run-Tool -ExePath $TrackingExe -Arguments $args -Label "TờĐT[$($order.PatientCode) $($order.Time)]"
        if ($ok) { $stats.TrackingOk++ } else { $stats.TrackingFail++ }
        
        Start-Sleep -Milliseconds 500  # Tránh spam API
    }
}

# ─────────────────────────────────────────────────────
# TÁC VỤ 2: Chỉ Định BM02426 (HisGlucoseBedsideAssigner.exe)
# ─────────────────────────────────────────────────────
if (-not $SkipBedside -and $bedsideOrders.Count -gt 0) {
    Write-Step "BM02426" "Chỉ định đường máu mao mạch ($($bedsideOrders.Count) nhóm giờ)..."
    
    foreach ($key in $bedsideOrders.Keys) {
        $group    = $bedsideOrders[$key]
        $patList  = ($group.Patients | Select-Object -Unique) -join ","
        $timeSlot = $group.Time
        $dateStr  = $group.Date
        
        if (-not $patList) {
            Write-Info "Bỏ qua nhóm $($key): không có mã BN hợp lệ"
            continue
        }
        
        $args = @(
            "-p", "`"$patList`"",
            "-time", "`"$timeSlot`"",
            "-date", "`"$dateStr`""
        )
        
        $ok = Run-Tool -ExePath $BedsideExe -Arguments $args -Label "BM02426[$timeSlot/$dateStr]"
        if ($ok) { $stats.BedsideOk++ } else { $stats.BedsideFail++ }
        
        Start-Sleep -Milliseconds 500
    }
}

# ─────────────────────────────────────────────────────
# TÁC VỤ 3: Kê Đơn Insulin (HisAutoPrescribe.exe --batch)
# ─────────────────────────────────────────────────────
if (-not $SkipInsulin -and $insulinOrders.Count -gt 0) {
    Write-Step "INSULIN" "Kê đơn Insulin batch ($($insulinOrders.Count) y lệnh)..."
    
    if (Test-Path $insulinCsvPath) {
        $args = @("--batch", "`"$insulinCsvPath`"")
        $ok = Run-Tool -ExePath $InsulinExe -Arguments $args -Label "Insulin[Batch]"
        if ($ok) { $stats.InsulinOk = $insulinOrders.Count } else { $stats.InsulinFail = $insulinOrders.Count }
    } else {
        Write-Fail "Không tìm thấy file CSV insulin: $insulinCsvPath"
        $stats.InsulinFail = $insulinOrders.Count
    }
}

# ==============================================================================
# BÁO CÁO KẾT QUẢ
# ==============================================================================

Write-Header "KẾT QUẢ THỰC THI"

$totalOk   = $stats.TrackingOk + $stats.BedsideOk + $stats.InsulinOk
$totalFail = $stats.TrackingFail + $stats.BedsideFail + $stats.InsulinFail

Write-Host ""
Write-Host ("  {0,-30} {1,-12} {2,-12}" -f "Tác vụ", "Thành công", "Lỗi") -ForegroundColor White
Write-Host "  " + "-" * 56 -ForegroundColor Gray
Write-Host ("  {0,-30} {1,-12} {2,-12}" -f "📝 Tờ điều trị", $stats.TrackingOk, $stats.TrackingFail) -ForegroundColor $(if($stats.TrackingFail -eq 0){"Green"}else{"Yellow"})
Write-Host ("  {0,-30} {1,-12} {2,-12}" -f "🩸 Chỉ định BM02426", $stats.BedsideOk, $stats.BedsideFail)  -ForegroundColor $(if($stats.BedsideFail -eq 0){"Green"}else{"Yellow"})
Write-Host ("  {0,-30} {1,-12} {2,-12}" -f "💉 Kê đơn Insulin", $stats.InsulinOk, $stats.InsulinFail)   -ForegroundColor $(if($stats.InsulinFail -eq 0){"Green"}else{"Yellow"})
Write-Host "  " + "-" * 56 -ForegroundColor Gray
Write-Host ("  {0,-30} {1,-12} {2,-12}" -f "TỔNG", $totalOk, $totalFail) -ForegroundColor $(if($totalFail -eq 0){"Green"}else{"Red"})
Write-Host ""

if ($totalFail -eq 0) {
    Write-Host "  🎉 HOÀN THÀNH TOÀN BỘ! Không có lỗi." -ForegroundColor Green
} else {
    Write-Host "  ⚠️  Hoàn thành với $totalFail lỗi. Xem log: $LogFile" -ForegroundColor Yellow
}

Write-Host "`n  📋 Log đầy đủ: $LogFile" -ForegroundColor Gray
if ($DryRun) { Write-Host "  [CHẾ ĐỘ DRY-RUN - Không có y lệnh nào thực sự được tạo]" -ForegroundColor Magenta }
Write-Host ""
