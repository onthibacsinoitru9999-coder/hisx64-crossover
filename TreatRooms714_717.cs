using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
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

public class PatientTargetInfo
{
    public string PatientCode { get; set; }
    public string RoomBed { get; set; }
    public bool IsSurgeryTomorrow { get; set; }
    public string SheetNote { get; set; }
    public string RationType { get; set; }
    public string TrackingContent { get; set; }
}

public class Program
{
    static void Main()
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
        RunService();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunService()
    {
        TreatEngine.Execute();
    }
}

public class TreatEngine
{
    static MyAdapter myAdapter = new MyAdapter();
    static CommonParam param = new CommonParam();
    static ApiConsumer mosConsumer;
    static string currentDoctorLogin = "034727";
    static string currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";

    public static void Execute()
    {
        try { Load.Init(); } catch { }
        InitSession();

        var targets = new List<PatientTargetInfo>
        {
            // BUỒNG 717
            new PatientTargetInfo
            {
                PatientCode = "0004033304",
                RoomBed = "717 - Giường 7",
                IsSurgeryTomorrow = false,
                SheetNote = "Hẹp ống sống C4-5, C5-6 / THA - Điện cơ tổn thương rễ S1, Dự kiến mổ T5",
                RationType = "BT01",
                TrackingContent = "Khám 08h00: Bệnh nhân tỉnh, tiếp xúc tốt, huyết áp ổn định 125/80 mmHg. Đau mỏi vùng cổ gáy lan vai tay hai bên, tê bì tay P > T, cơ lực chi trên 4/5. Đã có kết quả điện cơ và hoàn thiện bilan tiền phẫu. Tiếp tục điều trị nội khoa giảm đau, giãn cơ, kiểm soát huyết áp. Dự kiến phẫu thuật thứ 5.\n\nY lệnh:\n1. Thuốc dùng theo đơn (Kho Dược)\n2. Đeo nẹp cổ mềm khi ngồi dậy, hạn chế cúi xoay cổ mạnh\n3. Nhịn ăn uống từ 22h tối nay chuẩn bị mổ phiên sáng mai\n4. Suất ăn bệnh lý BT01"
            },
            new PatientTargetInfo
            {
                PatientCode = "0004048113",
                RoomBed = "717 - Giường 08",
                IsSurgeryTomorrow = false,
                SheetNote = "Viêm đốt sống đĩa đệm - Lịch mổ T5, soi da k nấm, RL tiêu hóa đã ổn",
                RationType = "BT01",
                TrackingContent = "Khám 08h00: Bệnh nhân tỉnh, không sốt, thể trạng trung bình. Đại tiện phân thành khuôn, bụng mềm không chướng, tiêu hóa ổn định. Đau vùng cột sống thắt lưng mức độ vừa, hạn chế vận động cúi ngửa. Bilan trước mổ đã hoàn thiện. Dự kiến phẫu thuật cố định cột sống thứ 5.\n\nY lệnh:\n1. Thuốc dùng theo đơn\n2. Chế độ ăn uống dinh dưỡng đầy đủ (BT01)\n3. Nhịn ăn từ 22h tối nay chuẩn bị mổ phiên"
            },
            new PatientTargetInfo
            {
                PatientCode = "0003380587",
                RoomBed = "717 - Giường 10",
                IsSurgeryTomorrow = false,
                SheetNote = "Sau mổ CĐCS L4-5 (Đã mổ phiên 29/09, Hậu phẫu N1)",
                RationType = "BT01",
                TrackingContent = "Khám 08h00: Hậu phẫu ngày 01 sau mổ làm sạch, cố định cột sống L4-5, ghép xương. Bệnh nhân tỉnh, tiếp xúc tốt, không sốt. Đau vết mổ mức độ vừa, dẫn lưu vết mổ ra ít dịch hồng thấm băng (~30ml). Bụng mềm, vận động cảm giác hai chân còn tốt, không tê bì tăng thêm.\n\nY lệnh:\n1. Kháng sinh, giảm đau, bảo vệ dạ dày theo y lệnh\n2. Thay băng vết mổ, theo dõi lượng dịch dẫn lưu\n3. Xoay trở tại giường, tập vận động thụ động chi dưới\n4. Suất ăn bồi dưỡng BT01"
            },
            new PatientTargetInfo
            {
                PatientCode = "0003002690",
                RoomBed = "717 - Giường 10",
                IsSurgeryTomorrow = false,
                SheetNote = "Bong điểm bám PCL gối T (Đã mổ nội soi 29/09, Hậu phẫu N1 - BN không ăn cơm viện)",
                RationType = null,
                TrackingContent = "Khám 08h00: Hậu phẫu ngày 01 sau phẫu thuật nội soi khâu cố định điểm bám dây chằng chéo sau / sụn chêm khớp gối trái. Bệnh nhân tỉnh, tiếp xúc tốt, sinh hiệu ổn định. Khớp gối trái cố định nẹp Zimmer, đau vết mổ vừa, ngọn chi hồng ấm, vận động các ngón chân tốt, mạch mu chân bắt rõ.\n\nY lệnh:\n1. Kháng sinh, giảm đau chống phù nề theo y lệnh\n2. Bất động gối trái bằng nẹp Zimmer, kê cao chân\n3. Tập gồng cơ tứ đầu đùi và vận động cổ bàn chân tại giường\n4. Dinh dưỡng tự túc theo nguyện vọng người bệnh"
            },
            new PatientTargetInfo
            {
                PatientCode = "0003863470",
                RoomBed = "717 - Giường 10a",
                IsSurgeryTomorrow = true,
                SheetNote = "HC ống cổ tay T nặng - MỔ PHIÊN HÔM NAY 30/09 (BS Ngô Đăng Quang)",
                RationType = null,
                TrackingContent = "Khám 08h00: Bệnh nhân tỉnh, tiếp xúc tốt. Tê bì, dị cảm nhiều ngón 1, 2, 3 và nửa ngón 4 bàn tay trái, teo nhẹ cơ ô mô cái. Bilan tiền phẫu đã hoàn thiện đầy đủ, không có chống chỉ định phẫu thuật. Chuẩn bị phẫu thuật phiên hôm nay: Cắt giải phóng dây chằng ngang cổ tay trái.\n\nY lệnh:\n1. Nhịn ăn nhịn uống hoàn toàn, chuẩn bị phẫu thuật phiên hôm nay\n2. Đánh dấu vị trí phẫu thuật cổ tay trái\n3. Vệ sinh vùng mổ, chuyển phòng phẫu thuật theo lịch điều phối"
            },

            // BUỒNG 714
            new PatientTargetInfo
            {
                PatientCode = "0001344789",
                RoomBed = "714 - Giường 18/17",
                IsSurgeryTomorrow = false,
                SheetNote = "Xẹp T12, L1 / ĐTĐ, THA, Loãng xương, Loét tỳ đè độ II - Bilan xét mổ",
                RationType = "DD01",
                TrackingContent = "Khám 08h00: Bệnh nhân tỉnh, tiếp xúc chậm do di chứng TBMMN cũ. Đau vùng CSTL mức độ vừa khi thay đổi tư thế, hạn chế ngồi dậy. Vết loét vùng cùng cụt độ II đáy sạch, không chảy mủ. Đường huyết và huyết áp đang được kiểm soát ổn định.\n\nY lệnh:\n1. Thuốc giảm đau, điều trị loãng xương và kiểm soát huyết áp theo đơn\n2. Tiếp tục theo dõi ĐMMM và tiêm Insulin theo phác đồ\n3. Chăm sóc loét tỳ đè: bôi xanh methylen, xoay trở tư thế mỗi 2 giờ, đệm hơi chống loét\n4. Suất ăn đái tháo đường DD01"
            },
            new PatientTargetInfo
            {
                PatientCode = "0004093343",
                RoomBed = "714 - Giường 89",
                IsSurgeryTomorrow = false,
                SheetNote = "Gãy đầu trên x.chày P, gãy x.mác P (Vào viện 29/09 sau TNGT)",
                RationType = "BT01",
                TrackingContent = "Khám 08h00: Bệnh nhân tỉnh, tiếp xúc tốt, sinh hiệu ổn định. Cẳng chân phải sưng nề, đau nhiều tại vị trí gãy, đang được bất động nẹp bột đùi cẳng bàn chân. Ngọn chi hồng ấm, vận động cảm giác các ngón chân còn tốt, mạch mu chân bắt rõ.\n\nY lệnh:\n1. Thuốc giảm đau, giảm phù nề chống viêm theo đơn\n2. Bất động nẹp bột cẳng chân phải, kê cao chi\n3. Theo dõi chèn ép khoang, mạch mu chân và cảm giác vận động ngọn chi\n4. Suất ăn bệnh lý BT01"
            },
            new PatientTargetInfo
            {
                PatientCode = "0004093319",
                RoomBed = "714 - Giường 92",
                IsSurgeryTomorrow = false,
                SheetNote = "Vết thương hở bàn chân P (Mổ cấp cứu 29/09, Hậu phẫu N1)",
                RationType = "BT01",
                TrackingContent = "Khám 08h00: Hậu phẫu ngày 01 sau mổ cấp cứu cắt lọc, xử trí vết thương hở bàn chân phải. Bệnh nhân tỉnh, không sốt. Vết mổ bàn chân phải còn đau, băng thấm ít dịch hồng, ngọn chi hồng, cử động ngón chân được, mạch mu chân rõ. Đã tiêm SAT phòng uốn ván.\n\nY lệnh:\n1. Kháng sinh, giảm đau chống phù nề theo đơn\n2. Thay băng vết mổ, giữ khô sạch\n3. Kê cao chân phải, hạn chế đi lại tỳ đè lên chân tổn thương\n4. Suất ăn bồi dưỡng BT01"
            }
        };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🚀 TIẾN HÀNH ĐIỀU TRỊ BUỒNG 714 & 717 - NGÀY 30/09/2026 (8H SÁNG)");
        Console.WriteLine("===============================================================================");

        long targetDate = 20260930080000;
        long targetDayStart = 20260930000000;
        long targetDayEnd = 20260930235959;

        foreach (var p in targets)
        {
            Console.WriteLine("\n-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("👉 [{0}] {1} (Mã BN: {2})", p.RoomBed, p.SheetNote, p.PatientCode));
            
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = p.PatientCode.PadLeft(10, '0') };
            var listT = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (listT == null || listT.Count == 0)
            {
                tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = p.PatientCode.PadLeft(12, '0') };
                listT = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            }
            if (listT == null || listT.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy đợt điều trị!");
                continue;
            }

            var tr = listT.OrderByDescending(x => x.IN_TIME).First();
            long treatmentId = tr.ID;
            Console.WriteLine(string.Format("   BN: {0} | Mã ĐT: {1} | ID: {2} | Khoa: {3}", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE, tr.ID, tr.LAST_DEPARTMENT_ID));

            // Room ID resolution
            long reqRoomId = 5248; // P734 default
            try
            {
                var tbrFilter = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = treatmentId, IS_IN_ROOM = true };
                var tbrList = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrFilter, param);
                if (tbrList != null && tbrList.Count > 0)
                {
                    var br = tbrList.First();
                    var bFilter = new HisBedRoomViewFilter { ID = br.BED_ROOM_ID };
                    var brList = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bFilter, param);
                    if (brList != null && brList.Count > 0 && brList[0].ROOM_ID > 0)
                    {
                        reqRoomId = brList[0].ROOM_ID;
                    }
                }
            } catch { }

            // Ensure work info
            try
            {
                var wi = new WorkInfoSDO { Rooms = new List<RoomSDO> { new RoomSDO { RoomId = reqRoomId }, new RoomSDO { RoomId = 5248 } } };
                myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, param);
            } catch { }

            // Step 1: Treatment Tracking (Tờ điều trị lúc 08:00 30/09/2026)
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = treatmentId };
            var trkList = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
            HIS_TRACKING tracking30 = null;
            if (trkList != null)
            {
                tracking30 = trkList.FirstOrDefault(x => x.TRACKING_TIME == targetDate || x.TRACKING_TIME.ToString().StartsWith("2026093008"));
            }

            long trackingId = 0;
            if (tracking30 != null)
            {
                trackingId = tracking30.ID;
                Console.WriteLine(string.Format("   ℹ️ Đã có Tờ điều trị ngày 30/09 (ID: {0} lúc {1})", trackingId, tracking30.TRACKING_TIME));
            }
            else
            {
                // Create Tracking
                HIS_TRACKING newTrk = new HIS_TRACKING
                {
                    TREATMENT_ID = treatmentId,
                    DEPARTMENT_ID = tr.LAST_DEPARTMENT_ID > 0 ? tr.LAST_DEPARTMENT_ID : 57,
                    ROOM_ID = reqRoomId,
                    TRACKING_TIME = targetDate,
                    CONTENT = p.TrackingContent,
                    ICD_CODE = tr.ICD_CODE,
                    ICD_NAME = tr.ICD_NAME,
                    ICD_SUB_CODE = tr.ICD_SUB_CODE,
                    ICD_TEXT = tr.ICD_TEXT
                };

                HisTrackingSDO trkSdo = new HisTrackingSDO
                {
                    Tracking = newTrk,
                    WorkingRoomId = reqRoomId,
                    Dhst = new HIS_DHST
                    {
                        TREATMENT_ID = treatmentId,
                        EXECUTE_TIME = targetDate,
                        EXECUTE_LOGINNAME = currentDoctorLogin,
                        EXECUTE_USERNAME = currentDoctorName,
                        PULSE = 78,
                        TEMPERATURE = 36.6m,
                        BLOOD_PRESSURE_MAX = 120,
                        BLOOD_PRESSURE_MIN = 80
                    }
                };

                var createdTrk = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, trkSdo, param);
                if (createdTrk != null && createdTrk.ID > 0)
                {
                    trackingId = createdTrk.ID;
                    Console.WriteLine(string.Format("   ✔ [TỜ ĐIỀU TRỊ] Tạo THÀNH CÔNG ID: {0} lúc {1}", createdTrk.ID, createdTrk.TRACKING_TIME));
                }
                else
                {
                    trkSdo.WorkingRoomId = 5248;
                    createdTrk = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, trkSdo, param);
                    if (createdTrk != null && createdTrk.ID > 0)
                    {
                        trackingId = createdTrk.ID;
                        Console.WriteLine(string.Format("   ✔ [TỜ ĐIỀU TRỊ] Tạo THÀNH CÔNG ID: {0} lúc {1}", createdTrk.ID, createdTrk.TRACKING_TIME));
                    }
                    else
                    {
                        string err = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "Lỗi không xác định";
                        Console.WriteLine("   ❌ [TỜ ĐIỀU TRỊ] Tạo thất bại: " + err);
                    }
                }
            }

            // Step 2: Prescriptions (Copy thuốc 08h05 ngày 30/09 nếu không mổ)
            if (p.IsSurgeryTomorrow)
            {
                Console.WriteLine("   ⚠️ BN MỔ PHIÊN HÔM NAY -> BỎ QUA copy thuốc & suất ăn sáng (Nhịn ăn trước mổ)");
            }
            else
            {
                var srf = new HisServiceReqViewFilter { TREATMENT_ID = treatmentId };
                var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                bool hasMeds30 = reqs != null && reqs.Any(r => r.SERVICE_REQ_TYPE_ID == 6 && r.INTRUCTION_TIME >= targetDayStart && r.INTRUCTION_TIME <= targetDayEnd && r.IS_DELETE != 1);

                if (hasMeds30)
                {
                    Console.WriteLine("   ℹ️ Đã có đơn thuốc ngày 30/09, không copy đè.");
                }
                else
                {
                    CopyMedsForPatient(treatmentId, trackingId, reqRoomId, tr);
                }

                // Step 3: Rations (Suất ăn 30/09)
                if (!string.IsNullOrEmpty(p.RationType))
                {
                    bool hasRation30 = reqs != null && reqs.Any(r => r.SERVICE_REQ_TYPE_ID == 14 && r.INTRUCTION_TIME >= targetDayStart && r.INTRUCTION_TIME <= targetDayEnd && r.IS_DELETE != 1);
                    if (hasRation30)
                    {
                        Console.WriteLine("   ℹ️ Đã có suất ăn ngày 30/09.");
                    }
                    else
                    {
                        AssignRationForPatient(treatmentId, trackingId, reqRoomId, tr, p.RationType);
                    }
                }
                else
                {
                    Console.WriteLine("   ℹ️ Bệnh nhân không có chỉ định suất ăn bệnh viện (Dinh dưỡng tự túc / Từ chối).");
                }
            }
        }
    }

    static void CopyMedsForPatient(long treatmentId, long trackingId, long reqRoomId, V_HIS_TREATMENT tr)
    {
        long targetPresTime = 20260930080500;
        long prevDayStart = 20260929000000;
        long prevDayEnd = 20260929235959;
        long friDayStart = 20260928000000;

        var emFilter = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = treatmentId };
        var ems = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emFilter, param);
        if (ems == null || ems.Count == 0)
        {
            Console.WriteLine("   ℹ️ BN không có lịch sử thuốc cũ để copy.");
            return;
        }

        var prevMeds = ems.Where(m => {
            long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
            return t >= prevDayStart && t <= prevDayEnd && m.IS_DELETE != 1;
        }).ToList();

        if (prevMeds == null || prevMeds.Count == 0)
        {
            prevMeds = ems.Where(m => {
                long t = m.TDL_INTRUCTION_TIME ?? m.EXP_TIME ?? m.CREATE_TIME ?? 0;
                return t >= friDayStart && t <= prevDayEnd && m.IS_DELETE != 1;
            }).ToList();
        }

        if (prevMeds == null || prevMeds.Count == 0)
        {
            Console.WriteLine("   ℹ️ Không tìm thấy đơn thuốc 28/09 hay 29/09 để copy.");
            return;
        }

        string[] diabetesKeywords = new string[] {
            "actrapid", "lantus", "mixtard", "novorapid", "insulin", "humalog", "apidra", "toujeo", "tresiba", "insulatard", "scilin",
            "metformin", "glucophage", "diamicron", "gliclazide", "amaryl", "glimepiride", "januvia", "sitagliptin", "galvus", "vildagliptin",
            "trajenta", "linagliptin", "forxiga", "dapagliflozin", "jardiance", "empagliflozin", "glucovance"
        };

        var distinctMeds = prevMeds.GroupBy(m => m.MEDICINE_TYPE_ID).Select(g => g.First()).ToList();
        var pharmacyMeds = new List<PresMedicineSDO>();
        var cabinetMeds = new List<PresMedicineSDO>();

        foreach (var m in distinctMeds)
        {
            string nameNorm = (m.MEDICINE_TYPE_NAME ?? "").ToLower();
            if (diabetesKeywords.Any(k => nameNorm.Contains(k))) continue;

            var sdoMed = new PresMedicineSDO
            {
                MedicineTypeId = m.MEDICINE_TYPE_ID,
                Amount = m.AMOUNT,
                Tutorial = m.TUTORIAL,
                Morning = m.MORNING,
                Noon = m.NOON,
                Afternoon = m.AFTERNOON,
                Evening = m.EVENING,
                MedicineUseFormId = m.MEDICINE_USE_FORM_ID,
                IsExpend = m.IS_EXPEND == 1,
                PatientTypeId = m.PATIENT_TYPE_ID ?? 1
            };

            if (m.MEDI_STOCK_ID == 810)
            {
                cabinetMeds.Add(sdoMed);
            }
            else
            {
                pharmacyMeds.Add(sdoMed);
            }
        }

        string cleanIcd = tr != null ? tr.ICD_CODE : "M54.5";

        // 1. Prescribe Cabinet Meds
        if (cabinetMeds.Count > 0)
        {
            try
            {
                var outPresSDO = new OutPatientPresSDO
                {
                    TreatmentId = treatmentId,
                    InstructionTime = targetPresTime,
                    UseTimes = new List<long> { targetPresTime },
                    TrackingId = trackingId > 0 ? (long?)trackingId : null,
                    RequestRoomId = reqRoomId > 0 ? reqRoomId : 5248,
                    RequestLoginName = currentDoctorLogin,
                    RequestUserName = currentDoctorName,
                    IcdCode = cleanIcd,
                    IcdName = tr != null ? tr.ICD_NAME : "Bệnh lý cơ xương khớp",
                    IcdSubCode = tr != null ? tr.ICD_SUB_CODE : null,
                    IcdText = tr != null ? tr.ICD_TEXT : null,
                    IsCabinet = true,
                    ClientSessionKey = Guid.NewGuid().ToString(),
                    Medicines = cabinetMeds
                };

                CommonParam pOut = new CommonParam();
                var outRes = myAdapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, pOut);
                if ((outRes == null || outRes.ServiceReqs == null || outRes.ServiceReqs.Count == 0) && reqRoomId != 5248)
                {
                    outPresSDO.RequestRoomId = 5248;
                    pOut = new CommonParam();
                    outRes = myAdapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, pOut);
                }

                if (outRes != null && outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0)
                {
                    string sReqCode = outRes.ServiceReqs[0].SERVICE_REQ_CODE;
                    Console.WriteLine(string.Format("   ✔ [TỦ TRỰC 810] Kê THÀNH CÔNG {0} loại thuốc (Mã y lệnh: {1})", cabinetMeds.Count, sReqCode));
                }
                else
                {
                    string err = (pOut.Messages != null && pOut.Messages.Count > 0) ? string.Join("; ", pOut.Messages) : "Lỗi tạo đơn tủ trực";
                    Console.WriteLine("   ❌ [TỦ TRỰC 810] Thất bại: " + err);
                }
            }
            catch (Exception exCab)
            {
                Console.WriteLine("   ❌ [TỦ TRỰC 810] Lỗi: " + exCab.Message);
            }
        }

        // 2. Prescribe Pharmacy Meds
        if (pharmacyMeds.Count > 0)
        {
            try
            {
                var presSDO = new InPatientPresSDO
                {
                    TreatmentId = treatmentId,
                    InstructionTimes = new List<long> { targetPresTime },
                    UseTimes = new List<long> { targetPresTime },
                    TrackingId = trackingId > 0 ? (long?)trackingId : null,
                    TrackingInfos = trackingId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = targetPresTime } } : null,
                    RequestRoomId = reqRoomId > 0 ? reqRoomId : 5248,
                    RequestLoginName = currentDoctorLogin,
                    RequestUserName = currentDoctorName,
                    IcdCode = cleanIcd,
                    IcdName = tr != null ? tr.ICD_NAME : "Bệnh lý cơ xương khớp",
                    IcdSubCode = tr != null ? tr.ICD_SUB_CODE : null,
                    IcdText = tr != null ? tr.ICD_TEXT : null,
                    Medicines = pharmacyMeds
                };

                CommonParam pPres = new CommonParam();
                var presRes = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, presSDO, pPres);
                if ((presRes == null || ((presRes.ServiceReqs == null || presRes.ServiceReqs.Count == 0) && (presRes.ExpMests == null || presRes.ExpMests.Count == 0))) && reqRoomId != 5248)
                {
                    presSDO.RequestRoomId = 5248;
                    pPres = new CommonParam();
                    presRes = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, presSDO, pPres);
                }

                if (presRes != null && ((presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0) || (presRes.ExpMests != null && presRes.ExpMests.Count > 0)))
                {
                    string reqCode = presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0 ? presRes.ServiceReqs[0].SERVICE_REQ_CODE : presRes.ExpMests[0].EXP_MEST_CODE;
                    Console.WriteLine(string.Format("   ✔ [KHO DƯỢC] Kê THÀNH CÔNG {0} loại thuốc (Mã y lệnh: {1})", pharmacyMeds.Count, reqCode));
                }
                else
                {
                    string err = (pPres.Messages != null && pPres.Messages.Count > 0) ? string.Join("; ", pPres.Messages) : "MOS từ chối tạo đơn Kho Dược";
                    if (pPres.BugCodes != null && pPres.BugCodes.Count > 0) err += " [" + string.Join(";", pPres.BugCodes) + "]";
                    Console.WriteLine("   ❌ [KHO DƯỢC] Thất bại: " + err);
                }
            }
            catch (Exception exP)
            {
                Console.WriteLine("   ❌ [KHO DƯỢC] Lỗi: " + exP.Message);
            }
        }
    }

    static void AssignRationForPatient(long treatmentId, long trackingId, long reqRoomId, V_HIS_TREATMENT tr, string rationType)
    {
        var rationList = new List<RationServiceSDO>();
        long ptId = 42;

        string combo = (rationType ?? "BT01").ToUpper();
        if (combo.Contains("DD01"))
        {
            rationList.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else
        {
            rationList.Add(new RationServiceSDO { ServiceId = 30073, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30153, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            rationList.Add(new RationServiceSDO { ServiceId = 30154, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }

        long instructionTime = 20260930060000;

        var sdo = new HisRationServiceReqSDO
        {
            TreatmentIds = new List<long> { treatmentId },
            InstructionTimes = new List<long> { instructionTime },
            RequestRoomId = reqRoomId > 0 ? reqRoomId : 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            HalfInFirstDay = false,
            IsForAutoCreateRation = false,
            IsForHomie = false,
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RationServices = rationList
        };

        var result = myAdapter.PostData<object>("api/HisServiceReq/RationCreate", mosConsumer, sdo, param);
        if (param.HasException)
        {
            string err = (param.Messages != null && param.Messages.Count > 0) ? string.Join("; ", param.Messages) : "Lỗi chỉ định suất ăn";
            Console.WriteLine("   ❌ [SUẤT ĂN] Thất bại: " + err);
        }
        else
        {
            Console.WriteLine(string.Format("   ✔ [SUẤT ĂN] Chỉ định THÀNH CÔNG combo {0} ({1} bữa)", combo, rationList.Count));
        }
    }

    static void InitSession()
    {
        try { Load.Init(); } catch { }
        string tokenCode = null;

        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        if (!File.Exists(cacheFile))
        {
            string alt = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\doctor_standalone.token";
            if (File.Exists(alt)) cacheFile = alt;
        }
        if (File.Exists(cacheFile))
        {
            try
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2 && parts[0].Length == 64)
                {
                    tokenCode = parts[0];
                    currentDoctorLogin = parts.Length >= 3 ? parts[2] : "034727";
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            var tokenManager = new Inventec.Token.ClientSystem.ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
            if (token == null || string.IsNullOrEmpty(token.TokenCode))
            {
                token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    currentDoctorLogin = "vmc";
                    currentDoctorName = "BS Vũ Minh Cường";
                }
            }
            else
            {
                tokenCode = token.TokenCode;
            }
        }

        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");

        long[] rooms = new long[] { 931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267 };
        var workInfo = new WorkInfoSDO { Rooms = rooms.Select(r => new RoomSDO { RoomId = r }).ToList() };
        myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        Console.WriteLine(string.Format("Đăng nhập BS: {0} ({1})", currentDoctorName, currentDoctorLogin));
    }
}
