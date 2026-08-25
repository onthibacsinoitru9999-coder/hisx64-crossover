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

namespace ShiftInspector
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

            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string tokenCode = "";

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
            if (!File.Exists(logPath))
            {
                string[] possiblePaths = new string[] {
                    @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                    @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
                };
                foreach (var p in possiblePaths)
                {
                    if (File.Exists(p)) { logPath = p; break; }
                }
            }

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
                                Console.WriteLine("Đã lấy Live Token từ LogSystem.txt: " + tokenCode.Substring(0, 10) + "...");
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                Console.WriteLine("KHÔNG TÌM THẤY LIVE TOKEN TRONG LOG SYSTEM!");
                return;
            }

            ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode);
            MyAdapter adapter = new MyAdapter();

            Console.WriteLine("\n=== 1. ALL DEPARTMENT TRANS INTO DEPT 57 (20260824070000 -> 20260825030500) ===");
            try
            {
                HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                dtf.DEPARTMENT_ID = 57;
                var dtList = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);
                Console.WriteLine("Total DeptTrans for dept 57: " + (dtList != null ? dtList.Count : 0));
                if (dtList != null)
                {
                    var recent = dtList.Where(x => (x.DEPARTMENT_IN_TIME >= 20260824070000 && x.DEPARTMENT_IN_TIME <= 20260825030500) ||
                                                   (x.CREATE_TIME >= 20260824070000 && x.CREATE_TIME <= 20260825030500)).OrderBy(x => x.DEPARTMENT_IN_TIME).ToList();
                    Console.WriteLine("Recent DeptTrans into Dept 57: " + recent.Count);
                    foreach (var d in recent)
                    {
                        Console.WriteLine(string.Format("ID: {0} | TreatmentID: {1} | BN: {2} | InTime: {3} | CreateTime: {4} | PrevDept: {5} -> Dept: {6}",
                            d.ID, d.TREATMENT_ID, d.TDL_PATIENT_NAME, d.DEPARTMENT_IN_TIME, d.CREATE_TIME, d.PREVIOUS_DEPARTMENT_NAME, d.DEPARTMENT_NAME));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeptTran Error: " + ex.ToString());
            }

            Console.WriteLine("\n=== 2. ALL RECENT TREATMENTS WITH IN_TIME (24/8 7h - 25/8 3h) ===");
            try
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.IN_TIME_FROM = 20260824070000;
                tf.IN_TIME_TO = 20260825030500;
                var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                Console.WriteLine("Total Treatments with IN_TIME in window: " + (tList != null ? tList.Count : 0));
                if (tList != null)
                {
                    foreach (var t in tList.Where(x => x.LAST_DEPARTMENT_ID == 57 || x.IN_DEPARTMENT_ID == 57 || x.END_DEPARTMENT_ID == 57))
                    {
                        Console.WriteLine(string.Format("TreatmentID: {0} | Code: {1} | BN: {2} | InTime: {3} | InDeptID: {4} | LastDeptID: {5} | ICD: {6} - {7} | BHYT: {8}",
                            t.ID, t.TREATMENT_CODE, t.TDL_PATIENT_NAME, t.IN_TIME, t.IN_DEPARTMENT_ID, t.LAST_DEPARTMENT_ID, t.IN_ICD_CODE, t.IN_ICD_NAME, t.TDL_HEIN_CARD_NUMBER));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Treatment Error: " + ex.ToString());
            }

            Console.WriteLine("\n=== 3. ALL SURGERIES / PTTT ON 2026-08-24 -> 2026-08-25 ===");
            try
            {
                HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                srf.SERVICE_REQ_TYPE_ID = 6; // PTTT
                srf.INTRUCTION_TIME_FROM = 20260824000000;
                srf.INTRUCTION_TIME_TO = 20260825030500;
                var srList = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                Console.WriteLine("Total ServiceReq PTTT: " + (srList != null ? srList.Count : 0));
                if (srList != null)
                {
                    foreach (var sr in srList.OrderBy(x => x.INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("ReqID: {0} | TreatmentID: {1} | BN: {2} | ReqDept: {3} | ExecRoom: {4} | Time: {5} | Stt: {6} | Icd: {7}",
                            sr.ID, sr.TREATMENT_ID, sr.TDL_PATIENT_NAME, sr.REQUEST_DEPARTMENT_NAME, sr.EXECUTE_ROOM_NAME, sr.INTRUCTION_TIME, sr.SERVICE_REQ_STT_NAME, sr.ICD_NAME));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ServiceReq Error: " + ex.ToString());
            }

            Console.WriteLine("\n=== 4. ALL EXP MESTS (XUAT KHO / MAU) (24/8 - 25/8) ===");
            try
            {
                HisExpMestViewFilter emf = new HisExpMestViewFilter();
                emf.CREATE_TIME_FROM = 20260824000000;
                emf.CREATE_TIME_TO = 20260825030500;
                var emList = adapter.FetchList<V_HIS_EXP_MEST>("api/HisExpMest/GetView", mosConsumer, emf, param);
                Console.WriteLine("Total ExpMests: " + (emList != null ? emList.Count : 0));
                if (emList != null)
                {
                    foreach (var em in emList)
                    {
                        Console.WriteLine(string.Format("ExpID: {0} | Code: {1} | Type: {2} ({3}) | Dept: {4} | Stock: {5} | Time: {6} | TreatmentID: {7}",
                            em.ID, em.EXP_MEST_CODE, em.EXP_MEST_TYPE_ID, em.EXP_MEST_TYPE_NAME, em.REQ_DEPARTMENT_NAME, em.MEDI_STOCK_NAME, em.CREATE_TIME, em.TDL_TREATMENT_ID));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExpMest Error: " + ex.ToString());
            }
        }
    }
}
