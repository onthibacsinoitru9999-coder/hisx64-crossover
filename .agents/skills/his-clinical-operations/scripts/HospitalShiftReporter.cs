using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace HospitalShiftReporter
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
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
                string path3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);
                return null;
            };

            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Cache-first token
            string tokenCode = null;
            foreach (var cf in new[] { Path.Combine(baseDir, "doctor_standalone.token"), Path.Combine(preferredDir, "doctor_standalone.token") })
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
                            { tokenCode = parts[0]; break; }
                        }
                    }
                }
                catch { }
            }

            // 2. Live log with identity guard
            if (string.IsNullOrEmpty(tokenCode))
            {
                List<string> candidates = new List<string>();
                candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
                try
                {
                    var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
                    if (procs != null && procs.Length > 0)
                    {
                        string hp = procs[0].MainModule.FileName;
                        if (hp.Contains("LBP2900_R150_V330_W64_uk_EN_2"))
                            candidates.Add(Path.Combine(Path.GetDirectoryName(hp), "Logs", "LogSystem.txt"));
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
                            long len = fs.Length; if (len == 0) continue;
                            int bsz = (int)Math.Min(131072L, len);
                            fs.Seek(len - bsz, SeekOrigin.Begin);
                            byte[] buf = new byte[bsz]; int r = fs.Read(buf, 0, bsz);
                            string chunk = Encoding.UTF8.GetString(buf, 0, r);
                            if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;
                            if (!chunk.Contains("034727") && !chunk.Contains("vmc")) continue;
                            int idx = chunk.LastIndexOf("TokenCode|");
                            if (idx >= 0 && chunk.Length >= idx + 74) { tokenCode = chunk.Substring(idx + 10, 64); break; }
                        }
                    }
                    catch { }
                }
            }

            // 3. Standalone login fallback
            if (string.IsNullOrEmpty(tokenCode))
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                if (token == null) token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (token == null) { Console.WriteLine("Login failed!"); return; }
                tokenCode = token.TokenCode;
                try { File.WriteAllText(Path.Combine(preferredDir, "doctor_standalone.token"), tokenCode + "|" + DateTime.UtcNow.Ticks + "|034727", Encoding.UTF8); } catch { }
            }

            ApiConsumers.SetConsunmer(tokenCode);
            MyAdapter adapter = new MyAdapter();

            using (StreamWriter sw = new StreamWriter("shift_report_full_result.txt", false, Encoding.UTF8))
            {
                Action<string> Log = delegate(string msg)
                {
                    Console.WriteLine(msg);
                    sw.WriteLine(msg);
                };

                Log("==========================================================================");
                Log("BÁO CÁO DỮ LIỆU HIS KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG (KHOA 57)");
                Log("THỜI GIAN CA TRỰC: TỪ 07:00 NGÀY 24/08/2026 ĐẾN 03:00 NGÀY 25/08/2026");
                Log("==========================================================================\n");

                // 1. Lấy toàn bộ bệnh nhân đang nằm nội trú tại Khoa 57
                HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
                tbrf.IS_IN_ROOM = true;
                tbrf.TREATMENT_IS_ACTIVE = true;
                var allInBedRooms = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);
                
                var dept57Patients = (allInBedRooms != null) ? allInBedRooms.Where(x => x.DEPARTMENT_ID == 57).ToList() : new List<V_HIS_TREATMENT_BED_ROOM>();
                Log(string.Format("✔ Tổng số bệnh nhân đang nằm nội trú tại Khoa 57: {0} bệnh nhân", dept57Patients.Count));

                // Danh sách tất cả treatment IDs tại khoa 57
                var allTreatIds = dept57Patients.Select(x => x.TREATMENT_ID).Distinct().ToList();

                // Tra cứu thông tin Treatment của toàn bộ bệnh nhân
                Dictionary<long, V_HIS_TREATMENT> treatMap = new Dictionary<long, V_HIS_TREATMENT>();
                foreach (var tId in allTreatIds)
                {
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = tId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    if (tList != null && tList.Count > 0)
                    {
                        treatMap[tId] = tList[0];
                    }
                }

                // =========================================================================================
                // MỤC 1: DANH SÁCH BỆNH NHÂN VÀO KHOA TỪ 07:00 24/8 ĐẾN 03:00 25/8
                // =========================================================================================
                Log("\n==========================================================================");
                Log("MỤC 1: DANH SÁCH BỆNH NHÂN VÀO KHOA CTCH & CS (24/8 07:00 -> 25/8 03:00)");
                Log("==========================================================================");

                List<long> admittedList = new List<long>();
                Dictionary<long, V_HIS_DEPARTMENT_TRAN> admDeptTranMap = new Dictionary<long, V_HIS_DEPARTMENT_TRAN>();

                foreach (var tId in allTreatIds)
                {
                    HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                    dtf.TREATMENT_ID = tId;
                    var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", ApiConsumers.MosConsumer, dtf, param);
                    if (dts != null)
                    {
                        var trans57 = dts.Where(x => x.DEPARTMENT_ID == 57).OrderByDescending(x => x.DEPARTMENT_IN_TIME ?? x.CREATE_TIME).FirstOrDefault();
                        if (trans57 != null)
                        {
                            long inTime = trans57.DEPARTMENT_IN_TIME ?? trans57.CREATE_TIME ?? 0;
                            // Check window 20260824070000 -> 20260825030500
                            if (inTime >= 20260824070000 && inTime <= 20260825030500)
                            {
                                string prev = trans57.PREVIOUS_DEPARTMENT_NAME ?? "";
                                if (!prev.Contains("Phẫu thuật") && !prev.Contains("Gây mê") && !prev.Contains("GMHS"))
                                {
                                    admittedList.Add(tId);
                                    admDeptTranMap[tId] = trans57;
                                }
                            }
                        }
                    }
                }

                Log(string.Format("Tìm thấy {0} bệnh nhân mới vào khoa trong ca trực:", admittedList.Count));
                int stt1 = 1;
                foreach (var tId in admittedList)
                {
                    var bed = dept57Patients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                    var t = treatMap.ContainsKey(tId) ? treatMap[tId] : null;
                    var dt = admDeptTranMap[tId];

                    Log(string.Format("\n--- BỆNH NHÂN {0}: {1} ---", stt1++, bed != null ? bed.TDL_PATIENT_NAME : ""));
                    Log(string.Format("  • Tuổi: {0} | Giới tính: {1} | Buồng: {2} - Giường: {3}",
                        t != null ? (2026 - int.Parse(t.TDL_PATIENT_DOB.ToString().Substring(0,4))).ToString() : "",
                        t != null ? t.TDL_PATIENT_GENDER_NAME : "",
                        bed != null ? bed.BED_ROOM_NAME : "",
                        bed != null ? bed.BED_NAME : ""));
                    Log(string.Format("  • Mã BN: {0} | Mã BA: {1} | Vào khoa lúc: {2} | Từ khoa/phòng: {3}",
                        bed != null ? bed.TDL_PATIENT_CODE : "",
                        bed != null ? bed.TREATMENT_CODE : "",
                        dt.DEPARTMENT_IN_TIME,
                        dt.PREVIOUS_DEPARTMENT_NAME));
                    Log(string.Format("  • Bác sĩ nhận / tạo y lệnh: {0}", dt.CREATOR));
                    Log(string.Format("  • Chẩn đoán vào khoa: {0} [{1}] - {2}",
                        t != null ? t.IN_ICD_NAME : "", t != null ? t.IN_ICD_CODE : "", t != null ? t.IN_ICD_TEXT : ""));

                    // Lấy diễn biến bệnh sử từ tờ điều trị đầu tiên
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkf, param);
                    if (trks != null && trks.Count > 0)
                    {
                        var firstTrk = trks.OrderBy(x => x.TRACKING_TIME).First();
                        Log(string.Format("  • Bệnh sử / Diễn biến lúc vào: {0}", firstTrk.CONTENT != null ? firstTrk.CONTENT.Replace("\r\n", " | ").Replace("\n", " | ") : "Chưa có"));
                    }

                    // Lấy Bilan xét nghiệm & CĐHA
                    PrintPatientBilan(adapter, tId, param, Log);
                }

                // =========================================================================================
                // MỤC 2: DANH SÁCH BỆNH NHÂN SAU MỔ VỀ KHOA TỪ GMHS (24/8 07:00 -> 25/8 03:00)
                // =========================================================================================
                Log("\n==========================================================================");
                Log("MỤC 2: DANH SÁCH BỆNH NHÂN SAU MỔ VỀ KHOA TỪ GMHS (24/8 07:00 -> 25/8 03:00)");
                Log("==========================================================================");

                List<long> postOpList = new List<long>();
                foreach (var tId in allTreatIds)
                {
                    // Check service req phẫu thuật ngày 24/8
                    HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                    srf.TREATMENT_ID = tId;
                    var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);
                    var ptReqs = (srs != null) ? srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 && x.INTRUCTION_TIME >= 20260824000000 && x.INTRUCTION_TIME <= 20260825030500).ToList() : null;

                    // Check chuyển khoa từ GMHS
                    HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                    dtf.TREATMENT_ID = tId;
                    var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", ApiConsumers.MosConsumer, dtf, param);
                    var transFromGmhs = (dts != null) ? dts.Where(x => x.DEPARTMENT_ID == 57 && ((x.PREVIOUS_DEPARTMENT_NAME ?? "").Contains("Gây mê") || (x.PREVIOUS_DEPARTMENT_NAME ?? "").Contains("Phẫu thuật") || (x.PREVIOUS_DEPARTMENT_NAME ?? "").Contains("Hồi tỉnh"))).ToList() : null;
                    var recentTransGmhs = (transFromGmhs != null) ? transFromGmhs.Where(x => (x.DEPARTMENT_IN_TIME >= 20260824070000 && x.DEPARTMENT_IN_TIME <= 20260825030500) || (x.CREATE_TIME >= 20260824070000 && x.CREATE_TIME <= 20260825030500)).ToList() : null;

                    // Check tracking hậu phẫu
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkf, param);
                    var postOpTrks = (trks != null) ? trks.Where(x => x.TRACKING_TIME >= 20260824070000 && x.TRACKING_TIME <= 20260825030500 &&
                        (x.CONTENT != null && (x.CONTENT.ToUpper().Contains("HẬU PHẪU") || x.CONTENT.ToUpper().Contains("VỀ KHOA") || x.CONTENT.ToUpper().Contains("BÀN GIAO") || x.CONTENT.ToUpper().Contains("THOÁT MÊ") || x.CONTENT.ToUpper().Contains("SAU MỔ")))).ToList() : null;

                    if ((ptReqs != null && ptReqs.Count > 0) || (recentTransGmhs != null && recentTransGmhs.Count > 0) || (postOpTrks != null && postOpTrks.Count > 0))
                    {
                        postOpList.Add(tId);
                    }
                }

                Log(string.Format("Tìm thấy {0} bệnh nhân sau mổ về khoa trong ca trực:", postOpList.Count));
                int stt2 = 1;
                foreach (var tId in postOpList)
                {
                    var bed = dept57Patients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                    var t = treatMap.ContainsKey(tId) ? treatMap[tId] : null;

                    Log(string.Format("\n--- BỆNH NHÂN SAU MỔ {0}: {1} ---", stt2++, bed != null ? bed.TDL_PATIENT_NAME : ""));
                    Log(string.Format("  • Tuổi: {0} | Giới: {1} | Buồng: {2} - Giường: {3} | Mã BN: {4} | Mã BA: {5}",
                        t != null ? (2026 - int.Parse(t.TDL_PATIENT_DOB.ToString().Substring(0,4))).ToString() : "",
                        t != null ? t.TDL_PATIENT_GENDER_NAME : "",
                        bed != null ? bed.BED_ROOM_NAME : "",
                        bed != null ? bed.BED_NAME : "",
                        bed != null ? bed.TDL_PATIENT_CODE : "",
                        bed != null ? bed.TREATMENT_CODE : ""));
                    Log(string.Format("  • Chẩn đoán: {0} [{1}] - {2}", t != null ? t.ICD_NAME : "", t != null ? t.ICD_CODE : "", t != null ? t.ICD_TEXT : ""));

                    // Phẫu thuật & Phẫu thuật viên
                    HisSereServViewFilter ssf = new HisSereServViewFilter();
                    ssf.TREATMENT_ID = tId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);
                    if (sss != null)
                    {
                        var ptServices = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 4 || (x.SERVICE_TYPE_NAME != null && x.SERVICE_TYPE_NAME.ToUpper().Contains("PHẪU THUẬT"))).ToList();
                        foreach (var pts in ptServices)
                        {
                            Log(string.Format("  • Phẫu thuật: {0} (Thời gian: {1})", pts.TDL_SERVICE_NAME, pts.TDL_INTRUCTION_TIME));
                        }
                    }

                    // Lấy ekip phẫu thuật viên
                    HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                    srf.TREATMENT_ID = tId;
                    var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);
                    if (srs != null)
                    {
                        var ptReqs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6).OrderByDescending(x => x.INTRUCTION_TIME).ToList();
                        foreach (var ptr in ptReqs)
                        {
                            Log(string.Format("  • Y lệnh PT: {0} | Bác sĩ chỉ định: {1} | Phòng mổ: {2} | Trạng thái: {3}",
                                ptr.SERVICE_REQ_CODE, ptr.CREATOR, ptr.EXECUTE_ROOM_NAME, ptr.SERVICE_REQ_STT_NAME));
                        }
                    }

                    // Tình trạng lâm sàng hiện tại (DHST + Tờ điều trị gần nhất)
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = tId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        var lastDhst = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                        Log(string.Format("  • Dấu hiệu sinh tồn gần nhất [{0}]: Mạch {1} ck/p | HA: {2}/{3} mmHg | NĐ: {4} °C | SpO2: {5}% | Thở: {6} l/p",
                            lastDhst.EXECUTE_TIME ?? lastDhst.CREATE_TIME, lastDhst.PULSE, lastDhst.BLOOD_PRESSURE_MAX, lastDhst.BLOOD_PRESSURE_MIN, lastDhst.TEMPERATURE, lastDhst.SPO2, lastDhst.BREATH_RATE));
                    }

                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkf, param);
                    if (trks != null && trks.Count > 0)
                    {
                        var lastTrk = trks.OrderByDescending(x => x.TRACKING_TIME).First();
                        Log(string.Format("  • Tình trạng lâm sàng ghi nhận [{0}] (BS {1}):\n     {2}",
                            lastTrk.TRACKING_TIME, lastTrk.CREATOR, lastTrk.CONTENT != null ? lastTrk.CONTENT.Replace("\r\n", "\n     ") : "Chưa ghi"));
                    }

                    // Kiểm tra truyền máu
                    bool hasBlood = false;
                    if (sss != null)
                    {
                        var bloods = sss.Where(x => (x.TDL_SERVICE_TYPE_ID == 5 || (x.SERVICE_TYPE_NAME != null && x.SERVICE_TYPE_NAME.ToUpper().Contains("MÁU")) ||
                                                    (x.TDL_SERVICE_NAME != null && (x.TDL_SERVICE_NAME.ToUpper().Contains("HỒNG CẦU") || x.TDL_SERVICE_NAME.ToUpper().Contains("HUYẾT TƯƠNG") || x.TDL_SERVICE_NAME.ToUpper().Contains("TIỂU CẦU")))) &&
                                                    x.TDL_INTRUCTION_TIME >= 20260824000000).ToList();
                        if (bloods.Count > 0)
                        {
                            hasBlood = true;
                            Log("  • TRUYỀN MÁU: CÓ");
                            foreach (var b in bloods)
                            {
                                Log(string.Format("     - [{0}] {1} | SL: {2} {3}", b.TDL_INTRUCTION_TIME, b.TDL_SERVICE_NAME, b.AMOUNT, b.SERVICE_UNIT_NAME));
                            }
                        }
                    }
                    if (!hasBlood)
                    {
                        Log("  • TRUYỀN MÁU: KHÔNG (Không có chỉ định truyền máu sau mổ)");
                    }

                    // CLS kiểm tra sau mổ
                    HisSereServTeinViewFilter teinf = new HisSereServTeinViewFilter();
                    teinf.TDL_TREATMENT_ID = tId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinf, param);
                    if (teins != null && teins.Count > 0)
                    {
                        var recentHgb = teins.Where(x => x.TEST_INDEX_CODE == "HGB" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Hemoglobin"))).OrderByDescending(x => x.MODIFY_TIME ?? x.CREATE_TIME).FirstOrDefault();
                        var recentHct = teins.Where(x => x.TEST_INDEX_CODE == "HCT" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Hematocrit"))).OrderByDescending(x => x.MODIFY_TIME ?? x.CREATE_TIME).FirstOrDefault();
                        var recentWbc = teins.Where(x => x.TEST_INDEX_CODE == "WBC" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Bạch cầu"))).OrderByDescending(x => x.MODIFY_TIME ?? x.CREATE_TIME).FirstOrDefault();
                        var recentPlt = teins.Where(x => x.TEST_INDEX_CODE == "PLT" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Tiểu cầu"))).OrderByDescending(x => x.MODIFY_TIME ?? x.CREATE_TIME).FirstOrDefault();

                        Log(string.Format("  • CLS kiểm tra gần nhất: CTM -> WBC: {0} | HGB: {1} | HCT: {2} | PLT: {3}",
                            recentWbc != null ? recentWbc.VALUE + " " + recentWbc.TEST_INDEX_UNIT_NAME : "Chưa làm lại",
                            recentHgb != null ? recentHgb.VALUE + " " + recentHgb.TEST_INDEX_UNIT_NAME : "Chưa làm lại",
                            recentHct != null ? recentHct.VALUE + " " + recentHct.TEST_INDEX_UNIT_NAME : "Chưa làm lại",
                            recentPlt != null ? recentPlt.VALUE + " " + recentPlt.TEST_INDEX_UNIT_NAME : "Chưa làm lại"));
                    }
                }

                // =========================================================================================
                // MỤC 3: DANH SÁCH BỆNH NHÂN DỰ KIẾN MỔ NGÀY MAI (25/08/2026) TỪ LỊCH MỔ
                // =========================================================================================
                Log("\n==========================================================================");
                Log("MỤC 3: DANH SÁCH BỆNH NHÂN DỰ KIẾN MỔ THỨ 3 NGÀY 25/08/2026 (12 BỆNH NHÂN)");
                Log("==========================================================================");

                var schedulePatients = new[]
                {
                    // PM5
                    new { Room = "PM5", Stt = 1, Name = "NGUYỄN ANH QUANG", Age = "45", Diag = "Hẹp ống sống L4-5L5S1", Method = "TLIF 2 tầng L4-5 L5-S1", Surgeon = "TS Trung" },
                    new { Room = "PM5", Stt = 2, Name = "PHẠM THỊ HIẾU", Age = "52", Diag = "Trượt L4-5/Hẹp ống sống, vùng thắt lưng/thoái hoá khớp gối 2 bên", Method = "TLIF L4-5", Surgeon = "TS Trung" },
                    new { Room = "PM5", Stt = 3, Name = "NGUYỄN THỊ HOA", Age = "59", Diag = "Thoát vị đĩa đệm L3/4, L4/5, L5/S1 / ĐTĐ type 2 - STT do thuốc - THA - TLLPM - Tăng men gan - Loãng xương", Method = "TLIF 3T", Surgeon = "TS Trung" },
                    new { Room = "PM5", Stt = 4, Name = "NGUYỄN THỊ HẰNG", Age = "46", Diag = "Mất vững C1-C2", Method = "CĐ C1-C2, ghép xương", Surgeon = "TS Trung" },
                    new { Room = "PM5", Stt = 5, Name = "TRẦN THỊ THU", Age = "63", Diag = "CTCS: vỡ xẹp L1", Method = "BXM", Surgeon = "BS Hoành" },
                    new { Room = "PM5", Stt = 6, Name = "TRẦN THỊ CHINH", Age = "76", Diag = "Xẹp thân đốt T12", Method = "BXM", Surgeon = "TS Trung" },

                    // PM2
                    new { Room = "PM2", Stt = 1, Name = "TRẦN VĂN CHÂU", Age = "66", Diag = "Đứt cũ gân gót P", Method = "tạo hình gân gót chân phải", Surgeon = "BS Luân + BS Cường H" },
                    new { Room = "PM2", Stt = 2, Name = "TRẦN DANH DŨNG", Age = "47", Diag = "Đứt dây chằng chéo trước gối phải", Method = "NS tái tạo dây chằng chéo trước", Surgeon = "BS Cường H" },
                    new { Room = "PM2", Stt = 3, Name = "TRẦN THỊ NGỌC HƯƠNG", Age = "67", Diag = "Gãy kín xương cánh tay trái", Method = "KHX", Surgeon = "BS Cường H" },
                    new { Room = "PM2", Stt = 4, Name = "PHÙNG MINH HÀ", Age = "41", Diag = "Gãy Dupuytren cổ chân phải", Method = "KHX", Surgeon = "BS Giang" },
                    new { Room = "PM2", Stt = 5, Name = "TRẦN THỊ TRANG", Age = "46", Diag = "U thần kinh gan chân phải", Method = "Lấy u, GPB", Surgeon = "BS Cường E" },
                    new { Room = "PM2", Stt = 6, Name = "MAI MINH HOÀNG", Age = "18", Diag = "Gãy kín 1/3G xương cánh tay phải", Method = "KHX", Surgeon = "BS Bình" }
                };

                foreach (var sp in schedulePatients)
                {
                    Log(string.Format("\n--- [{0} - STT {1}] BỆNH NHÂN: {2} ({3} TUỔI) ---", sp.Room, sp.Stt, sp.Name, sp.Age));
                    Log(string.Format("  • Chẩn đoán bảng giao ban: {0}", sp.Diag));
                    Log(string.Format("  • Phương pháp phẫu thuật: {0}", sp.Method));
                    Log(string.Format("  • Phẫu thuật viên: {0}", sp.Surgeon));

                    // Tìm trên HIS
                    string norm = sp.Name.Replace("*", "").Trim().ToUpper();
                    var matchedBed = dept57Patients.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && (x.TDL_PATIENT_NAME.ToUpper().Contains(norm) || norm.Contains(x.TDL_PATIENT_NAME.ToUpper())));
                    if (matchedBed != null)
                    {
                        var t = treatMap.ContainsKey(matchedBed.TREATMENT_ID) ? treatMap[matchedBed.TREATMENT_ID] : null;
                        Log(string.Format("  • Tìm thấy trên HIS: Mã BN: {0} | Mã BA: {1} | Vị trí: {2} - {3} | Giới: {4}",
                            matchedBed.TDL_PATIENT_CODE, matchedBed.TREATMENT_CODE, matchedBed.BED_ROOM_NAME, matchedBed.BED_NAME, t != null ? t.TDL_PATIENT_GENDER_NAME : ""));
                        Log(string.Format("  • Chẩn đoán HIS: {0} [{1}] - {2}", t != null ? t.ICD_NAME : "", t != null ? t.ICD_CODE : "", t != null ? t.ICD_TEXT : ""));

                        // Rà soát Bilan tiền phẫu
                        PrintPatientBilan(adapter, matchedBed.TREATMENT_ID, param, Log);
                    }
                    else
                    {
                        // Thử tìm trong toàn viện
                        var matchedAll = allInBedRooms != null ? allInBedRooms.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && (x.TDL_PATIENT_NAME.ToUpper().Contains(norm) || norm.Contains(x.TDL_PATIENT_NAME.ToUpper()))) : null;
                        if (matchedAll != null)
                        {
                            var t = treatMap.ContainsKey(matchedAll.TREATMENT_ID) ? treatMap[matchedAll.TREATMENT_ID] : null;
                            Log(string.Format("  • Tìm thấy tại Khoa khác (Khoa {0}): Mã BN: {1} | Buồng: {2} - {3} | Giới: {4}",
                                matchedAll.DEPARTMENT_ID, matchedAll.TDL_PATIENT_CODE, matchedAll.BED_ROOM_NAME, matchedAll.BED_NAME, t != null ? t.TDL_PATIENT_GENDER_NAME : ""));
                            PrintPatientBilan(adapter, matchedAll.TREATMENT_ID, param, Log);
                        }
                        else
                        {
                            // Thử tìm theo keyword trong Treatment
                            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                            tf.KEY_WORD = norm;
                            var searchTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                            if (searchTreats != null && searchTreats.Count > 0)
                            {
                                var st = searchTreats.OrderByDescending(x => x.IN_TIME).First();
                                Log(string.Format("  • Tìm thấy hồ sơ gần nhất: Mã BN: {0} | Mã BA: {1} | Vào viện: {2} | Khoa ID: {3} | Giới: {4}",
                                    st.TDL_PATIENT_CODE, st.TREATMENT_CODE, st.IN_TIME, st.LAST_DEPARTMENT_ID, st.TDL_PATIENT_GENDER_NAME));
                                PrintPatientBilan(adapter, st.ID, param, Log);
                            }
                            else
                            {
                                Log("  • Trạng thái HIS: Chưa tìm thấy buồng giường hiện tại (có thể chưa gán buồng hoặc vào viện trực tiếp PM)");
                            }
                        }
                    }
                }

                // =========================================================================================
                // MỤC 4: DANH SÁCH BỆNH NHÂN TRUYỀN MÁU HÔM QUA (24/08/2026)
                // =========================================================================================
                Log("\n==========================================================================");
                Log("MỤC 4: DANH SÁCH BỆNH NHÂN TRUYỀN MÁU HÔM QUA (24/08/2026)");
                Log("==========================================================================");

                int bloodCount = 0;
                foreach (var tId in allTreatIds)
                {
                    HisSereServViewFilter ssf = new HisSereServViewFilter();
                    ssf.TREATMENT_ID = tId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);
                    
                    var bloods = (sss != null) ? sss.Where(x => (x.TDL_SERVICE_TYPE_ID == 5 || (x.SERVICE_TYPE_NAME != null && x.SERVICE_TYPE_NAME.ToUpper().Contains("MÁU")) ||
                                                                (x.TDL_SERVICE_NAME != null && (x.TDL_SERVICE_NAME.ToUpper().Contains("HỒNG CẦU") || x.TDL_SERVICE_NAME.ToUpper().Contains("HUYẾT TƯƠNG") || x.TDL_SERVICE_NAME.ToUpper().Contains("TIỂU CẦU")))) &&
                                                                x.TDL_INTRUCTION_TIME >= 20260824000000 && x.TDL_INTRUCTION_TIME <= 20260825030500).ToList() : new List<V_HIS_SERE_SERV>();

                    // Kiểm tra cả tờ điều trị có y lệnh truyền máu trong ngày 24/8
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkf, param);
                    var bloodTrks = (trks != null) ? trks.Where(x => x.TRACKING_TIME >= 20260824000000 && x.TRACKING_TIME <= 20260825030500 &&
                        ((x.MEDICAL_INSTRUCTION != null && (x.MEDICAL_INSTRUCTION.ToUpper().Contains("TRUYỀN MÁU") || x.MEDICAL_INSTRUCTION.ToUpper().Contains("KHC") || x.MEDICAL_INSTRUCTION.ToUpper().Contains("HỒNG CẦU") || x.MEDICAL_INSTRUCTION.ToUpper().Contains("HUYẾT TƯƠNG"))) ||
                         (x.CONTENT != null && (x.CONTENT.ToUpper().Contains("TRUYỀN MÁU") || x.CONTENT.ToUpper().Contains("KHC") || x.CONTENT.ToUpper().Contains("HỒNG CẦU") || x.CONTENT.ToUpper().Contains("HUYẾT TƯƠNG"))))).ToList() : new List<V_HIS_TRACKING>();

                    if (bloods.Count > 0 || bloodTrks.Count > 0)
                    {
                        bloodCount++;
                        var bed = dept57Patients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                        var t = treatMap.ContainsKey(tId) ? treatMap[tId] : null;

                        Log(string.Format("\n--- BỆNH NHÂN TRUYỀN MÁU {0}: {1} ---", bloodCount, bed != null ? bed.TDL_PATIENT_NAME : ""));
                        Log(string.Format("  • Buồng: {0} - Giường: {1} | Mã BN: {2} | Mã BA: {3}",
                            bed != null ? bed.BED_ROOM_NAME : "", bed != null ? bed.BED_NAME : "",
                            bed != null ? bed.TDL_PATIENT_CODE : "", bed != null ? bed.TREATMENT_CODE : ""));
                        Log(string.Format("  • Chẩn đoán: {0} [{1}] - {2}", t != null ? t.ICD_NAME : "", t != null ? t.ICD_CODE : "", t != null ? t.ICD_TEXT : ""));

                        if (bloods.Count > 0)
                        {
                            foreach (var b in bloods)
                            {
                                Log(string.Format("  • Chế phẩm truyền: [{0}] {1} | Số lượng: {2} {3}",
                                    b.TDL_INTRUCTION_TIME, b.TDL_SERVICE_NAME, b.AMOUNT, b.SERVICE_UNIT_NAME));
                            }
                        }

                        if (bloodTrks.Count > 0)
                        {
                            foreach (var bt in bloodTrks)
                            {
                                Log(string.Format("  • Y lệnh tờ điều trị [{0}] (BS {1}): {2}",
                                    bt.TRACKING_TIME, bt.CREATOR, bt.MEDICAL_INSTRUCTION));
                            }
                        }

                        // Lấy chỉ số HGB và HCT trước khi truyền
                        HisSereServTeinViewFilter teinf = new HisSereServTeinViewFilter();
                        teinf.TDL_TREATMENT_ID = tId;
                        var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinf, param);
                        if (teins != null && teins.Count > 0)
                        {
                            var hgbs = teins.Where(x => x.TEST_INDEX_CODE == "HGB" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Hemoglobin"))).OrderBy(x => x.MODIFY_TIME ?? x.CREATE_TIME).ToList();
                            var hcts = teins.Where(x => x.TEST_INDEX_CODE == "HCT" || (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.Contains("Hematocrit"))).OrderBy(x => x.MODIFY_TIME ?? x.CREATE_TIME).ToList();

                            Log("  • Chỉ số HGB / HCT ghi nhận:");
                            foreach (var h in hgbs)
                            {
                                Log(string.Format("     - HGB lúc {0}: {1} {2}", h.MODIFY_TIME ?? h.CREATE_TIME, h.VALUE, h.TEST_INDEX_UNIT_NAME));
                            }
                            foreach (var hc in hcts)
                            {
                                Log(string.Format("     - HCT lúc {0}: {1} {2}", hc.MODIFY_TIME ?? hc.CREATE_TIME, hc.VALUE, hc.TEST_INDEX_UNIT_NAME));
                            }
                        }
                    }
                }

                if (bloodCount == 0)
                {
                    Log("✔ Không có bệnh nhân nào có y lệnh hoặc xuất kho truyền máu tại Khoa 57 trong ngày 24/08/2026.");
                }
            }
        }

        static void PrintPatientBilan(MyAdapter adapter, long treatmentId, CommonParam param, Action<string> Log)
        {
            // 1. Xét nghiệm (Tein)
            HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
            teinFilter.TDL_TREATMENT_ID = treatmentId;
            var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
            if (teins != null && teins.Count > 0)
            {
                Func<string, string> getTein = delegate(string codeOrName) {
                    var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                        ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(codeOrName, StringComparison.OrdinalIgnoreCase)) ||
                         (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(codeOrName, StringComparison.OrdinalIgnoreCase) >= 0)));
                    return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "Chưa có";
                };

                Log("  • CTM: WBC: " + getTein("WBC") + " | Neut%: " + getTein("NEUT%") + " | HGB: " + getTein("HGB") + " | PLT: " + getTein("PLT") + " | RBC: " + getTein("RBC") + " | HCT: " + getTein("HCT"));
                Log("  • Đông máu: PT%: " + getTein("PT (%)") + " | INR: " + getTein("PT - INR") + " | APTT: " + getTein("APTT (s)") + " | Fib: " + getTein("Fibrinogen"));
                Log("  • Sinh hóa: Glucose: " + getTein("Glucose") + " | Ure: " + getTein("Urê") + " | Creatinin: " + getTein("Creatinin") + " | AST: " + getTein("AST") + " | ALT: " + getTein("ALT") + " | eGFR: " + getTein("eGFR"));
                Log("  • Điện giải: Na: " + getTein("Natri") + " | K: " + getTein("Kali") + " | Cl: " + getTein("Clo") + " | Ca: " + getTein("Calci"));
                Log("  • Nhóm máu: ABO: " + getTein("ABO") + " | Rh: " + getTein("Rh"));
                Log("  • Virus/Miễn dịch: HIV: " + getTein("HIV") + " | HBsAg: " + getTein("HBsAg") + " | HCV: " + getTein("HCV"));
            }
            else
            {
                Log("  • Xét nghiệm máu: Chưa có kết quả");
            }

            // 2. Chẩn đoán hình ảnh & thăm dò chức năng (SereServ)
            HisSereServViewFilter ssFilter = new HisSereServViewFilter();
            ssFilter.TREATMENT_ID = treatmentId;
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);
            if (sss != null && sss.Count > 0)
            {
                var clsList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 8 || x.TDL_SERVICE_TYPE_ID == 11).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                if (clsList.Count > 0)
                {
                    Log("  • CĐHA & Thăm dò chức năng:");
                    foreach (var s in clsList)
                    {
                        Log(string.Format("     - [{0}] {1} ({2}) | Phòng: {3}",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME));
                    }
                }
            }
        }
    }
}
