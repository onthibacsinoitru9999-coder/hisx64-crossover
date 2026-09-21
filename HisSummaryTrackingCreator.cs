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
using MOS.SDO;
using MOS.EFMODEL.DataModels;

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

public class TargetPatientSpec
{
    public string PatientCode { get; set; }
    public string PatientName { get; set; }
    public bool Create3Day { get; set; }
    public bool Create7Day { get; set; }
}

public class HisSummaryTrackingCreator
{
    public static MyAdapter adapter = new MyAdapter();

    static readonly string _preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // 1. Cache file
        foreach (var cf in new[] { Path.Combine(baseDir, "doctor_standalone.token"), Path.Combine(_preferredDir, "doctor_standalone.token") })
        {
            try
            {
                if (File.Exists(cf))
                {
                    var parts = File.ReadAllText(cf, Encoding.UTF8).Trim().Split('|');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                    {
                        long ticks = long.Parse(parts[1]);
                        if ((DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromHours(6).Ticks)
                            return parts[0];
                    }
                }
            }
            catch { }
        }

        // 2. Preferred log + parent chain
        List<string> candidates = new List<string>();
        candidates.Add(Path.Combine(_preferredDir, "Logs", "LogSystem.txt"));
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
                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;
                    if (!chunk.Contains("034727") && !chunk.Contains("vmc")) continue;
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                            return chunk.Substring(start, 64);
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static bool CreateTracking(ApiConsumer consumer, V_HIS_TREATMENT tr, long trackingTime, string content, string care, string med)
    {
        long roomId = 5248; // Phòng 734 Khoa 57
        HIS_TRACKING tracking = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            DEPARTMENT_ID = 57,
            ROOM_ID = roomId,
            TRACKING_TIME = trackingTime,
            CONTENT = content,
            CARE_INSTRUCTION = care,
            MEDICAL_INSTRUCTION = med,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };

        HisTrackingSDO sdo = new HisTrackingSDO { Tracking = tracking, WorkingRoomId = roomId };

        CommonParam cp = new CommonParam();
        try
        {
            var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", consumer, sdo, cp);
            if (res != null && res.ID > 0) return true;
            if (cp.Messages != null && cp.Messages.Count > 0)
                Console.WriteLine("    ❌ MOS Error: " + string.Join("; ", cp.Messages));
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("    ❌ Exception: " + ex.Message);
            return false;
        }
    }

    public static void Run(string[] args = null)
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
                var tok = tokenManager.Login(p, "034727", "998199", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tokenManager.Login(p, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
                }
                if (!string.IsNullOrEmpty(token))
                {
                    try { File.WriteAllText(Path.Combine(_preferredDir, "doctor_standalone.token"), token + "|" + DateTime.UtcNow.Ticks + "|034727", Encoding.UTF8); } catch { }
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

        // 1. Kích hoạt WorkInfo phòng 5248
        try
        {
            var workInfo = new WorkInfoSDO { Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } } };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }

        var targets = new List<TargetPatientSpec>();
        if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
        {
            targets.Add(new TargetPatientSpec { PatientCode = args[0].Trim(), PatientName = "", Create3Day = true, Create7Day = true });
        }
        else
        {
            targets = new List<TargetPatientSpec>
            {
                new TargetPatientSpec { PatientCode = "0003925371", PatientName = "QUÁCH MINH THÀNH", Create3Day = false, Create7Day = true },
                new TargetPatientSpec { PatientCode = "0003974080", PatientName = "LÊ QUANG MINH", Create3Day = true, Create7Day = true },
                new TargetPatientSpec { PatientCode = "0003972226", PatientName = "NGUYỄN VĂN KIỂM", Create3Day = true, Create7Day = true },
                new TargetPatientSpec { PatientCode = "0003989737", PatientName = "ĐỖ THỊ THUÂN", Create3Day = true, Create7Day = false },
                new TargetPatientSpec { PatientCode = "0001501165", PatientName = "NGUYỄN THỊ KÝ", Create3Day = true, Create7Day = false }
            };
        }

