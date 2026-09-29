# THƯ VIỆN TRA CỨU CẬN LÂM SÀNG KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57) - BỆNH VIỆN BẠCH MAI (CƠ SỞ HÀ NỘI)

Thư viện này cung cấp bảng ánh xạ (mapping) chi tiết toàn bộ các mã xét nghiệm và cận lâm sàng (CLS) thường xuyên chỉ định tại Khoa CTCH & Cột Sống (Khoa 57) - Bệnh viện Bạch Mai Hà Nội, quy tắc gom nhóm 1-Barcode / 1-Ống nghiệm, địa chỉ phòng thực hiện, và các gói Bilan mổ phiên lâm sàng chuẩn hóa.

---

## 1. NGUYÊN TẮC CỐT LÕI: GOM NHÓM Y LỆNH & TRÁNH NHÂN BẢN ỐNG MÁU (1-BARCODE RULE)

* **Nguyên lý hoạt động của LIS & HIS**:
  - Mỗi một `HIS_SERVICE_REQ` (Phiếu y lệnh) khi tạo sẽ cấp **1 Barcode / Tem xét nghiệm** riêng biệt.
  - Điều dưỡng buồng bệnh sẽ lấy số lượng ống nghiệm / bệnh phẩm tương ứng đúng bằng số lượng barcode được sinh ra.
* **Cảnh báo sai sót lâm sàng nghiêm trọng**:
  - Nếu gửi y lệnh rời rạc từng dịch vụ (vòng lặp $N$ lần API) $\rightarrow$ Hệ thống in $N$ barcode khác nhau $\rightarrow$ Bệnh nhân bị lấy $N$ ống máu cùng loại, gây lãng phí, đau đớn cho người bệnh và quá tải phòng xét nghiệm.
* **Quy tắc Gom Y Lệnh Chuẩn (1 Request = 1 Ống / 1 Bệnh Phẩm / 1 Phòng Tiếp Nhận)**:
  Tất cả các dịch vụ có **cùng loại ống nghiệm và cùng Phòng thực hiện (`RoomId`)** BẮT BUỘC phải đưa vào chung mảng `ServiceReqDetails` của **1 `AssignServiceSDO` duy nhất**.

---

## 2. BẢNG MAPPING CHI TIẾT CÁC MÃ CLS THEO PHÒNG TIẾP NHẬN & LOẠI BỆNH PHẨM

