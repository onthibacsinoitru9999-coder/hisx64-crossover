using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

class QueryAdmissions
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
        MyAdapter myAdapter = new MyAdapter();
        CommonParam param = new CommonParam();
        ApiConsumer mosConsumer;

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

        if (string.IsNullOrEmpty(token))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var t = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (t != null) token = t.TokenCode;
            }
            catch {}
        }

        Console.WriteLine("Using Token: " + token.Substring(0, 15) + "...");
        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 5252 },
                    new RoomSDO { RoomId = 5251 },
                    new RoomSDO { RoomId = 5257 }
                }
            };
            myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch {}

        // 1. Query all active bed rooms in the whole hospital
        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);

        if (allBeds == null || allBeds.Count == 0)
        {
            Console.WriteLine("Không tìm thấy bệnh nhân nào đang nằm buồng!");
            return;
        }

        // Filter for Khoa CTCH & Cột Sống (DEPARTMENT_ID = 57)
        var dept57Beds = allBeds.Where(x => x.DEPARTMENT_ID == 57).OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME).ToList();
        Console.WriteLine(string.Format("Tổng số bệnh nhân đang nằm điều trị nội trú tại Khoa 57: {0}", dept57Beds.Count));

        // 2. Batch query Treatments for all patients in Khoa 57
        var treatIds = dept57Beds.Select(b => b.TREATMENT_ID).Distinct().ToList();
        var treatMap = new Dictionary<long, V_HIS_TREATMENT>();
        if (treatIds.Count > 0)
        {
            HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter();
            tfBatch.IDs = treatIds;
            var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
            if (tList != null)
            {
                foreach (var t in tList) treatMap[t.ID] = t;
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("📊 DANH SÁCH BỆNH NHÂN NHẬP VIỆN / VÀO KHOA 57 HÔM NAY (07/09/2026)");
        Console.WriteLine("===============================================================================");

        int admittedTodayCount = 0;
        foreach (var bed in dept57Beds)
        {
            V_HIS_TREATMENT t = null;
            treatMap.TryGetValue(bed.TREATMENT_ID, out t);

            string inTimeStr = t != null ? t.IN_TIME.ToString() : "";
            string clinInTimeStr = (t != null && t.CLINICAL_IN_TIME.HasValue) ? t.CLINICAL_IN_TIME.Value.ToString() : "";
            string addTimeStr = bed.ADD_TIME.ToString();

            bool isToday = inTimeStr.StartsWith("20260907") || clinInTimeStr.StartsWith("20260907") || addTimeStr.StartsWith("20260907");

            if (isToday)
            {
                admittedTodayCount++;
                string roomBed = string.Format("{0} - {1}", bed.BED_ROOM_NAME, bed.BED_NAME);
                string pName = t != null ? t.TDL_PATIENT_NAME : bed.TDL_PATIENT_NAME;
                string pCode = t != null ? t.TDL_PATIENT_CODE : bed.TDL_PATIENT_CODE;
                string tCode = t != null ? t.TREATMENT_CODE : "";
                string icdCode = t != null ? t.ICD_CODE : "";
                string icdName = t != null ? t.ICD_NAME : "";
                string icdText = t != null ? t.ICD_TEXT : "";

                string formattedInTime = inTimeStr.Length >= 12 ? string.Format("{0}:{1} {2}/{3}/{4}", inTimeStr.Substring(8, 2), inTimeStr.Substring(10, 2), inTimeStr.Substring(6, 2), inTimeStr.Substring(4, 2), inTimeStr.Substring(0, 4)) : inTimeStr;
                string formattedAddTime = addTimeStr.Length >= 12 ? string.Format("{0}:{1} {2}/{3}/{4}", addTimeStr.Substring(8, 2), addTimeStr.Substring(10, 2), addTimeStr.Substring(6, 2), addTimeStr.Substring(4, 2), addTimeStr.Substring(0, 4)) : addTimeStr;

                Console.WriteLine(string.Format("{0:D2}. BN: {1} | Mã BN: {2} | Mã ĐT: {3}", admittedTodayCount, pName, pCode, tCode));
                Console.WriteLine(string.Format("    📍 Vị trí: {0}", roomBed));
                Console.WriteLine(string.Format("    ⏰ Vào viện: {0} | Vào buồng: {1}", formattedInTime, formattedAddTime));
                Console.WriteLine(string.Format("    🩺 Chẩn đoán: [{0}] {1}", icdCode, icdName));
                if (!string.IsNullOrEmpty(icdText)) Console.WriteLine(string.Format("    📋 Chi tiết: {0}", icdText));
                Console.WriteLine();
            }
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏁 TỔNG KẾT: Có {0} bệnh nhân nhập viện/vào buồng Khoa CTCH & CS hôm nay (07/09/2026)", admittedTodayCount));
        Console.WriteLine("===============================================================================");

        // In all patients in Khoa 57 with their InTime for complete reference
        Console.WriteLine("\n📋 TOÀN BỘ DANH SÁCH BỆNH NHÂN HIỆN TẠI KHOA 57 (KÈM NGÀY VÀO VIỆN):");
        int idx = 1;
        foreach (var bed in dept57Beds)
        {
            V_HIS_TREATMENT t = null;
            treatMap.TryGetValue(bed.TREATMENT_ID, out t);
            string inTimeStr = t != null ? t.IN_TIME.ToString() : "";
            string formattedInTime = inTimeStr.Length >= 8 ? string.Format("{0}/{1}/{2}", inTimeStr.Substring(6, 2), inTimeStr.Substring(4, 2), inTimeStr.Substring(0, 4)) : inTimeStr;
            string pName = t != null ? t.TDL_PATIENT_NAME : bed.TDL_PATIENT_NAME;
            string pCode = t != null ? t.TDL_PATIENT_CODE : bed.TDL_PATIENT_CODE;
            string icd = t != null ? string.Format("[{0}] {1}", t.ICD_CODE, t.ICD_NAME) : "";
            Console.WriteLine(string.Format("{0:D2}. {1} ({2}) | {3} - {4} | Ngày vào: {5} | {6}",
                idx++, pName, pCode, bed.BED_ROOM_NAME, bed.BED_NAME, formattedInTime, icd));
        }
    }
}