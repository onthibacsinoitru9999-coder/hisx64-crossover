# ==============================================================================
# HIS LIVE BRANCH WATCHER & LEARNER v2.0
#   - Theo doi LogSystem.txt (API calls, Token, WorkInfo, Loi)
#   - BAT BAN PHIM + CHUOT toan cuc (Low-Level Win32 Hook)
#   - DO THOI GIAN CHO giua moi thao tac (wait_ms)
#   - Ghi journal CSV: Logs\HisInputJournal.csv
# ==============================================================================
param(
    [string]$HisRoot = "",
    [int]$PollMs     = 200
)
$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ===========================================================================
# 1. NGUON SU THAT DUY NHAT: Doc file .facility trong folder hien tai
#    File nay khai bao cung folder nao la Ninh Binh, folder nao la Ha Noi
#    Agent KHONG tu do chuyen sang folder khac
# ===========================================================================
$facilityConfig = $null
$facilityPaths = @(
    (Join-Path $PSScriptRoot ".facility"),
    (Join-Path (Get-Location).Path ".facility"),
    "D:\his-x64-28-11fix GDYK\his-x64\.facility",
    "E:\his-x64-28-11fix GDYK\his-x64\.facility"
)
foreach ($fp in $facilityPaths) {
    if (Test-Path $fp) {
        try {
            $facilityConfig = Get-Content $fp -Raw -Encoding UTF8 | ConvertFrom-Json
            Write-Host "[FACILITY] Doc tu: $fp" -ForegroundColor DarkGreen
            Write-Host "[FACILITY] Co so : $($facilityConfig.display_name)" -ForegroundColor Green
            if (-not $HisRoot -and $facilityConfig.his_root) {
                $HisRoot = $facilityConfig.his_root
            }
            break
        } catch {}
    }
}

if (-not $facilityConfig) {
    Write-Host "[FACILITY] CANH BAO: Khong tim thay file .facility - co the nham co so!" -ForegroundColor Red
    Write-Host "[FACILITY] Tao file .facility trong thu muc HIS de tranh chenh lenh." -ForegroundColor Yellow
}

# Fallback neu .facility khong co his_root: uu tien D: roi E:
if (-not $HisRoot) {
    $hisProc = Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($_.Path -match "his-x64|HIS.Desktop|MOS.Desktop|HisMcpServer") } |
        Select-Object -First 1
    if ($hisProc -and $hisProc.Path) {
        $procDir = Split-Path $hisProc.Path -Parent
        if (Test-Path (Join-Path $procDir "Logs\LogSystem.txt")) {
            $HisRoot = $procDir
        }
    }
}

$candidateRoots = @(
    $HisRoot,
    $PSScriptRoot,
    (Get-Location).Path,
    "D:\his 3-9\his-x64-28-11fix GDYK\his-x64",
    "D:\his-x64-28-11fix GDYK\his-x64",
    "E:\his-x64-28-11fix GDYK\his-x64",
    "D:\his-x64", "E:\his-x64"
)

$logDir = $null
foreach ($root in $candidateRoots) {
    if (-not $root) { continue }
    $cand = Join-Path $root "Logs"
    if (Test-Path $cand) {
        $logDir  = (Get-Item $cand).FullName
        $HisRoot = $root
        break
    }
}

if (-not $logDir) {
    Write-Host "[ERROR] Khong tim thay thu muc Logs! Chay lai voi: -HisRoot <duong-dan>" -ForegroundColor Red
    exit 1
}

$logPath     = Join-Path $logDir "LogSystem.txt"
$captureFile = Join-Path $logDir "Captured_Branch_Learning.txt"
$journalCsv  = Join-Path $logDir "HisInputJournal.csv"

if (-not (Test-Path $journalCsv)) {
    "timestamp,event_type,detail,window_title,wait_ms_since_last" |
        Out-File $journalCsv -Encoding UTF8
}
"=== BAT DAU THEO DOI & HOC TAP v2.0 LUC $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" |
    Out-File $captureFile -Encoding UTF8

