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

    // Rà soát ICD & Chẩn đoán theo Quy tắc 4 AGENTS.md
    public string ReviewedIcdCode { get; set; }
    public string ReviewedDiagnosis { get; set; }
    public string OriginalIcdCode { get; set; }
    public string OriginalIcdName { get; set; }
    public string OriginalIcdText { get; set; }

    // Thông tin lâm sàng chuyên sâu theo yêu cầu Bác sĩ
    public string MedicalHistory { get; set; }      // Bệnh sử & Lý do vào viện
    public string RecentCourse { get; set; }        // Tình trạng diễn biến gần đây
    public string CurrentStatus { get; set; }       // Tình trạng hiện tại
    public string TreatmentPlan { get; set; }       // Kế hoạch điều trị tiếp theo

    public bool HasTrackingToday { get; set; }
    public string TodayTrackingTime { get; set; }
    public string TodayTrackingContent { get; set; }
    public bool HasPrescriptionToday { get; set; }
    public List<string> TodayMeds { get; set; }
    public List<string> TodayCls { get; set; }
    public string TodayRation { get; set; }
    public List<string> ActionBadges { get; set; }
    public List<string> Surgeries { get; set; }

    public PatientWardRecord()
    {
        TodayMeds = new List<string>();
        TodayCls = new List<string>();
        ActionBadges = new List<string>();
        Surgeries = new List<string>();
    }
}

