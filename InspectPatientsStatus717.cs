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
using MOS.SDO;

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
    static void Main(string[] args)
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

    static string GetLiveToken()
    {
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line, token = "";
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
            return token;
        }
    }

    static void Run()
    {
        string token = GetLiveToken();
        if (string.IsNullOrEmpty(token)) { Console.WriteLine("LỖI: Không tìm thấy Token!"); return; }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        long[] treatmentIds = new long[] { 7309184, 7252303, 7332069 };

        foreach (var tId in treatmentIds)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = tId };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            var tr = trs != null && trs.Count > 0 ? trs[0] : null;

            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("🧑 BN: {0} | Mã BN: {1} | Mã ĐT: {2} | TreatmentId: {3}", 
                tr != null ? tr.TDL_PATIENT_NAME : "Unknown",
                tr != null ? tr.TDL_PATIENT_CODE : "",
                tr != null ? tr.TREATMENT_CODE : "",
                tId));
            if (tr != null)
            {
                Console.WriteLine(string.Format("   Chẩn đoán: [{0}] {1} | Phụ: [{2}] {3}", 
                    tr.ICD_CODE, tr.ICD_NAME, tr.ICD_SUB_CODE, tr.ICD_TEXT));
                Console.WriteLine(string.Format("   Khoa hiện tại (ID): {0}", tr.LAST_DEPARTMENT_ID));
            }

            // Bed room
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { TREATMENT_ID = tId, IS_IN_ROOM = true };
            var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mos, tbrf, cp);
            if (beds != null && beds.Count > 0)
            {
                Console.WriteLine(string.Format("   Buồng/Giường: {0} / {1} (BedRoomId: {2})", beds[0].BED_ROOM_NAME, beds[0].BED_NAME, beds[0].BED_ROOM_ID));
            }

            // Rations
            HisSereServRationViewFilter rf = new HisSereServRationViewFilter { TREATMENT_ID = tId };
            var rations = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
            Console.WriteLine("   🍚 Suất ăn từ 29/09 đến 02/10:");
            string[] checkDates = new string[] { "20260929", "20260930", "20261001", "20261002" };
            foreach (var d in checkDates)
            {
                var rOnDate = rations != null ? rations.Where(x => x.INTRUCTION_TIME.ToString().StartsWith(d)).ToList() : new List<V_HIS_SERE_SERV_RATION>();
                if (rOnDate.Count > 0)
                {
                    string details = string.Join("; ", rOnDate.Select(x => string.Format("Bữa {0}: {1}", x.RATION_TIME_ID, x.SERVICE_NAME)));
                    Console.WriteLine(string.Format("     • {0}: Có {1} bữa -> {2}", d, rOnDate.Count, details));
                }
                else
                {
                    Console.WriteLine(string.Format("     • {0}: CHƯA CÓ", d));
                }
            }

            // Prescriptions
            HisExpMestMedicineViewFilter medFilter = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId };
            var meds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, medFilter, cp);
            Console.WriteLine("   💊 Đơn thuốc theo TDL_INTRUCTION_TIME:");
            if (meds != null && meds.Count > 0)
            {
                var dates = meds.Select(x => (x.TDL_INTRUCTION_TIME ?? x.EXP_TIME ?? 0).ToString().Substring(0, 8)).Distinct().OrderByDescending(x => x).ToList();
                foreach (var d in dates)
                {
                    var dMeds = meds.Where(x => (x.TDL_INTRUCTION_TIME ?? x.EXP_TIME ?? 0).ToString().StartsWith(d)).ToList();
                    Console.WriteLine(string.Format("     📅 Ngày Y lệnh {0} ({1} thuốc):", d, dMeds.Count));
                    foreach (var m in dMeds)
                    {
                        Console.WriteLine(string.Format("        - {0} | SL: {1} {2} | Kho: {3} ({4}) | Cữ: S:{5} Tr:{6} C:{7} T:{8} | InsTime: {9} | CreateTime: {10}",
                            m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID,
                            m.MORNING, m.NOON, m.AFTERNOON, m.EVENING, m.TDL_INTRUCTION_TIME, m.CREATE_TIME));
                    }
                }
            }
            else
            {
                Console.WriteLine("     ⚠️ Không có đơn thuốc nào trong lịch sử!");
            }
        }
    }
}
