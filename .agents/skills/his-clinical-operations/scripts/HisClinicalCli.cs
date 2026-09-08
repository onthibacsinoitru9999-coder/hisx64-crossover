using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
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

public class ServiceTarget
{
    public long ServiceId { get; set; }
    public long RoomId { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceName { get; set; }
    public string Note { get; set; }
    public long? ConditionId { get; set; }

    public ServiceTarget(long sId, long rId, string code = "", string name = "", string note = "", long? condId = null)
    {
        ServiceId = sId;
        RoomId = rId;
        ServiceCode = code;
        ServiceName = name;
        Note = note;
        ConditionId = condId;
    }
}

public class HisClinicalCli
{
    public static BackendAdapter adapter;
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static ApiConsumer mosConsumer;
    public static ApiConsumer sdaConsumer;
    public static string currentToken = null;
    public static string currentDoctorLogin = "034727";
    public static string currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";

    public static readonly Dictionary<string, ServiceTarget> PredefinedServices = new Dictionary<string, ServiceTarget>
    {
        { "TROPONIN_THS", new ServiceTarget(63596, 410, "BM260527.26", "Định lượng Troponin Ths (Sau 28/5/2026)") },
        { "TROPONIN_OLD", new ServiceTarget(5920, 410, "BM02298", "Định lượng Troponin Ths (Trước 28/5/2026)") },
        { "KHI_MAU", new ServiceTarget(5886, 410, "BM02047", "Xét nghiệm Khí máu 11 thông số") },
        { "CBC_LASER", new ServiceTarget(5745, 1772, "BM00110", "Tổng phân tích tế bào máu laser") },
        { "COAGULATION", new ServiceTarget(2658, 1773, "BM00024", "Đông máu cơ bản") },
        { "FIBRINOGEN", new ServiceTarget(5716, 626, "BM00542", "Định lượng Fibrinogen (Clauss tự động)") },
        { "PT_TQ", new ServiceTarget(5713, 626, "BM00531", "Thời gian prothrombin (PT/TQ tự động)") },
        { "APTT_TCK", new ServiceTarget(63622, 626, "BM260527.52", "Thời gian APTT/TCK tự động") },
        { "BLOOD_GROUP_GEL", new ServiceTarget(5783, 1464, "BM01700", "Định nhóm máu hệ ABO, Rh(D) (Gelcard tự động)") },
        { "URE", new ServiceTarget(5923, 410, "BM02304", "Định lượng Urê [Máu]") },
        { "CREATININ", new ServiceTarget(5934, 410, "BM01361", "Định lượng Creatinin (máu)") },
        { "GOT", new ServiceTarget(5834, 410, "BM01352", "Đo hoạt độ AST (GOT)") },
        { "GPT", new ServiceTarget(5833, 410, "BM01347", "Đo hoạt độ ALT (GPT)") },
        { "ELECTROLYTES", new ServiceTarget(5853, 410, "BM00132", "Điện giải đồ (Na, K, Cl)") },
        { "HBA1C", new ServiceTarget(5870, 410, "BM01429", "Định lượng HbA1c", "", 4723) },
        { "URINE_10", new ServiceTarget(5950, 566, "BM02998", "Tổng phân tích nước tiểu (tự động)") },
        { "HBSAG", new ServiceTarget(6135, 871, "BM00859", "HBsAg miễn dịch tự động") },
        { "HCV_AB", new ServiceTarget(6147, 871, "BM00837", "HCV Ab miễn dịch tự động") },
        { "HIV_AB", new ServiceTarget(6020, 871, "BM00871", "HIV Ag/Ab miễn dịch tự động") },
        { "ECG", new ServiceTarget(920, 931, "BM04258", "Điện tim thường (ECG)") },
        { "ECHO_HEART", new ServiceTarget(5569, 1715, "BM00201", "Siêu âm Doppler tim, van tim", "điều dưỡng đưa bằng cáng - cs ii") },
        { "DEXA_2POS", new ServiceTarget(161, 6462, "BM08085", "Đo mật độ xương DEXA [2 vị trí]", "điều dưỡng đưa bằng cáng - cs ii") },
        { "XRAY_CHEST", new ServiceTarget(58112, 17552, "BM21074", "X-quang ngực thẳng số hóa") },
        { "GLUCOSE_BEDSIDE", new ServiceTarget(6217, 5248, "BM02426", "Xét nghiệm đường máu mao mạch tại giường (một lần)") }
    };

    public static string ReadLiveTokenFast()
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

    public static void InitSession(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(currentToken)) return;

        param = new CommonParam();
        string tokenCode = ReadLiveTokenFast();

