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
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        Console.WriteLine("🔑 Live Token: " + token.Substring(0, 16) + "...");
        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        Console.WriteLine("=== ĐANG TRUY VẤN BỆNH NHÂN GÃY XƯƠNG ĐÙI NĂM 2025 (BẠCH MAI HÀ NỘI) ===");

        // Thu thập tất cả lượt điều trị khớp với nhóm S72
        // Thử các mã: S72, S72.0, S72.1, S72.2, S72.3, S72.4, S72.7, S72.8, S72.9
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
            tf.BRANCH_ID = 1; // Bạch Mai Hà Nội
            param = new CommonParam();

            var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            int countAdded = 0;
            if (list != null)
            {
                foreach (var t in list)
                {
                    if (!allTreatments.ContainsKey(t.ID))
                    {
                        allTreatments[t.ID] = t;
                        countAdded++;
                    }
                }
            }
            Console.WriteLine(string.Format("Mã ICD '{0}': lấy được {1} hồ sơ (mới: {2})", 
                code, list != null ? list.Count : 0, countAdded));
        }

        Console.WriteLine(string.Format("\n👉 Tổng cộng hồ sơ chẩn đoán S72 tại BM Hà Nội năm 2025: {0}", allTreatments.Count));

        // Phân tích trạng thái phẫu thuật (mổ)
        var listAll = allTreatments.Values.ToList();

        // 1. Phân loại theo SURGERY_NAME trên Treatment
        var withSurgName = listAll.Where(x => !string.IsNullOrEmpty(x.SURGERY_NAME) || x.SURGERY_BEGIN_TIME.HasValue).ToList();
        var withoutSurgName = listAll.Where(x => string.IsNullOrEmpty(x.SURGERY_NAME) && !x.SURGERY_BEGIN_TIME.HasValue).ToList();

        Console.WriteLine(string.Format("  - Có ghi nhận phẫu thuật trực tiếp trên hồ sơ bệnh án (SURGERY_NAME): {0}", withSurgName.Count));
        Console.WriteLine(string.Format("  - Chưa có ghi nhận SURGERY_NAME trên hồ sơ bệnh án: {0}", withoutSurgName.Count));

        // Kiểm tra xem những ca "chưa có SURGERY_NAME" có chỉ định PTTT (SERVICE_REQ_TYPE_ID = 4) không
        List<long> extraSurgeryTreatmentIds = new List<long>();
        Dictionary<long, string> extraSurgNames = new Dictionary<long, string>();

        if (withoutSurgName.Count > 0)
        {
            Console.WriteLine("\nĐang kiểm tra thêm phiếu chỉ định PTTT (ServiceReq) cho các ca còn lại...");
            var withoutIds = withoutSurgName.Select(x => x.ID).ToList();

            // Chia batch 50 để query ServiceReq
            for (int i = 0; i < withoutIds.Count; i += 50)
            {
                var batchIds = withoutIds.Skip(i).Take(50).ToList();
                var srf = new HisServiceReqViewFilter
                {
                    TREATMENT_IDs = batchIds,
                    SERVICE_REQ_TYPE_ID = 4 // PTTT
                };
                param = new CommonParam();
                var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                if (srs != null && srs.Count > 0)
                {
                    // Lọc những PTTT thực sự (không phải thủ thuật nhỏ như bó bột/thay băng nếu có)
                    foreach (var req in srs)
                    {
                        if (req.SERVICE_REQ_STT_ID == 3 || req.SERVICE_REQ_STT_ID == 2) // đã thực hiện / đang xử lý
                        {
                            if (!extraSurgeryTreatmentIds.Contains(req.TREATMENT_ID))
                            {
                                extraSurgeryTreatmentIds.Add(req.TREATMENT_ID);
                                extraSurgNames[req.TREATMENT_ID] = req.SERVICE_REQ_CODE + " - " + req.EXECUTE_ROOM_NAME;
                            }
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("  - Tìm thấy thêm {0} ca có y lệnh PTTT được thực hiện!", extraSurgeryTreatmentIds.Count));
        }

        // Tổng hợp tất cả các ca MỔ
        HashSet<long> allSurgeryTreatmentIds = new HashSet<long>(withSurgName.Select(x => x.ID));
        foreach (var id in extraSurgeryTreatmentIds) allSurgeryTreatmentIds.Add(id);

        var surgeryCases = listAll.Where(x => allSurgeryTreatmentIds.Contains(x.ID)).ToList();
        var nonSurgeryCases = listAll.Where(x => !allSurgeryTreatmentIds.Contains(x.ID)).ToList();

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("📊 TỔNG HỢP KẾT QUẢ: BỆNH NHÂN MỔ GÃY XƯƠNG ĐÙI NĂM 2025 - BẠCH MAI HÀ NỘI");
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("1. TỔNG SỐ BN GÃY XƯƠNG ĐÙI ĐIỀU TRỊ NỘI TRÚ/NGOẠI TRÚ NĂM 2025: {0}", listAll.Count));
        Console.WriteLine(string.Format("2. TỔNG SỐ BN ĐÃ ĐƯỢC PHẪU THUẬT (MỔ): {0} BN", surgeryCases.Count));
        Console.WriteLine(string.Format("3. SỐ BN ĐIỀU TRỊ BẢO TỒN / KHÔNG MỔ / CHUYỂN VIỆN: {0} BN", nonSurgeryCases.Count));

        // Phân loại theo Khoa điều trị
        Console.WriteLine("\n--- PHÂN BỐ THEO KHOA ĐIỀU TRỊ (CÁC CA ĐÃ MỔ) ---");
        var deptGroups = surgeryCases.GroupBy(x => x.END_DEPARTMENT_NAME ?? "Không xác định").OrderByDescending(g => g.Count());
        foreach (var dg in deptGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} BN", dg.Key, dg.Count()));
        }

        // Phân loại theo Vị trí gãy xương đùi (Mã ICD)
        Console.WriteLine("\n--- PHÂN BỐ THEO VỊ TRÍ GÃY (MÃ ICD) (CÁC CA ĐÃ MỔ) ---");
        var icdGroups = surgeryCases.GroupBy(x => x.ICD_CODE ?? "Khác").OrderByDescending(g => g.Count());
        foreach (var ig in icdGroups)
        {
            var first = ig.First();
            Console.WriteLine(string.Format("  • [{0}] {1}: {2} BN", ig.Key, first.ICD_NAME, ig.Count()));
        }

        // Một số tên phẫu thuật tiêu biểu
        Console.WriteLine("\n--- CÁC LOẠI PHẪU THUẬT PHỔ BIẾN ---");
        var surgNameGroups = surgeryCases.Where(x => !string.IsNullOrEmpty(x.SURGERY_NAME))
                                         .GroupBy(x => x.SURGERY_NAME.Trim())
                                         .OrderByDescending(g => g.Count())
                                         .Take(10);
        foreach (var sg in surgNameGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} ca", sg.Key, sg.Count()));
        }

        // Danh sách mẫu 5 ca gần nhất
        Console.WriteLine("\n--- MẪU 5 CA PHẪU THUẬT GẦN NHẤT ---");
        var sample = surgeryCases.OrderByDescending(x => x.IN_TIME).Take(5);
        foreach (var s in sample)
        {
            string inDate = s.IN_TIME.ToString();
            if (inDate.Length == 14) inDate = inDate.Substring(6, 2) + "/" + inDate.Substring(4, 2) + "/" + inDate.Substring(0, 4);
            Console.WriteLine(string.Format("  Mã BA: {0} | BN: {1} ({2}) | Vào viện: {3} | ICD: [{4}] {5} | PT: {6}",
                s.TREATMENT_CODE, s.TDL_PATIENT_NAME, s.TDL_PATIENT_GENDER_NAME, inDate, s.ICD_CODE, s.ICD_NAME, s.SURGERY_NAME ?? "Có phiếu PTTT"));
        }
    }
}
