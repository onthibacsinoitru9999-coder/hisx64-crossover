using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapterWh : AdapterBase
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

public class PresItemPlan
{
    public long MedicineTypeId { get; set; }
    public string MedicineName { get; set; }
    public decimal Amount { get; set; }
    public long MediStockId { get; set; }
    public string Tutorial { get; set; }
    public long MedicineUseFormId { get; set; }
    public string Morning { get; set; }
    public string Noon { get; set; }
    public string Afternoon { get; set; }
    public string Evening { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string name = new AssemblyName(a.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", name);
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            return null;
        };
        RealMain(args);
    }

    static void RealMain(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token").Split('|')[0];
        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapterWh adapter = new MyAdapterWh();
        CommonParam cp = new CommonParam();

        // 1. Cập nhật WorkInfo
        try
        {
            var wi = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 }, // P734
                    new RoomSDO { RoomId = 5251 }, // P714
                    new RoomSDO { RoomId = 931 }   // Nhà Q
                }
            };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, wi, cp);
            Console.WriteLine("✔ Đã cập nhật WorkInfo phòng 5248, 5251, 931.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Lỗi UpdateWorkInfo: " + ex.Message);
        }

        string patCode = "0003908874";
        Console.WriteLine("\n====================================================================================================");
        Console.WriteLine("🏥 KÊ ĐƠN THUỐC LĨNH KHO DƯỢC CHO BỆNH NHÂN: PHẠM THỊ HUYỀN (MÃ BN: " + patCode + ")");
        Console.WriteLine("Các ngày yêu cầu: 10/10, 11/10, 12/10/2026");
        Console.WriteLine("BS Chỉ định: Ths.BS NGUYỄN HỮU SÂM (034727) | Khoa CTCH & CS (Khoa 57)");
        Console.WriteLine("====================================================================================================\n");

        // Tìm Treatment
        var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = patCode };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
        if (trs == null || trs.Count == 0)
        {
            Console.WriteLine("❌ Không tìm thấy hồ sơ điều trị của bệnh nhân " + patCode);
            return;
        }
        var tr = trs.OrderByDescending(x => x.IN_TIME).First();
        Console.WriteLine(string.Format("👤 Bệnh nhân: {0} ({1}) | Mã ĐT: {2} | ID: {3}",
            tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_GENDER_NAME, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} {2}", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT));

        // Xác định buồng phòng
        long reqRoomId = 5248;
        string bedName = "Giường số 17";
        string roomName = "Phòng 714";
        try
        {
            var tbrFilter = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tr.ID, IS_IN_ROOM = true };
            var tbrs = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mos, tbrFilter, cp);
            if (tbrs != null && tbrs.Count > 0)
            {
                bedName = tbrs[0].BED_NAME;
                roomName = tbrs[0].BED_ROOM_NAME;
                var brf = new HisBedRoomViewFilter { ID = tbrs[0].BED_ROOM_ID };
                var brs = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mos, brf, cp);
                if (brs != null && brs.Count > 0 && brs[0].ROOM_ID > 0)
                {
                    reqRoomId = brs[0].ROOM_ID;
                }
            }
        }
        catch { }
        Console.WriteLine(string.Format("   Vị trí hiện tại: {0} - {1} (RoomId: {2})", roomName, bedName, reqRoomId));

        // Kiểm tra y lệnh đã có
        var mf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tr.ID };
        var existingMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, mf, cp);
        var existingDates = new HashSet<string>();
        if (existingMeds != null)
        {
            foreach (var em in existingMeds)
            {
                long t = em.TDL_INTRUCTION_TIME ?? em.EXP_TIME ?? 0;
                if (t > 0) existingDates.Add(t.ToString().Substring(0, 8));
            }
        }

        // Định nghĩa thuốc
        var items = new List<PresItemPlan>
        {
            new PresItemPlan
            {
                MedicineTypeId = 18239,
                MedicineName = "Briozcal (500mg + 125IU)",
                Amount = 1,
                MediStockId = 4210,
                Tutorial = "Ngày uống 1 viên buổi sáng lúc 8h, sau ăn, uống với nhiều nước",
                MedicineUseFormId = 1,
                Morning = "01"
            },
            new PresItemPlan
            {
                MedicineTypeId = 23554,
                MedicineName = "Tramadol/Paracetamol Normon 37,5mg/325mg",
                Amount = 1,
                MediStockId = 4210,
                Tutorial = "Ngày uống 1 viên buổi sáng lúc 8h sau ăn khi đau",
                MedicineUseFormId = 1,
                Morning = "01"
            }
        };

        int[] targetDays = new int[] { 10, 11, 12 };
        int successCount = 0;

        foreach (var day in targetDays)
        {
            string dateStr = string.Format("202610{0:D2}", day);
            string dayOfWeekStr = (day == 10) ? "Thứ 7" : (day == 11) ? "Chủ Nhật" : "Thứ 2";
            long insTime = long.Parse(dateStr + "080000");

            Console.WriteLine(string.Format("\n----------------------------------------------------------------------------------------------------"));
            Console.WriteLine(string.Format("📅 KÊ ĐƠN NGÀY {0} ({1}/10/2026 lúc 08:00):", dayOfWeekStr, day));

            if (existingDates.Contains(dateStr))
            {
                Console.WriteLine(string.Format("   ⚠️ Ngày {0} ({1}/10/2026) đã có y lệnh thuốc xuất kho trước đó. Vẫn kiểm tra xem có cần bổ sung hay không...", dayOfWeekStr, day));
            }

            var presMeds = new List<PresMedicineSDO>();
            foreach (var itm in items)
            {
                long medPatType = tr.TDL_PATIENT_TYPE_ID ?? 1;
                presMeds.Add(new PresMedicineSDO
                {
                    MedicineTypeId = itm.MedicineTypeId,
                    MediStockId = itm.MediStockId,
                    Amount = itm.Amount,
                    PresAmount = itm.Amount,
                    PatientTypeId = medPatType,
                    Tutorial = itm.Tutorial,
                    MedicineUseFormId = itm.MedicineUseFormId,
                    Morning = itm.Morning,
                    Noon = itm.Noon,
                    Afternoon = itm.Afternoon,
                    Evening = itm.Evening,
                    IsExpend = false,
                    NumOfDays = 1
                });
            }

            var presSDO = new InPatientPresSDO
            {
                TreatmentId = tr.ID,
                InstructionTimes = new List<long> { insTime },
                UseTimes = new List<long> { insTime },
                TrackingId = null,
                TrackingInfos = null,
                RequestRoomId = reqRoomId > 0 ? reqRoomId : 5248,
                RequestLoginName = "034727",
                RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                IcdCode = tr.ICD_CODE,
                IcdName = tr.ICD_NAME,
                IcdSubCode = tr.ICD_SUB_CODE,
                IcdText = tr.ICD_TEXT,
                Medicines = presMeds
            };

            CommonParam pPres = new CommonParam();
            var presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mos, presSDO, pPres);

            // Fallback sang phòng 5248 nếu phòng buồng 5251 bị từ chối
            if ((presRes == null || ((presRes.ServiceReqs == null || presRes.ServiceReqs.Count == 0) && (presRes.ExpMests == null || presRes.ExpMests.Count == 0))) && presSDO.RequestRoomId != 5248)
            {
                Console.WriteLine("   ℹ️ Đang thử lại với RequestRoomId = 5248...");
                presSDO.RequestRoomId = 5248;
                pPres = new CommonParam();
                presRes = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mos, presSDO, pPres);
            }

            if (presRes != null && presRes.ServiceReqs != null && presRes.ServiceReqs.Count > 0)
            {
                var sr = presRes.ServiceReqs[0];
                string expCodes = (presRes.ExpMests != null) ? string.Join(", ", presRes.ExpMests.Select(x => x.EXP_MEST_CODE)) : "";
                Console.WriteLine(string.Format("   ✅ [THÀNH CÔNG] Ngày {0} ({1}/10/2026):", dayOfWeekStr, day));
                Console.WriteLine(string.Format("      • MÃ PHIẾU Y LỆNH : {0} (ServiceReqId: {1})", sr.SERVICE_REQ_CODE, sr.ID));
                Console.WriteLine(string.Format("      • MÃ XUẤT KHO DƯỢC: {0}", expCodes));
                Console.WriteLine(string.Format("      • Chi tiết thuốc  :"));
                foreach (var itm in items)
                {
                    Console.WriteLine(string.Format("        + {0} x {1} viên | Kho: {2} | HDSD: {3}", itm.MedicineName, itm.Amount, itm.MediStockId, itm.Tutorial));
                }
                successCount++;
            }
            else
            {
                string err = "";
                if (pPres.Messages != null && pPres.Messages.Count > 0) err = string.Join("; ", pPres.Messages);
                else if (pPres.BugCodes != null && pPres.BugCodes.Count > 0) err = string.Join("; ", pPres.BugCodes);
                else err = "API trả về null/không có ServiceReq";
                Console.WriteLine(string.Format("   ❌ [THẤT BẠI] Ngày {0} ({1}/10/2026): Lỗi: {2}", dayOfWeekStr, day, err));
            }
        }

        Console.WriteLine("\n====================================================================================================");
        Console.WriteLine(string.Format("🎯 TỔNG KẾT: Đã kê thành công {0}/{1} ngày cho bệnh nhân PHẠM THỊ HUYỀN", successCount, targetDays.Length));
        Console.WriteLine("====================================================================================================");
    }
}
