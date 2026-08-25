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

namespace QueryTruongFast
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
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            Console.WriteLine("1. Querying Patient table for NGUYỄN THẾ TRƯỜNG...");
            var patFilter = new HisPatientViewFilter();
            patFilter.KEY_WORD = "NGUYỄN THẾ TRƯỜNG";
            var pats = adapter.FetchList<V_HIS_PATIENT>("api/HisPatient/GetView", ApiConsumers.MosConsumer, patFilter, param) ?? new List<V_HIS_PATIENT>();
            Console.WriteLine("Found patients: " + pats.Count);

            foreach (var p in pats)
            {
                Console.WriteLine(string.Format("Patient: {0} | Code: {1} | DOB: {2} | Gender: {3} | Address: {4}",
                    p.VIR_PATIENT_NAME, p.PATIENT_CODE, p.DOB, p.GENDER_NAME, p.VIR_ADDRESS));

                var treatFilter = new HisTreatmentViewFilter();
                treatFilter.PATIENT_ID = p.ID;
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, treatFilter, param) ?? new List<V_HIS_TREATMENT>();
                Console.WriteLine("  Found treatments: " + treats.Count);

                foreach (var t in treats.OrderByDescending(x => x.IN_TIME))
                {
                    Console.WriteLine(string.Format("\n=== TREATMENT: {0} | Mã BA: {1} | Vào viện: {2} | Khoa cuối: {3} | Active: {4} ===",
                        t.TREATMENT_CODE, t.TREATMENT_CODE, t.IN_TIME, t.LAST_DEPARTMENT_ID, t.IS_ACTIVE));
                    Console.WriteLine(string.Format("  • Chẩn đoán: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));

                    // Bed room
                    var bf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = t.ID };
                    var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, bf, param) ?? new List<V_HIS_TREATMENT_BED_ROOM>();
                    foreach (var b in beds.OrderByDescending(x => x.ADD_TIME))
                    {
                        Console.WriteLine(string.Format("  • Buồng giường: {0} - {1} | Khoa ID: {2} | Vào: {3} -> Ra: {4}",
                            b.BED_ROOM_NAME, b.BED_NAME, b.DEPARTMENT_ID, b.ADD_TIME, b.REMOVE_TIME));
                    }

                    // Department tran
                    var df = new HisDepartmentTranViewFilter { TREATMENT_ID = t.ID };
                    var deps = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", ApiConsumers.MosConsumer, df, param) ?? new List<V_HIS_DEPARTMENT_TRAN>();
                    foreach (var d in deps.OrderBy(x => x.DEPARTMENT_IN_TIME))
                    {
                        Console.WriteLine(string.Format("  • Khoa: {0} (ID: {1}) lúc {2} | Từ: {3}", d.DEPARTMENT_NAME, d.DEPARTMENT_ID, d.DEPARTMENT_IN_TIME, d.PREVIOUS_DEPARTMENT_NAME));
                    }

                    // Service reqs
                    var srf = new HisServiceReqViewFilter { TREATMENT_ID = t.ID };
                    var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param) ?? new List<V_HIS_SERVICE_REQ>();
                    Console.WriteLine("\n  --- CÁC Y LỆNH / PHẪU THUẬT / CLS ---");
                    foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] Type {1}: {2} | YL: {3} | Thực hiện: {4} | BS YL: {5} | BS Thực hiện: {6} | STT: {7}",
                            r.INTRUCTION_TIME, r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_CODE, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_NAME, r.REQUEST_USERNAME, r.EXECUTE_USERNAME, r.SERVICE_REQ_STT_ID));
                    }

                    // Sere servs
                    var ssf = new HisSereServViewFilter { TREATMENT_ID = t.ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param) ?? new List<V_HIS_SERE_SERV>();
                    Console.WriteLine("\n  --- CHI TIẾT DỊCH VỤ & PHẪU THUẬT ---");
                    foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("  • [{0}] Type {1}: {2} (SL: {3}) | Phòng: {4}",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_TYPE_ID, s.TDL_SERVICE_NAME, s.AMOUNT, s.EXECUTE_ROOM_NAME ?? s.REQUEST_ROOM_NAME));
                    }

                    // Tracking
                    var trf = new HisTrackingViewFilter { TREATMENT_ID = t.ID };
                    var tracks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trf, param) ?? new List<V_HIS_TRACKING>();
                    Console.WriteLine("\n  --- TỜ ĐIỀU TRỊ & DIỄN BIẾN ---");
                    foreach (var tr in tracks.OrderBy(x => x.TRACKING_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] (BS: {1}):\n    Diễn biến: {2}\n    Chăm sóc: {3}",
                            tr.TRACKING_TIME, tr.CREATOR, tr.CONTENT, tr.CARE_INSTRUCTION));
                    }

                    // DHST
                    var dhstf = new HisDhstViewFilter { TREATMENT_ID = t.ID };
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstf, param) ?? new List<V_HIS_DHST>();
                    Console.WriteLine("\n  --- DẤU HIỆU SINH TỒN ---");
                    foreach (var h in dhsts.OrderBy(x => x.EXECUTE_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] Mạch: {1} | HA: {2}/{3} | Nhiệt: {4} | SpO2: {5} | Thở: {6}",
                            h.EXECUTE_TIME, h.PULSE, h.BLOOD_PRESSURE_MAX, h.BLOOD_PRESSURE_MIN, h.TEMPERATURE, h.SPO2, h.BREATH_RATE));
                    }

                    // Xét nghiệm máu
                    var teinf = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = t.ID };
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinf, param) ?? new List<V_HIS_SERE_SERV_TEIN>();
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
}
