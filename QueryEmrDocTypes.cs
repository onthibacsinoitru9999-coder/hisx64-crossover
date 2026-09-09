using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using EMR.EFMODEL.DataModels;
using EMR.Filter;

namespace EmrStudy
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
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR", new AssemblyName(a.Name).Name + ".dll");
                if (File.Exists(p1)) return Assembly.LoadFrom(p1);
                string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                if (File.Exists(p2)) return Assembly.LoadFrom(p2);
                return null;
            };
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string logPath = @"e:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
            string token = null;
            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string text = sr.ReadToEnd();
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].Contains("TokenCode|"))
                    {
                        token = lines[i].Substring(lines[i].IndexOf("TokenCode|") + 10, 64);
                        break;
                    }
                }
            }

            var emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", token, "HIS");
            var adapter = new MyAdapter();

            Console.WriteLine("=== DANH SÁCH CÁC LOẠI VĂN BẢN / BỆNH ÁN EMR ===");
            try
            {
                var docTypes = adapter.FetchList<EMR_DOCUMENT_TYPE>("api/EmrDocumentType/Get", emrConsumer, new EmrDocumentTypeFilter(), param);
                if (docTypes != null)
                {
                    foreach (var dt in docTypes)
                    {
                        Console.WriteLine(string.Format("ID: {0,4} | Code: {1,-18} | Name: {2}", dt.ID, dt.DOCUMENT_TYPE_CODE, dt.DOCUMENT_TYPE_NAME));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
        }
    }
}
