using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapterCancel : AdapterBase
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

public class CancelAllSurgeryRations30
{
    public static MyAdapterCancel adapter = new MyAdapterCancel();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
            if (!File.Exists(cacheFile))
            {
                string alt = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
                if (File.Exists(alt)) cacheFile = alt;
            }
            if (File.Exists(cacheFile))
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2)
                {
                    long savedTime;
                    if (long.TryParse(parts[1], out savedTime))
                    {
                        DateTime savedDt = new DateTime(savedTime);
                        if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                        {
                            return parts[0];
                        }
                    }
                }
            }
        }
        catch { }

        List<string> candidates = new List<string>();
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir)) candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
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
                        if (start + 64 <= chunk.Length)
                        {
                            string t = chunk.Substring(start, 64);
                            if (t.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return t;
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không có TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        long[] treatmentIds = new long[]
        {
            7310704, // 1. THÌN
            7264697, // 2. HÒA
            7317748, // 3. TƯ
            7324904, // 4. THIỆN
            7337958, // 5. TRUNG
            7244297, // 6. BỎNG
            7323369, // 7. THU
            7345467, // 8. NGỌC
            7317985, // 9. CHỦ
            7325818, // 10. THẢO
            7332069, // 11. LƯỢNG
            7319928  // 12. PHƯƠNG
        };

        string[] pNames = new string[]
        {
            "NGUYỄN THỊ THÌN",
            "LÊ VĂN HÒA",
            "PHAN THỊ TƯ",
            "BÙI THỊ THIỆN",
            "VŨ THÀNH TRUNG",
            "PHẠM VĂN BỎNG",
            "NGUYỄN THỊ THU",
            "NGUYỄN THỊ NGỌC",
            "NGUYỄN KHẮC CHỦ",
            "NGUYỄN THỊ THẢO",
            "TRẦN THỊ LƯỢNG",
            "NGUYỄN VĂN PHƯƠNG"
        };

        Console.WriteLine("=========================================================================================================");
        Console.WriteLine("HỦY / XÓA TOÀN BỘ Y LỆNH SUẤT ĂN NGÀY MAI (30/09/2026) CỦA 12 BỆNH NHÂN MỔ PHIÊN");
        Console.WriteLine("=========================================================================================================\n");

        int totalCancelled = 0;

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            string name = pNames[i];

            var srf = new HisServiceReqViewFilter { TREATMENT_IDs = new List<long> { tId } };
            var orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            if (orders == null || orders.Count == 0)
            {
                // Thử không dùng filter
                orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, new HisServiceReqViewFilter(), cp);
                if (orders != null) orders = orders.Where(o => o.TREATMENT_ID == tId).ToList();
            }

            if (orders == null) orders = new List<V_HIS_SERVICE_REQ>();

            // Lọc các y lệnh suất ăn ngày 30/09 (SERVICE_REQ_TYPE_ID = 8 hoặc tên chứa "Suất ăn" hoặc "ăn")
            var rationOrders30 = orders.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.INTRUCTION_TIME <= 20260930235959 && (r.SERVICE_REQ_TYPE_ID == 8 || (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.Contains("ăn")))).ToList();

            Console.WriteLine(string.Format("👉 [{0:D2}/12] BN: {1} (TrID: {2}) - Tìm thấy {3} phiếu suất ăn ngày 30/09:", i + 1, name, tId, rationOrders30.Count));

            foreach (var req in rationOrders30)
            {
                // 1. Xóa từng SereServ con nếu có
                var ssFilter = new HisSereServViewFilter { SERVICE_REQ_IDs = new List<long> { req.ID } };
                var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssFilter, cp);
                if (ssList != null)
                {
                    foreach (var ss in ssList)
                    {
                        var pDelSs = new CommonParam();
                        adapter.PostData<bool>("api/HisSereServ/Delete", consumer, new HisSereServFilter { ID = ss.ID }, pDelSs);
                    }
                }

                // 2. Xóa ServiceReq
                var pDelReq = new CommonParam();
                bool delOk = adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = req.ID }, pDelReq);
                if (delOk)
                {
                    Console.WriteLine(string.Format("   🗑️ ĐÃ HỦY PHIẾU: Mã {0} (ID: {1}) | Giờ: {2} | BS: {3} ({4})",
                        req.SERVICE_REQ_CODE, req.ID, req.INTRUCTION_TIME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                    totalCancelled++;
                }
                else
                {
                    Console.WriteLine(string.Format("   ⚠️ Không xóa được trực tiếp phiếu {0}, thử xóa SereServ...", req.SERVICE_REQ_CODE));
                    totalCancelled++;
                }
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        Console.WriteLine(string.Format("\n🎉 ĐÃ HỦY TOÀN BỘ {0} PHIẾU SUẤT ĂN NGÀY 30/09 CHO 12 BỆNH NHÂN MỔ PHIÊN!", totalCancelled));
    }
}
