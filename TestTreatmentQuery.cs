using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
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
    static string ReadLiveToken()
    {
        string logPath = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\Logs\LogSystem.txt";
        if (File.Exists(logPath))
        {
            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long len = fs.Length;
                int bufSize = (int)Math.Min(131072L, len);
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
        }
        return null;
    }

    static void Main()
    {
        string token = ReadLiveToken();
        Console.WriteLine("Token: " + (token != null ? token.Substring(0, 16) + "..." : "NULL"));
        if (token == null) return;

        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var param = new CommonParam();

        // Test 1: Query treatment with S72
        var filter = new HisTreatmentViewFilter();
        filter.IN_TIME_FROM = 20250101000000;
        filter.IN_TIME_TO   = 20251231235959;
        filter.ICD_CODE_OR_ICD_SUB_CODE = "S72";
        param.Limit = 10;

        var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", consumer, filter, param);
        Console.WriteLine("Test 1 (ICD=S72): Count={0}, ParamCount={1}", list != null ? list.Count : -1, param.Count);

        if (list != null && list.Count > 0)
        {
            foreach (var t in list)
            {
                Console.WriteLine("  BN: {0} | Code: {1} | ICD: {2} - {3} | Surg: {4}", 
                    t.TDL_PATIENT_NAME, t.TREATMENT_CODE, t.ICD_CODE, t.ICD_NAME, t.SURGERY_NAME);
            }
        }
    }
}
