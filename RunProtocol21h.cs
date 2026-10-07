using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;
using Inventec.Common.WebApiClient;
using Inventec.Common.Adapter;
using Inventec.Core;

class Program {
    public class PatientItem {
        public string RoomHint;
        public string Name;
        public string PatientCode;
        public decimal Glucose;
        public int Dose; // UI Lantus
        public string TrackingIdStr = "-";
        public string ClsCode = "-";
        public string PresCode = "-";
        public string Status = "PENDING";
    }

    static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();
        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new BackendAdapter(new CommonParam());

        // 1. Update WorkInfo with all Hanoi rooms
        var wInfo = new WorkInfoSDO {
            Rooms = new List<long> { 5248, 931, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267, 6622, 6623 }
                .Select(r => new RoomSDO { RoomId = r }).ToList()
        };
        adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, wInfo, new CommonParam());

        // Pre-fetch Lantus medicine type (ID 14956)
        var medFilter = new HisMedicineTypeViewFilter { ID = 14956 };
        var meds = adapter.Get<List<V_HIS_MEDICINE_TYPE>>("api/HisMedicineType/GetView", consumer, medFilter, new CommonParam());
        var lantusMed = meds != null && meds.Count > 0 ? meds[0] : null;
        if (lantusMed == null) {
            Console.WriteLine("❌ Không tìm thấy thuốc Lantus 14956!");
            return;
        }

        var patients = new List<PatientItem> {
            new PatientItem { RoomHint = "735",  Name = "Trương Thị Thi",          PatientCode = "0003362121", Glucose = 4.3m,  Dose = 0 },
            new PatientItem { RoomHint = "734",  Name = "Đoàn Thị Tuấn",          PatientCode = "0004083465", Glucose = 15.6m, Dose = 20 },
            new PatientItem { RoomHint = "732",  Name = "Đặng Thị Nam",           PatientCode = "0004060126", Glucose = 11.9m, Dose = 12 },
            new PatientItem { RoomHint = "731",  Name = "Đào Thị Lộc",            PatientCode = "0003550080", Glucose = 13.6m, Dose = 14 },
            new PatientItem { RoomHint = "740",  Name = "Đỗ Thị Tăng",            PatientCode = "0003863232", Glucose = 6.8m,  Dose = 10 },
            new PatientItem { RoomHint = "740",  Name = "Phan Thị Tư",            PatientCode = "0004079114", Glucose = 7.3m,  Dose = 10 },
            new PatientItem { RoomHint = "740",  Name = "Đinh Thị Vẻ",            PatientCode = "0002048483", Glucose = 5.0m,  Dose = 10 },
            new PatientItem { RoomHint = "740",  Name = "Trần Thị Xuân",          PatientCode = "0004099361", Glucose = 8.6m,  Dose = 10 },
            new PatientItem { RoomHint = "712A", Name = "Trần Thị Tho",           PatientCode = "0004071508", Glucose = 4.8m,  Dose = 10 },
            new PatientItem { RoomHint = "740",  Name = "Nguyễn Thị Vinh",        PatientCode = "0004054272", Glucose = 11.5m, Dose = 10 },
            new PatientItem { RoomHint = "724",  Name = "Trần Văn Thán",          PatientCode = "0004100248", Glucose = 10.3m, Dose = 10 },
            new PatientItem { RoomHint = "724",  Name = "Nguyễn Thị Biển",        PatientCode = "0003394090", Glucose = 6.3m,  Dose = 14 },
            new PatientItem { RoomHint = "740",  Name = "Phí Văn Thuỳ",           PatientCode = "0004064007", Glucose = 12.3m, Dose = 6 },
            new PatientItem { RoomHint = "722",  Name = "Vũ Thị Nghĩa",           PatientCode = "0001474608", Glucose = 5.7m,  Dose = 10 },
            new PatientItem { RoomHint = "722",  Name = "Nguyễn Thị Lý",          PatientCode = "0000046297", Glucose = 8.5m,  Dose = 10 },
            new PatientItem { RoomHint = "740",  Name = "Trần Thị Mai",           PatientCode = "0004107167", Glucose = 12.2m, Dose = 16 },
            new PatientItem { RoomHint = "715",  Name = "Nguyễn Thị Thanh Tuyết", PatientCode = "0002127550", Glucose = 10.9m, Dose = 8 },
            new PatientItem { RoomHint = "724",  Name = "Phạm Văn Dũng",          PatientCode = "0004114703", Glucose = 11.5m, Dose = 10 },
            new PatientItem { RoomHint = "716",  Name = "Trịnh Thanh Hoan",       PatientCode = "0001572762", Glucose = 9.6m,  Dose = 12 },
            new PatientItem { RoomHint = "736",  Name = "Đàm Thị Nê",             PatientCode = "0003878176", Glucose = 18.7m, Dose = 18 }
        };

        long insTime21h = 20261007210000;
        long presTime21h = 20261007210500;

        Console.WriteLine("===============================================================================");
        Console.WriteLine("BẮT ĐẦU XỬ LÝ ĐỒNG BỘ 20 BỆNH NHÂN CA 21:00 (LANTUS TỦ TRỰC 810)");
        Console.WriteLine("===============================================================================");

        foreach (var p in patients) {
            Console.WriteLine(string.Format("\n▶ Đang xử lý: [{0}] {1} ({2}) | ĐH: {3} | Liều: {4}L...", p.RoomHint, p.Name, p.PatientCode, p.Glucose, p.Dose));

            try {
                // Find Treatment
                var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = p.PatientCode };
                var trs = adapter.Get<List<V_HIS_TREATMENT>>("api/HisTreatment/GetView", consumer, tf, new CommonParam());
                if (trs == null || trs.Count == 0) {
                    var patF = new HisPatientFilter { PATIENT_CODE = p.PatientCode };
                    var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", consumer, patF, new CommonParam());
                    if (pats != null && pats.Count > 0) {
                        tf = new HisTreatmentViewFilter { PATIENT_ID = pats[0].ID };
                        trs = adapter.Get<List<V_HIS_TREATMENT>>("api/HisTreatment/GetView", consumer, tf, new CommonParam());
                    }
                }
                if (trs == null || trs.Count == 0) {
                    Console.WriteLine("  ❌ Không tìm thấy đợt điều trị!");
                    p.Status = "ERR_NO_TRM";
                    continue;
                }
                var tr = trs.OrderByDescending(t => t.IN_TIME).First();

                // 1. TẠO / TÌM TỜ ĐIỀU TRỊ 21:00
                long trkId = 0;
                var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
                var trks = adapter.Get<List<V_HIS_TRACKING>>("api/HisTracking/GetView", consumer, trkFilter, new CommonParam());
                var existing21h = trks != null ? trks.FirstOrDefault(x => x.TRACKING_TIME == insTime21h) : null;
                if (existing21h != null) {
                    trkId = existing21h.ID;
                    p.TrackingIdStr = trkId.ToString();
                    Console.WriteLine("  ✔ Tờ điều trị 21:00 đã tồn tại ID: " + trkId);
                } else {
                    string medInst = p.Dose > 0 ? string.Format("Tiêm dưới da {0} đơn vị Lantus lúc 21:05. Theo dõi ĐMMM.", p.Dose) : "Theo dõi đường huyết mao mạch, không tiêm Insulin.";
                    var newTrk = new HIS_TRACKING {
                        TREATMENT_ID = tr.ID,
                        TRACKING_TIME = insTime21h,
                        CONTENT = string.Format("Bệnh nhân tỉnh, tiếp xúc tốt. Đường huyết mao mạch 21h: {0} mmol/L.", p.Glucose),
                        MEDICAL_INSTRUCTION = medInst,
                        DEPARTMENT_ID = 57,
                        ROOM_ID = 5248
                    };
                    var sdoTrk = new HisTrackingSDO { Tracking = newTrk, WorkingRoomId = 5248 };
                    var cpTrk = new CommonParam();
                    var createdTrk = adapter.Post<HIS_TRACKING>("api/HisTracking/Create", consumer, sdoTrk, cpTrk);
                    if (createdTrk != null && createdTrk.ID > 0) {
                        trkId = createdTrk.ID;
                        p.TrackingIdStr = trkId.ToString();
                        Console.WriteLine("  ✔ Tạo mới Tờ điều trị 21:00 thành công ID: " + trkId);
                    } else {
                        Console.WriteLine("  ❌ Lỗi tạo Tờ điều trị!");
                    }
                }

                // 2. CHỈ ĐỊNH ĐMMM BM02426
                var assignSDO = new AssignServiceSDO {
                    TreatmentId = tr.ID,
                    RequestRoomId = 5248,
                    RequestLoginName = "034727",
                    RequestUserName = "NGUYỄN HỮU SÂM",
                    InstructionTime = insTime21h,
                    InstructionTimes = new List<long> { insTime21h },
                    UseTimes = new List<long> { insTime21h },
                    TrackingId = trkId > 0 ? (long?)trkId : null,
                    TrackingInfos = trkId > 0 ? new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trkId, IntructionTime = insTime21h } } : null,
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME,
                    IcdSubCode = tr.ICD_SUB_CODE,
                    IcdText = tr.ICD_TEXT,
                    ServiceReqDetails = new List<ServiceReqDetailSDO> {
                        new ServiceReqDetailSDO {
                            ServiceId = 6217,
                            Amount = 1.0m,
                            PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                            RoomId = 931,
                            SampleTypeCode = "BP0042",
                            InstructionNote = "Đo ĐMMM lúc 21:00",
                            MultipleExecute = 1,
                            IsNotUseBhyt = false,
                            IsNoHeinDifference = false
                        }
                    }
                };
                var cpAssign = new CommonParam();
                var assignRes = adapter.Post<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", consumer, assignSDO, cpAssign);
                if (assignRes != null && assignRes.ServiceReqs != null && assignRes.ServiceReqs.Count > 0) {
                    p.ClsCode = assignRes.ServiceReqs[0].SERVICE_REQ_CODE;
                    Console.WriteLine("  ✔ Chỉ định ĐMMM BM02426 thành công: " + p.ClsCode);
                } else {
                    Console.WriteLine("  ❌ Lỗi chỉ định ĐMMM!");
                }

                // 3. KÊ ĐƠN LANTUS TỦ TRỰC 810 (NẾU DOSE > 0)
                if (p.Dose > 0) {
                    decimal amountLo = (decimal)p.Dose / 1000.0m;
                    string tutorial = string.Format("Tiêm dưới da {0} đơn vị Lantus lúc 21:05.", p.Dose);
                    string sessionKey = Guid.NewGuid().ToString();

                    var takeBean = new TakeBeanSDO {
                        TypeId = 14956,
                        MediStockId = 810,
                        PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                        Amount = amountLo,
                        ClientSessionKey = sessionKey,
                        ExpiredDate = null
                    };
                    var cpTake = new CommonParam();
                    var beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
                    if (beans == null || beans.Count == 0) {
                        takeBean.PatientTypeId = 42;
                        beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", consumer, takeBean, cpTake);
                    }

                    if (beans != null && beans.Count > 0) {
                        var presMed = new PresMedicineSDO {
                            MedicineTypeId = 14956,
                            MediStockId = 810,
                            Amount = amountLo,
                            PresAmount = amountLo,
                            PatientTypeId = takeBean.PatientTypeId ?? 1,
                            Tutorial = tutorial,
                            MedicineUseFormId = 15,
                            Evening = p.Dose.ToString("D2"),
                            IsExpend = false,
                            NumOfDays = 1,
                            MedicineBeanIds = beans.Select(b => b.ID).ToList()
                        };

                        var outPresSDO = new OutPatientPresSDO {
                            TreatmentId = tr.ID,
                            InstructionTime = presTime21h,
                            UseTimes = new List<long> { presTime21h },
                            TrackingId = trkId > 0 ? (long?)trkId : null,
                            RequestRoomId = 5248,
                            RequestLoginName = "034727",
                            RequestUserName = "NGUYỄN HỮU SÂM",
                            IcdCode = tr.ICD_CODE,
                            IcdName = tr.ICD_NAME,
                            IcdSubCode = tr.ICD_SUB_CODE,
                            IcdText = tr.ICD_TEXT,
                            IsCabinet = true,
                            ClientSessionKey = sessionKey,
                            Medicines = new List<PresMedicineSDO> { presMed }
                        };

                        var cpPres = new CommonParam();
                        var presRes = adapter.Post<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", consumer, new List<OutPatientPresSDO> { outPresSDO }, cpPres);
                        if (presRes != null && presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0) {
                            p.PresCode = presRes.ServiceReqs[0].SERVICE_REQ_CODE;
                            Console.WriteLine(string.Format("  ✔ Kê Lantus {0} UI thành công: {1}", p.Dose, p.PresCode));
                            p.Status = "OK";
                        } else {
                            Console.WriteLine("  ❌ Lỗi tạo đơn tủ trực!");
                            p.Status = "ERR_PRES";
                        }
                    } else {
                        Console.WriteLine("  ❌ Lỗi giữ bean tủ trực (có thể hết tồn)!");
                        p.Status = "ERR_TAKE_BEAN";
                    }
                } else {
                    p.PresCode = "Không tiêm (0L)";
                    p.Status = "OK";
                    Console.WriteLine("  ✔ Liều 0 UI: Không cần kê đơn tủ trực.");
                }
            } catch (Exception ex) {
                Console.WriteLine("  ❌ EXCEPTION: " + ex.Message);
                p.Status = "EX";
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("BÁO CÁO TỔNG HỢP KẾT QUẢ ĐỐI SOÁT LÂM SÀNG CA 21:00:");
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("{0,-6} | {1,-20} | {2,-11} | {3,-6} | {4,-6} | {5,-10} | {6,-13} | {7,-13} | {8}",
            "Buồng", "Họ và tên", "Mã BN", "ĐH", "Liều", "Tờ ĐT ID", "Mã ĐMMM", "Mã Đơn 810", "Trạng thái"));
        Console.WriteLine(new string('-', 105));
        foreach (var p in patients) {
            Console.WriteLine(string.Format("{0,-6} | {1,-20} | {2,-11} | {3,-6} | {4,-6} | {5,-10} | {6,-13} | {7,-13} | {8}",
                p.RoomHint, p.Name, p.PatientCode, p.Glucose, (p.Dose > 0 ? p.Dose + "L" : "0"), p.TrackingIdStr, p.ClsCode, p.PresCode, p.Status));
        }
        Console.WriteLine("===============================================================================");
    }
}
