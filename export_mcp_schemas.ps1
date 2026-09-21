<#
.SYNOPSIS
    Exports tool schemas from HisMcpServer.exe to Antigravity MCP directory
    and registers his-clinical in global and local mcp_config.json files.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
if (-not $rootDir) { $rootDir = (Get-Location).Path }

$mcpExe = Join-Path $rootDir "HisMcpServer.exe"
if (-not (Test-Path $mcpExe)) {
    throw "HisMcpServer.exe not found! Run build_mcp_server.ps1 first."
}

Write-Host "=== EXPORTING HIS MCP TOOL SCHEMAS FOR ANTIGRAVITY ===" -ForegroundColor Cyan

# 1. Query tools/list from HisMcpServer.exe
$req = '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
$jsonOut = $req | & $mcpExe

try {
    $data = $jsonOut | ConvertFrom-Json
} catch {
    throw "Failed to parse JSON-RPC response from HisMcpServer: $_"
}

if (-not $data.result.tools) {
    throw "No tools returned from HisMcpServer tools/list!"
}

$tools = $data.result.tools
Write-Host "Discovered $($tools.Count) tools from HisMcpServer." -ForegroundColor Green

# 2. Target MCP directories
$userProfile = if ($env:USERPROFILE) { $env:USERPROFILE } else { "C:\Users\dr" }
$appData = Join-Path $userProfile ".gemini\antigravity\mcp"
$targetDirs = @(
    (Join-Path $appData "his-clinical"),
    (Join-Path $appData "his-clinical_his-clinical")
)

$instructions = @"
# HIS Clinical MCP Server Instructions
This server provides complete hospital automation tools for Bach Mai Hospital.
All agents MUST use these MCP tools instead of writing scratch scripts or temporary files.
"@

foreach ($tDir in $targetDirs) {
    if (-not (Test-Path $tDir)) {
        New-Item -ItemType Directory -Path $tDir -Force | Out-Null
    }

    # Write each tool schema
    foreach ($tool in $tools) {
        $toolSchema = [ordered]@{
            name = $tool.name
            description = $tool.description
            parameters = $tool.inputSchema
        }
        $toolJson = $toolSchema | ConvertTo-Json -Depth 10
        $filePath = Join-Path $tDir "$($tool.name).json"
        [System.IO.File]::WriteAllText($filePath, $toolJson, [System.Text.Encoding]::UTF8)
        Write-Host "  Written schema -> $filePath" -ForegroundColor Gray
    }

    # Write instructions.md
    $instPath = Join-Path $tDir "instructions.md"
    [System.IO.File]::WriteAllText($instPath, $instructions, [System.Text.Encoding]::UTF8)
    Write-Host "  Written instructions -> $instPath" -ForegroundColor Green
}

# 3. Update mcp_config.json files safely (preserve other existing servers)
$configPaths = @(
    (Join-Path $userProfile ".gemini\config\mcp_config.json"),
    (Join-Path $userProfile ".gemini\antigravity\mcp_config.json"),
    (Join-Path $rootDir ".agents\mcp_config.json")
)

$serverDef = [ordered]@{
    command = $mcpExe
    args = @()
    env = [ordered]@{
        HIS_BASE_DIR = $rootDir
    }
}

foreach ($cfgPath in $configPaths) {
    $cfgDir = Split-Path -Parent $cfgPath
    if (-not (Test-Path $cfgDir)) {
        New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null
    }

    $cfgObj = $null
    if (Test-Path $cfgPath) {
        try {
            $raw = Get-Content -Raw -Encoding UTF8 $cfgPath
            if ($raw -and $raw.Trim()) {
                $cfgObj = $raw | ConvertFrom-Json
            }
        } catch {
            $cfgObj = $null
        }
    }

    if (-not $cfgObj) {
        $cfgObj = [PSCustomObject]@{
            mcpServers = [PSCustomObject]@{}
        }
    }
    if (-not $cfgObj.mcpServers) {
        $cfgObj | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{}) -Force
    }

    $serverDefObj = [PSCustomObject]@{
        command = $mcpExe
        args = @()
        env = [PSCustomObject]@{
            HIS_BASE_DIR = $rootDir
        }
    }

    if ($cfgObj.mcpServers -is [System.Collections.IDictionary]) {
        $cfgObj.mcpServers["his-clinical"] = $serverDefObj
    } else {
        $cfgObj.mcpServers | Add-Member -MemberType NoteProperty -Name "his-clinical" -Value $serverDefObj -Force
    }

    $newJson = $cfgObj | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($cfgPath, $newJson, [System.Text.Encoding]::UTF8)
    Write-Host "Updated MCP Config -> $cfgPath" -ForegroundColor Green
}

# 4. Create Workspace Plugin definition (.agents/plugins/his-clinical-suite)
$pluginDir = Join-Path $rootDir ".agents\plugins\his-clinical-suite"
if (-not (Test-Path $pluginDir)) {
    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
}

$pluginJson = [ordered]@{
    name = "his-clinical-suite"
    description = "Bo cong cu lam sang BV Bach Mai MCP"
    version = "1.0.0"
} | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $pluginDir "plugin.json"), $pluginJson, [System.Text.Encoding]::UTF8)

$pluginMcp = [ordered]@{
    mcpServers = [ordered]@{
        "his-clinical" = $serverDef
    }
} | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $pluginDir "mcp_config.json"), $pluginMcp, [System.Text.Encoding]::UTF8)
Write-Host "Created Workspace Plugin -> $pluginDir" -ForegroundColor Green

Write-Host "=== HOAN TAT DONG GOI HIS MCP SERVER CHO ANTIGRAVITY ===" -ForegroundColor Green
