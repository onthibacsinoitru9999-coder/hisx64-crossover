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

class Assign6Patients
{
    static void Main(string[] args)
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
        if (File.Exists("doctor_standalone.token"))
        {
            token = File.ReadAllText("doctor_standalone.token").Split('|')[0].Trim();
        }
        else if (File.Exists("doctor_hn.token"))
        {
            token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();
        }

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
                    new RoomSDO { RoomId = 5259 },
                    new RoomSDO { RoomId = 5260 },
                    new RoomSDO { RoomId = 5261 },
                    new RoomSDO { RoomId = 5262 },
                    new RoomSDO { RoomId = 5267 }
                }
            };
            adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, new CommonParam());
        }
        catch { }

        string[] patientCodes = new string[] {
            "0004058475", // Chiến
            "0002871946", // Thiêm
            "0003842817", // Mai
            "0004079114", // Tư
            "0004076519", // Thanh
            "0004054272", // Vinh
            "0004038654"  // Thành
        };

        foreach (var pCode in patientCodes)
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("Bệnh nhân: " + pCode);
            var patFilter = new HisPatientFilter { PATIENT_CODE = pCode };
            var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", mosConsumer, patFilter, new CommonParam());
            if (pats == null || pats.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy BN");
                continue;
            }
            var pat = pats[0];
            Console.WriteLine(string.Format("Họ tên: {0} | ID: {1}", pat.VIR_PATIENT_NAME, pat.ID));

            // Get active treatment
            var trmFilter = new HisTreatmentFilter { PATIENT_ID = pat.ID };
            var trms = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, trmFilter, new CommonParam());
            var trm = trms != null ? trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault(x => x.IS_PAUSE != 1) : null;
            if (trm == null && trms != null) trm = trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault();
            if (trm == null)
            {
                Console.WriteLine("❌ Không tìm thấy đợt điều trị");
                continue;
            }
            Console.WriteLine(string.Format("Hồ sơ ĐT: {0} | InTime: {1} | IsPause: {2}", trm.TREATMENT_CODE, trm.IN_TIME, trm.IS_PAUSE));

            // Get tracking
            var trkFilter = new HisTrackingFilter { TREATMENT_ID = trm.ID };
            var trks = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, trkFilter, new CommonParam());
            var trkToday = trks != null ? trks.OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault(x => x.TRACKING_TIME.ToString().StartsWith("20260929")) : null;
            if (trkToday == null)
            {
                Console.WriteLine("❌ Chưa có tờ điều trị hôm nay (20260929)");
                continue;
            }
            Console.WriteLine(string.Format("Tờ điều trị hôm nay: ID={0}, Time={1}, Room={2}", trkToday.ID, trkToday.TRACKING_TIME, trkToday.ROOM_ID));

            // Assign BM02426
            long assignTime = trkToday.TRACKING_TIME;
            var assignSDO = new AssignServiceSDO
            {
                TreatmentId = trm.ID,
                RequestRoomId = 5248, // P734
                RequestLoginName = "034727",
                RequestUserName = "Ths.BS Nguyễn Hữu Sâm",
                InstructionTime = assignTime,
                InstructionTimes = new List<long> { assignTime },
                UseTimes = new List<long> { assignTime },
                TrackingId = trkToday.ID,
                TrackingInfos = new List<TrackingInfoSDO>
                {
                    new TrackingInfoSDO { TrackingId = trkToday.ID, IntructionTime = assignTime }
                },
                IcdCode = !string.IsNullOrEmpty(trkToday.ICD_CODE) ? trkToday.ICD_CODE : trm.ICD_CODE,
                IcdName = !string.IsNullOrEmpty(trkToday.ICD_NAME) ? trkToday.ICD_NAME : trm.ICD_NAME,
                ServiceReqDetails = new List<ServiceReqDetailSDO>
                {
                    new ServiceReqDetailSDO
                    {
                        ServiceId = 6217, // BM02426
                        Amount = 1.0m,
                        PatientTypeId = 1,
                        RoomId = 931, // Tiểu phẫu Nhà Q
                        SampleTypeCode = "BP0042",
                        InstructionNote = "Đo ĐMMM lúc 11:00",
                        MultipleExecute = 1
                    }
                }
            };

            var postCp = new CommonParam();
            var res = adapter.Post<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, assignSDO, postCp);
            if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
            {
                Console.WriteLine(string.Format("✔ CHỈ ĐỊNH THÀNH CÔNG! Mã Y Lệnh: {0} (ID: {1})", res.ServiceReqs[0].SERVICE_REQ_CODE, res.ServiceReqs[0].ID));
            }
            else
            {
                Console.WriteLine("❌ CHỈ ĐỊNH THẤT BẠI:");
                if (postCp.Messages != null && postCp.Messages.Count > 0) Console.WriteLine("  Messages: " + string.Join("; ", postCp.Messages));
                if (postCp.BugCodes != null && postCp.BugCodes.Count > 0) Console.WriteLine("  BugCodes: " + string.Join("; ", postCp.BugCodes));
            }
        }
    }
}
