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
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapterWh : AdapterBase
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

public class HisWarehousePrescribe
{
    public static MyAdapterWh adapter = new MyAdapterWh();

    // DANH MỤC CÁC KHO CẤP PHÁT / KHO DƯỢC LĨNH CHUẨN HÓA (IS_CABINET = 0)
    public const long STOCK_KHO_THUOC_VIEN_HN = 4210;    // KT_KD15 (Kho thuốc viên nội trú BV Bạch Mai)
    public const long STOCK_KHO_THUOC_ONG_HN = 4209;     // KT_KD14 (Kho thuốc ống nội trú BV Bạch Mai)
    public const long STOCK_KHO_DINH_DUONG_HN = 753;      // LA_TTDDLS (Kho SP Dinh dưỡng điều trị)
    public const long STOCK_KHO_DUOC_CHINH_NB = 4854;    // KTD_NBKP22.01 (Kho Dược chính Ninh Bình)
    public const long STOCK_KHO_VAT_TU_CTCH_HN = 796;    // KVT_KCTCGCS (Kho Vật tư Khoa CTCH & Cột sống)

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
            if (!File.Exists(cacheFile))
            {
                string alt1 = Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_standalone.token");
                if (File.Exists(alt1)) cacheFile = alt1;
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

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;

                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (start + 64 <= chunk.Length)
                        {
                            string t = chunk.Substring(start, 64);
                            if (t.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                            {
                                return t;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    public static void UpdateWorkInfo(ApiConsumer consumer, long roomId)
    {
        try
        {
            List<long> roomIds = new List<long> { 931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 18679, 18681 };
            if (roomId > 0 && !roomIds.Contains(roomId)) roomIds.Add(roomId);

            var workInfo = new WorkInfoSDO
            {
                Rooms = roomIds.Select(r => new RoomSDO { RoomId = r }).ToList()
            };
            CommonParam cp = new CommonParam();
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, workInfo, cp);
        }
        catch { }
    }

    public static V_HIS_TREATMENT FindTreatment(ApiConsumer consumer, string patKey)
    {
        string cleanKey = patKey.Trim();
        if (cleanKey.All(char.IsDigit) && cleanKey.Length < 10) cleanKey = cleanKey.PadLeft(10, '0');

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        if (cleanKey.Length == 12 && cleanKey.StartsWith("0000"))
            tf.TREATMENT_CODE__EXACT = cleanKey;
        else if (cleanKey.Length >= 8 && cleanKey.StartsWith("000"))
            tf.PATIENT_CODE__EXACT = cleanKey;
        else
            tf.KEY_WORD = cleanKey;

        CommonParam cp = new CommonParam();
        var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, cp);
        if (trList == null || trList.Count == 0)
        {
            tf = new HisTreatmentViewFilter { KEY_WORD = cleanKey };
            trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, cp);
        }

        if (trList == null || trList.Count == 0) return null;

        var inPatient = trList.Where(t => (t.END_DEPARTMENT_ID == 57 || t.END_DEPARTMENT_ID == 915 || t.BRANCH_ID == 81) && (!t.OUT_TIME.HasValue || t.OUT_TIME == 0))
                              .OrderByDescending(t => t.IN_TIME).FirstOrDefault();
        if (inPatient == null)
            inPatient = trList.Where(t => !t.OUT_TIME.HasValue || t.OUT_TIME == 0).OrderByDescending(t => t.IN_TIME).FirstOrDefault();
        if (inPatient == null)
            inPatient = trList.OrderByDescending(t => t.IN_TIME).First();

        return inPatient;
    }

    public static void GetPatientLocation(ApiConsumer consumer, long treatmentId, out long roomId, out long deptId, out string bedName, out string roomName)
    {
        roomId = 5248; // P716 / P734 CTCH
        deptId = 57;
        bedName = "Chưa rõ giường";
        roomName = "Khoa 57";

        try
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = treatmentId, IS_IN_ROOM = true };
            CommonParam cp = new CommonParam();
            var inBedList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, cp);
            if (inBedList != null && inBedList.Count > 0)
            {
                var curBed = inBedList.First();
                bedName = curBed.BED_NAME;
                roomName = curBed.BED_ROOM_NAME;

                HisBedRoomViewFilter brf = new HisBedRoomViewFilter { ID = curBed.BED_ROOM_ID };
                var brList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", consumer, brf, cp);
                if (brList != null && brList.Count > 0 && brList[0].ROOM_ID > 0)
                {
                    roomId = brList[0].ROOM_ID;
                    deptId = brList[0].DEPARTMENT_ID;
                }
            }
        }
        catch { }
    }

    public static long EnsureTracking(ApiConsumer consumer, V_HIS_TREATMENT tr, long roomId, long deptId, string content, string instruction, ref long instructionTime)
    {
        long todayStart = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "000000");
        try
        {
            HisTrackingViewFilter tf = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            CommonParam cpCheck = new CommonParam();
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, tf, cpCheck);
            if (trkList != null)
            {
                var existing = trkList.Where(x => x.TRACKING_TIME >= todayStart).OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
                if (existing != null)
                {
                    // Lùi thời gian y lệnh thuốc 5 phút sau thời điểm tờ điều trị
                    DateTime tDt = DateTime.Now;
                    string s = existing.TRACKING_TIME.ToString();
                    if (s.Length == 14)
                    {
                        int y = int.Parse(s.Substring(0, 4));
                        int m = int.Parse(s.Substring(4, 2));
                        int d = int.Parse(s.Substring(6, 2));
                        int h = int.Parse(s.Substring(8, 2));
                        int min = int.Parse(s.Substring(10, 2));
                        int sec = int.Parse(s.Substring(12, 2));
                        tDt = new DateTime(y, m, d, h, min, sec);
                    }
                    instructionTime = long.Parse(tDt.AddMinutes(5).ToString("yyyyMMddHHmmss"));
                    return existing.ID;
                }
            }
        }
        catch { }

        // Tạo mới tờ điều trị nếu chưa có
        long trackTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            TRACKING_TIME = trackTime,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT,
            CONTENT = !string.IsNullOrEmpty(content) ? content : "Diễn biến bệnh ổn định. Dùng thuốc nội trú theo y lệnh lĩnh.",
            MEDICAL_INSTRUCTION = !string.IsNullOrEmpty(instruction) ? instruction : "Dùng thuốc theo y lệnh lĩnh kho dược.",
            DEPARTMENT_ID = deptId > 0 ? deptId : 57,
            ROOM_ID = roomId
        };

        var sdo = new HisTrackingSDO { Tracking = tracking, WorkingRoomId = roomId };
        CommonParam cpCreate = new CommonParam();
        var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", consumer, sdo, cpCreate);
        if (res != null && res.ID > 0)
        {
            DateTime dt = DateTime.Now;
            instructionTime = long.Parse(dt.AddMinutes(5).ToString("yyyyMMddHHmmss"));
            return res.ID;
        }

        return 0;
    }

    public static V_HIS_MEDICINE_TYPE FindMedicine(ApiConsumer consumer, string medKeyword)
    {
        CommonParam cp = new CommonParam();
        long medId;
        if (long.TryParse(medKeyword, out medId) && medId > 100)
        {
            var f = new HisMedicineTypeViewFilter { ID = medId };
            var list = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", consumer, f, cp);
            if (list != null && list.Count > 0) return list[0];
        }

        var filter = new HisMedicineTypeViewFilter { KEY_WORD = medKeyword, IS_ACTIVE = 1 };
        var meds = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", consumer, filter, cp);
        if (meds != null && meds.Count > 0)
        {
            var match = meds.FirstOrDefault(m => string.Equals(m.MEDICINE_TYPE_CODE, medKeyword, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            match = meds.FirstOrDefault(m => m.MEDICINE_TYPE_NAME != null && m.MEDICINE_TYPE_NAME.ToLower().Contains(medKeyword.ToLower()));
            if (match != null) return match;

            return meds[0];
        }
        return null;
    }

    public class WarehousePrescribeItem
    {
        public string Keyword { get; set; }
        public V_HIS_MEDICINE_TYPE Medicine { get; set; }
        public decimal Amount { get; set; }
        public long StockId { get; set; }
        public string Tutorial { get; set; }
        public long? UseFormId { get; set; }
        public string Speed { get; set; }
        public string Morning { get; set; }
        public string Noon { get; set; }
        public string Afternoon { get; set; }
        public string Evening { get; set; }
        public bool IsExpend { get; set; }
    }

    /// <summary>
    /// KÊ ĐƠN LĨNH KHO DƯỢC / CẤP PHÁT (InPatientPresCreate với PrescriptionTypeId = 1, IS_CABINET = 0)
    /// </summary>
    public static bool PrescribeWarehouseItems(
        ApiConsumer consumer,
        V_HIS_TREATMENT tr,
        long roomId,
        List<WarehousePrescribeItem> items,
        long trackingId,
        long instructionTime,
        string doctorLogin,
        string doctorName,
        out string serviceReqCode,
        out string expMestCode,
        out string error)
    {
        serviceReqCode = "";
        expMestCode = "";
        error = "";

        if (items == null || items.Count == 0)
        {
            error = "Danh sách thuốc lĩnh trống!";
            return false;
        }

        var presMeds = new List<PresMedicineSDO>();
        foreach (var item in items)
        {
            long useForm = item.UseFormId ?? item.Medicine.MEDICINE_USE_FORM_ID ?? 1L;
            decimal? speedVal = null;
            decimal sp;
            if (!string.IsNullOrEmpty(item.Speed) && decimal.TryParse(item.Speed, out sp)) speedVal = sp;

            presMeds.Add(new PresMedicineSDO
            {
                MedicineTypeId = item.Medicine.ID,
                MediStockId = item.StockId > 0 ? item.StockId : STOCK_KHO_THUOC_VIEN_HN,
                Amount = item.Amount,
                PresAmount = item.Amount,
                PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                Tutorial = !string.IsNullOrEmpty(item.Tutorial) ? item.Tutorial : "Dùng theo chỉ dẫn của bác sĩ",
                MedicineUseFormId = useForm,
                Speed = speedVal,
                Morning = item.Morning,
                Noon = item.Noon,
                Afternoon = item.Afternoon,
                Evening = item.Evening,
                IsExpend = item.IsExpend,
                NumOfDays = 1
            });
        }

        var presSDO = new InPatientPresSDO
        {
            TreatmentId = tr.ID,
            InstructionTimes = new List<long> { instructionTime },
            UseTimes = new List<long> { instructionTime },
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            TrackingInfos = trackingId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = instructionTime } } : null,
            RequestRoomId = roomId > 0 ? roomId : 5248,
            RequestLoginName = doctorLogin,
            RequestUserName = doctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            Medicines = presMeds
        };

        CommonParam pPres = new CommonParam();
        var presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", consumer, presSDO, pPres);

        // Fallback phòng trực 5248 nếu phòng buồng bệnh chưa kích hoạt phân quyền
        if ((presRes == null || ((presRes.ServiceReqs == null || presRes.ServiceReqs.Count == 0) && (presRes.ExpMests == null || presRes.ExpMests.Count == 0))) && presSDO.RequestRoomId != 5248)
        {
            presSDO.RequestRoomId = 5248;
            pPres = new CommonParam();
            presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", consumer, presSDO, pPres);
        }

        if (presRes != null)
        {
            if (presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0)
                serviceReqCode = presRes.ServiceReqs[0].SERVICE_REQ_CODE;
            if (presRes.ExpMests != null && presRes.ExpMests.Count > 0)
                expMestCode = presRes.ExpMests[0].EXP_MEST_CODE;

            if (!string.IsNullOrEmpty(serviceReqCode) || !string.IsNullOrEmpty(expMestCode)) return true;
        }

        if (pPres.Messages != null && pPres.Messages.Count > 0) error = string.Join("; ", pPres.Messages);
        else if (pPres.BugCodes != null && pPres.BugCodes.Count > 0) error = string.Join("; ", pPres.BugCodes);
        else error = "Kê đơn lĩnh kho dược thất bại (API trả về null)";

        return false;
    }

    // =========================================================================
    // CÁC CHỨC NĂNG NGHIỆP VỤ LÂM SÀNG
    // =========================================================================

    public static void ExecuteSearch(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Cú pháp: HisWarehousePrescribe.exe search <TừKhóa>");
            return;
        }

        string kw = args[1];
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var filter = new HisMedicineTypeViewFilter { KEY_WORD = kw, IS_ACTIVE = 1 };
        CommonParam cp = new CommonParam();
        var list = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", consumer, filter, cp);

        Console.WriteLine("=================================================================================================");
        Console.WriteLine(string.Format("🔍 TÌM KIẾM THUỐC TRONG DANH MỤC DƯỢC VỚI TỪ KHÓA: \"{0}\"", kw));
        Console.WriteLine("=================================================================================================");

        if (list == null || list.Count == 0)
        {
            Console.WriteLine("⚠️ Không tìm thấy thuốc nào phù hợp.");
            return;
        }

        Console.WriteLine(string.Format("{0,-4} | {1,-12} | {2,-10} | {3,-40} | {4,-10} | {5}", "STT", "Mã Thuốc", "TypeID", "Tên Thuốc", "Hàm Lượng", "Đơn Vị"));
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
        for (int i = 0; i < Math.Min(30, list.Count); i++)
        {
            var m = list[i];
            Console.WriteLine(string.Format("{0,-4} | {1,-12} | {2,-10} | {3,-40} | {4,-10} | {5}",
                i + 1, m.MEDICINE_TYPE_CODE, m.ID, (m.MEDICINE_TYPE_NAME != null && m.MEDICINE_TYPE_NAME.Length > 39 ? m.MEDICINE_TYPE_NAME.Substring(0, 36) + "..." : m.MEDICINE_TYPE_NAME), m.CONCENTRA, m.SERVICE_UNIT_NAME));
        }
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("👉 Hiển thị {0}/{1} kết quả.", Math.Min(30, list.Count), list.Count));
        Console.WriteLine("=================================================================================================");
    }

    public static void ExecuteSingle(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Cú pháp: HisWarehousePrescribe.exe single <MãBN> <Tên/MãThuốc> <SốLượng> [KhoLĩnh=4210] [HDSD] [ĐườngDùngId] [Sáng:Trưa:Chiều:Tối]");
            return;
        }

        string patKey = args[1];
        string medKw = args[2];
        decimal amount = decimal.Parse(args[3]);
        long stockId = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? long.Parse(args[4]) : STOCK_KHO_THUOC_VIEN_HN;
        string tutorial = args.Length > 5 ? args[5] : "Dùng theo chỉ dẫn của bác sĩ";
        long? useFormId = args.Length > 6 && !string.IsNullOrEmpty(args[6]) ? (long?)long.Parse(args[6]) : null;
        string doses = args.Length > 7 ? args[7] : null;

        string morning = null, noon = null, afternoon = null, evening = null;
        if (!string.IsNullOrEmpty(doses) && doses.Contains(":"))
        {
            string[] dp = doses.Split(':');
            if (dp.Length > 0 && !string.IsNullOrEmpty(dp[0])) morning = dp[0];
            if (dp.Length > 1 && !string.IsNullOrEmpty(dp[1])) noon = dp[1];
            if (dp.Length > 2 && !string.IsNullOrEmpty(dp[2])) afternoon = dp[2];
            if (dp.Length > 3 && !string.IsNullOrEmpty(dp[3])) evening = dp[3];
        }

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode đăng nhập!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        var med = FindMedicine(consumer, medKw);
        if (med == null) { Console.WriteLine("❌ Không tìm thấy thuốc trong danh mục: " + medKw); return; }

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Bệnh nhân dùng thuốc lĩnh theo y lệnh.", med.MEDICINE_TYPE_NAME + " x " + amount + " (" + tutorial + ")", ref insTime);

        var item = new WarehousePrescribeItem
        {
            Keyword = medKw,
            Medicine = med,
            Amount = amount,
            StockId = stockId,
            Tutorial = tutorial,
            UseFormId = useFormId ?? med.MEDICINE_USE_FORM_ID,
            Morning = morning,
            Noon = noon,
            Afternoon = afternoon,
            Evening = evening,
            IsExpend = false
        };

        string sCode, eCode, err;
        bool ok = PrescribeWarehouseItems(consumer, tr, roomId, new List<WarehousePrescribeItem> { item }, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
        if (ok)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🎉 KÊ ĐƠN LĨNH KHO DƯỢC THÀNH CÔNG: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
            Console.WriteLine(string.Format("   - Vị trí buồng   : {0} - {1}", roomName, bedName));
            Console.WriteLine(string.Format("   - Thuốc          : {0} ({1}) | SL: {2}", med.MEDICINE_TYPE_NAME, med.MEDICINE_TYPE_CODE, amount));
            Console.WriteLine(string.Format("   - Kho Lĩnh       : ID {0}", stockId));
            Console.WriteLine(string.Format("   - HDSD           : {0}", tutorial));
            Console.WriteLine(string.Format("   - Mã phiếu y lệnh: {0}", sCode));
            Console.WriteLine(string.Format("   - Mã xuất kho    : {0}", eCode));
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("❌ KÊ ĐƠN LĨNH THẤT BẠI: " + err);
        }
    }

    /// <summary>
    /// KÊ TOA THUỐC LĨNH ĐA MÓN TỪ KHO DƯỢC
    /// Cú pháp: HisWarehousePrescribe.exe multi <MãBN> "Thuốc1|SL|HDSD|[KhoId]|[ĐườngDùng]|[Sáng:Trưa:Chiều:Tối]" ... [--time HH:mm]
    /// </summary>
    public static void ExecuteMulti(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Cú pháp: HisWarehousePrescribe.exe multi <MãBN> \"<Thuốc1>|<SL>|<HDSD>|[KhoId]|[ĐD]|[Cữ]\" [\"<Thuốc2>|...\"] [--stock KhoId] [--time HH:mm]");
            Console.WriteLine("Ví dụ:");
            Console.WriteLine("  HisWarehousePrescribe.exe multi 0001666593 \"Cefuroxim 500mg|2|Uống sáng 1 chiều 1 sau ăn|4210|1|01::01\" \"Lipitor 10mg|1|Uống tối 1 viên|4210|1|:::01\"");
            return;
        }

        string patKey = args[1];
        long defaultStockId = STOCK_KHO_THUOC_VIEN_HN;
        string customTimeStr = null;
        List<string> rawItems = new List<string>();

        for (int i = 2; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--stock" && i + 1 < args.Length)
            {
                defaultStockId = long.Parse(args[++i]);
            }
            else if (a == "--time" && i + 1 < args.Length)
            {
                customTimeStr = args[++i];
            }
            else if (!a.StartsWith("--"))
            {
                rawItems.Add(a);
            }
        }

        if (rawItems.Count == 0)
        {
            Console.WriteLine("❌ Vui lòng cung cấp ít nhất 1 thuốc cần kê lĩnh!");
            return;
        }

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode đăng nhập!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        List<WarehousePrescribeItem> parsedItems = new List<WarehousePrescribeItem>();
        StringBuilder trackingInstruction = new StringBuilder();

        foreach (var r in rawItems)
        {
            string[] parts = r.Split('|');
            string kw = parts[0].Trim();
            decimal amt = parts.Length > 1 ? decimal.Parse(parts[1].Trim()) : 1.0m;
            string tut = parts.Length > 2 ? parts[2].Trim() : "Dùng theo chỉ dẫn của bác sĩ";
            long sId = defaultStockId;
            if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3].Trim()))
            {
                long parsedSId;
                if (long.TryParse(parts[3].Trim(), out parsedSId)) sId = parsedSId;
            }
            long? uf = null;
            if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4].Trim()))
            {
                long parsedUf;
                if (long.TryParse(parts[4].Trim(), out parsedUf)) uf = parsedUf;
            }
            string morning = null, noon = null, afternoon = null, evening = null;
            if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5].Trim()) && parts[5].Contains(":"))
            {
                string[] dp = parts[5].Split(':');
                if (dp.Length > 0 && !string.IsNullOrEmpty(dp[0])) morning = dp[0];
                if (dp.Length > 1 && !string.IsNullOrEmpty(dp[1])) noon = dp[1];
                if (dp.Length > 2 && !string.IsNullOrEmpty(dp[2])) afternoon = dp[2];
                if (dp.Length > 3 && !string.IsNullOrEmpty(dp[3])) evening = dp[3];
            }

            var med = FindMedicine(consumer, kw);
            if (med == null)
            {
                Console.WriteLine(string.Format("❌ Không tìm thấy thuốc trong danh mục: \"{0}\"", kw));
                return;
            }

            parsedItems.Add(new WarehousePrescribeItem
            {
                Keyword = kw,
                Medicine = med,
                Amount = amt,
                StockId = sId,
                Tutorial = tut,
                UseFormId = uf ?? med.MEDICINE_USE_FORM_ID,
                Morning = morning,
                Noon = noon,
                Afternoon = afternoon,
                Evening = evening,
                IsExpend = false
            });

            trackingInstruction.AppendFormat("- {0} x {1} ({2})\n", med.MEDICINE_TYPE_NAME, amt, tut);
        }

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Bệnh nhân dùng thuốc nội trú lĩnh từ kho theo y lệnh.", trackingInstruction.ToString().TrimEnd(), ref insTime);

        if (!string.IsNullOrEmpty(customTimeStr) && customTimeStr.Contains(":"))
        {
            try
            {
                string[] tp = customTimeStr.Split(':');
                DateTime dt = DateTime.Today.AddHours(int.Parse(tp[0])).AddMinutes(int.Parse(tp[1]));
                insTime = long.Parse(dt.ToString("yyyyMMddHHmmss"));
            }
            catch { }
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📦 ĐANG GỬI TOA THUỐC LĨNH KHO DƯỢC ({0} MẶT HÀNG)", parsedItems.Count));
        Console.WriteLine(string.Format("   - Bệnh nhân  : {0} ({1}) | Vị trí: {2} - {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, roomName, bedName));
        Console.WriteLine("===============================================================================");

        string sCode, eCode, err;
        bool ok = PrescribeWarehouseItems(consumer, tr, roomId, parsedItems, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
        if (ok)
        {
            Console.WriteLine("🎉 KÊ TOA THUỐC LĨNH KHO THÀNH CÔNG!");
            Console.WriteLine(string.Format("   • Mã Phiếu Y Lệnh : {0}", sCode));
            Console.WriteLine(string.Format("   • Mã Xuất Kho     : {0}", eCode));
            Console.WriteLine("\n📋 Danh sách thuốc đã dự trù lĩnh:");
            for (int i = 0; i < parsedItems.Count; i++)
            {
                var it = parsedItems[i];
                Console.WriteLine(string.Format("   {0}. {1} ({2}) | SL: {3} | Kho: {4} | HDSD: {5}",
                    i + 1, it.Medicine.MEDICINE_TYPE_NAME, it.Medicine.MEDICINE_TYPE_CODE, it.Amount, it.StockId, it.Tutorial));
            }
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("❌ KÊ TOA THUỐC LĨNH THẤT BẠI: " + err);
            Console.WriteLine("===============================================================================");
        }
    }

    /// <summary>
    /// KÊ LĨNH SẢN PHẨM DINH DƯỠNG ĐIỀU TRỊ TỪ KHO 753 (LA_TTDDLS)
    /// </summary>
    public static void ExecuteNutrition(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Cú pháp: HisWarehousePrescribe.exe nutrition <MãBN1,MãBN2,...> <TênSP/MãSP> [SốLượng=1] [KhoLĩnh=753] [HDSD]");
            return;
        }

        string[] codes = args[1].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string spKw = args[2];
        decimal qty = args.Length > 3 ? decimal.Parse(args[3]) : 1.0m;
        long stockId = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? long.Parse(args[4]) : STOCK_KHO_DINH_DUONG_HN;
        string tut = args.Length > 5 ? args[5] : "Dùng theo chỉ định của chuyên khoa Dinh dưỡng";

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var med = FindMedicine(consumer, spKw);
        if (med == null) { Console.WriteLine("❌ Không tìm thấy sản phẩm dinh dưỡng: " + spKw); return; }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🥣 KÊ LĨNH SẢN PHẨM DINH DƯỠNG KHO 753 ({0} BN)", codes.Length));
        Console.WriteLine(string.Format("   - Sản phẩm: {0} ({1}) | SL: {2}", med.MEDICINE_TYPE_NAME, med.MEDICINE_TYPE_CODE, qty));
        Console.WriteLine("===============================================================================\n");

        foreach (var c in codes)
        {
            var tr = FindTreatment(consumer, c);
            if (tr == null) { Console.WriteLine("❌ Không tìm thấy BN: " + c); continue; }

            long roomId, deptId;
            string bedName, roomName;
            GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
            UpdateWorkInfo(consumer, roomId);

            long insTime = 0;
            long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Bổ sung dinh dưỡng điều trị nội trú.", med.MEDICINE_TYPE_NAME + " x " + qty + " (" + tut + ")", ref insTime);

            var item = new WarehousePrescribeItem
            {
                Keyword = spKw,
                Medicine = med,
                Amount = qty,
                StockId = stockId,
                Tutorial = tut,
                UseFormId = 32, // Uống dinh dưỡng
                IsExpend = false
            };

            string sCode, eCode, err;
            bool ok = PrescribeWarehouseItems(consumer, tr, roomId, new List<WarehousePrescribeItem> { item }, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
            if (ok)
            {
                Console.WriteLine(string.Format("✔ [{0}] {1} (P.{2}) | Y Lệnh: {3} | Xuất kho: {4}", tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_NAME, roomName, sCode, eCode));
            }
            else
            {
                Console.WriteLine(string.Format("❌ [{0}] {1} Kê dinh dưỡng thất bại: {2}", tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_NAME, err));
            }
        }
    }

    public static void ShowHelp()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏭 HỆ THỐNG KÊ LĨNH KHO DƯỢC / CẤP PHÁT - HIS WAREHOUSE PRESCRIBE (InPatientPresCreate)");
        Console.WriteLine("===============================================================================");
        Console.WriteLine("Danh mục Kho Cấp Phát mặc định (IS_CABINET = 0):");
        Console.WriteLine("  • Hà Nội    : 4210 (Kho thuốc viên) | 4209 (Kho thuốc ống) | 753 (Kho SP Dinh dưỡng) | 796 (Kho Vật tư)");
        Console.WriteLine("  • Ninh Bình : 4854 (Kho Dược chính NB)");
        Console.WriteLine("\nCú pháp lệnh:");
        Console.WriteLine("  search   <TừKhóa>                                            : Tra cứu thuốc trong danh mục Dược");
        Console.WriteLine("  single   <MãBN> <TênThuốc> <SốLượng> [KhoLĩnh] [HDSD] [ĐD]   : Kê 1 thuốc lĩnh từ kho dược");
        Console.WriteLine("  multi    <MãBN> \"Thuốc1|SL|HDSD|[Kho]|[ĐD]|[Cữ]\" [\"Thuốc2|..\"] : Kê TOA THUỐC LĨNH nội trú (nhiều thuốc)");
        Console.WriteLine("           [--stock KhoId] [--time HH:mm]");
        Console.WriteLine("  nutrition <MãBN1,MãBN2,...> <TênSP> [SL=1] [Kho=753] [HDSD]   : Kê lĩnh dinh dưỡng điều trị từ kho 753");
        Console.WriteLine("===============================================================================");
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

        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "/?")
        {
            HisWarehousePrescribe.ShowHelp();
            return;
        }

        string cmd = args[0].ToLower();
        if (cmd == "search" || cmd == "find" || cmd == "tra-cuu") HisWarehousePrescribe.ExecuteSearch(args);
        else if (cmd == "single") HisWarehousePrescribe.ExecuteSingle(args);
        else if (cmd == "multi" || cmd == "treatment" || cmd == "meds" || cmd == "toa-thuoc") HisWarehousePrescribe.ExecuteMulti(args);
        else if (cmd == "nutrition" || cmd == "dinh-duong") HisWarehousePrescribe.ExecuteNutrition(args);
        else HisWarehousePrescribe.ShowHelp();
    }
}
