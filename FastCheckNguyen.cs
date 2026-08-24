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

namespace FastCheckNguyen
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
            StreamWriter logFile = new StreamWriter("fast_check_nguyen_result.txt", false, Encoding.UTF8);

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

                // 1. Lay buong 712
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
                
                var room712 = bList.FirstOrDefault(x => x.BED_ROOM_NAME.Contains("712"));
                List<long> roomIds = room712 != null ? new List<long> { room712.ID } : bList.Select(x => x.ID).ToList();

                // 2. Lay benh nhan trong buong
                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                tbrf.BED_ROOM_IDs = roomIds;
                tbrf.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

                V_HIS_TREATMENT_BED_ROOM target = null;
                if (inPatients != null)
                {
                    Log("--- DANH SACH BENH NHAN TRONG BUONG 712 ---");
                    foreach (var p in inPatients)
                    {
                        Log(string.Format("BN: {0} | Ma BN: {1} | Giuong: {2} | TreatmentId: {3}",
                            p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.BED_NAME, p.TREATMENT_ID));
                        if (p.TDL_PATIENT_NAME != null && (p.TDL_PATIENT_NAME.ToUpper().Contains("NGUYÊN") || p.TDL_PATIENT_NAME.ToUpper().Contains("NGUYEN")))
                        {
                            target = p;
                        }
                    }
                }

                if (target == null)
                {
                    Log("Khong tim thay truc tiep trong buong 712, thu tim toan khoa 57...");
                    HisTreatmentBedRoomLViewFilter allTbrf = new HisTreatmentBedRoomLViewFilter();
                    allTbrf.BED_ROOM_IDs = bList.Select(x => x.ID).ToList();
                    allTbrf.IS_IN_ROOM = true;
                    var allInPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, allTbrf, param);
                    if (allInPatients != null)
                    {
                        target = allInPatients.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && (x.TDL_PATIENT_NAME.ToUpper().Contains("LÊ DOÃN NGUYÊN") || x.TDL_PATIENT_NAME.ToUpper().Contains("LE DOAN NGUYEN") || x.TDL_PATIENT_NAME.ToUpper().Contains("DOÃN NGUYÊN")));
                    }
                }

                if (target == null)
                {
                    Log("KHONG TIM THAY BENH NHAN LÊ DOÃN NGUYÊN!");
                    return;
                }

                long treatmentId = target.TREATMENT_ID;

                // 3. Lay thong tin Treatment
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.ID = treatmentId;
                var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                var t = tList.First();

                Log("\n==========================================================================");
                Log(string.Format("Họ tên: {0} | DOB: {1} | Giới tính: {2} | Mã BN: {3} | Mã BA: {4}",
                    t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
                Log(string.Format("Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                Log(string.Format("Vào viện: {0} | Buồng: {1} | Giường: {2} | Trạng thái: {3}",
                    t.IN_TIME, target.BED_ROOM_NAME, target.BED_NAME, t.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị"));
                Log(string.Format("Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));

                // DHST
                HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                dhstFilter.TREATMENT_ID = treatmentId;
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                if (dhsts != null && dhsts.Count > 0)
                {
                    var d = dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).First();
                    Log(string.Format("DHST gần nhất: Mạch: {0} l/p | HA: {1}/{2} mmHg | NĐ: {3} C | SpO2: {4}% | Cân nặng: {5} kg",
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
                        Log(string.Format("[{0}] ({1}, BS: {2}):\n   Diễn biến: {3}\n   Y lệnh: {4}",
                            trk.TRACKING_TIME, trk.ROOM_NAME, trk.CREATOR, 
                            trk.CONTENT != null ? trk.CONTENT.Replace("\n", " -- ") : "",
                            trk.MEDICAL_INSTRUCTION != null ? trk.MEDICAL_INSTRUCTION.Replace("\n", " -- ") : ""));
                    }
                }

                // Tein
                HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                teinFilter.TDL_TREATMENT_ID = treatmentId;
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
                if (teins != null && teins.Count > 0)
                {
                    Log("\n--- KẾT QUẢ XÉT NGHIỆM ĐÃ CÓ ---");
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
                else
                {
                    Log("\n--- CHƯA CÓ KẾT QUẢ XÉT NGHIỆM NÀO TRẢ VỀ ---");
                }

                // SereServ
                HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                ssFilter.TREATMENT_ID = treatmentId;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);
                if (sss != null)
                {
                    Log("\n--- DỊCH VỤ CẬN LÂM SÀNG / CHẨN ĐOÁN HÌNH ẢNH / Y LỆNH ---");
                    foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
                    {
                        Log(string.Format("  [{0}] {1} ({2}) | Phòng: {3} | TT: {4}",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME, s.IS_EXPEND == 1 ? "Hao phí" : "Chỉ định"));
                    }
                }

                Log("\n==========================================================================");
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
