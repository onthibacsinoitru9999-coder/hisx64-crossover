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

        long[] dates = new long[] { 20260825080000, 20260826080000 };

        foreach (var d in dates)
        {
            var sdo = new InPatientPresSDO
            {
                TreatmentId = 6962983, // QUÁCH MINH THÀNH
                RequestRoomId = 5252,  // Khoa CTCH - Phòng 712
                RequestLoginName = "034727",
                RequestUserName = "NGUYỄN HỮU SÂM",
                IcdCode = "S82.81",
                IcdName = "Gãy hở độ IIIA 1/3D hai xương cẳng chân phải",
                PrescriptionTypeId = (PrescriptionType)1,
                InstructionTimes = new List<long> { d },
                Medicines = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO
                    {
                        MedicineTypeId = 24858,
                        MediStockId = 4209,
                        PatientTypeId = 1,
                        Amount = 1.0m,
                        Morning = "01",
                        Tutorial = "Ngày truyền tĩnh mạch 1 chai buổi sáng 9h 30/p",
                        NumOfDays = 1
                    },
                    new PresMedicineSDO
                    {
                        MedicineTypeId = 14366,
                        MediStockId = 4209,
                        PatientTypeId = 1,
                        Amount = 2.0m,
                        Morning = "02",
                        Tutorial = "Ngày tiêm 2 lọ buổi sáng, pha với 100ml nacl 0,9%, truyền tm xxx g/p, 9h",
                        NumOfDays = 1
                    },
                    new PresMedicineSDO
                    {
                        MedicineTypeId = 25107,
                        MediStockId = 804,
                        PatientTypeId = 1,
                        Amount = 1.0m,
                        Noon = "01",
                        Tutorial = "pha ks",
                        NumOfDays = 1
                    },
                    new PresMedicineSDO
                    {
                        MedicineTypeId = 25683,
                        MediStockId = 4209,
                        PatientTypeId = 1,
                        Amount = 2.0m,
                        Morning = "01",
                        Evening = "01",
                        Tutorial = "truyền tĩnh mạch 30g/p lúc 10h-20h hoặc khi đau cách 4-6h",
                        NumOfDays = 1
                    }
                }
            };

            var cp = new CommonParam();
            var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cp, sdo, new object[0]);
            bool ok = res != null && res.ExpMests != null && res.ExpMests.Count > 0;
            string mests = ok ? string.Join(", ", res.ExpMests.Select(x => x.EXP_MEST_CODE)) : "";
            Console.WriteLine(string.Format("Date {0}: Success={1} | ExpMests=[{2}] | Bug={3}, Msg={4}", d, ok, mests, cp.GetBugCode(), cp.GetMessage()));
        }
    }
}
