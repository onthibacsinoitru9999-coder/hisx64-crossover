# -*- coding: utf-8 -*-
import os
import re
import html
from datetime import datetime

md_path = r"e:\his-x64-28-11fix GDYK\his-x64\Reports\BaoCao_HoiChan_CTCH_NinhBinh.md"
html_path = r"e:\his-x64-28-11fix GDYK\his-x64\Reports\BaoCao_HoiChan_CTCH_NinhBinh.html"
drive_dir = r"C:\Users\1995\OneDrive\BaoCaoBuongBenh_Khoa57"
drive_html_path = os.path.join(drive_dir, "BaoCao_HoiChan_CTCH_NinhBinh.html")

with open(md_path, "r", encoding="utf-8") as f:
    content = f.read()

# Tách từng bệnh nhân
sections = content.split("### ")
header_part = sections[0]
patient_sections = sections[1:]

patients = []
for sec in patient_sections:
    lines = sec.strip().split("\n")
    title_line = lines[0] # 1. Bệnh nhân: **BÙI THỊ QUÝ ** (63 tuổi - Nữ)
    p_match = re.search(r"\d+\.\s*Bệnh nhân:\s*\*\*(.*?)\*\*\s*\((.*?)\)", title_line)
    name = p_match.group(1).strip() if p_match else title_line
    meta = p_match.group(2).strip() if p_match else ""

    p_code = ""
    t_code = ""
    address = ""
    dept_room = ""
    req_time = ""
    stt = ""
    in_time = ""
    trt_stt = ""
    diag = ""
    sub_diag = ""
    opinions = []
    cdha = []
    tracking = ""

    in_opinion = False
    in_cdha = False
    curr_op = []

    for line in lines[1:]:
        if line.startswith("- **Mã Bệnh Nhân**:"):
            m = re.search(r"`(.*?)`.*?`(\d+)`", line)
            if m:
                p_code = m.group(1)
                t_code = m.group(2)
        elif line.startswith("- **Địa chỉ**:"):
            address = line.replace("- **Địa chỉ**:", "").strip()
        elif line.startswith("- **Khoa / Buồng hiện tại**:"):
            dept_room = line.replace("- **Khoa / Buồng hiện tại**:", "").strip()
        elif line.startswith("- **Thời gian gửi yêu cầu HC**:"):
            m = re.search(r"`(.*?)`.*?:\s*\*\*(.*?)\*\*", line)
            if m:
                req_time = m.group(1)
                stt = m.group(2)
        elif line.startswith("- **Thời gian vào viện**:"):
            m = re.search(r"`(.*?)`.*?: (.*)", line)
            if m:
                in_time = m.group(1)
                trt_stt = m.group(2).strip()
        elif line.startswith("- **Chẩn đoán xác định**:"):
            diag = line.replace("- **Chẩn đoán xác định**:", "").strip()
        elif line.startswith("- **Bệnh kèm theo / Tiền sử**:"):
            sub_diag = line.replace("- **Bệnh kèm theo / Tiền sử**:", "").strip()
        elif "> [!IMPORTANT]" in line or "> [!NOTE]" in line:
            in_opinion = True
            in_cdha = False
        elif "**Kết quả Chẩn đoán hình ảnh" in line:
            in_opinion = False
            in_cdha = True
            if curr_op:
                opinions.append("\n".join(curr_op))
                curr_op = []
        elif "**Diễn biến lâm sàng" in line:
            in_opinion = False
            in_cdha = False
            if curr_op:
                opinions.append("\n".join(curr_op))
                curr_op = []
        elif line.startswith("- [2026") and in_cdha:
            cdha.append(line[2:].strip())
        elif line.startswith("- [2026") and not in_cdha and "Diễn biến" not in line:
            tracking = line[2:].strip()
        elif line.startswith("> "):
            clean_l = line[2:].strip()
            if not clean_l.startswith("**Ý KIẾN") and not clean_l.startswith("**HỘI CHẨN"):
                curr_op.append(clean_l)
        elif in_opinion and line.strip():
            curr_op.append(line.strip())

    if curr_op:
        opinions.append("\n".join(curr_op))

    patients.append({
        "name": name,
        "meta": meta,
        "p_code": p_code,
        "t_code": t_code,
        "address": address,
        "dept_room": dept_room,
        "req_time": req_time,
        "stt": stt,
        "in_time": in_time,
        "trt_stt": trt_stt,
        "diag": diag,
        "sub_diag": sub_diag,
        "opinions": opinions,
        "cdha": cdha,
        "tracking": tracking
    })