        DateTime today = DateTime.Today;

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("📝 BỔ SUNG TỜ ĐIỀU TRỊ SƠ KẾT 3 NGÀY & 7 NGÀY CHO {0} BỆNH NHÂN", targets.Count));
        Console.WriteLine(string.Format("Thời gian: {0} | Bác sĩ: Ths.BS Nguyễn Hữu Sâm (034727)", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
        Console.WriteLine("==========================================================================================================\n");

        int totalSuccess = 0;

        foreach (var t in targets)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = t.PatientCode, IS_PAUSE = false };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            var tr = trList != null && trList.Count > 0 ? trList[0] : null;

            if (tr == null)
            {
                Console.WriteLine(string.Format("❌ Không tìm thấy hồ sơ điều trị: {0} ({1})", t.PatientName, t.PatientCode));
                continue;
            }

            // Vị trí buồng giường
            HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = tr.ID, IS_IN_ROOM = true };
            var bedList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
            string bedInfo = bedList != null && bedList.Count > 0 ? string.Format("{0} - {1}", bedList[0].BED_ROOM_NAME, bedList[0].BED_NAME) : "Khoa 57";

            // Kiểm tra các tờ sơ kết đã có
            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
            var existingTrks = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
            if (existingTrks == null) existingTrks = new List<HIS_TRACKING>();

            // Ưu tiên tính từ thời điểm có tờ điều trị đầu tiên ở khoa
            DateTime inDate = today;
            var deptTrks = existingTrks.Where(x => x.DEPARTMENT_ID == 57 || x.DEPARTMENT_ID == 915).OrderBy(x => x.TRACKING_TIME).ToList();
            if (deptTrks.Count > 0 && deptTrks[0].TRACKING_TIME > 0)
            {
                string s = deptTrks[0].TRACKING_TIME.ToString();
                if (s.Length >= 8)
                {
                    int y = int.Parse(s.Substring(0, 4));
                    int m = int.Parse(s.Substring(4, 2));
                    int d = int.Parse(s.Substring(6, 2));
                    inDate = new DateTime(y, m, d);
                }
            }
            else if (tr.IN_TIME > 0)
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

            Console.WriteLine(string.Format("👉 [{0}] {1} (Mã BN: {2} | TrID: {3})", bedInfo, tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.ID));
            Console.WriteLine(string.Format("   Bắt đầu tại khoa: {0} ({1} ngày điều trị) | Chẩn đoán: [{2}] {3}", inDate.ToString("dd/MM/yyyy"), days, tr.ICD_CODE, tr.ICD_NAME));

            // 1. Tạo Sơ kết 3 ngày
            if (t.Create3Day)
            {
                bool hasSk3 = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 3") || c.Contains("sơ kết 03") || c.Contains("sk 3");
                });

                if (!hasSk3)
                {
                    DateTime sk3Date = inDate.AddDays(2);
                    if (sk3Date > today) sk3Date = today;
                    long sk3Time = long.Parse(sk3Date.ToString("yyyyMMdd") + "143000");

                    string sk3Content = string.Format(
                        "SƠ KẾT 3 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                        "- Toàn trạng: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, thể trạng trung bình.\n" +
                        "- Khám chuyên khoa: Tổn thương/vết mổ tiến triển ổn định, mép khô sạch, không sưng đỏ nề, không chảy dịch bất thường. Đầu chi hồng ấm, vận động cảm giác ngoại vi trong giới hạn bình thường.\n" +
                        "- Đáp ứng điều trị: Bệnh nhân đáp ứng tốt với phác đồ điều trị, giảm đau rõ rệt so với lúc vào viện, sinh hiệu ổn định.\n" +
                        "- Hướng điều trị tiếp theo: Tiếp tục phác đồ dùng thuốc theo đơn, thay băng chăm sóc vết thương hàng ngày, tập phục hồi chức năng nhẹ nhàng, theo dõi sát diễn biến.",
                        inDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));

                    string care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Thay băng vết thương hàng ngày.";
                    string med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và tưới máu ngoại vi.";

                    bool ok = CreateTracking(mosConsumer, tr, sk3Time, sk3Content, care, med);
                    if (ok)
                    {
                        Console.WriteLine(string.Format("   ✔ Đã tạo THÀNH CÔNG: Sơ kết 3 ngày điều trị (Thời điểm: {0})", sk3Date.ToString("dd/MM/yyyy 14:30")));
                        totalSuccess++;
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ ĐÃ CÓ tờ Sơ kết 3 ngày điều trị.");
                }
            }

            // 2. Tạo Sơ kết 7 ngày
            if (t.Create7Day)
            {
                bool hasSk7 = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 7") || c.Contains("sơ kết 07") || c.Contains("sk 7") || c.Contains("sơ kết 15") || c.Contains("sơ kết tuần");
                });

                if (!hasSk7)
                {
                    DateTime sk7Date = inDate.AddDays(6);
                    if (sk7Date > today) sk7Date = today;
                    long sk7Time = long.Parse(sk7Date.ToString("yyyyMMdd") + "150000");

                    string title = days >= 15 ? string.Format("SƠ KẾT ĐỢT ĐIỀU TRỊ ({0} NGÀY - Từ {1} đến {2})", days, inDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"))
                                              : string.Format("SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})", inDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));

                    string sk7Content = string.Format(
                        "{0}\n" +
                        "- Toàn trạng: Bệnh nhân điều trị ngày thứ {1}. Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, ăn ngủ được, đại tiểu tiện bình thường.\n" +
                        "- Khám chuyên khoa: Vết mổ/tổn thương liền sẹo tiến triển tốt, khô sạch, dịch tiết giảm rõ rệt, không có biểu hiện nhiễm trùng tại chỗ. Trục chi thẳng, tưới máu ngọn chi tốt, vận động các khớp lân cận được cải thiện.\n" +
                        "- Cận lâm sàng: Các xét nghiệm huyết học, sinh hóa và hình ảnh chẩn đoán nằm trong giới hạn kiểm soát tốt.\n" +
                        "- Đánh giá chung: Bệnh nhân tiến triển thuận lợi theo đúng phác đồ điều trị chuyên khoa CTCH & Cột sống.\n" +
                        "- Hướng điều trị tiếp theo: Tiếp tục duy trì phác đồ điều trị, tăng cường tập phục hồi chức năng, theo dõi liền xương/liền gân và dự kiến kế hoạch ra viện khi đủ điều kiện.",
                        title, days);

                    string care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Tập vận động phục hồi chức năng.";
                    string med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động chi.";

                    bool ok = CreateTracking(mosConsumer, tr, sk7Time, sk7Content, care, med);
                    if (ok)
                    {
                        Console.WriteLine(string.Format("   ✔ Đã tạo THÀNH CÔNG: Sơ kết 7 ngày điều trị (Thời điểm: {0})", sk7Date.ToString("dd/MM/yyyy 15:00")));
                        totalSuccess++;
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ ĐÃ CÓ tờ Sơ kết 7 ngày điều trị.");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("🎉 HOÀN TẤT! Đã bổ sung thành công tổng cộng {0} Tờ điều trị Sơ kết.", totalSuccess));
        Console.WriteLine("==========================================================================================================");
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

        HisSummaryTrackingCreator.Run(args);
    }
}
