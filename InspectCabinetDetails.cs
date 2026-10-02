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

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "MOS");
        var myAdapter = new MyAdapter();
        var param = new CommonParam();

        var pats = new[] {
            new { Id = 7279572L, Name = "NGUYỄN THỊ VỰC", Code = "0004062358", Bed = "P711 - G27" },
            new { Id = 7324659L, Name = "NGUYỄN VĂN DŨNG", Code = "0002385100", Bed = "P711 - G28" },
            new { Id = 7321326L, Name = "LÊ MINH THỨC", Code = "0004080629", Bed = "P711 - G29" }
        };

        Console.WriteLine("====================================================================================================");
        Console.WriteLine("🔍 BÁO CÁO CHI TIẾT: AI ĐÃ CHO ĐƠN THUỐC TỦ TRỰC NGÀY HÔM QUA (29/09/2026)");
        Console.WriteLine("====================================================================================================");

        foreach (var p in pats)
        {
            Console.WriteLine(string.Format("\n🏥 [{0}] {1} (Mã BN: {2} | TrID: {3})", p.Bed, p.Name, p.Code, p.Id));
            Console.WriteLine("----------------------------------------------------------------------------------------------------");

            // Lấy tất cả Service Req
            var srf = new HisServiceReqViewFilter { TREATMENT_ID = p.Id };
            var allReqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);

            // Lấy tất cả Exp Mest Medicine
            var emf = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = p.Id };
            var allMeds = myAdapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);

            // 1. Kiểm tra các phiếu kê đơn thuốc được TẠO hoặc có Y LỆNH trong ngày 29/09/2026
            var reqs29 = allReqs != null ? allReqs.Where(r => {
                string it = r.INTRUCTION_TIME.ToString();
                string ct = (r.CREATE_TIME ?? 0).ToString();
                return (it.StartsWith("20260929") || ct.StartsWith("20260929")) && (r.SERVICE_REQ_TYPE_ID == 6 || r.SERVICE_REQ_TYPE_ID == 15 || r.SERVICE_REQ_TYPE_ID == 11 || r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("đơn"));
            }).OrderBy(r => r.CREATE_TIME ?? r.INTRUCTION_TIME).ToList() : new List<V_HIS_SERVICE_REQ>();

            // Lọc các thuốc từ TỦ TRỰC (MediStockId == 810 hoặc có tên Tủ trực) kê ngày 29/09
            var cabinetMeds29 = allMeds != null ? allMeds.Where(m => {
                string it = (m.TDL_INTRUCTION_TIME ?? 0).ToString();
                string ct = (m.CREATE_TIME ?? 0).ToString();
                string expT = (m.EXP_TIME ?? 0).ToString();
                bool isCabinet = (m.MEDI_STOCK_ID == 810 || (m.MEDI_STOCK_NAME != null && m.MEDI_STOCK_NAME.ToLower().Contains("tủ trực")));
                return isCabinet && (it.StartsWith("20260929") || ct.StartsWith("20260929") || expT.StartsWith("20260929"));
            }).OrderBy(m => m.CREATE_TIME ?? m.TDL_INTRUCTION_TIME).ToList() : new List<V_HIS_EXP_MEST_MEDICINE>();

            // Lọc các thuốc từ KHO DƯỢC kê ngày 29/09
            var warehouseMeds29 = allMeds != null ? allMeds.Where(m => {
                string it = (m.TDL_INTRUCTION_TIME ?? 0).ToString();
                string ct = (m.CREATE_TIME ?? 0).ToString();
                string expT = (m.EXP_TIME ?? 0).ToString();
                bool isCabinet = (m.MEDI_STOCK_ID == 810 || (m.MEDI_STOCK_NAME != null && m.MEDI_STOCK_NAME.ToLower().Contains("tủ trực")));
                return !isCabinet && (it.StartsWith("20260929") || ct.StartsWith("20260929") || expT.StartsWith("20260929"));
            }).OrderBy(m => m.CREATE_TIME ?? m.TDL_INTRUCTION_TIME).ToList() : new List<V_HIS_EXP_MEST_MEDICINE>();

            Console.WriteLine(string.Format("💊 1. ĐƠN THUỐC TỦ TRỰC (CABINET) NGÀY 29/09: {0} loại/lượt thuốc", cabinetMeds29.Count));
            if (cabinetMeds29.Count > 0)
            {
                var groupedByDoctor = cabinetMeds29.GroupBy(m => new { m.CREATOR, m.TDL_SERVICE_REQ_ID, m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID });
                foreach (var g in groupedByDoctor)
                {
                    var req = allReqs != null ? allReqs.FirstOrDefault(r => r.ID == g.Key.TDL_SERVICE_REQ_ID) : null;
                    string bsName = req != null ? string.Format("{0} ({1})", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME) : g.Key.CREATOR;
                    string createTime = req != null ? req.CREATE_TIME.ToString() : (g.First().CREATE_TIME.HasValue ? g.First().CREATE_TIME.Value.ToString() : "-");
                    if (createTime.Length >= 14) createTime = string.Format("{0}:{1}:{2} {3}/{4}/{5}", createTime.Substring(8,2), createTime.Substring(10,2), createTime.Substring(12,2), createTime.Substring(6,2), createTime.Substring(4,2), createTime.Substring(0,4));

                    string reqCode = req != null ? req.SERVICE_REQ_CODE : ("ReqID: " + g.Key.TDL_SERVICE_REQ_ID);
                    Console.WriteLine(string.Format("  👉 Phiếu: [{0}] | Bác sĩ kê/Người tạo: {1} | Lúc: {2} | Kho: {3}", reqCode, bsName, createTime, g.Key.MEDI_STOCK_NAME));
                    foreach (var m in g)
                    {
                        Console.WriteLine(string.Format("     • {0} | SL: {1} {2} | HD: \"{3}\"", m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.TUTORIAL));
                    }
                }
            }
            else
            {
                Console.WriteLine("  ⚠️ KHÔNG CÓ đơn thuốc tủ trực nào trong ngày 29/09.");
            }

            Console.WriteLine(string.Format("\n📦 2. ĐƠN THUỐC KHO DƯỢC LĨNH NGÀY 29/09: {0} lượt thuốc", warehouseMeds29.Count));
            if (warehouseMeds29.Count > 0)
            {
                var groupedWh = warehouseMeds29.GroupBy(m => new { m.CREATOR, m.TDL_SERVICE_REQ_ID, m.MEDI_STOCK_NAME });
                foreach (var g in groupedWh)
                {
                    var req = allReqs != null ? allReqs.FirstOrDefault(r => r.ID == g.Key.TDL_SERVICE_REQ_ID) : null;
                    string bsName = req != null ? string.Format("{0} ({1})", req.REQUEST_USERNAME, req.REQUEST_LOGINNAME) : g.Key.CREATOR;
                    string createTime = req != null ? req.CREATE_TIME.ToString() : (g.First().CREATE_TIME.HasValue ? g.First().CREATE_TIME.Value.ToString() : "-");
                    if (createTime.Length >= 14) createTime = string.Format("{0}:{1}:{2} {3}/{4}/{5}", createTime.Substring(8,2), createTime.Substring(10,2), createTime.Substring(12,2), createTime.Substring(6,2), createTime.Substring(4,2), createTime.Substring(0,4));
                    string reqCode = req != null ? req.SERVICE_REQ_CODE : ("ReqID: " + g.Key.TDL_SERVICE_REQ_ID);
                    Console.WriteLine(string.Format("  👉 Phiếu: [{0}] | Bác sĩ kê/Người tạo: {1} | Lúc: {2} | Kho: {3}", reqCode, bsName, createTime, g.Key.MEDI_STOCK_NAME));
                    foreach (var m in g)
                    {
                        Console.WriteLine(string.Format("     • {0} | SL: {1} {2} | HD: \"{3}\"", m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.TUTORIAL));
                    }
                }
            }
            else
            {
                Console.WriteLine("  ⚠️ KHÔNG CÓ đơn thuốc kho dược trong ngày 29/09.");
            }

            // 3. Toàn bộ các phiếu y lệnh khác trong ngày 29/09
            var otherReqs = allReqs != null ? allReqs.Where(r => {
                string it = r.INTRUCTION_TIME.ToString();
                string ct = (r.CREATE_TIME ?? 0).ToString();
                return (it.StartsWith("20260929") || ct.StartsWith("20260929")) && r.SERVICE_REQ_TYPE_ID != 6 && r.SERVICE_REQ_TYPE_ID != 15 && r.SERVICE_REQ_TYPE_ID != 11 && !r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("đơn");
            }).ToList() : new List<V_HIS_SERVICE_REQ>();

            if (otherReqs.Count > 0)
            {
                Console.WriteLine(string.Format("\n📋 3. CÁC Y LỆNH KHÁC TRONG NGÀY 29/09 ({0} phiếu):", otherReqs.Count));
                foreach (var r in otherReqs)
                {
                    string it = r.INTRUCTION_TIME.ToString();
                    if (it.Length >= 12) it = string.Format("{0}/{1} {2}:{3}", it.Substring(6,2), it.Substring(4,2), it.Substring(8,2), it.Substring(10,2));
                    Console.WriteLine(string.Format("  • [{0}] Lúc {1} | Loại: {2} | BS: {3} ({4})", r.SERVICE_REQ_CODE, it, r.SERVICE_REQ_TYPE_NAME, r.REQUEST_USERNAME, r.REQUEST_LOGINNAME));
                }
            }
        }
        Console.WriteLine("\n====================================================================================================");
    }
}
