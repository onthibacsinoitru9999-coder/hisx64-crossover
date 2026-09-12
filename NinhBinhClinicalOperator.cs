using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

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
}

public class Program
{
    public static MyAdapter adapter = new MyAdapter();
    public static CommonParam param = new CommonParam();

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        if (token == null)
        {
            token = tokenManager.Login(param, "034727", "998199", "2.390.0");
        }
        if (token == null)
        {
            Console.WriteLine("❌ Đăng nhập ACS thất bại!");
            return;
        }

        Console.WriteLine("✅ Đăng nhập thành công. Token: " + token.TokenCode);
        ApiConsumers.SetConsunmer(token.TokenCode);

        // 1. Get all rooms of Dept 915
        var rFilter = new HisRoomViewFilter { DEPARTMENT_ID = 915, IS_ACTIVE = 1 };
        var rooms915 = adapter.FetchList<V_HIS_ROOM>("api/HisRoom/GetView", ApiConsumers.MosConsumer, rFilter, param);
        Console.WriteLine("\n=== DANH SÁCH PHÒNG KHOA NGOẠI TỔNG HỢP (DEPT 915) ===");
        List<RoomSDO> workRooms = new List<RoomSDO>();
        if (rooms915 != null)
        {
            foreach (var r in rooms915)
            {
                workRooms.Add(new RoomSDO { RoomId = r.ID });
                Console.WriteLine(string.Format("  • Phòng: {0} (ID: {1}) | Loại: {2} (ID: {3})", r.ROOM_NAME, r.ID, r.ROOM_TYPE_NAME, r.ROOM_TYPE_ID));
            }
        }

        // Update WorkInfo with Dept 915 rooms
        try
        {
            var workInfo = new WorkInfoSDO { Rooms = workRooms };
            var wp = adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
            Console.WriteLine("✅ Đã cập nhật WorkInfo cho " + workRooms.Count + " phòng thuộc Khoa Ngoại TH Ninh Bình!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Cập nhật WorkInfo: " + ex.Message);
        }

        // 2. MediStocks of Dept 915 / CSNB
        var sFilter = new HisMediStockViewFilter { DEPARTMENT_ID = 915, IS_ACTIVE = 1 };
        var stocks915 = adapter.FetchList<V_HIS_MEDI_STOCK>("api/HisMediStock/GetView", ApiConsumers.MosConsumer, sFilter, param);
        Console.WriteLine("\n=== TỦ TRỰC / KHO KHOA NGOẠI TỔNG HỢP (DEPT 915) ===");
        long targetStockId = 0;
        if (stocks915 != null && stocks915.Count > 0)
        {
            foreach (var s in stocks915)
            {
                Console.WriteLine(string.Format("  • Tủ trực/Kho: {0} (ID: {1}) | Mã: {2} | RoomId: {3} | IsCabinet: {4}",
                    s.MEDI_STOCK_NAME, s.ID, s.MEDI_STOCK_CODE, s.ROOM_ID, s.IS_CABINET));
                if (targetStockId == 0) targetStockId = s.ID;
            }
        }
        else
        {
            Console.WriteLine("  Không có kho gắn trực tiếp DEPARTMENT_ID = 915. Đang tìm tủ trực thuộc CSNB...");
            var allStocks = adapter.FetchList<V_HIS_MEDI_STOCK>("api/HisMediStock/GetView", ApiConsumers.MosConsumer, new HisMediStockViewFilter { IS_ACTIVE = 1 }, param);
            if (allStocks != null)
            {
                foreach (var s in allStocks.Where(x => x.MEDI_STOCK_NAME != null && (x.MEDI_STOCK_NAME.Contains("Ngoại") || x.MEDI_STOCK_NAME.Contains("CSNB") || x.MEDI_STOCK_NAME.Contains("Ninh Bình"))))
                {
                    Console.WriteLine(string.Format("  • Tìm thấy: {0} (ID: {1}) | DeptId: {2}", s.MEDI_STOCK_NAME, s.ID, s.DEPARTMENT_ID));
                    if (targetStockId == 0) targetStockId = s.ID;
                }
            }
        }

