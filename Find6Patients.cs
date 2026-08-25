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

namespace Find6Patients
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
            CommonParam param = new CommonParam();
            Load.Init();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (token == null)
            {
                Console.WriteLine("LOGIN VMC FAILED!");
                return;
            }
            Console.WriteLine("LOGIN SUCCESS! Token: " + token.TokenCode.Substring(0, 10) + "...");
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            // 1. Lấy tất cả buồng bệnh khoa 57
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
            bf.DEPARTMENT_ID = 57;
            var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
            Console.WriteLine("Khoa 57 có " + (bList != null ? bList.Count : 0) + " buồng bệnh.");

            List<long> bedRoomIds = (bList != null) ? bList.Select(x => x.ID).ToList() : new List<long>();

            // 2. Lấy toàn bộ bệnh nhân đang nằm viện tại Khoa 57
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
            tbrf.BED_ROOM_IDs = bedRoomIds;
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

            Console.WriteLine("Tổng số bệnh nhân đang nằm trong phòng Khoa 57: " + (inPatients != null ? inPatients.Count : 0));
            if (inPatients != null)
            {
                foreach (var p in inPatients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    Console.WriteLine(string.Format("• [{0}] {1,-10} | BN: {2,-25} | Mã BN: {3} | Mã BA: {4} | TreatmentId: {5}",
                        p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));
                }
            }

            // 3. Tra cứu 6 bệnh nhân
            Console.WriteLine("\n--- TRA CỨU 6 BỆNH NHÂN TRONG DANH SÁCH ---");
            string[] targets = new string[] {
                "NGUYỄN ANH QUANG",
                "PHẠM THỊ HIẾU",
                "NGUYỄN THỊ HOA",
                "NGUYỄN THỊ HẰNG",
                "TRẦN THỊ THU",
                "TRẦN THỊ CHINH"
            };

            foreach (var t in targets)
            {
                Console.WriteLine("\n=======================================================");
                Console.WriteLine("TÌM KIẾM: " + t);
                Console.WriteLine("=======================================================");

                // Tìm trong inPatients
                if (inPatients != null)
                {
                    var inMatch = inPatients.FirstOrDefault(x => x.TDL_PATIENT_NAME != null && x.TDL_PATIENT_NAME.ToUpper().Contains(t.ToUpper()));
                    if (inMatch != null)
                    {
                        Console.WriteLine(string.Format("-> [ĐANG NẰM VIỆN] {0} ({1}) | Buồng: {2} | Giường: {3} | Mã BA: {4} | TreatmentId: {5}",
                            inMatch.TDL_PATIENT_NAME, inMatch.TDL_PATIENT_CODE, inMatch.BED_ROOM_NAME, inMatch.BED_NAME, inMatch.TREATMENT_CODE, inMatch.TREATMENT_ID));
                    }
                }

                // Tìm qua HisTreatment
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = t;
                var foundTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                if (foundTreats != null && foundTreats.Count > 0)
                {
                    foreach (var f in foundTreats.OrderByDescending(x => x.IN_TIME).Take(3))
                    {
                        Console.WriteLine(string.Format("   -> [HỒ SƠ BA] {0} | Mã BN: {1} | Mã BA: {2} | Vào: {3} | Ra: {4} | Trạng thái: {5} | ID: {6}",
                            f.TDL_PATIENT_NAME, f.TDL_PATIENT_CODE, f.TREATMENT_CODE, f.IN_TIME, f.OUT_TIME,
                            (f.IS_PAUSE == 1 || f.OUT_TIME.HasValue) ? "Đã ra viện" : "Đang điều trị", f.ID));
                    }
                }
                else
                {
                    Console.WriteLine("   Không tìm thấy hồ sơ qua KEY_WORD trong HisTreatment.");
                }
            }
        }
    }
}
