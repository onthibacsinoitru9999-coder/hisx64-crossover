using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

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
        Console.OutputEncoding = Encoding.UTF8;
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

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

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        // 1. Get all rooms in Khoa 57
        var bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var allRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        if (allRooms == null)
        {
            Console.WriteLine("Lỗi: Không lấy được phòng Khoa 57.");
            return;
        }

        Console.WriteLine(string.Format("Số buồng bệnh Khoa 57: {0}", allRooms.Count));

        // 2. Query all patients in these rooms
        var allPatients = new List<Tuple<V_HIS_BED_ROOM, V_HIS_TREATMENT_BED_ROOM, V_HIS_TREATMENT>>();
        foreach (var room in allRooms)
        {
            var tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
            if (inPatients == null || inPatients.Count == 0) continue;

            foreach (var p in inPatients)
            {
                V_HIS_TREATMENT tr = null;
                var tf = new HisTreatmentViewFilter { ID = p.TREATMENT_ID };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trList != null && trList.Count > 0) tr = trList[0];

                allPatients.Add(new Tuple<V_HIS_BED_ROOM, V_HIS_TREATMENT_BED_ROOM, V_HIS_TREATMENT>(room, p, tr));
            }
        }

        Console.WriteLine(string.Format("Tổng số bệnh nhân đang nằm buồng tại Khoa 57: {0}", allPatients.Count));

        // 3. Filter patients admitted today (20260907)
        Console.WriteLine("\n==========================================================================================");
        Console.WriteLine("🏥 DANH SÁCH BỆNH NHÂN NHẬP VIỆN / VÀO KHOA 57 HÔM NAY (07/09/2026)");
        Console.WriteLine("==========================================================================================");

        var todayAdmissions = new List<Tuple<V_HIS_BED_ROOM, V_HIS_TREATMENT_BED_ROOM, V_HIS_TREATMENT>>();
        foreach (var item in allPatients)
        {
            var room = item.Item1;
            var bed = item.Item2;
            var tr = item.Item3;

            string inTimeStr = tr != null ? tr.IN_TIME.ToString() : "";
            string clinInTimeStr = (tr != null && tr.CLINICAL_IN_TIME.HasValue) ? tr.CLINICAL_IN_TIME.Value.ToString() : "";
            string addTimeStr = bed.ADD_TIME > 0 ? bed.ADD_TIME.ToString() : "";

            if (inTimeStr.StartsWith("20260907") || clinInTimeStr.StartsWith("20260907") || addTimeStr.StartsWith("20260907"))
            {
                todayAdmissions.Add(item);
            }
        }

        Console.WriteLine(string.Format("🎯 SỐ LƯỢNG BỆNH NHÂN NHẬP VIỆN / VÀO BUỒNG HÔM NAY: {0} BỆNH NHÂN\n", todayAdmissions.Count));

        int stt = 1;
        foreach (var item in todayAdmissions)
        {
            var room = item.Item1;
            var bed = item.Item2;
            var tr = item.Item3;

            string pName = tr != null ? tr.TDL_PATIENT_NAME : bed.TDL_PATIENT_NAME;
            string pCode = tr != null ? tr.TDL_PATIENT_CODE : bed.TDL_PATIENT_CODE;
            string tCode = tr != null ? tr.TREATMENT_CODE : "";
            string dobStr = tr != null ? tr.TDL_PATIENT_DOB.ToString() : "";
            string gender = tr != null ? tr.TDL_PATIENT_GENDER_NAME : "";
            string inTimeStr = tr != null ? tr.IN_TIME.ToString() : "";
            string addTimeStr = bed.ADD_TIME > 0 ? bed.ADD_TIME.ToString() : "";
            string icd = tr != null ? string.Format("[{0}] {1}", tr.ICD_CODE, tr.ICD_NAME) : "";
            string icdText = tr != null ? tr.ICD_TEXT : "";

            string fInTime = inTimeStr.Length >= 12 ? string.Format("{0}:{1} - {2}/{3}/{4}", inTimeStr.Substring(8, 2), inTimeStr.Substring(10, 2), inTimeStr.Substring(6, 2), inTimeStr.Substring(4, 2), inTimeStr.Substring(0, 4)) : inTimeStr;
            string fAddTime = addTimeStr.Length >= 12 ? string.Format("{0}:{1} - {2}/{3}/{4}", addTimeStr.Substring(8, 2), addTimeStr.Substring(10, 2), addTimeStr.Substring(6, 2), addTimeStr.Substring(4, 2), addTimeStr.Substring(0, 4)) : addTimeStr;

            Console.WriteLine(string.Format("{0:D2}. {1} | {2} | Mã BN: {3} | Mã ĐT: {4}", stt++, pName, gender, pCode, tCode));
            Console.WriteLine(string.Format("    📍 Vị trí: {0} - {1}", room.BED_ROOM_NAME, bed.BED_NAME));
            Console.WriteLine(string.Format("    ⏰ Thời gian vào viện: {0} (Vào buồng: {1})", fInTime, fAddTime));
            Console.WriteLine(string.Format("    🩺 Chẩn đoán: {0}", icd));
            if (!string.IsNullOrEmpty(icdText)) Console.WriteLine(string.Format("    📋 Chi tiết: {0}", icdText));
            Console.WriteLine();
        }

        Console.WriteLine("==========================================================================================");

        // Also check if there are other patients with IN_TIME today that are registered in Khoa 57
        var allTreatmentsToday = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter
        {
            IN_TIME_FROM = 20260907000000,
            IN_TIME_TO = 20260907235959
        }, param);

        if (allTreatmentsToday != null)
        {
            var dept57Treats = allTreatmentsToday.Where(x => x.LAST_DEPARTMENT_ID == 57 || x.IN_DEPARTMENT_ID == 57).ToList();
            Console.WriteLine(string.Format("\n🔍 Bệnh nhân đăng ký tiếp đón / nhập viện có chỉ định Khoa 57 hôm nay: {0}", dept57Treats.Count));
            foreach (var dt in dept57Treats)
            {
                bool alreadyInBed = todayAdmissions.Any(x => x.Item3 != null && x.Item3.ID == dt.ID);
                Console.WriteLine(string.Format("- BN: {0} ({1}) | Mã ĐT: {2} | Giờ vào: {3} | Đã xếp buồng: {4} | ICD: [{5}] {6}",
                    dt.TDL_PATIENT_NAME, dt.TDL_PATIENT_CODE, dt.TREATMENT_CODE, dt.IN_TIME, alreadyInBed ? "ĐÃ XẾP" : "CHƯA XẾP BUỒNG", dt.ICD_CODE, dt.ICD_NAME));
            }
        }
    }
}