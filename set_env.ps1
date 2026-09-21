<#
.SYNOPSIS
    Companion PowerShell environment loader for HIS Automation System.
    Provides robust, multi-tier discovery for Git, Python, C# (csc.exe), and Rclone.
    Supports dot-sourcing (. .\set_env.ps1) or direct invocation.
#>

[CmdletBinding()]
param()

$scriptDir = $PSScriptRoot
if (-not $scriptDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
}

$systemRoot = if ($env:SystemRoot) { $env:SystemRoot } else { "C:\Windows" }
$systemDrive = if ($env:SystemDrive) { $env:SystemDrive } else { "C:" }
$programFiles = if (${env:ProgramFiles}) { ${env:ProgramFiles} } else { "C:\Program Files" }
$programFilesX86 = if (${env:ProgramFiles(x86)}) { ${env:ProgramFiles(x86)} } else { "C:\Program Files (x86)" }
$localAppData = if ($env:LOCALAPPDATA) { $env:LOCALAPPDATA } else { "$env:USERPROFILE\AppData\Local" }

function Prepend-Path([string]$dir) {
    if ([string]::IsNullOrWhiteSpace($dir) -or -not (Test-Path $dir)) { return }
    $currentPaths = ($env:PATH -split ';') | Where-Object { $_ -ne '' }
    if ($currentPaths -notcontains $dir) {
        $env:PATH = "$dir;" + $env:PATH
    }
}

# ------------------------------------------------------------------------------
# 1. Git (git.exe)
# ------------------------------------------------------------------------------
$gitFound = $null

$gitCmd = Get-Command git -ErrorAction SilentlyContinue
if ($gitCmd -and (Test-Path $gitCmd.Source)) {
    $gitFound = $gitCmd.Source
} else {
    $wingetGit = Get-ChildItem -Path "$localAppData\Microsoft\WinGet\Packages" -Filter "Git.MinGit*" -Directory -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($wingetGit -and (Test-Path "$($wingetGit.FullName)\cmd\git.exe")) {
        $gitFound = "$($wingetGit.FullName)\cmd\git.exe"
        Prepend-Path "$($wingetGit.FullName)\cmd"
        Prepend-Path "$($wingetGit.FullName)\mingw64\bin"
    } elseif (Test-Path "$programFiles\Git\cmd\git.exe") {
        $gitFound = "$programFiles\Git\cmd\git.exe"
        Prepend-Path "$programFiles\Git\cmd"
    } elseif (Test-Path "$programFilesX86\Git\cmd\git.exe") {
        $gitFound = "$programFilesX86\Git\cmd\git.exe"
        Prepend-Path "$programFilesX86\Git\cmd"
    } elseif (Test-Path "$localAppData\Programs\Git\cmd\git.exe") {
        $gitFound = "$localAppData\Programs\Git\cmd\git.exe"
        Prepend-Path "$localAppData\Programs\Git\cmd"
    } elseif (Test-Path "$scriptDir\Tool\Git\cmd\git.exe") {
        $gitFound = "$scriptDir\Tool\Git\cmd\git.exe"
        Prepend-Path "$scriptDir\Tool\Git\cmd"
    }
}

if ($gitFound) {
    $env:GIT = $gitFound
}

# ------------------------------------------------------------------------------
# 2. Python (python.exe) - Avoid WindowsApps 0-byte execution aliases
# ------------------------------------------------------------------------------
$pythonFound = $null

if (Test-Path "$scriptDir\.venv\Scripts\python.exe") {
    $pythonFound = "$scriptDir\.venv\Scripts\python.exe"
    Prepend-Path (Split-Path -Parent $pythonFound)
} else {
    $pyDirs = @()
    $pyDirs += Get-ChildItem -Path "$localAppData\Programs\Python" -Filter "Python3*" -Directory -ErrorAction SilentlyContinue
    $pyDirs += Get-ChildItem -Path "$programFiles" -Filter "Python3*" -Directory -ErrorAction SilentlyContinue
    $pyDirs += Get-ChildItem -Path "$programFilesX86" -Filter "Python3*" -Directory -ErrorAction SilentlyContinue
    $pyDirs += Get-ChildItem -Path "$systemDrive" -Filter "Python3*" -Directory -ErrorAction SilentlyContinue

    foreach ($p in $pyDirs) {
        $candidate = Join-Path $p.FullName "python.exe"
        if (Test-Path $candidate) {
            $pythonFound = $candidate
            Prepend-Path $p.FullName
            Prepend-Path (Join-Path $p.FullName "Scripts")
            break
        }
    }

    if (-not $pythonFound) {
        $pyCommands = Get-Command python -All -ErrorAction SilentlyContinue
        foreach ($c in $pyCommands) {
            if ($c.Source -notmatch "WindowsApps") {
                $pythonFound = $c.Source
                Prepend-Path (Split-Path -Parent $c.Source)
                break
            }
        }
    }

    if (-not $pythonFound) {
        $pyLauncher = Get-Command py -ErrorAction SilentlyContinue
        if ($pyLauncher) {
            try {
                $pyExe = & py -3 -c "import sys; print(sys.executable)" 2>$null
                if ($pyExe -and (Test-Path $pyExe.Trim())) {
                    $pythonFound = $pyExe.Trim()
                    $pyDir = Split-Path -Parent $pythonFound
                    Prepend-Path $pyDir
                    Prepend-Path (Join-Path $pyDir "Scripts")
                }
            } catch {}
        }
    }
}

