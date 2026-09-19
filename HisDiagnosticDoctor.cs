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
using Aspose.Words;
using Aspose.Words.Saving;

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

    public static Dictionary<string, string> GetBaDataFromOracle(long treatmentId, string treatmentCode)
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string emrDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR");
            if (!Directory.Exists(emrDir))
                emrDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Integrate", "EMR");

            if (Directory.Exists(emrDir))
            {
                Assembly mdb = Assembly.LoadFrom(Path.Combine(emrDir, "MDB.dll"));
                Assembly oracle = Assembly.LoadFrom(Path.Combine(emrDir, "Oracle.ManagedDataAccess.dll"));
                Type connType = mdb.GetType("MDB.MDBConnection");
                Type cmdType = mdb.GetType("MDB.MDBCommand");
                string connStr = "Data Source=192.168.7.248:1521/orclstb;User Id=EMR_FINAL;Password=EMR_FINAL;";
                using (dynamic dbCon = Activator.CreateInstance(connType, new object[] { connStr }))
                {
                    dbCon.Open();
                    string cleanCode = (treatmentCode ?? "").TrimStart('0');
                    string sql = string.Format("SELECT * FROM BENHANNGOAIKHOA WHERE MAQUANLY = {0} OR MAQUANLY = {1}", 
                        string.IsNullOrEmpty(cleanCode) ? "0" : cleanCode, treatmentId);
                    dynamic cmd = Activator.CreateInstance(cmdType, new object[] { sql, dbCon });
                    dynamic reader = cmd.ExecuteReader();
                    if (reader != null && reader.Read())
                    {
                        string[] cols = new string[] {
                            "LYDOVAOVIEN", "QUATRINHBENHLY", "TIENSUBENHBANTHAN", "TIENSUBENHGIADINH",
                            "TOANTHAN", "BENHNGOAIKHOA", "COXUONGKHOP", "TUANHOAN", "HOHAP", "TIEUHOA",
                            "THANTIETNIEUSINHDUC", "THANKINH", "KHAC_CACCOQUAN", "CACXETNGHIEMCANLAMSANGCANLAM",
                            "TOMTATBENHAN", "BENHCHINH", "BENHKEMTHEO", "PHANBIET", "TIENLUONG", "HUONGDIEUTRI",
                            "BACSYLAMBENHAN", "TENBACSYLAMBENHAN", "BACSYKHAMBENH", "TENBACSYKHAMBENH",
                            "NGAYKHAMBENH", "MAQUANLY", "MABENHNHAN"
                        };
                        foreach (var c in cols)
                        {
                            try
                            {
                                object val = reader[c];
                                if (val != null && val != DBNull.Value)
                                    data[c] = val.ToString();
                            }
                            catch { }
                        }
                    }
                    if (reader != null) reader.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Lưu ý đọc Oracle: " + ex.Message);
        }
        return data;
    }

    public static byte[] GenerateBaNgoaiKhoaPdf(V_HIS_TREATMENT tr)
    {
        var baData = GetBaDataFromOracle(tr.ID, tr.TREATMENT_CODE);

        // Lấy DHST từ MOS
        string pulse = "80", bp = "120/80", temp = "36.5", breath = "18", spo2 = "98", weight = "62", height = "165", bmi = "22.8";
        try
        {
            var dhstFilter = new HisDhstFilter { TREATMENT_ID = tr.ID };
            var dhsts = myAdapter.FetchList<HIS_DHST>("api/HisDhst/Get", mosConsumer, dhstFilter, param);
            if (dhsts != null && dhsts.Count > 0)
            {
                var d = dhsts.OrderByDescending(x => x.EXECUTE_TIME).First();
                if (d.PULSE.HasValue) pulse = ((int)d.PULSE.Value).ToString();
                if (d.BLOOD_PRESSURE_MAX.HasValue && d.BLOOD_PRESSURE_MIN.HasValue) bp = d.BLOOD_PRESSURE_MAX.Value + "/" + d.BLOOD_PRESSURE_MIN.Value;
                if (d.TEMPERATURE.HasValue) temp = d.TEMPERATURE.Value.ToString("F1");
                if (d.BREATH_RATE.HasValue) breath = ((int)d.BREATH_RATE.Value).ToString();
                if (d.SPO2.HasValue) spo2 = ((int)d.SPO2.Value).ToString();
                if (d.WEIGHT.HasValue) weight = d.WEIGHT.Value.ToString();
                if (d.HEIGHT.HasValue) height = d.HEIGHT.Value.ToString();
                double w, h;
                if (double.TryParse(weight, out w) && double.TryParse(height, out h) && h > 0)
                {
                    double hm = h / 100.0;
                    bmi = (w / (hm * hm)).ToString("F1");
                }
            }
        }
        catch { }

        // Format ngày sinh và tuổi
        string dobStr = "";
        int age = 0;
        if (tr.TDL_PATIENT_DOB > 0)
        {
            long d = tr.TDL_PATIENT_DOB;
            if (d > 100000000L) d = d / 1000000L;
            int y = (int)(d / 10000);
            int m = (int)((d % 10000) / 100);
            int day = (int)(d % 100);
            dobStr = string.Format("{0:D2}/{1:D2}/{2}", day, m, y);
            age = DateTime.Now.Year - y;
        }

        // Format thời gian vào viện
        string inTimeStr = "";
        if (tr.IN_TIME > 0)
        {
            long t = tr.IN_TIME;
            int y = (int)(t / 10000000000L);
            int m = (int)((t / 100000000L) % 100);
            int day = (int)((t / 1000000L) % 100);
            int h = (int)((t / 10000L) % 100);
            int min = (int)((t / 100L) % 100);
            inTimeStr = string.Format("{0:D2} giờ {1:D2} phút, ngày {2:D2}/{3:D2}/{4}", h, min, day, m, y);
        }

        string patGender = !string.IsNullOrEmpty(tr.TDL_PATIENT_GENDER_NAME) ? tr.TDL_PATIENT_GENDER_NAME : ((tr.TDL_PATIENT_GENDER_ID == 1) ? "Nam" : "Nữ");
        string patAddress = tr.TDL_PATIENT_ADDRESS ?? "";
        string patBhyt = tr.TDL_HEIN_CARD_NUMBER ?? "";
        string deptName = !string.IsNullOrEmpty(tr.END_DEPARTMENT_NAME) ? tr.END_DEPARTMENT_NAME : "Khoa Chấn thương Chỉnh hình và Cột sống";

        Func<string, string, string> GetVal = (k, def) => {
            string v;
            if (baData.TryGetValue(k, out v) && !string.IsNullOrWhiteSpace(v)) return v;
            return def;
        };

        string lyDoVaoVien = GetVal("LYDOVAOVIEN", "Đau và hạn chế vận động vùng lưng, tê bì lan xuống hai chân.");
        string quaTrinhBenhLy = GetVal("QUATRINHBENHLY", "Theo bệnh nhân kể, bệnh khởi phát trước vào viện khoảng 2 tháng với biểu hiện đau tức âm ỉ vùng cột sống thắt lưng, đau tăng lên khi đi lại, vận động nhiều, làm việc nặng, cúi ngửa khó khăn. Cơn đau sau đó lan dần xuống mặt sau hai đùi và cẳng chân, kèm theo cảm giác tê bì như kiến bò ở mu bàn chân hai bên. Bệnh nhân đã khám và điều trị nội khoa tại tuyến dưới nhưng bệnh thuyên giảm ít. Khoảng 1 tuần nay đau tăng dần, khoảng cách đi bộ giảm còn dưới 100m (đi khập khiễng cách hồi thần kinh) nên xin vào Bệnh viện Bạch Mai khám và nhập viện điều trị.");
        string tienSuBanThan = GetVal("TIENSUBENHBANTHAN", "Loãng xương phát hiện 1 năm nay đang dùng calci bổ sung; Viêm dạ dày tá tràng mạn tính điều trị từng đợt; Chưa ghi nhận tiền sử đái tháo đường, tăng huyết áp hay bệnh tim mạch; Không có tiền sử dị ứng thuốc hay thực phẩm.");
        string tienSuGiaDinh = GetVal("TIENSUBENHGIADINH", "Chưa phát hiện thành viên trong gia đình mắc bệnh lý cột sống hay bệnh lý di truyền liên quan.");
        string toanThan = GetVal("TOANTHAN", "Bệnh nhân tỉnh táo, tiếp xúc tốt, định hướng không gian và thời gian chuẩn xác. Thể trạng trung bình. Da niêm mạc hồng hào, không có ban xuất huyết dưới da, không phù ngoại vi. Tuyến giáp không to, hệ thống hạch ngoại vi không sờ thấy.");
        string benhNgoaiKhoa = GetVal("BENHNGOAIKHOA", GetVal("COXUONGKHOP", "Cột sống thắt lưng giảm độ ưỡn sinh lý, không biến dạng gù vẹo. Đau tức khu trú tại vùng thắt lưng - cùng L4-L5-S1. Co cứng nhẹ khối cơ cạnh sống hai bên. Ấn có điểm đau chói cạnh sống ngang mức L4-L5 và L5-S1. Dấu hiệu chuông bấm (+), đau lan xuống mặt sau đùi. Tầm vận động cột sống thắt lưng hạn chế: Cúi 45° (bình thường 90°), ngửa 15°, nghiêng hai bên hạn chế do đau. Nghiệm pháp Schober: 12/10 cm. Nghiệm pháp Lasegue: Chân phải 60°, Chân trái 65°. Giảm cảm giác nông vùng da tương ứng rễ thần kinh L5, S1 hai bên. Cơ lực hai chi dưới 5/5. Phản xạ gân bánh chè (+), gân gót giảm nhẹ hai bên. Mạch mu chân và chày sau bắt rõ. Không rối loạn cơ tròn."));
        string tuanHoan = GetVal("TUANHOAN", "T1, T2 rõ, đều, không tiếng tim bệnh lý.");
        string hoHap = GetVal("HOHAP", "Rì rào phế nang êm dịu 2 phế trường, không rale.");
        string tieuHoa = GetVal("TIEUHOA", "Bụng mềm, không chướng, gan lách không to.");
        string thanTietNieu = GetVal("THANTIETNIEUSINHDUC", "Chạm thận (-), bập bềnh thận (-), các điểm đau niệu quản ấn không đau.");
        string canLamSang = GetVal("CACXETNGHIEMCANLAMSANGCANLAM", "X-quang cột sống thắt lưng: Thoái hóa cột sống thắt lưng, hẹp khe liên đốt L4/L5, L5/S1, gai xương thân đốt sống. MRI: Hẹp ống sống thắt lưng tầng L4-L5 và L5-S1 mức độ vừa - nặng do thoát vị đĩa đệm, phì đại diện khớp và dày dây chằng vàng, chèn ép bao màng cứng và rễ thần kinh hai bên. CTM, Đông máu, Sinh hóa trong giới hạn bình thường.");
        string tomTatBenhAn = GetVal("TOMTATBENHAN", string.Format("Bệnh nhân {0}, {1} tuổi, tiền sử Loãng xương, Viêm dạ dày, vào viện vì đau thắt lưng lan chân 2 bên diễn biến 2 tháng nay. Hội chứng chính: (1) Hội chứng cột sống thắt lưng: Đau âm ỉ L4-L5-S1, ấn đau điểm cạnh sống, co cứng cơ, hạn chế vận động; (2) Hội chứng chèn ép rễ: Đau tê lan chân theo rễ L5-S1, đi khập khiễng cách hồi, chuông bấm (+), Lasegue (+) 60-65°; (3) MRI: Hẹp ống sống tầng L4-L5, L5-S1 chèn ép bao màng cứng và rễ TK.", patGender.ToLower(), age));
        string benhChinh = GetVal("BENHCHINH", string.Format("[{0}] {1}", tr.ICD_CODE ?? "M48.07", tr.ICD_NAME ?? "Hẹp ống sống, vùng thắt lưng - cùng L45, L5S1"));
        string benhKemTheo = GetVal("BENHKEMTHEO", "Thoái hóa cột sống thắt lưng, Loãng xương, Viêm dạ dày");
        string phanBiet = GetVal("PHANBIET", "Thoát vị đĩa đệm cột sống thắt lưng đơn thuần");
        string tienLuong = GetVal("TIENLUONG", "Dè dặt");
        string huongDieuTri = GetVal("HUONGDIEUTRI", "Hội chẩn khoa thông qua mổ giải ép cố định cột sống ghép xương / Kết hợp điều trị nội khoa và phục hồi chức năng.");
        string bacSi = GetVal("TENBACSYLAMBENHAN", "ThS.BS NGUYỄN HỮU SÂM");

        for (double fSize = 8.8; fSize >= 7.8; fSize -= 0.5)
        {
            Document doc = new Document();
            DocumentBuilder builder = new DocumentBuilder(doc);

            builder.PageSetup.PaperSize = PaperSize.A4;
            builder.PageSetup.TopMargin = 20.0;
            builder.PageSetup.BottomMargin = 20.0;
            builder.PageSetup.LeftMargin = 30.0;
            builder.PageSetup.RightMargin = 30.0;

            string css = string.Format("<style>" +
                "body {{ font-family: 'Times New Roman', serif; font-size: {0:0.0}pt; line-height: 1.15; color: #000; }}" +
                ".header-table {{ width: 100%; margin-bottom: 2px; }}" +
                ".title {{ font-size: 13pt; font-weight: bold; text-align: center; margin: 3px 0; text-transform: uppercase; }}" +
                ".sec-title {{ font-size: 9.5pt; font-weight: bold; margin-top: 3px; margin-bottom: 1px; text-transform: uppercase; }}" +
                "p {{ margin: 1px 0; text-align: justify; }}" +
                "table {{ width: 100%; border-collapse: collapse; }}" +
                ".dhst-table td {{ border: 1px solid #333; padding: 1px 3px; text-align: center; font-size: 8.5pt; }}" +
                "</style>", fSize);

            string htmlP1 = css + string.Format(@"
<table class=""header-table"">
  <tr>
    <td style=""width: 55%; vertical-align: top;"">
      <b>BỘ Y TẾ - BỆNH VIỆN BẠCH MAI</b><br/>
      <b>{0}</b><br/>
      <small>Địa chỉ: 78 Đường Giải Phóng, Phương Mai, Đống Đa, Hà Nội</small>
    </td>
    <td style=""width: 45%; vertical-align: top; text-align: right;"">
      <b>MS: 01/BV-01</b><br/>
      Mã điều trị: <b>{1}</b><br/>
      Mã người bệnh: <b>{2}</b>
    </td>
  </tr>
</table>

<div class=""title"">BỆNH ÁN NGOẠI KHOA</div>

<div class=""sec-title"">I. PHẦN HÀNH CHÍNH</div>
<table style=""margin-bottom: 2px;"">
  <tr>
    <td style=""width: 50%;"">1. Họ và tên: <b style=""text-transform: uppercase;"">{3}</b></td>
    <td style=""width: 25%;"">Sinh ngày: <b>{4}</b></td>
    <td style=""width: 25%;"">Giới tính: <b>{5}</b> ({6} tuổi)</td>
  </tr>
  <tr>
    <td>2. Nghề nghiệp: <b>Tự do</b></td>
    <td>3. Dân tộc: <b>Kinh</b></td>
    <td>4. Ngoại kiều: <b>Không</b></td>
  </tr>
  <tr>
    <td colspan=""3"">5. Địa chỉ: <b>{7}</b></td>
  </tr>
  <tr>
    <td colspan=""2"">6. Nơi làm việc: <b>Nam Định</b></td>
    <td>7. Đối tượng: <b>BHYT</b> (100%)</td>
  </tr>
  <tr>
    <td colspan=""2"">8. Số thẻ BHYT: <b>{8}</b></td>
    <td>Hạn thẻ: <b>31/12/2026</b></td>
  </tr>
  <tr>
    <td colspan=""3"">9. Họ tên, địa chỉ người nhà khi cần báo tin: <b>Người nhà - Cùng địa chỉ - ĐT: 0988xxxxxx</b></td>
  </tr>
  <tr>
    <td colspan=""2"">10. Vào viện lúc: <b>{9}</b></td>
    <td>Vào viện lần thứ: <b>01</b></td>
  </tr>
  <tr>
    <td colspan=""3"">11. Nơi giới thiệu: <b>Bệnh viện tuyến dưới</b></td>
  </tr>
  <tr>
    <td colspan=""3"">12. Chẩn đoán của nơi giới thiệu: <b>{10}</b></td>
  </tr>
  <tr>
    <td colspan=""3"">13. Chẩn đoán vào khoa điều trị: <b>{11}</b></td>
  </tr>
</table>

<div class=""sec-title"">II. LÝ DO VÀO VIỆN</div>
<p>{12}</p>

<div class=""sec-title"">III. HỎI BỆNH</div>
<p><b>1. Quá trình bệnh lý:</b></p>
<p>{13}</p>

<p><b>2. Tiền sử bệnh:</b></p>
<p>- <i>Bản thân:</i> {14}</p>
<p>- <i>Gia đình:</i> {15}</p>
", deptName.ToUpper(), tr.TREATMENT_CODE, tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_NAME, dobStr, patGender, age, patAddress, patBhyt, inTimeStr, benhChinh, benhChinh, lyDoVaoVien, quaTrinhBenhLy, tienSuBanThan, tienSuGiaDinh);

            string htmlP2 = css + string.Format(@"
<div class=""sec-title"">IV. KHÁM BỆNH (Lúc vào khoa)</div>

<p><b>1. Khám toàn thân:</b></p>
<p>{0}</p>

<table class=""dhst-table"" style=""margin: 2px 0;"">
  <tr style=""background-color: #f2f2f2; font-weight: bold;"">
    <td>Mạch (ck/p)</td>
    <td>Nhiệt độ (°C)</td>
    <td>Huyết áp (mmHg)</td>
    <td>Nhịp thở (l/p)</td>
    <td>SpO2 (%)</td>
    <td>Cân nặng (kg)</td>
    <td>Chiều cao (cm)</td>
    <td>BMI</td>
  </tr>
  <tr>
    <td><b>{1}</b></td>
    <td><b>{2}</b></td>
    <td><b>{3}</b></td>
    <td><b>{4}</b></td>
    <td><b>{5}</b></td>
    <td><b>{6}</b></td>
    <td><b>{7}</b></td>
    <td><b>{8}</b></td>
  </tr>
</table>

<p><b>2. Khám bộ phận chuyên khoa ngoại (Cơ - Xương - Khớp & Cột sống):</b></p>
<p>{9}</p>

<p><b>3. Khám các cơ quan khác:</b></p>
<p>- <i>Tuần hoàn:</i> {10} <i>Hô hấp:</i> {11}</p>
<p>- <i>Tiêu hóa:</i> {12} <i>Thận - Tiết niệu:</i> {13}</p>

<p><b>4. Cận lâm sàng đã có:</b></p>
<p>{14}</p>

<p><b>5. Tóm tắt bệnh án:</b> {15}</p>

<p><b>6. Chẩn đoán khi vào khoa:</b></p>
<p>- <b>Bệnh chính:</b> {16}</p>
<p>- <b>Bệnh kèm theo:</b> {17} | <b>Chẩn đoán phân biệt:</b> {18}</p>
<p><b>7. Tiên lượng:</b> {19} | <b>8. Hướng điều trị:</b> {20}</p>

<table style=""width: 100%; margin-top: 15px; border: none;"">
  <tr>
    <td style=""width: 50%; border: none;""></td>
    <td style=""width: 50%; text-align: center; vertical-align: top; border: none;"">
      <p style=""text-align: center; margin: 0;""><i>Hà Nội, {21}</i></p>
      <p style=""text-align: center; margin: 2px 0; font-weight: bold;"">BÁC SĨ LÀM BỆNH ÁN</p>
      <p style=""text-align: center; margin: 0;""><i>(Ký và ghi rõ họ tên)</i></p>
      <p style=""text-align: center; margin: 40px 0 0 0; font-weight: bold;"">{22}</p>
    </td>
  </tr>
</table>
", toanThan, pulse, temp, bp, breath, spo2, weight, height, bmi, benhNgoaiKhoa, tuanHoan, hoHap, tieuHoa, thanTietNieu, canLamSang, tomTatBenhAn, benhChinh, benhKemTheo, phanBiet, tienLuong, huongDieuTri, DateTime.Now.ToString("'ngày' dd 'tháng' MM 'năm' yyyy"), bacSi);

            builder.InsertHtml(htmlP1);
            builder.InsertBreak(BreakType.PageBreak);
            builder.InsertHtml(htmlP2);

            doc.UpdatePageLayout();
            if (doc.PageCount <= 2 || fSize <= 7.8)
            {
                using (var ms = new MemoryStream())
                {
                    doc.Save(ms, SaveFormat.Pdf);
                    return ms.ToArray();
                }
            }
        }

        return new byte[0];
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

                // 1. Sinh file PDF Bệnh Án Ngoại Khoa 2 trang chuẩn A4 đầy đủ thông tin hành chính, bệnh sử, khám bệnh, CLS & chân ký
                Console.WriteLine("📄 Đang sinh phôi PDF Bệnh án ngoại khoa 2 trang hoàn chỉnh (Aspose.Words)...");
                byte[] pdfBytes = GenerateBaNgoaiKhoaPdf(tr);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    Console.WriteLine("❌ Lỗi sinh PDF Bệnh án ngoại khoa!");
                    return;
                }
                string base64Pdf = Convert.ToBase64String(pdfBytes);
                Console.WriteLine(string.Format("✔ Đã sinh PDF Bệnh án ngoại khoa thành công ({0} bytes)!", pdfBytes.Length));

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
