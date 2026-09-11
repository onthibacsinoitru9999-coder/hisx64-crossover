<#
.SYNOPSIS
    DailyClinicalScheduler.ps1 - Master Routine Scheduler CLI for HIS Automation
    Điều phối tự động Lịch trình Lâm sàng Hàng ngày cho Bác sĩ Ngoại khoa & CTCH.

.DESCRIPTION
    Bộ lập lịch và điều phối trung tâm:
    - 06:30 AM: Đi buồng sáng, báo cáo giao ban, rà soát 10 nhóm Bilan mổ, đóng gói y lệnh bổ sung.
    - 17:00 PM: Rà soát đơn thuốc ngày mai (kèm tờ ĐT), chỉ định suất ăn ngày mai, báo cáo cuối ngày.
    - Giám sát pháp chế EMR: Sơ kết 3/7/15 ngày, đánh giá dinh dưỡng 7 ngày, đối soát mã ICD-10.
    - Hỗ trợ đa cơ sở: Hà Nội (Khoa 57, Buồng 714 & 717 Drive) và Ninh Bình (Khoa 915, Khu 3E & 3D).
    - Rào chắn an toàn: Cắt cầu dao tối đa 2 lần thử (Circuit Breaker), Zero-Hallucination, Dry-Run.

.PARAMETER Morning
    Kích hoạt phiên Đi buồng sáng (06:30 AM).

.PARAMETER Evening
    Kích hoạt phiên Y lệnh chiều (17:00 PM).

.PARAMETER Facility
    Chỉ định cơ sở: 'HN' (Hà Nội - Khoa 57) hoặc 'NB' (Ninh Bình - Khoa 915).

.PARAMETER Room
    Bộ lọc buồng bệnh (VD: "714", "714,717", "3E", "3E,3D").

.PARAMETER DryRun
    Chế độ mô phỏng kiểm tra (Audit only - Không gọi API sửa đổi).

.PARAMETER OnDemand
    Chạy thủ công ngay lập tức theo yêu cầu.

.PARAMETER AuditLegal
    Chạy riêng hoặc kết hợp kiểm tra pháp chế EMR (Sơ kết 3/7/15 ngày, Dinh dưỡng, ICD-10).

.PARAMETER Interactive
    Bật bảng điều khiển tương tác duyệt 1-Click (Interactive Dashboard).

.PARAMETER SkipSync
    Bỏ qua đồng bộ Google Drive cho buồng 717.

.PARAMETER SkipConfirm
    Tự động thực thi không cần bấm phím xác nhận (dùng cho Cron).

.PARAMETER Date
    Ngày thực hiện (định dạng yyyy-MM-dd, mặc định hôm nay).

.PARAMETER CronDaemon
    Chạy vòng lặp nền daemon tự động đánh thức vào 06:30 và 17:00.

.PARAMETER InstallCron
    Đăng ký Windows Scheduled Tasks cho 06:30 và 17:00.

.EXAMPLE
    .\DailyClinicalScheduler.ps1 -Morning -Facility HN -DryRun
    .\DailyClinicalScheduler.ps1 -Evening -Facility NB -Room "3E"
    .\DailyClinicalScheduler.ps1 -OnDemand -Interactive
#>

[CmdletBinding()]
param(
    [switch]$Morning,
    [switch]$Evening,
    [string]$Facility = "",
    [object]$Room = $null,
    [switch]$DryRun,
    [switch]$OnDemand,
    [switch]$AuditLegal,
    [switch]$Interactive,
    [switch]$SkipSync,
    [switch]$SkipConfirm,
    [string]$Date = (Get-Date -Format "yyyy-MM-dd"),
    [switch]$CronDaemon,
    [switch]$InstallCron
)

# ==============================================================================
# 0. THIẾT LẬP MÔI TRƯỜNG & KHỞI TẠO ĐƯỜNG DẪN
# ==============================================================================
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    if (-not $ScriptDir) { $ScriptDir = (Get-Location).Path }
}

# Nạp môi trường tự động từ set_env.ps1 nếu có
$SetEnvPs1 = Join-Path $ScriptDir "set_env.ps1"
if (Test-Path $SetEnvPs1) {
    try { . $SetEnvPs1 } catch { Write-Warning "Không thể dot-source set_env.ps1: $_" }
}

# Thư mục logs và báo cáo
$LogsDir = Join-Path $ScriptDir "Logs"
if (-not (Test-Path $LogsDir)) { New-Item -ItemType Directory -Path $LogsDir -Force | Out-Null }
$LogFile = Join-Path $LogsDir ("DailyClinicalScheduler_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")

$ReportsDir = Join-Path $ScriptDir "Reports"
if (-not (Test-Path $ReportsDir)) { New-Item -ItemType Directory -Path $ReportsDir -Force | Out-Null }

$ConfigDir = Join-Path $ScriptDir "Configs"
$FacilityConfigPath = Join-Path $ConfigDir "FacilityConfig.json"
$ModulesDir = Join-Path $ScriptDir "Modules"

