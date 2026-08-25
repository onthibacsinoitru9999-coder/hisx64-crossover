[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " 1. KIEM TRA TRANG THAI TAILSCALE TAI MAY NAY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

try {
    $tsStatusRaw = & tailscale status --json | ConvertFrom-Json
    $myIp = $tsStatusRaw.Self.TailscaleIPs[0]
    $myHost = $tsStatusRaw.Self.HostName
    Write-Host "[+] IP Tailscale may nay: $myIp ($myHost)" -ForegroundColor Green

    $routerFound = $false
    $peers = $tsStatusRaw.Peer.PSObject.Properties
    foreach ($p in $peers) {
        $peer = $p.Value
        $hostname = $peer.HostName
        $online = $peer.Online
        $routes = $peer.PrimaryRoutes
        if (-not $routes) { $routes = $peer.AllowedIPs }

        $hasHisRoute = $false
        foreach ($r in $routes) {
            if ($r -like "*192.168.7.0/24*") { $hasHisRoute = $true; break }
        }

        if ($hasHisRoute) {
            $routerFound = $true
            $statusStr = if ($online) { "ONLINE (Dang hoat dong)" } else { "OFFLINE (Tat may hoac Sleep!)" }
            $statusColor = if ($online) { "Green" } else { "Red" }
            $curAddr = if ($peer.CurAddr) { $peer.CurAddr } else { "Relay DERP: " + $peer.Relay }

            Write-Host "[!] May Subnet Router o benh vien: $hostname ($($peer.TailscaleIPs[0]))" -ForegroundColor Yellow
            Write-Host "    - Trang thai: " -NoNewline
            Write-Host $statusStr -ForegroundColor $statusColor
            Write-Host "    - Kieu ket noi: $curAddr"
            Write-Host "    - Lan cuoi online: $($peer.LastSeen)"
            if (-not $online) {
                Write-Host "    => NGUYEN NHAN CHINH: May tram '$hostname' o vien dang BI TAT hoac NGU DONG (Sleep)!" -ForegroundColor Red
                Write-Host "       Can bat may o vien len thi moi chuyen tiep duoc goi tin vao mang HIS (192.168.7.x)." -ForegroundColor Yellow
            }
        }
    }

    if (-not $routerFound) {
        Write-Host "[-] Chua tim thay may nao trong Tailscale chia se dai mang 192.168.7.0/24!" -ForegroundColor Red
    }
} catch {
    Write-Host "[-] Loi khi kiem tra Tailscale: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " 2. KIEM TRA KET NOI TOI CAC CONG MAY CHU HIS (192.168.7.x)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$services = @(
    @{ Name = "ACS (Xac thuc / Dang nhap)"; IP = "192.168.7.200"; Port = 1401 },
    @{ Name = "SDA (Cau hinh he thong / Danh muc)"; IP = "192.168.7.200"; Port = 1410 },
    @{ Name = "MOS (Nghiep vu kham chua benh)"; IP = "192.168.7.236"; Port = 1608 },
    @{ Name = "EMR (Benh an dien tu)"; IP = "192.168.7.239"; Port = 1415 },
    @{ Name = "FSS (Luu tru file, anh, bieu mau)"; IP = "192.168.7.216"; Port = 1405 },
    @{ Name = "SAR (Bao cao tong hop)"; IP = "192.168.7.200"; Port = 1409 },
    @{ Name = "MRS (Bao cao y te)"; IP = "192.168.7.200"; Port = 1413 },
    @{ Name = "AUP (Tu dong cap nhat)"; IP = "192.168.7.200"; Port = 1468 },
    @{ Name = "PACS (Chan doan hinh anh)"; IP = "192.168.7.200"; Port = 5000 }
)

$header = "{0,-38} | {1,-20} | {2,-15}" -f "Dich vu HIS", "Dia chi IP:Port", "Ket qua"
Write-Host $header
Write-Host ("-" * 78)

$successCount = 0
foreach ($svc in $services) {
    $ip = $svc.IP
    $port = $svc.Port
    $name = $svc.Name
    $addr = "$ip" + ":" + "$port"
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $iar = $tcpClient.BeginConnect($ip, $port, $null, $null)
    $success = $iar.AsyncWaitHandle.WaitOne(1000, $false)
    $sw.Stop()

    $leftPart = "{0,-38} | {1,-20} | " -f $name, $addr
    Write-Host $leftPart -NoNewline

    if ($success -and $tcpClient.Connected) {
        $tcpClient.EndConnect($iar)
        $tcpClient.Close()
        $successCount++
        $ms = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
        Write-Host "OK ($ms ms)" -ForegroundColor Green
    } else {
        $tcpClient.Close()
        Write-Host "TIMEOUT / MAT KET NOI" -ForegroundColor Red
    }
}

Write-Host ("-" * 78)
if ($successCount -eq $services.Count) {
    Write-Host "[+++] KET NOI TOAN BO CUM HIS HOAN HAO! Ban co the mo HIS.exe de lam viec." -ForegroundColor Green
} elseif ($successCount -gt 0) {
    Write-Host "[!] Ket noi duoc $successCount/$($services.Count) dich vu. Hay kiem tra cac dich vu con lai." -ForegroundColor Yellow
} else {
    Write-Host "[---] KHONG KET NOI DUOC DICH VU NAO. Vui long bat may Subnet Router o vien." -ForegroundColor Red
}
