using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class ServiceTarget
{
    public long ServiceId { get; set; }
    public long RoomId { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceName { get; set; }
    public string Note { get; set; }
    public long? ConditionId { get; set; }

    public ServiceTarget(long sId, long rId, string code = "", string name = "", string note = "", long? condId = null)
    {
        ServiceId = sId;
        RoomId = rId;
        ServiceCode = code;
        ServiceName = name;
        Note = note;
        ConditionId = condId;
    }
}

public class HisClinicalCli
{
    public static BackendAdapter adapter;
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static ApiConsumer mosConsumer;
    public static ApiConsumer sdaConsumer;
    public static string currentToken = null;
    public static string currentDoctorLogin = "034727";
    public static string currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";

    public static readonly Dictionary<string, ServiceTarget> PredefinedServices = new Dictionary<string, ServiceTarget>
    {
        { "TROPONIN_THS", new ServiceTarget(63596, 410, "BM260527.26", "Định lượng Troponin Ths (Sau 28/5/2026)") },
        { "TROPONIN_OLD", new ServiceTarget(5920, 410, "BM02298", "Định lượng Troponin Ths (Trước 28/5/2026)") },
        { "KHI_MAU", new ServiceTarget(5886, 410, "BM02047", "Xét nghiệm Khí máu 11 thông số") },
        { "CBC_LASER", new ServiceTarget(5745, 1772, "BM00110", "Tổng phân tích tế bào máu laser") },
        { "COAGULATION", new ServiceTarget(2658, 1773, "BM00024", "Đông máu cơ bản") },
        { "FIBRINOGEN", new ServiceTarget(5716, 626, "BM00542", "Định lượng Fibrinogen (Clauss tự động)") },
        { "PT_TQ", new ServiceTarget(5713, 626, "BM00531", "Thời gian prothrombin (PT/TQ tự động)") },
        { "APTT_TCK", new ServiceTarget(63622, 626, "BM260527.52", "Thời gian APTT/TCK tự động") },
        { "BLOOD_GROUP_GEL", new ServiceTarget(5783, 1464, "BM01700", "Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động)") },
        { "URE", new ServiceTarget(5923, 410, "BM02304", "Định lượng Urê [Máu]") },
        { "CREATININ", new ServiceTarget(5934, 410, "BM01361", "Định lượng Creatinin (máu)") },
        { "GOT", new ServiceTarget(5834, 410, "BM01352", "Đo hoạt độ AST (GOT)") },
        { "GPT", new ServiceTarget(5833, 410, "BM01347", "Đo hoạt độ ALT (GPT)") },
        { "ELECTROLYTES", new ServiceTarget(5853, 410, "BM00132", "Điện giải đồ (Na, K, Cl)") },
        { "HBA1C", new ServiceTarget(5870, 410, "BM01429", "Định lượng HbA1c", "", 4723) },
        { "URINE_10", new ServiceTarget(5950, 566, "BM02998", "Tổng phân tích nước tiểu (tự động)") },
        { "HBSAG", new ServiceTarget(6135, 871, "BM00859", "HBsAg miễn dịch tự động") },
        { "HCV_AB", new ServiceTarget(6147, 871, "BM00837", "HCV Ab miễn dịch tự động") },
        { "HIV_AB", new ServiceTarget(6020, 871, "BM00871", "HIV Ag/Ab miễn dịch tự động") },
        { "ECG", new ServiceTarget(920, 931, "BM04258", "Điện tim thường (ECG)") },
        { "ECHO_HEART", new ServiceTarget(5569, 1715, "BM00201", "Siêu âm Doppler tim, van tim", "điều dưỡng đưa bằng cáng - cs ii") },
        { "DEXA_2POS", new ServiceTarget(161, 6462, "BM08085", "Đo mật độ xương DEXA [2 vị trí]", "điều dưỡng đưa bằng cáng - cs ii") },
        { "XRAY_CHEST", new ServiceTarget(58112, 17552, "BM21074", "X-quang ngực thẳng số hóa") },
        { "GLUCOSE_BEDSIDE", new ServiceTarget(6217, 5248, "BM02426", "Xét nghiệm đường máu mao mạch tại giường (một lần)") }
    };

    public static void InitSession(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(currentToken)) return;

        param = new CommonParam();
        string tokenCode = null;

        string[] candidateLogs = new string[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "HLSLogSystem.txt"),
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };

        foreach (var logFile in candidateLogs)
        {
            if (File.Exists(logFile))
            {
                try
                {
                    using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string text = sr.ReadToEnd();
                        var lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (lines[i].Contains("TokenCode|"))
                            {
                                int idx = lines[i].IndexOf("TokenCode|") + 10;
                                if (lines[i].Length >= idx + 64)
                                {
                                    tokenCode = lines[i].Substring(idx, 64);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
                if (!string.IsNullOrEmpty(tokenCode)) break;
            }
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    currentDoctorLogin = "034727";
                    currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null)
                    {
                        tokenCode = token.TokenCode;
                        currentDoctorLogin = "vmc";
                        currentDoctorName = "BS Vũ Minh Cường";
                    }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            throw new Exception("Không thể lấy Token xác thực HIS từ cả Live Log và ACS Login!");
        }

        currentToken = tokenCode;
        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
        sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", currentToken, "HIS");
        adapter = new BackendAdapter(param);

        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 5252 },
                    new RoomSDO { RoomId = 5251 },
                    new RoomSDO { RoomId = 5257 }
                }
            };
            var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }
    }

    public static void LookupPatient(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;

        // 1. Try exact match by Patient Code or Treatment Code
        if (!string.IsNullOrEmpty(keyword))
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = keyword.Trim();
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = keyword.Trim();
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.KEY_WORD = keyword.Trim();
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }

        // 2. Check if keyword is a Bed Room search (e.g. 712, 714, 716...)
        if (treatments == null || treatments.Count == 0)
        {
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
            var deptBedRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
            var matchedRooms = deptBedRooms != null ? deptBedRooms.Where(x => x.BED_ROOM_NAME.Contains(keyword) || x.BED_ROOM_CODE.Contains(keyword)).ToList() : null;

            if (matchedRooms != null && matchedRooms.Count > 0)
            {
                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("🏨 DANH SÁCH BỆNH NHÂN THEO BUỒNG: {0}", keyword));
                Console.WriteLine("===============================================================================");
                foreach (var rm in matchedRooms)
                {
                    HisTreatmentBedRoomLViewFilter tbf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = rm.ID, IS_IN_ROOM = true };
                    var pts = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbf, param);
                    if (pts != null && pts.Count > 0)
                    {
                        Console.WriteLine(string.Format("\n📍 {0} ({1} bệnh nhân):", rm.BED_ROOM_NAME, pts.Count));
                        foreach (var p in pts.OrderBy(x => x.BED_NAME))
                        {
                            Console.WriteLine(string.Format("  👉 [{0}] {1} (Mã BN: {2} | Mã ĐT: {3})", 
                                p.BED_NAME ?? "Giường -", p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("\n📍 {0}: Không có bệnh nhân nằm ghép.", rm.BED_ROOM_NAME));
                    }
                }
                Console.WriteLine("===============================================================================");
                return;
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        var tr = treatments.LastOrDefault(x => x.IS_PAUSE != 1) ?? treatments.Last();

        HisTreatmentBedRoomLViewFilter bedFilter = new HisTreatmentBedRoomLViewFilter();
        bedFilter.TREATMENT_IDs = new List<long> { tr.ID };
        bedFilter.IS_IN_ROOM = true;
        var bedRooms = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, bedFilter, param);
        var curBed = bedRooms != null ? bedRooms.LastOrDefault(x => x.REMOVE_TIME == null || x.REMOVE_TIME == 0) : null;

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 THÔNG TIN BỆNH NHÂN: {0} ({1} tuổi - {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_DOB.ToString().Substring(0, 4), tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt điều trị: {2}", tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("Khoa: {0} | Buồng/Giường: {1} - {2}", tr.END_DEPARTMENT_NAME ?? "Khoa 57", curBed != null ? curBed.BED_ROOM_NAME : "Chưa xếp buồng", curBed != null ? curBed.BED_NAME : "-"));
        Console.WriteLine(string.Format("Chẩn đoán ICD: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
        Console.WriteLine(string.Format("BHYT: {0} | Trạng thái: {1}", tr.TDL_HEIN_CARD_NUMBER ?? "Không BHYT", tr.IS_PAUSE == 1 ? "ĐÃ RA VIỆN" : "ĐANG NẰM KHOA"));

        try
        {
            HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
            teinFilter.TDL_TREATMENT_ID = tr.ID;
            var teinList = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);

            if (teinList != null && teinList.Count > 0)
            {
                Func<string, string> getTein = (match) => {
                    var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
                         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
                    return item != null ? item.VALUE + " " + item.TEST_INDEX_UNIT_NAME : "-";
                };

                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("📊 BILAN XÉT NGHIỆM MỚI NHẤT:"));
                Console.WriteLine(string.Format("  • Huyết học: Hb: {0} | WBC: {1} | PLT: {2}", getTein("Hemoglobin"), getTein("Bạch cầu"), getTein("Tiểu cầu")));
                Console.WriteLine(string.Format("  • Đông máu: PT-INR: {0} | Fibrinogen: {1} | APTT: {2}", getTein("INR"), getTein("Fibrinogen"), getTein("APTT")));
                Console.WriteLine(string.Format("  • Sinh hóa: Glucose: {0} | Ure: {1} | Creatinin: {2} | AST: {3} | ALT: {4}", getTein("Glucose"), getTein("Urê"), getTein("Creatinin"), getTein("AST"), getTein("ALT")));
                Console.WriteLine(string.Format("  • Nhóm máu: {0}", getTein("ABO")));
            }
        }
        catch { }
        Console.WriteLine("===============================================================================");
    }

    public static long CreateTracking(long treatmentId, string content, long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null)
    {
        InitSession();
        long now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.ID = treatmentId;
        var treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        if (treatments == null || treatments.Count == 0) throw new Exception("Không tìm thấy đợt điều trị!");
        var tr = treatments[0];

        HIS_TRACKING tracking = new HIS_TRACKING
        {
            TREATMENT_ID = treatmentId,
            DEPARTMENT_ID = 57,
            TRACKING_TIME = now,
            CONTENT = content,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };

        HisTrackingSDO sdo = new HisTrackingSDO
        {
            Tracking = tracking
        };

        if (pulse.HasValue || temp.HasValue || bpMax.HasValue || bpMin.HasValue)
        {
            sdo.Dhst = new HIS_DHST
            {
                TREATMENT_ID = treatmentId,
                EXECUTE_TIME = now,
                EXECUTE_LOGINNAME = currentDoctorLogin,
                EXECUTE_USERNAME = currentDoctorName,
                PULSE = pulse,
                TEMPERATURE = temp,
                BLOOD_PRESSURE_MAX = bpMax,
                BLOOD_PRESSURE_MIN = bpMin
            };
        }

        var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, param);
        if (created == null) throw new Exception("Tạo tờ điều trị thất bại!");

        Console.WriteLine(string.Format("✔ Đã tạo Tờ điều trị ID: {0} lúc {1}", created.ID, created.TRACKING_TIME));
        return created.ID;
    }

    public static void PrescribeMedication(long treatmentId, long trackingId, long medicineTypeId, long stockId, decimal amount, string tutorial, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        if (tutorial.ToUpper().Contains("INSULIN") || tutorial.ToUpper().Contains("ACTRAPID") || tutorial.ToUpper().Contains("LANTUS") || tutorial.ToUpper().Contains("MIXTARD"))
        {
            if (stockId != 810)
            {
                Console.WriteLine("⚠️ CẢNH BÁO AN TOÀN: Đã tự động chuyển kho thuốc Insulin về TỦ TRỰC KHOA 57 (MediStockId = 810).");
                stockId = 810;
            }
            if (amount > 0.5m)
            {
                Console.WriteLine(string.Format("⚠️ CẢNH BÁO AN TOÀN: Liều Insulin là {0} UI. Đang quy đổi UI -> Lọ ({0} / 1000 = {1:F4} lọ).", amount, amount / 1000.0m));
                amount = amount / 1000.0m;
            }
        }

        InPatientPresSDO sdo = new InPatientPresSDO
        {
            TreatmentId = treatmentId,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            RequestRoomId = 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = medicineTypeId,
                    MediStockId = stockId,
                    Amount = amount,
                    PatientTypeId = patientTypeId,
                    Tutorial = tutorial
                }
            }
        };

        var res = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdo, param);
        if (res != null && res.ExpMests != null && res.ExpMests.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã xuất thuốc EXP_MEST: {0}", res.ExpMests[0].EXP_MEST_CODE));
        }
        else if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã y lệnh: {0}", res.ServiceReqs[0].SERVICE_REQ_CODE));
        }
        else
        {
            throw new Exception("Kê đơn thuốc thất bại!");
        }
    }

    public static void AssignClsService(long treatmentId, long trackingId, long serviceId, long roomId, string note, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        AssignServiceSDO sdo = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            InstructionTime = tr.TRACKING_TIME,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            SessionCode = null,
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = serviceId,
                    Amount = 1.0m,
                    PatientTypeId = patientTypeId,
                    PrimaryPatientTypeId = (patientTypeId == 1 ? (long?)null : patientTypeId),
                    RoomId = roomId,
                    InstructionNote = note,
                    MultipleExecute = 1,
                    IsNotUseBhyt = false,
                    IsNoHeinDifference = false,
                    IsGuaranteed = false,
                    EkipInfos = new List<EkipSDO>()
                }
            }
        };

        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdo, param);
        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            foreach (var sr in res.ServiceReqs)
            {
                Console.WriteLine(string.Format("✔ Chỉ định thành công! Mã y lệnh CLS: {0} (ID: {1})", sr.SERVICE_REQ_CODE, sr.ID));
            }
        }
        else
        {
            string err = "Chỉ định CLS thất bại!";
            if (param.Messages != null && param.Messages.Count > 0) err += " " + string.Join("; ", param.Messages);
            throw new Exception(err);
        }
    }

    public static void AssignSurgicalBilan(long treatmentId, long trackingId, string packType, int patientTypeId = 1)
    {
        InitSession();
        packType = packType.ToLower();

        List<ServiceTarget> targetList = new List<ServiceTarget>();
        string title = "";

        if (packType == "cement" || packType == "bxm")
        {
            title = "BILAN MỔ BƠM XI MĂNG CỘT SỐNG (VERTEBROPLASTY)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBA1C"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["ECHO_HEART"]);
            targetList.Add(PredefinedServices["DEXA_2POS"]);
        }
        else if (packType == "spine" || packType == "nepvit")
        {
            title = "BILAN MỔ CỐ ĐỊNH CỘT SỐNG (NẸP VÍT QUA CUỐNG / TLIF)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
            targetList.Add(PredefinedServices["ECHO_HEART"]);
        }
        else if (packType == "hip" || packType == "knee" || packType == "thaykhop")
        {
            title = "BILAN MỔ THAY KHỚP HÁNG / KHỚP GỐI NHÂN TẠO";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "hand" || packType == "viphau")
        {
            title = "BILAN MỔ VI PHẪU / NỐI GÂN MẠCH BÀN TAY";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
        }
        else
        {
            Console.WriteLine(string.Format("❌ Gói Bilan '{0}' không hợp lệ! Hỗ trợ: cement (BXM), spine (Cột sống), hip (Thay khớp), hand (Vi phẫu).", packType));
            return;
        }

        Console.WriteLine(string.Format("=== THỰC THI CHỈ ĐỊNH {0} ===", title));
        Console.WriteLine(string.Format("Treatment ID: {0} | Tờ điều trị ID: {1}", treatmentId, trackingId));

        int successCount = 0;
        int index = 1;
        foreach (var item in targetList)
        {
            try
            {
                AssignClsService(treatmentId, trackingId, item.ServiceId, item.RoomId, item.Note, patientTypeId);
                successCount++;
                Console.WriteLine(string.Format("  ✔ [{0:D2}/{1:D2}] {2} ({3})", index, targetList.Count, item.ServiceName, item.ServiceCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("  ❌ [{0:D2}/{1:D2}] {2}: {3}", index, targetList.Count, item.ServiceName, ex.Message));
            }
            index++;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("KẾT QUẢ CHỈ ĐỊNH GÓI: ✔ Thành công: {0}/{1}", successCount, targetList.Count));
        Console.WriteLine("===============================================================================");
    }

    public static void LookupConsultationDebate(string key)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("👥 TRA CỨU BIÊN BẢN HỘI CHẨN & Ý KIẾN CHUYÊN KHOA CHO: " + key);
        V_HIS_TREATMENT targetTreatment = null;
        if (key.StartsWith("00") || key.Length == 12)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        long trId = 0;
        if (targetTreatment == null && long.TryParse(key, out trId) && trId > 1000000 && trId < 99999999)
        {
            var tf = new HisTreatmentViewFilter { ID = trId };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = key.PadLeft(10, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + key);
            return;
        }

        Console.WriteLine(string.Format("BỆNH NHÂN: {0} ({1} tuổi - {2})", targetTreatment.TDL_PATIENT_NAME, DateTime.Now.Year - int.Parse(targetTreatment.TDL_PATIENT_DOB.ToString().Substring(0, 4)), targetTreatment.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt ĐT: {2}", targetTreatment.TDL_PATIENT_CODE, targetTreatment.TREATMENT_CODE, targetTreatment.ID));
        Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", targetTreatment.ICD_CODE, targetTreatment.ICD_NAME, targetTreatment.ICD_TEXT));
        Console.WriteLine("-------------------------------------------------------------------------------");

        // 1. Kiểm tra HIS_DEBATE
        var df = new HisDebateFilter { TREATMENT_ID = targetTreatment.ID };
        var debates = myAdapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, df, param);

        if (debates != null && debates.Count > 0)
        {
            Console.WriteLine(string.Format("📋 TÌM THẤY {0} BIÊN BẢN HỘI CHẨN CHÍNH (HIS_DEBATE):", debates.Count));
            foreach (var d in debates)
            {
                Console.WriteLine("\n-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("🔹 HỘI CHẨN ID: {0} | Thời gian: {1}", d.ID, d.DEBATE_TIME));
                Console.WriteLine("  • Chẩn đoán: [" + d.ICD_CODE + "] " + d.ICD_NAME + " (" + d.ICD_TEXT + ")");
                Console.WriteLine("  • Địa điểm: " + d.LOCATION);
                if (!string.IsNullOrEmpty(d.TREATMENT_TRACKING)) Console.WriteLine("  • Tóm tắt quá trình ĐT / Khám: " + d.TREATMENT_TRACKING);
                if (!string.IsNullOrEmpty(d.DISCUSSION)) Console.WriteLine("  • Nội dung thảo luận / Xin ý kiến: " + d.DISCUSSION);
                if (!string.IsNullOrEmpty(d.CONCLUSION)) Console.WriteLine("  • Kết luận / Hướng xử trí: " + d.CONCLUSION);

                var duf = new HisDebateUserFilter { DEBATE_ID = d.ID };
                var dUsers = myAdapter.FetchList<HIS_DEBATE_USER>("api/HisDebateUser/Get", mosConsumer, duf, param);
                if (dUsers != null && dUsers.Count > 0)
                {
                    Console.WriteLine("  • Thành viên tham gia:");
                    foreach (var u in dUsers)
                    {
                        string role = u.IS_PRESIDENT == 1 ? "[Chủ tọa]" : (u.IS_SECRETARY == 1 ? "[Thư ký]" : "[Thành viên]");
                        Console.WriteLine(string.Format("    - {0} {1} ({2})", role, u.USERNAME, u.LOGINNAME));
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Chưa có biên bản ghi nhận trong bảng HIS_DEBATE.");
        }

        // 2. Kiểm tra tất cả phiếu yêu cầu mời chuyên khoa liên khoa (HIS_SERVICE_REQ + HIS_SERE_SERV_EXT)
        var srf = new HisServiceReqViewFilter { TREATMENT_ID = targetTreatment.ID };
        var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

        if (reqs != null)
        {
            var consultReqs = reqs.Where(x => 
                x.SERVICE_REQ_TYPE_ID == 1 ||
                (x.EXECUTE_DEPARTMENT_NAME != null && (
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hô hấp") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nhiệt đới") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("truyền nhiễm") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("tim mạch") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hồi sức") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("thần kinh") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nội tiết")
                ))
            ).OrderByDescending(x => x.INTRUCTION_TIME).ToList();

            if (consultReqs.Count > 0)
            {
                Console.WriteLine("\n===============================================================================");
                Console.WriteLine(string.Format("🩺 Ý KIẾN TRẢ LỜI CỦA CÁC CHUYÊN KHOA KHÁCH ({0} PHIẾU CHỈ ĐỊNH):", consultReqs.Count));
                Console.WriteLine("===============================================================================");

                foreach (var cr in consultReqs)
                {
                    string statusBadge = cr.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ CÓ KẾT QUẢ / HOÀN THÀNH" : "🟡 ĐANG CHỜ XỬ LÝ / CHƯA CÓ KẾT QUẢ";
                    Console.WriteLine("\n-------------------------------------------------------------------------------");
                    Console.WriteLine(string.Format("🏢 ĐƠN VỊ: {0} ({1})", cr.EXECUTE_DEPARTMENT_NAME, cr.EXECUTE_ROOM_NAME));
                    Console.WriteLine(string.Format("• Phiếu #{0} [{1}] - Gửi lúc: {2}", cr.SERVICE_REQ_CODE, cr.SERVICE_REQ_TYPE_NAME, cr.INTRUCTION_TIME));
                    Console.WriteLine(string.Format("• Bác sĩ chỉ định: {0} ({1}) -> Khoa: {2}", cr.REQUEST_USERNAME, cr.REQUEST_LOGINNAME, cr.REQUEST_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("• Trạng thái: {0}", statusBadge));
                    if (cr.FINISH_TIME.HasValue) Console.WriteLine(string.Format("• Thời gian hoàn thành: {0}", cr.FINISH_TIME.Value));
                    Console.WriteLine(string.Format("• Bác sĩ hội chẩn/trả lời: {0} ({1})", cr.EXECUTE_USERNAME ?? "(Chưa tiếp nhận)", cr.EXECUTE_LOGINNAME ?? "-"));

                    // Lấy chi tiết ý kiến từ HIS_SERE_SERV_EXT
                    var ssf = new HisSereServFilter { SERVICE_REQ_ID = cr.ID };
                    var sss = myAdapter.FetchList<HIS_SERE_SERV>("api/HisSereServ/Get", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var ss in sss)
                        {
                            var ssef = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                            var sses = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                            if (sses != null && sses.Count > 0)
                            {
                                foreach (var se in sses)
                                {
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION))
                                    {
                                        Console.WriteLine("\n📝 NỘI DUNG Ý KIẾN HỘI CHẨN:");
                                        Console.WriteLine(se.DESCRIPTION);
                                    }
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".")
                                    {
                                        Console.WriteLine("📌 KẾT LUẬN: " + se.CONCLUDE);
                                    }
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE))
                                    {
                                        Console.WriteLine("💡 LỜI DẶN: " + se.INSTRUCTION_NOTE);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void ScanWardRooms()
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 QUÉT DANH SÁCH BỆNH NHÂN CÁC BUỒNG TRỌNG ĐIỂM KHOA 57");
        Console.WriteLine("Phòng: 712, 714, 716, 724, 725");
        Console.WriteLine("===============================================================================");

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);

        if (allBeds == null || allBeds.Count == 0)
        {
            Console.WriteLine("Không tìm thấy bệnh nhân nào đang nằm buồng!");
            return;
        }

        var dept57Beds = allBeds.Where(x => x.DEPARTMENT_ID == 57 && (
            (x.BED_ROOM_NAME != null && (x.BED_ROOM_NAME.Contains("712") || x.BED_ROOM_NAME.Contains("714") || x.BED_ROOM_NAME.Contains("716") || x.BED_ROOM_NAME.Contains("724") || x.BED_ROOM_NAME.Contains("725"))) ||
            (x.BED_NAME != null && (x.BED_NAME.Contains("712") || x.BED_NAME.Contains("714") || x.BED_NAME.Contains("716") || x.BED_NAME.Contains("724") || x.BED_NAME.Contains("725")))
        )).OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME).ToList();

        Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân tại các buồng phụ trách:\n", dept57Beds.Count));
        int stt = 1;
        foreach (var b in dept57Beds)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.ID = b.TREATMENT_ID;
            var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            var tr = tList != null && tList.Count > 0 ? tList[0] : null;

            string patName = tr != null ? tr.TDL_PATIENT_NAME : "N/A";
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "N/A";
            string icd = tr != null ? string.Format("[{0}] {1}", tr.ICD_CODE, tr.ICD_NAME) : "-";

            Console.WriteLine(string.Format("{0:D2}. [{1} - {2}] BN: {3} (Mã: {4}) | TrID: {5}", stt++, b.BED_ROOM_NAME, b.BED_NAME, patName, patCode, b.TREATMENT_ID));
            Console.WriteLine(string.Format("    Chẩn đoán: {0}", icd));
        }
        Console.WriteLine("===============================================================================");
    }

    public static void RunCli(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("🏥 UNIFIED HIS CLINICAL AUTOMATION CLI - KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Cú pháp lệnh:");
            Console.WriteLine("  lookup <patientCode|treatmentCode|name>   : Tra cứu thông tin, buồng giường & Bilan");
            Console.WriteLine("  wardround                                 : Quét danh sách BN buồng 712, 714, 716, 724, 725");
            Console.WriteLine("  create-tracking <trId> <content> [dhst..] : Tạo tờ điều trị và DHST");
            Console.WriteLine("  prescribe <trId> <tkId> <medId> <stId> <amount> <tutorial> : Kê đơn thuốc an toàn");
            Console.WriteLine("  assign-cls <trId> <tkId> <svcId> <roomId> [note] [ptId]    : Chỉ định CLS đơn lẻ");
            Console.WriteLine("  assign-bilan <trId> <tkId> <cement|spine|hip|hand>         : Chỉ định gói Bilan 1-Click");
            Console.WriteLine("  debate <patientCode|treatmentCode>                         : Tra cứu biên bản hội chẩn & ý kiến các chuyên khoa");
            Console.WriteLine("===============================================================================");
            return;
        }

        string cmd = args[0].ToLower();
        try
        {
            if (cmd == "lookup")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu!");
                LookupPatient(args[1]);
            }
            else if (cmd == "debate" || cmd == "hoichan")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc mã đợt điều trị!");
                LookupConsultationDebate(args[1]);
            }
            else if (cmd == "wardround")
            {
                ScanWardRooms();
            }
            else if (cmd == "create-tracking")
            {
                long treatmentId = long.Parse(args[1]);
                string content = args[2];
                long? pulse = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? (long?)long.Parse(args[3]) : null;
                decimal? temp = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? (decimal?)decimal.Parse(args[4]) : null;
                long? bpMax = args.Length > 5 && !string.IsNullOrEmpty(args[5]) ? (long?)long.Parse(args[5]) : null;
                long? bpMin = args.Length > 6 && !string.IsNullOrEmpty(args[6]) ? (long?)long.Parse(args[6]) : null;
                CreateTracking(treatmentId, content, pulse, temp, bpMax, bpMin);
            }
            else if (cmd == "prescribe")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long medId = long.Parse(args[3]);
                long stockId = long.Parse(args[4]);
                decimal amount = decimal.Parse(args[5]);
                string tutorial = args.Length > 6 ? args[6] : "";
                int ptId = args.Length > 7 ? int.Parse(args[7]) : 1;
                PrescribeMedication(treatmentId, trackingId, medId, stockId, amount, tutorial, ptId);
            }
            else if (cmd == "assign-cls")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long serviceId = long.Parse(args[3]);
                long roomId = long.Parse(args[4]);
                string note = args.Length > 5 ? args[5] : "";
                int ptId = args.Length > 6 ? int.Parse(args[6]) : 1;
                AssignClsService(treatmentId, trackingId, serviceId, roomId, note, ptId);
            }
            else if (cmd == "assign-bilan" || cmd == "assign-bilan-cement")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                string packType = (cmd == "assign-bilan-cement" || args.Length < 4) ? "cement" : args[3];
                int ptId = args.Length > 4 ? int.Parse(args[4]) : 1;
                AssignSurgicalBilan(treatmentId, trackingId, packType, ptId);
            }
            else
            {
                Console.WriteLine("Lệnh không hợp lệ: " + cmd);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LỖI THỰC THI: " + ex.Message);
        }
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

        DirectoryInfo rootDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (rootDir != null && !File.Exists(Path.Combine(rootDir.FullName, "Inventec.Core.dll")))
        {
            rootDir = rootDir.Parent;
        }
        if (rootDir != null)
        {
            Directory.SetCurrentDirectory(rootDir.FullName);
        }

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        HisClinicalCli.RunCli(args);
    }
}
