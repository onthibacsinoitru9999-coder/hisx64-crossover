using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.SDO;
using MOS.Filter;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
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

        Run();
    }

    static void Run()
    {
        string token = "";
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogSystem.txt");
        if (File.Exists(logPath))
        {
            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("TokenCode:"))
                    {
                        var parts = line.Split(new[] { "TokenCode:" }, StringSplitOptions.None);
                        if (parts.Length > 1) token = parts[1].Trim();
                    }
                }
            }
        }

        Console.WriteLine("Token: " + (string.IsNullOrEmpty(token) ? "NULL" : token.Substring(0, 10) + "..."));

        var adapter = new BackendAdapter(new CommonParam());
        var consumer = new ApiConsumer("http://192.168.200.111:8088/", "MOS", "HIS", token);

        // 1. Check patient status in Khoa 57
        var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = "000007363494" };
        var param = new CommonParam();
        var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
        if (trList == null || trList.Count == 0)
        {
            Console.WriteLine("Cannot find treatment!");
            return;
        }
        var tr = trList[0];
        Console.WriteLine(string.Format("Treatment ID: {0}, Patient: {1}, InTime: {2}, ClinicalInTime: {3}, Dept: {4}, Status: {5}, IsPause: {6}",
            tr.ID, tr.TDL_PATIENT_NAME, tr.IN_TIME, tr.CLINICAL_IN_TIME, tr.LAST_DEPARTMENT_ID, tr.TREATMENT_END_TYPE_ID, tr.IS_PAUSE));

        // 2. Check bed room
        var tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tr.ID };
        var tbrList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", consumer, tbrf, param);
        if (tbrList != null)
        {
            foreach (var b in tbrList)
            {
                Console.WriteLine(string.Format("Bed: {0}, BedRoom: {1} (ID: {2}), RoomId: {3}, IsInRoom: {4}",
                    b.BED_NAME, b.BED_ROOM_NAME, b.BED_ROOM_ID, b.ROOM_ID, b.IS_IN_ROOM));
            }
        }

        // 3. Try UpdateWorkInfo
        var wi = new WorkInfoSDO
        {
            Rooms = new List<RoomSDO>
            {
                new RoomSDO { RoomId = 5248 },
                new RoomSDO { RoomId = 5266 }
            }
        };
        var resWi = adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, wi, param);
        Console.WriteLine("UpdateWorkInfo result: " + (resWi != null ? resWi.Count.ToString() : "NULL") + " HasException: " + param.HasException);
        if (param.Messages != null) foreach (var m in param.Messages) Console.WriteLine("  Msg: " + m);

        // 4. Try CreateTracking
        var tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            DEPARTMENT_ID = tr.LAST_DEPARTMENT_ID ?? 57,
            ROOM_ID = 5248,
            TRACKING_TIME = 20261001113500,
            CONTENT = "Khám tiếp đón vào khoa...",
            CARE_INSTRUCTION = "Chăm sóc cấp II. Ăn BT01",
            MEDICAL_INSTRUCTION = "Chuẩn bị mổ",
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME
        };
        var sdo = new HisTrackingSDO
        {
            Tracking = tracking,
            WorkingRoomId = 5248
        };
        var cp = new CommonParam();
        var created = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", consumer, sdo, cp);
        Console.WriteLine("Create result: " + (created != null ? created.ID.ToString() : "NULL") + " HasException: " + cp.HasException);
        if (cp.Messages != null) foreach (var m in cp.Messages) Console.WriteLine("  Msg: " + m);
        if (cp.BugCodes != null) foreach (var b in cp.BugCodes) Console.WriteLine("  Bug: " + b);
    }
}
