---
name: his-inpatient-surgery-record
description: >-
  Tự động khởi tạo, trích xuất dữ liệu lâm sàng đa nguồn sâu (tờ điều trị, biên bản hội chẩn,
  kết luận CĐHA MRI/CT/X-quang) và điền chi tiết 100% vào Vỏ Bệnh Án Ngoại Khoa EMR
  (BENHANNGOAIKHOA / THONGTINDIEUTRI) qua Oracle EMR DB và HisEmrFiller.exe.
---

# HIS Inpatient Surgical Medical Record (Vỏ Bệnh Án Ngoại Khoa EMR & Lâm Sàng Sâu)

Skill này hướng dẫn quy chuẩn lâm sàng và kỹ thuật tự động hóa toàn diện việc điền **Vỏ Bệnh Án Ngoại Khoa (`BENHANNGOAIKHOA`)** và khởi tạo **Trang bìa (`THONGTINDIEUTRI`)** trên hệ thống Bệnh án điện tử EMR Bệnh viện Bạch Mai thông qua công cụ `HisEmrFiller.exe` (Launcher: `HisEmrFiller.bat`).

---

## 1. QUY TẮC CỨNG BẤT KHẢ XÂM PHẠM: CHỈ DÀNH CHO BỆNH NHÂN NỘI TRÚ (INPATIENT ONLY)

> [!CAUTION]
> - **Tuyệt đối CẤM**: Không tạo Vỏ Bệnh Án Ngoại Khoa cho bệnh nhân khám ngoại trú / phòng khám (`TDL_TREATMENT_TYPE_ID != 3`).
> - **Phạm vi áp dụng duy nhất**: Bệnh nhân **ĐIỀU TRỊ NỘI TRÚ** (`TDL_TREATMENT_TYPE_ID == 3` và đang nằm buồng bệnh nội trú Khoa 57 hoặc Khoa 915).
> - **Chốt chặn an toàn**: `HisEmrFiller.exe` tự động từ chối nếu bệnh nhân là ngoại trú. Khi quét theo ngày (`--date YYYYMMDD`), tool tự động lọc bỏ 100% ca khám ngoại trú.
> - **Lệnh thu hồi khẩn cấp**: `.\HisEmrFiller.bat --reverse-outpatients` (xóa sạch vỏ ngoại trú bị tạo nhầm trên DB Oracle EMR, bảo lưu nguyên vẹn 100% hồ sơ nội trú).

---

## 2. NGUYÊN TẮC TRÍCH XUẤT LÂM SÀNG SÂU (DEEP CLINICAL ENRICHMENT)

Khi tạo vỏ bệnh án, **TUYỆT ĐỐI KHÔNG DÙNG NỘI DUNG MẪU SƠ SÀI / ĐẠI TRÀ** kế thừa từ bệnh nhân khác nếu bệnh nhân đã có dữ liệu theo dõi thực tế. Công cụ `HisEmrFiller` tự động truy vấn 3 nguồn dữ liệu thật từ máy chủ MOS:

1. **Toàn bộ Tờ điều trị (`api/HisTracking/GetView`)**:
   - Quét từ 7 đến 10+ tờ điều trị trong toàn bộ đợt nằm viện.
   - Bóc tách diễn biến tri giác, mạch, huyết áp, nhiệt độ, nhịp thở, SpO2.
   - Bóc tách tình trạng dinh dưỡng, vận động, đại tiểu tiện, dẫn lưu, vết mổ cũ và loét tì đè.
2. **Biên bản Hội chẩn chuyên khoa / Liên viện (`api/HisDebate/Get`)**:
   - Đọc các trường `TREATMENT_TRACKING`, `DISCUSSION`, và `CONCLUSION`.
   - Trích xuất tiền sử chấn thương phức tạp (TNLĐ, TNGT), các can thiệp mổ cấp cứu tuyến trước (BV Việt Đức, BV tỉnh, tuyến huyện), quá trình tập phục hồi chức năng và lý do chuyển viện/chuyển khoa.
