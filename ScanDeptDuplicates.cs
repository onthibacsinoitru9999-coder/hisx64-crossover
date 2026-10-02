using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace DeptDuplicatesScanner
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
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
            string tokenCode = "";
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
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
                            int idx = lines[i].IndexOf("TokenCode|") + 10;
                            if (lines[i].Length >= idx + 64)
                            {
                                tokenCode = lines[i].Substring(idx, 64).Trim();
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                Console.WriteLine("LOI: Khong tim thay TokenCode!");
                return;
            }

            ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
            MyAdapter adapter = new MyAdapter();
            CommonParam param = new CommonParam();

            HisBedRoomViewFilter bf = new HisBedRoomViewFilter { DEPARTMENT_ID = 57 };
            var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
            if (bList == null || bList.Count == 0)
            {
                Console.WriteLine("Khong tim thay buong benh Khoa 57.");
                return;
            }

            var allPatients = new List<V_HIS_TREATMENT_BED_ROOM>();
            foreach (var room in bList.OrderBy(r => r.BED_ROOM_NAME))
            {
                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = room.ID, IS_IN_ROOM = true };
                var patients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
                if (patients != null && patients.Count > 0)
                {
                    allPatients.AddRange(patients);
                }
            }

            Console.WriteLine(string.Format("=== KHOA 57: TIM THAY {0} BENH NHAN TRONG {1} BUONG BENH ===", allPatients.Count, bList.Count));

            int duplicateCount = 0;
            long todayStart = 20260930000000;
            long todayEnd = 20260930235959;

            foreach (var pt in allPatients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
            {
                HisServiceReqViewFilter reqFilter = new HisServiceReqViewFilter();
                reqFilter.TREATMENT_ID = pt.TREATMENT_ID;
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, reqFilter, param);
                if (reqs == null || reqs.Count == 0) continue;

                var todayReqs = reqs.Where(r => r.INTRUCTION_TIME >= todayStart && r.INTRUCTION_TIME <= todayEnd).ToList();
                if (todayReqs.Count == 0) continue;

                bool hasMyOrder = todayReqs.Any(r => r.REQUEST_LOGINNAME == "034727" || r.REQUEST_LOGINNAME == "vmc");
                if (!hasMyOrder) continue;

                var reqIds = todayReqs.Select(r => r.ID).ToList();
                HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                ssFilter.SERVICE_REQ_IDs = reqIds;
                var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param) ?? new List<V_HIS_SERE_SERV>();

                var serviceGroups = ssList.GroupBy(s => s.TDL_SERVICE_NAME != null ? s.TDL_SERVICE_NAME.Trim().ToLower() : "")
                                          .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1).ToList();

                bool hasConflict = false;
                StringBuilder sbConflict = new StringBuilder();

                foreach (var g in serviceGroups)
                {
                    var itemReqIds = g.Select(x => x.SERVICE_REQ_ID ?? 0).Distinct().ToList();
                    var relatedReqs = todayReqs.Where(r => itemReqIds.Contains(r.ID)).ToList();
                    
                    if (relatedReqs.Any(r => r.REQUEST_LOGINNAME == "034727" || r.REQUEST_LOGINNAME == "vmc"))
                    {
                        hasConflict = true;
                        sbConflict.AppendLine(string.Format("   [!] TRUNG DUOC / DICH VU: [{0}] (Ke {1} lan)", g.First().TDL_SERVICE_NAME, g.Count()));
                        foreach (var item in g)
                        {
                            long sReqId = item.SERVICE_REQ_ID ?? 0;
                            var req = todayReqs.FirstOrDefault(r => r.ID == sReqId);
                            string dr = req != null ? req.REQUEST_USERNAME + " (" + req.REQUEST_LOGINNAME + ")" : "N/A";
                            string reqType = req != null ? req.SERVICE_REQ_TYPE_NAME : "N/A";
                            string reqCode = req != null ? req.SERVICE_REQ_CODE : "N/A";
                            string time = req != null ? req.INTRUCTION_TIME.ToString() : "N/A";
                            string stt = req != null ? req.SERVICE_REQ_STT_NAME : "N/A";
                            long sttId = req != null ? req.SERVICE_REQ_STT_ID : 0;
                            string color = sttId == 1 ? "TRANG (Chua thuc hien)" : (sttId == 2 ? "VANG" : "XANH (Da hoan thanh)");

                            sbConflict.AppendLine(string.Format("      -> [{0}] {1} (ID: {2} | Ma YL: {3} | SS_ID: {4}) | BS: {5} | Y lenh luc: {6} | SL: {7} {8}",
                                color, reqType, sReqId, reqCode, item.ID, dr, time, item.AMOUNT, item.SERVICE_UNIT_NAME));
                        }
                    }
                }

                var rationReqs = todayReqs.Where(r => r.SERVICE_REQ_TYPE_ID == 10).ToList();
                var rationIds = new HashSet<long>(rationReqs.Select(r => r.ID));
                if (rationReqs.Count > 1 && rationReqs.Any(r => r.REQUEST_LOGINNAME == "034727" || r.REQUEST_LOGINNAME == "vmc"))
                {
                    var rationSs = ssList.Where(s => s.SERVICE_REQ_ID.HasValue && rationIds.Contains(s.SERVICE_REQ_ID.Value)).ToList();
                    
                    var morningMeals = rationSs.Where(s => s.TDL_SERVICE_NAME != null && (s.TDL_SERVICE_NAME.Contains("06-Sáng") || s.TDL_SERVICE_NAME.Contains("06- Sáng"))).ToList();
                    var noonMeals = rationSs.Where(s => s.TDL_SERVICE_NAME != null && (s.TDL_SERVICE_NAME.Contains("11-Trưa") || s.TDL_SERVICE_NAME.Contains("11- Trưa"))).ToList();
                    var eveMeals = rationSs.Where(s => s.TDL_SERVICE_NAME != null && (s.TDL_SERVICE_NAME.Contains("17-Chiều") || s.TDL_SERVICE_NAME.Contains("17- Chiều"))).ToList();

                    if (morningMeals.Count > 1 || noonMeals.Count > 1 || eveMeals.Count > 1)
                    {
                        hasConflict = true;
                        sbConflict.AppendLine(string.Format("   [!] TRUNG BUA AN TRONG NGAY: Sang({0}), Trua({1}), Chieu({2})", morningMeals.Count, noonMeals.Count, eveMeals.Count));
                        foreach (var meal in rationSs)
                        {
                            long sReqId = meal.SERVICE_REQ_ID ?? 0;
                            var req = todayReqs.FirstOrDefault(r => r.ID == sReqId);
                            string dr = req != null ? req.REQUEST_USERNAME + " (" + req.REQUEST_LOGINNAME + ")" : "N/A";
                            string reqCode = req != null ? req.SERVICE_REQ_CODE : "N/A";
                            long sttId = req != null ? req.SERVICE_REQ_STT_ID : 0;
                            string color = sttId == 1 ? "TRANG" : (sttId == 2 ? "VANG" : "XANH");

                            sbConflict.AppendLine(string.Format("      -> [{0}] {1} (ID: {2} | Ma YL: {3}) | BS: {4}",
                                color, meal.TDL_SERVICE_NAME, sReqId, reqCode, dr));
                        }
                    }
                }

                if (hasConflict)
                {
                    duplicateCount++;
                    Console.WriteLine("\n-------------------------------------------------------------------------------");
                    Console.WriteLine(string.Format("BN: {0} (Ma BN: {1} | Ma DT: {2}) | Buong: {3} - Giuong: {4}",
                        pt.TDL_PATIENT_NAME, pt.TDL_PATIENT_CODE, pt.TREATMENT_CODE, pt.BED_ROOM_NAME, pt.BED_NAME));
                    Console.Write(sbConflict.ToString());
                }
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine(string.Format("TONG KET TOAN KHOA: Phat hien {0} benh nhan co y lenh trung lap lien quan den BS Sam (034727 / vmc) trong ngay 30/09.", duplicateCount));
            Console.WriteLine("===============================================================================");
        }
    }
}
