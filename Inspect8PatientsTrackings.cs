using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string token = File.ReadAllText(@"doctor_hn.token").Trim();
        var mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HN_TEST");
        var adapter = new MyAdapter();
        var cp = new CommonParam();

        string[] patientCodes = new string[] {
            "0002417556", "0004039468", "0004080629", "0004061261",
            "0004062280", "0004093319", "0002038416", "0004076093"
        };

        foreach (var pCode in patientCodes)
        {
            var pFilter = new HisPatientViewFilter { PATIENT_CODE = pCode };
            var pts = adapter.FetchList<V_HIS_PATIENT>("api/HisPatient/GetView", mos, pFilter, cp);
            if (pts == null || pts.Count == 0) continue;
            var pt = pts[0];

            var trFilter = new HisTreatmentViewFilter { PATIENT_ID = pt.ID };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, trFilter, cp);
            if (trs == null || trs.Count == 0) continue;
            var tr = trs.OrderByDescending(x => x.IN_TIME).First();

            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp);

            Console.WriteLine("===============================================================================");
            Console.WriteLine(string.Format("BN: {0} ({1}) | Mã ĐT: {2} (ID: {3})", pt.VIR_PATIENT_NAME, pt.PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
            if (trks != null)
            {
                foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
                {
                    Console.WriteLine(string.Format("  [ID: {0}] Time: {1} | Creator: {2} | Dept: {3}", t.ID, t.TRACKING_TIME, t.CREATOR, t.DEPARTMENT_ID));
                    Console.WriteLine("    CONTENT:\n" + t.CONTENT);
                    Console.WriteLine("    CARE: " + t.CARE_INSTRUCTION);
                    Console.WriteLine("    MED: " + t.MEDICAL_INSTRUCTION);
                    Console.WriteLine("  -------------------------------------------------------------");
                }
            }
        }
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return base.Get<List<T>>(uri, consumer, filter, param);
    }
    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return base.Post<T>(uri, consumer, data, param);
    }
}