3. **Kết luận Chẩn đoán Hình ảnh Đích danh (`api/HisSereServExt/Get`)**:
   - Đọc kết luận đọc phim MRI, CT Scanner, X-quang thực tế.
   - Bắt buộc trích xuất đích danh từng tầng tổn thương (Quy tắc 4 `AGENTS.md`): ví dụ `Trượt L5 ra trước độ III chèn ép chùm đuôi ngựa`, `Rách vòng xơ đĩa đệm L4/5`, không ghi chung chung "thoái hóa/chấn thương cột sống".

---

## 3. TIÊU CHUẨN ĐIỀN DỮ LIỆU TỪNG TRƯỜNG LÂM SÀNG TRÊN EMR

### 3.1. Tab Hỏi Bệnh (Phần Bệnh Sử & Tiền Sử):
- **Lý do vào viện (`LyDoVaoVien`)**:
  * Phản ánh chính xác triệu chứng cơ năng chính thúc đẩy vào viện: ví dụ *"Đại tiện, tiểu tiện không tự chủ, yếu hai chi dưới sau đa chấn thương"* hoặc *"Đau cột sống thắt lưng dữ dội, hạn chế vận động sau tai nạn giao thông"*.
- **Quá trình bệnh lý (`QuaTrinhBenhLy`)**:
  * Trình bày diễn tiến theo trục thời gian chi tiết:
    1. Hoàn cảnh tai nạn / khởi phát bệnh (thời gian, cơ chế chấn thương).
    2. Các chấn thương phối hợp được phát hiện và xử trí cấp cứu ban đầu tại tuyến trước (mổ tạng rỗng, dẫn lưu màng phổi, KHX xương đùi...).
    3. Thời gian nằm viện điều trị và tập PHCN tại các tuyến trước (tiến triển ít, còn di chứng gì).
    4. Thời điểm chuyển đến Bệnh viện Bạch Mai, khoa tiếp nhận ban đầu (Viện Thần kinh / Cấp cứu) và lý do chuyển sang Khoa Chấn thương Chỉnh hình & Cột sống.
    5. Tình trạng hiện tại khi bác sĩ làm bệnh án.
- **Tiền sử bản thân (`TienSuBanThan`)**:
  * Bóc tách rõ ràng: Tiền sử đa chấn thương (chấn thương ngực, bụng, thận, gãy xương chi đã phẫu thuật...), tiền sử bệnh nội khoa (tăng huyết áp, đái tháo đường, tim mạch), tiền sử dị ứng thuốc/thức ăn.
  * Nếu tiền sử khỏe mạnh/chưa ghi nhận bất thường: Rút gọn tự nhiên thành `"tiền sử khỏe mạnh"`.
- **Tiền sử gia đình (`TienSuGiaDinh`)**:
  * Ghi nhận chuẩn y khoa: *"Gia đình khỏe mạnh, chưa phát hiện ai mắc bệnh lý di truyền hoặc liên quan."*

### 3.2. Tab Khám Bệnh (Phần Khám Thực Thể Chuyên Sâu):
- **1. Toàn thân (`ToanThan`)**:
  * Tri giác (tỉnh, tiếp xúc tốt, Glasgow 15 điểm), da niêm mạc hồng, không phù, không xuất huyết dưới da, tuyến giáp không to, hạch ngoại vi không sờ thấy.
  * Dấu hiệu sinh tồn thực tế (Mạch, HA, Nhiệt độ, SpO2).
  * **Đặc biệt lưu ý**: Kiểm tra và ghi nhận rõ các **ổ loét tì đè** do nằm lâu (vị trí vùng cùng cụt, ụ ngồi, gót chân, độ sâu, tình trạng dịch tiết/chăm sóc).
