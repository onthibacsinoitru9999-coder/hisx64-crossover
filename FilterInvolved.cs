using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    class FilterInvolved
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

            RealMain();
        }

        const string MOS_BASE = "http://192.168.7.236:1608/";
        const string EMR_BASE = "http://192.168.7.239:1415/";
        const string EMR_CONNSTR = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

        static Assembly _emrMainLib;
        static Assembly _mdbLib;
        static Type _mdbConnType;
        static Type _baNKFuncType;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RealMain()
        {
            string tokenCode = File.ReadAllText("doctor_standalone.token").Split('|')[0];
            var consumer = new ApiConsumer(MOS_BASE, tokenCode, "HIS");
            var emrConsumer = new ApiConsumer(EMR_BASE, tokenCode, "HIS");
            var adapter = new MyAdapter();
            var param = new CommonParam();

            // Load Oracle assemblies
            string emrDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR");
            _mdbLib = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
            _emrMainLib = Assembly.LoadFrom(Path.Combine(emrDir, "EMR_MAIN.Library.dll"));
            _mdbConnType = _mdbLib.GetType("MDB.MDBConnection");
            _baNKFuncType = _emrMainLib.GetType("EMR_MAIN.BenhAnNgoaiKhoaFunc");

            dynamic con = Activator.CreateInstance(_mdbConnType, new object[] { EMR_CONNSTR });
            con.Open();

            long targetFrom = 20260928000000L;
            long targetTo   = 20260928235959L;

            var tf = new HisTreatmentViewFilter
            {
                OUT_TIME_FROM = targetFrom,
                OUT_TIME_TO = targetTo
            };
            var outTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param) ?? new List<V_HIS_TREATMENT>();

            var trCodes = new string[] {
                "000007241713", "000007239118", "000007303063", "000007108928", "000007263789",
                "000007286480", "000007231535", "000007293114", "000007275373", "000007309974",
                "000007292873", "000007298104", "000007306067", "000007256956", "000007236651",
                "000007300679", "000007310820", "000007267525", "000007329686", "000007309079"
            };

            var matched = outTreatments.Where(x => trCodes.Contains(x.TREATMENT_CODE)).ToList();

            Console.WriteLine(string.Format("=== CHI TIẾT 20 BỆNH NHÂN ĐƯỢC NHẬN DIỆN LIÊN QUAN ĐẾN BS 034727 ==="));
            int i = 0;
            foreach (var tr in matched)
            {
                i++;
                decimal maQuanLy = 0;
                decimal.TryParse(tr.TREATMENT_CODE, out maQuanLy);

                var pTrkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
                var pTrackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, pTrkFilter, param) ?? new List<V_HIS_TRACKING>();
                var myTrackings = pTrackings.Where(t => t.CREATOR == "034727" || t.MODIFIER == "034727").ToList();

                // EMR Docs
                var docFilter = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
                var emrDocs = adapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param) ?? new List<EMR_DOCUMENT>();

                // Oracle EMR
                var ttdtFuncType = _emrMainLib.GetType("EMR_MAIN.ThongTinDieuTriFunc");
                var checkMethod = ttdtFuncType.GetMethod("checkExistThongTinDieuTri", new Type[] { _mdbConnType, typeof(decimal) });
                bool hasTTDT = (bool)checkMethod.Invoke(null, new object[] { con, maQuanLy });
                if (!hasTTDT) hasTTDT = (bool)checkMethod.Invoke(null, new object[] { con, (decimal)tr.ID });

                var selectMethod = _baNKFuncType.GetMethod("Select", new Type[] { _mdbConnType, typeof(decimal) });
                dynamic ba = selectMethod.Invoke(null, new object[] { con, maQuanLy });
                if (ba == null || ba.MaQuanLy == 0) ba = selectMethod.Invoke(null, new object[] { con, (decimal)tr.ID });

                bool hasBANK = (ba != null && ba.MaQuanLy > 0);
                string tomtatBA = hasBANK ? Convert.ToString(ba.TomTatBenhAn) : "";
                string qtbl = hasBANK ? Convert.ToString(ba.QuaTrinhBenhLy) : "";
                string qtblVd = hasBANK ? Convert.ToString(ba.QuaTrinhBenhLyVaDienBien) : "";
                string tinhTrang = hasBANK ? Convert.ToString(ba.TinhTrangNguoiBenhRaVien) : "";

                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("[{0}] BN: {1} | Mã BN: {2} | Mã ĐT: {3}", i, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
                Console.WriteLine(string.Format("    Diện điều trị: {0} ({1}) | Ra viện (OUT_TIME): {2}",
                    tr.TDL_TREATMENT_TYPE_ID,
                    tr.TDL_TREATMENT_TYPE_ID == 3 ? "NỘI TRÚ" : (tr.TDL_TREATMENT_TYPE_ID == 1 ? "NGOẠI TRÚ/PHÒNG KHÁM" : "KHÁC"),
                    tr.OUT_TIME));
                Console.WriteLine(string.Format("    Khoa kết thúc: [{0}] {1} | Buồng/Phòng: [{2}] {3}", tr.END_DEPARTMENT_ID, tr.END_DEPARTMENT_NAME, tr.END_ROOM_ID, tr.END_ROOM_NAME));
                Console.WriteLine(string.Format("    Hình thức: [{0}] {1} | Kết quả: [{2}] {3}", tr.TREATMENT_END_TYPE_ID, tr.TREATMENT_END_TYPE_NAME, tr.TREATMENT_RESULT_ID, tr.TREATMENT_RESULT_NAME));
                Console.WriteLine(string.Format("    Người làm thủ tục ra/chuyển viện: {0} ({1})", tr.END_LOGINNAME, tr.END_USERNAME));
                Console.WriteLine(string.Format("    Bác sĩ điều trị                 : {0} ({1})", tr.DOCTOR_LOGINNAME, tr.DOCTOR_USERNAME));
                Console.WriteLine(string.Format("    Người sửa cuối hồ sơ (MODIFIER) : {0}", tr.MODIFIER));
                Console.WriteLine(string.Format("    Tờ điều trị BS 034727 tạo/sửa   : {0} tờ (Tổng số tờ trong BA: {1})", myTrackings.Count, pTrackings.Count));
                Console.WriteLine(string.Format("    Trang bìa THONGTINDIEUTRI       : {0}", hasTTDT ? "CÓ" : "CHƯA CÓ"));
                Console.WriteLine(string.Format("    Vỏ bệnh án Ngoại khoa (BANK)   : {0}", hasBANK ? "CÓ" : "CHƯA CÓ"));
                Console.WriteLine(string.Format("    Tóm tắt bệnh án (TomTatBenhAn)  : {0}", !string.IsNullOrWhiteSpace(tomtatBA) ? "CÓ (" + tomtatBA.Length + " ký tự)" : "TRỐNG/CHƯA CÓ"));
                Console.WriteLine(string.Format("    Bìa tổng kết ra viện            : {0}", (!string.IsNullOrWhiteSpace(qtblVd) || !string.IsNullOrWhiteSpace(tinhTrang)) ? "CÓ" : "TRỐNG/CHƯA CÓ"));
                Console.WriteLine(string.Format("    Văn bản EMR liên quan (Tổng {0} VB):", emrDocs.Count));
                foreach (var d in emrDocs.Where(x => x.DOCUMENT_NAME.Contains("Tóm tắt") || x.DOCUMENT_NAME.Contains("tóm tắt") || x.DOCUMENT_NAME.Contains("ra viện") || x.DOCUMENT_NAME.Contains("chuyển") || x.DOCUMENT_TYPE_ID == 2 || x.DOCUMENT_TYPE_ID == 16 || x.DOCUMENT_TYPE_ID == 25 || x.DOCUMENT_TYPE_ID == 115))
                {
                    Console.WriteLine(string.Format("      - [Loại {0}] {1} (Mã: {2})", d.DOCUMENT_TYPE_ID, d.DOCUMENT_NAME, d.DOCUMENT_CODE));
                }
            }

            con.Close();
        }
    }
}
