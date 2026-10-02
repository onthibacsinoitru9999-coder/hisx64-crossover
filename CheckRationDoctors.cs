using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapterChk : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class CheckRationDoctors
{
    public static MyAdapterChk adapter = new MyAdapterChk();

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
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        string[] patientCodes = new string[]
        {
            "0004062838", // THÌN
            "0001530113", // HÒA
            "0004079114", // TƯ
            "0004050211", // THIỆN
            "0004089090", // TRUNG
            "0002840646", // BỎNG
            "0004081634", // THU
            "0003837595", // NGỌC
            "0004079203", // CHỦ
            "0004067971", // THẢO
            "0003863470", // LƯỢNG
            "0002740977"  // PHƯƠNG
        };

        Console.WriteLine("=========================================================================================================");
        Console.WriteLine("KIỂM TRA CÁC Y LỆNH SUẤT ĂN NGÀY 30/09/2026 CỦA 12 BỆNH NHÂN");
        Console.WriteLine("=========================================================================================================");

        foreach (var pCode in patientCodes)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE = pCode };
            // Let's use HisServiceReqViewFilter
            var srf = new HisServiceReqViewFilter
            {
                TDL_PATIENT_CODE = pCode,
                SERVICE_REQ_TYPE_ID = 8 // Suất ăn (RATION)
            };
            var sreqs = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, new HisSereServViewFilter { TDL_PATIENT_CODE = pCode }, cp);
            
            // Query ServiceReq
            var srf2 = new HisServiceReqViewFilter { TDL_PATIENT_CODE = pCode };
            var listReq = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf2, cp);
            
            Console.WriteLine(string.Format("\n👉 BN Mã: {0}", pCode));
            if (listReq == null || listReq.Count == 0)
            {
                Console.WriteLine("   (Không có ServiceReq nào)");
                continue;
            }

            var rations = listReq.Where(r => r.SERVICE_REQ_TYPE_ID == 8 || (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn")) || (r.INSTRUCTION_TIME.ToString().StartsWith("20260930"))).ToList();
            var rations30 = listReq.Where(r => r.INSTRUCTION_TIME.ToString().StartsWith("20260930") && r.SERVICE_REQ_TYPE_ID == 8).ToList();
            
            Console.WriteLine(string.Format("   Tổng số ServiceReq suất ăn ngày 30/09: {0}", rations30.Count));
            foreach (var r in rations30)
            {
                Console.WriteLine(string.Format("   - Phiếu: {0} | Giờ: {1} | BS Chỉ định: {2} ({3}) | Người tạo: {4} ({5}) | Khoa/Phòng: {6}",
                    r.SERVICE_REQ_CODE, r.INSTRUCTION_TIME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.CREATOR, r.MODIFIER, r.REQUEST_ROOM_NAME));
            }

            // In tất cả y lệnh ngày 30/9 nếu không tìm thấy suất ăn type 8
            if (rations30.Count == 0)
            {
                var all30 = listReq.Where(r => r.INSTRUCTION_TIME.ToString().StartsWith("20260930")).ToList();
                Console.WriteLine(string.Format("   Các y lệnh khác ngày 30/09 (Total: {0}):", all30.Count));
                foreach (var r in all30)
                {
                    Console.WriteLine(string.Format("     + Type: {0} ({1}) | Mã: {2} | BS: {3} ({4})", r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_TYPE_NAME, r.SERVICE_REQ_CODE, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME));
                }
            }
        }
    }
}
