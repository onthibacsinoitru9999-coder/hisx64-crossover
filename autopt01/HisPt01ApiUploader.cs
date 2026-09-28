using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using HisPt01UiUploader;

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

public class SarPrintCreateSDO
{
    public string TITLE { get; set; }
    public string DESCRIPTION { get; set; }
    public string CONTENT { get; set; }
    public string GROUP_CODE { get; set; }
    public string APP_CREATOR { get; set; }
}

public class SarPrintResult
{
    public long ID { get; set; }
    public bool Success { get; set; }
}

class HisPt01ApiUploader
{
    const string HN_MOS_BASE = "http://192.168.7.236:1608/";
    const string NB_MOS_BASE = "http://192.168.7.239:1608/";
    const string SAR_BASE    = "http://192.168.7.200:1409/";

    const string LOG_PATH_D  = @"D:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
    const string LOG_PATH_E  = @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";

    const string PT01_TITLE       = "PT-01. Bien ban hoi chan thong qua phau thuat";
    const string PT01_DESCRIPTION = "PT-01. Bien ban hoi chan thong qua phau thuat";

    static string token = "";
    static string mosBase = NB_MOS_BASE;
    static MyAdapter adapter;
    static CommonParam param;

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new System.Reflection.AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return System.Reflection.Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return System.Reflection.Assembly.LoadFrom(path2);
            DirectoryInfo cur = new DirectoryInfo(folderPath);
            for (int i = 0; i < 5; i++)
            {
                if (cur.Parent == null) break;
                cur = cur.Parent;
                string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                if (File.Exists(pRef)) return System.Reflection.Assembly.LoadFrom(pRef);
            }
            return null;
        };

        RealMain(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RealMain(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   ⚡ HIS PT-01 API UPLOADER — autopt01 codename             ║");
        Console.WriteLine("║   Nạp Biên Bản Mổ PT-01 qua SAR API (Không cần UI)         ║");
        Console.WriteLine("║   SAR endpoint: api/SarPrint/Create                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        token = ReadTokenFromLog();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("X Không lấy được token từ LogSystem.txt. HIS phải đang chạy!");
            return;
        }
        Console.WriteLine(string.Format("[V] Token: {0}...{1}", token.Substring(0, 8), token.Substring(56)));

        param = new CommonParam();
        adapter = new MyAdapter();

        bool batch = args.Any(a => a == "--batch");
        string folderArg = null;
        var bnCodes = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--folder" && i + 1 < args.Length) folderArg = args[++i];
            else if (!args[i].StartsWith("-"))
            {
                bnCodes.AddRange(args[i].Split(',').Select(x => x.Trim()).Where(x => x.Length > 0));
            }
        }

        var docxFiles = new List<string>();

        if (batch || folderArg != null)
        {
            var baseDir = folderArg ?? FindLatestPt01Folder();
            if (!Directory.Exists(baseDir))
            {
                Console.WriteLine(string.Format("X Không tìm thấy thư mục PT-01: {0}", baseDir));
                return;
            }
            docxFiles.AddRange(Directory.GetFiles(baseDir, "*.docx").OrderBy(f => f));
            Console.WriteLine(string.Format("[+] Batch mode: tìm thấy {0} file trong {1}", docxFiles.Count, baseDir));
        }
        else if (bnCodes.Count > 0)
        {
            var baseDir = FindLatestPt01Folder();
            foreach (var code in bnCodes)
            {
                var found = FindDocxByCode(baseDir, code);
                if (found != null)
                {
                    docxFiles.Add(found);
                    Console.WriteLine(string.Format("[V] Tìm thấy file cho BN {0}: {1}", code, Path.GetFileName(found)));
                }
                else
                {
                    Console.WriteLine(string.Format("[!] Không tìm thấy file docx cho BN {0}", code));
                }
            }
        }
        else
        {
            PrintHelp(); return;
        }

        if (docxFiles.Count == 0)
        {
            Console.WriteLine("X Không có file nào để xử lý!");
            return;
        }

        int ok = 0, fail = 0;
        var results = new List<Tuple<string, bool, string>>();

        foreach (var docx in docxFiles)
        {
            Console.WriteLine("\n------------------------------------------------------------");
            Console.WriteLine(string.Format("[->] Đang nạp: {0}", Path.GetFileName(docx)));
            try
            {
                string patCode = ExtractPatientCode(docx);
                long treatmentId = 0;
                string patName = "";

                if (!string.IsNullOrEmpty(patCode))
                {
                    treatmentId = GetTreatmentId(patCode, out patName);
                }

                if (treatmentId == 0)
                {
                    Console.WriteLine(string.Format("   [!] Không tìm thấy TreatmentId cho {0} trên MOS NB. Nạp không kèm Treatment.", patCode));
                }
                else
                {
                    Console.WriteLine(string.Format("   [V] BN: {0} | TreatmentID: {1}", patName, treatmentId));
                }

                string cleanDocx = WordCleaner.EnsureCleanDocx(Path.GetFullPath(docx));
                string contentB64 = Convert.ToBase64String(File.ReadAllBytes(cleanDocx));
                Console.WriteLine(string.Format("   [V] CONTENT base64: {0} chars (docx: {1} bytes)", contentB64.Length, new FileInfo(cleanDocx).Length));

                long newId = CreateSarPrint(contentB64, treatmentId, Path.GetFileNameWithoutExtension(docx));

                if (newId > 0)
                {
                    Console.WriteLine(string.Format("   [V] ĐÃ TẠO SAR_PRINT ID: {0}", newId));
                    results.Add(Tuple.Create(Path.GetFileName(docx), true, "SarPrint ID=" + newId));
                    ok++;
                }
                else
                {
                    Console.WriteLine("   [X] Tạo SarPrint thất bại!");
                    results.Add(Tuple.Create(Path.GetFileName(docx), false, "API returned 0 or error"));
                    fail++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("   [X] Lỗi: {0}", ex.Message));
                results.Add(Tuple.Create(Path.GetFileName(docx), false, ex.Message));
                fail++;
                if (fail >= 2)
                {
                    Console.WriteLine("\n CẮT CẦU DAO: 2 lần lỗi liên tiếp! Dừng lại.");
                    break;
                }
            }
        }

        Console.WriteLine("\n============================================================");
        Console.WriteLine(string.Format(" KẾT QUẢ: {0}/{1} thành công | {2} lỗi", ok, docxFiles.Count, fail));
        Console.WriteLine("============================================================");
        Console.WriteLine(string.Format(" {0,-4} {1,-45} {2,-20} {3,-20}", "STT", "File", "Kết quả", "Thông tin"));
        Console.WriteLine(string.Format(" {0,-4} {1,-45} {2,-20} {3,-20}", "---", "---", "---", "---"));
        for (int i = 0; i < results.Count; i++)
        {
            var t = results[i];
            string status = t.Item2 ? "V OK" : "X FAIL";
            string shortF = t.Item1.Length > 44 ? t.Item1.Substring(0, 41) + "..." : t.Item1;
            Console.WriteLine(string.Format(" {0,-4} {1,-45} {2,-20} {3,-20}", i + 1, shortF, status, t.Item3));
        }
        Console.WriteLine("============================================================");
        Console.WriteLine("\n  Nhắc Bác sĩ: Vào HIS bật bộ lọc 'Tất cả bác sĩ' để xem biểu mẫu vừa tạo.");
    }

    static long CreateSarPrint(string contentB64, long treatmentId, string fileTitle)
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(SAR_BASE + "api/SarPrint/Create");
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers["Authorization"] = "Bearer " + token;

            string json = string.Format("{{\"CommonParam\":{{}},\"ApiData\":{{\"TITLE\":\"{0}\",\"DESCRIPTION\":\"{1}\",\"CONTENT\":\"{2}\"}}}}", 
                PT01_TITLE, PT01_DESCRIPTION, contentB64);

            using (var sw = new StreamWriter(req.GetRequestStream()))
            {
                sw.Write(json);
            }

            using (var res = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                string respText = sr.ReadToEnd();
                // Simple regex to extract ID
                var m = Regex.Match(respText, @"\""ID\""\s*:\s*(\d+)");
                if (m.Success)
                {
                    long newId = long.Parse(m.Groups[1].Value);
                    if (treatmentId > 0)
                    {
                        LinkPrintToTreatment(newId, treatmentId);
                    }
                    return newId;
                }
                else
                {
                    Console.WriteLine("   [X] Không tìm thấy ID trong response: " + (respText.Length > 200 ? respText.Substring(0, 200) + "..." : respText));
                }
            }
        }
        catch (WebException wex)
        {
            Console.WriteLine("   [X] Lỗi WebException: " + wex.Message);
            if (wex.Response != null)
            {
                using (var sr = new StreamReader(wex.Response.GetResponseStream()))
                    Console.WriteLine("   [X] Chi tiết: " + sr.ReadToEnd());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("   [X] Lỗi CreateSarPrint: " + ex.Message);
        }
        return 0;
    }

    static void LinkPrintToTreatment(long sarPrintId, long treatmentId)
    {
        try
        {
            var payload = new
            {
                ID = treatmentId,
                JSON_PRINT_ID = sarPrintId.ToString()
            };
            var mosConsumer = GetMosConsumer();
            adapter.PostData<object>("api/HisTreatment/UpdateJsonPrintId", mosConsumer, payload, param);
            Console.WriteLine(string.Format("   [V] Đã liên kết SarPrint {0} với Treatment {1}", sarPrintId, treatmentId));
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("   [!] Không liên kết được với Treatment: {0}", ex.Message));
        }
    }

    static long GetTreatmentId(string patientCode, out string patName)
    {
        patName = patientCode;
        try
        {
            patientCode = patientCode.PadLeft(10, '0');
            Console.WriteLine(string.Format("   [*] Đang tìm TreatmentId cho mã BN {0} trên MOS ({1})...", patientCode, mosBase));

            string json = string.Format("{{\"CommonParam\":{{}},\"ApiData\":{{\"PATIENT_CODE__EXACT\":\"{0}\"}}}}", patientCode);
            string base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            string url = mosBase + "api/HisTreatment/GetView?param=" + Uri.EscapeDataString(base64Json);

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers["Authorization"] = "Bearer " + token;
            req.Timeout = 10000;

            using (var res = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                string respText = sr.ReadToEnd();
                // We just need the ID from the first object in Data array, and TDL_PATIENT_NAME.
                // It looks like: "Data":[{"ID":3857786,"TDL_PATIENT_NAME":"ZHANG JIE",...
                var idMatch = Regex.Match(respText, @"\""ID\""\s*:\s*(\d+)");
                var nameMatch = Regex.Match(respText, @"\""TDL_PATIENT_NAME\""\s*:\s*\""([^\""]+)\""");
                
                if (idMatch.Success)
                {
                    if (nameMatch.Success) patName = nameMatch.Groups[1].Value;
                    return long.Parse(idMatch.Groups[1].Value);
                }
                else
                {
                    Console.WriteLine("   [!] Không tìm thấy Treatment ID trong JSON phản hồi.");
                }
            }
        }
        catch (WebException wex)
        {
            Console.WriteLine("   [!] WebException trong GetTreatmentId: " + wex.Message);
            if (wex.Response != null)
            {
                using (var sr = new StreamReader(wex.Response.GetResponseStream()))
                    Console.WriteLine("   [!] Chi tiết: " + sr.ReadToEnd());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("   [!] GetTreatmentId error: {0}", ex.Message));
        }
        return 0;
    }

    static string ReadTokenFromLog()
    {
        var candidates = new List<string>();
        try
        {
            foreach (var p in Process.GetProcessesByName("HIS"))
            {
                try
                {
                    string dir = Path.GetDirectoryName(p.MainModule.FileName);
                    candidates.Add(Path.Combine(dir, "Logs", "LogSystem.txt"));
                }
                catch { }
            }
        }
        catch { }

        candidates.Add(@"D:\New folder (3)\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt");
        candidates.Add(@"D:\his 3-9\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt");
        candidates.Add(LOG_PATH_D);
        candidates.Add(LOG_PATH_E);

        foreach (var logPath in candidates)
        {
            if (!File.Exists(logPath)) continue;
            try
            {
                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    long len = fs.Length;
                    if (len > 300000)
                    {
                        fs.Seek(len - 300000, SeekOrigin.Begin);
                    }
                    var text = sr.ReadToEnd();
                    var lines = text.Split('\n');
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        if (lines[i].Contains("TokenCode|"))
                        {
                            var mosMatch = Regex.Match(lines[i], @"MosBaseUri\|([^|]+)");
                            if (mosMatch.Success)
                            {
                                mosBase = mosMatch.Groups[1].Value;
                            }

                            int idx = lines[i].IndexOf("TokenCode|") + 10;
                            if (lines[i].Length >= idx + 64)
                                return lines[i].Substring(idx, 64);
                        }
                    }
                }
            }
            catch { }
        }
        return "";
    }

    static string FindLatestPt01Folder()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var list = new List<string>
        {
            Path.Combine(baseDir, "Reports", "BienBanHoiChan_PT01", "PT01_20260926_HN"),
            Path.Combine(baseDir, "..", "Reports", "BienBanHoiChan_PT01", "PT01_20260926_HN"),
            Path.Combine(baseDir, "Reports", "BienBanHoiChan_PT01"),
            Path.Combine(baseDir, "..", "Reports", "BienBanHoiChan_PT01")
        };
        foreach (var d in list)
        {
            if (Directory.Exists(d)) return d;
        }
        return baseDir;
    }

    static string FindDocxByCode(string folder, string code)
    {
        if (Directory.Exists(folder))
        {
            var match = Directory.GetFiles(folder, "*.docx", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileName(f).Contains(code));
            if (match != null) return match;
        }
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var r1 = Path.Combine(baseDir, "Reports");
        var r2 = Path.Combine(baseDir, "..", "Reports");
        string reports = Directory.Exists(r1) ? r1 : (Directory.Exists(r2) ? r2 : null);
        if (reports != null && Directory.Exists(reports))
        {
            return Directory.GetFiles(reports, "*.docx", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileName(f).Contains(code));
        }
        return null;
    }

    static string ExtractPatientCode(string path)
    {
        var m = Regex.Match(Path.GetFileName(path), @"(00\d{8})");
        return m.Success ? m.Groups[1].Value : null;
    }

    static ApiConsumer GetSarConsumer()
    {
        ApiConsumers.SetConsunmer(token);
        return new ApiConsumer(SAR_BASE, token);
    }

    static ApiConsumer GetMosConsumer()
    {
        return new ApiConsumer(mosBase, token);
    }

    static void PrintHelp()
    {
        Console.WriteLine("\nCú pháp:");
        Console.WriteLine("  HisPt01ApiUploader.exe <MãBN1,MãBN2,...>");
        Console.WriteLine("  HisPt01ApiUploader.exe --batch");
        Console.WriteLine("  HisPt01ApiUploader.exe --folder <path>");
    }
}
