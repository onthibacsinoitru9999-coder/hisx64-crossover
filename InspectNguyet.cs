using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.SDO;
using Newtonsoft.Json;

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

        // Test single medicine for Nguy?t
        var sdo = new InPatientPresSDO
        {
            TreatmentId = 7082063,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUY?N H?U SÂM",
            IcdCode = "M75.1",
            IcdName = "Theo dõi nhi?m trùng sau mô n?i soi khâu chóp xoay vai P",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260824080000 },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truy?n TM ch?m sáng 1 l?, chi?u 1 l?", NumOfDays = 1 }
            }
        };

        var cp = new CommonParam();
        var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);

        Console.WriteLine("Raw SDO serialized: " + JsonConvert.SerializeObject(sdo));
        Console.WriteLine("Result object: " + JsonConvert.SerializeObject(res));
        Console.WriteLine("CommonParam: " + JsonConvert.SerializeObject(cp));
    }
}
