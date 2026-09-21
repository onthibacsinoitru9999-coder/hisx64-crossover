# Từ Điển Điều Khiển UI (UI Control Dictionary) - Nghiệp Vụ Tiểu Phẫu
> Trích xuất từ phiên ghi `session_20260918_163401` của `HisUiWrapper`

Tài liệu này lưu trữ định danh chính xác của các thành phần giao diện WinForms / DevExpress v15.2 và EMR để phục vụ viết script phát lại tự động (Replay) hoặc thanh tra giao diện (UI Inspection):

---

## 1. Màn Hình Chọn Phòng Làm Việc (`frmChooseRoom`)

| Tên Điều Khiển | Control Type | AutomationId / Name | Hierarchy Path | Mục Đích |
| :--- | :--- | :--- | :--- | :--- |
| **Nút Mở Chọn Phòng** | Button | Name: `Chọn phòng` | `frmMain > ribbonMain > Quick Access Toolbar > Chọn phòng` | Bật hộp thoại đổi phòng |
| **Ô Nhập Từ Khóa** | Edit | `526252` | `frmChooseRoom > layoutControl1 > layoutControl3 > txtKeyword` | Tìm tên phòng làm việc |
| **Lưới Phòng** | Custom | Name: `Chọn row 1` | `frmChooseRoom > gridControlRooms > Data Panel > Row ...` | Chọn phòng tìm được |
| **Nút Chọn (Lưu)** | Button | `btnChoice` (Name: `Chọn (Ctrl S)`) | `frmChooseRoom > layoutControl1 > layoutControl3 > btnChoice` | Xác nhận đổi phòng (**Ctrl+S**) |

---

## 2. Màn Hình Xử Lý Tiểu Phẫu / Thủ Thuật (`SurgServiceReqExecuteControl`)

| Tên Điều Khiển | Control Type | AutomationId / Name | Hierarchy Path | Mục Đích |
| :--- | :--- | :--- | :--- | :--- |
| **Tab Ribbon Tiểu Phẫu** | TabItem | Name: `Phòng Tiểu Phẫu (Nhà Q)...` | `frmMain > ribbonMain > Ribbon Tabs` | Chuyển ngữ cảnh sang phòng tiểu phẫu |
| **Nút Mở Danh Sách** | Button | Name: `Xử lý yêu cầu khám/cls/pttt` | `Lower Ribbon > Xử lý yêu cầu khám/cls/pttt` | Mở danh sách BN chờ thủ thuật |
| **Bảng Danh Sách BN** | Custom | `gridControlServiceReq` | `layoutControl2 > gridControlServiceReq` | Hiển thị bệnh nhân được chỉ định |
| **Cách Thức PTTT** | Document / Edit | `2230824` (`txtMANNER`) | `SurgServiceReqExecuteControl > layoutControlRight > txtMANNER` | Tên can thiệp thực tế |
| **Kết Luận Thủ Thuật** | Document / Edit | `1378524` (`txtConclude`) | `SurgServiceReqExecuteControl > layoutControlRight > txtConclude` | Chẩn đoán sau can thiệp |
| **Loại Phẫu Thuật** | ComboBox | `cboLoaiPT` | `SurgServiceReqExecuteControl > layoutControlRight > cboLoaiPT` | Phân loại độ ưu tiên thủ thuật |
| **Tường Trình Phẫu Thuật** | Document / Edit | `2557146` (`txtDescription`)| `xtraTabControl1 > xtraTabPageMoTa > txtDescription` | Nhập diễn biến từng bước mổ |
| **Phẫu Thuật Viên Chính** | Edit | `3212878` (`txtSurgeon`) | `SurgServiceReqExecuteControl > layoutControlRight` | Nhập mã bác sĩ PTV (`hdc`) |
| **Thư Ký / Bác Sĩ Phụ** | Edit | `2098482` / `658204` / `854812` | `SurgServiceReqExecuteControl > layoutControlRight` | Nhập mã BS phụ (`sam`, `minh`) |
| **Checkbox Ký Số** | CheckBox | `chkSign` | `SurgServiceReqExecuteControl > chkSign` | Kích hoạt ký số EMR khi Lưu |
| **Nút Lưu** | Button | `btnSave` (Name: `Lưu (Ctrl S)`) | `SurgServiceReqExecuteControl > btnSave` | Lưu phiếu can thiệp (**Ctrl+S**) |
| **Nút Kết Thúc Ca** | Button | `btnFinish` (Name: `Kết thúc xử lý (Ctrl E)`)| `SurgServiceReqExecuteControl > btnFinish` | Hoàn tất ca bệnh (**Ctrl+E**) |

---

## 3. Màn Hình Xuất Thuốc Tủ Trực Phòng Khám

| Tên Điều Khiển | Control Type | AutomationId / Name | Hierarchy Path | Mục Đích |
| :--- | :--- | :--- | :--- | :--- |
| **Nút Mở Popup Tủ Trực** | Button | `btnTuTruc` (Name: `Tủ trực`) | `SurgServiceReqExecuteControl > btnTuTruc` | Bật màn hình xuất thuốc tủ trực |
| **Ô Tìm Thuốc / Vật Tư** | Edit | `7800386` | `Xuất thuốc tủ trực (phòng khám) > txtKeyword` | Nhập tên thuốc (ví dụ: `lido`) |
| **Bảng Kết Quả Thuốc** | Table | `gridControlMediMaty` | `Xuất thuốc tủ trực > gridControlMediMaty` | Chọn lô thuốc còn hạn/còn tồn |
| **Ô Nhập Số Lượng** | Edit | `6292914` | `Xuất thuốc tủ trực > txtAmount` | Số lượng ống/viên (ví dụ: `1`) |
| **Ô Nhập Đường Dùng** | Edit | `2361936` | `Xuất thuốc tủ trực > txtTutorial` | Cách dùng (ví dụ: `tê dưới da`) |
| **Nút Bổ Sung** | Button | `btnAdd` (Name: `Bổ sung (Ctrl A)`)| `Xuất thuốc tủ trực > btnAdd` | Đưa vào danh sách xuất (**Ctrl+A**) |
| **Nút Lưu Xuất Tủ Trực** | Button | `btnSave` (Name: `Lưu (Ctrl S)`) | `Xuất thuốc tủ trực > btnSave` | Ghi sổ xuất kho tủ trực (**Ctrl+S**) |
| **Nút Đóng Cửa Sổ** | Button | `Close` | `Xuất thuốc tủ trực > Close` | Đóng cửa sổ quay về phiếu mổ |

---

## 4. Màn Hình Văn Bản Điện Tử & Ký Số EMR

| Tên Điều Khiển | Control Type | AutomationId / Name | Hierarchy Path | Mục Đích |
| :--- | :--- | :--- | :--- | :--- |
| **Trình Xem Tài Liệu** | Pane | `DocumentViewer` | `Văn bản điện tử > DocumentViewer` | Hiển thị phiếu phẫu thuật dạng PDF/EMR |
| **Nút Ký Số** | Button | Name: `Ký` | `Văn bản điện tử > ToolBar > Ký` | Ký số USB token / SmartCA điện tử |
