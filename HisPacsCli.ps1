<#
.SYNOPSIS
    HisPacsCli - Tra cứu ca chụp RIS/PACS và tự động tạo link mở Web Viewer 1-Click
.DESCRIPTION
    Kết nối hệ thống RIS Minerva (192.168.200.110), xác thực tài khoản và trích xuất
    toàn bộ ca chụp (MRI, CT, X-Quang, Siêu âm) của bệnh nhân, lấy link Web Viewer trực tiếp (192.168.200.111:8081).
.EXAMPLE
    .\HisPacsCli.ps1 -PatientId "0004009330" -Open
    .\HisPacsCli.ps1 -PatientId "0004009330"
    .\HisPacsCli.ps1 -AccessionNo "000089828831" -Open
#>
param(
    [Parameter(Position = 0)]
    [string]$PatientId,

    [Parameter()]
    [string]$AccessionNo,

    [Parameter()]
    [string]$PatientName,

    [Parameter()]
    [switch]$Open,

    [Parameter()]
    [string]$RisHost = "192.168.200.110",

    [Parameter()]
    [string]$Account = "ctch",

    [Parameter()]
    [string]$Password = "ctchCS2026!"
)

$ErrorActionPreference = "Stop"

if (-not $PatientId -and -not $AccessionNo -and -not $PatientName) {
    Write-Host "Vui long truyen it nhat mot tham so: -PatientId <MaBN>, -AccessionNo <MaPhieu>, hoac -PatientName <TenBN>" -ForegroundColor Yellow
    exit 1
}

$risUrl = "http://$RisHost/ris"
$cookieFile = [System.IO.Path]::GetTempFileName()

try {
    # 1. Fetch validKey from login page (ensure single string for regex -match)
    $loginPageRaw = curl.exe -s -c $cookieFile "$risUrl/account/login"
    $loginPage = ($loginPageRaw -join "`n")
    $validKey = ""
    if ($loginPage -match "'validKey':\s*""([^""]+)""") {
        $validKey = $matches[1]
    }

    # 2. Login to RIS
    $null = curl.exe -s -b $cookieFile -c $cookieFile -d "account=$Account&password=$Password&isLocal=true&validKey=$validKey" "$risUrl/account/login"

    # 3. Build query params
    $queryParams = @("status=all")

    if ($PatientId) {
        $cleanPid = $PatientId.Trim()
        if ($cleanPid -notmatch "^VS\.") {
            if ($cleanPid -match "^\d+$") {
                $cleanPid = "VS." + $cleanPid.PadLeft(10, '0')
            } else {
                $cleanPid = "VS." + $cleanPid
            }
        }
        $queryParams += "pid=$cleanPid"
    }

    if ($AccessionNo) {
        $queryParams += "accessionNumber=$($AccessionNo.Trim())"
    }

    if ($PatientName) {
        $queryParams += "patientName=" + [System.Uri]::EscapeDataString($PatientName.Trim())
    }

    # Default date range: 60 days back to today
    $now = Get-Date
    $dateFrom = $now.AddDays(-60).ToString("yyyy-M-d")
    $dateTo = $now.AddDays(1).ToString("yyyy-M-d")
    $queryParams += "dateFrom=$dateFrom"
    $queryParams += "dateTo=$dateTo"

    $queryUrl = "$risUrl/rest/study?" + ($queryParams -join "&")
    $rawJson = (curl.exe -s -b $cookieFile "$queryUrl") -join "`n"

    $resp = $rawJson | ConvertFrom-Json
    if (-not $resp -or -not $resp.results -or $resp.results.Count -eq 0) {
        Write-Host "Khong tim thay ca chup nao tren RIS/PACS voi thong tin da cung cap." -ForegroundColor Yellow
        exit 0
    }

    Write-Host "`n=== KET QUA TRA CUU ANH PACS ($($resp.results.Count) ca chup) ===" -ForegroundColor Green

    foreach ($s in $resp.results) {
        $iuid = $s.studyIUID
        
        $serviceName = $s.modalityName
        $doctor = "Chua doc"
        if ($s.diagnosis) {
            $firstDiag = $null
            if ($s.diagnosis -is [System.Array] -and $s.diagnosis.Length -gt 0) {
                $firstDiag = $s.diagnosis[0]
            } elseif ($s.diagnosis.service) {
                $firstDiag = $s.diagnosis
            }
            if ($firstDiag) {
                if ($firstDiag.service -and $firstDiag.service.val) {
                    $serviceName = $firstDiag.service.val
                }
                if ($firstDiag.author -and $firstDiag.author.fullName) {
                    $doctor = $firstDiag.author.fullName
                }
            }
        }

        $pName = if ($s.patient) { $s.patient.name } else { "" }
        $pIdStr = if ($s.patient) { $s.patient.pid } else { "" }

        # Resolve direct 302 location from RIS
        $headRespRaw = curl.exe -s -i -b $cookieFile "$risUrl/viewer?study=$iuid"
        $directViewer = ""
        foreach ($line in $headRespRaw) {
            if ($line -match "location:\s*(.+)") {
                $directViewer = $matches[1].Trim()
                break
            }
        }

        [PSCustomObject]@{
            "BenhNhan"     = "$pName ($pIdStr)"
            "DichVu"       = $serviceName
            "NgayChup"     = $s.date
            "BacSiDoc"     = $doctor
            "MayChup"      = $s.modalityName
            "PacsAE"       = $s.pacsAE
            "StudyIUID"    = $iuid
            "DirectViewer" = $directViewer
        } | Format-List

        if ($Open -and $directViewer) {
            Write-Host ">> Dang mo trinh duyet: $directViewer" -ForegroundColor Cyan
            Start-Process $directViewer
            Start-Sleep -Milliseconds 500
        }
    }
}
finally {
    if (Test-Path $cookieFile) {
        Remove-Item -Force $cookieFile -ErrorAction SilentlyContinue
    }
}