# 2. Nap Win32 API - global keyboard + mouse low-level hook
Add-Type -ReferencedAssemblies "System.Windows.Forms","System.Drawing" -TypeDefinition @'
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public static class HisHook {
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT {
        public POINT pt;
        public uint mouseData, flags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT {
        public uint vkCode, scanCode, flags, time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc fn, IntPtr hMod, uint tid);
    [DllImport("user32.dll")] static extern bool   UnhookWindowsHookEx(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr h, int n, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)] static extern IntPtr GetModuleHandle(string m);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet=CharSet.Auto)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    const int WH_KEYBOARD_LL = 13;
    const int WH_MOUSE_LL    = 14;
    const int WM_KEYDOWN     = 0x0100;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_MBUTTONDOWN = 0x0207;

    static IntPtr _hkKbd, _hkMouse;
    static LowLevelProc _kbdCb, _mouseCb;

    public static ConcurrentQueue<string[]> Events = new ConcurrentQueue<string[]>();
    public static volatile bool Running = false;

    static string GetTitle() {
        var sb = new StringBuilder(256);
        GetWindowText(GetForegroundWindow(), sb, 256);
        return sb.ToString();
    }

    static IntPtr KbdCb(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode >= 0 && (int)wParam == WM_KEYDOWN) {
            var s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            string key = ((Keys)s.vkCode).ToString();
            Events.Enqueue(new[] { DateTime.Now.Ticks.ToString(), "KEY", key, GetTitle() });
        }
        return CallNextHookEx(_hkKbd, nCode, wParam, lParam);
    }

    static IntPtr MouseCb(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode >= 0) {
            int wp = (int)wParam;
            if (wp == WM_LBUTTONDOWN || wp == WM_RBUTTONDOWN || wp == WM_MBUTTONDOWN) {
                var s = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                string btn = wp == WM_LBUTTONDOWN ? "L-CLICK" : (wp == WM_RBUTTONDOWN ? "R-CLICK" : "M-CLICK");
                string detail = string.Format("{0} ({1},{2})", btn, s.pt.x, s.pt.y);
                Events.Enqueue(new[] { DateTime.Now.Ticks.ToString(), "MOUSE", detail, GetTitle() });
            }
        }
        return CallNextHookEx(_hkMouse, nCode, wParam, lParam);
    }

    public static void Start() {
        if (Running) return;
        Running = true;
        _kbdCb   = KbdCb;
        _mouseCb = MouseCb;
        var hMod = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
        _hkKbd   = SetWindowsHookEx(WH_KEYBOARD_LL, _kbdCb,   hMod, 0);
        _hkMouse = SetWindowsHookEx(WH_MOUSE_LL,    _mouseCb, hMod, 0);
        // Message pump tren background thread (can thiet de hook hoat dong)
        var t = new Thread(() => {
            while (Running) { Application.DoEvents(); Thread.Sleep(5); }
        });
        t.IsBackground = true;
        t.Start();
    }

    public static void Stop() {
        Running = false;
        if (_hkKbd   != IntPtr.Zero) { UnhookWindowsHookEx(_hkKbd);   _hkKbd   = IntPtr.Zero; }
        if (_hkMouse != IntPtr.Zero) { UnhookWindowsHookEx(_hkMouse); _hkMouse = IntPtr.Zero; }
    }
}
'@

Add-Type -AssemblyName System.Windows.Forms

# 3. Khoi dong hook
[HisHook]::Start()

Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "  HIS LIVE WATCHER v2.0 - BAN PHIM + CHUOT + API + THOI GIAN CHO" -ForegroundColor Yellow
Write-Host "  HIS Root : $HisRoot" -ForegroundColor Green
Write-Host "  Log In  : $logPath" -ForegroundColor Gray
Write-Host "  Capture : $captureFile" -ForegroundColor Gray
Write-Host "  Journal : $journalCsv" -ForegroundColor Gray
Write-Host "  Filter  : Chi ghi thao tac tren cua so HIS (bo qua terminal watcher)" -ForegroundColor DarkYellow
Write-Host "  Nhan Ctrl+C de dung" -ForegroundColor DarkGray
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] DANG THEO DOI..." -ForegroundColor Green

# 4. Ham tien ich
function Csv-Escape([string]$s) {
    '"' + $s.Replace('"', '""').Replace("`n",' ').Replace("`r",' ') + '"'
}
function Append-Journal([string]$ts, [string]$evType, [string]$detail, [string]$title, [long]$waitMs) {
    "$(Csv-Escape $ts),$(Csv-Escape $evType),$(Csv-Escape $detail),$(Csv-Escape $title),$waitMs" |
        Out-File $journalCsv -Append -Encoding UTF8
}

# 5. Mo stream LogSystem.txt
if (-not (Test-Path $logPath)) { New-Item -ItemType File -Path $logPath -Force | Out-Null }
$fs = [System.IO.File]::Open(
    $logPath,
    [System.IO.FileMode]::Open,
    [System.IO.FileAccess]::Read,
    [System.IO.FileShare]::ReadWrite
)
if ($fs.Length -gt 20000) { $fs.Seek(-20000, [System.IO.SeekOrigin]::End) | Out-Null }
$sr = New-Object System.IO.StreamReader($fs, [System.Text.Encoding]::UTF8)

# 6. Trang thai
$lastToken   = ""
$lastEventTs = [DateTime]::Now
$lastApiTs   = [DateTime]::Now

function Calc-Wait {
    $now = [DateTime]::Now
    $ms  = [long]($now - $script:lastEventTs).TotalMilliseconds
    $script:lastEventTs = $now
    return $ms
}

