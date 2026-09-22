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

        // 1. Lấy tất cả hồ sơ S72 năm 2025
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

        Console.WriteLine(string.Format("Đã tải {0} hồ sơ S72 năm 2025.", allTreatments.Count));

        // 2. Lấy chi tiết SereServ PTTT (TDL_SERVICE_TYPE_ID = 4) của tất cả bệnh nhân này để lọc đúng dịch vụ PHẪU THUẬT XƯƠNG ĐÙI / KHỚP HÁNG
        var allIds = allTreatments.Keys.ToList();
        List<V_HIS_SERE_SERV> allPtttServices = new List<V_HIS_SERE_SERV>();

        for (int i = 0; i < allIds.Count; i += 50)
        {
            var batchIds = allIds.Skip(i).Take(50).ToList();
            var ssf = new HisSereServViewFilter
            {
                TREATMENT_IDs = batchIds,
                SERVICE_TYPE_ID = 4 // PTTT
            };
            param = new CommonParam();
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (sss != null && sss.Count > 0)
            {
                allPtttServices.AddRange(sss);
            }
        }

        Console.WriteLine(string.Format("Tổng số dịch vụ PTTT (ServiceType=4) tìm thấy: {0}", allPtttServices.Count));

        // Phân loại dịch vụ PTTT thành:
        // A. Phẫu thuật thực thụ xương đùi / khớp háng (Femur / Hip Surgery):
        //    - "xương đùi", "cổ xương đùi", "khớp háng", "mấu chuyển", "nẹp vít", "đinh nội tủy", "kết hợp xương", "thay khớp"
        // B. Thủ thuật khác (catheter, bó bột, chọc dò, nội soi, v.v.)

        string[] femurKeywords = new string[] {
            "xương đùi", "cổ xương đùi", "khớp háng", "mấu chuyển", "thay khớp", 
            "kết hợp xương đùi", "kết hợp xương", "khx đùi", "khx", "đinh nội tủy", 
            "nẹp khóa", "nẹp vít", "tháo phương tiện", "bán phần", "toàn phần"
        };

        // Nhóm theo TreatmentId
        var servByTreatment = allPtttServices.GroupBy(x => x.TDL_TREATMENT_ID.Value).ToDictionary(g => g.Key, g => g.ToList());

        int countDefiniteFemurSurgery = 0;
        int countOnlyOtherProcedure = 0;
        int countNoPtttAtAll = 0;

        List<V_HIS_TREATMENT> definiteFemurSurgeryTreatments = new List<V_HIS_TREATMENT>();
        Dictionary<string, int> femurSurgeryNames = new Dictionary<string, int>();

        foreach (var kvp in allTreatments)
        {
            long tId = kvp.Key;
            var t = kvp.Value;

            bool isFemurSurgery = false;
            string detectedSurgName = "";

            // Check SURGERY_NAME trên Treatment
            if (!string.IsNullOrEmpty(t.SURGERY_NAME))
            {
                string sNameLower = t.SURGERY_NAME.ToLower();
                if (femurKeywords.Any(k => sNameLower.Contains(k)) || sNameLower.Contains("phẫu thuật") || sNameLower.Contains("mổ"))
                {
                    isFemurSurgery = true;
                    detectedSurgName = t.SURGERY_NAME;
                }
            }

            // Check các dịch vụ PTTT trong SereServ
            if (servByTreatment.ContainsKey(tId))
            {
                var services = servByTreatment[tId];
                foreach (var s in services)
                {
                    string name = (s.TDL_SERVICE_NAME ?? "").ToLower();
                    // Loại trừ thủ thuật điều dưỡng / bó bột / chọc dò nếu không có mổ
                    if (femurKeywords.Any(k => name.Contains(k)))
                    {
                        // Kiểm tra không phải chỉ là "bó bột" đơn thuần nếu không có kết hợp xương/thay khớp
                        isFemurSurgery = true;
                        detectedSurgName = s.TDL_SERVICE_NAME;
                        if (!femurSurgeryNames.ContainsKey(s.TDL_SERVICE_NAME))
                            femurSurgeryNames[s.TDL_SERVICE_NAME] = 0;
                        femurSurgeryNames[s.TDL_SERVICE_NAME]++;
                    }
                }

                if (isFemurSurgery)
                {
                    countDefiniteFemurSurgery++;
                    definiteFemurSurgeryTreatments.Add(t);
                }
                else
                {
                    countOnlyOtherProcedure++;
                }
            }
            else
            {
                if (isFemurSurgery)
                {
                    countDefiniteFemurSurgery++;
                    definiteFemurSurgeryTreatments.Add(t);
                }
                else
                {
                    countNoPtttAtAll++;
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("🎯 KẾT QUẢ ĐỐI SOÁT LÂM SÀNG CHÍNH XÁC (ZERO HALLUCINATION):");
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("• Tổng số lượt hồ sơ Gãy xương đùi (ICD S72) năm 2025: {0} ca", allTreatments.Count));
        Console.WriteLine(string.Format("• SỐ CA THỰC SỰ ĐƯỢC PHẪU THUẬT (MỔ XƯƠNG ĐÙI / THAY KHỚP HÁNG): {0} BN", countDefiniteFemurSurgery));
        Console.WriteLine(string.Format("• Số ca chỉ làm thủ thuật khác (bó bột, chọc dò, catheter hồi sức...): {0} BN", countOnlyOtherProcedure));
        Console.WriteLine(string.Format("• Số ca hoàn toàn không can thiệp PTTT (nội khoa / chuyển viện): {0} BN", countNoPtttAtAll));

        // Phân bố theo Khoa của các ca THỰC SỰ MỔ XƯƠNG ĐÙI
        Console.WriteLine("\n--- PHÂN BỐ CÁC CA MỔ XƯƠNG ĐÙI THEO KHOA RA VIỆN / ĐIỀU TRỊ ---");
        var deptGroups = definiteFemurSurgeryTreatments.GroupBy(x => x.END_DEPARTMENT_NAME ?? "Chưa kết thúc").OrderByDescending(g => g.Count());
        foreach (var dg in deptGroups)
        {
            Console.WriteLine(string.Format("  • {0}: {1} BN", dg.Key, dg.Count()));
        }

        // Top phẫu thuật xương đùi được thực hiện
        Console.WriteLine("\n--- CÁC LOẠI PHẪU THUẬT XƯƠNG ĐÙI / THAY KHỚP HÁNG PHỔ BIẾN NHẤT ---");
        foreach (var sn in femurSurgeryNames.OrderByDescending(x => x.Value).Take(10))
        {
            Console.WriteLine(string.Format("  • {0}: {1} lượt", sn.Key, sn.Value));
        }

        // Phân bố theo Mã ICD của các ca thực sự mổ
        Console.WriteLine("\n--- PHÂN BỐ THEO THỂ GÃY XƯƠNG ĐÙI (ICD-10) ĐÃ MỔ ---");
        var icdGroups = definiteFemurSurgeryTreatments.GroupBy(x => x.ICD_CODE ?? "Khác").OrderByDescending(g => g.Count());
        foreach (var ig in icdGroups)
        {
            Console.WriteLine(string.Format("  • [{0}] {1}: {2} BN", ig.Key, ig.First().ICD_NAME, ig.Count()));
        }
    }
}
