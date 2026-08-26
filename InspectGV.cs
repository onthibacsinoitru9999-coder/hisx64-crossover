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
        StreamWriter logFile = new StreamWriter("report_712_714_final.txt", false, Encoding.UTF8);

        Action<string> Log = delegate(string msg)
        {
            Console.WriteLine(msg);
            logFile.WriteLine(msg);
        };

        try
        {
            Load.Init();
            CommonParam param = new CommonParam();
            string tokenCode = "";

            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs\LogSystem.txt");
                if (File.Exists(logPath))
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string text = sr.ReadToEnd();
                        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (lines[i].Contains("TokenCode|"))
                            {
                                int tIdx = lines[i].IndexOf("TokenCode|") + 10;
                                if (lines[i].Length >= tIdx + 64)
                                {
                                    tokenCode = lines[i].Substring(tIdx, 64);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(tokenCode))
            {
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                if (token == null) token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (token != null) tokenCode = token.TokenCode;
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                Log("LOGIN FAILED!");
                return;
            }

            var mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", tokenCode, "MOS");
            MyAdapter adapter = new MyAdapter();

            // 1. Lấy danh sách buồng
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
            bf.DEPARTMENT_ID = 57;
            var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);

            var targetBedRooms = bList.Where(x => x.BED_ROOM_NAME.Contains("712") || x.BED_ROOM_NAME.Contains("714") || x.BED_ROOM_NAME.Contains("724") || x.BED_ROOM_NAME.Contains("725")).OrderBy(x => x.BED_ROOM_NAME).ToList();

            Log("====================================================================================================");
            Log("BÁO CÁO BỆNH NHÂN THEO MẪU BẢNG LÂM SÀNG - KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Log("THỜI ĐIỂM: 26/08/2026");
            Log("====================================================================================================\n");

            StringBuilder sbTable = new StringBuilder();
            sbTable.AppendLine("| Buồng - Giường | Họ và tên | Tuổi | Giới | Bệnh sử | Chẩn đoán & Tình trạng phẫu thuật | Xét nghiệm quan trọng có giá trị chẩn đoán | Hội chẩn hoặc Xét nghiệm mới có |");
            sbTable.AppendLine("| :--- | :--- | :---: | :---: | :--- | :--- | :--- | :--- |");

            List<string> detailedBlocks = new List<string>();

            foreach (var br in targetBedRooms)
            {
                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                tbrf.BED_ROOM_ID = br.ID;
                tbrf.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
                if (inPatients == null || inPatients.Count == 0) continue;

                foreach (var p in inPatients.OrderBy(x => x.BED_NAME))
                {
                    long treatmentId = p.TREATMENT_ID;

                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = treatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                    var t = tList != null ? tList.FirstOrDefault() : null;

                    string roomBed = string.Format("{0}<br>{1}", br.BED_ROOM_NAME.Replace("Phòng ", "P.").Replace("số ", ""), p.BED_NAME.Replace("Giường số ", "G.").Replace("giường ", "G."));
                    string fullNameWithCode = string.Format("**{0}**<br>(BN: {1})", p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE);
                    
                    int age = 0;
                    if (t != null && t.TDL_PATIENT_DOB > 0)
                    {
                        string dobStr = t.TDL_PATIENT_DOB.ToString();
                        if (dobStr.Length >= 4) age = 2026 - int.Parse(dobStr.Substring(0, 4));
                    }
                    string gender = t != null ? t.TDL_PATIENT_GENDER_NAME : "";

                    // Trackings
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = treatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);

                    // History (Bệnh sử)
                    string history = "";
                    if (trks != null && trks.Count > 0)
                    {
                        var firstTrk = trks.OrderBy(x => x.TRACKING_TIME).First();
                        if (firstTrk.CONTENT != null)
                        {
                            string c = firstTrk.CONTENT.Replace("\r\n", " ").Replace("\n", " ");
                            // Try extracting Bệnh sử or take first few sentences
                            int bsIdx = c.IndexOf("Bệnh sử");
                            if (bsIdx >= 0)
                            {
                                int endIdx = c.IndexOf("Khám", bsIdx);
                                if (endIdx > bsIdx) history = c.Substring(bsIdx, endIdx - bsIdx).Trim();
                                else history = c.Substring(bsIdx, Math.Min(250, c.Length - bsIdx)).Trim();
                            }
                            else
                            {
                                history = c.Length > 200 ? c.Substring(0, 200) + "..." : c;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(history) && t != null)
                    {
                        history = string.Format("Vào viện lúc {0}. Lý do: {1}", t.IN_TIME, t.IN_ICD_NAME);
                    }

                    // Surgeries
                    HisSereServViewFilter ssf = new HisSereServViewFilter();
                    ssf.TREATMENT_ID = treatmentId;
                    var allSereServs = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);

                    var surgeries = allSereServs != null ? allSereServs.Where(x => x.TDL_SERVICE_TYPE_ID == 4 || (x.SERVICE_TYPE_NAME != null && x.SERVICE_TYPE_NAME.ToLower().Contains("phẫu thuật"))).OrderByDescending(x => x.TDL_INTRUCTION_TIME).ToList() : new List<V_HIS_SERE_SERV>();

                    string diagAndSurg = "";
                    string baseDiag = string.Format("{0} [{1}]", t != null ? t.ICD_NAME : "", t != null ? t.ICD_CODE : "");
                    if (t != null && !string.IsNullOrEmpty(t.ICD_TEXT)) baseDiag += " - " + t.ICD_TEXT;

                    if (surgeries.Count > 0)
                    {
                        var s = surgeries[0];
                        string sTimeStr = s.TDL_INTRUCTION_TIME.ToString();
                        int postOpDays = 0;
                        string sDateStr = "";
                        if (sTimeStr.Length >= 8)
                        {
                            int yr = int.Parse(sTimeStr.Substring(0, 4));
                            int mo = int.Parse(sTimeStr.Substring(4, 2));
                            int dy = int.Parse(sTimeStr.Substring(6, 2));
                            postOpDays = (new DateTime(2026, 8, 26) - new DateTime(yr, mo, dy)).Days;
                            sDateStr = string.Format("{0:D2}/{1:D2}", dy, mo);
                        }
                        string statusTag = postOpDays == 0 ? "🟢 Đã mổ hôm nay (26/08)" : string.Format("🟢 Đã mổ {0} (Hậu phẫu N{1})", sDateStr, postOpDays);
                        diagAndSurg = string.Format("**{0}**<br>({1}: {2})", baseDiag, statusTag, s.TDL_SERVICE_NAME);
                    }
                    else
                    {
                        diagAndSurg = string.Format("**{0}**<br>(⚪ Chưa mổ / Điều trị nội khoa / Đang chuẩn bị mổ)", baseDiag);
                    }

                    // Paraclinical / Tein tests
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = treatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);

                    Func<string, string> getTein = delegate(string nameOrCode) {
                        if (teins == null) return "";
                        var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                            ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(nameOrCode, StringComparison.OrdinalIgnoreCase)) ||
                             (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(nameOrCode, StringComparison.OrdinalIgnoreCase) >= 0)));
                        return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "";
                    };

                    List<string> importantTests = new List<string>();

                    // CĐHA
                    if (allSereServs != null)
                    {
                        var imgs = allSereServs.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 8).OrderByDescending(x => x.TDL_INTRUCTION_TIME).Take(3).ToList();
                        foreach (var img in imgs)
                        {
                            importantTests.Add(string.Format("• {0}: {1}", img.SERVICE_TYPE_NAME, img.TDL_SERVICE_NAME));
                        }
                    }

                    // CTM
                    string wbc = getTein("WBC");
                    string hgb = getTein("HGB");
                    string plt = getTein("PLT");
                    if (!string.IsNullOrEmpty(wbc) || !string.IsNullOrEmpty(hgb))
                    {
                        importantTests.Add(string.Format("• CTM: WBC {0}, HGB {1}, PLT {2}", wbc, hgb, plt).Trim());
                    }

                    // Đông máu
                    string pt = getTein("PT (%)");
                    string inr = getTein("PT - INR");
                    string aptt = getTein("APTT (s)");
                    string fib = getTein("Fibrinogen");
                    if (!string.IsNullOrEmpty(pt) || !string.IsNullOrEmpty(aptt) || !string.IsNullOrEmpty(fib))
                    {
                        importantTests.Add(string.Format("• Đông máu: PT% {0}, INR {1}, APTT {2}, Fib {3}", pt, inr, aptt, fib).Trim());
                    }

                    // Sinh hóa
                    string glu = getTein("Glucose");
                    string ure = getTein("Urê");
                    string cre = getTein("Creatinin");
                    string ast = getTein("AST");
                    string alt = getTein("ALT");
                    if (!string.IsNullOrEmpty(glu) || !string.IsNullOrEmpty(cre))
                    {
                        importantTests.Add(string.Format("• Sinh hóa: Glu {0}, Ure {1}, Cre {2}, AST/ALT {3}/{4}", glu, ure, cre, ast, alt).Trim());
                    }

                    string importantTestsStr = string.Join("<br>", importantTests);
                    if (string.IsNullOrEmpty(importantTestsStr)) importantTestsStr = "Chưa có kết quả XN đầy đủ";

                    // New tests or Consultation today
                    List<string> todayUpdates = new List<string>();

                    if (trks != null && trks.Count > 0)
                    {
                        var lastTrk = trks.OrderByDescending(x => x.TRACKING_TIME).First();
                        if (lastTrk.CONTENT != null)
                        {
                            string lastContent = lastTrk.CONTENT.Trim().Replace("\r\n", " ").Replace("\n", " ");
                            todayUpdates.Add(string.Format("• **Tình trạng**: {0}", lastContent.Length > 150 ? lastContent.Substring(0, 150) + "..." : lastContent));
                        }
                    }

                    // Today tests
                    var todayTeins = teins != null ? teins.Where(x => !string.IsNullOrEmpty(x.VALUE) &&
                        ((x.MODIFY_TIME.HasValue && x.MODIFY_TIME.Value.ToString().StartsWith("20260826")) ||
                         (x.CREATE_TIME.HasValue && x.CREATE_TIME.Value.ToString().StartsWith("20260826")))).ToList() : new List<V_HIS_SERE_SERV_TEIN>();

                    if (todayTeins.Count > 0)
                    {
                        List<string> todayXn = new List<string>();
                        foreach (var it in todayTeins.OrderBy(x => x.TEST_INDEX_NAME).Take(6))
                        {
                            todayXn.Add(string.Format("{0}: {1} {2}", it.TEST_INDEX_NAME, it.VALUE, it.TEST_INDEX_UNIT_NAME));
                        }
                        todayUpdates.Add(string.Format("• **XN mới (26/08)**: {0}{1}", string.Join("; ", todayXn), todayTeins.Count > 6 ? string.Format(" (+{0} chỉ số)", todayTeins.Count - 6) : ""));
                    }

                    string todayUpdatesStr = string.Join("<br>", todayUpdates);
                    if (string.IsNullOrEmpty(todayUpdatesStr)) todayUpdatesStr = "Chưa có diễn biến / XN mới hôm nay";

                    // Clean for markdown table cells
                    string historyCell = history.Replace("|", "/").Replace("\r\n", " ").Replace("\n", " ");
                    string diagCell = diagAndSurg.Replace("|", "/");
                    string testCell = importantTestsStr.Replace("|", "/");
                    string updateCell = todayUpdatesStr.Replace("|", "/");

                    sbTable.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} |",
                        roomBed, fullNameWithCode, age, gender, historyCell, diagCell, testCell, updateCell));

                    // Add detailed log
                    Log(string.Format("• [{0}] {1} ({2}T, {3}):", roomBed.Replace("<br>", " "), fullNameWithCode.Replace("**", "").Replace("<br>", " "), age, gender));
                    Log(string.Format("  - Bệnh sử: {0}", historyCell));
                    Log(string.Format("  - Chẩn đoán & PT: {0}", diagCell.Replace("<br>", " ")));
                    Log(string.Format("  - Xét nghiệm: {0}", testCell.Replace("<br>", " | ")));
                    Log(string.Format("  - Hội chẩn / Mới: {0}", updateCell.Replace("<br>", " | ")));
                    Log("");
                }
            }

            File.WriteAllText("patient_table_report_markdown.md", sbTable.ToString(), Encoding.UTF8);
            Log("\n" + sbTable.ToString());

            Log("====================================================================================================");
            Log("HOÀN TẤT XUẤT BẢNG BÁO CÁO ĐẦY ĐỦ!");
            Log("====================================================================================================");
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex.ToString());
        }
        finally
        {
            logFile.Close();
        }
    }
}
