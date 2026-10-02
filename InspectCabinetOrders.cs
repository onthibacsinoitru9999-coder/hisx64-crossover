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

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

public class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            
            return null;
        };

        Run();
    }

    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = "";
        string cacheFile = "doctor_standalone.token";
        if (File.Exists(cacheFile))
        {
            token = File.ReadAllText(cacheFile).Split('|')[0].Trim();
        }
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy token.");
            return;
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "MOS");
        var myAdapter = new MyAdapter();
        var param = new CommonParam();

        long[] treatmentIds = new long[] { 7279572, 7324659, 7321326 };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🔍 KIỂM TRA CHI TIẾT ĐƠN THUỐC TỦ TRỰC & Y LỆNH NGÀY 29/09/2026 (HÔM QUA)");
        Console.WriteLine("===============================================================================");

        foreach (var tId in treatmentIds)
        {
            var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, new HisTreatmentViewFilter { ID = tId }, param);
            var tr = (trList != null && trList.Count > 0) ? trList[0] : null;

            string patName = tr != null ? tr.TDL_PATIENT_NAME : ("ID: " + tId);
            string patCode = tr != null ? tr.TDL_PATIENT_CODE : "";
            string treatCode = tr != null ? tr.TREATMENT_CODE : "";

            Console.WriteLine(string.Format("\n🧑 BỆNH NHÂN: {0} | Mã BN: {1} | Mã ĐT: {2} | TrID: {3}", patName, patCode, treatCode, tId));
            Console.WriteLine("-------------------------------------------------------------------------------");

            // 1. Lấy tất cả Service Req
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tId };
            var allReqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

            // 2. Lấy tất cả Exp Mest Medicine
            var emf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tId };
            var allMeds = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);

            // Lọc các y lệnh trong ngày 29/09/2026 (20260929000000 -> 20260929235959) hoặc tạo trong ngày 29/09
            var reqs29 = allReqs != null ? allReqs.Where(r => {
                string it = r.INTRUCTION_TIME.ToString();
                string ct = (r.CREATE_TIME ?? 0).ToString();
                return it.StartsWith("20260929") || ct.StartsWith("20260929");
            }).OrderBy(r => r.INTRUCTION_TIME).ToList() : new List<V_HIS_SERVICE_REQ>();

            Console.WriteLine(string.Format("📋 Tổng số phiếu y lệnh ngày 29/09: {0}", reqs29.Count));

            if (reqs29.Count == 0)
            {
                Console.WriteLine("  ⚠️ Không có phiếu y lệnh nào vào ngày 29/09/2026.");
            }

            foreach (var req in reqs29)
            {
                string itStr = req.INTRUCTION_TIME.ToString();
                string itFmt = itStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", itStr.Substring(6, 2), itStr.Substring(4, 2), itStr.Substring(0, 4), itStr.Substring(8, 2), itStr.Substring(10, 2)) : itStr;
                
                string ctStr = (req.CREATE_TIME ?? 0).ToString();
                string ctFmt = ctStr.Length >= 12 ? string.Format("{0}/{1}/{2} {3}:{4}", ctStr.Substring(6, 2), ctStr.Substring(4, 2), ctStr.Substring(0, 4), ctStr.Substring(8, 2), ctStr.Substring(10, 2)) : ctStr;

                Console.WriteLine(string.Format("\n  👉 Phiếu [{0}] (ID: {1}) | Loại: {2} (TypeID: {3})", req.SERVICE_REQ_CODE, req.ID, req.SERVICE_REQ_TYPE_NAME, req.SERVICE_REQ_TYPE_ID));
                Console.WriteLine(string.Format("     • Thời gian y lệnh: {0} | Thời gian tạo: {1}", itFmt, ctFmt));
                Console.WriteLine(string.Format("     • Bác sĩ chỉ định (Request): {0} (Login: {1})", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME));
                Console.WriteLine(string.Format("     • Người tạo (Creator): {0} | Người sửa: {1}", req.CREATOR, req.MODIFIER));
                Console.WriteLine(string.Format("     • Phòng yêu cầu: {0} | Phòng thực hiện: {1}", req.REQUEST_ROOM_NAME, req.EXECUTE_ROOM_NAME));
                Console.WriteLine(string.Format("     • Trạng thái: {0} (IsDelete: {1})", req.SERVICE_REQ_STT_NAME, req.IS_DELETE));

                // Tìm thuốc thuộc phiếu này
                if (allMeds != null)
                {
                    var reqMeds = allMeds.Where(m => m.TDL_SERVICE_REQ_ID == req.ID || (m.EXP_MEST_ID.HasValue && req.EXP_MEST_TEMPLATE_ID.HasValue && m.EXP_MEST_ID == req.EXP_MEST_TEMPLATE_ID)).ToList();
                    if (reqMeds.Count > 0)
                    {
                        Console.WriteLine("     💊 Chi tiết thuốc trong phiếu:");
                        foreach (var m in reqMeds)
                        {
                            Console.WriteLine(string.Format("        + {0} | SL: {1} {2} | Kho: {3} (StockID: {4})", m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID));
                            Console.WriteLine(string.Format("          HD: \"{0}\" [S:{1}|Tr:{2}|Ch:{3}|T:{4}]", m.TUTORIAL, m.MORNING, m.NOON, m.AFTERNOON, m.EVENING));
                        }
                    }
                }
            }

            // Kiểm tra xem có thuốc nào trong allMeds ngày 29/09 mà chưa gắn với phiếu ở trên không
            var meds29 = allMeds != null ? allMeds.Where(m => {
                string it = (m.TDL_INTRUCTION_TIME ?? 0).ToString();
                string expT = (m.EXP_TIME ?? 0).ToString();
                string ct = (m.CREATE_TIME ?? 0).ToString();
                return it.StartsWith("20260929") || expT.StartsWith("20260929") || ct.StartsWith("20260929");
            }).ToList() : new List<V_HIS_EXP_MEST_MEDICINE>();

            Console.WriteLine(string.Format("\n  💊 Tổng số lượt thuốc xuất/kê ngày 29/09: {0}", meds29.Count));
            foreach (var m in meds29)
            {
                string it = (m.TDL_INTRUCTION_TIME ?? 0).ToString();
                string itFmt = it.Length >= 12 ? string.Format("{0}/{1} {2}:{3}", it.Substring(6, 2), it.Substring(4, 2), it.Substring(8, 2), it.Substring(10, 2)) : it;
                Console.WriteLine(string.Format("    • [{0}] {1} | SL: {2} {3} | Kho: {4} (ID: {5}) | Creator: {6} | ReqID: {7}",
                    itFmt, m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID, m.CREATOR, m.TDL_SERVICE_REQ_ID));
                Console.WriteLine(string.Format("      HD: \"{0}\"", m.TUTORIAL));
            }
        }
    }
}
