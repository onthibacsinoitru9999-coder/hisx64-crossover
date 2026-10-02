using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;
using MOS.SDO;

class Map14Patients
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, r) =>
        {
            string n = new System.Reflection.AssemblyName(r.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, n);
            if (File.Exists(p1)) return System.Reflection.Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", n);
            if (File.Exists(p2)) return System.Reflection.Assembly.LoadFrom(p2);
            return null;
        };
        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = "";
        if (File.Exists("doctor_standalone.token")) token = File.ReadAllText("doctor_standalone.token").Split('|')[0].Trim();
        else if (File.Exists("doctor_hn.token")) token = File.ReadAllText("doctor_hn.token").Split('|')[0].Trim();

        var cp = new CommonParam();
        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new BackendAdapter(cp);

        var tbrf = new HisTreatmentBedRoomViewFilter
        {
            IS_IN_ROOM = true,
            TREATMENT_IS_ACTIVE = true
        };
        var beds = adapter.Get<List<V_HIS_TREATMENT_BED_ROOM>>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, cp);
        if (beds == null)
        {
            Console.WriteLine("❌ Không lấy được danh sách giường!");
            return;
        }

        var dept57Beds = beds.Where(b => b.DEPARTMENT_ID == 57).GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).ToList();
        Console.WriteLine(string.Format("Số BN tại Khoa 57: {0}", dept57Beds.Count));

        List<long> trmIds = dept57Beds.Select(b => b.TREATMENT_ID).ToList();
        Dictionary<long, V_HIS_TREATMENT> trmMap = new Dictionary<long, V_HIS_TREATMENT>();

        for (int i = 0; i < trmIds.Count; i += 100)
        {
            var batch = trmIds.Skip(i).Take(100).ToList();
            var tf = new HisTreatmentViewFilter { IDs = batch };
            var list = adapter.Get<List<V_HIS_TREATMENT>>("api/HisTreatment/GetView", mosConsumer, tf, cp);
            if (list != null)
            {
                foreach (var t in list) trmMap[t.ID] = t;
            }
        }

        foreach (var b in dept57Beds)
        {
            V_HIS_TREATMENT tr = null;
            trmMap.TryGetValue(b.TREATMENT_ID, out tr);
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : b.TDL_PATIENT_CODE;
            string patName = tr != null ? tr.TDL_PATIENT_NAME : b.TDL_PATIENT_NAME;
            string bedName = b.BED_NAME;
            string roomName = b.BED_ROOM_NAME;

            Console.WriteLine(string.Format("BN: {0,-10} | {1,-25} | {2,-20} | {3,-15} | TrmId: {4}",
                patCode, patName, roomName, bedName, b.TREATMENT_ID));
        }
    }
}
