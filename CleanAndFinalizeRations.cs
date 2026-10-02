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
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapterClean : AdapterBase
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

public class CleanAndFinalizeRations
{
    public static MyAdapterClean adapter = new MyAdapterClean();

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

    public static List<RationServiceSDO> BuildRationServices(string comboType)
    {
        var list = new List<RationServiceSDO>();
        long ptId = 42; // Viện phí

        if (comboType == "DD01")
        {
            list.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else if (comboType == "TM01")
        {
            list.Add(new RationServiceSDO { ServiceId = 30117, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30093, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30094, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else
        {
            list.Add(new RationServiceSDO { ServiceId = 30073, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30153, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30154, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        return list;
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

        // Kích hoạt phòng làm việc
        try
        {
            long[] rooms = new long[] { 931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267 };
            var wi = new WorkInfoSDO { Rooms = rooms.Select(r => new RoomSDO { RoomId = r }).ToList() };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, wi, cp);
        }
        catch { }

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
        Console.WriteLine("DỌN SẠCH & CHUẨN HÓA DUY NHẤT 3 SUẤT ĂN NGÀY 30/09 CHO THS.BS NGUYỄN HỮU SÂM (034727)");
        Console.WriteLine("=========================================================================================================\n");

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];
            string name = pNames[i];

            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, new HisTreatmentViewFilter { ID = tId }, cp);
            if (trList == null || trList.Count == 0) continue;
            var tr = trList[0];

            // Lấy tất cả y lệnh
            var orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, new HisServiceReqViewFilter { TREATMENT_ID = tId }, cp);
            if (orders == null) orders = new List<V_HIS_SERVICE_REQ>();

            var r30 = orders.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.INTRUCTION_TIME <= 20260930235959 && r.SERVICE_REQ_TYPE_ID == 8).ToList();

            // 1. Hủy bỏ tất cả các phiếu không phải của 034727
            var notSam = r30.Where(r => r.REQUEST_LOGINNAME != "034727" && r.SERVICE_REQ_STT_ID == 1).ToList();
            foreach (var ns in notSam)
            {
                var pDel = new CommonParam();
                adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = ns.ID }, pDel);
                Console.WriteLine(string.Format("   🗑️ Đã xóa phiếu của BS {0} ({1}): Mã {2}", ns.REQUEST_USERNAME, ns.REQUEST_LOGINNAME, ns.SERVICE_REQ_CODE));
            }

            // 2. Lấy lại các phiếu của 034727
            var samOrders = r30.Where(r => r.REQUEST_LOGINNAME == "034727").OrderBy(r => r.ID).ToList();

            // Nếu nhiều hơn 3 phiếu (bị trùng do chạy nhiều lần), giữ 3 phiếu mới nhất, xóa các phiếu thừa
            if (samOrders.Count > 3)
            {
                var toKeep = samOrders.OrderByDescending(r => r.ID).Take(3).Select(r => r.ID).ToList();
                var toRemove = samOrders.Where(r => !toKeep.Contains(r.ID) && r.SERVICE_REQ_STT_ID == 1).ToList();
                foreach (var trm in toRemove)
                {
                    var pDel = new CommonParam();
                    adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = trm.ID }, pDel);
                    Console.WriteLine(string.Format("   🗑️ Đã xóa phiếu trùng của 034727: Mã {0}", trm.SERVICE_REQ_CODE));
                }
            }
            else if (samOrders.Count < 3)
            {
                // Nếu chưa đủ 3 phiếu (như BN Trần Thị Lượng), tạo mới
                var sdo = new HisRationServiceReqSDO
                {
                    TreatmentIds = new List<long> { tId },
                    InstructionTimes = new List<long> { 20260930060000 },
                    RequestRoomId = 5248,
                    RequestLoginName = "034727",
                    RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME,
                    IcdSubCode = tr.ICD_SUB_CODE,
                    IcdText = tr.ICD_TEXT,
                    HalfInFirstDay = false,
                    IsForAutoCreateRation = false,
                    IsForHomie = false,
                    RationServices = BuildRationServices("BT01")
                };
                var pCreate = new CommonParam();
                adapter.PostData<object>("api/HisServiceReq/RationCreate", consumer, sdo, pCreate);
            }

            // 3. In kết quả cuối cùng
            var finalOrders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, new HisServiceReqViewFilter { TREATMENT_ID = tId }, cp);
            var final30 = finalOrders != null ? finalOrders.Where(r => r.INTRUCTION_TIME >= 20260930000000 && r.INTRUCTION_TIME <= 20260930235959 && r.SERVICE_REQ_TYPE_ID == 8).OrderBy(r => r.ID).ToList() : new List<V_HIS_SERVICE_REQ>();

            Console.WriteLine(string.Format("👉 [{0:D2}/12] BN: {1} ({2}) - {3} phiếu suất ăn 30/09:", i + 1, name, tr.TDL_PATIENT_CODE, final30.Count));
            foreach (var r in final30)
            {
                Console.WriteLine(string.Format("   ✔ Mã phiếu: {0} | Giờ: {1} | BS Chỉ định: {2} ({3})",
                    r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        Console.WriteLine("\n🎉 HOÀN TẤT 100%!");
    }
}
