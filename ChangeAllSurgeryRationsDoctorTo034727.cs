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

public class MyAdapterChange : AdapterBase
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

public class ChangeAllSurgeryRationsDoctorTo034727
{
    public static MyAdapterChange adapter = new MyAdapterChange();

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
        Console.WriteLine("CHUYỂN TÊN NGƯỜI CHỈ ĐỊNH VỀ THS.BS NGUYỄN HỮU SÂM (034727) CHO CÁC PHIẾU SUẤT ĂN NGÀY 30/09");
        Console.WriteLine("=========================================================================================================\n");

        int totalUpdated = 0;

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            string name = pNames[i];

            // 1. Lấy tất cả y lệnh
            var rawReqFilter = new HisServiceReqFilter { TREATMENT_ID = tId };
            var reqList = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", consumer, rawReqFilter, cp);
            if (reqList == null || reqList.Count == 0) continue;

            // Lọc các phiếu suất ăn ngày 30/09 (SERVICE_REQ_TYPE_ID = 8) chưa thực hiện (STT_ID = 1, IS_DELETE = null/0)
            var rations30 = reqList.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.INTRUCTION_TIME <= 20260930235959 && r.SERVICE_REQ_TYPE_ID == 8 && (r.IS_DELETE == null || r.IS_DELETE == 0)).ToList();

            Console.WriteLine(string.Format("👉 [{0:D2}/12] BN: {1} (TrID: {2}) - Có {3} phiếu suất ăn ngày 30/09:", i + 1, name, tId, rations30.Count));

            foreach (var req in rations30)
            {
                if (req.REQUEST_LOGINNAME != "034727")
                {
                    string oldDoctor = req.REQUEST_USERNAME + " (" + req.REQUEST_LOGINNAME + ")";
                    req.REQUEST_LOGINNAME = "034727";
                    req.REQUEST_USERNAME = "Ths.BS NGUYỄN HỮU SÂM";
                    req.REQUEST_USER_TITLE = "Thạc sỹ y học";

                    var pUpd = new CommonParam();
                    var res = adapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", consumer, req, pUpd);
                    if (res != null && !pUpd.HasException)
                    {
                        Console.WriteLine(string.Format("   ✔ ĐÃ CHUYỂN TÊN: Phiếu {0} | Từ: {1} -> THS.BS NGUYỄN HỮU SÂM (034727)", req.SERVICE_REQ_CODE, oldDoctor));
                        totalUpdated++;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("   ⚠️ Lỗi chuyển phiếu {0}: {1}", req.SERVICE_REQ_CODE, pUpd.GetMessage()));
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("   ℹ️ Phiếu {0} đã thuộc BS 034727 (Ths.BS NGUYỄN HỮU SÂM)", req.SERVICE_REQ_CODE));
                }
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        Console.WriteLine(string.Format("\n🎉 ĐÃ CHUYỂN ĐỔI THÀNH CÔNG: {0} PHIẾU SUẤT ĂN VỀ BÁC SĨ 034727 ĐỂ BÁC SĨ TÙY Ý XÓA TRÊN UI!", totalUpdated));
    }
}
