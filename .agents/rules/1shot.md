# 1-Shot Ponytail Mode (Lazy Senior Dev Mode)

Bất kỳ khi nào người dùng yêu cầu với từ khóa 1shot, 1 shot, ponytail, hoặc đang ở branch ponytail-1shot:

1. **Thang phản xạ 7 nấc (The Ladder):**
   - 1. YAGNI: Nhu cầu phỏng đoán -> Bỏ qua, nói 1 dòng.
   - 2. Codebase reuse: Tái sử dụng CLI có sẵn của HIS (HisClinicalCli.exe, HisTrackingCreator.exe, HisGlucoseBedsideAssigner.exe, HisAutoPrescribe.exe, HisRationAssigner.exe...). Không viết lại cái đã có.
   - 3. Stdlib: Dùng thư viện chuẩn.
   - 4. Native Platform: Dùng native feature.
   - 5. Installed dependencies: Dùng refs.rsp có sẵn.
   - 6. One line: Viết 1 dòng nếu có thể.
   - 7. Minimum code: Viết code tối thiểu.

2. **Sửa lỗi tận gốc (Bug fix = Root cause):** Grep callers, sửa 1 lần tại gốc rễ dùng chung.
3. **Quy chuẩn đầu ra:** Code/Lệnh trước tiên. Giải thích tối đa 3 dòng theo mẫu [code] -> skipped: [X], add when [Y].
4. **Ranh giới an toàn lâm sàng (Bất khả xâm phạm):**
   - Tuyệt đối không lười đọc hiểu yêu cầu và trace luồng thật.
   - Chống ảo giác (Quy tắc 10 AGENTS.md): Bắt buộc đối soát log/dữ liệu thật từ DB.
   - Chẩn đoán hình ảnh đích danh từng tầng tổn thương (Quy tắc 4).
   - Kho tủ trực 810 cho tiêm Insulin (Quy tắc 5).
   - Kiểm tra màu y lệnh trước khi hủy (Quy tắc 13: Chỉ hủy y lệnh màu trắng).
