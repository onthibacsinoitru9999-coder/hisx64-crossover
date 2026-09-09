# Test compilation of all target C# tools to TEMP
$ErrorActionPreference = 'SilentlyContinue'
$root = "f:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$scriptsDir = Join-Path $root ".agents\skills\his-clinical-operations\scripts"
$refDir = Join-Path $root "ReferencedAssemblies"

$rspFile = Join-Path $env:TEMP "survey2_test_refs.rsp"

# Generate complete RSP with all valid assemblies
$lines = @(
    "/target:exe",
    "/platform:x64",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Data.dll",
    "/reference:System.Xml.dll",
    "/reference:System.Net.Http.dll"
)

$added = @{}
Get-ChildItem -Path $refDir -Filter "*.dll" | ForEach-Object {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    if (-not $added.ContainsKey($name)) {
        try {
            [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
            $lines += "/reference:`"$($_.FullName)`""
            $added[$name] = $true
        } catch {}
    }
}
# Also check root DLLs
Get-ChildItem -Path $root -Filter "*.dll" | ForEach-Object {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    if (-not $added.ContainsKey($name)) {
        try {
            [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
            $lines += "/reference:`"$($_.FullName)`""
            $added[$name] = $true
        } catch {}
    }
}

$lines | Set-Content -Path $rspFile -Encoding UTF8
Write-Output "Generated test RSP with $($lines.Count) lines (Refs: $($added.Count) DLLs)"

# Target tools to test compile
$tools = @(
    @{ Name = "HisLeanproAssigner"; Path = (Join-Path $root "HisLeanproAssigner.cs") },
    @{ Name = "HisClinicalCli"; Path = (Join-Path $scriptsDir "HisClinicalCli.cs") },
    @{ Name = "HisAutoPrescribe"; Path = (Join-Path $scriptsDir "HisAutoPrescribe.cs") },
    @{ Name = "HisTrackingCreator"; Path = (Join-Path $scriptsDir "HisTrackingCreator.cs") },
    @{ Name = "HisGlucoseBedsideAssigner"; Path = (Join-Path $scriptsDir "HisGlucoseBedsideAssigner.cs") },
    @{ Name = "HisDebateCreator"; Path = (Join-Path $scriptsDir "HisDebateCreator.cs") },
    @{ Name = "HisRationAssigner"; Path = (Join-Path $root "HisRationAssigner.cs") },
    @{ Name = "HisDiagnosticDoctor"; Path = (Join-Path $root "HisDiagnosticDoctor.cs") },
    @{ Name = "HisWardReportCreator"; Path = (Join-Path $root "HisWardReportCreator.cs") },
    @{ Name = "HisSummaryTrackingCreator"; Path = (Join-Path $root "HisSummaryTrackingCreator.cs") },
    @{ Name = "HisSummaryTrackingDoctor"; Path = (Join-Path $root "HisSummaryTrackingDoctor.cs") },
    @{ Name = "HisDressingOrder"; Path = (Join-Path $scriptsDir "HisDressingOrder.cs") },
    @{ Name = "HospitalShiftReporter"; Path = (Join-Path $scriptsDir "HospitalShiftReporter.cs") },
    @{ Name = "HisClsCtchTracker"; Path = (Join-Path $root "HisClsCtchTracker.cs") }
)

foreach ($t in $tools) {
    $src = $t.Path
    $name = $t.Name
    if (-not (Test-Path $src)) {
        Write-Output "[$name] SOURCE NOT FOUND: $src"
        continue
    }
    $tempOut = Join-Path $env:TEMP "$name`_test.exe"
    if (Test-Path $tempOut) { Remove-Item -Force $tempOut }
    
    $out = & $csc "@$rspFile" "/out:$tempOut" "`"$src`"" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $size = (Get-Item $tempOut).Length
        Write-Output "[$name] COMPILE OK! Output size: $size bytes"
    } else {
        Write-Output "[$name] COMPILE FAILED! ExitCode: $LASTEXITCODE"
        $out | Where-Object { $_ -match 'error' -or $_ -match 'warning CS' } | Select-Object -First 10 | ForEach-Object {
            Write-Output "    $_"
        }
    }
}
