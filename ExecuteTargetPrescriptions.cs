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

namespace TargetPrescriptions
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

        class PatientConfig
        {
            public long TreatmentId { get; set; }
            public string PatientName { get; set; }
            public string RoomName { get; set; }
            public long RoomId { get; set; }
            public List<PresMedicineSDO> Meds { get; set; }
        }

        static void Run(bool doExecute)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = GetLiveToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("LỖI: Không tìm thấy Token!");
                return;
            }

            ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            MyAdapter adapter = new MyAdapter();
            CommonParam cp = new CommonParam();

            // Set room workinfo (5248: P734, 5257: P712, 5259: P714)
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
                var wpRes = adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, workInfo, cp);
                if (wpRes != null)
                {
                    Console.WriteLine(string.Format("✔ Kích hoạt WorkInfo thành công ({0} phòng làm việc Khoa 57).", wpRes.Count));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cảnh báo kích hoạt WorkInfo: " + ex.Message);
            }

            // Config list of patients and their maintenance meds
            var patients = new List<PatientConfig>
            {
                // 1. Vũ Đức Bình
                new PatientConfig
                {
                    TreatmentId = 7163469,
                    PatientName = "VŨ ĐỨC BÌNH",
                    RoomName = "P714",
                    RoomId = 5259,
                    Meds = new List<PresMedicineSDO>
                    {
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 15474, // Rocephin 1g I.V.
                            MediStockId = 4209, // Kho thuốc ống
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "02",
                            Tutorial = "Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 11h",
                            NumOfDays = 1
                        },
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 25107, // Sodium Chloride 0,9% 100ml
                            MediStockId = 804, // Kho Dịch truyền 2024
                            PatientTypeId = 1,
                            Amount = 1.0m,
                            Morning = "01",
                            Tutorial = "Dung môi pha Rocephin truyền TM sáng 11h",
                            NumOfDays = 1
                        },
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 25683, // Paracetamol Kabi AD 1g/100ml
                            MediStockId = 4209, // Kho thuốc ống
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "01",
                            Evening = "01",
                            Tutorial = "Truyền TM 40 giọt/phút lúc 10h - 20h khi đau",
                            NumOfDays = 1
                        },
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 24892, // Mobic 7,5mg
                            MediStockId = 4210, // Kho thuốc viên
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "01",
                            Evening = "01",
                            Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên, 11h-19h",
                            NumOfDays = 1
                        }
                    }
                },
                // 2. Nguyễn Văn Thơ
                new PatientConfig
                {
                    TreatmentId = 7163700,
                    PatientName = "NGUYỄN VĂN THƠ",
                    RoomName = "P714",
                    RoomId = 5259,
                    Meds = new List<PresMedicineSDO>
                    {
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 23554, // Tramadol/Paracetamol Normon 37,5mg/325mg
                            MediStockId = 4210, // Kho thuốc viên
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "01",
                            Evening = "01",
                            Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên, 10h - 19h, sau ăn",
                            NumOfDays = 1
                        }
                    }
                },
                // 3. Vi Trung Hiếu
                new PatientConfig
                {
                    TreatmentId = 7163879,
                    PatientName = "VI TRUNG HIẾU",
                    RoomName = "P717 (HIS 712)",
                    RoomId = 5257,
                    Meds = new List<PresMedicineSDO>
                    {
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 23554, // Tramadol/Paracetamol Normon 37,5mg/325mg
                            MediStockId = 4210, // Kho thuốc viên
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "01",
                            Evening = "01",
                            Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên, 9h - 19h, sau ăn",
                            NumOfDays = 1
                        }
                    }
                },
                // 4. Nguyễn Xuân Chín
                new PatientConfig
                {
                    TreatmentId = 7136089,
                    PatientName = "NGUYỄN XUÂN CHÍN",
                    RoomName = "P717 (HIS 712)",
                    RoomId = 5257,
                    Meds = new List<PresMedicineSDO>() // Đã có đơn, giữ rỗng để skip
                },
                // 5. Nguyễn Văn Trọng
                new PatientConfig
                {
                    TreatmentId = 7149475,
                    PatientName = "NGUYỄN VĂN TRỌNG",
                    RoomName = "P717 (HIS 712)",
                    RoomId = 5257,
                    Meds = new List<PresMedicineSDO>
                    {
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 15474, // Rocephin 1g I.V.
                            MediStockId = 4209, // Kho thuốc ống
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "02",
                            Tutorial = "Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 11h",
                            NumOfDays = 1
                        },
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 25107, // Sodium Chloride 0,9% 100ml
                            MediStockId = 804, // Kho Dịch truyền 2024
                            PatientTypeId = 1,
                            Amount = 1.0m,
                            Morning = "01",
                            Tutorial = "Dung môi pha Rocephin truyền TM sáng 11h",
                            NumOfDays = 1
                        },
                        new PresMedicineSDO
                        {
                            MedicineTypeId = 23554, // Tramadol/Paracetamol Normon 37,5mg/325mg
                            MediStockId = 4210, // Kho thuốc viên
                            PatientTypeId = 1,
                            Amount = 2.0m,
                            Morning = "01",
                            Evening = "01",
                            Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên, 9h - 19h, sau ăn",
                            NumOfDays = 1
                        }
                    }
                }
            };

            long[] targetDates = new long[] { 20260907080000, 20260908080000 };

            Console.WriteLine("==================================================================================");
            Console.WriteLine("  HỆ THỐNG KÊ ĐƠN THUỐC DUY TRÌ TỪ KHO DƯỢC (4209, 4210, 804)");
            Console.WriteLine("  NGÀY: 07/09/2026 VÀ 08/09/2026 | BỆNH NHÂN P714 & P717 (KHOA 57)");
            Console.WriteLine("  CHẾ ĐỘ: " + (doExecute ? ">>> THỰC THI CHÍNH THỨC (EXECUTE) <<<" : "KIỂM TRA (DRY RUN)"));
            Console.WriteLine("==================================================================================\n");

            foreach (var d in targetDates)
            {
                string dateStr = d.ToString().Substring(0, 8);
                string displayDate = string.Format("{0}/{1}/{2}", dateStr.Substring(6, 2), dateStr.Substring(4, 2), dateStr.Substring(0, 4));

                Console.WriteLine(string.Format("**********************************************************************************"));
                Console.WriteLine(string.Format("  XỬ LÝ NGÀY: {0} ({1})", displayDate, d));
                Console.WriteLine(string.Format("**********************************************************************************"));

                foreach (var pat in patients)
                {
                    Console.WriteLine(string.Format("\n--- [{0}] {1} (TrID: {2}) | Buồng: {3} ---", pat.RoomName, pat.PatientName, pat.TreatmentId, pat.RoomId));

                    // 1. Check existing exp mests on this date
                    HisExpMestViewFilter ef = new HisExpMestViewFilter { TDL_TREATMENT_ID = pat.TreatmentId };
                    var mests = adapter.FetchList<V_HIS_EXP_MEST>("api/HisExpMest/GetView", mos, ef, cp);
                    var existOnDate = mests != null ? mests.Where(m => (m.TDL_INTRUCTION_TIME ?? 0).ToString().StartsWith(dateStr)).ToList() : new List<V_HIS_EXP_MEST>();

                    if (existOnDate.Count > 0)
                    {
                        Console.WriteLine(string.Format("  ⚠️ ĐÃ CÓ {0} ĐƠN THUỐC TRÊN HỆ THỐNG NGÀY {1}:", existOnDate.Count, displayDate));
                        foreach (var m in existOnDate)
                        {
                            Console.WriteLine(string.Format("     + {0} | Kho: {1} ({2})", m.EXP_MEST_CODE, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID));
                        }
                        Console.WriteLine("  -> BỎ QUA (Không kê trùng).");
                        continue;
                    }

                    if (pat.Meds == null || pat.Meds.Count == 0)
                    {
                        Console.WriteLine("  ⚠️ Không có cấu hình thuốc cho BN này (đã bỏ qua).");
                        continue;
                    }

                    // 2. Fetch treatment ICD info
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = pat.TreatmentId };
                    var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                    var tr = trList != null && trList.Count > 0 ? trList[0] : null;

                    Console.WriteLine(string.Format("  Chẩn đoán: [{0}] {1} - {2}", tr != null ? tr.ICD_CODE : "", tr != null ? tr.ICD_NAME : "", tr != null ? tr.ICD_TEXT : ""));
                    Console.WriteLine(string.Format("  Danh sách thuốc ({0} khoản):", pat.Meds.Count));
                    foreach (var m in pat.Meds)
                    {
                        Console.WriteLine(string.Format("    • ID: {0} | SL: {1} | Kho: {2} | S:{3} Tr:{4} C:{5} T:{6} | HD: {7}",
                            m.MedicineTypeId, m.Amount, m.MediStockId, m.Morning, m.Noon, m.Afternoon, m.Evening, m.Tutorial));
                    }

                    if (doExecute)
                    {
                        var sdo = new InPatientPresSDO
                        {
                            TreatmentId = pat.TreatmentId,
                            RequestRoomId = pat.RoomId,
                            RequestLoginName = "034727",
                            RequestUserName = "NGUYỄN HỮU SÂM",
                            IcdCode = tr != null ? tr.ICD_CODE : "",
                            IcdName = tr != null ? tr.ICD_NAME : "",
                            IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                            IcdText = tr != null ? tr.ICD_TEXT : "",
                            PrescriptionTypeId = (PrescriptionType)1,
                            InstructionTimes = new List<long> { d },
                            Medicines = pat.Meds
                        };

                        CommonParam presParam = new CommonParam();
                        var res = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mos, sdo, presParam);

                        if (res != null && res.ExpMests != null && res.ExpMests.Count > 0)
                        {
                            string createdMests = string.Join(", ", res.ExpMests.Select(x => string.Format("{0} (Kho {1})", x.EXP_MEST_CODE, x.MEDI_STOCK_ID)));
                            Console.WriteLine(string.Format("  ✅ KÊ ĐƠN THÀNH CÔNG! Phiếu xuất: [{0}]", createdMests));
                        }
                        else
                        {
                            string bug = presParam.GetBugCode();
                            string msg = presParam.GetMessage();
                            string detail = (presParam.Messages != null && presParam.Messages.Count > 0) ? string.Join("; ", presParam.Messages) : msg;
                            Console.WriteLine(string.Format("  ❌ KÊ ĐƠN THẤT BẠI! BugCode={0}, Msg={1}, Detail={2}", bug, msg, detail));
                        }
                    }
                    else
                    {
                        Console.WriteLine("  [DRY RUN] Đã chuẩn bị đầy đủ payload. Thêm --execute để thực thi.");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}