using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class InspectManhRations
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

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
            string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        string token = "";
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // 1. Check Do Xuan Manh (7139074)
        long tId = 7139074;
        HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
        var tr = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp)[0];
        Console.WriteLine(string.Format("BN: {0} | TrID: {1} | Mã BN: {2}", tr.TDL_PATIENT_NAME, tId, tr.TDL_PATIENT_CODE));

        // Get all ServiceReqs
        HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
        var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
        var rationReqs = srs.Where(s => s.SERVICE_REQ_TYPE_ID == 17 || (s.SERVICE_REQ_TYPE_NAME != null && s.SERVICE_REQ_TYPE_NAME.Contains("ăn"))).ToList();
        Console.WriteLine("\n--- SERVICE_REQs Suất ăn của Đỗ Xuân Mạnh ---");
        foreach (var r in rationReqs.OrderBy(x => x.INTRUCTION_TIME))
        {
            Console.WriteLine(string.Format("ReqID: {0} | Code: {1} | Time: {2} | ReqRoom: {3} ({4}) | ExeRoom: {5} ({6}) | Creator: {7} | CreateTime: {8}",
                r.ID, r.SERVICE_REQ_CODE, r.INTRUCTION_TIME, r.REQUEST_ROOM_NAME, r.REQUEST_ROOM_ID, r.EXECUTE_ROOM_NAME, r.EXECUTE_ROOM_ID, r.CREATOR, r.CREATE_TIME));
        }

        // Get all SereServRation
        HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
        var rations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
        Console.WriteLine("\n--- SERE_SERV_RATION của Đỗ Xuân Mạnh ---");
        foreach (var r in rations.OrderBy(x => x.INTRUCTION_TIME).ThenBy(x => x.RATION_TIME_ID))
        {
            Console.WriteLine(string.Format("RationID: {0} | ReqID: {1} | Meal: {2} ({3}) | Time: {4} | SvcId: {5} | SvcName: {6} | Creator: {7}",
                r.ID, r.SERVICE_REQ_ID, r.RATION_TIME_ID, r.RATION_TIME_NAME, r.INTRUCTION_TIME, r.SERVICE_ID, r.SERVICE_NAME, r.CREATOR));
        }

        // Also check all available Refectory rooms (Nhà ăn / Phòng dinh dưỡng) in HIS
        Console.WriteLine("\n--- Danh sách Phòng Thực hiện Suất ăn / Nhà ăn trong hệ thống ---");
        HisRoomViewFilter roomFilter = new HisRoomViewFilter { IS_ACTIVE = 1 };
        var allRooms = adapter.FetchList<V_HIS_ROOM>("api/HisRoom/GetView", mos, roomFilter, cp);
        var foodRooms = allRooms.Where(r => r.ROOM_NAME != null && (r.ROOM_NAME.ToLower().Contains("ăn") || r.ROOM_NAME.ToLower().Contains("dinh dưỡng") || r.DEPARTMENT_ID == 30 || r.DEPARTMENT_ID == 12)).ToList();
        foreach (var rm in foodRooms)
        {
            Console.WriteLine(string.Format("RoomId: {0} | Code: {1} | Name: {2} | DeptId: {3} ({4}) | RoomTypeId: {5} ({6})",
                rm.ID, rm.ROOM_CODE, rm.ROOM_NAME, rm.DEPARTMENT_ID, rm.DEPARTMENT_NAME, rm.ROOM_TYPE_ID, rm.ROOM_TYPE_NAME));
        }
    }
}
