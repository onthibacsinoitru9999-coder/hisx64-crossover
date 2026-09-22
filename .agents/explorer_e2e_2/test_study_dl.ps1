$url = 'http://192.168.200.111:8080/pacs/0/rest/VRPACS/studies/123.149807022125412.1875937613807029?contentType=application/zip'
$tmp = [System.IO.Path]::GetTempFileName() + '.zip'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
curl.exe -s -o $tmp $url
$sw.Stop()
$len = (Get-Item $tmp).Length
$mb = [Math]::Round($len / (1024 * 1024), 2)
$speed = [Math]::Round($mb / [Math]::Max(0.01, $sw.Elapsed.TotalSeconds), 2)
Write-Host "Downloaded $len bytes ($mb MB) in $([Math]::Round($sw.Elapsed.TotalSeconds, 2))s ($speed MB/s)" -ForegroundColor Green

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($tmp)
Write-Host "Zip entry count: $($zip.Entries.Count)" -ForegroundColor Cyan
foreach ($e in $zip.Entries) {
    Write-Host "  Entry: $($e.FullName) ($($e.Length) bytes)"
}

# Verify first DICOM file header
if ($zip.Entries.Count -gt 0) {
    $firstEntry = $zip.Entries[0]
    $stream = $firstEntry.Open()
    $buf = New-Object byte[] 132
    $read = $stream.Read($buf, 0, 132)
    $stream.Dispose()
    $magic = [System.Text.Encoding]::ASCII.GetString($buf, 128, 4)
    Write-Host "First entry magic bytes at 128..131: '$magic' (Expected 'DICM')" -ForegroundColor Green
}
$zip.Dispose()
Remove-Item -Force $tmp
Write-Host "Cleaned up temporary zip file." -ForegroundColor Gray
