using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using EMR.Filter;
using EMR.EFMODEL.DataModels;
using MDB;
using EMR_MAIN;

namespace Audit28
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

            AuditRunner.Run();
        }
    }

    class AuditRunner
    {
        const string MOS_BASE = "http://192.168.7.236:1608/";
        const string EMR_BASE = "http://192.168.7.239:1415/";
        const string EMR_CONNSTR = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

        public static void Run()
        {
            string tokenCode = File.ReadAllText("doctor_standalone.token").Split('|')[0];
            var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
            var emrConsumer = new ApiConsumer(EMR_BASE, tokenCode, "HIS");
            var adapter = new MyAdapter();
            var param = new CommonParam();

            MDBConnection con = null;
            try
            {
                con = new MDBConnection(EMR_CONNSTR);
                con.Open();
                Console.WriteLine("✓ Đã kết nối Oracle EMR Database trực tiếp (x86).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi kết nối Oracle: " + ex.Message);
                return;
            }

            long targetFrom = 20260928000000L;
            long targetTo   = 20260928235959L;

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("🏥 QUÉT VÀ ĐỐI SOÁT HỒ SƠ RA VIỆN / CHUYỂN VIỆN NGÀY 28/09/2026");
            Console.WriteLine("===============================================================================");

            var tf = new HisTreatmentViewFilter
            {
                OUT_TIME_FROM = targetFrom,
                OUT_TIME_TO = targetTo
            };
            var outTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param) ?? new List<V_HIS_TREATMENT>();
            Console.WriteLine(string.Format("• Tìm thấy {0} hồ sơ có OUT_TIME trong ngày 28/09/2026 toàn viện.", outTreatments.Count));

            // Lọc các ca liên quan:
            // 1. Khoa 57 hoặc Khoa 915
            // 2. Hoặc người làm thủ tục ra viện / chuyển viện (END_LOGINNAME) = 034727
            // 3. Hoặc bác sĩ điều trị (DOCTOR_LOGINNAME) = 034727
            // 4. Hoặc MODIFIER = 034727
            var candidates = outTreatments.Where(tr => 
                tr.END_DEPARTMENT_ID == 57 || tr.LAST_DEPARTMENT_ID == 57 ||
                tr.END_DEPARTMENT_ID == 915 || tr.LAST_DEPARTMENT_ID == 915 ||
                tr.END_LOGINNAME == "034727" || tr.DOCTOR_LOGINNAME == "034727" ||
                tr.MODIFIER == "034727" || tr.CREATOR == "034727"
            ).ToList();

            Console.WriteLine(string.Format("• Tìm thấy {0} hồ sơ liên quan Khoa 57 / 915 / BS 034727.\n", candidates.Count));

            var listFull = new List<PatientAuditResult>();

            int idx = 0;
            foreach (var tr in candidates.OrderBy(x => x.OUT_TIME))
            {
                idx++;
                decimal maQuanLy = 0;
                decimal.TryParse(tr.TREATMENT_CODE, out maQuanLy);

                // Kiểm tra xem BS 034727 có tạo tờ điều trị trong hồ sơ này không
                var pTrkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
                var pTrackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, pTrkFilter, param) ?? new List<V_HIS_TRACKING>();
                var myTrackings = pTrackings.Where(t => t.CREATOR == "034727" || t.MODIFIER == "034727").ToList();

                bool isMyEnd = (tr.END_LOGINNAME == "034727");
                bool isMyDoc = (tr.DOCTOR_LOGINNAME == "034727");
                bool isMyMod = (tr.MODIFIER == "034727");
                bool hasMyTrk = (myTrackings.Count > 0);

                // Chỉ xét các ca mà Bác sĩ "có làm thủ tục" hoặc "trực tiếp điều trị/ký"
                if (!isMyEnd && !isMyDoc && !isMyMod && !hasMyTrk)
                    continue;

                var res = new PatientAuditResult
                {
                    Index = idx,
                    PatientName = tr.TDL_PATIENT_NAME,
                    PatientCode = tr.TDL_PATIENT_CODE,
                    TreatmentCode = tr.TREATMENT_CODE,
                    TreatmentId = tr.ID,
                    MaQuanLy = maQuanLy,
                    TreatmentType = tr.TDL_TREATMENT_TYPE_ID,
                    InTime = tr.IN_TIME.ToString(),
                    OutTime = tr.OUT_TIME.ToString(),
                    EndDeptName = tr.END_DEPARTMENT_NAME,
                    EndDeptId = tr.END_DEPARTMENT_ID,
                    EndTypeName = tr.TREATMENT_END_TYPE_NAME,
                    EndTypeId = tr.TREATMENT_END_TYPE_ID,
                    EndLoginName = tr.END_LOGINNAME,
                    EndUserName = tr.END_USERNAME,
                    DoctorLoginName = tr.DOCTOR_LOGINNAME,
                    DoctorUserName = tr.DOCTOR_USERNAME,
                    Modifier = tr.MODIFIER,
                    MyTrackingCount = myTrackings.Count,
                    TotalTrackingCount = pTrackings.Count,
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME
                };

                // 1. Kiểm tra Trang bìa THONGTINDIEUTRI
                res.HasTTDT = ThongTinDieuTriFunc.checkExistThongTinDieuTri(con, maQuanLy);
                if (!res.HasTTDT) res.HasTTDT = ThongTinDieuTriFunc.checkExistThongTinDieuTri(con, (decimal)tr.ID);

                // 2. Kiểm tra Vỏ bệnh án ngoại khoa BENHANNGOAIKHOA
                dynamic ba = BenhAnNgoaiKhoaFunc.Select(con, maQuanLy);
                if (ba == null || ba.MaQuanLy == 0) ba = BenhAnNgoaiKhoaFunc.Select(con, (decimal)tr.ID);

                if (ba != null && ba.MaQuanLy > 0)
                {
                    res.HasVoBenhAn = true;
                    res.DoctorLamBA = Convert.ToString(ba.BacSyLamBenhAn);
                    res.TomTatBenhAn = Convert.ToString(ba.TomTatBenhAn);
                    res.QuaTrinhBenhLy = Convert.ToString(ba.QuaTrinhBenhLy);
                    res.QuaTrinhBenhLyVaDienBien = Convert.ToString(ba.QuaTrinhBenhLyVaDienBien);
                    res.TomTatKetQuaXetNghiem = Convert.ToString(ba.TomTatKetQuaXetNghiem);
                    res.PhuongPhapDieuTri = Convert.ToString(ba.PhuongPhapDieuTri);
                    res.TinhTrangRaVien = Convert.ToString(ba.TinhTrangNguoiBenhRaVien);
                    res.HuongDieuTriTiepTheo = Convert.ToString(ba.HuongDieuTriVaCacCheDoTiepTheo);
                    res.LoiDanBacSi = Convert.ToString(ba.LoiDanBacSi);
                }

                // 3. Kiểm tra EMR Documents
                var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
                var emrDocs = adapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param) ?? new List<EMR_DOCUMENT>();
                res.EmrDocCount = emrDocs.Count;

                foreach (var d in emrDocs)
                {
                    string name = d.DOCUMENT_NAME ?? "";
                    if (name.IndexOf("tóm tắt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("tom tat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        d.DOCUMENT_TYPE_ID == 16 || d.DOCUMENT_TYPE_ID == 115)
                    {
                        res.EmrSummaryDocs.Add(string.Format("[Loại {0}] {1} ({2})", d.DOCUMENT_TYPE_ID, d.DOCUMENT_NAME, d.DOCUMENT_CODE));
                    }
                    if (name.IndexOf("ra viện", StringComparison.OrdinalIgnoreCase) >= 0 || d.DOCUMENT_TYPE_ID == 25)
                    {
                        res.EmrDischargeDocs.Add(string.Format("[Loại {0}] {1} ({2})", d.DOCUMENT_TYPE_ID, d.DOCUMENT_NAME, d.DOCUMENT_CODE));
                    }
                    if (name.IndexOf("chuyển", StringComparison.OrdinalIgnoreCase) >= 0 || d.DOCUMENT_TYPE_ID == 26)
                    {
                        res.EmrTransferDocs.Add(string.Format("[Loại {0}] {1} ({2})", d.DOCUMENT_TYPE_ID, d.DOCUMENT_NAME, d.DOCUMENT_CODE));
                    }
                }

                listFull.Add(res);
            }

            con.Close();

            // IN KẾT QUẢ ĐỐI SOÁT
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("📋 DANH SÁCH CHI TIẾT BỆNH NHÂN DO BÁC SĨ 034727 CÓ LIÊN QUAN / LÀM THỦ TỤC ({0} BN)", listFull.Count));
            Console.WriteLine("===============================================================================\n");

            int n = 0;
            foreach (var r in listFull)
            {
                n++;
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("#{0} | BN: {1} | Mã BN: {2} | Mã ĐT: {3}", n, r.PatientName, r.PatientCode, r.TreatmentCode));
                Console.WriteLine(string.Format("    Diện điều trị   : {0} ({1}) | Ra viện (OUT_TIME): {2}",
                    r.TreatmentType,
                    r.TreatmentType == 3 ? "NỘI TRÚ" : (r.TreatmentType == 1 ? "NGOẠI TRÚ/KHÁM" : "KHÁC"),
                    r.OutTime));
                Console.WriteLine(string.Format("    Khoa kết thúc   : [{0}] {1}", r.EndDeptId, r.EndDeptName));
                Console.WriteLine(string.Format("    Hình thức kết thúc: [{0}] {1}", r.EndTypeId, r.EndTypeName));
                Console.WriteLine(string.Format("    Người làm thủ tục ra viện : {0} ({1})", r.EndLoginName, r.EndUserName));
                Console.WriteLine(string.Format("    Bác sĩ điều trị           : {0} ({1})", r.DoctorLoginName, r.DoctorUserName));
                Console.WriteLine(string.Format("    Người sửa cuối hồ sơ      : {0}", r.Modifier));
                Console.WriteLine(string.Format("    Số tờ điều trị BS tạo/sửa: {0} / tổng {1} tờ", r.MyTrackingCount, r.TotalTrackingCount));
                Console.WriteLine(string.Format("    Chẩn đoán       : [{0}] {1}", r.IcdCode, r.IcdName));
                
                Console.WriteLine("    [TÌNH TRẠNG VỎ & TÓM TẮT]:");
                Console.WriteLine(string.Format("      • Trang bìa THONGTINDIEUTRI       : {0}", r.HasTTDT ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ"));
                Console.WriteLine(string.Format("      • Vỏ Bệnh Án Ngoại Khoa (BANK)    : {0} {1}",
                    r.HasVoBenhAn ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ",
                    r.HasVoBenhAn ? ("(BS làm: " + r.DoctorLamBA + ")") : ""));
                Console.WriteLine(string.Format("      • Tóm tắt bệnh án (TomTatBenhAn)  : {0} {1}",
                    !string.IsNullOrWhiteSpace(r.TomTatBenhAn) ? "✅ ĐÃ CÓ" : "❌ THIẾU / TRỐNG",
                    !string.IsNullOrWhiteSpace(r.TomTatBenhAn) ? ("(" + r.TomTatBenhAn.Length + " ký tự)") : ""));
                Console.WriteLine(string.Format("      • Bìa tổng kết ra viện            : {0}",
                    (!string.IsNullOrWhiteSpace(r.QuaTrinhBenhLyVaDienBien) || !string.IsNullOrWhiteSpace(r.TinhTrangRaVien)) ? "✅ ĐÃ CÓ" : "❌ THIẾU / TRỐNG"));
                Console.WriteLine(string.Format("      • Văn bản Tóm tắt hồ sơ BA EMR    : {0} ({1} VB)",
                    r.EmrSummaryDocs.Count > 0 ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ", r.EmrSummaryDocs.Count));
                foreach (var s in r.EmrSummaryDocs) Console.WriteLine("          + " + s);
                Console.WriteLine(string.Format("      • Giấy ra viện EMR                : {0}", r.EmrDischargeDocs.Count > 0 ? "✅ ĐÃ CÓ" : "❌ CHƯA CÓ"));
                foreach (var s in r.EmrDischargeDocs) Console.WriteLine("          + " + s);
                Console.WriteLine(string.Format("      • Giấy chuyển tuyến EMR           : {0}", r.EmrTransferDocs.Count > 0 ? "✅ ĐÃ CÓ" : "Không có (Ra viện thường)"));
                foreach (var s in r.EmrTransferDocs) Console.WriteLine("          + " + s);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("🏁 TỔNG KẾT PHÂN LOẠI THEO YÊU CẦU CỦA BÁC SĨ:");
            Console.WriteLine("===============================================================================");
            
            // Lọc các ca NỘI TRÚ do BS 034727 thực sự làm thủ tục ra viện/chuyển viện hoặc điều trị chính
            var inPatients = listFull.Where(x => x.TreatmentType == 3).ToList();
            Console.WriteLine(string.Format("• Bệnh nhân NỘI TRÚ liên quan BS 034727: {0} bệnh nhân", inPatients.Count));

            var missingAny = inPatients.Where(x => !x.HasVoBenhAn || string.IsNullOrWhiteSpace(x.TomTatBenhAn) || (string.IsNullOrWhiteSpace(x.QuaTrinhBenhLyVaDienBien) && string.IsNullOrWhiteSpace(x.TinhTrangRaVien))).ToList();
            
            if (missingAny.Count == 0)
            {
                Console.WriteLine("🎉 TẤT CẢ các ca bệnh nhân nội trú ra viện/chuyển viện ngày 28/9 có BS 034727 tham gia ĐÃ ĐẦY ĐỦ VỎ BỆNH ÁN VÀ TỜ TÓM TẮT!");
            }
            else
            {
                Console.WriteLine(string.Format("⚠️ CÓ {0} BỆNH NHÂN CÒN THIẾU THÔNG TIN:", missingAny.Count));
                foreach (var m in missingAny)
                {
                    Console.WriteLine(string.Format("  - BN: {0} ({1}) | Mã ĐT: {2} | Thủ tục: {3} (Bởi: {4})",
                        m.PatientName, m.PatientCode, m.TreatmentCode, m.EndTypeName, m.EndLoginName));
                    Console.WriteLine(string.Format("    + Vỏ bệnh án: {0} | Tóm tắt: {1} | Bìa tổng kết: {2} | VB Tóm tắt EMR: {3}",
                        m.HasVoBenhAn ? "ĐÃ CÓ" : "❌ THIẾU",
                        !string.IsNullOrWhiteSpace(m.TomTatBenhAn) ? "ĐÃ CÓ" : "❌ THIẾU",
                        (!string.IsNullOrWhiteSpace(m.QuaTrinhBenhLyVaDienBien) || !string.IsNullOrWhiteSpace(m.TinhTrangRaVien)) ? "ĐÃ CÓ" : "❌ THIẾU",
                        m.EmrSummaryDocs.Count > 0 ? "ĐÃ CÓ" : "❌ THIẾU"));
                }
            }
            Console.WriteLine("===============================================================================\n");
        }
    }

    class PatientAuditResult
    {
        public int Index;
        public string PatientName;
        public string PatientCode;
        public string TreatmentCode;
        public long TreatmentId;
        public decimal MaQuanLy;
        public long? TreatmentType;
        public string InTime;
        public string OutTime;
        public string EndDeptName;
        public long? EndDeptId;
        public string EndTypeName;
        public long? EndTypeId;
        public string EndLoginName;
        public string EndUserName;
        public string DoctorLoginName;
        public string DoctorUserName;
        public string Modifier;
        public int MyTrackingCount;
        public int TotalTrackingCount;
        public string IcdCode;
        public string IcdName;

        public bool HasTTDT;
        public bool HasVoBenhAn;
        public string DoctorLamBA = "";
        public string TomTatBenhAn = "";
        public string QuaTrinhBenhLy = "";
        public string QuaTrinhBenhLyVaDienBien = "";
        public string TomTatKetQuaXetNghiem = "";
        public string PhuongPhapDieuTri = "";
        public string TinhTrangRaVien = "";
        public string HuongDieuTriTiepTheo = "";
        public string LoiDanBacSi = "";

        public int EmrDocCount;
        public List<string> EmrSummaryDocs = new List<string>();
        public List<string> EmrDischargeDocs = new List<string>();
        public List<string> EmrTransferDocs = new List<string>();
    }
}