# Tạo giao diện HTML cao cấp
html_out = f"""<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Báo Cáo Phòng Hội Chẩn CTCH & Cột Sống - CS2 Ninh Bình</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;600&display=swap" rel="stylesheet">
    <style>
        :root {{
            --primary: #0284c7;
            --primary-dark: #0369a1;
            --primary-light: #e0f2fe;
            --success: #16a34a;
            --success-light: #dcfce7;
            --warning: #d97706;
            --warning-light: #fef3c7;
            --danger: #dc2626;
            --danger-light: #fee2e2;
            --slate-50: #f8fafc;
            --slate-100: #f1f5f9;
            --slate-200: #e2e8f0;
            --slate-700: #334155;
            --slate-800: #1e293b;
            --slate-900: #0f172a;
        }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            background-color: #f8fafc;
            color: var(--slate-800);
            line-height: 1.5;
            padding: 24px 20px;
        }}
        .container {{
            max-width: 1300px;
            margin: 0 auto;
        }}
        .header {{
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0369a1 100%);
            color: white;
            padding: 28px 32px;
            border-radius: 16px;
            box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.25);
            margin-bottom: 24px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 16px;
        }}
        .header h1 {{
            font-size: 22px;
            font-weight: 800;
            letter-spacing: -0.5px;
            margin-bottom: 6px;
            color: #ffffff;
        }}
        .header p {{
            font-size: 13px;
            color: #94a3b8;
        }}
        .btn-print {{
            background: white;
            color: var(--slate-900);
            font-weight: 600;
            font-size: 13px;
            padding: 10px 20px;
            border-radius: 8px;
            border: none;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 8px;
            box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);
            transition: all 0.2s ease;
        }}
        .btn-print:hover {{
            background: #e2e8f0;
            transform: translateY(-1px);
        }}
        .stats-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }}
        .stat-card {{
            background: white;
            padding: 18px 20px;
            border-radius: 12px;
            border: 1px solid var(--slate-200);
            box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.05);
        }}
        .stat-label {{
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: #64748b;
            margin-bottom: 4px;
        }}
        .stat-val {{
            font-size: 26px;
            font-weight: 800;
            color: var(--slate-900);
        }}
        .search-box {{
            background: white;
            padding: 16px 20px;
            border-radius: 12px;
            border: 1px solid var(--slate-200);
            margin-bottom: 24px;
            display: flex;
            gap: 12px;
            align-items: center;
        }}
        .search-input {{
            flex: 1;
            padding: 10px 16px;
            border-radius: 8px;
            border: 1px solid var(--slate-200);
            font-size: 14px;
            outline: none;
            transition: border-color 0.2s;
        }}
        .search-input:focus {{
            border-color: var(--primary);
            box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.15);
        }}
        .patient-card {{
            background: white;
            border-radius: 14px;
            border: 1px solid var(--slate-200);
            margin-bottom: 16px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.02);
            transition: all 0.2s;
        }}
        .patient-card:hover {{
            box-shadow: 0 8px 16px -4px rgba(0,0,0,0.06);
            border-color: #cbd5e1;
        }}
        .card-header {{
            padding: 16px 20px;
            background: #ffffff;
            border-bottom: 1px solid var(--slate-100);
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 10px;
            cursor: pointer;
        }}
        .card-header-left {{
            display: flex;
            align-items: center;
            gap: 12px;
        }}
        .idx-badge {{
            width: 28px;
            height: 28px;
            border-radius: 50%;
            background: var(--primary-light);
            color: var(--primary-dark);
            font-weight: 700;
            font-size: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .patient-title {{
            font-size: 16px;
            font-weight: 700;
            color: var(--slate-900);
        }}
        .patient-sub {{
            font-size: 13px;
            color: #64748b;
            font-weight: 500;
        }}
        .badge {{
            display: inline-flex;
            align-items: center;
            padding: 4px 10px;
            border-radius: 6px;
            font-size: 12px;
            font-weight: 600;
            letter-spacing: -0.2px;
        }}
        .badge-pending {{
            background: var(--warning-light);
            color: var(--warning);
            border: 1px solid #fde68a;
        }}
        .badge-done {{
            background: var(--success-light);
            color: var(--success);
            border: 1px solid #bbf7d0;
        }}
        .badge-danger {{
            background: var(--danger-light);
            color: var(--danger);
            border: 1px solid #fecaca;
        }}
        .card-body {{
            padding: 20px;
            display: grid;
            grid-template-columns: 1.2fr 1fr;
            gap: 20px;
        }}
        @media(max-width: 900px) {{
            .card-body {{ grid-template-columns: 1fr; }}
        }}
        .info-group {{
            margin-bottom: 12px;
        }}
        .info-label {{
            font-size: 11px;
            text-transform: uppercase;
            font-weight: 700;
            color: #64748b;
            letter-spacing: 0.5px;
            margin-bottom: 3px;
        }}
        .info-val {{
            font-size: 13px;
            color: var(--slate-800);
        }}
        .diag-box {{
            background: #fff1f2;
            border-left: 4px solid #f43f5e;
            padding: 10px 14px;
            border-radius: 0 8px 8px 0;
            margin-bottom: 12px;
        }}
        .diag-title {{
            font-weight: 700;
            color: #9f1239;
            font-size: 14px;
        }}
        .opinion-box {{
            background: #f0fdf4;
            border: 1px solid #bbf7d0;
            border-left: 4px solid #16a34a;
            border-radius: 0 8px 8px 0;
            padding: 12px 14px;
            margin-bottom: 12px;
        }}
        .opinion-title {{
            font-size: 12px;
            font-weight: 700;
            color: #15803d;
            text-transform: uppercase;
            margin-bottom: 6px;
        }}
        .opinion-content {{
            font-size: 13px;
            color: #14532d;
            white-space: pre-wrap;
            line-height: 1.6;
        }}
        .cdha-box {{
            background: #f8fafc;
            border: 1px solid var(--slate-200);
            border-radius: 8px;
            padding: 12px;
            margin-bottom: 12px;
        }}
        .cdha-item {{
            font-size: 12px;
            margin-bottom: 6px;
            padding-bottom: 6px;
            border-bottom: 1px dashed #e2e8f0;
            line-height: 1.4;
        }}
        .cdha-item:last-child {{
            margin-bottom: 0;
            padding-bottom: 0;
            border-bottom: none;
        }}
        .code-tag {{
            font-family: 'JetBrains Mono', monospace;
            background: var(--slate-100);
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 11px;
            font-weight: 600;
            color: var(--slate-700);
            border: 1px solid var(--slate-200);
        }}
        @media print {{
            body {{ padding: 0; background: white; }}
            .header {{ border-radius: 0; box-shadow: none; padding: 16px; }}
            .btn-print, .search-box {{ display: none; }}
            .patient-card {{ break-inside: avoid; border: 1px solid #ccc; margin-bottom: 20px; }}
        }}
    </style>
</head>
<body>
<div class="container">
    <div class="header">
        <div>
            <h1>🏥 BÁO CÁO PHÒNG HỘI CHẨN KHOA CTCH & PHẪU THUẬT CỘT SỐNG</h1>
            <p>Bệnh Viện Bạch Mai - Cơ Sở 2 Ninh Bình (Phòng Hội Chẩn ID: 18686 | Cập nhật lúc {datetime.now().strftime("%d/%m/%Y %H:%M:%S")})</p>
        </div>
        <div>
            <button class="btn-print" onclick="window.print()">
                <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M2.5 8a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1z"/><path d="M5 1a2 2 0 0 0-2 2v2H2a2 2 0 0 0-2 2v3a2 2 0 0 0 2 2h1v1a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2v-1h1a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-1V3a2 2 0 0 0-2-2H5zM4 3a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2H4V3zm1 5a2 2 0 0 0-2 2v1H2a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3a1 1 0 0 1-1 1h-1v-1a2 2 0 0 0-2-2H5zm7 2v3a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-3a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1z"/></svg>
                In / Xuất PDF A4
            </button>
        </div>
    </div>

    <div class="stats-grid">
        <div class="stat-card">
            <div class="stat-label">Tổng Ca Hội Chẩn Gần Đây</div>
            <div class="stat-val" style="color: var(--primary);">{len(patients)}</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Đang Chờ Xử Lý (Chưa HC)</div>
            <div class="stat-val" style="color: var(--warning);">{len([p for p in patients if 'Chưa' in p['stt']])}</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Đã Có Kết Luận Hội Chẩn</div>
            <div class="stat-val" style="color: var(--success);">{len([p for p in patients if 'Hoàn' in p['stt']])}</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Đang Điều Trị Nội Trú</div>
            <div class="stat-val" style="color: var(--slate-900);">{len([p for p in patients if 'điều trị' in p['trt_stt']])}</div>
        </div>
    </div>

    <div class="search-box">
        <input type="text" id="searchInput" class="search-input" placeholder="🔍 Tìm kiếm tức thì theo tên bệnh nhân, mã BN, khoa phòng, hoặc bệnh lý..." onkeyup="filterPatients()">
    </div>

    <div id="patientList">
"""

