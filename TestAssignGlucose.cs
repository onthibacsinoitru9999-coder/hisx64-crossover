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

class TestAssignGlucose
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
        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
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

        long treatmentId = 7271206; // Nguyễn Văn Chiến
        var trkFilter = new HisTrackingFilter { TREATMENT_ID = treatmentId };
        var trks = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, trkFilter, cp);
        Console.WriteLine(string.Format("Số tờ điều trị: {0}", trks != null ? trks.Count : 0));
        if (trks != null)
        {
            foreach (var t in trks.OrderByDescending(x => x.TRACKING_TIME))
            {
                Console.WriteLine(string.Format("  ID: {0} | Time: {1} | Room: {2} | Dept: {3}", t.ID, t.TRACKING_TIME, t.ROOM_ID, t.DEPARTMENT_ID));
            }
        }

        // Test creating tracking for 11h today if not exists
        long target11h = 20260929110000;
        var trk11h = trks != null ? trks.FirstOrDefault(x => x.TRACKING_TIME == target11h || x.TRACKING_TIME.ToString().StartsWith("20260929")) : null;
        long trkId = 0;
        long trkTime = target11h;
        if (trk11h != null)
        {
            trkId = trk11h.ID;
            trkTime = trk11h.TRACKING_TIME;
            Console.WriteLine(string.Format("Dùng tờ điều trị ngày hôm nay: ID={0}, Time={1}", trkId, trkTime));
        }
        else
        {
            Console.WriteLine("Chưa có tờ điều trị hôm nay, tạo tờ điều trị 11h...");
            var trkNew = new HIS_TRACKING
            {
                TREATMENT_ID = treatmentId,
                DEPARTMENT_ID = 57,
                ROOM_ID = 5248,
                TRACKING_TIME = target11h,
                CONTENT = "Khám: Đường máu mao mạch lúc 11:00.",
                CARE_INSTRUCTION = "Chăm sóc cấp II. Theo dõi đường máu mao mạch.",
                MEDICAL_INSTRUCTION = "Theo dõi đường máu mao mạch.",
                ICD_CODE = "M50.2",
                ICD_NAME = "Thoát vị đĩa đệm C3-4, C4-5, C5-6"
            };
            var sdoTrk = new HisTrackingSDO { Tracking = trkNew, WorkingRoomId = 5248 };
            cp = new CommonParam();
            var created = adapter.Post<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdoTrk, cp);
            if (created != null && created.ID > 0)
            {
                trkId = created.ID;
                trkTime = created.TRACKING_TIME;
                Console.WriteLine(string.Format("Đã tạo tờ điều trị mới: ID={0}", trkId));
            }
            else
            {
                Console.WriteLine("Lỗi tạo tờ điều trị: " + string.Join("; ", cp.Messages ?? new List<string>()));
                if (cp.BugCodes != null) Console.WriteLine("BugCodes: " + string.Join("; ", cp.BugCodes));
            }
        }

        // Test assign BM02426
        // Cập nhật WorkInfo
        try
        {
            var wi = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 931 },
                    new RoomSDO { RoomId = 5267 } // Room for P740
                }
            };
            adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, new CommonParam());
        }
        catch { }

        var assignSDO = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = 5248,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS Nguyễn Hữu Sâm",
            InstructionTime = trkTime,
            InstructionTimes = new List<long> { trkTime },
            UseTimes = new List<long> { trkTime },
            TrackingId = trkId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trkId, IntructionTime = trkTime }
            },
            IcdCode = "M50.2",
            IcdName = "Thoát vị đĩa đệm",
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

        cp = new CommonParam();
        var res = adapter.Post<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, assignSDO, cp);
        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine(string.Format("✔ CHỈ ĐỊNH THÀNH CÔNG! Mã Y Lệnh: {0} (ID: {1})", res.ServiceReqs[0].SERVICE_REQ_CODE, res.ServiceReqs[0].ID));
        }
        else
        {
            Console.WriteLine("❌ CHỈ ĐỊNH THẤT BẠI:");
            if (cp.Messages != null && cp.Messages.Count > 0) Console.WriteLine("  Messages: " + string.Join("; ", cp.Messages));
            if (cp.BugCodes != null && cp.BugCodes.Count > 0) Console.WriteLine("  BugCodes: " + string.Join("; ", cp.BugCodes));
        }
    }
}
