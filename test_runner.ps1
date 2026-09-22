param(
    [string]$Target = "exe",
    [Parameter()]
    [AllowEmptyString()]
    [string]$Arguments = ""
)

$psi = New-Object System.Diagnostics.ProcessStartInfo
if ($Target -eq "exe") {
    $psi.FileName = (Resolve-Path ".\HisPacsUploader.exe").Path
    $psi.Arguments = $Arguments
} elseif ($Target -eq "bat") {
    $psi.FileName = "cmd.exe"
    $psi.Arguments = "/c `"" + (Resolve-Path ".\HisPacsUploader.bat").Path + "`" " + $Arguments
} else {
    $psi.FileName = $Target
    $psi.Arguments = $Arguments
}

$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()
$sw.Stop()

$stdoutLines = ($stdout -split "\r?\n" | Where-Object { $_ -ne "" })

[PSCustomObject]@{
    Target        = $Target
    Arguments     = $Arguments
    ExitCode      = $proc.ExitCode
    DurationMs    = $sw.ElapsedMilliseconds
    StdoutCount   = $stdoutLines.Count
    StdoutLines   = $stdoutLines
    StderrPreview = ($stderr -split "\r?\n" | Select-Object -First 5) -join " | "
    StderrFull    = $stderr
} | ConvertTo-Json -Depth 5
