$ErrorActionPreference = 'Stop'
$rootDir    = 'D:\his\his-x64-28-11fix GDYK\his-x64'
$portDir    = Join-Path $rootDir 'HisAutoPrescribe_Portable'
$scriptDir  = Join-Path $rootDir '.agents\skills\his-clinical-operations\scripts'
$srcFile    = Join-Path $scriptDir 'HisAutoPrescribe.cs'
$outExe     = Join-Path $scriptDir 'HisAutoPrescribe.exe'
$cscPath    = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$rspFile    = Join-Path $env:TEMP 'his_autoprescribe_build.rsp'

Write-Host "=== Build HisAutoPrescribe.exe ===" -ForegroundColor Cyan

# Xây dựng response file (.rsp) để tránh command line quá dài
$lines = @(
    "/target:winexe",
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

# Thu thap managed DLLs - chi tu Portable folder (tranh duplicate voi root)
$addedNames = @{}
if (Test-Path $portDir) {
    Get-ChildItem $portDir -Filter '*.dll' | ForEach-Object {
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
# Them DLLs root neu chua co trong Portable
Get-ChildItem $rootDir -Depth 0 -Filter '*.dll' | ForEach-Object {
    $asmName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    if (-not $addedNames.ContainsKey($asmName)) {
        try {
            [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
            $lines += "/reference:`"$($_.FullName)`""
            $addedNames[$asmName] = $true
        } catch { }
    }
}
Write-Host "  Tong refs: $($lines.Count - 8) DLL"  # -8 = 7 sys refs + 1 target

# File nguồn
$lines += "`"$srcFile`""

# Ghi response file
$lines | Set-Content -Path $rspFile -Encoding UTF8
Write-Host "Response file: $rspFile ($($lines.Count) dòng)"

# Biên dịch
Write-Host "Compiling..." -ForegroundColor Yellow
& $cscPath "@$rspFile" 2>&1 | Tee-Object -Variable output

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[OK] BUILD THANH CONG: $outExe" -ForegroundColor Green
    $exeInfo = Get-Item $outExe
    Write-Host "   Size: $([math]::Round($exeInfo.Length/1024)) KB | Time: $($exeInfo.LastWriteTime)"
} else {
    Write-Host "`n[FAIL] BUILD THAT BAI! Loi bien dich:" -ForegroundColor Red
    $output | Where-Object { $_ -match 'error' } | Select-Object -First 20
}
