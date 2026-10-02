using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class WardTreatmentProcessor
{
    static MyAdapter myAdapter = new MyAdapter();
    static CommonParam param = new CommonParam();
    static ApiConsumer mosConsumer;
    static string currentDoctorLogin = "034727";
    static string currentDoctorName = "Ths.BS Nguyễn Hữu Sâm";

    public static void Main()
    {
        InitSession();

        var patientList = new List<PatientTarget>
        {
            new PatientTarget { PatientCode = "0004033304", RoomName = "Phòng 717 (Giường 07)", IsSurgeryTomorrow = false, Note = "Hẹp ống sống C4-5, C5-6 / THA - Dự kiến mổ T5", RationType = "BT01" },
            new PatientTarget { PatientCode = "0004048113", RoomName = "Phòng 717 (Giường 08)", IsSurgeryTomorrow = false, Note = "Viêm đốt sống đĩa đệm - Dự kiến mổ T5", RationType = "BT01" },
            new PatientTarget { PatientCode = "0003380587", RoomName = "Phòng 717 (Giường 10)", IsSurgeryTomorrow = false, Note = "Sau mổ CĐCS L4-5 ngày 29/09 (Hậu phẫu N1)", RationType = "BT01" },
            new PatientTarget { PatientCode = "0003002690", RoomName = "Phòng 717 (Giường 10)", IsSurgeryTomorrow = false, Note = "Chấn thương gối T, mổ nội soi 29/09 (Hậu phẫu N1)", RationType = "" }, // BN từ chối ăn cơm viện
            new PatientTarget { PatientCode = "0003863470", RoomName = "Phòng 717 (Giường 10a)", IsSurgeryTomorrow = true, Note = "HC ống cổ tay T nặng - MỔ PHIÊN 30/09 (Cắt DC ngang)", RationType = "" }, // Mai mổ
            new PatientTarget { PatientCode = "0001344789", RoomName = "Phòng 714 (Giường 18/17)", IsSurgeryTomorrow = false, Note = "Xẹp T12, L1 / ĐTĐ, THA, Loãng xương, Loét tỳ đè độ II", RationType = "DD01" },
            new PatientTarget { PatientCode = "0004093343", RoomName = "Phòng 714 (Giường 89)", IsSurgeryTomorrow = false, Note = "Gãy đầu trên x.chày P, gãy x.mác P (Vào viện 29/09)", RationType = "BT01" },
            new PatientTarget { PatientCode = "0004093319", RoomName = "Phòng 714 (Giường 92)", IsSurgeryTomorrow = false, Note = "Vết thương hở bàn chân P (Mổ CC 29/09, Hậu phẫu N1)", RationType = "BT01" }
        };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 QUY TRÌNH XỬ LÝ ĐIỀU TRỊ BUỒNG 714 & 717 - NGÀY 30/09/2026");
        Console.WriteLine("===============================================================================");

        foreach (var p in patientList)
        {
            ProcessPatient(p);
        }
    }

    static void InitSession()
    {
        try { Load.Init(); } catch { }
        string tokenCode = null;

        // Try standalone token cache
        string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token");
        if (!File.Exists(cacheFile))
        {
            string alt = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB\doctor_standalone.token";
            if (File.Exists(alt)) cacheFile = alt;
        }
        if (File.Exists(cacheFile))
        {
            try
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2 && parts[0].Length == 64)
                {
                    tokenCode = parts[0];
                    currentDoctorLogin = parts.Length >= 3 ? parts[2] : "034727";
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(tokenCode))
        {
            var tokenManager = new Inventec.Token.ClientSystem.ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
            if (token == null || string.IsNullOrEmpty(token.TokenCode))
            {
                token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                if (token != null)
                {
                    tokenCode = token.TokenCode;
                    currentDoctorLogin = "vmc";
                    currentDoctorName = "BS Vũ Minh Cường";
                }
            }
            else
            {
                tokenCode = token.TokenCode;
            }
        }

        mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");

        // WorkInfo
        long[] rooms = new long[] { 931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 5267 };
        var workInfo = new WorkInfoSDO { Rooms = rooms.Select(r => new RoomSDO { RoomId = r }).ToList() };
        myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
        Console.WriteLine(string.Format("Đăng nhập thành công BS: {0} ({1})", currentDoctorName, currentDoctorLogin));
    }

    static void ProcessPatient(PatientTarget p)
    {
        Console.WriteLine("\n-------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("👉 [{0}] Mã BN: {1} | {2}", p.RoomName, p.PatientCode, p.Note));

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = p.PatientCode.PadLeft(10, '0') };
        var listT = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        if (listT == null || listT.Count == 0)
        {
            tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = p.PatientCode.PadLeft(12, '0') };
            listT = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
        }
        if (listT == null || listT.Count == 0)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị đang hoạt động!");
            return;
        }

        var tr = listT.OrderByDescending(x => x.IN_TIME).First();
        Console.WriteLine(string.Format("   Họ tên: {0} | Mã ĐT: {1} | ID: {2} | Khoa: {3}", tr.TDL_PATIENT_NAME, tr.TREATMENT_CODE, tr.ID, tr.LAST_DEPARTMENT_ID));
        Console.WriteLine("   Chẩn đoán: " + tr.ICD_NAME + " - " + tr.ICD_SUB_CODE);

        // Fetch today's tracking
        var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
        var trks = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mosConsumer, trkFilter, param);
        HIS_TRACKING todayTrk = null;
        if (trks != null && trks.Count > 0)
        {
            todayTrk = trks.OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
            Console.WriteLine(string.Format("   📄 Tờ điều trị gần nhất [{0}]:\n   {1}", todayTrk.TRACKING_TIME, todayTrk.CONTENT.Replace("\n", "\n   ")));
        }

        // Fetch service reqs 29/09 and 30/09
        var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var reqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
        bool hasMedsTomorrow = false;
        bool hasRationTomorrow = false;
        bool hasTrackingTomorrow = false;

        if (reqs != null)
        {
            hasMedsTomorrow = reqs.Any(r => r.SERVICE_REQ_TYPE_ID == 6 && r.INTRUCTION_TIME.ToString().StartsWith("20260930") && r.IS_DELETE != 1);
            hasRationTomorrow = reqs.Any(r => r.SERVICE_REQ_TYPE_ID == 14 && r.INTRUCTION_TIME.ToString().StartsWith("20260930") && r.IS_DELETE != 1);
        }
        if (trks != null)
        {
            hasTrackingTomorrow = trks.Any(tk => tk.TRACKING_TIME.ToString().StartsWith("2026093008"));
        }

        Console.WriteLine(string.Format("   📊 Trạng thái ngày 30/09: Thuốc: {0} | Suất ăn: {1} | Tờ ĐT: {2}",
            hasMedsTomorrow ? "🟢 Đã có" : "🔴 Chưa có",
            hasRationTomorrow ? "🟢 Đã có" : "🔴 Chưa có",
            hasTrackingTomorrow ? "🟢 Đã có" : "🔴 Chưa có"));
    }
}

public class PatientTarget
{
    public string PatientCode { get; set; }
    public string RoomName { get; set; }
    public bool IsSurgeryTomorrow { get; set; }
    public string Note { get; set; }
    public string RationType { get; set; }
}
