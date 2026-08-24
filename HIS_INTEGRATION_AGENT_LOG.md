# HƯỚNG DẪN TÍCH HỢP & GIAO TIẾP HỆ THỐNG HIS CHO AGENT ANTIGRAVITY
> **Mục đích:** Tệp tài liệu này lưu trữ toàn bộ phương pháp kết nối, xác thực, truy vấn dữ liệu bệnh án, tra cứu cận lâm sàng (Bilan phẫu thuật) và tự động tạo chỉ định dịch vụ cận lâm sàng trên hệ thống HIS (Inventec MOS). 
> **Hướng dẫn sử dụng:** Khi dán tệp này vào bất kỳ phiên chat mới nào, Agent Antigravity có thể lập tức hiểu kiến trúc, kế thừa mã nguồn mẫu và tương tác trực tiếp với hệ thống HIS mà không cần thiết lập lại từ đầu.

---

## 1. THÔNG TIN MÔI TRƯỜNG & TÀI KHOẢN HỆ THỐNG

* **Thư mục ứng dụng HIS:** `d:\his\his-x64-28-11fix GDYK\his-x64`
* **Môi trường thực thi:** Windows x64 — .NET Framework 4.8 / C# 5 (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`)
* **Tài khoản đăng nhập HIS:**
  * **LoginName:** `vmc`
  * **Password:** `789789`
  * **App Version:** `2.390.0`
  * **Người dùng:** `VŨ MINH CƯỜNG` (Bác sĩ điều trị — Khoa Chấn thương Chỉnh hình & Cột sống)
* **Thông tin Cơ sở & Khoa phòng mục tiêu:**
  * **Cơ sở 1:** Bệnh viện Bạch Mai — Hà Nội (`BranchId = 1`)
  * **Khoa lâm sàng:** Khoa Chấn thương Chỉnh hình và Cột sống (Mã khoa: `9`, `DepartmentId = 57`)
  * **Phòng làm việc mặc định:** Phòng 734 (`RoomId = 5248`)

---

## 2. QUY TẮC BIÊN DỊCH VÀ THƯ VIỆN BẮT BUỘC (ASSEMBLIES)

Khi biên dịch các đoạn mã C# tương tác với HIS, bắt buộc phải:
1. **Sao chép tệp cấu hình:** `HIS.exe.Config` $\rightarrow$ `<TênChươngTrình>.exe.config` (cần thiết để nạp đúng endpoint WCF/WebAPI và ánh xạ Assembly).
2. **Tham chiếu các DLL chính:**
   * `ReferencedAssemblies\Inventec.Core.dll`
   * `ReferencedAssemblies\Inventec.Token.Core.dll`
   * `ReferencedAssemblies\Inventec.Token.ClientSystem.dll`
   * `ReferencedAssemblies\Inventec.Common.WebApiClient.dll`
   * `ReferencedAssemblies\Inventec.Common.Adapter.dll`
   * `ReferencedAssemblies\HIS.Desktop.LocalStorage.ConfigSystem.dll`
   * `ReferencedAssemblies\HIS.Desktop.ApiConsumer.dll`
   * `ReferencedAssemblies\MOS.Filter.dll`
   * `ReferencedAssemblies\MOS.SDO.dll`
   * `MOS.EFMODEL.dll`
   * `System.Configuration.dll`, `System.Data.dll`, `System.Data.SQLite.dll`

### Lệnh biên dịch chuẩn qua PowerShell / CMD:
```powershell
Copy-Item -Path "d:\his\his-x64-28-11fix GDYK\his-x64\HIS.exe.Config" -Destination "d:\his\his-x64-28-11fix GDYK\his-x64\<AppName>.exe.config" -Force

& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /out:"d:\his\his-x64-28-11fix GDYK\his-x64\<AppName>.exe" `
  /r:"System.Configuration.dll" /r:"System.Data.dll" /r:"d:\his\his-x64-28-11fix GDYK\his-x64\System.Data.SQLite.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Core.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.Core.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.ClientSystem.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Common.WebApiClient.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Common.Adapter.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\HIS.Desktop.LocalStorage.ConfigSystem.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\HIS.Desktop.ApiConsumer.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\MOS.Filter.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\MOS.SDO.dll" `
  /r:"d:\his\his-x64-28-11fix GDYK\his-x64\MOS.EFMODEL.dll" `
  "<SourceCodePath>.cs"

