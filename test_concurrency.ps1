param(
    [string]$MaBn = "0004009330"
)

$exePath = (Resolve-Path ".\HisPacsUploader.exe").Path

Write-Host "=== CONCURRENCY COLLISION STRESS TEST ===" -ForegroundColor Yellow
Write-Host "Target MaBN: $MaBn"
Write-Host "Launching 2 instances of HisPacsUploader.exe simultaneously..."

$psi1 = New-Object System.Diagnostics.ProcessStartInfo
$psi1.FileName = $exePath
$psi1.Arguments = "$MaBn --ttl 24h"
$psi1.RedirectStandardOutput = $true
$psi1.RedirectStandardError = $true
$psi1.UseShellExecute = $false
$psi1.CreateNoWindow = $true

$psi2 = New-Object System.Diagnostics.ProcessStartInfo
$psi2.FileName = $exePath
$psi2.Arguments = "$MaBn --ttl 7d"
$psi2.RedirectStandardOutput = $true
$psi2.RedirectStandardError = $true
$psi2.UseShellExecute = $false
$psi2.CreateNoWindow = $true

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$p1 = [System.Diagnostics.Process]::Start($psi1)
$p2 = [System.Diagnostics.Process]::Start($psi2)

$out1Task = $p1.StandardOutput.ReadToEndAsync()
$err1Task = $p1.StandardError.ReadToEndAsync()
$out2Task = $p2.StandardOutput.ReadToEndAsync()
$err2Task = $p2.StandardError.ReadToEndAsync()

$p1.WaitForExit()
$p2.WaitForExit()
$sw.Stop()

$out1 = $out1Task.Result
$err1 = $err1Task.Result
$out2 = $out2Task.Result
$err2 = $err2Task.Result

$result = [PSCustomObject]@{
    DurationMs = $sw.ElapsedMilliseconds
    Process1 = [PSCustomObject]@{
        ExitCode = $p1.ExitCode
        StdoutLines = ($out1 -split "\r?\n" | Where-Object { $_ -ne "" }).Count
        Stdout = $out1.Trim()
        StderrFull = $err1
    }
    Process2 = [PSCustomObject]@{
        ExitCode = $p2.ExitCode
        StdoutLines = ($out2 -split "\r?\n" | Where-Object { $_ -ne "" }).Count
        Stdout = $out2.Trim()
        StderrFull = $err2
    }
}

$result | ConvertTo-Json -Depth 5 | Set-Content -Path ".\concurrency_test_results.json" -Encoding utf8
Write-Host "Process 1 ExitCode: $($p1.ExitCode)"
Write-Host "Process 2 ExitCode: $($p2.ExitCode)"
if ($p2.ExitCode -ne 0) {
    Write-Host "Process 2 FAILED as predicted due to temp folder race condition!" -ForegroundColor Red
    Write-Host "Process 2 Stderr:"
    Write-Host $err2
}
