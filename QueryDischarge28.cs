using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Globalization;
using Newtonsoft.Json;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using EMR.Filter;
using EMR.EFMODEL.DataModels;

namespace CheckDischarge
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string name = new AssemblyName(e.Name).Name + ".dll";
                string[] dirs = new string[] { baseDir, Path.Combine(baseDir, "ReferencedAssemblies"), Path.Combine(baseDir, "Integrate", "EMR") };
                foreach (var d in dirs)
                {
                    string p = Path.Combine(d, name);
                    if (File.Exists(p))
                    {
                        try { return Assembly.LoadFrom(p); } catch { }
                    }
                }
                return null;
            };

            RealMain(args);
        }

        const string MOS_BASE = "http://192.168.7.236:1608/";
        const string EMR_BASE = "http://192.168.7.239:1415/";
        const string EMR_CONNSTR = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

        static Assembly _emrMainLib;
        static Assembly _mdbLib;
        static Type _mdbConnType;
        static Type _baNKFuncType;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RealMain(string[] args)
        {
            string tokenCode = ReadLiveToken();
            Console.WriteLine("🔑 Live Token: " + (tokenCode != null ? tokenCode.Substring(0, 10) + "..." : "NULL"));

            var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
            var emrConsumer = new ApiConsumer(EMR_BASE, tokenCode, "HIS");
            var adapter = new MyAdapter();
            var param = new CommonParam();

            // Load Oracle assemblies
            InitEmrAssemblies();
            dynamic con = null;
            try
            {
                con = Activator.CreateInstance(_mdbConnType, new object[] { EMR_CONNSTR });
                con.Open();
                Console.WriteLine("✓ Đã kết nối Oracle EMR Database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Không kết nối được Oracle EMR: " + ex.Message);
            }

            long targetFrom = 20260928000000L;
            long targetTo   = 20260928235959L;

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("🔍 TÌM KIẾM TẤT CẢ HỒ SƠ RA VIỆN / CHUYỂN VIỆN NGÀY 28/09/2026");
            Console.WriteLine("===============================================================================");

            // 1. Quét HisTreatment có OUT_TIME ngày 28/09/2026
            var tf = new HisTreatmentViewFilter
            {
                OUT_TIME_FROM = targetFrom,
                OUT_TIME_TO = targetTo
            };
            var outTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param) ?? new List<V_HIS_TREATMENT>();
            Console.WriteLine(string.Format("• Tìm thấy {0} hồ sơ có OUT_TIME trong ngày 28/09/2026 toàn viện.", outTreatments.Count));

            // Lọc các hồ sơ liên quan đến Khoa 57, Khoa 915 hoặc BS 034727
            var candidates = outTreatments.Where(tr => 
                tr.END_DEPARTMENT_ID == 57 || 
                tr.LAST_DEPARTMENT_ID == 57 || 
                tr.END_DEPARTMENT_ID == 915 || 
                tr.LAST_DEPARTMENT_ID == 915 ||
                tr.END_LOGINNAME == "034727" || 
                tr.DOCTOR_LOGINNAME == "034727" || 
                tr.MODIFIER == "034727" || 
                tr.CREATOR == "034727" ||
                tr.END_LOGINNAME == "vmc" || 
                tr.DOCTOR_LOGINNAME == "vmc"
            ).ToList();

            Console.WriteLine(string.Format("• Tìm thấy {0} hồ sơ liên quan Khoa 57 / Khoa 915 / BS 034727 / vmc.", candidates.Count));

            // Thêm kiểm tra Khoa 57 DepartmentTran nếu có bệnh nhân chuyển khoa/ra viện
            var dtf = new HisDepartmentTranViewFilter
            {
                DEPARTMENT_ID = 57
            };
            // Thử lấy thêm nếu cần, nhưng outTreatments đã bao gồm tất cả ca có OUT_TIME ngày 28/09

            Console.WriteLine(string.Format("\n📊 BẮT ĐẦU ĐỐI SOÁT CHI TIẾT TỪNG BỆNH NHÂN ({0} CA):\n", candidates.Count));

            int count = 0;
            var summaryMissing = new List<string>();

            foreach (var tr in candidates.OrderBy(x => x.OUT_TIME))
            {
                count++;
                decimal maQuanLy = 0;
                decimal.TryParse(tr.TREATMENT_CODE, out maQuanLy);

                // Kiểm tra xem BS 034727 có tờ điều trị trong hồ sơ này không
                var pTrkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
                var pTrackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, pTrkFilter, param) ?? new List<V_HIS_TRACKING>();
                
                var myTrackings = pTrackings.Where(t => t.CREATOR == "034727" || t.MODIFIER == "034727").ToList();
                var summaryTrackings = pTrackings.Where(t => 
                    (t.CONTENT != null && (
                        t.CONTENT.ToLower().Contains("sơ kết") || 
                        t.CONTENT.ToLower().Contains("tổng kết") ||
                        t.CONTENT.ToLower().Contains("ra viện") ||
                        t.CONTENT.ToLower().Contains("chuyển viện") ||
                        t.CONTENT.ToLower().Contains("chuyển tuyến")
                    ))
                ).ToList();

                bool doctorInvolved = (tr.END_LOGINNAME == "034727" || tr.DOCTOR_LOGINNAME == "034727" || tr.MODIFIER == "034727" || myTrackings.Count > 0);

                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("#{0} | BN: {1} ({2}) | Giới: {3}",
                    count, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_GENDER_NAME));
                Console.WriteLine(string.Format("   Mã ĐT: {0} | TreatmentId: {1} | MaQuanLy: {2}", tr.TREATMENT_CODE, tr.ID, maQuanLy));
                Console.WriteLine(string.Format("   Diện ĐT (Type): {0} | Vào viện : {1} | Ra viện (OUT_TIME): {2}", tr.TDL_TREATMENT_TYPE_ID, tr.IN_TIME, tr.OUT_TIME));
                Console.WriteLine(string.Format("   Khoa kết thúc: [{0}] {1} | Buồng/Phòng: [{2}] {3}",
                    tr.END_DEPARTMENT_ID, tr.END_DEPARTMENT_NAME, tr.END_ROOM_ID, tr.END_ROOM_NAME));
                Console.WriteLine(string.Format("   Hình thức kết thúc: [{0}] {1} | Kết quả: [{2}] {3}",
                    tr.TREATMENT_END_TYPE_ID, tr.TREATMENT_END_TYPE_NAME, tr.TREATMENT_RESULT_ID, tr.TREATMENT_RESULT_NAME));
                Console.WriteLine(string.Format("   Người làm thủ tục ra viện (END_LOGINNAME): {0} ({1})", tr.END_LOGINNAME, tr.END_USERNAME));
                Console.WriteLine(string.Format("   Bác sĩ điều trị (DOCTOR_LOGINNAME): {0} ({1})", tr.DOCTOR_LOGINNAME, tr.DOCTOR_USERNAME));
                Console.WriteLine(string.Format("   Người sửa cuối (MODIFIER): {0} | Người tạo: {1}", tr.MODIFIER, tr.CREATOR));
                Console.WriteLine(string.Format("   Số tờ điều trị BS 034727 tạo/sửa: {0} tờ", myTrackings.Count));
                Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT));

                Console.ForegroundColor = doctorInvolved ? ConsoleColor.Green : ConsoleColor.DarkGray;
                Console.WriteLine(string.Format("   👉 BS 034727 CÓ THỰC HIỆN THỦ TỤC / ĐIỀU TRỊ: {0}", doctorInvolved ? "CÓ" : "KHÔNG (DO BÁC SĨ KHÁC)"));
                Console.ResetColor();

                // 1. KIỂM TRA TRANG BÌA THONGTINDIEUTRI
                bool hasTTDT = false;
                if (con != null)
                {
                    try
                    {
                        var ttdtFuncType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTriFunc");
                        var checkMethod = ttdtFuncType.GetMethod("checkExistThongTinDieuTri", new Type[] { _mdbConnType, typeof(decimal) });
                        hasTTDT = (bool)checkMethod.Invoke(null, new object[] { con, maQuanLy });
                        if (!hasTTDT) hasTTDT = (bool)checkMethod.Invoke(null, new object[] { con, (decimal)tr.ID });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("      Lỗi check TTDT: " + ex.Message);
                    }
                }

                // 2. KIỂM TRA VỎ BỆNH ÁN NGOẠI KHOA (BENHANNGOAIKHOA)
                bool hasBANK = false;
                string bsLamBA = "";
                string tomtatBA = "";
                string qtbl = "";
                string coxuongkhop = "";
                string quaTrinhBenhLyVaDienBien = "";
                string tomTatKetQuaXetNghiem = "";
                string tinhTrangRaVien = "";

                if (con != null)
                {
                    try
                    {
                        dynamic ba = BenhAnNgoaiKhoaSelect(con, maQuanLy);
                        if (ba == null || ba.MaQuanLy == 0) ba = BenhAnNgoaiKhoaSelect(con, (decimal)tr.ID);
                        if (ba != null && ba.MaQuanLy > 0)
                        {
                            hasBANK = true;
                            bsLamBA = Convert.ToString(ba.BacSyLamBenhAn);
                            tomtatBA = Convert.ToString(ba.TomTatBenhAn);
                            qtbl = Convert.ToString(ba.QuaTrinhBenhLy);
                            coxuongkhop = Convert.ToString(ba.CoXuongKhop);
                            quaTrinhBenhLyVaDienBien = Convert.ToString(ba.QuaTrinhBenhLyVaDienBien);
                            tomTatKetQuaXetNghiem = Convert.ToString(ba.TomTatKetQuaXetNghiem);
                            tinhTrangRaVien = Convert.ToString(ba.TinhTrangNguoiBenhRaVien);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("      Lỗi check BANK: " + ex.Message);
                    }
                }

                // 3. KIỂM TRA VĂN BẢN EMR (TỜ TÓM TẮT BỆNH ÁN, GIẤY RA VIỆN...)
                var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
                var emrDocs = adapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param) ?? new List<EMR_DOCUMENT>();

                var summaryDocs = emrDocs.Where(d => 
                    (d.DOCUMENT_NAME != null && (
                        d.DOCUMENT_NAME.ToLower().Contains("tóm tắt") || 
                        d.DOCUMENT_NAME.ToLower().Contains("tom tat") ||
                        d.DOCUMENT_NAME.ToLower().Contains("ra viện") ||
                        d.DOCUMENT_NAME.ToLower().Contains("chuyển tuyến") ||
                        d.DOCUMENT_NAME.ToLower().Contains("chuyển viện")
                    )) ||
                    d.DOCUMENT_TYPE_ID == 2 ||
                    d.DOCUMENT_TYPE_ID == 16
                ).ToList();

                bool missingVo = !hasBANK;
                bool missingTomTat = string.IsNullOrWhiteSpace(tomtatBA);
                bool missingTongKet = string.IsNullOrWhiteSpace(quaTrinhBenhLyVaDienBien) && string.IsNullOrWhiteSpace(tinhTrangRaVien);

                Console.WriteLine("   📋 ĐÁNH GIÁ CHI TIẾT TRẠNG THÁI HỒ SƠ:");
                Console.WriteLine(string.Format("      1. Trang bìa THONGTINDIEUTRI       : {0}", hasTTDT ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ"));
                Console.WriteLine(string.Format("      2. Vỏ Bệnh Án Ngoại Khoa (BANK)    : {0} {1}", 
                    hasBANK ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ", 
                    hasBANK ? ("(BS: " + bsLamBA + " | QTBL: " + (qtbl.Length > 0 ? "OK" : "Trống") + " | CXK: " + (coxuongkhop.Length > 0 ? "OK" : "Trống") + ")") : ""));
                Console.WriteLine(string.Format("      3. Tóm tắt bệnh án (trong vỏ EMR) : {0} (Độ dài: {1} ký tự)", 
                    !string.IsNullOrWhiteSpace(tomtatBA) ? "✅ ĐÃ CÓ" : "❌ THIẾU / TRỐNG", tomtatBA.Length));
                Console.WriteLine(string.Format("      4. Bìa tổng kết ra viện (trong vỏ): {0} (Diễn biến: {1}, XN: {2}, TT ra viện: {3})",
                    (!string.IsNullOrWhiteSpace(quaTrinhBenhLyVaDienBien) || !string.IsNullOrWhiteSpace(tinhTrangRaVien)) ? "✅ ĐÃ CÓ" : "❌ THIẾU / TRỐNG",
                    quaTrinhBenhLyVaDienBien.Length > 0 ? "OK" : "Trống",
                    tomTatKetQuaXetNghiem.Length > 0 ? "OK" : "Trống",
                    tinhTrangRaVien.Length > 0 ? "OK" : "Trống"));
                Console.WriteLine(string.Format("      5. Tờ điều trị Tổng kết / Ra viện  : {0} ({1} tờ)", 
                    summaryTrackings.Count > 0 ? "✅ ĐÃ CÓ" : "❌ THIẾU", summaryTrackings.Count));
                Console.WriteLine(string.Format("      6. Văn bản Tóm tắt EMR (Loại 2/16): {0} ({1} văn bản)", 
                    summaryDocs.Count > 0 ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ TRÊN EMR", summaryDocs.Count));
                if (summaryDocs.Count > 0)
                {
                    foreach (var sd in summaryDocs)
                    {
                        Console.WriteLine(string.Format("         + [Loại {0}] {1} (Mã: {2})", sd.DOCUMENT_TYPE_ID, sd.DOCUMENT_NAME, sd.DOCUMENT_CODE));
                    }
                }

                if (doctorInvolved)
                {
                    if (missingVo || missingTomTat || missingTongKet)
                    {
                        summaryMissing.Add(string.Format("BN {0} ({1}) - Mã ĐT: {2} | Thiếu: {3}{4}{5}",
                            tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE,
                            missingVo ? "[VỎ BỆNH ÁN] " : "",
                            missingTomTat ? "[TỜ TÓM TẮT/TÓM TẮT BA] " : "",
                            missingTongKet ? "[BÌA TỔNG KẾT RA VIỆN] " : ""));
                    }
                }
            }

            if (con != null)
            {
                try { con.Close(); } catch { }
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("🏁 TỔNG HỢP CÁC BỆNH NHÂN DO BÁC SĨ LÀM THỦ TỤC CÒN THIẾU:");
            Console.WriteLine("===============================================================================");
            if (summaryMissing.Count == 0)
            {
                Console.WriteLine("🎉 Tuyệt vời! Tất cả bệnh nhân ra viện/chuyển viện ngày 28/09 do Bác sĩ làm thủ tục ĐÃ ĐẦY ĐỦ vỏ bệnh án và tóm tắt!");
            }
            else
            {
                foreach (var s in summaryMissing)
                {
                    Console.WriteLine("⚠️ " + s);
                }
            }
            Console.WriteLine("===============================================================================\n");
        }

        static void InitEmrAssemblies()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string emrDir = Path.Combine(baseDir, "Integrate", "EMR");
                _mdbLib = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
                _emrMainLib = Assembly.LoadFrom(Path.Combine(emrDir, "EMR_MAIN.Library.dll"));
                try { Assembly.LoadFrom(Path.Combine(emrDir, "EMR_MAIN.dll")); } catch { }
                try { Assembly.LoadFrom(Path.Combine(emrDir, "Oracle.ManagedDataAccess.dll")); } catch { }

                _mdbConnType = _mdbLib.GetType("MDB.MDBConnection");
                _baNKFuncType = _emrMainLib.GetType("EMR_MAIN.BenhAnNgoaiKhoaFunc");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi init EMR assemblies: " + ex.Message);
            }
        }

        static dynamic BenhAnNgoaiKhoaSelect(dynamic con, decimal maQuanLy)
        {
            try
            {
                var m = _baNKFuncType.GetMethod("Select", new Type[] { _mdbConnType, typeof(decimal) });
                return m.Invoke(null, new object[] { con, maQuanLy });
            }
            catch { return null; }
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
    }
}
