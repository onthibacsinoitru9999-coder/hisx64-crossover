$lines = Get-Content 'Reports\WardReports\BaoCao_BuongBenh_MoiNhat.csv' -Encoding UTF8

$allMabn = @()
$patientNames = @{}
foreach ($line in $lines) {
    if ($line -match "'(\d{10})'") {
        $m = $matches[1]
        if (-not $allMabn.Contains($m)) {
            $allMabn += $m
        }
    }
}
Write-Host "Found $($allMabn.Count) unique patient codes from ward report." -ForegroundColor Cyan

$risUrl = 'http://192.168.200.110/ris'
$cookieFile = [System.IO.Path]::GetTempFileName()

try {
    $loginPageRaw = curl.exe -s -c $cookieFile "$risUrl/account/login"
    $loginPage = ($loginPageRaw -join "`n")
    $validKey = ''
    if ($loginPage -match "'validKey':\s*""([^""]+)""") {
        $validKey = $matches[1]
    }

    $null = curl.exe -s -b $cookieFile -c $cookieFile -d "account=ctch&password=ctchCS2026%21&isLocal=true&validKey=$validKey" "$risUrl/account/login"

    $now = Get-Date
    $dateFrom = $now.AddDays(-60).ToString('yyyy-M-d')
    $dateTo = $now.AddDays(1).ToString('yyyy-M-d')

    $withStudies = @()
    $withoutStudies = @()

    foreach ($mabn in $allMabn) {
        $patientCode = 'VS.' + $mabn.PadLeft(10, '0')
        $url = "$risUrl/rest/study?status=all&pid=$patientCode&dateFrom=$dateFrom&dateTo=$dateTo"
        $raw = (curl.exe -s -b $cookieFile $url) -join "`n"
        $json = $raw | ConvertFrom-Json
        $count = 0
        if ($json -and $json.results) { $count = $json.results.Count }
        
        $name = $patientNames[$mabn]

        if ($count -gt 0) {
            $modalities = ($json.results | ForEach-Object { "$($_.modalityName) ($($_.pacsAE))" }) -join "; "
            $withStudies += [PSCustomObject]@{
                "MaBN" = $mabn
                "Name" = $name
                "StudyCount" = $count
                "Modalities" = $modalities
            }
        } else {
            $withoutStudies += [PSCustomObject]@{
                "MaBN" = $mabn
                "Name" = $name
                "StudyCount" = 0
            }
        }
    }

    Write-Host "`n=== BENH NHAN CO CA CHUP TREN RIS/PACS ($($withStudies.Count)) ===" -ForegroundColor Green
    $withStudies | Format-Table -AutoSize

    Write-Host "`n=== BENH NHAN KHONG CO CA CHUP TRONG 60 NGAY ($($withoutStudies.Count)) ===" -ForegroundColor Yellow
    $withoutStudies | Format-Table -AutoSize

} finally {
    if (Test-Path $cookieFile) {
        Remove-Item -Force $cookieFile -ErrorAction SilentlyContinue
    }
}
