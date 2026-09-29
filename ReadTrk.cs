using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

namespace ReadTrk
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();
            var consumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var myAdapter = new MyAdapter();
            var cp = new CommonParam();
            var trkf = new HisTrackingViewFilter { ID = 10196717 };
            var list = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", consumer, trkf, cp);
            if (list != null && list.Count > 0)
            {
                var t = list[0];
                Console.WriteLine("ID: " + t.ID);
                Console.WriteLine("TIME: " + t.TRACKING_TIME);
                Console.WriteLine("CONTENT:\n" + t.CONTENT);
            }
        }
    }

    class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam cp)
        {
            return Get<List<T>>(uri, consumer, filter, cp);
        }
    }
}