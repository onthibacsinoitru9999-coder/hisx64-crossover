$codes = @(
    "0004062838",
    "0001530113",
    "0004079114",
    "0004050211",
    "0004089090",
    "0002840646",
    "0004081634",
    "0003837595",
    "0004079203",
    "0004067971",
    "0003863470",
    "0002740977"
)

foreach ($c in $codes) {
    Write-Host "======================== PATIENT $c ========================"
    $lines = & ".\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe" orders $c
    $found = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "30/09 06:00" -or $lines[$i] -match "Loại: Suất ăn") {
            Write-Host $lines[$i]
            if ($i + 1 -lt $lines.Count) { Write-Host $lines[$i+1] }
            if ($i + 2 -lt $lines.Count) { Write-Host $lines[$i+2] }
            $found = $true
        }
    }
    if (-not $found) {
        Write-Host "Chưa tìm thấy dòng suất ăn 30/09"
    }
}
