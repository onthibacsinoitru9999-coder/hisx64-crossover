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
        Console.OutputEncoding = Encoding.UTF8;
        CommonParam param = new CommonParam();
        string token = "";
        if (File.Exists("doctor_standalone.token"))
        {
            var parts = File.ReadAllText("doctor_standalone.token", Encoding.UTF8).Trim().Split('|');
            if (parts.Length >= 1) token = parts[0];
        }

        ApiConsumer consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();

        // 1. Find all service requests created today by 034727 or in dep 57
        HisServiceReqViewFilter srf = new HisServiceReqViewFilter
        {
            REQUEST_DEPARTMENT_ID = 57,
            INTRUCTION_TIME_FROM = 20261001000000
        };

        var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf, param) ?? new List<V_HIS_SERVICE_REQ>();
        Console.WriteLine("Total reqs from 01/10: " + reqs.Count);

        var rationReqs = reqs.Where(r => r.SERVICE_REQ_TYPE_NAME != null && (r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn") || r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("dinh dưỡng")) || r.SERVICE_REQ_TYPE_ID == 8 || r.SERVICE_REQ_TYPE_ID == 12 || r.SERVICE_REQ_TYPE_ID == 14 || r.SERVICE_REQ_TYPE_ID == 17).ToList();

        Console.WriteLine("Ration reqs: " + rationReqs.Count);
        foreach (var r in rationReqs)
        {
            Console.WriteLine(string.Format("ID: {0} | Code: {1} | BN: {2} ({3}) | Type: {4} | Time: {5} | BS: {6}",
                r.ID, r.SERVICE_REQ_CODE, r.TDL_PATIENT_NAME, r.TDL_PATIENT_CODE, r.SERVICE_REQ_TYPE_NAME, r.INTRUCTION_TIME, r.REQUEST_LOGINNAME));
        }

        // Also check with CREATE_TIME_FROM today
        HisServiceReqViewFilter srf2 = new HisServiceReqViewFilter
        {
            REQUEST_LOGINNAME = "034727"
        };
        var reqs2 = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", consumer, srf2, param) ?? new List<V_HIS_SERVICE_REQ>();
        Console.WriteLine("\nTotal reqs by 034727: " + reqs2.Count);
        var recent2 = reqs2.OrderByDescending(x => x.CREATE_TIME ?? 0).Take(25).ToList();
        foreach (var r in recent2)
        {
            Console.WriteLine(string.Format("ID: {0} | Code: {1} | BN: {2} ({3}) | Type: {4} | InstrTime: {5} | CreateTime: {6}",
                r.ID, r.SERVICE_REQ_CODE, r.TDL_PATIENT_NAME, r.TDL_PATIENT_CODE, r.SERVICE_REQ_TYPE_NAME, r.INTRUCTION_TIME, r.CREATE_TIME));
        }
    }
}