& "d:\his\his-x64-28-11fix GDYK\his-x64\<AppName>.exe"
```

---

## 3. LUỒNG XÁC THỰC & KHỞI TẠO PHIÊN LÀM VIỆC (AUTHENTICATION WORKFLOW)

```csharp
using System;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

// 1. Khởi tạo cấu hình ứng dụng
Load.Init();

// 2. Đăng nhập lấy TokenCode
ClientTokenManager tokenManager = new ClientTokenManager("HIS");
CommonParam param = new CommonParam();
var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
ApiConsumers.SetConsunmer(token.TokenCode);

// 3. [BẮT BUỘC KHI RA CHỈ ĐỊNH / Y LỆNH] Kích hoạt thông tin phòng làm việc
WorkInfoSDO workInfo = new WorkInfoSDO
{
    DepartmentId = 57,
    BranchId = 1,
    Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
};
new MyAdapter().PostData<bool>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
```

---

## 4. DANH MỤC API ENDPOINTS & BỘ LỌC DỮ LIỆU CHÍNH (QUERY MATRIX)

| Nghiệp vụ tra cứu | API Endpoint | Lớp Filter (`MOS.Filter`) | Kiểu dữ liệu trả về (`MOS.EFMODEL.DataModels`) |
| :--- | :--- | :--- | :--- |
| **Danh sách buồng bệnh của khoa** | `api/HisBedRoom/GetView` | `HisBedRoomViewFilter` (`DEPARTMENT_ID = 57`) | `List<V_HIS_BED_ROOM>` |
| **Danh sách bệnh nhân đang nằm viện** | `api/HisTreatmentBedRoom/GetLView` | `HisTreatmentBedRoomLViewFilter` (`BED_ROOM_IDs`, `IS_IN_ROOM = true`, `IS_PAUSE = false`) | `List<V_HIS_TREATMENT_BED_ROOM>` |
| **Hồ sơ bệnh án / Đợt điều trị** | `api/HisTreatment/GetView` | `HisTreatmentViewFilter` (`ID`, `KEY_WORD`, `TREATMENT_CODE`) | `List<V_HIS_TREATMENT>` |
| **Diễn biến lâm sàng / Tờ điều trị** | `api/HisTracking/GetView` | `HisTrackingViewFilter` (`TREATMENT_ID`) | `List<V_HIS_TRACKING>` |
| **Dấu hiệu sinh tồn (Mạch, HA, SpO2, BMI)**| `api/HisDhst/GetView` | `HisDhstViewFilter` (`TREATMENT_ID`) | `List<V_HIS_DHST>` |
| **Kết quả chỉ số xét nghiệm (Lab Tests)** | `api/HisSereServTein/GetView` | `HisSereServTeinViewFilter` (`TDL_TREATMENT_ID`) | `List<V_HIS_SERE_SERV_TEIN>` |
| **Dịch vụ đã chỉ định (CĐHA, TDCN, CLS)** | `api/HisSereServ/GetView` | `HisSereServViewFilter` (`TREATMENT_ID`) | `List<V_HIS_SERE_SERV>` |
| **Phiếu yêu cầu y lệnh** | `api/HisServiceReq/GetView` | `HisServiceReqViewFilter` (`TREATMENT_ID`, `ID`) | `List<V_HIS_SERVICE_REQ>` |
| **Chỉ định dịch vụ cận lâm sàng mới** | `api/HisServiceReq/AssignServiceByInstructionTimes` | `AssignServiceSDO` (Post Body) | `HisServiceReqListResultSDO` |

---

## 5. MẪU CHỈ ĐỊNH DỊCH VỤ CẬN LÂM SÀNG (ASSIGN SERVICE SNIPPET)

```csharp
// Tạo chỉ định dịch vụ X-quang / Xét nghiệm / MRI
AssignServiceSDO assignSDO = new AssignServiceSDO();
assignSDO.TreatmentId = targetTreatmentId;
assignSDO.RequestRoomId = 5248; // Phòng yêu cầu của bác sĩ
assignSDO.RequestLoginName = "vmc";
assignSDO.RequestUserName = "Vũ Minh Cường";
assignSDO.InstructionTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
assignSDO.InstructionTimes = new List<long> { assignSDO.InstructionTime };
assignSDO.IcdCode = "M48.06";
assignSDO.IcdName = "Hẹp ống sống thắt lưng";
assignSDO.Description = "CSII"; // Ghi chú lâm sàng