- **2. Ngoại khoa (`BenhNgoaiKhoa`) & 3. Cơ xương khớp (`CoXuongKhop`)**:
  * Luôn điền đồng bộ cả 2 trường để không bị trống ô trên giao diện UI EMR.
  * Khám cột sống: Hình dáng cột sống (gù vẹo, mất ưỡn thắt lưng sinh lý, biến dạng đốt sống), điểm đau chói gai sau (`ấn đau gai sống L5-S1`), co cứng cơ cạnh sống.
  * Khám vận động & chi dưới: Đánh giá cơ lực đích danh từng bên (`cơ lực chân phải 3/5, chân trái 3+/5`), trương lực cơ, phản xạ gân xương hai bên (giảm hoặc mất phản xạ gân gót).
  * Khám vết mổ cũ: Vị trí sẹo mổ (sẹo mổ đùi, sẹo mổ đường trắng giữa thành bụng...), kích thước sẹo, tình trạng liền sẹo (sẹo khô, liền tốt, không sưng nề, không chảy dịch).
- **Thần kinh (`ThanKinh`)**:
  * Đánh giá hội chứng chùm đuôi ngựa: Rối loạn cảm giác vùng yên ngựa / tầng sinh môn, giảm cảm giác da mặt sau đùi và cẳng bàn chân.
  * Dấu hiệu màng não (gáy mềm, Kernig âm tính), Babinski âm tính hai bên.
- **Tiêu hóa (`TieuHoa`)**:
  * Đánh giá tình trạng bụng: bụng mềm hay chướng nhẹ, không có điểm đau khu trú, không có phản ứng thành bụng hay cảm ứng phúc mạc.
  * Đại tiện: Rối loạn cơ tròn, bí đại tiện, cần thụt tháo.
- **Thận - Tiết niệu (`ThanTietNieu`)**:
  * Đánh giá tiểu tiện: Rối loạn cơ tròn, tiểu không tự chủ / bí tiểu, đang lưu sonde bàng quang, màu sắc nước tiểu (vàng trong/hồng nhạt), số lượng nước tiểu 24h. Hố thắt lưng hai bên không căng gồ, chạm thận (-), bập bềnh thận (-).
- **Tuần hoàn & Hô hấp (`TuanHoan`, `HoHap`)**:
  * Tim nhịp đều, T1 T2 rõ, không tiếng thổi bệnh lý.
  * Lồng ngực hai bên cân đối, di động theo nhịp thở, rì rào phế nang rõ, không ran.
- **4. Cận lâm sàng (`CanLamSang`)**:
  * Tổng hợp kết quả CĐHA then chốt: Kết luận MRI cột sống thắt lưng (đích danh tầng chèn ép), CT Scanner ổ bụng (đánh giá các tạng sau chấn thương), X-quang.
  * Bilan xét nghiệm: CTM, Đông máu, Sinh hóa máu (Ure, Creatinin, Men gan, Điện giải đồ).
- **5. Tóm tắt bệnh án (`TomTatBenhAn`)**:
  * Cấu trúc chuẩn hóa:
    `"Bệnh nhân {nam/nữ}, {X} tuổi, tiền sử {tiền sử}, vào viện vì {lý do vào viện}. Qua hỏi bệnh và thăm khám phát hiện các hội chứng, triệu chứng sau:\n- Hội chứng tổn thương cột sống thắt lưng: ...\n- Hội chứng chùm đuôi ngựa / thần kinh: ...\n- Vết mổ cũ và thương tổn phối hợp: ...\n- Cận lâm sàng: ..."`
- **Tiên lượng & Hướng điều trị (`TienLuong`, `HuongDieuTri`)**:
  * Tiên lượng: Dè dặt / Nặng / Khá tùy mức độ tổn thương thần kinh và chấn thương phối hợp.
  * Hướng điều trị: Ghi rõ kế hoạch phẫu thuật chuyên khoa (ví dụ: *Chuẩn bị phẫu thuật giải ép thần kinh, nắn trượt và cố định cột sống thắt lưng hàn xương liên thân đốt TLIF/PLIF*).

---

