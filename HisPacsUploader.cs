using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace HisPacsUploader
{
    public class StudyDownloadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string StudyInstanceUid { get; set; }
        public string StudyDescription { get; set; }
        public string StudyDate { get; set; }
        public string PatientName { get; set; }
        public string PatientCode { get; set; }
        public string Modality { get; set; }
        public string PacsAE { get; set; }
        public List<string> DicomFiles { get; set; }

        public StudyDownloadResult()
        {
            DicomFiles = new List<string>();
        }
    }

    public class DriveUploadResult
    {
        public bool Success { get; set; }
        public bool IsCloud { get; set; }
        public string ErrorMessage { get; set; }
        public string ShareableUrl { get; set; }
        public DateTime ExpirationTime { get; set; }
    }

    public static class PacsClient
    {
        private const string RisBaseUrl = "http://192.168.200.110/ris";
        private const string PacsBaseUrlCs2 = "http://192.168.200.107:8080/pacs";
        private const string PacsBaseUrlHn = "http://192.168.200.111:8080/pacs";
        private static readonly string DefaultAccount = Environment.GetEnvironmentVariable("PACS_ACCOUNT") ?? "ctch";
        private static readonly string DefaultPassword = Environment.GetEnvironmentVariable("PACS_PASSWORD") ?? "ctchCS2026!";

        public static string GetPacsBaseUrl(string pacsAe)
        {
            if (!string.IsNullOrEmpty(pacsAe) && pacsAe.IndexOf("CS2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PacsBaseUrlCs2;
            }
            return PacsBaseUrlHn;
        }

        public static string GetFallbackPacsBaseUrl(string currentBaseUrl)
        {
            if (!string.IsNullOrEmpty(currentBaseUrl) && currentBaseUrl.IndexOf("192.168.200.107", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PacsBaseUrlHn;
            }
            return PacsBaseUrlCs2;
        }

        public static string NormalizePatientId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            string s = raw.Trim();
            if (s.StartsWith("VS.", StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
            long num;
            if (long.TryParse(s, out num))
            {
                return "VS." + s.PadLeft(10, '0');
            }
            return "VS." + s;
        }

        public static string MergeCookies(string currentCookie, string setCookieHeader)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(currentCookie))
            {
                var parts = currentCookie.Split(';');
                foreach (var p in parts)
                {
                    var kv = p.Trim().Split(new char[] { '=' }, 2);
                    if (kv.Length == 2) map[kv[0].Trim()] = kv[1].Trim();
                }
            }
            if (!string.IsNullOrEmpty(setCookieHeader))
            {
                var cookieStrings = setCookieHeader.Split(new string[] { ",", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cs in cookieStrings)
                {
                    var firstPart = cs.Split(';')[0].Trim();
                    var kv = firstPart.Split(new char[] { '=' }, 2);
                    if (kv.Length == 2 && !string.IsNullOrEmpty(kv[0]))
                    {
                        string k = kv[0].Trim();
                        if (!k.Equals("path", StringComparison.OrdinalIgnoreCase) &&
                            !k.Equals("domain", StringComparison.OrdinalIgnoreCase) &&
                            !k.Equals("expires", StringComparison.OrdinalIgnoreCase) &&
                            !k.Equals("httponly", StringComparison.OrdinalIgnoreCase) &&
                            !k.Equals("samesite", StringComparison.OrdinalIgnoreCase))
                        {
                            map[k] = kv[1].Trim();
                        }
                    }
                }
            }
            var list = new List<string>();
            foreach (var kvp in map)
            {
                list.Add(string.Format("{0}={1}", kvp.Key, kvp.Value));
            }
            return string.Join("; ", list.ToArray());
        }

        public static StudyDownloadResult DownloadStudy(string rawPatientId, string tempDir, string requestedModality = null)
        {
            ServicePointManager.Expect100Continue = false;

            var result = new StudyDownloadResult();
            string cleanPid = NormalizePatientId(rawPatientId);
            string browserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            result.PatientCode = cleanPid.StartsWith("VS.") ? cleanPid.Substring(3) : cleanPid;

            try
            {
                // 1. Get validKey from login page
                Console.Error.WriteLine(string.Format("[PacsClient] Ket noi toi RIS Minerva ({0})...", RisBaseUrl));
                var getReq = (HttpWebRequest)WebRequest.Create(RisBaseUrl + "/account/login");
                getReq.Timeout = 15000;
                getReq.Proxy = null;
                getReq.UserAgent = browserAgent;

                string loginHtml = string.Empty;
                using (var getResp = (HttpWebResponse)getReq.GetResponse())
                {
                    using (var reader = new StreamReader(getResp.GetResponseStream(), Encoding.UTF8))
                    {
                        loginHtml = reader.ReadToEnd();
                    }
                }

                var match = Regex.Match(loginHtml, @"'validKey':\s*""([^""]+)""");
                if (!match.Success)
                {
                    match = Regex.Match(loginHtml, @"""validKey""\s*:\s*""([^""]+)""");
                }
                if (!match.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = "Khong the lay validKey tu trang dang nhap RIS Minerva.";
                    return result;
                }
                string validKey = match.Groups[1].Value;

                // 2. Login to RIS (POST /account/login with AllowAutoRedirect = false to capture 302 session cookie)
                var postReq = (HttpWebRequest)WebRequest.Create(RisBaseUrl + "/account/login");
                postReq.Method = "POST";
                postReq.Timeout = 15000;
                postReq.Proxy = null;
                postReq.AllowAutoRedirect = false;
                postReq.ContentType = "application/x-www-form-urlencoded";
                postReq.UserAgent = browserAgent;

                string postBody = string.Format("account={0}&password={1}&isLocal=true&validKey={2}",
                    Uri.EscapeDataString(DefaultAccount),
                    Uri.EscapeDataString(DefaultPassword).Replace("!", "%21"),
                    Uri.EscapeDataString(validKey));
                byte[] bodyBytes = Encoding.UTF8.GetBytes(postBody);
                postReq.ContentLength = bodyBytes.Length;

                using (var reqStream = postReq.GetRequestStream())
                {
                    reqStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                var cookieList = new List<string>();
                try
                {
                    using (var postResp = (HttpWebResponse)postReq.GetResponse())
                    {
                        var scValues = postResp.Headers.GetValues("Set-Cookie");
                        if (scValues != null)
                        {
                            foreach (var sc in scValues)
                            {
                                string part = sc.Split(';')[0].Trim();
                                string k = part.Split('=')[0].Trim();
                                cookieList.RemoveAll(delegate(string x) { return x.StartsWith(k + "=", StringComparison.OrdinalIgnoreCase); });
                                cookieList.Add(part);
                            }
                        }
                    }
                }
                catch (WebException wex)
                {
                    if (wex.Response is HttpWebResponse)
                    {
                        var postResp = (HttpWebResponse)wex.Response;
                        var scValues = postResp.Headers.GetValues("Set-Cookie");
                        if (scValues != null)
                        {
                            foreach (var sc in scValues)
                            {
                                string part = sc.Split(';')[0].Trim();
                                string k = part.Split('=')[0].Trim();
                                cookieList.RemoveAll(delegate(string x) { return x.StartsWith(k + "=", StringComparison.OrdinalIgnoreCase); });
                                cookieList.Add(part);
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                string cookieHeader = string.Join("; ", cookieList.ToArray());

                // 3. Query studies for patient (last 60 days)
                string dateFrom = DateTime.Now.AddDays(-60).ToString("yyyy-M-d");
                string dateTo = DateTime.Now.AddDays(1).ToString("yyyy-M-d");
                string queryUrl = string.Format("{0}/rest/study?status=all&pid={1}&dateFrom={2}&dateTo={3}",
                    RisBaseUrl,
                    Uri.EscapeDataString(cleanPid),
                    dateFrom,
                    dateTo);

                Console.Error.WriteLine(string.Format("[PacsClient] Truy van ca chup cho {0} tu {1} den {2}...", cleanPid, dateFrom, dateTo));
                var queryReq = (HttpWebRequest)WebRequest.Create(queryUrl);
                queryReq.Timeout = 15000;
                queryReq.Proxy = null;
                queryReq.UserAgent = browserAgent;
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    queryReq.Headers["Cookie"] = cookieHeader;
                }

                string queryJson = string.Empty;
                using (var queryResp = (HttpWebResponse)queryReq.GetResponse())
                using (var reader = new StreamReader(queryResp.GetResponseStream(), Encoding.UTF8))
                {
                    queryJson = reader.ReadToEnd();
                }

                var root = JObject.Parse(queryJson);
                var resultsArr = root["results"] as JArray;
                if (resultsArr == null || resultsArr.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = string.Format("Khong tim thay ca chup nao tren RIS/PACS cho benh nhan {0} ({1}).", rawPatientId, cleanPid);
                    return result;
                }

                Console.Error.WriteLine(string.Format("[PacsClient] Tim thay {0} ca chup tren RIS Minerva.", resultsArr.Count));

                // Choose study (filter by modality if requested, otherwise latest date)
                JToken studyObj = null;
                if (!string.IsNullOrEmpty(requestedModality))
                {
                    foreach (var s in resultsArr)
                    {
                        string mName = (string)s["modalityName"] ?? "";
                        string dVal = "";
                        if (s["diagnosis"] != null) dVal = s["diagnosis"].ToString();
                        if (mName.IndexOf(requestedModality, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            dVal.IndexOf(requestedModality, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            studyObj = s;
                            Console.Error.WriteLine(string.Format("[PacsClient] Khop ca chup theo yeu cau modality '{0}': {1}", requestedModality, mName));
                            break;
                        }
                    }
                }

                if (studyObj == null)
                {
                    var sorted = new List<JToken>();
                    foreach (var s in resultsArr) sorted.Add(s);
                    sorted.Sort(delegate(JToken a, JToken b)
                    {
                        string da = (string)a["date"] ?? "";
                        string db = (string)b["date"] ?? "";
                        return string.Compare(db, da, StringComparison.Ordinal);
                    });
                    studyObj = sorted[0];
                }

                result.StudyInstanceUid = (string)studyObj["studyIUID"];
                result.StudyDate = (string)studyObj["date"];
                result.Modality = (string)studyObj["modalityName"];
                result.PacsAE = (string)studyObj["pacsAE"];

                if (string.IsNullOrEmpty(result.PacsAE) || result.PacsAE.Equals("MINERVACS2", StringComparison.OrdinalIgnoreCase))
                {
                    result.PacsAE = "CS2";
                }

                var patientObj = studyObj["patient"];
                if (patientObj != null)
                {
                    result.PatientName = (string)patientObj["name"];
                }

                var diagObj = studyObj["diagnosis"];
                if (diagObj != null)
                {
                    if (diagObj is JArray && ((JArray)diagObj).Count > 0)
                    {
                        var firstDiag = diagObj[0];
                        if (firstDiag["service"] != null)
                            result.StudyDescription = (string)firstDiag["service"]["val"];
                    }
                    else if (diagObj is JObject && diagObj["service"] != null)
                    {
                        result.StudyDescription = (string)diagObj["service"]["val"];
                    }
                }
                if (string.IsNullOrEmpty(result.StudyDescription))
                {
                    result.StudyDescription = result.Modality ?? "Imaging Study";
                }

                Console.Error.WriteLine(string.Format("[PacsClient] Chon ca chup: UID={0} | Loai={1} | Ngay={2} | BN={3}",
                    result.StudyInstanceUid, result.Modality, result.StudyDate, result.PatientName));

                // 4. Download study zip stream from PACS storage (dual routing with fallback retry)
                string primaryBaseUrl = GetPacsBaseUrl(result.PacsAE);
                string fallbackBaseUrl = GetFallbackPacsBaseUrl(primaryBaseUrl);
                string[] candidateBaseUrls = new string[] { primaryBaseUrl, fallbackBaseUrl };

                string effectiveAe = result.PacsAE;
                if (!string.IsNullOrEmpty(effectiveAe) && string.Equals(effectiveAe, "MINERVACS2", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveAe = "CS2";
                }

                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string zipPath = Path.Combine(tempDir, string.Format("study_{0}.zip", Guid.NewGuid().ToString("N")));

                bool downloadSuccess = false;
                Exception lastDlEx = null;

                foreach (var baseUrl in candidateBaseUrls)
                {
                    string dlUrl = string.Format("{0}/0/rest/{1}/studies/{2}?contentType=application/zip",
                        baseUrl,
                        effectiveAe,
                        result.StudyInstanceUid);

                    Console.Error.WriteLine(string.Format("[PacsClient] Dang tai goi ZIP DICOM tu {0}...", dlUrl));

                    if (File.Exists(zipPath))
                    {
                        try { File.Delete(zipPath); } catch { }
                    }

                    try
                    {
                        var dlReq = (HttpWebRequest)WebRequest.Create(dlUrl);
                        dlReq.Timeout = 180000; // 3 minutes
                        dlReq.Proxy = null;
                        dlReq.UserAgent = browserAgent;

                        long totalBytes = 0;
                        var sw = Stopwatch.StartNew();
                        using (var dlResp = (HttpWebResponse)dlReq.GetResponse())
                        using (var dlStream = dlResp.GetResponseStream())
                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[65536];
                            int read;
                            while ((read = dlStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, read);
                                totalBytes += read;
                            }
                        }
                        sw.Stop();

                        if (totalBytes > 0)
                        {
                            double mb = totalBytes / (1024.0 * 1024.0);
                            Console.Error.WriteLine(string.Format("[PacsClient] Tai xong {0:F2} MB trong {1:F2}s ({2:F2} MB/s).",
                                mb, sw.Elapsed.TotalSeconds, mb / Math.Max(0.1, sw.Elapsed.TotalSeconds)));
                            downloadSuccess = true;
                            break;
                        }
                    }
                    catch (WebException wex)
                    {
                        lastDlEx = wex;
                        string statusInfo = wex.Message;
                        HttpWebResponse errResp = wex.Response as HttpWebResponse;
                        if (errResp != null)
                        {
                            statusInfo = string.Format("HTTP {0} {1}", (int)errResp.StatusCode, errResp.StatusDescription);
                        }
                        Console.Error.WriteLine(string.Format("[PacsClient] Canh bao: Khong the tai tu {0} ({1}). Thu may chu du phong...", baseUrl, statusInfo));
                    }
                    catch (Exception ex)
                    {
                        lastDlEx = ex;
                        Console.Error.WriteLine(string.Format("[PacsClient] Canh bao: Loi khi tai tu {0}: {1}. Thu may chu du phong...", baseUrl, ex.Message));
                    }
                }

                if (!downloadSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = string.Format("Khong the tai goi ZIP DICOM tu ca 2 may chu PACS. Loi: {0}", lastDlEx != null ? lastDlEx.Message : "Unknown error");
                    return result;
                }

                // 5. Extract DICOM files
                string dicomDir = Path.Combine(tempDir, "dicom");
                if (Directory.Exists(dicomDir)) Directory.Delete(dicomDir, true);
                Directory.CreateDirectory(dicomDir);

                Console.Error.WriteLine("[PacsClient] Giai nen tep tin DICOM...");
                ZipFile.ExtractToDirectory(zipPath, dicomDir);

                try { File.Delete(zipPath); } catch { }

                // Collect and validate .dcm files
                var files = Directory.GetFiles(dicomDir, "*.*", SearchOption.AllDirectories);
                var validDcm = new List<string>();

                foreach (var f in files)
                {
                    if (f.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase))
                    {
                        validDcm.Add(f);
                    }
                    else
                    {
                        // Check if file without .dcm extension is actually DICOM
                        try
                        {
                            var fi = new FileInfo(f);
                            if (fi.Length >= 132)
                            {
                                byte[] header = new byte[132];
                                using (var fs = File.OpenRead(f))
                                {
                                    fs.Read(header, 0, 132);
                                }
                                string magic = Encoding.ASCII.GetString(header, 128, 4);
                                if (magic == "DICM")
                                {
                                    string newPath = f + ".dcm";
                                    File.Move(f, newPath);
                                    validDcm.Add(newPath);
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (validDcm.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Goi ZIP tai ve khong chua tep tin DICOM (.dcm) hop le nao.";
                    return result;
                }

                // Verify DICM magic header on first file
                bool magicOk = false;
                try
                {
                    byte[] hdr = new byte[132];
                    using (var fs = File.OpenRead(validDcm[0]))
                    {
                        if (fs.Read(hdr, 0, 132) >= 132)
                        {
                            string magic = Encoding.ASCII.GetString(hdr, 128, 4);
                            if (magic == "DICM") magicOk = true;
                        }
                    }
                }
                catch { }

                if (!magicOk)
                {
                    Console.Error.WriteLine("[PacsClient] Canh bao: File dau tien khong co header 'DICM' chuan PS 3.10.");
                }
                else
                {
                    Console.Error.WriteLine("[PacsClient] Xac thuc tieu chuan DICOM PS 3.10: Magic 'DICM' HOP LE 100%.");
                }

                result.DicomFiles = validDcm;
                result.Success = true;
                Console.Error.WriteLine(string.Format("[PacsClient] San sang {0} lat cat DICOM.", validDcm.Count));
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Loi ngoai le khi ket noi RIS/PACS: " + ex.Message;
                return result;
            }
        }
    }

    public static class ViewerPackager
    {
        public static void PackageViewer(string folderPath, List<string> dicomFiles, string patientName, string maBn, string studyDate, string modality, string studyUid)
        {
            Console.Error.WriteLine("[ViewerPackager] Dong goi Web Viewer va manifest.json...");

            // 1. Build manifest.json
            var manifest = new JObject();
            manifest["patientName"] = patientName ?? "";
            manifest["patientId"] = maBn ?? "";
            manifest["studyDate"] = studyDate ?? "";
            manifest["modality"] = modality ?? "";
            manifest["studyUid"] = studyUid ?? "";

            var slices = new JArray();
            foreach (var file in dicomFiles)
            {
                string rel = file.Substring(folderPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                slices.Add(rel);
            }
            manifest["slices"] = slices;

            string manifestPath = Path.Combine(folderPath, "manifest.json");
            File.WriteAllText(manifestPath, manifest.ToString(), Encoding.UTF8);

            // 2. Write index.html
            string indexPath = Path.Combine(folderPath, "index.html");
            string htmlContent = GetViewerHtml();
            File.WriteAllText(indexPath, htmlContent, Encoding.UTF8);

            Console.Error.WriteLine(string.Format("[ViewerPackager] Da tao index.html ({0:N0} bytes) va manifest.json ({1} slices).",
                new FileInfo(indexPath).Length, slices.Count));
        }

        private static string GetViewerHtml()
        {
            // 1. Check if ViewerAssets\index.html exists on disk
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cand1 = Path.Combine(baseDir, "ViewerAssets", "index.html");
            if (File.Exists(cand1)) return File.ReadAllText(cand1, Encoding.UTF8);

            string curDir = Directory.GetCurrentDirectory();
            string cand2 = Path.Combine(curDir, "ViewerAssets", "index.html");
            if (File.Exists(cand2)) return File.ReadAllText(cand2, Encoding.UTF8);

            // 2. Check embedded resource
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    using (var s = asm.GetManifestResourceStream(name))
                    using (var r = new StreamReader(s, Encoding.UTF8))
                    {
                        return r.ReadToEnd();
                    }
                }
            }

            // 3. Fallback inline HTML
            return "<!DOCTYPE html><html><head><meta charset='utf-8'><title>PACS Viewer</title></head><body><h2>Bach Mai PACS Viewer</h2><p>Manifest loaded.</p></body></html>";
        }
    }

    public static class SignedLinkService
    {
        public const string SecretKey = "BachMai_Pacs_Uploader_Secret_Salt_2026";

        public static string ComputeHmacSha256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string GenerateSignedDriveUrl(string driveFolderUrl, string folderIdOrName, TimeSpan ttl)
        {
            long expUnix = (long)(DateTime.UtcNow.Add(ttl) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            string ttlStr = ttl.TotalDays >= 1 ? ((int)ttl.TotalDays) + "d" : ((int)ttl.TotalHours) + "h";
            string payload = string.Format("id={0}&exp={1}", folderIdOrName, expUnix);
            string sig = ComputeHmacSha256(payload, SecretKey);

            string sep = driveFolderUrl.Contains("?") ? "&" : "?";
            return string.Format("{0}{1}ttl={2}&exp={3}&sig={4}", driveFolderUrl, sep, ttlStr, expUnix, sig);
        }
    }

    public static class DriveUploader
    {
        public static string LocateRclone()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(baseDir, "rclone.exe")))
                return Path.Combine(baseDir, "rclone.exe");

            string curDir = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(curDir, "rclone.exe")))
                return Path.Combine(curDir, "rclone.exe");

            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string winGetDir = Path.Combine(localApp, @"Microsoft\WinGet\Packages");
            if (Directory.Exists(winGetDir))
            {
                try
                {
                    foreach (var d in Directory.GetDirectories(winGetDir, "Rclone.Rclone*"))
                    {
                        foreach (var sub in Directory.GetDirectories(d))
                        {
                            string cand = Path.Combine(sub, "rclone.exe");
                            if (File.Exists(cand)) return cand;
                        }
                        string direct = Path.Combine(d, "rclone.exe");
                        if (File.Exists(direct)) return direct;
                    }
                }
                catch { }
            }

            string p1 = Path.Combine(localApp, @"Programs\rclone\rclone.exe");
            if (File.Exists(p1)) return p1;

            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string p2 = Path.Combine(pf, @"rclone\rclone.exe");
            if (File.Exists(p2)) return p2;

            return "rclone.exe";
        }

        public static DriveUploadResult UploadAndShare(string folderPath, string maBn, string studyDate, string studyUid, TimeSpan ttl)
        {
            var res = new DriveUploadResult();
            res.ExpirationTime = DateTime.UtcNow.Add(ttl);

            string rcloneExe = LocateRclone();
            Console.Error.WriteLine(string.Format("[DriveUploader] Phat hien cong cu rclone tai: {0}", rcloneExe));

            string cleanDate = (studyDate ?? DateTime.Now.ToString("yyyyMMdd")).Replace("-", "").Replace(" ", "_").Replace(":", "");
            if (cleanDate.Length > 8) cleanDate = cleanDate.Substring(0, 8);
            string remoteFolderName = string.Format("{0}_{1}", maBn, cleanDate);
            string remoteTarget = string.Format("gdrive:PACS/{0}", remoteFolderName);

            Console.Error.WriteLine(string.Format("[DriveUploader] Dong bo len Google Drive (muc tieu: {0})...", remoteTarget));

            bool rcloneCopied = false;
            string rcloneLink = null;
            string errorOutput = string.Empty;

            try
            {
                var psiCopy = new ProcessStartInfo
                {
                    FileName = rcloneExe,
                    Arguments = string.Format("copy \"{0}\" \"{1}\" --quiet", folderPath, remoteTarget),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using (var proc = Process.Start(psiCopy))
                {
                    errorOutput = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    rcloneCopied = (proc.ExitCode == 0);
                }

                if (rcloneCopied)
                {
                    Console.Error.WriteLine("[DriveUploader] Upload thanh cong! Dang tao Public Shareable Link...");
                    var psiLink = new ProcessStartInfo
                    {
                        FileName = rcloneExe,
                        Arguments = string.Format("link \"{0}\"", remoteTarget),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var proc = Process.Start(psiLink))
                    {
                        rcloneLink = proc.StandardOutput.ReadToEnd().Trim();
                        proc.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                errorOutput = ex.Message;
            }

            if (rcloneCopied && !string.IsNullOrEmpty(rcloneLink) && rcloneLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                res.Success = true;
                res.IsCloud = true;
                res.ShareableUrl = SignedLinkService.GenerateSignedDriveUrl(rcloneLink, remoteFolderName, ttl);
                Console.Error.WriteLine(string.Format("[DriveUploader] Da tao Google Drive signed link qua rclone."));
                return res;
            }

            // If rclone failed or gdrive remote is not configured
            string rcloneDetail = string.IsNullOrEmpty(errorOutput) ? "gdrive remote chua khai bao hoac chua cai dat rclone" : errorOutput.Trim();
            Console.Error.WriteLine(string.Format("[DriveUploader] Thong bao: rclone chua ket noi Google Drive ({0}).", rcloneDetail));

            res.Success = false;
            res.IsCloud = false;
            res.ErrorMessage = "Chua cau hinh hoac loi upload Google Drive: " + rcloneDetail;
            return res;
        }
    }

    public static class LocalViewerServer
    {
        public static int ServeAndOpen(string localDir)
        {
            int port = new Random().Next(18500, 19500);
            string prefix = string.Format("http://127.0.0.1:{0}/", port);
            HttpListener listener = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add(prefix);
                    listener.Start();
                    break;
                }
                catch
                {
                    listener = null;
                    port = new Random().Next(18500, 19500);
                    prefix = string.Format("http://127.0.0.1:{0}/", port);
                }
            }

            if (listener == null)
            {
                Console.Error.WriteLine("[LocalViewerServer] Khong the khoi dong HttpListener tren cac cong loopback duoc cap.");
                return 0;
            }

            Console.Error.WriteLine(string.Format("[LocalViewerServer] May chu cuc bo dang hoat dong tai: {0}", prefix));

            int servedCount = 0;
            DateTime lastRequestTime = DateTime.UtcNow;
            DateTime startTime = DateTime.UtcNow;

            var listenThread = new Thread(() =>
            {
                while (listener != null && listener.IsListening)
                {
                    try
                    {
                        var ctx = listener.GetContext();
                        lastRequestTime = DateTime.UtcNow;

                        string reqPath = ctx.Request.Url.AbsolutePath.TrimStart('/');
                        if (string.IsNullOrEmpty(reqPath)) reqPath = "index.html";

                        string filePath = Path.Combine(localDir, reqPath.Replace('/', Path.DirectorySeparatorChar));

                        if (ctx.Request.HttpMethod == "OPTIONS")
                        {
                            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                            ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                            ctx.Response.StatusCode = 200;
                            ctx.Response.Close();
                            continue;
                        }

                        if (File.Exists(filePath))
                        {
                            byte[] bytes = File.ReadAllBytes(filePath);
                            string ct = "application/octet-stream";
                            if (reqPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) ct = "text/html; charset=utf-8";
                            else if (reqPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) ct = "application/json; charset=utf-8";
                            else if (reqPath.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase)) ct = "application/dicom";
                            else if (reqPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) ct = "application/javascript; charset=utf-8";
                            else if (reqPath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) ct = "text/css; charset=utf-8";

                            ctx.Response.ContentType = ct;
                            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                            ctx.Response.ContentLength64 = bytes.Length;
                            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            Interlocked.Increment(ref servedCount);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
                        ctx.Response.Close();
                    }
                    catch
                    {
                        break;
                    }
                }
            });
            listenThread.IsBackground = true;
            listenThread.Start();

            string openUrl = prefix + "index.html";
            Console.Error.WriteLine(string.Format("[BROWSER] Dang mo trinh xem cuc bo: {0}", openUrl));
            Console.WriteLine(openUrl);
            Console.Out.Flush();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = openUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format("[LocalViewerServer] Loi bat trinh duyet: {0}", ex.Message));
            }

            int maxIdleSeconds = 30;
            string idleEnv = Environment.GetEnvironmentVariable("LOCAL_VIEWER_IDLE_TIMEOUT");
            int customIdle;
            if (int.TryParse(idleEnv, out customIdle) && customIdle > 0)
            {
                maxIdleSeconds = customIdle;
            }

            Console.Error.WriteLine("[LocalViewerServer] May chu dang phuc vu anh. Nhan Enter/phim bat ky trong console de dong...");

            while (true)
            {
                Thread.Sleep(500);
                double elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                double idle = (DateTime.UtcNow - lastRequestTime).TotalSeconds;

                if (!Console.IsInputRedirected)
                {
                    try
                    {
                        if (Console.KeyAvailable)
                        {
                            Console.ReadKey(true);
                            Console.Error.WriteLine("[LocalViewerServer] Nguoi dung yeu cau dong may chu.");
                            break;
                        }
                    }
                    catch { }
                }

                if (servedCount > 0 && idle >= maxIdleSeconds)
                {
                    Console.Error.WriteLine(string.Format("[LocalViewerServer] Da phuc vu {0} tap tin va khong co yeu cau moi trong {1}s. Dong may chu de giai phong...", servedCount, maxIdleSeconds));
                    break;
                }

                if (servedCount == 0 && elapsed >= maxIdleSeconds)
                {
                    Console.Error.WriteLine(string.Format("[LocalViewerServer] Khong co ket noi nao sau {0}s. Dong may chu...", maxIdleSeconds));
                    break;
                }

                if (elapsed > 600)
                {
                    Console.Error.WriteLine("[LocalViewerServer] Dat gioi han thoi gian toi da (10 phut). Dong may chu...");
                    break;
                }
            }

            try
            {
                listener.Stop();
                listener.Close();
            }
            catch { }

            return port;
        }
    }

    public static class Logger
    {
        public static void Log(string maBn, string studyUid, string url, string status)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "Logs");
                if (!Directory.Exists(logDir))
                {
                    try { Directory.CreateDirectory(logDir); } catch { }
                }

                string logEntry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] HisPacsUploader | MaBN={1} | StudyUID={2} | URL={3} | Status={4}{5}",
                    DateTime.Now,
                    maBn ?? "N/A",
                    studyUid ?? "N/A",
                    url ?? "N/A",
                    status ?? "N/A",
                    Environment.NewLine);

                byte[] bytes = Encoding.UTF8.GetBytes(logEntry);

                // 1. Try appending to primary LogSystem.txt (with retries)
                string logPath = Path.Combine(logDir, "LogSystem.txt");
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            fs.Write(bytes, 0, bytes.Length);
                            break;
                        }
                    }
                    catch
                    {
                        Thread.Sleep(30);
                    }
                }

                // 2. Always maintain dedicated tool log in Logs\HisPacsUploader.log
                try
                {
                    string dedicatedPath = Path.Combine(logDir, "HisPacsUploader.log");
                    using (var fs = new FileStream(dedicatedPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
                catch { }
            }
            catch { }
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs resolveArgs)
            {
                try
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
                        if (cur == null || cur.Parent == null) break;
                        cur = cur.Parent;
                        string pRoot = Path.Combine(cur.FullName, name);
                        if (File.Exists(pRoot)) return System.Reflection.Assembly.LoadFrom(pRoot);
                        string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                        if (File.Exists(pRef)) return System.Reflection.Assembly.LoadFrom(pRef);
                    }
                }
                catch { }
                return null;
            };

            return Run(args);
        }

        static int Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string maBn = null;
            TimeSpan ttl = TimeSpan.FromHours(24);
            string ttlDesc = "24h";
            bool isOpen = false;
            string requestedModality = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].Trim();
                if (a.Equals("--open", StringComparison.OrdinalIgnoreCase) || a.Equals("-open", StringComparison.OrdinalIgnoreCase) || a.Equals("/open", StringComparison.OrdinalIgnoreCase))
                {
                    isOpen = true;
                }
                else if (a.Equals("--modality", StringComparison.OrdinalIgnoreCase) || a.Equals("-modality", StringComparison.OrdinalIgnoreCase) || a.Equals("-m", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        i++;
                        requestedModality = args[i].Trim();
                    }
                }
                else if (a.Equals("--ttl", StringComparison.OrdinalIgnoreCase) || a.Equals("-ttl", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        i++;
                        string val = args[i].Trim().ToLowerInvariant();
                        if (val.EndsWith("d"))
                        {
                            int days;
                            if (int.TryParse(val.TrimEnd('d'), out days))
                            {
                                ttl = TimeSpan.FromDays(days);
                                ttlDesc = val;
                            }
                        }
                        else if (val.EndsWith("h"))
                        {
                            int hours;
                            if (int.TryParse(val.TrimEnd('h'), out hours))
                            {
                                ttl = TimeSpan.FromHours(hours);
                                ttlDesc = val;
                            }
                        }
                    }
                }
                else if (a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase) || a.Equals("/?", StringComparison.OrdinalIgnoreCase))
                {
                    PrintUsage();
                    return 0;
                }
                else if (a.StartsWith("-") || a.StartsWith("/"))
                {
                    // ignore other flags
                }
                else if (string.IsNullOrEmpty(maBn))
                {
                    maBn = a;
                }
            }

            if (string.IsNullOrEmpty(maBn))
            {
                Console.Error.WriteLine("[ERROR] Thieu tham so MaBN!");
                PrintUsage();
                return 1;
            }

            // Defense against Path Traversal: reject path navigation characters in patient ID
            if (maBn.Contains("/") || maBn.Contains("\\") || maBn.Contains("..") || maBn.Contains(":"))
            {
                Console.Error.WriteLine("[ERROR] Ma benh nhan chua ky tu duong dan khong hop le!");
                return 1;
            }

            string cleanMaBn = Regex.Replace((maBn ?? "").Replace("VS.", "").Trim(), @"[^a-zA-Z0-9_.-]", "_").Trim('.');
            if (string.IsNullOrEmpty(cleanMaBn)) cleanMaBn = "PATIENT";

            Console.Error.WriteLine("================================================================================");
            Console.Error.WriteLine(" BACH MAI PACS UPLOADER & WEB VIEWER CLI (.NET Framework 4.8 x64)");
            Console.Error.WriteLine("================================================================================");
            Console.Error.WriteLine(string.Format(" Ma benh nhan : {0}", maBn));
            if (!string.IsNullOrEmpty(requestedModality))
            {
                Console.Error.WriteLine(string.Format(" Loai ca chup : {0}", requestedModality));
            }
            Console.Error.WriteLine(string.Format(" Thoi han TTL : {0} ({1:F0} gio)", ttlDesc, ttl.TotalHours));
            Console.Error.WriteLine(string.Format(" Tu dong mo  : {0}", isOpen ? "CO (--open)" : "KHONG"));
            Console.Error.WriteLine("--------------------------------------------------------------------------------");

            string tempRoot = Path.Combine(Path.GetTempPath(), "HisPacsUploader");
            string sessionFolder = Path.Combine(tempRoot, string.Format("Pacs_{0}_{1:yyyyMMdd_HHmmss}_{2}_{3}",
                cleanMaBn,
                DateTime.Now,
                Process.GetCurrentProcess().Id,
                Guid.NewGuid().ToString("N").Substring(0, 8)));

            // Defense in depth: Verify sessionFolder is strictly inside tempRoot
            string fullSessionPath = Path.GetFullPath(sessionFolder);
            string fullTempRoot = Path.GetFullPath(tempRoot);
            if (!fullSessionPath.StartsWith(fullTempRoot, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("[SECURITY ERROR] Phat hien hanh vi Path Traversal khong hop le!");
                return 1;
            }

            try
            {
                // M1: Query & Download Study from RIS Minerva / PACS Storage
                var downloadResult = PacsClient.DownloadStudy(maBn, sessionFolder, requestedModality);
                if (!downloadResult.Success)
                {
                    Console.Error.WriteLine(string.Format("[ERROR] {0}", downloadResult.ErrorMessage));
                    Logger.Log(maBn, "N/A", "N/A", "FAILED: " + downloadResult.ErrorMessage);
                    return 1;
                }

                // M2: Package DICOM Viewer & Manifest
                ViewerPackager.PackageViewer(sessionFolder, downloadResult.DicomFiles,
                    downloadResult.PatientName, maBn, downloadResult.StudyDate, downloadResult.Modality, downloadResult.StudyInstanceUid);

                // M3: Upload to Google Drive & Generate Signed Link with TTL
                var uploadResult = DriveUploader.UploadAndShare(sessionFolder, maBn, downloadResult.StudyDate, downloadResult.StudyInstanceUid, ttl);

                string finalUrl = null;

                // Case 1: Upload to Google Drive SUCCEEDED
                if (uploadResult.Success && uploadResult.IsCloud)
                {
                    finalUrl = uploadResult.ShareableUrl;
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, finalUrl, "SUCCESS");

                    Console.Error.WriteLine("--------------------------------------------------------------------------------");
                    Console.Error.WriteLine(string.Format("[SUCCESS] Hoan tat xu ly ca chup BN: {0} ({1})", downloadResult.PatientName, maBn));
                    Console.Error.WriteLine(string.Format("[SUCCESS] Ca chup: {0} | So luong anh: {1} lat cat", downloadResult.Modality, downloadResult.DicomFiles.Count));
                    Console.Error.WriteLine(string.Format("[SUCCESS] Signed Shareable Link (TTL {0}):", ttlDesc));
                    Console.Error.WriteLine("--------------------------------------------------------------------------------");

                    if (isOpen)
                    {
                        Console.Error.WriteLine(string.Format("[BROWSER] Dang mo lien ket Google Drive tren trinh duyet: {0}", finalUrl));
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = finalUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(string.Format("[BROWSER] Khong the mo trinh duyet: {0}", ex.Message));
                        }
                    }

                    // Output signed URL as final line of stdout for script piping
                    Console.WriteLine(finalUrl);
                    return 0;
                }

                // Case 2 & 3: Upload Google Drive FAILED or UNCONFIGURED
                string strict = Environment.GetEnvironmentVariable("STRICT_DRIVE_UPLOAD");
                bool isStrict = !string.IsNullOrEmpty(strict) && string.Equals(strict.Trim(), "1", StringComparison.OrdinalIgnoreCase);

                if (isStrict || !isOpen)
                {
                    Console.Error.WriteLine(string.Format("[ERROR] {0}", uploadResult.ErrorMessage));
                    if (!isOpen)
                    {
                        Console.Error.WriteLine("[GOI Y] Google Drive chua ket noi. De xem anh DICOM cuc bo ngay tren trinh duyet, vui long truyen co --open:");
                        Console.Error.WriteLine(string.Format("       HisPacsUploader.exe {0} --open", maBn));
                    }
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, "N/A", "FAILED: " + uploadResult.ErrorMessage);
                    return 3;
                }

                // Case 4: Upload Drive failed/unconfigured, BUT --open was specified -> Fallback Local Viewer
                Console.Error.WriteLine("[FALLBACK] Google Drive chua duoc cau hinh. Chuyen sang Trinh xem DICOM cuc bo (Local Viewer)...");
                int localPort = LocalViewerServer.ServeAndOpen(sessionFolder);
                if (localPort <= 0)
                {
                    Console.Error.WriteLine("[ERROR] Khong the khoi dong Trinh xem cuc bo.");
                    Logger.Log(maBn, downloadResult.StudyInstanceUid, "N/A", "FAILED: LocalViewerServer failed");
                    return 2;
                }

                finalUrl = string.Format("http://127.0.0.1:{0}/index.html", localPort);
                Logger.Log(maBn, downloadResult.StudyInstanceUid, finalUrl, "LOCAL_VIEWER_SERVED");

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
                Console.Error.WriteLine(string.Format("[SUCCESS] Ca chup BN: {0} ({1}) da san sang tren Trinh xem cuc bo!", downloadResult.PatientName, maBn));
                Console.Error.WriteLine(string.Format("[SUCCESS] Local Viewer URL: {0}", finalUrl));
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format("[FATAL] Loi he thong: {0}", ex.Message));
                Logger.Log(maBn, "N/A", "N/A", "FATAL: " + ex.Message);
                return 2;
            }
            finally
            {
                CleanupTemp(tempRoot, sessionFolder);
            }
        }

        static void CleanupTemp(string tempRoot, string sessionFolder)
        {
            try
            {
                // 1. Xoa thu muc session cua phien hien tai
                if (!string.IsNullOrEmpty(sessionFolder) && Directory.Exists(sessionFolder))
                {
                    try
                    {
                        Directory.Delete(sessionFolder, true);
                        Console.Error.WriteLine("[CLEANUP] Da xoa thu muc tam cuc bo: " + Path.GetFileName(sessionFolder));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(string.Format("[CLEANUP] Canh bao: Chua the xoa sessionFolder ({0}).", ex.Message));
                    }
                }

                // 2. Quet va don dep cac phien cu mo coi (> 1 gio) trong tempRoot
                if (!string.IsNullOrEmpty(tempRoot) && Directory.Exists(tempRoot))
                {
                    try
                    {
                        string[] subDirs = Directory.GetDirectories(tempRoot);
                        DateTime threshold = DateTime.Now.AddHours(-1);

                        foreach (string dir in subDirs)
                        {
                            try
                            {
                                var di = new DirectoryInfo(dir);
                                if (di.LastWriteTime < threshold || di.CreationTime < threshold)
                                {
                                    Directory.Delete(dir, true);
                                    Console.Error.WriteLine(string.Format("[CLEANUP] Da don dep phien cu mo coi (>1h): {0}", di.Name));
                                }
                            }
                            catch { }
                        }

                        string[] looseFiles = Directory.GetFiles(tempRoot);
                        foreach (string file in looseFiles)
                        {
                            try
                            {
                                var fi = new FileInfo(file);
                                if (fi.LastWriteTime < threshold || fi.CreationTime < threshold)
                                {
                                    File.Delete(file);
                                }
                            }
                            catch { }
                        }

                        // 3. Neu tempRoot hoan toan rong, xoa luon tempRoot
                        if (Directory.GetFileSystemEntries(tempRoot).Length == 0)
                        {
                            Directory.Delete(tempRoot, false);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        static void PrintUsage()
        {
            Console.Error.WriteLine("Cu phap:");
            Console.Error.WriteLine("  HisPacsUploader.exe <MaBN> [--ttl 24h|7d] [--open]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Vi du:");
            Console.Error.WriteLine("  HisPacsUploader.exe 0004009330");
            Console.Error.WriteLine("  HisPacsUploader.exe 0004009330 --ttl 7d --open");
            Console.Error.WriteLine("  HisPacsUploader.exe VS.0004009330 --ttl 24h");
        }
    }
}
