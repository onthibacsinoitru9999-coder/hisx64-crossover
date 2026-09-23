using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

namespace HisThoDuongHuyet
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

    public class PatientProfile
    {
        public long TreatmentId { get; set; }
        public string TreatmentCode { get; set; }
        public string PatientCode { get; set; }
        public string PatientName { get; set; }
        public string GenderName { get; set; }
        public string DobStr { get; set; }
        public long PatientTypeId { get; set; }
        public string IcdCode { get; set; }
        public string IcdName { get; set; }
        public string IcdSubCode { get; set; }
        public string IcdText { get; set; }
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public long WorkingRoomId { get; set; }
        public string BedRoomName { get; set; }
        public string BedName { get; set; }
        public long BranchId { get; set; }
        public bool IsNinhBinh { get; set; }
    }

    public class DiabetesOrderInput
    {
        public string Facility { get; set; }
        public string DoctorLogin { get; set; }
        public string DoctorPassword { get; set; }
        public string PatientCode { get; set; }
        public string TimeStr { get; set; }
        public decimal GlucoseVal { get; set; }
        public string InsulinRaw { get; set; }
        public string InsulinType { get; set; }
        public int InsulinUnits { get; set; }
        public DateTime TargetDate { get; set; }
        public bool IsDryRun { get; set; }

        public DiabetesOrderInput()
        {
            Facility = "cs2";
            DoctorLogin = "034727";
            DoctorPassword = "981";
            TimeStr = "17h";
            InsulinRaw = "0";
            InsulinType = "Actrapid";
            InsulinUnits = 0;
            TargetDate = DateTime.Today;
            IsDryRun = false;
        }
    }

    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string PatientCode { get; set; }
        public string PatientName { get; set; }
        public string BedRoomFull { get; set; }
        public string FacilityName { get; set; }
        public string GlucoseTime { get; set; }
        public decimal GlucoseVal { get; set; }
        public string BedsideServiceReqCode { get; set; }
        public long TrackingId { get; set; }
        public string InsulinTime { get; set; }
        public string InsulinDesc { get; set; }
        public string PresServiceReqCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Program
    {
        private static ApiConsumer mosConsumer;
        private static ApiConsumer sdaConsumer;
        private static MyAdapter adapter = new MyAdapter();
        private static CommonParam param = new CommonParam();
        private static string activeTokenCode = null;
        private static string currentDoctorLogin = null;
        private static string currentDoctorName = null;
        private static string activeFacility = null;

        // Facility constants
        public const long SERVICE_ID_HN_GLUCOSE = 6217;
        public const string SERVICE_CODE_HN_GLUCOSE = "BM02426";
        public const long EXECUTE_ROOM_ID_HN = 931; // Tiểu phẫu Q
        public const long WORKING_ROOM_ID_HN = 5248; // P734
        public const long MEDI_STOCK_ID_HN = 810; // TT_KCTCHCS

        public const long SERVICE_ID_NB_GLUCOSE = 74281;
        public const string SERVICE_CODE_NB_GLUCOSE = "NB260620.6231";
        public const long EXECUTE_ROOM_ID_NB_3E = 18679; // P3E-05
        public const long EXECUTE_ROOM_ID_NB_3D = 18681; // P3D-05
        public const long MEDI_STOCK_ID_NB_3E = 5142; // TTT_NBKP05.02
        public const long MEDI_STOCK_ID_NB_3D = 5141; // TTT_NBKP05.01
        public const long WORKING_ROOM_ID_NB_3E = 18679; // P3E-05

        [STAThread]
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Hook AssemblyResolve to load DLLs from ReferencedAssemblies/
            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                try
                {
                    string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                    string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
                    string p1 = Path.Combine(folderPath, name);
                    if (File.Exists(p1)) return Assembly.LoadFrom(p1);
                    string p2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
                    if (File.Exists(p2)) return Assembly.LoadFrom(p2);
                    string p3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                    if (File.Exists(p3)) return Assembly.LoadFrom(p3);
                }
                catch { }
                return null;
            };

            return RunApp(args);
        }

        private static int RunApp(string[] args)
        {
            PrintHeader();

            if (args != null && args.Length > 0)
            {
                string first = args[0].Trim().ToLower();
                if (first == "--help" || first == "-h" || first == "/?" || first == "help")
                {
                    PrintUsage();
                    return 0;
                }

                if (first == "--file" || first == "-f")
                {
                    if (args.Length < 2 || !File.Exists(args[1]))
                    {
                        Console.WriteLine("❌ Lỗi: File không tồn tại: " + (args.Length >= 2 ? args[1] : ""));
                        return 1;
                    }
                    return ProcessBatchFile(args[1]);
                }

                // Process single order from command-line arguments
                string joined = string.Join(" ", args);
                var order = ParseOrderInput(joined);
                if (order == null)
                {
                    Console.WriteLine("❌ Lỗi: Cú pháp tham số không hợp lệ!");
                    PrintUsage();
                    return 1;
                }

                var res = ExecuteSingleOrder(order);
                PrintResultReport(res);
                return res.Success ? 0 : 1;
            }

            // Interactive mode (REPL)
            RunInteractiveRepl();
            return 0;
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   🩺 THỢ CHO ĐƯỜNG HUYẾT - HIS AUTOMATION 1-CLICK WIN APP (STANDALONE CLI)   ║");
            Console.WriteLine("║   Đường Máu Mao Mạch • Tờ Điều Trị • Kê Đơn Insulin Tủ Trực (HN & CSNB)      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("\n📖 HƯỚNG DẪN SỬ DỤNG (USAGE):");
            Console.WriteLine("  1. Cú pháp 1 dòng phân tách dấu phẩy (Standard CSV shorthand):");
            Console.WriteLine("     HisThoDuongHuyet.exe <CơSở>, <MãBS>, <MậtKhẩu>, <MãBN>, <Giờ>, <ĐườngHuyết>, <LiềuInsulin>");
            Console.WriteLine("     Ví dụ:");
            Console.WriteLine("       HisThoDuongHuyet.exe cs2, 034727, 981, 00376258, 17h, 14.3, 10R");
            Console.WriteLine("       HisThoDuongHuyet.exe cs1, 034727, 981, 0003969449, 21h, 16.5, 12L");
            Console.WriteLine("       HisThoDuongHuyet.exe cs2, 034727, 981, 00376258, 6h, 7.2, 0");
            Console.WriteLine();
            Console.WriteLine("  2. Cú pháp tham số dòng lệnh chi tiết (Named Flags):");
            Console.WriteLine("     HisThoDuongHuyet.exe -fac cs2 -u 034727 -pass 981 -p 00376258 -time 17h -glucose 14.3 -insulin 10R");
            Console.WriteLine();
            Console.WriteLine("  3. Chạy hàng loạt từ file danh sách:");
            Console.WriteLine("     HisThoDuongHuyet.exe -f danh_sach_don.txt");
            Console.WriteLine();
            Console.WriteLine("  4. Chế độ tương tác nhập liệu (Interactive REPL):");
            Console.WriteLine("     Chỉ cần gõ 'HisThoDuongHuyet.exe' (hoặc click đúp chuột), hệ thống sẽ mở lời nhắc nhập.");
            Console.WriteLine();
            Console.WriteLine("  * Quy chuẩn chữ cái viết tắt Insulin lâm sàng:");
            Console.WriteLine("    • R : Actrapid (Insulin tác dụng nhanh). Ví dụ: 6R, 8R, 10R");
            Console.WriteLine("    • L : Lantus   (Insulin nền kéo dài).   Ví dụ: 10L, 12L, 14L");
            Console.WriteLine("    • M : Mixtard  (Insulin hỗn hợp/Mix).   Ví dụ: 8M, 10M, 12M");
            Console.WriteLine("    • 0 : Không kê insulin (chỉ làm ĐMMM và ghi tờ điều trị)");
            Console.WriteLine("================════════════════════════════════════════════════════════════════\n");
        }

        private static void RunInteractiveRepl()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n💡 Đang mở Chế độ tương tác trực tiếp. Nhập lệnh theo cú pháp mẫu:");
            Console.WriteLine("   cs2, 034727, 981, 00376258, 17h, 14.3, 10R");
            Console.WriteLine("   (Nhập 'exit', 'quit' hoặc 'q' để thoát)");
            Console.ResetColor();

            while (true)
            {
                Console.Write("\n[ĐH-PROMPT] > ");
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                string trimmed = line.Trim();
                if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("👋 Tạm biệt Bác sĩ!");
                    break;
                }

                if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("?", StringComparison.OrdinalIgnoreCase))
                {
                    PrintUsage();
                    continue;
                }

                try
                {
                    var order = ParseOrderInput(trimmed);
                    if (order == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Cú pháp chưa chính xác! Ví dụ: cs2, 034727, 981, 00376258, 17h, 14.3, 10R");
                        Console.ResetColor();
                        continue;
                    }

                    var res = ExecuteSingleOrder(order);
                    PrintResultReport(res);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Lỗi ngoại lệ: " + ex.Message);
                    Console.ResetColor();
                }
            }
        }

        private static int ProcessBatchFile(string filePath)
        {
            Console.WriteLine("📂 Đang nạp danh sách y lệnh từ file: " + filePath);
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            int succ = 0;
            int fail = 0;
            int lineNum = 0;

            foreach (var rawLine in lines)
            {
                lineNum++;
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//")) continue;

                Console.WriteLine(string.Format("\n───────────────── XỬ LÝ DÒNG {0}: {1} ─────────────────", lineNum, line));
                var order = ParseOrderInput(line);
                if (order == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Bỏ qua dòng {0}: Cú pháp không hợp lệ.", lineNum);
                    Console.ResetColor();
                    fail++;
                    continue;
                }

                var res = ExecuteSingleOrder(order);
                PrintResultReport(res);
                if (res.Success) succ++; else fail++;
            }

            Console.WriteLine("\n================================================================================");
            Console.WriteLine(string.Format("TỔNG KẾT BATCH: ✔ Thành công: {0} | ❌ Thất bại: {1}", succ, fail));
            Console.WriteLine("================================================================================");
            return fail > 0 ? 1 : 0;
        }

        public static DiabetesOrderInput ParseOrderInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            DiabetesOrderInput order = new DiabetesOrderInput();

            // Extract global flags like --dry-run or -n
            if (input.Contains("--dry-run"))
            {
                order.IsDryRun = true;
                input = Regex.Replace(input, @"--dry-run", "", RegexOptions.IgnoreCase).Trim();
            }
            else if (Regex.IsMatch(input, @"\b-n\b", RegexOptions.IgnoreCase))
            {
                order.IsDryRun = true;
                input = Regex.Replace(input, @"\b-n\b", "", RegexOptions.IgnoreCase).Trim();
            }

            // 1. Check if input contains named flags like -fac or -p
            if (input.Contains("-fac") || input.Contains("-p") || input.Contains("-time") || input.Contains("-glucose"))
            {
                var tokens = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
                                  .Cast<Match>()
                                  .Select(m => m.Value.Trim('"'))
                                  .ToArray();

                for (int i = 0; i < tokens.Length; i++)
                {
                    string k = tokens[i].ToLower();
                    if ((k == "-fac" || k == "--facility") && i + 1 < tokens.Length) order.Facility = tokens[++i];
                    else if ((k == "-u" || k == "-user" || k == "--user") && i + 1 < tokens.Length) order.DoctorLogin = tokens[++i];
                    else if ((k == "-pass" || k == "--pass") && i + 1 < tokens.Length) order.DoctorPassword = tokens[++i];
                    else if ((k == "-p" || k == "-patient" || k == "--patient") && i + 1 < tokens.Length) order.PatientCode = tokens[++i];
                    else if ((k == "-time" || k == "-t" || k == "--time") && i + 1 < tokens.Length) order.TimeStr = tokens[++i];
                    else if ((k == "-glucose" || k == "-g" || k == "--glucose") && i + 1 < tokens.Length)
                    {
                        decimal g;
                        if (decimal.TryParse(tokens[++i].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out g)) order.GlucoseVal = g;
                    }
                    else if ((k == "-insulin" || k == "-i" || k == "--insulin") && i + 1 < tokens.Length) order.InsulinRaw = tokens[++i];
                    else if ((k == "-date" || k == "-d") && i + 1 < tokens.Length)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(tokens[++i], out dt)) order.TargetDate = dt;
                    }
                    else if (k == "--dry-run" || k == "-n") order.IsDryRun = true;
                }
            }
            else
            {
                // 2. Comma or whitespace separated line:
                // cs2, 034727, 981, 00376258, 17h, 14.3, 10R
                string[] parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => p.Trim())
                                      .ToArray();

                // If no commas, try space splitting
                if (parts.Length < 4)
                {
                    parts = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim())
                                 .ToArray();
                }

                if (parts.Length >= 4)
                {
                    order.Facility = parts[0];
                    order.DoctorLogin = parts[1];
                    order.DoctorPassword = parts[2];
                    order.PatientCode = parts[3];

                    if (parts.Length >= 5) order.TimeStr = parts[4];
                    if (parts.Length >= 6)
                    {
                        decimal g;
                        if (decimal.TryParse(parts[5].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out g))
                            order.GlucoseVal = g;
                    }
                    if (parts.Length >= 7) order.InsulinRaw = parts[6];
                }
                else
                {
                    return null;
                }
            }

            // Normalize doctor login (if 5 digits like 34727 from PS, pad to 6 digits 034727)
            if (!string.IsNullOrWhiteSpace(order.DoctorLogin))
            {
                long dNum;
                if (long.TryParse(order.DoctorLogin, out dNum) && order.DoctorLogin.Length < 6)
                {
                    order.DoctorLogin = order.DoctorLogin.PadLeft(6, '0');
                }
            }

            // Normalize patient code
            if (string.IsNullOrWhiteSpace(order.PatientCode)) return null;
            long pNum;
            if (long.TryParse(order.PatientCode, out pNum))
            {
                order.PatientCode = order.PatientCode.PadLeft(10, '0');
            }

            // Parse Insulin details
            ParseInsulin(order);

            return order;
        }

        private static void ParseInsulin(DiabetesOrderInput order)
        {
            string raw = (order.InsulinRaw ?? "").Trim().ToUpper();
            if (string.IsNullOrEmpty(raw) || raw == "0" || raw == "-" || raw == "NONE" || raw == "KHONG")
            {
                order.InsulinUnits = 0;
                order.InsulinType = "None";
                return;
            }

            // Extract units and suffix (e.g. 10R -> 10, R)
            var match = Regex.Match(raw, @"^(\d+)\s*([A-Z]*)$");
            if (match.Success)
            {
                order.InsulinUnits = int.Parse(match.Groups[1].Value);
                string suffix = match.Groups[2].Value;
                if (suffix == "R" || suffix.StartsWith("ACTR")) order.InsulinType = "Actrapid";
                else if (suffix == "L" || suffix.StartsWith("LANT")) order.InsulinType = "Lantus";
                else if (suffix == "M" || suffix.StartsWith("MIXT")) order.InsulinType = "Mixtard";
                else
                {
                    // Fallback based on hour if no suffix provided: 21h -> Lantus, otherwise Actrapid
                    string t = order.TimeStr.ToLower();
                    if (t.Contains("21") || t.Contains("22")) order.InsulinType = "Lantus";
                    else order.InsulinType = "Actrapid";
                }
            }
            else
            {
                int u;
                if (int.TryParse(raw, out u))
                {
                    order.InsulinUnits = u;
                    order.InsulinType = "Actrapid";
                }
            }
        }

        private static void SetAppSetting(string key, string val)
        {
            try
            {
                var settings = ConfigurationManager.AppSettings;
                var type = settings.GetType();
                while (type != null)
                {
                    var field = type.GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        field.SetValue(settings, false);
                        break;
                    }
                    type = type.BaseType;
                }
                settings[key] = val;
            }
            catch { }
        }

        public static bool EnsureSession(string user, string pass, string facility)
        {
            string facNorm = (facility ?? "cs2").Trim().ToLower();
            bool isNB = (facNorm == "cs2" || facNorm == "nb" || facNorm == "ninhbinh" || facNorm == "81" || facNorm == "915");
            string targetFacKey = isNB ? "NB" : "HN";

            // If token already valid and doctor / facility match, reuse
            if (!string.IsNullOrEmpty(activeTokenCode) && currentDoctorLogin == user && activeFacility == targetFacKey)
            {
                return true;
            }

            // 1. Direct ACS Login
            Console.WriteLine(string.Format("🔑 Đang xác thực bác sĩ [{0}] tại ACS máy chủ (192.168.7.200:1401)...", user));

            SetAppSetting("Inventec.Token.ClientSystem.Acs.Base.Uri", "http://192.168.7.200:1401/");
            SetAppSetting("Inventec.Token.ClientSystem.Acs.Renew.Uri", "api/Token/Renew");
            SetAppSetting("Inventec.Token.ClientSystem.Acs.Login.Uri", "api/Token/Login");
            SetAppSetting("Inventec.Token.ClientSystem.Acs.ChangePass.Uri", "api/Token/ChangePassword");
            SetAppSetting("Inventec.Token.ClientSystem.Acs.Logout.Uri", "api/Token/Logout");
            SetAppSetting("Inventec.Token.ClientSystem.Acs.GetAuthenticated.Uri", "api/Token/GetAuthenticated");
            SetAppSetting("Inventec.Token.ClientSystem.Timeout", "30");
            SetAppSetting("Inventec.Common.WebApiClient.Timeout", "300");
            SetAppSetting("Inventec.Desktop.ApplicationCode", "HIS");

            try
            {
                var constType = typeof(ClientTokenManager).Assembly.GetType("Inventec.Token.ClientSystem.Constants");
                var fBase = constType.GetField("BASE_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fBase != null) fBase.SetValue(null, "http://192.168.7.200:1401/");
                var fLogin = constType.GetField("LOGIN_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fLogin != null) fLogin.SetValue(null, "api/Token/Login");
            }
            catch { }

            param = new CommonParam();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");
            var tok = tokenManager.Login(param, user, pass, "2.390.0");

            if (tok == null || string.IsNullOrEmpty(tok.TokenCode))
            {
                // Fallback to password from environment if different
                string envPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
                if (!string.IsNullOrEmpty(envPass) && envPass != pass)
                {
                    tok = tokenManager.Login(param, user, envPass, "2.390.0");
                }
            }

            if (tok != null && !string.IsNullOrEmpty(tok.TokenCode))
            {
                activeTokenCode = tok.TokenCode;
                currentDoctorLogin = user;
                currentDoctorName = (user == "034727" ? "Ths.BS Nguyễn Hữu Sâm" : (user == "vmc" ? "BS Vũ Minh Cường" : user));
                activeFacility = targetFacKey;

                mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", activeTokenCode, "HIS");
                sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", activeTokenCode, "HIS");

                Console.WriteLine("   ✔ Xác thực ACS thành công! Token: " + activeTokenCode.Substring(0, 16) + "...");
                
                // Cache standalone token for other tools
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"),
                        activeTokenCode + "|" + DateTime.Now.Ticks + "|" + user, Encoding.UTF8);
                }
                catch { }

                // 2. Kích hoạt WorkInfo phòng làm việc
                ActivateWorkInfo(isNB);
                return true;
            }

            string err = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "Đăng nhập ACS thất bại";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Lỗi xác thực tài khoản: " + err);
            Console.ResetColor();
            return false;
        }

        private static void ActivateWorkInfo(bool isNinhBinh)
        {
            try
            {
                var workInfo = new WorkInfoSDO { Rooms = new List<RoomSDO>() };
                if (isNinhBinh)
                {
                    long[] nbRooms = new long[] { 18679, 18681, 15722, 15322, 15321 };
                    foreach (var r in nbRooms) workInfo.Rooms.Add(new RoomSDO { RoomId = r });

                    // Add rooms of Dept 915 if possible
                    var rf = new HisRoomViewFilter { DEPARTMENT_ID = 915, IS_ACTIVE = 1 };
                    var rooms = adapter.FetchList<V_HIS_ROOM>("api/HisRoom/GetView", mosConsumer, rf, param);
                    if (rooms != null)
                    {
                        foreach (var r in rooms)
                        {
                            if (!workInfo.Rooms.Any(x => x.RoomId == r.ID))
                                workInfo.Rooms.Add(new RoomSDO { RoomId = r.ID });
                        }
                    }
                }
                else
                {
                    long[] hnHallRooms = new long[] { 5248, 5252, 5251, 5257, 931 };
                    foreach (var r in hnHallRooms) workInfo.Rooms.Add(new RoomSDO { RoomId = r });
                }

                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
                Console.WriteLine(string.Format("   ✔ Đã kích hoạt WorkInfo {0} phòng làm việc ({1})",
                    workInfo.Rooms.Count, isNinhBinh ? "Cơ sở 2 Ninh Bình" : "Cơ sở 1 Hà Nội"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ⚠️ WorkInfo warning: " + ex.Message);
            }
        }

        public static PatientProfile LookupPatient(string patientCode, bool isNinhBinhTarget)
        {
            string pCodeNorm = patientCode.Trim();
            long numVal;
            if (long.TryParse(pCodeNorm, out numVal)) pCodeNorm = pCodeNorm.PadLeft(10, '0');

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCodeNorm };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);

            if (trs == null || trs.Count == 0)
            {
                var tfTr = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = pCodeNorm.PadLeft(12, '0') };
                trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfTr, param);
            }

            if (trs == null || trs.Count == 0) return null;

            var tr = trs.OrderByDescending(t => t.ID).First();
            var profile = new PatientProfile
            {
                TreatmentId = tr.ID,
                TreatmentCode = tr.TREATMENT_CODE,
                PatientCode = tr.TDL_PATIENT_CODE,
                PatientName = tr.TDL_PATIENT_NAME,
                GenderName = tr.TDL_PATIENT_GENDER_NAME,
                DobStr = tr.TDL_PATIENT_DOB > 0 ? tr.TDL_PATIENT_DOB.ToString() : "",
                PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                IcdCode = !string.IsNullOrEmpty(tr.ICD_CODE) ? tr.ICD_CODE : "E11.9",
                IcdName = !string.IsNullOrEmpty(tr.ICD_NAME) ? tr.ICD_NAME : "Đái tháo đường típ 2",
                IcdSubCode = tr.ICD_SUB_CODE,
                IcdText = !string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME,
                BranchId = tr.BRANCH_ID,
                IsNinhBinh = (tr.BRANCH_ID == 81 || isNinhBinhTarget),
                DepartmentId = isNinhBinhTarget ? 915 : 57,
                WorkingRoomId = isNinhBinhTarget ? 18679 : 5248,
                BedRoomName = isNinhBinhTarget ? "Khu 3E" : "P714",
                BedName = "Chưa rõ"
            };

            // Query Bed Room
            try
            {
                var tbrf = new HisTreatmentBedRoomViewFilter { TREATMENT_IDs = new List<long> { tr.ID }, IS_IN_ROOM = true };
                var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                if (beds != null && beds.Count > 0)
                {
                    var b = beds[0];
                    profile.DepartmentId = b.DEPARTMENT_ID;
                    profile.BedRoomName = b.BED_ROOM_NAME;
                    profile.BedName = b.BED_NAME;
                    profile.IsNinhBinh = (b.DEPARTMENT_ID == 915 || tr.BRANCH_ID == 81 || (b.BED_ROOM_NAME != null && (b.BED_ROOM_NAME.Contains("3E") || b.BED_ROOM_NAME.Contains("3D"))));

                    // Map BedRoom to RoomId
                    var brf = new HisBedRoomViewFilter { ID = b.BED_ROOM_ID };
                    var bRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
                    if (bRooms != null && bRooms.Count > 0)
                    {
                        profile.WorkingRoomId = bRooms[0].ROOM_ID;
                    }
                }
            }
            catch { }

            return profile;
        }

        public static ExecutionResult ExecuteSingleOrder(DiabetesOrderInput input)
        {
            ExecutionResult res = new ExecutionResult
            {
                PatientCode = input.PatientCode,
                GlucoseVal = input.GlucoseVal
            };

            string facNorm = (input.Facility ?? "cs2").Trim().ToLower();
            bool isNB = (facNorm == "cs2" || facNorm == "nb" || facNorm == "ninhbinh" || facNorm == "81" || facNorm == "915");
            res.FacilityName = isNB ? "Cơ sở 2 Ninh Bình (Khoa 915)" : "Cơ sở 1 Hà Nội (Khoa 57)";

            // 1. Session Auth
            if (!EnsureSession(input.DoctorLogin, input.DoctorPassword, input.Facility))
            {
                if (input.IsDryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ [DRY RUN] Đăng nhập ACS thất bại/bỏ qua trong chế độ mô phỏng offline.");
                    Console.ResetColor();
                    res.PatientName = "BỆNH NHÂN MÔ PHỎNG (TEST)";
                    res.BedRoomFull = isNB ? "P3E-05 - G01" : "P714 - G01";
                    res.Success = true;
                    res.BedsideServiceReqCode = "MOCK_CLS_" + Guid.NewGuid().ToString().Substring(0, 8);
                    res.TrackingId = 999999;
                    res.PresServiceReqCode = "MOCK_DON_" + Guid.NewGuid().ToString().Substring(0, 8);
                    res.InsulinDesc = string.Format("{0} {1} UI ({2:F4} lọ)", input.InsulinType, input.InsulinUnits, input.InsulinUnits / 1000.0m);
                    res.GlucoseTime = input.TimeStr + " " + DateTime.Today.ToString("dd/MM/yyyy");
                    res.InsulinTime = input.TimeStr + " " + DateTime.Today.ToString("dd/MM/yyyy");
                    return res;
                }
                res.Success = false;
                res.ErrorMessage = "Không thể đăng nhập tài khoản ACS với thông tin được cung cấp.";
                return res;
            }

            // 2. Patient Lookup
            Console.WriteLine(string.Format("🔍 Đang tra cứu thông tin bệnh nhân [{0}]...", input.PatientCode));
            var patient = LookupPatient(input.PatientCode, isNB);
            if (patient == null)
            {
                if (input.IsDryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ [DRY RUN] Không tìm thấy bệnh nhân thật, sử dụng hồ sơ giả lập để mô phỏng.");
                    Console.ResetColor();
                    patient = new PatientProfile
                    {
                        PatientCode = input.PatientCode,
                        PatientName = "BỆNH NHÂN MÔ PHỎNG",
                        TreatmentId = 999999,
                        TreatmentCode = "000000999999",
                        BedRoomName = isNB ? "P3E-05" : "P714",
                        BedName = "G01",
                        DepartmentId = isNB ? 915 : 57,
                        DepartmentName = isNB ? "Khoa Ngoại Tổng Hợp" : "Khoa CTCH & CS",
                        WorkingRoomId = isNB ? 18679 : 5248,
                        GenderName = "Nam",
                        IcdName = "Đái tháo đường typ 2"
                    };
                }
                else
                {
                    res.Success = false;
                    res.ErrorMessage = string.Format("Không tìm thấy hồ sơ đợt điều trị cho bệnh nhân mã [{0}]!", input.PatientCode);
                    return res;
                }
            }

            res.PatientName = patient.PatientName;
            res.BedRoomFull = string.Format("{0} - {1}", patient.BedRoomName, patient.BedName);
            Console.WriteLine(string.Format("   👤 Bệnh nhân: {0} ({1}) | {2} | Chẩn đoán: {3}",
                patient.PatientName, patient.GenderName, res.BedRoomFull, patient.IcdName));

            // Parse time
            int hour = 17;
            int minute = 0;
            string tClean = input.TimeStr.ToLower().Replace("h", ":").TrimEnd(':');
            var tParts = tClean.Split(':');
            int.TryParse(tParts[0], out hour);
            if (tParts.Length > 1) int.TryParse(tParts[1], out minute);

            DateTime sessionDate = input.TargetDate;
            // Rule 5: Mốc 06:00 tự động tính ngày hôm sau nếu chỉ định từ buổi trưa/chiều/tối
            if (hour <= 7 && DateTime.Now.Hour >= 12 && sessionDate.Date == DateTime.Today)
            {
                sessionDate = sessionDate.AddDays(1);
            }

            DateTime dtSession = new DateTime(sessionDate.Year, sessionDate.Month, sessionDate.Day, hour, minute, 0);
            long instructionTime = long.Parse(dtSession.ToString("yyyyMMddHHmmss"));
            res.GlucoseTime = dtSession.ToString("HH:mm dd/MM/yyyy");

            if (input.IsDryRun)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("⚠️ [DRY RUN] Đã mô phỏng thành công các bước, không gửi dữ liệu thật.");
                Console.ResetColor();
                res.Success = true;
                res.BedsideServiceReqCode = "MOCK_CLS_" + Guid.NewGuid().ToString().Substring(0, 8);
                res.TrackingId = 999999;
                res.PresServiceReqCode = "MOCK_DON_" + Guid.NewGuid().ToString().Substring(0, 8);
                res.InsulinDesc = string.Format("{0} {1} UI ({2:F4} lọ)", input.InsulinType, input.InsulinUnits, input.InsulinUnits / 1000.0m);
                return res;
            }

            // =========================================================================
            // TÁC VỤ 1: CHỈ ĐỊNH ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG (Step 1)
            // =========================================================================
            Console.WriteLine(string.Format("\n[BƯỚC 1/3] Tạo chỉ định ĐMMM tại giường ({0})...", isNB ? SERVICE_CODE_NB_GLUCOSE : SERVICE_CODE_HN_GLUCOSE));
            try
            {
                res.BedsideServiceReqCode = AssignBedsideGlucose(patient, instructionTime, dtSession.ToString("HH:mm"), isNB);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✔ Chỉ định ĐMMM THÀNH CÔNG! Mã Y Lệnh: " + res.BedsideServiceReqCode);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   ❌ Lỗi chỉ định ĐMMM: " + ex.Message);
                Console.ResetColor();
                res.ErrorMessage = "Lỗi chỉ định ĐMMM: " + ex.Message;
                // Continue to Step 2 even if Step 1 failed
            }

            // =========================================================================
            // TÁC VỤ 2: TẠO TỜ ĐIỀU TRỊ GHI NHẬN ĐƯỜNG HUYẾT (Step 2)
            // =========================================================================
            Console.WriteLine(string.Format("\n[BƯỚC 2/3] Tạo Tờ điều trị ghi nhận đường huyết {0} mmol/L...", input.GlucoseVal));
            try
            {
                DateTime dtPrescribe = dtSession.AddMinutes(5);
                string presTimeStr = dtPrescribe.ToString("HH'h'mm");
                res.InsulinTime = dtPrescribe.ToString("HH:mm dd/MM/yyyy");

                string medInstruction = (input.InsulinUnits > 0)
                    ? string.Format("Tiêm dưới da {0} {1} UI lúc {2}. Theo dõi tiếp đường huyết.", input.InsulinType, input.InsulinUnits, presTimeStr)
                    : "Theo dõi tiếp đường huyết.";

                string careInstruction = "Chăm sóc cấp II. Chế độ ăn ĐTĐ. Theo dõi đường huyết.";
                string content = string.Format("Khám: Đường máu mao mạch lúc {0}: {1} mmol/L", dtSession.ToString("HH'h'mm"), input.GlucoseVal);

                res.TrackingId = CreateTreatmentTracking(patient, instructionTime, content, careInstruction, medInstruction, isNB);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✔ Tạo Tờ điều trị THÀNH CÔNG! Tracking ID: " + res.TrackingId);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   ❌ Lỗi tạo Tờ điều trị: " + ex.Message);
                Console.ResetColor();
                res.ErrorMessage = (res.ErrorMessage != null ? res.ErrorMessage + " | " : "") + "Lỗi Tờ điều trị: " + ex.Message;
            }

            // =========================================================================
            // TÁC VỤ 3: KÊ ĐƠN INSULIN TỦ TRỰC (Step 3 - Circuit Breaker Protected)
            // =========================================================================
            if (input.InsulinUnits > 0)
            {
                res.InsulinDesc = string.Format("{0} {1} UI ({2:F4} lọ)", input.InsulinType, input.InsulinUnits, input.InsulinUnits / 1000.0m);
                Console.WriteLine(string.Format("\n[BƯỚC 3/3] Kê đơn Insulin {0} từ Tủ trực ({1})...", res.InsulinDesc, isNB ? "CS2" : "Khoa 57"));

                // Circuit Breaker: Kê đơn bắt buộc cần tờ điều trị
                if (res.TrackingId == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("   ⛔ CẮT CẦU DAO: Bỏ qua kê đơn Insulin vì Tờ điều trị chưa tạo thành công (Yêu cầu lâm sàng bắt buộc)!");
                    Console.ResetColor();
                    res.ErrorMessage = (res.ErrorMessage != null ? res.ErrorMessage + " | " : "") + "Kê đơn bị chặn do thiếu Tờ điều trị";
                }
                else
                {
                    try
                    {
                        DateTime dtPrescribe = dtSession.AddMinutes(5);
                        long presInstructionTime = long.Parse(dtPrescribe.ToString("yyyyMMddHHmmss"));

                        res.PresServiceReqCode = PrescribeCabinetInsulin(patient, res.TrackingId, presInstructionTime,
                            dtPrescribe.ToString("HH'h'mm"), input.InsulinType, input.InsulinUnits, isNB);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("   ✔ Kê đơn Insulin tủ trực THÀNH CÔNG! Mã Y Lệnh: " + res.PresServiceReqCode);
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   ❌ Lỗi kê đơn Insulin tủ trực: " + ex.Message);
                        Console.ResetColor();
                        res.ErrorMessage = (res.ErrorMessage != null ? res.ErrorMessage + " | " : "") + "Lỗi đơn thuốc: " + ex.Message;
                    }
                }
            }
            else
            {
                Console.WriteLine("\n[BƯỚC 3/3] Không kê Insulin (Liều = 0 UI). Hoàn thành phác đồ.");
                res.InsulinDesc = "Không chỉ định (0 UI)";
            }

            res.Success = (!string.IsNullOrEmpty(res.BedsideServiceReqCode) || res.TrackingId > 0 || !string.IsNullOrEmpty(res.PresServiceReqCode));
            return res;
        }

        private static string AssignBedsideGlucose(PatientProfile patient, long instructionTime, string timeStr, bool isNB)
        {
            long targetServiceId = isNB ? SERVICE_ID_NB_GLUCOSE : SERVICE_ID_HN_GLUCOSE;
            long executeRoomId = isNB ? (patient.BedRoomName.Contains("3D") ? EXECUTE_ROOM_ID_NB_3D : EXECUTE_ROOM_ID_NB_3E) : EXECUTE_ROOM_ID_HN;
            string targetSampleType = isNB ? null : "BP0042";
            long requestRoomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : (isNB ? WORKING_ROOM_ID_NB_3E : WORKING_ROOM_ID_HN);

            AssignServiceSDO sdo = new AssignServiceSDO
            {
                TreatmentId = patient.TreatmentId,
                RequestRoomId = requestRoomId,
                RequestLoginName = currentDoctorLogin,
                RequestUserName = currentDoctorName,
                InstructionTime = instructionTime,
                InstructionTimes = new List<long> { instructionTime },
                UseTimes = new List<long> { instructionTime },
                IcdCode = patient.IcdCode,
                IcdName = patient.IcdName,
                IcdSubCode = patient.IcdSubCode,
                IcdText = patient.IcdText,
                SessionCode = Guid.NewGuid().ToString(),
                ServiceReqDetails = new List<ServiceReqDetailSDO>
                {
                    new ServiceReqDetailSDO
                    {
                        ServiceId = targetServiceId,
                        Amount = 1.0m,
                        PatientTypeId = patient.PatientTypeId > 0 ? patient.PatientTypeId : 1,
                        PrimaryPatientTypeId = (patient.PatientTypeId == 1 ? (long?)null : patient.PatientTypeId),
                        RoomId = executeRoomId,
                        SampleTypeCode = targetSampleType,
                        InstructionNote = string.Format("Đo ĐMMM lúc {0} - Theo dõi đường huyết", timeStr),
                        MultipleExecute = 1,
                        IsNotUseBhyt = false,
                        IsNoHeinDifference = false,
                        EkipInfos = new List<EkipSDO>()
                    }
                }
            };

            CommonParam cp = new CommonParam();
            var res = adapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdo, cp);
            if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
            {
                return res.ServiceReqs[0].SERVICE_REQ_CODE;
            }

            string errMsg = (cp.Messages != null && cp.Messages.Count > 0) ? string.Join("; ", cp.Messages) : "Lỗi không xác định khi chỉ định ĐMMM";
            throw new Exception(errMsg);
        }

        private static long CreateTreatmentTracking(PatientProfile patient, long instructionTime, string content, string care, string med, bool isNB)
        {
            long deptId = isNB ? 915 : 57;
            long roomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : (isNB ? 18679 : 5248);

            HIS_TRACKING tracking = new HIS_TRACKING
            {
                TREATMENT_ID = patient.TreatmentId,
                DEPARTMENT_ID = deptId,
                ROOM_ID = roomId,
                TRACKING_TIME = instructionTime,
                CONTENT = content,
                MEDICAL_INSTRUCTION = med,
                CARE_INSTRUCTION = care,
                ICD_CODE = patient.IcdCode,
                ICD_NAME = patient.IcdName,
                ICD_SUB_CODE = patient.IcdSubCode,
                ICD_TEXT = patient.IcdText
            };

            HisTrackingSDO sdo = new HisTrackingSDO
            {
                Tracking = tracking,
                WorkingRoomId = roomId,
                Dhst = null
            };

            CommonParam cp = new CommonParam();
            var created = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
            if (created != null && created.ID > 0)
            {
                return created.ID;
            }

            string errMsg = (cp.Messages != null && cp.Messages.Count > 0) ? string.Join("; ", cp.Messages) : "MOS từ chối tạo Tờ điều trị";
            throw new Exception(errMsg);
        }

        private static string PrescribeCabinetInsulin(PatientProfile patient, long trackingId, long presTime, string timeSlotStr, string insulinType, int units, bool isNB)
        {
            long stockId;
            if (isNB)
            {
                stockId = (patient.BedRoomName != null && patient.BedRoomName.Contains("3D")) ? MEDI_STOCK_ID_NB_3D : MEDI_STOCK_ID_NB_3E;
            }
            else
            {
                stockId = MEDI_STOCK_ID_HN;
            }

            // 1. Find active medicine beans in cabinet
            var bf = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = stockId };
            var beans = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", mosConsumer, bf, param);
            if (beans == null || beans.Count == 0)
            {
                throw new Exception(string.Format("Không tìm thấy dữ liệu tồn kho Tủ trực ID {0}!", stockId));
            }

            var matchedBeans = beans.Where(b => b.AMOUNT > 0 && b.MEDICINE_TYPE_NAME != null &&
                b.MEDICINE_TYPE_NAME.IndexOf(insulinType, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (matchedBeans.Count == 0)
            {
                // Fallback to "Insulin" generic
                matchedBeans = beans.Where(b => b.AMOUNT > 0 && b.MEDICINE_TYPE_NAME != null &&
                    b.MEDICINE_TYPE_NAME.IndexOf("Insulin", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (matchedBeans.Count == 0)
            {
                throw new Exception(string.Format("Hết tồn thuốc {0} trong Tủ trực {1}!", insulinType, stockId));
            }

            var targetMed = matchedBeans.OrderByDescending(b => b.AMOUNT).First();
            decimal presAmount = units / 1000.0m;

            // 2. Take bean (Reserve in Cabinet)
            string sessionKey = Guid.NewGuid().ToString();
            var takeBean = new TakeBeanSDO
            {
                TypeId = targetMed.MEDICINE_TYPE_ID,
                MediStockId = stockId,
                PatientTypeId = patient.PatientTypeId > 0 ? patient.PatientTypeId : 1,
                Amount = presAmount,
                ClientSessionKey = sessionKey,
                ExpiredDate = null
            };

            CommonParam cpTake = new CommonParam();
            var takenBeans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, cpTake);
            if (takenBeans == null || takenBeans.Count == 0)
            {
                string takeErr = (cpTake.Messages != null && cpTake.Messages.Count > 0) ? string.Join("; ", cpTake.Messages) : "Không giữ được thuốc trong tủ trực (có thể tồn khả dụng không đủ).";
                throw new Exception(takeErr);
            }

            // 3. Post OutPatient Prescription (Tủ trực)
            string doseStr = units.ToString("D2");
            int hour = int.Parse(presTime.ToString().Substring(8, 2));
            string morning = null, noon = null, afternoon = null, evening = null;
            if (hour >= 5 && hour <= 9) morning = doseStr;
            else if (hour >= 10 && hour <= 13) noon = doseStr;
            else if (hour >= 14 && hour <= 18) afternoon = doseStr;
            else evening = doseStr;

            var outPresSDO = new OutPatientPresSDO
            {
                TreatmentId = patient.TreatmentId,
                InstructionTime = presTime,
                UseTimes = new List<long> { presTime },
                TrackingId = trackingId,
                RequestRoomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : (isNB ? 18679 : 5248),
                RequestLoginName = currentDoctorLogin,
                RequestUserName = currentDoctorName,
                IcdCode = patient.IcdCode,
                IcdName = patient.IcdName,
                IcdSubCode = patient.IcdSubCode,
                IcdText = patient.IcdText,
                IsCabinet = true,
                ClientSessionKey = sessionKey,
                Medicines = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO
                    {
                        MedicineTypeId = targetMed.MEDICINE_TYPE_ID,
                        MediStockId = stockId,
                        Amount = presAmount,
                        PresAmount = presAmount,
                        PatientTypeId = patient.PatientTypeId > 0 ? patient.PatientTypeId : 1,
                        Tutorial = string.Format("Tiêm dưới da {0} UI lúc {1}", units, timeSlotStr),
                        MedicineUseFormId = 15, // Tiêm
                        Morning = morning,
                        Noon = noon,
                        Afternoon = afternoon,
                        Evening = evening,
                        IsExpend = false,
                        NumOfDays = 1,
                        MedicineBeanIds = takenBeans.Select(b => b.ID).ToList()
                    }
                }
            };

            CommonParam cpPres = new CommonParam();
            var outRes = adapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, cpPres);
            if (outRes != null && outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0)
            {
                return outRes.ServiceReqs[0].SERVICE_REQ_CODE;
            }

            string presErr = (cpPres.Messages != null && cpPres.Messages.Count > 0) ? string.Join("; ", cpPres.Messages) : "Kê đơn tủ trực thất bại";
            throw new Exception(presErr);
        }

        private static void PrintResultReport(ExecutionResult res)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        BÁO CÁO KẾT QUẢ ĐIỀU HÀNH LÂM SÀNG                    ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine(string.Format("║ • Trạng thái       : {0,-55} ║", res.Success ? "🟢 THÀNH CÔNG" : "🔴 CÓ LỖI XẢY RA"));
            Console.WriteLine(string.Format("║ • Cơ sở điều trị   : {0,-55} ║", res.FacilityName));
            Console.WriteLine(string.Format("║ • Bệnh nhân        : {0,-55} ║", string.Format("{0} (Mã: {1})", res.PatientName ?? "N/A", res.PatientCode)));
            Console.WriteLine(string.Format("║ • Buồng - Giường   : {0,-55} ║", res.BedRoomFull ?? "N/A"));
            Console.WriteLine("╟──────────────────────────────────────────────────────────────────────────────╢");
            Console.WriteLine(string.Format("║ 1. Chỉ định ĐMMM   : Lúc {0,-17} | Mã Y Lệnh: {1,-18} ║",
                res.GlucoseTime ?? "N/A", res.BedsideServiceReqCode != null ? res.BedsideServiceReqCode : "THẤT BẠI"));
            Console.WriteLine(string.Format("║ 2. Tờ điều trị     : Ghi nhận ĐH {0,4:F1} mmol/L | ID Phiếu : {1,-18} ║",
                res.GlucoseVal, res.TrackingId > 0 ? res.TrackingId.ToString() : "THẤT BẠI"));
            Console.WriteLine(string.Format("║ 3. Đơn Insulin     : {0,-26} | Mã Y Lệnh: {1,-18} ║",
                res.InsulinDesc ?? "N/A", res.PresServiceReqCode != null ? res.PresServiceReqCode : "KHÔNG KÊ / LỖI"));
            if (!string.IsNullOrEmpty(res.ErrorMessage))
            {
                Console.WriteLine("╟──────────────────────────────────────────────────────────────────────────────╢");
                Console.WriteLine(string.Format("║ ⚠️ Cảnh báo/Chi tiết : {0,-53} ║", res.ErrorMessage.Length > 53 ? res.ErrorMessage.Substring(0, 50) + "..." : res.ErrorMessage));
            }
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("💡 [LƯU Ý BỘ LỌC HIS UI]: Hãy chọn bộ lọc 'Tất cả bác sĩ' trên phần mềm HIS");
            Console.WriteLine("   để xem trọn vẹn y lệnh ĐMMM và đơn tủ trực vừa được hệ thống tạo tự động!");
            Console.ResetColor();
        }
    }
}
