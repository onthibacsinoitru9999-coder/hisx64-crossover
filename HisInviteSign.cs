using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using EMR.EFMODEL.DataModels;
using EMR.Filter;
using EMR.SDO;

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

public class HisInviteSign
{
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static ApiConsumer mosConsumer;
    public static ApiConsumer emrConsumer;
    public static string currentToken;

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>
        {
            Path.Combine(baseDir, "doctor_standalone.token"),
            Path.Combine(Directory.GetCurrentDirectory(), "doctor_standalone.token"),
            @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\doctor_standalone.token",
            Path.Combine(baseDir, "doctor_hn.token"),
            Path.Combine(Directory.GetCurrentDirectory(), "doctor_hn.token")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                try
                {
                    string raw = File.ReadAllText(p, Encoding.UTF8).Trim();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        if (raw.Contains("|"))
                            currentToken = raw.Split('|')[0].Trim();
                        else
                            currentToken = raw;

                        if (currentToken.Length == 64) break;
                    }
                }
                catch { }
            }
        }

        if (string.IsNullOrEmpty(currentToken))
            throw new Exception("Không tìm thấy file token xác thực hợp lệ (doctor_standalone.token)!");

        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
        emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");
    }

    public static V_HIS_TREATMENT FindTreatment(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;
        string kw = (keyword ?? "").Trim();
        long numVal;
        bool isNum = long.TryParse(kw, out numVal);

        if (isNum)
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }
        else
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }

        if (treatments == null || treatments.Count == 0) return null;
        return treatments.OrderByDescending(t => t.IS_ACTIVE == 1).ThenByDescending(t => t.ID).First();
    }

    public static void LookupSignStatus(string keyword)
    {
        InitSession();
        var tr = FindTreatment(keyword);
        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy bệnh nhân cho từ khóa: " + keyword);
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📂 HỒ SƠ VĂN BẢN EMR & TRẠNG THÁI KÝ: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
        Console.WriteLine(string.Format("Mã ĐT: {0} | Chẩn đoán: [{1}] {2}", tr.TREATMENT_CODE, tr.ICD_CODE, tr.ICD_NAME));
        Console.WriteLine("===============================================================================");

        var df = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
        var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, df, param);
        if (docs == null || docs.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân chưa có văn bản EMR nào.");
            return;
        }

        var trkDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == 7 || (d.DOCUMENT_NAME != null && d.DOCUMENT_NAME.ToLower().Contains("điều trị"))).OrderByDescending(d => d.ID).ToList();
        Console.WriteLine(string.Format("📋 Tìm thấy {0} văn bản Tờ điều trị (Type 7):", trkDocs.Count));

        int idx = 1;
        foreach (var doc in trkDocs)
        {
            var sf = new EmrSignFilter { DOCUMENT_ID = doc.ID };
            var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, sf, param);

            bool isAllSigned = signs != null && signs.Count > 0 && signs.All(s => s.SIGN_TIME > 0);
            string stt = isAllSigned ? "🟢 ĐÃ KÝ ĐẦY ĐỦ" : (!string.IsNullOrEmpty(doc.NEXT_SIGNER) ? "🟡 Chờ " + doc.NEXT_SIGNER + " ký" : "🔴 Chưa ký");

            Console.WriteLine(string.Format("\n[{0}] DocID: {1,-8} | Mã VB: {2} | {3}", idx++, doc.ID, doc.DOCUMENT_CODE, stt));
            Console.WriteLine(string.Format("    Tên VB    : {0}", doc.DOCUMENT_NAME));
            Console.WriteLine(string.Format("    HIS_CODE  : {0}", doc.HIS_CODE));
            Console.WriteLine(string.Format("    NextSigner: {0}", doc.NEXT_SIGNER ?? "Trống"));

            if (signs != null && signs.Count > 0)
            {
                foreach (var s in signs.OrderBy(x => x.NUM_ORDER))
                {
                    string signInfo = s.SIGN_TIME > 0 ? string.Format("✅ Đã ký ({0})", s.SIGN_TIME) : "❌ Chưa ký";
                    Console.WriteLine(string.Format("      • Vị trí {0} ({1} - {2}): {3}", s.NUM_ORDER, s.LOGINNAME, s.USERNAME, signInfo));
                }
            }
        }
        Console.WriteLine("\n===============================================================================");
    }

    public static void InviteDoctorToSign(string keyword, string doctorLogin = "034727", long? targetTrackingId = null, bool force = false)
    {
        InitSession();
        var tr = FindTreatment(keyword);
        if (tr == null)
        {
            Console.WriteLine("❌ Không tìm thấy bệnh nhân cho từ khóa: " + keyword);
            return;
        }

        string targetDoctorLogin = !string.IsNullOrEmpty(doctorLogin) ? doctorLogin.Trim() : "034727";
        string targetDoctorName = targetDoctorLogin == "034727" ? "Ths.BS Nguyễn Hữu Sâm" : targetDoctorLogin;
        string targetDoctorTitle = targetDoctorLogin == "034727" ? "Ths.BS" : "BS";
        string targetDeptCode = "9";
        string targetDeptName = "Khoa Chấn thương Chỉnh hình và Cột sống";

        Console.WriteLine("===============================================================================");
        Console.WriteLine("📩 THỰC THI MỜI BÁC SĨ KÝ TỜ ĐIỀU TRỊ (INVITE TO SIGN)");
        Console.WriteLine(string.Format("👤 Bệnh nhân: {0} ({1}) | Mã ĐT: {2}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
        Console.WriteLine(string.Format("👨‍⚕️ Bác sĩ được mời ký: {0} ({1})", targetDoctorName, targetDoctorLogin));
        if (targetTrackingId.HasValue) Console.WriteLine(string.Format("🎯 Chỉ định đích danh Tracking ID: {0}", targetTrackingId.Value));
        Console.WriteLine("===============================================================================");

        var df = new EmrDocumentFilter { TREATMENT_CODE__EXACT = tr.TREATMENT_CODE };
        var docs = myAdapter.FetchList<EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, df, param);
        if (docs == null || docs.Count == 0)
        {
            Console.WriteLine("ℹ️ Bệnh nhân chưa có văn bản EMR nào.");
            return;
        }

        // Lọc các văn bản Tờ điều trị (Type 7)
        var trkDocs = docs.Where(d => d.DOCUMENT_TYPE_ID == 7 || (d.DOCUMENT_NAME != null && d.DOCUMENT_NAME.ToLower().Contains("điều trị"))).ToList();
        if (targetTrackingId.HasValue)
        {
            trkDocs = trkDocs.Where(d => d.HIS_CODE != null && d.HIS_CODE.Contains(targetTrackingId.Value.ToString())).ToList();
        }

        if (trkDocs.Count == 0)
        {
            Console.WriteLine(string.Format("ℹ️ Không tìm thấy văn bản EMR Tờ điều trị{0} của bệnh nhân.", targetTrackingId.HasValue ? " liên kết Tracking ID " + targetTrackingId.Value : ""));
            Console.WriteLine("💡 Lưu ý: Tờ điều trị cần được in/phát hành lên EMR từ HIS Desktop trước khi có thể mời ký số.");
            return;
        }

        Console.WriteLine(string.Format("📋 Tìm thấy {0} văn bản Tờ điều trị cần kiểm tra mời ký:\n", trkDocs.Count));
        int updatedCount = 0;
        int idx = 1;

        foreach (var doc in trkDocs.OrderByDescending(d => d.ID))
        {
            var sf = new EmrSignFilter { DOCUMENT_ID = doc.ID };
            var signs = myAdapter.FetchList<EMR_SIGN>("api/EmrSign/Get", emrConsumer, sf, param);

            Console.WriteLine(string.Format("[{0}] DocID: {1,-8} | Mã VB: {2} | Tên: {3}", idx++, doc.ID, doc.DOCUMENT_CODE, doc.DOCUMENT_NAME));
            Console.WriteLine(string.Format("    HIS_CODE  : {0}", doc.HIS_CODE));

            // Kiểm tra xem Bác sĩ đã ký ở vị trí nào chưa
            var mySigned = signs != null ? signs.FirstOrDefault(s => string.Equals(s.LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase) && s.SIGN_TIME > 0) : null;
            if (mySigned != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("    👉 TRẠNG THÁI: 🟢 ĐÃ ĐƯỢC KÝ BỞI BÁC SĨ ({0}) lúc {1}", targetDoctorLogin, mySigned.SIGN_TIME));
                Console.ResetColor();
                continue;
            }

            // Tìm vị trí ký chưa ký
            var slotToUpdate = signs != null ? signs.FirstOrDefault(s => 
                (string.Equals(s.LOGINNAME, targetDoctorLogin, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.LOGINNAME, doc.NEXT_SIGNER, StringComparison.OrdinalIgnoreCase) ||
                 string.IsNullOrEmpty(s.LOGINNAME)) &&
                (s.SIGN_TIME == null || s.SIGN_TIME == 0)
            ) : null;

            if (slotToUpdate == null && force && signs != null)
            {
                slotToUpdate = signs.FirstOrDefault(s => s.SIGN_TIME == null || s.SIGN_TIME == 0);
            }

            var updateSdo = new EmrSignUpdateSDO
            {
                DocumentId = doc.ID,
                Updates = new List<EMR_SIGN>()
            };

            if (slotToUpdate != null)
            {
                updateSdo.Updates.Add(new EMR_SIGN
                {
                    ID = slotToUpdate.ID,
                    DOCUMENT_ID = doc.ID,
                    NUM_ORDER = slotToUpdate.NUM_ORDER,
                    LOGINNAME = targetDoctorLogin,
                    USERNAME = targetDoctorName,
                    TITLE = targetDoctorTitle,
                    DEPARTMENT_CODE = targetDeptCode,
                    DEPARTMENT_NAME = targetDeptName,
                    SIGN_TIME = null,
                    SIGN_DATE = null
                });
            }
            else
            {
                updateSdo.Creates = new List<EMR_SIGN>
                {
                    new EMR_SIGN
                    {
                        DOCUMENT_ID = doc.ID,
                        NUM_ORDER = (signs != null && signs.Count > 0 ? signs.Max(x => x.NUM_ORDER) + 1 : 1),
                        LOGINNAME = targetDoctorLogin,
                        USERNAME = targetDoctorName,
                        TITLE = targetDoctorTitle,
                        DEPARTMENT_CODE = targetDeptCode,
                        DEPARTMENT_NAME = targetDeptName,
                        SIGN_TIME = null,
                        SIGN_DATE = null,
                        IS_ACTIVE = 1,
                        IS_DELETE = 0
                    }
                };
            }

            CommonParam pSign = new CommonParam();
            bool ok = myAdapter.PostData<bool>("api/EmrSign/UpdateSdo", emrConsumer, updateSdo, pSign);
            if (ok)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("    👉 MỜI KÝ THÀNH CÔNG: 🟡 ĐÃ CHUYỂN SANG 'CHỜ BÁC SĨ KÝ' ({0})", targetDoctorLogin));
                Console.ResetColor();
                updatedCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string err = (pSign.Messages != null && pSign.Messages.Count > 0) ? string.Join("; ", pSign.Messages) : "Lỗi gọi API UpdateSdo";
                Console.WriteLine(string.Format("    👉 THẤT BẠI: {0}", err));
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("📊 TỔNG KẾT: Đã xử lý {0} văn bản Tờ điều trị | Mời ký thành công: {1}", trkDocs.Count, updatedCount));
        Console.WriteLine("===============================================================================");
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";

            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);

            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);

            DirectoryInfo cur = new DirectoryInfo(folderPath);
            for (int i = 0; i < 5; i++)
            {
                if (cur.Parent == null) break;
                cur = cur.Parent;
                string pRoot = Path.Combine(cur.FullName, name);
                if (File.Exists(pRoot)) return Assembly.LoadFrom(pRoot);
                string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                if (File.Exists(pRef)) return Assembly.LoadFrom(pRef);
            }
            return null;
        };

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("🏥 HIS INVITE SIGN - PHẦN MỀM MỜI KÝ TỜ ĐIỀU TRỊ & VĂN BẢN EMR TỰ ĐỘNG");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Cú pháp lệnh:");
            Console.WriteLine("  lookup <MãBN|MãĐT>                                : Xem toàn bộ văn bản EMR & lượt ký");
            Console.WriteLine("  invite <MãBN|MãĐT> [DoctorLogin] [--tracking Id]  : Mời Bác sĩ ký Tờ điều trị");
            Console.WriteLine("Ví dụ:");
            Console.WriteLine("  HisInviteSign.exe lookup 0004053995");
            Console.WriteLine("  HisInviteSign.exe invite 0004053995 034727 --tracking 10179235");
            Console.WriteLine("===============================================================================");
            return;
        }

        string cmd = args[0].ToLower();
        try
        {
            if (cmd == "lookup" || cmd == "view" || cmd == "list")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc mã ĐT!");
                HisInviteSign.LookupSignStatus(args[1]);
            }
            else if (cmd == "invite" || cmd == "sign" || cmd == "moi-ky")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc mã ĐT!");
                string kw = args[1];
                string doctorLogin = "034727";
                long? trackingId = null;
                bool force = false;

                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--tracking" && i + 1 < args.Length)
                    {
                        long tid;
                        if (long.TryParse(args[++i], out tid)) trackingId = tid;
                    }
                    else if (args[i] == "--force" || args[i] == "-f")
                    {
                        force = true;
                    }
                    else if (!args[i].StartsWith("-"))
                    {
                        doctorLogin = args[i];
                    }
                }

                HisInviteSign.InviteDoctorToSign(kw, doctorLogin, trackingId, force);
            }
            else
            {
                Console.WriteLine("Lệnh không hợp lệ: " + cmd);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LỖI: " + ex.Message);
        }
    }
}
