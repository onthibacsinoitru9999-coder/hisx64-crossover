using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

class Program
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = e.Name.Split(',')[0];
            string[] paths = new string[]
            {
                Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies", shortName + ".dll"),
                Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module", shortName + ".dll"),
                Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll")
            };
            foreach (var p in paths) if (File.Exists(p)) return Assembly.LoadFrom(p);
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
            string text = reader.ReadToEnd();
            var matches = Regex.Matches(text, @"TokenCode\|([a-f0-9]{64})");
            if (matches.Count > 0)
                token = matches[matches.Count - 1].Groups[1].Value;
        }

        Console.WriteLine("Token: " + token);
        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        // Let's test querying HisTreatmentBedRoom
        try
        {
            var cp = new CommonParam();
            var brFilter = new HisTreatmentBedRoomViewFilter
            {
                IS_IN_ROOM = true
            };
            var activeBedRooms = consumer.Get<List<V_HIS_TREATMENT_BED_ROOM>>("api/HisTreatmentBedRoom/GetView", cp, brFilter, new object[0]);
            Console.WriteLine("ActiveBedRooms count: " + (activeBedRooms != null ? activeBedRooms.Count : 0));
            if (activeBedRooms != null)
            {
                var k57 = activeBedRooms.Where(x => x.BED_ROOM_CODE != null && x.BED_ROOM_CODE.StartsWith("NQCTCH")).ToList();
                Console.WriteLine("Khoa 57 active bed rooms count: " + k57.Count);
                foreach (var b in k57)
                {
                    Console.WriteLine(string.Format("BN: {0} ({1}) | TrID: {2} | Buồng: {3} | Giường: {4} | Giờ vào buồng: {5}",
                        b.TDL_PATIENT_NAME, b.TDL_PATIENT_CODE, b.TREATMENT_ID, b.BED_ROOM_NAME, b.BED_NAME, b.ADD_TIME));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error HisTreatmentBedRoom: " + ex.ToString());
            if (ex is ApiException)
            {
                var apiEx = (ApiException)ex;
                Console.WriteLine("ApiException Message: " + apiEx.Message);
            }
        }

        // Test querying HisDepartmentTran with treatment IDs
        try
        {
            var cp = new CommonParam();
            var dtFilter = new HisDepartmentTranViewFilter
            {
                DEPARTMENT_ID = 57
            };
            var deptTrans = consumer.Get<List<V_HIS_DEPARTMENT_TRAN>>("api/HisDepartmentTran/GetView", cp, dtFilter, new object[0]);
            Console.WriteLine("\nDeptTrans (DEPARTMENT_ID = 57) count: " + (deptTrans != null ? deptTrans.Count : 0));
            if (deptTrans != null)
            {
                var today = deptTrans.Where(x => x.DEPARTMENT_IN_TIME.ToString().StartsWith("20260907")).ToList();
                Console.WriteLine("DeptTrans today (20260907) count: " + today.Count);
                foreach (var dt in today)
                {
                    Console.WriteLine(string.Format("- BN: {0} ({1}) | Mã ĐT: {2} | Vào lúc: {3} | Từ khoa: {4} | Chẩn đoán: {5}",
                        dt.TDL_PATIENT_NAME, dt.TDL_PATIENT_CODE, dt.TREATMENT_CODE, dt.DEPARTMENT_IN_TIME, dt.PREVIOUS_DEPARTMENT_NAME, dt.ICD_NAME));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error HisDepartmentTran: " + ex.ToString());
        }
    }
}