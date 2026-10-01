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

public class AssignOct02Rations
{
    public static MyAdapter adapter = new MyAdapter();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
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
                            return parts[0];
                        }
                    }
                }
            }
        }
        catch { }

        List<string> candidates = new List<string>();
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

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

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true"))
                    {
                        continue;
                    }

                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string tok = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                return tok;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                CommonParam param = new CommonParam();
                var tok = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (tok != null) token = tok.TokenCode;
                else
                {
                    tok = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (tok != null) token = tok.TokenCode;
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không lấy được token!");
            return;
        }

        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam p = new CommonParam();

        // Update WorkInfo
        try
        {
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, p);
        }
        catch { }

        string[] patientCodes = new string[] { "0004093786", "0002779975", "0004099512" };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🍱 CHỈ ĐỊNH SUẤT ĂN TRƯA VÀ CHIỀU NGÀY 02/10/2026 CHO 3 BỆNH NHÂN MỚI");
        Console.WriteLine("===============================================================================\n");

        long oct02InstructionTime = 20261002060000; // 06:00 ngày 02/10

        foreach (var code in patientCodes)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = code };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, p);
            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy BN: " + code);
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();
            long tId = tr.ID;

            // Tracking
            HisTrackingFilter trkFilter = new HisTrackingFilter { TREATMENT_ID = tId };
            var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, p);
            long latestTrackingId = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First().ID : 0;

            // Check existing rations on 02/10
            HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
            var rList = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mosConsumer, rf, p);
            var oct02Rations = rList != null ? rList.Where(x => x.INTRUCTION_TIME >= 20261002000000 && x.INTRUCTION_TIME < 20261003000000).ToList() : new List<V_HIS_SERE_SERV_RATION>();

            Console.WriteLine(string.Format("👉 BN: {0} ({1}) | Mã ĐT: {2} | TrID: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tId));
            Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1}", tr.ICD_CODE, tr.ICD_NAME));
            Console.WriteLine(string.Format("   Số suất ăn hiện có ngày 02/10: {0}", oct02Rations.Count));

            if (oct02Rations.Count > 0)
            {
                foreach (var r in oct02Rations)
                {
                    Console.WriteLine(string.Format("     - [SS_ID: {0}] (Bữa ID: {1}) lúc {2}", r.ID, r.RATION_TIME_ID, r.INTRUCTION_TIME));
                }
            }

            if (oct02Rations.Count == 0)
            {
                // Chỉ định 2 bữa Trưa & Chiều BT01:
                // Trưa: 30153 (RationTimeId = 3)
                // Chiều: 30154 (RationTimeId = 5)
                var rationServices = new List<RationServiceSDO>
                {
                    new RationServiceSDO { ServiceId = 30153, PatientTypeId = 42, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 3 } },
                    new RationServiceSDO { ServiceId = 30154, PatientTypeId = 42, RoomId = 5809, Amount = 1.0m, RationTimeIds = new List<long> { 5 } }
                };

                var sdo = new HisRationServiceReqSDO
                {
                    TreatmentIds = new List<long> { tr.ID },
                    InstructionTimes = new List<long> { oct02InstructionTime },
                    RequestRoomId = 5248,
                    RequestLoginName = "034727",
                    RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME,
                    IcdSubCode = tr.ICD_SUB_CODE,
                    IcdText = tr.ICD_TEXT,
                    HalfInFirstDay = false,
                    IsForAutoCreateRation = false,
                    IsForHomie = false,
                    TrackingId = latestTrackingId > 0 ? (long?)latestTrackingId : null,
                    RationServices = rationServices
                };

                CommonParam callParam = new CommonParam();
                try
                {
                    var result = adapter.PostData<object>("api/HisServiceReq/RationCreate", mosConsumer, sdo, callParam);
                    if (!callParam.HasException)
                    {
                        Console.WriteLine("   ✔ Chỉ định THÀNH CÔNG Suất ăn Trưa & Chiều (BT01) ngày 02/10/2026!");
                    }
                    else
                    {
                        if (callParam.Messages != null)
                        {
                            foreach (var msg in callParam.Messages) Console.WriteLine("    ❌ " + msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    ❌ Exception: " + ex.Message);
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine("===============================================================================");
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

        AssignOct02Rations.Run();
    }
}
