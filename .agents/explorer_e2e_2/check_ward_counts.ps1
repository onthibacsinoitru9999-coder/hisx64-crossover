$lines = Get-Content 'Reports\WardReports\BaoCao_BuongBenh_MoiNhat.csv' -Encoding UTF8
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

    $report = @()
    foreach ($line in $lines) {
        if ($line -match "^(\d+),([^,]+),([^,]+),'(\d{10}),'(\d+),([^,]+),") {
            $stt = $matches[1]
            $room = $matches[2].Trim()
            $mabn = $matches[4].Trim()
            $name = $matches[6].Trim()

            $patientCode = 'VS.' + $mabn.PadLeft(10, '0')
            $url = "$risUrl/rest/study?status=all&pid=$patientCode&dateFrom=$dateFrom&dateTo=$dateTo"
            $raw = (curl.exe -s -b $cookieFile $url) -join "`n"
            $json = $raw | ConvertFrom-Json
            $count = 0
            if ($json -and $json.results) { $count = $json.results.Count }

            $report += [PSCustomObject]@{
                "STT" = $stt
                "MaBN" = $mabn
                "Name" = $name
                "Room" = $room
                "StudyCount" = $count
            }
        }
    }

    $report | Format-Table -AutoSize
    $zeroList = $report | Where-Object { $_.StudyCount -eq 0 }
    Write-Host "`nPatients with ZERO studies in last 60 days: $($zeroList.Count)" -ForegroundColor Yellow
    if ($zeroList.Count -gt 0) {
        $zeroList | Format-Table -AutoSize
    }
} finally {
    if (Test-Path $cookieFile) {
        Remove-Item -Force $cookieFile -ErrorAction SilentlyContinue
    }
}