| STT | Phân Nhóm & Loại Bệnh Phẩm | Tên Dịch Vụ Chuẩn Trên HIS | Mã DV (`SERVICE_CODE`) | Service ID | Phòng Thực Hiện (`RoomId`) | Tên Phòng Tiếp Nhận | Vị Trí / Tòa Nhà | Đối Tượng | Ghi Chú Lâm Sàng |
|:---:|:---|:---|:---:|:---:|:---:|:---|:---|:---:|:---|
| **I** | **HUYẾT HỌC & TRUYỀN MÁU** | | | | | | | | |
| 1 | Ống EDTA nắp tím | Tổng phân tích tế bào máu ngoại vi (máy laser) | `BM00110` | `5745` | **`1772`** | Phòng XN Huyết Học Tế Bào | Tầng 2 - Nhà Q | BHYT (`1`) | **Gom chung với Máu lắng** vào 1 phiếu |
| 2 | Ống EDTA nắp tím | Máu lắng (bằng máy tự động) (ESR) | `BM00138` | `5686` | **`1772`** | Phòng XN Huyết Học Tế Bào | Tầng 2 - Nhà Q | BHYT (`1`) | Lấy chung 1 ống EDTA với CTM |
| 3 | Ống EDTA / Serum | Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động) | `BM01700` | `5783` | **`1464`** | Phòng XN Truyền Máu | Tầng 2 - Nhà Q | BHYT (`1`) | Bắt buộc cho mọi ca mổ phiên/cấp cứu |
| **II**| **ĐÔNG MÁU CƠ BẢN (ĐMCB)** | | | | | | | | |
| 4 | Ống Citrate nắp xanh lam | Thời gian Prothrombin (PT / TQ) tự động | `BM00531` | `5713` | **`626`** | Đơn nguyên Đông Máu | Tầng 2 - Nhà Q | BHYT (`1`) | **GOM 3 XÉT NGHIỆM 4, 5, 6** thành 1 phiếu gửi Phòng 626 $\rightarrow$ Lấy 1 ống Citrate |
| 5 | Ống Citrate nắp xanh lam | Thời gian APTT (TCK) bằng máy tự động | `BM260527.52`| `63622`| **`626`** | Đơn nguyên Đông Máu | Tầng 2 - Nhà Q | BHYT (`1`) | Mã áp dụng chuẩn sau 28/05/2026 |
| 6 | Ống Citrate nắp xanh lam | Định lượng Fibrinogen bằng máy tự động | `BM00542` | `5716` | **`626`** | Đơn nguyên Đông Máu | Tầng 2 - Nhà Q | BHYT (`1`) | Đánh giá đông máu & phản ứng viêm cấp |
| **III**| **SINH HÓA MÁU (SHM)** | | | | | | | | |
| 7 | Ống Serum / Heparin đỏ/vàng | Định lượng Urê [Máu] | `BM02304` | `5923` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | **GOM TẤT CẢ SINH HÓA MÁU** (STT 7 đến 17) thành 1 phiếu gửi Phòng 410 $\rightarrow$ Lấy đúng 1 ống máu nắp đỏ/vàng |
| 8 | Ống Serum / Heparin đỏ/vàng | Định lượng Creatinin (máu) | `BM01361` | `5934` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | **Bắt buộc** trước tiêm thuốc cản quang |
| 9 | Ống Serum / Heparin đỏ/vàng | Định lượng Glucose [Máu] tĩnh mạch | `BM10249` | `5864` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Glucose tĩnh mạch lúc đói |
| 10| Ống Serum / Heparin đỏ/vàng | Đo hoạt độ AST (GOT) | `BM01352` | `5834` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Đánh giá chức năng gan |
| 11| Ống Serum / Heparin đỏ/vàng | Đo hoạt độ ALT (GPT) | `BM01347` | `5833` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Đánh giá chức năng gan |
| 12| Ống Serum / Heparin đỏ/vàng | Điện giải đồ (Na, K, Cl) | `BM00132` | `5853` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Theo dõi ion đồ chu phẫu |
| 13| Ống Serum / Heparin đỏ/vàng | Định lượng CRP (C-Reactive Protein) | `BM02180` | `5995` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Theo dõi viêm nhiễm, sau mổ, cột sống |
| 14| Ống Serum / Heparin đỏ/vàng | Định lượng Acid Uric | `BM10485` | `5892` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Đánh giá bệnh Gout, viêm khớp |
| 15| Ống Serum / Heparin đỏ/vàng | Định lượng HbA1c | `BM01429` | `5870` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Đánh giá kiểm soát đường huyết mổ phiên |
| 16| Ống Serum / Heparin đỏ/vàng | Định lượng Troponin Ths | `BM260527.26`| `63596`| **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Sàng lọc NMCT, đau ngực chu phẫu |
| 17| Ống Serum / Heparin đỏ/vàng | Định lượng Cortisol | `BM02129` | `5929` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Nghi suy thượng thận do corticoid |
| 18| Ống Heparin chuyên dụng | Xét nghiệm Khí máu 11 thông số | `BM02047` | `5886` | **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | BHYT (`1`) | Bơm tiêm tráng heparin, gửi ngay |
| **IV**| **VIRUS & MIỄN DỊCH** | | | | | | | | |
| 19| Ống Miễn dịch nắp vàng/đỏ | HIV Ag/Ab miễn dịch tự động | `BM00871` | `6020` | **`871`** | Phòng XN Miễn Dịch - Vi Sinh| Tầng 4 - Nhà Q | BHYT (`1`) | **GOM 3 XÉT NGHIỆM 19, 20, 21** thành 1 phiếu gửi Phòng 871 $\rightarrow$ Lấy 1 ống Miễn dịch |
| 20| Ống Miễn dịch nắp vàng/đỏ | HBsAg miễn dịch tự động | `BM00859` | `6135` | **`871`** | Phòng XN Miễn Dịch - Vi Sinh| Tầng 4 - Nhà Q | BHYT (`1`) | Bilan virus mổ phiên |
| 21| Ống Miễn dịch nắp vàng/đỏ | HCV Ab miễn dịch tự động | `BM00837` | `6147` | **`871`** | Phòng XN Miễn Dịch - Vi Sinh| Tầng 4 - Nhà Q | BHYT (`1`) | Hoặc mã `BM26341` (ID `34801`) |
| **V** | **NƯỚC TIỂU** | | | | | | | | |
| 22| Lọ nước tiểu sạch | Tổng phân tích nước tiểu (bằng máy tự động)| `BM02998` | `5950` | **`566`** | Phòng Xét Nghiệm Nước Tiểu | Tầng 3 - Nhà Q | BHYT (`1`) | 10 thông số nước tiểu, lấy 1 lọ sạch |
| **VI**| **VI SINH & KHÁNG SINH ĐỒ** | | | | | | | | |
| 23| Mẫu bệnh phẩm mủ/máu/dịch | Vi khuẩn nuôi cấy và định danh hệ thống tự động| `BM01691`| `6054` | **`4374`**| Phòng XN Vi Khuẩn - Vi Nấm | Tầng 4 - Nhà Q | BHYT (`1`) | **BẮT BUỘC GOM CHUNG** với `BMDK01` |
| 24| Phiếu đính kèm | Đính Kèm KSD (Kháng sinh đồ) | `BMDK01` | `38374`| **`4374`**| Phòng XN Vi Khuẩn - Vi Nấm | Tầng 4 - Nhà Q | Dịch Vụ (`42`)| Kèm cùng phiếu cấy vi khuẩn |
| **VII**| **XÉT NGHIỆM LAO (IGRA)** | | | | | | | | |
| 25| Ống QuantiFERON chuyên dụng| Mycobacterium tuberculosis Quantiferon | `BM26362` | `36522`| **`9645`**| Phòng XN Lao (Gửi BV Phổi TW)| Tầng 4 - Nhà Q | Yêu Cầu (`43`)| Nghi ngờ Lao cột sống / Lao xương khớp |
| **VIII**| **THĂM DÒ CHỨC NĂNG (TDCN)**| | | | | | | | |
| 26| Tại buồng / Tiểu phẫu | Điện tim thường (ECG) | `BM04258` | `920` | **`931`** | P.Tiểu phẫu Nhà Q - Khoa 57 | Tầng trệt Nhà Q| BHYT (`1`) | Hoặc Phòng 5248 (P734 Khoa 57) |
| 27| Phòng TDCN Ngoại biên | Ghi điện cơ đo tốc độ dẫn truyền (EMG chi)| `BM01892`| `908` | **`931`** | P.Tiểu phẫu Nhà Q - Khoa 57 | Tầng trệt Nhà Q| BHYT (`1`) | Tổn thương đám rối cánh tay / rễ thần kinh |
| 28| Phòng Đo mật độ xương | Đo mật độ xương DEXA [1 vị trí] | `BM08084` | `160` | **`6462`**| Phòng 202 - Đo MĐX | Tầng 2 - Nhà K2| BHYT (`1`) | Note: `"điều dưỡng đưa bằng cáng - cs ii"` |
| 29| Phòng Đo mật độ xương | Đo mật độ xương DEXA [2 vị trí] | `BM08085` | `161` | **`6462`**| Phòng 202 - Đo MĐX | Tầng 2 - Nhà K2| BHYT (`1`) | Cột sống + Cổ xương đùi (Bắt buộc ca BXM) |
| **IX**| **SIÊU ÂM (ULTRASOUND)** | | | | | | | | |
| 30| Phòng Siêu âm Nội trú | Siêu âm ổ bụng tổng quát (gan mật tụy lách...)| `BM00199`| `5567` | **`17547`**| Phòng tiếp đón Siêu âm Nội trú| Tầng 1 - Nhà K1| BHYT (`1`) | Note: `"điều dưỡng đưa bằng cáng - cs ii"` |
| 31| Phòng Siêu âm Tim Nội trú | Siêu âm Doppler tim, van tim | `BM00201` | `5569` | **`1715`**| Phòng Siêu âm tim nội trú | C2 Viện Tim Mạch| BHYT (`1`) | Bắt buộc BN > 60 tuổi hoặc có bệnh tim |
| 32| Phòng Siêu âm Mạch máu | Siêu âm Doppler mạch máu chi | `BM00203` | `5571` | **`17547`**| Phòng tiếp đón Siêu âm Nội trú| Tầng 1 - Nhà K1| BHYT (`1`) | Tìm huyết khối tĩnh mạch sâu (DVT) |
| **X** | **X-QUANG SỐ HÓA (XRAY)** | | | | | | | | |
| 33| Phòng X-quang Nội trú | X-quang ngực thẳng số hóa (XQP) | `BM21074` | `58112`| **`17552`**| Phòng tiếp đón CĐHA Nội trú | Tầng 1 - Nhà Q | BHYT (`1`) | Thường quy cho mọi ca mổ phiên |
| 34| Phòng X-quang Nội trú | X-quang xương đòn thẳng, nghiêng | `BM00245.260119`| `58094`| **`17552`**| Phòng tiếp đón CĐHA Nội trú | Tầng 1 - Nhà Q | BHYT (`1`) | Không in phim |
| 35| Phòng X-quang Nội trú | X-quang cột sống thắt lưng thẳng, nghiêng | `BM00262.260119`| - | **`17552`**| Phòng tiếp đón CĐHA Nội trú | Tầng 1 - Nhà Q | BHYT (`1`) | Không in phim |
| 36| Phòng X-quang Nội trú | X-quang cột sống thắt lưng động (cúi - ưỡn)| - | - | **`17552`**| Phòng tiếp đón CĐHA Nội trú | Tầng 1 - Nhà Q | BHYT (`1`) | Đánh giá mất vững cột sống |
| **XI**| **CẮT LỚP VI TÍNH (CLVT / CT SCANNER)**| | | | | | | | |
| 37| Phòng CLVT Nội trú | Chụp CLVT phổi HRCT [đến 32 dãy không CQ]| `BM00338.260119`| `58181`| **`17549`**| Phòng tiếp đón CLVT Nội trú | Tầng 1 - Nhà K1| BHYT (`1`) | Đánh giá phổi chuyên sâu trước phẫu thuật |
| 38| Phòng CLVT Nội trú | Chụp CLVT cột sống thắt lưng không tiêm CQ | `BM00400.260119`| `58191`| **`17549`**| Phòng tiếp đón CLVT Nội trú | Tầng 1 - Nhà K1| BHYT (`1`) | Đánh giá cung sau, eo đốt sống, loãng xương |
| **XII**| **CỘNG HƯỞNG TỪ (MRI)** | | | | | | | | |
| 39| Phòng MRI Nội trú | Chụp CHT cột sống thắt lưng – cùng [Không in phim]| `BM00482.260119`| `58292`| **`17548`**| Phòng tiếp đón MRI Nội trú | Tầng 1 - Nhà Q | BHYT (`1`) | Đánh giá phù tủy xương, rễ, bao màng cứng |
| 40| Phòng MRI Nội trú | Chụp CHT cột sống cổ [Không in phim] | `BM260119.0620`| `58290`| **`17552`**| Phòng tiếp đón CĐHA/MRI Nội trú| Tầng 1 - Nhà Q | BHYT (`1`) | Đánh giá tủy cổ, rễ cổ, đám rối cánh tay |
| **XIII**| **ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG**| | | | | | | | |
| 41| Tại Khoa 57 | Xét nghiệm đường máu mao mạch tại giường (1 lần)| `BM02426`| `6217` | **`5248`**| P734 - Khoa CTCH & Cột Sống | P734 Tầng 7 N.Q| BHYT (`1`) | Dùng cho protocol ĐTĐ / Tủ trực Khoa 57 |

