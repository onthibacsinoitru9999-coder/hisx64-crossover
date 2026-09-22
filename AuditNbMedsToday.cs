using System;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
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

public class SheetPatient
{
    public string Stt { get; set; }
    public string PatientCode { get; set; }
    public string Name { get; set; }
    public string Age { get; set; }
    public string Gender { get; set; }
    public string Room { get; set; }
    public string Bed { get; set; }
    public string Diagnosis { get; set; }
}

public class PatientAuditResult
{
    public SheetPatient SheetData { get; set; }
    public V_HIS_TREATMENT Treatment { get; set; }
    public bool IsFoundInHis { get; set; }
    public bool IsDischarged { get; set; }
    public bool HasPrescriptionToday { get; set; }
    public List<string> TodayMeds { get; set; }
    public bool HasTrackingToday { get; set; }
    public List<string> TodayTrackings { get; set; }
    public string Note { get; set; }

    public PatientAuditResult()
    {
        TodayMeds = new List<string>();
        TodayTrackings = new List<string>();
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

        Run();
    }

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("===============================================================================");
        Console.WriteLine("🔍 BẮT ĐẦU RÀ SOÁT ĐƠN THUỐC & TỜ ĐIỀU TRỊ HÔM NAY (22/09/2026) TẠI NINH BÌNH");
        Console.WriteLine("===============================================================================");