## 4. CƠ CHẾ GHI ĐÈ AN TOÀN (`ShouldOverwrite`) & CÁC CỜ DÒNG LỆNH

Để bảo vệ công sức gõ dở của bác sĩ trên máy trạm nhưng vẫn tự động hóa tối đa:

1. **Chế độ Merge mặc định**:
   - Nếu trường dữ liệu đã có nội dung hợp lệ do bác sĩ gõ tay $\rightarrow$ Công cụ **BẢO LƯU NGUYÊN VẸN**, chỉ điền bổ sung các trường còn trống.
2. **Nhận diện và xóa bỏ mẫu rác (`ShouldOverwrite`)**:
   - Nếu nội dung trong trường là câu chữ mặc định của template kế thừa từ bệnh nhân khác (chứa các cụm từ boilerplate như `"kính nhờ"`, `"phim gửi kèm"`, `"chưa ghi nhận"`, `"bình thường"` trong khi bệnh nhân có tổn thương nặng) $\rightarrow$ Tự động ghi đè dữ liệu lâm sàng thật.
3. **Các cờ dòng lệnh linh hoạt**:
   ```powershell
   # 1. Xem trước nội dung lâm sàng trích xuất được (không ghi vào DB):
   .\HisEmrFiller.bat <MãBN|MãĐT> --preview

   # 2. Điền bổ sung vào EMR (chế độ an toàn):
   .\HisEmrFiller.bat <MãBN|MãĐT> --save

   # 3. Làm mới toàn bộ nội dung theo diễn biến điều trị & hội chẩn mới nhất:
   .\HisEmrFiller.bat <MãBN|MãĐT> --force --save

   # 4. Quét danh sách bệnh nhân nội trú vào viện hôm nay:
   .\HisEmrFiller.bat --today

   # 5. Đối soát theo ngày vào viện (chỉ quét ca nội trú):
   .\HisEmrFiller.bat --date YYYYMMDD

   # 6. Tự động điền bù vỏ cho tất cả các ca nội trú còn thiếu trong ngày:
   .\HisEmrFiller.bat --date YYYYMMDD --auto

   # 7. Thu hồi / Xóa sạch vỏ ngoại trú bị tạo nhầm trên Oracle:
   .\HisEmrFiller.bat --reverse-outpatients
   ```

---

## 5. BẪY KỸ THUẬT ORACLE EMR & QUY CHUẨN KÝ SỐ

1. **Khóa `MAQUANLY` vs `TreatmentId` (Dual-Write)**:
   - Trên Oracle EMR, giao diện EMR Client ánh xạ trường `MAQUANLY` theo số của `TREATMENT_CODE` (ví dụ `000007258723` $\rightarrow$ `7258723`), trong khi backend HIS dùng `TreatmentId` (`7258539`).
   - `HisEmrFiller` thực hiện ghi đồng thời cả 2 ID để bảo đảm tương thích 100% khi bác sĩ mở EMR từ bất kỳ màn hình nào.
2. **Oracle Schema Trap**:
   - Bảng `EMR_FINAL.BENHANNGOAIKHOA` **KHÔNG CÓ CỘT `MABENHNHAN`** (cột này nằm ở bảng `THONGTINDIEUTRI`).
   - Mọi truy vấn SQL DELETE hoặc SELECT trực tiếp phải nối qua `WHERE MAQUANLY = ...`.
3. **Quy Chuẩn Ký Số Vỏ Bệnh Án Ngoại Khoa**:
   - Khác với Tờ điều trị (Type 7 ký tự động 100% qua Cloud HSM), Vỏ bệnh án ngoại khoa đòi hỏi bác sĩ kiểm tra và bấm **Ký 1-click trực tiếp trên giao diện EMR Desktop Client** để DevExpress XtraReports đóng gói chữ ký nội bộ.
   - Tuyệt đối không can thiệp API ký ngầm giả lập PDF cho Vỏ bệnh án để tránh phá vỡ liên kết báo cáo của EMR Client.
