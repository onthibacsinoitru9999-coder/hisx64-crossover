using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

public class Program
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T FetchSingle<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<T>(uri, consumer, filter, param);
        }

        public bool PostData<T>(string uri, ApiConsumer consumer, T data, CommonParam param)
        {
            return Post<bool>(uri, consumer, data, param);
        }

        public TResult PostDataWithResult<TResult, TData>(string uri, ApiConsumer consumer, TData data, CommonParam param)
        {
            return Post<TResult>(uri, consumer, data, param);
        }
    }

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

        Run(args);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
        string token = "";
        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("ERROR: Cannot find TokenCode in LogSystem.txt");
            return;
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // Target codes from user screenshot
        List<string> targetCodes = new List<string> { "000092317137", "000092317138", "000092317139" };
        
        // List of 12 patients treatment codes / patient codes
        var patientCodes = new string[] {
            "0004062838", // Nguyễn Thị Thìn
            "0001530113", // Lê Văn Hòa
            "0004079114", // Phan Thị Tư
            "0004050211", // Bùi Thị Thiện
            "0004089090", // Vũ Thành Trung
            "0002840646", // Phạm Văn Bỏng
            "0004081634", // Nguyễn Thị Thu
            "0003837595", // Nguyễn Thị Ngọc
            "0004079203", // Nguyễn Khắc Chủ
            "0004067971", // Nguyễn Thị Thảo
            "0003863470", // Trần Thị Lượng
            "0002740977"  // Nguyễn Văn Phương
        };

        Console.WriteLine("================================================================================");
        Console.WriteLine(">>> 1. KIỂM TRA VÀ XỬ LÝ 3 MÃ PHIẾU CỤ THỂ TRONG ẢNH CỦA BÁC SĨ:");
        Console.WriteLine("================================================================================");

        foreach (var code in targetCodes)
        {
            HisServiceReqFilter filter = new HisServiceReqFilter { SERVICE_REQ_CODE__EXACT = code };
            var reqs = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mos, filter, cp);
            if (reqs != null && reqs.Count > 0)
            {
                var req = reqs[0];
                Console.WriteLine(string.Format("Tìm thấy phiếu: {0} (ID: {1}) | Ngày: {2} | BS hiện tại: {3} ({4}) | Loại: {5}",
                    req.SERVICE_REQ_CODE, req.ID, req.INTRUCTION_TIME, req.REQUEST_LOGINNAME, req.REQUEST_USERNAME, req.SERVICE_REQ_TYPE_ID));

                ProcessServiceReq(adapter, mos, req);
            }
            else
            {
                Console.WriteLine("Không tìm thấy phiếu mã: " + code);
            }
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine(">>> 2. QUÉT VÀ XỬ LÝ TẤT CẢ PHIẾU SUẤT ĂN NGÀY 30/09 CHO 12 BỆNH NHÂN:");
        Console.WriteLine("================================================================================");

        foreach (var pCode in patientCodes)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            if (treatments == null || treatments.Count == 0) continue;

            var tr = treatments.OrderByDescending(x => x.IN_TIME).First();
            Console.WriteLine(string.Format("\n--- BN: {0} ({1}) | Mã ĐT: {2} | Địa chỉ: {3} ---",
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.TDL_PATIENT_ADDRESS));

            HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);
            if (allReqs != null)
            {
                // Find all ration/suất ăn or orders on 30/09
                var mealReqs = allReqs.Where(r => 
                    (r.INTRUCTION_TIME >= 20260930000000L && r.INTRUCTION_TIME <= 20260930235959L) &&
                    (r.SERVICE_REQ_TYPE_ID == 8 || r.SERVICE_REQ_TYPE_ID == 12 || r.SERVICE_REQ_TYPE_ID == 17 ||
                     (r.SERVICE_REQ_TYPE_NAME != null && r.SERVICE_REQ_TYPE_NAME.ToLower().Contains("ăn")) ||
                     (r.EXECUTE_ROOM_NAME != null && r.EXECUTE_ROOM_NAME.ToLower().Contains("dinh dưỡng")))
                ).ToList();

                if (mealReqs.Count == 0)
                {
                    Console.WriteLine("  -> Không có phiếu suất ăn ngày 30/09.");
                }
                else
                {
                    foreach (var mr in mealReqs)
                    {
                        Console.WriteLine(string.Format("  [PHÁT HIỆN] Phiếu: {0} | Loại: {1} | BS: {2} ({3}) | Giờ: {4} | Trạng thái: {5}",
                            mr.SERVICE_REQ_CODE, mr.SERVICE_REQ_TYPE_NAME, mr.REQUEST_LOGINNAME, mr.REQUEST_USERNAME, mr.INTRUCTION_TIME, mr.SERVICE_REQ_STT_ID));

                        HisServiceReqFilter sreqFilter = new HisServiceReqFilter { ID = mr.ID };
                        var rawReqs = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mos, sreqFilter, cp);
                        if (rawReqs != null && rawReqs.Count > 0)
                        {
                            ProcessServiceReq(adapter, mos, rawReqs[0]);
                        }
                    }
                }
            }
        }
    }

    static void ProcessServiceReq(MyAdapter adapter, ApiConsumer mos, HIS_SERVICE_REQ req)
    {
        CommonParam cp = new CommonParam();

        Console.WriteLine(string.Format("  >> [BƯỚC 1] Đổi REQUEST_LOGINNAME sang '034727' cho phiếu {0} (ID: {1})...", req.SERVICE_REQ_CODE, req.ID));
        req.REQUEST_LOGINNAME = "034727";
        req.REQUEST_USERNAME = "Ths.BS NGUYỄN HỮU SÂM";
        req.REQUEST_USER_TITLE = "Thạc sỹ y học";

        // Try Update or UpdateCommonInfo
        try
        {
            var updateResult = adapter.PostDataWithResult<HIS_SERVICE_REQ, HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mos, req, cp);
            if (updateResult != null)
            {
                Console.WriteLine("     => UpdateCommonInfo THÀNH CÔNG! Đã đổi người chỉ định sang 034727.");
            }
            else
            {
                Console.WriteLine("     => UpdateCommonInfo trả về null, thử Update...");
                var up2 = adapter.PostDataWithResult<HIS_SERVICE_REQ, HIS_SERVICE_REQ>("api/HisServiceReq/Update", mos, req, cp);
                if (up2 != null) Console.WriteLine("     => Update THÀNH CÔNG!");
                else Console.WriteLine("     => Update thất bại.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("     => Lỗi cập nhật: " + ex.Message);
        }

        Console.WriteLine(string.Format("  >> [BƯỚC 2] Thử XÓA TRỰC TIẾP phiếu {0} (ID: {1}) bằng API Delete...", req.SERVICE_REQ_CODE, req.ID));
        try
        {
            // First check SereServ
            HisSereServFilter ssFilter = new HisSereServFilter { SERVICE_REQ_ID = req.ID };
            var ssList = adapter.FetchList<HIS_SERE_SERV>("api/HisSereServ/Get", mos, ssFilter, cp);
            if (ssList != null && ssList.Count > 0)
            {
                Console.WriteLine("     -> Phiếu có " + ssList.Count + " dịch vụ con (SereServ).");
                foreach (var ss in ssList)
                {
                    Console.WriteLine("        - SereServ ID: " + ss.ID + " | ServiceId: " + ss.SERVICE_ID + " | Amount: " + ss.AMOUNT);
                }
            }

            // Call Delete on HisServiceReq
            var delResult = adapter.PostDataWithResult<bool, HIS_SERVICE_REQ>("api/HisServiceReq/Delete", mos, req, cp);
            Console.WriteLine("     => Kết quả Delete HisServiceReq: " + delResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine("     => Lỗi khi gọi Delete: " + ex.Message);
        }
    }
}