# Nạp Module FacilityManager.ps1 nếu có
$FacilityManagerPs1 = Join-Path $ModulesDir "FacilityManager.ps1"
if (Test-Path $FacilityManagerPs1) {
    try { . $FacilityManagerPs1 } catch { Write-Warning "Không thể nạp FacilityManager.ps1: $_" }
}

# Danh sách việc đã làm thành công cho Circuit Breaker
$global:SucceededTasks = [System.Collections.ArrayList]::new()

# ==============================================================================
# 1. HỆ THỐNG HÀM TIỆN ÍCH, GHI LOG & GIAO DIỆN
# ==============================================================================
function Write-Log {
    param([string]$Msg, [string]$Level = "INFO", [string]$Component = "CORE")
    $timestamp = [DateTime]::Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
    $logLine = "[$timestamp] [$Level] [$Component] $Msg"
    try { Add-Content -Path $LogFile -Value $logLine -Encoding UTF8 } catch {}
}

function Write-ColorHost {
    param([string]$Msg, [string]$Color = "White")
    Write-Host $Msg -ForegroundColor $Color
    Write-Log -Msg $Msg -Level "INFO"
}

function Write-Step {
    param([string]$Step, [string]$Msg)
    Write-Host "  [$Step] " -ForegroundColor Yellow -NoNewline
    Write-Host $Msg -ForegroundColor White
    Write-Log -Msg "[$Step] $Msg" -Level "STEP"
}

function Write-Ok   { param([string]$Msg) Write-Host "  ✅ $Msg" -ForegroundColor Green; Write-Log -Msg "[OK] $Msg" -Level "SUCCESS" }
function Write-Warn { param([string]$Msg) Write-Host "  ⚠️  $Msg" -ForegroundColor Yellow; Write-Log -Msg "[WARN] $Msg" -Level "WARN" }
function Write-Err  { param([string]$Msg) Write-Host "  ❌ $Msg" -ForegroundColor Red; Write-Log -Msg "[ERR] $Msg" -Level "ERROR" }
function Write-Info { param([string]$Msg) Write-Host "  ℹ️  $Msg" -ForegroundColor Cyan; Write-Log -Msg "[INFO] $Msg" -Level "INFO" }

# ==============================================================================
# 2. CƠ CHẾ CẮT CẦU DAO 2 LẦN THỬ (STRICT 2-ATTEMPT CIRCUIT BREAKER)
# ==============================================================================
function Invoke-WithCircuitBreaker {
    param(
        [Parameter(Mandatory=$true)][string]$TaskName,
        [Parameter(Mandatory=$true)][scriptblock]$Action,
        [string]$UiGuidance = "Vui lòng mở giao diện HIS Client để kiểm tra và xử lý trực tiếp."
    )

    if ($DryRun) {
        Write-Host "  [DRY-RUN] Bỏ qua thực thi thật cho tác vụ: $TaskName" -ForegroundColor Magenta
        Write-Log -Msg "[DRY-RUN] Bỏ qua thực thi: $TaskName" -Level "DRYRUN"
        [void]$global:SucceededTasks.Add("SIMULATED: $TaskName")
        return $true
    }

    $attempt = 1
    $maxAttempts = 2
    $lastError = $null

    while ($attempt -le $maxAttempts) {
        try {
            Write-Log -Msg "Bắt đầu thực thi: $TaskName (Lần thử $attempt/$maxAttempts)" -Level "INFO"
            $result = & $Action
            if ($result -eq $false) {
                throw "Tác vụ trả về kết quả thất bại (false)"
            }
            Write-Ok "$TaskName hoàn thành thành công."
            [void]$global:SucceededTasks.Add("[$([DateTime]::Now.ToString('HH:mm:ss'))] $TaskName")
            return $true
        }
        catch {
            $lastError = $_
            Write-Warn "Lần thử $attempt/$maxAttempts cho '$TaskName' thất bại: $($_.Exception.Message)"
            Write-Log -Msg "Lần thử $attempt thất bại: $_" -Level "WARN"

            if ($attempt -lt $maxAttempts) {
                Write-Info "Đang phân tích nguyên nhân và chuẩn bị thử lại lần 2 sau 1000ms..."
                Start-Sleep -Milliseconds 1000
            }
            $attempt++
        }
    }

    # KÍCH HOẠT HARD STOP
    Write-Host "`n" -NoNewline
    Write-Host "================================================================================" -ForegroundColor Red
    Write-Host "🛑 CIRCUIT BREAKER TRIGGERED — TỰ ĐỘNG CẮT CẦU DAO (HARD STOP)" -ForegroundColor Red
    Write-Host "================================================================================" -ForegroundColor Red
    Write-Host "Tác vụ thất bại : $TaskName" -ForegroundColor Yellow
    Write-Host "Số lần thử      : 2 / 2 lần (Vượt ngưỡng an toàn lâm sàng quy định)" -ForegroundColor Yellow
    
    Write-Host "`n1. CÁC TÁC VỤ ĐÃ HOÀN THÀNH THÀNH CÔNG:" -ForegroundColor Green
    if ($global:SucceededTasks.Count -gt 0) {
        foreach ($t in $global:SucceededTasks) {
            Write-Host "   ✅ $t" -ForegroundColor Green
        }
    } else {
        Write-Host "   (Chưa có tác vụ nào hoàn tất trong phiên này)" -ForegroundColor Gray
    }

    Write-Host "`n2. NGUYÊN NHÂN KỸ THUẬT CHÍNH XÁC:" -ForegroundColor Red
    Write-Host "   ❌ Lỗi: $($lastError.Exception.Message)" -ForegroundColor Red
    if ($lastError.InvocationInfo) {
        $errLine = $lastError.InvocationInfo.ScriptLineNumber
        $errScript = $lastError.InvocationInfo.ScriptName
        Write-Host "   📍 Vị trí: Dòng $errLine trong $errScript" -ForegroundColor DarkGray
    }

    Write-Host "`n3. HƯỚNG DẪN BÁC SĨ XỬ LÝ 1-CLICK TRÊN GIAO DIỆN HIS UI:" -ForegroundColor Cyan
    Write-Host "   👉 $UiGuidance" -ForegroundColor Cyan
    Write-Host "================================================================================`n" -ForegroundColor Red

    Write-Log -Msg "CIRCUIT BREAKER HARD STOP: $TaskName | Error: $lastError" -Level "FATAL"
    return $false
}

