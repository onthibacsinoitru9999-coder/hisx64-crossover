using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HisMcp
{
    public class HisMcpServer
    {
        private static readonly string ServerName = "his-clinical";
        private static readonly string ServerVersion = "1.0.0";
        private static string BaseDir;

        public static int Main(string[] args)
        {
            Console.InputEncoding = new UTF8Encoding(false);
            Console.OutputEncoding = new UTF8Encoding(false);

            BaseDir = Environment.GetEnvironmentVariable("HIS_BASE_DIR");
            if (string.IsNullOrEmpty(BaseDir) || !Directory.Exists(BaseDir))
            {
                BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (string.IsNullOrEmpty(BaseDir) || !Directory.Exists(BaseDir))
            {
                BaseDir = Directory.GetCurrentDirectory();
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                var requestedName = new System.Reflection.AssemblyName(resolveArgs.Name).Name;
                string[] searchPaths = new string[]
                {
                    Path.Combine(BaseDir, requestedName + ".dll"),
                    Path.Combine(BaseDir, "ReferencedAssemblies", requestedName + ".dll"),
                    Path.Combine(BaseDir, "Integrate", "EMR", requestedName + ".dll"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, requestedName + ".dll"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", requestedName + ".dll")
                };
                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        try { return System.Reflection.Assembly.LoadFrom(path); } catch { }
                    }
                }
                return null;
            };

            // Test mode: HisMcpServer.exe --test
            if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t"))
            {
                Console.WriteLine("HisMcpServer self-test OK. BaseDir: " + BaseDir);
                return 0;
            }

            RunLoop();
            return 0;
        }

        private static void RunLoop()
        {
            using (var reader = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var req = JObject.Parse(line);
                        HandleRequest(req);
                    }
                    catch (Exception ex)
                    {
                        SendError(null, -32700, "Parse error: " + ex.Message);
                    }
                }
            }
        }

        private static void HandleRequest(JObject req)
        {
            var id = req["id"];
            string method = req["method"] != null ? req["method"].ToString() : "";
            var @params = req["params"] as JObject ?? new JObject();

            // Handle notification (no id)
            if (id == null)
            {
                if (method == "notifications/initialized")
                {
                    // Client acknowledged initialization
                }
                return;
            }

            switch (method)
            {
                case "initialize":
                    HandleInitialize(id, @params);
                    break;
                case "ping":
                    SendResponse(id, new JObject());
                    break;
                case "tools/list":
                    HandleToolsList(id);
                    break;
                case "tools/call":
                    HandleToolsCall(id, @params);
                    break;
                default:
                    SendError(id, -32601, "Method not found: " + method);
                    break;
            }
        }

        private static void HandleInitialize(JToken id, JObject @params)
        {
            var res = Obj(
                "protocolVersion", "2024-11-05",
                "capabilities", Obj("tools", new JObject()),
                "serverInfo", Obj(
                    "name", ServerName,
                    "version", ServerVersion
                )
            );
            SendResponse(id, res);
        }

        private static void HandleToolsList(JToken id)
        {
            var tools = new JArray();

            // 0a. his_hn (Chuyên biệt Cơ sở Hà Nội)
            tools.Add(CreateTool(
                "his_hn",
                "CONG CU DIEU PHOI CHUYEN BIET CO SO HA NOI (Bệnh viện Bạch Mai - Khoa 57 CTCH & Cột sống, P710-P740, Phòng 5248, Tủ trực 810/7787, Kho 4210/4209/753, ĐMMM BM02426, Token doctor_hn.token - Ths.BS Nguyễn Hữu Sâm 034727). Tự động cắm context và token Hà Nội cho mọi tác vụ lâm sàng.",
                Obj(
                    "action", Obj("type", "string", "description", "Tác vụ lâm sàng tại Hà Nội: 'lookup' (tra cứu BN), 'orders' (xem y lệnh), 'cancel_order' (hủy y lệnh), 'cancel_service' (hủy dịch vụ lẻ), 'tracking' (tờ điều trị + DHST), 'cabinet' (kê tủ trực 810), 'warehouse' (kê kho dược 4210/4209/753), 'ration' (suất ăn), 'leanpro' (dinh dưỡng trước mổ), 'pt01' (biên bản PT-01), 'pacs' (xem CĐHA), 'debate_view' (xem hội chẩn), 'debate_create' (tạo hội chẩn), 'emr_fill' (vỏ BA EMR), 'health' (kiểm tra kết nối), 'glucose' (thợ cho đường huyết HN), 'discharge' (thợ làm ra viện HN), 'raw_cli' (chạy CLI tùy biến)", "enum", Arr("lookup", "orders", "cancel_order", "cancel_service", "tracking", "cabinet", "warehouse", "ration", "leanpro", "pt01", "pacs", "debate_view", "debate_create", "emr_fill", "health", "glucose", "discharge", "raw_cli")),
                    "patientCode", Obj("type", "string", "description", "Mã bệnh nhân hoặc mã điều trị (VD: 24001234)"),
                    "mode", Obj("type", "string", "description", "Chế độ phụ: cabinet ('single', 'multi', 'insulin', 'leanpro', 'dressing', 'stock') hoặc warehouse ('single', 'multi', 'nutrition', 'search')"),
                    "medicineName", Obj("type", "string", "description", "Tên thuốc hoặc từ khóa tra cứu"),
                    "amount", Obj("type", "number", "description", "Số lượng thuốc"),
                    "tutorial", Obj("type", "string", "description", "Hướng dẫn sử dụng thuốc"),
                    "stockId", Obj("type", "integer", "description", "Mã kho/tủ trực (HN mặc định: 810 tủ trực, 4210 kho thuốc viên)"),
                    "items", Obj("type", "array", "items", Obj("type", "string"), "description", "Danh sách thuốc khi mode='multi'"),
                    "insulinUnits", Obj("type", "number", "description", "Số đơn vị Insulin (UI, VD: 6, 8, 10)"),
                    "insulinType", Obj("type", "string", "description", "Loại Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "timeSlot", Obj("type", "string", "description", "Mốc giờ y lệnh: '17:00', '21:00', '06:00', '17h', '21h', '6h'"),
                    "room", Obj("type", "string", "description", "Buồng bệnh nội trú (VD: '714', '715')"),
                    "rationType", Obj("type", "string", "description", "Mã loại suất ăn: 'BT01', 'DD01', 'TM01'..."),
                    "patientCodes", Obj("type", "string", "description", "Danh sách mã BN cách nhau bởi dấu phẩy"),
                    "orderId", Obj("type", "string", "description", "Mã ID y lệnh (SERVICE_REQ_ID) để hủy"),
                    "serviceReqMatId", Obj("type", "string", "description", "Mã ID dịch vụ con để hủy"),
                    "progressNote", Obj("type", "string", "description", "Nội dung diễn biến lâm sàng cho tờ điều trị"),
                    "pulse", Obj("type", "integer", "description", "Mạch (lần/phút)"),
                    "bloodPressure", Obj("type", "string", "description", "Huyết áp (VD: '120/80')"),
                    "temperature", Obj("type", "number", "description", "Nhiệt độ (độ C)"),
                    "spO2", Obj("type", "integer", "description", "SpO2 (%)"),
                    "respiratoryRate", Obj("type", "integer", "description", "Nhịp thở (lần/phút)"),
                    "instructionTime", Obj("type", "string", "description", "Thời gian y lệnh YYYYMMDDHHmmss"),
                    "specialtyCode", Obj("type", "string", "description", "Mã khoa mời hội chẩn (VD: 'NOITIET')"),
                    "requestContent", Obj("type", "string", "description", "Nội dung yêu cầu hội chẩn"),
                    "save", Obj("type", "boolean", "description", "Lưu vào Oracle EMR (cho emr_fill)"),
                    "force", Obj("type", "boolean", "description", "Ghi đè dữ liệu (cho emr_fill)"),
                    "summary", Obj("type", "string", "description", "Tóm tắt bệnh án tùy biến"),
                    "openBrowser", Obj("type", "boolean", "description", "Mở trình duyệt xem ảnh PACS"),
                    "glucoseValue", Obj("type", "number", "description", "Giá trị đường huyết mmol/L (khi action='glucose')"),
                    "dryRun", Obj("type", "boolean", "description", "Chế độ chạy thử kiểm tra trước"),
                    "command", Obj("type", "string", "description", "Lệnh CLI tùy biến khi action='raw_cli'")
                ),
                Arr("action")
            ));

            // 0b. his_nb (Chuyên biệt Cơ sở Ninh Bình)
            tools.Add(CreateTool(
                "his_nb",
                "CONG CU DIEU PHOI CHUYEN BIET CO SO NINH BINH (Bệnh viện Bạch Mai CS2 - Khoa 915 Ngoại tổng hợp Tầng 3 Nhà E, Buồng 3E/3D, Phòng TT P3E-05 ID 18679 / P3D-05 ID 18681, Tủ trực 5142 Khu 3E / 5141 Khu 3D, Kho Dược chính 4854, ĐMMM NB260620.6231 ID 74281, Token doctor_nb.token - BS Vũ Minh Cường vmc). Tự động cắm context và token Ninh Bình cho mọi tác vụ lâm sàng.",
                Obj(
                    "action", Obj("type", "string", "description", "Tác vụ lâm sàng tại Ninh Bình: 'lookup' (tra cứu BN), 'orders' (xem y lệnh), 'cancel_order' (hủy y lệnh), 'cancel_service' (hủy dịch vụ lẻ), 'tracking' (tờ điều trị + DHST), 'cabinet' (kê tủ trực 5142/5141), 'warehouse' (kê kho dược 4854), 'ration' (suất ăn), 'leanpro' (dinh dưỡng trước mổ), 'pt01' (biên bản PT-01), 'pacs' (xem CĐHA), 'debate_view' (xem hội chẩn), 'debate_create' (tạo hội chẩn), 'emr_fill' (vỏ BA EMR), 'health' (kiểm tra kết nối), 'glucose' (thợ cho đường huyết NB), 'discharge' (thợ làm ra viện NB), 'raw_cli' (chạy CLI tùy biến)", "enum", Arr("lookup", "orders", "cancel_order", "cancel_service", "tracking", "cabinet", "warehouse", "ration", "leanpro", "pt01", "pacs", "debate_view", "debate_create", "emr_fill", "health", "glucose", "discharge", "raw_cli")),
                    "patientCode", Obj("type", "string", "description", "Mã bệnh nhân hoặc mã điều trị (VD: 24001234)"),
                    "mode", Obj("type", "string", "description", "Chế độ phụ: cabinet ('single', 'multi', 'insulin', 'leanpro', 'dressing', 'stock') hoặc warehouse ('single', 'multi', 'nutrition', 'search')"),
                    "medicineName", Obj("type", "string", "description", "Tên thuốc hoặc từ khóa tra cứu"),
                    "amount", Obj("type", "number", "description", "Số lượng thuốc"),
                    "tutorial", Obj("type", "string", "description", "Hướng dẫn sử dụng thuốc"),
                    "stockId", Obj("type", "integer", "description", "Mã kho/tủ trực (NB mặc định: 5142 tủ trực 3E, 4854 kho dược chính)"),
                    "items", Obj("type", "array", "items", Obj("type", "string"), "description", "Danh sách thuốc khi mode='multi'"),
                    "insulinUnits", Obj("type", "number", "description", "Số đơn vị Insulin (UI, VD: 6, 8, 10)"),
                    "insulinType", Obj("type", "string", "description", "Loại Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "timeSlot", Obj("type", "string", "description", "Mốc giờ y lệnh: '17:00', '21:00', '06:00', '17h', '21h', '6h'"),
                    "room", Obj("type", "string", "description", "Buồng bệnh nội trú Khu 3E/3D (VD: '3E-24', '3E-05', '3D-05')"),
                    "rationType", Obj("type", "string", "description", "Mã loại suất ăn: 'BT01', 'DD01', 'TM01'..."),
                    "patientCodes", Obj("type", "string", "description", "Danh sách mã BN cách nhau bởi dấu phẩy"),
                    "orderId", Obj("type", "string", "description", "Mã ID y lệnh (SERVICE_REQ_ID) để hủy"),
                    "serviceReqMatId", Obj("type", "string", "description", "Mã ID dịch vụ con để hủy"),
                    "progressNote", Obj("type", "string", "description", "Nội dung diễn biến lâm sàng cho tờ điều trị"),
                    "pulse", Obj("type", "integer", "description", "Mạch (lần/phút)"),
                    "bloodPressure", Obj("type", "string", "description", "Huyết áp (VD: '120/80')"),
                    "temperature", Obj("type", "number", "description", "Nhiệt độ (độ C)"),
                    "spO2", Obj("type", "integer", "description", "SpO2 (%)"),
                    "respiratoryRate", Obj("type", "integer", "description", "Nhịp thở (lần/phút)"),
                    "instructionTime", Obj("type", "string", "description", "Thời gian y lệnh YYYYMMDDHHmmss"),
                    "specialtyCode", Obj("type", "string", "description", "Mã khoa mời hội chẩn (VD: 'NOITIET')"),
                    "requestContent", Obj("type", "string", "description", "Nội dung yêu cầu hội chẩn"),
                    "save", Obj("type", "boolean", "description", "Lưu vào Oracle EMR (cho emr_fill)"),
                    "force", Obj("type", "boolean", "description", "Ghi đè dữ liệu (cho emr_fill)"),
                    "summary", Obj("type", "string", "description", "Tóm tắt bệnh án tùy biến"),
                    "openBrowser", Obj("type", "boolean", "description", "Mở trình duyệt xem ảnh PACS"),
                    "glucoseValue", Obj("type", "number", "description", "Giá trị đường huyết mmol/L (khi action='glucose')"),
                    "dryRun", Obj("type", "boolean", "description", "Chế độ chạy thử kiểm tra trước"),
                    "command", Obj("type", "string", "description", "Lệnh CLI tùy biến khi action='raw_cli'")
                ),
                Arr("action")
            ));

            // 1. his_patient_lookup
            tools.Add(CreateTool(
                "his_patient_lookup",
                "Tra cuu ho so benh nhan, thong tin hanh chinh, buong benh, tien su, dich vu, don thuoc cu...",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (VD: 24001234)"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCode")
            ));

            // 2. his_get_orders
            tools.Add(CreateTool(
                "his_get_orders",
                "Xem danh sach y lenh lam sang va trang thai mau sac (trang: chua thuc hien, vang: dang thuc hien, xanh: da hoan thanh)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri")
                ),
                Arr("patientCode")
            ));

            // 3. his_cancel_order
            tools.Add(CreateTool(
                "his_cancel_order",
                "Huy y lenh lam sang chua thuc hien (mau trang). Tuyet doi khong huy y lenh mau vang hoac xanh!",
                Obj(
                    "orderId", Obj("type", "string", "description", "Ma ID cua y lenh (SERVICE_REQ_ID)")
                ),
                Arr("orderId")
            ));

            // 4. his_cancel_service
            tools.Add(CreateTool(
                "his_cancel_service",
                "Huy dich vu con don le trong phieu y lenh lam sang (chua thuc hien)",
                Obj(
                    "serviceReqMatId", Obj("type", "string", "description", "Ma ID dich vu con (SERE_SERV_ID hoac SERVICE_REQ_MAT_ID)")
                ),
                Arr("serviceReqMatId")
            ));

            // 5. his_debate_view
            tools.Add(CreateTool(
                "his_debate_view",
                "Doc Bien ban Hoi chan va y kien cac chuyen khoa khach moi cho benh nhan",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri")
                ),
                Arr("patientCode")
            ));

            // 6. his_create_tracking
            tools.Add(CreateTool(
                "his_create_tracking",
                "Tao To dieu tri hang ngay (Treatment Tracking - Type 7) kem dau hieu sinh ton (DHST), tu dong sinh dien bien qua OpenRouter AI va ky so EMR",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "progressNote", Obj("type", "string", "description", "Noi dung dien bien lam sang (de trong de AI tu dong sinh)"),
                    "pulse", Obj("type", "integer", "description", "Mach (lan/phut, mac dinh 75-80)"),
                    "bloodPressure", Obj("type", "string", "description", "Huyet ap (VD: '120/80')"),
                    "temperature", Obj("type", "number", "description", "Nhiet do (do C, mac dinh 36.5)"),
                    "spO2", Obj("type", "integer", "description", "SpO2 (%, mac dinh 98)"),
                    "respiratoryRate", Obj("type", "integer", "description", "Nhip tho (lan/phut, mac dinh 18-20)"),
                    "instructionTime", Obj("type", "string", "description", "Thoi gian y lenh YYYYMMDDHHmmss (de trong lay gio hien tai)")
                ),
                Arr("patientCode")
            ));

            // 7a. his_prescribe_cabinet
            tools.Add(CreateTool(
                "his_prescribe_cabinet",
                "Ke don thuoc dieu tri (khang sinh, giam dau, dich truyen, vien nen...), tiem Insulin, Leanpro truoc mo, vat tu thay bang tu TU TRUC (IS_CABINET = 1: HN Khoa 57 kho 810, Tu truc dinh duong TTSPDD_9 kho 7787, NB Khu 3E kho 5142, Khu 3D kho 5141). Ho tro ca tra cuu ton tu truc (mode='stock').",
                Obj(
                    "mode", Obj("type", "string", "description", "Che do: 'single' (1 thuoc), 'multi' (toa thuoc dieu tri), 'insulin' (tiem insulin), 'leanpro' (dinh duong truoc mo 7787), 'dressing' (thay bang 810), 'stock' (xem ton tu truc)", "enum", Arr("single", "multi", "insulin", "leanpro", "dressing", "stock")),
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (khong bat buoc khi mode='stock')"),
                    "stockId", Obj("type", "integer", "description", "Ma kho tu truc (HN: 810 Khoa 57; Dinh duong: 7787; NB: 5142 Khu 3E, 5141 Khu 3D). Mac dinh 810"),
                    "medicineName", Obj("type", "string", "description", "Ten hoac ma thuoc (khi mode='single')"),
                    "amount", Obj("type", "number", "description", "So luong thuoc (khi mode='single' hoac 'leanpro')"),
                    "tutorial", Obj("type", "string", "description", "Huong dan su dung thuoc (khi mode='single')"),
                    "items", Obj("type", "array", "items", Obj("type", "string"), "description", "Danh sach thuoc khi mode='multi'. Dinh dang moi phan tu: 'TenThuoc|SoLuong|HDSD|[DuongDungId]'"),
                    "insulinUnits", Obj("type", "number", "description", "So don vi Insulin UI (khi mode='insulin', VD: 8, 10, 12)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17:00', '21:00', '06:00'..."),
                    "keyword", Obj("type", "string", "description", "Tu khoa loc ton kho (khi mode='stock')")
                ),
                Arr("mode")
            ));

            // 7b. his_prescribe_warehouse
            tools.Add(CreateTool(
                "his_prescribe_warehouse",
                "Ke don thuoc noi tru thuong quy, san pham dinh duong dieu tri LINH TU KHO DUOC / CAP PHAT (IS_CABINET = 0: Kho thuoc vien 4210, Kho thuoc ong 4209, Kho SP dinh duong 753, Kho duoc chinh NB 4854). Ho tro tra cuu danh muc Duoc (mode='search').",
                Obj(
                    "mode", Obj("type", "string", "description", "Che do: 'single' (1 thuoc linh), 'multi' (toa thuoc linh), 'nutrition' (dinh duong kho 753), 'search' (tra cuu danh muc)", "enum", Arr("single", "multi", "nutrition", "search")),
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (khong bat buoc khi mode='search')"),
                    "stockId", Obj("type", "integer", "description", "Ma kho cap phat (Kho thuoc vien: 4210, Kho thuoc ong: 4209, Kho dinh duong: 753, CSNB: 4854). Mac dinh 4210"),
                    "medicineName", Obj("type", "string", "description", "Ten/ma thuoc hoac tu khoa tra cuu (khi mode='single' hoac 'search')"),
                    "amount", Obj("type", "number", "description", "So luong thuoc linh (khi mode='single' hoac 'nutrition')"),
                    "tutorial", Obj("type", "string", "description", "Huong dan su dung thuoc"),
                    "items", Obj("type", "array", "items", Obj("type", "string"), "description", "Danh sach thuoc khi mode='multi'. Dinh dang moi phan tu: 'Thuoc|SL|HDSD|[KhoId]|[DuongDungId]|[Cu Sang:Trua:Chieu:Toi]'"),
                    "useFormId", Obj("type", "integer", "description", "ID duong dung (1: Uong, 15: Tiem, 20: Truyen TM, 25: Dung ngoai, 32: Dinh duong)"),
                    "doses", Obj("type", "string", "description", "Cu uong 'Sang:Trua:Chieu:Toi' (VD: '01::01')")
                ),
                Arr("mode")
            ));

            // 7c. his_prescribe_medicine (Fallback/Alias)
            tools.Add(CreateTool(
                "his_prescribe_medicine",
                "Ke don thuoc tu Tu truc (HN: Kho 810 / NB: Kho 5142) hoac Kho Duoc (Dieu phoi tu dong). Khuyen nghi uu tien dung his_prescribe_cabinet hoac his_prescribe_warehouse.",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "stockId", Obj("type", "integer", "description", "Ma kho/tu truc (HN: 810; NB: 5142; Kho vien: 4210). Mac dinh 810"),
                    "medicines", Obj("type", "string", "description", "Mo ta thuoc hoac danh sach thuoc (VD: 'Actrapid 8UI tiem duoi da')"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCode", "medicines")
            ));

            // 8. his_assign_ration
            tools.Add(CreateTool(
                "his_assign_ration",
                "Chi dinh Suat an dinh duong benh ly (BT01, DD01, TM01...) cho toan bo benh nhan trong buong benh",
                Obj(
                    "room", Obj("type", "string", "description", "Ten buong benh (VD: '714', '715', '3E-24')"),
                    "rationType", Obj("type", "string", "description", "Ma loai suat an: 'BT01', 'DD01', 'TM01'"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("room")
            ));

            // 9. his_assign_leanpro
            tools.Add(CreateTool(
                "his_assign_leanpro",
                "Chi dinh Dinh duong truoc mo (Leanpro PreSur). Tu dong chan benh nhan >= 70 tuoi hoac Dai thao duong",
                Obj(
                    "patientCodes", Obj("type", "string", "description", "Danh sach ma benh nhan cach nhau boi dau phay"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCodes")
            ));

            // 12. his_create_pt01
            tools.Add(CreateTool(
                "his_create_pt01",
                "Tao Bien ban Hoi chan thong qua mo (MS: PT-01) chuan docx tu du lieu lam sang va CDHA",
                Obj(
                    "patientCodes", Obj("type", "string", "description", "Danh sach ma benh nhan cach nhau boi dau phay")
                ),
                Arr("patientCodes")
            ));

            // 13. his_view_pacs
            tools.Add(CreateTool(
                "his_view_pacs",
                "Tra cuu danh sach ca chup CDHA (MRI, CT, X-Quang) va tao URL xem truc tiep tren Web PACS",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "openBrowser", Obj("type", "boolean", "description", "Co mo trinh duyet truc tiep khong (mac dinh false)")
                ),
                Arr("patientCode")
            ));

            // 14. his_debate_create
            tools.Add(CreateTool(
                "his_debate_create",
                "Tao Phieu chi dinh Hoi chan chuyen khoa va Trich bien ban hoi chan (EMR Type 17 / Mps000019)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "specialtyCode", Obj("type", "string", "description", "Ma khoa moi hoi chan (VD: 'NOITIET', 'TIMMACH')"),
                    "requestContent", Obj("type", "string", "description", "Noi dung yeu cau hoi chan")
                ),
                Arr("patientCode", "specialtyCode", "requestContent")
            ));

            // 15. his_emr_fill
            tools.Add(CreateTool(
                "his_emr_fill",
                "Dien Vo Benh An Ngoai Khoa EMR (CHI AP DUNG BENH NHAN NOI TRU Khoa 57 / Khoa 915). Mac dinh chi xem truoc (dry-run), can truyen save=true de ghi vao Oracle EMR",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan noi tru"),
                    "save", Obj("type", "boolean", "description", "Luu vao Oracle EMR (mac dinh false - dry-run truoc)"),
                    "force", Obj("type", "boolean", "description", "Ghi de toan bo cac truong du lieu (mac dinh false - chi cap nhat truong trong)"),
                    "summary", Obj("type", "string", "description", "Noi dung Tom tat benh an tuy bien neu can override logic mac dinh")
                ),
                Arr("patientCode")
            ));

            // 16. his_system_health
            tools.Add(CreateTool(
                "his_system_health",
                "Kiem tra suc khoe ket noi HIS, TokenCode, WorkInfo va ping server",
                new JObject(),
                new JArray()
            ));

            var res = Obj("tools", tools);
            SendResponse(id, res);
        }

        private static JObject CreateTool(string name, string desc, JObject properties, JArray required)
        {
            var schema = Obj(
                "type", "object",
                "properties", properties,
                "required", required,
                "additionalProperties", false
            );

            return Obj(
                "name", name,
                "description", desc,
                "inputSchema", schema
            );
        }

        private static string DetectFacility(JObject args, string defaultFacility = "HN")
        {
            if (args == null) return defaultFacility;

            // 1. Explicit facility parameter
            if (args["facility"] != null)
            {
                string f = args["facility"].ToString().Trim().ToUpper();
                if (f == "NB" || f == "NINHBINH" || f == "NINH BÌNH" || f == "NINH_BINH") return "NB";
                if (f == "HN" || f == "HANOI" || f == "HÀ NỘI" || f == "HA_NOI") return "HN";
            }

            // 2. Room keyword
            if (args["room"] != null)
            {
                string r = args["room"].ToString().ToUpper();
                if (r.Contains("3E") || r.Contains("3D")) return "NB";
                if (r.StartsWith("7") || r.Contains("71") || r.Contains("72") || r.Contains("73") || r.Contains("74")) return "HN";
            }

            // 3. Stock ID
            if (args["stockId"] != null)
            {
                long s;
                if (long.TryParse(args["stockId"].ToString(), out s))
                {
                    if (s == 5142 || s == 5141 || s == 4854) return "NB";
                    if (s == 810 || s == 7787 || s == 4210 || s == 4209 || s == 753) return "HN";
                }
            }

            // 4. Scan string fields for facility indicators
            foreach (var prop in args.Properties())
            {
                if (prop.Value != null)
                {
                    string val = prop.Value.ToString();
                    if (val.IndexOf("Ninh Bình", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("Khu 3E", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("Khu 3D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("Khoa 915", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("P3E-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("3E-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("5142", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("vmc", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "NB";
                    }
                    if (val.IndexOf("Hà Nội", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("Khoa 57", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("P734", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        val.IndexOf("034727", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "HN";
                    }
                }
            }

            return defaultFacility;
        }

        private static void HandleToolsCall(JToken id, JObject @params)
        {
            string toolName = @params["name"] != null ? @params["name"].ToString() : "";
            var args = @params["arguments"] as JObject ?? new JObject();

            try
            {
                string output = "";
                bool isError = false;
                string detectedFacility = DetectFacility(args);

                switch (toolName)
                {
                    case "his_hn":
                        output = ExecuteFacilityRouter("HN", args, out isError);
                        break;
                    case "his_nb":
                        output = ExecuteFacilityRouter("NB", args, out isError);
                        break;
                    case "his_patient_lookup":
                        output = ExecutePatientLookup(args, out isError, detectedFacility);
                        break;
                    case "his_get_orders":
                        output = ExecuteGetOrders(args, out isError, detectedFacility);
                        break;
                    case "his_cancel_order":
                        output = ExecuteCancelOrder(args, out isError, detectedFacility);
                        break;
                    case "his_cancel_service":
                        output = ExecuteCancelService(args, out isError, detectedFacility);
                        break;
                    case "his_debate_view":
                        output = ExecuteDebateView(args, out isError, detectedFacility);
                        break;
                    case "his_create_tracking":
                        output = ExecuteCreateTracking(args, out isError, detectedFacility);
                        break;
                    case "his_prescribe_cabinet":
                        output = ExecutePrescribeCabinet(args, out isError, detectedFacility);
                        break;
                    case "his_prescribe_warehouse":
                        output = ExecutePrescribeWarehouse(args, out isError, detectedFacility);
                        break;
                    case "his_prescribe_medicine":
                        output = ExecutePrescribe(args, out isError, detectedFacility);
                        break;
                    case "his_assign_ration":
                        output = ExecuteAssignRation(args, out isError, detectedFacility);
                        break;
                    case "his_assign_leanpro":
                        output = ExecuteAssignLeanpro(args, out isError, detectedFacility);
                        break;
                    case "his_create_pt01":
                        output = ExecuteCreatePt01(args, out isError, detectedFacility);
                        break;
                    case "his_view_pacs":
                        output = ExecuteViewPacs(args, out isError, detectedFacility);
                        break;
                    case "his_debate_create":
                        output = ExecuteDebateCreate(args, out isError, detectedFacility);
                        break;
                    case "his_emr_fill":
                        output = ExecuteEmrFill(args, out isError, detectedFacility);
                        break;
                    case "his_system_health":
                        output = ExecuteSystemHealth(out isError, detectedFacility);
                        break;
                    default:
                        SendError(id, -32602, "Unknown tool: " + toolName);
                        return;
                }

                var content = Arr(
                    Obj(
                        "type", "text",
                        "text", output
                    )
                );

                var res = Obj(
                    "content", content,
                    "isError", isError
                );
                SendResponse(id, res);
            }
            catch (Exception ex)
            {
                var content = Arr(
                    Obj(
                        "type", "text",
                        "text", "LOI THUC THI TOOL [" + toolName + "]: " + ex.Message + "\n" + ex.StackTrace
                    )
                );
                var res = Obj(
                    "content", content,
                    "isError", true
                );
                SendResponse(id, res);
            }
        }

        #region Tool Implementations

        private static string ExecuteFacilityRouter(string facility, JObject args, out bool isError)
        {
            string action = args["action"] != null ? args["action"].ToString().Trim().ToLower() : "";
            if (string.IsNullOrEmpty(action))
            {
                isError = true;
                return "Loi: Tham so 'action' khong duoc de trong cho " + (facility == "NB" ? "his_nb" : "his_hn");
            }

            // Force facility in arguments
            args["facility"] = facility;

            // Set facility stock defaults if stockId was not explicitly given or is default 810/5142
            if (facility == "NB")
            {
                if (args["stockId"] == null || (long)args["stockId"] == 810)
                {
                    if (action == "warehouse") args["stockId"] = 4854;
                    else args["stockId"] = 5142;
                }
            }
            else
            {
                if (args["stockId"] == null || (long)args["stockId"] == 5142)
                {
                    if (action == "warehouse") args["stockId"] = 4210;
                    else args["stockId"] = 810;
                }
            }

            string banner = string.Format(
                "===============================================================================\n" +
                "🏥 [HIS ROUTER -> CƠ SỞ: {0}]\n" +
                "• Khoa: {1} | Phòng: {2} | Tủ trực: {3} | Kho Dược: {4}\n" +
                "• Bác sĩ: {5} ({6}) | Token: {7}\n" +
                "===============================================================================\n\n",
                (facility == "NB" ? "BỆNH VIỆN BẠCH MAI CƠ SỞ 2 - NINH BÌNH" : "BỆNH VIỆN BẠCH MAI - HÀ NỘI"),
                (facility == "NB" ? "Khoa Ngoại tổng hợp Tầng 3 Nhà E (Khoa 915)" : "Khoa CTCH & Cột sống (Khoa 57)"),
                (facility == "NB" ? "P3E-05 (ID 18679) / P3D-05 (ID 18681)" : "P734 (ID 5248)"),
                (facility == "NB" ? "5142 (Khu 3E) / 5141 (Khu 3D)" : "810 (TT_KCTCHCS) / 7787 (TTSPDD_9)"),
                (facility == "NB" ? "4854 (Kho Dược chính CSNB)" : "4210 (Viên) / 4209 (Ống) / 753 (DD)"),
                (facility == "NB" ? "BS VŨ MINH CƯỜNG" : "Ths.BS NGUYỄN HỮU SÂM"),
                (facility == "NB" ? "vmc" : "034727"),
                (facility == "NB" ? "doctor_nb.token" : "doctor_hn.token")
            );

            string result = "";
            switch (action)
            {
                case "lookup":
                    result = ExecutePatientLookup(args, out isError, facility);
                    break;
                case "orders":
                    result = ExecuteGetOrders(args, out isError, facility);
                    break;
                case "cancel_order":
                    result = ExecuteCancelOrder(args, out isError, facility);
                    break;
                case "cancel_service":
                    result = ExecuteCancelService(args, out isError, facility);
                    break;
                case "debate_view":
                    result = ExecuteDebateView(args, out isError, facility);
                    break;
                case "tracking":
                    result = ExecuteCreateTracking(args, out isError, facility);
                    break;
                case "cabinet":
                    result = ExecutePrescribeCabinet(args, out isError, facility);
                    break;
                case "warehouse":
                    result = ExecutePrescribeWarehouse(args, out isError, facility);
                    break;
                case "ration":
                    result = ExecuteAssignRation(args, out isError, facility);
                    break;
                case "leanpro":
                    result = ExecuteAssignLeanpro(args, out isError, facility);
                    break;
                case "pt01":
                    result = ExecuteCreatePt01(args, out isError, facility);
                    break;
                case "pacs":
                    result = ExecuteViewPacs(args, out isError, facility);
                    break;
                case "debate_create":
                    result = ExecuteDebateCreate(args, out isError, facility);
                    break;
                case "emr_fill":
                    result = ExecuteEmrFill(args, out isError, facility);
                    break;
                case "health":
                    result = ExecuteSystemHealth(out isError, facility);
                    break;
                case "glucose":
                    result = ExecuteGlucoseProtocol(args, out isError, facility);
                    break;
                case "discharge":
                    result = ExecuteDischargeProtocol(args, out isError, facility);
                    break;
                case "raw_cli":
                    string cmd = args["command"] != null ? args["command"].ToString() : "";
                    if (string.IsNullOrEmpty(cmd))
                    {
                        isError = true;
                        return banner + "Loi: Tham so 'command' khong duoc de trong khi action='raw_cli'";
                    }
                    string cliPath = ResolveToolPath("HisClinicalCli.exe");
                    result = RunProcess(cliPath, cmd, out isError, facility);
                    break;
                default:
                    isError = true;
                    return banner + "Loi: Action khong hop le: " + action;
            }

            return banner + result;
        }

        private static string ExecuteGlucoseProtocol(JObject args, out bool isError, string facility)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            double glucose = args["glucoseValue"] != null ? (double)args["glucoseValue"] : 0;
            string insulinType = args["insulinType"] != null ? args["insulinType"].ToString().Trim().ToUpper() : "R";
            int units = args["insulinUnits"] != null ? (int)args["insulinUnits"] : (args["units"] != null ? (int)args["units"] : 0);
            string slot = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim() : "17h";
            bool dryRun = args["dryRun"] != null && (bool)args["dryRun"];

            string tool = ResolveToolPath("HisGlucoseMcpServer.exe");
            string cmdArgs = string.Format("{0} {1} {2} {3} {4} {5}{6}",
                EscapeArg(pCode), glucose, EscapeArg(insulinType), units, EscapeArg(slot), facility, (dryRun ? " --dry-run" : ""));
            return RunProcess(tool, cmdArgs, out isError, facility);
        }

        private static string ExecuteDischargeProtocol(JObject args, out bool isError, string facility)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            bool dryRun = args["dryRun"] != null && (bool)args["dryRun"];

            string tool = ResolveToolPath("HisDischargeMcpServer.exe");
            string cmdArgs = string.Format("{0} {1}{2}", EscapeArg(pCode), facility, (dryRun ? " --dry-run" : ""));
            return RunProcess(tool, cmdArgs, out isError, facility);
        }

        private static string ExecutePatientLookup(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "lookup " + EscapeArg(pCode), out isError, facility);
        }

        private static string ExecuteGetOrders(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "orders " + EscapeArg(pCode), out isError, facility);
        }

        private static string ExecuteCancelOrder(JObject args, out bool isError, string facility = "HN")
        {
            string orderId = args["orderId"] != null ? args["orderId"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(orderId))
            {
                isError = true;
                return "Loi: Tham so orderId khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "cancel-order " + EscapeArg(orderId), out isError, facility);
        }

        private static string ExecuteCancelService(JObject args, out bool isError, string facility = "HN")
        {
            string sId = args["serviceReqMatId"] != null ? args["serviceReqMatId"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(sId))
            {
                isError = true;
                return "Loi: Tham so serviceReqMatId khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "cancel-service " + EscapeArg(sId), out isError, facility);
        }

        private static string ExecuteDebateView(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "debate " + EscapeArg(pCode), out isError, facility);
        }

        private static string ExecuteCreateTracking(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisTrackingCreator.exe");
            var sb = new StringBuilder();
            sb.Append(EscapeArg(pCode));

            if (args["progressNote"] != null && !string.IsNullOrEmpty(args["progressNote"].ToString()))
            {
                sb.Append(" --note " + EscapeArg(args["progressNote"].ToString()));
            }
            if (args["pulse"] != null)
            {
                sb.Append(" --pulse " + args["pulse"].ToString());
            }
            if (args["bloodPressure"] != null)
            {
                sb.Append(" --bp " + EscapeArg(args["bloodPressure"].ToString()));
            }
            if (args["temperature"] != null)
            {
                sb.Append(" --temp " + args["temperature"].ToString());
            }
            if (args["spO2"] != null)
            {
                sb.Append(" --spo2 " + args["spO2"].ToString());
            }
            if (args["respiratoryRate"] != null)
            {
                sb.Append(" --rr " + args["respiratoryRate"].ToString());
            }
            if (args["instructionTime"] != null && !string.IsNullOrEmpty(args["instructionTime"].ToString()))
            {
                sb.Append(" --time " + EscapeArg(args["instructionTime"].ToString()));
            }

            return RunProcess(tool, sb.ToString(), out isError, facility);
        }

        private static string ExecutePrescribeCabinet(JObject args, out bool isError, string facility = "HN")
        {
            string mode = args["mode"] != null ? args["mode"].ToString().Trim().ToLower() : "single";
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            long defaultStock = (facility == "NB") ? 5142 : 810;
            long stockId = args["stockId"] != null ? (long)args["stockId"] : defaultStock;
            string tool = ResolveToolPath("HisCabinetPrescribe.exe");

            if (mode == "stock")
            {
                string kw = args["keyword"] != null ? args["keyword"].ToString().Trim() : "";
                string cmdArgs = "stock " + stockId + (string.IsNullOrEmpty(kw) ? "" : " " + EscapeArg(kw));
                return RunProcess(tool, cmdArgs, out isError, facility);
            }

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong cho che do " + mode;
            }

            if (mode == "insulin")
            {
                decimal units = args["insulinUnits"] != null ? (decimal)args["insulinUnits"] : 8m;
                string typeStr = args["insulinType"] != null ? args["insulinType"].ToString().Trim().ToUpper() : "R";
                string timeStr = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim() : "17:00";
                string cmdArgs = string.Format("insulin {0} {1} {2} {3} {4}",
                    EscapeArg(pCode), units, EscapeArg(typeStr), EscapeArg(timeStr), stockId);
                return RunProcess(tool, cmdArgs, out isError, facility);
            }

            if (mode == "leanpro")
            {
                decimal qty = args["amount"] != null ? (decimal)args["amount"] : 6m;
                string cmdArgs = string.Format("leanpro {0} {1}", EscapeArg(pCode), qty);
                return RunProcess(tool, cmdArgs, out isError, facility);
            }

            if (mode == "dressing")
            {
                string cmdArgs = string.Format("dressing {0} 1 1 {1}", EscapeArg(pCode), stockId);
                return RunProcess(tool, cmdArgs, out isError, facility);
            }

            if (mode == "multi")
            {
                var itemsArray = args["items"] as JArray;
                if (itemsArray == null || itemsArray.Count == 0)
                {
                    isError = true;
                    return "Loi: items (danh sach thuoc) khong duoc de trong khi mode='multi'.";
                }
                var sb = new StringBuilder();
                sb.Append("multi ").Append(EscapeArg(pCode));
                foreach (var it in itemsArray)
                {
                    sb.Append(" ").Append(EscapeArg(it.ToString()));
                }
                if (stockId != defaultStock) sb.Append(" --stock ").Append(stockId);
                if (args["timeSlot"] != null && !string.IsNullOrEmpty(args["timeSlot"].ToString()))
                {
                    sb.Append(" --time ").Append(EscapeArg(args["timeSlot"].ToString()));
                }
                return RunProcess(tool, sb.ToString(), out isError, facility);
            }

            // Default: mode == "single"
            string medName = args["medicineName"] != null ? args["medicineName"].ToString().Trim() : "";
            decimal amt = args["amount"] != null ? (decimal)args["amount"] : 1m;
            string tutorial = args["tutorial"] != null ? args["tutorial"].ToString().Trim() : "Dung theo chi dan cua bac si";
            string time = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(medName))
            {
                isError = true;
                return "Loi: medicineName khong duoc de trong khi mode='single'.";
            }

            string singleArgs = string.Format("single {0} {1} {2} {3} {4}",
                EscapeArg(pCode), EscapeArg(medName), amt, stockId, EscapeArg(tutorial));
            if (!string.IsNullOrEmpty(time)) singleArgs += " " + EscapeArg(time);

            return RunProcess(tool, singleArgs, out isError, facility);
        }

        private static string ExecutePrescribeWarehouse(JObject args, out bool isError, string facility = "HN")
        {
            string mode = args["mode"] != null ? args["mode"].ToString().Trim().ToLower() : "single";
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            long defaultStock = (facility == "NB") ? 4854 : 4210;
            long stockId = args["stockId"] != null ? (long)args["stockId"] : defaultStock;
            string tool = ResolveToolPath("HisWarehousePrescribe.exe");

            if (mode == "search")
            {
                string kw = args["medicineName"] != null ? args["medicineName"].ToString().Trim() : "";
                if (string.IsNullOrEmpty(kw))
                {
                    isError = true;
                    return "Loi: medicineName (tu khoa tra cuu) khong duoc de trong khi mode='search'.";
                }
                return RunProcess(tool, "search " + EscapeArg(kw), out isError, facility);
            }

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong cho che do " + mode;
            }

            if (mode == "nutrition")
            {
                string spName = args["medicineName"] != null ? args["medicineName"].ToString().Trim() : "Leanpro PreSur";
                decimal qty = args["amount"] != null ? (decimal)args["amount"] : 6m;
                string tut = args["tutorial"] != null ? args["tutorial"].ToString().Trim() : "Uong theo chi dinh chuyen khoa";
                string cmdArgs = string.Format("nutrition {0} {1} {2} {3} {4}",
                    EscapeArg(pCode), EscapeArg(spName), qty, (stockId > 0 ? stockId : 753), EscapeArg(tut));
                return RunProcess(tool, cmdArgs, out isError, facility);
            }

            if (mode == "multi")
            {
                var itemsArray = args["items"] as JArray;
                if (itemsArray == null || itemsArray.Count == 0)
                {
                    isError = true;
                    return "Loi: items (danh sach thuoc) khong duoc de trong khi mode='multi'.";
                }
                var sb = new StringBuilder();
                sb.Append("multi ").Append(EscapeArg(pCode));
                foreach (var it in itemsArray)
                {
                    sb.Append(" ").Append(EscapeArg(it.ToString()));
                }
                if (stockId != defaultStock) sb.Append(" --stock ").Append(stockId);
                if (args["timeSlot"] != null && !string.IsNullOrEmpty(args["timeSlot"].ToString()))
                {
                    sb.Append(" --time ").Append(EscapeArg(args["timeSlot"].ToString()));
                }
                return RunProcess(tool, sb.ToString(), out isError, facility);
            }

            // Default: mode == "single"
            string medName = args["medicineName"] != null ? args["medicineName"].ToString().Trim() : "";
            decimal amt = args["amount"] != null ? (decimal)args["amount"] : 1m;
            string tutorial = args["tutorial"] != null ? args["tutorial"].ToString().Trim() : "Dung theo chi dan cua bac si";
            long? useFormId = args["useFormId"] != null ? (long?)args["useFormId"] : null;
            string doses = args["doses"] != null ? args["doses"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(medName))
            {
                isError = true;
                return "Loi: medicineName khong duoc de trong khi mode='single'.";
            }

            string singleArgs = string.Format("single {0} {1} {2} {3} {4}",
                EscapeArg(pCode), EscapeArg(medName), amt, stockId, EscapeArg(tutorial));
            if (useFormId.HasValue) singleArgs += " " + useFormId.Value;
            if (!string.IsNullOrEmpty(doses)) singleArgs += " " + EscapeArg(doses);

            return RunProcess(tool, singleArgs, out isError, facility);
        }

        private static string ExecutePrescribe(JObject args, out bool isError, string facility = "HN")
        {
            long stockId = args["stockId"] != null ? (long)args["stockId"] : ((facility == "NB") ? 5142 : 810);
            // Neu stockId la tu truc (810, 7787, 5142, 5141) -> dieu phoi sang ExecutePrescribeCabinet
            if (stockId == 810 || stockId == 7787 || stockId == 5142 || stockId == 5141)
            {
                return ExecutePrescribeCabinet(args, out isError, facility);
            }
            return ExecutePrescribeWarehouse(args, out isError, facility);
        }

        private static string ExecuteAssignRation(JObject args, out bool isError, string facility = "HN")
        {
            string room = args["room"] != null ? args["room"].ToString().Trim() : "";
            string rationType = args["rationType"] != null ? args["rationType"].ToString().Trim() : "BT01";
            string fac = args["facility"] != null ? args["facility"].ToString().Trim() : facility;

            if (string.IsNullOrEmpty(room))
            {
                isError = true;
                return "Loi: room khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisRationAssigner.exe");
            string cmdArgs = string.Format("{0} {1} {2}", EscapeArg(room), EscapeArg(rationType), EscapeArg(fac));
            return RunProcess(tool, cmdArgs, out isError, fac);
        }

        private static string ExecuteAssignLeanpro(JObject args, out bool isError, string facility = "HN")
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            string fac = args["facility"] != null ? args["facility"].ToString().Trim() : facility;

            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Loi: patientCodes khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisLeanproAssigner.exe");
            string cmdArgs = EscapeArg(pCodes) + " " + EscapeArg(fac);
            return RunProcess(tool, cmdArgs, out isError, fac);
        }

        private static string ExecuteCreatePt01(JObject args, out bool isError, string facility = "HN")
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Loi: patientCodes khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisPt01Creator.exe");
            return RunProcess(tool, EscapeArg(pCodes), out isError, facility);
        }

        private static string ExecuteViewPacs(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            bool openBrowser = args["openBrowser"] != null && (bool)args["openBrowser"];

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            string cmdArgs = "pacs " + EscapeArg(pCode) + (openBrowser ? " -Open" : "");
            return RunProcess(cliPath, cmdArgs, out isError, facility);
        }

        private static string ExecuteDebateCreate(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            string spec = args["specialtyCode"] != null ? args["specialtyCode"].ToString().Trim() : "";
            string content = args["requestContent"] != null ? args["requestContent"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(pCode) || string.IsNullOrEmpty(spec))
            {
                isError = true;
                return "Loi: patientCode va specialtyCode khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisDebateCreator.exe");
            string cmdArgs = string.Format("{0} {1} {2}", EscapeArg(pCode), EscapeArg(spec), EscapeArg(content));
            return RunProcess(tool, cmdArgs, out isError, facility);
        }

        private static string ExecuteEmrFill(JObject args, out bool isError, string facility = "HN")
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            bool save = args["save"] != null && (bool)args["save"];
            bool force = args["force"] != null && (bool)args["force"];
            string summary = args["summary"] != null ? args["summary"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisEmrFiller.exe");
            var sb = new StringBuilder();
            sb.Append(EscapeArg(pCode));
            if (save) sb.Append(" --save");
            if (force) sb.Append(" --force");
            if (!string.IsNullOrEmpty(summary)) sb.Append(" --summary ").Append(EscapeArg(summary));
            return RunProcess(tool, sb.ToString(), out isError, facility);
        }

        private static string ExecuteSystemHealth(out bool isError, string facility = "HN")
        {
            string tool = ResolveToolPath("HisDiagnosticDoctor.exe");
            return RunProcess(tool, "health " + facility, out isError, facility);
        }

        #endregion

        #region Process Helpers & Path Resolution

        private static string ResolveToolPath(string exeName)
        {
            // Tier 1: Check in BaseDir (project root)
            string path1 = Path.Combine(BaseDir, exeName);
            if (File.Exists(path1)) return path1;

            // Tier 2: Check in .agents/skills/his-clinical-operations/scripts/
            string path2 = Path.Combine(BaseDir, Path.Combine(@".agents\skills\his-clinical-operations\scripts", exeName));
            if (File.Exists(path2)) return path2;

            // Tier 3: Check in Tools directory
            string path3 = Path.Combine(BaseDir, Path.Combine("Tools", exeName));
            if (File.Exists(path3)) return path3;

            // Return exeName directly (fallback to PATH)
            return exeName;
        }

        private static string RunProcess(string exePath, string arguments, out bool isError, string facility = "HN")
        {
            isError = false;
            try
            {
                string tokenFile = (facility == "NB") ? Path.Combine(BaseDir, "doctor_nb.token") : Path.Combine(BaseDir, "doctor_hn.token");
                string doctorLogin = (facility == "NB") ? "vmc" : "034727";

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = BaseDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = new UTF8Encoding(false),
                    StandardErrorEncoding = new UTF8Encoding(false)
                };

                psi.EnvironmentVariables["HIS_FACILITY"] = facility;
                psi.EnvironmentVariables["HIS_TOKEN_FILE"] = tokenFile;
                psi.EnvironmentVariables["HIS_DOCTOR_LOGIN"] = doctorLogin;

                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        isError = true;
                        return "Khong the khoi dong tien trinh: " + exePath;
                    }

                    string stdOut = proc.StandardOutput.ReadToEnd();
                    string stdErr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        isError = true;
                    }

                    var sb = new StringBuilder();
                    if (!string.IsNullOrEmpty(stdOut)) sb.Append(stdOut);
                    if (!string.IsNullOrEmpty(stdErr))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append("[STDERR]: ").Append(stdErr);
                    }

                    return sb.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                isError = true;
                return "Loi khi chay " + exePath + ": " + ex.Message;
            }
        }

        private static string EscapeArg(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            if (arg.Contains(" ") || arg.Contains("\"") || arg.Contains("\t") || arg.Contains("\n"))
            {
                return "\"" + arg.Replace("\"", "\\\"") + "\"";
            }
            return arg;
        }

        #endregion

        #region JSON-RPC Serialization Helpers

        public static JObject Obj(params object[] kvPairs)
        {
            var obj = new JObject();
            for (int i = 0; i < kvPairs.Length; i += 2)
            {
                string k = (string)kvPairs[i];
                object v = kvPairs[i + 1];
                if (v is JToken)
                    obj[k] = (JToken)v;
                else if (v == null)
                    obj[k] = null;
                else
                    obj[k] = JToken.FromObject(v);
            }
            return obj;
        }

        public static JArray Arr(params object[] items)
        {
            var arr = new JArray();
            foreach (var it in items)
            {
                if (it is JToken)
                    arr.Add((JToken)it);
                else if (it != null)
                    arr.Add(JToken.FromObject(it));
            }
            return arr;
        }

        private static void SendResponse(JToken id, JObject result)
        {
            var res = Obj(
                "jsonrpc", "2.0",
                "id", id,
                "result", result
            );
            Console.WriteLine(res.ToString(Formatting.None));
            Console.Out.Flush();
        }

        private static void SendError(JToken id, int code, string message)
        {
            var res = Obj(
                "jsonrpc", "2.0",
                "id", id,
                "error", Obj(
                    "code", code,
                    "message", message
                )
            );
            Console.WriteLine(res.ToString(Formatting.None));
            Console.Out.Flush();
        }

        #endregion
    }
}
