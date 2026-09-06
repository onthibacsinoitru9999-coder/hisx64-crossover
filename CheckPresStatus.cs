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
using MOS.SDO;

namespace AutoPrescribeSpecific
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
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
                string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);
                return null;
            };

            Run();
        }

        static string GetLiveToken()
        {
            string[] paths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
                @"Logs\LogSystem.txt",
                @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
            };

            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    try
                    {
                        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string line;
                            string token = "";
                            while ((line = reader.ReadLine()) != null)
                            {
                                int idx = line.IndexOf("TokenCode|");
                                if (idx >= 0 && line.Length >= idx + 10 + 64)
                                {
                                    token = line.Substring(idx + 10, 64);
                                }
                            }
                            if (!string.IsNullOrEmpty(token)) return token;
                        }
                    }
                    catch { }
                }
            }
            return "";
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = GetLiveToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("ERROR: Token not found!");
                return;
            }

            ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            MyAdapter adapter = new MyAdapter();
            CommonParam cp = new CommonParam();

            long[] targetTreatmentIds = new long[]
            {
                7163469,
                7163700,
                7163879,
                7136089,
                7149475
            };

            Console.WriteLine("================================================================================");
            Console.WriteLine("KIEM TRA VA LAP KE HOACH KE DON NGAY 07/09/2026 VA 08/09/2026");
            Console.WriteLine("================================================================================");

            foreach (var tId in targetTreatmentIds)
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
                var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, cp);
                var tr = trList != null && trList.Count > 0 ? trList[0] : null;

                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("BN: {0} | Ma BN: {1} | TrID: {2} | ICD: [{3}] {4}",
                    tr != null ? tr.TDL_PATIENT_NAME : "Unknown",
                    tr != null ? tr.TDL_PATIENT_CODE : "",
                    tId,
                    tr != null ? tr.ICD_CODE : "",
                    tr != null ? tr.ICD_NAME : ""));

                HisExpMestViewFilter ef = new HisExpMestViewFilter { TDL_TREATMENT_ID = tId };
                var mests = adapter.FetchList<V_HIS_EXP_MEST>("api/HisExpMest/GetView", mosConsumer, ef, cp);
                if (mests != null)
                {
                    var mests07 = mests.Where(m => m.TDL_INTRUCTION_TIME.ToString().StartsWith("20260907")).ToList();
                    var mests08 = mests.Where(m => m.TDL_INTRUCTION_TIME.ToString().StartsWith("20260908")).ToList();

                    Console.WriteLine(string.Format("  - Don ngay 07/09: {0} don", mests07.Count));
                    foreach (var m in mests07)
                    {
                        Console.WriteLine(string.Format("    + {0} | Kho: {1} ({2}) | Time: {3}", m.EXP_MEST_CODE, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.TDL_INTRUCTION_TIME));
                    }

                    Console.WriteLine(string.Format("  - Don ngay 08/09: {0} don", mests08.Count));
                    foreach (var m in mests08)
                    {
                        Console.WriteLine(string.Format("    + {0} | Kho: {1} ({2}) | Time: {3}", m.EXP_MEST_CODE, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.TDL_INTRUCTION_TIME));
                    }
                }

                HisTrackingViewFilter tkf = new HisTrackingViewFilter { TREATMENT_ID = tId };
                var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, tkf, cp);
                if (trackings != null)
                {
                    var tk07 = trackings.FirstOrDefault(x => x.TRACKING_TIME.ToString().StartsWith("20260907"));
                    var tk08 = trackings.FirstOrDefault(x => x.TRACKING_TIME.ToString().StartsWith("20260908"));
                    Console.WriteLine(string.Format("  - To dieu tri 07/09: {0}", tk07 != null ? ("ID " + tk07.ID + " (" + tk07.TRACKING_TIME + ")") : "Chua co"));
                    Console.WriteLine(string.Format("  - To dieu tri 08/09: {0}", tk08 != null ? ("ID " + tk08.ID + " (" + tk08.TRACKING_TIME + ")") : "Chua co"));
                }
            }
        }
    }
}