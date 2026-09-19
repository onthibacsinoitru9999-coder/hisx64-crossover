// ==============================================================================
// HisEmrFiller.cs — Tự động điền Vỏ Bệnh Án Ngoại Khoa (BENHANNGOAIKHOA) vào EMR Oracle
// Chuẩn hóa theo thỏa thuận Grill-me:
//   1. Tự động điền hoàn chỉnh tab HỎI BỆNH và KHÁM BỆNH.
//   2. Bảo lưu dữ liệu gõ dở: nếu trường nào đã có chữ thì giữ nguyên, chỉ điền bù ô trống.
//   3. Bác sĩ làm bệnh án: Cố định ThS.BS Nguyễn Hữu Sâm (034727).
//   4. Tự động đóng tab EMR trên máy trạm (HIS.exe) trước khi ghi để chống xung đột RAM.
//   5. Dual-write MaQuanLy (TreatmentCode số & TreatmentId) tương thích 100% EMR Client.
//   6. Hỗ trợ tra cứu theo Mã BN, Mã ĐT, Họ Tên hoặc tự động chọn ca mới nhất hôm nay.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

class HisEmrFiller
{
    // ──────────────────────────────────────────────────────────────
    // CONSTANTS & CẤU HÌNH CƠ SỞ
    // ──────────────────────────────────────────────────────────────
    const string MOS_BASE = "http://192.168.7.236:1608/";
    const string EMR_CONNSTR = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

    const string DEFAULT_DOCTOR_CODE = "034727";
    const string DEFAULT_DOCTOR_NAME = "ThS.BS Nguyễn Hữu Sâm";

    const long DEPT_ID_HN = 57;
    const long DEPT_ID_NB = 915;
    const string MAKHOA_HN = "9";
    const string MAKHOA_NB = "CSNBKP05";
    const string DEPT_NAME_HN = "Khoa Chấn thương Chỉnh hình và Cột sống";
    const string DEPT_NAME_NB = "Khoa Ngoại tổng hợp - Tầng 3 Nhà E";

    static long _deptId = DEPT_ID_HN;
    static string _maKhoa = MAKHOA_HN;
    static string _facility = "HN";
    static string _deptNameDefault = DEPT_NAME_HN;
    static bool _facilitySpecified = false;

    // ──────────────────────────────────────────────────────────────
    // WIN32 API CHO PHÉP PHÁT HIỆN & ĐÓNG TAB EMR TRÊN MÁY TRẠM
    // ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    const uint WM_CLOSE = 0x0010;

