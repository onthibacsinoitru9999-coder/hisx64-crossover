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
using MOS.SDO;

namespace WardRations
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
                string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);
                return null;
            };

            Run();
        }

        static string GetLiveToken()
        {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
            using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                string line, token = "";
                while ((line = reader.ReadLine()) != null)
                {
                    int idx = line.IndexOf("TokenCode|");
                    if (idx >= 0 && line.Length >= idx + 10 + 64) token = line.Substring(idx + 10, 64);
                }
                return token;
            }
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string token = GetLiveToken();
            if (string.IsNullOrEmpty(token)) { Console.WriteLine("LỖI: Không có token!"); return; }

            ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            MyAdapter adapter = new MyAdapter();
            CommonParam cp = new CommonParam();

            long treatmentId = 7163700; // NGUYỄN VĂN THƠ
            long ptId = 42;

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = treatmentId };
            var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, cp);
            var tr = trList != null && trList.Count > 0 ? trList[0] : null;

            // Add Trua (30153) and Chieu (30154) for 07/09/2026
            long instructionTime = 20260907050000;

            var services = new List<RationServiceSDO>
            {
                new RationServiceSDO
                {
                    ServiceId = 30153, // Trưa
                    PatientTypeId = ptId,
                    RoomId = 5809,
                    Amount = 1.0m,
                    RationTimeIds = new List<long> { 3 }
                },
                new RationServiceSDO
                {
                    ServiceId = 30154, // Chiều
                    PatientTypeId = ptId,
                    RoomId = 5809,
                    Amount = 1.0m,
                    RationTimeIds = new List<long> { 5 }
                }
            };

            var sdo = new HisRationServiceReqSDO
            {
                TreatmentIds = new List<long> { treatmentId },
                InstructionTimes = new List<long> { instructionTime },
                RequestRoomId = 5248,
                RequestLoginName = "034727",
                RequestUserName = "Ths.BS NGUYỄN HỮU SÂM",
                IcdCode = tr != null ? tr.ICD_CODE : "M51.1",
                IcdName = tr != null ? tr.ICD_NAME : "Thoát vị đĩa đệm",
                IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                IcdText = tr != null ? tr.ICD_TEXT : "",
                HalfInFirstDay = false,
                IsForAutoCreateRation = false,
                IsForHomie = false,
                RationServices = services
            };

            CommonParam reqParam = new CommonParam();
            var res = adapter.PostData<object>("api/HisServiceReq/RationCreate", mos, sdo, reqParam);

            if (!reqParam.HasException)
            {
                Console.WriteLine("✅ Bổ sung THÀNH CÔNG bữa Trưa & Chiều 07/09/2026 cho BN Nguyễn Văn Thơ!");
            }
            else
            {
                Console.WriteLine("❌ Thất bại: " + reqParam.GetMessage());
            }
        }
    }
}
