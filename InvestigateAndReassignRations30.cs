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

public class MyAdapterInv : AdapterBase
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

public class InvestigateAndReassignRations30
{
    public static MyAdapterInv adapter = new MyAdapterInv();

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
        if (diag.Contains("tháo đường") || diag.Contains("đtđ") || diag.Contains("diabetes"))
            return "DD01";
        if (diag.Contains("tăng huyết áp") || diag.Contains("tim mạch") || diag.Contains("suy tim") || diag.Contains("rung nhĩ"))
            return "TM01";
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

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không lấy được TokenCode!");
            return;
        }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        // Kích hoạt WorkInfo phòng 5248
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, workInfo, cp);
        }
        catch { }

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

        long targetDateStart = 20260930000000;
        long targetDateEnd   = 20260930235959;
        long instructionTime = 20260930060000;

        Console.WriteLine("\n=========================================================================================================");
        Console.WriteLine("TIẾN HÀNH ĐỐI SOÁT & CHUYỂN TOÀN BỘ SUẤT ĂN NGÀY 30/09 SANG THS.BS NGUYỄN HỮU SÂM (034727)");
        Console.WriteLine("=========================================================================================================\n");

        for (int i = 0; i < patientCodes.Length; i++)
        {
            string pCode = patientCodes[i];
            
            // Tìm treatment
            var trkViewFilter = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, trkViewFilter, cp);
            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine(string.Format("❌ [{0}/12] Không tìm thấy hồ sơ BN: {1}", i + 1, pCode));
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();
            long tId = tr.ID;

            // Tìm phòng bệnh nhân
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tId, IS_IN_ROOM = true };
            var inBed = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, cp);
            long reqRoomId = 5248;
            string bedName = "Chưa xếp giường";
            if (inBed != null && inBed.Count > 0)
            {
                bedName = inBed[0].BED_ROOM_NAME + " - " + inBed[0].BED_NAME;
                if (inBed[0].ROOM_ID > 0) reqRoomId = inBed[0].ROOM_ID;
            }

            // Kích hoạt phòng của bệnh nhân
            try
            {
                var wi = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO> { new RoomSDO { RoomId = reqRoomId }, new RoomSDO { RoomId = 5248 } }
                };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, wi, cp);
            }
            catch { }

            Console.WriteLine(string.Format("👉 [{0:D2}/12] BN: {1} ({2}) | Vị trí: {3} | ID: {4}", i + 1, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, bedName, tId));

            // Quét tất cả ServiceReq suất ăn hiện có
            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_ID = tId,
                SERVICE_REQ_TYPE_ID = 8 // Suất ăn
            };
            var existingReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            var reqs30 = existingReqs != null ? existingReqs.Where(r => r.INTRUCTION_TIME >= targetDateStart && r.INTRUCTION_TIME <= targetDateEnd).ToList() : new List<V_HIS_SERVICE_REQ>();

            Console.WriteLine(string.Format("   - Tìm thấy {0} phiếu suất ăn ngày 30/09 hiện có.", reqs30.Count));

            // Hủy tất cả các phiếu suất ăn ngày 30/09 cũ nếu không phải do 034727 tạo hoặc cần làm mới
            foreach (var oldReq in reqs30)
            {
                Console.WriteLine(string.Format("     * Phiếu: {0} | BS cũ: {1} ({2}) | Trạng thái: {3}", 
                    oldReq.SERVICE_REQ_CODE, oldReq.REQUEST_USERNAME, oldReq.REQUEST_LOGINNAME, oldReq.SERVICE_REQ_STT_ID));
                
                // Chỉ hủy nếu trạng thái màu trắng (STT_ID == 1)
                if (oldReq.SERVICE_REQ_STT_ID == 1)
                {
                    var pDel = new CommonParam();
                    var delRes = adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = oldReq.ID }, pDel);
                    if (delRes)
                    {
                        Console.WriteLine("       ✔ Đã hủy phiếu cũ: " + oldReq.SERVICE_REQ_CODE);
                    }
                    else
                    {
                        // Thử hủy theo SereServ
                        var ssDelFilter = new HisSereServViewFilter { SERVICE_REQ_ID = oldReq.ID };
                        var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssDelFilter, pDel);
                        if (ssList != null)
                        {
                            foreach (var ss in ssList)
                            {
                                adapter.PostData<bool>("api/HisSereServ/Delete", consumer, new HisSereServFilter { ID = ss.ID }, pDel);
                            }
                        }
                    }
                }
            }

            // Lấy Tờ điều trị mới nhất (hoặc null)
            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tId };
            var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trkFilter, cp);
            long latestTrackingId = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First().ID : 0;

            string combo = DetectCombo(tr);

            // TẠO LẠI SUẤT ĂN NGÀY 30/09 ĐÍCH DANH 034727
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
            var createRes = adapter.PostData<object>("api/HisServiceReq/RationCreate", consumer, sdo, pCreate);
            if (!pCreate.HasException)
            {
                // Verify lại phiếu vừa tạo
                var newReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
                var created30 = newReqs != null ? newReqs.Where(r => r.INTRUCTION_TIME >= targetDateStart && r.INTRUCTION_TIME <= targetDateEnd).ToList() : new List<V_HIS_SERVICE_REQ>();
                
                Console.WriteLine("   🎉 ĐÃ CHỈ ĐỊNH SUẤT ĂN NGÀY 30/09 THÀNH CÔNG:");
                foreach (var nr in created30)
                {
                    Console.WriteLine(string.Format("      ✔ Mã phiếu: {0} | Giờ: {1} | BS: {2} ({3})", 
                        nr.SERVICE_REQ_CODE, nr.INTRUCTION_TIME, nr.REQUEST_USERNAME, nr.REQUEST_LOGINNAME));
                }
            }
            else
            {
                Console.WriteLine("   ❌ Lỗi chỉ định: " + (pCreate.Messages != null ? string.Join("; ", pCreate.Messages) : "Unknown"));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }
    }
}
