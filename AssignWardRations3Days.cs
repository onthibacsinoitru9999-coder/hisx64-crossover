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

            bool doExecute = args != null && args.Any(a => a.ToLower() == "--execute" || a.ToLower() == "-y");
            Run(doExecute);
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

        static List<RationServiceSDO> BuildServices(string combo, long patientTypeId, List<long> neededMealIds, List<V_HIS_SERE_SERV_RATION> pastRations)
        {
            var list = new List<RationServiceSDO>();
            long ptId = patientTypeId > 0 ? patientTypeId : 42;

            foreach (var mealId in neededMealIds)
            {
                long svcId = 0;
                // Reuse existing service ID from patient history if available
                var pastMeal = pastRations != null ? pastRations.FirstOrDefault(m => m.RATION_TIME_ID == mealId) : null;
                if (pastMeal != null && pastMeal.SERVICE_ID > 0)
                {
                    svcId = pastMeal.SERVICE_ID;
                }
                else
                {
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

        class PatientTarget
        {
            public long TreatmentId { get; set; }
            public string PatientName { get; set; }
            public string RoomBed { get; set; }
            public string DefaultCombo { get; set; }
            public string Reason { get; set; }
        }

        static void Run(bool doExecute)
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
                Console.WriteLine("✔ Đã kích hoạt WorkInfo phòng làm việc thành công.");
            }
            catch { }

            // 13 Patients in ward list
            var targets = new List<PatientTarget>
            {
                new PatientTarget { TreatmentId = 7149582, PatientName = "NGUYỄN THỊ HOÀI NAM", RoomBed = "712A - G19", DefaultCombo = "BT01", Reason = "Thoát vị đĩa đệm C5-6" },
                new PatientTarget { TreatmentId = 7160960, PatientName = "TRẦN NHÂN CÁCH", RoomBed = "712 - G21", DefaultCombo = "BT01", Reason = "Hậu phẫu giải ép TVĐĐ L45" },
                new PatientTarget { TreatmentId = 7148757, PatientName = "PHẠM THỊNH NUÔI", RoomBed = "712 - G23", DefaultCombo = "BT01", Reason = "Đứt hoàn toàn DC bánh chè" },
                new PatientTarget { TreatmentId = 7114722, PatientName = "LÝ A ĐI", RoomBed = "712 - G24", DefaultCombo = "BT01", Reason = "Gãy cẳng chân P" },
                new PatientTarget { TreatmentId = 7139074, PatientName = "ĐỖ XUÂN MẠNH", RoomBed = "712 - G25", DefaultCombo = "BT01", Reason = "Gãy hở bàn chân P, CTSN" },
                new PatientTarget { TreatmentId = 7121315, PatientName = "DƯƠNG VĂN HIỀN", RoomBed = "712 - G26", DefaultCombo = "DD01", Reason = "Gãy LMC đùi P / THA - ĐTĐ - Tai biến cũ" },
                new PatientTarget { TreatmentId = 7159410, PatientName = "VŨ NGỌC THẮNG", RoomBed = "711 - G27", DefaultCombo = "BT01", Reason = "Trật cùng đòn vai P" },
                new PatientTarget { TreatmentId = 7161463, PatientName = "THÁI VĂN HINH", RoomBed = "711 - G28", DefaultCombo = "BT01", Reason = "Hạt tophi, Gút mạn" },
                new PatientTarget { TreatmentId = 7129165, PatientName = "TRỊNH THỊ KIM LOAN", RoomBed = "711 - G29", DefaultCombo = "BT01", Reason = "U đốt sống T2T3" },
                new PatientTarget { TreatmentId = 7138539, PatientName = "NGUYỄN THỊ THÌN", RoomBed = "711 - G30", DefaultCombo = "TM01", Reason = "Xẹp L2 / THA, Stent mạch vành" },
                new PatientTarget { TreatmentId = 7149214, PatientName = "ĐẶNG NGỌC OANH", RoomBed = "710 - G31", DefaultCombo = "BT01", Reason = "TVĐĐ L45-L5S1 hậu phẫu" },
                new PatientTarget { TreatmentId = 7141005, PatientName = "TRẦN VĂN GIANG", RoomBed = "710 - G32", DefaultCombo = "DD01", Reason = "TVĐĐ L4-5, hẹp ống sống / ĐTĐ" },
                new PatientTarget { TreatmentId = 7146983, PatientName = "NGUYỄN VĂN QUẾ", RoomBed = "710 - G33", DefaultCombo = "BT01", Reason = "TD u bao rễ TK L5" }
            };

            DateTime[] targetDates = new DateTime[]
            {
                new DateTime(2026, 9, 7),
                new DateTime(2026, 9, 8),
                new DateTime(2026, 9, 9)
            };

            Console.WriteLine("==================================================================================");
            Console.WriteLine("  HỆ THỐNG CHỈ ĐỊNH SUẤT ĂN DINH DƯỠNG BỆNH LÝ 3 NGÀY (07/09 - 09/09/2026)");
            Console.WriteLine("  DANH SÁCH: 13 BỆNH NHÂN BUỒNG 712A, 712, 711, 710 - KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Console.WriteLine("  CHẾ ĐỘ: " + (doExecute ? ">>> THỰC THI CHÍNH THỨC (EXECUTE) <<<" : "KIỂM TRA (DRY RUN)"));
            Console.WriteLine("==================================================================================\n");

            int totalSuccess = 0;
            int totalSkipped = 0;

            foreach (var pt in targets)
            {
                // Fetch Treatment
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = pt.TreatmentId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;

                // Fetch existing rations
                HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = pt.TreatmentId };
                var existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);

                // Detect actual combo used in history
                string activeCombo = pt.DefaultCombo;
                if (existingRations != null && existingRations.Count > 0)
                {
                    var lastRation = existingRations.OrderByDescending(x => x.INTRUCTION_TIME).FirstOrDefault();
                    if (lastRation != null && !string.IsNullOrEmpty(lastRation.SERVICE_NAME))
                    {
                        if (lastRation.SERVICE_NAME.StartsWith("GO")) activeCombo = "GO01 (Gút)";
                        else if (lastRation.SERVICE_NAME.StartsWith("DD")) activeCombo = "DD01 (Đái tháo đường)";
                        else if (lastRation.SERVICE_NAME.StartsWith("TM")) activeCombo = "TM01 (Tim mạch / THA)";
                        else if (lastRation.SERVICE_NAME.StartsWith("BT21")) activeCombo = "BT21 (Hậu phẫu)";
                        else if (lastRation.SERVICE_NAME.StartsWith("BT")) activeCombo = "BT01 (Ngoại khoa tiêu chuẩn)";
                    }
                }

                Console.WriteLine("----------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("🧑 [{0}] {1} (TrID: {2}) | Chẩn đoán: {3} | Chế độ ăn: {4}",
                    pt.RoomBed, pt.PatientName, pt.TreatmentId, pt.Reason, activeCombo));

                foreach (var date in targetDates)
                {
                    string dateStr = date.ToString("yyyyMMdd");
                    string displayDate = date.ToString("dd/MM/yyyy");
                    long instructionTime = long.Parse(dateStr + "050000");

                    // Check existing meals on this date
                    var mealsOnDate = existingRations != null
                        ? existingRations.Where(r => r.INTRUCTION_TIME.ToString().StartsWith(dateStr)).ToList()
                        : new List<V_HIS_SERE_SERV_RATION>();

                    var existingMealIds = mealsOnDate.Select(m => m.RATION_TIME_ID).Distinct().ToList();
                    var allMeals = new List<long> { 1, 3, 5 }; // Sáng, Trưa, Chiều
                    var neededMeals = allMeals.Where(m => !existingMealIds.Contains(m)).ToList();

                    if (neededMeals.Count == 0)
                    {
                        Console.WriteLine(string.Format("   • Ngày {0}: Đã có đủ {1} bữa ({2}) -> BỎ QUA",
                            displayDate, mealsOnDate.Count, string.Join(", ", mealsOnDate.Select(m => m.SERVICE_NAME))));
                        totalSkipped++;
                        continue;
                    }

                    string neededMealNames = string.Join(", ", neededMeals.Select(m => m == 1 ? "Sáng" : (m == 3 ? "Trưa" : "Chiều")));
                    Console.WriteLine(string.Format("   • Ngày {0}: Cần chỉ định {1} bữa ({2}) theo chế độ [{3}]",
                        displayDate, neededMeals.Count, neededMealNames, activeCombo));

                    if (doExecute)
                    {
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
                            RationServices = BuildServices(pt.DefaultCombo, tr != null ? (tr.TDL_PATIENT_TYPE_ID ?? 42) : 42, neededMeals, existingRations)
                        };

                        CommonParam reqParam = new CommonParam();
                        var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, reqParam);

                        if (!reqParam.HasException)
                        {
                            Console.WriteLine(string.Format("     ✅ Chỉ định THÀNH CÔNG suất ăn ngày {0}!", displayDate));
                            totalSuccess++;
                        }
                        else
                        {
                            string bug = reqParam.GetBugCode();
                            string msg = reqParam.GetMessage();
                            string detail = (reqParam.Messages != null && reqParam.Messages.Count > 0) ? string.Join("; ", reqParam.Messages) : msg;
                            Console.WriteLine(string.Format("     ❌ THẤT BẠI ngày {0}! Bug={1}, Msg={2}", displayDate, bug, detail));
                        }
                    }
                    else
                    {
                        Console.WriteLine("     [DRY RUN] Đã chuẩn bị sẵn sàng y lệnh.");
                    }
                }
            }

            Console.WriteLine("\n==================================================================================");
            Console.WriteLine(string.Format("TỔNG KẾT: Thành công={0} đợt | Bỏ qua đã có={1} đợt", totalSuccess, totalSkipped));
            Console.WriteLine("==================================================================================");
        }
    }
}