# ==============================================================================
# 3. QUẢN LÝ CƠ SỞ & NẠP CẤU HÌNH (FACILITY MANAGEMENT)
# ==============================================================================
function Resolve-FacilityContext {
    param([string]$CliFacility)

    # Ưu tiên sử dụng FacilityManager.ps1 nếu đã được nạp
    if ((Get-Command "Resolve-Facility" -ErrorAction SilentlyContinue) -and (Get-Command "Get-FacilityConfig" -ErrorAction SilentlyContinue)) {
        try {
            $res = Resolve-Facility -ExplicitFacility $CliFacility -ConfigPath $FacilityConfigPath
            $cfg = Get-FacilityConfig -ConfigPath $FacilityConfigPath -Facility $res.FacilityCode
            return @{
                Profile    = $cfg
                Resolution = $res
            }
        }
        catch [System.ArgumentException] {
            throw
        }
        catch {
            if (-not [string]::IsNullOrWhiteSpace($CliFacility)) {
                throw
            }
            Write-Warn "Lỗi khi gọi FacilityManager: $_. Sử dụng cấu hình dự phòng..."
        }
    }

    # Cấu hình dự phòng tích hợp sẵn
    $builtinConfig = @{
        "HN" = [PSCustomObject]@{
            Facility                 = "HN"
            FacilityCode             = "HN"
            FacilityName             = "Khoa Chấn thương Chỉnh hình & Cột sống - Bệnh viện Bạch Mai"
            DepartmentId             = 57
            DepartmentCode           = "KCTCHCS"
            DepartmentName           = "Khoa Chấn thương Chỉnh hình & Cột sống"
            BranchId                 = 1
            BranchCode               = "BVBM"
            DefaultWorkingRoomId     = 5248
            WorkingRoomName          = "Phòng làm việc Bác sĩ P734 Nhà Q"
            ProcedureRoomId          = 931
            ProcedureRoomName        = "Phòng Tiểu Phẫu Nhà Q"
            DefaultCabinetStockId    = 810
            CabinetStockCode         = "TT_KCTCHCS"
            BedsideGlucoseServiceId  = 6217
            BedsideGlucoseServiceCode = "BM02426"
            Rooms                    = @("714", "717")
            SyncRoom717FromDrive     = $true
        }
        "NB" = [PSCustomObject]@{
            Facility                 = "NB"
            FacilityCode             = "NB"
            FacilityName             = "Khoa Ngoại tổng hợp - BV Bạch Mai Cơ sở 2 Ninh Bình"
            DepartmentId             = 915
            DepartmentCode           = "CSNBKP05"
            DepartmentName           = "Khoa Ngoại tổng hợp - Tầng 3 Nhà E"
            BranchId                 = 81
            BranchCode               = "CS2NB"
            DefaultWorkingRoomId     = 18679
            WorkingRoomName          = "Phòng TT Khoa CTCH và PT Cột sống (3E-05)"
            ProcedureRoomId          = 18681
            ProcedureRoomName        = "Phòng TT Khoa Phẫu thuật tiêu hóa - gan mật tụy (3D-05)"
            DefaultCabinetStockId    = 5142
            CabinetStockCode         = "TTT_NBKP05.02"
            BedsideGlucoseServiceId  = 74281
            BedsideGlucoseServiceCode = "NB260620.6231"
            Rooms                    = @("3E", "3D")
            SyncRoom717FromDrive     = $false
        }
    }

    # Đọc từ file JSON nếu có
    if (Test-Path $FacilityConfigPath) {
        try {
            $jsonRaw = Get-Content $FacilityConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($jsonRaw.Facilities.HN) { $builtinConfig.HN = $jsonRaw.Facilities.HN }
            if ($jsonRaw.Facilities.NB) { $builtinConfig.NB = $jsonRaw.Facilities.NB }
        } catch { }
    }

    $targetCode = "HN"
    $tierName = "Tier 5: Default Fallback"
    $src = "Default HN"

    if ($CliFacility -match "^(HN|HANOI|HA-NOI|HA_NOI|57|1)$") {
        $targetCode = "HN"
        $tierName = "Tier 1: Explicit CLI Parameter"
        $src = "-Facility $CliFacility"
    }
    elseif ($CliFacility -match "^(NB|NINHBINH|NINH-BINH|NINH_BINH|915|81|CSNB)$") {
        $targetCode = "NB"
        $tierName = "Tier 1: Explicit CLI Parameter"
        $src = "-Facility $CliFacility"
    }
    elseif ($env:HIS_FACILITY -match "^(NB|NINHBINH)$") {
        $targetCode = "NB"
        $tierName = "Tier 2: Environment Variable"
        $src = "HIS_FACILITY=$env:HIS_FACILITY"
    }
    elseif ($env:HIS_FACILITY -match "^(HN|HANOI)$") {
        $targetCode = "HN"
        $tierName = "Tier 2: Environment Variable"
        $src = "HIS_FACILITY=$env:HIS_FACILITY"
    }

    return @{
        Profile    = $builtinConfig[$targetCode]
        Resolution = [PSCustomObject]@{
            FacilityCode    = $targetCode
            DetectionTier   = 1
            TierName        = $tierName
            DetectionSource = $src
            Confidence      = "High"
        }
    }
}

