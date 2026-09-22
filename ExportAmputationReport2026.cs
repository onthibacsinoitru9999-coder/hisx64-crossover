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
        FinalAmputationReport.Run();
    }
}

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class FinalAmputationReport
{
    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token").Split('|')[0];
        ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        // 1. Danh sách các bệnh nhân ứng viên cắt cụt chi tại Ninh Bình năm 2026
        string[] candidateCodes = new string[] {
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

        Console.WriteLine("==========================================================================================================");
        Console.WriteLine("📊 DANH SÁCH CHI TIẾT BỆNH NHÂN CẮT CỤT CHI TẠI PHÒNG MỔ CƠ SỞ NINH BÌNH TRONG NĂM 2026");
        Console.WriteLine("==========================================================================================================");

        int stt = 1;

        foreach (var pCode in candidateCodes)
        {
            // Lấy tất cả đợt điều trị của bệnh nhân này
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            param = new CommonParam();
            var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            if (tList == null || tList.Count == 0) continue;

            // Lấy tất cả các phiếu service req của toàn bộ đợt điều trị năm 2026
            var t2026 = tList.Where(x => x.IN_TIME.ToString().StartsWith("2026")).ToList();
            if (t2026.Count == 0) continue;

            var tIds = t2026.Select(x => x.ID).ToList();
            var srf = new HisServiceReqViewFilter { TREATMENT_IDs = tIds };
            param = new CommonParam();
            param.Limit = 1000;
            var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
            if (allReqs == null || allReqs.Count == 0) continue;

            var reqIds = allReqs.Select(x => x.ID).ToList();
            var ssf = new HisSereServViewFilter { SERVICE_REQ_IDs = reqIds };
            param = new CommonParam();
            param.Limit = 2000;
            var allSereServ = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);

            List<V_HIS_SERE_SERV_PTTT> ptttList = new List<V_HIS_SERE_SERV_PTTT>();
            if (allSereServ != null && allSereServ.Count > 0)
            {
                var ssIds = allSereServ.Select(x => x.ID).ToList();
                var pf = new HisSereServPtttViewFilter { SERE_SERV_IDs = ssIds };
                param = new CommonParam();
                param.Limit = 2000;
                var pts = adapter.FetchList<V_HIS_SERE_SERV_PTTT>("api/HisSereServPttt/GetView", mosConsumer, pf, param);
                if (pts != null) ptttList = pts;
            }

            // Lọc các dịch vụ có từ khóa cắt cụt / mỏm cụt / tháo khớp
            var ampServices = allSereServ != null ? allSereServ.Where(s => {
                string sn = (s.TDL_SERVICE_NAME ?? "").ToLower();
                return sn.Contains("cắt cụt chi") || sn.Contains("mỏm cụt") || sn.Contains("tháo khớp") || sn.Contains("cắt cụt ngón");
            }).ToList() : new List<V_HIS_SERE_SERV>();

            // Nếu không có dịch vụ, kiểm tra xem có biên bản mổ cắt cụt chi không
            if (ampServices.Count == 0)
            {
                var ptAmps = ptttList.Where(p => {
                    string m = (p.PTTT_METHOD_NAME ?? "").ToLower();
                    return m.Contains("cắt cụt") || m.Contains("mỏm cụt") || m.Contains("tháo khớp");
                }).ToList();
                if (ptAmps.Count > 0)
                {
                    foreach (var p in ptAmps)
                    {
                        var s = allSereServ.FirstOrDefault(x => x.ID == p.SERE_SERV_ID);
                        if (s != null && !ampServices.Contains(s)) ampServices.Add(s);
                    }
                }
            }

            // Loại trừ cắt cụt trực tràng (TRẦN THỊ LÊ)
            if (pCode == "0003015753") continue;

            // Kiểm tra xem bệnh nhân có thực sự được phẫu thuật cắt cụt chi trong năm 2026 tại Ninh Bình không
            if (ampServices.Count == 0)
            {
                // Kiểm tra xem chẩn đoán là "đã phẫu thuật cắt cụt" từ trước hay mới
                Console.WriteLine(string.Format("ℹ️ [TIỀN SỬ / KHÔNG PHẪU THUẬT TẠI NB 2026] BN: {0} ({1}) - Mã BN: {2}", 
                    t2026[0].TDL_PATIENT_NAME, t2026[0].ICD_NAME, pCode));
                continue;
            }

            // IN CHI TIẾT BỆNH NHÂN CẮT CỤT CHI CHÍNH THỨC
            var tr = t2026.FirstOrDefault(x => x.ID == ampServices[0].TDL_TREATMENT_ID) ?? t2026[0];

            string dobStr = tr.TDL_PATIENT_DOB.ToString();
            string yob = dobStr.Length >= 4 ? dobStr.Substring(0, 4) : "";
            int y;
            int age = DateTime.Now.Year - (int.TryParse(yob, out y) ? y : DateTime.Now.Year);

            Console.WriteLine(string.Format("\n#{0}. BỆNH NHÂN: {1} | TUỔI: {2} ({3}) | GIỚI: {4}",
                stt++, tr.TDL_PATIENT_NAME.ToUpper(), age, yob, tr.TDL_PATIENT_GENDER_NAME));
            Console.WriteLine(string.Format("   - Mã Bệnh Nhân: {0} | Mã Đợt Điều Trị (BA): {1}", tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            Console.WriteLine(string.Format("   - Địa chỉ: {0}", tr.TDL_PATIENT_ADDRESS));
            Console.WriteLine(string.Format("   - Khoa điều trị: {0} (Cơ sở: Bạch Mai Ninh Bình - Branch {1})", tr.END_DEPARTMENT_NAME, tr.BRANCH_ID));
            Console.WriteLine(string.Format("   - Vào viện: {0} | Ra viện: {1}", tr.IN_TIME, tr.OUT_TIME.HasValue ? tr.OUT_TIME.Value.ToString() : "Đang nằm viện điều trị"));
            Console.WriteLine(string.Format("   - Chẩn đoán bệnh chính: {0} (ICD: {1})", tr.ICD_NAME ?? tr.ICD_TEXT, tr.ICD_CODE));
            if (!string.IsNullOrEmpty(tr.ICD_SUB_CODE))
            {
                Console.WriteLine(string.Format("   - Mã bệnh kèm theo: {0}", tr.ICD_SUB_CODE));
            }

            Console.WriteLine("   🔪 CHI TIẾT PHẪU THUẬT CẮT CỤT CHI TẠI PHÒNG MỔ:");
            foreach (var s in ampServices)
            {
                var req = allReqs.FirstOrDefault(x => x.ID == s.SERVICE_REQ_ID);
                var pt = ptttList.FirstOrDefault(x => x.SERE_SERV_ID == s.ID);

                string reqTime = req != null ? req.INTRUCTION_TIME.ToString() : s.TDL_INTRUCTION_TIME.ToString();
                string roomName = req != null ? req.EXECUTE_ROOM_NAME : "";

                Console.WriteLine(string.Format("     * Phẫu thuật: [{0}] {1}", s.TDL_SERVICE_CODE, s.TDL_SERVICE_NAME));
                Console.WriteLine(string.Format("       + Thời gian mổ: {0} | Phòng mổ: {1} (Mã phiếu: {2})", reqTime, roomName, req != null ? req.SERVICE_REQ_CODE : ""));
                if (pt != null)
                {
                    Console.WriteLine(string.Format("       + Phương pháp phẫu thuật: {0}", pt.PTTT_METHOD_NAME));
                    Console.WriteLine(string.Format("       + Chẩn đoán trước mổ: {0}", pt.BEFORE_PTTT_ICD_NAME));
                    Console.WriteLine(string.Format("       + Chẩn đoán sau mổ: {0}", pt.AFTER_PTTT_ICD_NAME));
                }
            }
        }
    }
}
