---
name: his-emergency-surgery
description: >-
  Đăng ký bệnh nhân vào Danh sách mổ cấp cứu tự động trên Google Forms, phân luồng độc lập
  giữa Cơ sở Hà Nội (Khoa 57) và Cơ sở Ninh Bình (Khoa 915). Tự động tra cứu thông tin bệnh nhân,
  chẩn đoán, buồng giường từ HIS, điền sẵn thông tin và hỗ trợ tạo link xem 1-Click hoặc nạp trực tiếp qua POST.
---

# HIS Emergency Surgery Registration Skill (Kỹ Năng Đăng Ký Mổ Cấp Cứu)

Kỹ năng này cung cấp quy trình và công cụ tự động hóa toàn diện để **thêm bệnh nhân vào danh sách mổ cấp cứu**, tự động phân luồng theo 2 cơ sở (Hà Nội & Ninh Bình) qua Google Forms chính thức của Bệnh viện Bạch Mai.

Công cụ tự động trích xuất nhân khẩu học, mã điều trị, buồng giường và chẩn đoán từ hệ thống HIS/MOS thông qua `HisClinicalCli.exe lookup <MãBN>`, giúp Bác sĩ chỉ cần cung cấp Mã bệnh nhân và Cách thức mổ dự kiến.

---

## 1. Thông Số & Tài Khoản Mặc Định

- **Bác sĩ chỉ định mặc định**: **`034727`** - **Ths.BS Nguyễn Hữu Sâm** (Áp dụng chung cho **TẤT CẢ** các cơ sở).
- **Phân luồng cơ sở**:
  - 🏥 **Hà Nội (`HN`)**: Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57 - `DEPARTMENT_ID = 57`).
  - 🏥 **Ninh Bình (`NB`)**: Khoa Ngoại tổng hợp - Tầng 3 Nhà E (Khoa 915 - `DEPARTMENT_ID = 915`).

---

## 2. Ma Trận Phân Luồng Form Giữa Hai Cơ Sở