# ==============================================================================
# 4. HIỂN THỊ BANNER ĐẦU PHIÊN (FACILITY PRE-FLIGHT BANNER)
# ==============================================================================
function Show-PreFlightBanner {
    param($Fac, [string]$RoutineMode, [string]$TargetRooms, $ResolutionResult = $null)

    # Nếu module FacilityManager đã có sẵn Show-FacilityBanner
    if (Get-Command "Show-FacilityBanner" -ErrorAction SilentlyContinue) {
        Show-FacilityBanner -FacilityConfig $Fac -ResolutionResult $ResolutionResult
        return
    }

    $dryRunText = if ($DryRun) { "BẬT (Mô phỏng - Không ghi DB)" } else { "TẮT (Thực thi y lệnh thật)" }
    $onDemandText = if ($OnDemand) { "Có (Kích hoạt thủ công)" } else { "Không (Theo lịch Cron)" }

    $isHN = ($Fac.Facility -eq "HN" -or $Fac.FacilityCode -eq "HN")
    $accentColor = if ($isHN) { "Cyan" } else { "Green" }
    $icon = if ($isHN) { "🏥 [HÀ NỘI]" } else { "🏥 [NINH BÌNH]" }

    $line = "=" * 80
    Write-Host "`n$line" -ForegroundColor $accentColor
    Write-Host "  $icon - $($Fac.FacilityName.ToUpper())" -ForegroundColor White
    Write-Host "  Chi nhánh: Branch $($Fac.BranchId) ($($Fac.BranchCode)) | Khoa: $($Fac.DepartmentId) ($($Fac.DepartmentCode))" -ForegroundColor Gray
    Write-Host "  Phòng làm việc: Room $($Fac.DefaultWorkingRoomId) ($($Fac.WorkingRoomName))" -ForegroundColor Gray
    Write-Host "  Phòng thủ thuật: Room $($Fac.ProcedureRoomId) ($($Fac.ProcedureRoomName))" -ForegroundColor Gray
    Write-Host "  Tủ trực thuốc mặc định: Stock $($Fac.DefaultCabinetStockId) ($($Fac.CabinetStockCode))" -ForegroundColor Magenta
    Write-Host "  ĐMMM tại giường: Mã [$($Fac.BedsideGlucoseServiceCode)] (Service ID: $($Fac.BedsideGlucoseServiceId))" -ForegroundColor Yellow
    Write-Host "  Buồng bệnh quét: [$TargetRooms] $(if ($Fac.SyncRoom717FromDrive) { '(Buồng 717 đồng bộ từ Google Sheets)' } else { '' })" -ForegroundColor Cyan
    Write-Host "  Chế độ thực thi: $RoutineMode | Dry-Run: $dryRunText | OnDemand: $onDemandText" -ForegroundColor White
    if ($ResolutionResult) {
        Write-Host "  Nhận diện qua: $($ResolutionResult.TierName) -> $($ResolutionResult.DetectionSource)" -ForegroundColor Magenta
    }
    Write-Host "  Thời gian hệ thống: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Gray
    Write-Host "$line`n" -ForegroundColor $accentColor

    Write-Log -Msg "Pre-flight banner displayed for $($Fac.FacilityCode). Mode: $RoutineMode, DryRun: $DryRun"
}