---

## 3. CÁC GÓI BILAN MỔ PHIÊN CHUẨN LÂM SÀNG (PRE-OPERATIVE PACKAGES)

Hệ thống hỗ trợ gọi trực tiếp qua CLI `HisClinicalCli.exe assign-bilan <treatmentId> <trackingId> <packType>`:

### 🌟 Gói 1: Bilan Mổ Bơm Xi Măng Sinh Học Cột Sống (`cement` / `bxm`)
* **Chỉ định cho**: Xẹp đốt sống ngực/thắt lưng do loãng xương, chấn thương xẹp đốt sống người cao tuổi.
* **Số lượng**: **20 kỹ thuật** gom thành 8 phiếu y lệnh.
* **Danh sách kỹ thuật**:
  1. **Huyết học (Phòng 1772 - 1 ống EDTA)**: CTM (`5745`).
  2. **Truyền máu (Phòng 1464 - 1 ống)**: Nhóm máu ABO/Rh (`5783`).
  3. **Đông máu (Phòng 626 - 1 ống Citrate)**: PT/TQ (`5713`) + APTT/TCK (`63622`) + Fibrinogen (`5716`).
  4. **Sinh hóa máu (Phòng 410 - 1 ống Serum/Heparin)**: Urê (`5923`) + Creatinin (`5934`) + Glucose (`5864`) + AST (`5834`) + ALT (`5833`) + Điện giải đồ (`5853`).
  5. **Virus Miễn dịch (Phòng 871 - 1 ống)**: HBsAg (`6135`) + HCV Ab (`6147`) + HIV (`6020`).
  6. **Nước tiểu (Phòng 566 - 1 lọ)**: TPT nước tiểu 10 thông số (`5950`).
  7. **Cận lâm sàng tại khoa & CĐHA**:
     - Điện tim thường (`920` - P931)
     - Siêu âm ổ bụng tổng quát (`5567` - P17547)
     - X-quang tim phổi thẳng (`58112` - P17552)
     - **Siêu âm Doppler tim, van tim (`5569` - P1715)** *(Bắt buộc phòng ngừa biến cố tim mạch khi bơm xi măng)*
     - **Đo mật độ xương DEXA 2 vị trí (`161` - P6462)** *(Bắt buộc chẩn đoán mức độ loãng xương T-score)*

