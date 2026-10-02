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

class PrescribeRemainingInsulin17h
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

        long actrapidTypeId = 27727;
        long pres17h = 20260929170500;

        var list = new Tuple<string, string, int>[]
        {
            Tuple.Create("0003842817", "Nguyễn Thị Mai", 12),
            Tuple.Create("0003994971", "Vũ Xuân Cường", 10),
            Tuple.Create("0004060126", "Đặng Thị Nam", 16),
            Tuple.Create("0004029350", "Trần Thị Nhâm", 12),
            Tuple.Create("0004076519", "Ngô Thị Thanh", 10),
            Tuple.Create("0004054272", "Nguyễn Thị Vinh", 6),
            Tuple.Create("0003061170", "Đinh Văn Nghiệp", 10),
            Tuple.Create("0001344789", "Nguyễn Thị Ngọ", 6)
        };

        foreach (var item in list)
        {
            string pCode = item.Item1;
            string name = item.Item2;
            int units = item.Item3;
            decimal amountLo = (decimal)units / 1000.0m;

            Console.WriteLine(string.Format("\nKê Insulin: {0} - {1} | Liều: {2} UI...", pCode, name, units));

            var patFilter = new HisPatientFilter { PATIENT_CODE = pCode };
            var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", mosConsumer, patFilter, new CommonParam());
            if (pats == null || pats.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy BN");
                continue;
            }
            var pat = pats[0];

            var trmFilter = new HisTreatmentFilter { PATIENT_ID = pat.ID };
            var trms = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, trmFilter, new CommonParam());
            var trm = trms != null ? trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault(x => x.IS_PAUSE != 1) : null;
            if (trm == null && trms != null) trm = trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault();
            if (trm == null)
            {
                Console.WriteLine("❌ Không tìm thấy đợt ĐT");
                continue;
            }

            // Get tracking today
            var trkFilter = new HisTrackingFilter { TREATMENT_ID = trm.ID };
            var trks = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, trkFilter, new CommonParam());
            var trk17h = trks != null ? trks.OrderByDescending(t => t.TRACKING_TIME).FirstOrDefault(t => t.TRACKING_TIME.ToString().StartsWith("2026092917") || t.TRACKING_TIME.ToString().StartsWith("20260929")) : null;
            long trkId = trk17h != null ? trk17h.ID : 0;

            string sessionKey = Guid.NewGuid().ToString();
            var takeBean = new TakeBeanSDO
            {
                TypeId = actrapidTypeId,
                MediStockId = 810,
                PatientTypeId = 1,
                Amount = amountLo,
                ClientSessionKey = sessionKey,
                ExpiredDate = null
            };
            var cpTake = new CommonParam();
            var beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, cpTake);
            if (beans == null || beans.Count == 0)
            {
                takeBean.PatientTypeId = 42;
                beans = adapter.Post<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, cpTake);
            }

            if (beans != null && beans.Count > 0)
            {
                string cữVal = units.ToString("D2");
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
                            Tutorial = string.Format("Tiêm dưới da {0} đơn vị (UI) lúc 17h00", units),
                            MedicineUseFormId = 15,
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
                    Console.WriteLine(string.Format("✔ KÊ INSULIN THÀNH CÔNG: Mã Y Lệnh = {0} (ID: {1})", resPres.ServiceReqs[0].SERVICE_REQ_CODE, resPres.ServiceReqs[0].ID));
                }
                else
                {
                    Console.WriteLine("❌ Lỗi tạo phiếu y lệnh kê đơn: " + string.Join("; ", cpPres.Messages ?? new List<string>()));
                }
            }
            else
            {
                Console.WriteLine("❌ Lỗi TakeBean: " + string.Join("; ", cpTake.Messages ?? new List<string>()));
            }
        }
    }
}
