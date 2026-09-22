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

        // 1. Chi tiết BN NGUYỄN THỊ ÚT (TID: 7217332)
        Console.WriteLine("=================== CHI TIẾT NGUYỄN THỊ ÚT (TID: 7217332) ===================");
        long utTid = 7217332;
        var utTrList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { ID = utTid }, param);
        var utTr = (utTrList != null && utTrList.Count > 0) ? utTrList[0] : null;
        var utTbrList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mos, new HisTreatmentBedRoomViewFilter { TREATMENT_ID = utTid, IS_IN_ROOM = true }, param);
        var utTbr = (utTbrList != null && utTbrList.Count > 0) ? utTbrList[0] : null;

        if (utTr != null)
        {
            Console.WriteLine(string.Format("BN: {0} | Tuổi: {1} | Buồng: {2} | Giường: {3} | Chẩn đoán: [{4}] {5} | Chi tiết: {6}",
                utTr.TDL_PATIENT_NAME, 2026 - int.Parse(utTr.TDL_PATIENT_DOB.ToString().Substring(0, 4)),
                utTbr != null ? utTbr.BED_ROOM_NAME : "K rõ", utTbr != null ? utTbr.BED_NAME : "K rõ",
                utTr.ICD_CODE, utTr.ICD_NAME, utTr.ICD_TEXT));
        }

        var utTrks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, new HisTrackingViewFilter { TREATMENT_ID = utTid }, param);
        if (utTrks != null)
        {
            foreach (var tk in utTrks.OrderBy(x => x.TRACKING_TIME))
            {
                Console.WriteLine(string.Format("Tờ ĐT [{0} - BS {1}]: {2} | YL: {3}", tk.TRACKING_TIME, tk.CREATOR, tk.CONTENT, tk.CARE_INSTRUCTION));
            }
        }

        var utReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = utTid }, param);
        if (utReqs != null)
        {
            foreach (var r in utReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3 || x.SERVICE_REQ_TYPE_ID == 9))
            {
                var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = r.ID }, param);
                if (sss != null)
                {
                    foreach (var s in sss)
                    {
                        var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = s.ID }, param);
                        if (sses != null)
                        {
                            foreach (var se in sses)
                            {
                                if (!string.IsNullOrEmpty(se.DESCRIPTION) || !string.IsNullOrEmpty(se.CONCLUDE))
                                {
                                    Console.WriteLine(string.Format("* [{0}] {1} (YL: {2}):\n  Mô tả: {3}\n  Kết luận: {4}",
                                        r.INTRUCTION_TIME, s.TDL_SERVICE_NAME, r.SERVICE_REQ_CODE, se.DESCRIPTION, se.CONCLUDE));
                                }
                            }
                        }
                    }
                }
            }
        }

        // 2. Chi tiết BN PHẠM THỊ LIÊN (Mã BN: 0004046311)
        Console.WriteLine("\n=================== TẤT CẢ HỒ SƠ PHẠM THỊ LIÊN (0004046311) ===================");
        var lienTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { KEY_WORD = "0004046311" }, param);
        if (lienTreats != null)
        {
            foreach (var lt in lienTreats)
            {
                Console.WriteLine(string.Format("TID: {0} | Code: {1} | In: {2} | Out: {3} | ICD: [{4}] {5} | {6}",
                    lt.ID, lt.TREATMENT_CODE, lt.IN_TIME, lt.OUT_TIME, lt.ICD_CODE, lt.ICD_NAME, lt.ICD_TEXT));

                var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, new HisTrackingViewFilter { TREATMENT_ID = lt.ID }, param);
                if (trks != null)
                {
                    foreach (var tk in trks)
                    {
                        Console.WriteLine(string.Format("  Tờ ĐT [{0} - BS {1}]: {2}", tk.TRACKING_TIME, tk.CREATOR, tk.CONTENT));
                    }
                }

                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = lt.ID }, param);
                if (reqs != null)
                {
                    foreach (var r in reqs)
                    {
                        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = r.ID }, param);
                        if (sss != null)
                        {
                            foreach (var s in sss)
                            {
                                var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = s.ID }, param);
                                if (sses != null)
                                {
                                    foreach (var se in sses)
                                    {
                                        if (!string.IsNullOrEmpty(se.DESCRIPTION) || !string.IsNullOrEmpty(se.CONCLUDE))
                                        {
                                            Console.WriteLine(string.Format("  * [{0}] {1}: {2} | Kết luận: {3}", r.INTRUCTION_TIME, s.TDL_SERVICE_NAME, se.DESCRIPTION, se.CONCLUDE));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
