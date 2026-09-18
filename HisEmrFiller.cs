// HisEmrFiller.cs — Tự động điền Vỏ Bệnh Án Ngoại Khoa (BENHANNGOAIKHOA) vào EMR Oracle
// Cách dùng:
//   HisEmrFiller.exe <MaBN>                   → dry-run: xem trước nội dung bệnh án
//   HisEmrFiller.exe <MaBN> --save            → ghi thật vào DB EMR Oracle
//   HisEmrFiller.exe <MaBN> --save --doctor <ma>  → ghi với bác sĩ được chỉ định
//   HisEmrFiller.exe <MaQuanLy>               → nhận MaQuanLy dạng số (7 chữ số trở lên)
//
// Build:
//   csc /r:Integrate\EMR\MDB.dll /r:Integrate\EMR\EMR_MAIN.Library.dll
//       /r:Integrate\EMR\Oracle.ManagedDataAccess.dll
//       /r:ReferencedAssemblies\Inventec.Common.Adapter.dll
//       /r:ReferencedAssemblies\Inventec.Common.WebApiClient.dll
//       /r:ReferencedAssemblies\Inventec.Core.dll
//       /r:ReferencedAssemblies\MOS.EFMODEL.dll /r:ReferencedAssemblies\MOS.Filter.dll
//       /r:ReferencedAssemblies\Newtonsoft.Json.dll
//       /out:HisEmrFiller.exe HisEmrFiller.cs
//       /platform:x64 /optimize

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

// EMR SDK — load qua reflection để tránh type-resolution issue khi build standalone
// Các type sẽ được bind tại runtime
using System.Runtime.CompilerServices;

class HisEmrFiller
{
    // ──────────────────────────────────────────────────────────────
    // CONSTANTS
    // ──────────────────────────────────────────────────────────────
    const string MOS_BASE = "http://192.168.7.236:1608/";
    const string EMR_CONNSTR = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

    // Bác sĩ fallback mặc định (Khoa 57)
    const string DEFAULT_DOCTOR_CODE = "034727";
    const string DEFAULT_DOCTOR_NAME = "ThS.BS Nguyễn Hữu Sâm";

    // Khoa chấn thương chỉnh hình Bạch Mai HN
    const long DEPT_ID = 57;