---

### 🌟 Gói 2: Bilan Mổ Cố Định Cột Sống / Nẹp Vít / Giải Ép / TLIF (`spine` / `cotsong` / `nepvit`)
* **Chỉ định cho**: Trượt đốt sống, hẹp ống sống thắt lưng, thoát vị đĩa đệm có chỉ định nẹp vít hàn xương liên thân đốt.
* **Số lượng**: **18 kỹ thuật** gom thành 7 phiếu y lệnh.
* **Danh sách kỹ thuật**:
  1. Huyết học: CTM (`5745`).
  2. Truyền máu: Nhóm máu ABO/Rh (`5783`).
  3. Đông máu: PT/TQ (`5713`) + APTT/TCK (`63622`) + Fibrinogen (`5716`).
  4. Sinh hóa máu: Urê (`5923`) + Creatinin (`5934`) + Glucose (`5864`) + AST (`5834`) + ALT (`5833`) + Điện giải đồ (`5853`).
  5. Virus: HBsAg (`6135`) + HCV Ab (`6147`) + HIV (`6020`).
  6. Nước tiểu: TPT nước tiểu (`5950`).
  7. TDCN & CĐHA: Điện tim ECG (`920`) + Siêu âm ổ bụng (`5567`) + X-quang tim phổi (`58112`).
  *(Lưu ý: Nếu BN $\ge 60$ tuổi hoặc có bệnh tim, bổ sung thêm Siêu âm tim `BM00201`)*.

