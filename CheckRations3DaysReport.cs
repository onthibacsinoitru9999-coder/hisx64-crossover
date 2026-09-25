using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class CheckRations3DaysReport
{
    static string ReadLiveToken()
    {
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        if (!File.Exists(p)) return "";
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line, token = "";
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
            return token;
        }
    }

    public static string DetectCombo(string diag)
    {
        if (string.IsNullOrEmpty(diag)) return "BT01";
        string d = diag.ToLower();
        if (d.Contains("tháo đường") || d.Contains("đtđ") || d.Contains("diabetes"))
            return "DD01 (Tiểu đường)";
        if (d.Contains("tăng huyết áp") || d.Contains("tim mạch") || d.Contains("suy tim") || d.Contains("rung nhĩ"))
            return "TM01 (Tim mạch / THA)";
        return "BT01 (Tiêu chuẩn)";
    }

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
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

    static void Run()
    {
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // Danh sách 12 BN mổ T7 ngày 26/09/2026 cần loại trừ
        var surgeryTomorrowCodes = new HashSet<string>
        {
            "0004073176", // LÊ THIỆU TƯỜNG
            "0004076519", // NGÔ THỊ THANH
            "0003000402", // TRẦN THỊ NGỌC DIỆP
            "0004074524", // LÊ TRÍ QUYẾN
            "0004055395", // CHU VĂN TUẤN
            "0001923208", // NGUYỄN HỮU TÀI
            "0004072758", // NGUYỄN THỊ XUÂN
            "0004008409", // LÊ THỊ BÌNH
            "0004071508", // TRẦN THỊ THO
            "0003987361", // NGUYỄN THỊ NHUNG
            "0004077425", // NGUYỄN THỊ KIỀU TRANG
            "0004076916"  // NGUYỄN THỊ HÒA
        };

        // Lấy danh sách buồng Khoa 57
        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mos, bf, cp);
        if (bList == null)
        {
            Console.WriteLine("❌ Lỗi lấy danh sách buồng!");
            return;
        }

        DateTime d1 = new DateTime(2026, 9, 26);
        DateTime d2 = new DateTime(2026, 9, 27);
        DateTime d3 = new DateTime(2026, 9, 28);

        long d1_start = 20260926000000;
        long d1_end   = 20260926235959;
        long d2_start = 20260927000000;
        long d2_end   = 20260927235959;
        long d3_start = 20260928000000;
        long d3_end   = 20260928235959;

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine("📊 BÁO CÁO RÀ SOÁT SUẤT ĂN 3 NGÀY TỚI (26/09, 27/09, 28/09/2026) — KHOA 57");
        Console.WriteLine("   (ĐÃ LOẠI TRỪ 12 BỆNH NHÂN CHỜ MỔ NGÀY MAI THỨ BẢY 26/09)");
        Console.WriteLine("==========================================================================================================\n");

        int totalInPatients = 0;
        int excludedSurgery = 0;
        int missingRationCount = 0;

        foreach (var room in bList.OrderBy(x => x.BED_ROOM_NAME))
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var inP = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mos, tbrf, cp);
            if (inP == null || inP.Count == 0) continue;

            foreach (var patient in inP.OrderBy(x => x.BED_NAME))
            {
                totalInPatients++;
                string pCode = patient.TDL_PATIENT_CODE;
                if (surgeryTomorrowCodes.Contains(pCode))
                {
                    excludedSurgery++;
                    continue;
                }

                long tId = patient.TREATMENT_ID;
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;
                string diag = tr != null ? (tr.ICD_NAME ?? tr.ICD_TEXT ?? "") : "";

                // Query suất ăn
                HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
                var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp) ?? new List<V_HIS_SERE_SERV_RATION>();

                int cnt_d1 = rList.Count(x => x.INTRUCTION_TIME >= d1_start && x.INTRUCTION_TIME <= d1_end);
                int cnt_d2 = rList.Count(x => x.INTRUCTION_TIME >= d2_start && x.INTRUCTION_TIME <= d2_end);
                int cnt_d3 = rList.Count(x => x.INTRUCTION_TIME >= d3_start && x.INTRUCTION_TIME <= d3_end);

                // Nếu thiếu bất kỳ ngày nào trong 3 ngày
                if (cnt_d1 == 0 || cnt_d2 == 0 || cnt_d3 == 0)
                {
                    missingRationCount++;
                    string combo = DetectCombo(diag);

                    string s_d1 = cnt_d1 == 0 ? "❌ Chưa có" : string.Format("✔ {0} bữa", cnt_d1);
                    string s_d2 = cnt_d2 == 0 ? "❌ Chưa có" : string.Format("✔ {0} bữa", cnt_d2);
                    string s_d3 = cnt_d3 == 0 ? "❌ Chưa có" : string.Format("✔ {0} bữa", cnt_d3);

                    Console.WriteLine(string.Format("🏥 [{0} - {1}] {2} | Tuổi: {3} | Mã BN: {4}", 
                        room.BED_ROOM_NAME, patient.BED_NAME, patient.TDL_PATIENT_NAME, patient.TDL_PATIENT_DOB != null ? (2026 - (int)(patient.TDL_PATIENT_DOB / 10000000000L)).ToString() : "", pCode));
                    Console.WriteLine(string.Format("   - Chẩn đoán: {0}", diag));
                    Console.WriteLine(string.Format("   - Suất ăn: T7 (26/09): {0} | CN (27/09): {1} | T2 (28/09): {2}", s_d1, s_d2, s_d3));
                    Console.WriteLine(string.Format("   - Đề xuất: {0}\n", combo));
                }
            }
        }

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("TỔNG KẾT: Toàn khoa có {0} BN nội trú | Đã loại trừ {1} BN mổ ngày mai | Phát hiện {2} BN CHƯA ĐỦ SUẤT ĂN 3 ngày tới.",
            totalInPatients, excludedSurgery, missingRationCount));
        Console.WriteLine("==========================================================================================================");
    }
}
