using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace Query6Patients
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

            RunQuery(args);
        }

        static void RunQuery(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string outFile = "query_6_patients_detail.txt";
            StreamWriter logFile = new StreamWriter(outFile, false, Encoding.UTF8);

            Action<string> Log = delegate(string msg)
            {
                Console.WriteLine(msg);
                logFile.WriteLine(msg);
            };

            try
            {
                CommonParam param = new CommonParam();
                string tokenCode = "";

                // 1. Thử lấy live token từ Logs/LogSystem.txt
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
                                    Log("Đã lấy Live Token từ LogSystem.txt: " + tokenCode.Substring(0, 10) + "...");
                                    break;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(tokenCode))
                {
                    Log("KHÔNG TÌM THẤY LIVE TOKEN TRONG LOG SYSTEM!");
                    return;
                }

                ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode);
                MyAdapter adapter = new MyAdapter();

                string[] targetNames = new string[] {
                    "NGUYỄN ANH QUANG",
                    "PHẠM THỊ HIẾU",
                    "NGUYỄN THỊ HOA",
                    "NGUYỄN THỊ HẰNG",
                    "TRẦN THỊ THU",
                    "TRẦN THỊ CHINH"
                };

                // Lay danh sach buong benh khoa 57
                HisBedRoomViewFilter bedRoomFilter = new HisBedRoomViewFilter();
                bedRoomFilter.DEPARTMENT_ID = 57;
                var bedRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bedRoomFilter, param);
                List<long> bedRoomIds = (bedRooms != null) ? bedRooms.Select(x => x.ID).ToList() : new List<long>();

                // Lay danh sach BN dang nam phong khoa 57
                HisTreatmentBedRoomLViewFilter tbrFilter = new HisTreatmentBedRoomLViewFilter();
                tbrFilter.BED_ROOM_IDs = bedRoomIds;
                tbrFilter.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrFilter, param);

                foreach (var name in targetNames)
                {
                    Log("==========================================================================");
                    Log("TRA CỨU BỆNH NHÂN: " + name);
                    Log("==========================================================================");

                    V_HIS_TREATMENT_BED_ROOM matchInPatient = null;
                    if (inPatients != null)
                    {
                        matchInPatient = inPatients.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && x.TDL_PATIENT_NAME.Trim().ToUpper().Contains(name.Trim().ToUpper()));
                    }

                    long treatmentId = 0;
                    string roomName = "";
                    string bedName = "";

                    if (matchInPatient != null)
                    {
                        treatmentId = matchInPatient.TREATMENT_ID;
                        roomName = matchInPatient.BED_ROOM_NAME;
                        bedName = matchInPatient.BED_NAME;
                    }
                    else
                    {
                        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                        tf.KEY_WORD = name;
                        var searchTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                        if (searchTreats != null && searchTreats.Count > 0)
                        {
                            var latest = searchTreats.OrderByDescending(x => x.IN_TIME).First();
                            treatmentId = latest.ID;
                        }
                    }

                    if (treatmentId == 0)
                    {
                        Log("KHÔNG TÌM THẤY HỒ SƠ CHO: " + name);
                        continue;
                    }

                    // Chi tiet treatment
                    HisTreatmentViewFilter tFilter = new HisTreatmentViewFilter();
                    tFilter.ID = treatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tFilter, param);
                    var t = tList.First();

                    long dob = t.TDL_PATIENT_DOB;
                    long birthYear = dob > 10000000000L ? dob / 10000000000L : (dob > 10000 ? dob / 10000L : dob);
                    long age = birthYear > 1900 ? (2026 - birthYear) : 0;

                    Log(string.Format("Họ tên: {0} | DOB: {1} | Tuổi: {2} | Giới tính: {3} | Mã BN: {4} | Mã BA: {5} | TreatmentID: {6}",
                        t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, age, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.ID));
                    Log(string.Format("Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                    Log(string.Format("Đối tượng: {0} | Thẻ BHYT: {1}", t.TDL_PATIENT_TYPE_ID == 1 ? "BHYT" : "Viện phí", t.TDL_HEIN_CARD_NUMBER));
                    Log(string.Format("Vào viện: {0} | Ra viện: {1} | Buồng: {2} | Giường: {3}",
                        t.IN_TIME, t.OUT_TIME.HasValue ? t.OUT_TIME.Value.ToString() : "Đang điều trị", roomName, bedName));
                    Log(string.Format("Chẩn đoán vào: {0} [{1}] - {2}", t.IN_ICD_NAME, t.IN_ICD_CODE, t.IN_ICD_TEXT));
                    Log(string.Format("Chẩn đoán hiện tại/ra: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));
                    Log(string.Format("Chẩn đoán phụ: {0}", t.ICD_SUB_CODE));

                    // Lấy vị trí buồng giường chi tiết nếu chưa có
                    if (string.IsNullOrEmpty(roomName))
                    {
                        HisTreatmentBedRoomLViewFilter brf = new HisTreatmentBedRoomLViewFilter();
                        brf.TREATMENT_IDs = new List<long> { treatmentId };
                        var brs = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, brf, param);
                        if (brs != null && brs.Count > 0)
                        {
                            var lastBr = brs.OrderByDescending(x => x.ADD_TIME).First();
                            Log(string.Format("Buồng giường gần nhất: {0} - Giường: {1} (Vào: {2})", lastBr.BED_ROOM_NAME, lastBr.BED_NAME, lastBr.ADD_TIME));
                        }
                    }

                    // DHST
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = treatmentId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        Log("\n--- DẤU HIỆU SINH TỒN (DHST) ---");
                        foreach (var d in dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).Take(3))
                        {
                            Log(string.Format("[{0}] Mạch: {1} ck/p | HA: {2}/{3} mmHg | NĐ: {4} °C | SpO2: {5}% | Cân nặng: {6} kg | Chiều cao: {7} cm | Nhịp thở: {8}",
                                d.EXECUTE_TIME ?? d.CREATE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT, d.HEIGHT, d.BREATH_RATE));
                        }
                    }

                    // Trackings (Tờ điều trị / Tiền sử / Bệnh sử / Diễn biến)
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = treatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        Log("\n--- DIỄN BIẾN LÂM SÀNG & Y LỆNH (TRACKINGS) ---");
                        foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
                        {
                            Log(string.Format("──────────────────────────────────────────────────"));
                            Log(string.Format("[{0}] BS: {1} | Phòng: {2} | Chẩn đoán: {3} [{4}]", trk.TRACKING_TIME, trk.CREATOR, trk.ROOM_NAME, trk.ICD_NAME, trk.ICD_CODE));
                            Log("Diễn biến: " + (trk.CONTENT ?? ""));
                            Log("Y lệnh: " + (trk.MEDICAL_INSTRUCTION ?? ""));
                        }
                    }

                    // Xét nghiệm (Tein)
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = treatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);
                    if (teins != null && teins.Count > 0)
                    {
                        Log("\n--- XÉT NGHIỆM CHI TIẾT ---");
                        Func<string, string> getTein = delegate(string codeOrName) {
                            var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                                ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(codeOrName, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(codeOrName, StringComparison.OrdinalIgnoreCase) >= 0)));
                            return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "Chưa có";
                        };

                        Log("• CTM: WBC: " + getTein("WBC") + " | Neut%: " + getTein("NEUT%") + " | HGB: " + getTein("HGB") + " | PLT: " + getTein("PLT") + " | RBC: " + getTein("RBC") + " | HCT: " + getTein("HCT"));
                        Log("• Đông máu: PT%: " + getTein("PT (%)") + " | INR: " + getTein("PT - INR") + " | APTT: " + getTein("APTT (s)") + " | Fib: " + getTein("Fibrinogen"));
                        Log("• Sinh hóa: Glucose: " + getTein("Glucose") + " | Ure: " + getTein("Urê") + " | Creatinin: " + getTein("Creatinin") + " | AST: " + getTein("AST") + " | ALT: " + getTein("ALT") + " | eGFR: " + getTein("eGFR"));
                        Log("• Điện giải: Na: " + getTein("Natri") + " | K: " + getTein("Kali") + " | Cl: " + getTein("Clo") + " | Ca: " + getTein("Calci"));
                        Log("• Nhóm máu: ABO: " + getTein("ABO") + " | Rh: " + getTein("Rh"));
                        Log("• Virus/Miễn dịch: HIV: " + getTein("HIV") + " | HBsAg: " + getTein("HBsAg") + " | HCV: " + getTein("HCV"));
                        Log("• Nước tiểu: Protein: " + getTein("Protein NT") + " | Hồng cầu NT: " + getTein("Hồng cầu NT") + " | Bạch cầu NT: " + getTein("Bạch cầu NT"));
                        Log("• Tim mạch / Viêm / Nội tiết: Troponin: " + getTein("Troponin") + " | CRP: " + getTein("CRP") + " | Procalcitonin: " + getTein("Procalcitonin") + " | HbA1c: " + getTein("HbA1c") + " | Cortisol: " + getTein("Cortisol"));
                    }

                    // SereServ (Dịch vụ CLS, CĐHA, TDCN, PTTT)
                    HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                    ssFilter.TREATMENT_ID = treatmentId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                    if (sss != null && sss.Count > 0)
                    {
                        Log("\n--- CẬN LÂM SÀNG, CHẨN ĐOÁN HÌNH ẢNH & THĂM DÒ CHỨC NĂNG ---");
                        var clsList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8 || x.TDL_SERVICE_TYPE_ID == 11).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                        foreach (var s in clsList)
                        {
                            Log(string.Format("  [{0}] {1,-50} | Loại: {2,-12} | Phòng: {3}",
                                s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME));
                        }
                    }

                    Log("\n\n");
                }
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
}