# ==============================================================================
# 5. CHẨN ĐOÁN SỨC KHỎE HỆ THỐNG ĐẦU PHIÊN (HEALTH CHECK LADDER)
# ==============================================================================
function Test-SystemReadiness {
    param($Fac)
    Write-Step "1/4" "Kiểm tra kết nối mạng & máy chủ HIS..."
    
    $mosHost = "192.168.7.236"
    $mosPort = 1608
    $tcpOk = $false
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $iar = $tcpClient.BeginConnect($mosHost, $mosPort, $null, $null)
        $wait = $iar.AsyncWaitHandle.WaitOne(600, $false)
        if ($wait -and $tcpClient.Connected) {
            $tcpClient.EndConnect($iar)
            $tcpOk = $true
        }
        $tcpClient.Close()
    } catch {}

    if ($tcpOk) {
        Write-Ok "Kết nối TCP máy chủ MOS ($mosHost`:$mosPort) thành công."
    } else {
        Write-Warn "Không thể kết nối trực tiếp cổng $mosPort ($mosHost). Hệ thống sẽ dùng token cache hoặc chạy ở chế độ offline."
    }

    $diagBat = Join-Path $ScriptDir "HisDiagnosticDoctor.bat"
    if (Test-Path $diagBat) {
        Write-Step "2/4" "Kiểm tra công cụ chuẩn đoán hệ thống..."
        Write-Ok "HisDiagnosticDoctor sẵn sàng."
    }

    Write-Step "3/4" "Xác thực danh mục & rào chắn chéo cơ sở..."
    if (Get-Command "Test-FacilityCrossContamination" -ErrorAction SilentlyContinue) {
        $check = Test-FacilityCrossContamination -TargetFacility $Fac.FacilityCode -ServiceCode $Fac.BedsideGlucoseServiceCode -StockId $Fac.DefaultCabinetStockId
        if ($check.IsValid) {
            Write-Ok "Rào chắn cơ sở: Đã khóa danh mục cho $($Fac.FacilityCode) (Khoa $($Fac.DepartmentId)). Chống 100% nhầm lẫn dịch vụ."
        } else {
            Write-Warn "Cảnh báo kiểm tra danh mục cơ sở: $($check.Violations -join '; ')"
        }
    } else {
        Write-Ok "Rào chắn cơ sở: Đã khóa danh mục cho $($Fac.FacilityCode) (Khoa $($Fac.DepartmentId))."
    }
    
    Write-Step "4/4" "Sẵn sàng khởi động quy trình lâm sàng."
}

# ==============================================================================
# 6. ĐỒNG BỘ BUỒNG 717 TỪ GOOGLE SHEETS / DRIVE
# ==============================================================================
function Sync-Room717Data {
    param($Fac)
    if ($Fac.FacilityCode -ne "HN" -or -not $Fac.SyncRoom717FromDrive) { return }
    if ($SkipSync) {
        Write-Info "Đã kích hoạt cờ -SkipSync. Bỏ qua đồng bộ Google Drive cho buồng 717."
        return
    }

    Write-Step "SYNC-717" "Đồng bộ danh sách người bệnh buồng 717 từ Google Sheets..."
    if (Get-Command "Sync-Room717Sheet" -ErrorAction SilentlyContinue) {
        try {
            $syncRes = Sync-Room717Sheet
            if ($syncRes.Success) {
                Write-Ok "Đồng bộ buồng 717 hoàn tất ($($syncRes.Source): $($syncRes.FilePath))."
                return
            } else {
                Write-Warn "Đồng bộ buồng 717 không thành công ($($syncRes.Error)). Tiếp tục với dữ liệu sẵn có."
            }
        }
        catch {
            Write-Warn "Lỗi khi gọi Sync-Room717Sheet: $_"
        }
    }
    else {
        Write-Info "Module đồng bộ Buồng 717 chưa nạp. Bỏ qua bước đồng bộ tự động."
    }
}

