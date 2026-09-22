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

class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = e.Name.Split(',')[0];
            string cur = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = new string[]
            {
                Path.Combine(cur, "ReferencedAssemblies", shortName + ".dll"),
                Path.Combine(cur, "Plugins", "Module", shortName + ".dll"),
                Path.Combine(cur, shortName + ".dll")
            };
            foreach (var p in paths) if (File.Exists(p)) return Assembly.LoadFrom(p);
            return null;
        };

        Run();
    }

    static string ReadLiveToken()
    {
        string cur = AppDomain.CurrentDomain.BaseDirectory;
        string c = Path.Combine(cur, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(c, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            long len = fs.Length;
            int bufSize = (int)Math.Min(262144L, len);
            fs.Seek(len - bufSize, SeekOrigin.Begin);
            byte[] buf = new byte[bufSize];
            int read = fs.Read(buf, 0, bufSize);
            string chunk = Encoding.UTF8.GetString(buf, 0, read);
            int idx = chunk.LastIndexOf("TokenCode|");
            if (idx >= 0 && chunk.Length >= idx + 10 + 64)
            {
                return chunk.Substring(idx + 10, 64);
            }
        }
        return null;
    }

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = ReadLiveToken();
        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        // Lấy các BN S72 thuộc Khoa CTCH & Cột sống (57) năm 2025
        var tf = new HisTreatmentViewFilter();
        tf.IN_TIME_FROM = 20250101000000;
        tf.IN_TIME_TO   = 20251231235959;
        tf.ICD_CODE_OR_ICD_SUB_CODE = "S72";
        tf.BRANCH_ID = 1;
        param.Limit = 50;

        var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        if (treatments == null || treatments.Count == 0) return;

        var k57Treatments = treatments.Where(x => x.END_DEPARTMENT_ID == 57 || (x.END_DEPARTMENT_NAME != null && x.END_DEPARTMENT_NAME.Contains("Chấn thương"))).Take(15).ToList();
        Console.WriteLine(string.Format("Lấy {0} BN mẫu thuộc Khoa 57:", k57Treatments.Count));

        var tIds = k57Treatments.Select(x => x.ID).ToList();

        // Query tất cả HIS_SERVICE_REQ loại 4 (PTTT) của các BN này
        var srf = new HisServiceReqViewFilter
        {
            TREATMENT_IDs = tIds,
            SERVICE_REQ_TYPE_ID = 4
        };
        param = new CommonParam();
        var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        Console.WriteLine(string.Format("Tìm thấy {0} phiếu PTTT:", srs != null ? srs.Count : 0));

        if (srs != null)
        {
            foreach (var req in srs)
            {
                var tr = k57Treatments.FirstOrDefault(x => x.ID == req.TREATMENT_ID);
                Console.WriteLine("  BN: {0} ({1}) | Phòng thực hiện: {2} | Khoa: {3} | Mã phiếu: {4}",
                    tr != null ? tr.TDL_PATIENT_NAME : "", tr != null ? tr.ICD_NAME : "",
                    req.EXECUTE_ROOM_NAME, req.EXECUTE_DEPARTMENT_NAME, req.SERVICE_REQ_CODE);
            }

            // Lấy chi tiết SERE_SERV của các phiếu này
            var srIds = srs.Select(x => x.ID).ToList();
            var ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = srIds };
            param = new CommonParam();
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
            Console.WriteLine(string.Format("\nChi tiết {0} dịch vụ trong các phiếu PTTT trên:", sss != null ? sss.Count : 0));
            if (sss != null)
            {
                foreach (var s in sss)
                {
                    Console.WriteLine("  -> [{0}] {1} (ServiceType: {2})", 
                        s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.TDL_SERVICE_TYPE_ID);
                }
            }
        }
    }
}