        // 1. Tải danh sách từ Google Sheet Ninh Bình
        string sheetId = "1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM";
        string gid = "914015390";
        string csvUrl = string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}", sheetId, gid);
        
        List<SheetPatient> sheetPatients = new List<SheetPatient>();
        try
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(csvUrl);
            req.UserAgent = "Mozilla/5.0";
            using (var resp = req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string line;
                int rowIdx = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    rowIdx++;
                    if (rowIdx == 1) continue; // Header
                    var cols = ParseCsvLine(line);
                    if (cols.Count >= 3 && (!string.IsNullOrEmpty(cols[1]) || !string.IsNullOrEmpty(cols[2])))
                    {
                        var sp = new SheetPatient
                        {
                            Stt = cols.Count > 0 ? cols[0].Trim() : "",
                            PatientCode = cols.Count > 1 ? cols[1].Trim() : "",
                            Name = cols.Count > 2 ? cols[2].Trim() : "",
                            Age = cols.Count > 3 ? cols[3].Trim() : "",
                            Gender = cols.Count > 4 ? cols[4].Trim() : "",
                            Room = cols.Count > 5 ? cols[5].Trim() : "",
                            Bed = cols.Count > 6 ? cols[6].Trim() : "",
                            Diagnosis = cols.Count > 7 ? cols[7].Trim() : ""
                        };
                        sheetPatients.Add(sp);
                    }
                }
            }
            Console.WriteLine(string.Format("✔ Đã tải {0} bệnh nhân từ Google Sheet Ninh Bình.", sheetPatients.Count));
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi tải Google Sheet: " + ex.Message);
            return;
        }

        // 2. Khởi tạo Token & API Consumer
        string token = "";
        string tokenFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_nb.token");
        if (File.Exists(tokenFile))
        {
            token = File.ReadAllText(tokenFile, Encoding.UTF8).Split('|')[0].Trim();
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adp = new MyAdapter();
        CommonParam cp = new CommonParam();

        long todayStart = 20260922000000;
        long todayEnd   = 20260922235959;

        List<PatientAuditResult> results = new List<PatientAuditResult>();

        int idx = 0;
        foreach (var sp in sheetPatients)
        {
            idx++;
            var res = new PatientAuditResult { SheetData = sp };
            results.Add(res);

            string pCode = sp.PatientCode.PadLeft(10, '0');
            if (string.IsNullOrEmpty(sp.PatientCode) && !string.IsNullOrEmpty(sp.Name))
            {
                // Tìm theo tên nếu mã BN trống
                var tfName = new HisTreatmentViewFilter { KEY_WORD = sp.Name.Replace("*", "").Trim() };
                var trListByName = adp.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tfName, cp);
                if (trListByName != null && trListByName.Count > 0)
                {
                    res.Treatment = trListByName.LastOrDefault(x => x.IS_PAUSE != 1) ?? trListByName.Last();
                }
            }
            else
            {
                var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
                var trList = adp.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
                if (trList != null && trList.Count > 0)
                {
                    res.Treatment = trList.LastOrDefault(x => x.IS_PAUSE != 1) ?? trList.Last();
                }
            }

            if (res.Treatment == null)
            {
                res.IsFoundInHis = false;
                res.Note = "Không tìm thấy trên HIS";
                continue;
            }

            res.IsFoundInHis = true;
            if (res.Treatment.IS_PAUSE == 1)
            {
                res.IsDischarged = true;
                res.Note = "ĐÃ RA VIỆN";
            }

            long tId = res.Treatment.ID;

            // Rà soát y lệnh thuốc hôm nay (SERVICE_REQ_TYPE_ID = 6 hoặc 7, hoặc có SereServ thuốc)
            var srf = new HisServiceReqViewFilter
            {
                TREATMENT_ID = tId,
                INTRUCTION_TIME_FROM = todayStart,
                INTRUCTION_TIME_TO = todayEnd
            };
            var todayReqs = adp.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            if (todayReqs != null)
            {
                var medReqs = todayReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 || x.SERVICE_REQ_TYPE_ID == 7 ||
                    (x.SERVICE_REQ_TYPE_NAME != null && x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("thuốc"))).ToList();
                
                if (medReqs.Count > 0)
                {
                    res.HasPrescriptionToday = true;
                    foreach (var mr in medReqs)
                    {
                        res.TodayMeds.Add(string.Format("{0} ({1}) lúc {2}", mr.SERVICE_REQ_CODE, mr.SERVICE_REQ_TYPE_NAME, mr.INTRUCTION_TIME.ToString().Substring(8, 4)));
                    }
                }
            }

            // Rà soát tờ điều trị hôm nay
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tId };
            var allTrks = adp.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp);
            if (allTrks != null)
            {
                var todayTrks = allTrks.Where(x => x.TRACKING_TIME >= todayStart && x.TRACKING_TIME <= todayEnd).ToList();
                if (todayTrks.Count > 0)
                {
                    res.HasTrackingToday = true;
                    foreach (var tk in todayTrks)
                    {
                        res.TodayTrackings.Add(string.Format("ID {0} ({1}) BS {2}", tk.ID, tk.TRACKING_TIME.ToString().Substring(8, 4), tk.CREATOR));
                    }
                }
            }
        }

        // BÁO CÁO TỔNG HỢP
        Console.WriteLine("\n========================================================================================================================");
        Console.WriteLine("📊 KẾT QUẢ ĐỐI SOÁT CHI TIẾT TỪNG BỆNH NHÂN:");
        Console.WriteLine("========================================================================================================================");
        Console.WriteLine(string.Format("{0,-4} | {1,-10} | {2,-22} | {3,-7} | {4,-5} | {5,-16} | {6,-16} | {7}", 
            "STT", "Mã BN", "Họ và tên", "Phòng", "Giường", "Đơn thuốc 22/09", "Tờ ĐTrị 22/09", "Trạng thái"));
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");

        List<PatientAuditResult> missingMeds = new List<PatientAuditResult>();
        List<PatientAuditResult> missingTrackings = new List<PatientAuditResult>();

        foreach (var r in results)
        {
            string pCode = r.SheetData.PatientCode;
            string name = r.SheetData.Name.Replace("*", "").Trim();
            string room = r.SheetData.Room;
            string bed = r.SheetData.Bed;
            string medStatus = r.HasPrescriptionToday ? "✔ ĐÃ CÓ (" + r.TodayMeds.Count + ")" : "❌ CHƯA CÓ";
            string trkStatus = r.HasTrackingToday ? "✔ ĐÃ CÓ (" + r.TodayTrackings.Count + ")" : "❌ CHƯA CÓ";
            string state = r.IsDischarged ? "ĐÃ RA VIỆN" : (r.IsFoundInHis ? "Đang nằm viện" : "Không tìm thấy");

            if (!r.IsDischarged && r.IsFoundInHis)
            {
                if (!r.HasPrescriptionToday) missingMeds.Add(r);
                if (!r.HasTrackingToday) missingTrackings.Add(r);
            }

            Console.WriteLine(string.Format("{0,-4} | {1,-10} | {2,-22} | {3,-7} | {4,-5} | {5,-16} | {6,-16} | {7}",
                r.SheetData.Stt, pCode, name.Length > 22 ? name.Substring(0, 22) : name, room, bed, medStatus, trkStatus, state));
        }

        Console.WriteLine("========================================================================================================================");
        Console.WriteLine(string.Format("\n🚨 DANH SÁCH BỆNH NHÂN CHƯA CÓ ĐƠN THUỐC HÔM NAY (22/09/2026): {0} BN", missingMeds.Count));
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
        int mIdx = 1;
        foreach (var m in missingMeds)
        {
            Console.WriteLine(string.Format("{0:D2}. [{1}] {2,-22} | Buồng: {3,-6} | Giường: {4,-5} | Chẩn đoán: {5}",
                mIdx++, m.SheetData.PatientCode, m.SheetData.Name.Replace("*", "").Trim(), m.SheetData.Room, m.SheetData.Bed, m.SheetData.Diagnosis));
        }

        Console.WriteLine(string.Format("\n📝 DANH SÁCH BỆNH NHÂN CHƯA CÓ TỜ ĐIỀU TRỊ HÔM NAY (22/09/2026): {0} BN", missingTrackings.Count));
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
        int tIdx = 1;
        foreach (var t in missingTrackings)
        {
            Console.WriteLine(string.Format("{0:D2}. [{1}] {2,-22} | Buồng: {3,-6} | Giường: {4,-5} | Mã ĐT: {5}",
                tIdx++, t.SheetData.PatientCode, t.SheetData.Name.Replace("*", "").Trim(), t.SheetData.Room, t.SheetData.Bed, t.Treatment != null ? t.Treatment.TREATMENT_CODE : "N/A"));
        }
        Console.WriteLine("========================================================================================================================");
    }

    static List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder cur = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    cur.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(cur.ToString());
                cur.Clear();
            }
            else
            {
                cur.Append(c);
            }
        }
        result.Add(cur.ToString());
        return result;
    }
}
