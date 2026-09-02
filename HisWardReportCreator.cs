using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
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

public class PatientWardRecord
{
    public string RoomName { get; set; }
    public string BedName { get; set; }
    public long TreatmentId { get; set; }
    public string TreatmentCode { get; set; }
    public string PatientCode { get; set; }
    public string PatientName { get; set; }
    public string GenderName { get; set; }
    public string AgeStr { get; set; }
    public string InTimeStr { get; set; }
    public string IcdCode { get; set; }
    public string IcdName { get; set; }
    public string IcdText { get; set; }
    public string Pulse { get; set; }
    public string BloodPressure { get; set; }
    public string Temperature { get; set; }
    public string SpO2 { get; set; }
    public bool HasTrackingToday { get; set; }
    public string TodayTrackingTime { get; set; }
    public string TodayTrackingContent { get; set; }
    public bool HasPrescriptionToday { get; set; }
    public List<string> TodayMeds { get; set; }
    public List<string> TodayCls { get; set; }
    public string TodayRation { get; set; }
    public List<string> ActionBadges { get; set; }

    public PatientWardRecord()
    {
        TodayMeds = new List<string>();
        TodayCls = new List<string>();
        ActionBadges = new List<string>();
    }
}

public class HisWardReportCreator
{
    public static string ReadLiveToken()
    {
        string[] candidateLogs = new string[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "HLSLogSystem.txt"),
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };

