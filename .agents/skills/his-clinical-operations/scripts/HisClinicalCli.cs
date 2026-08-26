using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
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
    public ServiceTarget(long sId, long rId) { ServiceId = sId; RoomId = rId; }
}

public class HisClinicalCli
{
    private static BackendAdapter adapter;
    private static MyAdapter myAdapter = new MyAdapter();
    private static CommonParam param;
    private static string currentToken = null;

    // Fast In-Memory Service Catalog (Zero-Lag Lookup)
    public static readonly Dictionary<string, ServiceTarget> PredefinedServices = new Dictionary<string, ServiceTarget>
    {
        { "TROPONIN_THS", new ServiceTarget(63596, 410) },     // Định lượng Troponin Ths (Sau 28/5/2026) -> Phòng XN Sinh Hóa
        { "TROPONIN_OLD", new ServiceTarget(5920, 410) },      // Định lượng Troponin Ths (Trước 28/5/2026) -> Phòng XN Sinh Hóa
        { "KHI_MAU", new ServiceTarget(5886, 410) },           // Xét nghiệm Khí máu 11 thông số -> Phòng XN Sinh Hóa
        { "CBC_LASER", new ServiceTarget(5745, 1772) },        // Tổng phân tích tế bào máu laser -> Phòng HHTB
        { "COAGULATION", new ServiceTarget(2658, 1773) },      // Đông máu cơ bản -> Phòng Đông máu
        { "URE", new ServiceTarget(2663, 410) },               // Sinh hóa Ure -> Phòng Sinh Hóa
        { "CREATININ", new ServiceTarget(2664, 410) },         // Sinh hóa Creatinin -> Phòng Sinh Hóa
        { "GOT", new ServiceTarget(2665, 410) },               // AST/GOT -> Phòng Sinh Hóa
        { "GPT", new ServiceTarget(2666, 410) },               // ALT/GPT -> Phòng Sinh Hóa
        { "ELECTROLYTES", new ServiceTarget(2668, 410) },      // Điện giải đồ -> Phòng Sinh Hóa
        { "URINE_10", new ServiceTarget(2673, 410) },          // Tổng phân tích nước tiểu -> Phòng Sinh Hóa
        { "ECG", new ServiceTarget(10074, 1771) },             // Điện tim đồ -> Phòng TDCN
        { "XRAY_CHEST", new ServiceTarget(58112, 17552) },     // X-quang ngực thẳng số hóa -> Phòng XQ Nội trú
        { "XRAY_BONE", new ServiceTarget(5576, 1780) },        // X-quang xương khớp -> Phòng XQ
        { "CT_BRAIN", new ServiceTarget(5580, 1785) }          // CT Sọ não -> Phòng CT
    };

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        param = new CommonParam();
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        Console.WriteLine(string.Format("CWD: {0} | Token: {1}", Directory.GetCurrentDirectory(), (token != null ? token.TokenCode : "NULL")));
        if (token != null)
        {
            currentToken = token.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);

