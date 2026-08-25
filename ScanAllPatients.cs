using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace ScanAllPatients
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

            // 1. Quét tất cả BN đang nằm phòng toàn viện / khoa 57
            HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);

            Console.WriteLine("Tổng số BN đang nằm viện (IS_IN_ROOM=true): " + (inPatients != null ? inPatients.Count : 0));
            if (inPatients != null)
            {
                var dept57 = inPatients.Where(x => x.DEPARTMENT_ID == 57).ToList();
                Console.WriteLine("Tổng số BN Khoa CTCH (Khoa 57): " + dept57.Count);
                foreach (var p in dept57)
                {
                    Console.WriteLine(string.Format("• [{0}] Giường: {1,-8} | BN: {2,-25} | Mã BN: {3} | Mã BA: {4} | TreatmentID: {5}",
                        p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));
                }

                Console.WriteLine("\n--- TÌM KIẾM TÊN TRONG TOÀN VIỆN ---");
                string[] searchNames = new string[] { "QUANG", "HIẾU", "HIEU", "HOA", "HẰNG", "HANG", "THU", "CHINH" };
                foreach (var s in searchNames)
                {
                    var matches = inPatients.Where(x => x.TDL_PATIENT_NAME != null && x.TDL_PATIENT_NAME.ToUpper().Contains(s)).ToList();
                    Console.WriteLine(string.Format("Từ khóa '{0}': tìm thấy {1} BN", s, matches.Count));
                    foreach (var m in matches)
                    {
                        Console.WriteLine(string.Format("   -> [{0} - Khoa {1}] {2,-25} ({3}) | Giường: {4} | Mã BA: {5}",
                            m.BED_ROOM_NAME, m.DEPARTMENT_ID, m.TDL_PATIENT_NAME, m.TDL_PATIENT_CODE, m.BED_NAME, m.TREATMENT_CODE));
                    }
                }
            }

            // 2. Thử tìm kiếm theo Patient Name trong HisPatient
            Console.WriteLine("\n--- TÌM TRONG DANH SÁCH BỆNH NHÂN (HisPatient) ---");
            string[] fullNames = new string[] {
                "NGUYỄN ANH QUANG",
                "PHẠM THỊ HIẾU",
                "NGUYỄN THỊ HOA",
                "NGUYỄN THỊ HẰNG",
                "TRẦN THỊ THU",
                "TRẦN THỊ CHINH"
            };

            foreach (var fn in fullNames)
            {
                HisPatientViewFilter pf = new HisPatientViewFilter();
                pf.KEY_WORD = fn;
                var pts = adapter.FetchList<V_HIS_PATIENT>("api/HisPatient/GetView", mosConsumer, pf, param);
                Console.WriteLine(string.Format("Patient search '{0}': {1} kết quả", fn, pts != null ? pts.Count : 0));
                if (pts != null && pts.Count > 0)
                {
                    foreach (var pt in pts.Take(3))
                    {
                        Console.WriteLine(string.Format("   BN: {0} | Mã: {1} | DOB: {2} | Địa chỉ: {3}",
                            pt.VIR_PATIENT_NAME, pt.PATIENT_CODE, pt.DOB, pt.VIR_ADDRESS));
                    }
                }
            }
        }
    }
}