if ($pythonFound) {
    $env:PYTHON = $pythonFound
}

# ------------------------------------------------------------------------------
# 3. C# Compiler (csc.exe) - 64-bit first, fallback to 32-bit Framework and Roslyn
# ------------------------------------------------------------------------------
$cscFound = $null

$framework64 = "$systemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$framework32 = "$systemRoot\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if (Test-Path $framework64) {
    $cscFound = $framework64
    Prepend-Path (Split-Path -Parent $framework64)
} elseif (Test-Path $framework32) {
    $cscFound = $framework32
    Prepend-Path (Split-Path -Parent $framework32)
} else {
    $vsPatterns = @(
        "$programFiles\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "$programFilesX86\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe"
    )
    foreach ($pat in $vsPatterns) {
        $roslyn = Resolve-Path $pat -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($roslyn -and (Test-Path $roslyn.Path)) {
            $cscFound = $roslyn.Path
            Prepend-Path (Split-Path -Parent $cscFound)
            break
        }
    }

    if (-not $cscFound) {
        $cscCmd = Get-Command csc -ErrorAction SilentlyContinue
        if ($cscCmd -and (Test-Path $cscCmd.Source)) {
            $cscFound = $cscCmd.Source
        }
    }
}

if ($cscFound) {
    $env:CSC = $cscFound
}

# ------------------------------------------------------------------------------
# 4. Cloud Sync Tool (rclone.exe)
# ------------------------------------------------------------------------------
$rcloneFound = $null

if (Test-Path "$scriptDir\rclone.exe") {
    $rcloneFound = "$scriptDir\rclone.exe"
    Prepend-Path $scriptDir
} else {
    $rcloneCmd = Get-Command rclone -ErrorAction SilentlyContinue
    if ($rcloneCmd -and (Test-Path $rcloneCmd.Source)) {
        $rcloneFound = $rcloneCmd.Source
    } elseif (Test-Path "$localAppData\Programs\rclone\rclone.exe") {
        $rcloneFound = "$localAppData\Programs\rclone\rclone.exe"
        Prepend-Path "$localAppData\Programs\rclone"
    } elseif (Test-Path "$programFiles\rclone\rclone.exe") {
        $rcloneFound = "$programFiles\rclone\rclone.exe"
        Prepend-Path "$programFiles\rclone"
    }
}

if ($rcloneFound) {
    $env:RCLONE = $rcloneFound
}

# ------------------------------------------------------------------------------
# 5. HIS Doctor Authentication Defaults (Doctor 034727 across all facilities, Pass 981)
# ------------------------------------------------------------------------------
if (-not $env:HIS_DOCTOR_LOGIN) { $env:HIS_DOCTOR_LOGIN = "034727" }
if (-not $env:HIS_PASSWORD) { $env:HIS_PASSWORD = "981" }

$env:HIS_ENV_READY = "1"

Write-Output "[set_env.ps1] Environment configured successfully."
Write-Output "  - CSC:      $(if ($env:CSC) { $env:CSC } else { 'Not found' })"
Write-Output "  - GIT:      $(if ($env:GIT) { $env:GIT } else { 'Not found' })"
Write-Output "  - PYTHON:   $(if ($env:PYTHON) { $env:PYTHON } else { 'Not found' })"
Write-Output "  - RCLONE:   $(if ($env:RCLONE) { $env:RCLONE } else { 'Not found' })"
Write-Output "  - DOCTOR:   $env:HIS_DOCTOR_LOGIN (Current default pass: $env:HIS_PASSWORD)"

