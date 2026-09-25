using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HisGlucoseMcp
{
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
            return GlucoseMcpService.Run(args);
        }
    }

    public class GlucoseMcpService
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        public static int Run(string[] args)
        {
            // 1. Chế độ CLI Direct
            if (args.Length > 0 && args[0] != "--mcp" && args[0] != "-mcp")
            {
                return RunCli(args);
            }

            // 2. Chế độ MCP Server (JSON-RPC 2.0 qua Stdio)
            RunMcpServer();
            return 0;
        }

        #region CLI Direct Execution

        private static int RunCli(string[] args)
        {
            string cmd = args[0].ToLower().Trim();

            if (cmd == "assign" || cmd == "dmmm" || cmd == "chi-dinh")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Cú pháp: HisGlucoseMcpServer.exe assign <patientCodes> [HN|NB] [slot]");
                    return 1;
                }
                string pCodes = args[1];
                string facility = args.Length > 2 ? args[2] : "HN";
                string assignSlot = args.Length > 3 ? args[3] : "";

                var jArgs = new JObject();
                jArgs["patientCodes"] = pCodes;
                jArgs["facility"] = facility;
                jArgs["timeSlot"] = assignSlot;

                bool isError;
                string res = ExecuteAssignBedsideGlucose(jArgs, out isError);
                Console.WriteLine(res);
                return isError ? 1 : 0;
            }

            // Nhánh chạy Protocol Thợ cho đường huyết
            string pCode = "";
            double glucose = 0;
            string insulinType = "R";
            int units = 0;
            string slot = "17h";
            string fac = "HN";
            bool dryRun = false;

            int startIndex = 0;
            if (cmd == "protocol" || cmd == "tho-cho-duong-huyet" || cmd == "run")
            {
                startIndex = 1;
            }

            if (args.Length > startIndex) pCode = args[startIndex];
            if (args.Length > startIndex + 1)
            {
                double.TryParse(args[startIndex + 1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out glucose);
            }
            if (args.Length > startIndex + 2) insulinType = args[startIndex + 2];
            if (args.Length > startIndex + 3) int.TryParse(args[startIndex + 3], out units);
            if (args.Length > startIndex + 4) slot = args[startIndex + 4];

            for (int i = startIndex + 5; i < args.Length; i++)
            {
                string a = args[i].ToUpper();
                if (a == "HN" || a == "NB") fac = a;
                else if (a == "--DRY-RUN" || a == "-N" || a == "--PREVIEW") dryRun = true;
            }

            if (string.IsNullOrEmpty(pCode) || units <= 0)
            {
                Console.WriteLine("===============================================================================");
                Console.WriteLine("🩸 HIS GLUCOSE MCP SERVER - ĐẶC QUYỀN 'THỢ CHO ĐƯỜNG HUYẾT' CLI");
                Console.WriteLine("===============================================================================");
                Console.WriteLine("Cú pháp Protocol (3 bước 1-Click):");
                Console.WriteLine("  HisGlucoseMcpServer.exe <patientCode> <glucoseValue> <insulinType> <units> <timeSlot> [HN|NB] [--dry-run]");
                Console.WriteLine("  Ví dụ: HisGlucoseMcpServer.exe 0004018669 11.4 R 6 17h NB");
                Console.WriteLine("\nCú pháp Chỉ định ĐMMM lẻ:");
                Console.WriteLine("  HisGlucoseMcpServer.exe assign <patientCodes> [HN|NB] [timeSlot]");
                Console.WriteLine("  Ví dụ: HisGlucoseMcpServer.exe assign 0004018669,0004018670 NB 17h");
                Console.WriteLine("===============================================================================");
                return 1;
            }

            var pArgs = new JObject();
            pArgs["patientCode"] = pCode;
            pArgs["glucoseValue"] = glucose;
            pArgs["insulinType"] = insulinType;
            pArgs["units"] = units;
            pArgs["timeSlot"] = slot;
            pArgs["facility"] = fac;
            pArgs["dryRun"] = dryRun;

            bool err;
            string outStr = ExecuteProtocolGlucose(pArgs, out err);
            Console.WriteLine(outStr);
            return err ? 1 : 0;
        }

        #endregion

        #region MCP JSON-RPC 2.0 Loop

        private static void RunMcpServer()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var req = JObject.Parse(line);
                    string method = req["method"] != null ? req["method"].ToString() : "";
                    object id = req["id"] != null ? ((JValue)req["id"]).Value : null;

                    switch (method)
                    {
                        case "initialize":
                            HandleInitialize(id);
                            break;
                        case "notifications/initialized":
                            break;
                        case "tools/list":
                            HandleToolsList(id);
                            break;
                        case "tools/call":
                            HandleToolsCall(id, req["params"] as JObject);
                            break;
                        default:
                            SendError(id, -32601, "Method not found: " + method);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    SendError(null, -32700, "Parse error: " + ex.Message);
                }
            }
        }

        private static void HandleInitialize(object id)
        {
            var res = Obj(
                "protocolVersion", "2024-11-05",
                "capabilities", Obj("tools", Obj()),
                "serverInfo", Obj("name", "his-glucose", "version", "1.0.0")
            );
            SendResponse(id, res);
        }

        private static void HandleToolsList(object id)
        {
            var tools = new JArray();

            // 1. his_glucose_hn (Chuyên biệt Cơ sở Hà Nội)
            tools.Add(CreateTool(
                "his_glucose_hn",
                "Dac quyen 'Tho cho duong huyet' CHUYEN BIET CO SO HA NOI (Khoa 57 CTCH, DMMM BM02426 phong 5248/931, Tu truc 810, BS Nguyen Huu Sam 034727, token doctor_hn.token): Tu dong thuc thi tuan tu 3 buoc (To dieu tri -> Chi dinh DMMM tai giuong -> Ke don Insulin tu truc lech +5 phut)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "glucoseValue", Obj("type", "number", "description", "Ket qua duong huyet (mmol/L, VD: 11.4)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "units", Obj("type", "integer", "description", "So don vi Insulin (UI, VD: 6, 8, 10)"),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17h', '21h', '6h'", "enum", Arr("17h", "21h", "6h")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - ghi that)")
                ),
                Arr("patientCode", "glucoseValue", "insulinType", "units", "timeSlot")
            ));

            // 2. his_glucose_nb (Chuyên biệt Cơ sở Ninh Bình)
            tools.Add(CreateTool(
                "his_glucose_nb",
                "Dac quyen 'Tho cho duong huyet' CHUYEN BIET CO SO NINH BINH (Khoa 915 Ngoai TH, DMMM NB260620.6231 phong 18679/18681, Tu truc 5142, Ths.BS Nguyen Huu Sam 034727 mac dinh moi co so, token doctor_nb.token): Tu dong thuc thi tuan tu 3 buoc (To dieu tri -> Chi dinh DMMM tai giuong -> Ke don Insulin tu truc lech +5 phut)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "glucoseValue", Obj("type", "number", "description", "Ket qua duong huyet (mmol/L, VD: 11.4)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "units", Obj("type", "integer", "description", "So don vi Insulin (UI, VD: 6, 8, 10)"),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17h', '21h', '6h'", "enum", Arr("17h", "21h", "6h")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - ghi that)")
                ),
                Arr("patientCode", "glucoseValue", "insulinType", "units", "timeSlot")
            ));

            // 3. his_execute_protocol_glucose (Chính thức - Tự động điều hướng)
            tools.Add(CreateTool(
                "his_execute_protocol_glucose",
                "Dac quyen 'Tho cho duong huyet' (1-Click Protocol - Tu dong dieu huong HN/NB): Tu dong thuc thi tuan tu 3 buoc (To dieu tri -> Chi dinh DMMM tai giuong -> Ke don Insulin tu truc lech +5 phut)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "glucoseValue", Obj("type", "number", "description", "Ket qua duong huyet (mmol/L, VD: 11.4)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R' (Actrapid), 'L' (Lantus), 'M' (Mixtard)", "enum", Arr("R", "L", "M")),
                    "units", Obj("type", "integer", "description", "So don vi Insulin (UI, VD: 6, 8, 10)"),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17h', '21h', '6h'", "enum", Arr("17h", "21h", "6h")),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - ghi that)")
                ),
                Arr("patientCode", "glucoseValue", "insulinType", "units", "timeSlot")
            ));

            // 4. his_tho_cho_duong_huyet (Bi danh)
            tools.Add(CreateTool(
                "his_tho_cho_duong_huyet",
                "Bi danh cua 'Tho cho duong huyet' (1-Click Protocol): Tu dong thuc thi tuan tu 3 buoc (To dieu tri -> Chi dinh DMMM tai giuong -> Ke don Insulin tu truc lech +5 phut)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "glucoseValue", Obj("type", "number", "description", "Ket qua duong huyet (mmol/L, VD: 11.4)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R', 'L', 'M'", "enum", Arr("R", "L", "M")),
                    "units", Obj("type", "integer", "description", "So don vi Insulin (UI, VD: 6, 8, 10)"),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17h', '21h', '6h'", "enum", Arr("17h", "21h", "6h")),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "dryRun", Obj("type", "boolean", "description", "Che do chay thu kiem tra truoc (mac dinh false - ghi that)")
                ),
                Arr("patientCode", "glucoseValue", "insulinType", "units", "timeSlot")
            ));

            // 5. his_assign_bedside_glucose
            tools.Add(CreateTool(
                "his_assign_bedside_glucose",
                "Chi dinh Dinh luong Glucose mau mao mach tai giuong (DMMM). HN: BM02426 (ID 6217, phong 5248/931) / NB: NB260620.6231 (ID 74281, phong 18679/18681)",
                Obj(
                    "patientCodes", Obj("type", "string", "description", "Danh sach ma benh nhan cach nhau boi dau phay"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB' (tu dong nhan dien neu de trong)", "enum", Arr("HN", "NB")),
                    "timeSlot", Obj("type", "string", "description", "Moc thoi gian: '17h', '21h', '6h'")
                ),
                Arr("patientCodes")
            ));

            var res = Obj("tools", tools);
            SendResponse(id, res);
        }

        private static void HandleToolsCall(object id, JObject paramsObj)
        {
            if (paramsObj == null)
            {
                SendError(id, -32602, "Invalid params");
                return;
            }

            string toolName = paramsObj["name"] != null ? paramsObj["name"].ToString() : "";
            var args = paramsObj["arguments"] as JObject ?? new JObject();

            bool isError = false;
            string output = "";

            try
            {
                switch (toolName)
                {
                    case "his_glucose_hn":
                    case "glucose_hn":
                        args["facility"] = "HN";
                        output = ExecuteProtocolGlucose(args, out isError);
                        break;
                    case "his_glucose_nb":
                    case "glucose_nb":
                        args["facility"] = "NB";
                        output = ExecuteProtocolGlucose(args, out isError);
                        break;
                    case "his_execute_protocol_glucose":
                    case "his_tho_cho_duong_huyet":
                    case "tho_cho_duong_huyet":
                        output = ExecuteProtocolGlucose(args, out isError);
                        break;
                    case "his_assign_bedside_glucose":
                        output = ExecuteAssignBedsideGlucose(args, out isError);
                        break;
                    default:
                        SendError(id, -32602, "Unknown tool: " + toolName);
                        return;
                }
            }
            catch (Exception ex)
            {
                isError = true;
                output = "Exception: " + ex.Message + "\n" + ex.StackTrace;
            }

            var contentItem = Obj("type", "text", "text", output);
            var content = new JArray { contentItem };
            var res = Obj("content", content, "isError", isError);
            SendResponse(id, res);
        }

        #endregion

        #region Protocol & Service Implementations

        private static string FormatTimeSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return "06:00";
            string s = slot.Trim().ToLower().Replace("h", "").Replace(":", "");
            if (s == "17" || s == "1700") return "17:00";
            if (s == "21" || s == "2100") return "21:00";
            if (s == "6" || s == "06" || s == "0600") return "06:00";
            if (s == "11" || s == "1100") return "11:00";
            return slot.Contains(":") ? slot : slot + ":00";
        }

        private static string ExecuteAssignBedsideGlucose(JObject args, out bool isError)
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(pCodes, args);
            }
            string slot = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Lỗi: patientCodes không được để trống.";
            }

            string tool = ResolveToolPath("HisGlucoseBedsideAssigner.exe");
            string formattedSlot = FormatTimeSlot(slot);
            string facArg = string.Equals(facility, "NB", StringComparison.OrdinalIgnoreCase) ? "nb" : "hn";
            string cmdArgs = string.Format("-p {0} -fac {1} -time {2}", EscapeArg(pCodes), EscapeArg(facArg), EscapeArg(formattedSlot));

            return RunProcess(tool, cmdArgs, out isError, facility);
        }

        private static string ExecuteProtocolGlucose(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            double glucose = args["glucoseValue"] != null ? (double)args["glucoseValue"] : 0;
            string insulinType = args["insulinType"] != null ? args["insulinType"].ToString().Trim().ToUpper() : "R";
            int units = args["units"] != null ? (int)args["units"] : 0;
            string slot = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim().ToLower() : "17h";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(facility))
            {
                facility = DetectFacilityFromContext(pCode, args);
            }
            bool dryRun = args["dryRun"] != null && (bool)args["dryRun"];

            if (string.IsNullOrEmpty(pCode) || units <= 0)
            {
                isError = true;
                return "Lỗi: patientCode không được để trống và units phải > 0.";
            }

            string insulinName = (insulinType == "R" ? "Actrapid" : (insulinType == "L" ? "Lantus" : "Mixtard"));
            string envDoc = Environment.GetEnvironmentVariable("HIS_DOCTOR_LOGIN");
            string docLogin = !string.IsNullOrEmpty(envDoc) ? envDoc : "034727";
            var sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine(string.Format("🩸 THỰC THI PROTOCOL 'THỢ CHO ĐƯỜNG HUYẾT' - CƠ SỞ {0} ({1} - BS: {2})",
                facility, facility == "NB" ? "NINH BÌNH (Khoa 915)" : "HÀ NỘI (Khoa 57)", docLogin));
            sb.AppendLine(string.Format("• Bệnh nhân   : {0}", pCode));
            sb.AppendLine(string.Format("• Mốc giờ     : {0}", slot));
            sb.AppendLine(string.Format("• Đường huyết : {0} mmol/L", glucose));
            sb.AppendLine(string.Format("• Y lệnh tiêm : {0} UI {1} ({2})", units, insulinName, insulinType));
            sb.AppendLine(string.Format("• Chế độ      : {0}", dryRun ? "[DRY-RUN] Xem trước" : "THỰC THI CHÍNH THỨC"));
            sb.AppendLine("===============================================================================");

            if (dryRun)
            {
                sb.AppendLine("\n[XEM TRƯỚC BƯỚC 1] Tạo Tờ điều trị:");
                sb.AppendLine(string.Format("  HisTrackingCreator.exe {0} --note \"Ket qua DMMM {1}: {2} mmol/L. Y lenh: Tiem {3} UI {4}.\"",
                    pCode, slot, glucose, units, insulinName));

                sb.AppendLine("\n[XEM TRƯỚC BƯỚC 2] Chỉ định ĐMMM tại giường:");
                sb.AppendLine(string.Format("  HisGlucoseBedsideAssigner.exe -p {0} -fac {1} -time {2}",
                    pCode, (facility == "NB" ? "nb" : "hn"), FormatTimeSlot(slot)));

                sb.AppendLine("\n[XEM TRƯỚC BƯỚC 3] Kê đơn Insulin tủ trực (+5 phút):");
                long sId = (facility == "NB") ? 5142 : 810;
                string tStr = (slot == "17h") ? "17:05" : (slot == "21h" ? "21:05" : "06:05");
                sb.AppendLine(string.Format("  HisCabinetPrescribe.exe insulin {0} {1} {2} {3} {4}",
                    pCode, units, insulinType, tStr, sId));

                sb.AppendLine("\n===============================================================================");
                sb.AppendLine("🔎 [DRY-RUN] Kiểm tra hoàn tất. Bỏ cờ --dry-run để thực thi thật.");
                sb.AppendLine("===============================================================================");
                isError = false;
                return sb.ToString();
            }

            // BƯỚC 1: Tạo Tờ điều trị
            sb.AppendLine("\n--- BƯỚC 1: TẠO TỜ ĐIỀU TRỊ ---");
            string note = string.Format("Ket qua DMMM {0}: {1} mmol/L. Y lenh: Tiem {2} UI {3}.",
                slot, glucose, units, insulinName);

            bool step1Error;
            string toolTracking = ResolveToolPath("HisTrackingCreator.exe");
            string formattedSlot = FormatTimeSlot(slot);
            string trackingArgs = string.Format("-p {0} -time {1} -content {2} -med {3} -care {4} -u {5}",
                EscapeArg(pCode), EscapeArg(formattedSlot),
                EscapeArg("Khám: Đường máu mao mạch lúc " + formattedSlot + ": " + glucose + " mmol/L."),
                EscapeArg(string.Format("Tiêm dưới da {0} UI {1} lúc {2}.", units, insulinName, formattedSlot)),
                EscapeArg("Chăm sóc cấp II. Theo dõi đường máu mao mạch."),
                EscapeArg(docLogin));
            string res1 = RunProcess(toolTracking, trackingArgs, out step1Error, facility);
            sb.AppendLine(res1);

            // BƯỚC 2: Chỉ định CLS DMMM
            sb.AppendLine("\n--- BƯỚC 2: CHỈ ĐỊNH ĐMMM TẠI GIƯỜNG ---");
            bool step2Error;
            string toolGlucose = ResolveToolPath("HisGlucoseBedsideAssigner.exe");
            string facArg = string.Equals(facility, "NB", StringComparison.OrdinalIgnoreCase) ? "nb" : "hn";
            string res2 = RunProcess(toolGlucose, string.Format("-p {0} -fac {1} -time {2}", EscapeArg(pCode), EscapeArg(facArg), EscapeArg(formattedSlot)), out step2Error, facility);
            sb.AppendLine(res2);

            // BƯỚC 3: Kê đơn Insulin tủ trực (810 tại HN / 5142 tại NB) lệch +5 phút
            sb.AppendLine("\n--- BƯỚC 3: KÊ ĐƠN INSULIN TỦ TRỰC (+5 PHÚT OFFSET) ---");
            bool step3Error;
            long stockId = (facility == "NB") ? 5142 : 810;
            string toolPrescribe = ResolveToolPath("HisCabinetPrescribe.exe");
            string timeStr = (slot == "17h") ? "17:05" : (slot == "21h" ? "21:05" : "06:05");
            string res3 = RunProcess(toolPrescribe, string.Format("insulin {0} {1} {2} {3} {4}",
                EscapeArg(pCode), units, EscapeArg(insulinType), EscapeArg(timeStr), stockId), out step3Error, facility);
            sb.AppendLine(res3);

            // CIRCUIT-BREAKER: Bất kỳ bước nào lỗi là báo lỗi (OR)
            isError = step1Error || step2Error || step3Error;

            sb.AppendLine("\n===============================================================================");
            if (!isError)
            {
                sb.AppendLine("🎉 HOÀN TẤT TOÀN DIỆN PROTOCOL 'THỢ CHO ĐƯỜNG HUYẾT' THÀNH CÔNG RỰC RỠ!");
            }
            else
            {
                sb.AppendLine("⚠️ PROTOCOL KẾT THÚC VỚI CẢNH BÁO/LỖI Ở MỘT SỐ BƯỚC. VUI LÒNG KIỂM TRA LẠI CHI TIẾT TRÊN!");
            }
            sb.AppendLine("===============================================================================");
            return sb.ToString();
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
                kw.IndexOf("5142", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "NB";
            }
            return "HN";
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

            // Tier 3: Check in parent directory if in script dir
            if (BaseDir.Contains(".agents"))
            {
                DirectoryInfo cur = new DirectoryInfo(BaseDir);
                for (int i = 0; i < 5; i++)
                {
                    if (cur == null) break;
                    string p = Path.Combine(cur.FullName, exeName);
                    if (File.Exists(p)) return p;
                    cur = cur.Parent;
                }
            }

            return exeName;
        }

        private static string RunProcess(string exePath, string arguments, out bool isError, string facility = "HN")
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

                bool isNB = string.Equals(facility, "NB", StringComparison.OrdinalIgnoreCase);
                string docLogin = Environment.GetEnvironmentVariable("HIS_DOCTOR_LOGIN");
                if (string.IsNullOrEmpty(docLogin)) docLogin = "034727";
                string docPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
                if (string.IsNullOrEmpty(docPass)) docPass = Environment.GetEnvironmentVariable("HIS_PASS");
                if (string.IsNullOrEmpty(docPass)) docPass = "981";

                string targetTokenFile = isNB ? "doctor_nb.token" : "doctor_hn.token";
                if (!File.Exists(Path.Combine(BaseDir, targetTokenFile)) && File.Exists(Path.Combine(BaseDir, "doctor_standalone.token")))
                {
                    targetTokenFile = "doctor_standalone.token";
                }

                psi.EnvironmentVariables["HIS_FACILITY"] = isNB ? "NB" : "HN";
                psi.EnvironmentVariables["HIS_TOKEN_FILE"] = targetTokenFile;
                psi.EnvironmentVariables["HIS_DOCTOR_LOGIN"] = docLogin;
                psi.EnvironmentVariables["HIS_PASSWORD"] = docPass;

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
                return "Lỗi khi chạy " + exePath + ": " + ex.Message;
            }
        }

        private static string EscapeArg(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            if (!arg.Contains(" ") && !arg.Contains("\"")) return arg;
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        #endregion

        #region JSON Helpers

        private static JObject CreateTool(string name, string description, JObject properties, JArray required)
        {
            var schema = Obj("type", "object", "properties", properties);
            if (required != null && required.Count > 0) schema["required"] = required;
            schema["additionalProperties"] = false;
            return Obj("name", name, "description", description, "inputSchema", schema);
        }

        private static JObject Obj(params object[] keyValues)
        {
            var obj = new JObject();
            for (int i = 0; i < keyValues.Length; i += 2)
            {
                string key = keyValues[i].ToString();
                object val = keyValues[i + 1];
                if (val is JToken) obj[key] = (JToken)val;
                else if (val == null) obj[key] = JValue.CreateNull();
                else obj[key] = new JValue(val);
            }
            return obj;
        }

        private static JArray Arr(params object[] values)
        {
            var arr = new JArray();
            foreach (var v in values)
            {
                if (v is JToken) arr.Add((JToken)v);
                else arr.Add(new JValue(v));
            }
            return arr;
        }

        private static void SendResponse(object id, JObject result)
        {
            var res = Obj("jsonrpc", "2.0", "result", result);
            if (id != null) res["id"] = new JValue(id);
            else res["id"] = JValue.CreateNull();
            Console.WriteLine(res.ToString(Formatting.None));
        }

        private static void SendError(object id, int code, string message)
        {
            var err = Obj("code", code, "message", message);
            var res = Obj("jsonrpc", "2.0", "error", err);
            if (id != null) res["id"] = new JValue(id);
            else res["id"] = JValue.CreateNull();
            Console.WriteLine(res.ToString(Formatting.None));
        }

        #endregion
    }
}
