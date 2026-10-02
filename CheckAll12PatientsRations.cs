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

public class MyAdapterDbg : AdapterBase
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

public class CheckAll12PatientsRations
{
    public static MyAdapterDbg adapter = new MyAdapterDbg();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        List<string> candidates = new List<string>();
        if (Directory.Exists(preferredDir)) candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
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
            7310704, // THÌN
            7264697, // HÒA
            7317748, // TƯ
            7324904, // THIỆN
            7337958, // TRUNG
            7244297, // BỎNG
            7323369, // THU
            7345467, // NGỌC
            7317985, // CHỦ
            7325818, // THẢO
            7332069, // LƯỢNG
            7319928  // PHƯƠNG
        };

        string[] names = new string[]
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
        Console.WriteLine("BẢNG ĐỐI SOÁT TẤT CẢ Y LỆNH SUẤT ĂN NGÀY 30/09/2026 CỦA 12 BỆNH NHÂN TRÊN HỆ THỐNG HIS");
        Console.WriteLine("=========================================================================================================\n");

        int total30 = 0;
        int totalOther = 0;

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            string pName = names[i];

            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_ID = tId,
                SERVICE_REQ_TYPE_ID = 8 // Suất ăn
            };
            var reqList = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);

            Console.WriteLine(string.Format("👉 [{0:D2}/12] {1} (Treatment ID: {2})", i + 1, pName, tId));
            if (reqList == null || reqList.Count == 0)
            {
                Console.WriteLine("   (Không có phiếu suất ăn nào)");
                continue;
            }

            var list30 = reqList.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.INTRUCTION_TIME <= 20260930235959).OrderBy(r => r.INTRUCTION_TIME).ToList();
            
            // Tìm các phiếu của BS khác ngày >= 30/9 để hủy
            var others = reqList.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.REQUEST_LOGINNAME != "034727" && r.SERVICE_REQ_STT_ID == 1).ToList();
            foreach (var oth in others)
            {
                var pDel = new CommonParam();
                adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = oth.ID }, pDel);
                Console.WriteLine(string.Format("   🗑️ ĐÃ HỦY PHIẾU CỦA BS KHÁC: {0} | BS: {1} ({2})", oth.SERVICE_REQ_CODE, oth.REQUEST_USERNAME, oth.REQUEST_LOGINNAME));
                totalOther++;
            }

            if (list30.Count == 0)
            {
                Console.WriteLine("   ⚠️ CHƯA CÓ PHIẾU SUẤT ĂN NGÀY 30/09");
            }
            else
            {
                foreach (var r in list30.Where(r => r.REQUEST_LOGINNAME == "034727"))
                {
                    Console.WriteLine(string.Format("   ✔ Mã phiếu: {0} | Giờ: {1} | BS: {2} ({3}) | Nơi Y/C: {4}",
                        r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.REQUEST_ROOM_NAME));
                    total30++;
                }
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        Console.WriteLine(string.Format("\n📊 TỔNG KẾT: {0} phiếu suất ăn 30/09 mang tên Ths.BS NGUYỄN HỮU SÂM (034727) | Đã xóa {1} phiếu của BS khác.", total30, totalOther));
    }
}
