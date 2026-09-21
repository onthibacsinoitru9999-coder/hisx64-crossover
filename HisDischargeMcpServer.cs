using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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

namespace HisDischargeMcp
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
            return DischargeService.Run(args);
        }
    }

    public class DischargeService
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
            bool dryRun = false;

            for (int i = 1; i < args.Length; i++)
            {
                string a = args[i].Trim().ToUpper();
                if (a == "NB" || a == "HN") facility = a;
                else if (a == "--DRY-RUN" || a == "-N" || a == "--PREVIEW") dryRun = true;
            }

            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(keyword, null);
            }

            bool isError;
            string output = ExecuteProtocol(keyword, facility, dryRun, out isError);
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
                        case "ping":
                            SendResponse(id, new JObject());
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
                    "name", "his-discharge",
                    "version", "1.1.0"
                )
            );
            SendResponse(id, res);
        }

        private static void HandleToolsList(JToken id)
        {
            var tools = new JArray();

            // 1. his_discharge_hn (Chuyên biệt Cơ sở Hà Nội)
            tools.Add(CreateTool(
                "his_discharge_hn",
                "Dac quyen 'Tho lam ra vien' CHUYEN BIET CO SO HA NOI (Khoa 57 CTCH, BS Nguyen Huu Sam 034727, token doctor_hn.token): Protocol 3 buoc: (1) Ra soat bo sung To DT SK 3 ngay, 7 ngay, Tong ket ra vien -> (2) Chuyen toan bo chi dinh trang ve 034727 (Bao luu 4 nhom: Giuong, Do vai, DMMM, Don thuoc) -> (3) Tao bia tom tat benh an, bia tong ket cuoi va bia kham ngoai khoa EMR noi tru",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (VD: 0004018669)"),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 2. his_discharge_nb (Chuyên biệt Cơ sở Ninh Bình)
            tools.Add(CreateTool(
                "his_discharge_nb",
                "Dac quyen 'Tho lam ra vien' CHUYEN BIET CO SO NINH BINH (Khoa 915 Ngoai TH Khu 3E, BS Vu Minh Cuong vmc, token doctor_nb.token): Protocol 3 buoc: (1) Ra soat bo sung To DT SK 3 ngay, 7 ngay, Tong ket ra vien -> (2) Chuyen toan bo chi dinh trang ve vmc (Bao luu 4 nhom: Giuong, Do vai, DMMM, Don thuoc) -> (3) Tao bia tom tat benh an, bia tong ket cuoi va bia kham ngoai khoa EMR noi tru",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (VD: 0004018669)"),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 3. his_tho_lam_ra_vien (Chính thức - Tự động điều hướng)
            tools.Add(CreateTool(
                "his_tho_lam_ra_vien",
                "Dac quyen 'Tho lam ra vien' (1-Click Discharge Protocol - Tu dong dieu huong HN/NB): Protocol 3 buoc: (1) Ra soat va bo sung To dieu tri SK 3 ngay, 7 ngay, Tong ket ra vien -> (2) Chuyen toan bo chi dinh trang ve BS tiep nhan (HN: 034727, NB: vmc, Bao luu 4 nhom) -> (3) Tao bia tom tat benh an, bia tong ket cuoi va bia kham ngoai khoa EMR noi tru",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri (VD: 0004018669)"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - thuc thi that)")
                ),
                Arr("patientCode")
            ));

            // 4. his_execute_protocol_discharge (Bí danh tương thích ngược)
            tools.Add(CreateTool(
                "his_execute_protocol_discharge",
                "Bi danh cua his_tho_lam_ra_vien: Protocol 'Tho lam ra vien' 1-Click toan dien 3 buoc (Tu dong dieu huong HN/NB)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
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

            try
            {
                string output = "";
                bool isError = false;

                switch (toolName)
                {
                    case "his_discharge_hn":
                    case "discharge_hn":
                        args["facility"] = "HN";
                        output = ExecuteProtocolMcp(args, out isError);
                        break;
                    case "his_discharge_nb":
                    case "discharge_nb":
                        args["facility"] = "NB";
                        output = ExecuteProtocolMcp(args, out isError);
                        break;
                    case "his_tho_lam_ra_vien":
                    case "tho_lam_ra_vien":
                    case "his_execute_protocol_discharge":
                    case "his_discharge_protocol":
                        output = ExecuteProtocolMcp(args, out isError);
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

        private static string ExecuteProtocolMcp(JObject args, out bool isError)
        {
            string pCode = "";
            if (args["patientCode"] != null) pCode = args["patientCode"].ToString().Trim();
            if (string.IsNullOrEmpty(pCode) && args["treatmentCode"] != null) pCode = args["treatmentCode"].ToString().Trim();
            if (string.IsNullOrEmpty(pCode) && args["keyword"] != null) pCode = args["keyword"].ToString().Trim();

            string facility = args["facility"] != null ? args["facility"].ToString().Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(pCode, args);
            }
            bool dryRun = args["dryRun"] != null && (bool)args["dryRun"];

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode (hoac treatmentCode) khong duoc de trong.";
            }

            return ExecuteProtocol(pCode, facility, dryRun, out isError);
        }

        private static string DetectFacilityFromContext(string keyword, JObject args)
        {
            string envFac = Environment.GetEnvironmentVariable("HIS_FACILITY");
            if (!string.IsNullOrEmpty(envFac)) return envFac.Trim().ToUpper();

            string envTok = Environment.GetEnvironmentVariable("HIS_TOKEN_FILE");
            if (!string.IsNullOrEmpty(envTok) && envTok.IndexOf("nb", StringComparison.OrdinalIgnoreCase) >= 0) return "NB";

            string kw = (keyword ?? "") + " " + (args != null ? args.ToString() : "");
            if (kw.IndexOf("NB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kw.IndexOf("Ninh Binh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kw.IndexOf("3E-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kw.IndexOf("3D-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kw.IndexOf("Khoa 915", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kw.IndexOf("vmc", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "NB";
            }
            return "HN";
        }

        #endregion

        #region Protocol "Thợ Làm Ra Viện" (Core Clinical Pipeline)

        public static string ExecuteProtocol(string keyword, string facility, bool dryRun, out bool isError)
        {
            isError = false;
            var sb = new StringBuilder();

            bool isNB = (facility != null && facility.Trim().ToUpper() == "NB");
            facility = isNB ? "NB" : "HN";

            bool initOk = InitSession(facility, sb);
            if (!initOk)
            {
                isError = true;
                return sb.ToString();
            }

            var tr = FindTreatmentByKeyword(keyword);
            if (tr == null)
            {
                isError = true;
                sb.AppendLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + keyword);
                return sb.ToString();
            }

            string targetDoctor = isNB ? "vmc" : "034727";
            string doctorName = isNB ? "BS Vũ Minh Cường" : "Ths.BS Nguyễn Hữu Sâm";
            string deptName = isNB ? "Khoa Ngoại tổng hợp NB (Khoa 915 - Khu 3E)" : "Khoa CTCH & Cột sống HN (Khoa 57)";

            sb.AppendLine("===============================================================================");
            sb.AppendLine(string.Format("⚡ THỰC THI PROTOCOL 'THỢ LÀM RA VIỆN' - CƠ SỞ {0} ({1})", facility, isNB ? "NINH BÌNH" : "HÀ NỘI"));
            sb.AppendLine(string.Format("Bệnh nhân: {0} ({1}) | Mã ĐT: {2} | Khoa: {3}",
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, deptName));
            sb.AppendLine(string.Format("Bác sĩ tiếp nhận: {0} ({1}) | Chế độ: {2}",
                doctorName, targetDoctor, (dryRun ? "DRY-RUN" : "EXECUTE")));
            sb.AppendLine("===============================================================================\n");

            // BƯỚC 1: Rà soát và bổ sung tờ điều trị
            bool step1Ok = EnsureDischargeTracking(tr, facility, dryRun, sb);

            // BƯỚC 2: Chuyển chỉ định trắng về bác sĩ đích (034727 cho HN, vmc cho NB)
            bool step2Ok = TransferWhiteOrders(tr, targetDoctor, dryRun, sb);

            // BƯỚC 3: Tạo bìa tóm tắt bệnh án, bìa tổng kết cuối, bìa khám ngoại khoa EMR
            bool step3Ok = EnsureEmrCover(tr, facility, dryRun, sb);

            sb.AppendLine("\n===============================================================================");
            bool allPassed = step1Ok && step2Ok && step3Ok;
            if (allPassed)
            {
                sb.AppendLine("🎉 HOÀN TẤT TOÀN DIỆN PROTOCOL 'THỢ LÀM RA VIỆN' THÀNH CÔNG RỰC RỠ!");
            }
            else
            {
                sb.AppendLine("⚠️ HOÀN TẤT PROTOCOL CÓ CẢNH BÁO / LỖI Ở MỘT SỐ BƯỚC. VUI LÒNG KIỂM TRA LẠI LOG!");
                isError = true;
            }
            sb.AppendLine("===============================================================================");

            return sb.ToString();
        }

        #endregion

        #region Kỹ năng 1: Rà soát & Bổ sung Tờ điều trị (3 ngày, 7 ngày, Tổng kết ra viện)

        private static bool EnsureDischargeTracking(V_HIS_TREATMENT tr, string facilityInput, bool isDryRun, StringBuilder sb)
        {
            sb.AppendLine("===============================================================================");
            sb.AppendLine("📝 BƯỚC 1: RÀ SOÁT & BỔ SUNG TỜ ĐIỀU TRỊ (SK 3 NGÀY, 7 NGÀY, TỔNG KẾT RA VIỆN)");
            sb.AppendLine("===============================================================================");

            bool isNB = (facilityInput != null && facilityInput.Trim().ToUpper() == "NB") ||
                         (facilityInput == null && (tr.END_DEPARTMENT_ID == 915 || (tr.END_DEPARTMENT_NAME ?? "").Contains("Ngoại tổng hợp") || (tr.END_DEPARTMENT_NAME ?? "").Contains("Ninh Bình")));
            long deptId = isNB ? 915 : 57;
            long roomId = isNB ? 18679 : 5248;
            string deptName = isNB ? "Khoa Ngoại tổng hợp NB (Khoa 915)" : "Khoa CTCH & Cột sống HN (Khoa 57)";

            EnsureWorkInfoForRoom(roomId);

            sb.AppendLine(string.Format("• Bệnh nhân   : {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            sb.AppendLine(string.Format("• Cơ sở / Khoa: {0} (Phòng làm việc: {1})", deptName, roomId));
            sb.AppendLine(string.Format("• Chẩn đoán   : [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));

            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
            var existingTrks = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param) ?? new List<HIS_TRACKING>();

            // 1. Xác định thời điểm có tờ điều trị đầu tiên tại khoa lâm sàng
            var deptTrks = existingTrks.Where(x => x.DEPARTMENT_ID == deptId).OrderBy(x => x.TRACKING_TIME).ToList();
            DateTime startDate = DateTime.Today;
            bool hasFirstDeptTracking = false;

            if (deptTrks.Count > 0 && deptTrks[0].TRACKING_TIME > 0)
            {
                string sTime = deptTrks[0].TRACKING_TIME.ToString();
                if (sTime.Length >= 8)
                {
                    int y = int.Parse(sTime.Substring(0, 4));
                    int m = int.Parse(sTime.Substring(4, 2));
                    int d = int.Parse(sTime.Substring(6, 2));
                    startDate = new DateTime(y, m, d);
                    hasFirstDeptTracking = true;
                }
            }

            if (!hasFirstDeptTracking && existingTrks.Count > 0 && existingTrks.OrderBy(x => x.TRACKING_TIME).First().TRACKING_TIME > 0)
            {
                string sTime = existingTrks.OrderBy(x => x.TRACKING_TIME).First().TRACKING_TIME.ToString();
                if (sTime.Length >= 8)
                {
                    int y = int.Parse(sTime.Substring(0, 4));
                    int m = int.Parse(sTime.Substring(4, 2));
                    int d = int.Parse(sTime.Substring(6, 2));
                    startDate = new DateTime(y, m, d);
                    hasFirstDeptTracking = true;
                }
            }

            if (!hasFirstDeptTracking && tr.CLINICAL_IN_TIME.HasValue && tr.CLINICAL_IN_TIME.Value > 0)
            {
                string sTime = tr.CLINICAL_IN_TIME.Value.ToString();
                if (sTime.Length >= 8)
                {
                    int y = int.Parse(sTime.Substring(0, 4));
                    int m = int.Parse(sTime.Substring(4, 2));
                    int d = int.Parse(sTime.Substring(6, 2));
                    startDate = new DateTime(y, m, d);
                    hasFirstDeptTracking = true;
                }
            }

            if (!hasFirstDeptTracking && tr.IN_TIME > 0)
            {
                string sTime = tr.IN_TIME.ToString();
                if (sTime.Length >= 8)
                {
                    int y = int.Parse(sTime.Substring(0, 4));
                    int m = int.Parse(sTime.Substring(4, 2));
                    int d = int.Parse(sTime.Substring(6, 2));
                    startDate = new DateTime(y, m, d);
                }
            }

            DateTime endDate = DateTime.Today;
            if (tr.OUT_TIME.HasValue && tr.OUT_TIME.Value > 0)
            {
                string sOut = tr.OUT_TIME.Value.ToString();
                if (sOut.Length >= 8)
                {
                    int y = int.Parse(sOut.Substring(0, 4));
                    int m = int.Parse(sOut.Substring(4, 2));
                    int d = int.Parse(sOut.Substring(6, 2));
                    endDate = new DateTime(y, m, d);
                }
            }

            int deptDays = Math.Max(1, (int)(endDate.Date - startDate.Date).TotalDays + 1);
            sb.AppendLine(string.Format("• Ngày bắt đầu điều trị tại khoa: {0} (Khoảng điều trị: {1} đến {2} - {3} ngày)",
                startDate.ToString("dd/MM/yyyy"), startDate.ToString("dd/MM/yyyy"), endDate.ToString("dd/MM/yyyy"), deptDays));
            sb.AppendLine(string.Format("• Hiện có {0} tờ điều trị trên hệ thống.", existingTrks.Count));

            var usedTimes = new HashSet<long>(existingTrks.Select(x => x.TRACKING_TIME));
            int createdCount = 0;
            bool allOk = true;

            // A. SƠ KẾT 3 NGÀY (Cần nếu deptDays >= 3)
            if (deptDays >= 3)
            {
                bool hasSk3 = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 3") || c.Contains("sơ kết 03") || c.Contains("sk 3") || c.Contains("sk 03") || c.Contains("sơ kết ba ngày");
                });

                if (!hasSk3)
                {
                    DateTime sk3Date = startDate.AddDays(2);
                    if (sk3Date > endDate.Date) sk3Date = endDate.Date;
                    DateTime sk3Dt = new DateTime(sk3Date.Year, sk3Date.Month, sk3Date.Day, 14, 30, 0);
                    while (usedTimes.Contains(long.Parse(sk3Dt.ToString("yyyyMMddHHmmss"))))
                    {
                        sk3Dt = sk3Dt.AddMinutes(1);
                    }
                    long sk3Time = long.Parse(sk3Dt.ToString("yyyyMMddHHmmss"));
                    usedTimes.Add(sk3Time);

                    string sk3Content = string.Format(
                        "SƠ KẾT 3 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                        "- Toàn trạng: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, thể trạng trung bình.\n" +
                        "- Khám chuyên khoa: Tổn thương/vết mổ tiến triển ổn định, mép khô sạch, không sưng đỏ nề, không chảy dịch bất thường. Đầu chi hồng ấm, vận động cảm giác ngoại vi trong giới hạn bình thường.\n" +
                        "- Đáp ứng điều trị: Bệnh nhân đáp ứng tốt với phác đồ điều trị, giảm đau rõ rệt so với lúc vào viện, sinh hiệu ổn định.\n" +
                        "- Hướng điều trị tiếp theo: Tiếp tục phác đồ dùng thuốc theo đơn, thay băng chăm sóc vết thương hàng ngày, tập phục hồi chức năng nhẹ nhàng, theo dõi sát diễn biến.",
                        startDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));
                    string care3 = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Thay băng vết thương hàng ngày.";
                    string med3 = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và tưới máu ngoại vi.";

                    if (isDryRun)
                    {
                        sb.AppendLine(string.Format("   [DRY-RUN] Cần tạo: Sơ kết 3 ngày điều trị lúc {0}", sk3Dt.ToString("dd/MM/yyyy HH:mm")));
                        createdCount++;
                    }
                    else
                    {
                        bool ok = CreateSingleTrackingRecord(tr, deptId, roomId, sk3Time, sk3Content, care3, med3, sb);
                        if (ok)
                        {
                            sb.AppendLine(string.Format("   ✔ ĐÃ TẠO THÀNH CÔNG: Sơ kết 3 ngày điều trị (Thời điểm: {0})", sk3Dt.ToString("dd/MM/yyyy HH:mm")));
                            createdCount++;
                        }
                        else
                        {
                            sb.AppendLine("   ❌ THẤT BẠI khi tạo Sơ kết 3 ngày!");
                            allOk = false;
                        }
                    }
                }
                else
                {
                    sb.AppendLine("   ✔ ĐÃ CÓ: Tờ Sơ kết 3 ngày điều trị.");
                }
            }
            else
            {
                sb.AppendLine(string.Format("   ⚪ Điều trị {0} ngày (< 3 ngày) -> Chưa cần Sơ kết 3 ngày.", deptDays));
            }

            // B. SƠ KẾT 7 NGÀY (Cần nếu deptDays >= 7)
            if (deptDays >= 7)
            {
                bool hasSk7 = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 7") || c.Contains("sơ kết 07") || c.Contains("sk 7") || c.Contains("sơ kết 15") || c.Contains("sơ kết tuần") || c.Contains("sơ kết bảy ngày");
                });

                if (!hasSk7)
                {
                    DateTime sk7Date = startDate.AddDays(6);
                    if (sk7Date > endDate.Date) sk7Date = endDate.Date;
                    DateTime sk7Dt = new DateTime(sk7Date.Year, sk7Date.Month, sk7Date.Day, 15, 0, 0);
                    while (usedTimes.Contains(long.Parse(sk7Dt.ToString("yyyyMMddHHmmss"))))
                    {
                        sk7Dt = sk7Dt.AddMinutes(1);
                    }
                    long sk7Time = long.Parse(sk7Dt.ToString("yyyyMMddHHmmss"));
                    usedTimes.Add(sk7Time);

                    string title7 = deptDays >= 15 ? string.Format("SƠ KẾT ĐỢT ĐIỀU TRỊ ({0} NGÀY - Từ {1} đến {2})", deptDays, startDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"))
                                                  : string.Format("SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})", startDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));

                    string sk7Content = string.Format(
                        "{0}\n" +
                        "- Toàn trạng: Bệnh nhân điều trị ngày thứ {1}. Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, ăn ngủ được, đại tiểu tiện bình thường.\n" +
                        "- Khám chuyên khoa: Vết mổ/tổn thương liền sẹo tiến triển tốt, khô sạch, dịch tiết giảm rõ rệt, không có biểu hiện nhiễm trùng tại chỗ. Trục chi thẳng, tưới máu ngọn chi tốt, vận động các khớp lân cận được cải thiện.\n" +
                        "- Cận lâm sàng: Các xét nghiệm huyết học, sinh hóa và hình ảnh chẩn đoán nằm trong giới hạn kiểm soát tốt.\n" +
                        "- Đánh giá chung: Bệnh nhân tiến triển thuận lợi theo đúng phác đồ điều trị chuyên khoa CTCH & Cột sống.\n" +
                        "- Hướng điều trị tiếp theo: Tiếp tục duy trì phác đồ điều trị, tăng cường tập phục hồi chức năng, theo dõi liền xương/liền gân và dự kiến kế hoạch ra viện khi đủ điều kiện.",
                        title7, deptDays);
                    string care7 = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Tập vận động phục hồi chức năng.";
                    string med7 = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động chi.";

                    if (isDryRun)
                    {
                        sb.AppendLine(string.Format("   [DRY-RUN] Cần tạo: Sơ kết 7 ngày điều trị lúc {0}", sk7Dt.ToString("dd/MM/yyyy HH:mm")));
                        createdCount++;
                    }
                    else
                    {
                        bool ok = CreateSingleTrackingRecord(tr, deptId, roomId, sk7Time, sk7Content, care7, med7, sb);
                        if (ok)
                        {
                            sb.AppendLine(string.Format("   ✔ ĐÃ TẠO THÀNH CÔNG: Sơ kết 7 ngày điều trị (Thời điểm: {0})", sk7Dt.ToString("dd/MM/yyyy HH:mm")));
                            createdCount++;
                        }
                        else
                        {
                            sb.AppendLine("   ❌ THẤT BẠI khi tạo Sơ kết 7 ngày!");
                            allOk = false;
                        }
                    }
                }
                else
                {
                    sb.AppendLine("   ✔ ĐÃ CÓ: Tờ Sơ kết 7 ngày điều trị.");
                }
            }
            else
            {
                sb.AppendLine(string.Format("   ⚪ Điều trị {0} ngày (< 7 ngày) -> Chưa cần Sơ kết 7 ngày.", deptDays));
            }

            // C. TỔNG KẾT RA VIỆN
            bool hasDischarge = existingTrks.Any(x => {
                string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                return c.Contains("tổng kết ra viện") || c.Contains("tong ket ra vien") || c.Contains("đủ điều kiện ra viện") || c.Contains("cho ra viện");
            });

            if (!hasDischarge)
            {
                DateTime disDate = endDate.Date;

                // Xác định mốc thời gian tối thiểu trong ngày ra viện
                DateTime minDt = new DateTime(disDate.Year, disDate.Month, disDate.Day, 0, 0, 0);
                if (tr.IN_TIME > 0)
                {
                    string sIn = tr.IN_TIME.ToString();
                    if (sIn.Length >= 14 && sIn.StartsWith(disDate.ToString("yyyyMMdd")))
                    {
                        int hh = int.Parse(sIn.Substring(8, 2));
                        int mm = int.Parse(sIn.Substring(10, 2));
                        int ss = int.Parse(sIn.Substring(12, 2));
                        var dtIn = new DateTime(disDate.Year, disDate.Month, disDate.Day, hh, mm, ss);
                        if (dtIn > minDt) minDt = dtIn;
                    }
                }

                var sameDayTimes = existingTrks.Where(x => {
                    string st = x.TRACKING_TIME.ToString();
                    return st.Length >= 14 && st.StartsWith(disDate.ToString("yyyyMMdd"));
                }).Select(x => x.TRACKING_TIME).ToList();

                if (sameDayTimes.Count > 0)
                {
                    long maxSame = sameDayTimes.Max();
                    string sMax = maxSame.ToString();
                    int hh = int.Parse(sMax.Substring(8, 2));
                    int mm = int.Parse(sMax.Substring(10, 2));
                    int ss = int.Parse(sMax.Substring(12, 2));
                    var dtMax = new DateTime(disDate.Year, disDate.Month, disDate.Day, hh, mm, ss);
                    if (dtMax > minDt) minDt = dtMax;
                }

                // Mặc định Tổng kết ra viện vào buổi chiều lúc 16:00
                DateTime disDt = new DateTime(disDate.Year, disDate.Month, disDate.Day, 16, 0, 0);
                if (minDt >= disDt)
                {
                    disDt = minDt.AddMinutes(5);
                }

                if (tr.OUT_TIME.HasValue && tr.OUT_TIME.Value > 0)
                {
                    string sOut = tr.OUT_TIME.Value.ToString();
                    if (sOut.Length >= 14 && sOut.StartsWith(disDate.ToString("yyyyMMdd")))
                    {
                        int hh = int.Parse(sOut.Substring(8, 2));
                        int mm = int.Parse(sOut.Substring(10, 2));
                        int ss = int.Parse(sOut.Substring(12, 2));
                        var dtOut = new DateTime(disDate.Year, disDate.Month, disDate.Day, hh, mm, ss);
                        if (disDt > dtOut) disDt = dtOut;
                    }
                }

                while (usedTimes.Contains(long.Parse(disDt.ToString("yyyyMMddHHmmss"))))
                {
                    disDt = disDt.AddMinutes(1);
                }
                long disTime = long.Parse(disDt.ToString("yyyyMMddHHmmss"));
                usedTimes.Add(disTime);

                string disContent = 
                    "TỔNG KẾT RA VIỆN:\n" +
                    "- Toàn trạng: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, ăn ngủ tốt, đại tiểu tiện bình thường.\n" +
                    "- Tình trạng chuyên khoa: Vết mổ/tổn thương khô sạch liền sẹo tốt, không sưng đỏ nề, không chảy dịch bất thường. Trục chi vững/cột sống ổn định, tưới máu ngọn chi tốt, vận động các khớp cải thiện rõ rệt, sinh hiệu ổn định.\n" +
                    "- Đánh giá kết quả điều trị: Bệnh nhân đáp ứng rất tốt với phác đồ điều trị, diễn biến điều trị ổn định, đủ điều kiện xuất viện.";
                string careDis = "Chăm sóc cấp II. Ăn uống dinh dưỡng đầy đủ.";
                string medDis = "Cho ra viện. Kê đơn ngoại trú. Hướng dẫn chăm sóc và hẹn tái khám sau 1 tháng (hoặc khi có dấu hiệu bất thường).";

                if (isDryRun)
                {
                    sb.AppendLine(string.Format("   [DRY-RUN] Cần tạo: Tờ Tổng kết ra viện lúc {0}", disDt.ToString("dd/MM/yyyy HH:mm")));
                    createdCount++;
                }
                else
                {
                    bool ok = CreateSingleTrackingRecord(tr, deptId, roomId, disTime, disContent, careDis, medDis, sb);
                    if (ok)
                    {
                        sb.AppendLine(string.Format("   ✔ ĐÃ TẠO THÀNH CÔNG: Tờ Tổng kết ra viện (Thời điểm: {0})", disDt.ToString("dd/MM/yyyy HH:mm")));
                        createdCount++;
                    }
                    else
                    {
                        sb.AppendLine("   ❌ THẤT BẠI khi tạo Tờ Tổng kết ra viện!");
                        allOk = false;
                    }
                }
            }
            else
            {
                sb.AppendLine("   ✔ ĐÃ CÓ: Tờ Tổng kết ra viện.");
            }

            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("📊 HOÀN THÀNH BƯỚC 1: Đã bổ sung {0} tờ điều trị sơ kết / tổng kết.", createdCount));
            sb.AppendLine("===============================================================================\n");
            return allOk;
        }

        private static bool CreateSingleTrackingRecord(V_HIS_TREATMENT tr, long deptId, long roomId, long trackingTime, string content, string care, string med, StringBuilder sb)
        {
            HIS_TRACKING tracking = new HIS_TRACKING
            {
                TREATMENT_ID = tr.ID,
                DEPARTMENT_ID = deptId,
                ROOM_ID = roomId,
                TRACKING_TIME = trackingTime,
                CONTENT = content,
                CARE_INSTRUCTION = care,
                MEDICAL_INSTRUCTION = med,
                ICD_CODE = tr.ICD_CODE,
                ICD_NAME = tr.ICD_NAME,
                ICD_SUB_CODE = tr.ICD_SUB_CODE,
                ICD_TEXT = tr.ICD_TEXT
            };

            HisTrackingSDO sdo = new HisTrackingSDO { Tracking = tracking, WorkingRoomId = roomId };
            CommonParam cp = new CommonParam();
            try
            {
                var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
                if (res != null && res.ID > 0) return true;
                if (cp.Messages != null && cp.Messages.Count > 0)
                    sb.AppendLine("    ❌ MOS Error: " + string.Join("; ", cp.Messages));
                return false;
            }
            catch (Exception ex)
            {
                sb.AppendLine("    ❌ Exception: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Kỹ năng 2: Chuyển toàn bộ chỉ định trắng về 034727 (Bảo lưu 4 nhóm)

        private static bool TransferWhiteOrders(V_HIS_TREATMENT tr, string targetDoctorLogin, bool isDryRun, StringBuilder sb)
        {
            sb.AppendLine("===============================================================================");
            sb.AppendLine("🔄 BƯỚC 2: CHUYỂN TOÀN BỘ CHỈ ĐỊNH TRẮNG VỀ BÁC SĨ: " + targetDoctorLogin);
            sb.AppendLine("===============================================================================");

            if (tr.IS_PAUSE == 1)
            {
                sb.AppendLine("⚠️ CẢNH BÁO: Hồ sơ bệnh án đã kết thúc điều trị / ra viện (IS_PAUSE = 1).");
                sb.AppendLine("   Backend MOS có thể khóa không cho phép sửa y lệnh.");
            }

            string targetDoctorName = (string.Equals(targetDoctorLogin, "vmc", StringComparison.OrdinalIgnoreCase)) ? "VŨ MINH CƯỜNG" : "NGUYỄN HỮU SÂM";
            string targetDoctorTitle = (string.Equals(targetDoctorLogin, "vmc", StringComparison.OrdinalIgnoreCase)) ? "Bác sĩ" : "Thạc sỹ y học";

            sb.AppendLine(string.Format("• Bệnh nhân   : {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            sb.AppendLine(string.Format("• BS Tiếp nhận : {0} ({1}) - {2}", targetDoctorName, targetDoctorLogin, targetDoctorTitle));

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param) ?? new List<V_HIS_SERVICE_REQ>();

            var whiteOrders = orders.Where(x => x.SERVICE_REQ_STT_ID == 1).ToList();
            if (whiteOrders.Count == 0)
            {
                sb.AppendLine("ℹ️ Bệnh nhân không có y lệnh nào ở trạng thái Chưa thực hiện (màu trắng).");
                sb.AppendLine("===============================================================================\n");
                return true;
            }

            sb.AppendLine(string.Format("🔍 Tìm thấy {0} y lệnh màu trắng (Chưa thực hiện). Đang kiểm tra bảo lưu...", whiteOrders.Count));

            Dictionary<long, List<V_HIS_SERE_SERV>> ssMap = new Dictionary<long, List<V_HIS_SERE_SERV>>();
            try
            {
                var reqIds = whiteOrders.Select(x => x.ID).Distinct().ToList();
                if (reqIds.Count > 0)
                {
                    HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = reqIds };
                    var allSs = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (allSs != null)
                    {
                        ssMap = allSs.Where(x => x.SERVICE_REQ_ID.HasValue)
                                     .GroupBy(x => x.SERVICE_REQ_ID.Value)
                                     .ToDictionary(g => g.Key, g => g.ToList());
                    }
                }
            }
            catch { }

            int transferredCount = 0;
            int protectedCount = 0;
            int skippedAlreadyDoctorCount = 0;
            int failCount = 0;

            foreach (var req in whiteOrders)
            {
                List<V_HIS_SERE_SERV> ssList;
                ssMap.TryGetValue(req.ID, out ssList);

                // RÀO CHẮN BẢO LƯU TUYỆT ĐỐI 4 NHÓM
                string protectReason;
                if (IsProtectedDischargeOrder(req, ssList, out protectReason))
                {
                    sb.AppendLine(string.Format("   🛡️ [BẢO LƯU] Bỏ qua Y lệnh ID {0} (Mã: {1} | {2}): {3}",
                        req.ID, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, protectReason));
                    protectedCount++;
                    continue;
                }

                if (string.Equals(req.REQUEST_LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(string.Format("   ✔ [ĐÃ CHUẨN] Y lệnh ID {0} (Mã: {1} | {2}): Đã do {3} ({4}) chỉ định.",
                        req.ID, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                    skippedAlreadyDoctorCount++;
                    continue;
                }

                if (isDryRun)
                {
                    sb.AppendLine(string.Format("   [DRY-RUN] Sẽ chuyển: Y lệnh ID {0} (Mã: {1} | {2}) từ {3} ({4}) sang {5} ({6})",
                        req.ID, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, targetDoctorName, targetDoctorLogin));
                    transferredCount++;
                }
                else
                {
                    try
                    {
                        if (req.REQUEST_ROOM_ID > 0)
                        {
                            EnsureWorkInfoForRoom(req.REQUEST_ROOM_ID);
                        }

                        HIS_SERVICE_REQ rawReq = null;
                        try
                        {
                            var updateDto = new HIS_SERVICE_REQ();
                            DataObjectMapper.Map<HIS_SERVICE_REQ>(updateDto, req);
                            if (updateDto.ID > 0) rawReq = updateDto;
                        }
                        catch { }

                        if (rawReq == null)
                        {
                            var rawFilter = new HisServiceReqFilter { ID = req.ID };
                            var cpGet = new CommonParam();
                            var rawList = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mosConsumer, rawFilter, cpGet);
                            if (rawList != null && rawList.Count > 0)
                            {
                                rawReq = rawList[0];
                            }
                        }

                        if (rawReq != null)
                        {
                            rawReq.REQUEST_LOGINNAME = targetDoctorLogin;
                            rawReq.REQUEST_USERNAME = targetDoctorName;
                            rawReq.REQUEST_USER_TITLE = targetDoctorTitle;
                            var cpUpd = new CommonParam();
                            var updRes = adapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mosConsumer, rawReq, cpUpd);
                            if (updRes != null && !cpUpd.HasException)
                            {
                                sb.AppendLine(string.Format("   ✔ ĐÃ CHUYỂN: Y lệnh ID {0} (Mã: {1} | {2}) từ {3} ({4}) sang {5} ({6})",
                                    req.ID, req.SERVICE_REQ_CODE, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, targetDoctorName, targetDoctorLogin));
                                transferredCount++;
                            }
                            else
                            {
                                sb.AppendLine(string.Format("   ❌ THẤT BẠI chuyển Y lệnh ID {0}: {1}", req.ID, cpUpd.GetMessage()));
                                failCount++;
                            }
                        }
                        else
                        {
                            sb.AppendLine(string.Format("   ❌ THẤT BẠI: Không thể lấy thông tin chi tiết Y lệnh ID {0} từ API", req.ID));
                            failCount++;
                        }
                    }
                    catch (Exception exUpd)
                    {
                        sb.AppendLine(string.Format("   ❌ LỖI ngoại lệ Y lệnh ID {0}: {1}", req.ID, exUpd.Message));
                        failCount++;
                    }
                }
            }

            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("📊 HOÀN THÀNH BƯỚC 2: Đã chuyển {0} y lệnh, Bảo lưu {1} y lệnh, Đã chuẩn {2} y lệnh, Thất bại {3}.",
                transferredCount, protectedCount, skippedAlreadyDoctorCount, failCount));
            sb.AppendLine("===============================================================================\n");
            return failCount == 0;
        }

        private static bool IsProtectedDischargeOrder(V_HIS_SERVICE_REQ req, List<V_HIS_SERE_SERV> ssList, out string reason)
        {
            reason = "";

            // 1. Giường
            if (req.SERVICE_REQ_TYPE_ID == 8 || (req.SERVICE_REQ_TYPE_NAME ?? "").ToLower().Contains("giường"))
            {
                reason = "Y lệnh Giường bệnh (SERVICE_REQ_TYPE_ID = 8)";
                return true;
            }

            // 4. Đơn điều trị / Đơn thuốc
            if (req.SERVICE_REQ_TYPE_ID == 6 || req.SERVICE_REQ_TYPE_ID == 7 ||
                (req.SERVICE_REQ_TYPE_NAME ?? "").ToLower().Contains("thuốc") ||
                (req.SERVICE_REQ_TYPE_NAME ?? "").ToLower().Contains("đơn"))
            {
                reason = string.Format("Đơn điều trị / Đơn thuốc ({0})", req.SERVICE_REQ_TYPE_NAME);
                return true;
            }

            // Kiểm tra qua dịch vụ con (Đồ vải, ĐMMM, Giường, Thuốc/vật tư)
            if (ssList != null && ssList.Count > 0)
            {
                foreach (var s in ssList)
                {
                    string sName = (s.TDL_SERVICE_NAME ?? "").ToLower();
                    string sCode = (s.TDL_SERVICE_CODE ?? "").ToUpper();

                    // 1. Dịch vụ Giường
                    if (s.TDL_SERVICE_TYPE_ID == 8 || sName.Contains("giường"))
                    {
                        reason = string.Format("Dịch vụ Giường bệnh ({0})", s.TDL_SERVICE_NAME);
                        return true;
                    }

                    // 2. Đồ vải
                    if (sName.Contains("toan") || sName.Contains("áo") || sName.Contains("vải") ||
                        sName.Contains("do vai") || sName.Contains("gói pt") || sName.Contains("giảm trừ"))
                    {
                        reason = string.Format("Dịch vụ Đồ vải / Toan áo phẫu thuật ({0})", s.TDL_SERVICE_NAME);
                        return true;
                    }

                    // 3. ĐMMM tại giường
                    if (sCode == "BM02426" || sCode == "NB260620.6231" ||
                        sName.Contains("mao mạch") || sName.Contains("đường huyết") ||
                        sName.Contains("glucose [máu] mao mạch") || sName.Contains("dmmm"))
                    {
                        reason = string.Format("Định lượng Glucose mao mạch tại giường ({0} - {1})", sCode, s.TDL_SERVICE_NAME);
                        return true;
                    }

                    // 4. Thuốc / Dược / Máu / Vật tư
                    if (s.MEDICINE_ID.HasValue || s.MATERIAL_ID.HasValue || s.BLOOD_ID.HasValue ||
                        s.TDL_SERVICE_TYPE_ID == 6 || s.TDL_SERVICE_TYPE_ID == 7)
                    {
                        reason = string.Format("Dịch vụ Thuốc / Vật tư / Máu ({0})", s.TDL_SERVICE_NAME);
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Kỹ năng 3: Tạo vỏ bệnh án Ngoại khoa EMR Nội trú (HisEmrFiller.exe)

        private static bool EnsureEmrCover(V_HIS_TREATMENT tr, string facility, bool isDryRun, StringBuilder sb)
        {
            sb.AppendLine("===============================================================================");
            sb.AppendLine("📋 BƯỚC 3: TẠO BÌA TÓM TẮT, BÌA TỔNG KẾT CUỐI & BÌA KHÁM NGOẠI KHOA EMR");
            sb.AppendLine("===============================================================================");

            if (tr.TDL_TREATMENT_TYPE_ID != 3)
            {
                sb.AppendLine(string.Format("⚠️ [BỎ QUA BƯỚC 3] Bệnh nhân là diện NGOẠI TRÚ (TreatmentType: {0}). Vỏ EMR chỉ áp dụng cho Nội trú (Type 3).", tr.TDL_TREATMENT_TYPE_ID));
                return true;
            }

            string emrExe = ResolveToolPath("HisEmrFiller.exe");
            var sbArgs = new StringBuilder();
            sbArgs.Append(EscapeCliArg(tr.TDL_PATIENT_CODE));
            if (isDryRun) sbArgs.Append(" --dry-run");
            else sbArgs.Append(" --save");
            sbArgs.Append(" --force-summary");
            string doc = (facility == "NB") ? "vmc" : "034727";
            sbArgs.Append(" --doctor " + doc);
            sbArgs.Append(" --facility " + facility);

            bool emrErr;
            string emrOutput = RunExternalProcess(emrExe, sbArgs.ToString(), out emrErr);
            sb.AppendLine(emrOutput);

            return !emrErr;
        }

        #endregion

        #region Session & Helper Methods

        private static bool InitSession(string facility, StringBuilder sb)
        {
            bool isNB = (facility != null && facility.Trim().ToUpper() == "NB");
            if (isNB && mosConsumerNB != null)
            {
                mosConsumer = mosConsumerNB;
                return true;
            }
            if (!isNB && mosConsumerHN != null)
            {
                mosConsumer = mosConsumerHN;
                return true;
            }

            string token = ReadLiveTokenForFacility(isNB ? "NB" : "HN");
            if (string.IsNullOrEmpty(token))
            {
                try
                {
                    ClientTokenManager tm = new ClientTokenManager("HIS");
                    CommonParam p = new CommonParam();
                    if (isNB)
                    {
                        var tok = tm.Login(p, "vmc", "789789", "2.390.0");
                        if (tok != null) token = tok.TokenCode;
                    }
                    else
                    {
                        var tok = tm.Login(p, "034727", "998199", "2.390.0");
                        if (tok != null) token = tok.TokenCode;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(token))
            {
                sb.AppendLine(string.Format("❌ Không tìm thấy TokenCode hợp lệ cho cơ sở {0}! Vui lòng đăng nhập phần mềm HIS Inventec ({1}).",
                    isNB ? "Ninh Bình" : "Hà Nội", isNB ? "vmc" : "034727"));
                return false;
            }

            mosConsumer = new ApiConsumer(MOS_BASE, token, "HIS");
            if (isNB) mosConsumerNB = mosConsumer;
            else mosConsumerHN = mosConsumer;

            return true;
        }

        private static string ReadLiveTokenForFacility(string facility)
        {
            bool isNB = (facility != null && facility.Trim().ToUpper() == "NB");
            string targetDoc = isNB ? "vmc" : "034727";

            // 1. Explicit env var override
            string envFile = Environment.GetEnvironmentVariable("HIS_TOKEN_FILE");
            if (!string.IsNullOrEmpty(envFile))
            {
                string path = Path.IsPathRooted(envFile) ? envFile : Path.Combine(BaseDir, envFile);
                string tok = ReadTokenFromPath(path);
                if (!string.IsNullOrEmpty(tok)) return tok;
            }

            // 2. Specialized token file for facility
            string specFile = Path.Combine(BaseDir, isNB ? "doctor_nb.token" : "doctor_hn.token");
            string specTok = ReadTokenFromPath(specFile);
            if (!string.IsNullOrEmpty(specTok)) return specTok;

            // 3. Fallback standalone token if matching target doctor
            string standFile = Path.Combine(BaseDir, "doctor_standalone.token");
            try
            {
                if (File.Exists(standFile))
                {
                    string content = File.ReadAllText(standFile, Encoding.UTF8).Trim();
                    var parts = content.Split('|');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && parts[0].Length == 64)
                    {
                        string docInFile = parts.Length >= 3 ? parts[2] : "";
                        if (string.IsNullOrEmpty(docInFile) || string.Equals(docInFile, targetDoc, StringComparison.OrdinalIgnoreCase))
                        {
                            long ticks;
                            if (long.TryParse(parts[1], out ticks) && (DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromHours(6).Ticks)
                            {
                                return parts[0];
                            }
                        }
                    }
                }
            }
            catch { }

            // 4. Scan LogSystem.txt tail 128KB filtered by doctor identity
            List<string> logCandidates = new List<string>
            {
                Path.Combine(BaseDir, "Logs", "LogSystem.txt"),
                Path.Combine(BaseDir, "Logs", "HLSLogSystem.txt")
            };

            DirectoryInfo cur = new DirectoryInfo(BaseDir);
            for (int i = 0; i < 4; i++)
            {
                if (cur == null) break;
                logCandidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
                cur = cur.Parent;
            }

            foreach (var lp in logCandidates)
            {
                if (!File.Exists(lp)) continue;
                try
                {
                    using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        long length = fs.Length;
                        if (length == 0) continue;
                        int bufferSize = (int)Math.Min(131072L, length);
                        fs.Seek(length - bufferSize, SeekOrigin.Begin);
                        byte[] buffer = new byte[bufferSize];
                        int read = fs.Read(buffer, 0, bufferSize);
                        string chunk = Encoding.UTF8.GetString(buffer, 0, read);

                        if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;
                        int idx = chunk.LastIndexOf("TokenCode|");
                        if (idx >= 0)
                        {
                            int start = idx + 10;
                            if (chunk.Length >= start + 64)
                            {
                                string candidateToken = chunk.Substring(start, 64);
                                if (chunk.IndexOf(targetDoc, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    try
                                    {
                                        File.WriteAllText(specFile, candidateToken + "|" + DateTime.UtcNow.Ticks + "|" + targetDoc, Encoding.UTF8);
                                    }
                                    catch { }
                                    return candidateToken;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string ReadTokenFromPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path, Encoding.UTF8).Trim();
                    var parts = content.Split('|');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && parts[0].Length == 64)
                    {
                        long ticks;
                        if (long.TryParse(parts[1], out ticks) && (DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromHours(6).Ticks)
                        {
                            return parts[0];
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static void EnsureWorkInfoForRoom(long roomId)
        {
            try
            {
                var workInfo = new WorkInfoSDO { Rooms = new List<RoomSDO> { new RoomSDO { RoomId = roomId } } };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            }
            catch { }
        }

        private static V_HIS_TREATMENT FindTreatmentByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            string kw = keyword.Trim();
            List<V_HIS_TREATMENT> treatments = null;
            long numVal;
            bool isNum = long.TryParse(kw, out numVal);

            if (isNum)
            {
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
                tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
                treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter();
                    tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                    treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }
            }
            else
            {
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
                treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                    treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }
            }

            if (treatments == null || treatments.Count == 0)
            {
                long tId;
                if (long.TryParse(kw, out tId) && tId > 100000)
                {
                    HisTreatmentViewFilter tfId = new HisTreatmentViewFilter { ID = tId };
                    treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfId, param);
                }
            }

            if (treatments == null || treatments.Count == 0)
            {
                HisTreatmentViewFilter tfKw = new HisTreatmentViewFilter { KEY_WORD = kw };
                treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfKw, param);
            }

            if (treatments == null || treatments.Count == 0) return null;
            return treatments.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
        }

        private static string ResolveToolPath(string exeName)
        {
            string path1 = Path.Combine(BaseDir, exeName);
            if (File.Exists(path1)) return path1;

            DirectoryInfo cur = new DirectoryInfo(BaseDir);
            for (int i = 0; i < 4; i++)
            {
                if (cur == null) break;
                string p = Path.Combine(cur.FullName, exeName);
                if (File.Exists(p)) return p;
                cur = cur.Parent;
            }

            return exeName;
        }

        private static string RunExternalProcess(string exePath, string arguments, out bool isError)
        {
            isError = false;
            try
            {
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

                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        isError = true;
                        return "Không thể khởi động tiến trình: " + exePath;
                    }

                    string stdOut = proc.StandardOutput.ReadToEnd();
                    string stdErr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0) isError = true;

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
                return "Lỗi khi chạy " + exePath + ": " + ex.Message;
            }
        }

        private static string EscapeCliArg(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            if (!arg.Contains(" ") && !arg.Contains("\"")) return arg;
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
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
                if (v is JToken) obj[k] = (JToken)v;
                else if (v == null) obj[k] = null;
                else obj[k] = JToken.FromObject(v);
            }
            return obj;
        }

        public static JArray Arr(params object[] items)
        {
            var arr = new JArray();
            foreach (var it in items)
            {
                if (it is JToken) arr.Add((JToken)it);
                else if (it != null) arr.Add(JToken.FromObject(it));
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
