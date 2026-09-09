param(
    [string]$Command
)

$sw = [System.Diagnostics.Stopwatch]::StartNew()
switch ($Command) {
    "lookup" {
        & .\HisClinicalCli.exe lookup 0003757502
    }
    "orders" {
        & .\HisClinicalCli.exe orders 0003757502
    }
    "wardreport" {
        & .\HisWardReportCreator.exe
    }
    "health" {
        cmd.exe /c "call HisDiagnosticDoctor.bat health"
    }
    Default {
        Write-Host "Unknown benchmark command: $Command"
    }
}
$sw.Stop()
Write-Host "`n>>> BENCHMARK_RESULT: $Command elapsed $($sw.ElapsedMilliseconds) ms ($($sw.Elapsed.TotalSeconds) s) <<<"
