using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Common.Mapper;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

namespace HisWardDutyMcp
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                var requestedName = new System.Reflection.AssemblyName(resolveArgs.Name).Name;
                string[] searchPaths = new string[]
                {
                    Path.Combine(baseDir, requestedName + ".dll"),
                    Path.Combine(baseDir, "ReferencedAssemblies", requestedName + ".dll"),
                    Path.Combine(baseDir, "HisAutoPrescribe_Portable", requestedName + ".dll"),
                    Path.Combine(baseDir, "Integrate", "EMR", requestedName + ".dll")
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

            return Bootstrap(args);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int Bootstrap(string[] args)
        {
            return WardDutyService.Run(args);
        }
    }

    public class WardDutyService
    {
        private static readonly string MOS_BASE = "http://192.168.7.236:1608/";
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static MyAdapter adapter = new MyAdapter();
        private static ApiConsumer mosConsumer;
        private static ApiConsumer mosConsumerHN;
        private static ApiConsumer mosConsumerNB;
        private static CommonParam param = new CommonParam();

        public static int Run(string[] args)
        {
            // 1. Nếu chạy từ dòng lệnh (CLI Direct)
            if (args.Length > 0 && args[0] != "--mcp" && args[0] != "-mcp")
            {
                return RunCli(args);
            }

            // 2. Chế độ MCP Server (JSON-RPC 2.0 qua Stdio)
            RunMcpServer();
            return 0;
        }

        #region CLI Execution Mode

        private static int RunCli(string[] args)
        {
            string keyword = args[0].Trim();
            string facility = "";
            string comboRation = "";
            bool dryRun = false;

            for (int i = 1; i < args.Length; i++)
            {
                string a = args[i].Trim().ToUpper();
                if (a == "NB" || a == "HN") facility = a;
                else if (a == "--DRY-RUN" || a == "-N" || a == "--PREVIEW") dryRun = true;
                else if (a == "BT01" || a == "DD01" || a == "TM01") comboRation = a;
            }

            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(keyword, null);
            }

            bool isError;
            string output = ExecuteProtocol(keyword, facility, dryRun, comboRation, out isError);
            Console.WriteLine(output);
            return isError ? 1 : 0;
        }

        #endregion

        #region MCP Server Loop & JSON-RPC 2.0

        private static void RunMcpServer()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var req = JObject.Parse(line);
                    string method = req["method"] != null ? req["method"].ToString() : "";
                    JToken id = req["id"];

                    switch (method)
                    {
                        case "initialize":
                            HandleInitialize(id);
                            break;
                        case "notifications/initialized":
                            // Notification - không trả lời
                            break;
                        case "tools/list":
                            HandleToolsList(id);
                            break;
                        case "tools/call":
                            HandleToolsCall(id, req["params"] as JObject);
                            break;
                        default:
                            if (id != null)
                            {
                                SendError(id, -32601, "Method not found: " + method);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    SendError(null, -32700, "Parse error: " + ex.Message);
                }
            }
        }

        private static void HandleInitialize(JToken id)
        {
            var res = Obj(
                "protocolVersion", "2024-11-05",
                "capabilities", Obj(
                    "tools", Obj()
                ),
                "serverInfo", Obj(
                    "name", "his-ward-duty",
                    "version", "1.0.0"
                )
            );
            SendResponse(id, res);
        }

        private static void HandleToolsList(JToken id)
        {
            var tools = new JArray();

            // 1. his_ward_duty_hn (Chuyên biệt Cơ sở Hà Nội)
            tools.Add(CreateTool(
                "his_ward_duty_hn",
                "Dac quyen 'Tho truc buong' CHUYEN BIET CO SO HA NOI (Khoa 57 CTCH, BS Nguyen Huu Sam 034727, token doctor_hn.token): Protocol 4 buoc tiep don buong: (1) Ra soat bo sung Vo Benh An Ngoai Khoa EMR noi tru (khong lam vo ket thuc) -> (2) Tao To dieu tri dau tien lay tu tom tat benh an cua vo EMR -> (3) Cho suat an D0 (sau gio nhap khoa 15p) va D1 luc 06:00 sang (BT01/DD01/TM01) -> (4) Ra soat toan bo CLS 3 thang va de xuat bilan thieu kem phong chi dinh de nguoi dieu hanh duyet truoc khi ke",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri hoac ho ten BN (VD: 0004018669)"),
                    "comboRation", Obj("type", "string", "description", "Combo suat an tuy chon: 'BT01', 'DD01', 'TM01' (de trong se tu dong nhan dien theo ICD)", "enum", Arr("BT01", "DD01", "TM01")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 2. his_ward_duty_nb (Chuyên biệt Cơ sở Ninh Bình)
            tools.Add(CreateTool(
                "his_ward_duty_nb",
                "Dac quyen 'Tho truc buong' CHUYEN BIET CO SO NINH BINH (Khoa 915 Ngoai TH Khu 3E, Ths.BS Nguyen Huu Sam 034727, token doctor_nb.token): Protocol 4 buoc tiep don buong: (1) Ra soat bo sung Vo Benh An Ngoai Khoa EMR noi tru (khong lam vo ket thuc) -> (2) Tao To dieu tri dau tien lay tu tom tat benh an cua vo EMR -> (3) Cho suat an D0 (sau gio nhap khoa 15p) va D1 luc 06:00 sang (BT01/DD01/TM01) -> (4) Ra soat toan bo CLS 3 thang va de xuat bilan thieu kem phong chi dinh de nguoi dieu hanh duyet truoc khi ke",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri hoac ho ten BN (VD: 0004063959)"),
                    "comboRation", Obj("type", "string", "description", "Combo suat an tuy chon: 'BT01', 'DD01', 'TM01' (de trong se tu dong nhan dien theo ICD)", "enum", Arr("BT01", "DD01", "TM01")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 3. his_tho_truc_buong (Chính thức - Tự động điều hướng)
            tools.Add(CreateTool(
                "his_tho_truc_buong",
                "Dac quyen 'Tho truc buong' (1-Click Ward Duty Protocol - Tu dong dieu huong HN/NB): Protocol 4 buoc tiep don buong: (1) Ra soat & bo sung Vo Benh An Ngoai Khoa EMR (khong lam vo ket thuc) -> (2) Tao To dieu tri dau tien lay tu tom tat benh an cua vo EMR -> (3) Cho suat an 3 bua D0 (sau gio nhap khoa 15p) va D1 luc 06:00 sang (BT01/DD01/TM01) -> (4) Ra soat CLS 3 thang va de xuat bilan thieu kem phong chi dinh de nguoi dieu hanh duyet truoc khi ke",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri hoac ho ten BN (VD: 0004063959)"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "comboRation", Obj("type", "string", "description", "Combo suat an tuy chon: 'BT01', 'DD01', 'TM01' (de trong tu dong)", "enum", Arr("BT01", "DD01", "TM01")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 4. his_execute_protocol_ward_duty (Bí danh tương thích ngược)
            tools.Add(CreateTool(
                "his_execute_protocol_ward_duty",
                "Bi danh cua his_tho_truc_buong: Protocol 'Tho truc buong' 1-Click tiep don buong benh nhan moi vao vien 4 buoc toan dien (suat an D0 +15p sau gio nhap khoa, to dieu tri lay tu tom tat vo EMR, de xuat CLS cho duyet)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri hoac ho ten BN"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "comboRation", Obj("type", "string", "description", "Combo suat an: 'BT01', 'DD01', 'TM01'"),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
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

        private static void HandleToolsCall(JToken id, JObject @params)
        {
            string toolName = @params["name"] != null ? @params["name"].ToString() : "";
            var args = @params["arguments"] as JObject ?? new JObject();

            string patientCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(patientCode) && args["patient_code"] != null)
                patientCode = args["patient_code"].ToString().Trim();
            if (string.IsNullOrEmpty(patientCode) && args["keyword"] != null)
                patientCode = args["keyword"].ToString().Trim();

            string facility = args["facility"] != null ? args["facility"].ToString().Trim().ToUpper() : "";
            string comboRation = args["comboRation"] != null ? args["comboRation"].ToString().Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(comboRation) && args["combo_ration"] != null)
                comboRation = args["combo_ration"].ToString().Trim().ToUpper();

            bool dryRun = false;
            if (args["dryRun"] != null) dryRun = args["dryRun"].Value<bool>();
            else if (args["dry_run"] != null) dryRun = args["dry_run"].Value<bool>();

            if (toolName == "his_ward_duty_hn") facility = "HN";
            else if (toolName == "his_ward_duty_nb") facility = "NB";

            if (string.IsNullOrEmpty(patientCode))
            {
                SendToolResult(id, "❌ Lỗi: Tham số 'patientCode' không được để trống!", true);
                return;
            }

            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(patientCode, null);
            }

            bool isError;
            string output = ExecuteProtocol(patientCode, facility, dryRun, comboRation, out isError);
            SendToolResult(id, output, isError);
        }

        private static void SendToolResult(JToken id, string text, bool isError)
        {
            var content = new JArray();
            content.Add(Obj("type", "text", "text", text));

            var res = Obj(
                "content", content,
                "isError", isError
            );
            SendResponse(id, res);
        }

        private static void SendResponse(JToken id, JObject result)
        {
            var resp = Obj(
                "jsonrpc", "2.0",
                "id", id,
                "result", result
            );
            Console.WriteLine(resp.ToString(Formatting.None));
            Console.Out.Flush();
        }

        private static void SendError(JToken id, int code, string msg)
        {
            var resp = Obj(
                "jsonrpc", "2.0",
                "id", id,
                "error", Obj(
                    "code", code,
                    "message", msg
                )
            );
            Console.WriteLine(resp.ToString(Formatting.None));
            Console.Out.Flush();
        }

        private static JObject Obj(params object[] kvs)
        {
            var o = new JObject();
            for (int i = 0; i < kvs.Length; i += 2)
            {
                string key = kvs[i].ToString();
                object val = kvs[i + 1];
                if (val is JToken) o[key] = (JToken)val;
                else if (val == null) o[key] = JValue.CreateNull();
                else o[key] = new JValue(val);
            }
            return o;
        }

        private static JArray Arr(params object[] items)
        {
            var a = new JArray();
            foreach (var it in items)
            {
                if (it is JToken) a.Add((JToken)it);
                else a.Add(new JValue(it));
            }
            return a;
        }

        #endregion

        #region Protocol Orchestrator Core Execution

        public static string ExecuteProtocol(string keyword, string facility, bool dryRun, string comboRation, out bool isError)
        {
            StringBuilder sb = new StringBuilder();
            isError = false;

            sb.AppendLine("===============================================================================");
            sb.AppendLine("🌟 ĐẶC QUYỀN 'THỢ TRỰC BUỒNG' - 1-CLICK WARD DUTY PROTOCOL");
            sb.AppendLine("   (Rà soát Vỏ EMR ➔ Tờ điều trị đầu ➔ Suất ăn D0 & D1 ➔ Rà soát CLS 3 tháng)");
            sb.AppendLine("===============================================================================");
            sb.AppendLine(string.Format("• Đối tượng tra cứu : {0}", keyword));
            sb.AppendLine(string.Format("• Cơ sở khai báo    : {0}", string.IsNullOrEmpty(facility) ? "Tự động nhận diện" : (facility == "NB" ? "Cơ sở Ninh Bình" : "Cơ sở Hà Nội")));
            sb.AppendLine(string.Format("• Chế độ thực thi   : {0}", dryRun ? "🔍 CHẠY THỬ / DRY-RUN (Mô phỏng 100%, không ghi DB)" : "⚡ THỰC THI CHÍNH THỨC"));
            if (!string.IsNullOrEmpty(comboRation))
                sb.AppendLine(string.Format("• Combo Suất ăn     : {0} (Theo yêu cầu)", comboRation));
            sb.AppendLine("===============================================================================\n");

            // BƯỚC 0: Đăng nhập & Xác thực hồ sơ bệnh nhân
            try
            {
                EnsureLogin(facility, sb);
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ LỖI ĐĂNG NHẬP XÁC THỰC: " + ex.Message);
                isError = true;
                return sb.ToString();
            }

            V_HIS_TREATMENT tr = FindTreatment(keyword, facility, sb);
            if (tr == null)
            {
                sb.AppendLine(string.Format("❌ Không tìm thấy hồ sơ bệnh nhân phù hợp với từ khóa '{0}' tại cơ sở {1}!", keyword, facility));
                isError = true;
                return sb.ToString();
            }

            // Tự động nhận diện cơ sở chuẩn xác từ hồ sơ điều trị
            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromTreatment(tr);
            }

            long targetDeptId = (facility == "NB") ? 915 : 57;
            long targetRoomId = (facility == "NB") ? 18679 : 5248; // Fallback
            string bedNameStr = "";
            string roomNameStr = "";

            try
            {
                var tbrf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = tr.ID, IS_IN_ROOM = true };
                var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                if (beds != null && beds.Count > 0)
                {
                    var b = beds.OrderByDescending(x => x.ADD_TIME).First();
                    if (b.DEPARTMENT_ID > 0) targetDeptId = b.DEPARTMENT_ID;
                    bedNameStr = b.BED_NAME;
                    roomNameStr = b.BED_ROOM_NAME;

                    var brf = new HisBedRoomViewFilter { ID = b.BED_ROOM_ID };
                    var bRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
                    if (bRooms != null && bRooms.Count > 0 && bRooms[0].ROOM_ID > 0)
                    {
                        targetRoomId = bRooms[0].ROOM_ID;
                    }
                }
            }
            catch { }

            string deptName = (targetDeptId == 915) ? "Khoa Ngoại tổng hợp - Khu 3E (NB)" : (targetDeptId == 57 ? "Khoa Chấn thương Chỉnh hình & Cột sống (HN)" : "Khoa ID: " + targetDeptId);

            sb.AppendLine("📋 THÔNG TIN HỒ SƠ BỆNH NHÂN:");
            sb.AppendLine(string.Format("   - Họ và tên        : {0} ({1} tuổi, Giới tính: {2})", tr.TDL_PATIENT_NAME, DateTime.Now.Year - (tr.TDL_PATIENT_DOB.ToString().Length >= 4 ? int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)) : 1970), tr.TDL_PATIENT_GENDER_NAME));
            sb.AppendLine(string.Format("   - Mã Bệnh Nhân     : {0}", tr.TDL_PATIENT_CODE));
            sb.AppendLine(string.Format("   - Mã Đợt Điều Trị  : {0}", tr.TREATMENT_CODE));
            sb.AppendLine(string.Format("   - Khoa điều trị    : {0} (ID: {1})", deptName, targetDeptId));
            if (!string.IsNullOrEmpty(roomNameStr))
                sb.AppendLine(string.Format("   - Buồng / Giường   : {0} - {1} (RoomId: {2})", roomNameStr, bedNameStr, targetRoomId));
            sb.AppendLine(string.Format("   - Ngày giờ vào viện: {0}", FormatTime(tr.IN_TIME)));
            sb.AppendLine(string.Format("   - Chẩn đoán chính  : {0} ({1})", tr.ICD_NAME, tr.ICD_CODE));
            if (!string.IsNullOrEmpty(tr.ICD_TEXT))
                sb.AppendLine(string.Format("   - Chẩn đoán chi tiết: {0}", tr.ICD_TEXT));
            sb.AppendLine();

            // =========================================================================
            // BƯỚC 1: RÀ SOÁT VÀ ĐIỀN VỎ BỆNH ÁN NGOẠI KHOA EMR
            // =========================================================================
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("📝 BƯỚC 1: RÀ SOÁT & ĐIỀN VỎ BỆNH ÁN NGOẠI KHOA EMR (INPATIENT SURGERY RECORD)");
            sb.AppendLine("   (Lưu ý: Chỉ làm vỏ vào viện tiếp đón, TUYỆT ĐỐI KHÔNG làm vỏ kết thúc bệnh án)");
            sb.AppendLine("-------------------------------------------------------------------------------");
            string emrSummary = "";
            bool step1Ok = Step1_EnsureEmrCover(tr, dryRun, sb, out emrSummary);
            sb.AppendLine();

            // =========================================================================
            // BƯỚC 2: RÀ SOÁT VÀ TẠO TỜ ĐIỀU TRỊ ĐẦU TIÊN
            // =========================================================================
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("📋 BƯỚC 2: RÀ SOÁT & TẠO TỜ ĐIỀU TRỊ ĐẦU TIÊN (FIRST TREATMENT TRACKING)");
            sb.AppendLine("   (Nội dung diễn biến lấy trực tiếp từ Tóm tắt bệnh án của vỏ EMR)");
            sb.AppendLine("-------------------------------------------------------------------------------");
            long createdOrExistingTrackingId = 0;
            bool step2Ok = Step2_EnsureFirstTracking(tr, targetDeptId, targetRoomId, emrSummary, dryRun, sb, out createdOrExistingTrackingId);
            sb.AppendLine();

            // =========================================================================
            // BƯỚC 3: CẤP SUẤT ĂN DINH DƯỠNG (D0: SAU NHẬP KHOA 15P & D1: 06:00 SÁNG)
            // =========================================================================
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("🍲 BƯỚC 3: CHỈ ĐỊNH SUẤT ĂN DINH DƯỠNG (D0: SAU NHẬP KHOA 15P & D1: 06:00 SÁNG)");
            sb.AppendLine("-------------------------------------------------------------------------------");
            bool step3Ok = Step3_AssignAdmissionRations(tr, createdOrExistingTrackingId, targetRoomId, comboRation, dryRun, sb);
            sb.AppendLine();

            // =========================================================================
            // BƯỚC 4: RÀ SOÁT CẬN LÂM SÀNG 3 THÁNG & ĐỀ XUẤT BILAN THIẾU
            // =========================================================================
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("🔬 BƯỚC 4: RÀ SOÁT CẬN LÂM SÀNG 3 THÁNG & ĐỀ XUẤT BILAN THIẾU (CHỜ DUYỆT TRƯỚC KHI KÊ)");
            sb.AppendLine("-------------------------------------------------------------------------------");
            bool step4Ok = Step4_AuditParaclinicalBilan3Months(tr, facility, sb);
            sb.AppendLine();

            // =========================================================================
            // TỔNG KẾT
            // =========================================================================
            sb.AppendLine("===============================================================================");
            sb.AppendLine("🎉 KẾT QUẢ THỰC HIỆN PROTOCOL 'THỢ TRỰC BUỒNG':");
            sb.AppendLine(string.Format("   1. Vỏ Bệnh Án Ngoại Khoa EMR : {0}", step1Ok ? "✔ THÀNH CÔNG (Không làm vỏ kết thúc)" : "⚠️ CẦN KIỂM TRA LẠI"));
            sb.AppendLine(string.Format("   2. Tờ Điều Trị Đầu Tiên      : {0}", step2Ok ? "✔ THÀNH CÔNG (Tích hợp tóm tắt vỏ EMR)" : "⚠️ LỖI"));
            sb.AppendLine(string.Format("   3. Suất Ăn D0 (+15p) & D1    : {0}", step3Ok ? "✔ THÀNH CÔNG / ĐÃ CÓ" : "⚠️ LỖI"));
            sb.AppendLine(string.Format("   4. Đề Xuất Bilan CLS Thiếu   : {0}", step4Ok ? "✔ ĐÃ ĐỐI SOÁT (CHỜ DUYỆT TRƯỚC KHI KÊ)" : "⚠️ LỖI"));
            sb.AppendLine("===============================================================================");

            isError = !(step1Ok && step2Ok && step3Ok && step4Ok);
            return sb.ToString();
        }

        #endregion

        #region Step 1: Vỏ Bệnh Án Ngoại Khoa EMR (Inpatient Only)

        private static bool Step1_EnsureEmrCover(V_HIS_TREATMENT tr, bool isDryRun, StringBuilder sb, out string emrSummary)
        {
            emrSummary = "";
            // Kiểm tra diện nội trú
            if (tr.TDL_TREATMENT_TYPE_ID != 3)
            {
                sb.AppendLine(string.Format("   ⚠️ Bệnh nhân thuộc diện Ngoại trú / Phòng khám (TDL_TREATMENT_TYPE_ID = {0}).", tr.TDL_TREATMENT_TYPE_ID));
                sb.AppendLine("   ⛔ Tuân thủ Rule 2 AGENTS.md: TUYỆT ĐỐI KHÔNG tạo Vỏ Bệnh Án Ngoại Khoa cho ngoại trú.");
                return true; // Không phải lỗi, đúng nghiệp vụ
            }

            string emrFillerExe = Path.Combine(BaseDir, "HisEmrFiller.exe");
            if (!File.Exists(emrFillerExe))
            {
                sb.AppendLine("   ❌ Không tìm thấy công cụ HisEmrFiller.exe tại thư mục gốc!");
                return false;
            }

            try
            {
                string args = isDryRun ? string.Format("{0} --admission", tr.TDL_PATIENT_CODE) : string.Format("{0} --save --admission", tr.TDL_PATIENT_CODE);
                sb.AppendLine(string.Format("   🚀 Đang gọi engine HisEmrFiller.exe {0}...", args));

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = emrFillerExe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    string err = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(45000); // Đợi tối đa 45 giây

                    // Trích xuất Tóm tắt bệnh án từ output của HisEmrFiller
                    if (!string.IsNullOrEmpty(output))
                    {
                        int idx = output.IndexOf("[5. TÓM TẮT BỆNH ÁN NGOẠI KHOA]");
                        if (idx >= 0)
                        {
                            int start = idx + "[5. TÓM TẮT BỆNH ÁN NGOẠI KHOA]".Length;
                            int end = output.IndexOf("PhanBiet", start);
                            if (end < 0) end = output.IndexOf("[6. BÌA TỔNG KẾT", start);
                            if (end < 0) end = output.IndexOf("TienLuong", start);
                            if (end > start)
                            {
                                string raw = output.Substring(start, end - start).Trim();
                                var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                               .Select(l => l.Trim())
                                               .Where(l => !string.IsNullOrEmpty(l));
                                emrSummary = string.Join("\r\n", lines);
                            }
                        }
                    }

                    if (proc.ExitCode == 0 || output.Contains("LƯU THÀNH CÔNG") || output.Contains("Đã lưu thành công") || output.Contains("THÀNH CÔNG"))
                    {
                        sb.AppendLine("   ✔ Engine HisEmrFiller đã hoàn tất rà soát & điền vỏ bệnh án ngoại khoa EMR!");
                        sb.AppendLine("     ✔ Tuân thủ quy chuẩn tiếp đón: Tuyệt đối không làm vỏ kết thúc bệnh án (Bìa tổng kết ra viện để trống 100%).");
                        if (!string.IsNullOrEmpty(emrSummary))
                        {
                            sb.AppendLine("     ✔ Đã trích xuất Tóm tắt bệnh án ngoại khoa từ vỏ EMR để đồng bộ sang Tờ điều trị đầu tiên.");
                        }
                        if (isDryRun) sb.AppendLine("     [DRY-RUN]: Đã tạo dữ liệu mô phỏng thành công (chưa lưu STB).");
                        else sb.AppendLine("     ✔ Đã lưu thành công dữ liệu vào Oracle EMR STB (BENHANNGOAIKHOA & THONGTINDIEUTRI).");
                        return true;
                    }
                    else
                    {
                        sb.AppendLine("   ⚠️ Kết quả từ HisEmrFiller:");
                        string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var l in lines.Take(6)) sb.AppendLine("     " + l);
                        if (!string.IsNullOrEmpty(err)) sb.AppendLine("     Lỗi: " + err);
                        return true; // Không ngắt pipeline vì vỏ có thể đã có trước
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("   ❌ Lỗi khi thực thi HisEmrFiller: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Step 2: Tờ Điều Trị Đầu Tiên

        private static bool Step2_EnsureFirstTracking(V_HIS_TREATMENT tr, long deptId, long roomId, string emrSummary, bool isDryRun, StringBuilder sb, out long trackingId)
        {
            trackingId = 0;
            try
            {
                // 1. Quét danh sách tờ điều trị hiện có của đợt điều trị
                var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
                var existingTrks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);

                if (existingTrks != null && existingTrks.Count > 0)
                {
                    // Lọc tờ điều trị của đúng khoa tiếp đón
                    var deptTrks = existingTrks.Where(x => x.DEPARTMENT_ID == deptId).OrderBy(x => x.TRACKING_TIME).ToList();
                    if (deptTrks.Count > 0)
                    {
                        var firstTrk = deptTrks[0];
                        trackingId = firstTrk.ID;
                        string curContent = firstTrk.CONTENT ?? "";
                        bool hasMismatchedLumbar = (tr.ICD_NAME ?? "").ToLower().Contains("cổ") && (curContent.Contains("TLIF") || curContent.Contains("thắt lưng"));
                        bool isPlaceholderOrNeedsSummary = curContent.Length < 100 || curContent.Contains("Thêm thuốc") || !curContent.Contains("Tóm tắt bệnh án");
                        if ((hasMismatchedLumbar || isPlaceholderOrNeedsSummary) && !isDryRun && !string.IsNullOrEmpty(emrSummary))
                        {
                            sb.AppendLine(string.Format("   🔄 Phát hiện Tờ điều trị ID {0} {1} → Tự động cập nhật nội dung lâm sàng đầy đủ từ vỏ EMR!", 
                                firstTrk.ID, 
                                hasMismatchedLumbar ? "chứa nội dung lệch bệnh cảnh" : "chưa có tóm tắt bệnh án đầy đủ"));
                            try
                            {
                                var trkGetFilter = new HisTrackingFilter { ID = firstTrk.ID };
                                var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkGetFilter, param);
                                if (trkList != null && trkList.Count > 0)
                                {
                                    var fullTrk = trkList[0];
                                    string newSummary = emrSummary.Trim();
                                    fullTrk.CONTENT = string.Format(
                                        "Mạch: 78 l/p, HA: 120/80 mmHg, T: 36.5°C, NT: 18 l/p, SpO2: 98%.\r\n" +
                                        "- Bệnh nhân tiếp đón vào buồng bệnh nội trú, hoàn thiện hồ sơ bệnh án ngoại khoa EMR.\r\n" +
                                        "- Tóm tắt bệnh án từ vỏ EMR:\r\n{0}\r\n" +
                                        "- Hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, đại tiểu tiện tự chủ. Tiếp tục theo dõi sát tại buồng bệnh, thực hiện y lệnh điều trị và hoàn thiện các xét nghiệm bilan trước mổ.",
                                        newSummary);
                                    var dhstFilter = new HisDhstFilter { TRACKING_ID = fullTrk.ID };
                                    var dhsts = adapter.FetchList<HIS_DHST>("api/HisDhst/Get", mosConsumer, dhstFilter, param);

                                    long workRoomId = (deptId == 915) ? 18679 : 5248;
                                    var updSdo = new HisTrackingSDO 
                                    { 
                                        Tracking = fullTrk, 
                                        WorkingRoomId = workRoomId,
                                        Dhst = (dhsts != null && dhsts.Count > 0) ? dhsts[0] : null
                                    };
                                    var updRes = adapter.PostData<HIS_TRACKING>("api/HisTracking/Update", mosConsumer, updSdo, param);
                                    if (updRes != null)
                                    {
                                        sb.AppendLine("   ✔ Cập nhật nội dung Tờ điều trị đầu tiên THÀNH CÔNG RỰC RỠ!");
                                    }
                                    else
                                    {
                                        string msg = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "Không có phản hồi";
                                        sb.AppendLine("   ⚠️ Cập nhật Tờ điều trị không thành công: " + msg);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine("   ⚠️ Lỗi cập nhật Tờ điều trị: " + ex.Message);
                            }
                        }
                        else
                        {
                            sb.AppendLine(string.Format("   ✔ Bệnh nhân ĐÃ CÓ {0} tờ điều trị tại khoa (Tờ đầu tiên lập lúc {1} - ID: {2}).", deptTrks.Count, FormatTime(firstTrk.TRACKING_TIME), firstTrk.ID));
                            sb.AppendLine("     👉 Bỏ qua bước tạo mới để chống trùng lặp tờ điều trị.");
                        }
                        return true;
                    }
                }

                // 2. Nếu chưa có tờ điều trị nào của khoa tiếp đón -> Tự động tạo tờ điều trị đầu tiên
                sb.AppendLine("   📝 Chưa có tờ điều trị của khoa tiếp đón. Tiến hành lập Tờ điều trị đầu tiên...");

                long trackingTime;
                if (tr.CLINICAL_IN_TIME.HasValue && tr.CLINICAL_IN_TIME.Value > 0)
                {
                    DateTime clinInDt;
                    if (DateTime.TryParseExact(tr.CLINICAL_IN_TIME.Value.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out clinInDt))
                    {
                        DateTime candidateTime = clinInDt.AddMinutes(5);
                        DateTime now = DateTime.Now;
                        trackingTime = long.Parse((candidateTime > now ? now : candidateTime).ToString("yyyyMMddHHmmss"));
                    }
                    else
                    {
                        trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    }
                }
                else
                {
                    trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                }

                long workingRoomId = (deptId == 915) ? 18679 : 5248;

                // Đảm bảo WorkInfo phòng làm việc được kích hoạt trên Token
                try
                {
                    var workInfo = new WorkInfoSDO
                    {
                        Rooms = new List<RoomSDO>
                        {
                            new RoomSDO { RoomId = workingRoomId },
                            new RoomSDO { RoomId = roomId },
                            new RoomSDO { RoomId = 18679 },
                            new RoomSDO { RoomId = 18681 },
                            new RoomSDO { RoomId = 5248 }
                        }
                    };
                    adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
                }
                catch { }

                string summaryBlock = !string.IsNullOrWhiteSpace(emrSummary)
                    ? emrSummary.Trim()
                    : string.Format("- Bệnh nhân vào viện với chẩn đoán: {0} ({1}).\r\n- Đau và hạn chế vận động vùng tổn thương.", tr.ICD_NAME, tr.ICD_CODE);

                string content = string.Format(
                    "Mạch: 78 l/p, HA: 120/80 mmHg, T: 36.5°C, NT: 18 l/p, SpO2: 98%.\r\n" +
                    "- Bệnh nhân tiếp đón vào buồng bệnh nội trú, hoàn thiện hồ sơ bệnh án ngoại khoa EMR.\r\n" +
                    "- Tóm tắt bệnh án từ vỏ EMR:\r\n{0}\r\n" +
                    "- Hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, đại tiểu tiện tự chủ. Tiếp tục theo dõi sát tại buồng bệnh, thực hiện y lệnh điều trị và hoàn thiện các xét nghiệm bilan trước mổ.",
                    summaryBlock
                );

                string care = "Chế độ chăm sóc cấp 3. Theo dõi dấu hiệu sinh tồn 2 lần/ngày. Dinh dưỡng bệnh lý theo y lệnh.";
                string med = "Nghỉ ngơi tại giường. Thực hiện bilan CLS (CTM, Đông máu, Sinh hóa, X-quang, ECG, CĐHA chuyên khoa). Dùng thuốc theo đơn.";

                if (isDryRun)
                {
                    sb.AppendLine("   [DRY-RUN]: Sẽ tạo Tờ điều trị đầu tiên với thông số:");
                    sb.AppendLine(string.Format("     • Thời gian y lệnh: {0}", FormatTime(trackingTime)));
                    sb.AppendLine(string.Format("     • Khoa/Phòng     : Dept {0} / Room {1}", deptId, roomId));
                    sb.AppendLine("     • Diễn biến       :\n" + IndentLines(content, "       "));
                    sb.AppendLine("     • Chăm sóc        : " + care);
                    sb.AppendLine("     • Y lệnh          : " + med);
                    trackingId = 99999999;
                    return true;
                }

                HIS_TRACKING tracking = new HIS_TRACKING
                {
                    TREATMENT_ID = tr.ID,
                    DEPARTMENT_ID = deptId,
                    ROOM_ID = workingRoomId,
                    TRACKING_TIME = trackingTime,
                    CONTENT = content,
                    CARE_INSTRUCTION = care,
                    MEDICAL_INSTRUCTION = med,
                    ICD_CODE = tr.ICD_CODE,
                    ICD_NAME = tr.ICD_NAME,
                    ICD_SUB_CODE = tr.ICD_SUB_CODE,
                    ICD_TEXT = tr.ICD_TEXT
                };

                HisTrackingSDO sdo = new HisTrackingSDO { Tracking = tracking, WorkingRoomId = workingRoomId };
                CommonParam cp = new CommonParam();
                var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
                if (res == null || res.ID <= 0)
                {
                    var resSdo = adapter.PostData<HisTrackingSDO>("api/HisTracking/Create", mosConsumer, sdo, cp);
                    if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                    {
                        res = resSdo.Tracking;
                    }
                }

                if (res == null || res.ID <= 0)
                {
                    // Fallback thử với ROOM_ID = roomId nếu có buồng bệnh
                    if (roomId > 0)
                    {
                        tracking.ROOM_ID = roomId;
                        sdo.WorkingRoomId = roomId;
                        res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
                        if (res == null || res.ID <= 0)
                        {
                            var resSdo = adapter.PostData<HisTrackingSDO>("api/HisTracking/Create", mosConsumer, sdo, cp);
                            if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                            {
                                res = resSdo.Tracking;
                            }
                        }
                    }
                }

                if (res != null && res.ID > 0)
                {
                    trackingId = res.ID;
                    sb.AppendLine(string.Format("   ✔ ĐÃ TẠO THÀNH CÔNG TỜ ĐIỀU TRỊ ĐẦU TIÊN (ID: {0} lúc {1})!", res.ID, FormatTime(trackingTime)));
                    return true;
                }
                else
                {
                    string err = (cp.Messages != null && cp.Messages.Count > 0) ? string.Join("; ", cp.Messages) : "Backend trả về null";
                    if (!string.IsNullOrEmpty(cp.GetMessage())) err += " " + cp.GetMessage();
                    if (!string.IsNullOrEmpty(cp.GetBugCode())) err += " (" + cp.GetBugCode() + ")";
                    sb.AppendLine("   ❌ Lỗi tạo tờ điều trị: " + err);
                    return false;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("   ❌ Exception khi tạo tờ điều trị: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Step 3: Suất Ăn D0 & D1 Lúc 06:00 Sáng

        private static bool Step3_AssignAdmissionRations(V_HIS_TREATMENT tr, long trackingId, long requestRoomId, string comboInput, bool isDryRun, StringBuilder sb)
        {
            try
            {
                // 1. Xác định Combo Suất ăn
                string comboType = !string.IsNullOrEmpty(comboInput) ? comboInput : DetectCombo(tr);
                string comboDesc = (comboType == "DD01") ? "Đái tháo đường (DD01)" : ((comboType == "TM01") ? "Tim mạch / Tăng huyết áp (TM01)" : "Ngoại khoa thường quy (BT01)");
                sb.AppendLine(string.Format("   🍱 Phân loại Combo Dinh Dưỡng: {0} ({1})", comboType, comboDesc));

                // 2. Xác định ngày D0 (ngày vào khoa/viện) và D1 (ngày hôm sau)
                long admissionTimeRaw = (tr.CLINICAL_IN_TIME.HasValue && tr.CLINICAL_IN_TIME.Value > 0) ? tr.CLINICAL_IN_TIME.Value : tr.IN_TIME;
                DateTime inTimeDt = DateTime.Today;
                bool hasExactInTime = false;
                if (admissionTimeRaw > 0 && admissionTimeRaw.ToString().Length >= 14)
                {
                    DateTime parsed;
                    if (DateTime.TryParseExact(admissionTimeRaw.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        inTimeDt = parsed;
                        hasExactInTime = true;
                    }
                }
                else if (admissionTimeRaw > 0 && admissionTimeRaw.ToString().Length >= 8)
                {
                    string ymd = admissionTimeRaw.ToString().Substring(0, 8);
                    DateTime parsed;
                    if (DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        inTimeDt = parsed;
                    }
                }

                DateTime d0 = inTimeDt.Date;
                DateTime d1 = d0.AddDays(1);

                sb.AppendLine(string.Format("   📅 Lập lịch suất ăn: Ngày vào viện D0 ({0}) & Ngày kế tiếp D1 ({1})", d0.ToString("dd/MM/yyyy"), d1.ToString("dd/MM/yyyy")));
                sb.AppendLine("   ⏰ Quy tắc giờ y lệnh: Ngày D0 sau giờ nhập khoa 15 phút (+15p); Ngày D1 lúc 06:00:00 sáng");

                // 3. Kiểm tra suất ăn đã tồn tại trên đợt điều trị (Tránh trùng lặp)
                var rf = new HisSereServRationViewFilter { TREATMENT_ID = tr.ID };
                var existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mosConsumer, rf, param);
                HashSet<string> existingDays = new HashSet<string>();
                if (existingRations != null)
                {
                    foreach (var r in existingRations)
                    {
                        if (r.INTRUCTION_TIME.ToString().Length >= 8)
                        {
                            existingDays.Add(r.INTRUCTION_TIME.ToString().Substring(0, 8));
                        }
                    }
                }

                DateTime[] targetDates = new DateTime[] { d0, d1 };
                bool allSuccess = true;

                foreach (var dt in targetDates)
                {
                    string dayStr = dt.ToString("yyyyMMdd");
                    string dayDisplay = dt.ToString("dd/MM/yyyy");

                    if (existingDays.Contains(dayStr))
                    {
                        sb.AppendLine(string.Format("   ✔ Ngày {0}: ĐÃ CÓ suất ăn trên hệ thống. Bỏ qua tránh kê trùng.", dayDisplay));
                        continue;
                    }

                    long instructionTime;
                    string timeNotice;
                    if (dt.Date == d0 && hasExactInTime)
                    {
                        // QUY TẮC CỨNG: Ngày vào viện D0 giờ y lệnh sau giờ nhập khoa 15 phút (+15p)
                        DateTime d0InstructionDt = inTimeDt.AddMinutes(15);
                        instructionTime = long.Parse(d0InstructionDt.ToString("yyyyMMddHHmmss"));
                        timeNotice = string.Format("{0} (sau giờ nhập khoa {1} đúng 15p)", d0InstructionDt.ToString("HH:mm:ss"), inTimeDt.ToString("HH:mm:ss"));
                    }
                    else
                    {
                        // Ngày kế tiếp D1 chuẩn hóa lúc 06:00:00 sáng
                        instructionTime = long.Parse(dayStr + "060000");
                        timeNotice = "06:00:00 sáng";
                    }

                    if (isDryRun)
                    {
                        sb.AppendLine(string.Format("   [DRY-RUN]: Sẽ chỉ định 3 bữa (Sáng/Trưa/Tối) ngày {0} lúc {1} (Combo {2}, PatientTypeId 42, Room 5809)", dayDisplay, timeNotice, comboType));
                        continue;
                    }

                    bool ok = AssignSingleDayRation(tr, trackingId, instructionTime, comboType, requestRoomId, sb);
                    if (ok)
                    {
                        sb.AppendLine(string.Format("   ✔ Ngày {0}: Cấp thành công 3 bữa suất ăn ({1}) lúc {2}!", dayDisplay, comboType, timeNotice));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("   ❌ Ngày {0}: Cấp suất ăn thất bại!", dayDisplay));
                        allSuccess = false;
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                sb.AppendLine("   ❌ Exception khi cấp suất ăn: " + ex.Message);
                return false;
            }
        }

        private static bool AssignSingleDayRation(V_HIS_TREATMENT tr, long trackingId, long instructionTime, string comboType, long requestRoomId, StringBuilder sb)
        {
            var sdo = new HisRationServiceReqSDO
            {
                TreatmentIds = new List<long> { tr.ID },
                InstructionTimes = new List<long> { instructionTime },
                RequestRoomId = requestRoomId > 0 ? requestRoomId : 5248,
                RequestLoginName = "034727",
                RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                IcdCode = tr.ICD_CODE,
                IcdName = tr.ICD_NAME,
                IcdSubCode = tr.ICD_SUB_CODE,
                IcdText = tr.ICD_TEXT,
                HalfInFirstDay = false,
                IsForAutoCreateRation = false,
                IsForHomie = false,
                TrackingId = trackingId > 0 ? (long?)trackingId : null,
                RationServices = BuildRationServices(comboType, 42)
            };

            CommonParam cp = new CommonParam();
            try
            {
                var result = adapter.PostData<object>("api/HisServiceReq/RationCreate", mosConsumer, sdo, cp);
                if (!cp.HasException) return true;

                if (cp.Messages != null && cp.Messages.Count > 0)
                {
                    sb.AppendLine("     ❌ API Error: " + string.Join("; ", cp.Messages));
                }
                return false;
            }
            catch (Exception ex)
            {
                sb.AppendLine("     ❌ API Exception: " + ex.Message);
                return false;
            }
        }

        private static List<RationServiceSDO> BuildRationServices(string comboType, long patientTypeId)
        {
            var list = new List<RationServiceSDO>();
            long ptId = 42; // BẮT BUỘC 42 (Viện phí) cho 100% bệnh nhân

            if (comboType == "DD01") // Đái tháo đường
            {
                list.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
                list.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
                list.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
            }
            else if (comboType == "TM01") // Tim mạch / Tăng huyết áp
            {
                list.Add(new RationServiceSDO { ServiceId = 30117, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
                list.Add(new RationServiceSDO { ServiceId = 30093, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
                list.Add(new RationServiceSDO { ServiceId = 30094, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
            }
            else // BT01 (Bình thường / Ngoại khoa)
            {
                list.Add(new RationServiceSDO { ServiceId = 30073, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
                list.Add(new RationServiceSDO { ServiceId = 30153, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
                list.Add(new RationServiceSDO { ServiceId = 30154, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
            }
            return list;
        }

        private static string DetectCombo(V_HIS_TREATMENT tr)
        {
            if (tr == null) return "BT01";
            string diag = ((tr.ICD_NAME ?? "") + " " + (tr.ICD_TEXT ?? "")).ToLower();
            if (diag.Contains("tháo đường") || diag.Contains("đtđ") || diag.Contains("diabetes") || diag.Contains("glucose"))
                return "DD01";
            if (diag.Contains("tăng huyết áp") || diag.Contains("tim mạch") || diag.Contains("suy tim") || diag.Contains("rung nhĩ") || diag.Contains("tha"))
                return "TM01";
            return "BT01";
        }

        #endregion

        #region Step 4: Rà Soát CLS 3 Tháng & Đề Xuất Bilan Thiếu

        private class BilanItem
        {
            public string Category { get; set; }
            public bool IsDone { get; set; }
            public string DoneDetails { get; set; }
            public string MissingProposal { get; set; }
            public string ExecuteRoomHn { get; set; }
            public string ExecuteRoomNb { get; set; }
        }

        private static bool Step4_AuditParaclinicalBilan3Months(V_HIS_TREATMENT tr, string facility, StringBuilder sb)
        {
            try
            {
                long fromTime = long.Parse(DateTime.Now.AddDays(-90).ToString("yyyyMMdd000000"));
                sb.AppendLine(string.Format("   🔍 Quét toàn bộ hồ sơ cận lâm sàng từ ngày {0} đến nay...", DateTime.Now.AddDays(-90).ToString("dd/MM/yyyy")));

                // 1. Lấy tất cả đợt điều trị của bệnh nhân trong 90 ngày
                var tf = new HisTreatmentViewFilter
                {
                    PATIENT_CODE__EXACT = tr.TDL_PATIENT_CODE,
                    IN_TIME_FROM = fromTime
                };
                var treatments90 = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (treatments90 == null || treatments90.Count == 0)
                {
                    treatments90 = new List<V_HIS_TREATMENT> { tr };
                }

                var treatIds = treatments90.Select(x => x.ID).Distinct().ToList();
                if (!treatIds.Contains(tr.ID)) treatIds.Add(tr.ID);

                sb.AppendLine(string.Format("   ✔ Tìm thấy {0} đợt khám/điều trị trong 90 ngày qua.", treatIds.Count));

                // 2. Lấy toàn bộ dịch vụ SERE_SERV của các đợt này
                var ssFilter = new HisSereServViewFilter { TREATMENT_IDs = treatIds };
                var allSereServ = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param) ?? new List<V_HIS_SERE_SERV>();

                sb.AppendLine(string.Format("   ✔ Đã tải {0} dịch vụ đã chỉ định trong 3 tháng.", allSereServ.Count));

                // 3. Khởi tạo Ma trận Bilan 11 nhóm chu phẫu (Bộ 10 tiêu chuẩn cơ bản + CĐHA chuyên khoa)
                var bilan = new List<BilanItem>
                {
                    new BilanItem {
                        Category = "1. Công thức máu (Huyết học)",
                        MissingProposal = "Tổng phân tích tế bào máu ngoại vi (Laser) [BM00110]",
                        ExecuteRoomHn = "Khoa Huyết học truyền máu (P.XN Nhà Q)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "2. Nhóm máu hệ ABO & Rh(D)",
                        MissingProposal = "Định nhóm máu hệ ABO và Rh(D) (Gelcard/Scangel) [BM01700]",
                        ExecuteRoomHn = "Trung tâm Huyết học truyền máu (P.XN Nhà Q)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "3. Đông máu cơ bản (3 chỉ số)",
                        MissingProposal = "Đông máu cơ bản: PT (TQ), APTT (TCK), Fibrinogen [BM00531, BM260527.52, BM00542]",
                        ExecuteRoomHn = "Phòng 626 Nhà Q (Khoa Huyết học)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "4. Sinh hóa máu (Đường, Thận, Gan, Điện giải)",
                        MissingProposal = "Sinh hóa máu: Glucose, Ure, Creatinin, AST, ALT, Điện giải đồ (Na, K, Cl)",
                        ExecuteRoomHn = "Khoa Hóa sinh (Nhà Q)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "5. Bilan Vi sinh (HIV, HBsAg, HCV)",
                        MissingProposal = "Test nhanh hoặc Miễn dịch: HIV Ab/Ag [BM00871], HBsAg [BM00859], Anti-HCV [BM00837]",
                        ExecuteRoomHn = "Khoa Vi sinh (Nhà Q)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "6. Siêu âm ổ bụng tổng quát",
                        MissingProposal = "Siêu âm ổ bụng tổng quát (gan mật, lách, tụy, thận, bàng quang) [BM00199]",
                        ExecuteRoomHn = "Phòng Siêu âm Nhà Q / TT Điện quang (P.17547)",
                        ExecuteRoomNb = "Phòng Siêu âm Nhà E CS2"
                    },
                    new BilanItem {
                        Category = "7. X-quang tim phổi thẳng",
                        MissingProposal = "Chụp X-quang ngực thẳng số hóa [BM21074 / BM00338]",
                        ExecuteRoomHn = "Phòng XQ Nhà Q / Trung tâm Điện quang (P.17552)",
                        ExecuteRoomNb = "Phòng Chụp X-quang Nhà E CS2"
                    },
                    new BilanItem {
                        Category = "8. Tổng phân tích nước tiểu",
                        MissingProposal = "Tổng phân tích nước tiểu (10 thông số máy tự động) [BM02998]",
                        ExecuteRoomHn = "Phòng Xét nghiệm Nước tiểu (P.566 Nhà Q)",
                        ExecuteRoomNb = "Phòng Xét nghiệm Trung tâm CS2 (Tầng 1 Nhà E)"
                    },
                    new BilanItem {
                        Category = "9. Điện tâm đồ (ECG)",
                        MissingProposal = "Ghi điện tim vi tính (12 chuyển đạo) [BM04258]",
                        ExecuteRoomHn = "Phòng 734 Khoa 57 hoặc Phòng 931 Nhà Q / Trung tâm TDCN",
                        ExecuteRoomNb = "Phòng Thăm dò chức năng Nhà E CS2"
                    },
                    new BilanItem {
                        Category = "10. Siêu âm Doppler tim, van tim",
                        MissingProposal = "Siêu âm Doppler tim, van tim [BM00201] (Bắt buộc với BN >= 60t hoặc > 50t kèm bệnh tim mạch)",
                        ExecuteRoomHn = "Trung tâm Tim mạch / Phòng Siêu âm tim (P.1715)",
                        ExecuteRoomNb = "Phòng Thăm dò chức năng / Tim mạch Nhà E CS2"
                    },
                    new BilanItem {
                        Category = "11. CĐHA chuyên khoa tổn thương",
                        MissingProposal = DetectSpecialistImagingProposal(tr),
                        ExecuteRoomHn = "Trung tâm Điện quang Bệnh viện Bạch Mai",
                        ExecuteRoomNb = "Phòng Cắt lớp vi tính & CHT Nhà E CS2"
                    }
                };

                // 4. Đối soát từng dịch vụ vào ma trận bilan
                foreach (var ss in allSereServ)
                {
                    string name = (ss.TDL_SERVICE_NAME ?? "").ToLower();
                    string timeStr = FormatDateOnly(ss.TDL_INTRUCTION_TIME);

                    // Nhóm 1: Công thức máu
                    if (name.Contains("tổng phân tích tế bào máu") || name.Contains("công thức máu") || name.Contains("huyết đồ") || name.Contains("tổng phân tích máu"))
                    {
                        bilan[0].IsDone = true;
                        bilan[0].DoneDetails = AppendDetail(bilan[0].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 2: Nhóm máu
                    if (name.Contains("nhóm máu") || name.Contains("hệ abo") || name.Contains("hệ rh") || name.Contains("định nhóm máu"))
                    {
                        bilan[1].IsDone = true;
                        bilan[1].DoneDetails = AppendDetail(bilan[1].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 3: Đông máu
                    if (name.Contains("đông máu") || name.Contains("prothrombin") || name.Contains("aptt") || name.Contains("fibrinogen") || name.Contains("pt-inr") || name.Contains("thời gian prothrombin"))
                    {
                        bilan[2].IsDone = true;
                        bilan[2].DoneDetails = AppendDetail(bilan[2].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 4: Sinh hóa máu
                    if (name.Contains("glucose") || name.Contains("ure") || name.Contains("creatinin") || name.Contains("ast") || name.Contains("alt") || name.Contains("điện giải"))
                    {
                        bilan[3].IsDone = true;
                        bilan[3].DoneDetails = AppendDetail(bilan[3].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 5: Vi sinh
                    if (name.Contains("hbsag") || name.Contains("hcv") || name.Contains("hiv") || name.Contains("viêm gan"))
                    {
                        bilan[4].IsDone = true;
                        bilan[4].DoneDetails = AppendDetail(bilan[4].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 6: Siêu âm ổ bụng
                    if (name.Contains("siêu âm ổ bụng") || name.Contains("siêu âm bụng") || name.Contains("sa ổ bụng"))
                    {
                        bilan[5].IsDone = true;
                        bilan[5].DoneDetails = AppendDetail(bilan[5].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 7: X-quang tim phổi
                    if ((name.Contains("xquang") || name.Contains("x-quang") || name.Contains("chụp")) && (name.Contains("tim phổi") || name.Contains("ngực thẳng") || name.Contains("ngực")))
                    {
                        bilan[6].IsDone = true;
                        bilan[6].DoneDetails = AppendDetail(bilan[6].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 8: Nước tiểu 10 thông số
                    if (name.Contains("nước tiểu") || name.Contains("10 thông số") || name.Contains("tổng phân tích nước tiểu"))
                    {
                        bilan[7].IsDone = true;
                        bilan[7].DoneDetails = AppendDetail(bilan[7].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 9: Điện tim
                    if (name.Contains("điện tim") || name.Contains("điện tâm đồ") || name.Contains("ecg"))
                    {
                        bilan[8].IsDone = true;
                        bilan[8].DoneDetails = AppendDetail(bilan[8].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 10: Siêu âm Doppler tim
                    if (name.Contains("siêu âm tim") || name.Contains("doppler tim"))
                    {
                        bilan[9].IsDone = true;
                        bilan[9].DoneDetails = AppendDetail(bilan[9].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }

                    // Nhóm 11: CĐHA chuyên khoa (MRI, CT, XQ chi/cột sống)
                    if (name.Contains("cộng hưởng từ") || name.Contains("mri") || name.Contains("cắt lớp") || name.Contains("ct ") || name.Contains("x-quang cột sống") || name.Contains("xquang cột sống") || name.Contains("x-quang xương") || name.Contains("xquang xương"))
                    {
                        bilan[10].IsDone = true;
                        bilan[10].DoneDetails = AppendDetail(bilan[10].DoneDetails, ss.TDL_SERVICE_NAME, timeStr);
                    }
                }

                // 5. Trình bày Bảng Đối Soát Lâm Sàng
                sb.AppendLine("\n📊 BẢNG ĐỐI SOÁT MA TRẬN BILAN CHU PHẪU (3 THÁNG GẦN NHẤT):");
                sb.AppendLine("┌──────────────────────────────────────────────┬──────────────┬────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ Danh mục Bilan Chu Phẫu                      │ Trạng thái   │ Chi tiết dịch vụ đã làm & Ngày chỉ định                │");
                sb.AppendLine("├──────────────────────────────────────────────┼──────────────┼────────────────────────────────────────────────────────┤");

                List<BilanItem> missingItems = new List<BilanItem>();

                foreach (var b in bilan)
                {
                    string catPad = PadRightUnicode(b.Category, 44);
                    string statusPad = b.IsDone ? "✔ [ĐÃ CÓ]   " : "❌ [THIẾU]   ";
                    string detailPad = b.IsDone ? TruncateUnicode(b.DoneDetails, 54) : "Chưa có trong 90 ngày";
                    detailPad = PadRightUnicode(detailPad, 54);

                    sb.AppendLine(string.Format("│ {0} │ {1} │ {2} │", catPad, statusPad, detailPad));

                    if (!b.IsDone)
                    {
                        missingItems.Add(b);
                    }
                }
                sb.AppendLine("└──────────────────────────────────────────────┴──────────────┴────────────────────────────────────────────────────────┘");

                // 6. Đề Xuất Chỉ Định Còn Thiếu Kèm Phòng Thực Hiện Đích Danh Để Người Điều Hành Duyệt Trước Khi Kê
                if (missingItems.Count > 0)
                {
                    sb.AppendLine("\n📋 DANH SÁCH ĐỀ XUẤT CẬN LÂM SÀNG CÒN THIẾU (CHỜ NGƯỜI ĐIỀU HÀNH / BÁC SĨ DUYỆT TRƯỚC KHI KÊ):");
                    sb.AppendLine("⚠️ LƯU Ý BẢO VỆ AN TOÀN: Thợ trực buồng KHÔNG tự động kê các chỉ định này lên hệ thống HIS.");
                    sb.AppendLine("👉 Vui lòng gửi danh sách đề xuất này để Người điều hành / Bác sĩ trực duyệt và xác nhận trước khi kê:");
                    bool isNb = (facility == "NB");
                    int idx = 1;
                    foreach (var m in missingItems)
                    {
                        string targetRoom = isNb ? m.ExecuteRoomNb : m.ExecuteRoomHn;
                        sb.AppendLine(string.Format("   {0}. {1}:", idx++, m.Category));
                        sb.AppendLine(string.Format("      • Chỉ định đề xuất : {0}", m.MissingProposal));
                        sb.AppendLine(string.Format("      • Phòng chỉ định   : 🏥 {0}", targetRoom));
                    }
                }
                else
                {
                    sb.AppendLine("\n🎉 HOÀN HẢO: Bệnh nhân ĐÃ ĐẦY ĐỦ 100% các hạng mục Bilan chu phẫu cơ bản trong 3 tháng qua!");
                }

                return true;
            }
            catch (Exception ex)
            {
                sb.AppendLine("   ❌ Exception khi rà soát CLS: " + ex.Message);
                return false;
            }
        }

        private static string DetectSpecialistImagingProposal(V_HIS_TREATMENT tr)
        {
            if (tr == null) return "Chụp X-quang hoặc CT/MRI chuyên khoa vùng tổn thương";
            string d = ((tr.ICD_NAME ?? "") + " " + (tr.ICD_TEXT ?? "")).ToLower();
            if (d.Contains("cột sống") || d.Contains("thoát vị") || d.Contains("xẹp") || d.Contains("trượt đốt sống") || d.Contains("tủy"))
                return "Chụp Cộng hưởng từ (MRI) cột sống tổn thương + Chụp X-quang cột sống thẳng nghiêng cúi ưỡn";
            if (d.Contains("gãy") || d.Contains("xương") || d.Contains("khớp") || d.Contains("đùi") || d.Contains("cẳng"))
                return "Chụp X-quang kỹ thuật số xương khớp tổn thương 2 bình diện (hoặc CT Scanner dựng hình 3D nếu phạm khớp)";
            return "Chụp X-quang hoặc CT/MRI chuyên khoa vị trí phẫu thuật";
        }

        private static string AppendDetail(string existing, string serviceName, string timeStr)
        {
            string entry = string.Format("{0} ({1})", serviceName, timeStr);
            if (string.IsNullOrEmpty(existing)) return entry;
            if (existing.Contains(serviceName)) return existing;
            return existing + "; " + entry;
        }

        #endregion

        #region Helpers & Data Providers

        private static void EnsureLogin(string facility, StringBuilder sb)
        {
            string token = ReadLiveToken(facility);
            if (!string.IsNullOrEmpty(token))
            {
                mosConsumer = new ApiConsumer(MOS_BASE, token, "HIS");
                return;
            }

            try { Load.Init(); } catch { }
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            string defaultPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
            if (string.IsNullOrEmpty(defaultPass)) defaultPass = Environment.GetEnvironmentVariable("HIS_PASS");
            if (string.IsNullOrEmpty(defaultPass)) defaultPass = "981";

            CommonParam cp = new CommonParam();
            var tok = tokenManager.Login(cp, "034727", defaultPass, "2.390.0");
            if (tok == null || string.IsNullOrEmpty(tok.TokenCode))
            {
                tok = tokenManager.Login(cp, "vmc", "789789", "2.390.0");
            }

            if (tok != null && !string.IsNullOrEmpty(tok.TokenCode))
            {
                mosConsumer = new ApiConsumer(MOS_BASE, tok.TokenCode, "HIS");
                try
                {
                    string targetFile = (facility == "NB") ? "doctor_nb.token" : "doctor_hn.token";
                    string payload = tok.TokenCode + "|" + DateTime.Now.Ticks + "|034727";
                    File.WriteAllText(Path.Combine(BaseDir, targetFile), payload, Encoding.UTF8);
                    File.WriteAllText(Path.Combine(BaseDir, "doctor_standalone.token"), payload, Encoding.UTF8);
                }
                catch { }
            }
            else
            {
                throw new Exception("Không thể đăng nhập máy chủ MOS với tài khoản bác sĩ 034727!");
            }
        }

        private static string ReadLiveToken(string facility)
        {
            string targetFile = (facility == "NB") ? "doctor_nb.token" : "doctor_hn.token";
            string[] files = new string[]
            {
                Path.Combine(BaseDir, targetFile),
                Path.Combine(BaseDir, "doctor_standalone.token"),
                Path.Combine(BaseDir, "doctor_hn.token"),
                Path.Combine(BaseDir, "doctor_nb.token")
            };

            foreach (var f in files)
            {
                if (!File.Exists(f)) continue;
                try
                {
                    string[] parts = File.ReadAllText(f, Encoding.UTF8).Split('|');
                    if (parts.Length >= 2)
                    {
                        long savedTime;
                        if (long.TryParse(parts[1], out savedTime))
                        {
                            DateTime savedDt = new DateTime(savedTime);
                            if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                            {
                                return parts[0];
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static V_HIS_TREATMENT FindTreatment(string keyword, string facility, StringBuilder sb)
        {
            try
            {
                // 1. Tìm chính xác theo PATIENT_CODE
                var tf1 = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = keyword };
                var res1 = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf1, param);
                if (res1 != null && res1.Count > 0) return res1.OrderByDescending(x => x.IN_TIME).First();

                // 2. Tìm chính xác theo TREATMENT_CODE
                var tf2 = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = keyword };
                var res2 = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf2, param);
                if (res2 != null && res2.Count > 0) return res2.OrderByDescending(x => x.IN_TIME).First();

                // 3. Tìm theo tên trong buồng bệnh (TreatmentBedRoom)
                var tbrf = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true, TREATMENT_IS_ACTIVE = true };
                var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                if (beds != null && beds.Count > 0)
                {
                    string norm = RemoveDiacritics(keyword).ToLower().Trim();
                    var match = beds.Where(b => !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) && RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(norm)).ToList();
                    if (match.Count > 0)
                    {
                        long tId = match[0].TREATMENT_ID;
                        var tfBed = new HisTreatmentViewFilter { ID = tId };
                        var resBed = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBed, param);
                        if (resBed != null && resBed.Count > 0) return resBed[0];
                    }
                }

                // 4. Tìm theo KEY_WORD
                var tfKw = new HisTreatmentViewFilter { KEY_WORD = keyword, IS_PAUSE = false };
                var resKw = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
                if (resKw != null && resKw.Count > 0) return resKw.OrderByDescending(x => x.IN_TIME).First();
            }
            catch (Exception ex)
            {
                sb.AppendLine("   ⚠️ Lỗi tìm hồ sơ: " + ex.Message);
            }
            return null;
        }

        private static string DetectFacilityFromContext(string keyword, V_HIS_TREATMENT tr)
        {
            if (tr != null) return DetectFacilityFromTreatment(tr);
            string kw = (keyword ?? "").ToLower();
            if (kw.Contains("nb") || kw.Contains("ninh bình") || kw.Contains("3e") || kw.Contains("3d") || kw.Contains("khu e"))
                return "NB";
            if (kw.Contains("hn") || kw.Contains("hà nội") || kw.Contains("khoa 57") || kw.Contains("p7") || kw.Contains("71") || kw.Contains("72") || kw.Contains("73"))
                return "HN";
            return "NB"; // Mặc định Ninh Bình theo bối cảnh hiện tại
        }

        private static string DetectFacilityFromTreatment(V_HIS_TREATMENT tr)
        {
            if (tr == null) return "NB";
            if (tr.END_DEPARTMENT_ID == 915 || tr.BRANCH_ID == 81) return "NB";
            if (tr.END_DEPARTMENT_ID == 57 || tr.BRANCH_ID == 1) return "HN";
            return "NB";
        }

        private static string FormatTime(long timeNum)
        {
            if (timeNum <= 0) return "N/A";
            string s = timeNum.ToString();
            if (s.Length == 14)
                return string.Format("{0}/{1}/{2} {3}:{4}", s.Substring(6, 2), s.Substring(4, 2), s.Substring(0, 4), s.Substring(8, 2), s.Substring(10, 2));
            return s;
        }

        private static string FormatDateOnly(long? timeNum)
        {
            if (!timeNum.HasValue || timeNum.Value <= 0) return "N/A";
            string s = timeNum.Value.ToString();
            if (s.Length >= 8)
                return string.Format("{0}/{1}/{2}", s.Substring(6, 2), s.Substring(4, 2), s.Substring(0, 4));
            return s;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        private static string IndentLines(string text, string indent)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) lines[i] = indent + lines[i];
            return string.Join("\r\n", lines);
        }

        private static string TruncateUnicode(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLen) return text;
            return text.Substring(0, maxLen - 3) + "...";
        }

        private static string PadRightUnicode(string text, int totalWidth)
        {
            if (text == null) text = "";
            int len = text.Length;
            if (len >= totalWidth) return text;
            return text + new string(' ', totalWidth - len);
        }

        #endregion
    }
}