assignSDO.ServiceReqDetails = new List<ServiceReqDetailSDO>
{
    new ServiceReqDetailSDO
    {
        ServiceId = 58112,       // ID dịch vụ (VD: Chụp Xquang ngực thẳng BM00265.260119)
        RoomId = 17552,          // ID phòng thực hiện (VD: CDHA006)
        PatientTypeId = 1,       // 1 = BHYT, 2 = Viện phí
        Amount = 1,
        InstructionNote = "CSII"
    }
};

// Thực hiện gọi API tạo chỉ định
CommonParam postParam = new CommonParam();
HisServiceReqListResultSDO result = adapter.PostData<HisServiceReqListResultSDO>(
    "api/HisServiceReq/AssignServiceByInstructionTimes",
    ApiConsumers.MosConsumer,
    assignSDO,
    postParam
);

if (result != null && result.ServiceReqs != null && result.ServiceReqs.Count > 0)
{
    Console.WriteLine("Tạo y lệnh thành công! Mã phiếu: " + result.ServiceReqs[0].SERVICE_REQ_CODE);
}
```

> ⚠️ **BÀI HỌC KINH NGHIỆM QUAN TRỌNG (IDEMPOTENCY):**
> Máy chủ backend của HIS sẽ commit dữ liệu vào Database ngay khi nhận lệnh. Phía client luôn phải hứng kết quả đúng kiểu `HisServiceReqListResultSDO`. Nếu xảy ra lỗi mạng hoặc exception client, **không tự động retry lặp lại** mà phải query kiểm tra lại `V_HIS_SERVICE_REQ` để tránh tạo trùng nhiều y lệnh (tránh lỗi x4 chỉ định).

---

## 6. MẪU TRÍCH XUẤT BILAN TIỀN PHẪU & BIÊN BẢN THÔNG QUA MỔ

Mẫu code trích xuất tự động và xuất văn bản Word chuẩn:

```csharp
// Trích xuất các chỉ số xét nghiệm huyết học, đông máu, sinh hóa
var teinList = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);

// Lấy chỉ số cụ thể:
Func<string, string> getTein = (match) => {
    var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
    return item != null ? item.VALUE + " " + item.TEST_INDEX_UNIT_NAME : "-";
};

string hgb = getTein("Hemoglobin");
string wbc = getTein("Số lượng bạch cầu");
string plt = getTein("Số lượng tiểu cầu");
string inr = getTein("PT - INR");
string glucose = getTein("Glucose [Máu]");
string creatinin = getTein("Creatinin [máu]");
string bloodGroup = getTein("ABO") + " Rh " + getTein("Rh(D)");
```

---

## 7. CẤU TRÚC BIÊN BẢN THÔNG QUA MỔ CHUẨN

```text
BIÊN BẢN THÔNG QUA MỔ
Họ và tên người bệnh: [HỌ TÊN]
Ngày sinh: [DD/MM/YYYY]                     Giới tính: [Nam/Nữ]
Địa chỉ: [Địa chỉ bệnh nhân]
Vào viện: [Thời gian vào viện]
Chẩn đoán: [Chẩn đoán chính / Bệnh kèm theo]
Tiền sử: [Tiền sử bản thân, bệnh nền, phẫu thuật cũ, dị ứng]
Bệnh sử: [Quá trình diễn biến trước khi vào viện và tại bệnh viện]
Thời gian hội chẩn: 19/8/2026
Tóm tắt tình trạng bệnh: [DHST, toàn trạng, khám chuyên khoa cột sống/thần kinh, cơ lực, phản xạ]
Các xét nghiệm, chẩn đoán hình ảnh: [MRI, CT, X-quang, Nhóm máu, CBC, Đông máu, Sinh hóa, ECG, Siêu âm tim]
Phương pháp phẫu thuật: [TLIF / BXM / Lấy u - CĐCS / CĐCS vít nhồi xi măng - Nội soi giải ép]
```

---
*Tài liệu được khởi tạo và lưu giữ tự động bởi Antigravity Agent.*
