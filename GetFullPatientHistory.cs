using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            var requestedName = new System.Reflection.AssemblyName(resolveArgs.Name).Name;
            string[] searchPaths = new string[]
            {
                Path.Combine(baseDir, requestedName + ".dll"),
                Path.Combine(baseDir, "ReferencedAssemblies", requestedName + ".dll"),
                Path.Combine(baseDir, "HisAutoPrescribe_Portable", requestedName + ".dll"),
                Path.Combine(baseDir, "Integrate", "EMR", requestedName + ".dll")
            };
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try { return System.Reflection.Assembly.LoadFrom(path); } catch { }
                }
            }
            return null;
        };

        return Run(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(string[] args)
    {
        string token = ReadToken();
        var mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var cp = new CommonParam();

        string treatCode = args.Length > 0 ? args[0] : "000007345919";
        var tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = treatCode };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
        if (trs == null || trs.Count == 0)
        {
            Console.WriteLine("Không tìm thấy đợt điều trị: " + treatCode);
            return 1;
        }
        var tr = trs[0];

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 BỆNH NHÂN: {0} ({1}) - Sinh: {2} - Giới: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TDL_PATIENT_DOB, tr.TDL_PATIENT_GENDER_NAME));
        Console.WriteLine(string.Format("   Mã ĐT: {0} | ID: {1} | BHYT: {2}", tr.TREATMENT_CODE, tr.ID, tr.TDL_HEIN_CARD_NUMBER));
        Console.WriteLine(string.Format("   Vào viện: {0} | Ra viện: {1}", tr.IN_TIME, tr.OUT_TIME));
        Console.WriteLine(string.Format("   Khoa: {0} | Buồng: {1}", tr.END_DEPARTMENT_NAME, tr.END_ROOM_NAME));
        Console.WriteLine(string.Format("   Chẩn đoán vào viện: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT));

        // 1. Dịch vụ SereServ
        var ssFilter = new HisSereServViewFilter { TREATMENT_ID = tr.ID };
        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssFilter, cp) ?? new List<V_HIS_SERE_SERV>();
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("📋 1. DANH SÁCH DỊCH VỤ THỰC HIỆN TẠI KHOA NỘI TRÚ (" + sss.Count + ")");
        Console.WriteLine("===============================================================================");
        foreach (var s in sss.OrderBy(x => x.TDL_INTRUCTION_TIME))
        {
            Console.WriteLine(string.Format("  [{0}] {1} - {2} | SL: {3} | Phòng: {4} | Khoa: {5} | BS: {6} ({7})",
                s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.AMOUNT, s.EXECUTE_ROOM_NAME, s.REQUEST_DEPARTMENT_NAME, s.TDL_REQUEST_USERNAME, s.TDL_REQUEST_LOGINNAME));
        }

        // 2. Tra cứu ServiceReq & CĐHA
        var reqFilter = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, reqFilter, cp) ?? new List<V_HIS_SERVICE_REQ>();
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("🖼️ 2. KẾT QUẢ KẾT LUẬN CĐHA / TDCN / XÉT NGHIỆM");
        Console.WriteLine("===============================================================================");
        foreach (var req in reqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3 || x.SERVICE_REQ_TYPE_ID == 4 || x.SERVICE_REQ_TYPE_ID == 5))
        {
            var ssReqFilter = new HisSereServViewFilter { SERVICE_REQ_ID = req.ID };
            var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssReqFilter, cp);
            if (ssList != null)
            {
                foreach (var ss in ssList)
                {
                    var extFilter = new HisSereServExtFilter { SERE_SERV_ID = ss.ID };
                    var exts = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, extFilter, cp);
                    if (exts != null && exts.Count > 0)
                    {
                        foreach (var ext in exts)
                        {
                            Console.WriteLine(string.Format("  * [{0}] {1}\n    Mô tả: {2}", ss.TDL_SERVICE_NAME, ext.CONCLUDE, ext.DESCRIPTION));
                        }
                    }
                }
            }
        }

        // 3. Toàn bộ tờ điều trị theo thứ tự thời gian
        var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
        var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp) ?? new List<V_HIS_TRACKING>();
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine("📝 3. DIỄN BIẾN TOÀN BỘ TỜ ĐIỀU TRỊ (" + trks.Count + " TỜ)");
        Console.WriteLine("===============================================================================");
        foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
        {
            Console.WriteLine(string.Format("  [ID: {0} | Time: {1} | BS: {2}]", t.ID, t.TRACKING_TIME, t.CREATOR));
            Console.WriteLine("  DIỄN BIẾN:\n" + t.CONTENT);
            Console.WriteLine("  XỬ TRÍ / Y LỆNH:\n" + t.MEDICAL_INSTRUCTION);
            Console.WriteLine("  -------------------------------------------------------------");
        }

        return 0;
    }

    private static string ReadToken()
    {
        string[] candidates = new string[] {
            "doctor_hn.token",
            "doctor_standalone.token",
            "doctor_nb.token"
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                string text = File.ReadAllText(c, Encoding.UTF8).Trim();
                var parts = text.Split('|');
                if (parts.Length > 0 && parts[0].Length == 64) return parts[0];
            }
        }
        return null;
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return base.Get<List<T>>(uri, consumer, filter, param);
    }
}
