using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Data;
using Oracle.ManagedDataAccess.Client;
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

        string pCode = "0004076093";
        var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
        var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
        if (trs == null || trs.Count == 0)
        {
            Console.WriteLine("Không tìm thấy BN");
            return 1;
        }
        var tr = trs[0];

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("🏥 THÔNG TIN CHI TIẾT: {0} ({1}) | Mã ĐT: {2} | ID: {3}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID));
        Console.WriteLine(string.Format("   Vào viện: {0} | Ra viện: {1} | ICD: [{2}] {3} | ICD Phụ: {4}", 
            tr.IN_TIME, tr.OUT_TIME, tr.ICD_CODE, tr.ICD_NAME, tr.ICD_SUB_CODE));
        Console.WriteLine(string.Format("   Chẩn đoán vào: {0} | Chẩn đoán ra: {1}", tr.ICD_TEXT, tr.END_ORDER_TEXT));

        // 1. Dịch vụ & Y lệnh đã chỉ định
        var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
        var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp) ?? new List<V_HIS_SERVICE_REQ>();
        Console.WriteLine("\n--- 1. DANH SÁCH Y LỆNH & DỊCH VỤ (TỔNG SỐ: " + reqs.Count + ") ---");
        foreach (var r in reqs.OrderBy(x => x.INTRUCTION_TIME))
        {
            Console.WriteLine(string.Format("  [{0}] {1} ({2}) | Type: {3} | Status: {4} | BS: {5} ({6}) | Khoa: {7} -> Phòng: {8}",
                r.INTRUCTION_TIME, r.SERVICE_REQ_CODE, r.SERVICE_REQ_TYPE_NAME, r.SERVICE_REQ_TYPE_ID, r.SERVICE_REQ_STT_ID, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME, r.REQUEST_DEPARTMENT_NAME, r.EXECUTE_ROOM_NAME));
        }

        // 2. Tờ điều trị chi tiết
        var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
        var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp) ?? new List<V_HIS_TRACKING>();
        Console.WriteLine("\n--- 2. TỜ ĐIỀU TRỊ (" + trks.Count + " TỜ) ---");
        foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
        {
            Console.WriteLine(string.Format("  [ID: {0} | Time: {1} | BS: {2}]", t.ID, t.TRACKING_TIME, t.CREATOR));
            Console.WriteLine("  DIỄN BIẾN: " + t.CONTENT);
            Console.WriteLine("  XỬ TRÍ: " + t.MEDICAL_INSTRUCTION);
            Console.WriteLine("  -------------------------------------------------------------");
        }

        // 3. Tra cứu PTTT / Tường trình phẫu thuật
        var ssFilter = new HisSereServViewFilter { TDL_TREATMENT_ID = tr.ID };
        var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssFilter, cp) ?? new List<V_HIS_SERE_SERV>();
        Console.WriteLine("\n--- 3. CÁC DỊCH VỤ THỰC HIỆN CHI TIẾT (" + sss.Count + ") ---");
        foreach (var s in sss)
        {
            Console.WriteLine(string.Format("  - {0} | Tên: {1} | SL: {2} | Khoa/Phòng: {3}", 
                s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME, s.AMOUNT, s.EXECUTE_ROOM_NAME));
        }

        // 4. Tra cứu Oracle EMR
        InspectOracleEmr(tr.TREATMENT_CODE);

        return 0;
    }

    static void InspectOracleEmr(string treatmentCode)
    {
        Console.WriteLine("\n--- 4. DỮ LIỆU HIỆN TẠI TRONG ORACLE EMR ---");
        try
        {
            long maQuanLy = long.Parse(treatmentCode);
            string connStr = "Data Source=192.168.7.241:1521/EMR;User Id=EMR_FINAL;Password=EMR_FINAL;";
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BENHANNGOAIKHOA WHERE MaQuanLy = :mql";
                    cmd.Parameters.Add(new OracleParameter("mql", maQuanLy));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string name = reader.GetName(i);
                                object val = reader.GetValue(i);
                                if (val != null && val != DBNull.Value && !string.IsNullOrEmpty(val.ToString()))
                                {
                                    Console.WriteLine(string.Format("  EMR.[{0}] = {1}", name, val.ToString()));
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("  Không tìm thấy bản ghi trong BENHANNGOAIKHOA");
                        }
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM THONGTINDIEUTRI WHERE MaQuanLy = :mql";
                    cmd.Parameters.Add(new OracleParameter("mql", maQuanLy));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string name = reader.GetName(i);
                                object val = reader.GetValue(i);
                                if (val != null && val != DBNull.Value && !string.IsNullOrEmpty(val.ToString()))
                                {
                                    Console.WriteLine(string.Format("  THONGTINDIEUTRI.[{0}] = {1}", name, val.ToString()));
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("  Không tìm thấy bản ghi trong THONGTINDIEUTRI");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  Lỗi Oracle EMR: " + ex.Message);
        }
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
    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return base.Post<T>(uri, consumer, data, param);
    }
}
