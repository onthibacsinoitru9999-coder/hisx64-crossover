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

namespace AdmissionStudy
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
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(p) ? Assembly.LoadFrom(p) : null;
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
            string token = ReadLiveToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Không tìm thấy TokenCode!");
                return;
            }

            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
            var adapter = new MyAdapter();

            Console.WriteLine("===============================================================================");
            Console.WriteLine("🏥 NGHIÊN CỨU & KHẢO SÁT 10 BỆNH ÁN NGOẠI KHOA THỰC TẾ TRONG KHOA");
            Console.WriteLine("===============================================================================");

            // 1. Quét danh sách bệnh nhân đang nằm buồng
            HisTreatmentBedRoomViewFilter tbf = new HisTreatmentBedRoomViewFilter { IS_IN_ROOM = true };
            var allInBeds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbf, param);
            if (allInBeds == null || allInBeds.Count == 0)
            {
                Console.WriteLine("Không lấy được danh sách bệnh nhân đang nằm buồng.");
                return;
            }

            // Lấy 10 bệnh nhân từ Khoa 915 và Khoa 57
            var samplePatients = allInBeds
                .Where(b => b.DEPARTMENT_ID == 915 || b.DEPARTMENT_ID == 57)
                .GroupBy(b => b.TREATMENT_ID)
                .Select(g => g.First())
                .Take(10)
                .ToList();

            if (samplePatients.Count < 5)
            {
                samplePatients = allInBeds.GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).Take(10).ToList();
            }

            Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân thực tế để phân tích chi tiết.\n", samplePatients.Count));

            int count = 0;
            foreach (var p in samplePatients)
            {
                count++;
                Console.WriteLine("###############################################################################");
                Console.WriteLine(string.Format("🩺 BỆNH ÁN #{0}: {1} | Mã BN: {2} | Mã ĐT: {3} | ID: {4}", 
                    count, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));
                Console.WriteLine(string.Format("Buồng: {0} | Giường: {1} | Thời gian vào viện: {2}", 
                    p.BED_ROOM_NAME, p.BED_NAME, p.IN_TIME));
                Console.WriteLine("###############################################################################");

                // 1. Thông tin Bìa bệnh án & Chẩn đoán vào viện
                try
                {
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = p.TREATMENT_ID };
                    var treatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                    if (treatments != null && treatments.Count > 0)
                    {
                        var t = treatments[0];
                        Console.WriteLine("\n📌 [1. THÔNG TIN BÌA BỆNH ÁN & CHẨN ĐOÁN VÀO VIỆN]");
                        Console.WriteLine(string.Format("   * Họ và tên         : {0} ({1}) - Sinh năm: {2}", t.TDL_PATIENT_NAME, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_DOB));
                        Console.WriteLine(string.Format("   * Địa chỉ           : {0}", t.TDL_PATIENT_ADDRESS));
                        Console.WriteLine(string.Format("   * Số thẻ BHYT       : {0}", t.TDL_HEIN_CARD_NUMBER));
                        Console.WriteLine(string.Format("   * Vào viện lúc      : {0} (Vào khoa: {1})", t.IN_TIME, t.CLINICAL_IN_TIME));
                        Console.WriteLine(string.Format("   * Chẩn đoán KKB/CC  : [{0}] {1}", t.ICD_CODE, t.ICD_NAME));
                        Console.WriteLine(string.Format("   * Chẩn đoán vào khoa: [{0}] {1}", t.IN_ICD_CODE, t.IN_ICD_NAME));
                        if (!string.IsNullOrEmpty(t.ICD_SUB_CODE) || !string.IsNullOrEmpty(t.ICD_TEXT))
                        {
                            Console.WriteLine(string.Format("   * Bệnh kèm theo     : [{0}] {1}", t.ICD_SUB_CODE, t.ICD_TEXT));
                        }
                        Console.WriteLine(string.Format("   * Bác sĩ điều trị   : {0} ({1})", t.DOCTOR_USERNAME, t.DOCTOR_LOGINNAME));
                        Console.WriteLine(string.Format("   * Khoa điều trị     : {0}", t.END_DEPARTMENT_NAME));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   Lỗi lấy Treatment: " + ex.Message);
                }

                // 2. Dấu hiệu sinh tồn lúc vào viện
                try
                {
                    HisDhstViewFilter dhstf = new HisDhstViewFilter 
                    { 
                        TREATMENT_ID = p.TREATMENT_ID,
                        ORDER_FIELD = "EXECUTE_TIME",
                        ORDER_DIRECTION = "ASC"
                    };
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstf, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        var d = dhsts[0];
                        Console.WriteLine("\n📌 [2. DẤU HIỆU SINH TỒN (DHST) LÚC VÀO VIỆN]");
                        Console.WriteLine(string.Format("   Mạch: {0} l/p | Huyết áp: {1}/{2} mmHg | Nhiệt độ: {3} °C | SpO2: {4} % | Nhịp thở: {5} l/p | Cân nặng: {6} kg",
                            d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.BREATH_RATE, d.WEIGHT));
                    }
                }
                catch { }

                // 3. Tờ điều trị đầu tiên (First Tracking Sheet)
                try
                {
                    HisTrackingViewFilter trkf = new HisTrackingViewFilter 
                    { 
                        TREATMENT_ID = p.TREATMENT_ID,
                        ORDER_FIELD = "TRACKING_TIME",
                        ORDER_DIRECTION = "ASC"
                    };
                    var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkf, param);
                    if (trackings != null && trackings.Count > 0)
                    {
                        var firstTrk = trackings[0];
                        Console.WriteLine("\n📌 [3. TỜ ĐIỀU TRỊ ĐẦU TIÊN (FIRST TREATMENT TRACKING)]");
                        Console.WriteLine(string.Format("   * Thời gian lập : {0} (ID: {1})", firstTrk.TRACKING_TIME, firstTrk.ID));
                        Console.WriteLine(string.Format("   * Người lập     : {0}", firstTrk.CREATOR));
                        Console.WriteLine(string.Format("   * Chế độ CS     : {0}", firstTrk.CARE_INSTRUCTION));
                        Console.WriteLine("   * Diễn biến lâm sàng lúc vào viện (CONTENT):");
                        if (!string.IsNullOrEmpty(firstTrk.CONTENT))
                        {
                            string[] lines = firstTrk.CONTENT.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            foreach (var l in lines) Console.WriteLine("     | " + l);
                        }
                        else
                        {
                            Console.WriteLine("     | (Trống)");
                        }
                        Console.WriteLine("   * Y lệnh ghi trên Tờ điều trị (MEDICAL_INSTRUCTION):");
                        if (!string.IsNullOrEmpty(firstTrk.MEDICAL_INSTRUCTION))
                        {
                            string[] lines = firstTrk.MEDICAL_INSTRUCTION.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            foreach (var l in lines) Console.WriteLine("     > " + l);
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n📌 [3. TỜ ĐIỀU TRỊ ĐẦU TIÊN]: 🔴 Chưa tạo tờ điều trị!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   Lỗi lấy Tracking: " + ex.Message);
                }

                // 4. Đơn thuốc thực tế ngày đầu vào viện
                try
                {
                    HisExpMestMedicineViewFilter emf = new HisExpMestMedicineViewFilter 
                    { 
                        TDL_TREATMENT_ID = p.TREATMENT_ID,
                        ORDER_FIELD = "TDL_INTRUCTION_TIME",
                        ORDER_DIRECTION = "ASC"
                    };
                    var medicines = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);
                    if (medicines != null && medicines.Count > 0)
                    {
                        var minTime = medicines.Where(m => m.TDL_INTRUCTION_TIME.HasValue).Min(m => m.TDL_INTRUCTION_TIME.Value);
                        string firstDate = minTime.ToString().Substring(0, 8);
                        var firstDayMeds = medicines.Where(m => m.TDL_INTRUCTION_TIME.HasValue && m.TDL_INTRUCTION_TIME.Value.ToString().StartsWith(firstDate)).ToList();

                        Console.WriteLine(string.Format("\n📌 [4. ĐƠN THUỐC NGÀY ĐẦU VÀO VIỆN ({0}) - Tổng: {1} loại thuốc]", firstDate, firstDayMeds.Count));
                        int mIdx = 1;
                        foreach (var m in firstDayMeds)
                        {
                            Console.WriteLine(string.Format("   {0,2}. {1,-38} | SL: {2,3} {3,-5} | Kho: {4,-15} | HDSD: {5}",
                                mIdx++, m.MEDICINE_TYPE_NAME, m.AMOUNT, m.SERVICE_UNIT_NAME, m.MEDI_STOCK_NAME, m.TUTORIAL));
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n📌 [4. ĐƠN THUỐC NGÀY ĐẦU]: (Chưa có đơn thuốc xuất kho)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   Lỗi lấy ExpMestMedicine: " + ex.Message);
                }

                // 5. Cận lâm sàng chỉ định ngày đầu
                try
                {
                    HisSereServViewFilter ssf = new HisSereServViewFilter { TREATMENT_ID = p.TREATMENT_ID };
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssf, param);
                    if (sss != null && sss.Count > 0)
                    {
                        var validTimes = sss.Where(s => s.TDL_INTRUCTION_TIME > 0).Select(s => s.TDL_INTRUCTION_TIME).ToList();
                        if (validTimes.Count > 0)
                        {
                            var minTime = validTimes.Min();
                            string firstDate = minTime.ToString().Substring(0, 8);
                            var firstDayCls = sss.Where(s => s.TDL_INTRUCTION_TIME > 0 && s.TDL_INTRUCTION_TIME.ToString().StartsWith(firstDate) && s.TDL_SERVICE_TYPE_ID != 6).ToList();

                            Console.WriteLine(string.Format("\n📌 [5. CHỈ ĐỊNH CẬN LÂM SÀNG & SUẤT ĂN NGÀY ĐẦU ({0}) - Tổng: {1} chỉ định]", firstDate, firstDayCls.Count));
                            foreach (var c in firstDayCls.Take(10))
                            {
                                Console.WriteLine(string.Format("   * [{0}] {1} (SL: {2})", c.TDL_SERVICE_CODE, c.TDL_SERVICE_NAME, c.AMOUNT));
                            }
                            if (firstDayCls.Count > 10)
                            {
                                Console.WriteLine(string.Format("   ... và {0} chỉ định khác.", firstDayCls.Count - 10));
                            }
                        }
                    }
                }
                catch { }

                Console.WriteLine("\n");
            }

            Console.WriteLine("===============================================================================");
            Console.WriteLine("✅ HOÀN TẤT TRÍCH XUẤT 10 BỆNH ÁN NGOẠI KHOA THỰC TẾ!");
            Console.WriteLine("===============================================================================");
        }
    }
}
