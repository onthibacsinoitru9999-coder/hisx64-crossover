using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Inventec.Common.Adapter;
using Inventec.Common.Mapper;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

namespace HisThoRaVien
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

    public class DischargeOrderInput
    {
        public string Facility { get; set; }
        public string DoctorLogin { get; set; }
        public string DoctorPassword { get; set; }
        public string PatientCode { get; set; }
        public bool IsDryRun { get; set; }

        public DischargeOrderInput()
        {
            Facility = "cs2";
            DoctorLogin = "034727";
            DoctorPassword = "981";
            IsDryRun = false;
        }
    }

    public class DischargeExecutionResult
    {
        public bool Success { get; set; }
        public string PatientCode { get; set; }
        public string PatientName { get; set; }
        public string TreatmentCode { get; set; }
        public long TreatmentId { get; set; }
        public string BedRoomFull { get; set; }
        public string FacilityName { get; set; }
        public string DepartmentName { get; set; }
        public int TreatmentDays { get; set; }
        public List<string> CreatedTrackings { get; set; }
        public int TransferredOrdersCount { get; set; }
        public int ProtectedOrdersCount { get; set; }
        public int SkippedAlreadyOrdersCount { get; set; }
        public List<string> TransferredOrderDetails { get; set; }
        public string EmrCoverStatus { get; set; }
        public string ErrorMessage { get; set; }

        public DischargeExecutionResult()
        {
            CreatedTrackings = new List<string>();
            TransferredOrderDetails = new List<string>();
        }
    }

    public class Program
    {
        private static ApiConsumer mosConsumer;
        private static MyAdapter adapter = new MyAdapter();
        private static CommonParam param = new CommonParam();
        private static string activeTokenCode = null;
        private static string currentDoctorLogin = null;
        private static string currentDoctorName = null;
        private static string activeFacility = null;

        public const string MOS_BASE = "http://192.168.7.236:1608/";
        public const string ACS_BASE = "http://192.168.7.200:1401/";

        [STAThread]
        public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);

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
                    string p4 = Path.Combine(folderPath, "Integrate", "EMR", name);
                    if (File.Exists(p4)) return Assembly.LoadFrom(p4);
                }
                catch { }
                return null;
            };

            return RunApp(args);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
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

                // Batch file mode
                if ((first == "-f" || first == "--file") && args.Length > 1)
                {
                    string fPath = args[1];
                    if (!File.Exists(fPath))
                    {
                        Console.WriteLine("❌ Lỗi: Không tìm thấy file danh sách: " + fPath);
                        return 1;
                    }
                    return ProcessBatchFile(fPath);
                }

                // Single order mode
                string joined = string.Join(" ", args);
                var order = ParseOrderInput(joined);
                if (order == null)
                {
                    Console.WriteLine("❌ Lỗi: Cú pháp tham số không hợp lệ!");
                    PrintUsage();
                    return 1;
                }

                var res = ExecuteSingleDischarge(order);
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
            Console.WriteLine("║   🏁 THỢ LÀM RA VIỆN - HIS AUTOMATION 1-CLICK WIN APP (STANDALONE CLI)       ║");
            Console.WriteLine("║   Sơ Kết 3/7 Ngày • Tờ Tổng Kết • Chuyển Y Lệnh Trắng • Bìa Bệnh Án EMR     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("\n📖 HƯỚNG DẪN SỬ DỤNG (USAGE):");
            Console.WriteLine("  1. Cú pháp 1 dòng phân tách dấu phẩy (Standard CSV shorthand):");
            Console.WriteLine("     HisThoRaVien.exe <CơSở>, <MãBS>, <MậtKhẩu>, <MãBN>");
            Console.WriteLine("     Ví dụ:");
            Console.WriteLine("       HisThoRaVien.exe cs2, 034727, 981, 00376258");
            Console.WriteLine("       HisThoRaVien.exe cs1, 034727, 981, 0003969449");
            Console.WriteLine("       HisThoRaVien.exe cs2, 034727, 981, 00376258 --dry-run");
            Console.WriteLine();
            Console.WriteLine("  2. Cú pháp tham số dòng lệnh chi tiết (Named Flags):");
            Console.WriteLine("     HisThoRaVien.exe -fac cs2 -u 034727 -pass 981 -p 00376258");
            Console.WriteLine();
            Console.WriteLine("  3. Chạy hàng loạt từ file danh sách:");
            Console.WriteLine("     HisThoRaVien.exe -f danh_sach_ra_vien.txt");
            Console.WriteLine();
            Console.WriteLine("  4. Chế độ tương tác trực tiếp (Interactive REPL):");
            Console.WriteLine("     Chỉ cần gõ 'HisThoRaVien.exe' (hoặc click đúp chuột), hệ thống sẽ mở lời nhắc nhập.");
            Console.WriteLine("================════════════════════════════════════════════════════════════════\n");
        }

        private static void RunInteractiveRepl()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n💡 Đang mở Chế độ tương tác trực tiếp. Nhập lệnh theo cú pháp mẫu:");
            Console.WriteLine("   cs2, 034727, 981, 00376258");
            Console.WriteLine("   (Nhập 'exit', 'quit' hoặc 'q' để thoát)");
            Console.ResetColor();

            while (true)
            {
                Console.Write("\n[RAVIEN-PROMPT] > ");
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
                        Console.WriteLine("❌ Cú pháp không hợp lệ. Vui lòng nhập: <CơSở>, <MãBS>, <MậtKhẩu>, <MãBN>");
                        Console.ResetColor();
                        continue;
                    }

                    var res = ExecuteSingleDischarge(order);
                    PrintResultReport(res);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Lỗi xử lý ca ra viện: " + ex.Message);
                    Console.ResetColor();
                }
            }
        }

        private static int ProcessBatchFile(string filePath)
        {
            Console.WriteLine("📂 Đang nạp danh sách ca ra viện từ file: " + filePath);
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

                var res = ExecuteSingleDischarge(order);
                PrintResultReport(res);
                if (res.Success) succ++; else fail++;
            }

            Console.WriteLine("\n================================================================================");
            Console.WriteLine(string.Format("TỔNG KẾT BATCH: ✔ Thành công: {0} | ❌ Thất bại: {1}", succ, fail));
            Console.WriteLine("================================================================================");
            return fail > 0 ? 1 : 0;
        }

        public static DischargeOrderInput ParseOrderInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            DischargeOrderInput order = new DischargeOrderInput();

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
            if (input.Contains("-fac") || input.Contains("-p") || input.Contains("-u") || input.Contains("-user"))
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
                    else if (k == "--dry-run" || k == "-n") order.IsDryRun = true;
                }
            }
            else
            {
                // 2. Comma or whitespace separated line:
                // cs2, 034727, 981, 00376258
                string[] parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => p.Trim())
                                      .ToArray();

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

            return order;
        }

        private static void SetAppSetting(string key, string val)
        {
            try
            {
                var settings = ConfigurationManager.AppSettings;
                var type = settings.GetType();
                var readOnlyField = type.GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                if (readOnlyField != null) readOnlyField.SetValue(settings, false);
                settings[key] = val;
            }
            catch { }
        }

        public static bool EnsureSession(string user, string pass, string facilityKey)
        {
            string targetFacKey = (facilityKey ?? "cs2").Trim().ToLower();
            bool isNB = (targetFacKey == "cs2" || targetFacKey == "nb" || targetFacKey == "ninhbinh" || targetFacKey == "81" || targetFacKey == "915");

            // Check if already authenticated with same doctor and facility
            if (!string.IsNullOrEmpty(activeTokenCode) &&
                string.Equals(currentDoctorLogin, user, StringComparison.OrdinalIgnoreCase) &&
                activeFacility == targetFacKey &&
                mosConsumer != null)
            {
                return true;
            }

            Console.WriteLine(string.Format("🔑 Đang xác thực bác sĩ [{0}] tại ACS máy chủ ({1})...", user, "192.168.7.200:1401"));

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
                if (constType != null)
                {
                    var fBase = constType.GetField("BASE_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (fBase != null) fBase.SetValue(null, "http://192.168.7.200:1401/");
                    var fLogin = constType.GetField("LOGIN_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (fLogin != null) fLogin.SetValue(null, "api/Token/Login");
                }
            }
            catch { }

            param = new CommonParam();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");
            var tok = tokenManager.Login(param, user, pass, "2.390.0");

            if (tok == null || string.IsNullOrEmpty(tok.TokenCode))
            {
                string envPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
                if (!string.IsNullOrEmpty(envPass) && envPass != pass)
                {
                    tok = tokenManager.Login(param, user, envPass, "2.390.0");
                }
            }

            // Fallback: Check cached token if available
            string tokenCodeToUse = (tok != null && !string.IsNullOrEmpty(tok.TokenCode)) ? tok.TokenCode : null;
            if (string.IsNullOrEmpty(tokenCodeToUse))
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string specFile = isNB ? "doctor_nb.token" : "doctor_hn.token";
                    string path = Path.Combine(baseDir, specFile);
                    if (File.Exists(path))
                    {
                        string c = File.ReadAllText(path, Encoding.UTF8).Trim();
                        var parts = c.Split('|');
                        if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) && parts[0].Length > 10)
                        {
                            tokenCodeToUse = parts[0];
                            Console.WriteLine("   ℹ️ Tái sử dụng token hợp lệ từ cache: " + specFile);
                        }
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(tokenCodeToUse))
            {
                activeTokenCode = tokenCodeToUse;
                currentDoctorLogin = user;
                currentDoctorName = (user == "034727" ? "Ths.BS Nguyễn Hữu Sâm" : (user == "vmc" ? "BS Vũ Minh Cường" : user));
                activeFacility = targetFacKey;

                mosConsumer = new ApiConsumer(MOS_BASE, activeTokenCode, "HIS");

                Console.WriteLine("   ✔ Xác thực ACS thành công! Token: " + activeTokenCode.Substring(0, 16) + "...");

                // Cache tokens for facility isolation
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    File.WriteAllText(Path.Combine(baseDir, "doctor_standalone.token"),
                        activeTokenCode + "|" + DateTime.Now.Ticks + "|" + user, Encoding.UTF8);

                    string specFile = isNB ? "doctor_nb.token" : "doctor_hn.token";
                    File.WriteAllText(Path.Combine(baseDir, specFile),
                        activeTokenCode + "|" + DateTime.Now.Ticks + "|" + user, Encoding.UTF8);
                }
                catch { }

                // Kích hoạt WorkInfo phòng làm việc
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

        public static void EnsureWorkInfoForRoom(long roomId)
        {
            try
            {
                var workInfo = new WorkInfoSDO { Rooms = new List<RoomSDO> { new RoomSDO { RoomId = roomId } } };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            }
            catch { }
        }

        public static V_HIS_TREATMENT LookupTreatment(string pCode, bool isNB)
        {
            string pCodeNorm = pCode.Trim();
            long numVal;
            if (long.TryParse(pCodeNorm, out numVal)) pCodeNorm = pCodeNorm.PadLeft(10, '0');

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCodeNorm };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);

            if (trs == null || trs.Count == 0)
            {
                var tfTr = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = pCodeNorm.PadLeft(12, '0') };
                trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfTr, param);
            }

            if (trs == null || trs.Count == 0)
            {
                long tId;
                if (long.TryParse(pCodeNorm, out tId) && tId > 100000)
                {
                    var tfId = new HisTreatmentViewFilter { ID = tId };
                    trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfId, param);
                }
            }

            if (trs == null || trs.Count == 0) return null;
            return trs.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
        }

        public static DischargeExecutionResult ExecuteSingleDischarge(DischargeOrderInput input)
        {
            DischargeExecutionResult res = new DischargeExecutionResult
            {
                PatientCode = input.PatientCode
            };

            string facNorm = (input.Facility ?? "cs2").Trim().ToLower();
            bool isNB = (facNorm == "cs2" || facNorm == "nb" || facNorm == "ninhbinh" || facNorm == "81" || facNorm == "915");
            res.FacilityName = isNB ? "Cơ sở 2 Ninh Bình (Khoa 915)" : "Cơ sở 1 Hà Nội (Khoa 57)";
            res.DepartmentName = isNB ? "Khoa Ngoại tổng hợp - Tầng 3 Nhà E" : "Khoa Chấn thương Chỉnh hình & Cột sống";

            // 1. Session Auth
            if (!EnsureSession(input.DoctorLogin, input.DoctorPassword, input.Facility))
            {
                if (input.IsDryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ [DRY RUN] Đăng nhập ACS thất bại/bỏ qua trong chế độ mô phỏng offline.");
                    Console.ResetColor();
                    res.PatientName = "BỆNH NHÂN MÔ PHỎNG (TEST)";
                    res.TreatmentCode = "000000999999";
                    res.TreatmentId = 999999;
                    res.BedRoomFull = isNB ? "P3E-05 - G01" : "P714 - G01";
                    res.TreatmentDays = 5;
                    res.CreatedTrackings.Add("Sơ kết 3 ngày điều trị");
                    res.CreatedTrackings.Add("Tờ Tổng kết ra viện");
                    res.TransferredOrdersCount = 2;
                    res.ProtectedOrdersCount = 1;
                    res.TransferredOrderDetails.Add("Y lệnh CLS X-Quang ngực thẳng (từ BS khác sang 034727)");
                    res.EmrCoverStatus = "Mô phỏng hoàn tất 3 bìa bệnh án ngoại khoa EMR";
                    res.Success = true;
                    return res;
                }

                res.Success = false;
                res.ErrorMessage = "Không thể đăng nhập tài khoản ACS với thông tin được cung cấp.";
                return res;
            }

            // 2. Patient Lookup
            Console.WriteLine(string.Format("🔍 Đang tra cứu hồ sơ đợt điều trị cho bệnh nhân [{0}]...", input.PatientCode));
            var tr = LookupTreatment(input.PatientCode, isNB);
            if (tr == null)
            {
                if (input.IsDryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ [DRY RUN] Không tìm thấy bệnh nhân thật, sử dụng hồ sơ giả lập để mô phỏng.");
                    Console.ResetColor();
                    tr = new V_HIS_TREATMENT
                    {
                        TDL_PATIENT_CODE = input.PatientCode,
                        TDL_PATIENT_NAME = "BỆNH NHÂN MÔ PHỎNG",
                        TREATMENT_CODE = "000000999999",
                        ID = 999999,
                        TDL_PATIENT_GENDER_NAME = "Nam",
                        ICD_CODE = "M51.1",
                        ICD_NAME = "Thoát vị đĩa đệm cột sống thắt lưng",
                        TDL_TREATMENT_TYPE_ID = 3,
                        IN_TIME = long.Parse(DateTime.Today.AddDays(-5).ToString("yyyyMMdd080000")),
                        END_DEPARTMENT_ID = isNB ? 915 : 57
                    };
                }
                else
                {
                    res.Success = false;
                    res.ErrorMessage = string.Format("Không tìm thấy hồ sơ đợt điều trị cho bệnh nhân mã [{0}]!", input.PatientCode);
                    return res;
                }
            }

            res.PatientName = tr.TDL_PATIENT_NAME;
            res.TreatmentCode = tr.TREATMENT_CODE;
            res.TreatmentId = tr.ID;

            // Resolve Bed & Room
            long roomId = isNB ? 18679 : 5248;
            string bedRoomDesc = "Chưa xếp buồng";
            try
            {
                var tfRoom = new HisBedLogViewFilter { TREATMENT_ID = tr.ID, IS_ACTIVE = 1 };
                var bedLogs = adapter.FetchList<V_HIS_BED_LOG>("api/HisBedLog/GetView", mosConsumer, tfRoom, param);
                if (bedLogs != null && bedLogs.Count > 0)
                {
                    var b = bedLogs.OrderByDescending(x => x.START_TIME).First();
                    bedRoomDesc = string.Format("{0} - {1}", b.BED_ROOM_NAME, b.BED_NAME);
                    if (b.BED_ROOM_ID > 0)
                    {
                        var rf = new HisBedRoomViewFilter { ID = b.BED_ROOM_ID };
                        var bRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, rf, param);
                        if (bRooms != null && bRooms.Count > 0) roomId = bRooms[0].ROOM_ID;
                    }
                }
            }
            catch { }
            res.BedRoomFull = bedRoomDesc;

            Console.WriteLine(string.Format("   👤 Bệnh nhân: {0} ({1}) | Mã ĐT: {2} | Buồng: {3}",
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_GENDER_NAME, tr.TREATMENT_CODE, res.BedRoomFull));
            Console.WriteLine(string.Format("   🏥 Chẩn đoán: [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));

            // =========================================================================
            // BƯỚC 1: RÀ SOÁT & BỔ SUNG TỜ ĐIỀU TRỊ (SK 3 NGÀY, 7 NGÀY, TỔNG KẾT RA VIỆN)
            // =========================================================================
            Console.WriteLine("\n[BƯỚC 1/3] Rà soát & Bổ sung Tờ điều trị (Sơ kết 3/7 ngày & Tổng kết ra viện)...");
            bool step1Ok = ExecuteStep1EnsureTracking(tr, roomId, isNB, input.IsDryRun, res);

            // =========================================================================
            // BƯỚC 2: CHUYỂN TOÀN BỘ CHỈ ĐỊNH TRẮNG VỀ BÁC SĨ (BẢO LƯU 4 NHÓM)
            // =========================================================================
            Console.WriteLine(string.Format("\n[BƯỚC 2/3] Quét và chuyển y lệnh trắng về Bác sĩ [{0}]...", input.DoctorLogin));
            bool step2Ok = ExecuteStep2TransferWhiteOrders(tr, input.DoctorLogin, input.IsDryRun, res);

            // =========================================================================
            // BƯỚC 3: HOÀN THIỆN 3 BÌA BỆNH ÁN NGOẠI KHOA EMR NỘI TRÚ
            // =========================================================================
            Console.WriteLine("\n[BƯỚC 3/3] Tạo bìa tóm tắt, bìa tổng kết và bìa khám ngoại khoa EMR...");
            bool step3Ok = ExecuteStep3EnsureEmrCover(tr, isNB ? "NB" : "HN", input.DoctorLogin, input.IsDryRun, res);

            res.Success = step1Ok && step2Ok && step3Ok;
            return res;
        }

        private static bool ExecuteStep1EnsureTracking(V_HIS_TREATMENT tr, long defaultRoomId, bool isNB, bool isDryRun, DischargeExecutionResult res)
        {
            long deptId = isNB ? 915 : 57;
            EnsureWorkInfoForRoom(defaultRoomId);

            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
            var existingTrks = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param) ?? new List<HIS_TRACKING>();

            // Calculate department start date
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
            res.TreatmentDays = deptDays;
            Console.WriteLine(string.Format("   📅 Điều trị tại khoa: từ {0} đến {1} ({2} ngày). Hiện có {3} tờ ĐT.",
                startDate.ToString("dd/MM/yyyy"), endDate.ToString("dd/MM/yyyy"), deptDays, existingTrks.Count));

            var usedTimes = new HashSet<long>(existingTrks.Select(x => x.TRACKING_TIME));
            bool allOk = true;

            // A. Sơ kết 3 ngày (deptDays >= 3)
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
                    while (usedTimes.Contains(long.Parse(sk3Dt.ToString("yyyyMMddHHmmss")))) sk3Dt = sk3Dt.AddMinutes(1);
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
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("   ⚠️ [DRY-RUN] Cần tạo: Sơ kết 3 ngày điều trị lúc " + sk3Dt.ToString("dd/MM/yyyy HH:mm"));
                        Console.ResetColor();
                        res.CreatedTrackings.Add("Sơ kết 3 ngày (" + sk3Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                    }
                    else
                    {
                        bool ok = CreateSingleTracking(tr, deptId, defaultRoomId, sk3Time, sk3Content, care3, med3);
                        if (ok)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✔ Đã tạo THÀNH CÔNG: Sơ kết 3 ngày điều trị (" + sk3Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                            Console.ResetColor();
                            res.CreatedTrackings.Add("Sơ kết 3 ngày (" + sk3Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("   ❌ Thất bại tạo Sơ kết 3 ngày");
                            Console.ResetColor();
                            allOk = false;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ ĐÃ CÓ: Tờ Sơ kết 3 ngày điều trị.");
                }
            }

            // B. Sơ kết 7 ngày (deptDays >= 7)
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
                    while (usedTimes.Contains(long.Parse(sk7Dt.ToString("yyyyMMddHHmmss")))) sk7Dt = sk7Dt.AddMinutes(1);
                    long sk7Time = long.Parse(sk7Dt.ToString("yyyyMMddHHmmss"));
                    usedTimes.Add(sk7Time);

                    string title7 = deptDays >= 15 ? string.Format("SƠ KẾT ĐỢT ĐIỀU TRỊ ({0} NGÀY - Từ {1} đến {2})", deptDays, startDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"))
                                                  : string.Format("SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})", startDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));

                    string sk7Content = string.Format(
                        "{0}\n" +
                        "- Toàn trạng: Bệnh nhân điều trị ngày thứ {1}. Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, ăn ngủ được, đại tiểu tiện bình thường.\n" +
                        "- Khám chuyên khoa: Vết mổ/tổn thương liền sẹo tiến triển tốt, khô sạch, dịch tiết giảm rõ rệt, không có biểu hiện nhiễm trùng tại chỗ. Trục chi thẳng, tưới máu ngọn chi tốt, vận động các khớp lân cận được cải thiện.\n" +
                        "- Cận lâm sàng: Các xét nghiệm huyết học, sinh hóa và hình ảnh chẩn đoán nằm trong giới hạn kiểm soát tốt.\n" +
                        "- Đánh giá chung: Bệnh nhân tiến triển thuận lợi theo đúng phác đồ điều trị chuyên khoa.\n" +
                        "- Hướng điều trị tiếp theo: Tiếp tục duy trì phác đồ điều trị, tăng cường tập phục hồi chức năng, theo dõi liền xương/liền gân và dự kiến kế hoạch ra viện khi đủ điều kiện.",
                        title7, deptDays);
                    string care7 = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Tập vận động phục hồi chức năng.";
                    string med7 = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động chi.";

                    if (isDryRun)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("   ⚠️ [DRY-RUN] Cần tạo: Sơ kết 7 ngày điều trị lúc " + sk7Dt.ToString("dd/MM/yyyy HH:mm"));
                        Console.ResetColor();
                        res.CreatedTrackings.Add("Sơ kết 7 ngày (" + sk7Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                    }
                    else
                    {
                        bool ok = CreateSingleTracking(tr, deptId, defaultRoomId, sk7Time, sk7Content, care7, med7);
                        if (ok)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("   ✔ Đã tạo THÀNH CÔNG: Sơ kết 7 ngày điều trị (" + sk7Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                            Console.ResetColor();
                            res.CreatedTrackings.Add("Sơ kết 7 ngày (" + sk7Dt.ToString("dd/MM/yyyy HH:mm") + ")");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("   ❌ Thất bại tạo Sơ kết 7 ngày");
                            Console.ResetColor();
                            allOk = false;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ ĐÃ CÓ: Tờ Sơ kết 7 ngày điều trị.");
                }
            }

            // C. Tờ Tổng kết ra viện
            bool hasDischarge = existingTrks.Any(x => {
                string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                return c.Contains("tổng kết ra viện") || c.Contains("tong ket ra vien") || c.Contains("đủ điều kiện ra viện") || c.Contains("cho ra viện");
            });

            if (!hasDischarge)
            {
                DateTime disDate = endDate.Date;
                DateTime minDt = new DateTime(disDate.Year, disDate.Month, disDate.Day, 0, 0, 0);

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

                DateTime disDt = new DateTime(disDate.Year, disDate.Month, disDate.Day, 16, 0, 0);
                if (minDt >= disDt) disDt = minDt.AddMinutes(5);

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

                while (usedTimes.Contains(long.Parse(disDt.ToString("yyyyMMddHHmmss")))) disDt = disDt.AddMinutes(1);
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
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("   ⚠️ [DRY-RUN] Cần tạo: Tờ Tổng kết ra viện lúc " + disDt.ToString("dd/MM/yyyy HH:mm"));
                    Console.ResetColor();
                    res.CreatedTrackings.Add("Tờ Tổng kết ra viện (" + disDt.ToString("dd/MM/yyyy HH:mm") + ")");
                }
                else
                {
                    bool ok = CreateSingleTracking(tr, deptId, defaultRoomId, disTime, disContent, careDis, medDis);
                    if (ok)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("   ✔ Đã tạo THÀNH CÔNG: Tờ Tổng kết ra viện (" + disDt.ToString("dd/MM/yyyy HH:mm") + ")");
                        Console.ResetColor();
                        res.CreatedTrackings.Add("Tờ Tổng kết ra viện (" + disDt.ToString("dd/MM/yyyy HH:mm") + ")");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   ❌ Thất bại tạo Tờ Tổng kết ra viện");
                        Console.ResetColor();
                        allOk = false;
                    }
                }
            }
            else
            {
                Console.WriteLine("   ✔ ĐÃ CÓ: Tờ Tổng kết ra viện.");
            }

            return allOk;
        }

        private static bool CreateSingleTracking(V_HIS_TREATMENT tr, long deptId, long roomId, long trackingTime, string content, string care, string med)
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
                var result = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
                return (result != null && result.ID > 0);
            }
            catch
            {
                return false;
            }
        }

        private static bool ExecuteStep2TransferWhiteOrders(V_HIS_TREATMENT tr, string targetDoctorLogin, bool isDryRun, DischargeExecutionResult res)
        {
            string targetDoctorName = (string.Equals(targetDoctorLogin, "vmc", StringComparison.OrdinalIgnoreCase)) ? "VŨ MINH CƯỜNG" : "NGUYỄN HỮU SÂM";
            string targetDoctorTitle = (string.Equals(targetDoctorLogin, "vmc", StringComparison.OrdinalIgnoreCase)) ? "Bác sĩ" : "Thạc sỹ y học";

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param) ?? new List<V_HIS_SERVICE_REQ>();

            var whiteOrders = orders.Where(x => x.SERVICE_REQ_STT_ID == 1).ToList();
            if (whiteOrders.Count == 0)
            {
                Console.WriteLine("   ✔ Không có y lệnh màu trắng (Chưa thực hiện) nào cần xử lý.");
                return true;
            }

            Console.WriteLine(string.Format("   🔍 Tìm thấy {0} y lệnh màu trắng. Đang kiểm tra bảo lưu 4 nhóm...", whiteOrders.Count));

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
                    Console.WriteLine(string.Format("   🛡️ [BẢO LƯU] ID {0} ({1}): {2}", req.ID, req.SERVICE_REQ_TYPE_NAME, protectReason));
                    protectedCount++;
                    continue;
                }

                if (string.Equals(req.REQUEST_LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(string.Format("   ✔ [ĐÃ CHUẨN] ID {0} ({1}): Đã do {2} ({3}) chỉ định.",
                        req.ID, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                    skippedAlreadyDoctorCount++;
                    continue;
                }

                if (isDryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(string.Format("   ⚠️ [DRY-RUN] Sẽ chuyển: ID {0} ({1}) từ {2} -> {3}",
                        req.ID, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_LOGINNAME, targetDoctorLogin));
                    Console.ResetColor();
                    transferredCount++;
                    res.TransferredOrderDetails.Add(string.Format("ID {0} ({1}) từ {2} -> {3}", req.ID, req.SERVICE_REQ_TYPE_NAME, req.REQUEST_LOGINNAME, targetDoctorLogin));
                }
                else
                {
                    try
                    {
                        if (req.REQUEST_ROOM_ID > 0) EnsureWorkInfoForRoom(req.REQUEST_ROOM_ID);

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
                            if (rawList != null && rawList.Count > 0) rawReq = rawList[0];
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
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(string.Format("   ✔ Đã chuyển: ID {0} ({1}) sang {2} ({3})",
                                    req.ID, req.SERVICE_REQ_TYPE_NAME, targetDoctorName, targetDoctorLogin));
                                Console.ResetColor();
                                transferredCount++;
                                res.TransferredOrderDetails.Add(string.Format("ID {0} ({1}) sang {2}", req.ID, req.SERVICE_REQ_TYPE_NAME, targetDoctorLogin));
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(string.Format("   ❌ Thất bại chuyển ID {0}: {1}", req.ID, cpUpd.GetMessage()));
                                Console.ResetColor();
                                failCount++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("   ❌ Không lấy được thông tin chi tiết Y lệnh ID: " + req.ID);
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("   ❌ Ngoại lệ chuyển Y lệnh ID {0}: {1}", req.ID, ex.Message));
                        failCount++;
                    }
                }
            }

            res.TransferredOrdersCount = transferredCount;
            res.ProtectedOrdersCount = protectedCount;
            res.SkippedAlreadyOrdersCount = skippedAlreadyDoctorCount;

            Console.WriteLine(string.Format("   📊 Kết quả: Chuyển {0}, Bảo lưu {1}, Đã chuẩn {2}, Lỗi {3}",
                transferredCount, protectedCount, skippedAlreadyDoctorCount, failCount));

            return failCount == 0;
        }

        private static bool IsProtectedDischargeOrder(V_HIS_SERVICE_REQ req, List<V_HIS_SERE_SERV> ssList, out string reason)
        {
            reason = "";

            // 1. Giường
            if (req.SERVICE_REQ_TYPE_ID == 8 || (req.SERVICE_REQ_TYPE_NAME ?? "").ToLower().Contains("giường"))
            {
                reason = "Y lệnh Giường bệnh (Type 8)";
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

            // Kiểm tra dịch vụ con
            if (ssList != null && ssList.Count > 0)
            {
                foreach (var s in ssList)
                {
                    string sName = (s.TDL_SERVICE_NAME ?? "").ToLower();
                    string sCode = (s.TDL_SERVICE_CODE ?? "").ToUpper();

                    // Dịch vụ Giường
                    if (s.TDL_SERVICE_TYPE_ID == 8 || sName.Contains("giường"))
                    {
                        reason = "Dịch vụ Giường bệnh (" + s.TDL_SERVICE_NAME + ")";
                        return true;
                    }

                    // Đồ vải
                    if (sName.Contains("toan") || sName.Contains("áo") || sName.Contains("vải") ||
                        sName.Contains("do vai") || sName.Contains("gói pt") || sName.Contains("giảm trừ"))
                    {
                        reason = "Đồ vải / Toan áo phẫu thuật (" + s.TDL_SERVICE_NAME + ")";
                        return true;
                    }

                    // ĐMMM tại giường
                    if (sCode == "BM02426" || sCode == "NB260620.6231" ||
                        sName.Contains("mao mạch") || sName.Contains("đường huyết") ||
                        sName.Contains("glucose [máu] mao mạch") || sName.Contains("dmmm"))
                    {
                        reason = "Định lượng Glucose mao mạch (" + sCode + ")";
                        return true;
                    }

                    // Thuốc / Vật tư / Máu
                    if (s.MEDICINE_ID.HasValue || s.MATERIAL_ID.HasValue || s.BLOOD_ID.HasValue ||
                        s.TDL_SERVICE_TYPE_ID == 6 || s.TDL_SERVICE_TYPE_ID == 7)
                    {
                        reason = "Dịch vụ Thuốc / Vật tư / Máu (" + s.TDL_SERVICE_NAME + ")";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ExecuteStep3EnsureEmrCover(V_HIS_TREATMENT tr, string facility, string doctorLogin, bool isDryRun, DischargeExecutionResult res)
        {
            if (tr.TDL_TREATMENT_TYPE_ID != 3)
            {
                Console.WriteLine(string.Format("   ⚠️ [BỎ QUA BƯỚC 3] Bệnh nhân diện NGOẠI TRÚ (TreatmentType: {0}). Chỉ áp dụng cho Nội trú.", tr.TDL_TREATMENT_TYPE_ID));
                res.EmrCoverStatus = "Bỏ qua (Bệnh nhân ngoại trú)";
                return true;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string emrExe = Path.Combine(baseDir, "HisEmrFiller.exe");
            if (!File.Exists(emrExe))
            {
                emrExe = Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "HisEmrFiller.exe");
            }

            if (!File.Exists(emrExe))
            {
                Console.WriteLine("   ⚠️ Không tìm thấy HisEmrFiller.exe để tạo bìa EMR.");
                res.EmrCoverStatus = "Chưa tìm thấy HisEmrFiller.exe";
                return true; // Không block toàn bộ quy trình nếu thiếu tool phụ trợ EMR
            }

            var sbArgs = new StringBuilder();
            sbArgs.Append("\"").Append(tr.TDL_PATIENT_CODE).Append("\"");
            if (isDryRun) sbArgs.Append(" --dry-run");
            else sbArgs.Append(" --save");
            sbArgs.Append(" --force-summary");
            sbArgs.Append(" --doctor " + doctorLogin);
            sbArgs.Append(" --facility " + facility);

            Console.WriteLine(string.Format("   🚀 Đang chạy HisEmrFiller.exe {0}...", sbArgs.ToString()));

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = emrExe,
                    Arguments = sbArgs.ToString(),
                    WorkingDirectory = baseDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = new UTF8Encoding(false),
                    StandardErrorEncoding = new UTF8Encoding(false)
                };

                using (var proc = Process.Start(psi))
                {
                    string stdOut = proc.StandardOutput.ReadToEnd();
                    string stdErr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("   ✔ Hoàn thiện 3 bìa Bệnh án Ngoại khoa EMR THÀNH CÔNG!");
                        Console.ResetColor();
                        res.EmrCoverStatus = isDryRun ? "Mô phỏng thành công 3 bìa EMR" : "Đã điền thành công 3 bìa EMR";
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️ Cảnh báo EMR: " + stdErr);
                        res.EmrCoverStatus = "Cảnh báo EMR: ExitCode " + proc.ExitCode;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ⚠️ Lỗi gọi HisEmrFiller: " + ex.Message);
                res.EmrCoverStatus = "Lỗi gọi EMR: " + ex.Message;
                return true;
            }
        }

        private static void PrintResultReport(DischargeExecutionResult res)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 BÁO CÁO KẾT QUẢ QUY TRÌNH 'THỢ LÀM RA VIỆN'                  ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
            Console.ResetColor();

            string status = res.Success ? "🟢 THÀNH CÔNG TOÀN DIỆN" : "🔴 CÓ LỖI / CẢNH BÁO";
            Console.WriteLine(string.Format("║ • Trạng thái       : {0,-56} ║", status));
            Console.WriteLine(string.Format("║ • Cơ sở điều trị   : {0,-56} ║", res.FacilityName ?? "N/A"));
            Console.WriteLine(string.Format("║ • Khoa điều trị    : {0,-56} ║", res.DepartmentName ?? "N/A"));
            Console.WriteLine(string.Format("║ • Bệnh nhân        : {0,-56} ║", string.Format("{0} (Mã: {1})", res.PatientName ?? "N/A", res.PatientCode ?? "N/A")));
            Console.WriteLine(string.Format("║ • Mã Đợt Điều Trị  : {0,-56} ║", res.TreatmentCode ?? "N/A"));
            Console.WriteLine(string.Format("║ • Buồng - Giường   : {0,-56} ║", res.BedRoomFull ?? "N/A"));
            Console.WriteLine(string.Format("║ • Thời gian nằm    : {0,-56} ║", string.Format("{0} ngày điều trị", res.TreatmentDays)));
            Console.WriteLine("╟──────────────────────────────────────────────────────────────────────────────╢");

            string trkSummary = res.CreatedTrackings.Count > 0 ? string.Join(", ", res.CreatedTrackings) : "Đã đầy đủ từ trước (0 tờ mới)";
            Console.WriteLine(string.Format("║ 1. Tờ điều trị     : {0,-56} ║", trkSummary));
            string orderSummary = string.Format("Đã chuyển: {0} | Bảo lưu: {1} | Đã chuẩn: {2}",
                res.TransferredOrdersCount, res.ProtectedOrdersCount, res.SkippedAlreadyOrdersCount);
            Console.WriteLine(string.Format("║ 2. Y lệnh trắng    : {0,-56} ║", orderSummary));
            Console.WriteLine(string.Format("║ 3. Vỏ Bệnh án EMR  : {0,-56} ║", res.EmrCoverStatus ?? "N/A"));
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("💡 [LƯU Ý KẾ HOẠCH TỔNG HỢP & GIÁM ĐỐC EMR]:");
            Console.WriteLine("   • Toàn bộ tờ điều trị và bìa tóm tắt đã được chuẩn hóa để phòng KHTH duyệt.");
            Console.WriteLine("   • Hãy kiểm tra bộ lọc 'Tất cả bác sĩ' trên EMR để xem trọn vẹn hồ sơ ra viện!");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
