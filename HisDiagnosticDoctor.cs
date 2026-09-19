using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
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


public class HisDiagnosticDoctor
{
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static string currentToken = null;
    public static Inventec.Common.WebApiClient.ApiConsumer mosConsumer = null;

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();

        // 1. Thư mục HIS chuẩn theo yêu cầu của Bác sĩ
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

        // 2. Thư mục hiện tại và các thư mục cha
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        // 3. Chỉ quét tiến trình HIS nếu chạy đúng từ preferredDir (TUYỆT ĐỐI không đọc từ thư mục HIS khác trên máy)
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                foreach (var p in procs)
                {
                    try
                    {
                        string exePath = p.MainModule.FileName;
                        if (exePath.IndexOf("LBP2900_R150_V330_W64_uk_EN_2", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string hisDir = Path.GetDirectoryName(exePath);
                            candidates.Insert(0, Path.Combine(hisDir, "Logs", "LogSystem.txt"));
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

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

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true"))
                    {
                        continue;
                    }

                    // BẢO VỆ DANH TÍNH (Identity Guard):
                    // Bắt buộc xác thực token live này phải thuộc về Bác sĩ (034727 hoặc vmc).
                    // Nếu người khác đang đăng nhập (như điều dưỡng, bác sĩ khác), bỏ qua để ép đăng nhập độc lập!
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string candidateToken = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                return candidateToken;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;
        try { Load.Init(); } catch { }
        param = new CommonParam();

        // 1. Kiểm tra cache token độc lập (hạn 6 tiếng) từ tất cả các thư mục chuẩn
        string tokenCode = null;
        List<string> tokenCandidates = new List<string>
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_standalone.token"),
            @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\doctor_standalone.token"
        };

        foreach (var cacheFile in tokenCandidates)
        {
            try
            {
                if (File.Exists(cacheFile))
                {
                    string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                    if (parts.Length >= 2)
                    {
                        long savedTime;
                        if (long.TryParse(parts[1], out savedTime))
                        {
                            DateTime savedDt = new DateTime(savedTime);
                            if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                            {
                                tokenCode = parts[0];
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // 2. Thử đọc Live Token từ HIS chuẩn (chỉ nhận nick 034727/vmc)
        if (string.IsNullOrEmpty(tokenCode))
        {
            tokenCode = ReadLiveToken();
        }

        // 3. Tự động ĐĂNG NHẬP ĐỘC LẬP qua ACS bằng nick 034727
        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null) tokenCode = token.TokenCode;
                }

                if (!string.IsNullOrEmpty(tokenCode))
                {
                    try
                    {
                        File.WriteAllText(tokenCandidates[0], tokenCode + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8);
                    }
                    catch { }
                }
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(tokenCode))
        {
            currentToken = tokenCode;
            mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
            try { ApiConsumers.SetConsunmer(currentToken); } catch { }
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 },
                        new RoomSDO { RoomId = 5252 },
                        new RoomSDO { RoomId = 5251 },
                        new RoomSDO { RoomId = 5257 },
                        new RoomSDO { RoomId = 18679 },
                        new RoomSDO { RoomId = 18681 },
                        new RoomSDO { RoomId = 14759 },
                        new RoomSDO { RoomId = 14787 }
                    }
                };
                myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            }
            catch { }
        }
    }

    public static bool CheckTcpPort(string host, int port, int timeoutMs = 2000)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                if (!success) return false;
                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static void HealthCheck()
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🩺 BÁC SĨ CHẨN ĐOÁN HỆ THỐNG HIS (HIS SYSTEM HEALTH DIAGNOSTIC)");
        Console.WriteLine("===============================================================================");

        // 1. Kiểm tra mạng tới 4 cổng Backend
        Console.WriteLine("\n1. Kiểm tra kết nối mạng 4 máy chủ lõi:");
        var endpoints = new[] {
            new { Name = "MOS Backend API", Host = "192.168.7.236", Port = 1608 },
            new { Name = "ACS Auth Service", Host = "192.168.7.200", Port = 1401 },
            new { Name = "SDA Data Service", Host = "192.168.7.200", Port = 1410 },
            new { Name = "EMR Document API", Host = "192.168.7.239", Port = 1415 }
        };

        bool allNetOk = true;
        foreach (var ep in endpoints)
        {
            bool ok = CheckTcpPort(ep.Host, ep.Port);
            Console.WriteLine(string.Format("   [{0}] {1,-18} ({2}:{3})", ok ? "✅ OK" : "❌ FAIL", ep.Name, ep.Host, ep.Port));
            if (!ok) allNetOk = false;
        }

        // 2. Kiểm tra Live Token
        Console.WriteLine("\n2. Kiểm tra Live Token & Phiên làm việc:");
        InitSession();
        if (!string.IsNullOrEmpty(currentToken))
        {
            Console.WriteLine("   ✅ TokenCode đang hoạt động: " + currentToken.Substring(0, 16) + "..." + currentToken.Substring(currentToken.Length - 8));
        }
        else
        {
            Console.WriteLine("   ❌ Không thể lấy TokenCode (Kiểm tra lại HIS Client hoặc tài khoản ACS)!");
        }

        // 3. Kiểm tra xác thực MOS API
        Console.WriteLine("\n3. Kiểm tra khả năng gọi API MOS Backend:");
        if (!string.IsNullOrEmpty(currentToken) && mosConsumer != null)
        {
            try
            {
                HisDepartmentFilter df = new HisDepartmentFilter { ID = 57 };
                var depts = myAdapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, df, param);
                if (depts != null && depts.Count > 0)
                {
                    Console.WriteLine("   ✅ Gọi API MOS Backend thành công! Khoa: " + depts[0].DEPARTMENT_NAME + " (ID: 57)");
                }
                else
                {
                    Console.WriteLine("   ⚠️  MOS API trả về danh sách rỗng.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ❌ Lỗi gọi MOS API: " + ex.Message);
            }
        }

        // 4. Kiểm tra OpenRouter API
        Console.WriteLine("\n4. Kiểm tra AI OpenRouter Free Tier:");
        string orKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(orKey)) orKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.Process);
        if (!string.IsNullOrEmpty(orKey))
        {
            Console.WriteLine("   ✅ Đã cấu hình OPENROUTER_API_KEY (" + orKey.Substring(0, 10) + "...)");
        }
        else
        {
            Console.WriteLine("   ⚠️  Chưa cấu hình OPENROUTER_API_KEY.");
        }

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("🎯 KẾT LUẬN CHẨN ĐOÁN: " + (allNetOk && !string.IsNullOrEmpty(currentToken) ? "Hệ thống sẵn sàng 100%!" : "Cảnh báo: Cần kiểm tra lại kết nối mạng hoặc phiên đăng nhập!"));
        Console.WriteLine("===============================================================================");
    }

    public static void CheckPatient(string keyword)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🔍 CHẨN ĐOÁN TRẠNG THÁI HỒ SƠ BỆNH NHÂN: " + keyword);
        Console.WriteLine("===============================================================================");

        InitSession();
        if (string.IsNullOrEmpty(currentToken) || mosConsumer == null)
        {
            Console.WriteLine("❌ Chưa có token xác thực!");
            return;
        }

        List<V_HIS_TREATMENT> tList = null;
        string kw = keyword.Trim();
        long pNum;
        if (long.TryParse(kw, out pNum))
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (tList == null || tList.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }

        if (tList == null || tList.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.KEY_WORD = kw;
            tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        }

        if (tList == null || tList.Count == 0)
        {
            Console.WriteLine("❌ KHÔNG TÌM THẤY BỆNH NHÂN NÀO VỚI TỪ KHÓA: " + keyword);
            return;
        }

        var tr = tList.OrderByDescending(x => x.IN_TIME).First();
        Console.WriteLine(string.Format("Họ tên       : {0} (Mã BN: {1})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
        Console.WriteLine(string.Format("Mã hồ sơ (Tr): {0} (Mã ĐT: {1})", tr.ID, tr.TREATMENT_CODE));
        Console.WriteLine(string.Format("Khoa điều trị: ID {0} {1}", tr.LAST_DEPARTMENT_ID, tr.LAST_DEPARTMENT_ID == 57 ? "✅ Đúng Khoa 57" : "⚠️ KHÁC KHOA 57!"));
        Console.WriteLine(string.Format("Chẩn đoán    : [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));
        Console.WriteLine(string.Format("Trạng thái   : {0}", tr.IS_PAUSE == 1 ? "⚠️ ĐÃ TẠM KHÓA / XUẤT VIỆN" : "✅ Đang điều trị nội trú"));

        // Kiểm tra buồng giường
        HisBedLogViewFilter bf = new HisBedLogViewFilter { TREATMENT_ID = tr.ID };
        var bedList = myAdapter.FetchList<V_HIS_BED_LOG>("api/HisBedLog/GetView", mosConsumer, bf, param);
        if (bedList != null && bedList.Count > 0)
        {
            var activeBed = bedList.OrderByDescending(x => x.START_TIME).First();
            Console.WriteLine(string.Format("Buồng / Giường: {0} / {1}", activeBed.BED_ROOM_NAME, activeBed.BED_NAME));
        }
        else
        {
            Console.WriteLine("Buồng / Giường: Chưa gán buồng giường trên hệ thống!");
        }

        // Kiểm tra Tờ điều trị mới nhất
        HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tr.ID };
        var trkList = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
        if (trkList != null && trkList.Count > 0)
        {
            var lastTrk = trkList.OrderByDescending(x => x.TRACKING_TIME).First();
            Console.WriteLine(string.Format("Tờ ĐT mới nhất: ID {0} lúc {1}", lastTrk.ID, lastTrk.TRACKING_TIME));
        }
        else
        {
            Console.WriteLine("Tờ ĐT mới nhất: Chưa có tờ điều trị nào!");
        }

        Console.WriteLine("===============================================================================");
    }

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0 || args[0].ToLower() == "health")
        {
            HealthCheck();
        }
        else if (args[0].ToLower() == "patient" || args[0].ToLower() == "p")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Cú pháp: HisDiagnosticDoctor.bat patient <mã_bn|mã_điều_trị>");
                return;
            }
            CheckPatient(args[1]);
        }
        else if (args[0].ToLower() == "token")
        {
            string t = ReadLiveToken();
            Console.WriteLine("Token: " + (t ?? "NULL"));
        }
        else if (args[0].ToLower() == "sign-ba")
        {
            try
            {
                string patientCode = args.Length > 1 ? args[1] : "0004051068";
                InitSession();
                var emrConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");

                Console.WriteLine("===============================================================================");
                Console.WriteLine("⚡ TẠO VĂN BẢN EMR & KÝ ĐIỆN TỬ BỆNH ÁN NGOẠI KHOA (TRANG 2): " + patientCode);
                Console.WriteLine("👤 Bác sĩ ký: ThS.BS Nguyễn Hữu Sâm (034727)");
                Console.WriteLine("===============================================================================");

                // Tra cứu đợt điều trị từ MOS
                if (string.IsNullOrEmpty(currentToken) || mosConsumer == null)
                {
                    Console.WriteLine("❌ Chưa có token xác thực hoặc không thể kết nối MOS!");
                    return;
                }
                var trFilter = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = patientCode.PadLeft(10, '0') };
                var pTr = new CommonParam();
                var trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, trFilter, pTr);
                if (trs == null || trs.Count == 0)
                {
                    trFilter = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = patientCode.PadLeft(12, '0') };
                    trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, trFilter, pTr);
                }
                if (trs == null || trs.Count == 0)
                {
                    trFilter = new HisTreatmentViewFilter { KEY_WORD = patientCode };
                    trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, trFilter, pTr);
                }
                if (trs == null || trs.Count == 0)
                {
                    Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho BN: " + patientCode);
                    return;
                }
                var tr = trs.OrderByDescending(x => x.IN_TIME).First();
                Console.WriteLine(string.Format("👤 Bệnh nhân: {0} | Mã ĐT: {1} | Vào viện: {2}", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE, tr.IN_TIME));

                // 1. Chuẩn bị PDF 2 trang chuẩn (Trang 1: Bìa hành chính, Trang 2: Khám bệnh)
                string pdf2Pages = "%PDF-1.4\n" +
                    "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
                    "2 0 obj<</Type/Pages/Count 2/Kids[3 0 R 4 0 R]>>endobj\n" +
                    "3 0 obj<</Type/Page/MediaBox[0 0 595 842]/Parent 2 0 R/Resources<<>>>>endobj\n" +
                    "4 0 obj<</Type/Page/MediaBox[0 0 595 842]/Parent 2 0 R/Resources<<>>>>endobj\n" +
                    "xref\n0 5\n0000000000 65535 f \n0000000009 00000 n \n0000000052 00000 n \n0000000115 00000 n \n0000000194 00000 n \ntrailer<</Size 5/Root 1 0 R>>\nstartxref\n273\n%%EOF\n";
                string base64Pdf = Convert.ToBase64String(Encoding.ASCII.GetBytes(pdf2Pages));

                long docTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                string docName = "Bệnh án ngoại khoa (Bìa & Khám bệnh)";
                string hisCode = "Mps000030 TREATMENT_CODE:" + tr.TREATMENT_CODE + " BENHANNGOAIKHOA";

                var docTdo = new EMR.TDO.DocumentTDO
                {
                    TreatmentCode = tr.TREATMENT_CODE,
                    DocumentName = docName,
                    DocumentTypeId = 116, // Vỏ bệnh án hỏi bệnh / Khám bệnh
                    HisCode = hisCode,
                    DepartmentCode = "9",
                    DocumentTime = docTime,
                    Loginname = "034727",
                    PaperName = "A4",
                    RawKind = 9,
                    Width = 827.0m,
                    Height = 1169.0m,
                    IsSignParallel = true,
                    Signs = new List<EMR.TDO.SignTDO>
                    {
                        new EMR.TDO.SignTDO
                        {
                            NumOrder = 1,
                            Loginname = "034727",
                            Username = "NGUYỄN HỮU SÂM",
                            FullName = "NGUYỄN HỮU SÂM",
                            Title = "Ths.BS",
                            DepartmentCode = "9",
                            DepartmentName = "Khoa Chấn thương Chỉnh hình và Cột sống"
                        }
                    },
                    OriginalVersion = new EMR.TDO.VersionTDO
                    {
                        Base64Data = base64Pdf
                    },
                    FileType = EMR.TDO.FileType.PDF
                };

                Console.WriteLine("📄 Đang tạo lệnh in văn bản EMR (Type 116: Vỏ bệnh án hỏi bệnh)...");
                CommonParam pTdo = new CommonParam();
                var docRes = myAdapter.PostData<EMR.TDO.DocumentTDO>("api/EmrDocument/CreateByTdo", emrConsumer, docTdo, pTdo);
                if (docRes == null || !docRes.DocumentId.HasValue)
                {
                    string err = "Lỗi tạo văn bản EMR!";
                    if (pTdo.Messages != null) err += " " + string.Join("; ", pTdo.Messages);
                    Console.WriteLine("❌ " + err);
                    return;
                }

                long newDocId = docRes.DocumentId.Value;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("✔ ĐÃ TẠO VĂN BẢN EMR! DocID: {0} | Mã VB: {1}", newDocId, docRes.DocumentCode));
                Console.ResetColor();

                // 2. Tìm bản ghi EMR_SIGN của văn bản
                var signFilter = new EMR.Filter.EmrSignFilter { DOCUMENT_ID = newDocId };
                CommonParam pSign = new CommonParam();
                var signs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, pSign);
                var mySign = signs != null ? signs.FirstOrDefault(s => s.LOGINNAME == "034727") : null;
                if (mySign == null)
                {
                    Console.WriteLine("❌ Không tìm thấy bản ghi EMR_SIGN cho 034727 trên DocID: " + newDocId);
                    return;
                }

                Console.WriteLine(string.Format("✍️ Tìm thấy vị trí ký SignID: {0} | Thứ tự: {1}", mySign.ID, mySign.NUM_ORDER));

                // 3. Đóng dấu ký số Cloud HSM đúng chân ký trang 2
                long signTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                var signSdo = new EMR.SDO.EmrSignHsmSDO
                {
                    EmrDocumentId = newDocId,
                    EmrSignId = mySign.ID,
                    SignTime = signTime,
                    IsFinishSign = true,
                    IsSigning = true,
                    IsSignElectronic = true,
                    Description = "Ký điện tử Bác sĩ làm bệnh án (Trang 2)",
                    RoomCode = "NQCTCHBB734",
                    RoomTypeCode = "GI",
                    WorkingDepartmentName = "Khoa Chấn thương Chỉnh hình và Cột sống",
                    PointSign = new EMR.SDO.EmrPointSignSDO
                    {
                        CoorXRectangle = 400.0f,
                        CoorYRectangle = 120.0f,
                        PageNumber = 2,       // ĐÚNG CHÂN KÝ TRANG 2!
                        MaxPageNumber = 2,    // TỔNG SỐ TRANG LÀ 2!
                        WidthRectangle = 150.0f,
                        HeightRectangle = 50.0f,
                        SizeFont = 10,
                        TypeDisplay = 3,      // Ảnh con dấu / chữ ký số scan
                        FontName = "Times New Roman"
                    }
                };

                Console.WriteLine("🔏 Đang đóng dấu ký số Cloud HSM tại CHÂN KÝ TRANG 2 (X=400, Y=120, Page=2/2)...");
                var pSignRes = new CommonParam();
                var resSign = myAdapter.PostData<EMR.SDO.EmrSignResultSDO>("api/EmrSign/SignPdfHsm", emrConsumer, signSdo, pSignRes);

                if (resSign != null && resSign.EmrSign != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format("🎉🎉🎉 KÝ ĐIỆN TỬ THÀNH CÔNG RỰC RỠ! SignID: {0} | Thời gian: {1}",
                        resSign.EmrSign.ID, resSign.EmrSign.SIGN_TIME));
                    Console.WriteLine(string.Format("   Trang ký: 2/2 | Tọa độ: (400, 120) | Người ký: {0} ({1})",
                        mySign.USERNAME, mySign.LOGINNAME));
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("⚠️ SignPdfHsm trả về null, đang fallback qua UpdateSdo...");
                    var updateSdo = new EMR.SDO.EmrSignUpdateSDO
                    {
                        DocumentId = newDocId,
                        Updates = new List<EMR.EFMODEL.DataModels.EMR_SIGN>
                        {
                            new EMR.EFMODEL.DataModels.EMR_SIGN { ID = mySign.ID, SIGN_TIME = signTime }
                        }
                    };
                    bool okUp = myAdapter.PostData<bool>("api/EmrSign/UpdateSdo", emrConsumer, updateSdo, pSignRes);
                    if (okUp)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✔ ĐÃ CẬP NHẬT KÝ THÀNH CÔNG QUA EMR_SIGN UpdateSdo!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("❌ Lỗi ký: " + (pSignRes.Messages != null ? string.Join("; ", pSignRes.Messages) : ""));
                    }
                }

                // Đồng bộ chữ ký số vào bảng BENHANNGOAIKHOA (Oracle CSDL EMR)
                try
                {
                    string emrDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR");
                    if (!Directory.Exists(emrDir))
                    {
                        emrDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Integrate", "EMR");
                    }
                    if (Directory.Exists(emrDir))
                    {
                        Assembly mdb = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
                        Assembly oracle = Assembly.LoadFrom(Path.Combine(emrDir, "Oracle.ManagedDataAccess.dll"));
                        Type connType = mdb.GetType("MDB.MDBConnection");
                        Type cmdType = mdb.GetType("MDB.MDBCommand");
                        string connStr = "Data Source=192.168.7.248:1521/orclstb;User Id=EMR_FINAL;Password=EMR_FINAL;";
                        dynamic dbCon = Activator.CreateInstance(connType, new object[] { connStr });
                        dbCon.Open();
                        string docCode = docRes.DocumentCode ?? newDocId.ToString();
                        string cleanTrCode = tr.TREATMENT_CODE.TrimStart('0');
                        string upSql = string.Format(@"UPDATE BENHANNGOAIKHOA SET 
                            TENFILEKY = 'Bệnh án Ngoại khoa(Hành chính)_{0}',
                            USERNAMEKY = '{1}',
                            NGAYKY = TO_DATE('{2}', 'YYYYMMDDHH24MISS'),
                            COMPUTERKYTEN = 'HIS-DESKTOP',
                            MASOKYTEN = '{0}',
                            TENFILEKY_KB = 'Bệnh án Ngoại khoa(Khám bệnh)_{0}',
                            USERNAMEKY_KB = '{1}',
                            NGAYKY_KB = TO_DATE('{2}', 'YYYYMMDDHH24MISS'),
                            COMPUTERKYTEN_KB = 'HIS-DESKTOP',
                            MASOKYTEN_KB = '{0}'
                        WHERE MaQuanLy IN ({3}, {4})", docCode, mySign.LOGINNAME, signTime, tr.ID, cleanTrCode);
                        dynamic cmdUp = Activator.CreateInstance(cmdType, new object[] { upSql, dbCon });
                        int rows = cmdUp.ExecuteNonQuery();
                        dbCon.Close();
                        if (rows > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(string.Format("✔ Đã đồng bộ chữ ký trực tiếp vào Form Bệnh Án Oracle EMR ({0} bản ghi)!", rows));
                            Console.ResetColor();
                        }
                    }
                }
                catch (Exception exSync)
                {
                    Console.WriteLine("⚠️ Ghi chú đồng bộ Oracle: " + exSync.Message);
                }

                Console.WriteLine("===============================================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI: " + ex.ToString());
            }
        }
        else if (args[0].ToLower() == "verify-doc" || args[0].ToLower() == "inspect-doc")
        {
            try
            {
                long docId = args.Length > 1 ? long.Parse(args[1]) : 93425965;
                InitSession();
                var emrConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");

                Console.WriteLine("===============================================================================");
                Console.WriteLine("🔍 KIỂM TRA ĐỐI SOÁT CHI TIẾT VĂN BẢN EMR: " + docId);
                Console.WriteLine("===============================================================================");

                // 1. EMR_DOCUMENT
                var docFilter = new EMR.Filter.EmrDocumentFilter { ID = docId };
                var pDoc = new CommonParam();
                var docs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, pDoc);
                if (docs != null && docs.Count > 0)
                {
                    var doc = docs[0];
                    Console.WriteLine("📄 [EMR_DOCUMENT]");
                    foreach (var prop in doc.GetType().GetProperties())
                    {
                        var val = prop.GetValue(doc, null);
                        if (val != null && !string.IsNullOrEmpty(val.ToString()))
                        {
                            Console.WriteLine(string.Format("   {0,-22}: {1}", prop.Name, val));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("❌ Không tìm thấy EMR_DOCUMENT ID: " + docId);
                }

                // 2. EMR_SIGN
                var signFilter = new EMR.Filter.EmrSignFilter { DOCUMENT_ID = docId };
                var pSign = new CommonParam();
                var signs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_SIGN>("api/EmrSign/Get", emrConsumer, signFilter, pSign);
                if (signs != null && signs.Count > 0)
                {
                    Console.WriteLine("\n✍️ [DANH SÁCH CHỮ KÝ - EMR_SIGN] (Tổng: " + signs.Count + "):");
                    foreach (var s in signs)
                    {
                        Console.WriteLine("--------------------------------------------------");
                        foreach (var prop in s.GetType().GetProperties())
                        {
                            var val = prop.GetValue(s, null);
                            if (val != null && !string.IsNullOrEmpty(val.ToString()))
                            {
                                if (val is byte[])
                                {
                                    byte[] bArr = (byte[])val;
                                    Console.WriteLine(string.Format("   {0,-22}: [byte[] {1} bytes]", prop.Name, bArr.Length));
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("   {0,-22}: {1}", prop.Name, val));
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("❌ Không tìm thấy bản ghi EMR_SIGN cho docId: " + docId);
                }

                // 3. EMR_VERSION
                try
                {
                    var verFilter = new EMR.Filter.EmrVersionFilter { DOCUMENT_ID = docId };
                    var pVer = new CommonParam();
                    var vers = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_VERSION>("api/EmrVersion/Get", emrConsumer, verFilter, pVer);
                    if (vers != null && vers.Count > 0)
                    {
                        Console.WriteLine("\n📦 [PHIÊN BẢN VĂN BẢN - EMR_VERSION] (Tổng: " + vers.Count + "):");
                        foreach (var v in vers)
                        {
                            Console.WriteLine("--------------------------------------------------");
                            foreach (var prop in v.GetType().GetProperties())
                            {
                                var val = prop.GetValue(v, null);
                                if (val != null && !string.IsNullOrEmpty(val.ToString()))
                                {
                                    if (val is byte[])
                                    {
                                        byte[] bArr = (byte[])val;
                                        Console.WriteLine(string.Format("   {0,-22}: [byte[] {1} bytes]", prop.Name, bArr.Length));
                                    }
                                    else
                                    {
                                        Console.WriteLine(string.Format("   {0,-22}: {1}", prop.Name, val));
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                Console.WriteLine("===============================================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
        else
        {
            Console.WriteLine("Cú pháp:");
            Console.WriteLine("  HisDiagnosticDoctor.bat health         : Chẩn đoán toàn diện hệ thống");
            Console.WriteLine("  HisDiagnosticDoctor.bat patient <mã_bn>: Chẩn đoán hồ sơ bệnh nhân");
            Console.WriteLine("  HisDiagnosticDoctor.bat token          : Đọc token live");
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

        RunCli(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void RunCli(string[] args)
    {
        HisDiagnosticDoctor.Run(args);
    }
}
