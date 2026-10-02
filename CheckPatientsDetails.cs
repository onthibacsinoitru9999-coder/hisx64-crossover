using System;
using System.IO;
using System.Text.RegularExpressions;
using Inventec.Common.WebApiClient;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using System.Collections.Generic;

class Program {
    static void Main() {
        string token = "";
        string session = "";
        string logPath = @"LogSystem.txt";
        if (File.Exists(logPath)) {
            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs)) {
                string text = reader.ReadToEnd();
                var m = Regex.Match(text, @"TokenCode:\s*([a-zA-Z0-9_-]+)");
                if (m.Success) token = m.Groups[1].Value.Trim();
                m = Regex.Match(text, @"SessionCode:\s*([a-zA-Z0-9_-]+)");
                if (m.Success) session = m.Groups[1].Value.Trim();
            }
        }
        var client = new ApiConsumerStore();
        
        string[] pCodes = new string[] { "0004033304", "0004048113", "0003380587", "0003002690", "0003863470", "0001344789", "0004093343", "0004093319" };
        
        foreach (var pCode in pCodes) {
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("PATIENT: " + pCode);
            var tFilter = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode, IS_PAUSE = false };
            var tRes = client.Get<List<V_HIS_TREATMENT>>("api/HisTreatment/GetView", tFilter, token, session);
            if (tRes == null || tRes.Count == 0) {
                Console.WriteLine("No active treatment for " + pCode);
                continue;
            }
            foreach (var t in tRes) {
                Console.WriteLine(string.Format("Treatment: {0} | ID: {1} | Patient: {2} | InTime: {3} | Dept: {4} | BedRoom: {5} - {6}",
                    t.TREATMENT_CODE, t.ID, t.TDL_PATIENT_NAME, t.IN_TIME, t.LAST_DEPARTMENT_ID, t.BED_ROOM_NAME, t.BED_NAME));
                
                // Trackings
                var trFilter = new HisTrackingViewFilter { TREATMENT_ID = t.ID };
                var trackings = client.Get<List<V_HIS_TRACKING>>("api/HisTracking/GetView", trFilter, token, session);
                Console.WriteLine(string.Format("--- Trackings ({0}) ---", trackings != null ? trackings.Count : 0));
                if (trackings != null) {
                    trackings.Sort((a,b) => b.TRACKING_TIME.CompareTo(a.TRACKING_TIME));
                    for (int i = 0; i < Math.Min(2, trackings.Count); i++) {
                        var tr = trackings[i];
                        Console.WriteLine(string.Format("  [{0}] Subclinical/DienBien: {1}\n  Care/YLenh: {2}", tr.TRACKING_TIME, tr.SUBCLINICAL, tr.CARE));
                    }
                }

                // ServiceReqs
                var sFilter = new HisServiceReqViewFilter { TREATMENT_ID = t.ID };
                var sReqs = client.Get<List<V_HIS_SERVICE_REQ>>("api/HisServiceReq/GetView", sFilter, token, session);
                Console.WriteLine(string.Format("--- ServiceReqs ({0}) ---", sReqs != null ? sReqs.Count : 0));
                if (sReqs != null) {
                    sReqs.Sort((a,b) => b.INTRUCTION_TIME.CompareTo(a.INTRUCTION_TIME));
                    foreach (var req in sReqs) {
                        string reqTime = req.INTRUCTION_TIME.ToString();
                        if (reqTime.StartsWith("20260929") || reqTime.StartsWith("20260928") || reqTime.StartsWith("20260930")) {
                            Console.WriteLine(string.Format("  ReqID: {0} | TypeID: {1} ({2}) | Code: {3} | Time: {4} | ExecuteRoom: {5}",
                                req.ID, req.SERVICE_REQ_TYPE_ID, req.SERVICE_REQ_TYPE_NAME, req.SERVICE_REQ_CODE, req.INTRUCTION_TIME, req.EXECUTE_ROOM_NAME));
                            
                            // Check SereServ / Medicine
                            var ssFilter = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
                            var sss = client.Get<List<V_HIS_SERE_SERV>>("api/HisSereServ/GetView", ssFilter, token, session);
                            if (sss != null) {
                                foreach (var ss in sss) {
                                    Console.WriteLine(string.Format("    -> SereServ: {0} | Amount: {1} {2} | Price: {3} | Instruction: {4}",
                                        ss.TDL_SERVICE_NAME, ss.AMOUNT, ss.SERVICE_UNIT_NAME, ss.PRICE, ss.TUTORIAL));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
