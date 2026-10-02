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
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode hợp lệ!");
            return 1;
        }

        var mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        var adapter = new MyAdapter();
        var cp = new CommonParam();

        // Ensure WorkInfo for Room 5248 (Khoa 57)
        try
        {
            var workInfo = new WorkInfoSDO { Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5248 } } };
            adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mos, workInfo, cp);
        }
        catch { }

        string[] patientCodes = new string[] {
            "0002417556", "0004039468", "0004080629", "0004061261",
            "0004062280", "0004093319", "0002038416", "0004076093"
        };

        string newContent = 
@"TỔNG KẾT RA KHOA
=======================
Bệnh nhân tỉnh
Huyết động ổn
Da niêm mạc hồng
Đau vết mổ VAS3đ
Vết mổ băng khô
Đầu chi ấm
Vận động cảm giác đầu chi bình thường
=> Xin ý kiến ban lãnh đạo khoa
=> Thay băng chăm sóc vết thương
=> Tập phục hồi chức năng theo hướng dẫn
=> BN ỔN ĐỊNH
=> RA VIỆN";

        string newCare = "Chăm sóc cấp II. Ăn theo chế độ bệnh lý.";
        string newMed = "Xin ý kiến ban lãnh đạo khoa. BN ổn định -> Cho ra viện. Kê đơn ngoại trú.";

        Console.WriteLine("===============================================================================");
        Console.WriteLine("⚡ CẬP NHẬT GIỜ TỜ ĐIỀU TRỊ CUỐI CÙNG SANG 12H & ÁP DỤNG NỘI DUNG MỚI (8 BN)");
        Console.WriteLine("===============================================================================\n");

        foreach (var pCode in patientCodes)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pCode };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            if (trs == null || trs.Count == 0)
            {
                tf = new HisTreatmentViewFilter { TREATMENT_CODE__EXACT = pCode };
                trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            }
            if (trs == null || trs.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy hồ sơ cho BN: " + pCode);
                continue;
            }

            var tr = trs.OrderByDescending(x => x.IS_ACTIVE == 1).ThenByDescending(x => x.ID).First();

            HisTrackingViewFilter trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp);
            if (trks == null || trks.Count == 0)
            {
                Console.WriteLine(string.Format("⚪ BN {0} ({1}) chưa có tờ điều trị nào.", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE));
                continue;
            }

            // Tìm tờ điều trị cuối cùng (hoặc tờ tổng kết ra viện)
            var lastTrk = trks.OrderByDescending(x => x.TRACKING_TIME).First();

            Console.WriteLine(string.Format("🏥 BN: {0} ({1}) | Mã ĐT: {2}", tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            Console.WriteLine(string.Format("   - Tờ điều trị cuối hiện tại: ID {0} lúc {1}", lastTrk.ID, lastTrk.TRACKING_TIME));

            // Lấy chi tiết HIS_TRACKING đầy đủ
            var getFilter = new HisTrackingFilter { ID = lastTrk.ID };
            var trkList = adapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", mos, getFilter, cp);
            if (trkList == null || trkList.Count == 0)
            {
                Console.WriteLine("   ❌ Không lấy được chi tiết HIS_TRACKING ID: " + lastTrk.ID);
                continue;
            }

            var fullTrk = trkList[0];

            // Chuyển giờ sang 12h:00:00 ngày hôm đó
            string sTime = fullTrk.TRACKING_TIME.ToString();
            string datePart = sTime.Substring(0, 8); // yyyyMMdd
            long newTrackingTime = long.Parse(datePart + "120000");

            // Kiểm tra trùng lặp thời gian với các tracking khác
            var otherTimes = new HashSet<long>(trks.Where(x => x.ID != fullTrk.ID).Select(x => x.TRACKING_TIME));
            while (otherTimes.Contains(newTrackingTime))
            {
                DateTime dt = DateTime.ParseExact(newTrackingTime.ToString(), "yyyyMMddHHmmss", null);
                dt = dt.AddMinutes(1);
                newTrackingTime = long.Parse(dt.ToString("yyyyMMddHHmmss"));
            }

            fullTrk.TRACKING_TIME = newTrackingTime;
            fullTrk.CONTENT = newContent;
            fullTrk.CARE_INSTRUCTION = newCare;
            fullTrk.MEDICAL_INSTRUCTION = newMed;

            var dhstFilter = new HisDhstFilter { TRACKING_ID = fullTrk.ID };
            var dhsts = adapter.FetchList<HIS_DHST>("api/HisDhst/Get", mos, dhstFilter, cp);

            long workRoomId = (fullTrk.DEPARTMENT_ID == 915) ? 18679 : 5248;
            var updSdo = new HisTrackingSDO
            {
                Tracking = fullTrk,
                WorkingRoomId = workRoomId,
                Dhst = (dhsts != null && dhsts.Count > 0) ? dhsts[0] : null
            };

            var cpUpd = new CommonParam();
            var res = adapter.PostData<HIS_TRACKING>("api/HisTracking/Update", mos, updSdo, cpUpd);
            if (res != null)
            {
                DateTime dtNew = DateTime.ParseExact(newTrackingTime.ToString(), "yyyyMMddHHmmss", null);
                Console.WriteLine(string.Format("   ✔ CẬP NHẬT THÀNH CÔNG: ID {0} -> Giờ mới: {1} ({2})", fullTrk.ID, newTrackingTime, dtNew.ToString("dd/MM/yyyy HH:mm")));
            }
            else
            {
                string msg = (cpUpd.Messages != null && cpUpd.Messages.Count > 0) ? string.Join("; ", cpUpd.Messages) : "Lỗi không xác định";
                Console.WriteLine(string.Format("   ❌ THẤT BẠI khi cập nhật ID {0}: {1}", fullTrk.ID, msg));
            }
            Console.WriteLine();
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🎉 HOÀN TẤT CẬP NHẬT TỜ ĐIỀU TRỊ CHO CẢ 8 BỆNH NHÂN!");
        Console.WriteLine("===============================================================================");
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
    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return base.Post<T>(uri, consumer, data, param);
    }
}
