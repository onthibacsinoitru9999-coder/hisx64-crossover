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

namespace CheckAllVisitsBN12
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
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            string[] pCodes = new string[] { "0003972576", "0003972296" }; // Quang, Hiếu

            foreach (var pc in pCodes)
            {
                Console.WriteLine("=================================================================");
                Console.WriteLine("TRA CỨU TẤT CẢ CÁC LẦN KHÁM CỦA MÃ BN: " + pc);
                Console.WriteLine("=================================================================");

                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.PATIENT_CODE__EXACT = pc;
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                if (treats != null)
                {
                    foreach (var tr in treats.OrderByDescending(x => x.IN_TIME))
                    {
                        Console.WriteLine(string.Format("Lần khám: {0} | Vào: {1} | Ra: {2} | Khoa: {3} | CD: {4} | ID: {5}",
                            tr.TREATMENT_CODE, tr.IN_TIME, tr.OUT_TIME, tr.END_DEPARTMENT_NAME ?? "", tr.ICD_NAME, tr.ID));

                        // Lấy xét nghiệm của từng lần khám
                        HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                        teinFilter.TDL_TREATMENT_ID = tr.ID;
                        var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinFilter, param);
                        if (teins != null && teins.Count > 0)
                        {
                            Console.WriteLine("   -> Có " + teins.Count + " kết quả xét nghiệm.");
                            var validTeins = teins.Where(x => !string.IsNullOrEmpty(x.VALUE)).ToList();
                            foreach (var t in validTeins)
                            {
                                Console.WriteLine(string.Format("      • {0} ({1}): {2} {3}", t.TEST_INDEX_NAME, t.TEST_INDEX_CODE, t.VALUE, t.TEST_INDEX_UNIT_NAME));
                            }
                        }

                        // Lấy CLS / Hình ảnh
                        HisSereServViewFilter ssf = new HisSereServViewFilter();
                        ssf.TREATMENT_ID = tr.ID;
                        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);
                        if (sss != null && sss.Count > 0)
                        {
                            var cls = sss.Where(x => x.TDL_SERVICE_TYPE_ID == 2 || x.TDL_SERVICE_TYPE_ID == 3 || x.TDL_SERVICE_TYPE_ID == 4 || x.TDL_SERVICE_TYPE_ID == 8).ToList();
                            Console.WriteLine("   -> Có " + cls.Count + " chỉ định CLS/CĐHA:");
                            foreach (var c in cls)
                            {
                                Console.WriteLine(string.Format("      - [{0}] {1} ({2})", c.TDL_INTRUCTION_TIME, c.TDL_SERVICE_NAME, c.SERVICE_TYPE_NAME));
                            }
                        }
                    }
                }
            }
        }
    }
}
