using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }

    public bool PostBool(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<bool>(uri, consumer, data, param);
    }
}

class Program
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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Load.Init();
        ClientTokenManager tm = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");
        CommonParam p = new CommonParam();
        var token = tm.Login(p, "034727", "9981", "2.390.0");
        if (token == null)
        {
            Console.WriteLine("Login 034727 failed!");
            return;
        }
        Console.WriteLine("Logged in with 034727. Token: " + token.TokenCode.Substring(0, 10) + "...");
        ApiConsumers.SetConsunmer(token.TokenCode);
        MyAdapter adapter = new MyAdapter();

        long[] dept57Rooms = new long[] { 5248, 5252, 5251, 5257, 5250, 5253, 5254, 5255, 5256 };
        var workInfo = new WorkInfoSDO
        {
            Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList()
        };
        adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, p);

        long[] targetTrackingIds = new long[] { 9724187, 9724194, 9724201 };

        foreach (var trackId in targetTrackingIds)
        {
            CommonParam deleteParam = new CommonParam();
            // In frmHisTrackingList, AdapterBase.Post<bool>("api/HisTracking/Delete", ApiConsumers.MosConsumer, (object)tracking.ID, commonParam)
            bool result = adapter.PostBool("api/HisTracking/Delete", ApiConsumers.MosConsumer, trackId, deleteParam);
            Console.WriteLine(string.Format("Deleting Tracking ID {0}: Result = {1}", trackId, result));
            if (deleteParam.Messages != null)
            {
                foreach (var msg in deleteParam.Messages) Console.WriteLine("   Message: " + msg);
            }
            if (deleteParam.BugCodes != null)
            {
                foreach (var bug in deleteParam.BugCodes) Console.WriteLine("   BugCode: " + bug);
            }
        }

        // Re-check remaining tracking sheets
        Console.WriteLine("\n=== RE-CHECKING TRACKINGS FOR BN THUAN ===");
        CommonParam checkParam = new CommonParam();
        HisTrackingViewFilter tkf = new HisTrackingViewFilter { TREATMENT_ID = 7108039L };
        var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, checkParam);
        if (trackings != null)
        {
            foreach (var tk in trackings.OrderByDescending(x => x.TRACKING_TIME))
            {
                Console.WriteLine(string.Format("Tracking ID: {0} | Time: {1} | Creator: {2} | Modifier: {3}", tk.ID, tk.TRACKING_TIME, tk.CREATOR, tk.MODIFIER));
                Console.WriteLine("   Content: " + tk.CONTENT);
            }
        }
    }
}
