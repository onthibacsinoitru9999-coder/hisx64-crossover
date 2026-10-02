using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class Program
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            return null;
        };
        RunAudit();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunAudit()
    {
        string token = "";
        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        if (!File.Exists(cacheFile))
        {
            string alt = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\doctor_standalone.token";
            if (File.Exists(alt)) cacheFile = alt;
        }
        if (File.Exists(cacheFile))
        {
            try
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2 && parts[0].Length == 64) token = parts[0];
            } catch { }
        }

        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var param = new CommonParam();

        string[] codes = new string[] { "0004033304", "0004048113", "0003380587", "0003002690", "0003863470", "0001344789", "0004093343", "0004093319" };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("📊 KẾT QUẢ ĐỐI SOÁT CUỐI CÙNG CHO BUỒNG 714 & 717 NGÀY 30/09/2026");
        Console.WriteLine("===============================================================================");

        foreach (var c in codes)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = c.PadLeft(10, '0') };
            var listT = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            if (listT == null || listT.Count == 0)
            {
                tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = c.PadLeft(12, '0') };
                listT = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            }
            if (listT == null || listT.Count == 0) continue;

            var tr = listT.OrderByDescending(x => x.IN_TIME).First();

            // Tracking 30/09
            var trFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID, TRACKING_TIME_FROM = 20260930000000 };
            var trks = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trFilter, param);
            string trkStatus = (trks != null && trks.Count > 0) ? string.Format("✔ ID: {0} ({1})", trks[0].ID, trks[0].TRACKING_TIME) : "🔴 Chưa có";

            // ServiceReqs 30/09
            var srFilter = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID, INTRUCTION_TIME_FROM = 20260930000000 };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srFilter, param);
            
            var rationReq = srs != null ? srs.FirstOrDefault(x => x.SERVICE_REQ_TYPE_ID == 14 && x.IS_DELETE != 1) : null;
            string rationStatus = rationReq != null ? string.Format("✔ Mã: {0} ({1})", rationReq.SERVICE_REQ_CODE, rationReq.SERVICE_REQ_TYPE_NAME) : "🔴 Chưa có";

            var medReq = srs != null ? srs.FirstOrDefault(x => x.SERVICE_REQ_TYPE_ID == 6 && x.IS_DELETE != 1) : null;
            string medStatus = medReq != null ? string.Format("✔ Mã: {0}", medReq.SERVICE_REQ_CODE) : "Chưa kê";

            Console.WriteLine(string.Format("• {0} (Mã BN: {1} | Mã ĐT: {2}):\n    📄 Tờ ĐT 30/09: {3}\n    🍲 Suất ăn 30/09: {4}\n    💊 Đơn thuốc 30/09: {5}",
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, trkStatus, rationStatus, medStatus));
        }
    }
}
