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

namespace CheckNguyen
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
            StreamWriter logFile = new StreamWriter("check_nguyen_result.txt", false, Encoding.UTF8);

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

                // 1. Tim benh nhan theo ten
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = "LÊ DOÃN NGUYÊN";
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);

                if (treats == null || treats.Count == 0)
                {
                    // Thu tim bang tu khoa khong dau hoac trong buong 712
                    HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                    bf.DEPARTMENT_ID = 57;
                    var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
                    var room712 = bList.FirstOrDefault(x => x.BED_ROOM_NAME.Contains("712"));
                    if (room712 != null)
                    {
                        HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                        tbrf.BED_ROOM_ID = room712.ID;
                        tbrf.IS_IN_ROOM = true;
                        var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);
                        if (inPatients != null)
                        {
                            foreach (var p in inPatients)
                            {
                                Log(string.Format("Buong 712: BN: {0} | Ma BN: {1} | Giuong: {2} | TreatmentId: {3}",
                                    p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.BED_NAME, p.TREATMENT_ID));
                            }
                        }
                    }
                    return;
                }

                var t = treats.OrderByDescending(x => x.IN_TIME).First();
                long treatmentId = t.ID;

                Log("==========================================================================");
                Log(string.Format("Họ tên: {0} | DOB: {1} | Giới tính: {2} | Mã BN: {3} | Mã BA: {4}",
                    t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
                Log(string.Format("Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                Log(string.Format("Vào viện: {0} | Trạng thái: {1}", t.IN_TIME, t.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị"));
                Log(string.Format("Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));

                // Bed / Room
                HisTreatmentBedRoomLViewFilter tbrFilter = new HisTreatmentBedRoomLViewFilter();
                tbrFilter.TREATMENT_ID = treatmentId;
                var tbrList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrFilter, param);
                if (tbrList != null && tbrList.Count > 0)
                {
                    var lastBed = tbrList.OrderByDescending(x => x.ADD_TIME).First();
                    Log(string.Format("Buồng hiện tại: {0} | Giường: {1}", lastBed.BED_ROOM_NAME, lastBed.BED_NAME));
                }

                // DHST
                HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                dhstFilter.TREATMENT_ID = treatmentId;
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                if (dhsts != null && dhsts.Count > 0)
                {
                    var d = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                    Log(string.Format("DHST: Mạch: {0} l/p | HA: {1}/{2} mmHg | NĐ: {3} C | SpO2: {4}% | Cân nặng: {5} kg",
                        d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT));
                }

                // Trackings
                HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                trkFilter.TREATMENT_ID = treatmentId;
                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, param);
                if (trks != null && trks.Count > 0)
                {
                    Log("\n--- DIỄN BIẾN LÂM SÀNG (TỜ ĐIỀU TRỊ) ---");
                    foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
                    {
                        Log(string.Format("[{0}] ({1}, BS: {2}): {3}",
                            trk.TRACKING_TIME, trk.ROOM_NAME, trk.CREATOR, trk.CONTENT != null ? trk.CONTENT.Replace("\n", " -- ") : ""));
                    }
                }

                // Tein
                HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                teinFilter.TDL_TREATMENT_ID = treatmentId;
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
                if (teins != null && teins.Count > 0)
                {
                    Log("\n--- KẾT QUẢ XÉT NGHIỆM ---");
                    foreach (var grp in teins.Where(x => !string.IsNullOrEmpty(x.VALUE)).GroupBy(x => x.TEST_INDEX_GROUP_NAME ?? "Xét nghiệm khác"))
                    {
                        Log(string.Format("\n* [{0}]:", grp.Key));
                        foreach (var item in grp.OrderBy(x => x.TEST_INDEX_NAME))
                        {
                            Log(string.Format("   - {0} ({1}): {2} {3} [Tham chiếu: {4}]",
                                item.TEST_INDEX_NAME, item.TEST_INDEX_CODE, item.VALUE, item.TEST_INDEX_UNIT_NAME, item.NOTE));
                        }
                    }
                }

                // SereServ
                HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                ssFilter.TREATMENT_ID = treatmentId;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);
                if (sss != null)
                {
                    Log("\n--- CHẨN ĐOÁN HÌNH ẢNH, THĂM DÒ CHỨC NĂNG & DỊCH VỤ CLS ---");
                    var clsList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8 || x.TDL_SERVICE_TYPE_ID == 9).ToList();
                    foreach (var s in clsList.OrderBy(x => x.TDL_INTRUCTION_TIME))
                    {
                        Log(string.Format("  [{0}] {1} ({2}) | Phòng: {3} | Trạng thái: {4}",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME, s.IS_EXPEND == 1 ? "Hao phí" : "Chỉ định"));
                    }
                }

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
