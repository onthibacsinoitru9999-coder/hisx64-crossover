---

# BÁO CÁO REVIEW VÀ ĐỀ XUẤT NÂNG CẤP HỆ THỐNG TỰ ĐỘNG HÓA HIS/MOS/EMR - KHOA 57
*(Tiếp theo phần 1, 2 và 3)*

---

### 4. CÁC TÍNH NĂNG ĐỘT PHÁ CẦN BỔ SUNG NGAY (Next-Gen Features)

```
                       +------------------------------------------+
                       |    SYSTEM ARCHITECTURE NEXT-GEN CORE     |
                       +------------------------------------------+
                                            |
      +---------------------+---------------+---------------------+---------------------+
      |                     |                                     |                     |
+-----------+         +-----------+                         +-----------+         +-----------+
|  DAEMON   |         |  SAFETY   |                         |   BILAN   |         |  GLUCOSE  |
| Heartbeat |         |   GATES   |                         | 1-Click   |         | Macro 2.0 |
| Auto-Auth |         | Dual-Verif|                         | Surgical  |         | Fast-Sync |
+-----------+         +-----------+                         +-----------+         +-----------+
      |                     |                                     |                     |
      +----------> [ ATOMIC TRANSACTION & ROLLBACK ENGINE ] <-----+---------------------+
```

#### 4.1. Token Auto-Renewal & Heartbeat Daemon (Kiến trúc duy trì phiên liên tục)
*   **Vấn đề:** Token xác thực (JWT/Session-ID) hết hạn sau 15–30 phút không tương tác, gây gián đoạn giữa chừng khi đang chạy batch y lệnh (gây lỗi `401 Unauthorized`), làm mất dữ liệu đang soạn thảo.
*   **Cơ chế kỹ thuật:**
    *   **Heartbeat Ping:** Thiết lập tiến trình chạy ngầm (Daemon Worker) định kỳ mỗi 180 giây gửi payload rỗng (`GET /api/v1/auth/keepalive` hoặc giả lập truy vấn nhẹ) để duy trì session.
    *   **Proactive Refresh Token:** Tự động giải mã Payload JWT, đọc trường `exp` (expiration time). Nếu $T_{\text{remaining}} < 300\text{s}$, kích hoạt `POST /api/v1/auth/refresh` để lấy access token mới trước khi thực hiện request nghiệp vụ.
    *   **Axios/HTTP Interceptor Retry Queue:** Khi gặp mã lỗi `401`, hệ thống tạm dừng chuỗi request (`pause queue`), thực hiện refresh token khẩn cấp, cập nhật `Authorization: Bearer <new_token>` và tự động phát lại (replay) request bị lỗi mà không ném exception ra giao diện bác sĩ.

#### 4.2. Dual-Verification Safety Gates (Hàng rào kiểm soát kép an toàn dược lý)
*   **Cơ chế kiểm soát Real-time Pre-commit:** Tích hợp bộ quy tắc kiểm tra chéo trước khi payload được ký số và gửi lên máy chủ HIS.

```
[Bác sĩ ra y lệnh] 
        │
        ▼
[Gate 1: Client Validation Engine]
  ├── 1. Kiểm tra dị ứng (Dị ứng chéo Cephalosporin/Penicillin, NSAIDs)
  ├── 2. Liều tối đa / 24h (Paracetamol ≤ 4g, Tramadol ≤ 400mg, Enoxaparin vs Cân nặng)
  └── 3. Chống chỉ định theo eGFR/Creatinine huyết thanh mới nhất (NSAIDs, Aminoglycosid)
        │
        ├── (Cảnh báo Đỏ - Chặn tuyệt đối) ──► Yêu cầu đổi thuốc
        ▼ (Vượt qua / Cảnh báo Vàng có Override)
[Gate 2: Drug-Drug Interaction - DDI Engine]
  ├── Tương tác nghiêm trọng: NSAIDs + Kháng đông (Tăng nguy cơ xuất huyết tiêu hóa/tụ máu vết mổ)
  └── Trùng lặp hoạt chất / Nhóm điều trị
        │
        ▼
[Đẩy vào HIS Backend an toàn]
```

#### 4.3. 1-Click Surgical Bilan Pack (Gói chỉ định CLS trọn gói Chấn thương Chỉnh hình & Cột sống)
Chuẩn hóa danh mục cận lâm sàng theo mã viện phí/BHYT, gom thành các Gói Đơn (Atomic Bundles) cấu hình theo từng bệnh lý mũi nhọn của Khoa 57:

| Phân loại Phẫu thuật | Gói Tiền phẫu (Pre-op Bilan Pack) | Gói Hậu phẫu (Post-op Bilan Pack) |
| :--- | :--- | :--- |
| **1. Bơm xi măng sinh học** *(Vertebroplasty/Kyphoplasty)* | • X-quang CS Thắt lưng/Ngực thẳng nghiêng<br>• MRI Cột sống dựng hình<br>• Đông máu toàn bộ (PT, APTT, Fibrinogen)<br>• Tổng phân tích tế bào máu, Men gan, Creatinine | • X-quang Cột sống kiểm tra vị trí xi măng (sau 02 giờ)<br>• Công thức máu ngày D1 |
| **2. Cố định Cột sống** *(Nẹp vít qua cuống, TLIF/PLIF)* | • CT Cột sống đa dãy 3D<br>• Bilan đông máu + Nhóm máu + Phản ứng chéo 02 đơn vị hồng cầu khối<br>• X-quang tim phổi thẳng, ECG, Khí máu động mạch (nếu > 60T)<br>• HBsAg, Anti-HCV, HIV | • X-quang Cột sống thẳng - nghiêng tại giường/phòng chụp<br>• Huyết học D1, D3 (Hb, Hct, Bạch cầu)<br>• CRP định lượng kiểm soát nhiễm trùng |
| **3. Thay Khớp Háng / Gối** *(Total Hip / Knee Arthroplasty)* | • X-quang Khung chậu thẳng / Khớp gối 2 tư thế<br>• Siêu âm Doppler mạch máu chi dưới (tầm soát DVT)<br>• Bilan mổ lớn: Dự trù máu, Đông máu, Chức năng Thận, Đường huyết đói | • X-quang Khớp nhân tạo tư thế chuẩn<br>• Siêu âm Doppler mạch kiểm tra huyết khối D3<br>• Theo dõi D-Dimer, Hb/Hct sau mổ |
| **4. Vi phẫu Bàn tay** *(Nối gân, mạch máu, thần kinh)* | • Siêu âm Doppler mạch máu chi trên<br>• X-quang bàn tay/cổ tay đa hướng<br>• Bilan tiền phẫu cấp cứu (Đông máu nhanh, CTM, HIV/HBsAg) | • CTM theo dõi mất máu<br>• Cấy dịch vết mổ/kháng sinh đồ (nếu dập nát, vết thương bẩn) |

#### 4.4. Snapshot & Rollback Transaction Log (Cơ chế Checkpoint & Hoàn tác Giao dịch Y lệnh)
*   **Nguyên lý SAGA Pattern trong Client-side HIS:**
    *   Mỗi khi bấm "Chạy Y lệnh", hệ thống sinh một mã