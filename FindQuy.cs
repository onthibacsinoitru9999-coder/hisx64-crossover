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

        var tf = new HisTreatmentViewFilter
        {
            KEY_WORD = "TRẦN VĂN QUÝ"
        };
        var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, param);
        if (list == null || list.Count == 0)
        {
            tf.KEY_WORD = "TRAN VAN QUY";
            list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, param);
        }

        Console.WriteLine("Tìm thấy: " + (list != null ? list.Count : 0));
        if (list != null)
        {
            foreach (var tr in list.OrderByDescending(x => x.IN_TIME))
            {
                Console.WriteLine(string.Format("TrID: {0} | Mã BN: {1} | Mã ĐT: {2} | Tên: {3} | Tuổi: {4} | Giới: {5} | Khoa: {6} | Vào: {7} | Ra: {8} | IsPause: {9} | ICD: [{10}] {11}",
                    tr.ID, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.TDL_PATIENT_NAME,
                    tr.TDL_PATIENT_DOB > 0 ? (DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "?",
                    tr.TDL_PATIENT_GENDER_NAME, tr.LAST_DEPARTMENT_NAME, tr.IN_TIME, tr.OUT_TIME, tr.IS_PAUSE, tr.ICD_CODE, tr.ICD_NAME));
            }
        }
    }
}
