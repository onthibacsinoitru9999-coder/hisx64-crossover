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

namespace FindTruong2026
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
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            Console.WriteLine("=== 1. TÌM TRONG DANH SÁCH BỆNH NHÂN NỘI TRÚ TOÀN VIỆN ĐANG ĐIỀU TRỊ ===");
            var tbrf = new HisTreatmentBedRoomViewFilter { TREATMENT_IS_ACTIVE = true };
            var allBeds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param) ?? new List<V_HIS_TREATMENT_BED_ROOM>();
            Console.WriteLine("Tổng số BN nội trú toàn viện: " + allBeds.Count);

            var matched = allBeds.Where(x => x.TDL_PATIENT_NAME != null && x.TDL_PATIENT_NAME.ToUpper().Contains("TRƯỜNG")).ToList();
            Console.WriteLine("BN có tên chứa TRƯỜNG: " + matched.Count);
            foreach (var m in matched)
            {
                Console.WriteLine(string.Format("  • [{0}] Mã BN: {1} | Mã BA: {2} | DOB: {3} | Khoa ID: {4} | Buồng: {5} ({6})",
                    m.TDL_PATIENT_NAME, m.TDL_PATIENT_CODE, m.TREATMENT_CODE, m.TDL_PATIENT_DOB, m.DEPARTMENT_ID, m.BED_ROOM_NAME, m.BED_NAME));
            }

            Console.WriteLine("\n=== 2. TÌM TRONG DANH SÁCH Y LỆNH PHẪU THUẬT / DỊCH VỤ NGÀY 24/8 ĐẾN 25/8 ===");
            var srf = new HisServiceReqViewFilter
            {
                INTRUCTION_TIME_FROM = 20260824000000,
                INTRUCTION_TIME_TO = 20260825235959
            };
            var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param) ?? new List<V_HIS_SERVICE_REQ>();
            Console.WriteLine("Tổng số Y lệnh trong ngày 24-25/8: " + reqs.Count);

            var truongReqs = reqs.Where(x => x.TDL_PATIENT_NAME != null && x.TDL_PATIENT_NAME.ToUpper().Contains("TRƯỜNG")).ToList();
            Console.WriteLine("Y lệnh của BN có tên TRƯỜNG: " + truongReqs.Count);
            foreach (var r in truongReqs)
            {
                Console.WriteLine(string.Format("  • BN: {0} (Mã BN: {1}, Mã BA: {2}) | Y lệnh: {3} | Lúc: {4} | YL: {5} -> Thực hiện: {6} | BS: {7}",
                    r.TDL_PATIENT_NAME, r.TDL_PATIENT_CODE, r.TREATMENT_CODE, r.SERVICE_REQ_TYPE_NAME, r.INTRUCTION_TIME, r.REQUEST_ROOM_NAME, r.EXECUTE_ROOM_NAME, r.REQUEST_USERNAME));
            }

            // Also check for BS Lê Văn Luân / luanlv / lvl
            var luanReqs = reqs.Where(x => (x.REQUEST_USERNAME != null && (x.REQUEST_USERNAME.ToUpper().Contains("LUÂN") || x.REQUEST_USERNAME.ToUpper().Contains("LVL") || x.REQUEST_USERNAME.ToUpper().Contains("LUANLV")))
                                        || (x.EXECUTE_USERNAME != null && (x.EXECUTE_USERNAME.ToUpper().Contains("LUÂN") || x.EXECUTE_USERNAME.ToUpper().Contains("LVL")))).ToList();
            Console.WriteLine("\nY lệnh liên quan BS Luân trong ngày 24-25/8: " + luanReqs.Count);
            foreach (var r in luanReqs.Take(15))
            {
                Console.WriteLine(string.Format("  • BN: {0} (Mã BA: {1}) | Dịch vụ: {2} | Phòng TH: {3} | BS YL: {4} | BS TH: {5}",
                    r.TDL_PATIENT_NAME, r.TREATMENT_CODE, r.SERVICE_REQ_TYPE_NAME, r.EXECUTE_ROOM_NAME, r.REQUEST_USERNAME, r.EXECUTE_USERNAME));
            }
        }
    }
}