        foreach (var logFile in candidateLogs)
        {
            if (File.Exists(logFile))
            {
                try
                {
                    using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string text = sr.ReadToEnd();
                        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (lines[i].Contains("TokenCode|"))
                            {
                                int idx = lines[i].IndexOf("TokenCode|") + 10;
                                if (lines[i].Length >= idx + 64)
                                {
                                    return lines[i].Substring(idx, 64);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }
        return null;
    }

    public static string InitSession(ref ApiConsumer mosConsumer, ref CommonParam param, ref MyAdapter adapter)
    {
        string token = ReadLiveToken();
        param = new CommonParam();
        adapter = new MyAdapter();

        if (!string.IsNullOrEmpty(token))
        {
            mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            try
            {
                HisBedRoomViewFilter testBf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
                var testRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, testBf, param);
                if (testRooms != null && testRooms.Count > 0)
                {
                    return token;
                }
            }
            catch { }
        }

        // Live token is empty or invalid, fallback to direct ACS login
        token = null;
        try
        {
            Load.Init();
            try
            {
                var constType = typeof(ClientTokenManager).Assembly.GetType("Inventec.Token.ClientSystem.Constants");
                var fBase = constType.GetField("BASE_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fBase != null) fBase.SetValue(null, "http://192.168.7.200:1401/");
                var fLogin = constType.GetField("LOGIN_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fLogin != null) fLogin.SetValue(null, "api/Token/Login");
            }
            catch { }

            ClientTokenManager tokenManager = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");
            var loginToken = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (loginToken == null)
            {
                param = new CommonParam();
                loginToken = tokenManager.Login(param, "034727", "9981", "2.390.0");
            }

            if (loginToken != null && !string.IsNullOrEmpty(loginToken.TokenCode))
            {
                token = loginToken.TokenCode;
                mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

                try
                {
                    var workInfo = new WorkInfoSDO
                    {
                        Rooms = new List<RoomSDO>
                        {
                            new RoomSDO { RoomId = 5248 },
                            new RoomSDO { RoomId = 5252 },
                            new RoomSDO { RoomId = 5251 },
                            new RoomSDO { RoomId = 5257 }
                        }
                    };
                    adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
                }
                catch { }

                return token;
            }
            else
            {
                Console.WriteLine("❌ Đăng nhập ACS thất bại!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Fallback login error: " + ex.Message);
        }

        return token;
    }

    public static void GenerateReport(string roomFilter = "712,714,716,724,725", bool openBrowser = false)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ApiConsumer mosConsumer = null;
        CommonParam param = null;
        MyAdapter adapter = null;

        string token = InitSession(ref mosConsumer, ref param, ref adapter);
        if (string.IsNullOrEmpty(token) || mosConsumer == null)
        {
            Console.WriteLine("❌ Không khởi tạo được phiên làm việc HIS!");
            return;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 ĐANG TỔNG HỢP BÁO CÁO BUỒNG BỆNH KHOA CTCH & CỘT SỐNG (KHOA 57)");
        Console.WriteLine("Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        Console.WriteLine("===============================================================================");

        HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
        var allRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
        if (allRooms == null || allRooms.Count == 0)
        {
            Console.WriteLine("❌ Không tải được danh sách buồng bệnh Khoa 57!");
            return;
        }

        List<V_HIS_BED_ROOM> targetRooms;
        if (roomFilter.ToLower() == "all")
        {
            targetRooms = allRooms.OrderBy(x => x.BED_ROOM_NAME).ToList();
        }
        else
        {
            var filters = roomFilter.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            targetRooms = allRooms.Where(r => filters.Any(f => r.BED_ROOM_NAME != null && r.BED_ROOM_NAME.Contains(f))).OrderBy(x => x.BED_ROOM_NAME).ToList();
        }

        Console.WriteLine(string.Format("Đang quét {0} buồng bệnh được chỉ định...\n", targetRooms.Count));

        List<PatientWardRecord> records = new List<PatientWardRecord>();
        DateTime today = DateTime.Today;
        long todayStart = long.Parse(today.ToString("yyyyMMdd") + "000000");

        foreach (var room in targetRooms)
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
            if (inPatients == null || inPatients.Count == 0) continue;

            foreach (var p in inPatients.OrderBy(x => x.BED_NAME))
            {
                long tId = p.TREATMENT_ID;
                PatientWardRecord rec = new PatientWardRecord
                {
                    RoomName = room.BED_ROOM_NAME,
                    BedName = p.BED_NAME,
                    TreatmentId = tId,
                    TreatmentCode = p.TREATMENT_CODE,
                    PatientCode = p.TDL_PATIENT_CODE,
                    PatientName = p.TDL_PATIENT_NAME
                };

                // 1. Chi tiết Treatment
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (trList != null && trList.Count > 0)
                {
                    var tr = trList[0];
                    rec.GenderName = tr.TDL_PATIENT_GENDER_NAME;
                    if (tr.TDL_PATIENT_DOB > 0)
                    {
                        string dobStr = tr.TDL_PATIENT_DOB.ToString();
                        if (dobStr.Length >= 4)
                        {
                            int birthYear = int.Parse(dobStr.Substring(0, 4));
                            rec.AgeStr = (DateTime.Now.Year - birthYear).ToString();
                        }
                    }
                    if (tr.IN_TIME > 0)
                    {
                        string s = tr.IN_TIME.ToString();
                        if (s.Length == 14)
                        {
                            rec.InTimeStr = string.Format("{0}/{1}/{2}", s.Substring(6, 2), s.Substring(4, 2), s.Substring(0, 4));
                        }
                    }

                    rec.IcdCode = tr.ICD_CODE;
                    rec.IcdName = tr.ICD_NAME;
                    rec.IcdText = tr.ICD_TEXT;
                }

                // 2. Dấu hiệu sinh tồn (DHST)
                HisDhstViewFilter dhf = new HisDhstViewFilter { TREATMENT_ID = tId };
                var dhList = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhf, param);
                if (dhList != null && dhList.Count > 0)
                {
                    var lastDh = dhList.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                    if (lastDh.PULSE.HasValue) rec.Pulse = lastDh.PULSE.Value.ToString();
                    if (lastDh.BLOOD_PRESSURE_MAX.HasValue && lastDh.BLOOD_PRESSURE_MIN.HasValue)
                        rec.BloodPressure = string.Format("{0}/{1}", lastDh.BLOOD_PRESSURE_MAX, lastDh.BLOOD_PRESSURE_MIN);
                    if (lastDh.TEMPERATURE.HasValue) rec.Temperature = lastDh.TEMPERATURE.Value.ToString("0.#");
                    if (lastDh.SPO2.HasValue) rec.SpO2 = lastDh.SPO2.Value.ToString();
                }

                // 3. Tờ điều trị hôm nay
                HisTrackingViewFilter trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);
                if (trkList != null && trkList.Count > 0)
                {
                    var todayTrk = trkList.Where(x => x.TRACKING_TIME >= todayStart).OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
                    if (todayTrk != null)
                    {
                        rec.HasTrackingToday = true;
                        string s = todayTrk.TRACKING_TIME.ToString();
                        rec.TodayTrackingTime = string.Format("{0}:{1}", s.Substring(8, 2), s.Substring(10, 2));
                        rec.TodayTrackingContent = todayTrk.CONTENT != null ? todayTrk.CONTENT.Replace("\r\n", " ").Replace("\n", " ") : "";
                    }
                }

                // 4. Đơn thuốc & Y lệnh hôm nay
                HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
                var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                if (srs != null)
                {
                    var todaySrs = srs.Where(x => x.INTRUCTION_TIME >= todayStart).ToList();
                    var presSrs = todaySrs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList(); // Kê đơn
                    if (presSrs.Count > 0) rec.HasPrescriptionToday = true;

                    // Lấy chi tiết thuốc / CLS
                    HisSereServViewFilter ssf = new HisSereServViewFilter { TREATMENT_ID = tId };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        var todaySss = sss.Where(x => x.TDL_INTRUCTION_TIME >= todayStart).ToList();
                        foreach (var item in todaySss)
                        {
                            if (item.TDL_SERVICE_TYPE_ID == 6) // Thuốc
                            {
                                rec.TodayMeds.Add(string.Format("{0} ({1:0.##})", item.TDL_SERVICE_NAME, item.AMOUNT));
                            }
                            else if (item.TDL_SERVICE_TYPE_ID == 2 || item.TDL_SERVICE_TYPE_ID == 3 || item.TDL_SERVICE_TYPE_ID == 4) // XN / CDHA / TDCN
                            {
                                rec.TodayCls.Add(item.TDL_SERVICE_NAME);
                            }
                            else if (item.TDL_SERVICE_TYPE_ID == 14) // Suất ăn
                            {
                                rec.TodayRation = item.TDL_SERVICE_NAME;
                            }
                        }
                    }
                }

                // Đánh giá Badges
                if (!rec.HasTrackingToday) rec.ActionBadges.Add("🔴 Chưa có Tờ ĐT hôm nay");
                if (!rec.HasPrescriptionToday) rec.ActionBadges.Add("🔴 Chưa kê đơn thuốc hôm nay");
                if (rec.IcdName != null && (rec.IcdName.ToLower().Contains("tháo đường") || (rec.IcdText != null && rec.IcdText.ToLower().Contains("tháo đường"))))
                {
                    rec.ActionBadges.Add("🟠 BN Đái tháo đường (Theo dõi ĐH & Insulin)");
                }

                // Kiểm tra Hội chẩn chuyên khoa / Biên bản hội chẩn
                HisDebateFilter debFilter = new HisDebateFilter { TREATMENT_ID = tId };
                var debList = adapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, debFilter, param);
                if (debList != null && debList.Count > 0)
                {
                    var consultSrs = srs != null ? srs.Where(x => x.SERVICE_REQ_TYPE_ID == 1 || (x.EXECUTE_DEPARTMENT_NAME != null && (x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hô hấp") || x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("truyền nhiễm") || x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nhiệt đới")))).ToList() : new List<V_HIS_SERVICE_REQ>();
                    int compCount = consultSrs.Count(x => x.SERVICE_REQ_STT_ID == 3);
                    int waitCount = consultSrs.Count(x => x.SERVICE_REQ_STT_ID != 3);
                    if (compCount > 0) rec.ActionBadges.Add(string.Format("🔵 Đã có KQ Hội chẩn ({0} CK)", compCount));
                    if (waitCount > 0) rec.ActionBadges.Add(string.Format("🟡 Chờ ý kiến Hội chẩn ({0} CK)", waitCount));
                    if (consultSrs.Count == 0) rec.ActionBadges.Add("🔵 Có Biên bản Hội chẩn");
                }

                if (rec.HasTrackingToday && rec.HasPrescriptionToday)
                {
                    rec.ActionBadges.Add("🟢 Đã hoàn tất y lệnh ngày");
                }

                records.Add(rec);
                Console.WriteLine(string.Format("  ✔ [{0} - {1}] {2} (Mã: {3})", rec.RoomName, rec.BedName, rec.PatientName, rec.PatientCode));
            }
        }

        Console.WriteLine(string.Format("\n📊 Tổng cộng: {0} bệnh nhân đang nằm viện.", records.Count));

        // 5. Xuất báo cáo Markdown và HTML
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dateTitle = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        string projectDir = AppDomain.CurrentDomain.BaseDirectory;
        string reportFolder1 = Path.Combine(projectDir, "Reports", "WardReports");
        if (!Directory.Exists(reportFolder1)) Directory.CreateDirectory(reportFolder1);

        string oneDriveDir = @"C:\Users\1995\OneDrive\BaoCaoBuongBenh_Khoa57";
        if (!Directory.Exists(oneDriveDir))
        {
            try { Directory.CreateDirectory(oneDriveDir); } catch { }
        }

        string mdContent = GenerateMarkdown(records, dateTitle);
        string htmlContent = GenerateHtml(records, dateTitle);

        string mdPath1 = Path.Combine(reportFolder1, string.Format("BaoCao_BuongBenh_{0}.md", timeStamp));
        string htmlPath1 = Path.Combine(reportFolder1, string.Format("BaoCao_BuongBenh_{0}.html", timeStamp));
        File.WriteAllText(mdPath1, mdContent, Encoding.UTF8);
        File.WriteAllText(htmlPath1, htmlContent, Encoding.UTF8);

        if (Directory.Exists(oneDriveDir))
        {
            string mdPath2 = Path.Combine(oneDriveDir, string.Format("BaoCao_BuongBenh_{0}.md", timeStamp));
            string htmlPath2 = Path.Combine(oneDriveDir, string.Format("BaoCao_BuongBenh_{0}.html", timeStamp));
            try
            {
                File.WriteAllText(mdPath2, mdContent, Encoding.UTF8);
                File.WriteAllText(htmlPath2, htmlContent, Encoding.UTF8);
                Console.WriteLine("📁 Đã đồng bộ sang Thư mục Drive: " + htmlPath2);
            }
            catch { }
        }

        Console.WriteLine("📁 Đã lưu Báo cáo Buồng bệnh tại: " + htmlPath1);
        Console.WriteLine("===============================================================================");

        if (openBrowser)
        {
            try { System.Diagnostics.Process.Start(htmlPath1); } catch { }
        }
    }

