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

namespace CheckDept915
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
        static void Main(string[] args)
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
            var adapter = new MyAdapter();

            Console.WriteLine("=== TỦ TRỰC / KHO DƯỢC KHOA NGOẠI TỔNG HỢP CSNB (915) ===");
            HisMediStockViewFilter msf = new HisMediStockViewFilter { DEPARTMENT_ID = 915 };
            var stocks = adapter.FetchList<V_HIS_MEDI_STOCK>("api/HisMediStock/GetView", mosConsumer, msf, param);
            if (stocks != null)
            {
                foreach (var s in stocks)
                {
                    Console.WriteLine(string.Format("ID: {0,4} | Code: {1,-18} | Cabinet: {2,-3} | Name: {3}", 
                        s.ID, s.MEDI_STOCK_CODE, s.IS_CABINET == 1 ? "YES" : "NO", s.MEDI_STOCK_NAME));
                }
            }

            Console.WriteLine("\n=== TẤT CẢ BUỒNG BỆNH KHOA NGOẠI TỔNG HỢP CSNB (915) ===");
            HisBedRoomViewFilter brf = new HisBedRoomViewFilter { DEPARTMENT_ID = 915 };
            var bedRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
            if (bedRooms != null)
            {
                foreach (var br in bedRooms)
                {
                    Console.WriteLine(string.Format("BedRoomId: {0,4} | RoomId: {1,5} | Code: {2,-12} | Name: {3}", 
                        br.ID, br.ROOM_ID, br.BED_ROOM_CODE, br.BED_ROOM_NAME));
                }
            }
        }
    }
}