        // 3. Check Actrapid beans in stock
        Console.WriteLine("\n=== DANH MỤC ACTRAPID TRONG KHO/TỦ TRỰC ===");
        if (targetStockId > 0)
        {
            var bf = new HisMedicineBeanViewFilter { MEDI_STOCK_ID = targetStockId, IS_ACTIVE = 1 };
            var beans = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", ApiConsumers.MosConsumer, bf, param);
            if (beans != null)
            {
                var actBeans = beans.Where(b => b.MEDICINE_TYPE_NAME != null && (b.MEDICINE_TYPE_NAME.IndexOf("Actrapid", StringComparison.OrdinalIgnoreCase) >= 0 || b.MEDICINE_TYPE_NAME.IndexOf("Insulin", StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                foreach (var b in actBeans)
                {
                    Console.WriteLine(string.Format("  • Thuốc: {0} (MedId: {1} | MedTypeId: {2} | Code: {3}) | Sl tồn: {4}",
                        b.MEDICINE_TYPE_NAME, b.MEDICINE_ID, b.MEDICINE_TYPE_ID, b.MEDICINE_TYPE_CODE, b.AMOUNT));
                }
            }
        }

        // Also search Actrapid medicine type general info
        var mtf = new HisMedicineTypeViewFilter { KEY_WORD = "Actrapid", IS_ACTIVE = 1 };
        var mTypes = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, mtf, param);
        if (mTypes != null)
        {
            Console.WriteLine("\n=== DANH MỤC THUỐC ACTRAPID TRÊN HỆ THỐNG ===");
            foreach (var m in mTypes)
            {
                Console.WriteLine(string.Format("  • {0} (ID: {1} | Code: {2} | ServiceId: {3})", m.MEDICINE_TYPE_NAME, m.ID, m.MEDICINE_TYPE_CODE, m.SERVICE_ID));
            }
        }

        // 4. Paraclinical Bedside Glucose Service (BM02426) & Execute Room in CSNB
        Console.WriteLine("\n=== DỊCH VỤ ĐƯỜNG MÁU MAO MẠCH (BM02426) & PHÒNG THỰC HIỆN TẠI CSNB ===");
        var svf = new HisServiceViewFilter { SERVICE_CODE__EXACT = "BM02426" };
        var svcs = adapter.FetchList<V_HIS_SERVICE>("api/HisService/GetView", ApiConsumers.MosConsumer, svf, param);
        if (svcs != null && svcs.Count > 0)
        {
            var svc = svcs[0];
            Console.WriteLine(string.Format("  • Dịch vụ: {0} (ID: {1} | Code: {2})", svc.SERVICE_NAME, svc.ID, svc.SERVICE_CODE));
        }

        // 5. Patient treatments in Dept 915
        Console.WriteLine("\n=== CHI TIẾT 3 BỆNH NHÂN KHOA NGOẠI TỔNG HỢP CSNB ===");
        long[] trIds = new long[] { 6979004, 7113903, 7049837 };
        foreach (var id in trIds)
        {
            var tf = new HisTreatmentViewFilter { ID = id };
            var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            if (list != null && list.Count > 0)
            {
                var tr = list[0];
                Console.WriteLine(string.Format("\n👉 [{0}] Mã BN: {1} | Mã BA: {2} | TrId: {3} | Tuổi: {4} | Giới: {5}",
                    tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID, 2026 - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)), tr.TDL_PATIENT_GENDER_NAME));
                Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_SUB_CODE ?? tr.ICD_TEXT));

                // Find Bed and Room
                var brf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = id, IS_IN_ROOM = true };
                var brs = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, brf, param);
                if (brs != null && brs.Count > 0)
                {
                    var br = brs.Last();
                    Console.WriteLine(string.Format("   Buồng: {0} (ID: {1}) | Giường: {2} (ID: {3})", br.BED_ROOM_NAME, br.BED_ROOM_ID, br.BED_NAME, br.BED_ID));
                }

                // Check DHST / Vital signs
                var dhstFilter = new HisDhstViewFilter { TREATMENT_ID = id };
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstFilter, param);
                if (dhsts != null && dhsts.Count > 0)
                {
                    var lastDhst = dhsts.OrderByDescending(x => x.EXECUTE_TIME).FirstOrDefault();
                    Console.WriteLine(string.Format("   DHST gần nhất: Mạch {0} | HA {1}/{2} | Nhiệt {3}°C | SpO2 {4}%",
                        lastDhst.PULSE, lastDhst.BLOOD_PRESSURE_MAX, lastDhst.BLOOD_PRESSURE_MIN, lastDhst.TEMPERATURE, lastDhst.SPO2));
                }
            }
        }
    }
}