    public static string GenerateMarkdown(List<PatientWardRecord> records, string dateTitle)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Format("# 🏥 BÁO CÁO BUỒNG BỆNH KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57)"));
        sb.AppendLine(string.Format("*Thời điểm xuất báo cáo: {0} | Bác sĩ: Ths.BS Nguyễn Hữu Sâm (034727)*\n", dateTitle));
        sb.AppendLine(string.Format("**Tổng số bệnh nhân**: {0} người bệnh\n", records.Count));
        sb.AppendLine("| STT | Buồng - Giường | Mã BN / Mã ĐT | Họ và tên | Tuổi | Chẩn đoán chi tiết & ICD | DHST | Tờ ĐT Hôm nay | Đơn thuốc / Suất ăn | Cảnh báo & Trạng thái |");
        sb.AppendLine("| :---: | :--- | :---: | :--- | :---: | :--- | :--- | :--- | :--- | :--- |");

        int idx = 1;
        foreach (var r in records)
        {
            string dhst = string.Format("M:{0} HA:{1} T:{2} SpO2:{3}", r.Pulse ?? "-", r.BloodPressure ?? "-", r.Temperature ?? "-", r.SpO2 ?? "-");
            string trk = r.HasTrackingToday ? string.Format("✅ {0} ({1})", r.TodayTrackingTime, r.TodayTrackingContent) : "❌ Chưa tạo";
            string meds = r.HasPrescriptionToday ? string.Format("✅ {0} loại thuốc", r.TodayMeds.Count) : "❌ Chưa kê";
            if (!string.IsNullOrEmpty(r.TodayRation)) meds += string.Format("<br>🍚 {0}", r.TodayRation);
            string diag = string.Format("[{0}] {1} {2}", r.IcdCode, r.IcdName, !string.IsNullOrEmpty(r.IcdText) ? "(" + r.IcdText + ")" : "");
            string badges = string.Join("<br>", r.ActionBadges);

            sb.AppendLine(string.Format("| {0} | **{1}**<br>{2} | `{3}`<br>Tr:`{4}` | **{5}** ({6}) | {7} | {8} | {9} | {10} | {11} | {12} |",
                idx++, r.RoomName, r.BedName, r.PatientCode, r.TreatmentId, r.PatientName, r.GenderName ?? "-", r.AgeStr ?? "-", diag, dhst, trk, meds, badges));
        }

        return sb.ToString();
    }

    public static string GenerateHtml(List<PatientWardRecord> records, string dateTitle)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>Báo Cáo Buồng Bệnh - Khoa CTCH & Cột Sống</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background-color: #f4f6f9; color: #333; }");
        sb.AppendLine("    .container { max-width: 1600px; margin: 0 auto; background: #fff; padding: 25px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }");
        sb.AppendLine("    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #2b579a; padding-bottom: 15px; margin-bottom: 20px; }");
        sb.AppendLine("    .header h1 { margin: 0; color: #2b579a; font-size: 24px; }");
        sb.AppendLine("    .stats { display: flex; gap: 15px; margin-bottom: 20px; }");
        sb.AppendLine("    .card { flex: 1; padding: 15px; border-radius: 6px; background: #e8f0fe; border-left: 4px solid #1a73e8; }");
        sb.AppendLine("    .card.alert { background: #fce8e6; border-left-color: #d93025; }");
        sb.AppendLine("    .card.success { background: #e6f4ea; border-left-color: #1e8e3e; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 13px; }");
        sb.AppendLine("    th { background: #2b579a; color: #fff; text-align: left; padding: 10px 8px; border: 1px solid #1e3f73; }");
        sb.AppendLine("    td { padding: 8px; border: 1px solid #e0e0e0; vertical-align: top; }");
        sb.AppendLine("    tr:nth-child(even) { background-color: #f8f9fa; }");
        sb.AppendLine("    tr:hover { background-color: #eef3fc; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 3px 6px; border-radius: 4px; font-size: 11px; font-weight: bold; margin-bottom: 2px; }");
        sb.AppendLine("    .badge-danger { background: #fce8e6; color: #c5221f; border: 1px solid #fad2cf; }");
        sb.AppendLine("    .badge-warning { background: #fef7e0; color: #b06000; border: 1px solid #feefc3; }");
        sb.AppendLine("    .badge-success { background: #e6f4ea; color: #137333; border: 1px solid #ceead6; }");
        sb.AppendLine("    .room-tag { font-weight: bold; color: #1a73e8; }");
        sb.AppendLine("    .search-box { width: 100%; padding: 10px; margin-bottom: 15px; border: 1px solid #ccc; border-radius: 4px; box-sizing: border-box; font-size: 14px; }");
        sb.AppendLine("    @media print { .search-box, .no-print { display: none; } body { background: #fff; margin: 0; } .container { box-shadow: none; padding: 0; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <h1>🏥 BÁO CÁO BUỒNG BỆNH KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57)</h1>");
        sb.AppendLine(string.Format("        <p style=\"margin: 5px 0 0 0; color: #666;\">Thời điểm xuất: <b>{0}</b> | Bác sĩ phụ trách: <b>Ths.BS Nguyễn Hữu Sâm (034727)</b></p>", dateTitle));
        sb.AppendLine("      </div>");
        sb.AppendLine("      <button onclick=\"window.print()\" style=\"padding: 8px 16px; background: #2b579a; color: #fff; border: none; border-radius: 4px; cursor: pointer;\">🖨️ In Báo Cáo / PDF</button>");
        sb.AppendLine("    </div>");

        int missingTrk = records.Count(x => !x.HasTrackingToday);
        int missingPres = records.Count(x => !x.HasPrescriptionToday);
        int completed = records.Count(x => x.HasTrackingToday && x.HasPrescriptionToday);

        sb.AppendLine("    <div class=\"stats\">");
        sb.AppendLine(string.Format("      <div class=\"card\"><b>Tổng số Bệnh nhân</b><br><span style=\"font-size: 22px; font-weight: bold;\">{0}</span></div>", records.Count));
        sb.AppendLine(string.Format("      <div class=\"card success\"><b>Đã xong Y lệnh ngày</b><br><span style=\"font-size: 22px; font-weight: bold; color: #1e8e3e;\">{0}</span></div>", completed));
        sb.AppendLine(string.Format("      <div class=\"card alert\"><b>Chưa tạo Tờ điều trị</b><br><span style=\"font-size: 22px; font-weight: bold; color: #d93025;\">{0}</span></div>", missingTrk));
        sb.AppendLine(string.Format("      <div class=\"card alert\"><b>Chưa kê Đơn thuốc</b><br><span style=\"font-size: 22px; font-weight: bold; color: #d93025;\">{0}</span></div>", missingPres));
        sb.AppendLine("    </div>");

        sb.AppendLine("    <input type=\"text\" id=\"search\" class=\"search-box\" placeholder=\"🔍 Gõ tìm kiếm theo tên bệnh nhân, buồng phòng, mã BN, chẩn đoán...\" onkeyup=\"filterTable()\">");

        sb.AppendLine("    <table id=\"reportTable\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("          <th style=\"width: 30px;\">STT</th>");
        sb.AppendLine("          <th style=\"width: 120px;\">Buồng - Giường</th>");
        sb.AppendLine("          <th style=\"width: 100px;\">Mã BN / Mã ĐT</th>");
        sb.AppendLine("          <th style=\"width: 150px;\">Họ và tên</th>");
        sb.AppendLine("          <th style=\"width: 50px;\">Tuổi</th>");
        sb.AppendLine("          <th>Chẩn đoán & Tổn thương chi tiết</th>");
        sb.AppendLine("          <th style=\"width: 110px;\">DHST</th>");
        sb.AppendLine("          <th style=\"width: 160px;\">Tờ ĐT Hôm nay</th>");
        sb.AppendLine("          <th style=\"width: 180px;\">Đơn thuốc / Suất ăn</th>");
        sb.AppendLine("          <th style=\"width: 160px;\">Trạng thái & Cảnh báo</th>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        int stt = 1;
        foreach (var r in records)
        {
            sb.AppendLine("        <tr>");
            sb.AppendLine(string.Format("          <td style=\"text-align: center;\">{0}</td>", stt++));
            sb.AppendLine(string.Format("          <td><span class=\"room-tag\">{0}</span><br><b>{1}</b></td>", r.RoomName, r.BedName));
            sb.AppendLine(string.Format("          <td><code>{0}</code><br><small>Tr: {1}</small></td>", r.PatientCode, r.TreatmentId));
            sb.AppendLine(string.Format("          <td><b>{0}</b><br><small>({1}) Vào: {2}</small></td>", r.PatientName, r.GenderName ?? "-", r.InTimeStr ?? "-"));
            sb.AppendLine(string.Format("          <td style=\"text-align: center;\">{0}</td>", r.AgeStr ?? "-"));
            sb.AppendLine(string.Format("          <td><b>[{0}] {1}</b><br><small style=\"color: #666;\">{2}</small></td>", r.IcdCode, r.IcdName, r.IcdText ?? ""));
            sb.AppendLine(string.Format("          <td><small>Mạch: <b>{0}</b><br>HA: <b>{1}</b><br>NĐ: <b>{2}</b><br>SpO2: <b>{3}%</b></small></td>", r.Pulse ?? "-", r.BloodPressure ?? "-", r.Temperature ?? "-", r.SpO2 ?? "-"));
            
            if (r.HasTrackingToday)
            {
                sb.AppendLine(string.Format("          <td><span class=\"badge badge-success\">✔ {0}</span><br><small>{1}</small></td>", r.TodayTrackingTime, r.TodayTrackingContent));
            }
            else
            {
                sb.AppendLine("          <td><span class=\"badge badge-danger\">❌ Chưa tạo tờ ĐT</span></td>");
            }

            if (r.HasPrescriptionToday)
            {
                string medsList = string.Join(", ", r.TodayMeds.Take(3));
                if (r.TodayMeds.Count > 3) medsList += string.Format(" (+{0} thuốc)", r.TodayMeds.Count - 3);
                string ration = !string.IsNullOrEmpty(r.TodayRation) ? string.Format("<br><small>🍚 {0}</small>", r.TodayRation) : "";
                sb.AppendLine(string.Format("          <td><span class=\"badge badge-success\">✔ {0} thuốc</span><br><small>{1}</small>{2}</td>", r.TodayMeds.Count, medsList, ration));
            }
            else
            {
                sb.AppendLine("          <td><span class=\"badge badge-danger\">❌ Chưa kê đơn thuốc</span></td>");
            }

            sb.AppendLine("          <td>");
            foreach (var b in r.ActionBadges)
            {
                string clsName = b.StartsWith("🔴") ? "badge-danger" : (b.StartsWith("🟠") ? "badge-warning" : "badge-success");
                sb.AppendLine(string.Format("            <div class=\"badge {0}\">{1}</div>", clsName, b));
            }
            sb.AppendLine("          </td>");
            sb.AppendLine("        </tr>");
        }

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <script>");
        sb.AppendLine("    function filterTable() {");
        sb.AppendLine("      var input = document.getElementById('search');");
        sb.AppendLine("      var filter = input.value.toLowerCase();");
        sb.AppendLine("      var trs = document.getElementById('reportTable').getElementsByTagName('tr');");
        sb.AppendLine("      for (var i = 1; i < trs.Length; i++) {");
        sb.AppendLine("        var text = trs[i].textContent || trs[i].innerText;");
        sb.AppendLine("        trs[i].style.display = text.toLowerCase().indexOf(filter) > -1 ? '' : 'none';");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public static void Run(string[] args)
    {
        string rooms = "712,714,716,724,725";
        bool open = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == "--all" || args[i].ToLower() == "-a")
            {
                rooms = "all";
            }
            else if ((args[i].ToLower() == "--room" || args[i].ToLower() == "-r") && i + 1 < args.Length)
            {
                rooms = args[i + 1];
                i++;
            }
            else if (args[i].ToLower() == "--open" || args[i].ToLower() == "-o")
            {
                open = true;
            }
        }

        GenerateReport(rooms, open);
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
            
            DirectoryInfo cur = new DirectoryInfo(folderPath);
            for (int i = 0; i < 5; i++)
            {
                if (cur.Parent == null) break;
                cur = cur.Parent;
                string pRoot = Path.Combine(cur.FullName, name);
                if (File.Exists(pRoot)) return Assembly.LoadFrom(pRoot);
                string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                if (File.Exists(pRef)) return Assembly.LoadFrom(pRef);
            }
            return null;
        };

        DirectoryInfo rootDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (rootDir != null && !File.Exists(Path.Combine(rootDir.FullName, "Inventec.Core.dll")))
        {
            rootDir = rootDir.Parent;
        }
        if (rootDir != null)
        {
            Directory.SetCurrentDirectory(rootDir.FullName);
        }

        HisWardReportCreator.Run(args);
    }
}
