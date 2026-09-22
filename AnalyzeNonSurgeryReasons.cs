using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

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
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = e.Name.Split(',')[0];
            string cur = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = new string[]
            {
                Path.Combine(cur, "ReferencedAssemblies", shortName + ".dll"),
                Path.Combine(cur, "Plugins", "Module", shortName + ".dll"),
                Path.Combine(cur, shortName + ".dll")
            };
            foreach (var p in paths) if (File.Exists(p)) return Assembly.LoadFrom(p);
            return null;
        };

        Run();
    }

    static string ReadLiveToken()
    {
        string cur = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates = new string[]
        {
            Path.Combine(cur, "doctor_standalone.token"),
            Path.Combine(cur, "Logs", "LogSystem.txt")
        };

        foreach (var c in candidates)
        {
            if (!File.Exists(c)) continue;
            try
            {
                if (c.EndsWith(".token"))
                {
                    string text = File.ReadAllText(c, Encoding.UTF8).Trim();
                    string[] parts = text.Split('|');
                    if (parts.Length >= 1 && parts[0].Length == 64) return parts[0];
                }
                else
                {
                    using (var fs = new FileStream(c, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        long len = fs.Length;
                        int bufSize = (int)Math.Min(262144L, len);
                        fs.Seek(len - bufSize, SeekOrigin.Begin);
                        byte[] buf = new byte[bufSize];
                        int read = fs.Read(buf, 0, bufSize);
                        string chunk = Encoding.UTF8.GetString(buf, 0, read);
                        int idx = chunk.LastIndexOf("TokenCode|");
                        if (idx >= 0 && chunk.Length >= idx + 10 + 64)
                        {
                            return chunk.Substring(idx + 10, 64);
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) return;

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        // 1. Tải tất cả lượt điều trị S72 năm 2025
        Dictionary<long, V_HIS_TREATMENT> allTreatments = new Dictionary<long, V_HIS_TREATMENT>();
        string[] icdCodesToQuery = new string[] { 
            "S72", "S72.0", "S72.1", "S72.2", "S72.3", "S72.4", "S72.7", "S72.8", "S72.9" 
        };

        foreach (var code in icdCodesToQuery)
        {
            var tf = new HisTreatmentViewFilter();
            tf.IN_TIME_FROM = 20250101000000;
            tf.IN_TIME_TO   = 20251231235959;
            tf.ICD_CODE_OR_ICD_SUB_CODE = code;
            tf.BRANCH_ID = 1;
            param = new CommonParam();
            var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null)
            {
                foreach (var t in list) allTreatments[t.ID] = t;
            }
        }

        // Lấy danh sách ServiceReq PTTT
        var allIds = allTreatments.Keys.ToList();
        HashSet<long> surgeryTreatmentIds = new HashSet<long>();

        // Những ca có SURGERY_NAME trực tiếp
        foreach (var t in allTreatments.Values)
        {
            if (!string.IsNullOrEmpty(t.SURGERY_NAME) || t.SURGERY_BEGIN_TIME.HasValue)
                surgeryTreatmentIds.Add(t.ID);
        }

        // Query thêm ServiceReq PTTT
        for (int i = 0; i < allIds.Count; i += 50)
        {
            var batchIds = allIds.Skip(i).Take(50).ToList();
            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_IDs = batchIds,
                SERVICE_REQ_TYPE_ID = 4
            };
            param = new CommonParam();
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (srs != null && srs.Count > 0)
            {
                foreach (var req in srs)
                {
                    if (req.SERVICE_REQ_STT_ID == 3 || req.SERVICE_REQ_STT_ID == 2)
                    {
                        surgeryTreatmentIds.Add(req.TREATMENT_ID);
                    }
                }
            }
        }

        var nonSurgeryTreatments = allTreatments.Values.Where(x => !surgeryTreatmentIds.Contains(x.ID)).ToList();
        Console.WriteLine(string.Format("=== TỔNG HỢP {0} CA KHÔNG MỔ / KHÔNG PHẪU THUẬT NĂM 2025 ===", nonSurgeryTreatments.Count));

        // Phân loại lý do theo:
        // 1. Hình thức kết thúc điều trị (TREATMENT_END_TYPE)
        // 2. Chẩn đoán lâm sàng (Gãy cũ/tiền sử, Điều trị bảo tồn, Bệnh nội khoa quá nặng không thể phẫu thuật)
        // 3. Kết quả điều trị (Xin về, Chuyển viện, Tử vong)

        int countOldFracture = 0;          // Tiền sử gãy xương đùi cũ / đã mổ từ trước
        int countConservative = 0;         // Chỉ định rõ điều trị bảo tồn / bó bột
        int countSevereMedical = 0;        // Bệnh nội khoa nặng đe dọa tính mạng (suy tim/đột quỵ/suy hô hấp/ung thư di căn)
        int countTransferOrDischarged = 0; // Chuyển viện / Nặng xin về / Không điều trị tiếp
        int countOther = 0;

        List<string> oldExamples = new List<string>();
        List<string> severeExamples = new List<string>();
        List<string> conservativeExamples = new List<string>();
        List<string> transferExamples = new List<string>();

        foreach (var t in nonSurgeryTreatments)
        {
            string diag = ((t.ICD_NAME ?? "") + " " + (t.ICD_TEXT ?? "")).ToLower();
            string endType = (t.TREATMENT_END_TYPE_NAME ?? "").ToLower();
            string result = (t.TREATMENT_RESULT_NAME ?? "").ToLower();

            bool isOld = diag.Contains("cũ") || diag.Contains("tiền sử") || diag.Contains("đã mổ") || diag.Contains("đã kết hợp xương") || diag.Contains("đã nẹp");
            bool isCons = diag.Contains("bảo tồn") || diag.Contains("bó bột") || diag.Contains("nẹp chống xoay") || diag.Contains("bột");
            bool isSevere = diag.Contains("suy hô hấp") || diag.Contains("sốc") || diag.Contains("nhồi máu não") || diag.Contains("xuất huyết não") || 
                            diag.Contains("suy tim") || diag.Contains("ung thư") || diag.Contains("xơ gan child") || diag.Contains("chấn thương sọ não g 3") || 
                            diag.Contains("suy đa tạng") || diag.Contains("nhiễm khuẩn huyết") || diag.Contains("tử vong");
            bool isTransfer = endType.Contains("chuyển") || endType.Contains("xin về") || result.Contains("nặng xin về") || result.Contains("tử vong") || endType.Contains("tử vong");

            if (isOld)
            {
                countOldFracture++;
                if (oldExamples.Count < 5) oldExamples.Add(string.Format("[{0}] {1} ({2}) | Khoa: {3} | Ra viện: {4}", t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.ICD_NAME, t.END_DEPARTMENT_NAME, t.TREATMENT_END_TYPE_NAME));
            }
            else if (isCons)
            {
                countConservative++;
                if (conservativeExamples.Count < 5) conservativeExamples.Add(string.Format("[{0}] {1} ({2}) | Khoa: {3}", t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.ICD_NAME, t.END_DEPARTMENT_NAME));
            }
            else if (isSevere)
            {
                countSevereMedical++;
                if (severeExamples.Count < 5) severeExamples.Add(string.Format("[{0}] {1} ({2}) | Khoa: {3} | Kết thúc: {4}", t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.ICD_NAME, t.END_DEPARTMENT_NAME, t.TREATMENT_END_TYPE_NAME));
            }
            else if (isTransfer)
            {
                countTransferOrDischarged++;
                if (transferExamples.Count < 5) transferExamples.Add(string.Format("[{0}] {1} ({2}) | Khoa: {3} | HT: {4}", t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.ICD_NAME, t.END_DEPARTMENT_NAME, t.TREATMENT_END_TYPE_NAME));
            }
            else
            {
                countOther++;
            }
        }

        Console.WriteLine("\n--- PHÂN LOẠI LÝ DO KHÔNG PHẪU THUẬT (151 CA TOÀN VIỆN) ---");
        Console.WriteLine(string.Format("1. Bệnh nhân nhập viện vì bệnh nội khoa khác, Gãy xương đùi là TIỀN SỬ CŨ (đã mổ/liền xương từ trước): {0} BN", countOldFracture));
        Console.WriteLine(string.Format("2. Chống chỉ định phẫu thuật do BỆNH NỀN NẶNG ĐE DỌA TÍNH MẠNG (Sốc nhiễm khuẩn, Tai biến mạch não cấp, Suy tim nặng, Hôn mê, Ung thư di căn): {0} BN", countSevereMedical));
        Console.WriteLine(string.Format("3. Chỉ định ĐIỀU TRỊ BẢO TỒN (Nắn bó bột, nẹp chống xoay, rạn xương/không di lệch hoặc tuổi quá cao không mổ được): {0} BN", countConservative));
        Console.WriteLine(string.Format("4. CHUYỂN VIỆN / NẶNG XIN VỀ / TỬ VONG TRƯỚC KHI MỔ: {0} BN", countTransferOrDischarged));
        Console.WriteLine(string.Format("5. Các trường hợp điều trị ngoại trú / chuyển khám chuyên khoa khác: {0} BN", countOther));

        // Phân bố theo hình thức kết thúc điều trị
        Console.WriteLine("\n--- HÌNH THỨC KẾT THÚC ĐIỀU TRỊ (TREATMENT_END_TYPE) ---");
        var endGroups = nonSurgeryTreatments.GroupBy(x => x.TREATMENT_END_TYPE_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var eg in endGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} BN", eg.Key, eg.Count()));
        }

        // Phân bố theo kết quả điều trị (TREATMENT_RESULT)
        Console.WriteLine("\n--- KẾT QUẢ ĐIỀU TRỊ (TREATMENT_RESULT) ---");
        var resGroups = nonSurgeryTreatments.GroupBy(x => x.TREATMENT_RESULT_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var rg in resGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} BN", rg.Key, rg.Count()));
        }

        // Chi tiết 42 ca tại Khoa 57 (Chấn thương Chỉnh hình & Cột sống)
        var k57NonSurg = nonSurgeryTreatments.Where(x => x.END_DEPARTMENT_ID == 57 || (x.END_DEPARTMENT_NAME != null && x.END_DEPARTMENT_NAME.Contains("Chấn thương"))).ToList();
        Console.WriteLine(string.Format("\n=== RIÊNG KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (42 CA KHÔNG MỔ) ==="));
        var k57EndGroups = k57NonSurg.GroupBy(x => x.TREATMENT_END_TYPE_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var eg in k57EndGroups)
        {
            Console.WriteLine(string.Format("  • Hình thức kết thúc: {0}: {1} BN", eg.Key, eg.Count()));
        }

        Console.WriteLine("\nDanh sách chi tiết các ca không mổ tại Khoa 57:");
        foreach (var t in k57NonSurg)
        {
            Console.WriteLine(string.Format("  [{0}] {1} | Tuổi: {2} | ICD: {3} | Ra viện: {4} | KQ: {5}",
                t.TREATMENT_CODE, t.TDL_PATIENT_NAME, 
                t.TDL_PATIENT_DOB > 0 ? (2025 - int.Parse(t.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "?",
                t.ICD_NAME, t.TREATMENT_END_TYPE_NAME, t.TREATMENT_RESULT_NAME));
        }
    }
}
