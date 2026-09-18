# HƯỚNG DẪN SỬ DỤNG: HIS & EMR UI WRAPPER (BỘ GHI THAO TÁC UI LIÊN ỨNG DỤNG)

Ứng dụng **HisUiWrapper** là bộ công cụ ghi nhận và học thao tác người dùng (Click chuột, Gõ phím, Nhập văn bản) trên cả hai phần mềm: **HIS** (`HIS.exe`) và phần mềm con Bệnh án điện tử **EMR** (`ConnectToEMR.exe`, `EMR.exe`, `EHR.exe`).

---

## 1. Mục Đích & Khả Năng Vượt Trội
1. **Học tương tác UI tự động & Liền mạch (HIS ➡️ EMR Cross-App Workflow)**:
   - Thông thường Bác sĩ thao tác trên HIS, sau đó bấm mở **Bệnh án điện tử (EMR)** để ký phiếu, làm biên bản hoặc hội chẩn.
   - **HisUiWrapper** tự động theo vết: Khi cửa sổ EMR mở lên từ HIS, bộ ghi tự động nhận diện và gắn kết ngay lập tức mà không cần bấm thêm thao tác gì.
2. **Bóc tách sâu UI Automation (Deep UIA Inspection)**:
   - Tự động trích xuất `AutomationId` (ví dụ: `btnNew`, `btnSave`, `btnKeDonThuoc`, `txtContent` trên HIS; `0` [HÀNH CHÍNH], `1` [HỎI BỆNH], `butMauPhieu`, `btnTemplate` trên EMR), `ControlType`, `Name`, `ClassName`.
   - Tính toán tọa độ tương đối bên trong control (`RelPctX%`, `RelPctY%`) giúp nhận diện chính xác kể cả khi màn hình co giãn hoặc di chuyển.
3. **Phân định rõ ràng ứng dụng nguồn (`AppBadge`)**:
   - Mọi thao tác đều được phân loại đích danh: 🏥 **`[HIS]`** hay 📋 **`[EMR]`**, giúp bản tóm tắt quy trình phản ánh đúng hành trình người dùng qua các cửa sổ.
4. **Bảo mật & Lọc thông minh 100%**:
   - Chỉ ghi nhận khi con trỏ chuột hoặc phím gõ tác động vào cửa sổ thuộc **`HIS.exe`** hoặc **`ConnectToEMR.exe` / `EMR.exe`**.
   - Tuyệt đối không ghi nhận các ứng dụng khác (trình duyệt, Zalo, ứng dụng cá nhân).
5. **Xuất dữ liệu 3 tầng**:
   - **`logs\ui_recordings\session_YYYYMMDD_HHmmss.jsonl`**: Tệp JSON stream chứa đầy đủ thông số kỹ thuật cho AI phân tích.
   - **`logs\ui_recordings\session_YYYYMMDD_HHmmss_workflow.md`**: Bản tóm tắt quy trình bằng tiếng Việt dễ đọc (có gắn nhãn `[HIS]` / `[EMR]`).
   - **`logs\ui_recordings\session_YYYYMMDD_HHmmss_replay.cs`**: Mã nguồn C# UI Automation mẫu để tự động hóa chạy lại quy trình.

---

## 2. Cách Khởi Động Nhanh (1-Click)

### Cách 1: Chạy trực tiếp từ file Batch
Nhấp đúp chuột vào file:
👉 **`HisUiWrapper.bat`** tại thư mục gốc của dự án.

### Cách 2: Khởi chạy kèm phần mềm HIS
```powershell
.\HisUiWrapper.exe --launch
```
Lệnh này sẽ khởi động `HIS.exe` và tự động gắn bộ ghi vào cửa sổ HIS.

### Cách 3: Chạy ở chế độ dòng lệnh (Headless Console)
```powershell
.\HisUiWrapper.exe --console
```

---

## 3. Các Phím Tắt & Thao Tác Trong Khi Ghi

| Phím Tắt / Nút Bấm | Chức Năng | Chi Tiết |
| :---: | :--- | :--- |
| **`F9`** | **Tạm dừng / Tiếp tục ghi** | Bấm để tạm ngưng ghi khi cần xử lý việc riêng, bấm lại để tiếp tục |
| **`F10`** | **Đánh dấu mốc thao tác** | Hiện hộp thoại nhập ghi chú (ví dụ: "Bắt đầu chọn mẫu đơn...") |
| **`F11`** | **Hoàn tất & Xuất báo cáo** | Dừng ghi, đóng gói file `.jsonl`, `.md` và tự động mở thư mục chứa kết quả |

---

## 4. Cấu Trúc Mã Nguồn

- `HisUiWrapper.cs`: Hàm `Main` điều phối, kết nối desktop tương tác, quản lý tiến trình.
- `HisUiHookEngine.cs`: Cài đặt `WH_MOUSE_LL` và `WH_KEYBOARD_LL`, bộ lọc PID và gom cụm ký tự gõ phím thông minh (`Smart Typing Accumulator`).
- `HisUiaInspector.cs`: Bóc tách thuộc tính `UIAutomationClient` và Windows Native Handle.
- `HisFloatingHud.cs`: Giao diện HUD nổi hiện đại, bán trong suốt, không cướp focus của HIS.
- `HisUiSessionWriter.cs`: Xuất dữ liệu đa định dạng (JSONL, Markdown, Replay C#).
- `build_wrapper.bat`: Script biên dịch bằng `csc.exe` v4.0 64-bit không phụ thuộc IDE.
