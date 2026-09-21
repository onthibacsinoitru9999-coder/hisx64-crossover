<#
.SYNOPSIS
    Compiles HisMcpServer.cs into HisMcpServer.exe using 64-bit csc.exe.
    Synchronizes HisMcpServer.exe to project root and .agents/skills/his-clinical-operations/scripts/
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
if (-not $rootDir) { $rootDir = (Get-Location).Path }

Write-Host "=== BIEN DICH HIS MCP SERVER (64-BIT) ===" -ForegroundColor Cyan

# 0. Terminate running HisMcpServer instance if any to release file locks
Get-Process HisMcpServer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

# 1. Resolve csc.exe
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

# 2. Locate Newtonsoft.Json.dll
$jsonDll = Join-Path $rootDir "ReferencedAssemblies\Newtonsoft.Json.dll"
if (-not (Test-Path $jsonDll)) {
    $jsonDll = Join-Path $rootDir "Integrate\EMR\Newtonsoft.Json.dll"
}
if (-not (Test-Path $jsonDll)) {
    throw "Newtonsoft.Json.dll not found in ReferencedAssemblies!"
}
Write-Host "Json Library: $jsonDll" -ForegroundColor Green

# 3. Source and Target
$srcFile = Join-Path $rootDir "HisMcpServer.cs"
$outExe = Join-Path $rootDir "HisMcpServer.exe"
$scriptsDir = Join-Path $rootDir ".agents\skills\his-clinical-operations\scripts"
$outScriptsExe = Join-Path $scriptsDir "HisMcpServer.exe"

# 4. Compile
Write-Host "Compiling HisMcpServer.exe..." -ForegroundColor Cyan
& $csc /target:exe /platform:x64 /nologo /utf8output `
    "/reference:System.dll" `
    "/reference:System.Core.dll" `
    "/reference:System.Data.dll" `
    "/reference:`"$jsonDll`"" `
    "/out:`"$outExe`"" `
    "`"$srcFile`""

if ($LASTEXITCODE -ne 0) {
    throw "Biên dịch thất bại với mã lỗi $LASTEXITCODE"
}

# 5. Sync to root and scripts dir
$rootJson = Join-Path $rootDir "Newtonsoft.Json.dll"
if (-not (Test-Path $rootJson) -and (Test-Path $jsonDll)) {
    Copy-Item -Force $jsonDll $rootJson
    Write-Host "Copied Newtonsoft.Json.dll to root" -ForegroundColor Green
}
if (Test-Path $scriptsDir) {
    Copy-Item -Force $outExe $outScriptsExe
    Write-Host "Dong bo HisMcpServer.exe -> $outScriptsExe" -ForegroundColor Green
}

$fi = Get-Item $outExe
$sizeKb = [math]::Round($fi.Length / 1024, 1)
Write-Host "[OK] BIEN DICH THANH CONG: $outExe ($sizeKb KB)" -ForegroundColor Green

# 6. Run quick self-test
Write-Host "Running self-test..." -ForegroundColor Cyan
& $outExe --test
