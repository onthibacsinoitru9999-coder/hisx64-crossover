using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
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

        Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        param = new CommonParam();
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
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
            HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkPlaceSDO = workPlaces;
            HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkInfoSDO = workInfo;
        }
    }

    public static long CreateTracking(long treatmentId, string content, long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null)
    {
        InitSession();
        long now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.ID = treatmentId;
        var treatments = adapter.Get<List<V_HIS_TREATMENT>>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
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
                    PrimaryPatientTypeId = patientTypeId,
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
        if (res != null && res.ServiceReqs != null)
        {
            foreach (var sr in res.ServiceReqs)
            {
                Console.WriteLine(string.Format("✔ Chỉ định thành công! Mã y lệnh CLS: {0} (ID: {1})", sr.SERVICE_REQ_CODE, sr.ID));
            }
        }
        else
        {
            throw new Exception("Chỉ định CLS thất bại!");
        }
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("HisClinicalCli: Sẵn sàng thực thi y lệnh lâm sàng nhanh.");
            return;
        }
    }
}
