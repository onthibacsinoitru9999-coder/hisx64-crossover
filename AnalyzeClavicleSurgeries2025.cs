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

        Console.WriteLine("=== ĐANG TRUY VẤN CÁC CA GÃY XƯƠNG ĐÒN NĂM 2025 (BẠCH MAI HÀ NỘI) ===");

        // Thu thập tất cả lượt điều trị khớp với nhóm S42 (Gãy xương vai và cánh tay) và đặc biệt là S42.0 (Gãy xương đòn)
        Dictionary<long, V_HIS_TREATMENT> allTreatments = new Dictionary<long, V_HIS_TREATMENT>();
        string[] icdCodesToQuery = new string[] { 
            "S42", "S42.0", "S42.00", "S42.01", "S42.8", "S42.9"
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
            int added = 0;
            if (list != null)
            {
                foreach (var t in list)
                {
                    if (!allTreatments.ContainsKey(t.ID))
                    {
                        allTreatments[t.ID] = t;
                        added++;
                    }
                }
            }
            Console.WriteLine(string.Format("Mã ICD '{0}': lấy được {1} hồ sơ (mới: {2})", code, list != null ? list.Count : 0, added));
        }

        // Lọc chính xác các ca GÃY XƯƠNG ĐÒN (Loại trừ gãy xương bả vai, xương cánh tay không kèm xương đòn)
        string[] clavicleKeywords = new string[] {
            "xương đòn", "xuong don", "gãy đòn", "gay don", "cùng đòn", "cung don", "clavicle", "clavicula"
        };

        List<V_HIS_TREATMENT> clavicleCases = new List<V_HIS_TREATMENT>();

        foreach (var t in allTreatments.Values)
        {
            string code = (t.ICD_CODE ?? "").ToUpper();
            string subCode = (t.ICD_SUB_CODE ?? "").ToUpper();
            string diag = ((t.ICD_NAME ?? "") + " " + (t.ICD_TEXT ?? "")).ToLower();

            bool isClavicleByCode = code.StartsWith("S42.0") || subCode.Contains("S42.0");
            bool isClavicleByText = clavicleKeywords.Any(k => diag.Contains(k));

            if (isClavicleByCode || isClavicleByText)
            {
                clavicleCases.Add(t);
            }
        }

        Console.WriteLine(string.Format("\n👉 Tổng cộng hồ sơ chẩn đoán GÃY XƯƠNG ĐÒN tại BM Hà Nội năm 2025: {0} ca", clavicleCases.Count));

        // Phân tích trạng thái phẫu thuật (mổ)
        var allIds = clavicleCases.Select(x => x.ID).ToList();
        HashSet<long> surgTreatmentIds = new HashSet<long>();
        Dictionary<long, string> surgeryDetailNames = new Dictionary<long, string>();

        // 1. Kiểm tra SURGERY_NAME trên Treatment
        foreach (var t in clavicleCases)
        {
            if (!string.IsNullOrEmpty(t.SURGERY_NAME) || t.SURGERY_BEGIN_TIME.HasValue)
            {
                surgTreatmentIds.Add(t.ID);
                surgeryDetailNames[t.ID] = t.SURGERY_NAME ?? "Phẫu thuật theo hồ sơ";
            }
        }

        // 2. Kiểm tra phiếu chỉ định PTTT (ServiceReq loại 4)
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
                        surgTreatmentIds.Add(req.TREATMENT_ID);
                        if (!surgeryDetailNames.ContainsKey(req.TREATMENT_ID))
                        {
                            surgeryDetailNames[req.TREATMENT_ID] = "Y lệnh PTTT: " + req.EXECUTE_ROOM_NAME;
                        }
                    }
                }
            }
        }

        var operatedCases = clavicleCases.Where(x => surgTreatmentIds.Contains(x.ID)).ToList();
        var nonOperatedCases = clavicleCases.Where(x => !surgTreatmentIds.Contains(x.ID)).ToList();

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("📊 TỔNG HỢP KẾT QUẢ: BỆNH NHÂN MỔ GÃY XƯƠNG ĐÒN NĂM 2025 - BẠCH MAI HÀ NỘI");
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("1. TỔNG SỐ BN GÃY XƯƠNG ĐÒN GHI NHẬN: {0} BN", clavicleCases.Count));
        Console.WriteLine(string.Format("2. SỐ BỆNH NHÂN ĐÃ ĐƯỢC PHẪU THUẬT (MỔ): {0} BN", operatedCases.Count));
        Console.WriteLine(string.Format("3. SỐ BỆNH NHÂN ĐIỀU TRỊ BẢO TỒN / BĂNG SỐ 8 / KHÁM NGOẠI TRÚ: {0} BN", nonOperatedCases.Count));

        // Phân loại vị trí gãy xương đòn
        int count13Giua = 0;
        int count13Ngoai = 0;
        int count13Trong = 0;
        int countChung = 0;

        foreach (var t in operatedCases)
        {
            string diag = ((t.ICD_NAME ?? "") + " " + (t.ICD_TEXT ?? "")).ToLower();
            if (diag.Contains("1/3 giữa") || diag.Contains("1/3 giua") || diag.Contains("1/3g") || diag.Contains("thân xương đòn")) count13Giua++;
            else if (diag.Contains("1/3 ngoài") || diag.Contains("1/3 ngoai") || diag.Contains("đầu ngoài") || diag.Contains("cùng đòn")) count13Ngoai++;
            else if (diag.Contains("1/3 trong") || diag.Contains("1/3 trong") || diag.Contains("đầu trong")) count13Trong++;
            else countChung++;
        }

        Console.WriteLine("\n--- PHÂN BỐ VỊ TRÍ GÃY Ở CÁC CA ĐÃ MỔ ---");
        Console.WriteLine(string.Format("  • Gãy 1/3 giữa xương đòn: {0} ca (phổ biến nhất, kết hợp xương nẹp vít/nẹp khóa)", count13Giua));
        Console.WriteLine(string.Format("  • Gãy 1/3 ngoài / đầu ngoài / kèm trật khớp cùng đòn: {0} ca (nẹp móc/chỉ siêu bền)", count13Ngoai));
        Console.WriteLine(string.Format("  • Gãy 1/3 trong / đầu trong xương đòn: {0} ca", count13Trong));
        Console.WriteLine(string.Format("  • Gãy xương đòn chung (chưa phân đoạn): {0} ca", countChung));

        // Phân bố theo Khoa điều trị các ca mổ
        Console.WriteLine("\n--- PHÂN BỐ KHOA ĐIỀU TRỊ (CÁC CA MỔ) ---");
        var deptGroups = operatedCases.GroupBy(x => x.END_DEPARTMENT_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var dg in deptGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} BN", dg.Key, dg.Count()));
        }

        // Danh sách mẫu các ca mổ xương đòn tiêu biểu
        Console.WriteLine("\n--- DANH SÁCH CHI TIẾT CÁC CA MỔ GÃY XƯƠNG ĐÒN (MẪU) ---");
        foreach (var t in operatedCases.Take(12))
        {
            string inDate = t.IN_TIME.ToString();
            if (inDate.Length == 14) inDate = inDate.Substring(6, 2) + "/" + inDate.Substring(4, 2) + "/" + inDate.Substring(0, 4);
            string surgDesc = surgeryDetailNames.ContainsKey(t.ID) ? surgeryDetailNames[t.ID] : "Đã phẫu thuật";

            Console.WriteLine(string.Format("  Mã BA: {0} | BN: {1} ({2}t - {3}) | Vào: {4} | Khoa: {5} | ICD: [{6}] {7} | PT: {8}",
                t.TREATMENT_CODE, t.TDL_PATIENT_NAME,
                t.TDL_PATIENT_DOB > 0 ? (2025 - int.Parse(t.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "?",
                t.TDL_PATIENT_GENDER_NAME, inDate, t.END_DEPARTMENT_NAME, t.ICD_CODE, t.ICD_NAME, surgDesc));
        }
    }
}
