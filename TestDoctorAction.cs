using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

namespace DoctorAction
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
        }
    }

    class Program
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
            CommonParam param = new CommonParam();
            Load.Init();

            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (token == null)
            {
                Console.WriteLine("❌ Đăng nhập không thành công!");
                return;
            }

            string tokenCode = token.TokenCode;
            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
            MyAdapter adapter = new MyAdapter();
            long treatmentId = 6979004;

            // 1. Get treatment
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = treatmentId };
            var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
            var tr = trs[0];
            Console.WriteLine(string.Format("✅ Bệnh nhân: {0} | Mã BN: {1} | HSBA: {2}", 
                tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));

            // 2. Kích hoạt WorkInfo phòng hợp lệ của Khoa Ngoại tổng hợp CSNB
            long workingRoomId = 15272; // Buồng BB 3E - 16
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 15272 }, // BB 3E - 16
                    new RoomSDO { RoomId = 18679 }, // Phòng TT CTCH & CS (3E-05)
                    new RoomSDO { RoomId = 18686 }  // Phòng HC CTCH & CS (CSNB)
                }
            };
            param = new CommonParam();
            var workPlaces = adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            if (workPlaces != null && workPlaces.Count > 0)
            {
                Console.WriteLine("✅ UpdateWorkInfo THÀNH CÔNG! Đã kích hoạt " + workPlaces.Count + " phòng làm việc.");
            }
            else
            {
                Console.WriteLine("❌ UpdateWorkInfo thất bại: " + (param.Messages != null ? string.Join("; ", param.Messages) : ""));
                return;
            }

            // 3. Tạo Tờ điều trị mới
            long trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            string content = "Bệnh nhân tỉnh, tiếp xúc tốt. Đau vết mổ giảm nhiều, vết mổ khô, không sưng đỏ, dịch thấm băng ít.\nBệnh nhân còn cảm giác tê bì hai chân, cơ lực 2 chi dưới 4/5. Tim đều, phổi thông khí rõ, bụng mềm, sonde tiểu vàng trong.\nY lệnh: Bổ sung Methylprednisolone (Solu-Medrol 40mg) tủ trực khu 3E chống viêm giảm phù nề chèn ép thần kinh. Hướng dẫn bệnh nhân chế độ tập phục hồi chức năng và theo dõi sát vận động, cảm giác chi dưới.";
            string instruction = "Chăm sóc cấp II. Chế độ ăn BT01. Bổ sung Solu-Medrol 40mg (2 lọ) tủ trực khu 3E. Theo dõi DHST và vận động hai chân.";

            HIS_TRACKING trk = new HIS_TRACKING
            {
                TREATMENT_ID = treatmentId,
                TRACKING_TIME = trackingTime,
                CONTENT = content,
                MEDICAL_INSTRUCTION = instruction,
                ICD_CODE = tr.ICD_CODE,
                ICD_NAME = tr.ICD_NAME,
                ICD_SUB_CODE = tr.ICD_SUB_CODE,
                ICD_TEXT = tr.ICD_TEXT,
                DEPARTMENT_ID = 915,
                ROOM_ID = workingRoomId
            };

            HisTrackingSDO sdo = new HisTrackingSDO
            {
                Tracking = trk,
                WorkingRoomId = workingRoomId
            };

            param = new CommonParam();
            var resTrk = adapter.PostData<HisTrackingSDO>("api/HisTracking/Create", mosConsumer, sdo, param);
            long trackingId = 0;
            if (resTrk != null && resTrk.Tracking != null && resTrk.Tracking.ID > 0)
            {
                trackingId = resTrk.Tracking.ID;
                Console.WriteLine(string.Format("🎉 TẠO TỜ ĐIỀU TRỊ THÀNH CÔNG! ID: {0} lúc {1}", trackingId, trackingTime));
            }
            else
            {
                Console.WriteLine("Cảnh báo tạo tờ điều trị: " + (param.Messages != null ? string.Join("; ", param.Messages) : ""));
                // Fallback to existing tracking if any
                HisTrackingViewFilter tkf = new HisTrackingViewFilter { TREATMENT_ID = treatmentId };
                var tks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, tkf, param);
                var lastTk = tks.OrderByDescending(x => x.TRACKING_TIME).First();
                trackingId = lastTk.ID;
                trackingTime = lastTk.TRACKING_TIME;
                Console.WriteLine("Dùng Tracking ID: " + trackingId);
            }

            // 4. Kê đơn 2 lọ Solumedrol 40mg từ Tủ trực khu 3E (Stock 5142)
            long medTypeId = 16843; // Solu-Medrol 40mg
            long stockId = 5142;    // Tủ trực thuốc khu 3E - khoa Ngoại tổng hợp (Cơ sở Ninh Bình)
            decimal amount = 2.0m;  // 2 lọ
            string tutorial = "Ngày tiêm truyền 02 lọ chia 2 lần (sáng: 01 lọ, chiều: 01 lọ)";

            InPatientPresSDO presSDO = new InPatientPresSDO
            {
                TreatmentId = tr.ID,
                InstructionTimes = new List<long> { trackingTime },
                UseTimes = new List<long> { trackingTime },
                TrackingId = trackingId,
                TrackingInfos = new List<TrackingInfoSDO>
                {
                    new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
                },
                RequestRoomId = workingRoomId,
                RequestLoginName = "vmc",
                RequestUserName = "BS VŨ MINH CƯỜNG",
                IcdCode = tr.ICD_CODE,
                IcdName = tr.ICD_NAME,
                IcdSubCode = tr.ICD_SUB_CODE,
                IcdText = tr.ICD_TEXT,
                Medicines = new List<PresMedicineSDO>
                {
                    new PresMedicineSDO
                    {
                        MedicineTypeId = medTypeId,
                        MediStockId = stockId,
                        Amount = amount,
                        PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                        Tutorial = tutorial
                    }
                }
            };

            param = new CommonParam();
            var presResult = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, presSDO, param);
            if (presResult != null && ((presResult.ExpMests != null && presResult.ExpMests.Count > 0) || (presResult.ServiceReqs != null && presResult.ServiceReqs.Count > 0)))
            {
                string expCode = presResult.ExpMests != null && presResult.ExpMests.Count > 0 ? presResult.ExpMests[0].EXP_MEST_CODE : "N/A";
                string reqCode = presResult.ServiceReqs != null && presResult.ServiceReqs.Count > 0 ? presResult.ServiceReqs[0].SERVICE_REQ_CODE : "N/A";
                Console.WriteLine("\n🎉🎉🎉 KÊ ĐƠN THUỐC SOLUMEDROL THÀNH CÔNG!");
                Console.WriteLine(string.Format("   • Mã y lệnh (ServiceReq): {0}", reqCode));
                Console.WriteLine(string.Format("   • Mã xuất kho (ExpMest): {0}", expCode));
                Console.WriteLine(string.Format("   • Thuốc: Solu-Medrol 40mg (Số lượng: {0} lọ)", amount));
                Console.WriteLine(string.Format("   • Kho xuất: Tủ trực thuốc khu 3E - Khoa Ngoại tổng hợp CSNB (Stock ID: {0})", stockId));
                Console.WriteLine(string.Format("   • Hướng dẫn dùng: {0}", tutorial));
            }
            else
            {
                Console.WriteLine("\n❌ Kê đơn thất bại!");
                Console.WriteLine("HasException: " + param.HasException);
                if (param.Messages != null) foreach (var m in param.Messages) Console.WriteLine("Msg: " + m);
                if (param.BugCodes != null) foreach (var b in param.BugCodes) Console.WriteLine("Bug: " + b);
            }
        }
    }
}
