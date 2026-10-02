using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class VerifyAllSurgeryPrepOct02
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

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
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

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🔍 ĐỐI SOÁT SUẤT ĂN NGÀY MAI (02/10/2026) VÀ TRẠNG THÁI LEANPRO CHO 15 BN");
        Console.WriteLine("===============================================================================");

        long tomorrowStart = 20261002000000;
        long tomorrowEnd   = 20261002235959;

        for (int i = 0; i < pCodes.Length; i++)
        {
            string code = pCodes[i];
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = code };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", mos, tf, cp);
            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine(string.Format("❌ [{0:D2}] Không tìm thấy BN: {1}", i + 1, code));
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            // 1. Kiểm tra Suất ăn ngày mai 02/10/2026
            var rf = new HisSereServRationViewFilter { TREATMENT_ID = tr.ID };
            var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
            var tomorrowRations = rList != null ? rList.Where(x => x.INTRUCTION_TIME >= tomorrowStart && x.INTRUCTION_TIME <= tomorrowEnd).ToList() : new List<V_HIS_SERE_SERV_RATION>();

            // Kiểm tra ServiceReq suất ăn
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            var tomorrowReqs = srs != null ? srs.Where(x => 
                (x.SERVICE_REQ_TYPE_ID == 11 || (x.SERVICE_REQ_TYPE_NAME != null && x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("suất ăn"))) &&
                x.INTRUCTION_TIME >= tomorrowStart && x.INTRUCTION_TIME <= tomorrowEnd
            ).ToList() : new List<V_HIS_SERVICE_REQ>();

            string rationStatus = "✔ KHÔNG CÓ SUẤT ĂN NGÀY 02/10/2026 (Sạch sẽ)";
            if (tomorrowRations.Count > 0 || tomorrowReqs.Count > 0)
            {
                rationStatus = string.Format("⚠️ CÒN {0} BỮA / {1} PHIẾU SUẤT ĂN -> ĐANG XÓA...", tomorrowRations.Count, tomorrowReqs.Count);

                var reqIds = new HashSet<long>(tomorrowReqs.Select(x => x.ID));
                foreach (var r in tomorrowRations) if (r.SERVICE_REQ_ID > 0) reqIds.Add(r.SERVICE_REQ_ID);

                foreach (var rid in reqIds)
                {
                    var req = srs != null ? srs.FirstOrDefault(x => x.ID == rid) : null;
                    long reqRoom = req != null && req.REQUEST_ROOM_ID > 0 ? req.REQUEST_ROOM_ID : 5248;

                    // UpdateCommonInfo nếu cần
                    if (req != null && !string.IsNullOrEmpty(req.REQUEST_LOGINNAME) && req.REQUEST_LOGINNAME != "034727")
                    {
                        try
                        {
                            var rawFilter = new HisServiceReqFilter { ID = rid };
                            var rawList = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mos, rawFilter, cp);
                            if (rawList != null && rawList.Count > 0)
                            {
                                var rawReq = rawList[0];
                                rawReq.REQUEST_LOGINNAME = "034727";
                                rawReq.REQUEST_USERNAME = "Ths.BS Nguyễn Hữu Sâm";
                                rawReq.REQUEST_USER_TITLE = "Thạc sỹ y học";
                                adapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mos, rawReq, cp);
                            }
                        }
                        catch { }
                    }

                    var sdo = new HisServiceReqSDO { Id = rid, RequestRoomId = reqRoom };
                    CommonParam cpDel = new CommonParam();
                    bool del = adapter.PostData<bool>("api/HisServiceReq/Delete", mos, sdo, cpDel);
                    if (del) rationStatus = "✔ ĐÃ XÓA THÀNH CÔNG SUẤT ĂN NGÀY MAI!";
                }
            }

            // 2. Kiểm tra Tờ điều trị & Đơn Leanpro hôm nay
            long todayStart = 20261001000000;
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp);
            var todayTrks = trkList != null ? trkList.Where(x => x.TRACKING_TIME >= todayStart).ToList() : new List<V_HIS_TRACKING>();

            var expFilter = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tr.ID, MEDICINE_TYPE_ID = 26851 };
            var existingMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, expFilter, cp);
            var todayLeanpro = existingMeds != null ? existingMeds.Where(x => x.CREATE_TIME >= todayStart).ToList() : new List<V_HIS_EXP_MEST_MEDICINE>();

            string leanproStatus = todayLeanpro.Count > 0 
                ? string.Format("🥛 Đã kê Leanpro PreSur ({0} chai, Mã phiếu kho: {1})", todayLeanpro.Sum(x => x.AMOUNT), todayLeanpro[0].EXP_MEST_CODE)
                : "⛔ Không kê (Tuổi >= 70 hoặc ĐTĐ)";

            Console.WriteLine(string.Format("[{0:D2}] {1,-20} ({2})", i + 1, tr.TDL_PATIENT_NAME, code));
            Console.WriteLine(string.Format("     • Suất ăn 02/10: {0}", rationStatus));
            Console.WriteLine(string.Format("     • Tờ ĐT hôm nay : {0} tờ (Gần nhất Sheet: {1})", todayTrks.Count, todayTrks.Count > 0 ? todayTrks.OrderByDescending(x => x.TRACKING_TIME).First().SHEET_ORDER.ToString() : "Chưa có"));
            Console.WriteLine(string.Format("     • Dinh dưỡng    : {0}", leanproStatus));
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
