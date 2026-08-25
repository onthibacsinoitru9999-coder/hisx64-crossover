using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

        var mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        var sdo = new MOS.SDO.InPatientPresSDO
        {
            Id = 88163250,
            TreatmentId = 7092075,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUY?N H?U SÂM",
            IcdCode = "S91.0",
            IcdName = "Ð?t gân Achilles chân trái",
            IcdSubCode = ";M10.00",
            PrescriptionTypeId = (MOS.SDO.PrescriptionType)1,
            InstructionTimes = new List<long> { 20260825080000 },
            Medicines = new List<MOS.SDO.PresMedicineSDO>
            {
                new MOS.SDO.PresMedicineSDO
                {
                    MedicineTypeId = 23646, // Medivernol 1g
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "02",
                    Tutorial = "Pha 02 l? v?i 100ml NaCl 0.9%, truy?n TM 30-40 gi?t/phút lúc 9h sáng",
                    NumOfDays = 1
                },
                new MOS.SDO.PresMedicineSDO
                {
                    MedicineTypeId = 25683, // Paracetamol Kabi 1g/100ml
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "01",
                    Afternoon = "01",
                    Tutorial = "Truy?n TM 30 gi?t/phút lúc 10h - 18h khi dau/s?t",
                    NumOfDays = 1
                }
            }
        };

        var cp = new Inventec.Core.CommonParam();
        var res = mosConsumer.Post<MOS.SDO.InPatientPresResultSDO>("api/HisServiceReq/InPatientPresUpdate", cp, sdo, new object[0]);
        Console.WriteLine("res is null: " + (res == null));
        if (res != null)
        {
            Console.WriteLine("res.ExpMests count: " + (res.ExpMests != null ? res.ExpMests.Count : -1));
            Console.WriteLine("res.Medicines count: " + (res.Medicines != null ? res.Medicines.Count : -1));
            Console.WriteLine("res.ServiceReqs count: " + (res.ServiceReqs != null ? res.ServiceReqs.Count : -1));
        }
        Console.WriteLine("HasException: " + cp.HasException);
        Console.WriteLine("BugCodes: " + string.Join(", ", cp.BugCodes ?? new List<string>()));
        Console.WriteLine("Messages: " + string.Join(", ", cp.Messages ?? new List<string>()));
    }
}
