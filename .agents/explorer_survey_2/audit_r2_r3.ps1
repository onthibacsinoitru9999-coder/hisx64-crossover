# PowerShell audit script for R2 and R3
$ErrorActionPreference = 'SilentlyContinue'
$root = "f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB"

Write-Output "=== 1. CSC.EXE LOCATIONS AND VERSIONS ==="
$cscPaths = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
)
foreach ($p in $cscPaths) {
    if (Test-Path $p) {
        $ver = (Get-Item $p).VersionInfo.ProductVersion
        Write-Output "FOUND: $p (Version: $ver)"
    } else {
        Write-Output "MISSING: $p"
    }
}
$whereCsc = (where.exe csc 2>$null)
Write-Output "PATH csc: $whereCsc"

Write-Output "`n=== 2. TARGET TOOLS AUDIT (ROOT vs SCRIPTS) ==="
$targets = @(
    "HisClinicalCli",
    "HisTrackingCreator",
    "HisAutoPrescribe",
    "HisGlucoseBedsideAssigner",
    "HisRationAssigner",
    "HisDebateCreator",
    "HisDiagnosticDoctor",
    "HisWardReportCreator",
    "HisLeanproAssigner",
    "HisSummaryTrackingCreator",
    "HisSummaryTrackingDoctor",
    "HisDressingOrder",
    "HospitalShiftReporter",
    "HisClsCtchTracker"
)

foreach ($t in $targets) {
    $csRoot = Join-Path $root "$t.cs"
    $exeRoot = Join-Path $root "$t.exe"
    $cfgRoot = Join-Path $root "$t.exe.config"
    $batRoot = Join-Path $root "$t.bat"

    $scriptsDir = Join-Path $root ".agents\skills\his-clinical-operations\scripts"
    $csScript = Join-Path $scriptsDir "$t.cs"
    $exeScript = Join-Path $scriptsDir "$t.exe"
    $cfgScript = Join-Path $scriptsDir "$t.exe.config"

    Write-Output "--- Target: $t ---"
    if (Test-Path $csRoot) {
        $item = Get-Item $csRoot
        Write-Output "  [Root CS]   Size: $($item.Length) | Modified: $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    } else {
        Write-Output "  [Root CS]   NOT FOUND"
    }
    if (Test-Path $exeRoot) {
        $item = Get-Item $exeRoot
        Write-Output "  [Root EXE]  Size: $($item.Length) | Modified: $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    } else {
        Write-Output "  [Root EXE]  NOT FOUND"
    }
    if (Test-Path $csScript) {
        $item = Get-Item $csScript
        Write-Output "  [Script CS]  Size: $($item.Length) | Modified: $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    } else {
        Write-Output "  [Script CS]  NOT FOUND"
    }
    if (Test-Path $exeScript) {
        $item = Get-Item $exeScript
        Write-Output "  [Script EXE] Size: $($item.Length) | Modified: $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    } else {
        Write-Output "  [Script EXE] NOT FOUND"
    }
}

Write-Output "`n=== 3. REFERENCED ASSEMBLIES AUDIT ==="
$refDir = Join-Path $root "ReferencedAssemblies"
if (Test-Path $refDir) {
    $dlls = Get-ChildItem -Path $refDir -Filter *.dll
    Write-Output "Total DLLs in ReferencedAssemblies: $($dlls.Count)"
    foreach ($d in $dlls | Select-Object -First 10) {
        Write-Output "  $($d.Name) ($($d.Length) bytes)"
    }
    if ($dlls.Count -gt 10) {
        Write-Output "  ... and $($dlls.Count - 10) more DLLs"
    }
} else {
    Write-Output "ReferencedAssemblies DIR NOT FOUND at $refDir"
}

Write-Output "`n=== 4. REFS.RSP AND BUILD SCRIPTS ==="
$rspFiles = Get-ChildItem -Path $root -Recurse -Filter "*.rsp"
foreach ($r in $rspFiles) {
    Write-Output "RSP file: $($r.FullName) ($($r.Length) bytes)"
}

$buildFiles = Get-ChildItem -Path $root -Recurse -Filter "*build*.*" | Where-Object { $_.Extension -in @('.bat','.ps1','.cmd') }
foreach ($b in $buildFiles) {
    Write-Output "Build script: $($b.FullName)"
}
