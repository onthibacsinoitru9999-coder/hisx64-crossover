using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace DeepShiftScanner
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
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

            RunScan();
        }

        static void RunScan()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string tokenCode = "";

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
            if (File.Exists(logPath))
            {
                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string text = sr.ReadToEnd();
                    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        if (lines[i].Contains("TokenCode|"))
                        {
                            int idx = lines[i].IndexOf("TokenCode|") + 10;
                            if (lines[i].Length >= idx + 64)
                            {
                                tokenCode = lines[i].Substring(idx, 64).Trim();
                                break;
                            }
                        }
                    }
                }
            }

            ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode);
            MyAdapter adapter = new MyAdapter();

            Console.WriteLine("==========================================================================");
            Console.WriteLine("1. TOÀN BỘ BỆNH NHÂN ĐANG NẰM KHOA 57 (CTCH & CỘT SỐNG)");
            Console.WriteLine("==========================================================================");

            HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
            List<V_HIS_TREATMENT_BED_ROOM> dept57Patients = new List<V_HIS_TREATMENT_BED_ROOM>();
            if (inPatients != null)
            {
                dept57Patients = inPatients.Where(x => x.DEPARTMENT_ID == 57).ToList();
                Console.WriteLine("Tổng số bệnh nhân đang nằm Khoa 57: " + dept57Patients.Count);
                foreach (var p in dept57Patients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    Console.WriteLine(string.Format("[{0} - {1}] BN: {2,-25} | Mã BN: {3} | Mã BA: {4} | TreatmentID: {5} | AddTime: {6}",
                        p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID, p.ADD_TIME));
                }
            }

            // Lấy danh sách tất cả treatmentId của khoa 57
            List<long> treatmentIds = dept57Patients.Select(x => x.TREATMENT_ID).Distinct().ToList();

            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("2. KIỂM TRA BỆNH NHÂN VÀO KHOA TỪ 07h 24/08/2026 ĐẾN 03h 25/08/2026");
            Console.WriteLine("==========================================================================");

            // Truy vấn Treatment và DepartmentTran của từng BN
            foreach (var tId in treatmentIds)
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.ID = tId;
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                var t = treats != null && treats.Count > 0 ? treats[0] : null;

                HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                dtf.TREATMENT_ID = tId;
                var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);

                if (t != null && dts != null)
                {
                    // Tìm lần chuyển vào khoa 57 gần nhất
                    var transInto57 = dts.Where(x => x.DEPARTMENT_ID == 57).OrderByDescending(x => x.DEPARTMENT_IN_TIME ?? x.CREATE_TIME).FirstOrDefault();
                    if (transInto57 != null)
                    {
                        long inTime = transInto57.DEPARTMENT_IN_TIME ?? transInto57.CREATE_TIME ?? 0;
                        if (inTime >= 20260824070000 && inTime <= 20260825030500)
                        {
                            Console.WriteLine(string.Format(">>> [VÀO KHOA 24-25/8] BN: {0} | InTime: {1} | PrevDept: {2} | Chẩn đoán vào: {3} | Bác sĩ nhận: {4}",
                                t.TDL_PATIENT_NAME, inTime, transInto57.PREVIOUS_DEPARTMENT_NAME, t.IN_ICD_NAME ?? t.ICD_NAME, transInto57.CREATOR));
                        }
                    }
                    // Kiểm tra cả IN_TIME của Treatment
                    if (t.IN_TIME >= 20260824070000 && t.IN_TIME <= 20260825030500)
                    {
                        Console.WriteLine(string.Format(">>> [TREATMENT IN_TIME 24-25/8] BN: {0} | InTime: {1} | InDept: {2} | Chẩn đoán: {3}",
                            t.TDL_PATIENT_NAME, t.IN_TIME, t.IN_DEPARTMENT_ID, t.IN_ICD_NAME));
                    }
                }
            }

            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("3. TẤT CẢ LỊCH SỬ CHUYỂN KHOA (DEPARTMENT_TRAN) GẦN NHẤT CỦA CÁC BN KHOA 57");
            Console.WriteLine("==========================================================================");
            foreach (var tId in treatmentIds)
            {
                HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                dtf.TREATMENT_ID = tId;
                var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);
                if (dts != null)
                {
                    foreach (var dt in dts)
                    {
                        Console.WriteLine(string.Format("TreatmentID: {0} | BN: {1} | InTime: {2} | Dept: {3} (ID: {4}) | PrevDept: {5} | Creator: {6}",
                            dt.TREATMENT_ID, dt.TDL_PATIENT_NAME, dt.DEPARTMENT_IN_TIME, dt.DEPARTMENT_NAME, dt.DEPARTMENT_ID, dt.PREVIOUS_DEPARTMENT_NAME, dt.CREATOR));
                    }
                }
            }
        }
    }
}
