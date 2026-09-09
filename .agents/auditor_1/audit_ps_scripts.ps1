# Audit PowerShell Scripts AST Parsing and UTF-8 BOM
$psFiles = Get-ChildItem -Path "." -Filter "*.ps1" -File
$results = foreach ($f in $psFiles) {
    $tokens = $null
    $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($f.FullName, [ref]$tokens, [ref]$errors)
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    [PSCustomObject]@{
        Script      = $f.Name
        HasBom      = $hasBom
        ParseErrors = $errors.Count
        Verdict     = if ($errors.Count -eq 0) { "PASS" } else { "FAIL" }
    }
}
$results | Format-Table -AutoSize
$allClean = ($results | Where-Object { $_.ParseErrors -ne 0 }).Count -eq 0
Write-Host "All PowerShell scripts parsed with 0 errors: $allClean"
if (-not $allClean) { exit 1 } else { exit 0 }
