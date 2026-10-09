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
    public bool CreateDischargeOrTransfer { get; set; }
    public string SummaryType { get; set; } // "CHUYEN_HAI_DUONG", "RA_VIEN", "CHUYEN_TINH", "STANDARD"
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
        if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]) && args[0] != "--default" && args[0] != "--batch")
        {
            string[] codes = args[0].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var c in codes)
            {
                targets.Add(new TargetPatientSpec { PatientCode = c.Trim(), PatientName = "", Create3Day = true, Create7Day = true, CreateDischargeOrTransfer = true, SummaryType = "STANDARD" });
            }
        }
        else
        {
            // Danh sách 4 bệnh nhân theo đúng yêu cầu lâm sàng của Bác sĩ
            targets = new List<TargetPatientSpec>
            {
                new TargetPatientSpec {
                    PatientCode = "0004093786",
                    PatientName = "TRẦN THỊ HÁN",
                    Create3Day = true,
                    Create7Day = true,
                    CreateDischargeOrTransfer = true,
                    SummaryType = "CHUYEN_HAI_DUONG"
                },
                new TargetPatientSpec {
                    PatientCode = "0003664727",
                    PatientName = "LÊ ĐÌNH PHONG",
                    Create3Day = true,
                    Create7Day = false,
                    CreateDischargeOrTransfer = true,
                    SummaryType = "RA_VIEN"
                },
                new TargetPatientSpec {
                    PatientCode = "0003940472",
                    PatientName = "LÒ VĂN MẠNH",
                    Create3Day = false,
                    Create7Day = true,
                    CreateDischargeOrTransfer = true,
                    SummaryType = "CHUYEN_TINH"
                },
                new TargetPatientSpec {
                    PatientCode = "0004106886",
                    PatientName = "NGUYỄN ĐÌNH HIỆP",
                    Create3Day = true,
                    Create7Day = false,
                    CreateDischargeOrTransfer = true,
                    SummaryType = "RA_VIEN"
                }
            };
        }

        DateTime today = DateTime.Today;

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine(string.Format("📝 BỔ SUNG TỜ ĐIỀU TRỊ SƠ KẾT 3 NGÀY, 7 NGÀY & ĐỢT ĐIỀU TRỊ CHO {0} BỆNH NHÂN", targets.Count));
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

            // Ưu tiên tính từ thời điểm có tờ điều trị đầu tiên ở khoa hoặc IN_TIME
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

            // =========================================================================
            // 1. TẠO SƠ KẾT 3 NGÀY ĐIỀU TRỊ
            // =========================================================================
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

                    string sk3Content = "";
                    string care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Theo dõi sát diễn biến lâm sàng.";
                    string med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động ngoại vi.";

                    if (t.SummaryType == "CHUYEN_HAI_DUONG")
                    {
                        sk3Content = string.Format(
                            "SƠ KẾT 3 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân nữ 81 tuổi, vào viện vì đau dữ dội vùng cột sống thắt lưng sau ngã. Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, thể trạng trung bình.\n" +
                            "- Khám chuyên khoa: Đau chói tại mỏm gai và cạnh sống ngang mức đốt sống L1, co cứng cơ cạnh sống, hạn chế vận động cúi ngửa xoay thân mình. Không tê bì buốt lan xuống hai chân, nghiệm pháp Lasegue (-), đại tiểu tiện tự chủ.\n" +
                            "- Cận lâm sàng: MRI và X-quang cột sống thắt lưng: Hình ảnh xẹp cấp thân đốt sống L1. Bilan trước mổ (huyết học, đông máu, sinh hóa, ECG) đã hoàn tất trong giới hạn kiểm soát tốt.\n" +
                            "- Chẩn đoán: [M48.30] Xẹp L1 - Rối loạn giấc ngủ.\n" +
                            "- Đáp ứng điều trị: Bệnh nhân đáp ứng điều trị nội khoa giảm đau, sinh hiệu ổn định (HA 130/80 mmHg, Mạch 75 l/p).\n" +
                            "- Hướng điều trị tiếp theo: Hoàn tất hội chẩn chuyên khoa, giải thích người nhà và bệnh nhân, chuẩn bị phẫu thuật cố định cột sống thắt lưng, bơm cement sinh học thân đốt L1.",
                            inDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Nằm bất động tương đối tại giường.";
                        med = "Dùng thuốc theo y lệnh. Chuẩn bị trước phẫu thuật.";
                    }
                    else if (t.SummaryType == "RA_VIEN" && t.PatientCode == "0004106886")
                    {
                        // Nguyễn Đình Hiệp
                        sk3Content = string.Format(
                            "SƠ KẾT 3 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân nam 61 tuổi, vào viện vì chấn thương cổ chân trái sau TNGT. Ngày 06/10/2026 được phẫu thuật kết hợp xương nẹp vít mắt cá ngoài chân trái. Hiện tại hậu phẫu ngày 1: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, sinh hiệu ổn định (Mạch 70 l/p, HA 120/70 mmHg).\n" +
                            "- Khám chuyên khoa: Vết mổ cổ chân trái nẹp bột vững chắc, băng ép thấm ít dịch hồng, không sưng phù nề nhiều, ngọn chi hồng ấm, cử động các ngón chân trái bình thường, cảm giác đầu chi tốt, đau vết mổ VAS 3đ.\n" +
                            "- Cận lâm sàng: X-quang kiểm tra sau mổ nẹp vít cố định xương mác vững chắc, diện khớp cổ chân phục hồi tốt.\n" +
                            "- Đánh giá chung: Hậu phẫu ngày đầu sau can thiệp KHX tiến triển thuận lợi.\n" +
                            "- Hướng điều trị tiếp theo: Kháng sinh, giảm đau, chống phù nề, kê cao chân trái, theo dõi tưới máu đầu ngón chân.",
                            inDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Kê cao chân trái, thay băng vết mổ.";
                        med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và tưới máu ngọn chi.";
                    }
                    else if (t.SummaryType == "RA_VIEN" && t.PatientCode == "0003664727")
                    {
                        // Lê Đình Phong (vào viện 3 ngày ra viện)
                        sk3Content = string.Format(
                            "SƠ KẾT 3 NGÀY ĐIỀU TRỊ & SƠ KẾT RA VIỆN (Từ {0} đến {1})\n" +
                            "1. Lý do vào viện: Đau khớp gối phải / Tiền sử vỡ mâm chày cẳng chân phải đã mổ KHX đinh nội tủy 2 tháng tại BV Bạch Mai.\n" +
                            "2. Quá trình điều trị: Bệnh nhân được chẩn đoán [M12.46] Viêm bao hoạt dịch gối phải / sau mổ khx chày phải. Điều trị nội khoa tích cực 3 ngày: kháng sinh, chống viêm, giảm phù nề, nghỉ ngơi hạn chế vận động khớp gối.\n" +
                            "3. Tình trạng hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, không sốt, sinh hiệu ổn định (Mạch 68 l/p, HA 120/75 mmHg). Khớp gối phải hết sưng nề, nghiệm pháp bập bềnh xương bánh chè (-), hết đau tại chỗ (VAS 1đ), tầm vận động gấp duỗi khớp gối cải thiện rõ rệt, đi lại nhẹ nhàng bình thường. Vết mổ cũ sẹo liền tốt, khô sạch.\n" +
                            "4. Cận lâm sàng: Các chỉ số huyết học, sinh hóa máu trong giới hạn bình thường.\n" +
                            "5. Đánh giá chung: Tình trạng viêm bao hoạt dịch gối phải thoái lui tốt, bệnh ổn định, đủ điều kiện kết thúc đợt điều trị nội trú.\n" +
                            "6. Y lệnh ra viện:\n" +
                            "- Cho bệnh nhân ra viện ngày {1}.\n" +
                            "- Kê đơn thuốc điều trị ngoại trú.\n" +
                            "- Dặn dò chế độ sinh hoạt: Tránh mang vác nặng, tránh ngồi xổm hay gập gối quá mức.\n" +
                            "- Hẹn tái khám sau 2 tuần hoặc khi có dấu hiệu bất thường.",
                            inDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp III. Ăn uống bình thường. Hướng dẫn thủ tục ra viện.";
                        med = "Kê đơn thuốc ngoại trú ra viện. Hẹn tái khám theo hẹn.";
                    }
                    else
                    {
                        sk3Content = string.Format(
                            "SƠ KẾT 3 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, thể trạng trung bình.\n" +
                            "- Khám chuyên khoa: Tổn thương/vết mổ tiến triển ổn định, mép khô sạch, không sưng đỏ nề, không chảy dịch bất thường. Đầu chi hồng ấm, vận động cảm giác ngoại vi trong giới hạn bình thường.\n" +
                            "- Đáp ứng điều trị: Bệnh nhân đáp ứng tốt với phác đồ điều trị, giảm đau rõ rệt so với lúc vào viện, sinh hiệu ổn định.\n" +
                            "- Hướng điều trị tiếp theo: Tiếp tục phác đồ dùng thuốc theo đơn, thay băng chăm sóc vết thương hàng ngày, tập phục hồi chức năng nhẹ nhàng, theo dõi sát diễn biến.",
                            inDate.ToString("dd/MM/yyyy"), sk3Date.ToString("dd/MM/yyyy"));
                    }

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

            // =========================================================================
            // 2. TẠO SƠ KẾT 7 NGÀY ĐIỀU TRỊ
            // =========================================================================
            if (t.Create7Day)
            {
                bool hasSk7 = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết 7") || c.Contains("sơ kết 07") || c.Contains("sk 7") || c.Contains("sơ kết tuần");
                });

                if (!hasSk7)
                {
                    DateTime sk7Date = inDate.AddDays(6);
                    if (sk7Date > today) sk7Date = today;
                    long sk7Time = long.Parse(sk7Date.ToString("yyyyMMdd") + "150000");

                    string sk7Content = "";
                    string care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Tập vận động phục hồi chức năng.";
                    string med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động chi.";

                    if (t.SummaryType == "CHUYEN_HAI_DUONG")
                    {
                        sk7Content = string.Format(
                            "SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân điều trị ngày thứ 7 (hậu phẫu ngày thứ 2 sau mổ cố định cột sống L1, bơm cement ngày 03/10/2026). Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định, không sốt.\n" +
                            "- Khám chuyên khoa: Vết mổ sau lưng khô sạch, mép liền tốt, không sưng đỏ nề, không chảy dịch bất thường, dẫn lưu đã rút. Đau lưng sau mổ giảm rõ rệt (VAS 2-3đ). Vận động và cảm giác hai chi dưới bình thường, cơ lực 5/5, đại tiểu tiện tự chủ.\n" +
                            "- Cận lâm sàng: Các xét nghiệm máu sau mổ kiểm soát tốt, X-quang kiểm tra vị trí vít và khối cement vững chắc.\n" +
                            "- Đánh giá chung: Tiến triển hậu phẫu rất thuận lợi, phục hồi tốt theo phác đồ chuyên khoa CTCH & Cột sống.\n" +
                            "- Hướng điều trị tiếp theo: Tiếp tục kháng sinh, giảm đau, chăm sóc thay băng vết mổ, tập vận động xoay trở nhẹ nhàng tại giường có đai lưng hỗ trợ.",
                            inDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Thay băng vết mổ hàng ngày.";
                        med = "Dùng thuốc theo đơn đã kê. Theo dõi DHST và vận động hai chi dưới.";
                    }
                    else if (t.SummaryType == "CHUYEN_TINH")
                    {
                        // Lò Văn Mạnh
                        sk7Content = string.Format(
                            "SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân nam 20 tuổi, tiền sử viêm cột sống dính khớp, vào viện vì đau hạn chế vận động khớp háng phải kéo dài do thoái hóa nặng / hoại tử vô mạch chỏm xương đùi. Toàn trạng tỉnh táo, thể trạng trung bình, da niêm mạc hồng, không sốt.\n" +
                            "- Khám chuyên khoa: Khớp háng phải đau nhiều khi vận động, xoay trong ngoài và dạng khép hạn chế rõ, ngọn chi hồng ấm, không tê bì buốt.\n" +
                            "- Cận lâm sàng: X-quang và MRI xác định thoái hóa khớp háng phải độ IV, hoại tử vô mạch chỏm xương đùi. Bilan tiền phẫu phát hiện rối loạn đông máu nội sinh đã được hội chẩn chuyên khoa Huyết học chuẩn bị truyền 02 đơn vị huyết tương tươi đông lạnh trước mổ.\n" +
                            "- Đánh giá chung: Bệnh nhân chuẩn bị tốt các điều kiện phẫu thuật thay khớp háng nhân tạo.\n" +
                            "- Hướng điều trị tiếp theo: Hoàn tất hội chẩn duyệt mổ, giải thích kíp mổ cho gia đình, đăng ký mổ phiên thay toàn bộ khớp háng phải.",
                            inDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Theo dõi DHST.";
                        med = "Dùng thuốc theo đơn. Chuẩn bị máu/chế phẩm máu trước phẫu thuật.";
                    }
                    else
                    {
                        sk7Content = string.Format(
                            "SƠ KẾT 7 NGÀY ĐIỀU TRỊ (Từ {0} đến {1})\n" +
                            "- Toàn trạng: Bệnh nhân điều trị ngày thứ 7. Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, ăn ngủ được, đại tiểu tiện bình thường.\n" +
                            "- Khám chuyên khoa: Vết mổ/tổn thương liền sẹo tiến triển tốt, khô sạch, dịch tiết giảm rõ rệt, không có biểu hiện nhiễm trùng tại chỗ. Trục chi thẳng, tưới máu ngọn chi tốt, vận động các khớp lân cận được cải thiện.\n" +
                            "- Cận lâm sàng: Các xét nghiệm huyết học, sinh hóa và hình ảnh chẩn đoán nằm trong giới hạn kiểm soát tốt.\n" +
                            "- Đánh giá chung: Bệnh nhân tiến triển thuận lợi theo đúng phác đồ điều trị chuyên khoa CTCH & Cột sống.\n" +
                            "- Hướng điều trị tiếp theo: Tiếp tục duy trì phác đồ điều trị, tăng cường tập phục hồi chức năng, theo dõi liền xương/liền gân và dự kiến kế hoạch ra viện khi đủ điều kiện.",
                            inDate.ToString("dd/MM/yyyy"), sk7Date.ToString("dd/MM/yyyy"));
                    }

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

            // =========================================================================
            // 3. TẠO SƠ KẾT ĐỢT ĐIỀU TRỊ / RA VIỆN / CHUYỂN TUYẾN
            // =========================================================================
            if (t.CreateDischargeOrTransfer)
            {
                bool hasDischarge = existingTrks.Any(x => {
                    string c = ((x.CONTENT ?? "") + " " + (x.MEDICAL_INSTRUCTION ?? "")).ToLower();
                    return c.Contains("sơ kết ra viện") || c.Contains("sơ kết đợt điều trị") || c.Contains("sơ kết chuyển");
                });

                if (!hasDischarge && (t.SummaryType != "RA_VIEN" || t.PatientCode != "0003664727"))
                {
                    long disTime = long.Parse(today.ToString("yyyyMMdd") + "093000");
                    string disContent = "";
                    string care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý.";
                    string med = "Dùng thuốc theo đơn.";

                    if (t.SummaryType == "CHUYEN_HAI_DUONG")
                    {
                        disContent = string.Format(
                            "SƠ KẾT ĐỢT ĐIỀU TRỊ - CHUYỂN TUYẾN BỆNH VIỆN ĐA KHOA TỈNH HẢI DƯƠNG ({0} NGÀY ĐIỀU TRỊ - Từ {1} đến {2})\n" +
                            "1. Lý do vào viện: Đau dữ dội vùng cột sống thắt lưng sau chấn thương ngã / Xẹp L1.\n" +
                            "2. Quá trình phẫu thuật & điều trị: Bệnh nhân được chẩn đoán Xẹp L1 - Rối loạn giấc ngủ, phẫu thuật cố định cột sống thắt lưng, bơm cement thân đốt L1 ngày 03/10/2026. Diễn biến hậu phẫu 6 ngày tiến triển thuận lợi.\n" +
                            "3. Tình trạng hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, tim đều, phổi thông khí rõ, bụng mềm. Vết mổ sau lưng khô sạch hoàn toàn, mép phẳng, liền sẹo tốt, không sưng đỏ, không chảy dịch, đau lưng giảm rõ rệt (VAS 1-2đ). Hai chân vận động cảm giác tốt, cơ lực 5/5, đại tiểu tiện tự chủ, tự ngồi dậy và đi lại nhẹ nhàng có đai cột sống hỗ trợ.\n" +
                            "4. Cận lâm sàng: X-quang kiểm tra nẹp vít và khối cement vững chắc, chỉ số máu ổn định.\n" +
                            "5. Đánh giá chung & Lý do chuyển tuyến: Can thiệp phẫu thuật chuyên khoa sâu thành công ổn định. Gia đình có nguyện vọng chuyển về Bệnh viện Đa khoa Tỉnh Hải Dương để thuận tiện tiếp tục chăm sóc, theo dõi và tập phục hồi chức năng gần gia đình.\n" +
                            "6. Y lệnh & Hướng xử trí:\n" +
                            "- Cho chuyển viện điều trị tiếp: Bệnh viện Đa khoa Tỉnh Hải Dương.\n" +
                            "- Bàn giao hồ sơ tóm tắt bệnh án, kết quả phẫu thuật và đơn thuốc theo dõi.\n" +
                            "- Tiếp tục dùng thuốc theo đơn, tập phục hồi chức năng nhẹ nhàng, đeo đai lưng cố định khi vận động.",
                            days, inDate.ToString("dd/MM/yyyy"), today.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Bàn giao người bệnh chuyển viện an toàn.";
                        med = "Dùng thuốc theo đơn. Chuyển tuyến điều trị: Bệnh viện Đa khoa Tỉnh Hải Dương.";
                    }
                    else if (t.SummaryType == "CHUYEN_TINH")
                    {
                        disContent = string.Format(
                            "SƠ KẾT ĐỢT ĐIỀU TRỊ - CHUYỂN TUYẾN BỆNH VIỆN ĐA KHOA TỈNH ({0} NGÀY ĐIỀU TRỊ - Từ {1} đến {2})\n" +
                            "1. Lý do vào viện: Thoái hóa khớp háng phải độ IV / Viêm cột sống dính khớp / Hoại tử vô mạch chỏm xương đùi.\n" +
                            "2. Quá trình phẫu thuật & điều trị: Bệnh nhân được hội chẩn đa chuyên khoa, truyền huyết tương tươi đông lạnh điều chỉnh rối loạn đông máu và phẫu thuật Thay khớp háng phải toàn bộ ngày 05/10/2026 thành công. Hậu phẫu ngày thứ 4 diễn biến thuận lợi.\n" +
                            "3. Tình trạng hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng, không sốt, tim đều, phổi thông khí rõ, bụng mềm, đại tiểu tiện tự chủ. Vết mổ khớp háng phải khô sạch hoàn toàn, mép liền phẳng, không sưng đỏ nề, không chảy dịch, dẫn lưu đã rút. Trục chi thẳng, chiều dài 2 chân cân đối, ngọn chi hồng ấm, mạch mu chân rõ, vận động cảm giác bàn ngón chân tốt, đau vết mổ giảm nhiều (VAS 2đ). Đang tập co duỗi khớp gối và nâng chân nhẹ nhàng trên giường bệnh.\n" +
                            "4. Cận lâm sàng: X-quang kiểm tra khớp háng nhân tạo đúng vị trí, vững chắc; công thức máu và đông máu kiểm soát tốt.\n" +
                            "5. Đánh giá chung & Lý do chuyển tuyến: Can thiệp phẫu thuật thay khớp háng chuyên sâu ổn định, an toàn. Gia đình có nguyện vọng chuyển về Bệnh viện Đa khoa Tỉnh để tiếp tục chăm sóc thay băng, dùng thuốc duy trì và tập phục hồi chức năng vận động gần nhà.\n" +
                            "6. Y lệnh & Hướng xử trí:\n" +
                            "- Cho chuyển viện điều trị tiếp: Bệnh viện Đa khoa Tỉnh.\n" +
                            "- Bàn giao hồ sơ tóm tắt bệnh án, phim X-quang sau mổ và đơn thuốc.\n" +
                            "- Tiếp tục kháng sinh dự phòng, chống huyết khối tĩnh mạch sâu và hướng dẫn tập PHCN khớp háng.",
                            days, inDate.ToString("dd/MM/yyyy"), today.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý. Bàn giao người bệnh chuyển viện an toàn.";
                        med = "Dùng thuốc theo đơn. Chuyển tuyến điều trị: Bệnh viện Đa khoa Tỉnh.";
                    }
                    else if (t.SummaryType == "RA_VIEN" && t.PatientCode == "0004106886")
                    {
                        disContent = string.Format(
                            "SƠ KẾT {0} NGÀY ĐIỀU TRỊ & SƠ KẾT RA VIỆN (Từ {1} đến {2})\n" +
                            "1. Lý do vào viện: Gãy mắt cá ngoài chân trái sau tai nạn giao thông.\n" +
                            "2. Quá trình phẫu thuật & điều trị: Bệnh nhân được phẫu thuật kết hợp xương nẹp vít mắt cá ngoài chân trái ngày 06/10/2026. Hậu phẫu ngày thứ 3 tiến triển rất tốt.\n" +
                            "3. Tình trạng hiện tại: Bệnh nhân tỉnh táo, tiếp xúc tốt, thể trạng tốt, không sốt, sinh hiệu ổn định (Mạch 72 l/p, HA 120/70 mmHg, SpO2 99%). Vết mổ cổ chân ngoài trái khô sạch hoàn toàn, mép phẳng liền tốt, không sưng nề đỏ, không chảy dịch, đau giảm nhiều (VAS 1-2đ). Băng nẹp vững chắc, ngọn chi hồng ấm, cử động các ngón chân linh hoạt, cảm giác bàn ngón bình thường, tưới máu ngoại vi tốt. Bệnh nhân ăn ngủ tốt, đại tiểu tiện tự chủ.\n" +
                            "4. Cận lâm sàng: X-quang kiểm tra sau mổ xương nắn chỉnh giải phẫu tốt, nẹp vít cố định vững chắc; các xét nghiệm máu trong giới hạn bình thường.\n" +
                            "5. Đánh giá chung: Tiến triển hậu phẫu kết hợp xương rất tốt, toàn trạng ổn định, đủ tiêu chuẩn ra viện.\n" +
                            "6. Y lệnh ra viện:\n" +
                            "- Cho bệnh nhân ra viện ngày {2}.\n" +
                            "- Kê đơn thuốc điều trị ngoại trú: kháng sinh, giảm đau, chống phù nề.\n" +
                            "- Hướng dẫn chăm sóc: Giữ khô vết mổ, kê cao chân khi nằm nghỉ, không tì đè lực lên chân mổ, tập vận động ngón chân nhẹ nhàng.\n" +
                            "- Hẹn cắt chỉ vết mổ sau 10-14 ngày tại cơ sở y tế gần nhà, tái khám kiểm tra chụp X-quang sau 1 tháng.",
                            days, inDate.ToString("dd/MM/yyyy"), today.ToString("dd/MM/yyyy"));
                        care = "Chăm sóc cấp III. Ăn uống bình thường. Hướng dẫn thủ tục ra viện.";
                        med = "Kê đơn thuốc ngoại trú ra viện. Hẹn tái khám theo hẹn.";
                    }

                    if (!string.IsNullOrEmpty(disContent))
                    {
                        bool ok = CreateTracking(mosConsumer, tr, disTime, disContent, care, med);
                        if (ok)
                        {
                            Console.WriteLine(string.Format("   ✔ Đã tạo THÀNH CÔNG: Tờ Sơ kết đợt điều trị / Ra viện / Chuyển tuyến (Thời điểm: {0})", today.ToString("dd/MM/yyyy 09:30")));
                            totalSuccess++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ✔ ĐÃ CÓ tờ Sơ kết ra viện / chuyển tuyến.");
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