        if (string.IsNullOrEmpty(tokenCode))
        {
            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    currentDoctorLogin = "034727";
                    currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";
                }
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null)
                    {
                        tokenCode = token.TokenCode;
                        currentDoctorLogin = "vmc";
                        currentDoctorName = "BS Vũ Minh Cường";
                    }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            throw new Exception("Không thể lấy Token xác thực HIS từ cả Live Log và ACS Login!");
        }

        currentToken = tokenCode;
        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", currentToken, "HIS");
        sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", currentToken, "HIS");
        adapter = new BackendAdapter(param);

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
            var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        }
        catch { }
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    public static List<string> GenerateSearchVariants(string input)
    {
        var variants = new List<string>();
        if (string.IsNullOrEmpty(input)) return variants;

        string original = input.Trim();
        variants.Add(original);

        // Biến thể dấu thanh tiếng Việt kiểu truyền thống vs hiện đại (hòa <-> hoà, hóa <-> hoá...)
        string[,] pairs = new string[,] {
            { "òa", "oà" }, { "óa", "oá" }, { "ỏa", "oả" }, { "õa", "oã" }, { "ọa", "oạ" },
            { "ÒA", "OÀ" }, { "ÓA", "OÁ" }, { "ỎA", "OẢ" }, { "ÕA", "OÃ" }, { "ỌA", "OẠ" },
            { "òe", "oè" }, { "óe", "oé" }, { "ỏe", "oẻ" }, { "õe", "oẽ" }, { "ọe", "oẹ" },
            { "ùy", "uỳ" }, { "úy", "uý" }, { "ủy", "uỷ" }, { "ũy", "uỹ" }, { "ụy", "uỵ" },
            { "ÙY", "UỲ" }, { "ÚY", "UÝ" }, { "ỦY", "UỶ" }, { "ŨY", "UỸ" }, { "ỤY", "UỴ" }
        };

        for (int i = 0; i < pairs.GetLength(0); i++)
        {
            string from = pairs[i, 0];
            string to = pairs[i, 1];
            if (original.Contains(from))
            {
                string v = original.Replace(from, to);
                if (!variants.Contains(v)) variants.Add(v);
            }
            if (original.Contains(to))
            {
                string v = original.Replace(to, from);
                if (!variants.Contains(v)) variants.Add(v);
            }
        }

        // Biến thể không dấu
        string unaccented = RemoveDiacritics(original);
        if (!string.IsNullOrEmpty(unaccented) && !variants.Contains(unaccented))
        {
            variants.Add(unaccented);
        }

        return variants;
    }

    public static void LookupPatient(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;

        // 1. Try exact match by Patient Code or Treatment Code
        if (!string.IsNullOrEmpty(keyword))
        {
            string kw = keyword.Trim();
            long numVal;
            bool isNum = long.TryParse(kw, out numVal);

            if (isNum)
            {
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
                tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter();
                    tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                    treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }
            }
            else
            {
                // Thử tìm theo PATIENT_CODE hoặc TREATMENT_CODE chữ
                HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

                if (treatments == null || treatments.Count == 0)
                {
                    tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                    treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
                }

                if (treatments == null || treatments.Count == 0)
                {
                    // BƯỚC 1: Quét nhanh trong danh sách bệnh nhân đang nằm buồng (In-memory, Siêu tốc < 0.1s, loại bỏ 100% rào cản dấu)
                    string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
                    try
                    {
                        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter
                        {
                            IS_IN_ROOM = true,
                            TREATMENT_IS_ACTIVE = true
                        };
                        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                        if (allBeds != null && allBeds.Count > 0)
                        {
                            var matchedBeds = allBeds.Where(b => 
                                b.DEPARTMENT_ID == 57 &&
                                !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) &&
                                RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)
                            ).ToList();

                            // Nếu không có ở Khoa 57, tìm ở các khoa khác
                            if (matchedBeds.Count == 0)
                            {
                                matchedBeds = allBeds.Where(b => 
                                    !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) &&
                                    RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)
                                ).ToList();
                            }

                            if (matchedBeds.Count > 0)
                            {
                                var tIds = matchedBeds.Select(x => x.TREATMENT_ID).Distinct().ToList();
                                HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter { IDs = tIds };
                                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
                            }
                        }
                    }
                    catch { }
                }

                if (treatments == null || treatments.Count == 0)
                {
                    // BƯỚC 2: Tự động tìm kiếm qua các biến thể dấu thanh (hòa <-> hoà) và không dấu trên MOS
                    var variants = GenerateSearchVariants(kw);
                    var allFound = new List<V_HIS_TREATMENT>();
                    var seenIds = new HashSet<long>();

                    foreach (var v in variants)
                    {
                        var tf = new HisTreatmentViewFilter { KEY_WORD = v, IS_PAUSE = false };
                        var res = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                        if (res != null)
                        {
                            foreach (var item in res)
                            {
                                if (seenIds.Add(item.ID)) allFound.Add(item);
                            }
                        }
                        if (allFound.Count > 0 && !v.Contains("òa") && !v.Contains("oà")) break;
                    }
                    treatments = allFound;
                }
            }
        }

        // 2. Check if keyword is a Bed Room search (e.g. 712, 714, 716...)
        if (treatments == null || treatments.Count == 0)
        {
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
            var deptBedRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
            var matchedRooms = deptBedRooms != null ? deptBedRooms.Where(x => x.BED_ROOM_NAME.Contains(keyword) || x.BED_ROOM_CODE.Contains(keyword)).ToList() : null;

            if (matchedRooms != null && matchedRooms.Count > 0)
            {
                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("🏨 DANH SÁCH BỆNH NHÂN THEO BUỒNG: {0}", keyword));
                Console.WriteLine("===============================================================================");
                foreach (var rm in matchedRooms)
                {
                    HisTreatmentBedRoomLViewFilter tbf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = rm.ID, IS_IN_ROOM = true };
                    var pts = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbf, param);
                    if (pts != null && pts.Count > 0)
                    {
                        Console.WriteLine(string.Format("\n📍 {0} ({1} bệnh nhân):", rm.BED_ROOM_NAME, pts.Count));
                        foreach (var p in pts.OrderBy(x => x.BED_NAME))
                        {
                            Console.WriteLine(string.Format("  👉 [{0}] {1} (Mã BN: {2} | Mã ĐT: {3})", 
                                p.BED_NAME ?? "Giường -", p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("\n📍 {0}: Không có bệnh nhân nằm ghép.", rm.BED_ROOM_NAME));
                    }
                }
                Console.WriteLine("===============================================================================");
                return;
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        // CẢNH BÁO ĐỐI SOÁT KHI CÓ NHIỀU BỆNH NHÂN TRÙNG TÊN / TRÙNG TỪ KHÓA
        var activeTreatments = treatments.Where(x => x.IS_PAUSE != 1).ToList();
        if (activeTreatments.Count > 1)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("⚠️ CẢNH BÁO ĐỐI SOÁT TRÙNG TÊN: Tìm thấy {0} bệnh nhân đang nằm viện khớp với '{1}':", activeTreatments.Count, keyword));
            Console.WriteLine("-------------------------------------------------------------------------------");
            int idx = 1;
            foreach (var at in activeTreatments)
            {
                Console.WriteLine(string.Format("  {0}. [{1}] Mã BN: {2} | Mã ĐT: {3} | Khoa: {4}",
                    idx++, at.TDL_PATIENT_NAME, at.TDL_PATIENT_CODE, at.TREATMENT_CODE, at.END_DEPARTMENT_NAME ?? "Khoa 57"));
                Console.WriteLine(string.Format("     Chẩn đoán: [{0}] {1}", at.ICD_CODE, at.ICD_NAME));
            }
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("👉 Chi tiết bên dưới hiển thị hồ sơ ưu tiên tại Khoa 57:");
            Console.WriteLine("===============================================================================");
        }

        var tr = treatments.LastOrDefault(x => x.IS_PAUSE != 1 && (x.END_DEPARTMENT_ID == 57 || x.LAST_DEPARTMENT_ID == 57))
                 ?? treatments.LastOrDefault(x => x.IS_PAUSE != 1)
                 ?? treatments.Last();

        HisTreatmentBedRoomLViewFilter bedFilter = new HisTreatmentBedRoomLViewFilter();
        bedFilter.TREATMENT_IDs = new List<long> { tr.ID };
        bedFilter.IS_IN_ROOM = true;
        var bedRooms = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, bedFilter, param);
        var curBed = bedRooms != null ? bedRooms.LastOrDefault(x => x.REMOVE_TIME == null || x.REMOVE_TIME == 0) : null;

        int birthYear = 0;
        string dobStr = tr.TDL_PATIENT_DOB.ToString();
        if (dobStr.Length >= 4) int.TryParse(dobStr.Substring(0, 4), out birthYear);
        int age = birthYear > 0 ? (DateTime.Now.Year - birthYear) : 0;
        string ageDisplay = age > 0 ? string.Format("{0} tuổi (Sinh năm: {1})", age, birthYear) : "N/A";

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 THÔNG TIN BỆNH NHÂN: {0} ({1} - {2})", tr.TDL_PATIENT_NAME, ageDisplay, tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt điều trị: {2}", tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("Khoa: {0} | Buồng/Giường: {1} - {2}", tr.END_DEPARTMENT_NAME ?? "Khoa 57", curBed != null ? curBed.BED_ROOM_NAME : "Chưa xếp buồng", curBed != null ? curBed.BED_NAME : "-"));
        Console.WriteLine(string.Format("Chẩn đoán ICD: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT ?? tr.ICD_SUB_CODE));
        Console.WriteLine(string.Format("BHYT: {0} | Trạng thái: {1}", tr.TDL_HEIN_CARD_NUMBER ?? "Không BHYT", tr.IS_PAUSE == 1 ? "ĐÃ RA VIỆN" : "ĐANG NẰM KHOA"));

        try
        {
            HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
            teinFilter.TDL_TREATMENT_ID = tr.ID;
            var teinList = myAdapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);

            if (teinList != null && teinList.Count > 0)
            {
                Func<string, string> getTein = (match) => {
                    var item = teinList.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) && 
                        ((x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.ToUpper().Contains(match.ToUpper())) ||
                         (x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.ToUpper() == match.ToUpper())));
                    return item != null ? item.VALUE + " " + item.TEST_INDEX_UNIT_NAME : "-";
                };

                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("📊 BILAN XÉT NGHIỆM MỚI NHẤT:"));
                Console.WriteLine(string.Format("  • Huyết học: Hb: {0} | WBC: {1} | PLT: {2}", getTein("Hemoglobin"), getTein("Bạch cầu"), getTein("Tiểu cầu")));
                Console.WriteLine(string.Format("  • Đông máu: PT-INR: {0} | Fibrinogen: {1} | APTT: {2}", getTein("INR"), getTein("Fibrinogen"), getTein("APTT")));
                Console.WriteLine(string.Format("  • Sinh hóa: Glucose: {0} | Ure: {1} | Creatinin: {2} | AST: {3} | ALT: {4}", getTein("Glucose"), getTein("Urê"), getTein("Creatinin"), getTein("AST"), getTein("ALT")));
                Console.WriteLine(string.Format("  • Nhóm máu: {0}", getTein("ABO")));
            }
        }
        catch { }
        Console.WriteLine("===============================================================================");
    }

    public static long CreateTracking(long treatmentId, string content, long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null)
    {
        InitSession();
        long now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.ID = treatmentId;
        var treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        if (treatments == null || treatments.Count == 0) throw new Exception("Không tìm thấy đợt điều trị!");
        var tr = treatments[0];

        HIS_TRACKING tracking = new HIS_TRACKING
        {
            TREATMENT_ID = treatmentId,
            DEPARTMENT_ID = 57,
            TRACKING_TIME = now,
            CONTENT = content,
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };

        HisTrackingSDO sdo = new HisTrackingSDO
        {
            Tracking = tracking
        };

        if (pulse.HasValue || temp.HasValue || bpMax.HasValue || bpMin.HasValue)
        {
            sdo.Dhst = new HIS_DHST
            {
                TREATMENT_ID = treatmentId,
                EXECUTE_TIME = now,
                EXECUTE_LOGINNAME = currentDoctorLogin,
                EXECUTE_USERNAME = currentDoctorName,
                PULSE = pulse,
                TEMPERATURE = temp,
                BLOOD_PRESSURE_MAX = bpMax,
                BLOOD_PRESSURE_MIN = bpMin
            };
        }

        var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mosConsumer, sdo, param);
        if (created == null) throw new Exception("Tạo tờ điều trị thất bại!");

        Console.WriteLine(string.Format("✔ Đã tạo Tờ điều trị ID: {0} lúc {1}", created.ID, created.TRACKING_TIME));
        return created.ID;
    }

    public static void PrescribeMedication(long treatmentId, long trackingId, long medicineTypeId, long stockId, decimal amount, string tutorial, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        if (tutorial.ToUpper().Contains("INSULIN") || tutorial.ToUpper().Contains("ACTRAPID") || tutorial.ToUpper().Contains("LANTUS") || tutorial.ToUpper().Contains("MIXTARD"))
        {
            if (stockId != 810)
            {
                Console.WriteLine("⚠️ CẢNH BÁO AN TOÀN: Đã tự động chuyển kho thuốc Insulin về TỦ TRỰC KHOA 57 (MediStockId = 810).");
                stockId = 810;
            }
            if (amount > 0.5m)
            {
                Console.WriteLine(string.Format("⚠️ CẢNH BÁO AN TOÀN: Liều Insulin là {0} UI. Đang quy đổi UI -> Lọ ({0} / 1000 = {1:F4} lọ).", amount, amount / 1000.0m));
                amount = amount / 1000.0m;
            }
        }

        InPatientPresSDO sdo = new InPatientPresSDO
        {
            TreatmentId = treatmentId,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            RequestRoomId = 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = medicineTypeId,
                    MediStockId = stockId,
                    Amount = amount,
                    PatientTypeId = patientTypeId,
                    Tutorial = tutorial
                }
            }
        };

        var res = myAdapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdo, param);
        if (res != null && res.ExpMests != null && res.ExpMests.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã xuất thuốc EXP_MEST: {0}", res.ExpMests[0].EXP_MEST_CODE));
        }
        else if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã y lệnh: {0}", res.ServiceReqs[0].SERVICE_REQ_CODE));
        }
        else
        {
            throw new Exception("Kê đơn thuốc thất bại!");
        }
    }

    public static void AssignClsService(long treatmentId, long trackingId, long serviceId, long roomId, string note, int patientTypeId = 1)
    {
        InitSession();

        HisTrackingFilter tf = new HisTrackingFilter();
        tf.ID = trackingId;
        var trackings = adapter.Get<List<HIS_TRACKING>>("api/HisTracking/Get", mosConsumer, tf, param);
        if (trackings == null || trackings.Count == 0) throw new Exception("Không tìm thấy tờ điều trị!");
        var tr = trackings[0];

        AssignServiceSDO sdo = new AssignServiceSDO
        {
            TreatmentId = treatmentId,
            RequestRoomId = 5248,
            RequestLoginName = currentDoctorLogin,
            RequestUserName = currentDoctorName,
            InstructionTime = tr.TRACKING_TIME,
            InstructionTimes = new List<long> { tr.TRACKING_TIME },
            UseTimes = new List<long> { tr.TRACKING_TIME },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = tr.TRACKING_TIME }
            },
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = tr.ICD_TEXT,
            SessionCode = null,
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = serviceId,
                    Amount = 1.0m,
                    PatientTypeId = patientTypeId,
                    PrimaryPatientTypeId = (patientTypeId == 1 ? (long?)null : patientTypeId),
                    RoomId = roomId,
                    InstructionNote = note,
                    MultipleExecute = 1,
                    IsNotUseBhyt = false,
                    IsNoHeinDifference = false,
                    EkipInfos = new List<EkipSDO>()
                }
            }
        };

        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdo, param);
        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            foreach (var sr in res.ServiceReqs)
            {
                Console.WriteLine(string.Format("✔ Chỉ định thành công! Mã y lệnh CLS: {0} (ID: {1})", sr.SERVICE_REQ_CODE, sr.ID));
            }
        }
        else
        {
            string err = "Chỉ định CLS thất bại!";
            if (param.Messages != null && param.Messages.Count > 0) err += " " + string.Join("; ", param.Messages);
            throw new Exception(err);
        }
    }

    public static void AssignSurgicalBilan(long treatmentId, long trackingId, string packType, int patientTypeId = 1)
    {
        InitSession();
        packType = packType.ToLower();

        List<ServiceTarget> targetList = new List<ServiceTarget>();
        string title = "";

        if (packType == "cement" || packType == "bxm")
        {
            title = "BILAN MỔ BƠM XI MĂNG CỘT SỐNG (VERTEBROPLASTY)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["FIBRINOGEN"]);
            targetList.Add(PredefinedServices["PT_TQ"]);
            targetList.Add(PredefinedServices["APTT_TCK"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBA1C"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["ECHO_HEART"]);
            targetList.Add(PredefinedServices["DEXA_2POS"]);
        }
        else if (packType == "spine" || packType == "nepvit")
        {
            title = "BILAN MỔ CỐ ĐỊNH CỘT SỐNG (NẸP VÍT QUA CUỐNG / TLIF)";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["URINE_10"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
            targetList.Add(PredefinedServices["ECHO_HEART"]);
        }
        else if (packType == "hip" || packType == "knee" || packType == "thaykhop")
        {
            title = "BILAN MỔ THAY KHỚP HÁNG / KHỚP GỐI NHÂN TẠO";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["GOT"]);
            targetList.Add(PredefinedServices["GPT"]);
            targetList.Add(PredefinedServices["ELECTROLYTES"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HCV_AB"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
            targetList.Add(PredefinedServices["XRAY_CHEST"]);
        }
        else if (packType == "hand" || packType == "viphau")
        {
            title = "BILAN MỔ VI PHẪU / NỐI GÂN MẠCH BÀN TAY";
            targetList.Add(PredefinedServices["CBC_LASER"]);
            targetList.Add(PredefinedServices["COAGULATION"]);
            targetList.Add(PredefinedServices["BLOOD_GROUP_GEL"]);
            targetList.Add(PredefinedServices["URE"]);
            targetList.Add(PredefinedServices["CREATININ"]);
            targetList.Add(PredefinedServices["HBSAG"]);
            targetList.Add(PredefinedServices["HIV_AB"]);
            targetList.Add(PredefinedServices["ECG"]);
        }
        else
        {
            Console.WriteLine(string.Format("❌ Gói Bilan '{0}' không hợp lệ! Hỗ trợ: cement (BXM), spine (Cột sống), hip (Thay khớp), hand (Vi phẫu).", packType));
            return;
        }

        Console.WriteLine(string.Format("=== THỰC THI CHỈ ĐỊNH {0} ===", title));
        Console.WriteLine(string.Format("Treatment ID: {0} | Tờ điều trị ID: {1}", treatmentId, trackingId));

        int successCount = 0;
        int index = 1;
        foreach (var item in targetList)
        {
            try
            {
                AssignClsService(treatmentId, trackingId, item.ServiceId, item.RoomId, item.Note, patientTypeId);
                successCount++;
                Console.WriteLine(string.Format("  ✔ [{0:D2}/{1:D2}] {2} ({3})", index, targetList.Count, item.ServiceName, item.ServiceCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("  ❌ [{0:D2}/{1:D2}] {2}: {3}", index, targetList.Count, item.ServiceName, ex.Message));
            }
            index++;
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("KẾT QUẢ CHỈ ĐỊNH GÓI: ✔ Thành công: {0}/{1}", successCount, targetList.Count));
        Console.WriteLine("===============================================================================");
    }

    public static void LookupConsultationDebate(string key)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("👥 TRA CỨU BIÊN BẢN HỘI CHẨN & Ý KIẾN CHUYÊN KHOA CHO: " + key);
        V_HIS_TREATMENT targetTreatment = null;
        if (key.StartsWith("00") || key.Length == 12)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        long trId = 0;
        if (targetTreatment == null && long.TryParse(key, out trId) && trId > 1000000 && trId < 99999999)
        {
            var tf = new HisTreatmentViewFilter { ID = trId };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = key.PadLeft(10, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = key.PadLeft(12, '0') };
            var list = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (list != null && list.Count > 0) targetTreatment = list[0];
        }

        if (targetTreatment == null)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị cho từ khóa: " + key);
            return;
        }

        Console.WriteLine(string.Format("BỆNH NHÂN: {0} ({1} tuổi - {2})", targetTreatment.TDL_PATIENT_NAME, DateTime.Now.Year - int.Parse(targetTreatment.TDL_PATIENT_DOB.ToString().Substring(0, 4)), targetTreatment.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("Mã BN: {0} | Mã ĐT: {1} | ID Đợt ĐT: {2}", targetTreatment.TDL_PATIENT_CODE, targetTreatment.TREATMENT_CODE, targetTreatment.ID));
        Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", targetTreatment.ICD_CODE, targetTreatment.ICD_NAME, targetTreatment.ICD_TEXT));
        Console.WriteLine("-------------------------------------------------------------------------------");

        // 1. Kiểm tra HIS_DEBATE
        var df = new HisDebateFilter { TREATMENT_ID = targetTreatment.ID };
        var debates = myAdapter.FetchList<HIS_DEBATE>("api/HisDebate/Get", mosConsumer, df, param);

        if (debates != null && debates.Count > 0)
        {
            Console.WriteLine(string.Format("📋 TÌM THẤY {0} BIÊN BẢN HỘI CHẨN CHÍNH (HIS_DEBATE):", debates.Count));
            foreach (var d in debates)
            {
                Console.WriteLine("\n-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("🔹 HỘI CHẨN ID: {0} | Thời gian: {1}", d.ID, d.DEBATE_TIME));
                Console.WriteLine("  • Chẩn đoán: [" + d.ICD_CODE + "] " + d.ICD_NAME + " (" + d.ICD_TEXT + ")");
                Console.WriteLine("  • Địa điểm: " + d.LOCATION);
                if (!string.IsNullOrEmpty(d.TREATMENT_TRACKING)) Console.WriteLine("  • Tóm tắt quá trình ĐT / Khám: " + d.TREATMENT_TRACKING);
                if (!string.IsNullOrEmpty(d.DISCUSSION)) Console.WriteLine("  • Nội dung thảo luận / Xin ý kiến: " + d.DISCUSSION);
                if (!string.IsNullOrEmpty(d.CONCLUSION)) Console.WriteLine("  • Kết luận / Hướng xử trí: " + d.CONCLUSION);

                var duf = new HisDebateUserFilter { DEBATE_ID = d.ID };
                var dUsers = myAdapter.FetchList<HIS_DEBATE_USER>("api/HisDebateUser/Get", mosConsumer, duf, param);
                if (dUsers != null && dUsers.Count > 0)
                {
                    Console.WriteLine("  • Thành viên tham gia:");
                    foreach (var u in dUsers)
                    {
                        string role = u.IS_PRESIDENT == 1 ? "[Chủ tọa]" : (u.IS_SECRETARY == 1 ? "[Thư ký]" : "[Thành viên]");
                        Console.WriteLine(string.Format("    - {0} {1} ({2})", role, u.USERNAME, u.LOGINNAME));
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Chưa có biên bản ghi nhận trong bảng HIS_DEBATE.");
        }

        // 2. Kiểm tra tất cả phiếu yêu cầu mời chuyên khoa liên khoa (HIS_SERVICE_REQ + HIS_SERE_SERV_EXT)
        var srf = new HisServiceReqViewFilter { TREATMENT_ID = targetTreatment.ID };
        var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

        if (reqs != null)
        {
            var consultReqs = reqs.Where(x => 
                x.SERVICE_REQ_TYPE_ID == 1 ||
                (x.EXECUTE_DEPARTMENT_NAME != null && (
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hô hấp") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nhiệt đới") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("truyền nhiễm") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("tim mạch") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("hồi sức") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("thần kinh") ||
                    x.EXECUTE_DEPARTMENT_NAME.ToLower().Contains("nội tiết")
                ))
            ).OrderByDescending(x => x.INTRUCTION_TIME).ToList();

            if (consultReqs.Count > 0)
            {
                Console.WriteLine("\n===============================================================================");
                Console.WriteLine(string.Format("🩺 Ý KIẾN TRẢ LỜI CỦA CÁC CHUYÊN KHOA KHÁCH ({0} PHIẾU CHỈ ĐỊNH):", consultReqs.Count));
                Console.WriteLine("===============================================================================");

                foreach (var cr in consultReqs)
                {
                    string statusBadge = cr.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ CÓ KẾT QUẢ / HOÀN THÀNH" : "🟡 ĐANG CHỜ XỬ LÝ / CHƯA CÓ KẾT QUẢ";
                    Console.WriteLine("\n-------------------------------------------------------------------------------");
                    Console.WriteLine(string.Format("🏢 ĐƠN VỊ: {0} ({1})", cr.EXECUTE_DEPARTMENT_NAME, cr.EXECUTE_ROOM_NAME));
                    Console.WriteLine(string.Format("• Phiếu #{0} [{1}] - Gửi lúc: {2}", cr.SERVICE_REQ_CODE, cr.SERVICE_REQ_TYPE_NAME, cr.INTRUCTION_TIME));
                    Console.WriteLine(string.Format("• Bác sĩ chỉ định: {0} ({1}) -> Khoa: {2}", cr.REQUEST_USERNAME, cr.REQUEST_LOGINNAME, cr.REQUEST_DEPARTMENT_NAME));
                    Console.WriteLine(string.Format("• Trạng thái: {0}", statusBadge));
                    if (cr.FINISH_TIME.HasValue) Console.WriteLine(string.Format("• Thời gian hoàn thành: {0}", cr.FINISH_TIME.Value));
                    Console.WriteLine(string.Format("• Bác sĩ hội chẩn/trả lời: {0} ({1})", cr.EXECUTE_USERNAME ?? "(Chưa tiếp nhận)", cr.EXECUTE_LOGINNAME ?? "-"));

                    // Lấy chi tiết ý kiến từ HIS_SERE_SERV_EXT
                    var ssf = new HisSereServFilter { SERVICE_REQ_ID = cr.ID };
                    var sss = myAdapter.FetchList<HIS_SERE_SERV>("api/HisSereServ/Get", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var ss in sss)
                        {
                            var ssef = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                            var sses = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, ssef, param);
                            if (sses != null && sses.Count > 0)
                            {
                                foreach (var se in sses)
                                {
                                    if (!string.IsNullOrEmpty(se.DESCRIPTION))
                                    {
                                        Console.WriteLine("\n📝 NỘI DUNG Ý KIẾN HỘI CHẨN:");
                                        Console.WriteLine(se.DESCRIPTION);
                                    }
                                    if (!string.IsNullOrEmpty(se.CONCLUDE) && se.CONCLUDE != ".")
                                    {
                                        Console.WriteLine("📌 KẾT LUẬN: " + se.CONCLUDE);
                                    }
                                    if (!string.IsNullOrEmpty(se.INSTRUCTION_NOTE))
                                    {
                                        Console.WriteLine("💡 LỜI DẶN: " + se.INSTRUCTION_NOTE);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Console.WriteLine("===============================================================================");
    }

    public static void LookupConsultationQueue(string dateParam = null)
    {
        InitSession();
        DateTime targetDate = DateTime.Now;
        if (!string.IsNullOrEmpty(dateParam))
        {
            DateTime parsed;
            if (DateTime.TryParseExact(dateParam, new string[] { "yyyyMMdd", "dd/MM/yyyy", "yyyy-MM-dd" }, null, System.Globalization.DateTimeStyles.None, out parsed))
            {
                targetDate = parsed;
            }
        }
        long fromTime = long.Parse(targetDate.ToString("yyyyMMdd000000"));
        long toTime   = long.Parse(targetDate.ToString("yyyyMMdd235959"));

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 PHÒNG HỘI CHẨN KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (ROOM ID: 11387)");
        Console.WriteLine("Ngày: " + targetDate.ToString("dd/MM/yyyy") + " | Quét tất cả yêu cầu hội chẩn gửi đến Khoa 57");
        Console.WriteLine("===============================================================================");

        var srf = new HisServiceReqViewFilter
        {
            EXECUTE_ROOM_ID = 11387,
            INTRUCTION_TIME_FROM = fromTime,
            INTRUCTION_TIME_TO = toTime
        };

        var list = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (list == null || list.Count == 0)
        {
            Console.WriteLine("Không có yêu cầu hội chẩn nào gửi đến Khoa 57 trong ngày.");
            return;
        }

        Console.WriteLine(string.Format("Tìm thấy {0} yêu cầu hội chẩn:\n", list.Count));
        int idx = 1;
        foreach (var r in list.OrderBy(x => x.INTRUCTION_TIME))
        {
            string timeStr = r.INTRUCTION_TIME.ToString().Length >= 12 ? r.INTRUCTION_TIME.ToString().Substring(8, 4).Insert(2, ":") : r.INTRUCTION_TIME.ToString();
            string stt = r.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ KHÁM/TRẢ LỜI" : (r.SERVICE_REQ_STT_ID == 2 ? "🟡 ĐANG KHÁM" : "🔴 CHỜ KHÁM/HC");
            Console.WriteLine(string.Format("{0}. [{1}] {2} | Mã ĐT: {3}", 
                idx++, timeStr, r.TDL_PATIENT_NAME, r.TREATMENT_CODE));
            Console.WriteLine(string.Format("   • Nơi gửi: {0} ({1}) | Bác sĩ Y/C: {2}", 
                r.REQUEST_DEPARTMENT_NAME, r.REQUEST_ROOM_NAME, r.REQUEST_USERNAME ?? r.REQUEST_LOGINNAME));
            Console.WriteLine(string.Format("   • Trạng thái: {0} | Mã phiếu: {1} | BS xử lý: {2}", 
                stt, r.SERVICE_REQ_CODE, r.EXECUTE_USERNAME ?? "Chưa phân công"));
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
    }

    public static void ScanWardRooms()
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 QUÉT DANH SÁCH BỆNH NHÂN CÁC BUỒNG TRỌNG ĐIỂM KHOA 57");
        Console.WriteLine("Phòng: 712, 714, 716, 724, 725");
        Console.WriteLine("===============================================================================");

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);

        if (allBeds == null || allBeds.Count == 0)
        {
            Console.WriteLine("Không tìm thấy bệnh nhân nào đang nằm buồng!");
            return;
        }

        var dept57Beds = allBeds.Where(x => x.DEPARTMENT_ID == 57 && (
            (x.BED_ROOM_NAME != null && (x.BED_ROOM_NAME.Contains("712") || x.BED_ROOM_NAME.Contains("714") || x.BED_ROOM_NAME.Contains("716") || x.BED_ROOM_NAME.Contains("724") || x.BED_ROOM_NAME.Contains("725"))) ||
            (x.BED_NAME != null && (x.BED_NAME.Contains("712") || x.BED_NAME.Contains("714") || x.BED_NAME.Contains("716") || x.BED_NAME.Contains("724") || x.BED_NAME.Contains("725")))
        )).OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME).ToList();

        Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân tại các buồng phụ trách:\n", dept57Beds.Count));

        // BATCH QUERY: Gom toàn bộ Treatment IDs vào 1 request HTTP duy nhất
        var treatIds = dept57Beds.Select(b => b.TREATMENT_ID).Distinct().ToList();
        var treatMap = new Dictionary<long, V_HIS_TREATMENT>();
        if (treatIds.Count > 0)
        {
            HisTreatmentViewFilter tfBatch = new HisTreatmentViewFilter();
            tfBatch.IDs = treatIds;
            var tList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfBatch, param);
            if (tList != null)
            {
                foreach (var t in tList) treatMap[t.ID] = t;
            }
        }

        int stt = 1;
        foreach (var b in dept57Beds)
        {
            var tr = treatMap.ContainsKey(b.TREATMENT_ID) ? treatMap[b.TREATMENT_ID] : null;

            string patName = tr != null ? tr.TDL_PATIENT_NAME : "N/A";
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "N/A";
            string icd = tr != null ? string.Format("[{0}] {1}", tr.ICD_CODE, tr.ICD_NAME) : "-";

            Console.WriteLine(string.Format("{0:D2}. [{1} - {2}] BN: {3} (Mã: {4}) | TrID: {5}", stt++, b.BED_ROOM_NAME, b.BED_NAME, patName, patCode, b.TREATMENT_ID));
            Console.WriteLine(string.Format("    Chẩn đoán: {0}", icd));
        }
        Console.WriteLine("===============================================================================");
    }

    public static void ListOrders(string keyword)
    {
        InitSession();
        List<V_HIS_TREATMENT> treatments = null;
        string kw = keyword.Trim();
        long numVal;
        bool isNum = long.TryParse(kw, out numVal);

        if (isNum)
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter();
            tfCode.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);

            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter();
                tfCode.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
        }
        else
        {
            HisTreatmentViewFilter tfCode = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = kw };
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            if (treatments == null || treatments.Count == 0)
            {
                tfCode = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = kw };
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfCode, param);
            }
            if (treatments == null || treatments.Count == 0)
            {
                string searchNorm = RemoveDiacritics(kw).Trim().ToLower();
                try
                {
                    HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true, TREATMENT_IS_ACTIVE = true };
                    var allBeds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
                    if (allBeds != null && allBeds.Count > 0)
                    {
                        var matchedBeds = allBeds.Where(b => !string.IsNullOrEmpty(b.TDL_PATIENT_NAME) && RemoveDiacritics(b.TDL_PATIENT_NAME).ToLower().Contains(searchNorm)).ToList();
                        if (matchedBeds.Count > 0)
                        {
                            var tIds = matchedBeds.Select(x => x.TREATMENT_ID).Distinct().ToList();
                            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { IDs = tIds }, param);
                        }
                    }
                }
                catch { }
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy bệnh nhân nào khớp với từ khóa: {0}", keyword));
            return;
        }

        var tr = treatments.LastOrDefault(x => x.IS_PAUSE != 1) ?? treatments.Last();
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("📋 DANH SÁCH Y LỆNH: {0} (Mã BN: {1} | Mã ĐT: {2})", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
        Console.WriteLine("===============================================================================");

        HisServiceReqViewFilter srf = new HisServiceReqViewFilter
        {
            TREATMENT_ID = tr.ID
        };
        var orders = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        if (orders == null || orders.Count == 0)
        {
            Console.WriteLine("Bệnh nhân chưa có y lệnh nào.");
            Console.WriteLine("===============================================================================");
            return;
        }

        var sorted = orders.OrderByDescending(x => x.INTRUCTION_TIME).ToList();
        int stt = 1;
        foreach (var r in sorted)
        {
            string timeStr = r.INTRUCTION_TIME.ToString().Length >= 12 
                ? string.Format("{0}/{1} {2}:{3}", r.INTRUCTION_TIME.ToString().Substring(6, 2), r.INTRUCTION_TIME.ToString().Substring(4, 2), r.INTRUCTION_TIME.ToString().Substring(8, 2), r.INTRUCTION_TIME.ToString().Substring(10, 2)) 
                : r.INTRUCTION_TIME.ToString();

            string sttBadge;
            string canCancelTag = "";
            if (r.SERVICE_REQ_STT_ID == 1)
            {
                sttBadge = "⚪ CHƯA THỰC HIỆN (Màu trắng)";
                canCancelTag = " 👉 [CÓ THỂ HỦY/XÓA]";
            }
            else if (r.SERVICE_REQ_STT_ID == 2)
            {
                sttBadge = "🟡 ĐANG THỰC HIỆN";
            }
            else if (r.SERVICE_REQ_STT_ID == 3)
            {
                sttBadge = "🟢 ĐÃ HOÀN THÀNH";
            }
            else
            {
                sttBadge = "⚫ KHÁC (" + r.SERVICE_REQ_STT_ID + ")";
            }

            Console.WriteLine(string.Format("{0:D2}. [ID: {1} | Mã: {2}] Lúc {3} | {4}{5}", 
                stt++, r.ID, r.SERVICE_REQ_CODE, timeStr, sttBadge, canCancelTag));
            Console.WriteLine(string.Format("    • Loại: {0} | BS Chỉ định: {1} ({2}) | Nơi Y/C: {3}", 
                r.SERVICE_REQ_TYPE_NAME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.REQUEST_ROOM_NAME));
            
            try
            {
                HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
                var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                if (ssList != null && ssList.Count > 0)
                {
                    var names = ssList.Select(x => string.Format("{0} (SS_ID: {1})", x.TDL_SERVICE_NAME, x.ID));
                    Console.WriteLine(string.Format("    • Dịch vụ: {0}", string.Join("; ", names)));
                }
            }
            catch { }
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
        Console.WriteLine("💡 Hủy cả phiếu: .\\.agents\\skills\\his-clinical-operations\\scripts\\HisClinicalCli.exe cancel-order <ID>");
        Console.WriteLine("💡 Hủy dịch vụ lẻ: .\\.agents\\skills\\his-clinical-operations\\scripts\\HisClinicalCli.exe cancel-service <SS_ID>");
        Console.WriteLine("===============================================================================");
    }

    public static void CancelOrder(string orderKey, long? customRoomId = null)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🗑️ HỦY/XÓA Y LỆNH (MÃ HOẶC ID: {0})", orderKey));
        Console.WriteLine("===============================================================================");

        if (string.IsNullOrEmpty(orderKey))
        {
            Console.WriteLine("❌ Vui lòng cung cấp ID phiếu y lệnh (ServiceReqId) hoặc Mã phiếu (ServiceReqCode)!");
            return;
        }

        string ok = orderKey.Trim();
        List<V_HIS_SERVICE_REQ> reqs = null;

        // Nếu chuỗi bắt đầu bằng 0 hoặc có độ dài >= 11 ký tự -> Ưu tiên tìm theo SERVICE_REQ_CODE trước (tránh nhầm sang ID)
        if (ok.StartsWith("0") || ok.Length >= 11)
        {
            HisServiceReqViewFilter srfCode = new HisServiceReqViewFilter { SERVICE_REQ_CODE = ok };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfCode, param);
        }

        long reqId = 0;
        if ((reqs == null || reqs.Count == 0) && long.TryParse(ok, out reqId))
        {
            HisServiceReqViewFilter srfId = new HisServiceReqViewFilter { ID = reqId };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfId, param);
        }

        if (reqs == null || reqs.Count == 0)
        {
            HisServiceReqViewFilter srfCode = new HisServiceReqViewFilter { SERVICE_REQ_CODE = ok };
            reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfCode, param);
        }

        if (reqs == null || reqs.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy y lệnh với từ khóa: {0} (hoặc y lệnh đã bị xóa trước đó).", orderKey));
            return;
        }

        var req = reqs[0];
        string timeStr = req.INTRUCTION_TIME.ToString().Length >= 12 
            ? string.Format("{0}/{1} {2}:{3}", req.INTRUCTION_TIME.ToString().Substring(6, 2), req.INTRUCTION_TIME.ToString().Substring(4, 2), req.INTRUCTION_TIME.ToString().Substring(8, 2), req.INTRUCTION_TIME.ToString().Substring(10, 2)) 
            : req.INTRUCTION_TIME.ToString();

        Console.WriteLine(string.Format("• Bệnh nhân     : {0} (Mã BN: {1} | Mã ĐT: {2})", req.TDL_PATIENT_NAME, req.TDL_PATIENT_CODE, req.TREATMENT_CODE));
        Console.WriteLine(string.Format("• Mã phiếu      : {0} | ID Phiếu: {1} | Loại: {2}", req.SERVICE_REQ_CODE, req.ID, req.SERVICE_REQ_TYPE_NAME));
        Console.WriteLine(string.Format("• Người chỉ định: {0} ({1}) lúc {2}", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME, timeStr));
        Console.WriteLine(string.Format("• Nơi chỉ định  : {0} (Phòng ID: {1})", req.REQUEST_ROOM_NAME, req.REQUEST_ROOM_ID));
        Console.WriteLine(string.Format("• Bác sĩ thực hiện hủy: {0} ({1})", currentDoctorName, currentDoctorLogin));

        // RÀO CHẮN AN TOÀN 1: Kiểm tra trạng thái y lệnh
        if (req.SERVICE_REQ_STT_ID != 1)
        {
            string sttName = req.SERVICE_REQ_STT_ID == 2 ? "🟡 ĐANG THỰC HIỆN (Đã tiếp nhận mẫu)" : (req.SERVICE_REQ_STT_ID == 3 ? "🟢 ĐÃ HOÀN THÀNH / ĐÃ CÓ KẾT QUẢ" : "KHÁC (" + req.SERVICE_REQ_STT_ID + ")");
            Console.WriteLine(string.Format("❌ CẢNH BÁO AN TOÀN: Y lệnh đang ở trạng thái {0}.", sttName));
            Console.WriteLine("👉 Quy định HIS: Chỉ được phép xóa y lệnh ở trạng thái CHƯA THỰC HIỆN (Chỉ định màu trắng).");
            Console.WriteLine("👉 Nếu cần hủy chỉ định này, vui lòng liên hệ phòng thực hiện để HỦY TIẾP NHẬN / HỦY KẾT QUẢ trên HIS trước.");
            return;
        }

        // Liệt kê chi tiết dịch vụ đính kèm
        try
        {
            HisSereServViewFilter ssf = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
            var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            if (ssList != null && ssList.Count > 0)
            {
                Console.WriteLine(string.Format("• Dịch vụ sẽ bị hủy ({0} mục):", ssList.Count));
                foreach (var s in ssList)
                {
                    Console.WriteLine(string.Format("   - [{0}] {1} (SL: {2})", s.ID, s.TDL_SERVICE_NAME, s.AMOUNT));
                }
            }
        }
        catch { }

        // RÀO CHẮN AN TOÀN 2: Kiểm tra và hủy văn bản ký EMR nếu có
        try
        {
            var docFilter = new EMR.Filter.EmrDocumentFilter();
            docFilter.TREATMENT_CODE__EXACT = req.TDL_TREATMENT_CODE;
            ApiConsumer emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", currentToken, "HIS");
            var emrDocs = myAdapter.FetchList<EMR.EFMODEL.DataModels.EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
            if (emrDocs != null && emrDocs.Count > 0)
            {
                string reqTag = "SERVICE_REQ_CODE:" + req.SERVICE_REQ_CODE;
                var matchedDocs = emrDocs.Where(d => d.HIS_CODE != null && d.HIS_CODE.Contains(reqTag)).ToList();
                if (matchedDocs.Count > 0)
                {
                    Console.WriteLine(string.Format("⚠️ Phát hiện {0} văn bản ký EMR liên kết với y lệnh này. Đang tự động hủy văn bản EMR...", matchedDocs.Count));
                    foreach (var doc in matchedDocs)
                    {
                        bool delEmr = myAdapter.PostData<bool>("api/EmrDocument/Delete", emrConsumer, doc, param);
                        Console.WriteLine(string.Format("   - Hủy văn bản EMR ID {0}: {1}", doc.ID, delEmr ? "✔ THÀNH CÔNG" : "❌ THẤT BẠI"));
                    }
                }
            }
        }
        catch { }

        // RÀO CHẮN 3: Thực thi xóa ServiceReq qua API MOS Backend
        long reqRoomId = customRoomId ?? req.REQUEST_ROOM_ID;
        if (reqRoomId <= 0) reqRoomId = 5252; // Mặc định P712 Khoa 57

        var sdo = new HisServiceReqSDO
        {
            Id = req.ID,
            RequestRoomId = reqRoomId
        };

        Console.WriteLine(string.Format("🚀 Đang gửi lệnh xóa y lệnh đến Backend MOS (RequestRoomId: {0})...", reqRoomId));
        bool isSuccess = myAdapter.PostData<bool>("api/HisServiceReq/Delete", mosConsumer, sdo, param);

        if (isSuccess)
        {
            // Post-verify
            HisServiceReqViewFilter vf = new HisServiceReqViewFilter { ID = req.ID };
            var vList = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, vf, param);
            bool isGone = (vList == null || vList.Count == 0 || vList[0].IS_DELETE == 1);

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("✔ ĐÃ XÓA THÀNH CÔNG Y LỆNH: ID {0} (Mã phiếu: {1})", req.ID, req.SERVICE_REQ_CODE));
            if (isGone)
            {
                Console.WriteLine("✔ ĐỐI SOÁT HỆ THỐNG: Y lệnh đã được gỡ bỏ hoàn toàn khỏi hồ sơ bệnh án.");
            }
            Console.WriteLine("👉 Bác sĩ tải lại màn hình HIS Desktop sẽ thấy y lệnh màu trắng đã biến mất.");
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("❌ XÓA THẤT BẠI: {0} (BugCode: {1})", param.GetMessage(), param.GetBugCode()));
            Console.WriteLine("===============================================================================");
        }
    }

    public static void CancelSereServ(long sereServId)
    {
        InitSession();
        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🗑️ HỦY/XÓA DỊCH VỤ CON ĐƠN LẺ (SERE_SERV_ID: {0})", sereServId));
        Console.WriteLine("===============================================================================");

        HisSereServViewFilter ssf = new HisSereServViewFilter { ID = sereServId };
        var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
        if (ssList == null || ssList.Count == 0)
        {
            Console.WriteLine(string.Format("❌ Không tìm thấy dịch vụ với ID: {0} (hoặc đã bị xóa trước đó).", sereServId));
            return;
        }

        var ss = ssList[0];
        Console.WriteLine(string.Format("• Tên dịch vụ : {0} (Mã: {1})", ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_CODE));
        Console.WriteLine(string.Format("• Phiếu y lệnh: ID {0} (Mã phiếu: {1})", ss.SERVICE_REQ_ID, ss.TDL_SERVICE_REQ_CODE));
        Console.WriteLine(string.Format("• Bệnh nhân   : Mã ĐT {0} (TreatmentId: {1})", ss.TDL_TREATMENT_CODE, ss.TDL_TREATMENT_ID));
        Console.WriteLine(string.Format("• Số lượng    : {0} | Đơn giá: {1:N0} đ", ss.AMOUNT, ss.PRICE));

        Console.WriteLine("🚀 Đang gửi lệnh xóa dịch vụ đến Backend MOS...");
        bool isSuccess = myAdapter.PostData<bool>("api/HisSereServ/ExamDelete", mosConsumer, new HIS_SERE_SERV { ID = sereServId }, param);

        if (isSuccess)
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("✔ ĐÃ XÓA THÀNH CÔNG DỊCH VỤ: {0} (ID: {1})", ss.TDL_SERVICE_NAME, sereServId));
            Console.WriteLine("===============================================================================");
        }
        else
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("❌ XÓA THẤT BẠI: {0} (BugCode: {1})", param.GetMessage(), param.GetBugCode()));
            Console.WriteLine("===============================================================================");
        }
    }

    public static void RunCli(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("🏥 UNIFIED HIS CLINICAL AUTOMATION CLI - KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Cú pháp lệnh:");
            Console.WriteLine("  lookup <patientCode|treatmentCode|name>      : Tra cứu thông tin, buồng giường & Bilan");
            Console.WriteLine("  orders <patientCode|treatmentCode|name>      : Liệt kê danh sách y lệnh & trạng thái màu");
            Console.WriteLine("  cancel-order <serviceReqId|reqCode> [roomId] : Hủy/Xóa y lệnh chưa thực hiện (Màu trắng)");
            Console.WriteLine("  cancel-service <sereServId>                  : Hủy/Xóa 1 dịch vụ con lẻ trong phiếu");
            Console.WriteLine("  wardround                                    : Quét danh sách BN buồng 712, 714, 716, 724, 725");
            Console.WriteLine("  create-tracking <trId> <content> [dhst..]    : Tạo tờ điều trị và DHST");
            Console.WriteLine("  prescribe <trId> <tkId> <medId> <stId> <amount> <tutorial> : Kê đơn thuốc an toàn");
            Console.WriteLine("  assign-cls <trId> <tkId> <svcId> <roomId> [note] [ptId]    : Chỉ định CLS đơn lẻ");
            Console.WriteLine("  assign-bilan <trId> <tkId> <cement|spine|hip|hand>         : Chỉ định gói Bilan 1-Click");
            Console.WriteLine("  debate <patientCode|treatmentCode>                         : Tra cứu biên bản hội chẩn & ý kiến các chuyên khoa");
            Console.WriteLine("===============================================================================");
            return;
        }

        string cmd = args[0].ToLower();
        try
        {
            if (cmd == "lookup")
            {
                if (args.Length < 2) throw new Exception("Thiếu từ khóa tra cứu!");
                LookupPatient(args[1]);
            }
            else if (cmd == "debate" || cmd == "hoichan")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN hoặc mã đợt điều trị!");
                LookupConsultationDebate(args[1]);
            }
            else if (cmd == "wardround")
            {
                ScanWardRooms();
            }
            else if (cmd == "create-tracking")
            {
                long treatmentId = long.Parse(args[1]);
                string content = args[2];
                long? pulse = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? (long?)long.Parse(args[3]) : null;
                decimal? temp = args.Length > 4 && !string.IsNullOrEmpty(args[4]) ? (decimal?)decimal.Parse(args[4]) : null;
                long? bpMax = args.Length > 5 && !string.IsNullOrEmpty(args[5]) ? (long?)long.Parse(args[5]) : null;
                long? bpMin = args.Length > 6 && !string.IsNullOrEmpty(args[6]) ? (long?)long.Parse(args[6]) : null;
                CreateTracking(treatmentId, content, pulse, temp, bpMax, bpMin);
            }
            else if (cmd == "prescribe")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long medId = long.Parse(args[3]);
                long stockId = long.Parse(args[4]);
                decimal amount = decimal.Parse(args[5]);
                string tutorial = args.Length > 6 ? args[6] : "";
                int ptId = args.Length > 7 ? int.Parse(args[7]) : 1;
                PrescribeMedication(treatmentId, trackingId, medId, stockId, amount, tutorial, ptId);
            }
            else if (cmd == "assign-cls")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                long serviceId = long.Parse(args[3]);
                long roomId = long.Parse(args[4]);
                string note = args.Length > 5 ? args[5] : "";
                int ptId = args.Length > 6 ? int.Parse(args[6]) : 1;
                AssignClsService(treatmentId, trackingId, serviceId, roomId, note, ptId);
            }
            else if (cmd == "assign-bilan" || cmd == "assign-bilan-cement")
            {
                long treatmentId = long.Parse(args[1]);
                long trackingId = long.Parse(args[2]);
                string packType = (cmd == "assign-bilan-cement" || args.Length < 4) ? "cement" : args[3];
                int ptId = args.Length > 4 ? int.Parse(args[4]) : 1;
                AssignSurgicalBilan(treatmentId, trackingId, packType, ptId);
            }
            else if (cmd == "consult-room" || cmd == "consult-queue" || cmd == "room11387")
            {
                string dateStr = args.Length > 1 ? args[1] : null;
                LookupConsultationQueue(dateStr);
            }
            else if (cmd == "orders" || cmd == "list-orders")
            {
                if (args.Length < 2) throw new Exception("Thiếu mã BN, mã ĐT hoặc tên bệnh nhân!");
                ListOrders(args[1]);
            }
            else if (cmd == "cancel-order" || cmd == "delete-order" || cmd == "cancel-req")
            {
                if (args.Length < 2) throw new Exception("Thiếu ID hoặc Mã phiếu y lệnh cần hủy!");
                long? rId = args.Length > 2 ? (long?)long.Parse(args[2]) : null;
                CancelOrder(args[1], rId);
            }
            else if (cmd == "cancel-service" || cmd == "delete-service")
            {
                if (args.Length < 2) throw new Exception("Thiếu ID dịch vụ con (SERE_SERV_ID) cần hủy!");
                CancelSereServ(long.Parse(args[1]));
            }
            else
            {
                Console.WriteLine("Lệnh không hợp lệ: " + cmd);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LỖI THỰC THI: " + ex.Message);
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

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        HisClinicalCli.RunCli(args);
    }
}
