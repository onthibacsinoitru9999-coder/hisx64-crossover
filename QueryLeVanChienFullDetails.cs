using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Token.Core;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace SurgeryQuery
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
            long treatmentId = 7060265; // LÊ VĂN CHIẾN

            CommonParam param = new CommonParam();
            TokenClient.ClientTokenManager = new ClientTokenManager();
            TokenClient.ClientTokenManager.SetTokenData(new TokenData { TokenCode = "HIS_DESKTOP_TOKEN" });

            MyAdapter adapter = new MyAdapter();
            var consumer = ApiConsumerStore.MosConsumer;

            Console.WriteLine("================================================================================");
            Console.WriteLine("TRA CỨU TOÀN BỘ DỮ LIỆU PHẪU THUẬT CỦA BỆNH NHÂN LÊ VĂN CHIẾN (TREATMENT_ID: 7060265)");
            Console.WriteLine("================================================================================");

            // 1. HisTreatment
            HisTreatmentFilter tFilter = new HisTreatmentFilter { ID = treatmentId };
            var treatments = adapter.FetchList<HIS_TREATMENT>("api/HisTreatment/Get", consumer, tFilter, param);
            if (treatments != null && treatments.Count > 0)
            {
                var t = treatments[0];
                Console.WriteLine(string.Format("Họ tên: {0} | Mã BN: {1} | Mã BA: {2}", t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE));
                Console.WriteLine(string.Format("Thời gian vào viện: {0} | Kết thúc: {1}", t.IN_TIME, t.OUT_TIME));
                Console.WriteLine(string.Format("Chẩn đoán: {0} - {1}", t.ICD_CODE, t.ICD_NAME));
                Console.WriteLine(string.Format("Chẩn đoán phụ: {0} - {1}", t.ICD_SUB_CODE, t.ICD_TEXT));
            }

            // 2. ServiceReq (Y lệnh dịch vụ)
            Console.WriteLine("\n--- TẤT CẢ SERVICE_REQ (Y LỆNH DỊCH VỤ / PHẪU THUẬT / THỦ THUẬT) ---");
            HisServiceReqFilter srFilter = new HisServiceReqFilter { TREATMENT_ID = treatmentId };
            var srs = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", consumer, srFilter, param);
            if (srs != null)
            {
                foreach (var sr in srs)
                {
                    Console.WriteLine(string.Format("[SR ID: {0}] Code: {1} | Type: {2} | IntructionTime: {3} | ReqRoomId: {4} | ExecuteRoomId: {5}",
                        sr.ID, sr.SERVICE_REQ_CODE, sr.SERVICE_REQ_TYPE_ID, sr.INTRUCTION_TIME, sr.REQUEST_ROOM_ID, sr.EXECUTE_ROOM_ID));
                    Console.WriteLine(string.Format("  BS Y Lệnh: {0} ({1}) | BS Thực hiện: {2} ({3})",
                        sr.REQUEST_LOGINNAME, sr.REQUEST_USERNAME, sr.EXECUTE_LOGINNAME, sr.EXECUTE_USERNAME));
                    Console.WriteLine(string.Format("  Trạng thái: {0} | StartTime: {1} | FinishTime: {2}",
                        sr.SERVICE_REQ_STT_ID, sr.START_TIME, sr.FINISH_TIME));
                    Console.WriteLine(string.Format("  ICD: {0} - {1} | SubICD: {2} - {3}",
                        sr.ICD_CODE, sr.ICD_NAME, sr.ICD_SUB_CODE, sr.ICD_TEXT));
                }
            }

            // 3. SereServ (Dịch vụ chỉ định)
            Console.WriteLine("\n--- TẤT CẢ SERE_SERV (DỊCH VỤ THỰC HIỆN) ---");
            HisSereServFilter ssFilter = new HisSereServFilter { TREATMENT_ID = treatmentId };
            var sss = adapter.FetchList<HIS_SERE_SERV>("api/HisSereServ/Get", consumer, ssFilter, param);
            if (sss != null)
            {
                foreach (var ss in sss)
                {
                    if (ss.TDL_SERVICE_TYPE_ID == 4 || ss.TDL_SERVICE_TYPE_ID == 3 || ss.EKIP_ID.HasValue)
                    {
                        Console.WriteLine(string.Format("[SS ID: {0}] Dịch vụ: {1} (Type: {2}) | Amount: {3}",
                            ss.ID, ss.TDL_SERVICE_NAME, ss.TDL_SERVICE_TYPE_ID, ss.AMOUNT));
                        Console.WriteLine(string.Format("  ServiceReqId: {0} | EkipId: {1} | IntructionTime: {2}",
                            ss.SERVICE_REQ_ID, ss.EKIP_ID, ss.TDL_INTRUCTION_TIME));
                        Console.WriteLine(string.Format("  BS Chỉ định: {0} ({1}) | ExecuteRoom: {2}",
                            ss.TDL_REQUEST_LOGINNAME, ss.TDL_REQUEST_USERNAME, ss.TDL_EXECUTE_ROOM_ID));

                        // Check Ekip
                        if (ss.EKIP_ID.HasValue)
                        {
                            HisEkipUserFilter euFilter = new HisEkipUserFilter { EKIP_ID = ss.EKIP_ID.Value };
                            var euList = adapter.FetchList<HIS_EKIP_USER>("api/HisEkipUser/Get", consumer, euFilter, param);
                            if (euList != null && euList.Count > 0)
                            {
                                Console.WriteLine("  ===> DANH SÁCH THÀNH VIÊN KÍP MỔ (EKIP):");
                                foreach (var eu in euList)
                                {
                                    Console.WriteLine(string.Format("    - Vai trò (EXECUTE_ROLE_ID): {0} | Login: {1} | Tên: {2} | Khoa: {3}",
                                        eu.EXECUTE_ROLE_ID, eu.LOGINNAME, eu.USERNAME, eu.DEPARTMENT_ID));
                                }
                            }
                        }

                        // Check SereServPttt
                        HisSereServPtttFilter ptttFilter = new HisSereServPtttFilter { SERE_SERV_ID = ss.ID };
                        var ptttList = adapter.FetchList<HIS_SERE_SERV_PTTT>("api/HisSereServPttt/Get", consumer, ptttFilter, param);
                        if (ptttList != null && ptttList.Count > 0)
                        {
                            Console.WriteLine("  ===> PHIẾU PHẪU THUẬT THỦ THUẬT (SERE_SERV_PTTT):");
                            foreach (var pt in ptttList)
                            {
                                Console.WriteLine(string.Format("    CD Trước mổ: {0} | CD Sau mổ: {1}", pt.BEFORE_PTTT_ICD_NAME, pt.AFTER_PTTT_ICD_NAME));
                                Console.WriteLine(string.Format("    Phương pháp PTTT: {0} | Cách thức: {1}", pt.REAL_PTTT_METHOD, pt.MANNER));
                                Console.WriteLine(string.Format("    Vô cảm: {0} | Nhóm PTTT: {1}", pt.EMOTION_LESS_METHOD_ID, pt.PTTT_GROUP_ID));
                            }
                        }

                        // Check SereServExt
                        HisSereServExtFilter extFilter = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                        var extList = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", consumer, extFilter, param);
                        if (extList != null && extList.Count > 0)
                        {
                            Console.WriteLine("  ===> SERE_SERV_EXT:");
                            foreach (var ext in extList)
                            {
                                Console.WriteLine(string.Format("    Bắt đầu: {0} | Kết thúc: {1} | Mô tả: {2}", ext.BEGIN_TIME, ext.END_TIME, ext.DESCRIPTION));
                            }
                        }
                    }
                }
            }

            // 4. Check Tracking sheets
            Console.WriteLine("\n--- TỜ ĐIỀU TRỊ (HIS_TRACKING) ---");
            HisTrackingFilter trFilter = new HisTrackingFilter { TREATMENT_ID = treatmentId };
            var trackings = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", consumer, trFilter, param);
            if (trackings != null)
            {
                foreach (var tr in trackings)
                {
                    Console.WriteLine(string.Format("[{0} - BS: {1}] {2}", tr.TRACKING_TIME, tr.CREATOR, tr.TRACKING_CONTENT));
                    if (!string.IsNullOrEmpty(tr.TREATMENT_INSTRUCTION))
                    {
                        Console.WriteLine("  Y LỆNH: " + tr.TREATMENT_INSTRUCTION);
                    }
                }
            }
        }
    }
}
