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

namespace TestBedRooms
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

            Console.OutputEncoding = Encoding.UTF8;
            Load.Init();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            CommonParam param = new CommonParam();
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (token == null)
            {
                Console.WriteLine("Token login returned null!");
                return;
            }
            Console.WriteLine("Login successful!");
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.KEY_WORD = "NGUYỄN THẾ TRƯỜNG";
            var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param) ?? new List<V_HIS_TREATMENT>();
            Console.WriteLine("Tìm thấy treatments: " + treats.Count);

            foreach (var t in treats.OrderByDescending(x => x.IN_TIME))
            {
                Console.WriteLine(string.Format("\n=========================================================================="));
                Console.WriteLine(string.Format("HỒ SƠ: {0} | Mã BN: {1} | Mã BA: {2} | Vào: {3} | DOB: {4} | Giới: {5}",
                    t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.IN_TIME, t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME));
                Console.WriteLine(string.Format("  • Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));
                Console.WriteLine(string.Format("  • Khoa cuối: {0} | Tình trạng: {1} | IS_PAUSE: {2}", t.LAST_DEPARTMENT_ID, (t.IS_ACTIVE == 1 ? "Đang điều trị" : "Đã ra viện"), t.IS_PAUSE));

                // Buồng giường
                var bedFilter = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = t.ID };
                var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, bedFilter, param) ?? new List<V_HIS_TREATMENT_BED_ROOM>();
                foreach (var b in beds.OrderByDescending(x => x.ADD_TIME))
                {
                    Console.WriteLine(string.Format("  • Buồng giường: {0} ({1}) | Vào: {2} -> Ra: {3}", b.BED_ROOM_NAME, b.BED_NAME, b.ADD_TIME, b.REMOVE_TIME));
                }

                // Department trans
                var depFilter = new HisDepartmentTranViewFilter { TREATMENT_ID = t.ID };
                var deps = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", ApiConsumers.MosConsumer, depFilter, param) ?? new List<V_HIS_DEPARTMENT_TRAN>();
                foreach (var d in deps.OrderBy(x => x.DEPARTMENT_IN_TIME))
                {
                    Console.WriteLine(string.Format("  • Khoa: {0} (Vào: {1}) | Từ: {2}", d.DEPARTMENT_NAME, d.DEPARTMENT_IN_TIME, d.PREVIOUS_DEPARTMENT_NAME));
                }

                // Service reqs (Surgeries, CLS, etc)
                var reqFilter = new HisServiceReqViewFilter { TREATMENT_ID = t.ID };
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, reqFilter, param) ?? new List<V_HIS_SERVICE_REQ>();
                Console.WriteLine("\n  --- DANH SÁCH Y LỆNH / PHẪU THUẬT / CLS ---");
                foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("  [{0}] Type {1}: {2} | YL: {3} | Thực hiện: {4} | BS Y Lệnh: {5} | BS Thực hiện: {6} | STT: {7}",
                        r.INTRUCTION_TIME, r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_CODE, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_NAME, r.REQUEST_USERNAME, r.EXECUTE_USERNAME, r.SERVICE_REQ_STT_ID));
                }

                // Sere servs
                var ssFilter = new HisSereServViewFilter { TREATMENT_ID = t.ID };
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssFilter, param) ?? new List<V_HIS_SERE_SERV>();
                Console.WriteLine("\n  --- CHI TIẾT DỊCH VỤ & PHẪU THUẬT ---");
                foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
                {
                    Console.WriteLine(string.Format("  • [{0}] Type {1}: {2} (SL: {3}) | Phòng: {4} | ReqID: {5}",
                        s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_TYPE_ID, s.TDL_SERVICE_NAME, s.AMOUNT, s.EXECUTE_ROOM_NAME ?? s.REQUEST_ROOM_NAME, s.SERVICE_REQ_ID));
                }

                // Tracking
                var trackFilter = new HisTrackingViewFilter { TREATMENT_ID = t.ID };
                var tracks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trackFilter, param) ?? new List<V_HIS_TRACKING>();
                Console.WriteLine("\n  --- TỜ ĐIỀU TRỊ & DIỄN BIẾN ---");
                foreach (var tr in tracks.OrderBy(x => x.TRACKING_TIME))
                {
                    Console.WriteLine(string.Format("  [{0}] (BS: {1}):\n    Diễn biến: {2}\n    Xử trí: {3}",
                        tr.TRACKING_TIME, tr.CREATOR, tr.CONTENT, tr.CARE_INSTRUCTION));
                }

                // DHST
                var dhstFilter = new HisDhstViewFilter { TREATMENT_ID = t.ID };
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param) ?? new List<V_HIS_DHST>();
                Console.WriteLine("\n  --- DẤU HIỆU SINH TỒN ---");
                foreach (var h in dhsts.OrderBy(x => x.EXECUTE_TIME))
                {
                    Console.WriteLine(string.Format("  [{0}] Mạch: {1} | HA: {2}/{3} | Nhiệt: {4} | SpO2: {5} | Thở: {6}",
                        h.EXECUTE_TIME, h.PULSE, h.BLOOD_PRESSURE_MAX, h.BLOOD_PRESSURE_MIN, h.TEMPERATURE, h.SPO2, h.BREATH_RATE));
                }

                // Xét nghiệm máu
                var teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = t.ID };
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param) ?? new List<V_HIS_SERE_SERV_TEIN>();
                Console.WriteLine("\n  --- KẾT QUẢ XÉT NGHIỆM ---");
                foreach (var grp in teins.GroupBy(x => x.TEST_INDEX_NAME))
                {
                    var latest = grp.OrderByDescending(x => x.MODIFY_TIME ?? x.CREATE_TIME).FirstOrDefault();
                    if (latest != null && !string.IsNullOrEmpty(latest.VALUE))
                    {
                        Console.WriteLine(string.Format("    {0}: {1} {2} ({3})", latest.TEST_INDEX_NAME, latest.VALUE, latest.TEST_INDEX_UNIT_NAME, latest.MODIFY_TIME ?? latest.CREATE_TIME));
                    }
                }
            }
        }
    }
}
