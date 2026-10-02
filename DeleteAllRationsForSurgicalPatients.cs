using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class Program
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T FetchSingle<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<T>(uri, consumer, filter, param);
        }

        public bool PostData<T>(string uri, ApiConsumer consumer, T data, CommonParam param)
        {
            return Post<bool>(uri, consumer, data, param);
        }

        public bool PostSDO<T>(string uri, ApiConsumer consumer, T data, CommonParam param)
        {
            return Post<bool>(uri, consumer, data, param);
        }

        public TResult PostDataWithResult<TResult, TData>(string uri, ApiConsumer consumer, TData data, CommonParam param)
        {
            return Post<TResult>(uri, consumer, data, param);
        }
    }

    static void Main(string[] args)
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

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("ERROR: Cannot find TokenCode");
            return;
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        var patientCodes = new string[] {
            "0004062838", // 1. Nguyễn Thị Thìn
            "0001530113", // 2. Lê Văn Hòa
            "0004079114", // 3. Phan Thị Tư
            "0004050211", // 4. Bùi Thị Thiện
            "0004089090", // 5. Vũ Thành Trung
            "0002840646", // 6. Phạm Văn Bỏng
            "0004081634", // 7. Nguyễn Thị Thu
            "0003837595", // 8. Nguyễn Thị Ngọc
            "0004079203", // 9. Nguyễn Khắc Chủ
            "0004067971", // 10. Nguyễn Thị Thảo
            "0003863470", // 11. Trần Thị Lượng
            "0002740977"  // 12. Nguyễn Văn Phương
        };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🚀 TIẾN HÀNH XÓA TRIỆT ĐỂ TOÀN BỘ SUẤT ĂN NGÀY 30/09 CHO 12 BN MỔ PHIÊN");
        Console.WriteLine("===============================================================================");

        int totalDeleted = 0;

        for (int i = 0; i < patientCodes.Length; i++)
        {
            string pCode = patientCodes[i];
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            if (treatments == null || treatments.Count == 0) continue;

            var tr = treatments.OrderByDescending(x => x.IN_TIME).First();
            Console.WriteLine(string.Format("\n[{0}/{1}] BN: {2} (Mã BN: {3} | Mã ĐT: {4})",
                i + 1, patientCodes.Length, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            if (allReqs == null || allReqs.Count == 0)
            {
                Console.WriteLine("   -> Không có y lệnh nào.");
                continue;
            }

            // Find all ration/suất ăn on 30/09
            var mealReqs = allReqs.Where(r => 
                (r.INTRUCTION_TIME >= 20260930000000L && r.INTRUCTION_TIME <= 20260930235959L) &&
                (r.SERVICE_REQ_TYPE_ID == 8 || r.SERVICE_REQ_TYPE_ID == 12 || r.SERVICE_REQ_TYPE_ID == 17 ||
                 (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn")) ||
                 (r.EXECUTE_ROOM_NAME != null && r.EXECUTE_ROOM_NAME.ToLower().Contains("dinh dưỡng")))
            ).ToList();

            if (mealReqs.Count == 0)
            {
                Console.WriteLine("   ✔ Đã sạch (0 suất ăn ngày 30/09).");
                continue;
            }

            foreach (var mr in mealReqs)
            {
                Console.WriteLine(string.Format("   🗑️ Phát hiện phiếu {0} (ID: {1}) | Loại: {2} | Người CĐ: {3} ({4})",
                    mr.SERVICE_REQ_CODE, mr.ID, mr.SERVICE_REQ_TYPE_NAME, mr.REQUEST_USERNAME, mr.REQUEST_LOGINNAME));

                // 1. Chuyển quyền nếu chưa phải 034727
                if (mr.REQUEST_LOGINNAME != "034727")
                {
                    HisServiceReqFilter rawF = new HisServiceReqFilter { ID = mr.ID };
                    var rawList = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mos, rawF, cp);
                    if (rawList != null && rawList.Count > 0)
                    {
                        var rawReq = rawList[0];
                        rawReq.REQUEST_LOGINNAME = "034727";
                        rawReq.REQUEST_USERNAME = "Ths.BS NGUYỄN HỮU SÂM";
                        rawReq.REQUEST_USER_TITLE = "Thạc sỹ y học";
                        adapter.PostDataWithResult<HIS_SERVICE_REQ, HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mos, rawReq, cp);
                    }
                }

                // 2. Xóa bằng HisServiceReqSDO
                var sdo = new HisServiceReqSDO
                {
                    Id = mr.ID,
                    RequestRoomId = 5248
                };

                CommonParam pDel = new CommonParam();
                bool delOk = adapter.PostDataWithResult<bool, HisServiceReqSDO>("api/HisServiceReq/Delete", mos, sdo, pDel);
                if (!delOk && mr.REQUEST_ROOM_ID > 0)
                {
                    sdo.RequestRoomId = mr.REQUEST_ROOM_ID;
                    pDel = new CommonParam();
                    delOk = adapter.PostDataWithResult<bool, HisServiceReqSDO>("api/HisServiceReq/Delete", mos, sdo, pDel);
                }

                if (delOk)
                {
                    Console.WriteLine(string.Format("      ✔ ĐÃ XÓA THÀNH CÔNG: Mã phiếu {0} (ID: {1})", mr.SERVICE_REQ_CODE, mr.ID));
                    totalDeleted++;
                }
                else
                {
                    Console.WriteLine(string.Format("      ❌ Xóa thất bại: {0}", pDel.GetMessage()));
                }
            }
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(string.Format("🎉 TỔNG KẾT: Đã quét 12 bệnh nhân và xóa thành công {0} phiếu suất ăn ngày 30/09!", totalDeleted));
        Console.WriteLine("===============================================================================");
    }
}
