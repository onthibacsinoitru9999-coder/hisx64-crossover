param(
    [string]$exePath,
    [string]$newtonsoftDll,
    [string]$viewerHtmlPath
)

# 1. Inspect Embedded Resource
$asm = [System.Reflection.Assembly]::LoadFile($exePath)
$resNames = $asm.GetManifestResourceNames()
$targetRes = $resNames | Where-Object { $_ -like '*index.html*' }

if (-not $targetRes) {
    Write-Output "RESOURCE_NOT_FOUND"
    exit 1
}

$stream = $asm.GetManifestResourceStream($targetRes)
$reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
$resContent = $reader.ReadToEnd()

$diskContent = Get-Content -Raw -Encoding UTF8 $viewerHtmlPath
$resLen = $resContent.Length
$diskLen = $diskContent.Length

Write-Output "RESOURCE_FOUND|$targetRes|$resLen|$diskLen"

# 2. Test ViewerPackager Invocation with isolated directory
if (Test-Path $newtonsoftDll) {
    [System.Reflection.Assembly]::LoadFrom($newtonsoftDll) | Out-Null
}

$type = $asm.GetType('HisPacsUploader.ViewerPackager')
$method = $type.GetMethod('PackageViewer', [System.Reflection.BindingFlags]'Public,Static')

$testDir = [string](Join-Path $env:TEMP ('PacsPackager_' + [System.Guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $testDir | Out-Null

try {
    $slices = New-Object 'System.Collections.Generic.List[string]'
    $argsList = [object[]]@($testDir, $slices.psobject.BaseObject, 'TEST PATIENT', '00012345', '2026-09-10', 'CT', '1.2.3.4')
    $method.Invoke($null, $argsList)

    $indexPath = Join-Path $testDir 'index.html'
    $manifestPath = Join-Path $testDir 'manifest.json'

    $indexExists = Test-Path $indexPath
    $manifestExists = Test-Path $manifestPath
    $indexSize = if ($indexExists) { (Get-Item $indexPath).Length } else { 0 }

    Write-Output "PACKAGER_EXECUTION|$indexExists|$manifestExists|$indexSize"
} finally {
    Remove-Item -Recurse -Force $testDir -ErrorAction SilentlyContinue
}
