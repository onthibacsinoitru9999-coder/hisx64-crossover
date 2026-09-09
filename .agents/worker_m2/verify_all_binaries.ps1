$rootDir = (Get-Item $PSScriptRoot).Parent.Parent.FullName
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

function Test-PeX64([string]$path) {
    if (-not (Test-Path $path)) { return $false }
    try {
        $fs = [System.IO.File]::OpenRead($path)
        $br = [System.IO.BinaryReader]::new($fs)
        $mz = $br.ReadUInt16()
        if ($mz -ne 0x5A4D) { $fs.Dispose(); return $false }
        $fs.Seek(0x3C, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peOffset = $br.ReadInt32()
        $fs.Seek($peOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peSig = $br.ReadUInt32()
        if ($peSig -ne 0x00004550) { $fs.Dispose(); return $false }
        $machine = $br.ReadUInt16()
        $fs.Dispose()
        return ($machine -eq 0x8664)
    } catch {
        return $false
    }
}

Write-Host "==========================================================================================" -ForegroundColor Cyan
Write-Host " VERIFICATION MATRIX - 14 CLINICAL BINARIES (ROOT & SCRIPTS)" -ForegroundColor Cyan
Write-Host "==========================================================================================" -ForegroundColor Cyan

$allPass = $true
$table = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($t in $tools) {
    $exeName = "$t.exe"
    $rootExe = Join-Path $rootDir $exeName
    $scriptExe = Join-Path $scriptDir $exeName

    $rootExists = Test-Path $rootExe
    $scriptExists = Test-Path $scriptExe

    $rootSize = if ($rootExists) { (Get-Item $rootExe).Length } else { 0 }
    $scriptSize = if ($scriptExists) { (Get-Item $scriptExe).Length } else { 0 }

    $rootX64 = if ($rootExists) { Test-PeX64 $rootExe } else { $false }
    $scriptX64 = if ($scriptExists) { Test-PeX64 $scriptExe } else { $false }

    $status = if ($rootExists -and $scriptExists -and $rootX64 -and $scriptX64 -and ($rootSize -eq $scriptSize) -and ($rootSize -gt 0)) {
        "PASS"
    } else {
        $allPass = $false
        "FAIL"
    }

    $table.Add([PSCustomObject]@{
        ToolName   = $t
        Root       = if ($rootExists) { "YES ($rootSize B)" } else { "NO" }
        Script     = if ($scriptExists) { "YES ($scriptSize B)" } else { "NO" }
        RootArch   = if ($rootX64) { "PE x64" } else { "NOT_x64" }
        ScriptArch = if ($scriptX64) { "PE x64" } else { "NOT_x64" }
        Synced     = if ($rootSize -eq $scriptSize) { "MATCH" } else { "DIFF" }
        Verdict    = $status
    })
}

$table | Format-Table -Property ToolName, Root, Script, RootArch, ScriptArch, Synced, Verdict -AutoSize

if ($allPass) {
    Write-Host "SUCCESS: All 14 binaries verified! Present in both locations, identical sizes, PE x64 architecture." -ForegroundColor Green
    exit 0
} else {
    Write-Host "FAILURE: Verification failed for one or more binaries." -ForegroundColor Red
    exit 1
}
