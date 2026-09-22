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

        // 1. Tìm NGUYỄN THỊ ÚT ~76t (sinh 1950)
        Console.WriteLine("=================== TÌM TẤT CẢ BN NGUYỄN THỊ ÚT ===================");
        var utList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { KEY_WORD = "NGUYỄN THỊ ÚT" }, param);
        if (utList != null)
        {
            foreach (var u in utList.OrderByDescending(x => x.IN_TIME))
            {
                int age = DateTime.Now.Year - int.Parse(u.TDL_PATIENT_DOB.ToString().Substring(0, 4));
                Console.WriteLine(string.Format("TID: {0} | BN: {1} | Tuổi: {2} | Mã BN: {3} | Vào: {4} | Khoa: {5} | ICD: [{6}] {7} | {8}",
                    u.ID, u.TDL_PATIENT_NAME, age, u.TDL_PATIENT_CODE, u.IN_TIME, u.END_DEPARTMENT_NAME, u.ICD_CODE, u.ICD_NAME, u.ICD_TEXT));
            }
        }

        // 2. PHẠM THỊ LIÊN: kiểm tra tất cả treatments và dịch vụ
        Console.WriteLine("\n=================== TẤT CẢ ĐỢT CỦA PHẠM THỊ LIÊN ===================");
        var lienList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { KEY_WORD = "PHẠM THỊ LIÊN" }, param);
        if (lienList != null)
        {
            foreach (var l in lienList.OrderByDescending(x => x.IN_TIME).Take(5))
            {
                int age = DateTime.Now.Year - int.Parse(l.TDL_PATIENT_DOB.ToString().Substring(0, 4));
                Console.WriteLine(string.Format("TID: {0} | Tuổi: {1} | Vào: {2} | Khoa: {3} | ICD: [{4}] {5} | {6}",
                    l.ID, age, l.IN_TIME, l.END_DEPARTMENT_NAME, l.ICD_CODE, l.ICD_NAME, l.ICD_TEXT));
                
                // Lấy tất cả CĐHA của TID này
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = l.ID }, param);
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
                                        if (!string.IsNullOrEmpty(se.CONCLUDE) || !string.IsNullOrEmpty(se.DESCRIPTION))
                                        {
                                            Console.WriteLine(string.Format("   -> [{0}] {1}: {2} | Kết luận: {3}", r.INTRUCTION_TIME, s.TDL_SERVICE_NAME, se.DESCRIPTION, se.CONCLUDE));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 3. ĐOÀN THỊ LỰU: kiểm tra các đợt khác hoặc tất cả CĐHA
        Console.WriteLine("\n=================== TẤT CẢ ĐỢT CỦA ĐOÀN THỊ LỰU ===================");
        var luuList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, new HisTreatmentViewFilter { KEY_WORD = "ĐOÀN THỊ LỰU" }, param);
        if (luuList != null)
        {
            foreach (var lu in luuList.OrderByDescending(x => x.IN_TIME).Take(5))
            {
                Console.WriteLine(string.Format("TID: {0} | Vào: {1} | Khoa: {2} | ICD: [{3}] {4} | {5}",
                    lu.ID, lu.IN_TIME, lu.END_DEPARTMENT_NAME, lu.ICD_CODE, lu.ICD_NAME, lu.ICD_TEXT));

                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = lu.ID }, param);
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
                                        if (!string.IsNullOrEmpty(se.CONCLUDE) || !string.IsNullOrEmpty(se.DESCRIPTION))
                                        {
                                            Console.WriteLine(string.Format("   -> [{0}] {1}: {2} | Kết luận: {3}", r.INTRUCTION_TIME, s.TDL_SERVICE_NAME, se.DESCRIPTION, se.CONCLUDE));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 4. NGÔ VĂN XƯNG: Đọc kỹ mô tả MRI thắt lưng tiêm tương phản 000090386528
        Console.WriteLine("\n=================== MRI CHI TIẾT NGÔ VĂN XƯNG ===================");
        var xungReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { SERVICE_REQ_CODE = "000090386528" }, param);
        if (xungReqs != null && xungReqs.Count > 0)
        {
            var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = xungReqs[0].ID }, param);
            if (sss != null)
            {
                foreach (var s in sss)
                {
                    var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = s.ID }, param);
                    if (sses != null)
                    {
                        foreach (var se in sses)
                        {
                            Console.WriteLine("Mô tả MRI: " + se.DESCRIPTION);
                            Console.WriteLine("Kết luận MRI: " + se.CONCLUDE);
                        }
                    }
                }
            }
        }
    }
}
