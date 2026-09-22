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

        var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { KEY_WORD = "PHẠM THỊ LIÊN" }, param);
        if (list != null)
        {
            var p1991 = list.Where(x => x.TDL_PATIENT_DOB.ToString().StartsWith("1991") || x.TDL_PATIENT_DOB.ToString().StartsWith("1990") || x.TDL_PATIENT_DOB.ToString().StartsWith("1992")).ToList();
            Console.WriteLine("Tìm thấy BN sinh ~1991: " + p1991.Count);
            foreach (var p in p1991.OrderByDescending(x => x.IN_TIME))
            {
                Console.WriteLine(string.Format("TID: {0} | Code: {1} | BN: {2} ({3}) | DOB: {4} | In: {5} | Khoa: {6} | ICD: [{7}] {8} | {9}",
                    p.ID, p.TREATMENT_CODE, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TDL_PATIENT_DOB, p.IN_TIME, p.END_DEPARTMENT_NAME, p.ICD_CODE, p.ICD_NAME, p.ICD_TEXT));

                // Lấy CĐHA
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = p.ID }, param);
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
                                            Console.WriteLine(string.Format("  -> [{0}] {1}: {2} | Kết luận: {3}", r.INTRUCTION_TIME, s.TDL_SERVICE_NAME, se.DESCRIPTION, se.CONCLUDE));
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