# 7. Vong lap chinh
try {
    while ($true) {

        # 7a. Drain hang doi ban phim / chuot
        $arr = [string[]]$null
        while ([HisHook]::Events.TryDequeue([ref]$arr)) {
            $evTs    = [DateTime]([long]$arr[0])
            $evType  = $arr[1]
            $detail  = $arr[2]
            $title   = $arr[3]
            $tsStr   = $evTs.ToString("HH:mm:ss.fff")
            $wait    = [long]($evTs - $script:lastEventTs).TotalMilliseconds
            $script:lastEventTs = $evTs

            # Bo qua phim lap qua nhanh (< 30ms) de tranh flood
            if ($wait -lt 30 -and $evType -eq "KEY") { continue }

            # Chi ghi khi focus KHONG phai o cua so watcher / terminal nay
            $isWatcherWindow = ($title -match "HIS Watcher|cmd\.exe|powershell" -and $title -notmatch "HIS\.Desktop|MOS|Inventec")
            if ($isWatcherWindow) { continue }

            $waitStr = "${wait}ms"
            $icon    = if ($evType -eq "KEY") { "[KEY]" } else { "[MOUSE]" }
            $titleShort = $title.Substring(0, [Math]::Min(50, $title.Length))

            Write-Host ("[{0}] {1} {2}  +{3}  Win: {4}" -f $tsStr, $icon, $detail, $waitStr, $titleShort) -ForegroundColor White

            "[{0}] {1} +{2} | {3} | Win: {4}" -f $tsStr, $icon, $waitStr, $detail, $title |
                Out-File $captureFile -Append -Encoding UTF8
            Append-Journal $tsStr $evType $detail $title $wait
        }

        # 7b. Poll LogSystem.txt
        $line = $sr.ReadLine()
        if ($null -ne $line) {
            $tsStr = (Get-Date).ToString("HH:mm:ss.fff")
            "[{0}] {1}" -f $tsStr, $line | Out-File $captureFile -Append -Encoding UTF8

            # Token moi
            if ($line -match "TokenCode\|([a-fA-F0-9]{64})") {
                $token = $Matches[1]
                if ($token -ne $lastToken) {
                    $lastToken = $token
                    Write-Host ("`n[{0}] [TOKEN MOI] {1}" -f (Get-Date -Format 'HH:mm:ss'), $token) -ForegroundColor Yellow
                    "TOKEN_CAPTURED: $token" | Out-File (Join-Path $logDir "ActiveToken.txt") -Encoding UTF8
                    $wait = Calc-Wait
                    Append-Journal $tsStr "TOKEN" $token "" $wait
                }
            }

            # WorkInfo / Branch / Room / Dept
            if ($line -match "UpdateWorkInfo|WorkInfoSDO|DEPARTMENT_ID|BRANCH_ID|ROOM_ID") {
                $snippet = $line.Substring(0, [Math]::Min(140, $line.Length))
                Write-Host ("[{0}] [WORKINFO] {1}" -f (Get-Date -Format 'HH:mm:ss'), $snippet) -ForegroundColor Magenta
                $wait = Calc-Wait
                Append-Journal $tsStr "WORKINFO" $line "" $wait
            }

            # API Begin
            if ($line -match "WebApiClient\.(Post|Get)\.Begin.*?api:([^\s_]+)") {
                $method  = $Matches[1]
                $apiName = $Matches[2]
                $apiWait = [long]([DateTime]::Now - $script:lastApiTs).TotalMilliseconds
                $script:lastApiTs = [DateTime]::Now
                Write-Host ("[{0}] [API>] {1} -> {2}  API-gap: {3}ms" -f (Get-Date -Format 'HH:mm:ss'), $method, $apiName, $apiWait) -ForegroundColor Cyan
                $wait = Calc-Wait
                Append-Journal $tsStr "API_BEGIN" "$method/$apiName" "" $wait
            }

            # API End
            if ($line -match "WebApiClient\.(Post|Get)\.End.*?api:([^\s_]+)") {
                $method  = $Matches[1]
                $apiName = $Matches[2]
                $ok = if ($line -match "Success.*true") { "OK" } elseif ($line -match "Success.*false") { "FAIL" } else { "?" }
                Write-Host ("[{0}] [API<] {1} -> {2}  {3}" -f (Get-Date -Format 'HH:mm:ss'), $method, $apiName, $ok) -ForegroundColor DarkCyan
                $wait = Calc-Wait
                Append-Journal $tsStr "API_END" "$method/$apiName $ok" "" $wait
            }

            # Payload DTO
            if ($line -match "SerializeObject data api: (.*)") {
                $data = $Matches[1]
                if ($data.Length -gt 150) { $data = $data.Substring(0, 150) + "..." }
                Write-Host ("[{0}] [PAYLOAD] {1}" -f (Get-Date -Format 'HH:mm:ss'), $data) -ForegroundColor Gray
                $wait = Calc-Wait
                Append-Journal $tsStr "PAYLOAD" $data "" $wait
            }

            # Loi
            if ($line -match "Exception|Error|Fail") {
                $snippet = $line.Substring(0, [Math]::Min(180, $line.Length))
                Write-Host ("[{0}] [LOI] {1}" -f (Get-Date -Format 'HH:mm:ss'), $snippet) -ForegroundColor Red
                $wait = Calc-Wait
                Append-Journal $tsStr "ERROR" $line "" $wait
            }

        }
        else {
            Start-Sleep -Milliseconds $PollMs
        }
    }
}
finally {
    [HisHook]::Stop()
    $sr.Close()
    $fs.Close()
    Write-Host ("`n[{0}] Da dung theo doi." -f (Get-Date -Format 'HH:mm:ss')) -ForegroundColor Red
}
