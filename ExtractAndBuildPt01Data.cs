using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapterEx : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class ExtractAndBuildPt01Data
{
    public static MyAdapterEx adapter = new MyAdapterEx();

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        List<string> candidates = new List<string>();
        if (Directory.Exists(preferredDir)) candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
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
                        if (start + 64 <= chunk.Length)
                        {
                            string t = chunk.Substring(start, 64);
                            if (t.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return t;
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        string[] patientCodes = new string[]
        {
            "0004062838", // THÌN
            "0001530113", // HÒA
            "0004079114", // TƯ
            "0004050211", // THIỆN
            "0004089090", // TRUNG
            "0002840646", // BỎNG
            "0004081634", // THU
            "0003837595", // NGỌC
            "0004079203", // CHỦ
            "0004067971", // THẢO
            "0003863470", // LƯỢNG
            "0002740977"  // PHƯƠNG
        };

        var sbOutput = new StringBuilder();

        for (int i = 0; i < patientCodes.Length; i++)
        {
            string code = patientCodes[i];
            var tf = new HisTreatmentViewFilter { PATIENT_CODE = code };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", consumer, tf, cp);
            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy BN: " + code);
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();
            Console.WriteLine(string.Format("\n=== [{0}/{1}] {2} (Mã: {3} | ID: {4}) ===", i + 1, patientCodes.Length, tr.TDL_PATIENT_NAME, tr.PATIENT_CODE, tr.ID));

            // 1. Xét nghiệm
            var ssf = new HisSereServViewFilter { TDL_TREATMENT_ID = tr.ID };
            var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", consumer, ssf, cp);

            var teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tr.ID };
            var teinList = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", consumer, teinFilter, cp);

            // 2. Tờ điều trị gần nhất
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, trkFilter, cp);
            var latestTrk = trkList != null && trkList.Count > 0 ? trkList.OrderByDescending(x => x.TRACKING_TIME).First() : null;

            // 3. DHST
            var dhstFilter = new HisDhstViewFilter { TREATMENT_ID = tr.ID };
            var dhstList = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", consumer, dhstFilter, cp);
            var latestDhst = dhstList != null && dhstList.Count > 0 ? dhstList.OrderByDescending(x => x.EXECUTE_TIME).First() : null;

            Console.WriteLine(string.Format("Họ tên: {0} | DOB: {1} | Giới: {2} | ĐC: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_DOB, tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_ADDRESS));
            Console.WriteLine(string.Format("Vào viện: {0} | ICD: {1} - {2}", tr.IN_TIME, tr.ICD_CODE, tr.ICD_NAME));
            if (latestDhst != null)
            {
                Console.WriteLine(string.Format("DHST: Mạch {0}, HA {1}/{2}, T° {3}, SpO2 {4}", latestDhst.PULSE, latestDhst.BLOOD_PRESSURE_MAX, latestDhst.BLOOD_PRESSURE_MIN, latestDhst.TEMPERATURE, latestDhst.SPO2));
            }

            // In tóm tắt các kết quả XN chính
            if (teinList != null && teinList.Count > 0)
            {
                var dict = new Dictionary<string, string>();
                foreach (var t in teinList)
                {
                    if (!string.IsNullOrEmpty(t.TEST_INDEX_CODE) && !string.IsNullOrEmpty(t.VALUE))
                    {
                        dict[t.TEST_INDEX_CODE.Trim().ToUpper()] = t.VALUE.Trim();
                    }
                }
                Console.WriteLine("Xét nghiệm tìm thấy: " + string.Join(", ", dict.Select(kv => kv.Key + "=" + kv.Value).Take(15)));
            }

            // In các dịch vụ CĐHA
            if (ssList != null && ssList.Count > 0)
            {
                var cdha = ssList.Where(s => s.TDL_SERVICE_TYPE_ID == 2 || s.TDL_SERVICE_TYPE_ID == 3).ToList(); // Chẩn đoán hình ảnh / TDCN
                if (cdha.Count > 0)
                {
                    Console.WriteLine("CĐHA đã chỉ định: " + string.Join("; ", cdha.Select(s => s.TDL_SERVICE_NAME).Distinct()));
                }
            }
        }
    }
}
