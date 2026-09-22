$candidates = @('0004009330', '0004023255', '0004032593', '0004032715', '0004025259', '0000747808', '0004008351', '0003757502', '0004029611', '0004029076', '0003975441', '0004007655', '0003914548', '0003595506', '0004007274')

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

    $allResults = @()
    foreach ($mabn in $candidates) {
        $patientCode = 'VS.' + $mabn.PadLeft(10, '0')
        $url = "$risUrl/rest/study?status=all&pid=$patientCode&dateFrom=$dateFrom&dateTo=$dateTo"
        $raw = (curl.exe -s -b $cookieFile $url) -join "`n"
        $json = $raw | ConvertFrom-Json
        $count = 0
        if ($json -and $json.results) { $count = $json.results.Count }
        $studiesList = @()
        if ($count -gt 0) {
            foreach ($st in $json.results) {
                $diagName = $st.modalityName
                if ($st.diagnosis) {
                    if ($st.diagnosis -is [System.Array] -and $st.diagnosis.Length -gt 0 -and $st.diagnosis[0].service) {
                        $diagName = $st.diagnosis[0].service.val
                    } elseif ($st.diagnosis.service) {
                        $diagName = $st.diagnosis.service.val
                    }
                }
                $pName = if ($st.patient) { $st.patient.name } else { '' }
                $studiesList += [PSCustomObject]@{
                    "StudyIUID" = $st.studyIUID
                    "Modality" = $st.modalityName
                    "PacsAE" = $st.pacsAE
                    "Date" = $st.date
                    "PatientName" = $pName
                    "Description" = $diagName
                }
            }
        }
        $allResults += [PSCustomObject]@{
            "MaBN" = $mabn
            "PatientCode" = $patientCode
            "StudyCount" = $count
            "Studies" = $studiesList
        }
    }
    $allResults | ConvertTo-Json -Depth 5 | Set-Content -Path '.agents\explorer_e2e_2\candidates_result.json' -Encoding utf8
    Write-Host "Saved candidates_result.json successfully. Total patients queried: $($allResults.Count)" -ForegroundColor Green
} finally {
    if (Test-Path $cookieFile) {
        Remove-Item -Force $cookieFile -ErrorAction SilentlyContinue
    }
}