---

### 🌟 Gói 3: Bilan Mổ Chấn Thương / Kết Hợp Xương Chi (`trauma` / `chanthuong` / `ortho` / `khx`)
* **Chỉ định cho**: Gãy xương đùi, mâm chày, cẳng chân, xương đòn, cẳng tay, xương bánh chè.
* **Số lượng**: **18 kỹ thuật** gom thành 7 phiếu y lệnh.
* **Danh sách kỹ thuật**: Tương tự Gói Bilan Cột sống cơ bản (CTM, Nhóm máu, ĐMCB 3 chỉ số, SHM 6 chỉ số, Bilan Virus 3 chỉ số, Nước tiểu, Điện tim, Siêu âm bụng, X-quang phổi).
* Bổ sung theo tổn thương: X-quang chi tổn thương (`BM00245...`), Siêu âm Doppler mạch máu chi (`5571`) nếu nghi tắc mạch/chèn ép mạch.

---

### 🌟 Gói 4: Bilan Mổ Thay Khớp Háng / Khớp Gối (`hip` / `knee` / `thaykhop`)
* **Chỉ định cho**: Thoái hóa khớp gối/háng độ IV, gãy cổ xương đùi, hoại tử vô mạch chỏm xương đùi.
* **Bổ sung bắt buộc**:
  - Siêu âm tim Doppler (`5569` - P1715).
  - Siêu âm Doppler mạch máu chi dưới hai bên (`5571` - P17547) để sàng lọc huyết khối tĩnh mạch sâu (DVT) trước mổ.
  - Đo mật độ xương DEXA (`161` - P6462) để lựa chọn loại chuôi khớp có xi măng (cemented) hay không xi măng (cementless).

