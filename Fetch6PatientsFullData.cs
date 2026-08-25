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

namespace Fetch6PatientsFullData
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
    }

    public class PatientTarget
    {
        public string TargetName { get; set; }
        public string TargetAge { get; set; }
        public string TargetDiagnosis { get; set; }
        public string TargetSurgeryMethod { get; set; }
        public string TargetSurgeon { get; set; }
        public long TreatmentId { get; set; }
        public string PatientCode { get; set; }
        public string TreatmentCode { get; set; }
        public string BedRoomName { get; set; }
        public string BedName { get; set; }

        public PatientTarget(string name, string age, string diag, string method, string surgeon, long tId, string pCode, string tCode, string room, string bed)
        {
            TargetName = name;
            TargetAge = age;
            TargetDiagnosis = diag;
            TargetSurgeryMethod = method;
            TargetSurgeon = surgeon;
            TreatmentId = tId;
            PatientCode = pCode;
            TreatmentCode = tCode;
            BedRoomName = room;
            BedName = bed;
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
            string outFile = "patients_6_full_clinical_data.txt";
            StreamWriter logFile = new StreamWriter(outFile, false, Encoding.UTF8);

            Action<string> Log = delegate(string msg)
            {
                Console.WriteLine(msg);
                logFile.WriteLine(msg);
            };

            try
            {
                CommonParam param = new CommonParam();
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (token == null)
                {
                    Log("LOGIN FAILED!");
                    return;
                }
                ApiConsumers.SetConsunmer(token.TokenCode);
                MyAdapter adapter = new MyAdapter();

                List<PatientTarget> targets = new List<PatientTarget>
                {
                    new PatientTarget("NGUYỄN ANH QUANG", "45", "Hẹp ống sống L4-5L5S1", "TLIF 2 tầng L4-5 L5-S1", "TS Trung", 7097961, "0003972576", "000007098145", "Phòng 716", "Giường số 01"),
                    new PatientTarget("PHẠM THỊ HIẾU", "52", "Trượt L4-5/Hẹp ống sống, vùng thắt lưng/thoái hoá khớp gối 2 bên", "TLIF L4-5", "TS Trung", 7097937, "0003972296", "000007098121", "Phòng 735", "Giường số 81"),
                    new PatientTarget("NGUYỄN THỊ HOA", "59", "Thoát vị đĩa đệm L3/4, L4/5, L5/S1 / ĐTĐ type 2 - STT do thuốc - THA - TLLPM - Tăng men gan - Loãng xương", "TLIF 3T", "TS Trung", 6976111, "0003916815", "000006976295", "Phòng 711", "Giường số 27"),
                    new PatientTarget("NGUYỄN THỊ HẰNG", "46", "Mất vững C1-C2", "CĐ C1-C2, ghép xương", "TS Trung", 7073508, "0003808904", "000007073692", "Phòng 723", "Giường số 41"),
                    new PatientTarget("TRẦN THỊ THU", "63", "CTCS: vỡ xẹp L1", "BXM", "BS Hoành", 7092152, "0003983144", "000007092336", "Phòng số 712A", "Giường số 19A"),
                    new PatientTarget("TRẦN THỊ CHINH", "76", "Xẹp thân đốt T12", "BXM", "TS Trung", 7089110, "0003968520", "000007089294", "Phòng 733", "Giường số 85")
                };

                int stt = 1;
                foreach (var p in targets)
                {
                    Log("====================================================================================================");
                    Log(string.Format("BỆNH NHÂN {0}: {1} (Tuổi: {2})", stt++, p.TargetName, p.TargetAge));
                    Log(string.Format("• Chẩn đoán theo bảng giao ban: {0}", p.TargetDiagnosis));
                    Log(string.Format("• Phương pháp phẫu thuật: {0} | Phẫu thuật viên: {1}", p.TargetSurgeryMethod, p.TargetSurgeon));
                    Log(string.Format("• Vị trí hiện tại: {0} - {1} | Mã BN: {2} | Mã BA: {3} | TreatmentID: {4}",
                        p.BedRoomName, p.BedName, p.PatientCode, p.TreatmentCode, p.TreatmentId));
                    Log("====================================================================================================");

                    // 1. Treatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = p.TreatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    var t = tList != null && tList.Count > 0 ? tList.First() : null;

                    if (t != null)
                    {
                        Log(string.Format("Họ tên: {0} | DOB: {1} | Giới tính: {2} | Địa chỉ: {3}",
                            t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_ADDRESS));
                        Log(string.Format("Đối tượng: {0} | Thẻ BHYT: {1}", t.TDL_PATIENT_TYPE_ID == 1 ? "BHYT" : "Viện phí", t.TDL_HEIN_CARD_NUMBER));
                        Log(string.Format("Vào viện: {0} | Ra viện: {1} | Trạng thái: {2}",
                            t.IN_TIME, t.OUT_TIME.HasValue ? t.OUT_TIME.Value.ToString() : "Đang điều trị", t.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị"));
                        Log(string.Format("Chẩn đoán vào: {0} [{1}] - {2}", t.IN_ICD_NAME, t.IN_ICD_CODE, t.IN_ICD_TEXT));
                        Log(string.Format("Chẩn đoán hiện tại/ra: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));
                        Log(string.Format("Chẩn đoán phụ: {0}", t.ICD_SUB_CODE));
                    }

                    // 2. DHST
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = p.TreatmentId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        Log("\n--- DẤU HIỆU SINH TỒN (DHST) GẦN NHẤT ---");
                        foreach (var d in dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).Take(2))
                        {
                            Log(string.Format("  [{0}] Mạch: {1} ck/p | HA: {2}/{3} mmHg | NĐ: {4} °C | SpO2: {5}% | Cân nặng: {6} kg | Chiều cao: {7} cm | Nhịp thở: {8}",
                                d.EXECUTE_TIME ?? d.CREATE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT, d.HEIGHT, d.BREATH_RATE));
                        }
                    }

                    // 3. Trackings (Bệnh sử, tiền sử, diễn biến lâm sàng)
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = p.TreatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        Log("\n--- CÁC TỜ ĐIỀU TRỊ (TRACKINGS) ---");
                        foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
                        {
                            Log(string.Format("  [{0}] (BS: {1}, Buồng: {2}):", trk.TRACKING_TIME, trk.CREATOR, trk.ROOM_NAME));
                            if (!string.IsNullOrEmpty(trk.ICD_TEXT)) Log("    Chẩn đoán: " + trk.ICD_TEXT);
                            if (!string.IsNullOrEmpty(trk.CONTENT)) Log("    Diễn biến: " + trk.CONTENT.Replace("\r\n", " | ").Replace("\n", " | "));
                            if (!string.IsNullOrEmpty(trk.MEDICAL_INSTRUCTION)) Log("    Y lệnh: " + trk.MEDICAL_INSTRUCTION.Replace("\r\n", " | ").Replace("\n", " | "));
                        }
                    }

                    // 4. Xét nghiệm (Tein)
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = p.TreatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
                    if (teins != null && teins.Count > 0)
                    {
                        Log("\n--- KẾT QUẢ XÉT NGHIỆM CHI TIẾT ---");
                        Func<string, string> getTein = delegate(string codeOrName) {
                            var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                                ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(codeOrName, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(codeOrName, StringComparison.OrdinalIgnoreCase) >= 0)));
                            return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "Chưa có";
                        };

                        Log("  • CTM: WBC: " + getTein("WBC") + " | Neut%: " + getTein("NEUT%") + " | HGB: " + getTein("HGB") + " | PLT: " + getTein("PLT") + " | RBC: " + getTein("RBC") + " | HCT: " + getTein("HCT"));
                        Log("  • Đông máu: PT%: " + getTein("PT (%)") + " | INR: " + getTein("PT - INR") + " | APTT: " + getTein("APTT (s)") + " | Fib: " + getTein("Fibrinogen"));
                        Log("  • Sinh hóa: Glucose: " + getTein("Glucose") + " | Ure: " + getTein("Urê") + " | Creatinin: " + getTein("Creatinin") + " | AST: " + getTein("AST") + " | ALT: " + getTein("ALT") + " | eGFR: " + getTein("eGFR"));
                        Log("  • Điện giải: Na: " + getTein("Natri") + " | K: " + getTein("Kali") + " | Cl: " + getTein("Clo") + " | Ca: " + getTein("Calci"));
                        Log("  • Nhóm máu: ABO: " + getTein("ABO") + " | Rh: " + getTein("Rh"));
                        Log("  • Virus/Miễn dịch: HIV: " + getTein("HIV") + " | HBsAg: " + getTein("HBsAg") + " | HCV: " + getTein("HCV"));
                        Log("  • Viêm / Nội tiết: Troponin: " + getTein("Troponin") + " | CRP: " + getTein("CRP") + " | Procalcitonin: " + getTein("Procalcitonin") + " | HbA1c: " + getTein("HbA1c") + " | Cortisol: " + getTein("Cortisol") + " | ACTH: " + getTein("ACTH"));
                    }

                    // 5. Cận lâm sàng & Chẩn đoán hình ảnh (SereServ)
                    HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                    ssFilter.TREATMENT_ID = p.TreatmentId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);
                    if (sss != null && sss.Count > 0)
                    {
                        Log("\n--- DANH SÁCH DỊCH VỤ CẬN LÂM SÀNG & HÌNH ẢNH (SERE_SERV) ---");
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
                Console.WriteLine("Đã ghi toàn bộ dữ liệu ra file: " + outFile);
            }
        }
    }
}
