using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class AssignMissingRations
{
    public static MyAdapter adapter;

    static string ReadLiveToken()
    {
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        if (!File.Exists(p)) return "";
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line, token = "";
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
            return token;
        }
    }

    public static List<RationServiceSDO> BuildRationServices(string comboType, long patientTypeId)
    {
        var list = new List<RationServiceSDO>();
        long ptId = 42; // BẮT BUỘC 42 (Viện phí) cho 100% bệnh nhân

        if (comboType == "DD01") // Đái tháo đường
        {
            list.Add(new RationServiceSDO { ServiceId = 30180, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30181, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30133, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else if (comboType == "TM01") // Tim mạch / Tăng huyết áp
        {
            list.Add(new RationServiceSDO { ServiceId = 30117, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30093, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30094, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        else // BT01 (Bình thường / Ngoại khoa)
        {
            list.Add(new RationServiceSDO { ServiceId = 30073, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 1 } });
            list.Add(new RationServiceSDO { ServiceId = 30153, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } });
            list.Add(new RationServiceSDO { ServiceId = 30154, PatientTypeId = ptId, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } });
        }
        return list;
    }

    public static bool AssignRationForDay(ApiConsumer consumer, V_HIS_TREATMENT tr, long trackingId, DateTime date, string comboType, long requestRoomId = 5248)
    {
        long instructionTime = long.Parse(date.ToString("yyyyMMdd") + "050000");

        var sdo = new HisRationServiceReqSDO
        {
            TreatmentIds = new List<long> { tr.ID },
            InstructionTimes = new List<long> { instructionTime },
            RequestRoomId = requestRoomId > 0 ? requestRoomId : 5248,
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            HalfInFirstDay = false,
            IsForAutoCreateRation = false,
            IsForHomie = false,
            TrackingId = trackingId > 0 ? (long?)trackingId : null,
            RationServices = BuildRationServices(comboType, 42)
        };

        CommonParam callParam = new CommonParam();
        try
        {
            var result = adapter.PostData<object>("api/HisServiceReq/RationCreate", consumer, sdo, callParam);
            if (!callParam.HasException)
            {
                return true;
            }
            else
            {
                if (callParam.Messages != null)
                {
                    foreach (var msg in callParam.Messages) Console.WriteLine("      ❌ " + msg);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("      ❌ Lỗi gọi API: {0}", ex.Message));
            return false;
        }
    }

    class TargetPatient
    {
        public string PatientCode { get; set; }
        public string PatientName { get; set; }
        public string RoomBed { get; set; }
        public string Combo { get; set; }
        public List<DateTime> MissingDates { get; set; }
    }

    static void Main()
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
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        adapter = new MyAdapter();
        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        // Kích hoạt WorkInfo phòng 5248
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, workInfo, cp);
        }
        catch { }

        DateTime dSunday = new DateTime(2026, 9, 27);
        DateTime dMonday = new DateTime(2026, 9, 28);

        var targets = new List<TargetPatient>
        {
            new TargetPatient { PatientCode = "0004051068", PatientName = "LÊ VĂN MƯỜI", RoomBed = "P710 - G32", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0004074194", PatientName = "HỒ ANH HÙNG", RoomBed = "P712 - G22", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0002969881", PatientName = "HOÀNG QUANG PHƯỢNG", RoomBed = "P712 - G24", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0004061261", PatientName = "ĐOÀN ĐỨC PHONG", RoomBed = "P712 - G24", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0003677843", PatientName = "VŨ ÁNH DƯƠNG", RoomBed = "P715 - G15", Combo = "DD01", MissingDates = new List<DateTime> { dMonday } },
            new TargetPatient { PatientCode = "0003940472", PatientName = "LÒ VĂN MẠNH", RoomBed = "P716 - G02", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0004071017", PatientName = "CAO MINH ĐỨC", RoomBed = "P716 - G05", Combo = "BT01", MissingDates = new List<DateTime> { dSunday, dMonday } },
            new TargetPatient { PatientCode = "0004025186", PatientName = "BIỆN VĂN CƠ", RoomBed = "P724 - G54", Combo = "TM01", MissingDates = new List<DateTime> { dMonday } }
        };

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine("⚡ TIẾN HÀNH CHỈ ĐỊNH SUẤT ĂN DINH DƯỠNG CHO 8 BỆNH NHÂN CÒN THIẾU (27/09 - 28/09/2026)");
        Console.WriteLine("==========================================================================================================\n");

        int successCount = 0;
        int failCount = 0;

        foreach (var p in targets)
        {
            Console.WriteLine(string.Format("👉 [{0}] {1} (Mã BN: {2}) | Chế độ: {3}", p.RoomBed, p.PatientName, p.PatientCode, p.Combo));

            // Fetch Treatment
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = p.PatientCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            var tr = trList != null && trList.Count > 0 ? trList.OrderByDescending(x => x.IN_TIME).First() : null;

            if (tr == null)
            {
                Console.WriteLine("   ❌ Không tìm thấy hồ sơ điều trị!");
                failCount++;
                continue;
            }

            // Fetch latest Tracking
            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
            var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mos, trkFilter, cp);
            long latestTrackingId = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First().ID : 0;

            foreach (var date in p.MissingDates)
            {
                string dateStr = date.ToString("dd/MM/yyyy (ddd)");
                Console.Write(string.Format("   - Đang chỉ định ngày {0}... ", dateStr));

                bool ok = AssignRationForDay(mos, tr, latestTrackingId, date, p.Combo, 5248);
                if (ok)
                {
                    Console.WriteLine("✔ THÀNH CÔNG (3 bữa Sáng - Trưa - Chiều)");
                    successCount++;
                }
                else
                {
                    Console.WriteLine("❌ THẤT BẠI");
                    failCount++;
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("🎉 HOÀN THÀNH: {0} lượt chỉ định thành công | {1} lượt thất bại.", successCount, failCount));
        Console.WriteLine("==========================================================================================================");
    }
}
