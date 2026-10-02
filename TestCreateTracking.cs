using System;
using System.Collections.Generic;
using System.IO;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

class TestCreateTracking
{
    static void Main(string[] args)
    {
        string token = File.ReadAllText("doctor_hn.token").Trim();
        var client = new WebApiClient();
        client.SetToken(token);
        var mosConsumer = new ApiConsumerStore("http://192.168.7.236:1608/", token, "MOS_KEY");
        var adapter = new BackendAdapter(new CommonParam());

        long treatmentId = 7344259;
        long trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        // Lấy Treatment
        var tf = new HisTreatmentFilter { ID = treatmentId };
        var trs = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, tf, new CommonParam());
        if (trs == null || trs.Count == 0)
        {
            Console.WriteLine("Không tìm thấy hồ sơ điều trị!");
            return;
        }
        var tr = trs[0];
        Console.WriteLine(string.Format("Treatment: ID={0}, Code={1}, Status={2}, Dept={3}", tr.ID, tr.TREATMENT_CODE, tr.IS_PAUSE, tr.DEPARTMENT_ID));

        // Kích hoạt WorkInfo phòng 5248 (P734) và 5263 (P733)
        try
        {
            var wi = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 5263 }
                }
            };
            adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, new CommonParam());
            Console.WriteLine("Đã cập nhật WorkInfo phòng 5248, 5263");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi UpdateWorkInfo: " + ex.Message);
        }

        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            DEPARTMENT_ID = 57,
            ROOM_ID = 5248,
            TRACKING_TIME = trackingTime,
            CONTENT = "Mạch: 95 l/p, HA: 127/78 mmHg. Tiếp đón bệnh nhân vào buồng 733.",
            CARE_INSTRUCTION = "Chế độ chăm sóc cấp 3.",
            MEDICAL_INSTRUCTION = "Nghỉ ngơi tại giường, giảm đau.",
            ICD_CODE = tr.ICD_CODE ?? "M48.55",
            ICD_NAME = tr.ICD_NAME ?? "Xẹp đốt sống thắt lưng",
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };

        var sdo = new HisTrackingSDO
        {
            Tracking = tracking,
            WorkingRoomId = 5248
        };

        var cp = new CommonParam();
        var res = adapter.Post<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, cp);
        if (res != null && res.ID > 0)
        {
            Console.WriteLine("✔ TẠO THÀNH CÔNG TỜ ĐIỀU TRỊ! ID: " + res.ID);
        }
        else
        {
            Console.WriteLine("❌ TẠO THẤT BẠI!");
            if (cp.Messages != null && cp.Messages.Count > 0)
                Console.WriteLine("Messages: " + string.Join(" | ", cp.Messages));
            if (cp.BugCodes != null && cp.BugCodes.Count > 0)
                Console.WriteLine("BugCodes: " + string.Join(" | ", cp.BugCodes));
            if (!string.IsNullOrEmpty(cp.GetMessage()))
                Console.WriteLine("GetMessage: " + cp.GetMessage());
            if (!string.IsNullOrEmpty(cp.GetBugCode()))
                Console.WriteLine("GetBugCode: " + cp.GetBugCode());
        }
    }
}
