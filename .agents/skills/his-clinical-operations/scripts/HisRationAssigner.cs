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
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
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

public class HisRationAssigner
{
    public static MyAdapter adapter = new MyAdapter();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
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
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

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

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true"))
                    {
                        continue;
                    }

                    // BẢO VỆ DANH TÍNH: Phải thuộc 034727 hoặc vmc
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string tok = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                return tok;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static List<RationServiceSDO> BuildRationServices(string comboType, long patientTypeId)
    {
        var list = new List<RationServiceSDO>();
        long ptId = 42; // BẮT BUỘC 42 (Viện phí) cho 100% bệnh nhân, BHYT không chi trả tiền suất ăn

        if (comboType == "DD01") // Đái tháo đường
        {
            list.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else if (comboType == "TM01") // Tim mạch / Tăng huyết áp
        {
            list.Add(new RationServiceSDO { ServiceId = 30117, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30093, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30094, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else // BT01 (Bình thường / Ngoại khoa)
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

    public static bool AssignRationForDay(ApiConsumer consumer, V_HIS_TREATMENT tr, long trackingId, DateTime date, string comboType, long requestRoomId = 5248)
    {
        // QUY TẮC: Ngày nhập khoa: sau giờ nhập khoa 15 phút; Hôm nay thông thường: DateTime.Now; Các ngày tới: 06:00 sáng
        long instructionTime;
        long inTimeRaw = tr.CLINICAL_IN_TIME ?? tr.IN_TIME;
        DateTime inTimeDt = DateTime.MinValue;
        if (inTimeRaw > 0)
        {
            DateTime.TryParseExact(inTimeRaw.ToString(), "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out inTimeDt);
        }

        if (inTimeDt != DateTime.MinValue && date.Date == inTimeDt.Date)
        {
            instructionTime = long.Parse(inTimeDt.AddMinutes(15).ToString("yyyyMMddHHmmss"));
        }
        else if (date.Date == DateTime.Today)
        {
            instructionTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        }
        else
        {
            instructionTime = long.Parse(date.ToString("yyyyMMdd") + "060000");
        }

        var sdo = new HisRationServiceReqSDO
        {
            TreatmentIds = new List<long> { tr.ID },
            InstructionTimes = new List<long> { instructionTime },
            RequestRoomId = requestRoomId > 0 ? requestRoomId : 5248,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            HalfInFirstDay = false,
            IsForAutoCreateRation = false,
            IsForHomie = false,
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RationServices = BuildRationServices(comboType, 42)
        };

        CommonParam callParam = new CommonParam();
        try
        {
            var result = adapter.PostData<object>("api/HisServiceReq/RationCreate", consumer, sdo, callParam);
            if (!callParam.HasException)
            {
                return true;
            }
            else
            {
                if (callParam.Messages != null)
                {
                    foreach (var msg in callParam.Messages) Console.WriteLine("    ❌ " + msg);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("    ❌ Lỗi gọi API: {0}", ex.Message));
            return false;
        }
    }

    public static void ProcessRooms(string roomListStr = "all")
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                CommonParam param = new CommonParam();
                var tok = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
                }
                if (!string.IsNullOrEmpty(token))
                {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), token + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8); } catch { }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam p = new CommonParam();

        // Kích hoạt WorkInfo phòng 5248
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, p);
        }
        catch { }

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, p);

        List<V_HIS_BED_ROOM> targetRooms;
        if (string.IsNullOrEmpty(roomListStr) || roomListStr.ToLower() == "all")
        {
            targetRooms = bList.OrderBy(x => x.BED_ROOM_NAME).ToList();
            roomListStr = "Toàn khoa (710 - 740)";
        }
        else
        {
            var filters = roomListStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            targetRooms = bList.Where(r => r.BED_ROOM_NAME != null && filters.Any(f => r.BED_ROOM_NAME.Contains(f))).OrderBy(x => x.BED_ROOM_NAME).ToList();
        }

        DateTime today = DateTime.Today;
        DateTime tomorrow = today.AddDays(1);
        long todayStart = long.Parse(today.ToString("yyyyMMdd") + "000000");
        long tomorrowStart = long.Parse(tomorrow.ToString("yyyyMMdd") + "000000");

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🍚 KIỂM TRA & BỔ SUNG SUẤT ĂN DINH DƯỠNG CHO NGƯỜI BỆNH (PHÒNG " + roomListStr + ")");
        Console.WriteLine(string.Format("Thời gian: {0} | Bác sĩ chỉ định: Ths.BS Nguyễn Hữu Sâm (034727)", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
        Console.WriteLine(string.Format("Hôm nay: {0} | Ngày mai: {1}", today.ToString("dd/MM/yyyy"), tomorrow.ToString("dd/MM/yyyy")));
        Console.WriteLine("===============================================================================\n");

        int totalAssigned = 0;

        foreach (var room in targetRooms.OrderBy(x => x.BED_ROOM_NAME))
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var inP = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, p);
            if (inP == null || inP.Count == 0) continue;

            Console.WriteLine(string.Format("🏥 Buồng: {0} ({1} bệnh nhân)", room.BED_ROOM_NAME, inP.Count));

            long roomId = room.ROOM_ID > 0 ? room.ROOM_ID : 5248;
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO> { new RoomSDO { RoomId = roomId } }
                };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, p);
            }
            catch { }

            foreach (var patient in inP.OrderBy(x => x.BED_NAME))
            {
                long tId = patient.TREATMENT_ID;
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, p);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;
                if (tr == null) continue;

                // Tờ điều trị mới nhất
                HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tId };
                var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, p);
                long latestTrackingId = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First().ID : 0;

                // Kiểm tra suất ăn hiện có
                HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
                var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mosConsumer, rf, p);

                var todayRations = rList != null ? rList.Where(x => x.INTRUCTION_TIME >= todayStart && x.INTRUCTION_TIME < tomorrowStart).ToList() : new List<V_HIS_SERE_SERV_RATION>();
                var tmrRations = rList != null ? rList.Where(x => x.INTRUCTION_TIME >= tomorrowStart).ToList() : new List<V_HIS_SERE_SERV_RATION>();

                string combo = DetectCombo(tr);

                Console.WriteLine(string.Format("\n👉 [{0}] {1} (Mã: {2} | TrID: {3})", patient.BED_NAME, patient.TDL_PATIENT_NAME, patient.TDL_PATIENT_CODE, tId));
                Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} -> Chế độ phù hợp: {2}", tr.ICD_CODE, tr.ICD_NAME, combo));

                // 1. Chỉ định cho Hôm nay (nếu chưa có)
                if (todayRations.Count == 0)
                {
                    Console.WriteLine("   ⚡ Đang chỉ định suất ăn hôm nay (" + today.ToString("dd/MM") + ")...");
                    bool ok = AssignRationForDay(mosConsumer, tr, latestTrackingId, today, combo, roomId);
                    if (ok)
                    {
                        Console.WriteLine("   ✔ Chỉ định THÀNH CÔNG suất ăn " + combo + " (3 bữa Sáng - Trưa - Chiều) ngày " + today.ToString("dd/MM/yyyy"));
                        totalAssigned++;
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ Hôm nay (" + today.ToString("dd/MM") + ") ĐÃ CÓ " + todayRations.Count + " bữa ăn.");
                }

                // 2. Chỉ định cho Ngày mai (nếu chưa có)
                if (tmrRations.Count == 0)
                {
                    Console.WriteLine("   ⚡ Đang chỉ định suất ăn ngày mai (" + tomorrow.ToString("dd/MM") + ")...");
                    bool ok = AssignRationForDay(mosConsumer, tr, latestTrackingId, tomorrow, combo, roomId);
                    if (ok)
                    {
                        Console.WriteLine("   ✔ Chỉ định THÀNH CÔNG suất ăn " + combo + " (3 bữa Sáng - Trưa - Chiều) ngày " + tomorrow.ToString("dd/MM/yyyy"));
                        totalAssigned++;
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ Ngày mai (" + tomorrow.ToString("dd/MM") + ") ĐÃ CÓ " + tmrRations.Count + " bữa ăn.");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🎉 HOÀN TẤT! Đã bổ sung thành công {0} đợt suất ăn cho các bệnh nhân còn thiếu.", totalAssigned));
        Console.WriteLine("===============================================================================");
    }

    public static void Run(string[] args)
    {
        string rooms = "all";
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            rooms = args[0];
        }
        ProcessRooms(rooms);
    }
}

class Program
{
    static void Main(string[] args)
    {
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

        HisRationAssigner.Run(args);
    }
}
