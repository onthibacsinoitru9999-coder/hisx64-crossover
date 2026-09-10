using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class InspectRealOrder
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
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
        CommonParam param = new CommonParam();
        Load.Init();
        string tokenCode = null;
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs))
        {
            string text = sr.ReadToEnd();
            var matches = Regex.Matches(text, @"TokenCode\|([a-f0-9]{64})");
            if (matches.Count > 0) tokenCode = matches[matches.Count - 1].Groups[1].Value;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
        MyAdapter adapter = new MyAdapter();

        Console.WriteLine("=== CHI TIẾT REQ 90049969 (HÀ ĐÌNH XUYÊN LÚC 06:20) ===");
        HisServiceReqViewFilter srf620 = new HisServiceReqViewFilter { ID = 90049969L };
        var reqs620 = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf620, param);
        if (reqs620 != null && reqs620.Count > 0)
        {
            var r = reqs620[0];
            Console.WriteLine(string.Format("REQ: ID={0} | Code={1} | Type={2} | Time={3} | ReqRoom={4} | ExRoom={5} | TrackingId={6}",
                r.ID, r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_ID, r.INTRUCTION_TIME, r.REQUEST_ROOM_ID, r.EXECUTE_ROOM_ID, r.TRACKING_ID));
        }
        HisExpMestMedicineViewFilter emf620 = new HisExpMestMedicineViewFilter { TDL_SERVICE_REQ_ID = 90049969L };
        var meds620 = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf620, param);
        if (meds620 != null)
        {
            foreach (var m in meds620)
            {
                Console.WriteLine(string.Format("  MED: {0} | TypeId={1} | StockId={2} | Amount={3} | Tutorial={4} | UseFormId={5}",
                    m.MEDICINE_TYPE_NAME, m.MEDICINE_TYPE_ID, m.MEDI_STOCK_ID, m.AMOUNT, m.TUTORIAL, m.MEDICINE_USE_FORM_ID));
            }
        }

        Console.WriteLine("=== TẤT CẢ Y LỆNH HÔM NAY CỦA NGUYỄN THỊ PHƯỢNG (7202064) ===");
        HisServiceReqViewFilter srfPAll = new HisServiceReqViewFilter { TREATMENT_ID = 7202064L };
        var reqsPAll = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srfPAll, param);
        if (reqsPAll != null)
        {
            foreach (var r in reqsPAll.OrderByDescending(x => x.INTRUCTION_TIME))
            {
                Console.WriteLine(string.Format("REQ P: ID={0} | Code={1} | Type={2} | Time={3} | ReqRoom={4} | ExRoom={5} | TrackingId={6}",
                    r.ID, r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_ID, r.INTRUCTION_TIME, r.REQUEST_ROOM_ID, r.EXECUTE_ROOM_ID, r.TRACKING_ID));

                HisExpMestMedicineViewFilter emf = new HisExpMestMedicineViewFilter { TDL_SERVICE_REQ_ID = r.ID };
                var meds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);
                if (meds != null && meds.Count > 0)
                {
                    foreach (var m in meds)
                    {
                        Console.WriteLine(string.Format("   -> MED: {0} | TypeId={1} | StockId={2} | Amount={3} | Tutorial={4}",
                            m.MEDICINE_TYPE_NAME, m.MEDICINE_TYPE_ID, m.MEDI_STOCK_ID, m.AMOUNT, m.TUTORIAL));
                    }
                }
            }
        }
    }
}
