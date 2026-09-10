<#
.SYNOPSIS
    Master compilation script for all 14 clinical C# tools in HIS Automation.
.DESCRIPTION
    - Detects 64-bit csc.exe (/platform:x64)
    - Updates refs.rsp in project root and scripts folder with portable relative paths
    - Ensures essential assemblies (MOS.EFMODEL.dll, LIS.EFMODEL.dll) are referenced
    - Compiles all 14 clinical tools with exit code checking
    - Synchronizes compiled .exe binaries between project root and scripts directory
    - Validates PE x64 architecture for all binaries
#>

[CmdletBinding()]
param (
    [switch]$ForceRebuild,
    [string]$TargetTool
)

$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
if (-not $rootDir) {
    $rootDir = (Get-Location).Path
}

$refDir    = Join-Path $rootDir "ReferencedAssemblies"
$scriptDir = Join-Path $rootDir ".agents\skills\his-clinical-operations\scripts"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " HIS AUTOMATION - MASTER COMPILER (build_all_cs_tools.ps1)       " -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " Project Root: $rootDir"
Write-Host " Assembly Dir: $refDir"
Write-Host " Scripts Dir : $scriptDir`n"

# 1. Resolve 64-bit csc.exe
$csc = if ($env:CSC -and (Test-Path $env:CSC)) {
    $env:CSC
} elseif (Test-Path "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe") {
    "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
} elseif (Test-Path "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe") {
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
} else {
    throw "64-bit csc.exe not found! Please check .NET Framework 4.8 installation."
}
Write-Host "[1/5] Resolved 64-bit C# Compiler: $csc" -ForegroundColor Green

# 2. Ensure essential root DLLs are in ReferencedAssemblies
@("MOS.EFMODEL.dll", "LIS.EFMODEL.dll") | ForEach-Object {
    $srcDll = Join-Path $rootDir $_
    $dstDll = Join-Path $refDir $_
    if ((Test-Path $srcDll) -and (-not (Test-Path $dstDll))) {
        Copy-Item -Force $srcDll $dstDll
        Write-Host "  Copied $_ to ReferencedAssemblies/" -ForegroundColor Yellow
    }
}

# 3. Discover all valid .NET assemblies and build portable refs.rsp
Write-Host "[2/5] Scanning assemblies and generating portable refs.rsp..." -ForegroundColor Cyan
$stdRefs = @(
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Data.dll",
    "/reference:System.Xml.dll",
    "/reference:System.Net.Http.dll"
)

$validDlls = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$addedAsmNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

