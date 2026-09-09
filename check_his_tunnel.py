# -*- coding: utf-8 -*-
import socket
import time
import subprocess
import json
import sys

HIS_SERVICES = [
    {"name": "ACS (Xác thực / Đăng nhập)", "ip": "192.168.7.200", "port": 1401},
    {"name": "SDA (Cấu hình hệ thống / Danh mục)", "ip": "192.168.7.200", "port": 1410},
    {"name": "MOS (Nghiệp vụ khám chữa bệnh)", "ip": "192.168.7.236", "port": 1608},
    {"name": "EMR (Bệnh án điện tử)", "ip": "192.168.7.239", "port": 1415},
    {"name": "FSS (Lưu trữ file, ảnh, mẫu)", "ip": "192.168.7.216", "port": 1405},
    {"name": "SAR (Báo cáo tổng hợp)", "ip": "192.168.7.200", "port": 1409},
    {"name": "MRS (Báo cáo y tế)", "ip": "192.168.7.200", "port": 1413},
    {"name": "AUP (Tự động cập nhật)", "ip": "192.168.7.200", "port": 1468},
    {"name": "PACS (Chẩn đoán hình ảnh)", "ip": "192.168.7.200", "port": 5000},
]

def check_tailscale_status():
    print("=" * 65)
    print(" 1. KIỂM TRA TRẠNG THÁI TAILSCALE TẠI MÁY NÀY")
    print("=" * 65)
    try:
        res = subprocess.run(["tailscale", "status", "--json"], capture_output=True, text=True, check=True)
        data = json.loads(res.stdout)
        self_node = data.get("Self", {})
        print(f"[+] IP Tailscale máy này: {self_node.get('TailscaleIPs', ['N/A'])[0]} ({self_node.get('HostName', 'N/A')})")
        
        peers = data.get("Peer", {})
        router_found = False
        for node_key, peer in peers.items():
            hostname = peer.get("HostName", "")
            online = peer.get("Online", False)
            routes = peer.get("PrimaryRoutes", []) or peer.get("AllowedIPs", [])
            has_his_route = any("192.168.7.0/24" in str(r) for r in routes)
            
            if has_his_route:
                router_found = True
                status_str = "ONLINE (Đang kết nối)" if online else "OFFLINE (Không hoạt động!)"
                relay_mode = peer.get("CurAddr") or ("Relay: " + peer.get("Relay", "N/A") if peer.get("Relay") else "N/A")
                print(f"[!] Máy làm Subnet Router ở viện: {hostname} ({peer.get('TailscaleIPs', ['N/A'])[0]})")
                print(f"    - Trạng thái: {status_str}")
                print(f"    - Chế độ kết nối: {relay_mode}")
                print(f"    - Lần cuối nhìn thấy: {peer.get('LastSeen', 'N/A')}")
                if not online:
                    print("    => CẢNH BÁO: Máy này đang TẮT hoặc mất kết nối mạng ở viện!")
                    print("       Cần bật máy này ở viện lên thì mới truy cập được HIS!")
        
        if not router_found:
            print("[-] Chưa tìm thấy máy nào trong Tailscale chia sẻ dải mạng 192.168.7.0/24!")
    except Exception as e:
        print(f"[-] Lỗi khi gọi tailscale CLI: {e}")

def test_tcp_port(ip, port, timeout=3.0):
    start = time.time()
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(timeout)
    try:
        s.connect((ip, port))
        elapsed = (time.time() - start) * 1000
        s.close()
        return True, elapsed
    except Exception as e:
        return False, str(e)

def check_all_his_services():
    print("\n" + "=" * 65)
    print(" 2. KIỂM TRA KẾT NỐI TỚI CÁC CỔNG MÁY CHỦ HIS (192.168.7.x)")
    print("=" * 65)
    print(f"{'Dịch vụ HIS':<35} | {'Địa chỉ IP:Port':<22} | {'Kết quả':<15}")
    print("-" * 78)
    
    success_count = 0
    for svc in HIS_SERVICES:
        ip = svc["ip"]
        port = svc["port"]
        name = svc["name"]
        addr_str = f"{ip}:{port}"
        
        ok, detail = test_tcp_port(ip, port, timeout=2.0)
        if ok:
            success_count += 1
            res_str = f"OK ({detail:.1f} ms)"
        else:
            res_str = "TIMEOUT / LỖI"
        
        print(f"{name:<35} | {addr_str:<22} | {res_str:<15}")

    print("-" * 78)
    if success_count == len(HIS_SERVICES):
        print("[+++] KẾT NỐI TOÀN BỘ CỤM HIS HOÀN HẢO! Bạn có thể mở HIS.exe để làm việc.")
    elif success_count > 0:
        print(f"[!] Kết nối được {success_count}/{len(HIS_SERVICES)} dịch vụ. Kiểm tra lại server bị thiếu.")
    else:
        print("[---] KHÔNG KẾT NỐI ĐƯỢC DỊCH VỤ NÀO. Vui lòng kiểm tra lại máy Subnet Router ở viện.")

if __name__ == "__main__":
    check_tailscale_status()
    check_all_his_services()
