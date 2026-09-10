using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };

        Run();
    }

    static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> candidates = new List<string>();

        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                string hisDir = Path.GetDirectoryName(procs[0].MainModule.FileName);
                candidates.Add(Path.Combine(hisDir, "Logs", "LogSystem.txt"));
            }
        }
        catch { }

        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            return chunk.Substring(start, 64);
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    static void Run()
    {
        CommonParam param = new CommonParam();
        MyAdapter adapter = new MyAdapter();
        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("No live token");
            return;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");

        // Update WorkInfo to include room 17425, 18679, 15322
        var workInfo = new WorkInfoSDO
        {
            Rooms = new List<RoomSDO>
            {
                new RoomSDO { RoomId = 17425 },
                new RoomSDO { RoomId = 18679 },
                new RoomSDO { RoomId = 15322 }
            }
        };
        adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);

        long trId = 7201962L;
        long trackingId = 9929115L; // Tracking of Ha Dinh Xuyen today
        long trackingTime = 20260910110000L;
        string sessionKey = Guid.NewGuid().ToString();

        // 1. Take Bean from Cabinet Stock 5142
        Console.WriteLine("Taking bean from stock 5142...");
        TakeBeanSDO takeBean = new TakeBeanSDO
        {
            TypeId = 27727L, // Actrapid 1000IU/10ml
            MediStockId = 5142L, // Tu truc Khu 3E
            PatientTypeId = 1L,
            Amount = 0.0140m, // 14 UI = 0.0140 lo
            ClientSessionKey = sessionKey,
            ExpiredDate = null
        };
        CommonParam pTake = new CommonParam();
        var beans = adapter.PostData<List<HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", mosConsumer, takeBean, pTake);
        if (beans == null || beans.Count == 0)
        {
            Console.WriteLine("❌ LỖI TakeBean: " + string.Join("; ", pTake.Messages ?? new List<string>()));
            if (pTake.BugCodes != null) Console.WriteLine("   BugCodes: " + string.Join("; ", pTake.BugCodes));
            return;
        }
        Console.WriteLine("✅ TakeBean thành công: " + beans.Count + " bean, IDs: " + string.Join(",", beans.Select(b => b.ID)));

        // 2. Create OutPatientPres (Cabinet prescription)
        var outPresSDO = new OutPatientPresSDO
        {
            TreatmentId = trId,
            InstructionTime = trackingTime,
            UseTimes = new List<long> { trackingTime },
            TrackingId = trackingId,
            RequestRoomId = 17425L, // BB 3E - 33
            RequestLoginName = "034727",
            RequestUserName = "Ths.BS Nguyễn Hữu Sâm",
            IcdCode = "T07",
            IcdName = "đụng dập phần mềm bàn chân 2 bên có lóc thượng bì gan chân - vết thương cẳng chân phải đã tiểu phẫu",
            IsCabinet = true,
            ClientSessionKey = sessionKey,
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = 27727L,
                    MediStockId = 5142L,
                    Amount = 0.0140m,
                    PresAmount = 0.0140m,
                    PatientTypeId = 1L,
                    Tutorial = "Tiêm dưới da 14 đơn vị lúc 11h00",
                    MedicineUseFormId = 15L, // Tiem
                    Noon = "14",
                    IsExpend = false,
                    NumOfDays = 1,
                    MedicineBeanIds = beans.Select(b => b.ID).ToList()
                }
            }
        };

        CommonParam pPres = new CommonParam();
        var res = adapter.PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", mosConsumer, new List<OutPatientPresSDO> { outPresSDO }, pPres);

        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("✅ KÊ ĐƠN TỦ TRỰC THÀNH CÔNG 100%!");
            Console.WriteLine("MÃ PHIẾU Y LỆNH LÂM SÀNG (ServiceReqCode): " + res.ServiceReqs[0].SERVICE_REQ_CODE);
            Console.WriteLine("ID Y lệnh: " + res.ServiceReqs[0].ID);
            if (res.ExpMests != null && res.ExpMests.Count > 0)
            {
                Console.WriteLine("Mã xuất kho dược (ExpMestCode): " + res.ExpMests[0].EXP_MEST_CODE);
            }
            Console.WriteLine("================================================================================");
        }
        else
        {
            Console.WriteLine("❌ LỖI OutPatientPresCreateList: " + string.Join("; ", pPres.Messages ?? new List<string>()));
            if (pPres.BugCodes != null && pPres.BugCodes.Count > 0)
            {
                Console.WriteLine("   BugCodes: " + string.Join("; ", pPres.BugCodes));
            }
        }
    }
}