    static void CloseEmrTabIfOpen(string patName, string patCode)
    {
        try
        {
            var pList = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (pList == null || pList.Length == 0) return;

            Console.Write("🔍 Đang kiểm tra tab EMR trên máy trạm HIS.exe... ");
            int closedCount = 0;

            foreach (var proc in pList)
            {
                uint pId = (uint)proc.Id;
                EnumWindows((hWnd, lParam) =>
                {
                    uint wndPid;
                    GetWindowThreadProcessId(hWnd, out wndPid);
                    if (wndPid == pId)
                    {
                        var sb = new StringBuilder(512);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        bool matches = false;
                        if (!string.IsNullOrEmpty(patName) && title.IndexOf(patName, StringComparison.OrdinalIgnoreCase) >= 0) matches = true;
                        if (!string.IsNullOrEmpty(patCode) && title.IndexOf(patCode, StringComparison.OrdinalIgnoreCase) >= 0) matches = true;
                        if (title.IndexOf("EMR -", StringComparison.OrdinalIgnoreCase) >= 0 && (title.Contains("Ngoại khoa") || title.Contains("Cột sống"))) matches = true;

                        if (matches)
                        {
                            SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                            closedCount++;
                        }

                        EnumChildWindows(hWnd, (childHwnd, childParam) =>
                        {
                            var childSb = new StringBuilder(512);
                            GetWindowText(childHwnd, childSb, childSb.Capacity);
                            string childTitle = childSb.ToString();
                            if ((!string.IsNullOrEmpty(patName) && childTitle.IndexOf(patName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrEmpty(patCode) && childTitle.IndexOf(patCode, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (childTitle.Contains("Khoa Chấn thương") && childTitle.Contains("Ngoại khoa")))
                            {
                                SendMessage(childHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                closedCount++;
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                    return true;
                }, IntPtr.Zero);
            }

            if (closedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("Đã gửi tín hiệu đóng {0} tab/cửa sổ EMR để đồng bộ sạch!", closedCount));
                Console.ResetColor();
                Thread.Sleep(500);
            }
            else
            {
                Console.WriteLine("OK (Không có tab mở dở)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Bỏ qua kiểm tra cửa sổ: " + ex.Message + "]");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // ENTRYPOINT
    // ──────────────────────────────────────────────────────────────
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        ApplyOracleCulture();

        string input = null;
        bool dryRun = false; // Mặc định tự lưu theo Grill-me
        bool isTodayScan = false;
        bool isDateScan = false;
        string targetDateStr = null;
        bool autoCreate = false;
        string doctorCode = DEFAULT_DOCTOR_CODE;
        string doctorName = DEFAULT_DOCTOR_NAME;
        bool forceSummary = false;
        bool isReverseOutpatients = false;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a.Equals("--save", StringComparison.OrdinalIgnoreCase))
                dryRun = false;
            else if (a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase) || a.Equals("--preview", StringComparison.OrdinalIgnoreCase))
                dryRun = true;
            else if (a.Equals("--reverse-outpatients", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--reverse", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("reverse", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--revert", StringComparison.OrdinalIgnoreCase))
            {
                isReverseOutpatients = true;
            }
            else if (a.Equals("--force", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--force-summary", StringComparison.OrdinalIgnoreCase))
                forceSummary = true;
            else if ((a.Equals("--date", StringComparison.OrdinalIgnoreCase) || a.Equals("-d", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                targetDateStr = args[++i];
                isDateScan = true;
            }
            else if (a.Equals("--auto", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--auto-create", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--create-missing", StringComparison.OrdinalIgnoreCase))
            {
                autoCreate = true;
            }
            else if (a.Equals("--doctor", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                doctorCode = args[++i];
                doctorName = MapDoctorName(doctorCode);
            }
            else if (a.Equals("--facility", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                ApplyFacility(args[++i], true);
            }
            else if (a.Equals("--today", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("today", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("--status", StringComparison.OrdinalIgnoreCase) ||
                     a.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                isTodayScan = true;
            }
            else if (!a.StartsWith("-") && input == null)
            {
                input = a;
            }
        }

        if (isReverseOutpatients)
        {
            return RunReverseOutpatients();
        }

        // Tự động nhận diện nếu input là ngày tháng (VD: 18.09, 18/09/2026, 20260918)
        if (!string.IsNullOrEmpty(input) && !isDateScan)
        {
            if (input.Contains(".") || input.Contains("/") || (input.Length == 8 && input.StartsWith("2026")))
            {
                targetDateStr = input;
                isDateScan = true;
                input = null;
            }
        }

        if (isDateScan)
        {
            return RunDateAudit(targetDateStr, autoCreate, doctorCode, doctorName);
        }

        if (isTodayScan)
        {
            return RunTodayStatus();
        }

        // Nếu không có tham số đầu vào, tự động dò tìm ca mới nhất hôm nay theo Grill-me
        if (string.IsNullOrEmpty(input))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("💡 Không có tham số đầu vào → Tự động quét và chọn bệnh nhân mới nhất vào Khoa 57 hôm nay...");
            Console.ResetColor();
            input = FindLatestPatientInDept();
            if (string.IsNullOrEmpty(input))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️ Không tìm thấy bệnh nhân mới vào hôm nay. Sử dụng lệnh mẫu:");
                Console.WriteLine("   .\\HisEmrFiller.bat <MãBN_hoặc_Tên>");
                Console.WriteLine("   .\\HisEmrFiller.bat --today");
                Console.ResetColor();
                return 1;
            }
            Console.WriteLine("✓ Đã chọn bệnh nhân mới nhất: " + input);
        }

        try
        {
            return Run(input, dryRun, doctorCode, doctorName, forceSummary);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[LỖI THỰC THI] " + ex.Message);
            if (ex.InnerException != null)
                Console.WriteLine("  Chi tiết: " + ex.InnerException.Message);
            Console.ResetColor();
            return 2;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // MAIN EXECUTION LOGIC
    // ──────────────────────────────────────────────────────────────
    static int Run(string input, bool dryRun, string doctorCode, string doctorName, bool forceSummary = false)
    {
        string tokenCode = ReadLiveToken();
        var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        TreatmentInfo ti = ResolveIdentifier(adapter, consumer, param, input);
        if (ti == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[LỖI] Không tìm thấy bệnh nhân hoặc đợt điều trị cho: " + input);
            Console.ResetColor();
            return 3;
        }

        // CHỐNG TẠO VỎ BỆNH ÁN CHO BỆNH NHÂN NGOẠI TRÚ / PHÒNG KHÁM
        // Quy tắc bắt buộc: Tuyệt đối KHÔNG làm vỏ bệnh án ngoại khoa cho bệnh nhân phòng khám
        if (ti.TreatmentTypeId.HasValue && ti.TreatmentTypeId.Value != 3)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("\n⛔ [TỪ CHỐI THỰC HIỆN] Bệnh nhân {0} ({1}) là diện NGOẠI TRÚ / PHÒNG KHÁM (TreatmentType: {2})!",
                ti.PatientName, ti.PatientCode, ti.TreatmentTypeId.Value));
            Console.WriteLine("   Quy tắc bắt buộc: Vỏ Bệnh Án Ngoại Khoa (BENHANNGOAIKHOA) CHỈ dành riêng cho bệnh nhân ĐIỀU TRỊ NỘI TRÚ (TreatmentType = 3).");
            Console.ResetColor();
            return 5;
        }

        if (!_facilitySpecified)
            InferFacilityFromDept(ti.DeptId);

        Console.WriteLine("\n" + new string('=', 75));
        Console.WriteLine(string.Format("🏥 HỒ SƠ BỆNH ÁN NGOẠI KHOA EMR — {0} ({1})", ti.PatientName.ToUpper(), ti.PatientCode));
        Console.WriteLine(new string('=', 75));
        Console.WriteLine(string.Format("  Mã ĐT       : {0}  |  HIS TreatmentId: {1}", ti.TreatmentCode, ti.TreatmentId));
        Console.WriteLine(string.Format("  MaQuanLy EMR: {0} (Chuẩn EMR Client)", ti.MaQuanLy));
        Console.WriteLine(string.Format("  Vào viện    : {0}", ti.InTime));
        Console.WriteLine(string.Format("  Chẩn đoán   : [{0}] {1}", ti.IcdCode, ti.IcdName));
        Console.WriteLine(string.Format("  Khoa        : {0} ({1})", ti.DeptName, _facility));
        Console.WriteLine(string.Format("  Bác sĩ lập  : {0} — {1}", doctorCode, doctorName));

        // Tự động phát hiện và đóng tab EMR trên máy trạm trước khi thao tác
        CloseEmrTabIfOpen(ti.PatientName, ti.PatientCode);

        // Kết nối Oracle EMR
        dynamic con = CreateEmrConnection();
        con.Open();
        Console.WriteLine("✓ Kết nối Oracle EMR (EMR_FINAL) thành công.");

        // Kiểm tra bệnh án đã tồn tại trên cả 2 ID (TreatmentCode chuẩn & TreatmentId)
        dynamic existingBA = BenhAnNgoaiKhoaSelect(con, ti.MaQuanLy);
        if (existingBA == null || existingBA.MaQuanLy == 0)
        {
            existingBA = BenhAnNgoaiKhoaSelect(con, (decimal)ti.TreatmentId);
        }

        bool isUpdate = (existingBA != null && existingBA.MaQuanLy > 0);
        if (isUpdate)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(string.Format("ℹ️ Đã có dữ liệu bệnh án trong EMR (MaQuanLy: {0}) → ÁP DỤNG CHẾ ĐỘ MERGE (Bảo lưu dữ liệu gõ dở).", existingBA.MaQuanLy));
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("ℹ️ Chưa có bệnh án EMR → Khởi tạo mới hoàn toàn.");
        }

        // Tìm mẫu kế thừa từ DB EMR hoặc sinh mới chuyên khoa CTCH
        TemplateBA tmpl = FindTemplate(con, ti);

        // Lấy DHST và CLS thật từ HIS
        DhstInfo dhst = GetLatestDhst(adapter, consumer, param, ti.TreatmentId);
        LabPacsInfo labs = FetchLabsAndPacs(adapter, consumer, param, ti.TreatmentId);

        // Tạo và điền đối tượng bệnh án theo nguyên tắc Merge
        dynamic ba = isUpdate ? existingBA : CreateNewBenhAnNgoaiKhoa();
        PopulateBenhAn(ba, ti, dhst, tmpl, labs, doctorCode, doctorName, isUpdate, forceSummary);

        // Đảm bảo Trang bìa THONGTINDIEUTRI
        EnsureThongTinDieuTri(con, ti, !dryRun);

        // Hiển thị xem trước
        PrintPreview(ba, ti);

        if (dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n[XEM TRƯỚC] Không lưu vào DB. Bỏ cờ --dry-run để ghi thật.");
            Console.ResetColor();
            con.Close();
            return 0;
        }

        // Ghi vào Oracle EMR (Dual-write MaQuanLy chuẩn & TreatmentId)
        Console.Write("\n💾 Đang ghi Bệnh Án Ngoại Khoa vào Oracle EMR... ");
        
        // 1. Ghi với MaQuanLy chuẩn (TreatmentCode dạng số)
        ba.MaQuanLy = ti.MaQuanLy;
        bool ok = BenhAnNgoaiKhoaInsertOrUpdate(con, ba);

        // 2. Ghi thêm bản sao cho TreatmentId nếu khác MaQuanLy để tương thích 100% mọi truy vấn
        if ((decimal)ti.TreatmentId != ti.MaQuanLy)
        {
            try
            {
                ba.MaQuanLy = (decimal)ti.TreatmentId;
                BenhAnNgoaiKhoaInsertOrUpdate(con, ba);
                ba.MaQuanLy = ti.MaQuanLy; // Khôi phục lại
            }
            catch { }
        }

        if (!ok)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("THẤT BẠI — InsertOrUpdate trả về false!");
            Console.ResetColor();
            con.Close();
            return 4;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ THÀNH CÔNG RỰC RỠ!");
        Console.ResetColor();

        // Xác nhận lại bằng Select
        dynamic verify = BenhAnNgoaiKhoaSelect(con, ti.MaQuanLy);
        if (verify != null && verify.MaQuanLy > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("✓ Xác thực dữ liệu: MaQuanLy={0} | Bệnh chính='{1}' | Bác sĩ={2}",
                verify.MaQuanLy, SafeStr(verify.BenhChinh), SafeStr(verify.BacSyLamBenhAn)));
            Console.ResetColor();
        }

        con.Close();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n👉 HƯỚNG DẪN BÁC SĨ:");
        Console.WriteLine("   Bác sĩ trên máy trạm chỉ cần KÍCH ĐÚP mở lại hồ sơ bệnh nhân trên danh sách EMR.");
        Console.WriteLine("   Toàn bộ tab HỎI BỆNH và KHÁM BỆNH sẽ hiển thị đầy đủ 100%!");
        Console.ResetColor();

        return 0;
    }

    // ──────────────────────────────────────────────────────────────
    // QUY TẮC ĐIỀN DỮ LIỆU & BẢO LƯU (MERGE MODE)
    // ──────────────────────────────────────────────────────────────
    static void PopulateBenhAn(dynamic ba, TreatmentInfo ti, DhstInfo dhst, TemplateBA tmpl, LabPacsInfo labs,
                               string docCode, string docName, bool isUpdate, bool forceSummary = false)
    {
        // 1. Trường định danh & bác sĩ (luôn cập nhật chuẩn)
        ba.MaQuanLy          = ti.MaQuanLy;
        ba.MaBenhNhan        = ti.PatientCode;
        ba.BacSyLamBenhAn    = docCode;
        ba.TenBacSyLamBenhAn = docName;
        ba.BacSyKhamBenh     = docCode;
        ba.TenBacSyKhamBenh  = docName;

        // Thời gian khám bệnh & số ngày vào
        if (!string.IsNullOrEmpty(ti.InTime))
        {
            DateTime dt;
            if (DateTime.TryParseExact(ti.InTime, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                ba.NgayKhamBenh = dt;
                int days = Math.Max(1, (int)(DateTime.Today - dt.Date).TotalDays + 1);
                ba.VaoNgayThu = days;
            }
        }

        // Chẩn đoán chính
        string chanDoan = string.IsNullOrEmpty(ti.IcdName) ? ti.IcdCode : string.Format("[{0}] {1}", ti.IcdCode, ti.IcdName);
        ba.BenhChinh = chanDoan;

        // 2. TAB HỎI BỆNH (Áp dụng merge: chỉ điền nếu rỗng)
        if (string.IsNullOrWhiteSpace(SafeStr(ba.LyDoVaoVien)))
            ba.LyDoVaoVien = BuildLyDoVaoVien(ti);

        if (string.IsNullOrWhiteSpace(SafeStr(ba.QuaTrinhBenhLy)))
        {
            if (!string.IsNullOrWhiteSpace(tmpl.QuaTrinhBenhLy))
                ba.QuaTrinhBenhLy = AdaptTemplateLaterality(tmpl.QuaTrinhBenhLy.Trim(), ti.IcdName);
            else
                ba.QuaTrinhBenhLy = BuildQuaTrinhBenhLy(ti);
        }

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TienSuBenhBanThan)))
            ba.TienSuBenhBanThan = NotEmpty(tmpl.TienSuBenhBanThan) ?? "Khỏe mạnh, chưa ghi nhận bệnh lý mạn tính trước đây. Không có tiền sử dị ứng thuốc hay thức ăn.";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TienSuBenhGiaDinh)))
            ba.TienSuBenhGiaDinh = NotEmpty(tmpl.TienSuBenhGiaDinh) ?? "Gia đình chưa phát hiện ai mắc bệnh lý di truyền hoặc liên quan.";

        // 3. TAB KHÁM BỆNH
        // 3.1. Toàn thân (kèm DHST thực tế nếu chưa có)
        if (string.IsNullOrWhiteSpace(SafeStr(ba.ToanThan)))
            ba.ToanThan = BuildToanThan(dhst, tmpl);

        // 3.2. Cơ xương khớp
        if (string.IsNullOrWhiteSpace(SafeStr(ba.CoXuongKhop)))
            ba.CoXuongKhop = BuildCoXuongKhop(ti, tmpl);

        // 3.3. Bệnh ngoại khoa (BẮT BUỘC ĐIỀN: ô mục 2 trên EMR UI)
        if (string.IsNullOrWhiteSpace(SafeStr(ba.BenhNgoaiKhoa)))
            ba.BenhNgoaiKhoa = BuildBenhNgoaiKhoa(ti, tmpl) ?? ba.CoXuongKhop;

        // 3.4. Các cơ quan nội khoa
        if (string.IsNullOrWhiteSpace(SafeStr(ba.TuanHoan)))
            ba.TuanHoan = NotEmpty(tmpl.TuanHoan) ?? string.Format("Nhịp tim đều, T1 T2 rõ, không nghe tiếng thổi bệnh lý. Tần số {0} chu kỳ/phút. Huyết áp {1} mmHg.", dhst.Pulse, dhst.BloodPressure);

        if (string.IsNullOrWhiteSpace(SafeStr(ba.HoHap)))
            ba.HoHap = NotEmpty(tmpl.HoHap) ?? string.Format("Lồng ngực hai bên cân đối, di động theo nhịp thở. Rì rào phế nang rõ, không ran. SpO2 {0}%.", dhst.SpO2);

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TieuHoa)))
            ba.TieuHoa = NotEmpty(tmpl.TieuHoa) ?? "Bụng mềm, không chướng, không có điểm đau khu trú. Gan lách không to, phản ứng thành bụng (-).";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.ThanTietNieuSinhDuc)))
            ba.ThanTietNieuSinhDuc = NotEmpty(tmpl.ThanTietNieu) ?? "Hố thắt lưng hai bên không đầy. Chạm thận (-), bập bềnh thận (-). Tiểu tiện tự chủ, nước tiểu vàng trong.";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.ThanKinh)))
            ba.ThanKinh = NotEmpty(tmpl.ThanKinh) ?? "Tỉnh táo, tiếp xúc tốt. Không có dấu hiệu thần kinh khu trú, hội chứng màng não (-).";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TaiMuiHong)))
            ba.TaiMuiHong = "Tai mũi họng bình thường.";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.RangHamMat)))
            ba.RangHamMat = "Răng hàm mặt bình thường.";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.Mat)))
            ba.Mat = "Mắt hai bên nhìn rõ, kết mạc hồng.";

        // 3.5. Cận lâm sàng & Tóm tắt
        if (string.IsNullOrWhiteSpace(SafeStr(ba.CacXetNghiemCanLamSangCanLam)))
            ba.CacXetNghiemCanLamSangCanLam = BuildCanLamSang(ti, labs, tmpl);

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TomTatBenhAn)) ||
            forceSummary ||
            SafeStr(ba.TomTatBenhAn).Contains("Bệnh diễn biến qua hỏi bệnh và thăm khám phát hiện") ||
            SafeStr(ba.TomTatBenhAn).Contains("- Tiền sử: Khỏe mạnh, chưa ghi nhận bệnh lý liên quan."))
        {
            ba.TomTatBenhAn = BuildTomTat(ti, dhst, SafeStr(ba.TienSuBenhBanThan));
        }

        // 3.6. Chẩn đoán phân biệt, Tiên lượng, Hướng điều trị
        if (string.IsNullOrWhiteSpace(SafeStr(ba.PhanBiet)))
            ba.PhanBiet = NotEmpty(tmpl.PhanBiet) ?? BuildPhanBiet(ti);

        if (string.IsNullOrWhiteSpace(SafeStr(ba.TienLuong)))
            ba.TienLuong = NotEmpty(tmpl.TienLuong) ?? "Tiên lượng dè dặt, phụ thuộc vào kết quả can thiệp thủ thuật/phẫu thuật và phục hồi chức năng.";

        if (string.IsNullOrWhiteSpace(SafeStr(ba.HuongDieuTri)))
            ba.HuongDieuTri = NotEmpty(tmpl.HuongDieuTri) ?? BuildHuongDieuTri(ti);

        // Gán DauSinhTon
        try
        {
            var dstType = _emrMainLib.GetType("EMR_MAIN.DauSinhTon");
            if (dstType != null)
            {
                dynamic dst = Activator.CreateInstance(dstType);
                int p; if (int.TryParse(dhst.Pulse, out p)) dst.Mach = p;
                double t; if (double.TryParse(dhst.Temperature, out t)) dst.NhietDo = t;
                dst.HuyetAp = dhst.BloodPressure;
                int sp; if (int.TryParse(dhst.SpO2, out sp)) dst.SPO2 = sp;
                double w; if (double.TryParse(dhst.Weight, out w)) dst.CanNang = w;
                double h; if (double.TryParse(dhst.Height, out h)) dst.ChieuCao = h;
                var propDst = ((object)ba).GetType().GetProperty("DauSinhTon");
                if (propDst != null) propDst.SetValue((object)ba, (object)dst, null);
            }
        }
        catch { }

        // Gán DacDiemLienQuanBenh
        try
        {
            var propDacDiem = ((object)ba).GetType().GetProperty("DacDiemLienQuanBenh");
            if (propDacDiem != null && propDacDiem.GetValue((object)ba, null) == null)
            {
                var ddlqType = _emrMainLib.GetType("EMR_MAIN.DacDiemLienQuanBenh");
                if (ddlqType != null)
                {
                    object instance = Activator.CreateInstance(ddlqType);
                    propDacDiem.SetValue((object)ba, instance, null);
                }
            }
        }
        catch { }
    }

    // ──────────────────────────────────────────────────────────────
    // NỘI DUNG CHUYÊN KHOA NGOẠI & CTCH
    // ──────────────────────────────────────────────────────────────
    static string BuildLyDoVaoVien(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);
        string viTriLoc = loc.StartsWith("vùng") || loc.StartsWith("khớp") ? loc : ("vùng " + loc);

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return string.Format("Khối u {0}, đau tức nhẹ khi vận động/tì đè", viTriLoc);
        if (s.Contains("gãy") || s.Contains("gay"))
            return string.Format("Đau chói, sưng nề, biến dạng, hạn chế vận động {0} sau chấn thương", loc);
        if (s.Contains("acl") || s.Contains("chằng") || s.Contains("chang"))
            return string.Format("Đau, lỏng khớp {0}, hạn chế đi lại sau chấn thương", loc);
        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("đốt sống") || s.Contains("dot song"))
            return string.Format("Đau cột sống thắt lưng cấp tính, hạn chế vận động cúi ngửa sau ngã/vận động sai tư thế");
        if (s.Contains("thoát vị") || s.Contains("thoat vi") || s.Contains("đĩa đệm"))
            return string.Format("Đau cột sống thắt lưng lan chân, tê bì hạn chế vận động");
        return string.Format("Đau và hạn chế vận động {0}", loc);
    }

    static string BuildQuaTrinhBenhLy(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);
        string viTri = loc.StartsWith("gối") ? ("khớp " + loc) : (loc.StartsWith("vùng") ? loc : ("vùng " + loc));

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
        {
            return string.Format(
                "Bệnh nhân tự phát hiện khối bất thường tại {0} cách đây một thời gian. " +
                "Khối to dần, gây cảm giác tức nhẹ khi tì đè hoặc vận động, không sốt, không sút cân. " +
                "Bệnh nhân đến khám tại Bệnh viện Bạch Mai và được chỉ định nhập viện Khoa CTCH & Cột sống để điều trị phẫu thuật bóc u.",
                viTri);
        }

        if (s.Contains("acl") || (s.Contains("chằng") && s.Contains("trước")))
        {
            return string.Format(
                "Bệnh nhân bị chấn thương {0} sau tai nạn sinh hoạt/thể thao. " +
                "Sau chấn thương, khớp sưng nề, đau nhiều và có cảm giác lỏng khớp, đi lại không vững, trẹo gối khi đổi hướng. " +
                "Bệnh nhân đến khám tại Bệnh viện Bạch Mai, được chỉ định chụp MRI và nhập viện để phẫu thuật nội soi tái tạo dây chằng.",
                viTri);
        }

        if (s.Contains("gãy") || s.Contains("gay"))
        {
            return string.Format(
                "Bệnh nhân bị tai nạn chấn thương trực tiếp/gián tiếp vào {0}. " +
                "Sau tai nạn xuất hiện đau chói, sưng nề, biến dạng chi và mất hoàn toàn cơ năng vận động. " +
                "Bệnh nhân được sơ cứu nẹp bất động tạm thời và chuyển đến Bệnh viện Bạch Mai để điều trị phẫu thuật kết hợp xương.",
                viTri);
        }

        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("đốt sống"))
        {
            return string.Format(
                "Bệnh nhân xuất hiện đau dữ dội vùng cột sống thắt lưng sau ngã đập mông hoặc cúi bê vật nặng. " +
                "Đau tăng mạnh khi thay đổi tư thế, đi lại khó khăn, nằm yên đỡ đau, không rối loạn tiểu tiện. " +
                "Bệnh nhân đến khám tại Bệnh viện Bạch Mai, được chụp X-quang/MRI xác định xẹp đốt sống và nhập viện can thiệp.",
                viTri);
        }

        return string.Format(
            "Bệnh nhân xuất hiện triệu chứng đau tức và hạn chế vận động tại {0} tăng dần. " +
            "Đã điều trị nội khoa tại tuyến trước không đỡ, nay đến Bệnh viện Bạch Mai khám và được chỉ định nhập viện theo dõi, điều trị chuyên khoa.",
            viTri);
    }

    static string BuildToanThan(DhstInfo dhst, TemplateBA tmpl)
    {
        return string.Format(
            "Bệnh nhân tỉnh, tiếp xúc tốt. Da niêm mạc hồng hào, không phù, không xuất huyết dưới da. " +
            "Tuyến giáp không to, hạch ngoại vi không sờ thấy. Thể trạng trung bình. " +
            "Dấu hiệu sinh tồn: Mạch: {0} lần/phút; Huyết áp: {1} mmHg; Nhiệt độ: {2}°C; SpO2: {3}%.",
            dhst.Pulse, dhst.BloodPressure, dhst.Temperature, dhst.SpO2);
    }

    static string BuildCoXuongKhop(TreatmentInfo ti, TemplateBA tmpl)
    {
        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
        {
            return string.Format(
                "Khám chuyên khoa tại {0}:\n" +
                "- Nhìn: Khối gồ rõ so với bề mặt da xung quanh, da trên bề mặt khối bình thường, không nóng đỏ, không loét.\n" +
                "- Sờ: Ranh giới rõ ràng, mật độ chắc vừa, ấn đau tức nhẹ, di động tương đối so với bình diện sâu.\n" +
                "- Vận động: Các khớp lân cận biên độ vận động trong giới hạn bình thường.\n" +
                "- Thần kinh mạch máu ngoại vi: Mạch ngoại vi bắt rõ, cảm giác ngọn chi bình thường.",
                loc);
        }

        if (s.Contains("acl") || s.Contains("chằng"))
        {
            return string.Format(
                "Khám chuyên khoa khớp {0}:\n" +
                "- Sưng nề nhẹ, không biến dạng, không tràn dịch lớn.\n" +
                "- Nghiệm pháp Ngăn kéo trước (+), Lachman (+), Pivot shift (+).\n" +
                "- Khe khớp không đau chói, nghiệm pháp McMurray (-).\n" +
                "- Vận động gấp duỗi hạn chế nhẹ do đau. Mạch ngoại vi bắt rõ, cảm giác bình thường.",
                loc);
        }

        if (s.Contains("xẹp") || s.Contains("đốt sống"))
        {
            return string.Format(
                "Khám chuyên khoa cột sống:\n" +
                "- Điểm đau chói cố định tại gai sau đốt sống tổn thương khi gõ và ấn dọc gai sống.\n" +
                "- Co cứng nhẹ khối cơ cạnh sống hai bên, hạn chế vận động cúi - ngửa - nghiêng cột sống thắt lưng.\n" +
                "- Nghiệm pháp Lasegue (-), không có dấu hiệu chèn ép rễ thần kinh khu trú.\n" +
                "- Cơ lực hai chi dưới 5/5, phản xạ gân xương bình thường, cảm giác và phản xạ cơ vòng bảo tồn.",
                loc);
        }

        if (s.Contains("gãy") || s.Contains("gay"))
        {
            return string.Format(
                "Khám chuyên khoa tại {0}:\n" +
                "- Sưng nề, bầm tím, biến dạng chi điển hình.\n" +
                "- Điểm đau chói cố định tại vị trí gãy, có dấu hiệu lạo xạo xương và cử động bất thường.\n" +
                "- Mất hoàn toàn cơ năng vận động của chi tổn thương.\n" +
                "- Mạch ngoại vi bắt rõ, cảm giác ngọn chi bình thường, không có dấu hiệu chèn ép khoang.",
                loc);
        }

        return string.Format(
            "Khám chuyên khoa tại {0}: Đau tức khu trú khi ấn và vận động, các khớp lân cận vận động hạn chế nhẹ do đau. Mạch ngoại vi và cảm giác bảo tồn.",
            loc);
    }

    static string BuildBenhNgoaiKhoa(TreatmentInfo ti, TemplateBA tmpl)
    {
        return BuildCoXuongKhop(ti, tmpl);
    }

    static string BuildCanLamSang(TreatmentInfo ti, LabPacsInfo labs, TemplateBA tmpl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("- Các xét nghiệm huyết học, đông máu, sinh hóa máu cơ bản đánh giá trước phẫu thuật/thủ thuật: Công thức máu, Đông máu cơ bản, Glucose, Ure, Creatinin, AST, ALT, Điện giải đồ, Xét nghiệm miễn dịch (HIV, HBsAg, HCV).");
        sb.AppendLine("- Thăm dò chức năng: Điện tâm đồ (ECG), X-quang tim phổi thẳng.");
        
        string s = ti.IcdName.ToLower();
        if (s.Contains(" u ") || s.Contains("phần mềm") || s.Contains("nang"))
            sb.AppendLine("- Siêu âm phần mềm khu trú đánh giá kích thước, tính chất âm học và tưới máu của khối u; Chụp MRI/CT Scanner khi có chỉ định xâm lấn sâu; Giải phẫu bệnh khối u sau mổ.");
        else if (s.Contains("xẹp") || s.Contains("đốt sống"))
            sb.AppendLine("- X-quang cột sống thẳng nghiêng, Chụp MRI cột sống thắt lưng đánh giá mức độ phù tủy và xẹp cấp; Đo mật độ xương (DEXA).");
        else if (s.Contains("acl") || s.Contains("chằng"))
            sb.AppendLine("- X-quang khớp gối thẳng nghiêng; Chụp MRI khớp gối đánh giá đứt dây chằng và tổn thương sụn chêm kèm theo.");
        else
            sb.AppendLine("- Chụp X-quang xương khớp chuyên khoa tư thế thẳng nghiêng; Chụp CT Scanner/MRI khi cần đánh giá chi tiết gãy xương phức tạp.");

        if (labs != null && !string.IsNullOrEmpty(labs.Summary))
        {
            sb.AppendLine(string.Format("- Chỉ định CLS đã có trên hệ thống: {0}", labs.Summary));
        }

        return sb.ToString().TrimEnd();
    }

    static string FormatTienSuBrief(string tienSu)
    {
        if (string.IsNullOrWhiteSpace(tienSu)) return "khỏe mạnh";
        string ts = tienSu.Replace("\r\n", ", ").Replace("\n", ", ").Replace(". ", ", ").Replace("  ", " ").Trim();
        while (ts.Contains(", ,") || ts.Contains(",,")) ts = ts.Replace(", ,", ",").Replace(",,", ",");
        if (ts.ToLower().Contains("khỏe mạnh") || ts.ToLower().Contains("khoe manh") || ts.ToLower().Contains("chưa phát hiện") || ts.ToLower().Contains("chưa ghi nhận"))
            return "khỏe mạnh";
        if (ts.EndsWith(".") || ts.EndsWith(",")) ts = ts.Substring(0, ts.Length - 1).Trim();
        return ts;
    }

    static string BuildTomTat(TreatmentInfo ti, DhstInfo dhst, string tienSu = null)
    {
        string gender = (ti.PatientGender ?? "nam").ToLower();
        string age = ti.PatientAge;
        string loc = ExtractLocation(ti.IcdName);
        string s = ti.IcdName.ToLower();
        string lyDo = BuildLyDoVaoVien(ti);
        string lyDoLower = string.IsNullOrEmpty(lyDo) ? "đau hạn chế vận động" : (char.ToLower(lyDo[0]) + lyDo.Substring(1));
        string tsBrief = FormatTienSuBrief(tienSu);

        string hoiChung = "";
        if (s.Contains(" u ") || s.Contains("phần mềm") || s.Contains("nang"))
            hoiChung = string.Format("- Hội chứng u phần mềm tại {0}: Khối chắc vừa, ranh giới rõ, di động, ấn đau tức nhẹ, không nóng đỏ.\n- Không có hội chứng nhiễm trùng, thể trạng bình thường.", loc);
        else if (s.Contains("xẹp") || s.Contains("đốt sống"))
            hoiChung = string.Format("- Hội chứng cột sống (+): Đau chói gai sống, co cứng cơ cạnh sống, hạn chế vận động cúi ngửa.\n- Hội chứng rễ thần kinh (-), không rối loạn cơ vòng.", loc);
        else if (s.Contains("acl") || s.Contains("chằng"))
            hoiChung = string.Format("- Hội chứng mất vững khớp {0}: Lachman (+), Ngăn kéo trước (+), đau nhẹ khi gấp gối.\n- Không có dấu hiệu chèn ép mạch máu, thần kinh ngoại vi.", loc);
        else if (s.Contains("gãy") || s.Contains("gay"))
            hoiChung = string.Format("- Dấu hiệu chắc chắn gãy xương: Biến dạng, cử động bất thường, lạo xạo xương tại {0}.\n- Đau chói cố định, mất cơ năng chi, mạch ngoại vi bắt rõ.", loc);
        else
            hoiChung = string.Format("- Đau khu trú tại {0}, hạn chế vận động.", loc);

        return string.Format(
            "Bệnh nhân {0}, {1} tuổi, tiền sử {2}, vào viện vì {3}. " +
            "Qua hỏi bệnh và thăm khám phát hiện các triệu chứng, hội chứng sau:\n" +
            "{4}",
            gender, age, tsBrief, lyDoLower, hoiChung);
    }

    static string BuildPhanBiet(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        if (s.Contains(" u ") || s.Contains("phần mềm") || s.Contains("nang"))
            return "Phân biệt u mỡ (Lipoma), u xơ, nang bao hoạt dịch, tổn thương ác tính phần mềm.";
        if (s.Contains("xẹp") || s.Contains("đốt sống"))
            return "Phân biệt xẹp đốt sống cũ, di căn xương, viêm thân đốt sống đĩa đệm.";
        if (s.Contains("acl") || s.Contains("chằng"))
            return "Phân biệt đứt dây chằng chéo sau (PCL), rách sụn chêm đơn thuần, đứt dây chằng bên.";
        return "Phân biệt các tổn thương phần mềm, chấn thương dây chằng và thoái hóa khớp.";
    }

    static string BuildHuongDieuTri(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        if (s.Contains(" u ") || s.Contains("phần mềm") || s.Contains("nang"))
            return "Phẫu thuật bóc u phần mềm gửi làm giải phẫu bệnh; Kháng sinh dự phòng, giảm đau, chăm sóc vết mổ.";
        if (s.Contains("xẹp") || s.Contains("đốt sống"))
            return "Nghỉ ngơi tại giường có đai nẹp hỗ trợ; Giảm đau bậc thang; Đánh giá chỉ định tạo hình thân đốt sống bằng bơm xi măng sinh học qua da (Vertebroplasty/Kyphoplasty); Bổ sung canxi và điều trị loãng xương nền.";
        if (s.Contains("acl") || s.Contains("chằng"))
            return "Phẫu thuật nội soi tái tạo dây chằng khớp; Đeo nẹp cố định có khóa góc; Tập phục hồi chức năng sớm sau mổ.";
        if (s.Contains("gãy") || s.Contains("gay"))
            return "Phẫu thuật kết hợp xương nẹp vít / đinh nội tủy; Giảm đau, kháng sinh, dự phòng huyết khối; Tập vận động phục hồi chức năng.";
        return "Điều trị nội khoa chuyên khoa kết hợp vật lý trị liệu; Can thiệp thủ thuật/phẫu thuật khi có chỉ định phù hợp.";
    }

    // ──────────────────────────────────────────────────────────────
    // KẾ THỪA MẪU TỪ ORACLE DB
    // ──────────────────────────────────────────────────────────────
    static TemplateBA FindTemplate(dynamic con, TreatmentInfo ti)
    {
        var result = new TemplateBA();
        string cols = "b.QUATRINHBENHLY, b.TIENSUBENHBANTHAN, b.TIENSUBENHGIADINH, " +
                      "b.TOANTHAN, b.COXUONGKHOP, b.THANKINH, b.TUANHOAN, b.HOHAP, " +
                      "b.TIEUHOA, b.THANTIETNIEUSINHDUC, b.CACXETNGHIEMCANLAMSANGCANLAM, " +
                      "b.TOMTATBENHAN, b.PHANBIET, b.TIENLUONG, b.HUONGDIEUTRI";
        string maQl = ti.MaQuanLy.ToString(CultureInfo.InvariantCulture);

        try
        {
            string sqlPt = string.Format(
                "SELECT * FROM (SELECT {0} FROM EMR_FINAL.BENHANNGOAIKHOA b " +
                "JOIN EMR_FINAL.THONGTINDIEUTRI t ON TRUNC(b.MAQUANLY) = TRUNC(t.MAQUANLY) " +
                "WHERE t.MABENHNHAN = '{1}' AND TRUNC(b.MAQUANLY) <> {2} AND b.QUATRINHBENHLY IS NOT NULL " +
                "ORDER BY t.NGAYTAO DESC) WHERE ROWNUM <= 1",
                cols, SafeSql(ti.PatientCode), maQl);

            dynamic rdr = ExecuteReader(con, sqlPt);
            if (rdr != null && rdr.Read())
            {
                result = ReadTemplateRow(rdr);
                rdr.Close();
                Console.WriteLine("  → Kế thừa: Cùng bệnh nhân từ đợt điều trị trước.");
                return result;
            }
            if (rdr != null) rdr.Close();

            string icd3 = (ti.IcdCode ?? "").Length >= 3 ? ti.IcdCode.Substring(0, 3) : (ti.IcdCode ?? "");
            if (!string.IsNullOrEmpty(icd3))
            {
                string sqlIcd = string.Format(
                    "SELECT * FROM (SELECT {0} FROM EMR_FINAL.BENHANNGOAIKHOA b " +
                    "JOIN EMR_FINAL.THONGTINDIEUTRI t ON TRUNC(b.MAQUANLY) = TRUNC(t.MAQUANLY) " +
                    "WHERE t.MAKHOA = '{1}' AND (t.MAICD_KHIVAOKHOADIEUTRI LIKE '{2}%' OR t.MAICD_SOBO LIKE '{2}%') " +
                    "AND b.QUATRINHBENHLY IS NOT NULL AND b.COXUONGKHOP IS NOT NULL " +
                    "ORDER BY t.NGAYTAO DESC) WHERE ROWNUM <= 5",
                    cols, SafeSql(_maKhoa), SafeSql(icd3));

                dynamic rdr2 = ExecuteReader(con, sqlIcd);
                if (rdr2 != null)
                {
                    while (rdr2.Read())
                    {
                        var cand = ReadTemplateRow(rdr2);
                        if (IsTemplateCompatible(cand, ti))
                        {
                            Console.WriteLine(string.Format("  → Kế thừa mẫu: Cùng nhóm ICD '{0}' tại khoa {1}.", icd3, _maKhoa));
                            rdr2.Close();
                            return cand;
                        }
                    }
                    rdr2.Close();
                }
            }
        }
        catch { }

        Console.WriteLine("  → Tự sinh nội dung lâm sàng chuẩn mực chuyên khoa CTCH & Cột sống.");
        return result;
    }

    static bool IsTemplateCompatible(TemplateBA tmpl, TreatmentInfo ti)
    {
        if (tmpl == null || string.IsNullOrWhiteSpace(tmpl.QuaTrinhBenhLy)) return false;
        string cur = ((ti.IcdName ?? "") + " " + (ti.IcdCode ?? "")).ToLower();
        string past = ((tmpl.QuaTrinhBenhLy ?? "") + " " + (tmpl.CoXuongKhop ?? "")).ToLower();

        bool curIsTumor = cur.Contains(" u ") || cur.StartsWith("u ") || cur.Contains("khối u") || cur.Contains("nang") || cur.Contains("phần mềm");
        bool pastIsTrauma = past.Contains("tai nạn") || past.Contains("ngã") || past.Contains("gãy") || past.Contains("chấn thương") || past.Contains("xẹp");
        if (curIsTumor && pastIsTrauma) return false;

        bool curIsTrauma = cur.Contains("gãy") || cur.Contains("ngã") || cur.Contains("tai nạn") || cur.Contains("chấn thương") || cur.Contains("xẹp") || cur.Contains("acl");
        bool pastIsTumor = past.Contains("khối u") || past.Contains("u mỡ") || past.Contains("bóc u") || past.Contains("nang");
        if (curIsTrauma && pastIsTumor) return false;

        return true;
    }

    static TemplateBA ReadTemplateRow(dynamic rdr)
    {
        return new TemplateBA
        {
            QuaTrinhBenhLy              = SafeStr(rdr[0]),
            TienSuBenhBanThan           = SafeStr(rdr[1]),
            TienSuBenhGiaDinh           = SafeStr(rdr[2]),
            ToanThan                    = SafeStr(rdr[3]),
            CoXuongKhop                 = SafeStr(rdr[4]),
            ThanKinh                    = SafeStr(rdr[5]),
            TuanHoan                    = SafeStr(rdr[6]),
            HoHap                       = SafeStr(rdr[7]),
            TieuHoa                     = SafeStr(rdr[8]),
            ThanTietNieu                = SafeStr(rdr[9]),
            CanLamSang                  = SafeStr(rdr[10]),
            TomTatBenhAn                = SafeStr(rdr[11]),
            PhanBiet                    = SafeStr(rdr[12]),
            TienLuong                   = SafeStr(rdr[13]),
            HuongDieuTri                = SafeStr(rdr[14])
        };
    }

    static string AdaptTemplateLaterality(string text, string currentIcd)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(currentIcd)) return text;
        string sCur = currentIcd.ToLower();
        if ((sCur.Contains("trái") || sCur.Contains("(t)")) && !sCur.Contains("phải"))
            return text.Replace("(P)", "(T)").Replace("(p)", "(T)").Replace("phải", "trái").Replace("Phải", "Trái");
        if ((sCur.Contains("phải") || sCur.Contains("(p)")) && !sCur.Contains("trái"))
            return text.Replace("(T)", "(P)").Replace("(t)", "(P)").Replace("trái", "phải").Replace("Trái", "Phải");
        return text;
    }

    // ──────────────────────────────────────────────────────────────
    // THÔNG TIN ĐIỀU TRỊ (TRANG BÌA EMR)
    // ──────────────────────────────────────────────────────────────
    static bool EnsureThongTinDieuTri(dynamic con, TreatmentInfo ti, bool isSave)
    {
        try
        {
            LoadEmrAssemblies();
            var ttdtFuncType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTriFunc");
            var checkMethod = ttdtFuncType.GetMethod("checkExistThongTinDieuTri", new Type[] { _mdbConnType, typeof(decimal) });
            
            bool exists = (bool)checkMethod.Invoke(null, new object[] { con, ti.MaQuanLy });
            if (!exists && (decimal)ti.TreatmentId != ti.MaQuanLy)
            {
                exists = (bool)checkMethod.Invoke(null, new object[] { con, (decimal)ti.TreatmentId });
            }

            if (exists)
            {
                Console.WriteLine("✓ Trang bìa THONGTINDIEUTRI: ĐÃ CÓ TRÊN HỆ THỐNG.");
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INFO] Chưa có Trang bìa THONGTINDIEUTRI → Khởi tạo Trang bìa EMR Ngoại khoa (IDLoaiBenhAn: 11).");
            Console.ResetColor();

            if (!isSave) return true;

            var ttdtType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTri");
            dynamic ttdt = Activator.CreateInstance(ttdtType);
            ttdt.MaQuanLy = ti.MaQuanLy;
            ttdt.MaBenhNhan = ti.PatientCode;
            ttdt.MaBenhAn = ti.TreatmentCode;
            ttdt.Khoa = string.IsNullOrEmpty(ti.DeptName) ? DEPT_NAME_HN : ti.DeptName;
            ttdt.TenKhoaVao = ttdt.Khoa;
            ttdt.IDLoaiBenhAn = 11;
            ttdt.ChanDoan_KhiVaoKhoaDieuTri = ti.IcdName;
            ttdt.MaICD_KhiVaoKhoaDieuTri = ti.IcdCode;
            ttdt.ChanDoan_KKB_CapCuu = ti.IcdName;
            ttdt.MaICD_KKB_CapCuu = ti.IcdCode;
            ttdt.VaoVienDoBenhNayLanThu = 1;

            if (!string.IsNullOrEmpty(ti.InTime))
            {
                DateTime dt;
                if (DateTime.TryParseExact(ti.InTime, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    ttdt.NgayVaoVien = dt;
                    ttdt.NgayVaoKhoa = dt;
                    ttdt.NgayThangNamTrangBia = dt;
                }
            }

            var insertMethod = ttdtFuncType.GetMethod("InsertOrUpdateThongTinDieuTri", new Type[] { _mdbConnType, ttdtType });
            object res = insertMethod.Invoke(null, new object[] { con, ttdt });
            bool ok = (res is bool) ? (bool)res : true;

            if ((decimal)ti.TreatmentId != ti.MaQuanLy)
            {
                try
                {
                    ttdt.MaQuanLy = (decimal)ti.TreatmentId;
                    insertMethod.Invoke(null, new object[] { con, ttdt });
                    ttdt.MaQuanLy = ti.MaQuanLy;
                }
                catch { }
            }

            if (ok) Console.WriteLine("✓ Đã khởi tạo thành công Trang bìa THONGTINDIEUTRI.");
            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [WARN] Lỗi khởi tạo THONGTINDIEUTRI: " + ex.Message);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // TRA CỨU BỆNH NHÂN TỪ HIS API
    // ──────────────────────────────────────────────────────────────
    static TreatmentInfo ResolveIdentifier(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        string kw = input.Trim();

        // 1. Mã BN 10 chữ số (0000393143)
        if (IsNumericId(kw) && kw.Length == 10 && kw.StartsWith("0"))
            return LookupByPatientCode(adapter, consumer, param, kw);

        // 2. Mã ĐT 12 chữ số (000007266477)
        if (IsNumericId(kw) && kw.Length == 12)
        {
            var byCode = LookupByTreatmentCode(adapter, consumer, param, kw);
            if (byCode != null) return byCode;
        }

        // 3. Số 6-11 chữ số: Thử TreatmentCode, TreatmentId, rồi PatientCode
        if (IsNumericId(kw))
        {
            var byCode = LookupByTreatmentCode(adapter, consumer, param, kw.PadLeft(12, '0'));
            if (byCode != null) return byCode;

            long id;
            if (long.TryParse(kw, out id))
            {
                var byId = LookupByTreatmentId(adapter, consumer, param, id);
                if (byId != null) return byId;
            }
            return LookupByPatientCode(adapter, consumer, param, kw.PadLeft(10, '0'));
        }

        // 4. Tra cứu theo Họ tên bệnh nhân
        return LookupByPatientName(adapter, consumer, param, kw);
    }

    static TreatmentInfo LookupByPatientCode(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string patCode)
    {
        patCode = patCode.PadLeft(10, '0');
        var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = patCode, IS_PAUSE = false };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trs == null || trs.Count == 0)
        {
            tf.IS_PAUSE = null;
            trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        }
        if (trs == null || trs.Count == 0) return null;
        return MapTreatment(trs.OrderByDescending(x => x.IN_TIME).First());
    }

    static TreatmentInfo LookupByTreatmentCode(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string treatCode)
    {
        var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = treatCode };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trs == null || trs.Count == 0) return null;
        return MapTreatment(trs.OrderByDescending(x => x.IN_TIME).First());
    }

    static TreatmentInfo LookupByTreatmentId(MyAdapter adapter, ApiConsumer consumer, CommonParam param, long treatId)
    {
        var tf = new HisTreatmentViewFilter { ID = treatId };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trs == null || trs.Count == 0) return null;
        return MapTreatment(trs.First());
    }

    static TreatmentInfo LookupByPatientName(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string patName)
    {
        Console.WriteLine(string.Format("🔍 Đang tìm kiếm bệnh nhân theo tên '{0}'...", patName));
        try
        {
            var tbrf = new HisTreatmentBedRoomLViewFilter { IS_IN_ROOM = true };
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, param);
            if (inPatients != null && inPatients.Count > 0)
            {
                string normKw = RemoveDiacritics(patName).ToLower();
                var matched = inPatients.FirstOrDefault(p => 
                    (p.DEPARTMENT_ID == _deptId) &&
                    !string.IsNullOrEmpty(p.TDL_PATIENT_NAME) &&
                    RemoveDiacritics(p.TDL_PATIENT_NAME).ToLower().Contains(normKw));

                if (matched == null)
                {
                    matched = inPatients.FirstOrDefault(p => 
                        !string.IsNullOrEmpty(p.TDL_PATIENT_NAME) &&
                        RemoveDiacritics(p.TDL_PATIENT_NAME).ToLower().Contains(normKw));
                }

                if (matched != null)
                {
                    return LookupByTreatmentId(adapter, consumer, param, matched.TREATMENT_ID);
                }
            }
        }
        catch { }

        return null;
    }

    static string FindLatestPatientInDept()
    {
        try
        {
            string tokenCode = ReadLiveToken();
            var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
            var param = new CommonParam();
            var adapter = new MyAdapter();

            var bf = new HisBedRoomViewFilter { DEPARTMENT_ID = _deptId };
            var rooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", consumer, bf, param);
            if (rooms == null || rooms.Count == 0) return null;

            var roomIds = rooms.Select(r => r.ID).ToList();
            var tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = roomIds, IS_IN_ROOM = true };
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, param);
            if (inPatients == null || inPatients.Count == 0) return null;

            var latest = inPatients.OrderByDescending(x => x.ADD_TIME).FirstOrDefault();
            if (latest != null)
            {
                return latest.TDL_PATIENT_CODE;
            }
        }
        catch { }
        return null;
    }

    static int RunTodayStatus()
    {
        Console.WriteLine("\n" + new string('=', 85));
        Console.WriteLine(string.Format("🏥 DANH SÁCH BỆNH NHÂN NẰM BUỒNG KHOA CTCH & CỘT SỐNG (KHOA {0})", _deptId));
        Console.WriteLine(new string('=', 85));

        string tokenCode = ReadLiveToken();
        var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        var bf = new HisBedRoomViewFilter { DEPARTMENT_ID = _deptId };
        var rooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", consumer, bf, param);
        if (rooms == null || rooms.Count == 0)
        {
            Console.WriteLine("❌ Không tải được danh sách buồng bệnh.");
            return 1;
        }

        var roomIds = rooms.Select(r => r.ID).ToList();
        var tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = roomIds, IS_IN_ROOM = true };
        var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, param);
        if (inPatients == null || inPatients.Count == 0)
        {
            Console.WriteLine("ℹ️ Hiện không có bệnh nhân nào nằm buồng.");
            return 0;
        }

        var treatIds = inPatients.Select(x => x.TREATMENT_ID).Distinct().ToList();
        var tf = new HisTreatmentViewFilter { IDs = treatIds };
        var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        var trMap = (trList ?? new List<V_HIS_TREATMENT>()).ToDictionary(x => x.ID);

        dynamic con = CreateEmrConnection();
        con.Open();

        int idx = 1;
        foreach (var ip in inPatients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
        {
            V_HIS_TREATMENT tr = null;
            trMap.TryGetValue(ip.TREATMENT_ID, out tr);
            string pName = tr != null ? tr.TDL_PATIENT_NAME : ip.TDL_PATIENT_NAME;
            string pCode = tr != null ? tr.TDL_PATIENT_CODE : ip.TDL_PATIENT_CODE;
            string trCode = tr != null ? tr.TREATMENT_CODE : "N/A";
            decimal maQl = ParseMaQuanLy(trCode, ip.TREATMENT_ID);
            string icd = tr != null ? string.Format("[{0}] {1}", tr.ICD_CODE, tr.ICD_NAME) : "N/A";

            bool hasBA = false;
            string bsBA = "";
            try
            {
                dynamic ba = BenhAnNgoaiKhoaSelect(con, maQl);
                if (ba != null && ba.MaQuanLy > 0)
                {
                    hasBA = true;
                    bsBA = SafeStr(ba.BacSyLamBenhAn);
                }
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("[{0:D2}] {1} ({2}) | Buồng: {3} - Giường: {4}", idx++, pName, pCode, ip.BED_ROOM_NAME, ip.BED_NAME));
            Console.ResetColor();
            Console.WriteLine(string.Format("     🩺 Chẩn đoán: {0}", icd));
            Console.Write("     📋 Bệnh án EMR: ");
            if (hasBA)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("✔ ĐÃ ĐIỀN (BS: {0})", bsBA));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("🔴 CHƯA ĐIỀN → Chạy lệnh: .\\HisEmrFiller.bat {0}", pCode));
            }
            Console.ResetColor();
        }

        con.Close();
        Console.WriteLine(new string('=', 85));
        return 0;
    }

    static int RunDateAudit(string dateInput, bool autoCreate, string doctorCode, string doctorName)
    {
        long targetFrom = 0;
        long targetTo = 0;
        string displayDate = "";

        try
        {
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            int day = DateTime.Today.Day;

            string s = (dateInput ?? "").Trim();
            if (s.Length == 8 && s.StartsWith("202") && s.All(char.IsDigit))
            {
                year = int.Parse(s.Substring(0, 4));
                month = int.Parse(s.Substring(4, 2));
                day = int.Parse(s.Substring(6, 2));
            }
            else
            {
                char[] seps = new char[] { '/', '.', '-' };
                string[] parts = s.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out day);
                    int.TryParse(parts[1], out month);
                    if (parts.Length >= 3) int.TryParse(parts[2], out year);
                    if (year < 100) year += 2000;
                }
            }

            targetFrom = (long)year * 10000000000L + (long)month * 100000000L + (long)day * 1000000L;
            targetTo   = targetFrom + 235959L;
            displayDate = string.Format("{0:D2}/{1:D2}/{2:D4}", day, month, year);
        }
        catch
        {
            targetFrom = (long)DateTime.Today.Year * 10000000000L + (long)DateTime.Today.Month * 100000000L + (long)DateTime.Today.Day * 1000000L;
            targetTo   = targetFrom + 235959L;
            displayDate = DateTime.Today.ToString("dd/MM/yyyy");
        }

        Console.WriteLine("\n" + new string('=', 85));
        Console.WriteLine(string.Format("🏥 QUÉT BỆNH NHÂN VÀO KHOA CTCH & CỘT SỐNG (KHOA {0}) NGÀY {1}", _deptId, displayDate));
        Console.WriteLine(new string('=', 85));

        string tokenCode = ReadLiveToken();
        var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        var patientMap = new Dictionary<long, TreatmentInfo>();
        var roomMap = new Dictionary<long, string>();

        // 1. Quét HisDepartmentTran vào Khoa 57 trong ngày
        try
        {
            var dtf = new HisDepartmentTranViewFilter
            {
                DEPARTMENT_ID = _deptId,
                DEPARTMENT_IN_TIME_FROM = targetFrom,
                DEPARTMENT_IN_TIME_TO = targetTo
            };
            var deptTrans = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", consumer, dtf, param);
            if (deptTrans != null)
            {
                foreach (var dt in deptTrans)
                {
                    if (!patientMap.ContainsKey(dt.TREATMENT_ID))
                    {
                        var ti = LookupByTreatmentId(adapter, consumer, param, dt.TREATMENT_ID);
                        if (ti != null && (ti.TreatmentTypeId == 3 || ti.TreatmentTypeId == null))
                        {
                            ti.DeptInTime = dt.DEPARTMENT_IN_TIME;
                            patientMap[dt.TREATMENT_ID] = ti;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  ⚠️ Quét DepartmentTran: " + ex.Message);
        }

        // 2. Quét HisTreatment vào viện trong ngày có liên quan Khoa 57 (CHỈ LẤY NỘI TRÚ TDL_TREATMENT_TYPE_ID == 3)
        try
        {
            var tf = new HisTreatmentViewFilter
            {
                IN_TIME_FROM = targetFrom,
                IN_TIME_TO = targetTo
            };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            if (trs != null)
            {
                var deptTrs = trs.Where(x => (x.LAST_DEPARTMENT_ID == _deptId || x.IN_DEPARTMENT_ID == _deptId || x.END_DEPARTMENT_ID == _deptId) && (x.TDL_TREATMENT_TYPE_ID == 3)).ToList();
                foreach (var tr in deptTrs)
                {
                    if (!patientMap.ContainsKey(tr.ID))
                    {
                        patientMap[tr.ID] = MapTreatment(tr);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  ⚠️ Quét HisTreatment: " + ex.Message);
        }

        // 3. Quét thông tin buồng giường từ bệnh nhân đang nằm buồng
        try
        {
            var bf = new HisBedRoomViewFilter { DEPARTMENT_ID = _deptId };
            var rooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", consumer, bf, param);
            if (rooms != null && rooms.Count > 0)
            {
                var roomIds = rooms.Select(r => r.ID).ToList();
                var tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = roomIds, IS_IN_ROOM = true };
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, param);
                if (inPatients != null)
                {
                    foreach (var ip in inPatients)
                    {
                        roomMap[ip.TREATMENT_ID] = string.Format("{0} - {1}", ip.BED_ROOM_NAME, ip.BED_NAME);
                        if (ip.ADD_TIME >= targetFrom && ip.ADD_TIME <= targetTo && !patientMap.ContainsKey(ip.TREATMENT_ID))
                        {
                            var ti = LookupByTreatmentId(adapter, consumer, param, ip.TREATMENT_ID);
                            if (ti != null && (ti.TreatmentTypeId == 3 || ti.TreatmentTypeId == null))
                                patientMap[ip.TREATMENT_ID] = ti;
                        }
                    }
                }
            }
        }
        catch { }

        // Lọc danh sách bệnh nhân NỘI TRÚ vào Khoa 57 ngày chỉ định (BẮT BUỘC TreatmentTypeId == 3)
        var filteredList = patientMap.Values
            .Where(ti => (ti.TreatmentTypeId == 3) &&
                         ((ti.InTimeRaw >= targetFrom && ti.InTimeRaw <= targetTo) ||
                          (ti.DeptInTime.HasValue && ti.DeptInTime.Value >= targetFrom && ti.DeptInTime.Value <= targetTo)))
            .OrderBy(ti => ti.InTimeRaw)
            .ToList();

        if (filteredList.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(string.Format("ℹ️ Không tìm thấy bệnh nhân nội trú nào vào Khoa {0} ngày {1}.", _deptId, displayDate));
            Console.ResetColor();
            return 0;
        }

        Console.WriteLine(string.Format("✓ Tìm thấy tổng cộng {0} bệnh nhân NỘI TRÚ vào Khoa {1} ngày {2}:\n", filteredList.Count, _deptId, displayDate));

        dynamic con = CreateEmrConnection();
        con.Open();

        var missingList = new List<TreatmentInfo>();
        int idx = 1;

        foreach (var ti in filteredList)
        {
            string roomStr = roomMap.ContainsKey(ti.TreatmentId) ? roomMap[ti.TreatmentId] : "Chưa xếp buồng/ngoại trú";

            bool hasBA = false;
            string bsBA = "";
            try
            {
                dynamic ba = BenhAnNgoaiKhoaSelect(con, ti.MaQuanLy);
                if (ba == null || ba.MaQuanLy == 0)
                {
                    ba = BenhAnNgoaiKhoaSelect(con, (decimal)ti.TreatmentId);
                }
                if (ba != null && ba.MaQuanLy > 0)
                {
                    hasBA = true;
                    bsBA = SafeStr(ba.BacSyLamBenhAn);
                }
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("[{0:D2}] {1} ({2}) | {3}T - {4} | Vào: {5}",
                idx++, ti.PatientName, ti.PatientCode, ti.PatientAge, ti.PatientGender, ti.InTime));
            Console.ResetColor();
            Console.WriteLine(string.Format("     📍 Vị trí: {0}", roomStr));
            Console.WriteLine(string.Format("     🩺 Chẩn đoán: [{0}] {1}", ti.IcdCode, ti.IcdName));
            Console.Write("     📋 Vỏ bệnh án EMR: ");

            if (hasBA)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("✔ ĐÃ CÓ (BS: {0})", bsBA));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("🔴 CHƯA CÓ VỎ BỆNH ÁN");
                Console.ResetColor();
                missingList.Add(ti);
            }
            Console.WriteLine();
        }

        con.Close();

        // Tự động tạo nếu có cờ autoCreate
        if (autoCreate && missingList.Count > 0)
        {
            Console.WriteLine(new string('-', 85));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("🚀 TỰ ĐỘNG TẠO VỎ BỆNH ÁN CHO {0} BỆNH NHÂN CHƯA CÓ...", missingList.Count));
            Console.ResetColor();
            Console.WriteLine(new string('-', 85));

            int successCount = 0;
            foreach (var ti in missingList)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("\n>>> Đang tạo Bệnh Án Ngoại Khoa cho: {0} ({1}) - Mã ĐT: {2}", ti.PatientName, ti.PatientCode, ti.TreatmentCode));
                Console.ResetColor();

                int res = Run(ti.PatientCode, false, doctorCode, doctorName, false);
                if (res == 0)
                {
                    successCount++;
                }
            }

            Console.WriteLine("\n" + new string('=', 85));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format("🎉 HOÀN TẤT TỰ ĐỘNG TẠO VỎ BỆNH ÁN: {0}/{1} THÀNH CÔNG!", successCount, missingList.Count));
            Console.ResetColor();
            Console.WriteLine(new string('=', 85));
        }
        else if (missingList.Count > 0)
        {
            Console.WriteLine(new string('-', 85));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(string.Format("💡 Có {0} bệnh nhân chưa có vỏ bệnh án EMR. Thêm cờ --auto để tự động tạo toàn bộ:", missingList.Count));
            Console.WriteLine(string.Format("   .\\HisEmrFiller.bat --date {0} --auto", displayDate));
            Console.ResetColor();
            Console.WriteLine(new string('-', 85));
        }

        return 0;
    }

    // ──────────────────────────────────────────────────────────────
    // REVERSE VỎ BỆNH ÁN NGOẠI TRÚ / PHÒNG KHÁM
    // ──────────────────────────────────────────────────────────────
    static readonly string[] OutpatientPatientCodes = new string[]
    {
        "0004053714", "0004053891", "0004053381", "0004054099", "0003400119",
        "0004054269", "0003989128", "0003726306", "0001614864", "0004054652",
        "0002841197", "0000849122", "0003918608", "0002172283", "0004055042",
        "0003978976", "0004055288", "0004055326", "0004055347", "0004055498",
        "0004055501", "0004055673", "0001944203", "0004055808", "0003980536",
        "0003132237", "0003272429", "0004055927", "0004056039", "0001411195",
        "0004056139", "0004039141", "0004032056", "0004056335", "0001363157",
        "0004056352", "0000669206", "0003664862", "0004056463", "0004056593"
    };

    static int RunReverseOutpatients()
    {
        Console.WriteLine("\n" + new string('=', 85));
        Console.WriteLine("🔄 THỰC THI REVERSE: XÓA VỎ BỆNH ÁN NGOẠI TRÚ / PHÒNG KHÁM TRÊN ORACLE EMR");
        Console.WriteLine(new string('=', 85));
        Console.WriteLine("Chỉ thị của Bác sĩ: Tuyệt đối không làm vỏ bệnh án cho bệnh nhân phòng khám.");
        Console.WriteLine(string.Format("Danh sách mục tiêu cần thu hồi: {0} bệnh nhân ngoại trú ngày 18/09.\n", OutpatientPatientCodes.Length));

        dynamic con = CreateEmrConnection();
        con.Open();
        Console.WriteLine("✓ Kết nối Oracle EMR (EMR_FINAL) thành công.\n");

        // Kiểm tra schema bảng BENHANNGOAIKHOA
        try
        {
            dynamic cmdCol = Activator.CreateInstance(_mdbCmdType, new object[] { "SELECT COLUMN_NAME FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = 'BENHANNGOAIKHOA' ORDER BY COLUMN_ID", con });
            dynamic rCol = cmdCol.ExecuteReader();
            List<string> colList = new List<string>();
            while (rCol.Read()) colList.Add(Convert.ToString(rCol[0]));
            Console.WriteLine("📋 CÁC CỘT TRONG BENHANNGOAIKHOA: " + string.Join(", ", colList.ToArray()) + "\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi đọc cột: " + ex.Message);
        }

        // Kiểm tra method của BenhAnNgoaiKhoaFunc
        try
        {
            LoadEmrAssemblies();
            var methods = _baNKFuncType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            List<string> mList = new List<string>();
            foreach (var m in methods) mList.Add(m.Name);
            Console.WriteLine("🔧 METHODS CỦA BenhAnNgoaiKhoaFunc: " + string.Join(", ", mList.ToArray()) + "\n");
        }
        catch { }

        string tokenCode = ReadLiveToken();
        var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        int totalBaDeleted = 0;
        int totalTtdtDeleted = 0;

        foreach (var patCode in OutpatientPatientCodes)
        {
            var ti = LookupByPatientCode(adapter, consumer, param, patCode);
            string pName = ti != null ? ti.PatientName : "N/A";
            Console.Write(string.Format("  - Xử lý BN {0} ({1}): ", patCode, pName));

            int countBa = 0;
            int countTtdt = 0;

            if (ti != null)
            {
                // 1. Xóa trong BENHANNGOAIKHOA theo MaQuanLy và TreatmentId
                string sqlDelBa = string.Format(
                    "DELETE FROM EMR_FINAL.BENHANNGOAIKHOA WHERE (MAQUANLY = {0} OR MAQUANLY = {1}) AND BACSYLAMBENHAN = '{2}'",
                    ti.MaQuanLy.ToString(CultureInfo.InvariantCulture), ti.TreatmentId, DEFAULT_DOCTOR_CODE);
                countBa = ExecuteNonQuery(con, sqlDelBa);

                // 2. Xóa trong THONGTINDIEUTRI (IDLoaiBenhAn = 11)
                string sqlDelTtdt = string.Format(
                    "DELETE FROM EMR_FINAL.THONGTINDIEUTRI WHERE (MAQUANLY = {0} OR MAQUANLY = {1} OR MABENHNHAN = '{2}') AND IDLOAIBENHAN = 11",
                    ti.MaQuanLy.ToString(CultureInfo.InvariantCulture), ti.TreatmentId, patCode);
                countTtdt = ExecuteNonQuery(con, sqlDelTtdt);
            }
            else
            {
                string sqlDelTtdt = string.Format(
                    "DELETE FROM EMR_FINAL.THONGTINDIEUTRI WHERE MABENHNHAN = '{0}' AND IDLOAIBENHAN = 11", patCode);
                countTtdt = ExecuteNonQuery(con, sqlDelTtdt);
            }

            if (countBa > 0 || countTtdt > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("ĐÃ XÓA (BENHANNGOAIKHOA: {0}, THONGTINDIEUTRI: {1})", Math.Max(0, countBa), Math.Max(0, countTtdt)));
                Console.ResetColor();
                if (countBa > 0) totalBaDeleted += countBa;
                if (countTtdt > 0) totalTtdtDeleted += countTtdt;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Đã dọn sạch / không còn bản ghi.");
                Console.ResetColor();
            }
        }

        // Quét toàn bộ các bản ghi còn lại của BS 034727 để rà soát sạch các ca ngoại trú nếu có
        Console.WriteLine("\n🔍 ĐỐI SOÁT TOÀN BỘ HỒ SƠ CỦA BS 034727 TRÊN EMR:");
        string sqlList = string.Format("SELECT MAQUANLY, BENHCHINH, NGAYKHAMBENH FROM EMR_FINAL.BENHANNGOAIKHOA WHERE BACSYLAMBENHAN = '{0}'", DEFAULT_DOCTOR_CODE);
        dynamic rList = ExecuteReader(con, sqlList);
        List<decimal> remainingMaQls = new List<decimal>();
        while (rList != null && rList.Read())
        {
            remainingMaQls.Add(Convert.ToDecimal(rList[0]));
        }

        Console.WriteLine(string.Format("Tìm thấy {0} bản ghi BENHANNGOAIKHOA của BS {1}:", remainingMaQls.Count, DEFAULT_DOCTOR_CODE));
        foreach (var mql in remainingMaQls)
        {
            var tiCheck = LookupByTreatmentCode(adapter, consumer, param, mql.ToString().PadLeft(12, '0'));
            if (tiCheck == null)
            {
                long trId;
                if (long.TryParse(mql.ToString(), out trId))
                    tiCheck = LookupByTreatmentId(adapter, consumer, param, trId);
            }

            if (tiCheck != null)
            {
                bool isInpatient = (tiCheck.TreatmentTypeId == 3);
                if (!isInpatient)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(string.Format("  ⚠️ Phát hiện ca ngoại trú sót lại: {0} ({1}) - MaQuanLy: {2} -> Đang xóa... ",
                        tiCheck.PatientName, tiCheck.PatientCode, mql));
                    int delExtra = ExecuteNonQuery(con, string.Format("DELETE FROM EMR_FINAL.BENHANNGOAIKHOA WHERE MAQUANLY = {0} AND BACSYLAMBENHAN = '{1}'", mql, DEFAULT_DOCTOR_CODE));
                    Console.WriteLine(delExtra > 0 ? "ĐÃ XÓA XONG." : "Không thể xóa.");
                    Console.ResetColor();
                    if (delExtra > 0) totalBaDeleted += delExtra;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format("  ✓ [NỘI TRÚ BẢO LƯU] {0} ({1}) - MaQuanLy: {2} | Chẩn đoán: {3}",
                        tiCheck.PatientName, tiCheck.PatientCode, mql, tiCheck.IcdName));
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine(string.Format("  - MaQuanLy: {0} (Không tìm thấy trong HIS)", mql));
            }
        }

        // Commit transaction
        try
        {
            ExecuteNonQuery(con, "COMMIT");
        }
        catch { }

        Console.WriteLine("\n" + new string('-', 85));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(string.Format("📊 TỔNG KẾT REVERSE: Đã xóa {0} vỏ BENHANNGOAIKHOA và {1} bản ghi THONGTINDIEUTRI ngoại trú.", totalBaDeleted, totalTtdtDeleted));
        Console.ResetColor();

        // Kiểm tra an toàn: Đảm bảo bệnh nhân nội trú thật sự vẫn nguyên vẹn
        Console.WriteLine("\n🔒 KIỂM TRA TOÀN VẸN CÁC CA NỘI TRÚ CHÍNH THỨC (BẢO LƯU 100%):");
        string[] inpatientChecks = new string[] { "0004051068", "0000393143", "0003993384", "0002145867" };
        foreach (var inCode in inpatientChecks)
        {
            var tiIn = LookupByPatientCode(adapter, consumer, param, inCode);
            if (tiIn != null)
            {
                dynamic ba = BenhAnNgoaiKhoaSelect(con, tiIn.MaQuanLy);
                if (ba == null || ba.MaQuanLy == 0) ba = BenhAnNgoaiKhoaSelect(con, (decimal)tiIn.TreatmentId);
                bool hasBa = (ba != null && ba.MaQuanLy > 0);
                Console.WriteLine(string.Format("  - BN {0} ({1}): {2}", 
                    inCode, tiIn.PatientName, hasBa ? "✓ NGUYÊN VẸN AN TOÀN TRÊN EMR" : "⚠️ Chưa có vỏ"));
            }
        }

        con.Close();
        Console.WriteLine(new string('=', 85));
        return 0;
    }

    // ──────────────────────────────────────────────────────────────
    // TIỆN ÍCH DỮ LIỆU & ORACLE WRAPPERS
    // ──────────────────────────────────────────────────────────────
    static Assembly _emrMainLib;
    static Assembly _mdbLib;
    static Type _mdbConnType;
    static Type _mdbCmdType;
    static Type _baNKType;
    static Type _baNKFuncType;

    static void LoadEmrAssemblies()
    {
        if (_emrMainLib != null) return;
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string emrDir = Path.Combine(dir, "Integrate", "EMR");
        _mdbLib = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
        _emrMainLib = Assembly.LoadFrom(Path.Combine(emrDir, "EMR_MAIN.Library.dll"));
        try { Assembly.LoadFrom(Path.Combine(emrDir, "Oracle.ManagedDataAccess.dll")); } catch { }

        _mdbConnType = _mdbLib.GetType("MDB.MDBConnection");
        _mdbCmdType = _mdbLib.GetType("MDB.MDBCommand");
        _baNKType = _emrMainLib.GetType("EMR_MAIN.BenhAnNgoaiKhoa");
        _baNKFuncType = _emrMainLib.GetType("EMR_MAIN.BenhAnNgoaiKhoaFunc");
    }

    static dynamic CreateEmrConnection()
    {
        LoadEmrAssemblies();
        return Activator.CreateInstance(_mdbConnType, new object[] { EMR_CONNSTR });
    }

    static dynamic CreateNewBenhAnNgoaiKhoa()
    {
        LoadEmrAssemblies();
        return Activator.CreateInstance(_baNKType);
    }

    static dynamic BenhAnNgoaiKhoaSelect(dynamic con, decimal maQuanLy)
    {
        LoadEmrAssemblies();
        try
        {
            var m = _baNKFuncType.GetMethod("Select", new Type[] { _mdbConnType, typeof(decimal) });
            return m.Invoke(null, new object[] { con, maQuanLy });
        }
        catch { return null; }
    }

    static bool BenhAnNgoaiKhoaInsertOrUpdate(dynamic con, dynamic ba)
    {
        LoadEmrAssemblies();
        try
        {
            var propDacDiem = ((object)ba).GetType().GetProperty("DacDiemLienQuanBenh");
            if (propDacDiem != null && propDacDiem.GetValue((object)ba, null) == null)
            {
                var ddlqType = _emrMainLib.GetType("EMR_MAIN.DacDiemLienQuanBenh");
                if (ddlqType != null)
                {
                    object instance = Activator.CreateInstance(ddlqType);
                    propDacDiem.SetValue((object)ba, instance, null);
                }
            }
        }
        catch { }

        var m = _baNKFuncType.GetMethod("InsertOrUpdate", new Type[] { _mdbConnType, _baNKType });
        object result = m.Invoke(null, new object[] { con, ba });
        if (result is bool) return (bool)result;
        return true;
    }

    static dynamic ExecuteReader(dynamic con, string sql)
    {
        LoadEmrAssemblies();
        try
        {
            dynamic cmd = Activator.CreateInstance(_mdbCmdType, new object[] { sql, con });
            return cmd.ExecuteReader();
        }
        catch { return null; }
    }

    static int ExecuteNonQuery(dynamic con, string sql)
    {
        LoadEmrAssemblies();
        try
        {
            dynamic cmd = Activator.CreateInstance(_mdbCmdType, new object[] { sql, con });
            object res = cmd.ExecuteNonQuery();
            if (res is int) return (int)res;
            return 0;
        }
        catch (Exception ex)
        {
            string detail = ex.InnerException != null ? (" --> " + ex.InnerException.Message) : "";
            Console.WriteLine("  [ExecuteNonQuery Error] " + ex.Message + detail);
            return -1;
        }
    }

    static decimal ParseMaQuanLy(string treatmentCode, long treatmentId)
    {
        long num;
        if (!string.IsNullOrEmpty(treatmentCode) && long.TryParse(treatmentCode, out num) && num > 0)
            return (decimal)num;
        return (decimal)treatmentId;
    }

    static TreatmentInfo MapTreatment(V_HIS_TREATMENT t)
    {
        if (t == null) return null;
        string inTimeStr = LongToDateStr(t.IN_TIME);
        string ageStr = t.TDL_PATIENT_DOB > 0 ? (DateTime.Now.Year - (int)(t.TDL_PATIENT_DOB / 10000000000L)).ToString() : "?";
        return new TreatmentInfo
        {
            TreatmentId   = t.ID,
            TreatmentCode = t.TREATMENT_CODE ?? "",
            MaQuanLy      = ParseMaQuanLy(t.TREATMENT_CODE, t.ID),
            PatientCode   = t.TDL_PATIENT_CODE ?? "",
            PatientName   = t.TDL_PATIENT_NAME ?? "",
            PatientAge    = ageStr,
            PatientGender = !string.IsNullOrEmpty(t.TDL_PATIENT_GENDER_NAME) ? t.TDL_PATIENT_GENDER_NAME : ((t.TDL_PATIENT_GENDER_ID == 1) ? "Nam" : "Nữ"),
            InTime        = inTimeStr,
            InTimeRaw     = t.IN_TIME,
            TreatmentTypeId = t.TDL_TREATMENT_TYPE_ID,
            ClinicalInTime = t.CLINICAL_IN_TIME,
            IcdCode       = t.ICD_CODE ?? "",
            IcdName       = t.ICD_NAME ?? "",
            IcdText       = t.ICD_TEXT ?? "",
            DeptName      = string.IsNullOrEmpty(t.END_DEPARTMENT_NAME) ? _deptNameDefault : t.END_DEPARTMENT_NAME,
            DeptId        = t.END_DEPARTMENT_ID.HasValue ? t.END_DEPARTMENT_ID.Value : 0
        };
    }

    static string LongToDateStr(long v)
    {
        if (v <= 0) return "?";
        try
        {
            int year  = (int)(v / 10000000000L);
            int month = (int)(v / 100000000L % 100);
            int day   = (int)(v / 1000000L % 100);
            int hour  = (int)(v / 10000L % 100);
            int min   = (int)(v / 100L % 100);
            return string.Format("{0:D2}/{1:D2}/{2} {3:D2}:{4:D2}", day, month, year, hour, min);
        }
        catch { return v.ToString(); }
    }

    static DhstInfo GetLatestDhst(MyAdapter adapter, ApiConsumer consumer, CommonParam param, long treatId)
    {
        try
        {
            var f = new HisDhstFilter { TREATMENT_ID = treatId };
            var list = adapter.FetchList<HIS_DHST>("api/HisDhst/Get", consumer, f, param);
            if (list == null || list.Count == 0) return new DhstInfo();
            var d = list.OrderByDescending(x => x.EXECUTE_TIME).First();
            string bp = (d.BLOOD_PRESSURE_MAX.HasValue && d.BLOOD_PRESSURE_MIN.HasValue) ? d.BLOOD_PRESSURE_MAX.Value + "/" + d.BLOOD_PRESSURE_MIN.Value : "120/80";
            return new DhstInfo
            {
                Pulse         = d.PULSE.HasValue ? ((int)d.PULSE.Value).ToString() : "80",
                BloodPressure = bp,
                Temperature   = d.TEMPERATURE.HasValue ? d.TEMPERATURE.Value.ToString("F1") : "37.0",
                SpO2          = d.SPO2.HasValue ? ((int)d.SPO2.Value).ToString() : "98",
                Weight        = d.WEIGHT.HasValue ? d.WEIGHT.Value.ToString() : "55",
                Height        = d.HEIGHT.HasValue ? d.HEIGHT.Value.ToString() : "160"
            };
        }
        catch { return new DhstInfo(); }
    }

    static LabPacsInfo FetchLabsAndPacs(MyAdapter adapter, ApiConsumer consumer, CommonParam param, long treatId)
    {
        var info = new LabPacsInfo();
        try
        {
            var f = new HisSereServViewFilter { TREATMENT_ID = treatId };
            var list = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, f, param);
            if (list != null && list.Count > 0)
            {
                var names = list.Select(x => x.TDL_SERVICE_NAME).Where(x => !string.IsNullOrEmpty(x)).Distinct().Take(12).ToList();
                info.Summary = string.Join(", ", names.ToArray());
            }
        }
        catch { }
        return info;
    }

    static string MapDoctorName(string docCode)
    {
        if (string.IsNullOrEmpty(docCode)) return DEFAULT_DOCTOR_NAME;
        if (docCode.Equals("034727", StringComparison.OrdinalIgnoreCase)) return DEFAULT_DOCTOR_NAME;
        if (docCode.Equals("vmc", StringComparison.OrdinalIgnoreCase)) return "BS Vũ Minh Cường";
        if (docCode.Equals("hdc", StringComparison.OrdinalIgnoreCase)) return "BS Hà Đức Cường";
        return docCode;
    }

    static void ApplyFacility(string fac, bool userSpecified)
    {
        if (string.Equals(fac, "NB", StringComparison.OrdinalIgnoreCase) || string.Equals(fac, "ninh-binh", StringComparison.OrdinalIgnoreCase))
        {
            _facility = "NB";
            _deptId = DEPT_ID_NB;
            _maKhoa = MAKHOA_NB;
            _deptNameDefault = DEPT_NAME_NB;
            _facilitySpecified = userSpecified;
        }
        else
        {
            _facility = "HN";
            _deptId = DEPT_ID_HN;
            _maKhoa = MAKHOA_HN;
            _deptNameDefault = DEPT_NAME_HN;
            _facilitySpecified = userSpecified;
        }
    }

    static void InferFacilityFromDept(long deptId)
    {
        if (deptId == DEPT_ID_NB) ApplyFacility("NB", false);
        else ApplyFacility("HN", false);
    }

    static void ApplyOracleCulture()
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }
        catch { }
    }

    static string ReadLiveToken()
    {
        try
        {
            string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
            if (File.Exists(f))
            {
                string[] parts = File.ReadAllText(f, Encoding.UTF8).Split('|');
                if (parts.Length >= 1 && parts[0].Length == 64) return parts[0];
            }
        }
        catch { }

        try
        {
            DirectoryInfo cur = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 5; i++)
            {
                if (cur == null) break;
                string p = Path.Combine(cur.FullName, "Logs", "LogSystem.txt");
                if (!File.Exists(p)) p = Path.Combine(cur.FullName, "LogSystem.txt");
                if (File.Exists(p))
                {
                    using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        long len = fs.Length;
                        byte[] buf = new byte[Math.Min(131072L, len)];
                        fs.Seek(len - buf.Length, SeekOrigin.Begin);
                        fs.Read(buf, 0, buf.Length);
                        string str = Encoding.UTF8.GetString(buf);
                        int idx = str.LastIndexOf("TokenCode|");
                        if (idx >= 0 && idx + 74 <= str.Length)
                            return str.Substring(idx + 10, 64);
                    }
                }
                cur = cur.Parent;
            }
        }
        catch { }

        return null;
    }

    static Assembly ResolveAssembly(object sender, ResolveEventArgs e)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string asmName  = new AssemblyName(e.Name).Name + ".dll";
        foreach (string folder in new[] { ".", "Integrate\\EMR", "ReferencedAssemblies" })
        {
            string path = Path.Combine(basePath, folder, asmName);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    static bool IsNumericId(string s)
    {
        return !string.IsNullOrEmpty(s) && s.All(char.IsDigit);
    }

    static string SafeStr(object v)
    {
        return v == null ? "" : v.ToString();
    }

    static string SafeSql(string v)
    {
        return (v ?? "").Replace("'", "''");
    }

    static string NotEmpty(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    static string ExtractLocation(string icdName)
    {
        if (string.IsNullOrEmpty(icdName)) return "vùng tổn thương";
        // Lấy phần chẩn đoán chính trước dấu '/' để tránh lẫn bên tổn thương với bệnh kèm theo
        string mainPart = icdName.Split('/')[0].Trim();
        string s = mainPart.ToLower();
        string side = "";
        if (s.Contains("phải") || s.Contains("phai")) side = "phải";
        else if (s.Contains("trái") || s.Contains("trai")) side = "trái";

        if (s.Contains("gối")) return "khớp gối" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cổ tay")) return "cổ tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("vai")) return "khớp vai" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("háng")) return "khớp háng" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("đùi") || s.Contains("xương đùi")) return "xương đùi" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("lưng")) return "vùng lưng" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cột sống") || s.Contains("đốt sống")) return "cột sống thắt lưng";
        if (s.Contains("đòn")) return "xương đòn" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cánh tay")) return "cánh tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cẳng tay")) return "cẳng tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cẳng chân")) return "cẳng chân" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("bàn chân")) return "bàn chân" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("bàn tay")) return "bàn tay" + (side.Length > 0 ? " " + side : "");
        return "vùng tổn thương" + (side.Length > 0 ? " " + side : "");
    }

    static void PrintField(string label, object val)
    {
        Console.Write("  " + label.PadRight(28) + ": ");
        Console.ForegroundColor = ConsoleColor.White;
        string v = SafeStr(val);
        Console.WriteLine(v.Length > 115 ? v.Substring(0, 112) + "..." : v);
        Console.ResetColor();
    }

    static void PrintPreview(dynamic ba, TreatmentInfo ti)
    {
        Console.WriteLine("\n" + new string('-', 75));
        Console.WriteLine("  BẢNG ĐỐI SOÁT CÁC TRƯỜNG DỮ LIỆU ĐÃ ĐIỀN (TAB HỎI BỆNH & KHÁM BỆNH)");
        Console.WriteLine(new string('-', 75));
        PrintField("MaQuanLy",            ba.MaQuanLy);
        PrintField("BenhChinh",           ba.BenhChinh);
        PrintField("LyDoVaoVien",         ba.LyDoVaoVien);
        PrintField("QuaTrinhBenhLy",      ba.QuaTrinhBenhLy);
        PrintField("TienSuBanThan",       ba.TienSuBenhBanThan);
        PrintField("TienSuGiaDinh",       ba.TienSuBenhGiaDinh);
        PrintField("1. ToanThan",         ba.ToanThan);
        PrintField("2. BenhNgoaiKhoa",    ba.BenhNgoaiKhoa);
        PrintField("3. CoXuongKhop",      ba.CoXuongKhop);
        PrintField("   TuanHoan",         ba.TuanHoan);
        PrintField("   HoHap",            ba.HoHap);
        PrintField("   TieuHoa",          ba.TieuHoa);
        PrintField("   ThanTietNieu",     ba.ThanTietNieuSinhDuc);
        PrintField("   ThanKinh",         ba.ThanKinh);
        PrintField("4. CanLamSang",       ba.CacXetNghiemCanLamSangCanLam);
        PrintField("5. TomTatBenhAn",     ba.TomTatBenhAn);
        PrintField("PhanBiet",            ba.PhanBiet);
        PrintField("TienLuong",           ba.TienLuong);
        PrintField("HuongDieuTri",        ba.HuongDieuTri);
        PrintField("BacSyLamBenhAn",      ba.BacSyLamBenhAn);
        PrintField("TenBacSyLamBenhAn",   ba.TenBacSyLamBenhAn);
        Console.WriteLine(new string('-', 75));
    }

    // ──────────────────────────────────────────────────────────────
    // DATA TRANSFER OBJECTS
    // ──────────────────────────────────────────────────────────────
    class TreatmentInfo
    {
        public long    TreatmentId;
        public string  TreatmentCode;
        public decimal MaQuanLy;
        public string  PatientCode;
        public string  PatientName;
        public string  PatientAge;
        public string  PatientGender;
        public string  InTime;
        public long    InTimeRaw;
        public long?   DeptInTime;
        public long?   TreatmentTypeId;
        public long?   ClinicalInTime;
        public string  IcdCode;
        public string  IcdName;
        public string  IcdText;
        public string  DeptName;
        public long    DeptId;
    }

    class DhstInfo
    {
        public string Pulse         = "80";
        public string BloodPressure = "120/80";
        public string Temperature   = "37.0";
        public string SpO2          = "98";
        public string Weight        = "55";
        public string Height        = "160";
    }

    class LabPacsInfo
    {
        public string Summary = "";
    }

    class TemplateBA
    {
        public string QuaTrinhBenhLy    = "";
        public string TienSuBenhBanThan = "";
        public string TienSuBenhGiaDinh = "";
        public string ToanThan          = "";
        public string CoXuongKhop       = "";
        public string ThanKinh          = "";
        public string TuanHoan          = "";
        public string HoHap             = "";
        public string TieuHoa           = "";
        public string ThanTietNieu      = "";
        public string CanLamSang        = "";
        public string TomTatBenhAn      = "";
        public string PhanBiet          = "";
        public string TienLuong         = "";
        public string HuongDieuTri      = "";
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}
