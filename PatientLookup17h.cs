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

class PatientLookup17h
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

        // Update WorkInfo
        try
        {
            var wi = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 },
                    new RoomSDO { RoomId = 931 },
                    new RoomSDO { RoomId = 5250 },
                    new RoomSDO { RoomId = 5252 },
                    new RoomSDO { RoomId = 5253 },
                    new RoomSDO { RoomId = 5255 },
                    new RoomSDO { RoomId = 5256 },
                    new RoomSDO { RoomId = 5257 },
                    new RoomSDO { RoomId = 5258 },
                    new RoomSDO { RoomId = 5259 },
                    new RoomSDO { RoomId = 5260 },
                    new RoomSDO { RoomId = 5261 },
                    new RoomSDO { RoomId = 5262 },
                    new RoomSDO { RoomId = 5263 },
                    new RoomSDO { RoomId = 5264 },
                    new RoomSDO { RoomId = 5265 },
                    new RoomSDO { RoomId = 5266 },
                    new RoomSDO { RoomId = 5267 }
                }
            };
            adapter.Post<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, wi, new CommonParam());
        }
        catch { }

        // Find active in-patients in Khoa 57
        var treatFilter = new HisTreatmentFilter
        {
            IS_PAUSE = false,
            IS_LOCK_HEIN = false
        };
        var currentTreatments = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, treatFilter, new CommonParam());
        Console.WriteLine(string.Format("Tổng số hồ sơ đang điều trị: {0}", currentTreatments != null ? currentTreatments.Count : 0));

        // Get DepartmentTrans to filter Khoa 57
        var deptTransFilter = new HisDepartmentTranFilter
        {
            DEPARTMENT_ID = 57
        };
        var deptTrans = adapter.Get<List<HIS_DEPARTMENT_TRAN>>("api/HisDepartmentTran/Get", mosConsumer, deptTransFilter, new CommonParam());

        // Target list names to search
        string[] searchNames = new string[]
        {
            "NGUYỄN VĂN CHIẾN",
            "HÁN THỊ THIÊM",
            "NGUYỄN THỊ MAI",
            "VŨ XUÂN CƯỜNG",
            "VŨ XUÂN CƯƠNG",
            "ĐẶNG THỊ NAM",
            "TRẦN THỊ NHÂM",
            "TRẦN THỊ NHẬM",
            "TRẦN THỊ NHAM",
            "PHAN THỊ TƯ",
            "NGÔ THỊ THANH",
            "NGUYỄN THỊ VINH",
            "TRƯƠNG THỊ THÀNH",
            "ĐINH THỊ VẺ",
            "NGUYỄN PHÚ BÌNH",
            "ĐINH VĂN NGHIỆP",
            "NGUYỄN THỊ NGỌ"
        };

        foreach (var sName in searchNames)
        {
            var pFilter = new HisPatientFilter { KEY_WORD = sName };
            var pats = adapter.Get<List<HIS_PATIENT>>("api/HisPatient/Get", mosConsumer, pFilter, new CommonParam());
            if (pats != null && pats.Count > 0)
            {
                foreach (var p in pats)
                {
                    // check treatment
                    var trmFilter = new HisTreatmentFilter { PATIENT_ID = p.ID };
                    var trms = adapter.Get<List<HIS_TREATMENT>>("api/HisTreatment/Get", mosConsumer, trmFilter, new CommonParam());
                    var activeTrm = trms != null ? trms.OrderByDescending(x => x.IN_TIME).FirstOrDefault(x => x.IS_PAUSE != 1) : null;
                    if (activeTrm != null)
                    {
                        Console.WriteLine(string.Format("FOUND: '{0}' -> Code: {1} | Name: {2} | TrmId: {3} | TrmCode: {4} | InTime: {5}",
                            sName, p.PATIENT_CODE, p.VIR_PATIENT_NAME, activeTrm.ID, activeTrm.TREATMENT_CODE, activeTrm.IN_TIME));
                    }
                }
            }
        }
    }
}
