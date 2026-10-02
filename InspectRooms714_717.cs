using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
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
        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        try { Load.Init(); } catch { }
        var param = new CommonParam();
        var tokenManager = new Inventec.Token.ClientSystem.ClientTokenManager("HIS");
        var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
        if (token == null || string.IsNullOrEmpty(token.TokenCode))
        {
            token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        }
        string tokenCode = token != null ? token.TokenCode : "";
        Console.WriteLine("Token: " + (tokenCode.Length > 10 ? tokenCode.Substring(0, 10) : "") + "...");

        var consumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
        var adapter = new MyAdapter();

        // Update WorkInfo
        long[] dept57Rooms = new long[] { 931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267 };
        var workInfo = new WorkInfoSDO { Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList() };
        adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", consumer, workInfo, param);

        string[] codes = new string[] { "0004033304", "0004048113", "0003380587", "0003002690", "0003863470", "0001344789", "0004093343", "0004093319" };

        foreach (var c in codes)
        {
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("KIỂM TRA BỆNH NHÂN: " + c);
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = c.PadLeft(10, '0') };
            var listT = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            if (listT == null || listT.Count == 0)
            {
                tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = c.PadLeft(12, '0') };
                listT = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, tf, param);
            }
            if (listT == null || listT.Count == 0)
            {
                Console.WriteLine("KHÔNG TÌM THẤY HỒ SƠ!");
                continue;
            }

            var t = listT.OrderByDescending(x => x.IN_TIME).First();
            Console.WriteLine(string.Format("BN: {0} | Mã BN: {1} | Mã ĐT: {2} | TreatmentID: {3} | InTime: {4} | Dept: {5}",
                t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.ID, t.IN_TIME, t.LAST_DEPARTMENT_ID));
            Console.WriteLine("Chẩn đoán: " + t.ICD_NAME + " - " + t.ICD_SUB_CODE + " - " + t.ICD_TEXT);

            // 1. Tracking 29/09
            var trf = new HisTrackingViewFilter { TREATMENT_ID = t.ID };
            var trs = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trf, param);
            if (trs != null && trs.Count > 0)
            {
                var trSorted = trs.OrderByDescending(x => x.TRACKING_TIME).Take(2).ToList();
                foreach (var tr in trSorted)
                {
                    Console.WriteLine(string.Format("📄 TỜ ĐIỀU TRỊ [{0}] (ID: {1}):\n{2}", tr.TRACKING_TIME, tr.ID, tr.CONTENT));
                }
            }
            else
            {
                Console.WriteLine("⚠️ CHƯA CÓ TỜ ĐIỀU TRỊ NÀO!");
            }

            // 2. ServiceReqs
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = t.ID };
            var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, param);
            if (reqs != null && reqs.Count > 0)
            {
                Console.WriteLine("💊 Y LỆNH 28, 29, 30/09:");
                var recentReqs = reqs.Where(x => {
                    string s = x.INTRUCTION_TIME.ToString();
                    return s.StartsWith("20260928") || s.StartsWith("20260929") || s.StartsWith("20260930");
                }).OrderBy(x => x.INTRUCTION_TIME).ToList();

                foreach (var r in recentReqs)
                {
                    Console.WriteLine(string.Format("  • [ID: {0} | Loại: {1} ({2}) | Lúc: {3} | Mã: {4}]",
                        r.ID, r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_TYPE_NAME, r.INTRUCTION_TIME, r.SERVICE_REQ_CODE));

                    var ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var s in sss)
                        {
                            Console.WriteLine(string.Format("      - {0} | SL: {1} {2} | Kho/Phòng: {3}",
                                s.TDL_SERVICE_NAME, s.AMOUNT, s.SERVICE_UNIT_NAME, s.TDL_REQUEST_ROOM_ID));
                        }
                    }
                }
            }
        }
    }
}
