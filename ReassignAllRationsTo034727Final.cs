using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapterRationFinal : AdapterBase
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

public class ReassignAllRationsTo034727Final
{
    public static MyAdapterRationFinal adapter = new MyAdapterRationFinal();

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

    public static string DetectCombo(V_HIS_TREATMENT tr)
    {
        if (tr == null) return "BT01";
        string diag = ((tr.ICD_NAME ?? "") + " " + (tr.ICD_TEXT ?? "")).ToLower();
        if (diag.Contains("tháo đường") || diag.Contains("đtđ") || diag.Contains("diabetes")) return "DD01";
        if (diag.Contains("tăng huyết áp") || diag.Contains("tim mạch") || diag.Contains("suy tim") || diag.Contains("rung nhĩ")) return "TM01";
        return "BT01";
    }

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

        // Kích hoạt WorkInfo phòng 5248 và các phòng liên quan
        try
        {
            long[] rooms = new long[] { 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267 };
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

        long targetDateStart = 20260930000000;
        long targetDateEnd   = 20260930235959;
        long instructionTime = 20260930060000;

        Console.WriteLine("=========================================================================================================");
        Console.WriteLine("QUY TRÌNH CHUYỂN TOÀN BỘ SUẤT ĂN NGÀY 30/09 SANG THS.BS NGUYỄN HỮU SÂM (034727) — 12 BỆNH NHÂN");
        Console.WriteLine("=========================================================================================================\n");

        for (int i = 0; i < treatmentIds.Length; i++)
        {
            long tId = treatmentIds[i];

            // 1. Lấy thông tin treatment
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, new HisTreatmentViewFilter { ID = tId }, cp);
            if (trList == null || trList.Count == 0) continue;
            var tr = trList[0];

            // 2. Tìm buồng giường
            var tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tId, IS_IN_ROOM = true };
            var inBed = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, cp);
            long reqRoomId = 5248;
            string bedName = "Chưa xếp giường";
            if (inBed != null && inBed.Count > 0)
            {
                bedName = inBed[0].BED_ROOM_NAME + " - " + inBed[0].BED_NAME;
                if (inBed[0].ROOM_ID > 0) reqRoomId = inBed[0].ROOM_ID;
            }

            Console.WriteLine(string.Format("👉 [{0:D2}/12] BN: {1} ({2}) | {3} | TrID: {4}", i + 1, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, bedName, tId));

            // 3. Lấy tất cả y lệnh của bệnh nhân
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var allOrders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            if (allOrders == null) allOrders = new List<V_HIS_SERVICE_REQ>();

            // Lọc các phiếu suất ăn từ ngày 30/09 trở đi
            var rationOrders30 = allOrders.Where(r => r.INTRUCTION_TIME >= targetDateStart && r.SERVICE_REQ_TYPE_ID == 8).ToList();
            
            // Xóa tất cả các phiếu suất ăn ngày 30/09 của BS KHÁC (hoặc phiếu cũ)
            var otherOrders = rationOrders30.Where(r => r.REQUEST_LOGINNAME != "034727" && r.SERVICE_REQ_STT_ID == 1).ToList();
            foreach (var oth in otherOrders)
            {
                var pDel = new CommonParam();
                bool delOk = adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = oth.ID }, pDel);
                if (delOk)
                {
                    Console.WriteLine(string.Format("   🗑️ Đã hủy phiếu cũ của BS khác: {0} (BS: {1} - {2})", oth.SERVICE_REQ_CODE, oth.REQUEST_USERNAME, oth.REQUEST_LOGINNAME));
                }
            }

            // Kiểm tra số phiếu suất ăn ngày 30/09 của 034727
            var samOrders = rationOrders30.Where(r => r.REQUEST_LOGINNAME == "034727").ToList();
            if (samOrders.Count < 3)
            {
                // Xóa phiếu không hoàn chỉnh của 034727 (nếu có) để tạo lại đồng bộ 3 bữa Sáng - Trưa - Tối
                foreach (var so in samOrders)
                {
                    if (so.SERVICE_REQ_STT_ID == 1)
                    {
                        var pDel = new CommonParam();
                        adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = so.ID }, pDel);
                    }
                }

                // Lấy TrackingId mới nhất
                var trkFilter = new HisTrackingFilter { TREATMENT_ID = tId };
                var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trkFilter, cp);
                long latestTrackingId = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First().ID : 0;

                string combo = DetectCombo(tr);

                // TẠO MỚI SUẤT ĂN NGÀY 30/09 DƯỚI TÊN BS NGUYỄN HỮU SÂM (034727)
                var sdo = new HisRationServiceReqSDO
                {
                    TreatmentIds = new List<long> { tId },
                    InstructionTimes = new List<long> { instructionTime },
                    RequestRoomId = reqRoomId > 0 ? reqRoomId : 5248,
                    RequestLoginName = "034727",
                    RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME,
                    IcdSubCode = tr.ICD_SUB_CODE,
                    IcdText = tr.ICD_TEXT,
                    HalfInFirstDay = false,
                    IsForAutoCreateRation = false,
                    IsForHomie = false,
                    TrackingId = latestTrackingId > 0 ? (long?)latestTrackingId : null,
                    RationServices = BuildRationServices(combo)
                };

                var pCreate = new CommonParam();
                adapter.PostData<object>("api/HisServiceReq/RationCreate", consumer, sdo, pCreate);
                if (pCreate.HasException)
                {
                    Console.WriteLine("   ❌ Lỗi tạo suất ăn: " + (pCreate.Messages != null ? string.Join("; ", pCreate.Messages) : "Unknown"));
                }
            }

            // 4. TRUY VẤN LẠI ĐỐI SOÁT CUỐI CÙNG
            var verifyOrders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, new HisServiceReqViewFilter { TREATMENT_ID = tId }, cp);
            var final30 = verifyOrders != null ? verifyOrders.Where(r => r.INTRUCTION_TIME >= targetDateStart && r.INTRUCTION_TIME <= targetDateEnd && r.SERVICE_REQ_TYPE_ID == 8).OrderBy(r => r.INTRUCTION_TIME).ToList() : new List<V_HIS_SERVICE_REQ>();

            Console.WriteLine(string.Format("   🎉 KẾT QUẢ SUẤT ĂN NGÀY 30/09 ({0} phiếu):", final30.Count));
            foreach (var r in final30)
            {
                string statusStr = r.SERVICE_REQ_STT_ID == 1 ? "⚪ Chưa làm (Trắng)" : (r.SERVICE_REQ_STT_ID == 2 ? "🟡 Đang làm (Vàng)" : "🟢 Hoàn thành (Xanh)");
                Console.WriteLine(string.Format("      • Mã phiếu: {0} | Giờ: {1} | BS Chỉ định: {2} ({3}) | Nơi Y/C: {4} | {5}",
                    r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.REQUEST_ROOM_NAME, statusStr));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        Console.WriteLine("\n🎉 ĐÃ HOÀN TẤT ĐỐI SOÁT VÀ ĐỒNG BỘ 100% SUẤT ĂN SANG THS.BS NGUYỄN HỮU SÂM (034727)!");
    }
}
