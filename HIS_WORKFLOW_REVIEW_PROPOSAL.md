# ĐÁNH GIÁ KIẾN TRÚC & ĐỀ XUẤT NÂNG CẤP TOÀN DIỆN HỆ THỐNG TỰ ĐỘNG HÓA HIS / MOS / EMR
**BỆNH VIỆN BẠCH MAI – KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57)**
*Cố vấn & Kiến trúc sư Trưởng: Lead Healthcare System Architect*

---

## 1. ĐÁNH GIÁ KIẾN TRÚC & TÍNH THỰC CHIẾN HIỆN TẠI

Hệ sinh thái tự động hóa HIS/MOS/EMR hiện tại đang vận hành với mô hình **Hybrid Automation (CLI-First + Continuous Knowledge Push)**, giải quyết xuất sắc nhiều bài toán đặc thù của phần mềm nghiệp vụ y tế bệnh viện công lập cấp đặc biệt:

```
                  ┌─────────────────────────────────────────────────────────┐
                  │                 AI Clinical Agent Layer                 │
                  │   (AGENTS.md / Context Playbook / Clinical Protocols)   │
                  └───────────┬─────────────────────────────────┬───────────┘
                              │                                 │
                 CLI-First Native Tools             Live Log Interceptor
         (HisClinicalCli / HisAutoPrescribe / etc.) (FileShare.ReadWrite Token)
                              │                                 │
     ┌────────────────────────┴─────────────────────────────────┴──────────┐
     │                       Bach Mai Intranet (192.168.x.x)                 │
     ├───────────────────────┬──────────────────────┬──────────────────────┤
     │  MOS Core (:1608)     │  ACS Auth (:1401)    │  EMR Service (:1415) │
     │  - Tracking / Diet    │  - Login / Renew     │  - Electronic Records│
     │  - Prescriptions/CLS  │  - UpdateWorkInfo    │  - Digital Signature │
     └───────────────────────┴──────────────────────┴──────────────────────┘
```

### Các điểm mạnh cốt lõi:
1. **Triết lý CLI-First triệt tiêu lỗi Runtime:** Thay vì cho phép LLM tự tạo mã HTTP cào log/bắn API thô (dễ gây gãy vỡ payload JSON và sai cấu trúc DTO), hệ thống đã đóng gói logic nghiệp vụ vào các nhị phân C# (`HisClinicalCli.exe`, `HisAutoPrescribe.exe`, `HisGlucoseBedsideAssigner.exe`). Việc này bảo đảm tính toàn vẹn kiểu dữ liệu (`Strong Typing`) và tương thích hoàn toàn với các DLL lõi của HIS (`MOS.EFMODEL.dll`, `HIS.Desktop.LocalStorage`).
2. **Xử lý File Lock & Live Token chuẩn xác:** Giải pháp đọc `LogSystem.txt` với chế độ non-locking `FileShare.ReadWrite` triệt tiêu lỗi xung đột file I/O (`IOException`) khi HIS Client của máy bàn liên tục ghi log.
3. **Cơ chế nạp ngữ cảnh kích hoạt phòng làm việc (`UpdateWorkInfo`):** Nắm bắt đúng cơ chế phiên làm việc của backend MOS: Token hợp lệ vẫn bị từ chối nếu chưa gắn định danh `WorkInfoSDO` (Khoa 57, Buồng 712, 714, 724, 734).
4. **Chuẩn hóa danh mục & Hướng dẫn sử dụng thuốc (130+ Tutorials):** Khắc phục triệt để lỗi điều dưỡng/bác sĩ phải sửa tay lại cách dùng; chuẩn hóa đường dùng, dung môi pha truyền và cữ tiêm/uống.
5. **Cơ chế Continuous Learning qua Git:** Mọi bẫy lỗi (Gotchas) được cập nhật đồng bộ giữa máy bàn và laptop, tạo nên hệ thống "tự sửa đổi và tiến hóa".

---

## 2. PHÂN TÍCH RỦI RO LÂM SÀNG & ĐIỂM NGHẼN HỆ THỐNG

