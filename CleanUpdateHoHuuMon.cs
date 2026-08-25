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

        // 1. Delete old SereServs in 88163250 and 88163251
        long[] oldReqIds = new long[] { 88163250, 88163251 };
        foreach (var reqId in oldReqIds)
        {
            var cpSere = new CommonParam();
            var ss = mosConsumer.Get<List<HIS_SERE_SERV>>("api/HisSereServ/Get", cpSere, new { SERVICE_REQ_ID = reqId, IS_INCLUDE_DELETED = false }, new object[0]);
            if (ss != null)
            {
                foreach (var s in ss)
                {
                    var cpDelSere = new CommonParam();
                    bool delSere = mosConsumer.Post<bool>("api/HisSereServ/ExamDelete", cpDelSere, new HIS_SERE_SERV { ID = s.ID }, new object[0]);
                    Console.WriteLine("ExamDelete SereServ " + s.ID + " (" + s.TDL_SERVICE_NAME + "): " + delSere);
                }
            }
            var cpDelReq = new CommonParam();
            bool delReq = mosConsumer.Post<bool>("api/HisServiceReq/Delete", cpDelReq, new HisServiceReqSDO { Id = reqId, RequestRoomId = 5252 }, new object[0]);
            Console.WriteLine("Delete Req " + reqId + ": " + delReq);
        }

        // 2. Create updated prescription for Ho Huu Mon (7092075) on 25/08/2026
        var sdo = new InPatientPresSDO
        {
            TreatmentId = 7092075,
            RequestRoomId = 5252,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "S91.0",
            IcdName = "Đứt gân Achilles chân trái",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { 20260825080000 },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = 23646, // Medivernol 1g
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "02",
                    Tutorial = "Pha 02 lọ với 100ml NaCl 0.9%, truyền TM 30-40 giọt/phút lúc 9h sáng",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 25107, // Sodium Chloride 0.9% 100ml
                    MediStockId = 804,
                    PatientTypeId = 1,
                    Amount = 1.0m,
                    Morning = "01",
                    Tutorial = "Dung môi pha Medivernol truyền TM sáng 9h",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 25683, // Paracetamol Kabi 1g/100ml
                    MediStockId = 4209,
                    PatientTypeId = 1,
                    Amount = 2.0m,
                    Morning = "01",
                    Afternoon = "01",
                    Tutorial = "Truyền TM 30 giọt/phút lúc 10h - 18h khi đau/sốt",
                    NumOfDays = 1
                }
            }
        };

        var cpCreate = new CommonParam();
        var res = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", cpCreate, sdo, new object[0]);
        bool ok = res != null && res.ExpMests != null && res.ExpMests.Count > 0;
        Console.WriteLine("Ho Huu Mon Re-create Success: " + ok);
        if (res != null && res.ExpMests != null)
        {
            foreach (var m in res.ExpMests)
                Console.WriteLine(" -> ExpMest: " + m.EXP_MEST_CODE + " (Stock: " + m.MEDI_STOCK_ID + ")");
        }
    }
}
