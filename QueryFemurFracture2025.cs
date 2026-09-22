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

        // 1. Kiểm tra mã ICD S72 trên HIS
        Console.WriteLine("\n--- THỬ NGHIỆM 1: Query bằng ICD_CODE_OR_ICD_SUB_CODE = S72 ---");
        var tf1 = new HisTreatmentViewFilter();
        tf1.IN_TIME_FROM = 20250101000000;
        tf1.IN_TIME_TO   = 20251231235959;
        tf1.ICD_CODE_OR_ICD_SUB_CODE = "S72";
        param.Limit = 10;
        var list1 = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf1, param);
        Console.WriteLine("Kết quả Test 1: Count={0}, ParamCount={1}", list1 != null ? list1.Count : -1, param.Count);

        // 2. Thử các sub-code phổ biến của S72
        string[] subCodes = new string[] { "S72.0", "S72.1", "S72.2", "S72.3", "S72.4", "S72.7", "S72.8", "S72.9" };
        foreach (var sc in subCodes)
        {
            var tf = new HisTreatmentViewFilter();
            tf.IN_TIME_FROM = 20250101000000;
            tf.IN_TIME_TO   = 20251231235959;
            tf.ICD_CODE_OR_ICD_SUB_CODE = sc;
            param = new CommonParam();
            param.Limit = 1;
            var res = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            Console.WriteLine("SubCode {0}: Count={1}, ParamCount={2}", sc, res != null ? res.Count : -1, param.Count);
        }

        // 3. Thử ICD_CODE_OR_ICD_SUB_CODEs (danh sách)
        Console.WriteLine("\n--- THỬ NGHIỆM 3: Query bằng danh sách ICD_CODE_OR_ICD_SUB_CODEs ---");
        var tf3 = new HisTreatmentViewFilter();
        tf3.IN_TIME_FROM = 20250101000000;
        tf3.IN_TIME_TO   = 20251231235959;
        tf3.ICD_CODE_OR_ICD_SUB_CODEs = new List<string> { "S72", "S72.0", "S72.1", "S72.2", "S72.3", "S72.4", "S72.7", "S72.8", "S72.9" };
        param = new CommonParam();
        param.Limit = 5;
        var list3 = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf3, param);
        Console.WriteLine("Kết quả Test 3: Count={0}, ParamCount={1}", list3 != null ? list3.Count : -1, param.Count);
        if (list3 != null && list3.Count > 0)
        {
            foreach (var t in list3)
            {
                Console.WriteLine("  Mã: {0} | {1} | ICD: {2} ({3}) | Phẫu thuật: {4}",
                    t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.ICD_CODE, t.ICD_NAME, t.SURGERY_NAME);
            }
        }
    }
}
