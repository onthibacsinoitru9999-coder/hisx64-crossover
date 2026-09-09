# Audit 14 Clinical C# Binaries
$rootDir = (Resolve-Path ".").Path
$scriptDir = Join-Path $rootDir ".agents\skills\his-clinical-operations\scripts"

$tools = @(
    "HisLeanproAssigner",
    "HisClinicalCli",
    "HisAutoPrescribe",
    "HisTrackingCreator",
    "HisGlucoseBedsideAssigner",
    "HisDebateCreator",
    "HisRationAssigner",
    "HisDiagnosticDoctor",
    "HisWardReportCreator",
    "HisSummaryTrackingCreator",
    "HisSummaryTrackingDoctor",
    "HisDressingOrder",
    "HospitalShiftReporter",
    "HisClsCtchTracker"
)

function Check-Binary([string]$filePath) {
    if (-not (Test-Path $filePath)) {
        return [PSCustomObject]@{
            Exists = $false
            Size = 0
            IsPe = $false
            IsX64 = $false
            Machine = "N/A"
            Hash = "N/A"
        }
    }
    $fi = Get-Item $filePath
    $hash = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.Substring(0, 16)
    
    $fs = [System.IO.File]::OpenRead($filePath)
    $br = [System.IO.BinaryReader]::new($fs)
    $mz = $br.ReadUInt16()
    $fs.Seek(0x3C, [System.IO.SeekOrigin]::Begin) | Out-Null
    $peOffset = $br.ReadInt32()
    $fs.Seek($peOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
    $peSig = $br.ReadUInt32()
    $machine = $br.ReadUInt16()
    $fs.Dispose()

    $isPe = ($mz -eq 0x5A4D -and $peSig -eq 0x00004550)
    $isX64 = ($machine -eq 0x8664)
    $machineHex = "0x{0:X4}" -f $machine

    return [PSCustomObject]@{
        Exists = $true
        Size = $fi.Length
        IsPe = $isPe
        IsX64 = $isX64
        Machine = $machineHex
        Hash = $hash
    }
}

$results = foreach ($t in $tools) {
    $rootPath = Join-Path $rootDir "$t.exe"
    $scriptPath = Join-Path $scriptDir "$t.exe"
    
    $r = Check-Binary $rootPath
    $s = Check-Binary $scriptPath
    
    $synced = ($r.Exists -and $s.Exists -and ($r.Hash -eq $s.Hash))
    $valid = ($r.Exists -and $r.IsPe -and $r.IsX64 -and $s.Exists -and $s.IsPe -and $s.IsX64 -and $synced)

    [PSCustomObject]@{
        Tool         = $t
        RootExists   = $r.Exists
        RootX64      = $r.IsX64
        RootSize     = $r.Size
        ScriptExists = $s.Exists
        ScriptX64    = $s.IsX64
        ScriptSize   = $s.Size
        HashMatch    = $synced
        Verdict      = if ($valid) { "PASS" } else { "FAIL" }
    }
}

$results | Format-Table -AutoSize

$allPass = ($results | Where-Object { $_.Verdict -ne "PASS" }).Count -eq 0
Write-Host "`nAll 14 binaries verified PE x64 and synchronized: $allPass"
if (-not $allPass) { exit 1 } else { exit 0 }
