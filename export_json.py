import json
import time
from mitmproxy import http

# Tên file log JSON xuất ra
LOG_FILE = "winapp_session_log.json"

# Dải IP / Domain của bệnh viện muốn lọc (để tránh rác từ web khác)
TARGET_KEYWORDS = ["192.168.7.", "192.168.200."]

def response(flow: http.HTTPFlow) -> None:
    url = flow.request.pretty_url
    
    # Chỉ bắt các gói tin thuộc dải mạng bệnh viện
    if any(keyword in url for keyword in TARGET_KEYWORDS):
        
        # Đọc nội dung body an toàn (tránh lỗi nếu là file ảnh/binary)
        try:
            req_body = flow.request.get_text(strict=False)
        except Exception:
            req_body = "[Binary Data]"

        try:
            res_body = flow.response.get_text(strict=False)
        except Exception:
            res_body = "[Binary Data]"

        # Cấu trúc JSON ghi nhận 1 transaction
        log_entry = {
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "request": {
                "method": flow.request.method,
                "url": url,
                "headers": dict(flow.request.headers),
                "body": req_body
            },
            "response": {
                "status_code": flow.response.status_code,
                "headers": dict(flow.response.headers),
                "body": res_body
            }
        }

        # Ghi nối (append) vào file JSONL (JSON Lines)
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(json.dumps(log_entry, ensure_ascii=False) + "\n")
            
        print(f"✅ Đã lưu API: {flow.request.method} {url}")