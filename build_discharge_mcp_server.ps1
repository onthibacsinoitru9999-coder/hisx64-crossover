$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
if (-not $rootDir) { $rootDir = (Get-Location).Path }

Write-Host "=== BIEN DICH HIS DISCHARGE MCP SERVER (THO LAM RA VIEN) ===" -ForegroundColor Cyan

# Terminate running instance if any
Get-Process HisDischargeMcpServer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

# Resolve csc.exe
$csc = if ($env:CSC -and (Test-Path $env:CSC)) {
    $env:CSC
} elseif (Test-Path "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe") {
    "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
} elseif (Test-Path "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe") {
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
} else {
    throw "64-bit csc.exe not found! Please check .NET Framework 4.8 installation."
}
Write-Host "Compiler: $csc" -ForegroundColor Green

# Locate Newtonsoft.Json.dll
$jsonDll = Join-Path $rootDir "ReferencedAssemblies\Newtonsoft.Json.dll"
if (-not (Test-Path $jsonDll)) {
    $jsonDll = Join-Path $rootDir "Integrate\EMR\Newtonsoft.Json.dll"
}

# Files
$srcFile = Join-Path $rootDir "HisDischargeMcpServer.cs"
$outExe = Join-Path $rootDir "HisDischargeMcpServer.exe"
$rspFile = Join-Path $env:TEMP "his_discharge_mcp_build.rsp"

$lines = @(
    "/target:exe",
    "/platform:x64",
    "/nologo",
    "/utf8output",
    "/out:`"$outExe`"",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Data.dll",
    "/reference:`"$jsonDll`""
)

$addedNames = @{}
$addedNames['System'] = $true
$addedNames['System.Core'] = $true
$addedNames['System.Data'] = $true
$addedNames['Newtonsoft.Json'] = $true

$refDirs = @(
    (Join-Path $rootDir "ReferencedAssemblies"),
    (Join-Path $rootDir "HisAutoPrescribe_Portable")
)

foreach ($d in $refDirs) {
    if (Test-Path $d) {
        Get-ChildItem $d -Depth 0 -Filter '*.dll' | ForEach-Object {
            $asmName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            if (-not $addedNames.ContainsKey($asmName)) {
                try {
                    [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
                    $lines += "/reference:`"$($_.FullName)`""
                    $addedNames[$asmName] = $true
                } catch { }
            }
        }
    }
}

$lines += "`"$srcFile`""
$lines | Set-Content -Path $rspFile -Encoding UTF8

Write-Host "Compiling HisDischargeMcpServer.exe..." -ForegroundColor Cyan
& $csc "@$rspFile" 2>&1 | Tee-Object -Variable output

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[OK] BUILD THANH CONG: $outExe" -ForegroundColor Green
    $info = Get-Item $outExe
    Write-Host "   Size: $([math]::Round($info.Length/1024)) KB | Time: $($info.LastWriteTime)" -ForegroundColor Green

    # Sync to scripts dir if exists
    $scriptsDir = Join-Path $rootDir ".agents\skills\his-clinical-operations\scripts"
    if (Test-Path $scriptsDir) {
        Copy-Item -Force $outExe (Join-Path $scriptsDir "HisDischargeMcpServer.exe")
        Write-Host "   Dong bo vao: $(Join-Path $scriptsDir 'HisDischargeMcpServer.exe')" -ForegroundColor Green
    }
} else {
    Write-Host "`n[FAIL] BUILD THAT BAI!" -ForegroundColor Red
    $output | Where-Object { $_ -match 'error' } | Select-Object -First 20
    exit 1
}