---

### 🌟 Gói 5: Bilan Mổ Vi Phẫu / Nối Gân Mạch Bàn Tay (`hand` / `viphau`)
* **Chỉ định cho**: Đứt gân gấp/duỗi, đứt mạch máu thần kinh ngón tay/bàn tay, chuyển gân.
* **Đặc điểm**: Bệnh nhân trẻ tuổi, thường không cần siêu âm ổ bụng nếu là chấn thương chi đơn thuần.
* **Danh sách kỹ thuật**: CTM, Nhóm máu, ĐMCB, SHM, Virus, Nước tiểu, Điện tim, X-quang phổi.

---

### 🌟 Gói 6: Bilan Theo Dõi Nhiễm Trùng Sau Mổ / Đánh Giá Viêm (`infection`)
* **Chỉ định cho**: Sốt sau mổ, vết mổ tấy đỏ, nghi ngờ nhiễm trùng vết mổ sâu hoặc viêm đĩa đệm đốt sống sau can thiệp.
* **Danh sách kỹ thuật**:
  - CTM (`5745` - P1772)
  - Máu lắng ESR (`5686` - P1772) $\rightarrow$ *Gom chung 1 ống EDTA*
  - CRP (`5995` - P410)
  - Procalcitonin (nếu sốt cao/nhiễm trùng huyết)
  - Cấy dịch mủ vết mổ (`6054` - P4374) + Đính kèm KSD (`38374` - P4374) $\rightarrow$ *Gom chung 1 phiếu*

---

## 4. BẢNG TỔNG HỢP CÁC PHÒNG TIẾP NHẬN CHUẨN KHOA 57 TẠI HÀ NỘI

