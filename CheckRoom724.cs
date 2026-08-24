using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace Room724
{
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
            Console.OutputEncoding = Encoding.UTF8;
            StreamWriter logFile = new StreamWriter("room_724_result.txt", false, Encoding.UTF8);

            Action<string> Log = delegate(string msg)
            {
                Console.WriteLine(msg);
                logFile.WriteLine(msg);
            };

            try
            {
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                CommonParam param = new CommonParam();
                var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                ApiConsumers.SetConsunmer(token.TokenCode);
                MyAdapter adapter = new MyAdapter();

                // 1. Lay Buong 724
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
                var room724 = bList.FirstOrDefault(x => x.BED_ROOM_NAME.Contains("724"));

                if (room724 == null)
                {
                    Log("KHONG TIM THAY BUONG 724 TRONG KHOA 57!");
                    return;
                }

                Log("==========================================================================");
                Log(string.Format("BUONG BENH: {0} (ID: {1})", room724.BED_ROOM_NAME, room724.ID));
                Log("==========================================================================");

                // 2. Lay danh sach BN dang nam trong buong 724
                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                tbrf.BED_ROOM_ID = room724.ID;
                tbrf.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

                if (inPatients == null || inPatients.Count == 0)
                {
                    Log("Hien tai khong co benh nhan nao dang nam tai buong 724.");
                    return;
                }

                Log(string.Format("Tong so benh nhan dang nam tai phong 724: {0}\n", inPatients.Count));

                int count = 1;
                foreach (var p in inPatients.OrderBy(x => x.BED_NAME))
                {
                    long treatmentId = p.TREATMENT_ID;
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = treatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    var t = tList != null ? tList.FirstOrDefault() : null;

                    Log("--------------------------------------------------------------------------------------------------");
                    Log(string.Format("{0}. BN: {1} | Giường: {2} | Mã BN: {3} | Mã BA: {4} | Giới tính: {5} | DOB: {6}",
                        count++, p.TDL_PATIENT_NAME, p.BED_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE,
                        t != null ? t.TDL_PATIENT_GENDER_NAME : "", t != null ? t.TDL_PATIENT_DOB.ToString() : ""));
                    if (t != null)
                    {
                        Log(string.Format("   Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                        Log(string.Format("   Vào viện: {0} | Trạng thái: {1}", t.IN_TIME, t.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị"));
                        Log(string.Format("   Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));
                    }

                    // DHST
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = treatmentId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        var d = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                        Log(string.Format("   DHST gần nhất: Mạch: {0} l/p | HA: {1}/{2} mmHg | NĐ: {3} C | SpO2: {4}% | Cân nặng: {5} kg",
                            d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT));
                    }

                    // Dien bien gan nhat
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = treatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        var lastTrk = trks.OrderByDescending(x => x.TRACKING_TIME).First();
                        Log(string.Format("   Tờ điều trị mới nhất [{0}] (BS: {1}): {2}",
                            lastTrk.TRACKING_TIME, lastTrk.CREATOR, lastTrk.CONTENT != null ? lastTrk.CONTENT.Replace("\n", " -- ") : ""));
                    }

                    // Tein (Xet nghiem)
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = treatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);

                    Func<string, string> getTein = delegate(string codeOrName) {
                        if (teins == null) return "Chưa có";
                        var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                            ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(codeOrName, StringComparison.OrdinalIgnoreCase)) ||
                             (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(codeOrName, StringComparison.OrdinalIgnoreCase) >= 0)));
                        return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "Chưa có";
                    };

                    Log("   --- XÉT NGHIỆM TIỀN PHẪU ---");
                    Log(string.Format("   • CTM: WBC: {0} | Neut%: {1} | HGB: {2} | PLT: {3}",
                        getTein("WBC"), getTein("NEUT%"), getTein("HGB"), getTein("PLT")));
                    Log(string.Format("   • Đông máu: PT%: {0} | INR: {1} | APTT: {2} | Fibrinogen: {3}",
                        getTein("PT (%)"), getTein("PT - INR"), getTein("APTT (s)"), getTein("Fibrinogen")));
                    Log(string.Format("   • Sinh hóa: Glucose: {0} | Urê: {1} | Creatinin: {2} | AST: {3} | ALT: {4}",
                        getTein("Glucose"), getTein("Urê"), getTein("Creatinin"), getTein("AST"), getTein("ALT")));
                    Log(string.Format("   • Điện giải: Na: {0} | K: {1} | Cl: {2}",
                        getTein("Natri"), getTein("Kali"), getTein("Clo")));
                    Log(string.Format("   • Nhóm máu: ABO: {0} | Rh: {1}",
                        getTein("ABO"), getTein("Rh")));
                    Log(string.Format("   • Bilan Virus: HIV: {0} | HBsAg: {1} | HCV: {2}",
                        getTein("HIV"), getTein("HBsAg"), getTein("HCV")));

                    // SereServ (CDHA & Phau thuat)
                    HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                    ssFilter.TREATMENT_ID = treatmentId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);

                    Log("   --- CẬN LÂM SÀNG & CHẨN ĐOÁN HÌNH ẢNH ---");
                    if (sss != null)
                    {
                        var clsList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                        foreach (var s in clsList)
                        {
                            Log(string.Format("     [{0}] {1} ({2})", s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME));
                        }
                    }
                }

                Log("\n==========================================================================");
                Log("HOAN TAT SOAT BILAN BUONG 724");
                Log("==========================================================================");
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
