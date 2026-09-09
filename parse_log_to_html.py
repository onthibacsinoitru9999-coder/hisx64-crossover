import json
import html
import os

LOG_FILE = "winapp_session_log.json"
OUTPUT_HTML = "api_viewer.html"

def load_logs():
    logs = []
    if not os.path.exists(LOG_FILE):
        print(f"❌ Không tìm thấy file {LOG_FILE}. Hãy chắc chắn bạn đã chạy mitmproxy để tạo log!")
        return logs

    with open(LOG_FILE, "r", encoding="utf-8") as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                data = json.loads(line)
                logs.append(data)
            except json.JSONDecodeError:
                continue
    return logs

def generate_html(logs):
    html_content = f"""<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bảng Nhật Ký API Bệnh Viện</title>
    <style>
        :root {{
            --bg-primary: #f8fafc;
            --text-primary: #0f172a;
            --border-color: #e2e8f0;
            --card-bg: #ffffff;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            background-color: var(--bg-primary);
            color: var(--text-primary);
            margin: 0;
            padding: 20px;
        }}
        .header {{
            margin-bottom: 20px;
        }}
        .stats {{
            font-size: 14px;
            color: #64748b;
        }}
        .search-box {{
            width: 100%;
            padding: 10px 14px;
            font-size: 15px;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            margin-bottom: 20px;
            box-sizing: border-box;
        }}
        .api-card {{
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 8px;
            margin-bottom: 12px;
            overflow: hidden;
            box-shadow: 0 1px 3px rgba(0,0,0,0.05);
        }}
        .api-header {{
            display: flex;
            align-items: center;
            padding: 12px 16px;
            cursor: pointer;
            gap: 12px;
            user-select: none;
        }}
        .api-header:hover {{
            background-color: #f1f5f9;
        }}
        .badge {{
            padding: 4px 8px;
            border-radius: 4px;
            font-weight: bold;
            font-size: 12px;
            text-transform: uppercase;
        }}
        .method-GET {{ background: #e0f2fe; color: #0369a1; }}
        .method-POST {{ background: #dcfce7; color: #15803d; }}
        .status-200 {{ background: #dcfce7; color: #166534; }}
        .status-error {{ background: #fee2e2; color: #991b1b; }}
        .url {{
            font-family: monospace;
            font-size: 13px;
            flex-grow: 1;
            word-break: break-all;
        }}
        .time {{
            font-size: 12px;
            color: #94a3b8;
        }}
        .api-body {{
            display: none;
            padding: 16px;
            border-top: 1px solid var(--border-color);
            background-color: #fafafa;
        }}
        .section-title {{
            font-weight: bold;
            font-size: 13px;
            margin-top: 12px;
            margin-bottom: 6px;
            color: #475569;
        }}
        pre {{
            background: #0f172a;
            color: #f8fafc;
            padding: 12px;
            border-radius: 6px;
            font-size: 12px;
            overflow-x: auto;
            max-height: 250px;
        }}
    </style>
</head>
<body>

    <div class="header">
        <h2>📑 Nhật Ký Lệnh API Bệnh Viện</h2>
        <div class="stats">Tổng số gói tin bắt được: <strong>{len(logs)}</strong></div>
    </div>

    <input type="text" id="searchInput" class="search-box" placeholder="🔍 Tìm kiếm theo URL, Mã điều trị, Tên xét nghiệm..." onkeyup="filterLogs()">

    <div id="logList">
"""

    for idx, item in enumerate(logs):
        req = item.get("request", {})
        res = item.get("response", {})
        
        method = req.get("method", "GET")
        url_str = req.get("url", "")
        status = res.get("status_code", 0)
        timestamp = item.get("timestamp", "")
        
        status_class = "status-200" if 200 <= status < 300 else "status-error"
        method_class = f"method-{method}" if method in ["GET", "POST"] else "method-GET"

        # Định dạng JSON cho đẹp mắt nếu có
        try:
            req_body_formatted = json.dumps(json.loads(req.get("body", "")), indent=2, ensure_ascii=False)
        except Exception:
            req_body_formatted = req.get("body", "")

        try:
            res_body_formatted = json.dumps(json.loads(res.get("body", "")), indent=2, ensure_ascii=False)
        except Exception:
            res_body_formatted = res.get("body", "")

        html_content += f"""
        <div class="api-card" data-search="{html.escape((url_str + ' ' + req_body_formatted + ' ' + res_body_formatted).lower())}">
            <div class="api-header" onclick="toggleDetails({idx})">
                <span class="badge {method_class}">{method}</span>
                <span class="badge {status_class}">{status}</span>
                <span class="url">{html.escape(url_str)}</span>
                <span class="time">{timestamp}</span>
            </div>
            <div class="api-body" id="details-{idx}">
                <div class="section-title">📥 Request Headers & Parameters</div>
                <pre>{html.escape(json.dumps(req.get('headers', {{}}), indent=2))}</pre>
                
                {'<div class="section-title">📤 Request Body</div><pre>' + html.escape(req_body_formatted) + '</pre>' if req_body_formatted else ''}
                
                <div class="section-title">📄 Response Body (Dữ liệu trả về)</div>
                <pre>{html.escape(res_body_formatted)}</pre>
            </div>
        </div>
        """

    html_content += """
    </div>

    <script>
        function toggleDetails(id) {
            const el = document.getElementById('details-' + id);
            el.style.display = el.style.display === 'block' ? 'none' : 'block';
        }

        function filterLogs() {
            const query = document.getElementById('searchInput').value.toLowerCase();
            const cards = document.querySelectorAll('.api-card');
            
            cards.forEach(card => {
                const searchText = card.getAttribute('data-search');
                if (searchText.includes(query)) {
                    card.style.display = 'block';
                } else {
                    card.style.display = 'none';
                }
            });
        }
    </script>
</body>
</html>
"""

    with open(OUTPUT_HTML, "w", encoding="utf-8") as f:
        f.write(html_content)

    print(f"🎉 Đã tạo thành công file {OUTPUT_HTML}!")
    print(f"👉 Hãy nhấp đúp vào file {OUTPUT_HTML} để mở trên trình duyệt.")

if __name__ == "__main__":
    logs = load_logs()
    if logs:
        generate_html(logs)