| RoomId | Tên Phòng Trên Hệ Thống HIS | Tòa Nhà / Tầng | Chức Năng Tiếp Nhận Chính | Ghi Chú Khi Chỉ Định |
|:---:|:---|:---|:---|:---|
| **`410`** | Phòng Xét Nghiệm Sinh Hóa | Tầng 3 - Nhà Q | Toàn bộ xét nghiệm sinh hóa máu, khí máu, enzyme, ion đồ | Gom tất cả các chỉ số sinh hóa vào 1 phiếu để lấy 1 ống máu |
| **`626`** | Đơn nguyên Đông Máu | Tầng 2 - Nhà Q | Xét nghiệm chức năng đông máu (PT, APTT, Fibrinogen) | Bắt buộc gom đủ 3 chỉ số vào 1 phiếu. CẤM gửi mã gộp sang 1772 |
| **`1772`**| Phòng XN Huyết Học Tế Bào | Tầng 2 - Nhà Q | Tổng phân tích tế bào máu laser, Máu lắng (ESR) | Gom CTM và Máu lắng chung 1 ống EDTA |
| **`1464`**| Phòng XN Truyền Máu | Tầng 2 - Nhà Q | Định nhóm máu ABO, Rh(D) Gelcard | 1 ống riêng |
| **`871`** | Phòng XN Miễn Dịch - Vi Sinh | Tầng 4 - Nhà Q | Bilan virus (HIV, HBsAg, HCV Ab) | Gom 3 xét nghiệm vào 1 ống Miễn dịch |
| **`566`** | Phòng Xét Nghiệm Nước Tiểu | Tầng 3 - Nhà Q | Tổng phân tích nước tiểu 10 thông số | 1 lọ nước tiểu sạch |
| **`4374`**| Phòng XN Vi Khuẩn - Vi Nấm | Tầng 4 - Nhà Q | Nuôi cấy vi khuẩn định danh | Luôn đính kèm mã KSD `BMDK01` |
| **`9645`**| Phòng tiếp nhận QuantiFERON | Tầng 4 - Nhà Q | Xét nghiệm Lao chuyển BV Phổi TW | Đối tượng Yêu Cầu (`43`) |
| **`931`** | Phòng Tiểu phẫu Nhà Q | Tầng trệt - Nhà Q | Điện tim (ECG), Điện cơ (EMG), Tiểu phẫu tháo nẹp vít | Thực hiện tại chỗ Khoa 57 |
| **`5248`**| P734 - Khoa CTCH & CS | Tầng 7 - Nhà Q | ĐMMM tại giường, làm việc bác sĩ nội trú | Chỉ định ĐMMM `BM02426` |
| **`1715`**| Phòng Siêu âm tim nội trú | C2 - Viện Tim Mạch | Siêu âm Doppler tim, van tim cho BN nội trú | Kèm ghi chú `"điều dưỡng đưa bằng cáng - cs ii"` |
| **`17547`**| Phòng tiếp đón Siêu âm Nội trú| Tầng 1 - Nhà K1 | Siêu âm ổ bụng tổng quát, Siêu âm Doppler mạch máu chi | Kèm ghi chú `"điều dưỡng đưa bằng cáng - cs ii"` |
| **`17552`**| Phòng tiếp đón CĐHA Nội trú | Tầng 1 - Nhà Q | X-quang số hóa ngực, cột sống, xương chi | Gom các chỉ định X-quang cùng phiên |
| **`17549`**| Phòng tiếp đón CLVT Nội trú | Tầng 1 - Nhà K1 | Chụp CLVT (CT Scanner) phổi, cột sống, sọ não, chi | Gom các vị trí chụp CLVT cùng phiên |
| **`17548`**| Phòng tiếp đón MRI Nội trú | Tầng 1 - Nhà Q | Chụp cộng hưởng từ (MRI) cột sống, khớp, sọ não | Chụp CHT không in phim |
| **`6462`**| Phòng 202 - Đo MĐX | Tầng 2 - Nhà K2 | Đo mật độ xương phương pháp DEXA (1 hoặc 2 vị trí) | Kèm ghi chú `"điều dưỡng đưa bằng cáng - cs ii"` |

---

## 5. CÁC LỆNH CLI THỰC THI NHANH QUA `HisClinicalCli.exe`

```powershell
# 1. Chỉ định gói Bilan Mổ Bơm Xi Măng Cột Sống (BXM / Vertebroplasty)
.\HisClinicalCli.exe assign-bilan <treatmentId> <trackingId> cement

# 2. Chỉ định gói Bilan Mổ Cột Sống (Nẹp vít, giải ép)
.\HisClinicalCli.exe assign-bilan <treatmentId> <trackingId> spine

# 3. Chỉ định gói Bilan Mổ Chấn Thương Chỉnh Hình (Kết hợp xương)
.\HisClinicalCli.exe assign-bilan <treatmentId> <trackingId> trauma

# 4. Chỉ định tùy biến nhiều kỹ thuật chuẩn (tự động gom nhóm theo phòng)
.\HisClinicalCli.exe assign-custom <treatmentId> <trackingId> "CBC_LASER,PT_TQ,APTT_TCK,FIBRINOGEN,URE,CREATININ,GLUCOSE,GOT,GPT,ELECTROLYTES,CRP,ECG,XRAY_CHEST"

# 5. Tra cứu danh mục CLS chuẩn Khoa 57
.\HisClinicalCli.exe lookup-cls [từ khóa]
```
