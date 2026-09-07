using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class WardRationManager
{
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

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        Run();
    }

    static string ReadLiveToken()
    {
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
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

    static List<RationServiceSDO> BuildRationServices(string combo, long patientTypeId, List<long> neededMeals)
    {
        var list = new List<RationServiceSDO>();
        long ptId = patientTypeId > 0 ? patientTypeId : 42;

        foreach (var mealId in neededMeals)
        {
            long svcId = 0;
            if (combo == "DD01")
            {
                if (mealId == 1) svcId = 30180;
                else if (mealId == 3) svcId = 30181;
                else if (mealId == 5) svcId = 30133;
            }
            else if (combo == "TM01")
            {
                if (mealId == 1) svcId = 30117;
                else if (mealId == 3) svcId = 30093;
                else if (mealId == 5) svcId = 30094;
            }
            else // BT01
            {
                if (mealId == 1) svcId = 30073;
                else if (mealId == 3) svcId = 30153;
                else if (mealId == 5) svcId = 30154;
            }

            if (svcId > 0)
            {
                list.Add(new RationServiceSDO
                {
                    ServiceId = svcId,
                    PatientTypeId = ptId,
                    RoomId = 5809,
                    Amount = 1.0m,
                    RationTimeIds = new List<long> { mealId }
                });
            }
        }
        return list;
    }

    static string DetectCombo(V_HIS_TREATMENT tr, string hintName)
    {
        string text = "";
        if (tr != null)
        {
            text = ((tr.ICD_NAME ?? "") + " " + (tr.ICD_TEXT ?? "")).ToLower();
        }

        if (hintName.Contains("HIỀN") || hintName.Contains("GIANG") || text.Contains("tháo đường") || text.Contains("đtđ") || text.Contains("diabetes"))
            return "DD01";
        if (hintName.Contains("THÌN") || text.Contains("tăng huyết áp") || text.Contains("tim mạch") || text.Contains("stent") || text.Contains("máy tạo nhịp"))
            return "TM01";
        return "BT01";
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("LỖI: Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // Kích hoạt WorkInfo phòng 5248, 5257, 5259
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 5257 },
                    new RoomSDO { RoomId = 5259 }
                }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, workInfo, cp);
        }
        catch { }

        // Danh sách phòng cần quét
        string[] targetRoomKeywords = new string[] { "710", "711", "712", "712A", "714" };

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mos, bf, cp);
        var targetRooms = bList.Where(r => r.BED_ROOM_NAME != null && targetRoomKeywords.Any(k => r.BED_ROOM_NAME.Contains(k))).ToList();

        DateTime[] targetDates = new DateTime[]
        {
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 8),
            new DateTime(2026, 9, 9)
        };

        Console.WriteLine("==========================================================================================");
        Console.WriteLine("  HỆ THỐNG KIỂM TRA & CHỈ ĐỊNH SUẤT ĂN 3 NGÀY (07/09 - 09/09/2026) - KHOA 57");
        Console.WriteLine("==========================================================================================\n");

        int totalCreated = 0;

        foreach (var room in targetRooms.OrderBy(r => r.BED_ROOM_NAME))
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var patients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mos, tbrf, cp);
            if (patients == null || patients.Count == 0) continue;

            Console.WriteLine(string.Format("🏥 BUỒNG: {0} ({1} bệnh nhân)", room.BED_ROOM_NAME, patients.Count));

            foreach (var p in patients.OrderBy(x => x.BED_NAME))
            {
                long tId = p.TREATMENT_ID;
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;

                string combo = DetectCombo(tr, p.TDL_PATIENT_NAME != null ? p.TDL_PATIENT_NAME.ToUpper() : "");

                // Lấy danh sách suất ăn hiện có
                HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
                var existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);

                Console.WriteLine(string.Format("\n🧑 [{0}] {1} (TrID: {2} | Mã BN: {3}) | Chế độ: [{4}]",
                    p.BED_NAME, p.TDL_PATIENT_NAME, tId, p.TDL_PATIENT_CODE, combo));

                foreach (var d in targetDates)
                {
                    string dStr = d.ToString("yyyyMMdd");
                    string displayDate = d.ToString("dd/MM/yyyy");
                    long instructionTime = long.Parse(dStr + "050000");

                    var mealsOnDate = existingRations != null
                        ? existingRations.Where(r => r.INTRUCTION_TIME.ToString().StartsWith(dStr)).ToList()
                        : new List<V_HIS_SERE_SERV_RATION>();

                    var existingMealIds = mealsOnDate.Select(m => m.RATION_TIME_ID).Distinct().ToList();
                    var allMeals = new List<long> { 1, 3, 5 };
                    var neededMeals = allMeals.Where(m => !existingMealIds.Contains(m)).ToList();

                    if (neededMeals.Count == 0)
                    {
                        Console.WriteLine(string.Format("   • Ngày {0}: Đủ 3 bữa ({1})",
                            displayDate, string.Join(", ", mealsOnDate.Select(m => m.RATION_TIME_NAME + ":" + m.SERVICE_NAME.Substring(0, Math.Min(10, m.SERVICE_NAME.Length))))));
                        continue;
                    }

                    string neededMealNames = string.Join(", ", neededMeals.Select(m => m == 1 ? "Sáng" : (m == 3 ? "Trưa" : "Chiều")));
                    Console.WriteLine(string.Format("   ⚡ Ngày {0}: Thiếu {1} bữa ({2}) -> Đang chỉ định theo combo [{3}]...",
                        displayDate, neededMeals.Count, neededMealNames, combo));

                    var services = BuildRationServices(combo, tr != null ? (tr.TDL_PATIENT_TYPE_ID ?? 42) : 42, neededMeals);

                    var sdo = new HisRationServiceReqSDO
                    {
                        TreatmentIds = new List<long> { tId },
                        InstructionTimes = new List<long> { instructionTime },
                        RequestRoomId = 5248,
                        RequestLoginName = "034727",
                        RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                        IcdCode = tr != null ? tr.ICD_CODE : "M51.1",
                        IcdName = tr != null ? tr.ICD_NAME : "Thoát vị đĩa đệm",
                        IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                        IcdText = tr != null ? tr.ICD_TEXT : "",
                        HalfInFirstDay = false,
                        IsForAutoCreateRation = false,
                        IsForHomie = false,
                        RationServices = services
                    };

                    CommonParam reqParam = new CommonParam();
                    var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, reqParam);

                    if (!reqParam.HasException)
                    {
                        Console.WriteLine(string.Format("     ✅ ĐÃ TẠO THÀNH CÔNG suất ăn ngày {0}!", displayDate));
                        totalCreated++;
                        // Cập nhật lại danh sách suất ăn
                        existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("     ❌ THẤT BẠI ngày {0}: {1}", displayDate, reqParam.GetMessage()));
                    }
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("==========================================================================================");
        Console.WriteLine(string.Format("KẾT THÚC: Đã bổ sung {0} lượt y lệnh suất ăn cho các bệnh nhân còn thiếu.", totalCreated));
        Console.WriteLine("==========================================================================================");
    }
}
