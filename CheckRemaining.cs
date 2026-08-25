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

namespace CheckRemaining
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

            string[] names = new string[] { "Trần Thị Vi", "Vũ Minh Phúc", "Lưu Văn Khoa" };
            foreach (var n in names)
            {
                Console.WriteLine(">>> TÌM: " + n);
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = n;
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                if (treats != null && treats.Count > 0)
                {
                    foreach (var tr in treats.OrderByDescending(x => x.IN_TIME).Take(2))
                    {
                        Console.WriteLine(string.Format("   BN: {0} | Mã BN: {1} | Mã BA: {2} | Khoa: {3} | Vào: {4} | Ra: {5}",
                            tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.END_DEPARTMENT_NAME, tr.IN_TIME, tr.OUT_TIME));
                    }
                }
                else
                {
                    Console.WriteLine("   Không tìm thấy qua KEY_WORD");
                }
            }
        }
    }
}
