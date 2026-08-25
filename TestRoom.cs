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

        long[] roomIds = new long[] { 780, 5252, 5809, 786, 787 }; // 780 is Phong 724

        foreach (var rId in roomIds)
        {
            var sdo = new InPatientPresSDO
            {
                TreatmentId = 7082063, // Nguy?t
                RequestRoomId = rId,
                RequestLoginName = "034727",
                RequestUserName = "NGUY?N H?U SÂM",
                IcdCode = "M75.1",
                IcdName = "Theo dõi nhi?m trùng sau mô n?i soi khâu chóp xoay vai P",
                PrescriptionTypeId = (PrescriptionType)1,
                InstructionTimes = new List<long> { 20260824080000 },
                Medicines = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm TM sáng 1 chi?u 1", NumOfDays = 1 }
                }
            };

            var cp = new CommonParam();
            var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);
            bool ok = res != null && res.ExpMests != null && res.ExpMests.Count > 0;
            Console.WriteLine(string.Format("RequestRoomId {0}: Success={1}, Bug={2}, Msg={3}", rId, ok, cp.GetBugCode(), cp.GetMessage()));
        }
    }
}
