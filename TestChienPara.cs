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

        // Test Chien with Para Kabi
        var sdo = new InPatientPresSDO
        {
            TreatmentId = 7060265,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "M47.0",
            IcdName = "Viêm đốt sống đĩa đệm",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260824080000 },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO {
                    MedicineTypeId = 25683,
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "1",
                    Afternoon = "1",
                    Tutorial = "Truyền TM 2 chai/ngày",
                    NumOfDays = 1
                }
            }
        };

        var cp = new CommonParam();
        var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);
        bool ok = res != null && res.ExpMests != null && res.ExpMests.Count > 0;
        Console.WriteLine(string.Format("Chien Para Test: Success={0}, Bug={1}, Msg={2}", ok, cp.GetBugCode(), cp.GetMessage()));
        if (cp.Messages != null) Console.WriteLine("Messages: " + string.Join(" | ", cp.Messages));
        if (cp.BugCodes != null) Console.WriteLine("BugCodes: " + string.Join(" | ", cp.BugCodes));
    }
}
