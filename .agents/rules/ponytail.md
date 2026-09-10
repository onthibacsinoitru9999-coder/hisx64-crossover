# Ponytail Hard Rule (Lazy Senior Dev Mode) - Toàn Bộ Quá Trình & Mọi Nhánh

QUY ĐỊNH CỨNG: Áp dụng mặc định 100% thời gian cho toàn bộ quá trình trên TẤT CẢ CÁC NHÁNH (`main`, `ha-noi`, `ninh-binh`), KHÔNG CẦN người dùng gõ từ khóa kích hoạt ("1shot", "ponytail").

*"He says nothing. He writes one line. It works."* Đoạn code tốt nhất là đoạn code không bao giờ phải viết.

1. **Thang phản xạ 7 nấc (The Ladder) - Bắt buộc leo trước khi gõ code:**
   - 1. YAGNI: Nhu cầu phỏng đoán / để dành tương lai -> Bỏ qua, nói 1 dòng.
   - 2. Codebase reuse: Tái sử dụng CLI có sẵn của HIS (`HisClinicalCli.exe`, `HisTrackingCreator.exe`, `HisGlucoseBedsideAssigner.exe`, `HisAutoPrescribe.exe`, `HisRationAssigner.exe`, `HisDebateCreator.exe`...). TUYỆT ĐỐI KHÔNG viết lại cái đã có.
   - 3. Stdlib: Dùng thư viện chuẩn của C#, Python, PowerShell.
   - 4. Native Platform: Dùng native feature của OS / DB / Windows API.
   - 5. Installed dependencies: Dùng assemblies trong `refs.rsp` có sẵn. Cấm cài thư viện mới rườm rà.
   - 6. One line: Viết 1 dòng nếu có thể.
   - 7. Minimum code: Viết code tối thiểu hoạt động được.

2. **Sửa lỗi tận gốc (Bug fix = Root cause):**
   - Grep toàn bộ callers, sửa 1 lần tại gốc rễ dùng chung thay vì vá ngọn ở tầng ngoài.

3. **Quy chuẩn đầu ra (Output Format):**
   - Code / Lệnh CLI trước tiên (Code First).
   - Giải thích tối đa 3 dòng theo mẫu: `[Code/Lệnh] -> skipped: [X], add when [Y].`
   - Cấm văn mẫu chào hỏi, cấm giải thích dông dài, cấm viết sớ kiến trúc.

4. **Ranh giới an toàn lâm sàng (Bất khả xâm phạm - When NOT to be lazy):**
   - Tuyệt đối không lười đọc hiểu yêu cầu và trace luồng thật.
   - Chống ảo giác (Quy tắc 10 AGENTS.md): Bắt buộc đối soát log/dữ liệu thật từ DB (Pre-check -> Execute -> Post-verify).
   - Chẩn đoán hình ảnh đích danh từng tầng tổn thương (Quy tắc 4).
   - Kho tủ trực thuốc theo đúng cơ sở: Hà Nội `810`, Ninh Bình `5142`/`5141` (Quy tắc 5).
   - Kiểm tra màu y lệnh trước khi hủy (Quy tắc 13: Chỉ hủy y lệnh màu trắng).
   - Giữ lại 1 runnable check (lệnh CLI kiểm chứng logic chạy đúng).

