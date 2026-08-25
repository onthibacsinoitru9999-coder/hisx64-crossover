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
            TreatmentId = 7060265,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "M47.00\u2020", // M47.00†
            IcdName = "Viêm đốt sống đĩa đệm T4, T5 áp xe ngoài màng cứng, AIS(A)/ TD viêm phổi - Đái tháo đường - VGC",
            PrescriptionTypeId = (MOS.SDO.PrescriptionType)1,
            InstructionTimes = new List<long> { 20260825080000 },
            Medicines = new List<MOS.SDO.PresMedicineSDO>
            {
                new MOS.SDO.PresMedicineSDO
                {
                    MedicineTypeId = 23647, // Rocephin 1g I.V.
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "02",
                    Tutorial = "Pha 2 lọ với 100ml NaCl 0.9%, truyền TM 30 giọt/phút lúc 9h sáng",
                    NumOfDays = 1
                },
                new MOS.SDO.PresMedicineSDO
                {
                    MedicineTypeId = 25107, // Sodium Chloride 0.9% 100ml
                    MediStockId = 804,
                    PatientTypeId = 1,
                    Amount = 1.0m,
                    Morning = "01",
                    Tutorial = "Dung môi pha Rocephin truyền TM lúc 9h sáng",
                    NumOfDays = 1
                },
                new MOS.SDO.PresMedicineSDO
                {
                    MedicineTypeId = 25683, // Paracetamol Kabi 1g/100ml
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "01",
                    Evening = "01",
                    Tutorial = "Truyền TM 30 giọt/phút lúc 10h - 18h khi đau/sốt",
                    NumOfDays = 1
                }
            }
        };

        var cp = new Inventec.Core.CommonParam();
        var res = mosConsumer.Post<MOS.SDO.InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);
        bool ok = (res != null && res.ExpMests != null && res.ExpMests.Count > 0);
        Console.WriteLine("Create Result: " + ok + " | Count: " + (res != null && res.ExpMests != null ? res.ExpMests.Count : 0));
        Console.WriteLine("HasException: " + cp.HasException);
        Console.WriteLine("BugCodes: " + string.Join(", ", cp.BugCodes ?? new List<string>()));
        Console.WriteLine("Messages: " + string.Join(", ", cp.Messages ?? new List<string>()));
    }
}
