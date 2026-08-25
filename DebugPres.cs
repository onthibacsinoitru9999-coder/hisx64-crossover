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

        // 1. Get token
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

        Console.WriteLine("Token: " + token);
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var commonParam = new CommonParam();

        var sdo = new InPatientPresSDO
        {
            TreatmentId = 7092075,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "S91.0",
            IcdName = "Đứt gân Achilles chân trái",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260824080000 },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO {
                    MedicineTypeId = 23646,
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "1",
                    Afternoon = "1",
                    Tutorial = "Tiêm TM sáng 1 chiều 1",
                    NumOfDays = 1
                }
            }
        };

        var result = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", commonParam, sdo, new object[0]);

        Console.WriteLine("Result: " + (result != null));
        if (result != null)
        {
            if (result.ServiceReqs != null)
            {
                foreach (var req in result.ServiceReqs)
                {
                    Console.WriteLine("  ServiceReq Code: " + req.SERVICE_REQ_CODE + ", ID: " + req.ID);
                }
            }
            if (result.Medicines != null)
            {
                Console.WriteLine("  Total Medicines Created: " + result.Medicines.Count);
                foreach (var m in result.Medicines)
                {
                    Console.WriteLine("    Med: " + m.TDL_MEDICINE_TYPE_NAME + ", Amount: " + m.AMOUNT + ", Stock: " + m.TDL_MEDI_STOCK_ID);
                }
            }
            if (result.ExpMests != null)
            {
                Console.WriteLine("  Total ExpMests: " + result.ExpMests.Count);
                foreach (var em in result.ExpMests)
                {
                    Console.WriteLine("    ExpMest Code: " + em.EXP_MEST_CODE + ", Type: " + em.EXP_MEST_TYPE_ID + ", Stock: " + em.MEDI_STOCK_ID);
                }
            }
        }
        Console.WriteLine("BugCodes: " + string.Join(", ", commonParam.BugCodes));
        Console.WriteLine("Messages: " + string.Join(", ", commonParam.Messages));
    }
}
