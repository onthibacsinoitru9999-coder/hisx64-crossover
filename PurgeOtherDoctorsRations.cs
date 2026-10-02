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

public class MyAdapterPurge : AdapterBase
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

public class PurgeOtherDoctorsRations
{
    public static MyAdapterPurge adapter = new MyAdapterPurge();

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
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không có TokenCode!"); return; }

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
        Console.WriteLine("DỌN SẠCH TẤT CẢ CÁC PHIẾU SUẤT ĂN KHÔNG PHẢI CỦA THS.BS NGUYỄN HỮU SÂM (034727) TỪ NGÀY 30/09 TRỞ ĐI");
        Console.WriteLine("=========================================================================================================\n");

        int totalDeleted = 0;

        foreach (var pCode in patientCodes)
        {
            var trFilter = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, trFilter, cp);
            if (trList == null || trList.Count == 0) continue;

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();
            long tId = tr.ID;

            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_ID = tId,
                SERVICE_REQ_TYPE_ID = 8 // Suất ăn
            };
            var reqList = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            if (reqList == null || reqList.Count == 0) continue;

            // Tìm các phiếu từ ngày 30/09 trở đi (>= 20260930000000) mà KHÔNG PHẢI của 034727 hoặc bị trùng
            var toDelete = reqList.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.REQUEST_LOGINNAME != "034727" && r.SERVICE_REQ_STT_ID == 1).ToList();

            if (toDelete.Count > 0)
            {
                Console.WriteLine(string.Format("👉 BN: {0} ({1}) - Tìm thấy {2} phiếu suất ăn cần xóa:", tr.TDL_PATIENT_NAME, pCode, toDelete.Count));
                foreach (var req in toDelete)
                {
                    var pDel = new CommonParam();
                    var delRes = adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = req.ID }, pDel);
                    if (delRes)
                    {
                        Console.WriteLine(string.Format("   ✔ ĐÃ HỦY: Mã {0} | Giờ: {1} | BS cũ: {2} ({3})", 
                            req.SERVICE_REQ_CODE, req.INTRUCTION_TIME, req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                        totalDeleted++;
                    }
                    else
                    {
                        // Thử xóa theo SereServ
                        var ssFilter = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
                        var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssFilter, pDel);
                        if (ssList != null)
                        {
                            foreach (var ss in ssList)
                            {
                                adapter.PostData<bool>("api/HisSereServ/Delete", consumer, new HisSereServFilter { ID = ss.ID }, pDel);
                            }
                            Console.WriteLine(string.Format("   ✔ ĐÃ HỦY CHI TIẾT DỊCH VỤ: Mã {0}", req.SERVICE_REQ_CODE));
                            totalDeleted++;
                        }
                    }
                }
            }
        }

        Console.WriteLine(string.Format("\n🎉 ĐÃ HỦY THÀNH CÔNG TỔNG CỘNG: {0} PHIẾU SUẤT ĂN CŨ CỦA BÁC SĨ KHÁC!", totalDeleted));
    }
}
