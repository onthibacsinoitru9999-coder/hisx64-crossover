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

namespace Query5Patients
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
            StreamWriter logFile = new StreamWriter("query_5_patients_detail.txt", false, Encoding.UTF8);

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

                string[] targetNames = new string[] {
                    "HOÀNG HỮU CƯỜNG",
                    "NGUYỄN THỊ SỆT",
                    "PHẠM THÙY DUNG",
                    "TRẦN VĂN HẢI",
                    "TRẦN THỊ TƠM"
                };

                // Lay danh sach buong benh khoa 57
                HisBedRoomViewFilter bedRoomFilter = new HisBedRoomViewFilter();
                bedRoomFilter.DEPARTMENT_ID = 57;
                var bedRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bedRoomFilter, param);
                List<long> bedRoomIds = bedRooms.Select(x => x.ID).ToList();

                // Lay danh sach BN dang nam phong
                HisTreatmentBedRoomLViewFilter tbrFilter = new HisTreatmentBedRoomLViewFilter();
                tbrFilter.BED_ROOM_IDs = bedRoomIds;
                tbrFilter.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrFilter, param);

                foreach (var name in targetNames)
                {
                    Log("==========================================================================");
                    Log("TRA CUU: " + name);
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
                        var searchTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                        if (searchTreats != null && searchTreats.Count > 0)
                        {
                            var latest = searchTreats.OrderByDescending(x => x.IN_TIME).First();
                            treatmentId = latest.ID;
                        }
                    }

                    if (treatmentId == 0)
                    {
                        Log("KHONG TIM THAY HO SO CHO: " + name);
                        continue;
                    }

                    // Chi tiet treatment
                    HisTreatmentViewFilter tFilter = new HisTreatmentViewFilter();
                    tFilter.ID = treatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tFilter, param);
                    var t = tList.First();

                    Log(string.Format("Họ tên: {0} | DOB: {1} | Giới tính: {2} | Mã BN: {3} | Mã BA: {4}",
                        t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
                    Log(string.Format("Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                    Log(string.Format("Vào viện: {0} | Buồng: {1} | Giường: {2}", t.IN_TIME, roomName, bedName));
                    Log(string.Format("Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));

                    // DHST
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = treatmentId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        var d = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                        Log(string.Format("DHST: Mạch: {0} | HA: {1}/{2} | NĐ: {3} | SpO2: {4} | Cân nặng: {5} | Chiều cao: {6}",
                            d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT, d.HEIGHT));
                    }

                    // Trackings
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = treatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        Log("\n--- DIEN BIEN LAM SANG (TRACKINGS) ---");
                        foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
                        {
                            Log(string.Format("[{0}] (Phòng: {1}, BS: {2}): {3}",
                                trk.TRACKING_TIME, trk.ROOM_NAME, trk.CREATOR, trk.CONTENT != null ? trk.CONTENT.Replace("\n", " -- ") : ""));
                        }
                    }

                    // Tein (Xet nghiem)
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = treatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
                    if (teins != null && teins.Count > 0)
                    {
                        Log("\n--- XET NGHIEM ---");
                        Func<string, string> getTein = delegate(string codeOrName) {
                            var match = teins.LastOrDefault(x => !string.IsNullOrEmpty(x.VALUE) &&
                                ((x.TEST_INDEX_CODE != null && x.TEST_INDEX_CODE.Equals(codeOrName, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.TEST_INDEX_NAME != null && x.TEST_INDEX_NAME.IndexOf(codeOrName, StringComparison.OrdinalIgnoreCase) >= 0)));
                            return match != null ? match.VALUE + " " + match.TEST_INDEX_UNIT_NAME : "Chưa có";
                        };

                        Log("• CTM: WBC: " + getTein("WBC") + " | Neut%: " + getTein("NEUT%") + " | HGB: " + getTein("HGB") + " | PLT: " + getTein("PLT"));
                        Log("• Đông máu: PT%: " + getTein("PT (%)") + " | INR: " + getTein("PT - INR") + " | APTT: " + getTein("APTT (s)") + " | Fib: " + getTein("Fibrinogen"));
                        Log("• Sinh hóa: Glucose: " + getTein("Glucose") + " | Ure: " + getTein("Urê") + " | Creatinin: " + getTein("Creatinin") + " | AST: " + getTein("AST") + " | ALT: " + getTein("ALT"));
                        Log("• Điện giải: Na: " + getTein("Natri") + " | K: " + getTein("Kali") + " | Cl: " + getTein("Clo"));
                        Log("• Nhóm máu: ABO: " + getTein("ABO") + " | Rh: " + getTein("Rh"));
                        Log("• Virus: HIV: " + getTein("HIV") + " | HBsAg: " + getTein("HBsAg") + " | HCV: " + getTein("HCV"));
                    }

                    // SereServ (CDHA / CLS / Phau thuat)
                    HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                    ssFilter.TREATMENT_ID = treatmentId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);
                    if (sss != null)
                    {
                        Log("\n--- CDHA & DICH VU CLS ---");
                        var clsList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8).ToList();
                        foreach (var s in clsList.OrderBy(x => x.TDL_INTRUCTION_TIME))
                        {
                            Log(string.Format("  [{0}] {1} ({2}) - Phòng: {3}",
                                s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME));
                        }
                    }
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
