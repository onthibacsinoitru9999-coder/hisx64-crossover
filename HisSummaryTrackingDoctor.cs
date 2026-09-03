using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class HisSummaryTrackingDoctor
{
    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            return chunk.Substring(start, 64);
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                CommonParam p = new CommonParam();
                var tok = tokenManager.Login(p, "034727", "9981", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tokenManager.Login(p, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam param = new CommonParam();
        MyAdapter adapter = new MyAdapter();

        string roomFilter = "712,714";
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            roomFilter = args[0];
        }

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        var filters = roomFilter.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        var targetRooms = bList.Where(r => r.BED_ROOM_NAME != null && filters.Any(f => r.BED_ROOM_NAME.Contains(f))).ToList();

        DateTime today = DateTime.Today;

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("🏥 ĐỐI SOÁT TỜ ĐIỀU TRỊ SƠ KẾT 3 NGÀY & 7 NGÀY - PHÒNG {0} (KHOA 57)", roomFilter));
        Console.WriteLine(string.Format("Thời gian kiểm tra: {0} | Bác sĩ: Ths.BS Nguyễn Hữu Sâm (034727)", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
        Console.WriteLine("==========================================================================================================\n");

        foreach (var room in targetRooms.OrderBy(x => x.BED_ROOM_NAME))
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var inP = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
            if (inP == null || inP.Count == 0) continue;

            Console.WriteLine(string.Format("🏨 Buồng: {0} ({1} bệnh nhân):\n", room.BED_ROOM_NAME, inP.Count));

            foreach (var patient in inP.OrderBy(x => x.BED_NAME))
            {
                long tId = patient.TREATMENT_ID;
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;
                if (tr == null) continue;

                DateTime inDate = today;
                if (tr.IN_TIME > 0)
                {
                    string s = tr.IN_TIME.ToString();
                    if (s.Length >= 8)
                    {
                        int y = int.Parse(s.Substring(0, 4));
                        int m = int.Parse(s.Substring(4, 2));
                        int d = int.Parse(s.Substring(6, 2));
                        inDate = new DateTime(y, m, d);
                    }
                }
                int days = (int)(today - inDate).TotalDays + 1;

                HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tId };
                var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
                if (trkList == null) trkList = new List<HIS_TRACKING>();

                var sk3List = trkList.Where(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 3") || c.Contains("sơ kết 03") || c.Contains("sk 3 ngày") || c.Contains("sơ kết 3 ngày") || c.Contains("sơ kết ba ngày");
                }).ToList();

                var sk7List = trkList.Where(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 7") || c.Contains("sơ kết 07") || c.Contains("sk 7 ngày") || c.Contains("sơ kết 7 ngày") || c.Contains("sơ kết tuần") || c.Contains("sơ kết 15") || c.Contains("sơ kết bảy ngày");
                }).ToList();

                var allSkList = trkList.Where(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết");
                }).ToList();

                string reqStr = "";
                string statusBadge = "";

                if (days <= 2)
                {
                    reqStr = "Mới vào viện (" + days + " ngày)";
                    statusBadge = "⚪ CHƯA ĐẾN HẠN";
                }
                else if (days >= 3 && days <= 6)
                {
                    reqStr = string.Format("Ngày thứ {0} -> CẦN SƠ KẾT 3 NGÀY", days);
                    if (sk3List.Count > 0)
                    {
                        statusBadge = "🟢 ĐÃ CÓ SƠ KẾT 3 NGÀY";
                    }
                    else if (allSkList.Count > 0)
                    {
                        statusBadge = "🟠 ĐÃ CÓ SƠ KẾT KHÁC (" + allSkList.Count + " tờ)";
                    }
                    else
                    {
                        statusBadge = "🔴 THIẾU SƠ KẾT 3 NGÀY";
                    }
                }
                else // >= 7 ngày
                {
                    reqStr = string.Format("Ngày thứ {0} -> CẦN SƠ KẾT 7 NGÀY", days);
                    bool has7 = sk7List.Count > 0;
                    bool has3 = sk3List.Count > 0;

                    if (has7 && has3)
                    {
                        statusBadge = "🟢 ĐÃ CÓ ĐỦ SK 3 NGÀY & 7 NGÀY";
                    }
                    else if (has7)
                    {
                        statusBadge = "🟢 ĐÃ CÓ SƠ KẾT 7 NGÀY";
                    }
                    else if (has3)
                    {
                        statusBadge = "🟠 ĐÃ CÓ SK 3 NGÀY (CẦN BỔ SUNG SK 7 NGÀY)";
                    }
                    else if (allSkList.Count > 0)
                    {
                        statusBadge = "🟠 ĐÃ CÓ SƠ KẾT KHÁC (CẦN ĐỐI SOÁT)";
                    }
                    else
                    {
                        statusBadge = "🔴 THIẾU SƠ KẾT 7 NGÀY (CHƯA CÓ SƠ KẾT NÀO)";
                    }
                }

                Console.WriteLine(string.Format("👉 [{0}] {1} (Mã BN: {2} | TrID: {3})", patient.BED_NAME, patient.TDL_PATIENT_NAME, patient.TDL_PATIENT_CODE, tId));
                Console.WriteLine(string.Format("   Ngày vào viện: {0} ({1} ngày điều trị) | Chẩn đoán: [{2}] {3}",
                    inDate.ToString("dd/MM/yyyy"), days, tr.ICD_CODE, tr.ICD_NAME));
                Console.WriteLine(string.Format("   Yêu cầu: {0} -> Trạng thái: {1}", reqStr, statusBadge));

                if (allSkList.Count > 0)
                {
                    Console.WriteLine("   📝 Chi tiết các tờ sơ kết đã có:");
                    foreach (var sk in allSkList.OrderBy(x => x.TRACKING_TIME))
                    {
                        string tt = sk.TRACKING_TIME.ToString();
                        string timeStr = tt.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", tt.Substring(6,2), tt.Substring(4,2), tt.Substring(0,4), tt.Substring(8,2), tt.Substring(10,2)) : tt;
                        string preview = (sk.CONTENT ?? "").Replace("\r\n", " ").Replace("\n", " ");
                        if (preview.Length > 100) preview = preview.Substring(0, 100) + "...";
                        Console.WriteLine(string.Format("     * [{0}] (ID: {1}): {2}", timeStr, sk.ID, preview));
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ Chưa có tờ sơ kết nào.");
                }
                Console.WriteLine();
            }
        }
    }
}

class Program
{
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

        HisSummaryTrackingDoctor.Run(args);
    }
}
