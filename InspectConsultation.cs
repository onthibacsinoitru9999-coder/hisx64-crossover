using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace InspectConsultation
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
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
            };
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            string logPath = @"e:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
            string token = null;
            if (File.Exists(logPath))
            {
                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string text = sr.ReadToEnd();
                    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        if (lines[i].Contains("TokenCode|"))
                        {
                            token = lines[i].Substring(lines[i].IndexOf("TokenCode|") + 10, 64);
                            break;
                        }
                    }
                }
            }

            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var adapter = new MyAdapter();

            Console.WriteLine("=== 1. TÌM KIẾM CÁC PHÒNG HỘI CHẨN & KHOA PHÒNG LIÊN QUAN ===");
            HisExecuteRoomViewFilter erf = new HisExecuteRoomViewFilter();
            var execRooms = adapter.FetchList<V_HIS_EXECUTE_ROOM>("api/HisExecuteRoom/GetView", mosConsumer, erf, param);
            List<V_HIS_EXECUTE_ROOM> ctchDebateRooms = new List<V_HIS_EXECUTE_ROOM>();

            if (execRooms != null)
            {
                foreach (var r in execRooms)
                {
                    if (r.EXECUTE_ROOM_NAME != null && (r.EXECUTE_ROOM_NAME.IndexOf("hội chẩn", StringComparison.OrdinalIgnoreCase) >= 0 || r.EXECUTE_ROOM_NAME.IndexOf("hoi chan", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        Console.WriteLine(string.Format("RoomId: {0,5} | ExecRoomId: {1,5} | Branch: {2,2} | DeptId: {3,4} | Dept: {4,-30} | RoomName: {5}",
                            r.ROOM_ID, r.ID, r.BRANCH_ID, r.DEPARTMENT_ID, r.DEPARTMENT_NAME, r.EXECUTE_ROOM_NAME));
                        if (r.EXECUTE_ROOM_NAME.IndexOf("Chấn thương", StringComparison.OrdinalIgnoreCase) >= 0 || r.EXECUTE_ROOM_NAME.IndexOf("CTCH", StringComparison.OrdinalIgnoreCase) >= 0
                            || (r.DEPARTMENT_NAME != null && (r.DEPARTMENT_NAME.IndexOf("Chấn thương", StringComparison.OrdinalIgnoreCase) >= 0 || r.DEPARTMENT_NAME.IndexOf("CTCH", StringComparison.OrdinalIgnoreCase) >= 0)))
                        {
                            ctchDebateRooms.Add(r);
                        }
                    }
                }
            }

            Console.WriteLine("\n=== 2. MATCH CÁC PHÒNG HỘI CHẨN KHOA CTCH VÀ CS ===");
            foreach (var r in ctchDebateRooms)
            {
                Console.WriteLine(string.Format("MATCH ROOM: RoomId: {0} | ExecRoomId: {1} | BranchId: {2} | DeptId: {3} | Code: {4} | Name: {5}",
                    r.ROOM_ID, r.ID, r.BRANCH_ID, r.DEPARTMENT_ID, r.EXECUTE_ROOM_CODE, r.EXECUTE_ROOM_NAME));
            }

            Console.WriteLine("\n=== 3. TÌM YÊU CẦU DỊCH VỤ / BỆNH NHÂN TRONG PHÒNG HỘI CHẨN ===");
            foreach (var dr in ctchDebateRooms)
            {
                Console.WriteLine(string.Format("\n--- Quét RoomId: {0} ({1}) ---", dr.ROOM_ID, dr.EXECUTE_ROOM_NAME));
                HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                srf.EXECUTE_ROOM_ID = dr.ROOM_ID;
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                if (reqs != null && reqs.Count > 0)
                {
                    Console.WriteLine(string.Format("Tìm thấy {0} yêu cầu tại phòng {1}:", reqs.Count, dr.ROOM_ID));
                    foreach (var req in reqs.OrderByDescending(x => x.INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("ReqId: {0} | TreatmentId: {1} | TreatCode: {2} | PatientName: {3} | ReqType: {4} | Status: {5} | Time: {6} | ReqDoctor: {7}",
                            req.ID, req.TREATMENT_ID, req.TREATMENT_CODE, req.TDL_PATIENT_NAME, req.SERVICE_REQ_TYPE_NAME, req.SERVICE_REQ_STT_ID, req.INTRUCTION_TIME, req.REQUEST_LOGINNAME));
                    }
                }
                else
                {
                    Console.WriteLine("Không tìm thấy HisServiceReq theo EXECUTE_ROOM_ID = " + dr.ROOM_ID);
                }
            }

            Console.WriteLine("\n=== 4. TÌM TẤT CẢ KHOA TẠI CƠ SỞ NINH BÌNH (BRANCH_ID = 81) ===");
            HisDepartmentViewFilter dpf = new HisDepartmentViewFilter { BRANCH_ID = 81 };
            var depts = adapter.FetchList<V_HIS_DEPARTMENT>("api/HisDepartment/GetView", mosConsumer, dpf, param);
            if (depts != null)
            {
                foreach (var d in depts)
                {
                    Console.WriteLine(string.Format("DeptId: {0,4} | Code: {1,-10} | Name: {2}", d.ID, d.DEPARTMENT_CODE, d.DEPARTMENT_NAME));
                }
            }
        }
    }
}
