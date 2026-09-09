using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.EFMODEL.DataModels;

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
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
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

            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var emrConsumer = new ApiConsumer("http://192.168.7.239:1415/", token, "HIS");
            var adapter = new MyAdapter();

            Console.WriteLine("=== 1. TÌM KIẾM DANH MỤC LOẠI VĂN BẢN EMR (EMR DOCUMENT TYPES) ===");
            try
            {
                var docTypes = adapter.FetchList<EMR.EFMODEL.DataModels.EMR_DOCUMENT_TYPE>("api/EmrDocumentType/Get", emrConsumer, new EMR.Filter.EmrDocumentTypeFilter(), param);
                if (docTypes != null)
                {
                    foreach (var dt in docTypes)
                    {
                        if (dt.DOCUMENT_TYPE_NAME.Contains("Bệnh án") || dt.DOCUMENT_TYPE_NAME.Contains("Ngoại") || dt.DOCUMENT_TYPE_NAME.Contains("bìa") || dt.DOCUMENT_TYPE_NAME.Contains("Vào viện"))
                        {
                            Console.WriteLine(string.Format("ID: {0,4} | Code: {1,-15} | Name: {2}", dt.ID, dt.DOCUMENT_TYPE_CODE, dt.DOCUMENT_TYPE_NAME));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi EmrDocumentType: " + ex.Message);
            }

            Console.WriteLine("\n=== 2. KIỂM TRA HỒ SƠ EMR CỦA BỆNH NHÂN VÀO VIỆN (MAI VĂN KINH - 7133268) ===");
            try
            {
                EMR.Filter.EmrDocumentFilter docFilter = new EMR.Filter.EmrDocumentFilter { TREATMENT_CODE = "000007133452" };
                var docs = adapter.FetchList<EMR.EFMODEL.DataModels.EMR_DOCUMENT>("api/EmrDocument/Get", emrConsumer, docFilter, param);
                if (docs != null)
                {
                    Console.WriteLine(string.Format("Tìm thấy {0} văn bản EMR:", docs.Count));
                    foreach (var d in docs)
                    {
                        Console.WriteLine(string.Format("ID: {0,6} | Type: {1,4} | Code: {2,-15} | Name: {3} | Url: {4}", 
                            d.ID, d.DOCUMENT_TYPE_ID, d.DOCUMENT_CODE, d.DOCUMENT_NAME, d.DOCUMENT_URL));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi EmrDocument: " + ex.Message);
            }
        }
    }
}
