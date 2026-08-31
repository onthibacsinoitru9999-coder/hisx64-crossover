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

namespace NinhBinhLearner
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
                string path3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);
                return null;
            };

            Run();
        }

        static string ReadLiveToken()
        {
            string[] candidates = new string[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
                @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
            };

            foreach (var logPath in candidates)
            {
                if (File.Exists(logPath))
                {
                    try
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
                                    int idx = lines[i].IndexOf("TokenCode|") + 10;
                                    if (lines[i].Length >= idx + 64)
                                    {
                                        return lines[i].Substring(idx, 64);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();

            string tokenCode = ReadLiveToken();
            if (string.IsNullOrEmpty(tokenCode))
            {
                Console.WriteLine("❌ Không tìm thấy TokenCode!");
                return;
            }

            Console.WriteLine("🔑 Live TokenCode: " + tokenCode.Substring(0, 16) + "...");
            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
            MyAdapter adapter = new MyAdapter();

            // 1. Tra cứu chi tiết Bệnh nhân Mai Văn Kinh (Treatment ID 7133268)
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("1. CHI TIẾT HỒ SƠ BỆNH NHÂN MAI VĂN KINH (TREATMENT_ID = 7133268)");
            Console.WriteLine("===============================================================================");
            try
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = 7133268 };
                var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                if (treatments != null && treatments.Count > 0)
                {
                    var t = treatments[0];
                    Console.WriteLine(string.Format("Mã Bệnh Án/Điều Trị : {0}", t.TREATMENT_CODE));
                    Console.WriteLine(string.Format("Mã Bệnh Nhân        : {0}", t.TDL_PATIENT_CODE));
                    Console.WriteLine(string.Format("Họ và Tên           : {0}", t.TDL_PATIENT_NAME));
                    Console.WriteLine(string.Format("Ngày sinh / Giới    : {0} ({1})", t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME));
                    Console.WriteLine(string.Format("Địa chỉ             : {0}", t.TDL_PATIENT_ADDRESS));
                    Console.WriteLine(string.Format("Thời gian vào viện  : {0}", t.IN_TIME));
                    Console.WriteLine(string.Format("Khoa điều trị       : {0} (ID: {1})", t.END_DEPARTMENT_NAME, t.END_DEPARTMENT_ID ?? t.LAST_DEPARTMENT_ID));
                    Console.WriteLine(string.Format("Chẩn đoán chính     : [{0}] {1}", t.ICD_CODE, t.ICD_NAME));
                    Console.WriteLine(string.Format("Chẩn đoán kèm theo  : [{0}] {1}", t.ICD_SUB_CODE, t.ICD_TEXT));
                    Console.WriteLine(string.Format("Bác sĩ điều trị     : {0} ({1})", t.DOCTOR_USERNAME, t.DOCTOR_LOGINNAME));
                    Console.WriteLine(string.Format("Trạng thái tạm khóa : {0}", t.IS_PAUSE));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy Treatment: " + ex.Message);
            }

            // 2. Tra cứu Buồng Giường của BN
            try
            {
                HisTreatmentBedRoomViewFilter tbf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = 7133268, IS_IN_ROOM = true };
                var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbf, param);
                if (beds != null && beds.Count > 0)
                {
                    foreach (var b in beds)
                    {
                        Console.WriteLine(string.Format("Buồng / Giường      : Buồng {0} ({1}) - Giường {2}", b.BED_ROOM_NAME, b.BED_ROOM_CODE, b.BED_NAME));
                    }
                }
            }
            catch { }

            // 3. Tra cứu Kho Dược & Tủ Trực tại Cơ sở Ninh Bình (Branch 81 / Department 915)
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("2. DANH MỤC KHO DƯỢC & TỦ TRỰC TẠI CƠ SỞ NINH BÌNH (BRANCH_ID = 81)");
            Console.WriteLine("===============================================================================");
            try
            {
                HisMediStockViewFilter msf = new HisMediStockViewFilter();
                var stocks = adapter.FetchList<V_HIS_MEDI_STOCK>("api/HisMediStock/GetView", mosConsumer, msf, param);
                if (stocks != null)
                {
                    foreach (var s in stocks)
                    {
                        if (s.DEPARTMENT_ID == 915 || (s.MEDI_STOCK_NAME ?? "").Contains("NB") || (s.MEDI_STOCK_NAME ?? "").Contains("Ninh Bình") || (s.MEDI_STOCK_CODE ?? "").Contains("NB"))
                        {
                            Console.WriteLine(string.Format("Kho ID: {0,4} | Code: {1,-10} | Khoa: {2,4} | Tủ trực: {3,-5} | Tên Kho: {4}", 
                                s.ID, s.MEDI_STOCK_CODE, s.DEPARTMENT_ID, s.IS_CABINET == 1 ? "CÓ" : "KHÔNG", s.MEDI_STOCK_NAME));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy MediStock: " + ex.Message);
            }

            // 4. Tra cứu tất cả Khoa phòng tại Cơ sở Ninh Bình
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("3. DANH SÁCH KHOA PHÒNG CƠ SỞ NINH BÌNH (BRANCH_ID = 81)");
            Console.WriteLine("===============================================================================");
            try
            {
                HisDepartmentFilter df = new HisDepartmentFilter { BRANCH_ID = 81 };
                var depts = adapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, df, param);
                if (depts != null)
                {
                    foreach (var d in depts)
                    {
                        Console.WriteLine(string.Format("Dept ID: {0,4} | Code: {1,-12} | Active: {2} | Tên Khoa: {3}",
                            d.ID, d.DEPARTMENT_CODE, d.IS_ACTIVE, d.DEPARTMENT_NAME));
                    }
                }
            }
            catch { }

            // 5. Quét danh sách tất cả BN đang nằm tại Khoa Ngoại tổng hợp CSNB (Dept 915)
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("4. DANH SÁCH BỆNH NHÂN ĐANG NẰM NỘI TRÚ TẠI KHOA NGOẠI TỔNG HỢP CSNB (915)");
            Console.WriteLine("===============================================================================");
            try
            {
                HisTreatmentBedRoomViewFilter allBedsFilter = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true };
                var allBeds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, allBedsFilter, param);
                if (allBeds != null)
                {
                    var nbPatients = allBeds.Where(b => b.DEPARTMENT_ID == 915 || (b.BED_ROOM_NAME ?? "").Contains("3E") || (b.BED_ROOM_NAME ?? "").Contains("3D")).ToList();
                    Console.WriteLine(string.Format("Tổng số BN đang nằm: {0} bệnh nhân", nbPatients.Count));
                    int idx = 1;
                    foreach (var p in nbPatients)
                    {
                        Console.WriteLine(string.Format("{0,2}. [{1}] Mã BN: {2} - {3,-22} | Buồng: {4,-12} | Giường: {5,-8} | Vào viện: {6}",
                            idx++, p.TREATMENT_CODE, p.TDL_PATIENT_CODE, p.TDL_PATIENT_NAME, p.BED_ROOM_NAME, p.BED_NAME, p.IN_TIME));
                    }
                }
            }
            catch { }
        }
    }
}