| Rủi ro / Điểm nghẽn | Mức độ | Cơ chế phát sinh | Hậu quả lâm sàng / Kỹ thuật |
| :--- | :---: | :--- | :--- |
| **Token Expiry khi chạy Batch dài** | 🔴 **Cao** | Phiên làm việc (Session) hết hạn giữa chừng khi đang thực thi danh sách 10-20 bệnh nhân. | Lệnh tạo tờ điều trị thành công nhưng lệnh kê thuốc/chỉ định CLS bị từ chối $\rightarrow$ Lệch pha dữ liệu hồ sơ bệnh án. |
| **Nhầm lẫn Kho Tủ Trực vs Kho Dược** | 🔴 **Nghiêm trọng** | Kê thuốc cấp cứu/Insulin/thay băng vào nhầm Kho Dược Ngoại Trú/Nội trú (`4209/4210`) thay vì Tủ trực (`810`). | Điều dưỡng không thể xuất kho thực tế tại khoa, phát sinh trùng lặp y lệnh hoặc chậm trễ xử lý tăng/hạ đường huyết cấp. |
| **Sai lệch đơn vị chuyển đổi Insulin** | 🔴 **Nghiêm trọng** | Insulin tính theo UI nhưng hệ thống HIS lưu theo đơn vị Lọ/Bút (`Amount = UI / 1000.0m`). | Nếu nhầm lẫn `Amount = 8` (8 lọ thay vì 8 đơn vị = 0.0080 lọ), BHYT xuất toán toàn bộ tiền triệu và sai lệch tồn kho. |
| **Thiếu Idempotency (Ghi trùng y lệnh)** | 🟡 **Trung bình** | Khi mạng nội bộ trễ, Agent gọi lại API lần 2 mà không kiểm tra trạng thái trước đó. | Nhân đôi suất ăn bệnh lý, nhân đôi xét nghiệm đường máu mao mạch `BM02426`. |
| **Bỏ sót Bilan Tiền Phẫu bắt buộc** | 🔴 **Nghiêm trọng** | Bác sĩ chỉ định bơm xi măng (BXM) nhưng chưa có DEXA, hoặc mổ tư thế sấp BN ĐTĐ chưa soi đáy mắt. | Nguy cơ tai biến phẫu thuật, hoãn mổ sát giờ gây bức xúc cho bệnh nhân và vi phạm quy trình an toàn phẫu thuật. |

---

## 3. ĐỀ XUẤT NÂNG CẤP CHI TIẾT THEO CÁC PHÂN HỆ NGHIỆP VỤ

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                               PIPELINE TỰ ĐỘNG HÓA Y LỆNH NỘI TRÚ TOÀN DIỆN                            │
└────────────────────────────────────────────────────────────────────────────────────────────────────────┘
  [1. Tra cứu BN] ──> [2. Tờ điều trị/DHST] ──> [3. Suất ăn DD] ──> [4. Đơn thuốc/Vật tư] ──> [5. CLS/ĐMMM]
           │                        │                    │                     │                     │
           ▼                        ▼                    ▼                     ▼                     ▼
    Đối soát buồng/         ICD-10 5 ký tự +      Vòng lặp N ngày      Kho 810 vs 4209/4210   BM02426 + Bilan
    giường & Tiền sử        Trích CĐHA tầng        Pre-check tránh      Dual-Verification      Pre-op Guardrail
    (Khoa 57)               tổn thương rõ ràng     trùng lặp            chuẩn liều/dung môi    (DEXA/Siêu âm tim)
```

### 3.1. Phân hệ Tra cứu & Đồng bộ Hồ sơ Bệnh nhân
* **Nâng cấp:** Tự động kết nối song song 2 API: `api/HisTreatment/GetView` (Lấy chẩn đoán, BHYT) và `api/HisTreatmentBedRoom/GetLView` (Lấy buồng/giường thực tế).
* **Kiểm soát:** Thiết lập cứng bộ lọc mặc định `DEPARTMENT_ID = 57` (Khoa CTCH & Cột sống), tự động từ chối thao tác nếu bệnh nhân đã chuyển khoa hoặc đóng hồ sơ (`IS_PAUSE = 1`), cảnh báo ngay lập tức nếu bệnh nhân chưa được xếp giường (`BED_ROOM_ID == null`).

### 3.2. Phân hệ Tờ Điều Trị (`HIS_TRACKING`) & DHST
* **Bắt buộc cấu trúc DTO:** Đóng gói toàn bộ trong `HisTrackingSDO` với thuộc tính `Tracking.MEDICAL_INSTRUCTION`.
* **Ràng buộc Chẩn đoán Hình ảnh:** Tuyệt đối cấm các chuỗi mô tả chung chung. Tự động kiểm tra biểu thức chính quy (Regex) bắt buộc có tầng tổn thương (Ví dụ: `(L[1-5]|T[1-12]|C[1-7]|gãy|xẹp cấp|hẹp ống sống)`).
* **Ràng buộc ICD-10:** Yêu cầu đầy đủ mã bệnh chính (`ICD_CODE`) kèm mã định danh chi tiết (Ví dụ: `M47.00†` thay vì `M47`).

### 3.3. Phân hệ Suất Ăn Dinh Dưỡng Bệnh Lý (Ration Orders)
* **Xử lý bất đồng bộ đa ngày:** Triển khai cơ chế lặp an toàn `1 Request = 1 Ngày`, bọc trong hàm xác nhận đối soát:
  1. *Bước 1 (Pre-check):* Quét `api/HisSereServRation/GetView` xem ngày chỉ định đã có suất ăn chưa.
  2. *Bước 2 (Execute):* Đẩy `HisRationServiceReqSDO` với `InstructionTimes = [YYYYMMDD050000]`.
  3. *Bước 3 (Post-verify):* Đối soát lại phản hồi từ phòng `580