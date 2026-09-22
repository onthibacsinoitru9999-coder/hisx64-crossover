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
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class Program
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string name = new AssemblyName(a.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", name);
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            return null;
        };
        RealMain();
    }

    static void RealMain()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token", Encoding.UTF8).Split('|')[0];
        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        long[] tIds = new long[] { 7163469, 7163700, 7163879, 7200072, 7180230, 7160889, 7172565 };

        for (int i = 0; i < tIds.Length; i++)
        {
            long tid = tIds[i];
            var tr = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { ID = tid }, param).FirstOrDefault();
            var tbr = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mos, new HisTreatmentBedRoomViewFilter { TREATMENT_ID = tid, IS_IN_ROOM = true }, param).FirstOrDefault();
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, new HisTrackingViewFilter { TREATMENT_ID = tid, ORDER_FIELD = "TRACKING_TIME", ORDER_DIRECTION = "DESC" }, param);

            Console.WriteLine(string.Format("==============================================================================="));
            Console.WriteLine(string.Format("BN {0}: {1} | Tuổi: {2} | Giới: {3} | Mã BN: {4} | Mã ĐT: {5}",
                i + 1, tr.TDL_PATIENT_NAME, DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)),
                tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            Console.WriteLine(string.Format("Buồng/Giường: {0} - {1} | Vào viện: {2} | BHYT: {3}",
                tbr != null ? tbr.BED_ROOM_NAME : "K rõ", tbr != null ? tbr.BED_NAME : "K rõ", tr.IN_TIME, tr.TDL_HEIN_CARD_NUMBER));
            Console.WriteLine(string.Format("Chẩn đoán: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT));

            // All Trackings
            if (trks != null && trks.Count > 0)
            {
                Console.WriteLine(string.Format("Tờ điều trị (Tổng {0} tờ, hiển thị 3 tờ gần nhất):", trks.Count));
                foreach (var tk in trks.Take(3))
                {
                    Console.WriteLine(string.Format("  * Time: {0} | BS: {1} ({2})", tk.TRACKING_TIME, tk.CREATOR, tk.ROOM_NAME));
                    Console.WriteLine(string.Format("    Diễn biến: {0}", tk.CONTENT != null ? tk.CONTENT.Replace("\r\n", " | ").Replace("\n", " | ") : ""));
                    Console.WriteLine(string.Format("    Y lệnh: {0}", tk.CARE_INSTRUCTION != null ? tk.CARE_INSTRUCTION.Replace("\r\n", " | ").Replace("\n", " | ") : ""));
                }
            }
        }
    }
}
