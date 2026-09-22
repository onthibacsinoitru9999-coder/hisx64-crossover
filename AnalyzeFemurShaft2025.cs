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

        // Lọc các ca Gãy thân xương đùi:
        // A. Mã ICD là S72.3, S72.30, S72.31
        // B. Chẩn đoán có chứa: "thân xương đùi", "1/3 giữa xương đùi", "1/3 trên xương đùi", "1/3 dưới xương đùi"
        // (Chú ý: loại trừ gãy cổ xương đùi S72.0 hoặc gãy liên mấu chuyển S72.1 nếu không kèm gãy thân)

        string[] shaftKeywords = new string[] {
            "thân xương đùi", "than xuong dui",
            "1/3 giữa xương đùi", "1/3 giua xuong dui", "1/3g xương đùi", "1/3 g xương đùi",
            "1/3 trên xương đùi", "1/3 tren xuong dui", "1/3t xương đùi", "1/3 t xương đùi",
            "1/3 dưới xương đùi", "1/3 duoi xuong dui", "1/3d xương đùi", "1/3 d xương đùi",
            "giữa đùi", "dưới đùi", "trên đùi", "thân đùi"
        };

        List<V_HIS_TREATMENT> shaftCases = new List<V_HIS_TREATMENT>();

        foreach (var t in allTreatments.Values)
        {
            string code = (t.ICD_CODE ?? "").ToUpper();
            string subCode = (t.ICD_SUB_CODE ?? "").ToUpper();
            string diag = ((t.ICD_NAME ?? "") + " " + (t.ICD_TEXT ?? "")).ToLower();

            bool isShaftByIcd = code.StartsWith("S72.3") || subCode.Contains("S72.3");
            bool isShaftByText = shaftKeywords.Any(k => diag.Contains(k));

            if (isShaftByIcd || isShaftByText)
            {
                shaftCases.Add(t);
            }
        }

        Console.WriteLine(string.Format("=== KẾT QUẢ RÀ SOÁT GÃY THÂN XƯƠNG ĐÙI NĂM 2025 ==="));
        Console.WriteLine(string.Format("Tổng số trường hợp gãy thân xương đùi (mã S72.3 + chẩn đoán lâm sàng): {0} trường hợp\n", shaftCases.Count));

        // Phân loại: Mổ vs Không mổ
        var shaftIds = shaftCases.Select(x => x.ID).ToList();
        HashSet<long> surgIds = new HashSet<long>();

        foreach (var t in shaftCases)
        {
            if (!string.IsNullOrEmpty(t.SURGERY_NAME) || t.SURGERY_BEGIN_TIME.HasValue)
                surgIds.Add(t.ID);
        }

        // Query ServiceReq PTTT cho các ca còn lại
        var remainIds = shaftIds.Where(id => !surgIds.Contains(id)).ToList();
        if (remainIds.Count > 0)
        {
            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_IDs = remainIds,
                SERVICE_REQ_TYPE_ID = 4
            };
            param = new CommonParam();
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (srs != null)
            {
                foreach (var r in srs)
                {
                    if (r.SERVICE_REQ_STT_ID == 3 || r.SERVICE_REQ_STT_ID == 2)
                        surgIds.Add(r.TREATMENT_ID);
                }
            }
        }

        var operated = shaftCases.Where(x => surgIds.Contains(x.ID)).ToList();
        var nonOperated = shaftCases.Where(x => !surgIds.Contains(x.ID)).ToList();

        Console.WriteLine(string.Format("• Đã phẫu thuật (kết hợp xương đinh nội tủy/nẹp vít...): {0} ca", operated.Count));
        Console.WriteLine(string.Format("• Không mổ (điều trị bảo tồn / bó bột / tái khám cũ / chuyển viện): {0} ca", nonOperated.Count));

        // Phân bố theo phân đoạn thân xương đùi
        int count13Giua = 0;
        int count13Duoi = 0;
        int count13Tren = 0;
        int countChung = 0;

        foreach (var t in shaftCases)
        {
            string diag = ((t.ICD_NAME ?? "") + " " + (t.ICD_TEXT ?? "")).ToLower();
            if (diag.Contains("1/3 giữa") || diag.Contains("1/3 giua") || diag.Contains("1/3g")) count13Giua++;
            else if (diag.Contains("1/3 dưới") || diag.Contains("1/3 duoi") || diag.Contains("1/3d")) count13Duoi++;
            else if (diag.Contains("1/3 trên") || diag.Contains("1/3 tren") || diag.Contains("1/3t")) count13Tren++;
            else countChung++;
        }

        Console.WriteLine("\n--- PHÂN BỐ THEO ĐOẠN THÂN XƯƠNG ĐÙI ---");
        Console.WriteLine(string.Format("  • Gãy 1/3 giữa thân xương đùi: {0} ca", count13Giua));
        Console.WriteLine(string.Format("  • Gãy 1/3 dưới thân xương đùi: {0} ca", count13Duoi));
        Console.WriteLine(string.Format("  • Gãy 1/3 trên thân xương đùi: {0} ca", count13Tren));
        Console.WriteLine(string.Format("  • Gãy thân xương đùi chung (chưa định vị đoạn): {0} ca", countChung));

        // Phân bố theo Khoa điều trị
        Console.WriteLine("\n--- PHÂN BỐ THEO KHOA ĐIỀU TRỊ ---");
        var deptGroups = shaftCases.GroupBy(x => x.END_DEPARTMENT_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var dg in deptGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} ca (Trong đó mổ: {2})", 
                dg.Key, dg.Count(), dg.Count(x => surgIds.Contains(x.ID))));
        }

        // Danh sách mẫu 10 ca
        Console.WriteLine("\n--- DANH SÁCH CHI TIẾT CÁC CA GÃY THÂN XƯƠNG ĐÙI ---");
        foreach (var t in shaftCases.Take(10))
        {
            Console.WriteLine(string.Format("  [{0}] {1} ({2}) | Khoa: {3} | ICD: [{4}] {5} | Mổ: {6}",
                t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.TDL_PATIENT_GENDER_NAME,
                t.END_DEPARTMENT_NAME, t.ICD_CODE, t.ICD_NAME,
                surgIds.Contains(t.ID) ? "CÓ" : "KHÔNG"));
        }
    }
}
