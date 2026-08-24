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

namespace CheckTiep
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
            StreamWriter logFile = new StreamWriter("van_tiep_detail.txt", false, Encoding.UTF8);

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

                // 1. Tim trong Khoa 57 truoc
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
                List<long> roomIds = bList.Select(x => x.ID).ToList();

                HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                tbrf.BED_ROOM_IDs = roomIds;
                tbrf.IS_IN_ROOM = true;
                var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

                V_HIS_TREATMENT_BED_ROOM target = null;
                if (inPatients != null)
                {
                    target = inPatients.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && (x.TDL_PATIENT_NAME.ToUpper().Contains("VĂN TIỆP") || x.TDL_PATIENT_NAME.ToUpper().Contains("VAN TIEP") || x.TDL_PATIENT_NAME.ToUpper().Contains("TIỆP")));
                }

                long treatmentId = 0;
                string roomName = "";
                string bedName = "";

                if (target != null)
                {
                    treatmentId = target.TREATMENT_ID;
                    roomName = target.BED_ROOM_NAME;
                    bedName = target.BED_NAME;
                    Log(string.Format("Tìm thấy trong Khoa 57: BN {0} | Phòng: {1} | Giường: {2} | TreatmentId: {3}",
                        target.TDL_PATIENT_NAME, roomName, bedName, treatmentId));
                }
                else
                {
                    Log("Không thấy đang nằm buồng Khoa 57, tìm kiếm trên toàn viện theo từ khóa...");
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.KEY_WORD = "TIỆP";
                    var searchList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    if (searchList != null && searchList.Count > 0)
                    {
                        var match = searchList.Where(x => x.TDL_PATIENT_NAME != null && (x.TDL_PATIENT_NAME.ToUpper().Contains("VĂN TIỆP") || x.TDL_PATIENT_NAME.ToUpper().Contains("VAN TIEP")))
                                              .OrderByDescending(x => x.IN_TIME).FirstOrDefault();
                        if (match == null) match = searchList.OrderByDescending(x => x.IN_TIME).First();
                        treatmentId = match.ID;
                    }
                }

                if (treatmentId == 0)
                {
                    Log("KHÔNG TÌM THẤY BỆNH NHÂN VĂN TIỆP TRÊN HỆ THỐNG!");
                    return;
                }

                // Chi tiet Treatment
                HisTreatmentViewFilter tFilter = new HisTreatmentViewFilter();
                tFilter.ID = treatmentId;
                var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tFilter, param);
                var t = tList.First();

                Log("==========================================================================");
                Log(string.Format("Họ tên: {0} | DOB: {1} | Giới tính: {2} | Mã BN: {3} | Mã BA: {4}",
                    t.TDL_PATIENT_NAME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
                Log(string.Format("Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                Log(string.Format("Vào viện: {0} | Buồng: {1} | Giường: {2} | Trạng thái: {3}",
                    t.IN_TIME, roomName, bedName, t.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị"));
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

                // Tờ điều trị / Trackings
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

                // SereServ (Tat ca dich vu va ngay chi dinh)
                HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                ssFilter.TREATMENT_ID = treatmentId;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param);

                // Tein (Chi tiet ket qua xet nghiem kem TDL_INTRUCTION_TIME)
                HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                teinFilter.TDL_TREATMENT_ID = treatmentId;
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);

                Log("\n==========================================================================");
                Log("CHI TIẾT TỪNG XÉT NGHIỆM KÈM NGÀY GIỜ CHỈ ĐỊNH VÀ KẾT QUẢ");
                Log("==========================================================================");

                if (sss != null)
                {
                    var xnList = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                    Log(string.Format("\n--- DANH SÁCH {0} DỊCH VỤ XÉT NGHIỆM ĐÃ CHỈ ĐỊNH ---", xnList.Count));
                    foreach (var xn in xnList)
                    {
                        Log(string.Format("\n[Ngày chỉ định: {0}] Dịch vụ: {1} (Mã: {2}) | Phòng TH: {3} | SereServId: {4}",
                            xn.TDL_INTRUCTION_TIME, xn.TDL_SERVICE_NAME, xn.TDL_SERVICE_CODE, xn.EXECUTE_ROOM_NAME, xn.ID));

                        // Lay cac chi so tein thuoc seraserv nay
                        if (teins != null)
                        {
                            var relatedTeins = teins.Where(titem => titem.SERE_SERV_ID == xn.ID).ToList();
                            if (relatedTeins.Count > 0)
                            {
                                foreach (var titem in relatedTeins.OrderBy(o => o.TEST_INDEX_NAME))
                                {
                                    Log(string.Format("    -> Chỉ số: {0} ({1}) = {2} {3} [Tham chiếu: {4}]",
                                        titem.TEST_INDEX_NAME, titem.TEST_INDEX_CODE, titem.VALUE, titem.TEST_INDEX_UNIT_NAME, titem.NOTE));
                                }
                            }
                            else
                            {
                                Log("    -> (Chưa có kết quả hoặc kết quả dạng text/file)");
                            }
                        }
                    }

                    // CDHA & Dich vu khac
                    var otherCls = sss.Where(x => x.TDL_SERVICE_TYPE_ID != 2 && (x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8 || x.TDL_SERVICE_TYPE_ID == 9 || x.TDL_SERVICE_TYPE_ID == 11)).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                    Log(string.Format("\n--- DANH SÁCH {0} DỊCH VỤ CĐHA / SIÊU ÂM / TDCN / THỦ THUẬT ---", otherCls.Count));
                    foreach (var s in otherCls)
                    {
                        Log(string.Format("  [Ngày chỉ định: {0}] {1} ({2}) | Phòng: {3} | Trạng thái: {4}",
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