# Scan ReferencedAssemblies first, then rootDir for any unique assemblies
$searchDirs = @($refDir, $rootDir)
foreach ($dir in $searchDirs) {
    if (Test-Path $dir) {
        Get-ChildItem -Path $dir -Filter "*.dll" -File | ForEach-Object {
            $asmName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            if (-not $addedAsmNames.Contains($asmName)) {
                try {
                    [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
                    $validDlls.Add($_)
                    $addedAsmNames.Add($asmName) | Out-Null
                } catch {}
            }
        }
    }
}
Write-Host "  Found $($validDlls.Count) valid .NET assemblies." -ForegroundColor Green

# Build response files
# a. Dynamic build response file (absolute paths for rock-solid current build)
$buildRsp = Join-Path $env:TEMP "his_master_build.rsp"
$buildRspLines = [System.Collections.Generic.List[string]]::new()
$buildRspLines.Add("/target:exe")
$buildRspLines.Add("/platform:x64")
$buildRspLines.Add("/nologo")
$buildRspLines.Add("/utf8output")
foreach ($r in $stdRefs) { $buildRspLines.Add($r) }
foreach ($dll in $validDlls) {
    $buildRspLines.Add("/reference:`"$($dll.FullName)`"")
}
[System.IO.File]::WriteAllLines($buildRsp, $buildRspLines, [System.Text.Encoding]::ASCII)

# b. Portable root refs.rsp (relative to project root)
$rootRsp = Join-Path $rootDir "refs.rsp"
$rootRspLines = [System.Collections.Generic.List[string]]::new()
foreach ($r in $stdRefs) { $rootRspLines.Add($r) }
foreach ($dll in $validDlls) {
    $rel = if ($dll.DirectoryName -eq $rootDir) {
        ".\$($dll.Name)"
    } else {
        ".\ReferencedAssemblies\$($dll.Name)"
    }
    $rootRspLines.Add("/reference:`"$rel`"")
}
[System.IO.File]::WriteAllLines($rootRsp, $rootRspLines, [System.Text.Encoding]::ASCII)
Write-Host "  Updated Root refs.rsp -> $rootRsp" -ForegroundColor Green

# c. Portable scripts refs.rsp (relative to scripts directory)
$scriptsRsp = Join-Path $scriptDir "refs.rsp"
if (Test-Path $scriptDir) {
    $scriptsRspLines = [System.Collections.Generic.List[string]]::new()
    foreach ($r in $stdRefs) { $scriptsRspLines.Add($r) }
    foreach ($dll in $validDlls) {
        $rel = if ($dll.DirectoryName -eq $rootDir) {
            "..\..\..\..\$($dll.Name)"
        } else {
            "..\..\..\..\ReferencedAssemblies\$($dll.Name)"
        }
        $scriptsRspLines.Add("/reference:`"$rel`"")
    }
    [System.IO.File]::WriteAllLines($scriptsRsp, $scriptsRspLines, [System.Text.Encoding]::ASCII)
    Write-Host "  Updated Scripts refs.rsp -> $scriptsRsp" -ForegroundColor Green
}

# 4. Inventory of all 14 clinical tools
$toolList = @(
    @{ Name = "HisLeanproAssigner";        Source = "HisLeanproAssigner.cs" },
    @{ Name = "HisClinicalCli";            Source = ".agents\skills\his-clinical-operations\scripts\HisClinicalCli.cs" },
    @{ Name = "HisAutoPrescribe";          Source = ".agents\skills\his-clinical-operations\scripts\HisAutoPrescribe.cs" },
    @{ Name = "HisTrackingCreator";        Source = "HisTrackingCreator.cs" },
    @{ Name = "HisGlucoseBedsideAssigner";  Source = ".agents\skills\his-clinical-operations\scripts\HisGlucoseBedsideAssigner.cs" },
    @{ Name = "HisDebateCreator";          Source = ".agents\skills\his-clinical-operations\scripts\HisDebateCreator.cs" },
    @{ Name = "HisRationAssigner";         Source = "HisRationAssigner.cs" },
    @{ Name = "HisDiagnosticDoctor";       Source = "HisDiagnosticDoctor.cs" },
    @{ Name = "HisWardReportCreator";      Source = "HisWardReportCreator.cs" },
    @{ Name = "HisSummaryTrackingCreator"; Source = "HisSummaryTrackingCreator.cs" },
    @{ Name = "HisSummaryTrackingDoctor";  Source = "HisSummaryTrackingDoctor.cs" },
    @{ Name = "HisDressingOrder";          Source = ".agents\skills\his-clinical-operations\scripts\HisDressingOrder.cs" },
    @{ Name = "HospitalShiftReporter";     Source = ".agents\skills\his-clinical-operations\scripts\HospitalShiftReporter.cs" },
    @{ Name = "HisClsCtchTracker";         Source = "HisClsCtchTracker.cs" }
)

if ($TargetTool) {
    $toolList = @($toolList | Where-Object { $_.Name -like "*$TargetTool*" })
    if (-not $toolList -or $toolList.Count -eq 0) {
        throw "Target tool '$TargetTool' not found in clinical tools matrix!"
    }
}

Write-Host "`n[3/5] Compiling $($toolList.Count) C# Clinical Tools..." -ForegroundColor Cyan

function Test-PeX64([string]$filePath) {
    if (-not (Test-Path $filePath)) { return $false }
    try {
        $fs = [System.IO.File]::OpenRead($filePath)
        $br = [System.IO.BinaryReader]::new($fs)
        $mz = $br.ReadUInt16()
        if ($mz -ne 0x5A4D) { $fs.Dispose(); return $false }
        $fs.Seek(0x3C, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peOffset = $br.ReadInt32()
        $fs.Seek($peOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
        $peSig = $br.ReadUInt32()
        if ($peSig -ne 0x00004550) { $fs.Dispose(); return $false } # "PE\0\0"
        $machine = $br.ReadUInt16()
        $fs.Dispose()
        return ($machine -eq 0x8664) # IMAGE_FILE_MACHINE_AMD64 (x64)
    } catch {
        return $false
    }
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()
$hasErrors = $false

for ($i = 0; $i -lt $toolList.Count; $i++) {
    $item = $toolList[$i]
    $name = $item.Name
    $srcRel = $item.Source
    $num = "{0,2}" -f ($i + 1)
    
    # Locate source file (check primary path, then fallback between root and scripts)
    $srcPath = Join-Path $rootDir $srcRel
    if (-not (Test-Path $srcPath)) {
        $altPath = Join-Path $scriptDir ([System.IO.Path]::GetFileName($srcRel))
        if (Test-Path $altPath) {
            $srcPath = $altPath
        } else {
            $altRoot = Join-Path $rootDir ([System.IO.Path]::GetFileName($srcRel))
            if (Test-Path $altRoot) {
                $srcPath = $altRoot
            } else {
                Write-Host "  [$num/$($toolList.Count)] [MISSING] ${name}: SOURCE NOT FOUND ($srcRel)" -ForegroundColor Red
                $hasErrors = $true
                $results.Add([PSCustomObject]@{
                    Tool = $name
                    Status = "SOURCE_NOT_FOUND"
                    RootExe = "MISSING"
                    ScriptExe = "MISSING"
                    Arch = "N/A"
                    SizeKB = 0
                })
                continue
            }
        }
    }

    $outRoot = Join-Path $rootDir "$name.exe"
    $outScript = Join-Path $scriptDir "$name.exe"

    Write-Host "  [$num/$($toolList.Count)] Compiling $name..." -NoNewline

    $compOutput = & $csc "@$buildRsp" "/out:$outRoot" "$srcPath" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host " [FAIL]" -ForegroundColor Red
        $hasErrors = $true
        $compOutput | Where-Object { $_ -match 'error\s+CS' } | ForEach-Object {
            Write-Host "       $_" -ForegroundColor Red
        }
        $results.Add([PSCustomObject]@{
            Tool = $name
            Status = "COMPILE_FAIL"
            RootExe = "FAIL"
            ScriptExe = "FAIL"
            Arch = "N/A"
            SizeKB = 0
        })
        continue
    }

    # Synchronize binary to scripts folder
    if (Test-Path $scriptDir) {
        Copy-Item -Force $outRoot $outScript
    }

    $fileInfo = Get-Item $outRoot
    $isX64 = Test-PeX64 $outRoot
    $archStr = if ($isX64) { "x64" } else { "NOT_x64" }
    $sizeKb = [math]::Round($fileInfo.Length / 1024, 1)

    Write-Host " [OK] ($archStr, ${sizeKb} KB)" -ForegroundColor Green

    $results.Add([PSCustomObject]@{
        Tool = $name
        Status = "SUCCESS"
        RootExe = "PRESENT"
        ScriptExe = if (Test-Path $outScript) { "PRESENT" } else { "MISSING" }
        Arch = $archStr
        SizeKB = $sizeKb
    })
}

Write-Host "`n[4/5] Binary Synchronization & Verification Matrix:" -ForegroundColor Cyan
$results | Format-Table -Property Tool, Status, RootExe, ScriptExe, Arch, SizeKB -AutoSize

# 5. Health verification
Write-Host "[5/5] Checking Critical Tool Prerequisites..." -ForegroundColor Cyan
$leanproExeRoot = Join-Path $rootDir "HisLeanproAssigner.exe"
$leanproExeScript = Join-Path $scriptDir "HisLeanproAssigner.exe"
if ((Test-Path $leanproExeRoot) -and (Test-Path $leanproExeScript)) {
    Write-Host "  [OK] HisLeanproAssigner.exe successfully deployed to BOTH locations!" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] HisLeanproAssigner.exe synchronization failed!" -ForegroundColor Red
    $hasErrors = $true
}

$wardExeRoot = Join-Path $rootDir "HisWardReportCreator.exe"
if (Test-Path $wardExeRoot) {
    Write-Host "  [OK] HisWardReportCreator.exe verified and deployed!" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] HisWardReportCreator.exe missing!" -ForegroundColor Red
    $hasErrors = $true
}

if ($hasErrors) {
    Write-Host "`n==================================================================" -ForegroundColor Red
    Write-Host " BUILD FAILED: One or more clinical tools failed to compile.     " -ForegroundColor Red
    Write-Host "==================================================================" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n==================================================================" -ForegroundColor Green
    Write-Host " BUILD SUCCESS: All $($toolList.Count) clinical tools compiled cleanly (PE x64)! " -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Green
    exit 0
}
