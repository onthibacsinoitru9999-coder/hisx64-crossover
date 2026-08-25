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

namespace QueryFullHospitalShift
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

            RunScan();
        }

        static void RunScan()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
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

            ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode);
            MyAdapter adapter = new MyAdapter();

            using (StreamWriter sw = new StreamWriter("shift_report_raw.txt", false, Encoding.UTF8))
            {
                Action<string> Log = delegate(string msg)
                {
                    Console.WriteLine(msg);
                    sw.WriteLine(msg);
                };

                Log("=== 1. LẤY TẤT CẢ BUỒNG BỆNH VÀ BỆNH NHÂN KHOA 57 ===");
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
                List<long> bedRoomIds = (bList != null) ? bList.Select(x => x.ID).ToList() : new List<long>();
                Log("Số buồng bệnh Khoa 57: " + bedRoomIds.Count);

                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                tbrf.BED_ROOM_IDs = bedRoomIds;
                tbrf.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
                Log("Số BN đang nằm viện tại Khoa 57: " + (inPatients != null ? inPatients.Count : 0));

                List<long> allTreatmentIds = inPatients != null ? inPatients.Select(x => x.TREATMENT_ID).Distinct().ToList() : new List<long>();

                Log("\n=== 2. QUÉT BỆNH NHÂN VÀO KHOA 57 TỪ 7H 24/8 ĐẾN 3H 25/8 ===");
                // Quét từng BN đang nằm viện để lấy DepartmentTran và Treatment
                List<long> admitted24_25 = new List<long>();
                foreach (var tId in allTreatmentIds)
                {
                    HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                    dtf.TREATMENT_ID = tId;
                    var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);
                    
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = tId;
                    var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                    var t = treats != null && treats.Count > 0 ? treats[0] : null;

                    if (dts != null && t != null)
                    {
                        // Tìm lần chuyển vào khoa 57
                        foreach (var dt in dts.Where(x => x.DEPARTMENT_ID == 57))
                        {
                            long inTime = dt.DEPARTMENT_IN_TIME ?? dt.CREATE_TIME ?? 0;
                            // Check window 20260824070000 -> 20260825030500
                            if (inTime >= 20260824070000 && inTime <= 20260825030500)
                            {
                                admitted24_25.Add(tId);
                                Log(string.Format("[BN VÀO KHOA] BN: {0,-25} | Tuổi: {1} | Giới: {2} | Mã BN: {3} | Mã BA: {4} | InTime: {5} | Từ khoa: {6} | BS nhận/tạo: {7}",
                                    t.TDL_PATIENT_NAME, DateTime.Now.Year - (t.TDL_PATIENT_DOB.ToString().Length >= 4 ? int.Parse(t.TDL_PATIENT_DOB.ToString().Substring(0,4)) : 0),
                                    t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, inTime, dt.PREVIOUS_DEPARTMENT_NAME, dt.CREATOR));
                            }
                        }
                    }
                }

                Log("\n=== 3. QUÉT BỆNH NHÂN SAU MỔ VỀ KHOA TỪ GMHS (7H 24/8 -> 3H 25/8) ===");
                // Lọc bệnh nhân có chuyển khoa từ GMHS về 57 hoặc có phẫu thuật vào 24/8
                List<long> postOp24_25 = new List<long>();
                foreach (var tId in allTreatmentIds)
                {
                    HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                    dtf.TREATMENT_ID = tId;
                    var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);
                    
                    // Check service req phẫu thuật ngày 24/8
                    HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                    srf.TREATMENT_ID = tId;
                    var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                    var ptReqs = (srs != null) ? srs.Where(x => x.SERVICE_REQ_TYPE_ID == 6 && x.INTRUCTION_TIME >= 20260824000000 && x.INTRUCTION_TIME <= 20260825030500).ToList() : null;

                    // Check trackings có ghi nhận sau mổ về khoa
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                    var postOpTrks = (trks != null) ? trks.Where(x => x.TRACKING_TIME >= 20260824070000 && x.TRACKING_TIME <= 20260825030500 &&
                        (x.CONTENT != null && (x.CONTENT.ToUpper().Contains("HẬU PHẪU") || x.CONTENT.ToUpper().Contains("VỀ KHOA") || x.CONTENT.ToUpper().Contains("BÀN GIAO") || x.CONTENT.ToUpper().Contains("THOÁT MÊ")))).ToList() : null;

                    if ((ptReqs != null && ptReqs.Count > 0) || (postOpTrks != null && postOpTrks.Count > 0))
                    {
                        postOp24_25.Add(tId);
                        var p = inPatients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                        Log(string.Format("[BN HẬU PHẪU 24-25/8] BN: {0,-25} | Mã BN: {1} | Mã BA: {2} | TreatmentID: {3}",
                            p != null ? p.TDL_PATIENT_NAME : tId.ToString(), p != null ? p.TDL_PATIENT_CODE : "", p != null ? p.TREATMENT_CODE : "", tId));
                        if (ptReqs != null)
                        {
                            foreach (var pt in ptReqs)
                            {
                                Log(string.Format("   • Y lệnh PT: [{0}] {1} | Phòng: {2} | Trạng thái: {3} | Chẩn đoán: {4}",
                                    pt.INTRUCTION_TIME, pt.SERVICE_REQ_CODE, pt.EXECUTE_ROOM_NAME, pt.SERVICE_REQ_STT_NAME, pt.ICD_TEXT ?? pt.ICD_NAME));
                            }
                        }
                        if (postOpTrks != null)
                        {
                            foreach (var trk in postOpTrks)
                            {
                                Log(string.Format("   • Tờ điều trị: [{0}] BS: {1} | Diễn biến: {2}",
                                    trk.TRACKING_TIME, trk.CREATOR, trk.CONTENT.Replace("\r\n", " | ").Replace("\n", " | ")));
                            }
                        }
                    }
                }

                Log("\n=== 4. QUÉT BỆNH NHÂN TRUYỀN MÁU HÔM QUA (24/8/2026) ===");
                // Lấy tất cả dịch vụ máu / chế phẩm máu hoặc xuất máu của các BN khoa 57
                foreach (var tId in allTreatmentIds)
                {
                    HisSereServViewFilter ssf = new HisSereServViewFilter();
                    ssf.TREATMENT_ID = tId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        var bloods = sss.Where(x => (x.TDL_SERVICE_TYPE_ID == 5 || (x.SERVICE_TYPE_NAME != null && x.SERVICE_TYPE_NAME.ToUpper().Contains("MÁU")) ||
                                                    (x.TDL_SERVICE_NAME != null && (x.TDL_SERVICE_NAME.ToUpper().Contains("HỒNG CẦU") || x.TDL_SERVICE_NAME.ToUpper().Contains("HUYẾT TƯƠNG") || x.TDL_SERVICE_NAME.ToUpper().Contains("TIỂU CẦU")))) &&
                                                    x.TDL_INTRUCTION_TIME >= 20260824000000 && x.TDL_INTRUCTION_TIME <= 20260825030500).ToList();
                        if (bloods.Count > 0)
                        {
                            var p = inPatients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                            Log(string.Format("[BN TRUYỀN MÁU 24/8] BN: {0,-25} | Mã BN: {1} | Mã BA: {2}",
                                p != null ? p.TDL_PATIENT_NAME : tId.ToString(), p != null ? p.TDL_PATIENT_CODE : "", p != null ? p.TREATMENT_CODE : ""));
                            foreach (var b in bloods)
                            {
                                Log(string.Format("   • Máu/Chế phẩm: [{0}] {1} | SL: {2} {3}",
                                    b.TDL_INTRUCTION_TIME, b.TDL_SERVICE_NAME, b.AMOUNT, b.SERVICE_UNIT_NAME));
                            }
                        }
                    }

                    // Check y lệnh trong tracking
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter();
                    trkf.TREATMENT_ID = tId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                    if (trks != null)
                    {
                        var bloodTrks = trks.Where(x => x.TRACKING_TIME >= 20260824000000 && x.TRACKING_TIME <= 20260825030500 &&
                            ((x.MEDICAL_INSTRUCTION != null && (x.MEDICAL_INSTRUCTION.ToUpper().Contains("TRUYỀN MÁU") || x.MEDICAL_INSTRUCTION.ToUpper().Contains("KHC") || x.MEDICAL_INSTRUCTION.ToUpper().Contains("HỒNG CẦU"))) ||
                             (x.CONTENT != null && (x.CONTENT.ToUpper().Contains("TRUYỀN MÁU") || x.CONTENT.ToUpper().Contains("KHC") || x.CONTENT.ToUpper().Contains("HỒNG CẦU"))))).ToList();
                        if (bloodTrks.Count > 0)
                        {
                            var p = inPatients.FirstOrDefault(x => x.TREATMENT_ID == tId);
                            Log(string.Format("[TRACKING TRUYỀN MÁU 24/8] BN: {0,-25} | Mã BN: {1}",
                                p != null ? p.TDL_PATIENT_NAME : tId.ToString(), p != null ? p.TDL_PATIENT_CODE : ""));
                            foreach (var bt in bloodTrks)
                            {
                                Log(string.Format("   • Y lệnh TĐ: [{0}] Y lệnh: {1} | Diễn biến: {2}",
                                    bt.TRACKING_TIME, bt.MEDICAL_INSTRUCTION, bt.CONTENT));
                            }
                        }
                    }
                }
            }
        }
    }
}
