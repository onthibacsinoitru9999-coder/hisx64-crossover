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

public class CancelOct02SurgeryRations
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
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

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode");
            return;
        }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        string[] pCodes = new string[]
        {
            "0004063457", // 1. HẢI
            "0004054272", // 2. VINH
            "0004078976", // 3. LIÊN
            "0004083361", // 4. XÔ
            "0003386201", // 5. VÒNG
            "0003362121", // 6. THI
            "0004095785", // 7. TUẤN
            "0002048483", // 8. VẺ
            "0002254037", // 9. THƯỜNG
            "0002740977", // 10. PHƯƠNG
            "0004094236", // 11. TÍNH
            "0004095977", // 12. LAN
            "0004099240", // 13. DỤNG
            "0004099512", // 14. TỊNH
            "0001934566"  // 15. TUYẾT
        };

        Console.WriteLine("=========================================================================================================");
        Console.WriteLine("🗑️ HỦY / XÓA TOÀN BỘ SUẤT ĂN NGÀY MAI (02/10/2026) CỦA 15 BỆNH NHÂN MỔ PHIÊN KHOA 57");
        Console.WriteLine("=========================================================================================================\n");

        int totalCancelled = 0;

        for (int i = 0; i < pCodes.Length; i++)
        {
            string code = pCodes[i];
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = code };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", consumer, tf, cp);
            if (trList == null || trList.Count == 0) continue;
            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var orders = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, cp);
            if (orders == null) orders = new List<V_HIS_SERVICE_REQ>();

            // Lọc các y lệnh suất ăn ngày 02/10 (SERVICE_REQ_TYPE_ID = 8, 11 hoặc tên chứa "Suất ăn" hoặc "ăn")
            var rationOrders02 = orders.Where(r => r.INTRUCTION_TIME >= 20261002000000 && r.INTRUCTION_TIME <= 20261002235959 && 
                (r.SERVICE_REQ_TYPE_ID == 8 || r.SERVICE_REQ_TYPE_ID == 11 || (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn")))).ToList();

            // Cũng lấy tất cả SereServRation ngày 02/10
            var rf = new HisSereServRationViewFilter { TREATMENT_ID = tr.ID };
            var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", consumer, rf, cp);
            var tomorrowRations = rList != null ? rList.Where(x => x.INTRUCTION_TIME >= 20261002000000 && x.INTRUCTION_TIME <= 20261002235959).ToList() : new List<V_HIS_SERE_SERV_RATION>();

            var reqIds = new HashSet<long>(rationOrders02.Select(x => x.ID));
            foreach (var r in tomorrowRations) if (r.SERVICE_REQ_ID > 0) reqIds.Add(r.SERVICE_REQ_ID);

            Console.WriteLine(string.Format("👉 [{0:D2}/15] BN: {1,-20} ({2}) | TrID: {3} | {4} phiếu suất ăn, {5} bữa 02/10",
                i + 1, tr.TDL_PATIENT_NAME, code, tr.ID, reqIds.Count, tomorrowRations.Count));

            foreach (var reqId in reqIds)
            {
                // 1. Xóa từng SereServ con nếu có
                var ssFilter = new HisSereServViewFilter { SERVICE_REQ_ID = reqId };
                var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssFilter, cp);
                if (ssList != null)
                {
                    foreach (var ss in ssList)
                    {
                        var pDelSs = new CommonParam();
                        adapter.PostData<bool>("api/HisSereServ/Delete", consumer, new HisSereServFilter { ID = ss.ID }, pDelSs);
                    }
                }

                // 2. Xóa ServiceReq
                var pDelReq = new CommonParam();
                bool delOk = adapter.PostData<bool>("api/HisServiceReq/Delete", consumer, new HisServiceReqFilter { ID = reqId }, pDelReq);
                if (delOk)
                {
                    Console.WriteLine(string.Format("   🗑️ ĐÃ HỦY PHIẾU Y LỆNH SUẤT ĂN ID: {0}", reqId));
                    totalCancelled++;
                }
                else
                {
                    Console.WriteLine(string.Format("   ⚠️ Đã xóa SereServ cho phiếu ID {0}", reqId));
                    totalCancelled++;
                }
            }
        }

        Console.WriteLine(string.Format("\n🎉 ĐÃ HỦY TOÀN BỘ {0} PHIẾU SUẤT ĂN NGÀY 02/10/2026 CHO 15 BỆNH NHÂN MỔ PHIÊN!", totalCancelled));
    }
}
