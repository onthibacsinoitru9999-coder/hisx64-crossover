import re, os, json
from datetime import datetime

md_path = r"F:\NB\winrar\HIS CSNB\.agents\skills\his-clinical-operations\scripts\Reports\BaoCao_CatCutChi_2026.md"
html_out_path = r"F:\NB\winrar\HIS CSNB\Reports\BaoCao_CatCutChi_2026.html"

os.makedirs(os.path.dirname(html_out_path), exist_ok=True)

with open(md_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

patients = []
for line in lines:
    line = line.strip()
    if line.startswith("|") and not line.startswith("| STT") and not line.startswith("|:---:"):
        parts = [p.strip() for p in line.split("|")[1:-1]]
        if len(parts) >= 11:
            patients.append({
                "stt": parts[0],
                "code": parts[1],
                "name": parts[2].replace("**", ""),
                "age": parts[3],
                "gender": parts[4],
                "time": parts[5],
                "service": parts[6],
                "room": parts[7],
                "ptv": parts[8],
                "dept": parts[9],
                "diag": parts[10]
            })

# Statistics
total_cases = len(patients)
dept_counts = {}
service_counts = {}
room_counts = {}
gender_counts = {"Nam": 0, "Nữ": 0}

for p in patients:
    d = p["dept"]
    dept_counts[d] = dept_counts.get(d, 0) + 1
    
    s = p["service"]
    service_counts[s] = service_counts.get(s, 0) + 1
    
    r = p["room"]
    room_counts[r] = room_counts.get(r, 0) + 1
    
    g = p["gender"]
    if g in gender_counts:
        gender_counts[g] += 1

html_content = f"""<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Báo Cáo Phẫu Thuật Cắt Cụt Chi Năm 2026 - BV Bạch Mai</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333; }}
        .header-box {{ background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: white; padding: 30px 20px; border-radius: 0 0 15px 15px; margin-bottom: 25px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }}
        .stat-card {{ background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); border-left: 5px solid #2a5298; transition: transform 0.2s; }}
        .stat-card:hover {{ transform: translateY(-3px); }}
        .badge-surgery {{ background-color: #dc3545; color: white; padding: 4px 8px; border-radius: 4px; font-weight: 500; font-size: 0.85rem; }}
        .table-container {{ background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
        .search-box {{ border-radius: 20px; padding: 10px 20px; border: 1px solid #ced4da; width: 100%; max-width: 400px; }}
        .filter-select {{ border-radius: 20px; padding: 8px 15px; }}
        table th {{ background-color: #f8f9fa; font-weight: 600; color: #495057; }}
        .highlight {{ font-weight: bold; color: #1e3c72; }}
    </style>
</head>
<body>

<div class="header-box">
    <div class="container-fluid">
        <div class="d-flex justify-content-between align-items-center">
            <div>
                <h2><i class="fa-solid fa-hospital me-2"></i> BÁO CÁO DỮ LIỆU PHÒNG MỔ: PHẪU THUẬT CẮT CỤT CHI NĂM 2026</h2>
                <p class="mb-0 opacity-75"><i class="fa-solid fa-server me-1"></i> Máy chủ: Bệnh viện Bạch Mai - Hà Nội (MOS 192.168.7.236:1608) | Trích xuất: 15/09/2026</p>
            </div>
            <div>
                <button onclick="window.print()" class="btn btn-light btn-sm fw-bold"><i class="fa-solid fa-print me-1"></i> In Báo Cáo / Xuất PDF</button>
            </div>
        </div>
    </div>
</div>

<div class="container-fluid px-4">
    <!-- Stat Cards -->
    <div class="row g-3 mb-4">
        <div class="col-md-3">
            <div class="stat-card" style="border-left-color: #e63946;">
                <div class="text-muted small">TỔNG SỐ CA PHẪU THUẬT</div>
                <h3 class="fw-bold text-danger mb-0">{total_cases}</h3>
                <div class="small text-secondary mt-1">Đã phẫu thuật tại phòng mổ 2026</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="stat-card" style="border-left-color: #457b9d;">
                <div class="text-muted small">GIỚI TÍNH</div>
                <h3 class="fw-bold mb-0" style="color: #457b9d;">{gender_counts['Nam']} Nam / {gender_counts['Nữ']} Nữ</h3>
                <div class="small text-secondary mt-1">Tỷ lệ nam chiếm {round(gender_counts['Nam']*100/max(1, total_cases), 1)}%</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="stat-card" style="border-left-color: #2a9d8f;">
                <div class="text-muted small">KHOA ĐIỀU TRỊ CHÍNH</div>
                <h4 class="fw-bold mb-0 text-truncate" style="color: #2a9d8f;">{list(sorted(dept_counts.items(), key=lambda x: x[1], reverse=True))[0][0] if dept_counts else 'N/A'}</h4>
                <div class="small text-secondary mt-1">{list(sorted(dept_counts.items(), key=lambda x: x[1], reverse=True))[0][1] if dept_counts else 0} ca phẫu thuật</div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="stat-card" style="border-left-color: #f4a261;">
                <div class="text-muted small">PHÒNG MỔ THỰC HIỆN NHIỀU NHẤT</div>
                <h4 class="fw-bold mb-0 text-truncate" style="color: #e76f51;">{list(sorted(room_counts.items(), key=lambda x: x[1], reverse=True))[0][0] if room_counts else 'N/A'}</h4>
                <div class="small text-secondary mt-1">{list(sorted(room_counts.items(), key=lambda x: x[1], reverse=True))[0][1] if room_counts else 0} lượt phẫu thuật</div>
            </div>
        </div>
    </div>

    <!-- Table Container -->
    <div class="table-container mb-5">
        <div class="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2">
            <div class="d-flex gap-2 align-items-center">
                <input type="text" id="searchInput" class="search-box" placeholder="🔍 Tìm theo Tên BN, Mã BN, Chẩn đoán..." onkeyup="filterTable()">
                <select id="deptFilter" class="form-select filter-select" onchange="filterTable()">
                    <option value="">-- Tất cả Khoa phòng --</option>
                    {"".join(f'<option value="{k}">{k} ({v})</option>' for k, v in sorted(dept_counts.items(), key=lambda x: x[1], reverse=True))}
                </select>
            </div>
            <div class="text-muted small">
                Hiển thị: <span id="recordCount" class="fw-bold text-primary">{total_cases}</span> / {total_cases} bệnh nhân
            </div>
        </div>

        <div class="table-responsive">
            <table class="table table-hover table-striped align-middle" id="patientTable">
                <thead class="table-light">
                    <tr>
                        <th class="text-center">STT</th>
                        <th>Mã BN</th>
                        <th>Họ và Tên</th>
                        <th class="text-center">Tuổi</th>
                        <th class="text-center">Giới</th>
                        <th>Thời Gian Mổ</th>
                        <th>Kỹ Thuật Phẫu Thuật</th>
                        <th>Phòng Mổ</th>
                        <th>PTV / Kíp Mổ</th>
                        <th>Khoa Điều Trị</th>
                        <th>Chẩn Đoán Bệnh</th>
                    </tr>
                </thead>
                <tbody>
"""

for idx, p in enumerate(patients, 1):
    html_content += f"""                    <tr>
                        <td class="text-center">{idx}</td>
                        <td><code>{p['code']}</code></td>
                        <td class="highlight">{p['name']}</td>
                        <td class="text-center">{p['age']}</td>
                        <td class="text-center">{p['gender']}</td>
                        <td><small>{p['time']}</small></td>
                        <td><span class="badge-surgery">{p['service']}</span></td>
                        <td><small>{p['room']}</small></td>
                        <td><small>{p['ptv']}</small></td>
                        <td><small class="fw-semibold">{p['dept']}</small></td>
                        <td><small class="text-muted">{p['diag']}</small></td>
                    </tr>\n"""

html_content += """                </tbody>
            </table>
        </div>
    </div>
</div>

<script>
function filterTable() {
    let input = document.getElementById("searchInput").value.toLowerCase();
    let dept = document.getElementById("deptFilter").value.toLowerCase();
    let table = document.getElementById("patientTable");
    let tr = table.getElementsByTagName("tr");
    let count = 0;

    for (let i = 1; i < tr.length; i++) {
        let text = tr[i].textContent.toLowerCase();
        let deptText = tr[i].cells[9] ? tr[i].cells[9].textContent.toLowerCase() : "";
        
        let matchInput = text.includes(input);
        let matchDept = (dept === "" || deptText.includes(dept));

        if (matchInput && matchDept) {
            tr[i].style.display = "";
            count++;
        } else {
            tr[i].style.display = "none";
        }
    }
    document.getElementById("recordCount").innerText = count;
}
</script>

</body>
</html>
"""

with open(html_out_path, "w", encoding="utf-8") as f:
    f.write(html_content)

print(f"✔ Đã tạo thành công giao diện báo cáo HTML: {html_out_path}")
