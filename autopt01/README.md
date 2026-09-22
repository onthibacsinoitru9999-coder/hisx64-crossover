# 🔬 AutoPT01 — Capture HTTP Traffic (Step 1)

**Mục tiêu:** Bắt API endpoint khi thao tác "Biểu mẫu khác → PT-01 → Lưu" trên HIS để build tool bypass UI.

---

## 📋 Cách bắt traffic bằng Fiddler (khuyến nghị)

### Bước 1: Cài & mở Fiddler Classic
- Tải: https://www.telerik.com/fiddler (miễn phí)
- Mở Fiddler → đảm bảo **Capture is ON** (F12)
- Filter: Vào tab **Filters** → Tick **Show only if URL contains** → nhập `192.168.7.236`

### Bước 2: Thao tác trên HIS (1 BN bất kỳ)
1. Mở HIS → Vào buồng bệnh → Chọn 1 BN
2. **Click phải** BN → **In ấn** → **Biểu mẫu khác hồ sơ điều trị**
3. Ctrl+F → gõ `pt` → Chọn **Mẫu PT-01** → Bấm **Editing control**
4. Cửa sổ soạn thảo mở → Bấm **Open** → Nạp file docx bất kỳ → **Ctrl+S**
5. Quan sát Fiddler: **Ghi lại TẤT CẢ các request POST** trong lúc thao tác

### Bước 3: Export request cần thiết
- Trong Fiddler: click vào request POST có URL dạng `api/His.../Create` hoặc `api/His.../Save`
- Tab **Inspectors** → **Raw** → Copy toàn bộ request (URL + Headers + Body)
- Lưu vào file: `autopt01/captured_request.txt`

---

## 📋 Cách bắt traffic KHÔNG có Fiddler (dùng Wireshark / Network Monitor)

```powershell
# Bắt traffic port 1608 (HIS MOS API server)
netsh trace start capture=yes IPv4.Address=192.168.7.236 tracefile=C:\HIS_capture.etl
# ... thao tác trên HIS ...
netsh trace stop
```

---

## 📝 Thông tin cần ghi lại

Sau khi thao tác, cần copy vào file `autopt01/captured_request.txt`:

```
=== REQUEST URL ===
POST http://192.168.7.236:1608/api/???/???

=== REQUEST HEADERS ===
TokenCode: ...
Content-Type: application/json
...

=== REQUEST BODY (JSON) ===
{
  "... toàn bộ JSON body ..."
}

=== RESPONSE BODY ===
{
  "Success": true,
  "Result": { "ID": ... }
}
```

---

## 📁 Cấu trúc thư mục autopt01

```
autopt01/
├── README.md               ← File này (hướng dẫn)
├── captured_request.txt    ← (CẦN TẠO) Kết quả capture traffic
├── HisPt01ApiUploader.cs   ← (CHỜ BUILD) Tool bypass UI — sẽ build sau khi có endpoint
└── HisPt01Batch.bat        ← (CHỜ BUILD) Batch runner 10 BN
```

---

## ⚡ Sau khi có captured_request.txt

Commit file lên git rồi báo Agent — Agent sẽ build `HisPt01ApiUploader.cs` + `HisPt01Batch.bat` ngay lập tức.

```powershell
git add autopt01/captured_request.txt
git commit -m "feat(autopt01): add captured HTTP request for PT-01 form API"
git push origin main
```
