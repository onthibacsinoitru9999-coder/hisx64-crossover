$ErrorActionPreference = 'Stop'
$rootDir    = $PSScriptRoot
$portDir    = Join-Path $rootDir 'HisAutoPrescribe_Portable'
$scriptDir  = Join-Path $rootDir '.agents\skills\his-clinical-operations\scripts'
$srcFile    = Join-Path $scriptDir 'HisClinicalCli.cs'
$outExe     = Join-Path $scriptDir 'HisClinicalCli.exe'
$cscPath    = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$rspFile    = Join-Path $env:TEMP 'his_clinicalcli_build.rsp'

Write-Host "=== Build HisClinicalCli.exe ===" -ForegroundColor Cyan

$lines = @(
    "/target:exe",
    "/platform:x64",
    "/out:`"$outExe`"",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Data.dll",
    "/reference:System.Xml.dll",
    "/reference:System.Net.Http.dll"
)

$addedNames = @{}
$refDir = Join-Path $rootDir 'ReferencedAssemblies'
$searchDirs = @($portDir, $refDir, $rootDir)
foreach ($d in $searchDirs) {
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
Write-Host "  Tong refs: $($lines.Count - 8) DLL"

$lines += "`"$srcFile`""
$lines | Set-Content -Path $rspFile -Encoding UTF8
Write-Host "Response file: $rspFile ($($lines.Count) dòng)"

Write-Host "Compiling..." -ForegroundColor Yellow
& $cscPath "@$rspFile" 2>&1 | Tee-Object -Variable output

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[OK] BUILD THANH CONG: $outExe" -ForegroundColor Green
    Copy-Item -Force $outExe (Join-Path $rootDir 'HisClinicalCli.exe')
    Write-Host "   Da dong bo vao thu muc goc: $(Join-Path $rootDir 'HisClinicalCli.exe')" -ForegroundColor Green
    $exeInfo = Get-Item $outExe
    Write-Host "   Size: $([math]::Round($exeInfo.Length/1024)) KB | Time: $($exeInfo.LastWriteTime)"
} else {
    Write-Host "`n[FAIL] BUILD THAT BAI! Loi bien dich:" -ForegroundColor Red
    $output | Where-Object { $_ -match 'error' } | Select-Object -First 20
}
