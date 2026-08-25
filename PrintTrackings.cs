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

namespace PrintTrackings
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

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.PATIENT_CODE__EXACT = "0003985947";
            var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            if (treats == null || treats.Count == 0) return;

            long treatmentId = treats[0].ID;
            Console.WriteLine("Treatment ID: " + treatmentId);
            HisTrackingViewFilter tkf = new HisTrackingViewFilter();
            tkf.TREATMENT_ID = treatmentId;
            var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, param);
            if (trackings != null)
            {
                foreach (var tk in trackings.OrderBy(x => x.TRACKING_TIME))
                {
                    Console.WriteLine("=========================================================================");
                    Console.WriteLine("THỜI GIAN: " + tk.TRACKING_TIME);
                    Console.WriteLine("CHẨN ĐOÁN: " + tk.ICD_TEXT);
                    Console.WriteLine("DIỄN BIẾN LÂM SÀNG:\n" + tk.CONTENT);
                    Console.WriteLine("CHĂM SÓC:\n" + tk.CARE_INSTRUCTION);
                    Console.WriteLine("Y LỆNH:\n" + tk.MEDICAL_INSTRUCTION);
                }
            }
        }
    }
}
