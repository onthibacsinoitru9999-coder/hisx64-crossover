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

        // Test update ServiceReqMeties for Ho Huu Mon (ServiceReqId: 88163250)
        var sdo = new InPatientPresSDO
        {
            Id = 88163250,
            TreatmentId = 7092075,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "S91.0",
            IcdName = "Đứt gân Achilles chân trái",
            IcdSubCode = ";M10.00",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260825080000 },
            ServiceReqMeties = new List<PresOutStockMetySDO>
            {
                new PresOutStockMetySDO
                {
                    MedicineTypeId = 23646, // Medivernol 1g
                    Amount = 2.0m,
                    Morning = "02",
                    Tutorial = "Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30-40 giọt/phút lúc 9h sáng",
                    InstructionTimes = new List<long> { 20260825080000 }
                },
                new PresOutStockMetySDO
                {
                    MedicineTypeId = 25683, // Paracetamol Kabi 1g/100ml
                    Amount = 2.0m,
                    Morning = "01",
                    Afternoon = "01",
                    Tutorial = "Truyền TM 30 giọt/phút lúc 10h - 18h khi đau/sốt",
                    InstructionTimes = new List<long> { 20260825080000 }
                }
            }
        };

        var cp = new CommonParam();
        var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresUpdate", cp, sdo, new object[0]);
        Console.WriteLine("Update Result Success: " + (res != null));
        Console.WriteLine("HasException: " + cp.HasException);
        Console.WriteLine("BugCodes: " + string.Join(", ", cp.BugCodes ?? new List<string>()));
        Console.WriteLine("Messages: " + string.Join(", ", cp.Messages ?? new List<string>()));
    }
}