            // Bind token session to working rooms on MOS backend
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 }, // Phòng 734 (Phòng trực/khám CTCH)
                    new RoomSDO { RoomId = 5252 }, // Phòng 712 (Buồng bệnh)
                    new RoomSDO { RoomId = 5251 }  // Phòng 714 (Buồng bệnh)
                }
            };
            var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
        }
    }

    public static long CreateTracking(long treatmentId, string content, long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null)
    {
        InitSession();
        long now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.ID = treatmentId;
        var treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
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
                EXECUTE_LOGINNAME = "vmc",
                EXECUTE_USERNAME = "Vũ Minh Cường",
                PULSE = pulse,
                TEMPERATURE = temp,
                BLOOD_PRESSURE_MAX = bpMax,
                BLOOD_PRESSURE_MIN = bpMin
            };
        }

        var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
        if (created == null) throw new Exception("Tạo tờ điều trị thất bại!");

        Console.WriteLine(string.Format("✔ Đã tạo Tờ điều trị ID: {0} lúc {1}", created.ID, created.TRACKING_TIME));
        return created.ID;
    }

    public static void PrescribeMedication(long treatmentId, long trackingId, long medicineTypeId, long stockId, decimal amount, string tutorial, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", ApiConsumers.MosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

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
            RequestLoginName = "vmc",
            RequestUserName = "Vũ Minh Cường",
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

        var res = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, sdo, param);
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
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", ApiConsumers.MosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        AssignServiceSDO sdo = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = 5248,
            RequestLoginName = "vmc",
            RequestUserName = "Vũ Minh Cường",
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
            SessionCode = null, // Mandatory NULL for new assignment
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

        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", ApiConsumers.MosConsumer, sdo, param);
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
            if (param.BugCodes != null && param.BugCodes.Count > 0) err += " [" + string.Join(", ", param.BugCodes) + "]";
            throw new Exception(err);
        }
    }

    public class CementBilanItem
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public long ServiceId { get; set; }
        public long RoomId { get; set; }
        public string Note { get; set; }
        public long? ConditionId { get; set; }

        public CementBilanItem(string name, string code, long svcId, long roomId, string note = "", long? condId = null)
        {
            Name = name;
            Code = code;
            ServiceId = svcId;
            RoomId = roomId;
            Note = note;
            ConditionId = condId;
        }
    }

    public static readonly List<CementBilanItem> StandardCementBilan = new List<CementBilanItem>
    {
        // 1. Huyết học tế bào
        new CementBilanItem("Tổng phân tích tế bào máu laser", "BM00110", 5745, 1772),
        
        // 2. Đông máu (3 xét nghiệm thành phần độc lập tại Phòng 626)
        new CementBilanItem("Định lượng Fibrinogen (Clauss tự động)", "BM00542", 5716, 626),
        new CementBilanItem("Thời gian prothrombin (PT/TQ tự động)", "BM00531", 5713, 626),
        new CementBilanItem("Thời gian APTT/TCK tự động [28/5/2026]", "BM260527.52", 63622, 626),
        
        // 3. Định nhóm máu (Gelcard tự động tại Phòng 1464)
        new CementBilanItem("Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động)", "BM01700", 5783, 1464),
        
        // 4. Sinh hóa máu (Phòng 410)
        new CementBilanItem("Định lượng Urê [Máu]", "BM02304", 5923, 410),
        new CementBilanItem("Định lượng Creatinin (máu)", "BM01361", 5934, 410),
        new CementBilanItem("Đo hoạt độ AST (GOT)", "BM01352", 5834, 410),
        new CementBilanItem("Đo hoạt độ ALT (GPT)", "BM01347", 5833, 410),
        new CementBilanItem("Điện giải đồ (Na, K, Cl)", "BM00132", 5853, 410),
        new CementBilanItem("Định lượng HbA1c", "BM01429", 5870, 410, "", 4723), // Kèm Condition 4723
        
        // 5. Nước tiểu (Phòng 566)
        new CementBilanItem("Tổng phân tích nước tiểu (tự động)", "BM02998", 5950, 566),
        
        // 6. Vi sinh / Miễn dịch (Phòng 871)
        new CementBilanItem("HBsAg miễn dịch tự động", "BM00859", 6135, 871),
        new CementBilanItem("HCV Ab miễn dịch tự động", "BM00837", 6147, 871),
        new CementBilanItem("HIV Ag/Ab miễn dịch tự động", "BM00871", 6020, 871),
        
        // 7. Thăm dò chức năng & Chẩn đoán hình ảnh
        new CementBilanItem("Điện tim thường (ECG)", "BM04258", 920, 931), // Tiểu phẫu Khoa 57
        new CementBilanItem("Siêu âm Doppler tim, van tim", "BM00201", 5569, 1715, "điều dưỡng đưa bằng cáng - cs ii"), // Phòng 1715
        new CementBilanItem("Đo mật độ xương DEXA [2 vị trí]", "BM08085", 161, 6462, "điều dưỡng đưa bằng cáng - cs ii") // P202 Nhà K2
    };

    public static void AssignBilanCement(long treatmentId, long trackingId, int patientTypeId = 1)
    {
        InitSession();

        if (trackingId <= 0) trackingId = 9730387;

        HisTrackingViewFilter tf = new HisTrackingViewFilter();
        tf.ID = trackingId;
        var trackings = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tf, param);
        
        long trackingTime = 0;
        string icdCode = "M80.0", icdName = "Lún xẹp đốt sống do loãng xương", icdSubCode = "", icdText = "";

        if (trackings != null && trackings.Count > 0)
        {
            var tr = trackings[0];
            trackingTime = tr.TRACKING_TIME;
            icdCode = tr.ICD_CODE;
            icdName = tr.ICD_NAME;
            icdSubCode = tr.ICD_SUB_CODE;
            icdText = tr.ICD_TEXT;
        }
        else
        {
            trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        }

        Console.WriteLine(string.Format("=== THỰC THI CHỈ ĐỊNH BILAN MỔ BƠM XI MĂNG CHUẨN LÂM SÀNG BẠCH MAI ==="));
        Console.WriteLine(string.Format("Treatment ID: {0} | Tờ điều trị ID: {1} lúc {2}", treatmentId, trackingId, trackingTime));

        int successCount = 0;
        int index = 1;
        foreach (var item in StandardCementBilan)
        {
            try
            {
                var reqDetail = new ServiceReqDetailSDO
                {
                    ServiceId = item.ServiceId,
                    Amount = 1.0m,
                    PatientTypeId = patientTypeId,
                    PrimaryPatientTypeId = (patientTypeId == 1 ? (long?)null : patientTypeId),
                    RoomId = item.RoomId,
                    InstructionNote = item.Note ?? "",
                    MultipleExecute = 1,
                    IsNotUseBhyt = false,
                    IsNoHeinDifference = false,
                    IsGuaranteed = false,
                    EkipInfos = new List<EkipSDO>()
                };

                if (item.ConditionId.HasValue)
                {
                    reqDetail.ServiceConditionId = item.ConditionId.Value;
                }

                AssignServiceSDO sdo = new AssignServiceSDO
                {
                    TreatmentId = treatmentId,
                    RequestRoomId = 5248,
                    RequestLoginName = "vmc",
                    RequestUserName = "Vũ Minh Cường",
                    InstructionTime = trackingTime,
                    InstructionTimes = new List<long> { trackingTime },
                    UseTimes = new List<long> { trackingTime },
                    TrackingId = trackingId,
                    TrackingInfos = new List<TrackingInfoSDO>
                    {
                        new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
                    },
                    IcdCode = icdCode,
                    IcdName = icdName,
                    IcdSubCode = icdSubCode,
                    IcdText = icdText,
                    SessionCode = null,
                    ServiceReqDetails = new List<ServiceReqDetailSDO> { reqDetail }
                };

                CommonParam pOrd = new CommonParam();
                var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", ApiConsumers.MosConsumer, sdo, pOrd);
                if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
                {
                    successCount++;
                    Console.WriteLine(string.Format("  ✔ [{0:D2}/18] {1} ({2}) -> Y lệnh: {3} (Room ID: {4})",
                        index, item.Name, item.Code, res.ServiceReqs[0].SERVICE_REQ_CODE, item.RoomId));
                }
                else
                {
                    string err = (pOrd.Messages != null && pOrd.Messages.Count > 0) ? string.Join("; ", pOrd.Messages) : "Lỗi hệ thống MOS";
                    Console.WriteLine(string.Format("  ❌ [{0:D2}/18] {1} ({2}): {3}", index, item.Name, item.Code, err));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("  ❌ [{0:D2}/18] {1}: {2}", index, item.Name, ex.Message));
            }
            index++;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("KẾT QUẢ CHỈ ĐỊNH BILAN: ✔ Thành công: {0}/{1}", successCount, StandardCementBilan.Count));
        Console.WriteLine("===============================================================================");
    }

    public static void AssignRemaining3(long treatmentId, long trackingId, int patientTypeId = 1)
    {
        InitSession();

        if (trackingId <= 0) trackingId = 9730387;

        HisTrackingViewFilter tf = new HisTrackingViewFilter();
        tf.ID = trackingId;
        var trackings = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tf, param);
        
        long trackingTime = 0;
        string icdCode = "M80.0", icdName = "Lún xẹp đốt sống do loãng xương", icdSubCode = "", icdText = "";

        if (trackings != null && trackings.Count > 0)
        {
            var tr = trackings[0];
            trackingTime = tr.TRACKING_TIME;
            icdCode = tr.ICD_CODE;
            icdName = tr.ICD_NAME;
            icdSubCode = tr.ICD_SUB_CODE;
            icdText = tr.ICD_TEXT;
            Console.WriteLine(string.Format("Tìm thấy tờ điều trị ID: {0} lúc {1}", trackingId, trackingTime));
        }
        else
        {
            trackingTime = 20260826083653; // Default tracking time from today
            Console.WriteLine(string.Format("Sử dụng tờ điều trị ID mặc định: {0} lúc {1}", trackingId, trackingTime));
        }

        Console.WriteLine("=== THỬ NGHIỆM CHỈ ĐỊNH ĐÍCH DANH NHÓM MÁU VÀ HBA1C ===");

        var bgCandidates = new[]
        {
            new { Id = 73898L, Code = "NB260620.5848", Name = "Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động)" },
            new { Id = 73907L, Code = "NB260620.5857", Name = "Định nhóm máu hệ ABO (ống nghiệm)" },
            new { Id = 73890L, Code = "NB260620.5840", Name = "Định nhóm máu hệ Rh(D) (ống nghiệm)" },
            new { Id = 5783L, Code = "BM01700", Name = "Định nhóm máu hệ ABO, Rh(D) (Gelcard)" },
            new { Id = 5675L, Code = "BM01886", Name = "Định nhóm máu hệ ABO (ống nghiệm)" },
            new { Id = 5785L, Code = "BM01286", Name = "Định nhóm máu hệ Rh(D) (ống nghiệm)" },
            new { Id = 5757L, Code = "BM00122", Name = "Định nhóm máu hệ ABO, Rh(D)" },
            new { Id = 24562L, Code = "BM21671", Name = "Định nhóm máu hệ ABO bằng giấy" }
        };

        var hbCandidates = new[]
        {
            new { Id = 73996L, Code = "NB260620.5946", Name = "Định lượng HbA1c [Máu]" },
            new { Id = 5870L, Code = "BM01429", Name = "Định lượng HbA1c" },
            new { Id = 2683L, Code = "BM00049", Name = "Định lượng HbA1c [Máu]" }
        };

        long[] allRooms = new long[] { 1772, 1773, 1771, 2993, 410, 566, 1852, 17773 };
        int[] ptTypes = new int[] { 1, 2 };

        bool bgDone = false;
        foreach (var bg in bgCandidates)
        {
            if (bgDone) break;
            foreach (var pt in ptTypes)
            {
                if (bgDone) break;
                foreach (var rId in allRooms)
                {
                    if (bgDone) break;
                    AssignServiceSDO sdo = new AssignServiceSDO
                    {
                        TreatmentId = treatmentId,
                        RequestRoomId = 5248,
                        RequestLoginName = "vmc",
                        RequestUserName = "Vũ Minh Cường",
                        InstructionTime = trackingTime,
                        InstructionTimes = new List<long> { trackingTime },
                        UseTimes = new List<long> { trackingTime },
                        TrackingId = trackingId,
                        TrackingInfos = new List<TrackingInfoSDO>
                        {
                            new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
                        },
                        IcdCode = icdCode,
                        IcdName = icdName,
                        IcdSubCode = icdSubCode,
                        IcdText = icdText,
                        SessionCode = null,
                        ServiceReqDetails = new List<ServiceReqDetailSDO>
                        {
                            new ServiceReqDetailSDO
                            {
                                ServiceId = bg.Id,
                                Amount = 1.0m,
                                PatientTypeId = pt,
                                PrimaryPatientTypeId = (pt == 1 ? (long?)null : pt),
                                RoomId = rId,
                                InstructionNote = "",
                                MultipleExecute = 1,
                                IsNotUseBhyt = (pt == 2),
                                IsNoHeinDifference = false,
                                IsGuaranteed = false,
                                EkipInfos = new List<EkipSDO>()
                            }
                        }
                    };

                    CommonParam pOrd = new CommonParam();
                    var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", ApiConsumers.MosConsumer, sdo, pOrd);
                    if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
                    {
                        bgDone = true;
                        Console.WriteLine(string.Format("  ✔ [THÀNH CÔNG] {0} (SVC ID: {1}, Room ID: {2}, PT: {3}) -> Y lệnh: {4}",
                            bg.Name, bg.Id, rId, pt, res.ServiceReqs[0].SERVICE_REQ_CODE));
                    }
                    else
                    {
                        var fields = pOrd.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        string info = string.Join(", ", fields.Select(p => p.Name + "=" + (p.GetValue(pOrd, null) != null ? p.GetValue(pOrd, null).ToString() : "null")));
                        Console.WriteLine(string.Format("  ❌ NULL RES {0} ({1}) tại Room {2}, PT {3}: {4}", bg.Name, bg.Code, rId, pt, info));
                        break; // Print one failure and break
                    }
                }
            }
        }

        bool hbDone = false;
        foreach (var hb in hbCandidates)
        {
            if (hbDone) break;
            foreach (var pt in ptTypes)
            {
                if (hbDone) break;
                foreach (var rId in allRooms)
                {
                    if (hbDone) break;
                    AssignServiceSDO sdo = new AssignServiceSDO
                    {
                        TreatmentId = treatmentId,
                        RequestRoomId = 5248,
                        RequestLoginName = "vmc",
                        RequestUserName = "Vũ Minh Cường",
                        InstructionTime = trackingTime,
                        InstructionTimes = new List<long> { trackingTime },
                        UseTimes = new List<long> { trackingTime },
                        TrackingId = trackingId,
                        TrackingInfos = new List<TrackingInfoSDO>
                        {
                            new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
                        },
                        IcdCode = icdCode,
                        IcdName = icdName,
                        IcdSubCode = icdSubCode,
                        IcdText = icdText,
                        SessionCode = null,
                        ServiceReqDetails = new List<ServiceReqDetailSDO>
                        {
                            new ServiceReqDetailSDO
                            {
                                ServiceId = hb.Id,
                                Amount = 1.0m,
                                PatientTypeId = pt,
                                PrimaryPatientTypeId = (pt == 1 ? (long?)null : pt),
                                RoomId = rId,
                                InstructionNote = "",
                                MultipleExecute = 1,
                                IsNotUseBhyt = (pt == 2),
                                IsNoHeinDifference = false,
                                IsGuaranteed = false,
                                EkipInfos = new List<EkipSDO>()
                            }
                        }
                    };

                    CommonParam pOrd = new CommonParam();
                    var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", ApiConsumers.MosConsumer, sdo, pOrd);
                    if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
                    {
                        hbDone = true;
                        Console.WriteLine(string.Format("  ✔ [THÀNH CÔNG] {0} (SVC ID: {1}, Room ID: {2}, PT: {3}) -> Y lệnh: {4}",
                            hb.Name, hb.Id, rId, pt, res.ServiceReqs[0].SERVICE_REQ_CODE));
                    }
                }
            }
        }
    }



    public static void RunCli(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("HisClinicalCli: Sẵn sàng thực thi y lệnh lâm sàng nhanh.");
            Console.WriteLine("Commands:");
            Console.WriteLine("  create-tracking <treatmentId> <content> [pulse] [temp] [bpMax] [bpMin]");
            Console.WriteLine("  assign-cls <treatmentId> <trackingId> <serviceId> <roomId> [note] [patientTypeId]");
            Console.WriteLine("  assign-bilan-cement <treatmentId> <trackingId> [patientTypeId]");
            Console.WriteLine("  assign-remaining3 <treatmentId> <trackingId> [patientTypeId]");
            return;
        }

        string cmd = args[0].ToLower();
        try
        {
            if (cmd == "create-tracking")
            {
                long treatmentId = long.Parse(args[1]);
                string content = args[2];
                long? pulse = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? (long?)long.Parse(args[3]) : null;
                decimal? temp = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? (decimal?)decimal.Parse(args[4]) : null;
                long? bpMax = args.Length > 5 && !string.IsNullOrEmpty(args[5]) ? (long?)long.Parse(args[5]) : null;
                long? bpMin = args.Length > 6 && !string.IsNullOrEmpty(args[6]) ? (long?)long.Parse(args[6]) : null;
                CreateTracking(treatmentId, content, pulse, temp, bpMax, bpMin);
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
            else if (cmd == "assign-bilan-cement")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                int ptId = args.Length > 3 ? int.Parse(args[3]) : 1;
                AssignBilanCement(treatmentId, trackingId, ptId);
            }
            else if (cmd == "assign-remaining3")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                int ptId = args.Length > 3 ? int.Parse(args[3]) : 1;
                AssignRemaining3(treatmentId, trackingId, ptId);
            }
            else
            {
                Console.WriteLine("Lệnh không hợp lệ: " + cmd);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("LỖI THỰC THI: " + ex.Message);
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



