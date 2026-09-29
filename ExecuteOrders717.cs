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

    static List<RationServiceSDO> BuildRationServices(string combo, long patientTypeId, List<long> mealIds)
    {
        var list = new List<RationServiceSDO>();
        long ptId = patientTypeId > 0 ? patientTypeId : 42;

        foreach (var mealId in mealIds)
        {
            long svcId = 0;
            if (combo == "TM01")
            {
                if (mealId == 1) svcId = 30117;      // TM01-06 Sáng
                else if (mealId == 3) svcId = 30093; // TM01-11 Trưa
                else if (mealId == 5) svcId = 30094; // TM01-17 Chiều
            }
            else // BT01
            {
                if (mealId == 1) svcId = 30073;      // BT01-06 Sáng
                else if (mealId == 3) svcId = 30153; // BT01-11 Trưa
                else if (mealId == 5) svcId = 30154; // BT01-17 Chiều
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

    static void Run(bool doExecute)
    {
        string token = GetLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("❌ LỖI: Không tìm thấy Token!"); return; }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // Kích hoạt WorkInfo phòng 5248 (P734 Khoa 57)
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 5252 },
                    new RoomSDO { RoomId = 5251 }
                }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, workInfo, cp);
        }
        catch { }

        long[] treatmentIds = new long[] { 7309184, 7252303, 7332069 };
        string[] targetDates = new string[] { "20260930", "20261001", "20261002" }; // T4, T5, T6

        Console.WriteLine("==========================================================================================");
        Console.WriteLine("  HỆ THỐNG KÊ SUẤT ĂN & SAO CHÉP ĐƠN THUỐC TỚI THỨ 6 (02/10/2026)");
        Console.WriteLine("  ÁP DỤNG: 3 BỆNH NHÂN ĐẦU TIÊN PHÒNG 717 - KHOA CTCH & CỘT SỐNG (KHOA 57)");
        Console.WriteLine(string.Format("  CHẾ ĐỘ: {0}", doExecute ? ">>> THỰC THI CHÍNH THỨC (EXECUTE) <<<" : "KIỂM TRA ĐỐI SOÁT (DRY RUN)"));
        Console.WriteLine("==========================================================================================\n");

        foreach (var tId in treatmentIds)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
            var tr = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp).FirstOrDefault();
            if (tr == null) continue;

            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("🧑 BN: {0} | Mã BN: {1} | Mã ĐT: {2} | TreatmentId: {3}",
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tId));
            Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} | Phụ: [{2}] {3}",
                tr.ICD_CODE, tr.ICD_NAME, tr.ICD_SUB_CODE, tr.ICD_TEXT));

            // Xác định combo suất ăn theo tiền sử / chẩn đoán
            string rationCombo = "BT01";
            if (tId == 7309184 || (tr.ICD_CODE ?? "").Contains("I10") || (tr.ICD_SUB_CODE ?? "").Contains("I10"))
            {
                rationCombo = "TM01";
            }

            // ==========================================
            // 1. XỬ LÝ SUẤT ĂN
            // ==========================================
            Console.WriteLine(string.Format("   🍚 [SUẤT ĂN DINH DƯỠNG - Combo {0}]:", rationCombo));
            HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
            var existingRations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);

            foreach (var dateStr in targetDates)
            {
                string displayDate = string.Format("{0}/{1}/{2}", dateStr.Substring(6, 2), dateStr.Substring(4, 2), dateStr.Substring(0, 4));
                var mealsOnDate = existingRations != null
                    ? existingRations.Where(x => x.INTRUCTION_TIME.ToString().StartsWith(dateStr)).ToList()
                    : new List<V_HIS_SERE_SERV_RATION>();

                var existingMealIds = mealsOnDate.Select(m => m.RATION_TIME_ID).Distinct().ToList();
                var allMeals = new List<long> { 1, 3, 5 };
                var neededMeals = allMeals.Where(m => !existingMealIds.Contains(m)).ToList();

                if (neededMeals.Count == 0)
                {
                    Console.WriteLine(string.Format("     • Ngày {0}: ĐÃ CÓ ĐỦ {1} bữa ({2}) -> BỎ QUA",
                        displayDate, mealsOnDate.Count, string.Join(", ", mealsOnDate.Select(m => m.SERVICE_NAME))));
                    continue;
                }

                Console.WriteLine(string.Format("     • Ngày {0}: CẦN KÊ {1} bữa ({2})",
                    displayDate, neededMeals.Count, string.Join(", ", neededMeals.Select(m => m == 1 ? "Sáng" : (m == 3 ? "Trưa" : "Chiều")))));

                if (doExecute)
                {
                    long instructionTime = long.Parse(dateStr + "050000");
                    var sdo = new HisRationServiceReqSDO
                    {
                        TreatmentIds = new List<long> { tId },
                        InstructionTimes = new List<long> { instructionTime },
                        RequestRoomId = 5248,
                        RequestLoginName = "034727",
                        RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                        IcdCode = tr.ICD_CODE,
                        IcdName = tr.ICD_NAME,
                        IcdSubCode = tr.ICD_SUB_CODE,
                        IcdText = tr.ICD_TEXT,
                        HalfInFirstDay = false,
                        IsForAutoCreateRation = false,
                        IsForHomie = false,
                        RationServices = BuildRationServices(rationCombo, tr.TDL_PATIENT_TYPE_ID ?? 42, neededMeals)
                    };

                    CommonParam reqParam = new CommonParam();
                    var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, reqParam);
                    if (!reqParam.HasException)
                    {
                        Console.WriteLine(string.Format("       ✅ Kê thành công suất ăn ngày {0}!", displayDate));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("       ❌ Kê thất bại ngày {0}! {1} - {2}", displayDate, reqParam.GetBugCode(), reqParam.GetMessage()));
                    }
                }
            }

            // ==========================================
            // 2. XỬ LÝ SAO CHÉP ĐƠN THUỐC
            // ==========================================
            Console.WriteLine("   💊 [SAO CHÉP ĐƠN THUỐC]:");
            HisExpMestMedicineViewFilter mf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId };
            var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, mf, cp);

            // Kiểm tra các thuốc mẫu từ ngày 29/09/2026 (lọc theo TDL_INTRUCTION_TIME)
            var sourceMeds = allMeds != null
                ? allMeds.Where(x => (x.TDL_INTRUCTION_TIME ?? 0).ToString().StartsWith("20260929")).ToList()
                : new List<V_HIS_EXP_MEST_MEDICINE>();

            // Lọc ra các thuốc uống thường quy độc lập (loại trừ thuốc trùng lặp nếu có)
            var distinctSourceMeds = sourceMeds
                .GroupBy(x => x.MEDICINE_TYPE_ID)
                .Select(g => g.First())
                .ToList();

            if (distinctSourceMeds.Count == 0)
            {
                Console.WriteLine("     ⚠️ Không tìm thấy thuốc mẫu ngày 29/09/2026 để sao chép!");
                continue;
            }

            Console.WriteLine(string.Format("     Đơn mẫu ngày 29/09/2026 gồm {0} khoản thuốc:", distinctSourceMeds.Count));
            foreach (var sm in distinctSourceMeds)
            {
                Console.WriteLine(string.Format("       - [{0}] | SL: {1} {2} | Cữ: S:{3} Tr:{4} C:{5} T:{6} | HD: {7}",
                    sm.MEDICINE_TYPE_NAME, sm.AMOUNT, sm.SERVICE_UNIT_NAME, sm.MORNING, sm.NOON, sm.AFTERNOON, sm.EVENING, sm.TUTORIAL));
            }

            foreach (var dateStr in targetDates)
            {
                string displayDate = string.Format("{0}/{1}/{2}", dateStr.Substring(6, 2), dateStr.Substring(4, 2), dateStr.Substring(0, 4));
                var existingOnDate = allMeds != null
                    ? allMeds.Where(x => (x.TDL_INTRUCTION_TIME ?? 0).ToString().StartsWith(dateStr)).ToList()
                    : new List<V_HIS_EXP_MEST_MEDICINE>();

                if (existingOnDate.Count > 0)
                {
                    Console.WriteLine(string.Format("     • Ngày {0}: ĐÃ CÓ {1} khoản thuốc ({2}) -> BỎ QUA",
                        displayDate, existingOnDate.Count, string.Join(", ", existingOnDate.Select(m => m.MEDICINE_TYPE_NAME))));
                    continue;
                }

                Console.WriteLine(string.Format("     • Ngày {0}: CẦN SAO CHÉP {1} khoản thuốc sang Kho thuốc viên (4210)",
                    displayDate, distinctSourceMeds.Count));

                if (doExecute)
                {
                    long targetInstructionTime = long.Parse(dateStr + "080000");

                    var targetPresMeds = new List<PresMedicineSDO>();
                    foreach (var sm in distinctSourceMeds)
                    {
                        decimal m = 0, no = 0, af = 0, ev = 0;
                        decimal.TryParse(sm.MORNING, out m);
                        decimal.TryParse(sm.NOON, out no);
                        decimal.TryParse(sm.AFTERNOON, out af);
                        decimal.TryParse(sm.EVENING, out ev);
                        decimal dailyDose = m + no + af + ev;
                        decimal amount = (dailyDose > 0) ? dailyDose : sm.AMOUNT;

                        // Thuốc uống lĩnh kho viên 4210
                        long targetStock = 4210;

                        targetPresMeds.Add(new PresMedicineSDO
                        {
                            MedicineTypeId = sm.MEDICINE_TYPE_ID,
                            MediStockId = targetStock,
                            PatientTypeId = sm.PATIENT_TYPE_ID ?? 1,
                            Amount = amount,
                            Morning = sm.MORNING,
                            Noon = sm.NOON,
                            Afternoon = sm.AFTERNOON,
                            Evening = sm.EVENING,
                            Tutorial = sm.TUTORIAL,
                            NumOfDays = 1
                        });
                    }

                    var presSdo = new InPatientPresSDO
                    {
                        TreatmentId = tId,
                        RequestRoomId = 5248,
                        RequestLoginName = "034727",
                        RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                        IcdCode = tr.ICD_CODE,
                        IcdName = tr.ICD_NAME,
                        IcdSubCode = tr.ICD_SUB_CODE,
                        IcdText = tr.ICD_TEXT,
                        PrescriptionTypeId = (PrescriptionType)1,
                        InstructionTimes = new List<long> { targetInstructionTime },
                        Medicines = targetPresMeds
                    };

                    CommonParam presParam = new CommonParam();
                    var presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mos, presSdo, presParam);

                    if (presRes != null && presRes.ExpMests != null && presRes.ExpMests.Count > 0)
                    {
                        string mests = string.Join(", ", presRes.ExpMests.Select(x => string.Format("{0} (Kho {1})", x.EXP_MEST_CODE, x.MEDI_STOCK_ID)));
                        Console.WriteLine(string.Format("       ✅ Sao chép thành công đơn thuốc ngày {0}! Phiếu xuất=[{1}]", displayDate, mests));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("       ❌ Sao chép thất bại ngày {0}! {1} - {2}", displayDate, presParam.GetBugCode(), presParam.GetMessage()));
                    }
                }
            }
        }

        Console.WriteLine("\n==========================================================================================");
        Console.WriteLine("HOÀN TẤT KIỂM TRA & XỬ LÝ.");
        Console.WriteLine("==========================================================================================");
    }
}
