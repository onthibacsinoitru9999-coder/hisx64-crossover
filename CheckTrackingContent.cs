using System;
using System.IO;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();
        var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new BackendAdapter();
        var cp = new CommonParam();
        var trkFilter = new HisTrackingViewFilter { ID = 10196717 };
        var list = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, trkFilter, cp);
        if (list != null && list.Count > 0)
        {
            var t = list[0];
            Console.WriteLine("TRACKING TIME: " + t.TRACKING_TIME);
            Console.WriteLine("CONTENT:\n" + t.CONTENT);
            Console.WriteLine("\nCARE:\n" + t.CARE_INSTRUCTION);
            Console.WriteLine("\nMEDICAL_INSTRUCTION:\n" + t.MEDICAL_INSTRUCTION);
        }
    }
}