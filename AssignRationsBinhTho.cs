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

namespace WardRations
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

    class Program
    {
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

        static string GetLiveToken()
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

        static List<RationServiceSDO> BuildServices(string combo, long patientTypeId, List<long> neededMealIds)
        {
            var list = new List<RationServiceSDO>();
            long ptId = patientTypeId > 0 ? patientTypeId : 42;

            foreach (var mealId in neededMealIds)
            {
                long svcId = 0;
                // BT01: Sang = 30073 (Meal 1), Trua = 30153 (Meal 3), Chieu = 30154 (Meal 5)
                if (mealId == 1) svcId = 30073;
                else if (mealId == 3) svcId = 30153;
                else if (mealId == 5) svcId = 30154;

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

        class TargetPatient
        {
            public long TreatmentId { get; set; }
            public string PatientName { get; set; }
            public string RoomBed { get; set; }
            public string Combo { get; set; }
            public List<DateTime> Dates { get; set; }
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = GetLiveToken();
            if (string.IsNullOrEmpty(token)) { Console.WriteLine("LỖI: Không có token!"); return; }

            ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            MyAdapter adapter = new MyAdapter();
            CommonParam cp = new CommonParam();

            // Update WorkInfo
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
                Console.WriteLine("✔ Đã kích hoạt WorkInfo phòng làm việc P734 & Buồng 714 thành công.");
            }
            catch { }

            var targets = new List<TargetPatient>
            {
                new TargetPatient {
                    TreatmentId = 7163469,
                    PatientName = "VŨ ĐỨC BÌNH",
                    RoomBed = "714 - G17",
                    Combo = "BT01",
                    Dates = new List<DateTime> { new DateTime(2026, 9, 9) }
                },
                new TargetPatient {
                    TreatmentId = 7163700,
                    PatientName = "NGUYỄN VĂN THƠ",
                    RoomBed = "714 - G18",
                    Combo = "BT01",
                    Dates = new List<DateTime> { new DateTime(2026, 9, 8), new DateTime(2026, 9, 9) }
                }
            };

            foreach (var pt in targets)
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = pt.TreatmentId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;

                HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = pt.TreatmentId };
                var existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);

                Console.WriteLine("==================================================================================");
                Console.WriteLine(string.Format("🧑 [{0}] {1} (TrID: {2}) | Chế độ ăn: {3}",
                    pt.RoomBed, pt.PatientName, pt.TreatmentId, pt.Combo));

                foreach (var date in pt.Dates)
                {
                    string dateStr = date.ToString("yyyyMMdd");
                    string displayDate = date.ToString("dd/MM/yyyy");
                    long instructionTime = long.Parse(dateStr + "050000");

                    var mealsOnDate = existingRations != null
                        ? existingRations.Where(r => r.INTRUCTION_TIME.ToString().StartsWith(dateStr)).ToList()
                        : new List<V_HIS_SERE_SERV_RATION>();

                    var existingMealIds = mealsOnDate.Select(m => m.RATION_TIME_ID).Distinct().ToList();
                    var allMeals = new List<long> { 1, 3, 5 }; // Sáng, Trưa, Chiều
                    var neededMeals = allMeals.Where(m => !existingMealIds.Contains(m)).ToList();

                    if (neededMeals.Count == 0)
                    {
                        Console.WriteLine(string.Format("   • Ngày {0}: Đã có đủ {1} bữa -> BỎ QUA", displayDate, mealsOnDate.Count));
                        continue;
                    }

                    string neededMealNames = string.Join(", ", neededMeals.Select(m => m == 1 ? "Sáng" : (m == 3 ? "Trưa" : "Chiều")));
                    Console.WriteLine(string.Format("   • Ngày {0}: Đang chỉ định {1} bữa ({2}) theo chế độ [{3}]...",
                        displayDate, neededMeals.Count, neededMealNames, pt.Combo));

                    var sdo = new HisRationServiceReqSDO
                    {
                        TreatmentIds = new List<long> { pt.TreatmentId },
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
                        RationServices = BuildServices(pt.Combo, tr != null ? (tr.TDL_PATIENT_TYPE_ID ?? 42) : 42, neededMeals)
                    };

                    CommonParam reqParam = new CommonParam();
                    var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, reqParam);

                    if (!reqParam.HasException)
                    {
                        Console.WriteLine(string.Format("     ✅ THÀNH CÔNG chỉ định suất ăn ngày {0}!", displayDate));
                    }
                    else
                    {
                        string bug = reqParam.GetBugCode();
                        string msg = reqParam.GetMessage();
                        string detail = (reqParam.Messages != null && reqParam.Messages.Count > 0) ? string.Join("; ", reqParam.Messages) : msg;
                        Console.WriteLine(string.Format("     ❌ THẤT BẠI ngày {0}! Bug={1}, Msg={2}", displayDate, bug, detail));
                    }
                }
            }

            Console.WriteLine("\nHoàn tất thực hiện chỉ định suất ăn cho BN Bình & Thơ.");
        }
    }
}