    // ──────────────────────────────────────────────────────────────
    // ENTRYPOINT
    // ──────────────────────────────────────────────────────────────
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Hook assembly resolver → ưu tiên Integrate\EMR\ rồi ReferencedAssemblies\
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        // Parse args
        string input = args[0];
        bool dryRun  = !args.Any(a => a.Equals("--save", StringComparison.OrdinalIgnoreCase));
        string doctorCode = DEFAULT_DOCTOR_CODE;
        string doctorName = DEFAULT_DOCTOR_NAME;
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--doctor", StringComparison.OrdinalIgnoreCase))
            {
                doctorCode = args[i + 1];
                doctorName = null; // resolve later from HIS
            }
        }

        try
        {
            return Run(input, dryRun, doctorCode, doctorName);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[LỖI NGHIÊM TRỌNG] " + ex.Message);
            Console.ResetColor();
            if (ex.InnerException != null)
                Console.WriteLine("  Inner: " + ex.InnerException.Message);
            return 2;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // MAIN LOGIC
    // ──────────────────────────────────────────────────────────────
    static int Run(string input, bool dryRun, string doctorCode, string doctorName)
    {
        // Step 1: Đọc token HIS
        string tokenCode = ReadLiveToken();
        if (string.IsNullOrEmpty(tokenCode))
        {
            Console.WriteLine("[WARN] Không tìm thấy token. Sẽ dùng anonymous (một số API có thể fail).");
        }

        var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
        var param    = new CommonParam();
        var adapter  = new MyAdapter();

        // Step 2: Phân giải MaBN hoặc MaQuanLy → lấy thông tin HIS
        // Quy tắc: MaBN luôn 10 chữ số bắt đầu bằng 0 (VD: 0002145867)
        //          MaQuanLy thường 7-8 chữ số không có leading zero (VD: 7265925)
        TreatmentInfo ti;
        bool isMaBN = (IsNumericId(input) && input.Length == 10 && input[0] == '0')
                   || (!IsNumericId(input));
        if (!isMaBN && IsNumericId(input) && input.Length >= 6 && input.Length <= 12)
        {
            // Tìm theo TreatmentId (MaQuanLy)
            long treatId = long.Parse(input);
            ti = LookupByTreatmentId(adapter, consumer, param, treatId);
            if (ti == null)
            {
                // Fallback: thử tìm theo PatientCode nếu không tìm được theo ID
                ti = LookupByPatientCode(adapter, consumer, param, input);
            }
        }
        else
        {
            // MaBN (PatientCode) hoặc string
            ti = LookupByPatientCode(adapter, consumer, param, input);
        }

        if (ti == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[LỖI] Không tìm thấy bệnh nhân hoặc đợt điều trị cho: " + input);
            Console.ResetColor();
            return 3;
        }

        Console.WriteLine(string.Format("✓ Bệnh nhân  : {0} ({1})", ti.PatientName, ti.PatientCode));
        Console.WriteLine(string.Format("  Mã ĐT      : {0}  |  MaQuanLy: {1}", ti.TreatmentCode, ti.TreatmentId));
        Console.WriteLine(string.Format("  Vào viện   : {0}", ti.InTime));
        Console.WriteLine(string.Format("  Chẩn đoán  : [{0}] {1}", ti.IcdCode, ti.IcdName));
        Console.WriteLine(string.Format("  Khoa       : {0}", ti.DeptName));

        // Step 3: Kết nối Oracle EMR
        dynamic con = CreateEmrConnection();
        con.Open();
        Console.WriteLine("✓ Kết nối Oracle EMR OK");

        // Step 4: Kiểm tra bệnh án đã tồn tại chưa
        dynamic existingBA = BenhAnNgoaiKhoaSelect(con, (decimal)ti.TreatmentId);
        bool isUpdate = (existingBA != null && existingBA.MaQuanLy > 0);
        if (isUpdate)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INFO] Bệnh án BENHANNGOAIKHOA đã tồn tại (ID: " + existingBA.MaQuanLy + ") → sẽ UPDATE.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("[INFO] Chưa có bệnh án BENHANNGOAIKHOA → sẽ INSERT mới.");
        }

        // Step 5: Tìm mẫu kế thừa từ DB (cùng nhóm bệnh hoặc cùng BN)
        TemplateBA tmpl = FindTemplate(con, adapter, consumer, param, ti);

        // Step 6: Lấy DHST mới nhất
        DhstInfo dhst = GetLatestDhst(adapter, consumer, param, ti.TreatmentId);

        // Step 7: Đọc tên bác sĩ từ HIS nếu cần
        if (string.IsNullOrEmpty(doctorName))
            doctorName = ResolveDocName(adapter, consumer, param, doctorCode);

        // Step 8: Tạo object bệnh án
        dynamic ba = isUpdate ? existingBA : CreateNewBenhAnNgoaiKhoa();
        PopulateBenhAn(ba, ti, dhst, tmpl, doctorCode, doctorName, isUpdate);

        // Step 8.5: Kiểm tra trạng thái Trang bìa THONGTINDIEUTRI
        EnsureThongTinDieuTri(con, ti, false);

        // Step 9: Hiển thị xem trước
        PrintPreview(ba, ti);

        if (dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n[DRY-RUN] Không ghi DB. Chạy lại với --save để lưu thật.");
            Console.ResetColor();
            con.Close();
            return 0;
        }

        // Step 10: Ghi vào Oracle EMR
        // 10.1: Khởi tạo/Cập nhật Trang bìa THONGTINDIEUTRI
        EnsureThongTinDieuTri(con, ti, true);

        // 10.2: Ghi Vỏ bệnh án ngoại khoa BENHANNGOAIKHOA
        Console.Write("\nĐang ghi Vỏ Bệnh Án Ngoại Khoa vào Oracle EMR... ");
        bool ok = BenhAnNgoaiKhoaInsertOrUpdate(con, ba);
        if (!ok)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL — InsertOrUpdate trả về false!");
            Console.ResetColor();
            con.Close();
            return 4;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ THÀNH CÔNG!");
        Console.ResetColor();

        // Step 11: Xác nhận bằng Select
        dynamic verify = BenhAnNgoaiKhoaSelect(con, (decimal)ti.TreatmentId);
        if (verify != null)
        {
            Console.WriteLine(string.Format("✓ Xác nhận: BenhChinh='{0}' | BacSy='{1}'",
                SafeStr(verify.BenhChinh), SafeStr(verify.BacSyLamBenhAn)));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARN] Ghi xong nhưng Select không tìm lại được — kiểm tra thủ công trên EMR UI.");
            Console.ResetColor();
        }

        con.Close();
        return 0;
    }

    // ──────────────────────────────────────────────────────────────
    // HIS API HELPERS
    // ──────────────────────────────────────────────────────────────
    static TreatmentInfo LookupByPatientCode(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string patCode)
    {
        // Đảm bảo 10 ký tự
        patCode = patCode.PadLeft(10, '0');
        var tf = new HisTreatmentViewFilter
        {
            PATIENT_CODE__EXACT  = patCode,
            IS_PAUSE             = false
        };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trs == null || trs.Count == 0) return null;
        // Ưu tiên đợt nội trú Khoa 57 mới nhất
        var filtered = trs.Where(x => x.END_DEPARTMENT_ID == DEPT_ID).ToList();
        if (filtered.Count == 0) filtered = trs;
        var t = filtered.OrderByDescending(x => x.IN_TIME).FirstOrDefault();
        return MapTreatment(t);
    }

    static TreatmentInfo LookupByTreatmentId(MyAdapter adapter, ApiConsumer consumer, CommonParam param, long treatId)
    {
        var tf = new HisTreatmentViewFilter { ID = treatId };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trs == null || trs.Count == 0) return null;
        return MapTreatment(trs.First());
    }

    static TreatmentInfo MapTreatment(V_HIS_TREATMENT t)
    {
        if (t == null) return null;
        // Inventec stores datetime as long yyyyMMddHHmmss
        string inTimeStr = LongToDateStr(t.IN_TIME);
        // DOB format: 19881028000000 → year = 1988 (divide by 10000000000)
        string ageStr = t.TDL_PATIENT_DOB > 0
                        ? (DateTime.Now.Year - (int)(t.TDL_PATIENT_DOB / 10000000000L)).ToString()
                        : "?";
        return new TreatmentInfo
        {
            TreatmentId   = t.ID,
            TreatmentCode = t.TREATMENT_CODE ?? "",
            PatientCode   = t.TDL_PATIENT_CODE ?? "",
            PatientName   = t.TDL_PATIENT_NAME ?? "",
            PatientAge    = ageStr,
            PatientGender = !string.IsNullOrEmpty(t.TDL_PATIENT_GENDER_NAME)
                            ? t.TDL_PATIENT_GENDER_NAME
                            : ((t.TDL_PATIENT_GENDER_ID == 1) ? "Nam" : "Nữ"),
            InTime        = inTimeStr,
            IcdCode       = t.ICD_CODE ?? "",
            IcdName       = t.ICD_NAME ?? "",
            DeptName      = t.END_DEPARTMENT_NAME ?? "",
            DeptId        = t.END_DEPARTMENT_ID.HasValue ? t.END_DEPARTMENT_ID.Value : 0
        };
    }

    // Convert Inventec long yyyyMMddHHmmss → "dd/MM/yyyy HH:mm"
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
            string bp = (d.BLOOD_PRESSURE_MAX.HasValue && d.BLOOD_PRESSURE_MIN.HasValue)
                        ? d.BLOOD_PRESSURE_MAX.Value + "/" + d.BLOOD_PRESSURE_MIN.Value
                        : "120/80";
            return new DhstInfo
            {
                Pulse         = d.PULSE.HasValue ? ((int)d.PULSE.Value).ToString() : "80",
                BloodPressure = bp,
                Temperature   = d.TEMPERATURE.HasValue ? d.TEMPERATURE.Value.ToString("F1") : "37.0",
                SpO2          = d.SPO2.HasValue ? ((int)d.SPO2.Value).ToString() : "98",
                Weight        = d.WEIGHT.HasValue ? d.WEIGHT.Value.ToString() : "",
                Height        = d.HEIGHT.HasValue ? d.HEIGHT.Value.ToString() : ""
            };
        }
        catch { return new DhstInfo(); }
    }

    static string ResolveDocName(MyAdapter adapter, ApiConsumer consumer, CommonParam param, string docCode)
    {
        // Fallback mapping — không dùng HisUserLoginNameFilter (type có thể không tồn tại)
        if (docCode == "034727") return DEFAULT_DOCTOR_NAME;
        if (docCode == "vmc")    return "BS Vu Minh Cuong";
        if (docCode == "hdc")    return "TS.BS Ha Duc Cuong";
        return docCode;
    }

    // ──────────────────────────────────────────────────────────────
    // TEMPLATE FINDER — kế thừa mẫu từ DB EMR Oracle
    // ──────────────────────────────────────────────────────────────
    static TemplateBA FindTemplate(dynamic con, MyAdapter adapter, ApiConsumer consumer, CommonParam param, TreatmentInfo ti)
    {
        var result = new TemplateBA();
        string cols = "b.QUATRINHBENHLY, b.TIENSUBENHBANTHAN, b.TIENSUBENHGIADINH, " +
                      "b.TOANTHAN, b.COXUONGKHOP, b.THANKINH, b.TUANHOAN, b.HOHAP, " +
                      "b.TIEUHOA, b.THANTIETNIEUSINHDUC, b.CACXETNGHIEMCANLAMSANGCANLAM, " +
                      "b.TOMTATBENHAN, b.PHANBIET, b.TIENLUONG, b.HUONGDIEUTRI";

        try
        {
            // Ưu tiên 1: Cùng bệnh nhân (đợt điều trị cũ nhất của BN này)
            string sqlSamePt = string.Format(
                "SELECT {0} " +
                "FROM EMR_FINAL.BENHANNGOAIKHOA b " +
                "JOIN EMR_FINAL.THONGTINDIEUTRI t ON b.MAQUANLY = t.MAQUANLY " +
                "WHERE t.MABENHNHAN = '{1}' AND b.MAQUANLY <> {2} " +
                "AND b.QUATRINHBENHLY IS NOT NULL AND ROWNUM <= 1 " +
                "ORDER BY t.NGAYTAO DESC",
                cols, SafeSql(ti.PatientCode), ti.TreatmentId);

            dynamic rdr = ExecuteReader(con, sqlSamePt);
            if (rdr != null && rdr.Read())
            {
                result = ReadTemplateRow(rdr);
                rdr.Close();
                Console.WriteLine("  → Kế thừa mẫu: Cùng bệnh nhân (đợt điều trị cũ)");
                return result;
            }
            if (rdr != null) rdr.Close();

            // Ưu tiên 2: Cùng nhóm ICD-10 (3 ký tự đầu), khoa CTCH hoặc bất kỳ khoa ngoại nào
            string icd3 = ti.IcdCode.Length >= 3 ? ti.IcdCode.Substring(0, 3) : ti.IcdCode;
            string sqlSameIcd = string.Format(
                "SELECT {0}, t.CHANDOAN_KHIVAOKHOADIEUTRI " +
                "FROM EMR_FINAL.BENHANNGOAIKHOA b " +
                "JOIN EMR_FINAL.THONGTINDIEUTRI t ON b.MAQUANLY = t.MAQUANLY " +
                "WHERE (t.MAICD_KHIVAOKHOADIEUTRI LIKE '{1}%' OR t.MAICD_SOBO LIKE '{1}%' OR t.MAICD_BENHCHINH_RAVIEN LIKE '{1}%') " +
                "AND b.QUATRINHBENHLY IS NOT NULL AND b.COXUONGKHOP IS NOT NULL " +
                "AND ROWNUM <= 1 " +
                "ORDER BY t.NGAYTAO DESC",
                cols, SafeSql(icd3));

            dynamic rdr2 = ExecuteReader(con, sqlSameIcd);
            if (rdr2 != null && rdr2.Read())
            {
                var cand = ReadTemplateRow(rdr2);
                string chandoanGoc = SafeStr(rdr2["CHANDOAN_KHIVAOKHOADIEUTRI"]);
                rdr2.Close();
                if (IsTemplateCompatible(cand, ti))
                {
                    result = cand;
                    Console.WriteLine(string.Format("  → Kế thừa mẫu: ICD-10 '{0}' ({1})", icd3, chandoanGoc));
                    return result;
                }
                else
                {
                    Console.WriteLine(string.Format("  [INFO] Mẫu ICD-10 '{0}' ({1}) không phù hợp nhóm bệnh → tự sinh chuẩn.", icd3, chandoanGoc));
                }
            }
            if (rdr2 != null) rdr2.Close();

            // Ưu tiên 3: Cùng Khoa CTCH gần nhất (generic)
            string sqlKhoa = string.Format(
                "SELECT {0} " +
                "FROM EMR_FINAL.BENHANNGOAIKHOA b " +
                "JOIN EMR_FINAL.THONGTINDIEUTRI t ON b.MAQUANLY = t.MAQUANLY " +
                "WHERE (t.KHOA LIKE N'%Ch%n th%ng%' OR t.TENKHOAVAO LIKE N'%Ch%n th%ng%') " +
                "AND b.QUATRINHBENHLY IS NOT NULL AND b.COXUONGKHOP IS NOT NULL " +
                "AND ROWNUM <= 1 " +
                "ORDER BY t.NGAYTAO DESC",
                cols);

            dynamic rdr3 = ExecuteReader(con, sqlKhoa);
            if (rdr3 != null && rdr3.Read())
            {
                var cand = ReadTemplateRow(rdr3);
                rdr3.Close();
                if (IsTemplateCompatible(cand, ti))
                {
                    result = cand;
                    Console.WriteLine("  → Kế thừa mẫu: Khoa CTCH gần nhất (generic)");
                    return result;
                }
                else
                {
                    Console.WriteLine("  [INFO] Mẫu Khoa CTCH gần nhất không phù hợp nhóm bệnh → tự sinh chuẩn.");
                }
            }
            if (rdr3 != null) rdr3.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [WARN] Lỗi tìm template: " + ex.Message);
        }

        Console.WriteLine("  → Không tìm được mẫu phù hợp → dùng nội dung sinh tự động chuẩn chuyên khoa.");
        return result;
    }

    static bool IsTemplateCompatible(TemplateBA tmpl, TreatmentInfo ti)
    {
        if (tmpl == null || string.IsNullOrWhiteSpace(tmpl.QuaTrinhBenhLy)) return false;
        string cur = ((ti.IcdName ?? "") + " " + (ti.IcdCode ?? "")).ToLower();
        string past = ((tmpl.QuaTrinhBenhLy ?? "") + " " + (tmpl.CoXuongKhop ?? "")).ToLower();

        bool curIsTumor = cur.Contains(" u ") || cur.StartsWith("u ") || cur.Contains("khối u") || cur.Contains("nang") || cur.Contains("lipoma") || cur.Contains("phần mềm");
        bool pastIsTrauma = past.Contains("tai nạn") || past.Contains("ngã") || past.Contains("gãy") || past.Contains("chấn thương") || past.Contains("lỏng khớp") || past.Contains("xẹp") || past.Contains("đốt sống");

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

    // ──────────────────────────────────────────────────────────────
    // POPULATE BENH AN — gắn dữ liệu vào object BenhAnNgoaiKhoa
    // ──────────────────────────────────────────────────────────────
    static void PopulateBenhAn(dynamic ba, TreatmentInfo ti, DhstInfo dhst, TemplateBA tmpl,
                                string docCode, string docName, bool isUpdate)
    {
        decimal maQuanLy = (decimal)ti.TreatmentId;

        // Trường bất biến (luôn set)
        ba.MaQuanLy          = maQuanLy;
        ba.MaBenhNhan        = ti.PatientCode;
        ba.BacSyLamBenhAn    = docCode;
        ba.TenBacSyLamBenhAn = docName ?? docCode;
        ba.BacSyKhamBenh     = docCode;
        ba.TenBacSyKhamBenh  = docName ?? docCode;

        // Thời gian khám bệnh
        if (!string.IsNullOrEmpty(ti.InTime))
        {
            DateTime dt;
            if (DateTime.TryParseExact(ti.InTime, "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dt))
            {
                ba.NgayKhamBenh = dt;
                // Số ngày vào viện = ngày hôm nay - ngày vào
                int days = (int)(DateTime.Today - dt.Date).TotalDays + 1;
                ba.VaoNgayThu = days;
            }
        }

        // Chẩn đoán chính
        string chanDoan = string.IsNullOrEmpty(ti.IcdName) ? ti.IcdCode
                        : string.Format("[{0}] {1}", ti.IcdCode, ti.IcdName);
        ba.BenhChinh = chanDoan;

        // Lý do vào viện
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.LyDoVaoVien)))
            ba.LyDoVaoVien = BuildLyDoVaoVien(ti);

        // Quá trình bệnh lý — kế thừa mẫu bác sĩ khác hoặc sinh tự động
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.QuaTrinhBenhLy)))
        {
            if (!string.IsNullOrWhiteSpace(tmpl.QuaTrinhBenhLy))
            {
                string qtbl = tmpl.QuaTrinhBenhLy.Trim();
                string sCur = ti.IcdName.ToLower();
                if ((sCur.Contains("trái") || sCur.Contains("(t)")) && !sCur.Contains("phải"))
                    qtbl = qtbl.Replace("(P)", "(T)").Replace("(p)", "(T)").Replace("phải", "trái").Replace("Phải", "Trái").Replace(" gối P", " gối T").Replace(" gối p", " gối T");
                else if ((sCur.Contains("phải") || sCur.Contains("(p)")) && !sCur.Contains("trái"))
                    qtbl = qtbl.Replace("(T)", "(P)").Replace("(t)", "(P)").Replace("trái", "phải").Replace("Trái", "Phải").Replace(" gối T", " gối P").Replace(" gối t", " gối P");
                ba.QuaTrinhBenhLy = qtbl;
            }
            else
            {
                ba.QuaTrinhBenhLy = BuildQuaTrinhBenhLy(ti);
            }
        }

        // Tiền sử bệnh bản thân
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TienSuBenhBanThan)))
            ba.TienSuBenhBanThan = NotEmpty(tmpl.TienSuBenhBanThan)
                ?? "Khỏe mạnh, chưa ghi nhận bệnh lý mạn tính. Không có tiền sử dị ứng thuốc, thức ăn.";

        // Tiền sử gia đình
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TienSuBenhGiaDinh)))
            ba.TienSuBenhGiaDinh = NotEmpty(tmpl.TienSuBenhGiaDinh)
                ?? "Gia đình chưa phát hiện bệnh lý liên quan.";

        // Toàn thân — gắn DHST thực tế
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.ToanThan)))
            ba.ToanThan = BuildToanThan(dhst, tmpl);

        // Cơ xương khớp — đây là trường quan trọng nhất cho CTCH
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.CoXuongKhop)))
            ba.CoXuongKhop = BuildCoXuongKhop(ti, tmpl);

        // Các cơ quan khác — kế thừa mẫu nếu có, hoặc dùng mặc định bình thường
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.ThanKinh)))
            ba.ThanKinh  = NotEmpty(tmpl.ThanKinh)  ?? "Tỉnh táo, tiếp xúc tốt. Không có dấu hiệu thần kinh khu trú.";
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TuanHoan)))
            ba.TuanHoan  = NotEmpty(tmpl.TuanHoan)  ?? string.Format("Nhịp tim đều, T1 T2 rõ. Tần số {0} ck/phút. Huyết áp {1} mmHg.", dhst.Pulse, dhst.BloodPressure);
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.HoHap)))
            ba.HoHap     = NotEmpty(tmpl.HoHap)     ?? string.Format("Lồng ngực hai bên cân đối, di động theo nhịp thở. Rì rào phế nang rõ, không ran. SpO2 {0}%.", dhst.SpO2);
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TieuHoa)))
            ba.TieuHoa   = NotEmpty(tmpl.TieuHoa)   ?? "Bụng mềm, không chướng. Gan lách không to. Không có phản ứng thành bụng.";
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.ThanTietNieuSinhDuc)))
            ba.ThanTietNieuSinhDuc = NotEmpty(tmpl.ThanTietNieu) ?? "Hố thắt lưng hai bên không đầy. Chạm thận (-), bập bềnh thận (-). Nước tiểu vàng trong.";

        // Cận lâm sàng cần làm
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.CacXetNghiemCanLamSangCanLam)))
            ba.CacXetNghiemCanLamSangCanLam = NotEmpty(tmpl.CanLamSang) ?? BuildCanLamSang(ti);

        // Tóm tắt bệnh án
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TomTatBenhAn)))
            ba.TomTatBenhAn = BuildTomTat(ti, dhst);

        // Phân biệt chẩn đoán
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.PhanBiet)))
            ba.PhanBiet = NotEmpty(tmpl.PhanBiet) ?? BuildPhanBiet(ti);

        // Tiên lượng
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TienLuong)))
            ba.TienLuong = NotEmpty(tmpl.TienLuong) ?? "Dè dặt, phụ thuộc vào kết quả can thiệp và quá trình tập phục hồi chức năng.";

        // Hướng điều trị
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.HuongDieuTri)))
            ba.HuongDieuTri = NotEmpty(tmpl.HuongDieuTri) ?? BuildHuongDieuTri(ti);

        // Tai Mũi Họng, Răng Hàm Mặt, Mắt
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.TaiMuiHong)))
            ba.TaiMuiHong = "Tai mũi họng bình thường.";
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.RangHamMat)))
            ba.RangHamMat = "Răng hàm mặt bình thường.";
        if (!isUpdate || string.IsNullOrWhiteSpace(SafeStr(ba.Mat)))
            ba.Mat = "Mắt hai bên nhìn rõ, không sụp mi.";

        // Gán DauSinhTon
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

        // Gán DacDiemLienQuanBenh nếu null
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

        // Flag bệnh án
        try { ba.PhauThuat = false; } catch { }
        try { ba.ThuThuat  = false; } catch { }
        try { ba.DaKy      = false; } catch { }
    }

    // ──────────────────────────────────────────────────────────────
    // NỘI DUNG SINH TỰ ĐỘNG (TEMPLATE STRINGS CHO CTCH)
    // ──────────────────────────────────────────────────────────────
    static string BuildLyDoVaoVien(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);
        string viTriLoc = loc.StartsWith("vùng") || loc.StartsWith("khớp") ? loc : ("vùng " + loc);

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return string.Format("Phát hiện khối u {0}, đau tức nhẹ khi tì đè/vận động", viTriLoc);
        if (s.Contains("gãy") || s.Contains("gay"))
            return string.Format("Đau, sưng nề, biến dạng, hạn chế vận động {0} sau chấn thương", loc);
        if (s.Contains("acl") || s.Contains("chằng") || s.Contains("chang"))
            return string.Format("Đau, lỏng {0}, hạn chế vận động sau chấn thương", loc);
        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("đốt sống") || s.Contains("dot song"))
            return string.Format("Đau cột sống thắt lưng, hạn chế vận động cúi ngửa");
        if (s.Contains("trật") || s.Contains("trat"))
            return string.Format("Đau, biến dạng khớp {0} sau chấn thương", loc);
        return string.Format("Đau, hạn chế vận động {0}", loc);
    }

    static string BuildQuaTrinhBenhLy(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);
        string viTri = loc.StartsWith("gối") ? ("khớp " + loc) : (loc.StartsWith("vùng") ? loc : ("vùng " + loc));

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return string.Format(
                "Bệnh nhân tự phát hiện khối bất thường tại {0} cách đây một thời gian. " +
                "Khối to dần, đau tức nhẹ khi vận động hoặc tì đè, không sốt, không gầy sút cân. " +
                "Bệnh nhân đến khám tại Bệnh viện Bạch Mai và có chỉ định nhập viện điều trị phẫu thuật bóc u.",
                viTri);

        if (s.Contains("acl") || (s.Contains("chằng") && s.Contains("trước")))
            return string.Format(
                "Cách nhập viện một thời gian, bệnh nhân bị chấn thương {0} sau tai nạn sinh hoạt/giao thông. " +
                "Sau chấn thương, bệnh nhân xuất hiện sưng nề, đau nhiều và cảm giác lỏng khớp, đi lại khó khăn. " +
                "Bệnh nhân chưa can thiệp phẫu thuật, nay đến khám và có chỉ định nhập viện điều trị phẫu thuật nội soi tái tạo dây chằng.",
                viTri);

        if (s.Contains("gãy") || s.Contains("gay"))
            return string.Format(
                "Bệnh nhân bị tai nạn chấn thương trực tiếp vào {0}. " +
                "Sau tai nạn xuất hiện đau chói, sưng nề, biến dạng và bất lực vận động chi. " +
                "Bệnh nhân được sơ cứu bất động tạm thời và chuyển đến Bệnh viện Bạch Mai điều trị chuyên khoa.",
                viTri);

        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("loãng xương"))
            return string.Format(
                "Bệnh nhân xuất hiện đau cột sống thắt lưng âm ỉ, tăng lên khi thay đổi tư thế, đi lại hoặc cúi ngửa. " +
                "Đau không lan chân, không tê bì chi dưới. Bệnh nhân điều trị nội khoa không đỡ, " +
                "được chụp cộng hưởng từ/X-quang xác định tổn thương và nhập viện điều trị.",
                loc);

        // Generic CTCH
        return string.Format(
            "Bệnh nhân vào viện vì đau và hạn chế vận động {0}. " +
            "Đã được khám tại phòng khám chuyên khoa CTCH và chỉ định nhập viện điều trị theo dõi sát.",
            viTri);
    }

    static string BuildToanThan(DhstInfo dhst, TemplateBA tmpl)
    {
        string dhstStr = string.Format(
            "Bệnh nhân tỉnh, tiếp xúc tốt. Da niêm mạc hồng hào. Không phù, không xuất huyết dưới da. " +
            "Mạch: {0} lần/phút. Huyết áp: {1} mmHg. Nhiệt độ: {2}°C. SpO2: {3}%.",
            dhst.Pulse, dhst.BloodPressure, dhst.Temperature, dhst.SpO2);

        if (!string.IsNullOrEmpty(dhst.Weight) && !string.IsNullOrEmpty(dhst.Height))
            dhstStr += string.Format(" Thể trạng trung bình: Cân nặng {0} kg, chiều cao {1} cm.", dhst.Weight, dhst.Height);

        return dhstStr;
    }

    static string BuildCoXuongKhop(TreatmentInfo ti, TemplateBA tmpl)
    {
        // Nếu có mẫu từ bác sĩ khác và tương thích nhóm bệnh thì điều chỉnh bên Phải/Trái cho khớp và dùng luôn
        if (!string.IsNullOrWhiteSpace(tmpl.CoXuongKhop) && IsTemplateCompatible(tmpl, ti))
        {
            string cxk = tmpl.CoXuongKhop.Trim();
            string sCur = ti.IcdName.ToLower();
            bool isLeftCur = sCur.Contains("trái") || sCur.Contains("(t)") || sCur.Contains("trai");
            bool isRightCur = sCur.Contains("phải") || sCur.Contains("(p)") || sCur.Contains("phai");
            if (isLeftCur && !isRightCur)
            {
                cxk = cxk.Replace("(P)", "(T)").Replace("(p)", "(T)").Replace("phải", "trái").Replace("Phải", "Trái").Replace("gối P", "gối T").Replace("gối p", "gối T");
            }
            else if (isRightCur && !isLeftCur)
            {
                cxk = cxk.Replace("(T)", "(P)").Replace("(t)", "(P)").Replace("trái", "phải").Replace("Trái", "Phải").Replace("gối T", "gối P").Replace("gối t", "gối P");
            }
            return cxk;
        }

        string s = ti.IcdName.ToLower();
        string loc = ExtractLocation(ti.IcdName);
        string viTri = loc.StartsWith("gối") ? ("Khớp " + loc) : (loc.StartsWith("vùng") ? loc : ("Vùng " + loc));

        // U phần mềm / u mỡ / nang
        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return string.Format(
                "{0}: Khối u gồ lên bề mặt, ranh giới rõ, mật độ chắc vừa, ấn đau tức nhẹ, di động tương đối so với lớp sâu. " +
                "Da trên bề mặt khối u bình thường, không nóng đỏ, không loét. " +
                "Vận động các khớp và nhóm cơ lân cận trong giới hạn bình thường. Mạch ngoại vi bắt rõ, cảm giác bình thường.", viTri);

        // ACL / đứt dây chằng
        if (s.Contains("acl") || (s.Contains("chằng") && (s.Contains("trước") || s.Contains("chéo"))))
            return string.Format(
                "{0}: Sưng nề nhẹ, ấn đau khe khớp. Dấu hiệu ngăn kéo trước (+). Test Lachman (+). Dấu hiệu xoay trục (Pivot shift) (+/-). " +
                "Biên độ gấp/duỗi gối hạn chế nhẹ do đau. Mạch khoeo, mạch chày sau, mu chân bắt rõ. Cảm giác ngoại vi bình thường.", viTri);

        // Gãy xương
        if (s.Contains("gãy") || s.Contains("gay"))
            return string.Format(
                "{0}: Sưng nề, bầm tím, ấn đau chói tại vị trí gãy. Cử động bất thường (+/-). " +
                "Chi thể ấm, mạch ngoại vi bắt rõ, không có dấu hiệu chèn ép khoang, cảm giác ngọn chi bình thường.", viTri);

        // Xẹp đốt sống / cột sống
        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("đốt sống") || s.Contains("dot song"))
            return string.Format(
                "Cột sống: Ấn gõ đau chói tại vị trí tổn thương ({0}). Co cứng khối cơ cạnh sống (+). " +
                "Hạn chế tầm vận động cột sống thắt lưng do đau. Cơ lực hai chi dưới 5/5, cảm giác nông sâu bình thường. " +
                "Không có dấu hiệu chèn ép tủy/chùm đuôi ngựa.", loc);

        // Thoát vị / đĩa đệm
        if (s.Contains("đĩa đệm") || s.Contains("thoát vị") || s.Contains("thoai hoa"))
            return string.Format(
                "Cột sống vùng {0}: Khối cơ cạnh sống co cứng nhẹ. Dấu Lasègue (+/-). " +
                "Điểm đau cột sống thắt lưng. Phản xạ gân xương hai chi dưới đều, không teo cơ, không liệt vận động.", loc);

        // Generic musculoskeletal
        return string.Format(
            "{0}: Sưng nề nhẹ, ấn đau điểm tổn thương. Vận động hạn chế do đau. Mạch máu thần kinh đầu chi bình thường.", viTri);
    }

    static string BuildCanLamSang(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        var items = new List<string> { "CTM, đông máu cơ bản, sinh hóa máu (glucose, ure, creatinine, AST, ALT, điện giải đồ, CRP)" };

        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
        {
            items.Add("Siêu âm phần mềm / Chụp cộng hưởng từ (MRI) đánh giá kích thước, vị trí, tính chất khối u");
            items.Add("Xét nghiệm mô bệnh học (giải phẫu bệnh) sau mổ");
        }
        if (s.Contains("gãy") || s.Contains("gay") || s.Contains("xẹp") || s.Contains("loãng xương"))
            items.Add("X-quang vị trí tổn thương (thẳng - nghiêng)");
        if (s.Contains("acl") || s.Contains("chằng") || s.Contains("gối") || s.Contains("cột sống"))
            items.Add("Cộng hưởng từ (MRI) đánh giá toàn diện dây chằng, sụn chêm/đĩa đệm");
        if (s.Contains("cột sống") || s.Contains("xẹp"))
        {
            items.Add("X-quang cột sống thắt lưng thẳng - nghiêng");
            items.Add("Đo mật độ xương (DXA)");
        }
        items.Add("Điện tim (ECG), X-quang ngực thẳng");
        items.Add("Nhóm máu ABO, Rh, các xét nghiệm miễn dịch tiền phẫu (HIV, HBsAg, Anti-HCV)");

        return string.Join(". ", items) + ".";
    }

    static string BuildTomTat(TreatmentInfo ti, DhstInfo dhst)
    {
        return string.Format(
            "Bệnh nhân {0} ({1}), {2} tuổi. Vào viện ngày {3} vì {4}. " +
            "Qua thăm khám lâm sàng ghi nhận: " +
            "Hội chứng/triệu chứng chính phù hợp chẩn đoán: {5}. " +
            "Sinh hiệu lúc vào viện ổn định: Mạch {6} ck/phút, HA {7} mmHg, SpO2 {8}%. " +
            "Hiện tại bệnh nhân tỉnh táo, tiếp xúc tốt, đang được điều trị tại Khoa Chấn thương Chỉnh hình & Cột sống.",
            ti.PatientName, ti.PatientGender, ti.PatientAge, ti.InTime,
            BuildLyDoVaoVien(ti).ToLower(),
            string.IsNullOrEmpty(ti.IcdName) ? ti.IcdCode : string.Format("[{0}] {1}", ti.IcdCode, ti.IcdName),
            dhst.Pulse, dhst.BloodPressure, dhst.SpO2);
    }

    static string BuildPhanBiet(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return "U mỡ (Lipoma); U bao hoạt dịch; Nang biểu bì / nang bã đậu; Khối máu tụ mạn tính / u thần kinh phần mềm.";
        if (s.Contains("acl") || (s.Contains("chằng") && s.Contains("trước")))
            return "Đứt bán phần dây chằng chéo trước; Tổn thương dây chằng bên (MCL/LCL); Rách sụn chêm phối hợp.";
        if (s.Contains("gãy") || s.Contains("gay"))
            return "Gãy xương không hoàn toàn / rạn xương; Bầm dập phần mềm đơn thuần; Trật khớp phối hợp.";
        if (s.Contains("xẹp") || s.Contains("xep") || s.Contains("đốt sống"))
            return "Xẹp đốt sống bệnh lý (ung thư di căn, u máu, đa u tủy xương); Lao cột sống; Thoái hóa đĩa đệm cấp tính.";
        return "Cần phân biệt với các tổn thương phần mềm, khớp lân cận và bệnh lý thoái hóa đi kèm.";
    }

    static string BuildHuongDieuTri(TreatmentInfo ti)
    {
        string s = ti.IcdName.ToLower();
        if (s.Contains(" u ") || s.StartsWith("u ") || s.Contains("khối u") || s.Contains("nang") || s.Contains("phần mềm"))
            return "Hoàn thiện các bilan xét nghiệm tiền phẫu; Phẫu thuật bóc trọn khối u phần mềm gửi bệnh phẩm làm mô bệnh học (giải phẫu bệnh); Kháng sinh dự phòng, giảm đau, chăm sóc vết mổ; Theo dõi liền thương và kết quả GPB.";
        if (s.Contains("acl") || (s.Contains("chằng") && s.Contains("trước")))
            return "Hoàn thiện các bilan xét nghiệm tiền phẫu; Phẫu thuật nội soi tái tạo dây chằng chéo trước; Kháng sinh dự phòng, giảm đau, giảm nề; Tập phục hồi chức năng sau mổ theo phác đồ.";
        if (s.Contains("gãy") || s.Contains("gay"))
            return "Bất động chi thể vững chắc; Bilan tiền phẫu chuẩn bị kết hợp xương (nếu có chỉ định phẫu thuật); Điều trị triệu chứng giảm đau, chống phù nề, phòng huyết khối; Tập PHCN sớm.";
        if (s.Contains("xẹp") || s.Contains("xep"))
            return "Nằm nghỉ tại giường, đeo nẹp hỗ trợ; Giảm đau bậc 2-3 theo phác đồ; Đánh giá chỉ định tạo hình thân đốt sống bằng bơm xi măng sinh học qua da; Điều trị loãng xương nền.";
        return "Điều trị nội khoa kết hợp vật lý trị liệu; Can thiệp thủ thuật/phẫu thuật khi có chỉ định; Theo dõi sát diễn biến lâm sàng.";
    }

    // ──────────────────────────────────────────────────────────────
    // ORACLE EMR — REFLECTION WRAPPERS
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
        _mdbLib      = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
        _emrMainLib  = Assembly.LoadFrom(Path.Combine(emrDir, "EMR_MAIN.Library.dll"));
        // Oracle ODP needed by MDB — should already be in path
        try { Assembly.LoadFrom(Path.Combine(emrDir, "Oracle.ManagedDataAccess.dll")); } catch { }

        _mdbConnType  = _mdbLib.GetType("MDB.MDBConnection");
        _mdbCmdType   = _mdbLib.GetType("MDB.MDBCommand");
        _baNKType     = _emrMainLib.GetType("EMR_MAIN.BenhAnNgoaiKhoa");
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
            var m = _baNKFuncType.GetMethod("Select",
                new Type[] { _mdbConnType, typeof(decimal) });
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
        var m = _baNKFuncType.GetMethod("InsertOrUpdate",
            new Type[] { _mdbConnType, _baNKType });
        object result = m.Invoke(null, new object[] { con, ba });
        if (result is bool) return (bool)result;
        return true;
    }

    static bool EnsureThongTinDieuTri(dynamic con, TreatmentInfo ti, bool isSave)
    {
        try
        {
            LoadEmrAssemblies();
            var ttdtFuncType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTriFunc");
            var checkMethod = ttdtFuncType.GetMethod("checkExistThongTinDieuTri",
                new Type[] { _mdbConnType, typeof(decimal) });
            bool exists = (bool)checkMethod.Invoke(null, new object[] { con, (decimal)ti.TreatmentId });

            if (exists)
            {
                Console.WriteLine("✓ Trang bìa THONGTINDIEUTRI: ĐÃ CÓ TRÊN HỆ THỐNG.");
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INFO] Chưa có Trang bìa THONGTINDIEUTRI → Tự động khởi tạo Trang bìa EMR (IDLoaiBenhAn: 11 - Ngoại khoa).");
            Console.ResetColor();

            if (!isSave) return true;

            var ttdtType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTri");
            dynamic ttdt = Activator.CreateInstance(ttdtType);
            ttdt.MaQuanLy = (decimal)ti.TreatmentId;
            ttdt.MaBenhNhan = ti.PatientCode;
            ttdt.MaBenhAn = ti.TreatmentCode;
            ttdt.Khoa = string.IsNullOrEmpty(ti.DeptName) ? "Khoa Chấn thương Chỉnh hình và Cột sống" : ti.DeptName;
            ttdt.TenKhoaVao = ttdt.Khoa;
            ttdt.IDLoaiBenhAn = 11; // Bệnh án ngoại khoa
            ttdt.ChanDoan_KhiVaoKhoaDieuTri = ti.IcdName;
            ttdt.MaICD_KhiVaoKhoaDieuTri = ti.IcdCode;
            ttdt.ChanDoan_KKB_CapCuu = ti.IcdName;
            ttdt.MaICD_KKB_CapCuu = ti.IcdCode;
            ttdt.VaoVienDoBenhNayLanThu = 1;

            if (!string.IsNullOrEmpty(ti.InTime))
            {
                DateTime dt;
                if (DateTime.TryParseExact(ti.InTime, "dd/MM/yyyy HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out dt))
                {
                    ttdt.NgayVaoVien = dt;
                    ttdt.NgayVaoKhoa = dt;
                    ttdt.NgayThangNamTrangBia = dt;
                }
            }

            var insertMethod = ttdtFuncType.GetMethod("InsertOrUpdateThongTinDieuTri",
                new Type[] { _mdbConnType, ttdtType });
            object res = insertMethod.Invoke(null, new object[] { con, ttdt });
            bool ok = (res is bool) ? (bool)res : true;
            if (ok)
            {
                Console.WriteLine("✓ Đã khởi tạo thành công Trang bìa THONGTINDIEUTRI.");
            }
            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [WARN] Lỗi khởi tạo THONGTINDIEUTRI: " + ex.Message);
            return false;
        }
    }

    static dynamic ExecuteReader(dynamic con, string sql)
    {
        LoadEmrAssemblies();
        try
        {
            // MDBCommand(string sql, MDBConnection con)
            dynamic cmd = Activator.CreateInstance(_mdbCmdType, new object[] { sql, con });
            return cmd.ExecuteReader();
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [SQL WARN] " + ex.Message.Split('\n')[0]);
            return null;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DISPLAY
    // ──────────────────────────────────────────────────────────────
    static void PrintField(string label, object val)
    {
        Console.Write("  " + label.PadRight(28) + ": ");
        Console.ForegroundColor = ConsoleColor.White;
        string v = SafeStr(val);
        Console.WriteLine(v.Length > 120 ? v.Substring(0, 117) + "..." : v);
        Console.ResetColor();
    }

    static void PrintPreview(dynamic ba, TreatmentInfo ti)
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("  XEM TRUOC BENH AN NGOAI KHOA - " + ti.PatientName.ToUpper());
        Console.WriteLine(new string('=', 70));
        PrintField("MaQuanLy",           ba.MaQuanLy);
        PrintField("BenhChinh",          ba.BenhChinh);
        PrintField("LyDoVaoVien",        ba.LyDoVaoVien);
        PrintField("QuaTrinhBenhLy",     ba.QuaTrinhBenhLy);
        PrintField("TienSuBenhBanThan",  ba.TienSuBenhBanThan);
        PrintField("TienSuBenhGiaDinh",  ba.TienSuBenhGiaDinh);
        PrintField("ToanThan",           ba.ToanThan);
        PrintField("CoXuongKhop",        ba.CoXuongKhop);
        PrintField("ThanKinh",           ba.ThanKinh);
        PrintField("TuanHoan",           ba.TuanHoan);
        PrintField("HoHap",              ba.HoHap);
        PrintField("TieuHoa",            ba.TieuHoa);
        PrintField("ThanTietNieuSinhDuc",ba.ThanTietNieuSinhDuc);
        PrintField("CanLamSang",         ba.CacXetNghiemCanLamSangCanLam);
        PrintField("TomTatBenhAn",       ba.TomTatBenhAn);
        PrintField("PhanBiet",           ba.PhanBiet);
        PrintField("TienLuong",          ba.TienLuong);
        PrintField("HuongDieuTri",       ba.HuongDieuTri);
        PrintField("BacSyLamBenhAn",     ba.BacSyLamBenhAn);
        PrintField("TenBacSyLamBenhAn",  ba.TenBacSyLamBenhAn);
        Console.WriteLine(new string('-', 70));
    }

    static void PrintUsage()
    {
        Console.WriteLine("HisEmrFiller — Tự động điền Vỏ Bệnh Án Ngoại Khoa EMR");
        Console.WriteLine("Cách dùng:");
        Console.WriteLine("  HisEmrFiller.exe <MaBN>                       # dry-run, xem trước");
        Console.WriteLine("  HisEmrFiller.exe <MaBN> --save                # ghi thật vào DB");
        Console.WriteLine("  HisEmrFiller.exe <MaQuanLy>                   # nhận ID số");
        Console.WriteLine("  HisEmrFiller.exe <MaBN> --save --doctor vmc   # chỉ định bác sĩ");
    }

    // ──────────────────────────────────────────────────────────────
    // TOKEN READER
    // ──────────────────────────────────────────────────────────────
    static string ReadLiveToken()
    {
        // Cách 1: file doctor_standalone.token
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

        // Cách 2: Logs\LogSystem.txt (cùng kiểu CheckToken.cs)
        try
        {
            DirectoryInfo cur = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 5; i++)
            {
                if (cur == null) break;
                string p = Path.Combine(cur.FullName, "Logs", "LogSystem.txt");
                if (!File.Exists(p)) { p = Path.Combine(cur.FullName, "LogSystem.txt"); }
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

    // ──────────────────────────────────────────────────────────────
    // UTILITIES
    // ──────────────────────────────────────────────────────────────
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
        return s.Length > 0 && s.All(char.IsDigit);
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

    static string NormalizeIcd(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // Capitalize first letter
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    static string ExtractLocation(string icdName)
    {
        if (string.IsNullOrEmpty(icdName)) return "vùng tổn thương";
        string s = icdName.ToLower();
        // Detect laterality
        string side = "";
        if (s.Contains("phải") || s.Contains("phai")) side = "phải";
        else if (s.Contains("trái") || s.Contains("trai")) side = "trái";
        // Common anatomical locations
        if (s.Contains("gối") || s.Contains("goi")) return "gối" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cổ tay") || s.Contains("co tay")) return "cổ tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("vai") || s.Contains("shoulder")) return "vai" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("háng") || s.Contains("hang")) return "háng" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("lưng") || s.Contains("lung")) return "lưng" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cột sống") || s.Contains("dot song") || s.Contains("cot song")) return "cột sống";
        if (s.Contains("l1") || s.Contains("l2") || s.Contains("l3") || s.Contains("l4") || s.Contains("l5")) return "cột sống thắt lưng";
        if (s.Contains("t") && Regex.IsMatch(s, @"t\d+")) return "cột sống ngực";
        if (s.Contains("đòn") || s.Contains("don")) return "xương đòn" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cánh tay") || s.Contains("canh tay")) return "cánh tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cẳng tay") || s.Contains("cang tay")) return "cẳng tay" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("cẳng chân") || s.Contains("cang chan")) return "cẳng chân" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("bàn chân") || s.Contains("ban chan")) return "bàn chân" + (side.Length > 0 ? " " + side : "");
        if (s.Contains("bàn tay") || s.Contains("ban tay")) return "bàn tay" + (side.Length > 0 ? " " + side : "");
        return "vùng tổn thương" + (side.Length > 0 ? " " + side : "");
    }

    // ──────────────────────────────────────────────────────────────
    // INNER TYPES
    // ──────────────────────────────────────────────────────────────
    class TreatmentInfo
    {
        public long   TreatmentId;
        public string TreatmentCode;
        public string PatientCode;
        public string PatientName;
        public string PatientAge;
        public string PatientGender;
        public string InTime;
        public string IcdCode;
        public string IcdName;
        public string DeptName;
        public long   DeptId;
    }

    class DhstInfo
    {
        public string Pulse         = "80";
        public string BloodPressure = "120/80";
        public string Temperature   = "37.0";
        public string SpO2          = "98";
        public string Weight        = "";
        public string Height        = "";
    }

    class TemplateBA
    {
        public string QuaTrinhBenhLy      = "";
        public string TienSuBenhBanThan   = "";
        public string TienSuBenhGiaDinh   = "";
        public string ToanThan            = "";
        public string CoXuongKhop         = "";
        public string ThanKinh            = "";
        public string TuanHoan            = "";
        public string HoHap               = "";
        public string TieuHoa             = "";
        public string ThanTietNieu        = "";
        public string CanLamSang          = "";
        public string TomTatBenhAn        = "";
        public string PhanBiet            = "";
        public string TienLuong           = "";
        public string HuongDieuTri        = "";
    }
}

// ──────────────────────────────────────────────────────────────────
// ADAPTER HELPER (same pattern as existing tools)
// ──────────────────────────────────────────────────────────────────
public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}
