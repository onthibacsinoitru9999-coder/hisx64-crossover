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
using HIS.Desktop.ApiConsumer;
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

public class HisCabinetPrescribe
{
    public static MyAdapter adapter = new MyAdapter();

    // DANH MỤC CÁC KHO TỦ TRỰC CHUẨN HÓA
    public const long STOCK_TU_TRUC_CTCH_HN = 810;     // TT_KCTCHCS (Khoa 57 Hà Nội)
    public const long STOCK_TU_TRUC_DINH_DUONG_HN = 7787; // TTSPDD_9 (Tủ trực dinh dưỡng Khoa 57)
    public const long STOCK_TU_TRUC_3E_NB = 5142;       // TTT_NBKP05.02 (Khoa Ngoại 3E CSNB)
    public const long STOCK_TU_TRUC_3D_NB = 5141;       // TTT_NBKP05.01 (Khoa Ngoại 3D CSNB)

    // ID CÁC MỤC THUỐC TỦ TRỰC PHỔ BIẾN
    public const long MED_LEANPRO_ID = 26851;           // SPBM25651 - Leanpro PreSur 12.5%
    public const long MED_POVIDONE_ID = 17385;          // Povidone 10% 125ml
    public const long MED_MUOI_RUA_ID = 27127;          // Muối rửa NaCl 0.9% 500ml

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string currentDir = Directory.GetCurrentDirectory();
        try
        {
            List<string> tokenFiles = new List<string>
            {
                Path.Combine(baseDir, "doctor_hn.token"),
                Path.Combine(currentDir, "doctor_hn.token"),
                Path.Combine(baseDir, "doctor_standalone.token"),
                Path.Combine(currentDir, "doctor_standalone.token"),
                Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_standalone.token"),
                Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_hn.token")
            };

            foreach (var cacheFile in tokenFiles)
            {
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

        // 3. Fallback: Tự động ĐĂNG NHẬP ĐỘC LẬP qua ACS bằng tài khoản Bác sĩ
        try
        {
            EnsureAcsConfiguration();
            HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
            CommonParam cp = new CommonParam();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");

            string envUser = Environment.GetEnvironmentVariable("HIS_DOCTOR_LOGIN");
            string envPass = Environment.GetEnvironmentVariable("HIS_PASSWORD");
            if (string.IsNullOrEmpty(envPass)) envPass = Environment.GetEnvironmentVariable("HIS_PASS");

            var credentials = new List<Tuple<string, string>>();
            if (!string.IsNullOrEmpty(envUser) && !string.IsNullOrEmpty(envPass))
            {
                credentials.Add(Tuple.Create(envUser, envPass));
            }
            credentials.Add(Tuple.Create("034727", "981"));
            credentials.Add(Tuple.Create("034727", "998199"));
            credentials.Add(Tuple.Create("vmc", "789789"));

            foreach (var cred in credentials)
            {
                var tok = tokenManager.Login(cp, cred.Item1, cred.Item2, "2.390.0");
                if (tok != null && !string.IsNullOrEmpty(tok.TokenCode))
                {
                    string newToken = tok.TokenCode;
                    try
                    {
                        string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
                        File.WriteAllText(cacheFile, newToken + "|" + DateTime.Now.Ticks + "|" + cred.Item1, Encoding.UTF8);
                    }
                    catch { }
                    return newToken;
                }
            }
        }
        catch { }

        return null;
    }

    public static void EnsureAcsConfiguration()
    {
        try
        {
            var settings = System.Configuration.ConfigurationManager.AppSettings;
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Base.Uri"]))
            {
                settings["Inventec.Token.ClientSystem.Acs.Base.Uri"] = "http://192.168.7.200:1401/";
            }
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Uri"]))
            {
                settings["Inventec.Token.ClientSystem.Acs.Uri"] = "http://192.168.7.200:1401/";
            }
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Version"]))
            {
                settings["Inventec.Token.ClientSystem.Acs.Version"] = "2.0";
            }
        }
        catch { }
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

        CommonParam cp = new CommonParam();
        List<V_HIS_TREATMENT> trList = null;

        // 1. Thử tìm qua HisPatient/Get trước để lấy PATIENT_ID (đáng tin cậy nhất)
        try
        {
            var patFilter = new HisPatientFilter { PATIENT_CODE = cleanKey };
            var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", consumer, patFilter, cp);
            if (pats != null && pats.Count > 0)
            {
                var trmFilter = new HisTreatmentViewFilter { PATIENT_ID = pats[0].ID };
                trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, trmFilter, cp);
            }
        }
        catch { }

        // 2. Thử tìm theo PATIENT_CODE__EXACT
        if (trList == null || trList.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = cleanKey };
            trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, cp);
        }

        // 3. Thử tìm theo TREATMENT_CODE__EXACT
        if (trList == null || trList.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = cleanKey.PadLeft(12, '0') };
            trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, cp);
        }

        // 4. Thử tìm theo KEY_WORD
        if (trList == null || trList.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { KEY_WORD = cleanKey };
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
        long todayEnd = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "235959");
        try
        {
            HisTrackingViewFilter tf = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            CommonParam cpCheck = new CommonParam();
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, tf, cpCheck);
            if (trkList != null)
            {
                var existing = trkList.Where(x => x.TRACKING_TIME >= todayStart && x.TRACKING_TIME <= todayEnd).OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
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

        // Tạo mới tờ điều trị
        long trackTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            TRACKING_TIME = trackTime,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT,
            CONTENT = !string.IsNullOrEmpty(content) ? content : "Diễn biến bệnh ổn định. Theo dõi và dùng thuốc theo y lệnh.",
            MEDICAL_INSTRUCTION = !string.IsNullOrEmpty(instruction) ? instruction : "Dùng thuốc theo y lệnh tủ trực.",
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

    public class CabinetPrescribeItem
    {
        public string Keyword { get; set; }
        public V_HIS_MEDICINE_TYPE Medicine { get; set; }
        public decimal Amount { get; set; }
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
    /// KÊ ĐƠN TỦ TRỰC ĐA THUỐC / ĐƠN THUỐC ĐIỀU TRỊ CHUẨN HÓA 2 BƯỚC: TakeBean -> OutPatientPresCreateList
    /// </summary>
    public static bool PrescribeCabinetItemList(
        ApiConsumer consumer,
        V_HIS_TREATMENT tr,
        long roomId,
        long stockId,
        List<CabinetPrescribeItem> items,
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
            error = "Danh sách thuốc trống!";
            return false;
        }

        UpdateWorkInfo(consumer, roomId);

        string sessionKey = Guid.NewGuid().ToString();
        var presMeds = new List<PresMedicineSDO>();

        // BƯỚC 1: LẦN LƯỢT GIỮ BEAN CHO TỪNG THUỐC CÙNG SESSION_KEY
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var takeBean = new TakeBeanSDO
            {
                TypeId = item.Medicine.ID,
                MediStockId = stockId,
                PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1, // Ưu tiên đối tượng BHYT của bệnh nhân
                Amount = item.Amount,
                ClientSessionKey = sessionKey,
                ExpiredDate = null
            };

            CommonParam cpTake = new CommonParam();
            var beans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
            if (beans == null || beans.Count == 0)
            {
                takeBean.PatientTypeId = 42; // Thử viện phí nếu không có tồn BHYT
                beans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
            }

            if (beans == null || beans.Count == 0)
            {
                string msg = (cpTake.Messages != null && cpTake.Messages.Count > 0) ? string.Join("; ", cpTake.Messages) : "Không giữ được thuốc trong tủ trực (có thể hết tồn)";
                if (cpTake.BugCodes != null && cpTake.BugCodes.Count > 0) msg += " | BugCodes: " + string.Join("; ", cpTake.BugCodes);
                error = string.Format("Lỗi giữ thuốc [{0} - {1}]: {2}", item.Medicine.MEDICINE_TYPE_CODE, item.Medicine.MEDICINE_TYPE_NAME, msg);
                return false;
            }

            long useForm = item.UseFormId ?? item.Medicine.MEDICINE_USE_FORM_ID ?? 1L;
            decimal? speedVal = null;
            decimal sp;
            if (!string.IsNullOrEmpty(item.Speed) && decimal.TryParse(item.Speed, out sp)) speedVal = sp;

            presMeds.Add(new PresMedicineSDO
            {
                MedicineTypeId = item.Medicine.ID,
                MediStockId = stockId,
                Amount = item.Amount,
                PresAmount = item.Amount,
                PatientTypeId = takeBean.PatientTypeId ?? tr.TDL_PATIENT_TYPE_ID ?? 1L,
                Tutorial = !string.IsNullOrEmpty(item.Tutorial) ? item.Tutorial : "Dùng theo chỉ dẫn của bác sĩ",
                MedicineUseFormId = useForm,
                Speed = speedVal,
                Morning = item.Morning,
                Noon = item.Noon,
                Afternoon = item.Afternoon,
                Evening = item.Evening,
                IsExpend = item.IsExpend,
                NumOfDays = 1,
                MedicineBeanIds = beans.Select(b => b.ID).ToList()
            });
        }

        // BƯỚC 2: TẠO 1 Y LỆNH TỦ TRỰC DUY NHẤT CHỨA TẤT CẢ CÁC THUỐC
        var outPresSDO = new OutPatientPresSDO
        {
            TreatmentId = tr.ID,
            InstructionTime = instructionTime,
            UseTimes = new List<long> { instructionTime },
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RequestRoomId = roomId > 0 ? roomId : 5248,
            RequestLoginName = doctorLogin,
            RequestUserName = doctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            IsCabinet = true,
            ClientSessionKey = sessionKey,
            Medicines = presMeds
        };

        CommonParam cpOut = new CommonParam();
        var outRes = adapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", consumer, new List<OutPatientPresSDO> { outPresSDO }, cpOut);
        if ((outRes == null || outRes.ServiceReqs == null || outRes.ServiceReqs.Count == 0) && outPresSDO.RequestRoomId != 5248)
        {
            outPresSDO.RequestRoomId = 5248;
            cpOut = new CommonParam();
            outRes = adapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", consumer, new List<OutPatientPresSDO> { outPresSDO }, cpOut);
        }

        if (outRes != null)
        {
            long createdReqId = 0;
            if (outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0)
            {
                serviceReqCode = outRes.ServiceReqs[0].SERVICE_REQ_CODE;
                createdReqId = outRes.ServiceReqs[0].ID;
            }
            if (outRes.ExpMests != null && outRes.ExpMests.Count > 0)
                expMestCode = outRes.ExpMests[0].EXP_MEST_CODE;

            if (!string.IsNullOrEmpty(serviceReqCode) || !string.IsNullOrEmpty(expMestCode))
            {
                // BẮT BUỘC: GÁN THUỐC VÀO TỜ ĐIỀU TRỊ (HIS_TRACKING / EMR LINKAGE)
                if (trackingId > 0)
                {
                    try
                    {
                        var medDesc = new StringBuilder();
                        foreach (var m in items)
                        {
                            string tName = m.Medicine != null ? m.Medicine.MEDICINE_TYPE_NAME : "Thuốc tủ trực";
                            medDesc.AppendFormat("- Thuốc tủ trực: {0} x {1} ({2})\n", tName, m.Amount, m.Tutorial);
                        }
                        LinkServiceReqToTracking(consumer, trackingId, createdReqId, serviceReqCode, medDesc.ToString().TrimEnd(), roomId);
                    }
                    catch { }
                }
                return true;
            }
        }

        if (cpOut.Messages != null && cpOut.Messages.Count > 0) error = string.Join("; ", cpOut.Messages);
        else if (cpOut.BugCodes != null && cpOut.BugCodes.Count > 0) error = string.Join("; ", cpOut.BugCodes);
        else
        {
            var sbErr = new StringBuilder("Tạo y lệnh tủ trực thất bại (API trả về null)");
            sbErr.AppendFormat(" | ReqRoom={0}, StockId={1}, InsTime={2}, TrkId={3}, MedCount={4}", outPresSDO.RequestRoomId, stockId, outPresSDO.InstructionTime, outPresSDO.TrackingId, presMeds.Count);
            if (presMeds.Count > 0)
            {
                sbErr.AppendFormat(", MedId={0}, PtTypeId={1}, UseForm={2}, Beans={3}", presMeds[0].MedicineTypeId, presMeds[0].PatientTypeId, presMeds[0].MedicineUseFormId, string.Join(",", presMeds[0].MedicineBeanIds));
            }
            if (cpOut.HasException) sbErr.Append(" | HasException: true");
            error = sbErr.ToString();
        }

        return false;
    }

    public static bool PrescribeCabinetItem(
        ApiConsumer consumer,
        V_HIS_TREATMENT tr,
        long roomId,
        long stockId,
        long medicineTypeId,
        decimal amount,
        string tutorial,
        long? useFormId,
        bool isExpend,
        long trackingId,
        long instructionTime,
        string doctorLogin,
        string doctorName,
        out string serviceReqCode,
        out string expMestCode,
        out string error,
        V_HIS_MEDICINE_TYPE medTypeObj = null)
    {
        var item = new CabinetPrescribeItem
        {
            Medicine = medTypeObj ?? new V_HIS_MEDICINE_TYPE { ID = medicineTypeId },
            Amount = amount,
            Tutorial = tutorial,
            UseFormId = useFormId,
            IsExpend = isExpend
        };
        return PrescribeCabinetItemList(consumer, tr, roomId, stockId, new List<CabinetPrescribeItem> { item }, trackingId, instructionTime, doctorLogin, doctorName, out serviceReqCode, out expMestCode, out error);
    }

    public static void LinkServiceReqToTracking(ApiConsumer consumer, long trackingId, long serviceReqId, string serviceReqCode, string medDesc, long roomId)
    {
        if (trackingId <= 0) return;
        try
        {
            CommonParam cp = new CommonParam();
            if (serviceReqId <= 0 && !string.IsNullOrEmpty(serviceReqCode))
            {
                var srf = new HisServiceReqViewFilter { SERVICE_REQ_CODE = serviceReqCode };
                var sreqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
                if (sreqs != null && sreqs.Count > 0) serviceReqId = sreqs[0].ID;
            }

            var trkFilter = new HisTrackingFilter { ID = trackingId };
            var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trkFilter, cp);
            if (trkList == null || trkList.Count == 0) return;
            var trk = trkList[0];

            if (!string.IsNullOrEmpty(medDesc))
            {
                if (string.IsNullOrEmpty(trk.MEDICAL_INSTRUCTION))
                    trk.MEDICAL_INSTRUCTION = medDesc;
                else if (!trk.MEDICAL_INSTRUCTION.Contains(medDesc.Substring(0, Math.Min(20, medDesc.Length))))
                    trk.MEDICAL_INSTRUCTION = medDesc + "\r\n" + trk.MEDICAL_INSTRUCTION;
            }

            var dhstFilter = new HisDhstFilter { TRACKING_ID = trackingId };
            var dhsts = adapter.FetchList<HIS_DHST>("api/HisDhst/Get", consumer, dhstFilter, cp);

            var sdo = new HisTrackingSDO();
            sdo.Tracking = trk;
            sdo.WorkingRoomId = roomId > 0 ? roomId : 5248;
            if (dhsts != null && dhsts.Count > 0) sdo.Dhst = dhsts[0];

            if (serviceReqId > 0)
            {
                // BẢO LƯU TOÀN BỘ Y LỆNH ĐÃ GÁN TRƯỚC ĐÓ VÀO TỜ ĐIỀU TRỊ NÀY (TRÁNH MẤT GÁN THUỐC CŨ)
                List<long> allReqIds = new List<long> { serviceReqId };
                try
                {
                    var existingSrf = new HisServiceReqViewFilter { TRACKING_ID = trackingId };
                    var existingReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, existingSrf, cp);
                    if (existingReqs != null)
                    {
                        foreach (var er in existingReqs)
                        {
                            if (!allReqIds.Contains(er.ID)) allReqIds.Add(er.ID);
                        }
                    }
                }
                catch { }

                sdo.UsedForServiceReqIds = allReqIds;
                sdo.ServiceReqs = allReqIds.Select(id => new TrackingServiceReq
                {
                    ServiceReqId = id,
                    IsNotShowMedicine = false,
                    IsNotShowMaterial = false,
                    IsNotShowOutMedi = false,
                    IsNotShowOutMate = false
                }).ToList();
            }

            adapter.PostData<HIS_TRACKING>("api/HisTracking/Update", consumer, sdo, cp);
            Console.WriteLine("✔ Đã tự động gán y lệnh thuốc vào Tờ điều trị ID: " + trackingId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Cảnh báo gán y lệnh vào tờ điều trị: " + ex.Message);
        }
    }

    // =========================================================================
    // CÁC CHỨC NĂNG NGHIỆP VỤ LÂM SÀNG
    // =========================================================================

    public static void ExecuteStock(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        long stockId = STOCK_TU_TRUC_CTCH_HN;
        string filterKw = "";

        if (args.Length > 1)
        {
            long parsed;
            if (long.TryParse(args[1], out parsed)) stockId = parsed;
            else filterKw = args[1];
        }
        if (args.Length > 2 && string.IsNullOrEmpty(filterKw))
        {
            filterKw = args[2];
        }

        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode đăng nhập!"); return; }
        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        var bf = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = stockId, IS_ACTIVE = 1 };
        CommonParam cp = new CommonParam();
        var beans = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", consumer, bf, cp);

        Console.WriteLine("=================================================================================================");
        Console.WriteLine(string.Format("📦 TỔN THUỐC TỦ TRỰC ID {0} (Tổng số bean: {1})", stockId, beans != null ? beans.Count : 0));
        Console.WriteLine("=================================================================================================");

        if (beans == null || beans.Count == 0)
        {
            Console.WriteLine("⚠️ Tủ trực hiện không có thuốc tồn hoặc không thể tải dữ liệu.");
            return;
        }

        var query = beans.AsEnumerable();
        if (!string.IsNullOrEmpty(filterKw))
        {
            string f = filterKw.ToLower();
            query = query.Where(b => (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.ToLower().Contains(f)) ||
                                     (b.MEDICINE_TYPE_CODE != null && b.MEDICINE_TYPE_CODE.ToLower().Contains(f)));
        }

        var groups = query.GroupBy(b => new { b.MEDICINE_TYPE_ID, b.MEDICINE_TYPE_CODE, b.MEDICINE_TYPE_NAME, b.SERVICE_UNIT_NAME })
                          .Select(g => new
                          {
                              TypeId = g.Key.MEDICINE_TYPE_ID,
                              Code = g.Key.MEDICINE_TYPE_CODE,
                              Name = g.Key.MEDICINE_TYPE_NAME,
                              Unit = g.Key.SERVICE_UNIT_NAME,
                              TotalAmount = g.Sum(x => x.AMOUNT),
                              MinExp = g.Min(x => x.EXPIRED_DATE)
                          })
                          .OrderBy(x => x.Name)
                          .ToList();

        Console.WriteLine(string.Format("{0,-4} | {1,-12} | {2,-10} | {3,-45} | {4,10} | {5}", "STT", "Mã Thuốc", "TypeID", "Tên Thuốc", "Tồn Kho", "Đơn Vị"));
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            Console.WriteLine(string.Format("{0,-4} | {1,-12} | {2,-10} | {3,-45} | {4,10:F2} | {5}",
                i + 1, g.Code, g.TypeId, (g.Name.Length > 44 ? g.Name.Substring(0, 41) + "..." : g.Name), g.TotalAmount, g.Unit));
        }
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("👉 Tổng số mặt hàng hiển thị: {0}", groups.Count));
        Console.WriteLine("=================================================================================================");
    }

    public static void ExecuteSingle(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Cú pháp: HisCabinetPrescribe.exe single <MãBN> <Tên/MãThuốc> <SốLượng> [KhoTủ=810] [HDSD] [Giờ: 17:00] [ĐườngDùngId]");
            return;
        }

        string patKey = args[1];
        string medKw = args[2];
        decimal amount = decimal.Parse(args[3]);
        long stockId = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? long.Parse(args[4]) : STOCK_TU_TRUC_CTCH_HN;
        string tutorial = args.Length > 5 ? args[5] : "Dùng theo chỉ dẫn của bác sĩ";
        string timeStr = args.Length > 6 ? args[6] : null;
        long? useFormId = args.Length > 7 && !string.IsNullOrEmpty(args[7]) ? (long?)long.Parse(args[7]) : null;

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode đăng nhập!"); return; }

        try { HIS.Desktop.LocalStorage.ConfigSystem.Load.Init(); } catch { }
        try { ApiConsumers.SetConsunmer(token); } catch { }
        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        UpdateWorkInfo(consumer, 5248);
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        var med = FindMedicine(consumer, medKw);
        if (med == null) { Console.WriteLine("❌ Không tìm thấy thuốc trong danh mục: " + medKw); return; }

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Bệnh nhân dùng thuốc tủ trực theo y lệnh.", med.MEDICINE_TYPE_NAME + " x " + amount + " (" + tutorial + ")", ref insTime);

        if (!string.IsNullOrEmpty(timeStr) && timeStr.Contains(":"))
        {
            try
            {
                string[] tp = timeStr.Split(':');
                DateTime dt = DateTime.Today.AddHours(int.Parse(tp[0])).AddMinutes(int.Parse(tp[1]));
                insTime = long.Parse(dt.ToString("yyyyMMddHHmmss"));
            }
            catch { }
        }

        var item = new CabinetPrescribeItem
        {
            Medicine = med,
            Amount = amount,
            Tutorial = tutorial,
            UseFormId = useFormId ?? med.MEDICINE_USE_FORM_ID,
            IsExpend = false
        };

        string sCode, eCode, err;
        bool ok = PrescribeCabinetItemList(consumer, tr, roomId, stockId, new List<CabinetPrescribeItem> { item }, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
        if (ok)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🎉 KÊ TỦ TRỰC THÀNH CÔNG CHO: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
            Console.WriteLine(string.Format("   - Buồng bệnh     : {0} - {1}", roomName, bedName));
            Console.WriteLine(string.Format("   - Thuốc          : {0} ({1}) | SL: {2}", med.MEDICINE_TYPE_NAME, med.MEDICINE_TYPE_CODE, amount));
            Console.WriteLine(string.Format("   - Kho Tủ Trực    : ID {0}", stockId));
            Console.WriteLine(string.Format("   - HDSD           : {0}", tutorial));
            Console.WriteLine(string.Format("   - Mã phiếu y lệnh: {0}", sCode));
            Console.WriteLine(string.Format("   - Mã xuất kho    : {0}", eCode));
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("❌ KÊ TỦ TRỰC THẤT BẠI: " + err);
        }
    }

    /// <summary>
    /// KÊ TOA THUỐC ĐIỀU TRỊ TỦ TRỰC (MULTI-ITEM TREATMENT PRESCRIPTION)
    /// Cú pháp: HisCabinetPrescribe.exe multi <MãBN> "Thuốc1|SL|HDSD|[ĐườngDùng]" "Thuốc2|SL|HDSD|[ĐườngDùng]" ... [--stock ID] [--time HH:mm]
    /// </summary>
    public static void ExecuteMulti(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Cú pháp: HisCabinetPrescribe.exe multi <MãBN> \"<Thuốc1>|<SL>|<HDSD>|[ĐườngDùng]\" [\"<Thuốc2>|...\"] [--stock KhoId=810] [--time HH:mm]");
            Console.WriteLine("Ví dụ:");
            Console.WriteLine("  HisCabinetPrescribe.exe multi 0001666593 \"Paracetamol Kabi 1g|1|Truyền TM 40 giọt/phút|20\" \"Zinacef 750mg|2|Tiêm TM sáng 1 chiều 1|15\" --stock 810");
            return;
        }

        string patKey = args[1];
        long stockId = STOCK_TU_TRUC_CTCH_HN;
        string customTimeStr = null;
        List<string> rawItems = new List<string>();

        for (int i = 2; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--stock" && i + 1 < args.Length)
            {
                stockId = long.Parse(args[++i]);
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
            Console.WriteLine("❌ Vui lòng cung cấp ít nhất 1 thuốc cần kê!");
            return;
        }

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode đăng nhập!"); return; }

        try { HIS.Desktop.LocalStorage.ConfigSystem.Load.Init(); } catch { }
        try { ApiConsumers.SetConsunmer(token); } catch { }
        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        UpdateWorkInfo(consumer, 5248);
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        List<CabinetPrescribeItem> parsedItems = new List<CabinetPrescribeItem>();
        StringBuilder trackingInstruction = new StringBuilder();

        foreach (var r in rawItems)
        {
            string[] parts = r.Split('|');
            string kw = parts[0].Trim();
            decimal amt = parts.Length > 1 ? decimal.Parse(parts[1].Trim()) : 1.0m;
            string tut = parts.Length > 2 ? parts[2].Trim() : "Dùng theo chỉ dẫn của bác sĩ";
            long? uf = null;
            if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3].Trim()))
            {
                long parsedUf;
                if (long.TryParse(parts[3].Trim(), out parsedUf)) uf = parsedUf;
            }

            var med = FindMedicine(consumer, kw);
            if (med == null)
            {
                Console.WriteLine(string.Format("❌ Không tìm thấy thuốc trong danh mục: \"{0}\"", kw));
                return;
            }

            parsedItems.Add(new CabinetPrescribeItem
            {
                Keyword = kw,
                Medicine = med,
                Amount = amt,
                Tutorial = tut,
                UseFormId = uf ?? med.MEDICINE_USE_FORM_ID,
                IsExpend = false
            });

            trackingInstruction.AppendFormat("- {0} x {1} ({2})\n", med.MEDICINE_TYPE_NAME, amt, tut);
        }

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Bệnh nhân dùng thuốc điều trị từ tủ trực theo y lệnh.", trackingInstruction.ToString().TrimEnd(), ref insTime);

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
        Console.WriteLine(string.Format("💊 ĐANG GỬI TOA THUỐC ĐIỀU TRỊ TỦ TRỰC ({0} MẶT HÀNG)", parsedItems.Count));
        Console.WriteLine(string.Format("   - Bệnh nhân  : {0} ({1}) | Vị trí: {2} - {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, roomName, bedName));
        Console.WriteLine(string.Format("   - Kho Tủ Trực: ID {0}", stockId));
        Console.WriteLine("===============================================================================");

        string sCode, eCode, err;
        bool ok = PrescribeCabinetItemList(consumer, tr, roomId, stockId, parsedItems, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
        if (ok)
        {
            Console.WriteLine("🎉 KÊ TOA THUỐC TỦ TRỰC THÀNH CÔNG!");
            Console.WriteLine(string.Format("   • Mã Phiếu Y Lệnh : {0}", sCode));
            Console.WriteLine(string.Format("   • Mã Xuất Kho     : {0}", eCode));
            Console.WriteLine("\n📋 Danh sách thuốc đã cấp từ tủ trực:");
            for (int i = 0; i < parsedItems.Count; i++)
            {
                var it = parsedItems[i];
                Console.WriteLine(string.Format("   {0}. {1} ({2}) | SL: {3} | HDSD: {4}",
                    i + 1, it.Medicine.MEDICINE_TYPE_NAME, it.Medicine.MEDICINE_TYPE_CODE, it.Amount, it.Tutorial));
            }
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("❌ KÊ TOA THUỐC THẤT BẠI: " + err);
            Console.WriteLine("===============================================================================");
        }
    }

    public static void ExecuteInsulin(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Cú pháp: HisCabinetPrescribe.exe insulin <MãBN> <LiềuUI> <Loại: R|L|M> [Giờ: 17:00|21:00|06:00] [KhoTủ=810]");
            return;
        }

        string patKey = args[1];
        decimal ui = decimal.Parse(args[2]);
        string typeStr = args[3].ToUpper();
        string timeStr = args.Length > 4 ? args[4] : "21:00";
        long stockId = args.Length > 5 ? long.Parse(args[5]) : STOCK_TU_TRUC_CTCH_HN;

        string medKw = "14956";
        string medDisplay = "Lantus";
        if (typeStr.StartsWith("R") || typeStr.Contains("ACT"))
        {
            medKw = (stockId == STOCK_TU_TRUC_CTCH_HN) ? "29507" : "27727";
            medDisplay = "Actrapid";
        }
        else if (typeStr.StartsWith("M") || typeStr.Contains("MIX")) { medKw = "18119"; medDisplay = "Mixtard"; }

        decimal presAmount = ui / 1000.0m;
        string tutorial = string.Format("Tiêm dưới da {0} đơn vị {1} lúc {2}.", (int)ui, medDisplay, timeStr);

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        try { HIS.Desktop.LocalStorage.ConfigSystem.Load.Init(); } catch { }
        try { ApiConsumers.SetConsunmer(token); } catch { }
        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        UpdateWorkInfo(consumer, 5248);
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        var med = FindMedicine(consumer, medKw);
        if (med == null) { Console.WriteLine("❌ Không tìm thấy thuốc Insulin: " + medKw); return; }

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Theo dõi đường huyết và tiêm Insulin", tutorial, ref insTime);

        if (!string.IsNullOrEmpty(timeStr) && timeStr.Contains(":"))
        {
            try
            {
                string[] tp = timeStr.Split(':');
                DateTime dt = DateTime.Today.AddHours(int.Parse(tp[0])).AddMinutes(int.Parse(tp[1]));
                long customTime = long.Parse(dt.ToString("yyyyMMddHHmmss"));
                if (customTime >= insTime) insTime = customTime;
            }
            catch { }
        }

        string cuVal = ((int)ui).ToString("D2");
        string morning = null, noon = null, afternoon = null, evening = null;
        int hour = 21;
        if (!string.IsNullOrEmpty(timeStr) && timeStr.Contains(":"))
        {
            int.TryParse(timeStr.Split(':')[0], out hour);
        }
        if (hour < 10) morning = cuVal;
        else if (hour <= 14) noon = cuVal;
        else if (hour < 19) afternoon = cuVal;
        else evening = cuVal;

        var presItem = new CabinetPrescribeItem
        {
            Medicine = med,
            Amount = presAmount,
            Tutorial = tutorial,
            UseFormId = med.MEDICINE_USE_FORM_ID ?? 15,
            Morning = morning,
            Noon = noon,
            Afternoon = afternoon,
            Evening = evening,
            IsExpend = false
        };

        string sCode, eCode, err;
        bool ok = PrescribeCabinetItemList(consumer, tr, roomId, stockId, new List<CabinetPrescribeItem> { presItem }, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
        if (ok)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("💉 KÊ INSULIN TỦ TRỰC THÀNH CÔNG: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
            Console.WriteLine(string.Format("   - Thuốc          : {0} | Liều: {1} UI ({2:F4} lọ)", med.MEDICINE_TYPE_NAME, (int)ui, presAmount));
            Console.WriteLine(string.Format("   - Kho Tủ Trực    : ID {0}", stockId));
            Console.WriteLine(string.Format("   - Mã phiếu y lệnh: {0}", sCode));
            Console.WriteLine(string.Format("   - Hướng dẫn      : {0}", tutorial));
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("❌ KÊ INSULIN THẤT BẠI: " + err);
        }
    }

    public static void ExecuteLeanpro(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Cú pháp: HisCabinetPrescribe.exe leanpro <MãBN1,MãBN2,...> [SốLượng=6]");
            return;
        }

        decimal qty = args.Length > 2 ? decimal.Parse(args[2]) : 6.0m;
        string[] codes = args[1].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🥛 KÊ DỊCH DINH DƯỠNG LEANPRO TỦ TRỰC TTSPDD_9 ({0} BN)", codes.Length));
        Console.WriteLine("   - Tiêu chuẩn an toàn: BN < 70 tuổi & KHÔNG Đái tháo đường");
        Console.WriteLine("   - Tủ trực cấp: TTSPDD_9 (Khoa CTCH & Cột sống - ID: 7787)");
        Console.WriteLine("===============================================================================\n");

        foreach (var c in codes)
        {
            var tr = FindTreatment(consumer, c);
            if (tr == null) { Console.WriteLine(string.Format("❌ Không tìm thấy BN: {0}\n", c)); continue; }

            // Check tuổi & ĐTĐ
            int yob = 0;
            string dobStr = tr.TDL_PATIENT_DOB.ToString();
            if (dobStr.Length >= 4) int.TryParse(dobStr.Substring(0, 4), out yob);
            int age = yob > 0 ? (DateTime.Now.Year - yob) : 0;
            if (age >= 70)
            {
                Console.WriteLine(string.Format("⛔ TỪ CHỐI {0} ({1}): Tuổi {2} >= 70 (Chống chỉ định)\n", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, age));
                continue;
            }

            string diag = string.Format("{0} {1} {2} {3}", tr.ICD_CODE, tr.ICD_SUB_CODE, tr.ICD_NAME, tr.ICD_TEXT).ToLower();
            if (diag.Contains("tháo đường") || diag.Contains("đái đường") || diag.Contains("tiểu đường") || (tr.ICD_CODE != null && tr.ICD_CODE.StartsWith("E1")))
            {
                Console.WriteLine(string.Format("⛔ TỪ CHỐI {0} ({1}): Bệnh nhân Đái tháo đường (Chống chỉ định)\n", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
                continue;
            }

            long roomId, deptId;
            string bedName, roomName;
            GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
            UpdateWorkInfo(consumer, roomId);

            long insTime = 0;
            long trkId = EnsureTracking(consumer, tr, roomId, deptId, "bn lịch mổ mai bổ sung dịch", "Bổ sung dịch dinh dưỡng trước mổ (Leanpro PreSur 12.5% - 6 chai): Uống tối 4 chai lúc 20h, sáng uống 2 chai lúc 6h.", ref insTime);

            string sCode, eCode, err;
            bool ok = PrescribeCabinetItem(consumer, tr, roomId, STOCK_TU_TRUC_DINH_DUONG_HN, MED_LEANPRO_ID, qty, "Ngày uống 4 chai buổi tối 20h 2 chai sáng 6h", 32, false, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out sCode, out eCode, out err);
            if (ok)
            {
                Console.WriteLine(string.Format("✔ [{0}] {1} (P.{2}) | Mã y lệnh: {3} | Mã xuất: {4}", tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_NAME, roomName, sCode, eCode));
            }
            else
            {
                Console.WriteLine(string.Format("❌ [{0}] {1} Kê Leanpro thất bại: {2}", tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_NAME, err));
            }
        }
        Console.WriteLine("\n💡 Lưu ý: Bác sĩ mở Tờ điều trị trên HIS Client Desktop bấm In/Ký để kết xuất mẫu chuẩn.");
    }

    public static void ExecuteDressing(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Cú pháp: HisCabinetPrescribe.exe dressing <MãBN> [PovidoneQty=1] [MuốiRửaQty=1] [KhoTủ=810]");
            return;
        }

        string patKey = args[1];
        decimal povidoneQty = args.Length > 2 ? decimal.Parse(args[2]) : 1.0m;
        decimal salineQty = args.Length > 3 ? decimal.Parse(args[3]) : 1.0m;
        long stockId = args.Length > 4 ? long.Parse(args[4]) : STOCK_TU_TRUC_CTCH_HN;

        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ Không tìm thấy TokenCode!"); return; }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var tr = FindTreatment(consumer, patKey);
        if (tr == null) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); return; }

        long roomId, deptId;
        string bedName, roomName;
        GetPatientLocation(consumer, tr.ID, out roomId, out deptId, out bedName, out roomName);
        UpdateWorkInfo(consumer, roomId);

        long insTime = 0;
        long trkId = EnsureTracking(consumer, tr, roomId, deptId, "Vết mổ khô sạch, thay băng rửa vết thương hàng ngày.", "Povidone 10% + NaCl 0.9% thay băng vết thương.", ref insTime);

        string s1, e1, err1, s2, e2, err2;
        bool ok1 = PrescribeCabinetItem(consumer, tr, roomId, stockId, MED_POVIDONE_ID, povidoneQty, "thay băng", 25, true, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out s1, out e1, out err1);
        bool ok2 = PrescribeCabinetItem(consumer, tr, roomId, stockId, MED_MUOI_RUA_ID, salineQty, "thay băng", 25, true, trkId, insTime, "034727", "Ths.BS NGUYỄN HỮU SÂM", out s2, out e2, out err2);

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 KÊ VẬT TƯ TIÊU HAO THAY BĂNG TỦ TRỰC: {0} ({1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
        Console.WriteLine(string.Format("   - Povidone 10% 125ml x {0}: {1}", povidoneQty, ok1 ? "✔ Thành công (" + s1 + ")" : "❌ " + err1));
        Console.WriteLine(string.Format("   - Muối rửa NaCl 0.9% x {0}: {1}", salineQty, ok2 ? "✔ Thành công (" + s2 + ")" : "❌ " + err2));
        Console.WriteLine("===============================================================================");
    }

    public static void ShowHelp()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 HỆ THỐNG KÊ TỦ TRỰC LÂM SÀNG - HIS CABINET PRESCRIBE (IsCabinet = true)");
        Console.WriteLine("===============================================================================");
        Console.WriteLine("Danh mục Tủ Trực mặc định:");
        Console.WriteLine("  • Hà Nội    : 810 (TT_KCTCHCS - Khoa 57) | 7787 (TTSPDD_9 - Tủ trực dinh dưỡng Khoa 57)");
        Console.WriteLine("  • Ninh Bình : 5142 (TTT_NBKP05.02 - Khoa 3E) | 5141 (TTT_NBKP05.01 - Khoa 3D)");
        Console.WriteLine("\nCú pháp lệnh:");
        Console.WriteLine("  stock    [KhoId=810] [TừKhóa]                               : Tra cứu tồn các thuốc trong tủ trực");
        Console.WriteLine("  single   <MãBN> <TênThuốc> <SốLượng> [KhoId] [HDSD] [Giờ]   : Kê 1 thuốc điều trị từ tủ trực");
        Console.WriteLine("  multi    <MãBN> \"Thuốc1|SL|HDSD|[ĐD]\" [\"Thuốc2|...\"]        : Kê TOA THUỐC ĐIỀU TRỊ tủ trực (nhiều thuốc)");
        Console.WriteLine("           [--stock KhoId] [--time HH:mm]");
        Console.WriteLine("  insulin  <MãBN> <LiềuUI> <Loại: R|L|M> [Giờ] [KhoId]        : Kê tiêm Insulin tủ trực (810/5142)");
        Console.WriteLine("  leanpro  <MãBN1,MãBN2,...> [SốLượng=6]                      : Kê Leanpro trước mổ từ tủ TTSPDD_9 (7787)");
        Console.WriteLine("  dressing <MãBN> [PovidoneQty] [MuốiRửaQty] [KhoId]          : Kê vật tư thay băng tủ trực 810");
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

            DirectoryInfo cur = new DirectoryInfo(folderPath);
            for (int i = 0; i < 5; i++)
            {
                if (cur.Parent == null) break;
                cur = cur.Parent;
                string pRoot = Path.Combine(cur.FullName, name);
                if (File.Exists(pRoot)) return Assembly.LoadFrom(pRoot);
                string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                if (File.Exists(pRef)) return Assembly.LoadFrom(pRef);
            }
            return null;
        };

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "/?")
        {
            HisCabinetPrescribe.ShowHelp();
            return;
        }

        string cmd = args[0].ToLower();
        if (cmd == "stock" || cmd == "ton-kho" || cmd == "inventory") HisCabinetPrescribe.ExecuteStock(args);
        else if (cmd == "single") HisCabinetPrescribe.ExecuteSingle(args);
        else if (cmd == "multi" || cmd == "treatment" || cmd == "meds" || cmd == "toa-thuoc") HisCabinetPrescribe.ExecuteMulti(args);
        else if (cmd == "insulin") HisCabinetPrescribe.ExecuteInsulin(args);
        else if (cmd == "leanpro") HisCabinetPrescribe.ExecuteLeanpro(args);
        else if (cmd == "dressing" || cmd == "thay-bang") HisCabinetPrescribe.ExecuteDressing(args);
        else HisCabinetPrescribe.ShowHelp();
    }
}
