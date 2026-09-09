using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
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
    static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            return chunk.Substring(start, 64);
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    static void Main()
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
            return null;
        };

        string token = ReadLiveToken();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var param = new CommonParam();
        var adapter = new MyAdapter();

        long[] treatmentIds = new long[] { 7163469, 7163700, 7163879, 7136089, 7149475 };
        string[] names = new string[] { 
            "VŨ ĐỨC BÌNH (P714 - G17)", 
            "NGUYỄN VĂN THƠ (P714 - G18)", 
            "VI TRUNG HIẾU (P717 - G7)", 
            "NGUYỄN XUÂN CHÍN (P717 - G8)", 
            "NGUYỄN VĂN TRỌNG (P717 - G9)" 
        };

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🏥 BỆNH NHÂN: {0} (TreatmentID: {1})", names[i], tId));
            Console.WriteLine("===============================================================================");

            var medFilter = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId };
            var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, medFilter, param);

            if (allMeds == null || allMeds.Count == 0)
            {
                Console.WriteLine("  ❌ Chưa từng có đơn thuốc nào được xuất/kê trên hệ thống.");
                Console.WriteLine();
                continue;
            }

            // Group by date (yyyyMMdd)
            var grp = allMeds.GroupBy(x => {
                long t = x.TDL_INTRUCTION_TIME ?? x.EXP_TIME ?? x.CREATE_TIME ?? 0;
                string s = t.ToString();
                return s.Length >= 8 ? s.Substring(0, 8) : "Khac";
            }).OrderByDescending(g => g.Key);

            foreach (var g in grp)
            {
                string dKey = g.Key;
                string dateFormatted = dKey.Length == 8 ? string.Format("{0}/{1}/{2}", dKey.Substring(6, 2), dKey.Substring(4, 2), dKey.Substring(0, 4)) : dKey;
                Console.WriteLine(string.Format("\n  📅 NGÀY {0} (Tổng cộng {1} loại thuốc):", dateFormatted, g.Count()));

                foreach (var m in g)
                {
                    string timing = string.Format("S:{0}|Tr:{1}|Ch:{2}|T:{3}", m.MORNING ?? "-", m.NOON ?? "-", m.AFTERNOON ?? "-", m.EVENING ?? "-");
                    Console.WriteLine(string.Format("     • {0} | SL: {1:0.##} {2} | Liều: [{3}] | Kho: {4}", 
                        m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, timing, m.MEDI_STOCK_NAME));
                    if (!string.IsNullOrEmpty(m.TUTORIAL))
                    {
                        Console.WriteLine(string.Format("       HD: \"{0}\"", m.TUTORIAL));
                    }
                }
            }
            Console.WriteLine();
        }
    }
}
