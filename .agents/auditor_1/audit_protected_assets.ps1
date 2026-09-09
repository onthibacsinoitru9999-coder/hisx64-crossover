# Audit Protected Assets
$rootDir = (Resolve-Path ".").Path

# 1. ConfigSystem.xml
$cfgPath = Join-Path $rootDir "ConfigSystem.xml"
$cfgExists = Test-Path $cfgPath
$cfgValidXml = $false
$cfgSize = 0
if ($cfgExists) {
    $cfgSize = (Get-Item $cfgPath).Length
    try {
        [xml]$xml = Get-Content $cfgPath -Raw
        $cfgValidXml = ($xml.DocumentElement -ne $null)
    } catch {
        $cfgValidXml = $false
    }
}

# 2. ReferencedAssemblies/
$refDir = Join-Path $rootDir "ReferencedAssemblies"
$refExists = Test-Path $refDir
$dllCount = 0
$criticalMissing = @()
if ($refExists) {
    $dllFiles = Get-ChildItem -Path $refDir -Filter "*.dll" -File
    $dllCount = $dllFiles.Count
    $criticalDlls = @("MOS.EFMODEL.dll", "LIS.EFMODEL.dll", "Newtonsoft.Json.dll", "Inventec.Core.dll", "HIS.Desktop.LocalStorage.ConfigSystem.dll", "HIS.Desktop.LocalStorage.LocalData.dll")
    foreach ($c in $criticalDlls) {
        $cPath = Join-Path $refDir $c
        if (-not (Test-Path $cPath)) {
            $criticalMissing += $c
        }
    }
}

# 3. Logs/ and Logs/LogSystem.txt
$logDir = Join-Path $rootDir "Logs"
$logPath = Join-Path $logDir "LogSystem.txt"
$logExists = Test-Path $logPath
$logReadable = $false
$logSize = 0
if ($logExists) {
    $logSize = (Get-Item $logPath).Length
    try {
        $fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $logReadable = ($fs.Length -gt 0)
        $fs.Dispose()
    } catch {
        $logReadable = $false
    }
}

# 4. *.exe.config files
$configFiles = Get-ChildItem -Path $rootDir -Filter "*.exe.config" -Recurse -File
$configCount = $configFiles.Count
$corruptedConfigs = @()
foreach ($f in $configFiles) {
    try {
        [xml]$x = Get-Content $f.FullName -Raw
    } catch {
        $corruptedConfigs += $f.FullName
    }
}

$report = [PSCustomObject]@{
    ConfigSystem_Exists    = $cfgExists
    ConfigSystem_ValidXml  = $cfgValidXml
    ConfigSystem_SizeBytes = $cfgSize
    RefAssemblies_Exists   = $refExists
    RefAssemblies_DllCount = $dllCount
    RefAssemblies_Missing  = ($criticalMissing -join ", ")
    LogSystem_Exists       = $logExists
    LogSystem_Readable     = $logReadable
    LogSystem_SizeBytes    = $logSize
    ExeConfig_TotalCount   = $configCount
    ExeConfig_Corrupted    = $corruptedConfigs.Count
}

$report | Format-List

$clean = ($cfgExists -and $cfgValidXml -and $refExists -and $dllCount -ge 1100 -and $criticalMissing.Count -eq 0 -and $logExists -and $logReadable -and $configCount -gt 0 -and $corruptedConfigs.Count -eq 0)

Write-Host "Protected Assets Audit Clean: $clean"
if (-not $clean) { exit 1 } else { exit 0 }
