using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.SDO;

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

        // 1. Kê Nguyệt 25/08 (Dùng đúng cấu trúc y hệt DebugPatient6 đã thành công 24/08)
        var sdoN25 = new InPatientPresSDO
        {
            TreatmentId = 7082063,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "M75.1",
            IcdName = "Theo dõi nhiễm trùng sau mô nội soi khâu chóp xoay vai P",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260825080000 },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
            }
        };

        var cpN = new CommonParam();
        var resN = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cpN, sdoN25, new object[0]);
        Console.WriteLine("Nguyệt 25/08 Result: " + (resN != null && resN.ExpMests != null));

        // 2. Kê Chiến 24/08 và 25/08
        long[] dates = new long[] { 20260824080000, 20260825080000 };
        foreach (var d in dates)
        {
            var sdoC = new InPatientPresSDO
            {
                TreatmentId = 7060265,
                RequestRoomId = 5252,
                RequestLoginName = "034727",
                RequestUserName = "NGUYỄN HỮU SÂM",
                IcdCode = "M47.00†",
                IcdName = "Viêm đốt sống đĩa đệm T4, T5 áp xe ngoài màng cứng, AIHB/ THA, ĐTĐ typ 2",
                PrescriptionTypeId = (PrescriptionType)1,
                InstructionTimes = new List<long> { d },
                Medicines = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 26573, MediStockId = 4210, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Uống sáng 1v, chiều 1v sau ăn", NumOfDays = 1 },
                    new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                }
            };
            var cpC = new CommonParam();
            var resC = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cpC, sdoC, new object[0]);
            Console.WriteLine(string.Format("Chiến {0} Result: {1}", d, (resC != null && resC.ExpMests != null)));
        }
    }
}
