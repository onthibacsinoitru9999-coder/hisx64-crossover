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

namespace CopyPrescriptions
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
            string[] paths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
                @"Logs\LogSystem.txt",
                @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
            };

            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    try
                    {
                        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string line;
                            string token = "";
                            while ((line = reader.ReadLine()) != null)
                            {
                                int idx = line.IndexOf("TokenCode|");
                                if (idx >= 0 && line.Length >= idx + 10 + 64)
                                {
                                    token = line.Substring(idx + 10, 64);
                                }
                            }
                            if (!string.IsNullOrEmpty(token)) return token;
                        }
                    }
                    catch { }
                }
            }
            return "";
        }

        public class PatientPrescriptionPlan
        {
            public V_HIS_TREATMENT_BED_ROOM BedPatient { get; set; }
            public V_HIS_TREATMENT Treatment { get; set; }
            public long RoomId { get; set; }
            public string SourceDate { get; set; }
            public List<V_HIS_EXP_MEST_MEDICINE> SourceMeds { get; set; }
            public List<PresMedicineSDO> TargetMeds { get; set; }
            public bool AlreadyHasTomorrowPrescription { get; set; }
            public List<string> ExistingTomorrowMests { get; set; }
            public string Status { get; set; }
            public string ResultDetails { get; set; }
        }

        static void Run(bool doExecute)
        {
            Console.OutputEncoding = Encoding.UTF8;
            StreamWriter log = new StreamWriter("copy_prescriptions_712_714_724_725_execution_report.txt", false, Encoding.UTF8);

            Action<string> Log = msg =>
            {
                Console.WriteLine(msg);
                log.WriteLine(msg);
            };

            try
            {
                string token = GetLiveToken();
                if (string.IsNullOrEmpty(token))
                {
                    Log("ERROR: Không tìm thấy Token hợp lệ trong LogSystem.txt!");
                    return;
                }

                ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
                MyAdapter adapter = new MyAdapter();
                CommonParam cp = new CommonParam();

                Log("==================================================================================================");
                Log("  HỆ THỐNG SAO CHÉP ĐƠN THUỐC NỘI TRÚ SANG NGÀY MAI (27/08/2026 08:00:00)");
                Log("  ÁP DỤNG: CÁC BUỒNG 712, 714, 724, 725 - KHOA CTCH & CỘT SỐNG (KHOA 57)");
                Log("  KHO ĐÍCH: KHO DƯỢC (4209 - THUỐC ỐNG, 4210 - THUỐC VIÊN, 804 - DỊCH TRUYỀN, 4208 - HƯỚNG THẦN)");
                Log(string.Format("  CHẾ ĐỘ: {0}", doExecute ? "THỰC THI CHÍNH THỨC (EXECUTE)" : "KIỂM TRA & XÁC THỰC (DRY RUN)"));
                Log("==================================================================================================\n");

                // 1. Get Bed Rooms in Dept 57
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, cp);

                if (bList == null || bList.Count == 0)
                {
                    Log("ERROR: Không lấy được danh sách buồng bệnh khoa 57!");
                    return;
                }

                string[] roomKws = new string[] { "712", "714", "724", "725" };
                var targetRooms = new List<V_HIS_BED_ROOM>();
                foreach (var kw in roomKws)
                {
                    var rms = bList.Where(x => x.BED_ROOM_NAME != null && x.BED_ROOM_NAME.Contains(kw)).ToList();
                    targetRooms.AddRange(rms);
                }

                // Bind token to target rooms
                var workInfo = new WorkInfoSDO
                {
                    Rooms = targetRooms.Select(r => new RoomSDO { RoomId = r.ROOM_ID }).Concat(new[] { new RoomSDO { RoomId = 5248 } }).Distinct().ToList()
                };
                try { adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, cp); } catch { }

                // 2. Get In-Patients for these rooms
                var patients = new List<V_HIS_TREATMENT_BED_ROOM>();
                var roomMap = new Dictionary<long, long>(); // BedRoomId -> RoomId
                foreach (var rm in targetRooms)
                {
                    roomMap[rm.ID] = rm.ROOM_ID;
                    HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                    tbrf.BED_ROOM_ID = rm.ID;
                    tbrf.IS_IN_ROOM = true;
                    var pts = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, cp);
                    if (pts != null && pts.Count > 0)
                    {
                        patients.AddRange(pts);
                    }
                }

                Log(string.Format("Tìm thấy tổng cộng: {0} bệnh nhân đang nằm tại các buồng 712, 714, 724, 725.\n", patients.Count));

                // 3. Process each patient
                List<PatientPrescriptionPlan> plans = new List<PatientPrescriptionPlan>();

                foreach (var p in patients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    PatientPrescriptionPlan plan = new PatientPrescriptionPlan();
                    plan.BedPatient = p;
                    plan.RoomId = roomMap.ContainsKey(p.BED_ROOM_ID) ? roomMap[p.BED_ROOM_ID] : 5248;
                    plan.ExistingTomorrowMests = new List<string>();

                    // Treatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = p.TREATMENT_ID;
                    var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, cp);
                    plan.Treatment = trList != null && trList.Count > 0 ? trList[0] : null;

                    // Check ExpMest on 20260827 (tomorrow)
                    HisExpMestViewFilter emfTom = new HisExpMestViewFilter();
                    emfTom.TDL_TREATMENT_ID = p.TREATMENT_ID;
                    var emList = adapter.FetchList<V_HIS_EXP_MEST>("api/HisExpMest/GetView", mosConsumer, emfTom, cp);
                    if (emList != null && emList.Count > 0)
                    {
                        var tomMests = emList.Where(x => (x.TDL_INTRUCTION_TIME.HasValue && x.TDL_INTRUCTION_TIME.Value.ToString().StartsWith("20260827")) ||
                                                         (x.CREATE_TIME.HasValue && x.CREATE_TIME.Value.ToString().StartsWith("20260827"))).ToList();
                        if (tomMests.Count > 0)
                        {
                            plan.AlreadyHasTomorrowPrescription = true;
                            plan.ExistingTomorrowMests = tomMests.Select(x => string.Format("{0} ({1})", x.EXP_MEST_CODE, x.MEDI_STOCK_NAME)).ToList();
                        }
                    }

                    // Check ExpMestMedicines
                    HisExpMestMedicineViewFilter medFilter = new HisExpMestMedicineViewFilter();
                    medFilter.TDL_TREATMENT_ID = p.TREATMENT_ID;
                    var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, medFilter, cp);
                    if (allMeds != null && allMeds.Count > 0)
                    {
                        // Get latest instruction / creation date
                        var dates = allMeds.Select(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().Substring(0, 8)).Distinct().OrderByDescending(d => d).ToList();
                        // Exclude future date 20260827 if looking for source
                        var pastDates = dates.Where(d => string.Compare(d, "20260827") < 0).ToList();
                        string sourceDate = pastDates.FirstOrDefault();

                        if (!string.IsNullOrEmpty(sourceDate))
                        {
                            plan.SourceDate = sourceDate;
                            plan.SourceMeds = allMeds.Where(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().StartsWith(sourceDate)).ToList();

                            // Filter out 1-time emergency vaccines/serums like SAT if appropriate, or keep
                            var medsToCopy = plan.SourceMeds.Where(x => !x.MEDICINE_TYPE_NAME.ToLower().Contains("sat 1500")).ToList();

                            // Convert to TargetMeds for Central Pharmacy
                            plan.TargetMeds = new List<PresMedicineSDO>();
                            foreach (var sm in medsToCopy)
                            {
                                long targetStockId = sm.MEDI_STOCK_ID;
                                string unit = (sm.SERVICE_UNIT_NAME ?? "").Trim().ToLower();
                                string medName = (sm.MEDICINE_TYPE_NAME ?? "").Trim().ToLower();

                                // Central Pharmacy Stock Remapping:
                                if (medName.Contains("diazepam") || medName.Contains("seduxen") || medName.Contains("gardenal"))
                                {
                                    targetStockId = 4208; // Kho Hướng thần
                                }
                                else if (medName.Contains("clorid 0,9%") || medName.Contains("clorid 0.9%") || medName.Contains("nacl") || 
                                         medName.Contains("ringer") || medName.Contains("glucose") || medName.Contains("povidone") ||
                                         unit == "chai" || unit == "túi")
                                {
                                    targetStockId = 804; // Kho Dịch truyền
                                }
                                else if (unit == "viên" || unit == "gói" || unit == "viên nén" || unit == "viên nang" || unit == "ống uống" ||
                                         medName.Contains("amlor") || medName.Contains("cordaflex") || medName.Contains("lyrica") ||
                                         medName.Contains("celebrex") || medName.Contains("arcoxia") || medName.Contains("partamol") ||
                                         medName.Contains("briozcal") || medName.Contains("tramadol/paracetamol") || medName.Contains("nexium"))
                                {
                                    targetStockId = 4210; // Kho thuốc viên
                                }
                                else // Vials, injectable ampoules, syringes: Rocephin, Voxin, Meropenem, Zyvox, Gemapaxane, Paracetamol Kabi
                                {
                                    targetStockId = 4209; // Kho thuốc ống
                                }

                                // Calculate 1-day quantity
                                decimal m = 0, no = 0, af = 0, ev = 0;
                                decimal.TryParse(sm.MORNING, out m);
                                decimal.TryParse(sm.NOON, out no);
                                decimal.TryParse(sm.AFTERNOON, out af);
                                decimal.TryParse(sm.EVENING, out ev);
                                decimal dailyDose = m + no + af + ev;
                                decimal amount = (dailyDose > 0) ? dailyDose : sm.AMOUNT;

                                var pMed = new PresMedicineSDO
                                {
                                    MedicineTypeId = sm.MEDICINE_TYPE_ID,
                                    MediStockId = targetStockId,
                                    PatientTypeId = sm.PATIENT_TYPE_ID ?? 1,
                                    Amount = amount,
                                    Morning = sm.MORNING,
                                    Noon = sm.NOON,
                                    Afternoon = sm.AFTERNOON,
                                    Evening = sm.EVENING,
                                    Tutorial = sm.TUTORIAL,
                                    NumOfDays = 1
                                };
                                plan.TargetMeds.Add(pMed);
                            }
                        }
                    }

                    plans.Add(plan);
                }

                // 4. Print Summary & Execute if requested
                int index = 1;
                int countSuccess = 0;
                int countSkipped = 0;
                int countFailed = 0;

                long targetInstructionTime = 20260827080000;

                foreach (var plan in plans)
                {
                    var p = plan.BedPatient;
                    var tr = plan.Treatment;
                    Log("--------------------------------------------------------------------------------------------------");
                    Log(string.Format("{0}. BN: {1} | Buồng: {2} | Giường: {3} | Mã BN: {4} | Mã BA: {5} | TreatmentId: {6}",
                        index++, p.TDL_PATIENT_NAME, p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));

                    if (tr != null)
                    {
                        Log(string.Format("   Chẩn đoán: {0} [{1}] - {2}", tr.ICD_NAME, tr.ICD_CODE, tr.ICD_TEXT));
                    }

                    if (plan.AlreadyHasTomorrowPrescription)
                    {
                        Log(string.Format("   ⚠️ ĐÃ CÓ ĐƠN THUỐC NGÀY 27/08/2026: {0}", string.Join("; ", plan.ExistingTomorrowMests)));
                        Log("   -> BỎ QUA (Không kê trùng).");
                        plan.Status = "BỎ QUA (Đã có đơn)";
                        countSkipped++;
                        continue;
                    }

                    if (plan.TargetMeds == null || plan.TargetMeds.Count == 0)
                    {
                        Log("   ⚠️ KHÔNG TÌM THẤY ĐƠN THUỐC TRƯỚC ĐÓ ĐỂ SAO CHÉP.");
                        plan.Status = "BỎ QUA (Không có đơn trước)";
                        countSkipped++;
                        continue;
                    }

                    Log(string.Format("   Nguồn sao chép từ ngày: [{0}] ({1} khoản thuốc):", plan.SourceDate, plan.TargetMeds.Count));
                    foreach (var m in plan.TargetMeds)
                    {
                        var orig = plan.SourceMeds.FirstOrDefault(x => x.MEDICINE_TYPE_ID == m.MedicineTypeId);
                        string origStock = orig != null ? orig.MEDI_STOCK_NAME : "";
                        Log(string.Format("     • [{0}] (ID {1}) | SL: {2} {3} | Kho gốc: {4} ({5}) -> Kho đích: {6} | Cữ: S:{7} Tr:{8} C:{9} T:{10} | HDSD: {11}",
                            orig != null ? orig.MEDICINE_TYPE_NAME : m.MedicineTypeId.ToString(),
                            m.MedicineTypeId, m.Amount,
                            orig != null ? orig.SERVICE_UNIT_NAME : "",
                            origStock, orig != null ? orig.MEDI_STOCK_ID.ToString() : "",
                            m.MediStockId,
                            m.Morning, m.Noon, m.Afternoon, m.Evening,
                            m.Tutorial));
                    }

                    if (doExecute)
                    {
                        var sdo = new InPatientPresSDO
                        {
                            TreatmentId = p.TREATMENT_ID,
                            RequestRoomId = plan.RoomId > 0 ? plan.RoomId : 5248,
                            RequestLoginName = "034727",
                            RequestUserName = "NGUYỄN HỮU SÂM",
                            IcdCode = tr != null ? tr.ICD_CODE : "M51.1",
                            IcdName = tr != null ? tr.ICD_NAME : "Thoát vị đĩa đệm",
                            IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                            IcdText = tr != null ? tr.ICD_TEXT : "",
                            PrescriptionTypeId = (PrescriptionType)1,
                            InstructionTimes = new List<long> { targetInstructionTime },
                            Medicines = plan.TargetMeds
                        };

                        CommonParam presParam = new CommonParam();
                        var res = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdo, presParam);

                        if (res != null && res.ExpMests != null && res.ExpMests.Count > 0)
                        {
                            string createdMests = string.Join(", ", res.ExpMests.Select(x => string.Format("{0} (Kho {1})", x.EXP_MEST_CODE, x.MEDI_STOCK_ID)));
                            Log(string.Format("   ✅ KÊ ĐƠN THÀNH CÔNG: Phiếu xuất=[{0}]", createdMests));
                            plan.Status = "THÀNH CÔNG";
                            plan.ResultDetails = createdMests;
                            countSuccess++;
                        }
                        else
                        {
                            string bug = presParam.GetBugCode();
                            string msg = presParam.GetMessage();
                            string detail = (presParam.Messages != null && presParam.Messages.Count > 0) ? string.Join("; ", presParam.Messages) : msg;
                            Log(string.Format("   ❌ KÊ ĐƠN THẤT BẠI! BugCode={0}, Msg={1}, Detail={2}", bug, msg, detail));
                            plan.Status = "THẤT BẠI";
                            plan.ResultDetails = detail;
                            countFailed++;
                        }
                    }
                    else
                    {
                        Log("   [DRY RUN] Đã chuẩn bị dữ liệu (thêm cờ --execute để thực thi)");
                        plan.Status = "CHỜ THỰC THI (DRY RUN)";
                    }
                }

                Log("\n==================================================================================================");
                Log(string.Format("TỔNG KẾT: Tổng số BN={0} | Thành công={1} | Bỏ qua (Đã có đơn/Không có đơn)={2} | Thất bại={3}",
                    plans.Count, countSuccess, countSkipped, countFailed));
                Log("==================================================================================================");
            }
            catch (Exception ex)
            {
                Log("EXCEPTION: " + ex.ToString());
            }
            finally
            {
                log.Close();
            }
        }
    }
}
