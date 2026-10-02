using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class Program
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

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token").Split('|')[0].Trim();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "MOS");
        var myAdapter = new MyAdapter();
        var param = new CommonParam();

        string[] reqCodes = new string[] { "000092480474", "000092480480", "000092480482", "000092480485", "000092482131" };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("TRẠNG THÁI CÁC PHIẾU ĐÃ KÊ LÚC 22:48 - 23:39 ĐÊM QUA:");
        Console.WriteLine("===============================================================================");

        foreach (var code in reqCodes)
        {
            var srf = new HisServiceReqViewFilter { SERVICE_REQ_CODE = code };
            var list = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (list != null && list.Count > 0)
            {
                var r = list[0];
                string stt = r.SERVICE_REQ_STT_NAME;
                bool canCancel = (r.SERVICE_REQ_STT_ID == 1 && r.IS_DELETE != 1);
                Console.WriteLine(string.Format("• Phiếu [{0}] (ID: {1}) | BN: {2} | TT: {3} | Có thể hủy: {4}",
                    r.SERVICE_REQ_CODE, r.ID, r.TDL_PATIENT_NAME, stt, canCancel ? "CÓ (Màu trắng)" : "KHÔNG (Đã thực hiện/đã hủy)"));
            }
        }
    }
}