for idx, p in enumerate(patients, start=1):
    stt_class = "badge-pending" if "Chưa" in p['stt'] else "badge-done"
    stt_icon = "🟡" if "Chưa" in p['stt'] else "🟢"

    opinions_html = ""
    if p['opinions']:
        for op in p['opinions']:
            opinions_html += f"""
            <div class="opinion-box">
                <div class="opinion-title">📋 Ý Kiến & Kết Luận Chuyên Khoa CTCH & Cột Sống:</div>
                <div class="opinion-content">{html.escape(op)}</div>
            </div>"""
    else:
        opinions_html = """
        <div class="opinion-box" style="background:#fffbeb; border-color:#fde68a; border-left-color:#d97706;">
            <div class="opinion-title" style="color:#b45309;">⏳ Trạng thái:</div>
            <div class="opinion-content" style="color:#92400e;">Phiếu yêu cầu hội chẩn đang chờ Bác sĩ chuyên khoa CTCH & Cột sống tiếp nhận và đưa ra kết luận.</div>
        </div>"""

    cdha_html = ""
    if p['cdha']:
        items = "".join([f'<div class="cdha-item">• {html.escape(c)}</div>' for c in p['cdha']])
        cdha_html = f"""
        <div class="cdha-box">
            <div class="info-label" style="margin-bottom:6px;">📷 Cận lâm sàng & Chẩn đoán hình ảnh:</div>
            {items}
        </div>"""

    html_out += f"""
    <div class="patient-card" data-search="{html.escape(p['name'].lower())} {html.escape(p['p_code'])} {html.escape(p['dept_room'].lower())} {html.escape(p['diag'].lower())}">
        <div class="card-header">
            <div class="card-header-left">
                <div class="idx-badge">{idx}</div>
                <div>
                    <span class="patient-title">{html.escape(p['name'])}</span>
                    <span class="patient-sub">({html.escape(p['meta'])})</span>
                    <span class="code-tag" style="margin-left:6px;">Mã BN: {html.escape(p['p_code'])}</span>
                    <span class="code-tag">Mã ĐT: {html.escape(p['t_code'])}</span>
                </div>
            </div>
            <div>
                <span class="badge {stt_class}">{stt_icon} {html.escape(p['stt'])}</span>
            </div>
        </div>
        <div class="card-body">
            <div>
                <div class="diag-box">
                    <div class="info-label" style="color:#9f1239;">Chẩn đoán chính:</div>
                    <div class="diag-title">{html.escape(p['diag'])}</div>
                    {f'<div style="font-size:12px; color:#be123c; margin-top:4px;">Bệnh kèm: {html.escape(p["sub_diag"])}</div>' if p["sub_diag"] else ''}
                </div>
                <div class="info-group">
                    <div class="info-label">Khoa / Buồng chỉ định:</div>
                    <div class="info-val"><strong>{html.escape(p['dept_room'])}</strong></div>
                </div>
                <div class="info-group">
                    <div class="info-label">Địa chỉ & Thời gian:</div>
                    <div class="info-val">{html.escape(p['address'])} | Vào viện: {html.escape(p['in_time'])} ({html.escape(p['trt_stt'])})</div>
                </div>
                {f'<div class="info-group"><div class="info-label">Diễn biến gần nhất:</div><div class="info-val" style="font-size:12px; color:#475569;">{html.escape(p["tracking"])}</div></div>' if p["tracking"] else ''}
            </div>
            <div>
                {opinions_html}
                {cdha_html}
            </div>
        </div>
    </div>
"""

html_out += """
    </div>
</div>

<script>
function filterPatients() {
    let query = document.getElementById('searchInput').value.toLowerCase().trim();
    let cards = document.querySelectorAll('.patient-card');
    cards.forEach(card => {
        let searchData = card.getAttribute('data-search');
        if (searchData.includes(query)) {
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

with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_out)

if os.path.exists(drive_dir):
    with open(drive_html_path, "w", encoding="utf-8") as f:
        f.write(html_out)
    print(f"Synced to Drive: {drive_html_path}")

print(f"Generated HTML report: {html_path}")
