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
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

class AssignGlucoseNB
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

        // 1. Assign for BN HA DINH XUYEN
        Console.WriteLine("=== CHỈ ĐỊNH ĐMMM CHO BN HÀ ĐÌNH XUYÊN ===");
        AssignServiceSDO sdoXuyen = new AssignServiceSDO
        {
            TreatmentId = 7201962L,
            RequestRoomId = 17425, // BB 3E - 33
            RequestLoginName = "vmc",
            RequestUserName = "VŨ MINH CƯỜNG",
            InstructionTime = 20260910060000,
            InstructionTimes = new List<long> { 20260910060000 },
            UseTimes = new List<long> { 20260910060000 },
            IcdCode = "T07",
            IcdName = "đụng dập phần mềm bàn chân 2 bên có lóc thượng bì gan chân - vết thương cẳng chân phải đã tiểu phẫu",
            SessionCode = Guid.NewGuid().ToString(),
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = 74281, // NB260620.6231
                    Amount = 1.0m,
                    PatientTypeId = 1,
                    RoomId = 18679,
                    InstructionNote = "Đo ĐMMM lúc 06:00"
                }
            }
        };
        CommonParam pXuyen = new CommonParam();
        var resXuyen = adapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdoXuyen, pXuyen);
        if (resXuyen != null && resXuyen.ServiceReqs != null && resXuyen.ServiceReqs.Count > 0)
        {
            Console.WriteLine("✅ HÀ ĐÌNH XUYÊN: Mã phiếu y lệnh CLS = " + resXuyen.ServiceReqs[0].SERVICE_REQ_CODE + " (ID: " + resXuyen.ServiceReqs[0].ID + ")");
        }
        else
        {
            Console.WriteLine("❌ Lỗi BN Xuyên: " + string.Join("; ", pXuyen.Messages ?? new List<string>()));
        }

        // 2. Assign for BN NGUYEN THI THU
        Console.WriteLine("\n=== CHỈ ĐỊNH ĐMMM CHO BN NGUYỄN THỊ THÚ ===");
        AssignServiceSDO sdoThu = new AssignServiceSDO
        {
            TreatmentId = 7171147L,
            RequestRoomId = 17416, // BB 3E - 24
            RequestLoginName = "vmc",
            RequestUserName = "VŨ MINH CƯỜNG",
            InstructionTime = 20260910060000,
            InstructionTimes = new List<long> { 20260910060000 },
            UseTimes = new List<long> { 20260910060000 },
            IcdCode = "S32.00",
            IcdName = "Xẹp cấp thân đốt sống L1, L2 sau ngã",
            SessionCode = Guid.NewGuid().ToString(),
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = 74281, // NB260620.6231
                    Amount = 1.0m,
                    PatientTypeId = 1,
                    RoomId = 18679,
                    InstructionNote = "Đo ĐMMM lúc 06:00"
                }
            }
        };
        CommonParam pThu = new CommonParam();
        var resThu = adapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", mosConsumer, sdoThu, pThu);
        if (resThu != null && resThu.ServiceReqs != null && resThu.ServiceReqs.Count > 0)
        {
            Console.WriteLine("✅ NGUYỄN THỊ THÚ: Mã phiếu y lệnh CLS = " + resThu.ServiceReqs[0].SERVICE_REQ_CODE + " (ID: " + resThu.ServiceReqs[0].ID + ")");
        }
        else
        {
            Console.WriteLine("❌ Lỗi BN Thú: " + string.Join("; ", pThu.Messages ?? new List<string>()));
        }
    }
}
