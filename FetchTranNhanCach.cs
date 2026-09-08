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

namespace FetchTranNhanCach
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

            Run();
        }

        static string ReadLiveTokenFast()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string lp = Path.Combine(baseDir, "Logs", "LogSystem.txt");
            if (!File.Exists(lp)) return null;
            using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long len = fs.Length;
                int bufSize = (int)Math.Min(131072L, len);
                fs.Seek(len - bufSize, SeekOrigin.Begin);
                byte[] buf = new byte[bufSize];
                int read = fs.Read(buf, 0, bufSize);
                string chunk = Encoding.UTF8.GetString(buf, 0, read);
                int idx = chunk.LastIndexOf("TokenCode|");
                if (idx >= 0 && chunk.Length >= idx + 74) return chunk.Substring(idx + 10, 64);
            }
            return null;
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string token = ReadLiveTokenFast();
            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            MyAdapter adapter = new MyAdapter();

            string patientCode = "0003895280";

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.PATIENT_CODE__EXACT = patientCode;
            var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);

            if (treatments != null)
            {
                Console.WriteLine("TỔNG SỐ ĐỢT ĐIỀU TRỊ: " + treatments.Count);
                foreach (var t in treatments.OrderByDescending(x => x.IN_TIME))
                {
                    Console.WriteLine(string.Format("TREATMENT ID: {0} | Code: {1} | In: {2} | Out: {3} | ICD: {4} - {5}",
                        t.ID, t.TREATMENT_CODE, t.IN_TIME, t.OUT_TIME, t.ICD_CODE, t.ICD_NAME));
                }

                var curT = treatments.OrderByDescending(x => x.IN_TIME).First();
                Console.WriteLine("\n=== ĐỢT ĐIỀU TRỊ HIỆN TẠI: " + curT.ID + " ===");

                // DHST
                HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                dhstFilter.TREATMENT_ID = curT.ID;
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstFilter, param);
                if (dhsts != null)
                {
                    Console.WriteLine("\n--- TẤT CẢ DHST HIỆN TẠI ---");
                    foreach (var d in dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] Mạch: {1} | HA: {2}/{3} | NĐ: {4} | Cân nặng: {5} | Chiều cao: {6} | SpO2: {7}",
                            d.EXECUTE_TIME ?? d.CREATE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.WEIGHT, d.HEIGHT, d.SPO2));
                    }
                }

                // If empty vitals in curT, check previous treatments
                if (dhsts == null || !dhsts.Any(x => x.PULSE.HasValue || x.BLOOD_PRESSURE_MAX.HasValue))
                {
                    foreach (var pastT in treatments.Where(x => x.ID != curT.ID))
                    {
                        var pastDhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, new HisDhstViewFilter { TREATMENT_ID = pastT.ID }, param);
                        if (pastDhsts != null && pastDhsts.Any(x => x.PULSE.HasValue))
                        {
                            Console.WriteLine("\n--- DHST ĐỢT CŨ " + pastT.ID + " ---");
                            foreach (var d in pastDhsts.Where(x => x.PULSE.HasValue).OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).Take(2))
                            {
                                Console.WriteLine(string.Format("  [{0}] Mạch: {1} | HA: {2}/{3} | NĐ: {4} | Cân nặng: {5} | Chiều cao: {6}",
                                    d.EXECUTE_TIME ?? d.CREATE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.WEIGHT, d.HEIGHT));
                            }
                            break;
                        }
                    }
                }

                // Lab tests from curT
                HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                teinFilter.TDL_TREATMENT_ID = curT.ID;
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);
                if (teins != null)
                {
                    Console.WriteLine("\n--- LAB TESTS (TẤT CẢ - ĐỢT NÀY) ---");
                    foreach (var te in teins.Where(x => !string.IsNullOrEmpty(x.VALUE)))
                    {
                        Console.WriteLine(string.Format("  {0,-35} | {1,-15} | {2}", te.TEST_INDEX_NAME, te.VALUE, te.TEST_INDEX_UNIT_NAME));
                    }
                }

                // SereServ
                HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                ssFilter.TREATMENT_ID = curT.ID;
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                if (sss != null)
                {
                    Console.WriteLine("\n--- CLS & PTTT ---");
                    foreach (var s in sss.OrderByDescending(x => x.TDL_INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] {1} (Loại: {2}, Phòng: {3}, ID: {4})",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.TDL_SERVICE_TYPE_ID, s.REQUEST_ROOM_NAME, s.ID));
                    }
                }
            }
        }
    }
}