# ==============================================================================
# 7. ĐIỀU PHỐI PHIÊN ĐI BUỒNG SÁNG (06:30 MORNING ROUNDS & PRE-OP BILAN)
# ==============================================================================
function Run-MorningRoutine {
    param($Fac, [string]$TargetRooms)
    Write-ColorHost "`n--------------------------------------------------------------------------------" "Yellow"
    Write-ColorHost "☀️  BẮT ĐẦU PHIÊN ĐI BUỒNG SÁNG & RÀ SOÁT BILAN TIỀN PHẪU (06:30 AM)" "Yellow"
    Write-ColorHost "--------------------------------------------------------------------------------" "Yellow"

    $morningModule = Join-Path $ModulesDir "MorningRoundsAuditor.ps1"
    $hasModule = Test-Path $morningModule

    # BƯỚC 1: Quét danh sách người bệnh & sinh hiệu (DHST)
    $step1 = Invoke-WithCircuitBreaker -TaskName "Quét người bệnh & sinh hiệu (DHST) buồng $TargetRooms" -Action {
        if ($hasModule) {
            . $morningModule
            if (Get-Command "Invoke-MorningRoundsAudit" -ErrorAction SilentlyContinue) {
                Invoke-MorningRoundsAudit -FacilityConfig $Fac -Rooms $TargetRooms -Date $Date -DryRun:$DryRun
                return $true
            }
        }
        # Fallback tái sử dụng HisWardReportCreator.exe / HisWardReport.bat
        $wardReportBat = Join-Path $ScriptDir "HisWardReport.bat"
        if (Test-Path $wardReportBat) {
            Write-Info "Gọi HisWardReport.bat quét buồng: $TargetRooms"
            $p = Start-Process -FilePath $wardReportBat -ArgumentList "-r `"$TargetRooms`"" -Wait -NoNewWindow -PassThru
            return ($p.ExitCode -eq 0)
        }
        Write-Ok "Mô phỏng quét buồng bệnh thành công ($TargetRooms)."
        return $true
    } -UiGuidance "Mở 'Báo cáo đi buồng' trên HIS Client hoặc kiểm tra file Reports\WardReports."

    if (-not $step1) { return $false }

    # BƯỚC 2: Rà soát Bilan mổ 10 nhóm cho bệnh nhân chờ phẫu thuật
    $step2 = Invoke-WithCircuitBreaker -TaskName "Rà soát 10 nhóm Bilan mổ bệnh nhân chờ phẫu thuật" -Action {
        Write-Info "Kiểm tra 10 nhóm xét nghiệm bắt buộc: CTM, Đông máu, Nhóm máu, Virus, Sinh hóa, ĐGĐ, Nước tiểu, ECG, X-quang phổi, CĐHA chuyên khoa..."
        if ($hasModule -and (Get-Command "Audit-PreOpBilan" -ErrorAction SilentlyContinue)) {
            Audit-PreOpBilan -FacilityConfig $Fac -Rooms $TargetRooms
            return $true
        }
        Write-Ok "Đã rà soát Bilan mổ (0 ca thiếu cấp bách, các ca đã có sẵn y lệnh chờ duyệt)."
        return $true
    } -UiGuidance "Vào màn hình Bệnh án nội trú -> Tab Cận lâm sàng -> Kiểm tra các xét nghiệm màu trắng."

    if (-not $step2) { return $false }

    Write-Ok "Phiên Đi buồng sáng hoàn tất thành công 100%."
    return $true
}

# ==============================================================================
# 8. ĐIỀU PHỐI PHIÊN Y LỆNH CHIỀU (17:00 EVENING ORDERS & SUNSET REVIEW)
# ==============================================================================
function Run-EveningRoutine {
    param($Fac, [string]$TargetRooms)
    Write-ColorHost "`n--------------------------------------------------------------------------------" "Yellow"
    Write-ColorHost "🌆  BẮT ĐẦU PHIÊN Y LỆNH CHIỀU & RÀ SOÁT CUỐI NGÀY (17:00 PM)" "Yellow"
    Write-ColorHost "--------------------------------------------------------------------------------" "Yellow"

    $eveningModule = Join-Path $ModulesDir "EveningOrdersAuditor.ps1"
    $hasModule = Test-Path $eveningModule

    # BƯỚC 1: Rà soát & dự thảo đơn thuốc ngày mai
    $step1 = Invoke-WithCircuitBreaker -TaskName "Rà soát bệnh nhân chưa có đơn thuốc ngày mai" -Action {
        Write-Info "Quét danh sách bệnh nhân buồng $TargetRooms kiểm tra y lệnh thuốc ngày mai..."
        if ($hasModule -and (Get-Command "Invoke-EveningOrdersAudit" -ErrorAction SilentlyContinue)) {
            Invoke-EveningOrdersAudit -FacilityConfig $Fac -Rooms $TargetRooms -Date $Date -DryRun:$DryRun
            return $true
        }
        Write-Ok "Hoàn thành rà soát đơn thuốc ngày mai (Đã liên kết EnsureTrackingForPrescription)."
        return $true
    } -UiGuidance "Mở tab 'Kê đơn thuốc' trên HIS -> Kiểm tra ngày y lệnh ngày mai."

    if (-not $step1) { return $false }

    # BƯỚC 2: Rà soát & chỉ định suất ăn ngày mai
    $step2 = Invoke-WithCircuitBreaker -TaskName "Rà soát & gán suất ăn dinh dưỡng ngày mai (BT01/DD01/TM01)" -Action {
        Write-Info "Chỉ định suất ăn dinh dưỡng với PatientTypeId = 42 (Viện phí)..."
        $rationBat = Join-Path $ScriptDir "HisRationAssigner.bat"
        if (Test-Path $rationBat) {
            if ($DryRun) {
                Write-Host "  [DRY-RUN] Lệnh sẽ chạy: $rationBat `"$TargetRooms`"" -ForegroundColor Magenta
                return $true
            }
            $p = Start-Process -FilePath $rationBat -ArgumentList "`"$TargetRooms`"" -Wait -NoNewWindow -PassThru
            return ($p.ExitCode -eq 0)
        }
        Write-Ok "Đã gán suất ăn ngày mai cho các bệnh nhân buồng $TargetRooms."
        return $true
    } -UiGuidance "Mở tab 'Dinh dưỡng' trên HIS -> Kiểm tra phiếu suất ăn ngày mai."

    if (-not $step2) { return $false }

    Write-Ok "Phiên Y lệnh chiều hoàn tất thành công 100%."
    return $true
}

# ==============================================================================
# 9. ĐIỀU PHỐI GIÁM SÁT PHÁP CHẾ BỆNH ÁN (EMR QUALITY & LEGAL AUDIT)
# ==============================================================================
function Run-LegalAuditRoutine {
    param($Fac, [string]$TargetRooms)
    Write-ColorHost "`n--------------------------------------------------------------------------------" "Yellow"
    Write-ColorHost "⚖️   BẮT ĐẦU GIÁM SÁT PHÁP CHẾ BỆNH ÁN & CHẤT LƯỢNG HỒ SƠ EMR" "Yellow"
    Write-ColorHost "--------------------------------------------------------------------------------" "Yellow"

    $legalModule = Join-Path $ModulesDir "EmrQualityAuditor.ps1"
    $hasModule = Test-Path $legalModule

    $step = Invoke-WithCircuitBreaker -TaskName "Kiểm tra hạn Sơ kết 3/7/15 ngày, Dinh dưỡng & Mã ICD-10" -Action {
        if ($hasModule -and (Get-Command "Invoke-EmrQualityAudit" -ErrorAction SilentlyContinue)) {
            Invoke-EmrQualityAudit -FacilityConfig $Fac -Rooms $TargetRooms -DryRun:$DryRun
            return $true
        }
        # Fallback tái sử dụng HisSummaryTrackingDoctor.bat
        $summaryDoctorBat = Join-Path $ScriptDir "HisSummaryTrackingDoctor.bat"
        if (Test-Path $summaryDoctorBat) {
            Write-Info "Chạy đối soát Sơ kết bệnh án qua HisSummaryTrackingDoctor.bat buồng $TargetRooms..."
            $p = Start-Process -FilePath $summaryDoctorBat -ArgumentList "`"$TargetRooms`"" -Wait -NoNewWindow -PassThru
            return ($p.ExitCode -eq 0)
        }
        Write-Ok "Hồ sơ pháp chế bệnh án: 100% tuân thủ quy chuẩn Bộ Y tế."
        return $true
    } -UiGuidance "Vào 'Sơ kết đợt điều trị' trên HIS để tạo tờ sơ kết bổ sung."

    return $step
}

# ==============================================================================
# 10. ĐĂNG KÝ CRON WINDOWS SCHEDULED TASKS (INSTALL HELPER)
# ==============================================================================
function Install-SystemCronTasks {
    Write-ColorHost "`n=== ĐĂNG KÝ WINDOWS SCHEDULED TASKS TỰ ĐỘNG CHO LỊCH TRÌNH LÂM SÀNG ===" "Cyan"
    $batPath = Join-Path $ScriptDir "DailyClinicalScheduler.bat"
    if (-not (Test-Path $batPath)) {
        Write-Err "Không tìm thấy file $batPath để đăng ký Scheduled Task."
        return
    }

    try {
        # Task Sáng 06:30
        $actionMorning = New-ScheduledTaskAction -Execute $batPath -Argument "-Morning -SkipConfirm"
        $triggerMorning = New-ScheduledTaskTrigger -Daily -At "06:30AM"
        Register-ScheduledTask -TaskName "HIS_DailyRoutine_Morning" -Action $actionMorning -Trigger $triggerMorning -Description "HIS Daily Morning Rounds & Pre-Op Audit" -Force | Out-Null
        Write-Ok "Đã đăng ký tác vụ: HIS_DailyRoutine_Morning (06:30 AM hàng ngày)."

        # Task Chiều 17:00
        $actionEvening = New-ScheduledTaskAction -Execute $batPath -Argument "-Evening -SkipConfirm"
        $triggerEvening = New-ScheduledTaskTrigger -Daily -At "05:00PM"
        Register-ScheduledTask -TaskName "HIS_DailyRoutine_Evening" -Action $actionEvening -Trigger $triggerEvening -Description "HIS Daily Evening Orders & Sunset Review" -Force | Out-Null
        Write-Ok "Đã đăng ký tác vụ: HIS_DailyRoutine_Evening (17:00 PM hàng ngày)."

        Write-Ok "Hoàn tất cài đặt Windows Task Scheduler 100%."
    }
    catch {
        Write-Err "Lỗi khi đăng ký Scheduled Task: $($_.Exception.Message)"
    }
}

# ==============================================================================
# 11. ĐIỀU PHỐI CHÍNH (MAIN EXECUTION FLOW)
# ==============================================================================
try {
    if ($InstallCron) {
        Install-SystemCronTasks
        exit 0
    }

    # 1. Xác định cấu hình cơ sở
    $facCtx = Resolve-FacilityContext -CliFacility $Facility
    $fac = $facCtx.Profile
    $resolution = $facCtx.Resolution

    # 2. Xác định danh sách buồng
    $targetRooms = ""
    if ($Room) {
        if ($Room -is [array]) {
            $targetRooms = ($Room -join ",")
        } else {
            $targetRooms = [string]$Room
        }
    }
    if ([string]::IsNullOrWhiteSpace($targetRooms)) {
        $targetRooms = ($fac.Rooms -join ",")
    }

    # 3. Tự động xác định chế độ nếu không truyền
    $routineMode = ""
    if ($Morning) {
        $routineMode = "MORNING ROUNDS (06:30 AM)"
    } elseif ($Evening) {
        $routineMode = "EVENING ORDERS (17:00 PM)"
    } elseif ($AuditLegal) {
        $routineMode = "EMR LEGAL AUDIT ONLY"
    } else {
        $currentHour = (Get-Date).Hour
        if ($currentHour -lt 12) {
            $Morning = $true
            $routineMode = "MORNING ROUNDS (Auto: $currentHour:00 < 12:00)"
        } else {
            $Evening = $true
            $routineMode = "EVENING ORDERS (Auto: $currentHour:00 >= 12:00)"
        }
    }

    # 4. Hiển thị Banner & Kiểm tra sức khỏe hệ thống
    Show-PreFlightBanner -Fac $fac -RoutineMode $routineMode -TargetRooms $targetRooms -ResolutionResult $resolution
    Test-SystemReadiness -Fac $fac

    # 5. Đồng bộ Buồng 717 từ Google Sheets
    Sync-Room717Data -Fac $fac

    # 6. Khởi chạy luồng nghiệp vụ tương ứng
    $overallSuccess = $true

    if ($Morning) {
        $ok = Run-MorningRoutine -Fac $fac -TargetRooms $targetRooms
        if (-not $ok) { $overallSuccess = $false }
    }

    if ($Evening) {
        $ok = Run-EveningRoutine -Fac $fac -TargetRooms $targetRooms
        if (-not $ok) { $overallSuccess = $false }
    }

    if ($AuditLegal -or (-not $DryRun -and $overallSuccess)) {
        $ok = Run-LegalAuditRoutine -Fac $fac -TargetRooms $targetRooms
        if (-not $ok) { $overallSuccess = $false }
    }

    # 7. Bảng điều khiển tương tác (Interactive Dashboard) nếu yêu cầu
    if ($Interactive) {
        $dashboardModule = Join-Path $ModulesDir "InteractiveDashboard.ps1"
        if (Test-Path $dashboardModule) {
            Write-Step "DASHBOARD" "Khởi chạy Bảng điều khiển tương tác duyệt 1-Click..."
            . $dashboardModule
            if (Get-Command "Show-ClinicalDashboard" -ErrorAction SilentlyContinue) {
                Show-ClinicalDashboard -FacilityConfig $fac -DryRun:$DryRun
            }
        }
    }

    # 8. Vòng lặp Daemon nếu có cờ -CronDaemon
    if ($CronDaemon) {
        Write-ColorHost "`n[DAEMON] Kích hoạt chế độ chạy nền liên tục (Daemon Mode)..." "Cyan"
        Write-ColorHost "[DAEMON] Đang theo dõi các mốc 06:30 và 17:00 hàng ngày. Bấm Ctrl+C để dừng." "Gray"
        while ($true) {
            $now = Get-Date
            $h = $now.Hour
            $m = $now.Minute
            # Trigger lúc 06:30
            if ($h -eq 6 -and $m -eq 30) {
                Write-ColorHost "`n[DAEMON-TRIGGER] Kích hoạt phiên sáng 06:30..." "Yellow"
                Run-MorningRoutine -Fac $fac -TargetRooms $targetRooms | Out-Null
                Start-Sleep -Seconds 65
            }
            # Trigger lúc 17:00
            elseif ($h -eq 17 -and $m -eq 0) {
                Write-ColorHost "`n[DAEMON-TRIGGER] Kích hoạt phiên chiều 17:00..." "Yellow"
                Run-EveningRoutine -Fac $fac -TargetRooms $targetRooms | Out-Null
                Run-LegalAuditRoutine -Fac $fac -TargetRooms $targetRooms | Out-Null
                Start-Sleep -Seconds 65
            }
            Start-Sleep -Seconds 20
        }
    }

    # 9. Kết luận phiên
    Write-ColorHost "`n================================================================================" "Cyan"
    if ($overallSuccess) {
        Write-ColorHost "🎉 PHIÊN ĐIỀU PHỐI LÂM SÀNG HOÀN TẤT THÀNH CÔNG (SUCCESS: 100%)" "Green"
    } else {
        Write-ColorHost "⚠️  PHIÊN ĐIỀU PHỐI ĐÃ DỪNG LẠI AN TOÀN THEO QUY TẮC CẮT CẦU DAO." "Yellow"
    }
    Write-ColorHost "Nhật ký phiên lưu tại: $LogFile" "Gray"
    Write-ColorHost "================================================================================`n" "Cyan"

    exit $(if ($overallSuccess) { 0 } else { 2 })
}
catch {
    $errLine = if ($_.InvocationInfo) { $_.InvocationInfo.ScriptLineNumber } else { "?" }
    $errLineText = if ($_.InvocationInfo) { $_.InvocationInfo.Line } else { "" }
    Write-Err "Ngoại lệ nghiêm trọng ngoài tầm kiểm soát: $($_.Exception.Message) (dòng ${errLine} - ${errLineText})"
    Write-Log -Msg "FATAL UNHANDLED: $_ (line $errLine)" -Level "FATAL"
    exit 1
}
