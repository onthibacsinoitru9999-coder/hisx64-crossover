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

            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(BaseDir) || !Directory.Exists(BaseDir))
            {
                BaseDir = Directory.GetCurrentDirectory();
            }

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
                    "instructionTime", Obj("type", "string", "description", "Thoi gian y lenh YYYYMMDDHHmmss (de trong lay gio hien tai)")
                ),
                Arr("patientCode")
            ));

            // 7. his_prescribe_medicine
            tools.Add(CreateTool(
                "his_prescribe_medicine",
                "Ke don thuoc tu Tu truc (HN: Kho 810 / NB: Kho 5142) hoac Kho Duoc. Ho tro don tiem Insulin va thuoc vien/truyen",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "stockId", Obj("type", "integer", "description", "Ma kho/tu truc (HN: 810; NB: 5142). Mac dinh 810"),
                    "medicines", Obj("type", "string", "description", "Mo ta thuoc hoac danh sach thuoc (VD: 'Actrapid 8UI tiem duoi da')"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCode", "medicines")
            ));

            // 8. his_assign_bedside_glucose
            tools.Add(CreateTool(
                "his_assign_bedside_glucose",
                "Chi dinh Dinh luong Glucose mau mao mach tai giuong (DMMM). HN: BM02426 (ID 6217) / NB: NB260620.6231 (ID 74281)",
                Obj(
                    "patientCodes", Obj("type", "string", "description", "Danh sach ma benh nhan cach nhau boi dau phay"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB")),
                    "timeSlot", Obj("type", "string", "description", "Moc thoi gian: '17h', '21h', '6h'")
                ),
                Arr("patientCodes")
            ));

            // 9. his_assign_ration
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

            // 10. his_assign_leanpro
            tools.Add(CreateTool(
                "his_assign_leanpro",
                "Chi dinh Dinh duong truoc mo (Leanpro PreSur). Tu dong chan benh nhan >= 70 tuoi hoac Dai thao duong",
                Obj(
                    "patientCodes", Obj("type", "string", "description", "Danh sach ma benh nhan cach nhau boi dau phay"),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCodes")
            ));

            // 11. his_execute_protocol_glucose
            tools.Add(CreateTool(
                "his_execute_protocol_glucose",
                "Dac quyen 'Tho cho duong huyet' (1-Click Protocol): Tu dong thuc thi tuan tu 3 buoc (To dieu tri -> Chi dinh DMMM -> Ke don Insulin lech +5 phut)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan hoac ma dieu tri"),
                    "glucoseValue", Obj("type", "number", "description", "Ket qua duong huyet (mmol/L, VD: 11.4)"),
                    "insulinType", Obj("type", "string", "description", "Loai Insulin: 'R', 'L', 'M'", "enum", Arr("R", "L", "M")),
                    "units", Obj("type", "integer", "description", "So don vi Insulin (UI, VD: 6, 8)"),
                    "timeSlot", Obj("type", "string", "description", "Moc gio: '17h', '21h', '6h'", "enum", Arr("17h", "21h", "6h")),
                    "facility", Obj("type", "string", "description", "Co so: 'HN' hoac 'NB'", "enum", Arr("HN", "NB"))
                ),
                Arr("patientCode", "glucoseValue", "insulinType", "units", "timeSlot")
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
                "Dien Vo Benh An Ngoai Khoa EMR (CHI AP DUNG BENH NHAN NOI TRU Khoa 57 / Khoa 915)",
                Obj(
                    "patientCode", Obj("type", "string", "description", "Ma benh nhan noi tru"),
                    "save", Obj("type", "boolean", "description", "Luu vao Oracle EMR (mac dinh false)")
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
                    case "his_patient_lookup":
                        output = ExecutePatientLookup(args, out isError);
                        break;
                    case "his_get_orders":
                        output = ExecuteGetOrders(args, out isError);
                        break;
                    case "his_cancel_order":
                        output = ExecuteCancelOrder(args, out isError);
                        break;
                    case "his_cancel_service":
                        output = ExecuteCancelService(args, out isError);
                        break;
                    case "his_debate_view":
                        output = ExecuteDebateView(args, out isError);
                        break;
                    case "his_create_tracking":
                        output = ExecuteCreateTracking(args, out isError);
                        break;
                    case "his_prescribe_medicine":
                        output = ExecutePrescribe(args, out isError);
                        break;
                    case "his_assign_bedside_glucose":
                        output = ExecuteAssignBedsideGlucose(args, out isError);
                        break;
                    case "his_assign_ration":
                        output = ExecuteAssignRation(args, out isError);
                        break;
                    case "his_assign_leanpro":
                        output = ExecuteAssignLeanpro(args, out isError);
                        break;
                    case "his_execute_protocol_glucose":
                        output = ExecuteProtocolGlucose(args, out isError);
                        break;
                    case "his_create_pt01":
                        output = ExecuteCreatePt01(args, out isError);
                        break;
                    case "his_view_pacs":
                        output = ExecuteViewPacs(args, out isError);
                        break;
                    case "his_debate_create":
                        output = ExecuteDebateCreate(args, out isError);
                        break;
                    case "his_emr_fill":
                        output = ExecuteEmrFill(args, out isError);
                        break;
                    case "his_system_health":
                        output = ExecuteSystemHealth(out isError);
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

        private static string ExecutePatientLookup(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "lookup " + EscapeArg(pCode), out isError);
        }

        private static string ExecuteGetOrders(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "orders " + EscapeArg(pCode), out isError);
        }

        private static string ExecuteCancelOrder(JObject args, out bool isError)
        {
            string orderId = args["orderId"] != null ? args["orderId"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(orderId))
            {
                isError = true;
                return "Loi: Tham so orderId khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "cancel-order " + EscapeArg(orderId), out isError);
        }

        private static string ExecuteCancelService(JObject args, out bool isError)
        {
            string sId = args["serviceReqMatId"] != null ? args["serviceReqMatId"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(sId))
            {
                isError = true;
                return "Loi: Tham so serviceReqMatId khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "cancel-service " + EscapeArg(sId), out isError);
        }

        private static string ExecuteDebateView(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: Tham so patientCode khong duoc de trong.";
            }

            string cliPath = ResolveToolPath("HisClinicalCli.exe");
            return RunProcess(cliPath, "debate " + EscapeArg(pCode), out isError);
        }

        private static string ExecuteCreateTracking(JObject args, out bool isError)
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
            if (args["instructionTime"] != null && !string.IsNullOrEmpty(args["instructionTime"].ToString()))
            {
                sb.Append(" --time " + EscapeArg(args["instructionTime"].ToString()));
            }

            return RunProcess(tool, sb.ToString(), out isError);
        }

        private static string ExecutePrescribe(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            string meds = args["medicines"] != null ? args["medicines"].ToString().Trim() : "";
            long stockId = args["stockId"] != null ? (long)args["stockId"] : 810;
            string facility = args["facility"] != null ? args["facility"].ToString().Trim() : "HN";

            if (string.IsNullOrEmpty(pCode) || string.IsNullOrEmpty(meds))
            {
                isError = true;
                return "Loi: patientCode va medicines khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisAutoPrescribe.exe");
            string cmdArgs = string.Format("single {0} --stock {1} --items {2} --facility {3}",
                EscapeArg(pCode), stockId, EscapeArg(meds), EscapeArg(facility));

            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteAssignBedsideGlucose(JObject args, out bool isError)
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim() : "HN";
            string slot = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim() : "";

            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Loi: patientCodes khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisGlucoseBedsideAssigner.exe");
            string cmdArgs = EscapeArg(pCodes);
            if (!string.IsNullOrEmpty(facility)) cmdArgs += " --facility " + EscapeArg(facility);
            if (!string.IsNullOrEmpty(slot)) cmdArgs += " --slot " + EscapeArg(slot);

            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteAssignRation(JObject args, out bool isError)
        {
            string room = args["room"] != null ? args["room"].ToString().Trim() : "";
            string rationType = args["rationType"] != null ? args["rationType"].ToString().Trim() : "BT01";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim() : "HN";

            if (string.IsNullOrEmpty(room))
            {
                isError = true;
                return "Loi: room khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisRationAssigner.exe");
            string cmdArgs = string.Format("{0} {1} {2}", EscapeArg(room), EscapeArg(rationType), EscapeArg(facility));
            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteAssignLeanpro(JObject args, out bool isError)
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim() : "HN";

            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Loi: patientCodes khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisLeanproAssigner.exe");
            string cmdArgs = EscapeArg(pCodes) + " " + EscapeArg(facility);
            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteProtocolGlucose(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            double glucose = args["glucoseValue"] != null ? (double)args["glucoseValue"] : 0;
            string insulinType = args["insulinType"] != null ? args["insulinType"].ToString().Trim().ToUpper() : "R";
            int units = args["units"] != null ? (int)args["units"] : 0;
            string slot = args["timeSlot"] != null ? args["timeSlot"].ToString().Trim().ToLower() : "17h";
            string facility = args["facility"] != null ? args["facility"].ToString().Trim().ToUpper() : "HN";

            if (string.IsNullOrEmpty(pCode) || units <= 0)
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong va units phai > 0.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== THUC THI PROTOCOL 'THO CHO DUONG HUYET' (1-CLICK IN-MEMORY) ===");
            sb.AppendLine(string.Format("Benh nhan: {0} | Co so: {1} | Moc: {2} | DH: {3} mmol/L | Tiem: {4}{5} UI",
                pCode, facility, slot, glucose, units, insulinType));

            // Buoc 1: Tao To dieu tri
            sb.AppendLine("\n--- BUOC 1: TAO TO DIEU TRI ---");
            string note = string.Format("Ket qua DMMM {0}: {1} mmol/L. Y lenh: Tiem {2} UI {3}.",
                slot, glucose, units, (insulinType == "R" ? "Actrapid" : (insulinType == "L" ? "Lantus" : "Mixtard")));
            
            bool step1Error;
            string toolTracking = ResolveToolPath("HisTrackingCreator.exe");
            string res1 = RunProcess(toolTracking, string.Format("{0} --note {1}", EscapeArg(pCode), EscapeArg(note)), out step1Error);
            sb.AppendLine(res1);

            // Buoc 2: Chi dinh CLS DMMM
            sb.AppendLine("\n--- BUOC 2: CHI DINH DMMM TAI GIUONG ---");
            bool step2Error;
            string toolGlucose = ResolveToolPath("HisGlucoseBedsideAssigner.exe");
            string res2 = RunProcess(toolGlucose, string.Format("{0} --facility {1} --slot {2}", EscapeArg(pCode), EscapeArg(facility), EscapeArg(slot)), out step2Error);
            sb.AppendLine(res2);

            // Buoc 3: Ke don Insulin (tu truc 810 hoac 5142)
            sb.AppendLine("\n--- BUOC 3: KE DON INSULIN TU TRUC (+5 PHUT OFFSET) ---");
            bool step3Error;
            long stockId = (facility == "NB") ? 5142 : 810;
            string insulinDesc = string.Format("{0}{1}", units, insulinType);
            string toolPrescribe = ResolveToolPath("HisAutoPrescribe.exe");
            string res3 = RunProcess(toolPrescribe, string.Format("single {0} --stock {1} --items {2} --facility {3}",
                EscapeArg(pCode), stockId, EscapeArg(insulinDesc), EscapeArg(facility)), out step3Error);
            sb.AppendLine(res3);

            isError = step1Error && step2Error && step3Error;
            sb.AppendLine("\n=== HOAN TAT PROTOCOL DUONG HUYET ===");
            return sb.ToString();
        }

        private static string ExecuteCreatePt01(JObject args, out bool isError)
        {
            string pCodes = args["patientCodes"] != null ? args["patientCodes"].ToString().Trim() : "";
            if (string.IsNullOrEmpty(pCodes))
            {
                isError = true;
                return "Loi: patientCodes khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisPt01Creator.exe");
            return RunProcess(tool, EscapeArg(pCodes), out isError);
        }

        private static string ExecuteViewPacs(JObject args, out bool isError)
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
            return RunProcess(cliPath, cmdArgs, out isError);
        }

        private static string ExecuteDebateCreate(JObject args, out bool isError)
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
            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteEmrFill(JObject args, out bool isError)
        {
            string pCode = args["patientCode"] != null ? args["patientCode"].ToString().Trim() : "";
            bool save = args["save"] != null && (bool)args["save"];

            if (string.IsNullOrEmpty(pCode))
            {
                isError = true;
                return "Loi: patientCode khong duoc de trong.";
            }

            string tool = ResolveToolPath("HisEmrFiller.exe");
            string cmdArgs = EscapeArg(pCode) + (save ? " --save" : "");
            return RunProcess(tool, cmdArgs, out isError);
        }

        private static string ExecuteSystemHealth(out bool isError)
        {
            string tool = ResolveToolPath("HisDiagnosticDoctor.exe");
            return RunProcess(tool, "health", out isError);
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

        private static string RunProcess(string exePath, string arguments, out bool isError)
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