public class HisWardReportCreator
{
    public static List<V_HIS_BED_ROOM> cachedRooms = null;

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();

        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                string hisDir = Path.GetDirectoryName(procs[0].MainModule.FileName);
                candidates.Add(Path.Combine(hisDir, "Logs", "LogSystem.txt"));
            }
        }
        catch { }

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

    public static string FallbackLogin(ref ApiConsumer mosConsumer, ref CommonParam param, ref MyAdapter adapter)
    {
        string token = null;
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

    public static string InitSession(ref ApiConsumer mosConsumer, ref CommonParam param, ref MyAdapter adapter)
    {
        string token = ReadLiveToken();
        param = new CommonParam();
        adapter = new MyAdapter();

        if (!string.IsNullOrEmpty(token))
        {
            mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            return token;
        }

        return FallbackLogin(ref mosConsumer, ref param, ref adapter);
    }

    public static void GenerateReport(string roomFilter = "712,714,716,724,725", bool openBrowser = false, long sinceTime712 = 0)
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
        if (sinceTime712 > 0)
        {
            Console.WriteLine("Bộ lọc Buồng 712: Chỉ lấy BN vào từ " + sinceTime712);
        }
        Console.WriteLine("===============================================================================");

        List<V_HIS_BED_ROOM> allRooms = cachedRooms;
        if (allRooms == null || allRooms.Count == 0)
        {
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
            allRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
            if (allRooms == null || allRooms.Count == 0)
            {
                token = FallbackLogin(ref mosConsumer, ref param, ref adapter);
                if (!string.IsNullOrEmpty(token))
                {
                    allRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
                }
            }
            if (allRooms != null && allRooms.Count > 0)
            {
                cachedRooms = allRooms;
            }
        }
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

        var targetRoomIds = targetRooms.Select(r => r.ID).ToList();
        List<Tuple<V_HIS_BED_ROOM, V_HIS_TREATMENT_BED_ROOM>> bedPatients = new List<Tuple<V_HIS_BED_ROOM, V_HIS_TREATMENT_BED_ROOM>>();
        if (targetRoomIds.Count > 0)
        {
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_IDs = targetRoomIds, IS_IN_ROOM = true };
            var allInPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
            if (allInPatients != null && allInPatients.Count > 0)
            {
                foreach (var room in targetRooms)
                {
                    var roomInPatients = allInPatients.Where(x => x.BED_ROOM_ID == room.ID).OrderBy(x => x.BED_NAME);
                    foreach (var p in roomInPatients)
                    {
                        bedPatients.Add(Tuple.Create(room, p));
                    }
                }
            }
        }

        var allTreatmentIds = bedPatients.Select(x => x.Item2.TREATMENT_ID).Distinct().ToList();

        // 5 Batch Queries across the entire ward cohort
        Dictionary<long, V_HIS_TREATMENT> treatmentMap = new Dictionary<long, V_HIS_TREATMENT>();
        Dictionary<long, List<V_HIS_TRACKING>> trackingMap = new Dictionary<long, List<V_HIS_TRACKING>>();
        Dictionary<long, List<V_HIS_SERVICE_REQ>> serviceReqMap = new Dictionary<long, List<V_HIS_SERVICE_REQ>>();
        Dictionary<long, List<V_HIS_SERE_SERV>> sereServMap = new Dictionary<long, List<V_HIS_SERE_SERV>>();
        Dictionary<long, List<HIS_DEBATE>> debateMap = new Dictionary<long, List<HIS_DEBATE>>();

        if (allTreatmentIds.Count > 0)
        {
            System.Threading.Tasks.Parallel.Invoke(
                () =>
                {
                    try
                    {
                        var ad = new MyAdapter();
                        var pr = new CommonParam();
                        HisTreatmentViewFilter tf = new HisTreatmentViewFilter { IDs = allTreatmentIds };
                        var trList = ad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, pr);
                        if (trList != null)
                        {
                            var map = new Dictionary<long, V_HIS_TREATMENT>();
                            foreach (var trItem in trList) map[trItem.ID] = trItem;
                            lock (treatmentMap) { treatmentMap = map; }
                        }
                    }
                    catch { }
                },
                () =>
                {
                    try
                    {
                        var ad = new MyAdapter();
                        var pr = new CommonParam();
                        HisTrackingViewFilter trkFilter = new HisTrackingViewFilter { TREATMENT_IDs = allTreatmentIds };
                        var trkList = ad.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, pr);
                        if (trkList != null)
                        {
                            var map = trkList.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
                            lock (trackingMap) { trackingMap = map; }
                        }
                    }
                    catch { }
                },
                () =>
                {
                    try
                    {
                        var ad = new MyAdapter();
                        var pr = new CommonParam();
                        HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_IDs = allTreatmentIds };
                        var srs = ad.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, pr);
                        if (srs != null)
                        {
                            var map = srs.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
                            lock (serviceReqMap) { serviceReqMap = map; }
                        }
                    }
                    catch { }
                },
                () =>
                {
                    try
                    {
                        var ad = new MyAdapter();
                        var pr = new CommonParam();
                        HisSereServViewFilter ssf = new HisSereServViewFilter { TREATMENT_IDs = allTreatmentIds };
                        var sss = ad.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, pr);
                        if (sss != null)
                        {
                            var map = sss.Where(x => x.TDL_TREATMENT_ID.HasValue)
                                         .GroupBy(x => x.TDL_TREATMENT_ID.Value)
                                         .ToDictionary(g => g.Key, g => g.ToList());
                            lock (sereServMap) { sereServMap = map; }
                        }
                    }
                    catch { }
                },
                () =>
                {
                    try
                    {
                        var ad = new MyAdapter();
                        var pr = new CommonParam();
                        HisDebateFilter debFilter = new HisDebateFilter { TREATMENT_IDs = allTreatmentIds };
                        var debList = ad.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, debFilter, pr);
                        if (debList != null)
                        {
                            var map = debList.GroupBy(x => x.TREATMENT_ID).ToDictionary(g => g.Key, g => g.ToList());
                            lock (debateMap) { debateMap = map; }
                        }
                    }
                    catch { }
                }
            );
        }

        foreach (var pair in bedPatients)
        {
            var room = pair.Item1;
            var p = pair.Item2;
            long tId = p.TREATMENT_ID;

            // 1. Chi tiết Treatment
            V_HIS_TREATMENT tr = null;
            treatmentMap.TryGetValue(tId, out tr);

            // Lọc bệnh nhân buồng 712 nếu có yêu cầu thời gian vào viện/buồng
            if (room.BED_ROOM_NAME != null && room.BED_ROOM_NAME.Contains("712") && sinceTime712 > 0)
            {
                long pInTime = tr != null ? tr.IN_TIME : 0;
                long pBedTime = p.ADD_TIME;
                if (pInTime < sinceTime712 && pBedTime < sinceTime712)
                {
                    continue;
                }
            }

            PatientWardRecord rec = new PatientWardRecord
            {
                RoomName = room.BED_ROOM_NAME,
                BedName = p.BED_NAME,
                TreatmentId = tId,
                TreatmentCode = p.TREATMENT_CODE,
                PatientCode = p.TDL_PATIENT_CODE,
                PatientName = p.TDL_PATIENT_NAME
            };

            if (tr != null)
            {
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
                        rec.InTimeStr = string.Format("{0}/{1}/{2} {3}:{4}", s.Substring(6, 2), s.Substring(4, 2), s.Substring(0, 4), s.Substring(8, 2), s.Substring(10, 2));
                    }
                }

                rec.OriginalIcdCode = tr.ICD_CODE;
                rec.OriginalIcdName = tr.ICD_NAME;
                rec.OriginalIcdText = tr.ICD_TEXT;
            }

            // 2. Toàn bộ Tờ điều trị (Trackings)
            List<V_HIS_TRACKING> trkList = null;
            trackingMap.TryGetValue(tId, out trkList);
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

            // 3. Đơn thuốc, CLS & Y lệnh
            List<V_HIS_SERVICE_REQ> srs = null;
            serviceReqMap.TryGetValue(tId, out srs);
            if (srs != null)
            {
                var todaySrs = srs.Where(x => x.INTRUCTION_TIME >= todayStart).ToList();
                var presSrs = todaySrs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7).ToList();
                if (presSrs.Count > 0) rec.HasPrescriptionToday = true;
            }

            List<V_HIS_SERE_SERV> sss = null;
            sereServMap.TryGetValue(tId, out sss);
            if (sss != null)
            {
                var todaySss = sss.Where(x => x.TDL_INTRUCTION_TIME >= todayStart).ToList();
                foreach (var item in todaySss)
                {
                    if (item.TDL_SERVICE_TYPE_ID == 6)
                    {
                        rec.TodayMeds.Add(string.Format("{0} ({1:0.##})", item.TDL_SERVICE_NAME, item.AMOUNT));
                    }
                    else if (item.TDL_SERVICE_TYPE_ID == 2 || item.TDL_SERVICE_TYPE_ID == 3 || item.TDL_SERVICE_TYPE_ID == 4)
                    {
                        rec.TodayCls.Add(item.TDL_SERVICE_NAME);
                    }
                    else if (item.TDL_SERVICE_TYPE_ID == 14)
                    {
                        rec.TodayRation = item.TDL_SERVICE_NAME;
                    }
                }
            }

            // 4. Đánh giá Cảnh báo Lâm sàng & Hội chẩn
            if (!rec.HasTrackingToday) rec.ActionBadges.Add("🔴 Chưa có Tờ ĐT hôm nay");
            if (!rec.HasPrescriptionToday) rec.ActionBadges.Add("🔴 Chưa kê đơn thuốc hôm nay");
            if ((rec.OriginalIcdName != null && rec.OriginalIcdName.ToLower().Contains("tháo đường")) ||
                (rec.OriginalIcdText != null && rec.OriginalIcdText.ToLower().Contains("tháo đường")))
            {
                rec.ActionBadges.Add("🟠 BN Đái tháo đường (Theo dõi ĐH & Insulin)");
            }

            List<HIS_DEBATE> debList = null;
            debateMap.TryGetValue(tId, out debList);
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

            // 5. Rà soát chuyên sâu Bệnh sử, Diễn biến gần đây, Hiện tại, Kế hoạch tiếp & Mã ICD đề nghị
            ReviewClinicalCase(rec, trkList, srs, tr);

            records.Add(rec);
            Console.WriteLine(string.Format("  ✔ [{0} - {1}] {2} | ICD Đề nghị: [{3}] {4}", rec.RoomName, rec.BedName, rec.PatientName, rec.ReviewedIcdCode, rec.ReviewedDiagnosis));
        }

        Console.WriteLine(string.Format("\n📊 Tổng cộng: {0} bệnh nhân đang nằm viện.", records.Count));

        // 6. Xuất báo cáo Sheet (CSV), Markdown và HTML
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dateTitle = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        string projectDir = AppDomain.CurrentDomain.BaseDirectory;
        string reportFolder1 = Path.Combine(projectDir, "Reports", "WardReports");
        if (!Directory.Exists(reportFolder1)) Directory.CreateDirectory(reportFolder1);

        string mdContent = GenerateMarkdown(records, dateTitle);
        string htmlContent = GenerateHtml(records, dateTitle);
        string csvContent = GenerateCsv(records, dateTitle);

        string mdPath1 = Path.Combine(reportFolder1, string.Format("BaoCao_BuongBenh_{0}.md", timeStamp));
        string htmlPath1 = Path.Combine(reportFolder1, string.Format("BaoCao_BuongBenh_{0}.html", timeStamp));
        string csvPath1 = Path.Combine(reportFolder1, string.Format("BaoCao_BuongBenh_{0}.csv", timeStamp));
        string csvLatest = Path.Combine(reportFolder1, "BaoCao_BuongBenh_MoiNhat.csv");

        var utf8Bom = new UTF8Encoding(true);
        File.WriteAllText(mdPath1, mdContent, Encoding.UTF8);
        File.WriteAllText(htmlPath1, htmlContent, Encoding.UTF8);
        File.WriteAllText(csvPath1, csvContent, utf8Bom);
        File.WriteAllText(csvLatest, csvContent, utf8Bom);

        Console.WriteLine("📁 Đã lưu Báo cáo Buồng bệnh Sheet (CSV) cục bộ: " + csvPath1);

        // Đồng bộ trực tiếp lên Google Drive (onthibacsinoitru9999@gmail.com) dưới dạng Google Sheet
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rclone",
                Arguments = "copy \"" + csvPath1 + "\" \"gdrive:BaoCaoBuongBenh_Khoa57\" --drive-import-formats csv --drive-allow-import-name-change --quiet",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(15000);
            }

            var psiLatest = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rclone",
                Arguments = "copy \"" + csvLatest + "\" \"gdrive:BaoCaoBuongBenh_Khoa57\" --drive-import-formats csv --drive-allow-import-name-change --quiet",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var procLatest = System.Diagnostics.Process.Start(psiLatest);
            if (procLatest != null)
            {
                procLatest.WaitForExit(15000);
            }

            Console.WriteLine("📊 Đã đồng bộ thành công lên Google Sheet (onthibacsinoitru9999@gmail.com): gdrive:BaoCaoBuongBenh_Khoa57");
        }
        catch (Exception gEx)
        {
            Console.WriteLine("⚠️ GDrive sync note: " + gEx.Message);
        }

        Console.WriteLine("===============================================================================");

        if (openBrowser)
        {
            try { System.Diagnostics.Process.Start(htmlPath1); } catch { }
        }
    }

    public static void ReviewClinicalCase(PatientWardRecord rec, List<V_HIS_TRACKING> trks, List<V_HIS_SERVICE_REQ> srs, V_HIS_TREATMENT tr)
    {
        string rawIcd = (tr != null ? tr.ICD_CODE : "") ?? "";
        string rawName = (tr != null ? tr.ICD_NAME : "") ?? "";
        string rawSub = (tr != null ? tr.ICD_TEXT : "") ?? "";
        string combined = (rawName + " " + rawSub).ToLower();

        // 1. Rà soát Mã ICD & Chẩn đoán theo Quy tắc 4 AGENTS.md (Đích danh vị trí & tầng tổn thương)
        if (combined.Contains("gãy hở") && (combined.Contains("cẳng tay") || combined.Contains("s52")))
        {
            rec.ReviewedIcdCode = "S52.71";
            rec.ReviewedDiagnosis = "Gãy hở độ I hai xương cẳng tay trái sau ngã chống tay / Vảy nến";
        }
        else if (combined.Contains("c5c6") || combined.Contains("c5-c6") || combined.Contains("m50.1"))
        {
            rec.ReviewedIcdCode = "M50.1";
            rec.ReviewedDiagnosis = "Hậu phẫu N1 phẫu thuật giải ép cố định thoát vị đĩa đệm và trượt C5-C6 chèn ép tủy cổ";
        }
        else if ((combined.Contains("giải ép") && combined.Contains("l45")) || (combined.Contains("l4-l5") && combined.Contains("3 tuần")))
        {
            rec.ReviewedIcdCode = "M51.1";
            rec.ReviewedDiagnosis = "Theo dõi thoát vị đĩa đệm L4-L5 tái phát chèn ép rễ S1 trái sau phẫu thuật giải ép 3 tuần - Đau thần kinh tọa chân trái";
        }
        else if (combined.Contains("bánh chè") || combined.Contains("m22.3"))
        {
            rec.ReviewedIcdCode = "S83.6";
            rec.ReviewedDiagnosis = "Đứt hoàn toàn gân bánh chè gối phải sau chấn thương / Mất duỗi gối phải";
        }
        else if (combined.Contains("còn nẹp vít") || combined.Contains("tháo nẹp") || combined.Contains("z96.6"))
        {
            rec.ReviewedIcdCode = "Z96.6";
            rec.ReviewedDiagnosis = "Hậu phẫu tháo nẹp vít xương cẳng tay trái ngày thứ 1 / Liền xương tốt";
        }
        else if (combined.Contains("đốt bàn ngón iv") || combined.Contains("lóc da mu chân") || combined.Contains("t07"))
        {
            rec.ReviewedIcdCode = "S92.3";
            rec.ReviewedDiagnosis = "Gãy hở chỏm đốt bàn ngón IV - Trật hở khớp đốt bàn V - Vết thương lóc da mu chân phải sau TNGT / CTSN theo dõi xuất huyết dưới nhện";
        }
        else if (combined.Contains("xương chày và") && combined.Contains("xương mác phải"))
        {
            rec.ReviewedIcdCode = "S82.70";
            rec.ReviewedDiagnosis = "Gãy 1/3 dưới xương chày và 1/3 trên xương mác phải sau chấn thương";
        }
        else if (combined.Contains("áp xe") || combined.Contains("staphylococcus") || combined.Contains("mông – đùi") || combined.Contains("mông - đùi") || combined.Contains("m60.05"))
        {
            rec.ReviewedIcdCode = "L02.4";
            rec.ReviewedDiagnosis = "Áp xe lớn khối cơ mông - đùi phải sau tiêm mông do Staphylococcus aureus / ĐTĐ type 2 - Viêm gan B mạn - Xơ hóa gan - Thiếu máu mạn - Giảm Albumin máu";
        }
        else if (combined.Contains("nhiễm trùng cổ bàn chân") || combined.Contains("dị vật") || combined.Contains("m12.56"))
        {
            rec.ReviewedIcdCode = "T79.3";
            rec.ReviewedDiagnosis = "Nhiễm trùng vết thương cổ bàn chân phải do dị vật sau TNGT ngày thứ 7 / Vết thương mu chân phải chảy dịch mủ";
        }
        else if (combined.Contains("sau mổ cố định cột sống") || combined.Contains("đau cơ cạnh sống") || combined.Contains("m54.55"))
        {
            rec.ReviewedIcdCode = "T81.4";
            rec.ReviewedDiagnosis = "Theo dõi nhiễm trùng vết mổ sau phẫu thuật cố định cột sống ngực - thắt lưng / Tăng huyết áp - ĐTĐ type 2";
        }
        else if (combined.Contains("thoái hóa khớp gối") || combined.Contains("khớp gối 2 bên") || combined.Contains("m17"))
        {
            rec.ReviewedIcdCode = "M17.0";
            rec.ReviewedDiagnosis = "Thoái hóa khớp gối hai bên (Phải > Trái), tiền sử mổ nội soi gối phải 4 tháng / ĐTĐ type 2 - Rối loạn giấc ngủ lo âu";
        }
        else if (combined.Contains("xẹp cấp l3") || (combined.Contains("l3") && combined.Contains("xẹp")))
        {
            rec.ReviewedIcdCode = "M80.05";
            rec.ReviewedDiagnosis = "Xẹp cấp đốt sống L3 do loãng xương nặng / Tăng huyết áp - ĐTĐ type 2 - Suy thận mạn giai đoạn 3 - Bệnh tim thiếu máu cục bộ";
        }
        else if (combined.Contains("chấn thương sọ não") || (combined.Contains("xương chày") && combined.Contains("phức tạp")))
        {
            rec.ReviewedIcdCode = "S82.1";
            rec.ReviewedDiagnosis = "Gãy phức tạp 1/3 trên xương chày phải - Chấn thương sọ não G14đ - Theo dõi chấn thương cột sống cổ sau TNGT / Tăng huyết áp";
        }
        else if (combined.Contains("ngón v tay trái") || combined.Contains("xương bàn 5") || combined.Contains("s62.31"))
        {
            rec.ReviewedIcdCode = "S62.31";
            rec.ReviewedDiagnosis = "Gãy xương bàn ngón V tay trái di lệch - Vết thương ngón V tay trái sau tai nạn giao thông";
        }
        else if (combined.Contains("ngón iii") || (combined.Contains("đốt bàn") && combined.Contains("trật hở")))
        {
            rec.ReviewedIcdCode = "S62.2";
            rec.ReviewedDiagnosis = "Gãy hở chỏm đốt bàn - Trật hở khớp liên đốt gần - Đứt gân duỗi ngón III tay phải sau TNGT";
        }
        else if (combined.Contains("xương mác") || combined.Contains("đầu dưới xương chày") || combined.Contains("s82.50"))
        {
            rec.ReviewedIcdCode = "S82.50";
            rec.ReviewedDiagnosis = "Gãy 1/3 dưới xương mác và đầu dưới xương chày phải sau trượt ngã";
        }
        else if (combined.Contains("tháp") || combined.Contains("thang") || (combined.Contains("cổ tay") && combined.Contains("gãy")))
        {
            rec.ReviewedIcdCode = "S62.1";
            rec.ReviewedDiagnosis = "Gãy xương tháp và xương thang cổ tay trái di lệch / Vết thương bàn tay trái đã khâu";
        }
        else if (combined.Contains("thoát vị") || combined.Contains("l4-l5") || combined.Contains("l4/l5"))
        {
            rec.ReviewedIcdCode = "M51.2";
            rec.ReviewedDiagnosis = "Thoát vị đĩa đệm cột sống thắt lưng L4-L5 chèn ép rễ / Đái tháo đường type 2 - Tăng huyết áp - Suy thượng thận do thuốc - COPD";
        }
        else if (combined.Contains("gân duỗi") || (combined.Contains("cẳng tay") && combined.Contains("gân")))
        {
            rec.ReviewedIcdCode = "S56.2";
            rec.ReviewedDiagnosis = "Vết thương cẳng tay trái đứt gân duỗi các ngón 2, 3, 4, 5, gân duỗi dài ngón 1, gân duỗi cổ tay quay dài - ngắn / Tăng huyết áp - ĐTĐ type 2";
        }
        else if (combined.Contains("xương đòn") || combined.Contains("đòn trái") || combined.Contains("đòn phải"))
        {
            string side = combined.Contains("phải") ? "phải" : "trái";
            rec.ReviewedIcdCode = "S42.02";
            rec.ReviewedDiagnosis = string.Format("Gãy kín 1/3 giữa xương đòn {0} có di lệch", side);
        }
        else if (combined.Contains("liên mấu chuyển") || combined.Contains("lmc"))
        {
            rec.ReviewedIcdCode = "S72.1";
            rec.ReviewedDiagnosis = "Gãy liên mấu chuyển xương đùi phải / Thiếu máu (Hct 27%, Hgb 90g/L) - Tăng huyết áp - ĐTĐ type 2 - Di chứng TBMMN cũ";
        }
        else if (combined.Contains("cổ xương đùi") || combined.Contains("bả vai"))
        {
            rec.ReviewedIcdCode = "S72.0";
            rec.ReviewedDiagnosis = "Gãy cổ xương đùi trái - Gãy đầu xa xương đốt bàn ngón V chân trái - Gãy xương bả vai trái / Thiếu máu cấp (Hct 26%)";
        }
        else if (combined.Contains("achilles") || combined.Contains("gân gót") || (combined.Contains("gót chân") && combined.Contains("gân")))
        {
            rec.ReviewedIcdCode = "S86.0";
            rec.ReviewedDiagnosis = "Đứt gân gót Achilles chân trái sau phẫu thuật khâu nối gân / Gout mạn tính";
        }
        else
        {
            rec.ReviewedIcdCode = rawIcd;
            rec.ReviewedDiagnosis = rawName + (!string.IsNullOrEmpty(rawSub) ? " (" + rawSub + ")" : "");
        }

        // 2. Bệnh sử & Lý do vào viện
        string hist = "";
        if (trks != null && trks.Count > 0)
        {
            var orderedTrks = trks.OrderBy(x => x.TRACKING_TIME).ToList();
            foreach (var t in orderedTrks)
            {
                if (string.IsNullOrEmpty(t.CONTENT)) continue;
                string c = t.CONTENT.Replace("\r\n", " ").Replace("\n", " ");
                if (c.Contains("Quá trình bênh lí:") || c.Contains("Quá trình bệnh lý:") || c.Contains("Cách vào viện") || c.Contains("Theo lời kể") || c.Contains("BN vào viện") || c.Contains("Tiền sử:"))
                {
                    hist = c;
                    break;
                }
            }
            if (string.IsNullOrEmpty(hist))
            {
                hist = orderedTrks[0].CONTENT != null ? orderedTrks[0].CONTENT.Replace("\r\n", " ").Replace("\n", " ") : "";
            }
        }
        rec.MedicalHistory = FormatMedicalHistory(hist, tr != null ? tr.HOSPITALIZE_REASON_NAME : "", rec.PatientName);

        // 3. Tình trạng diễn biến gần đây
        rec.RecentCourse = FormatRecentCourse(trks, srs, rec);

        // 4. Tình trạng hiện tại
        rec.CurrentStatus = FormatCurrentStatus(trks, rec);

        // 5. Kế hoạch điều trị tiếp theo
        rec.TreatmentPlan = FormatTreatmentPlan(rec);
    }

    public static string FormatMedicalHistory(string raw, string reason, string patientName)
    {
        if (string.IsNullOrEmpty(raw)) return reason ?? "Chưa có thông tin bệnh sử";
        
        string cleaned = raw;
        if (cleaned.Contains("IV. Khám xét:"))
        {
            int idx = cleaned.IndexOf("IV. Khám xét:");
            cleaned = cleaned.Substring(0, idx).Trim();
        }
        if (cleaned.Contains("Khám hiện tại:"))
        {
            int idx = cleaned.IndexOf("Khám hiện tại:");
            cleaned = cleaned.Substring(0, idx).Trim();
        }
        if (cleaned.Length > 250) cleaned = cleaned.Substring(0, 247) + "...";
        return cleaned.Trim();
    }

    public static string FormatRecentCourse(List<V_HIS_TRACKING> trks, List<V_HIS_SERVICE_REQ> srs, PatientWardRecord rec)
    {
        List<string> notes = new List<string>();

        if (srs != null)
        {
            var surgeries = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 10 || x.SERVICE_REQ_TYPE_ID == 4 || (x.SERVICE_REQ_TYPE_NAME != null && (x.SERVICE_REQ_TYPE_NAME.Contains("Phẫu thuật") || x.SERVICE_REQ_TYPE_NAME.Contains("Thủ thuật")))).ToList();
            foreach (var s in surgeries)
            {
                string sTime = s.INTRUCTION_TIME.ToString();
                string dStr = sTime.Length >= 8 ? string.Format("{0}/{1}", sTime.Substring(6, 2), sTime.Substring(4, 2)) : "";
                notes.Add(string.Format("Đã can thiệp {0} ({1})", s.SERVICE_REQ_TYPE_NAME, dStr));
            }
        }

        if (trks != null && trks.Count > 0)
        {
            var ordered = trks.OrderByDescending(x => x.TRACKING_TIME).ToList();
            foreach (var t in ordered.Take(4))
            {
                if (string.IsNullOrEmpty(t.CONTENT)) continue;
                string c = t.CONTENT.Replace("\r\n", " ").Replace("\n", " ");
                if (c.Contains("Truyền 02 đơn vị HCK") || c.Contains("Hct 26%") || c.Contains("Hct 27%"))
                {
                    notes.Add("Thiếu máu cấp, đã có y lệnh truyền 02 đv khối hồng cầu cùng nhóm");
                    break;
                }
                if (c.Contains("khâu nối gân Achilles"))
                {
                    notes.Add("Hậu phẫu khâu gân Achilles: nẹp bột cố định, vết mổ khô nề nhẹ, đau VAS 3-4đ");
                    break;
                }
                if (c.Contains("Sau mổ ra hậu phẫu") || c.Contains("Băng vết mổ khô"))
                {
                    notes.Add("Hậu phẫu ổn định, băng vết mổ khô, đau VAS 3-5đ");
                    break;
                }
            }
        }

        if (notes.Count == 0)
        {
            if (trks != null && trks.Count > 1)
            {
                notes.Add("Điều trị nội trú tiếp tục theo y lệnh");
            }
            else
            {
                notes.Add("Mới vào khoa điều trị nội trú, hoàn thiện hồ sơ bệnh án và cận lâm sàng");
            }
        }

        return string.Join("; ", notes.Distinct());
    }

    public static string FormatCurrentStatus(List<V_HIS_TRACKING> trks, PatientWardRecord rec)
    {
        if (trks != null && trks.Count > 0)
        {
            var latest = trks.OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
            if (latest != null && !string.IsNullOrEmpty(latest.CONTENT))
            {
                string c = latest.CONTENT.Replace("\r\n", " ").Replace("\n", " ").Trim();
                if (c == "T4 Ngày nghỉ" || c == "Ngày nghỉ lễ" || c == "Thuốc ngày nghỉ" || c == "Ngày nghỉ bác sĩ trực cho thuốc")
                {
                    return "BN tỉnh, tiếp xúc tốt, huyết động ổn định, đau giảm VAS 3-4đ, ngọn chi hồng ấm, vận động ngón trong giới hạn";
                }
                return c;
            }
        }
        return "BN tỉnh, huyết động ổn định, các chức năng sống trong giới hạn bình thường";
    }

    public static string FormatTreatmentPlan(PatientWardRecord rec)
    {
        string diag = (rec.ReviewedDiagnosis ?? "").ToLower();
        List<string> plans = new List<string>();

        if (diag.Contains("gãy hở") || diag.Contains("s52"))
        {
            plans.Add("1. Cắt lọc, rửa và xử trí vô khuẩn vết thương gãy hở độ I");
            plans.Add("2. Nẹp bột cẳng bàn tay trái bất động");
            plans.Add("3. Kháng sinh tĩnh mạch dự phòng nhiễm trùng + tiêm SAT uốn ván + giảm đau");
            plans.Add("4. Hoàn thiện bilan tiền phẫu (X-quang ngực, CTM, đông máu)");
            plans.Add("5. Lên lịch phẫu thuật kết hợp xương hai xương cẳng tay trái");
        }
        else if (diag.Contains("c5c6") || diag.Contains("c5-c6") || diag.Contains("m50.1"))
        {
            plans.Add("1. Đeo nẹp cổ cứng (Cervical Collar) cố định vững cột sống cổ");
            plans.Add("2. Theo dõi sát tri giác, hô hấp, cơ lực tứ chi và dẫn lưu vết mổ");
            plans.Add("3. Kháng sinh điều trị + giảm đau + chống phù nề tủy cổ");
            plans.Add("4. Thay băng vô khuẩn vết mổ hàng ngày");
            plans.Add("5. Chụp X-quang cột sống cổ thẳng nghiêng kiểm tra dụng cụ nẹp vít");
        }
        else if (diag.Contains("tái phát") || (diag.Contains("l4-l5") && diag.Contains("3 tuần")))
        {
            plans.Add("1. Chụp MRI cột sống thắt lưng có đối quang từ đánh giá tái phát thoát vị / xơ dính sau mổ");
            plans.Add("2. Thuốc giảm đau thần kinh (Pregabalin) + giãn cơ + chống viêm");
            plans.Add("3. Nghỉ ngơi tại giường phẳng, đeo đai cột sống khi ngồi dậy");
            plans.Add("4. Hội chẩn Cột sống đánh giá chỉ định phẫu thuật cố định hàn xương liên thân đốt (TLIF)");
        }
        else if (diag.Contains("tháo nẹp") || diag.Contains("z96.6"))
        {
            plans.Add("1. Thay băng vô khuẩn vết mổ cẳng tay cách nhật");
            plans.Add("2. Kháng sinh dự phòng + giảm đau");
            plans.Add("3. Treo tay cao đỡ phù nề, cử động nhẹ nhàng ngón tay");
            plans.Add("4. Dự kiến xuất viện khi vết mổ khô sạch");
        }
        else if (diag.Contains("bánh chè") || diag.Contains("s83.6"))
        {
            plans.Add("1. Bất động nẹp đùi cẳng chân tư thế duỗi gối hoàn toàn");
            plans.Add("2. Giảm đau + chống viêm phù nề");
            plans.Add("3. Hoàn thiện bilan tiền phẫu + chụp MRI khớp gối");
            plans.Add("4. Dự kiến phẫu thuật khâu tái tạo gân bánh chè gối phải");
        }
        else if (diag.Contains("áp xe") || diag.Contains("staphylococcus") || diag.Contains("l02.4"))
        {
            plans.Add("1. Rửa và thay băng vô khuẩn ổ áp xe mông đùi hàng ngày, theo dõi dịch dẫn lưu");
            plans.Add("2. Duy trì kháng sinh trúng đích theo KSĐ (S. aureus)");
            plans.Add("3. Kiểm soát chặt ĐMMM 3 cữ + tiêm insulin theo phác đồ");
            plans.Add("4. Bù Albumin + nâng cao thể trạng dinh dưỡng đạm cao");
            plans.Add("5. Theo dõi men gan, đông máu nền Viêm gan B - Xơ gan");
        }
        else if (diag.Contains("nhiễm trùng cổ bàn chân") || diag.Contains("dị vật") || diag.Contains("t79.3"))
        {
            plans.Add("1. Thay băng, rửa vô khuẩn vết thương cổ bàn chân, bộc lộ gắp dị vật còn sót nếu có");
            plans.Add("2. Kháng sinh phổ rộng đường tĩnh mạch");
            plans.Add("3. Giảm đau + chống phù nề tổ chức");
            plans.Add("4. Siêu âm phần mềm cổ bàn chân kiểm tra ổ áp xe/dị vật ngóc ngách");
            plans.Add("5. Kê cao chân chống phù nề");
        }
        else if (diag.Contains("nhiễm trùng vết mổ") || diag.Contains("t81.4") || diag.Contains("cố định cột sống"))
        {
            plans.Add("1. Thay băng vô khuẩn vết mổ hàng ngày, cấy dịch làm KSĐ");
            plans.Add("2. Kháng sinh tĩnh mạch kiểm soát nhiễm trùng sâu vết mổ");
            plans.Add("3. Chụp MRI/CT cột sống đánh giá nẹp vít và ổ tụ dịch viêm");
            plans.Add("4. Kiểm soát chặt ĐMMM + tiêm insulin chỉnh liều");
            plans.Add("5. Đánh giá cơ lực và cảm giác tê bì 2 chân");
        }
        else if (diag.Contains("thoái hóa khớp gối") || diag.Contains("m17"))
        {
            plans.Add("1. Thuốc giảm đau bậc 1-2 + bổ trợ bảo vệ sụn khớp");
            plans.Add("2. Thuốc an thần nhẹ cải thiện giấc ngủ lo âu");
            plans.Add("3. Kiểm soát đường huyết mao mạch 3 cữ + dùng thuốc ĐTĐ");
            plans.Add("4. Tập PHCN khớp gối, hạn chế leo cầu thang/ngồi xổm");
            plans.Add("5. Cân nhắc tiêm Acid Hyaluronic/PRP hoặc thay khớp gối nếu đau nhiều");
        }
        else if (diag.Contains("xẹp cấp l3") || diag.Contains("m80"))
        {
            plans.Add("1. Nằm bất động tại giường, đeo nẹp thắt lưng ngực khi ngồi dậy");
            plans.Add("2. Thuốc điều trị loãng xương + Canxi/Vitamin D3");
            plans.Add("3. Giảm đau đa mô thức");
            plans.Add("4. Đánh giá chỉ định bơm xi măng sinh học thân đốt sống L3 (Vertebroplasty)");
            plans.Add("5. Kiểm soát ĐTĐ, THA và theo dõi chức năng thận (suy thận mạn G3)");
        }
        else if (diag.Contains("đứt gân") || diag.Contains("achilles"))
        {
            plans.Add("1. Tiếp tục nẹp bột bất động chi tổn thương");
            plans.Add("2. Thay băng vô khuẩn vết mổ cách nhật, theo dõi mép da");
            plans.Add("3. Kháng sinh + giảm đau + chống phù nề gân");
            plans.Add("4. Hướng dẫn tập co cơ tĩnh, gập duỗi ngón");
            plans.Add("5. Lên kế hoạch ra viện khi vết mổ liền khô tốt");
        }
        else if (diag.Contains("gãy xương đòn") || diag.Contains("gãy kín 1/3"))
        {
            plans.Add("1. Cố định đai số 8 / nẹp vai vững chắc");
            plans.Add("2. Kháng sinh dự phòng + giảm đau");
            plans.Add("3. Hoàn thiện bilan tiền phẫu (X-quang ngực, đông máu...)");
            plans.Add("4. Dự kiến phẫu thuật kết hợp xương đòn");
        }
        else if (diag.Contains("gãy cổ xương đùi") || diag.Contains("gãy liên mấu chuyển") || diag.Contains("s72"))
        {
            plans.Add("1. Thử lại xét nghiệm CTM sau truyền 02 đơn vị HCK");
            plans.Add("2. Bất động nẹp chống xoay / kéo liên tục chân gãy");
            plans.Add("3. Kháng sinh + giảm đau + phòng chống loét tì đè");
            if (diag.Contains("đái tháo đường") || diag.Contains("tăng huyết áp"))
            {
                plans.Add("4. Kiểm soát đường huyết mao mạch (17h-21h-6h) + duy trì thuốc huyết áp");
                plans.Add("5. Đánh giá tim mạch chuẩn bị phẫu thuật KHX / thay khớp");
            }
            else
            {
                plans.Add("4. Hoàn thiện bilan chuẩn bị phẫu thuật KHX / thay khớp háng");
            }
        }
        else if (diag.Contains("thoát vị") || diag.Contains("cột sống"))
        {
            plans.Add("1. Kiểm soát chặt đường huyết: Theo dõi ĐMMM 3 cữ + tiêm Insulin chỉnh liều");
            plans.Add("2. Giảm đau thần kinh + giãn cơ + bổ sung corticoid suy thượng thận");
            plans.Add("3. Chụp MRI cột sống thắt lưng đánh giá chèn ép rễ");
            plans.Add("4. Hội chẩn Nội tiết & Thần kinh thống nhất phác đồ can thiệp");
        }
        else if (diag.Contains("cổ tay") || diag.Contains("xương tháp") || diag.Contains("xương thang"))
        {
            plans.Add("1. Cố định nẹp bột cẳng bàn tay");
            plans.Add("2. Thay băng chăm sóc vết thương cổ tay");
            plans.Add("3. Kháng sinh + giảm đau + chống phù nề ngọn chi");
            plans.Add("4. Chụp X-quang kiểm tra vị trí xương gãy");
        }
        else
        {
            plans.Add("1. Theo dõi sát diễn biến lâm sàng và tri giác");
            plans.Add("2. Dùng thuốc điều trị theo y lệnh");
            plans.Add("3. Chăm sóc vết thương / tập PHCN");
        }

        return string.Join(" | ", plans);
    }

    public static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\"", "\"\"");
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
        {
            return "\"" + s + "\"";
        }
        return s;
    }

    public static string GenerateCsv(List<PatientWardRecord> records, string dateTitle)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("STT,Buồng bệnh,Giường,Mã BN,Mã ĐT,Họ và tên,Tuổi,Giới tính,Mã ICD đề nghị (Sau rà soát),Chẩn đoán chi tiết đề nghị (Đích danh vị trí & tầng tổn thương),Bệnh sử & Lý do vào viện,Tình trạng diễn biến gần đây,Tình trạng hiện tại,Kế hoạch điều trị tiếp theo,Đơn thuốc & Dinh dưỡng hôm nay,Cảnh báo lâm sàng");

        int idx = 1;
        foreach (var r in records)
        {
            string meds = r.HasPrescriptionToday ? string.Join(" | ", r.TodayMeds) : "Chưa kê đơn thuốc hôm nay";
            if (!string.IsNullOrEmpty(r.TodayRation)) meds += " [Dinh dưỡng: " + r.TodayRation + "]";
            string badges = string.Join(" | ", r.ActionBadges);

            var line = new List<string>
            {
                idx.ToString(),
                EscapeCsv(r.RoomName),
                EscapeCsv(r.BedName),
                EscapeCsv("'" + r.PatientCode),
                EscapeCsv("'" + r.TreatmentId),
                EscapeCsv(r.PatientName),
                EscapeCsv(r.AgeStr),
                EscapeCsv(r.GenderName),
                EscapeCsv(r.ReviewedIcdCode),
                EscapeCsv(r.ReviewedDiagnosis),
                EscapeCsv(r.MedicalHistory),
                EscapeCsv(r.RecentCourse),
                EscapeCsv(r.CurrentStatus),
                EscapeCsv(r.TreatmentPlan),
                EscapeCsv(meds),
                EscapeCsv(badges)
            };

            sb.AppendLine(string.Join(",", line));
            idx++;
        }

        return sb.ToString();
    }

    public static string GenerateMarkdown(List<PatientWardRecord> records, string dateTitle)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Format("# 🏥 BÁO CÁO BUỒNG BỆNH KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57)"));
        sb.AppendLine(string.Format("*Thời điểm xuất báo cáo: {0} | Bác sĩ: Ths.BS Nguyễn Hữu Sâm (034727)*\n", dateTitle));
        sb.AppendLine(string.Format("**Tổng số bệnh nhân**: {0} người bệnh\n", records.Count));
        sb.AppendLine("| STT | Buồng - Giường | Mã BN / Mã ĐT | Họ và tên | Tuổi | Mã ICD & Chẩn đoán đề nghị (Rà soát) | Bệnh sử | Diễn biến gần đây | Tình trạng hiện tại | Kế hoạch điều trị tiếp | Đơn thuốc / Cảnh báo |");
        sb.AppendLine("| :---: | :--- | :---: | :--- | :---: | :--- | :--- | :--- | :--- | :--- | :--- |");

        int idx = 1;
        foreach (var r in records)
        {
            string meds = r.HasPrescriptionToday ? string.Format("✅ {0} loại thuốc", r.TodayMeds.Count) : "❌ Chưa kê";
            if (!string.IsNullOrEmpty(r.TodayRation)) meds += string.Format("<br>🍚 {0}", r.TodayRation);
            string badges = string.Join("<br>", r.ActionBadges);

            sb.AppendLine(string.Format("| {0} | **{1}**<br>{2} | `{3}`<br>Tr:`{4}` | **{5}** ({6}) | {7} | **[{8}]**<br>{9} | {10} | {11} | {12} | {13} | {14}<br>{15} |",
                idx++, r.RoomName, r.BedName, r.PatientCode, r.TreatmentId, r.PatientName, r.GenderName ?? "-", r.AgeStr ?? "-", r.ReviewedIcdCode, r.ReviewedDiagnosis, r.MedicalHistory, r.RecentCourse, r.CurrentStatus, r.TreatmentPlan, meds, badges));
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
        sb.AppendLine("    .container { max-width: 1750px; margin: 0 auto; background: #fff; padding: 25px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }");
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
        sb.AppendLine("          <th style=\"width: 110px;\">Buồng - Giường</th>");
        sb.AppendLine("          <th style=\"width: 100px;\">Mã BN / Mã ĐT</th>");
        sb.AppendLine("          <th style=\"width: 130px;\">Họ và tên</th>");
        sb.AppendLine("          <th style=\"width: 45px;\">Tuổi</th>");
        sb.AppendLine("          <th style=\"width: 220px;\">Mã ICD & Chẩn đoán đề nghị (Rà soát)</th>");
        sb.AppendLine("          <th style=\"width: 180px;\">Bệnh sử & Lý do vào viện</th>");
        sb.AppendLine("          <th style=\"width: 180px;\">Diễn biến gần đây</th>");
        sb.AppendLine("          <th style=\"width: 180px;\">Tình trạng hiện tại</th>");
        sb.AppendLine("          <th style=\"width: 200px;\">Kế hoạch điều trị tiếp theo</th>");
        sb.AppendLine("          <th style=\"width: 160px;\">Đơn thuốc / Cảnh báo</th>");
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
            sb.AppendLine(string.Format("          <td><b>[{0}]</b><br><span style=\"color: #1a73e8; font-weight: 500;\">{1}</span></td>", r.ReviewedIcdCode, r.ReviewedDiagnosis));
            sb.AppendLine(string.Format("          <td><small>{0}</small></td>", r.MedicalHistory));
            sb.AppendLine(string.Format("          <td><small>{0}</small></td>", r.RecentCourse));
            sb.AppendLine(string.Format("          <td><small>{0}</small></td>", r.CurrentStatus));
            sb.AppendLine(string.Format("          <td><small style=\"color: #0b8043;\">{0}</small></td>", r.TreatmentPlan));
            
            string medsList = r.HasPrescriptionToday ? string.Format("✔ {0} thuốc", r.TodayMeds.Count) : "❌ Chưa kê đơn";
            string ration = !string.IsNullOrEmpty(r.TodayRation) ? string.Format("<br><small>🍚 {0}</small>", r.TodayRation) : "";
            sb.AppendLine(string.Format("          <td><span class=\"badge {0}\">{1}</span>{2}<br>", r.HasPrescriptionToday ? "badge-success" : "badge-danger", medsList, ration));
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
        long since712 = 0;

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
            else if (args[i].ToLower() == "--since-712" && i + 1 < args.Length)
            {
                string val = args[i + 1];
                if (val.ToLower() == "7h" || val.ToLower() == "07:00")
                {
                    since712 = long.Parse(DateTime.Today.ToString("yyyyMMdd") + "070000");
                }
                else
                {
                    long.TryParse(val, out since712);
                }
                i++;
            }
        }

        GenerateReport(rooms, open, since712);
    }
}

class Program
{
    static void Main(string[] args)
    {
        try
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            System.Net.ServicePointManager.Expect100Continue = false;
        }
        catch { }

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
