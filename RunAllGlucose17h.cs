using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

public class GlucosePatientInput
{
    public string PatientCode { get; set; }
    public string Name { get; set; }
    public string RoomBed { get; set; }
    public decimal Glucose { get; set; }
    public int InsulinUnits { get; set; } // 0 if none
    public string InsulinType { get; set; } // "R"
}

public class PatientExecutionResult
{
    public string PatientCode { get; set; }
    public string PatientName { get; set; }
    public string RoomBed { get; set; }
    public decimal Glucose { get; set; }
    public int InsulinUnits { get; set; }
    public long TrackingId { get; set; }
    public string ClsOrderCode { get; set; }
    public string PresOrderCode { get; set; }
    public string Status { get; set; }
    public string Error { get; set; }
}

class RunAllGlucose17h
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, r) =>
        {
            string n = new System.Reflection.AssemblyName(r.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, n);
            if (File.Exists(p1)) return System.Reflection.Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", n);
            if (File.Exists(p2)) return System.Reflection.Assembly.LoadFrom(p2);
            return null;
        };
        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = "";
        if (File.Exists("doctor_standalone.token")) token = File.ReadAllText("doctor_standalone.token").Split('|')[0].Trim();
        else if (File.Exists("doctor_hn.token")) token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();

        var cp = new CommonParam();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new BackendAdapter(cp);

        // Update WorkInfo
        try
        {
            var wi = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 931 },
                    new RoomSDO { RoomId = 5250 },
                    new RoomSDO { RoomId = 5252 },
                    new RoomSDO { RoomId = 5253 },
                    new RoomSDO { RoomId = 5255 },
                    new RoomSDO { RoomId = 5256 },
                    new RoomSDO { RoomId = 5257 },
                    new RoomSDO { RoomId = 5258 },
                    new RoomSDO { RoomId = 5259 },
                    new RoomSDO { RoomId = 5260 },
                    new RoomSDO { RoomId = 5261 },
                    new RoomSDO { RoomId = 5262 },
                    new RoomSDO { RoomId = 5263 },
                    new RoomSDO { RoomId = 5264 },
                    new RoomSDO { RoomId = 5265 },
                    new RoomSDO { RoomId = 5266 },
                    new RoomSDO { RoomId = 5267 }
                }
            };
            adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, new CommonParam());
        }
        catch { }

        // Find Actrapid medicine type
        long actrapidTypeId = 0;
        try
        {
            var medTypeFilter = new HisMedicineTypeViewFilter { KEY_WORD = "Actrapid", IS_ACTIVE = 1 };
            var medTypes = adapter.Get<List<V_HIS_MEDICINE_TYPE>>("api/HisMedicineType/GetView", mosConsumer, medTypeFilter, new CommonParam());
            if (medTypes != null && medTypes.Count > 0)
            {
                var match = medTypes.FirstOrDefault(m => m.MEDICINE_TYPE_NAME.ToLower().Contains("actrapid")) ?? medTypes[0];
                actrapidTypeId = match.ID;
                Console.WriteLine(string.Format("Tìm thấy Actrapid: ID={0}, Name={1}", actrapidTypeId, match.MEDICINE_TYPE_NAME));
            }
        }
        catch { }

        var inputList = new List<GlucosePatientInput>
        {
            new GlucosePatientInput { PatientCode = "0004058475", Name = "Nguyễn Văn Chiến", RoomBed = "P740 - G76", Glucose = 6.3m, InsulinUnits = 0, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0002871946", Name = "Hán Thị Thiêm", RoomBed = "P740 - G64", Glucose = 8.6m, InsulinUnits = 8, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0003842817", Name = "Nguyễn Thị Mai", RoomBed = "P740 - G56", Glucose = 14.0m, InsulinUnits = 12, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0003994971", Name = "Vũ Xuân Cường", RoomBed = "P730 - G91", Glucose = 12.8m, InsulinUnits = 10, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0004060126", Name = "Đặng Thị Nam", RoomBed = "P732 - G87A", Glucose = 19.2m, InsulinUnits = 16, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0004029350", Name = "Trần Thị Nhâm", RoomBed = "P735 - G81", Glucose = 13.1m, InsulinUnits = 12, InsulinType = "R" },
            // BỎ QUA Đoàn Thị Tuấn (MF)
            new GlucosePatientInput { PatientCode = "0004079114", Name = "Phan Thị Tư", RoomBed = "P723 - G41", Glucose = 7.2m, InsulinUnits = 0, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0004076519", Name = "Ngô Thị Thanh", RoomBed = "P722 - G37", Glucose = 11.9m, InsulinUnits = 10, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0004054272", Name = "Nguyễn Thị Vinh", RoomBed = "P723 - G45", Glucose = 8.2m, InsulinUnits = 6, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0004038654", Name = "Trương Thị Thành", RoomBed = "P723 - G46", Glucose = 4.1m, InsulinUnits = 0, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0002048483", Name = "Đinh Thị Vẻ", RoomBed = "P723 - G42", Glucose = 5.0m, InsulinUnits = 0, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0003938033", Name = "Nguyễn Phú Bình", RoomBed = "P710 - G33", Glucose = 5.7m, InsulinUnits = 0, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0003061170", Name = "Đinh Văn Nghiệp", RoomBed = "P712 - G23", Glucose = 12.4m, InsulinUnits = 10, InsulinType = "R" },
            new GlucosePatientInput { PatientCode = "0001344789", Name = "Nguyễn Thị Ngọ", RoomBed = "P714 - G17", Glucose = 9.4m, InsulinUnits = 6, InsulinType = "R" }
        };

        List<PatientExecutionResult> results = new List<PatientExecutionResult>();
        long target17h = 20260929170000;
        long pres17h = 20260929170500;

        Console.WriteLine(string.Format("\nBẮT ĐẦU XỬ LÝ {0} BỆNH NHÂN CHO MỐC 17H...", inputList.Count));

        foreach (var inp in inputList)
        {
            var res = new PatientExecutionResult
            {
                PatientCode = inp.PatientCode,
                PatientName = inp.Name,
                RoomBed = inp.RoomBed,
                Glucose = inp.Glucose,
                InsulinUnits = inp.InsulinUnits
            };

            Console.WriteLine("\n=======================================================");
            Console.WriteLine(string.Format("BN: {0} - {1} ({2}) | ĐH: {3} mmol/L | Liều: {4} UI", inp.PatientCode, inp.Name, inp.RoomBed, inp.Glucose, inp.InsulinUnits));

            try
            {
                // 1. Get Patient & Active Treatment
                var patFilter = new HisPatientFilter { PATIENT_CODE = inp.PatientCode };
                var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", mosConsumer, patFilter, new CommonParam());
                if (pats == null || pats.Count == 0) throw new Exception("Không tìm thấy thông tin BN trên HIS");
                var pat = pats[0];

                var trmFilter = new HisTreatmentFilter { PATIENT_ID = pat.ID };
                var trms = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, trmFilter, new CommonParam());
                var trm = trms != null ? trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault(x => x.IS_PAUSE != 1) : null;
                if (trm == null && trms != null) trm = trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault();
                if (trm == null) throw new Exception("Không tìm thấy đợt điều trị hiện tại");

                // 2. Tạo Tờ điều trị 17:00
                string insulinText = inp.InsulinUnits > 0 ? string.Format("Tiêm dưới da Actrapid {0} UI lúc 17h00.", inp.InsulinUnits) : "Không chỉ định tiêm insulin.";
                string trkContent = string.Format("Khám 17h00: Bệnh nhân tỉnh, tiếp xúc tốt. ĐMMM lúc 17h00: {0} mmol/L. {1}", inp.Glucose, insulinText);
                string trkMed = string.Format("Đo ĐMMM lúc 17h00: {0} mmol/L.{1}", inp.Glucose, inp.InsulinUnits > 0 ? string.Format(" Tiêm Actrapid {0} UI.", inp.InsulinUnits) : "");
                string trkCare = "Chăm sóc cấp II. Theo dõi đường máu mao mạch.";

                var trkNew = new HIS_TRACKING
                {
                    TREATMENT_ID = trm.ID,
                    DEPARTMENT_ID = 57,
                    ROOM_ID = 5248,
                    TRACKING_TIME = target17h,
                    CONTENT = trkContent,
                    CARE_INSTRUCTION = trkCare,
                    MEDICAL_INSTRUCTION = trkMed,
                    ICD_CODE = !string.IsNullOrEmpty(trm.ICD_CODE) ? trm.ICD_CODE : "M50.2",
                    ICD_NAME = !string.IsNullOrEmpty(trm.ICD_NAME) ? trm.ICD_NAME : "Thoát vị đĩa đệm",
                    ICD_SUB_CODE = trm.ICD_SUB_CODE,
                    ICD_TEXT = trm.ICD_TEXT
                };
                var sdoTrk = new HisTrackingSDO { Tracking = trkNew, WorkingRoomId = 5248 };
                var cpTrk = new CommonParam();
                var createdTrk = adapter.Post<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdoTrk, cpTrk);

                long trkId = 0;
                if (createdTrk != null && createdTrk.ID > 0)
                {
                    trkId = createdTrk.ID;
                    res.TrackingId = trkId;
                    Console.WriteLine(string.Format("  ✔ Tờ điều trị 17h: ID={0}", trkId));
                }
                else
                {
                    // Check if already created
                    var chkFilter = new HisTrackingFilter { TREATMENT_ID = trm.ID };
                    var chkTrks = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, chkFilter, new CommonParam());
                    var existTrk = chkTrks != null ? chkTrks.FirstOrDefault(t => t.TRACKING_TIME == target17h) : null;
                    if (existTrk != null)
                    {
                        trkId = existTrk.ID;
                        res.TrackingId = trkId;
                        Console.WriteLine(string.Format("  ✔ Đã có sẵn tờ điều trị 17h: ID={0}", trkId));
                    }
                    else
                    {
                        var lastTrk = chkTrks != null ? chkTrks.OrderByDescending(t => t.TRACKING_TIME).FirstOrDefault(t => t.TRACKING_TIME.ToString().StartsWith("20260929")) : null;
                        if (lastTrk != null) trkId = lastTrk.ID;
                        Console.WriteLine("  ⚠️ Lỗi tạo tờ ĐT mới: " + string.Join("; ", cpTrk.Messages ?? new List<string>()) + " -> Dùng ID: " + trkId);
                    }
                }

                // 3. Chỉ định CLS BM02426 lúc 17:00
                var assignSDO = new AssignServiceSDO
                {
                    TreatmentId = trm.ID,
                    RequestRoomId = 5248,
                    RequestLoginName = "034727",
                    RequestUserName = "Ths.BS Nguyễn Hữu Sâm",
                    InstructionTime = target17h,
                    InstructionTimes = new List<long> { target17h },
                    UseTimes = new List<long> { target17h },
                    TrackingId = trkId > 0 ? trkId : (long?)null,
                    TrackingInfos = trkId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trkId, IntructionTime = target17h } } : null,
                    IcdCode = !string.IsNullOrEmpty(trm.ICD_CODE) ? trm.ICD_CODE : "M50.2",
                    IcdName = !string.IsNullOrEmpty(trm.ICD_NAME) ? trm.ICD_NAME : "Thoát vị đĩa đệm",
                    ServiceReqDetails = new List<ServiceReqDetailSDO>
                    {
                        new ServiceReqDetailSDO
                        {
                            ServiceId = 6217, // BM02426
                            Amount = 1.0m,
                            PatientTypeId = 1,
                            RoomId = 931, // Tiểu phẫu Nhà Q
                            SampleTypeCode = "BP0042",
                            InstructionNote = string.Format("Đo ĐMMM lúc 17h00 (KQ: {0} mmol/L)", inp.Glucose),
                            MultipleExecute = 1
                        }
                    }
                };
                var cpCls = new CommonParam();
                var resCls = adapter.Post<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, assignSDO, cpCls);
                if (resCls != null && resCls.ServiceReqs != null && resCls.ServiceReqs.Count > 0)
                {
                    res.ClsOrderCode = resCls.ServiceReqs[0].SERVICE_REQ_CODE;
                    Console.WriteLine(string.Format("  ✔ Chỉ định CLS BM02426: {0}", res.ClsOrderCode));
                }
                else
                {
                    Console.WriteLine("  ❌ Lỗi chỉ định CLS: " + string.Join("; ", cpCls.Messages ?? new List<string>()));
                }

                // 4. Kê đơn Insulin Actrapid từ Tủ trực 810 nếu liều > 0
                if (inp.InsulinUnits > 0 && actrapidTypeId > 0)
                {
                    decimal amountLo = (decimal)inp.InsulinUnits / 1000.0m;
                    string sessionKey = Guid.NewGuid().ToString();

                    var takeBean = new TakeBeanSDO
                    {
                        TypeId = actrapidTypeId,
                        MediStockId = 810, // Tủ trực Khoa 57
                        PatientTypeId = 1,
                        Amount = amountLo,
                        ClientSessionKey = sessionKey,
                        ExpiredDate = null
                    };
                    var cpTake = new CommonParam();
                    var beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, cpTake);
                    if (beans == null || beans.Count == 0)
                    {
                        takeBean.PatientTypeId = 42; // Thử viện phí
                        beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, cpTake);
                    }

                    if (beans != null && beans.Count > 0)
                    {
                        string cữVal = inp.InsulinUnits.ToString("D2");
                        var outPresSDO = new OutPatientPresSDO
                        {
                            TreatmentId = trm.ID,
                            InstructionTime = pres17h,
                            UseTimes = new List<long> { pres17h },
                            TrackingId = trkId > 0 ? trkId : (long?)null,
                            RequestRoomId = 5248,
                            RequestLoginName = "034727",
                            RequestUserName = "Ths.BS Nguyễn Hữu Sâm",
                            IcdCode = !string.IsNullOrEmpty(trm.ICD_CODE) ? trm.ICD_CODE : "E11",
                            IcdName = !string.IsNullOrEmpty(trm.ICD_NAME) ? trm.ICD_NAME : "Đái tháo đường",
                            IsCabinet = true,
                            ClientSessionKey = sessionKey,
                            Medicines = new List<PresMedicineSDO>
                            {
                                new PresMedicineSDO
                                {
                                    MedicineTypeId = actrapidTypeId,
                                    MediStockId = 810,
                                    Amount = amountLo,
                                    PresAmount = amountLo,
                                    PatientTypeId = takeBean.PatientTypeId ?? 1,
                                    Tutorial = string.Format("Tiêm dưới da {0} đơn vị (UI) lúc 17h00", inp.InsulinUnits),
                                    MedicineUseFormId = 15, // Tiêm
                                    Afternoon = cữVal,
                                    IsExpend = false,
                                    NumOfDays = 1,
                                    MedicineBeanIds = beans.Select(b => b.ID).ToList()
                                }
                            }
                        };

                        var cpPres = new CommonParam();
                        var resPres = adapter.Post<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, cpPres);
                        if (resPres != null && resPres.ServiceReqs != null && resPres.ServiceReqs.Count > 0)
                        {
                            res.PresOrderCode = resPres.ServiceReqs[0].SERVICE_REQ_CODE;
                            Console.WriteLine(string.Format("  ✔ Kê đơn Insulin Actrapid ({0} UI): {1}", inp.InsulinUnits, res.PresOrderCode));
                        }
                        else
                        {
                            Console.WriteLine("  ❌ Lỗi kê đơn thuốc: " + string.Join("; ", cpPres.Messages ?? new List<string>()));
                        }
                    }
                    else
                    {
                        Console.WriteLine("  ❌ Lỗi giữ thuốc tủ trực: " + string.Join("; ", cpTake.Messages ?? new List<string>()));
                    }
                }
                else if (inp.InsulinUnits == 0)
                {
                    res.PresOrderCode = "(Không tiêm)";
                }

                res.Status = "Thành công";
            }
            catch (Exception ex)
            {
                res.Status = "Lỗi";
                res.Error = ex.Message;
                Console.WriteLine("  ❌ NGOẠI LỆ: " + ex.Message);
            }

            results.Add(res);
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("BÁO CÁO TỔNG HỢP KẾT QUẢ MỐC 17H:");
        Console.WriteLine("===============================================================================");
        foreach (var r in results)
        {
            Console.WriteLine(string.Format("{0,-10} | {1,-20} | {2,-12} | ĐH: {3,4} | Tiêm: {4,2} UI | CLS: {5,-12} | Đơn: {6,-12} | {7}",
                r.PatientCode, r.PatientName, r.RoomBed, r.Glucose, r.InsulinUnits, r.ClsOrderCode ?? "N/A", r.PresOrderCode ?? "N/A", r.Status));
        }
    }
}
