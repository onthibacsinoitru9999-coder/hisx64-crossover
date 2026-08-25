using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine(baseDir, shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        Run();
    }

    class Config
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string IcdCode { get; set; }
        public string IcdName { get; set; }
        public List<PresMedicineSDO> Meds { get; set; }
    }

    static void Run()
    {
        string logFile = @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
        string token = "";
        using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64)
                    token = line.Substring(idx + 10, 64);
            }
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        var list = new List<Config>
        {
            new Config
            {
                Id = 7082063,
                Name = "NGUYỄN THỊ NGUYỆT",
                Code = "000007082247",
                IcdCode = "M75.1",
                IcdName = "Theo dõi nhiễm trùng sau mô nội soi khâu chóp xoay vai phải",
                Meds = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                }
            },
            new Config
            {
                Id = 7060265,
                Name = "LÊ VĂN CHIẾN",
                Code = "000007060449",
                IcdCode = "M47.00†",
                IcdName = "Viêm đốt sống đĩa đệm T4, T5 áp xe ngoài màng cứng, AIHB/ THA, ĐTĐ typ 2",
                Meds = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 26573, MediStockId = 4210, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Uống sáng 1v, chiều 1v sau ăn", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                }
            }
        };

        long[] dates = new long[] { 20260824080000, 20260825080000 };

        foreach (var c in list)
        {
            Console.WriteLine(string.Format("Processing: {0} ({1})", c.Name, c.Code));
            foreach (var d in dates)
            {
                var sdo = new InPatientPresSDO
                {
                    TreatmentId = c.Id,
                    RequestRoomId = 5257, // Phòng 724
                    RequestLoginName = "034727",
                    RequestUserName = "NGUYỄN HỮU SÂM",
                    IcdCode = c.IcdCode,
                    IcdName = c.IcdName,
                    PrescriptionTypeId = (PrescriptionType)1,
                    InstructionTimes = new List<long> { d },
                    Medicines = c.Meds
                };

                var cp = new CommonParam();
                var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);
                bool ok = res != null && res.ExpMests != null && res.ExpMests.Count > 0;
                string mests = (res != null && res.ExpMests != null) ? string.Join(", ", res.ExpMests.Select(x => x.EXP_MEST_CODE)) : "";
                Console.WriteLine(string.Format("  Date {0}: Success={1} | ExpMests=[{2}] | Bug={3}, Msg={4}", d, ok, mests, cp.GetBugCode(), cp.GetMessage()));
            }
        }
    }
}
