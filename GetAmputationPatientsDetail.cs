using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

class EntryPoint
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = e.Name.Split(',')[0];
            string cur = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = new string[]
            {
                Path.Combine(cur, "ReferencedAssemblies", shortName + ".dll"),
                Path.Combine(cur, "Plugins", "Module", shortName + ".dll"),
                Path.Combine(cur, shortName + ".dll")
            };
            foreach (var p in paths) if (File.Exists(p)) return Assembly.LoadFrom(p);
            return null;
        };

        RunProgram();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void RunProgram()
    {
        AmputationDetailReporter.Run();
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class AmputationDetailReporter
{
    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token").Split('|')[0];
        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        string[] patientCodes = new string[] {
            "0003921198", // NGUYỄN THỊ HẠ
            "0003757466", // NGUYỄN VIỆT TRUNG
            "0003753334", // CÙ VĂN CHIẾN
            "0003800257", // NGUYỄN VĂN MINH
            "0002529251", // HOÀNG VĂN CẨN
            "0003807763", // BÙI THỊ THẠNH
            "0003707731", // TRẦN VĂN MẬU
            "0003894684", // TRỊNH VĂN TAM
            "0003998925", // TRƯƠNG ĐÌNH QUANG
            "0003964825", // PHẠM VĂN TRƯỜNG
            "0003954061", // PHẠM VĂN ĐÔNG
            "0003985686", // LƯU GIANG NAM
            "0004011296", // TRỊNH THỊ THÁP
            "0004009055", // PHẠM VĂN TOÀN
            "0004008437", // TRỊNH THỊ OANH
            "0002320430", // NHỮ NGỌC HOÀNG
            "0003015753"  // TRẦN THỊ LÊ
        };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("CHI TIẾT LÂM SÀNG CÁC CA PHẪU THUẬT CẮT CỤT CHI TẠI CS NINH BÌNH NĂM 2026");
        Console.WriteLine("===============================================================================");

        foreach (var pCode in patientCodes)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            param = new CommonParam();
            var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (tList == null || tList.Count == 0) continue;

            // Lọc các đợt điều trị năm 2026
            var t2026 = tList.Where(x => x.IN_TIME.ToString().StartsWith("2026")).OrderByDescending(x => x.IN_TIME).ToList();
            if (t2026.Count == 0) t2026 = tList.OrderByDescending(x => x.IN_TIME).Take(1).ToList();

            foreach (var tr in t2026)
            {
                // Lấy tất cả phiếu PTTT của đợt này
                var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID, SERVICE_REQ_TYPE_ID = 4 };
                param = new CommonParam();
                var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

                List<V_HIS_SERE_SERV> services = new List<V_HIS_SERE_SERV>();
                List<V_HIS_SERE_SERV_PTTT> ptttList = new List<V_HIS_SERE_SERV_PTTT>();

                if (srs != null && srs.Count > 0)
                {
                    var srIds = srs.Select(x => x.ID).ToList();
                    var ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = srIds };
                    param = new CommonParam();
                    var ss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (ss != null)
                    {
                        services = ss;
                        var ssIds = ss.Select(x => x.ID).ToList();
                        var pf = new HisSereServPtttViewFilter { SERE_SERV_IDs = ssIds };
                        param = new CommonParam();
                        var pts = adapter.FetchList<V_HIS_SERE_SERV_PTTT>("api/HisSereServPttt/GetView", mosConsumer, pf, param);
                        if (pts != null) ptttList = pts;
                    }
                }

                // Lọc các dịch vụ liên quan đến mổ cắt cụt chi
                var ampServices = services.Where(s => {
                    string sn = (s.TDL_SERVICE_NAME ?? "").ToLower();
                    return sn.Contains("cắt cụt") || sn.Contains("mỏm cụt") || sn.Contains("tháo khớp") || sn.Contains("amputation");
                }).ToList();

                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine(string.Format("👤 BN: {0} ({1} tuổi - {2}) | Mã BN: {3} | Mã BA: {4}",
                    tr.TDL_PATIENT_NAME, 
                    DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4)),
                    tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
                Console.WriteLine(string.Format("   - Khoa điều trị: {0} | Chi nhánh: Branch {1}", tr.END_DEPARTMENT_NAME, tr.BRANCH_ID));
                Console.WriteLine(string.Format("   - Thời gian: Vào viện {0} | Ra viện: {1}", 
                    tr.IN_TIME, tr.OUT_TIME.HasValue ? tr.OUT_TIME.Value.ToString() : "Đang nằm viện"));
                Console.WriteLine(string.Format("   - Chẩn đoán ra viện/hiện tại: {0} [{1}]", tr.ICD_NAME ?? tr.ICD_TEXT, tr.ICD_CODE));

                if (ampServices.Count > 0)
                {
                    Console.WriteLine("   🔪 CÁC CAN THIỆP CẮT CỤT CHI GHI NHẬN TẠI PHÒNG MỔ:");
                    foreach (var aserv in ampServices)
                    {
                        var req = srs.FirstOrDefault(x => x.ID == aserv.SERVICE_REQ_ID);
                        var pt = ptttList.FirstOrDefault(x => x.SERE_SERV_ID == aserv.ID);
                        Console.WriteLine(string.Format("      * Dịch vụ: [{0}] {1}", aserv.TDL_SERVICE_CODE, aserv.TDL_SERVICE_NAME));
                        Console.WriteLine(string.Format("        + Thời gian y lệnh: {0} | Nơi làm: {1} (Phòng: {2})",
                            req != null ? req.INTRUCTION_TIME.ToString() : aserv.TDL_INTRUCTION_TIME.ToString(),
                            req != null ? req.EXECUTE_ROOM_NAME : "", req != null ? req.EXECUTE_ROOM_ID.ToString() : ""));
                        if (pt != null)
                        {
                            Console.WriteLine(string.Format("        + Phương pháp mổ: {0}", pt.PTTT_METHOD_NAME));
                            Console.WriteLine(string.Format("        + Chẩn đoán trước mổ: {0}", pt.BEFORE_PTTT_ICD_NAME));
                            Console.WriteLine(string.Format("        + Chẩn đoán sau mổ: {0}", pt.AFTER_PTTT_ICD_NAME));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠️ KHÔNG CÓ DỊCH VỤ CẮT CỤT TRỰC TIẾP (Chỉ có trong chẩn đoán/tiền sử)");
                    var reqWithAmp = srs.Where(r => (r.ICD_NAME ?? "").ToLower().Contains("cụt")).ToList();
                    foreach (var r in reqWithAmp)
                    {
                        Console.WriteLine(string.Format("      * Phiếu PTTT: {0} | Nơi làm: {1} | CĐ: {2}",
                            r.SERVICE_REQ_CODE, r.EXECUTE_ROOM_NAME, r.ICD_NAME));
                    }
                }
            }
        }
    }
}