| Đặc tính / Trường thông tin | Cơ sở Ninh Bình (`NB`) | Cơ sở Hà Nội (`HN`) |
| :--- | :--- | :--- |
| **Link Xem Form (View URL)** | [Google Form Mổ Cấp Cứu NB](https://docs.google.com/forms/d/e/1FAIpQLScn9LfQxqVPL0A-uVcLRDFwTah6GpgKNDabhcONXycLJ8ALkQ/viewform) | [Google Form Mổ Cấp Cứu HN](https://docs.google.com/forms/d/e/1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/viewform) |
| **Action URL (POST)** | `.../1FAIpQLScn9LfQxqVPL0A-uVcLRDFwTah6GpgKNDabhcONXycLJ8ALkQ/formResponse` | `.../1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/formResponse` |
| **Vị trí bệnh nhân** | *Không có* | `entry.1771260210` (Mặc định: `Nội trú tại khoa CTCH & CS`) |
| **Khoa điều trị** | *Không có* | `entry.1225676642` (Mặc định: `Khoa 57 - CTCH & Cột sống`) |
| **Họ và tên BN** | `entry.744010330` *(Bắt buộc)* | `entry.1876536986` *(Bắt buộc)* |
| **Tuổi BN** | `entry.1326381732` *(Bắt buộc, số nguyên)* | `entry.2076581334` *(Bắt buộc, số nguyên)* |
| **Giới tính** | `entry.1146288352` (`Nam` / `Nữ`) | `entry.688804617` (`Nam` / `Nữ`) |
| **Mã bệnh nhân** | `entry.938074737` *(Bắt buộc)* | `entry.1034142534` *(Bắt buộc)* |
| **Mã điều trị** | *Không có* | `entry.1407489499` *(Bắt buộc)* |
| **Buồng / Giường** | `entry.300014506` *(Bắt buộc)* | `entry.1681626113` *(Bắt buộc, format: Khoa 57 / ...)* |
| **Chẩn đoán** | `entry.732172205` *(Bắt buộc)* | `entry.1982187638` *(Bắt buộc)* |
| **Cách thức mổ dự kiến** | `entry.437609881` *(Bắt buộc)* | `entry.1121377833` *(Bắt buộc)* |
| **Phân loại cấp cứu** | *Không có* | `entry.777442342` (Mặc định: `Cấp cứu`) |
| **Bác sĩ chỉ định** | `entry.961181856` (Mặc định: `Ths.BS Nguyễn Hữu Sâm (034727)`) | `entry.716253062` (Mặc định: `Ths.BS Nguyễn Hữu Sâm (034727)`) |
| **PTV chính** | *Không có* | `entry.1993991632` *(Tùy chọn)* |
| **PTV phụ** | *Không có* | `entry.1769648167` *(Tùy chọn)* |
| **Ghi chú** | `entry.1922640834` *(Tùy chọn)* | `entry.1348503941` *(Tùy chọn)* |

---

## 3. Kiến Trúc Hoạt Động & Pipeline Xử Lý

```mermaid
flowchart TD
    A["Yêu cầu Bác sĩ / Agent:<br>Mã BN + Cách thức mổ + Cơ sở"]
    --> B["HisClinicalCli.exe lookup &lt;MãBN&gt;"]
    --> C["Tự động trích xuất:<br>Tên, Tuổi, Giới, Mã ĐT, Buồng/Giường, Chẩn đoán"]
    --> D{"Nhận diện Cơ sở"}
    D -->|Hà Nội (HN)| E["Map Entry Form Hà Nội<br>Vị trí: Nội trú Khoa 57<br>Phân loại: Cấp cứu<br>BS: Ths.BS Nguyễn Hữu Sâm"]
    D -->|Ninh Bình (NB)| F["Map Entry Form Ninh Bình<br>Khoa Ngoại T3 Nhà E<br>BS: Ths.BS Nguyễn Hữu Sâm"]
    E --> G["Tạo Link 1-Click (Pre-filled URL)"]
    F --> G
    G --> H{"Cờ --submit?"}
    H -->|Không / --dry-run| I["In Link cho Bác sĩ bấm duyệt 1-Click"]
    H -->|Có --submit| J["HTTP POST formResponse<br>Đăng ký trực tiếp vào Google Sheet"]
```

---

## 4. Hướng Dẫn Sử Dụng CLI

### 4.1. Cú pháp cơ bản

```powershell
# Chế độ kiểm tra & lấy link 1-Click (Dry Run) - Cơ sở Hà Nội
.\HisEmergencySurgery.bat 0004060486 "Phẫu thuật kết hợp xương kim Kirschner ngón 5 bàn tay phải" HN --dry-run

# Chế độ kiểm tra & lấy link 1-Click - Cơ sở Ninh Bình
.\HisEmergencySurgery.bat 0004060486 "Phẫu thuật kết hợp xương ngón 5 tay phải" NB --dry-run

# Gửi trực tiếp lên danh sách mổ cấp cứu (--submit)
.\HisEmergencySurgery.bat 0004060486 "Phẫu thuật kết hợp xương kim Kirschner ngón 5 bàn tay phải" HN --submit
```

### 4.2. Các cờ tham số chi tiết (Qua Python Core)

```powershell
python .agents\skills\his-emergency-surgery\scripts\his_emergency_surgery.py [MãBN] "[CáchThứcMổ]" [HN|NB] [Tùy chọn]
```

- `--facility <HN|NB>`: Chỉ định đích danh cơ sở (Hà Nội hoặc Ninh Bình).
- `--patient <MãBN>`: Mã số bệnh nhân.
- `--surgery "<NộiDung>"`: Cách thức mổ dự kiến.
- `--urgency "<PhânLoại>"`: Phân loại cấp cứu (Áp dụng cho HN):
  - `'Cấp cứu'` *(Mặc định)*
  - `'Cấp cứu (nặng)'`
  - `'Cấp cứu có trì hoãn'`
  - `'Mổ phiên hữu trùng'`
  - `'Chưa phân loại'`
- `--position "<VịTrí>"`: Vị trí bệnh nhân (Áp dụng cho HN):
  - `'Nội trú tại khoa CTCH & CS'` *(Mặc định)*
  - `'BN khoa khác (hội chẩn ngoại khoa)'`
  - `'BN từ khoa Cấp cứu'`
  - `'BN ngoại trú / phòng khám vào mổ cấp cứu'`
- `--surgeon "<TênPTV>"`: Phẫu thuật viên chính (Tùy chọn, VD: `BS Hà Đức Cường`).
- `--assistant "<TênPhụMổ>"`: Phụ mổ (Tùy chọn, VD: `Ths.BS Nguyễn Hữu Sâm`).
- `--note "<GhiChú>"`: Ghi chú lâm sàng hoặc lưu ý gây mê hồi sức.
- `--submit`: Gửi HTTP POST trực tiếp lên Google Forms (`formResponse`).
- `--dry-run`: Chỉ trích xuất thông tin, đối chiếu trường dữ liệu và in link xem trước.
- `--open-browser`: Tự động mở link form đã điền sẵn trên trình duyệt mặc định.
- `--json`: Xuất dữ liệu định dạng JSON để phục vụ tự động hóa và MCP tool.

---

## 5. Ví Dụ Thực Tế & Mẫu Kết Quả

### Đầu vào:
```powershell
.\HisEmergencySurgery.bat 0004060486 "Phẫu thuật kết hợp xương kim Kirschner ngón 5 bàn tay phải" HN --dry-run
```

### Kết quả hiển thị:
```text
===============================================================================
🚨 [ĐĂNG KÝ DANH SÁCH MỔ CẤP CỨU - BỆNH VIỆN BẠCH MAI - HÀ NỘI]
• Khoa: Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)
• Bác sĩ chỉ định: Ths.BS Nguyễn Hữu Sâm (034727)
-------------------------------------------------------------------------------
👤 BỆNH NHÂN:   TRẦN HẢI DƯƠNG (30 tuổi - Nam)
🆔 MÃ BN:       0004060486 | MÃ ĐT: 000007275651
🛏️ BUỒNG/GIƯỜNG: Phòng 733 - Giường số 85
🩺 CHẨN ĐOÁN:   [S62.61] Gãy hở ngón 5 tay phải
🔪 CÁCH THỨC MỔ: Phẫu thuật kết hợp xương kim Kirschner ngón 5 bàn tay phải
⚡ PHÂN LOẠI:   Cấp cứu | VỊ TRÍ: Nội trú tại khoa CTCH & CS
-------------------------------------------------------------------------------
🌐 LINK XEM & ĐIỀN FORM 1-CLICK (PRE-FILLED URL):
https://docs.google.com/forms/d/e/1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/viewform?usp=pp_url&entry.1771260210=N%E1%BB%99i+tr%C3%BA+t%E1%BA%A1i+khoa+CTCH+%26+CS&entry.1225676642=Khoa+57&entry.1876536986=TR%E1%BA%A6N+H%E1%BA%A2I+D%C6%AF%C6%A0NG&entry.2076581334=30&entry.688804617=Nam&entry.1034142534=0004060486&entry.1407489499=000007275651&entry.1681626113=Khoa+57+%2F+Ph%C3%B2ng+733+-+Gi%C6%B0%E1%BB%9Dng+s%E1%BB%91+85&entry.1982187638=%5BS62.61%5D+G%C3%A3y+h%E1%BB%9F+ng%C3%B3n+5+tay+ph%E1%BA%A3i&entry.1121377833=Ph%E1%BA%ABu+thu%E1%BA%ADt+k%E1%BA%BFt+h%E1%BB%A3p+x%C6%B0%C6%A1ng+kim+Kirschner+ng%C3%B3n+5+b%C3%A0n+tay+ph%E1%BA%A3i&entry.777442342=C%E1%BA%A5p+c%E1%BB%A9u&entry.716253062=Ths.BS+Nguy%E1%BB%85n+H%E1%BB%AFu+S%C3%A2m+%28034727%29
-------------------------------------------------------------------------------
📊 TRẠNG THÁI GỬI: Chưa gửi (chế độ kiểm tra / pre-fill)
===============================================================================
```

---

## 6. Quy Tắc & Lưu Ý An Toàn Lâm Sàng

1. **Tuyệt đối không nhầm lẫn form**: Form Hà Nội dành riêng cho Khoa 57 tại Hà Nội; Form Ninh Bình dành riêng cho Khoa 915 tại Bệnh viện Bạch Mai CS2 Ninh Bình.
2. **Cách thức mổ dự kiến**: Phải ghi rõ tên phẫu thuật, vị trí giải phẫu cụ thể (ví dụ: `Phẫu thuật kết hợp xương kim Kirschner ngón 5 tay phải`, không ghi tắt chung chung `mổ ngón tay`).
3. **Mã bệnh nhân & Mã điều trị**: Luôn kiểm tra đối soát với kết quả `HisClinicalCli.exe lookup` để đảm bảo bệnh nhân đang nằm viện nội trú đúng khoa.
4. **Bảo mật & Tách biệt**: Không lưu mật khẩu hoặc token cá nhân trong payload gửi lên Google Forms; form chỉ tiếp nhận thông tin chỉ định lâm sàng.
