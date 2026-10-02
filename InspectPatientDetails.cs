using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

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
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            HIS.Desktop.LocalStorage.ConfigSystem.ConfigSystem.Load.Init();
            var mosConsumer = HIS.Desktop.ApiConsumer.ApiConsumers.MosConsumer;
            var param = new CommonParam();
            var myAdapter = new MyAdapter();

            string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
            string currentToken = null;
            if (File.Exists(cacheFile))
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 1 && parts[0].Length == 64)
                    currentToken = parts[0];
            }
            if (string.IsNullOrEmpty(currentToken))
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
                if (File.Exists(logFile))
                {
                    string text = File.ReadAllText(logFile);
                    int idx = text.LastIndexOf("TokenCode|");
                    if (idx >= 0 && text.Length >= idx + 10 + 64)
                        currentToken = text.Substring(idx + 10, 64);
                }
            }

            Inventec.Token.ClientSystem.TokenClient.SetToken(currentToken);

            long tId = 7348631;

            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

            if (reqs != null)
            {
                foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
                {
                    Console.WriteLine("\n===============================================================================");
                    Console.WriteLine(string.Format("PHIẾU #{0} (ID: {1}) | Ngày: {2} | Loại: {3} | BS: {4} | Nơi TH: {5} | STT: {6}",
                        r.SERVICE_REQ_CODE, r.ID, r.INTRUCTION_TIME, r.SERVICE_REQ_TYPE_NAME, r.REQUEST_USERNAME, r.EXECUTE_ROOM_NAME, r.SERVICE_REQ_STT_NAME));

                    var ssf = new HisSereServViewFilter { SERVICE_REQ_ID = r.ID };
                    var sss = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null)
                    {
                        foreach (var s in sss)
                        {
                            Console.WriteLine(string.Format("  • Dịch vụ: {0} (Mã: {1} | SS_ID: {2})", s.TDL_SERVICE_NAME, s.TDL_SERVICE_CODE, s.ID));

                            var extFilter = new HisSereServExtFilter { SERE_SERV_ID = s.ID };
                            var exts = myAdapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mosConsumer, extFilter, param);
                            if (exts != null && exts.Count > 0)
                            {
                                foreach (var ext in exts)
                                {
                                    if (!string.IsNullOrEmpty(ext.DESCRIPTION)) Console.WriteLine("    👉 Mô tả: " + ext.DESCRIPTION.Trim().Replace("\r\n", " "));
                                    if (!string.IsNullOrEmpty(ext.CONCLUDE)) Console.WriteLine("    👉 Kết luận: " + ext.CONCLUDE.Trim().Replace("\r\n", " "));
                                    if (!string.IsNullOrEmpty(ext.NOTE)) Console.WriteLine("    👉 Ghi chú: " + ext.NOTE.Trim().Replace("\r\n", " "));
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
