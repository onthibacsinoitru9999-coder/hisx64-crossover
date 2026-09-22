$results = @()

function Invoke-Test($id, $suite, $name, $cmd, $cmdArgs) {
    Write-Host "[$id] Running: $name ($cmd $cmdArgs)..." -ForegroundColor Cyan
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $cmd
    $psi.Arguments = $cmdArgs
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = [System.Diagnostics.Process]::Start($psi)

    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $exited = $proc.WaitForExit(25000) # 25s timeout
    if (-not $exited) {
        try { $proc.Kill() } catch {}
        $sw.Stop()
        Write-Host "[$id] TIMED OUT after 25s!" -ForegroundColor Red
        return [PSCustomObject]@{
            Id            = $id
            Suite         = $suite
            Name          = $name
            Command       = "$cmd $cmdArgs"
            ExitCode      = -999
            DurationMs    = $sw.ElapsedMilliseconds
            StdoutCount   = 0
            StdoutFirst   = ""
            StdoutLast    = ""
            StdoutAll     = ""
            StderrPreview = "TIMED OUT (killed after 25s)"
            StderrAll     = "TIMED OUT (killed after 25s)"
        }
    }
    
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $sw.Stop()

    $stdoutLines = ($stdout -split "\r?\n" | Where-Object { $_ -ne "" })
    $firstStdout = if ($stdoutLines.Count -gt 0) { $stdoutLines[0] } else { "" }
    $lastStdout = if ($stdoutLines.Count -gt 0) { $stdoutLines[-1] } else { "" }

    $res = [PSCustomObject]@{
        Id            = $id
        Suite         = $suite
        Name          = $name
        Command       = "$cmd $cmdArgs"
        ExitCode      = $proc.ExitCode
        DurationMs    = $sw.ElapsedMilliseconds
        StdoutCount   = $stdoutLines.Count
        StdoutFirst   = $firstStdout
        StdoutLast    = $lastStdout
        StdoutAll     = $stdout
        StderrPreview = ($stderr -split "\r?\n" | Select-Object -First 3) -join " | "
        StderrAll     = $stderr
    }
    return $res
}

$exePath = (Resolve-Path ".\HisPacsUploader.exe").Path
$batPath = (Resolve-Path ".\HisPacsUploader.bat").Path

# Suite 1: CLI Syntax Variations
$results += Invoke-Test "TC-A01" "CLI Syntax" "Exe No Arguments" $exePath ""
$results += Invoke-Test "TC-A02" "CLI Syntax" "Bat No Arguments" "cmd.exe" "/c `"`"$batPath`"`""
$results += Invoke-Test "TC-A03" "CLI Syntax" "Unknown Flag (--unknown)" $exePath "0004009330 --unknown"
$results += Invoke-Test "TC-A04" "CLI Syntax" "Invalid TTL Text (--ttl invalid)" $exePath "0004009330 --ttl invalid"
$results += Invoke-Test "TC-A05" "CLI Syntax" "TTL Large 100d (--ttl 100d)" $exePath "0004009330 --ttl 100d"
$results += Invoke-Test "TC-A06" "CLI Syntax" "Invalid TTL Unit (--ttl 0s)" $exePath "0004009330 --ttl 0s"
$results += Invoke-Test "TC-A07" "CLI Syntax" "Zero TTL (--ttl 0d)" $exePath "0004009330 --ttl 0d"
$results += Invoke-Test "TC-A08" "CLI Syntax" "Negative TTL (--ttl -5h)" $exePath "0004009330 --ttl -5h"
$results += Invoke-Test "TC-A09" "CLI Syntax" "Dangling Flag (--ttl without value)" $exePath "0004009330 --ttl"
$results += Invoke-Test "TC-A10" "CLI Syntax" "Whitespace Around MaBN" $exePath "`"  0004009330  `""
$results += Invoke-Test "TC-A11" "CLI Syntax" "Whitespace in TTL Flag" $exePath "0004009330 --ttl `" 7d `""
$results += Invoke-Test "TC-A12" "CLI Syntax" "Help Flag (--help)" $exePath "--help"

# Suite 2: Invalid MaBN Formats & Sanitization
$results += Invoke-Test "TC-B01" "MaBN Format" "Letters in MaBN" $exePath "ABCD1234EF"
$results += Invoke-Test "TC-B02" "MaBN Format" "Special Symbols in MaBN" $exePath "!@#$%^&*()"
$results += Invoke-Test "TC-B03" "MaBN Format" "Non-existent 10-digit ID" $exePath "9999999999"
$results += Invoke-Test "TC-B04" "MaBN Format" "All Zeroes ID" $exePath "0000000000"
$results += Invoke-Test "TC-B05" "MaBN Format" "Whitespace String MaBN" $exePath "`"   `""
$results += Invoke-Test "TC-B06" "MaBN Format" "Valid ID with VS. Prefix" $exePath "VS.0004009330"
$results += Invoke-Test "TC-B07" "MaBN Format" "Unknown Prefix" $exePath "INVALID.0004009330"
$results += Invoke-Test "TC-B08" "MaBN Format" "Short Numeric ID (4009330)" $exePath "4009330"

# Suite 3: Stdout Redirection Purity
$results += Invoke-Test "TC-C01" "Stdout Purity" "Exe Valid MaBN Redirect Stdout/Stderr" $exePath "0004009330"
$results += Invoke-Test "TC-C02" "Stdout Purity" "Bat Valid MaBN Redirect Stdout/Stderr" "cmd.exe" "/c `"`"$batPath`" 0004009330`""
$results += Invoke-Test "TC-C03" "Stdout Purity" "Exe Error Redirect Stdout/Stderr" $exePath "9999999999"
$results += Invoke-Test "TC-C04" "Stdout Purity" "Bat Error Redirect Stdout/Stderr" "cmd.exe" "/c `"`"$batPath`" 9999999999`""

$reportPath = ".\adversarial_test_results.json"
$results | ConvertTo-Json -Depth 5 | Set-Content -Path $reportPath -Encoding utf8
Write-Host "Completed $($results.Count) tests. Saved to $reportPath" -ForegroundColor Green
