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

namespace InspectTomorrow
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
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
            string[] paths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
                @"Logs\LogSystem.txt",
                @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
            };

            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    try
                    {
                        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string line;
                            string token = "";
                            while ((line = reader.ReadLine()) != null)
                            {
                                int idx = line.IndexOf("TokenCode|");
                                if (idx >= 0 && line.Length >= idx + 10 + 64)
                                {
                                    token = line.Substring(idx + 10, 64);
                                }
                            }
                            if (!string.IsNullOrEmpty(token)) return token;
                        }
                    }
                    catch { }
                }
            }
            return "";
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            StreamWriter log = new StreamWriter("tomorrow_prescriptions_details.txt", false, Encoding.UTF8);

            Action<string> Log = msg =>
            {
                Console.WriteLine(msg);
                log.WriteLine(msg);
            };

            try
            {
                string token = GetLiveToken();
                ApiConsumer mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
                MyAdapter adapter = new MyAdapter();
                CommonParam cp = new CommonParam();

                Log("==================================================================================================");
                Log("CHI TIẾT ĐƠN THUỐC ĐÃ KÊ NGÀY MAI (27/08/2026) CHO CÁC BUỒNG 712, 714, 724, 725");
                Log("==================================================================================================\n");

                // Get Bed Rooms
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, cp);
                string[] roomKws = new string[] { "712", "714", "724", "725" };
                var targetRooms = new List<V_HIS_BED_ROOM>();
                foreach (var kw in roomKws)
                {
                    targetRooms.AddRange(bList.Where(x => x.BED_ROOM_NAME != null && x.BED_ROOM_NAME.Contains(kw)));
                }

                // In-patients
                var patients = new List<V_HIS_TREATMENT_BED_ROOM>();
                foreach (var rm in targetRooms)
                {
                    HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                    tbrf.BED_ROOM_ID = rm.ID;
                    tbrf.IS_IN_ROOM = true;
                    var pts = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, cp);
                    if (pts != null) patients.AddRange(pts);
                }

                int index = 1;
                foreach (var p in patients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    Log("--------------------------------------------------------------------------------------------------");
                    Log(string.Format("{0}. BN: {1} | Buồng: {2} | Giường: {3} | Mã BN: {4} | TreatmentId: {5}",
                        index++, p.TDL_PATIENT_NAME, p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_ID));

                    HisExpMestMedicineViewFilter medFilter = new HisExpMestMedicineViewFilter();
                    medFilter.TDL_TREATMENT_ID = p.TREATMENT_ID;
                    var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, medFilter, cp);

                    if (allMeds != null)
                    {
                        var tomMeds = allMeds.Where(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().StartsWith("20260827")).ToList();
                        if (tomMeds.Count > 0)
                        {
                            Log(string.Format("   Đơn thuốc ngày 27/08/2026 ({0} khoản thuốc):", tomMeds.Count));
                            foreach (var m in tomMeds.OrderBy(x => x.MEDI_STOCK_ID).ThenBy(x => x.MEDICINE_TYPE_NAME))
                            {
                                Log(string.Format("     • [{0}] | SL: {1} {2} | Kho: {3} (ID {4}) | Giờ chỉ định: {5} | Cữ: S:{6} Tr:{7} C:{8} T:{9} | HDSD: {10}",
                                    m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME,
                                    m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID,
                                    m.EXP_TIME ?? m.CREATE_TIME,
                                    m.MORNING, m.NOON, m.AFTERNOON, m.EVENING,
                                    m.TUTORIAL));
                            }
                        }
                        else
                        {
                            Log("   Chưa có thuốc nào trong đơn ngày 27/08/2026.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("EXCEPTION: " + ex.ToString());
            }
            finally
            {
                log.Close();
            }
        }
    }
}
