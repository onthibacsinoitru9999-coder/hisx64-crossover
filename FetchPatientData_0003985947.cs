using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace FetchPatientData
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
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
                string path3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);
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
            ApiConsumers.SetConsunmer(token.TokenCode);
            MyAdapter adapter = new MyAdapter();

            string pCode = "0003985947";

            Console.WriteLine("=================================================================");
            Console.WriteLine("TRÍCH XUẤT TOÀN BỘ DỮ LIỆU BỆNH NHÂN: " + pCode);
            Console.WriteLine("=================================================================");

            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.PATIENT_CODE__EXACT = pCode;
            var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);

            if (treats == null || treats.Count == 0)
            {
                Console.WriteLine("Không tìm thấy hồ sơ điều trị nào!");
                return;
            }

            foreach (var tr in treats.OrderByDescending(x => x.IN_TIME))
            {
                Console.WriteLine(string.Format("\nHỒ SƠ: {0} | TreatmentID: {1} | Vào: {2} | Ra: {3} | Trạng thái: {4}",
                    tr.TREATMENT_CODE, tr.ID, tr.IN_TIME, tr.OUT_TIME, tr.IS_PAUSE == 1 ? "Đã kết thúc" : "Đang điều trị"));
                Console.WriteLine(string.Format("BN: {0} | DOB: {1} | Giới tính: {2} | Địa chỉ: {3} | BHYT: {4}",
                    tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_DOB, tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_ADDRESS, tr.TDL_HEIN_CARD_NUMBER));
                Console.WriteLine(string.Format("Chẩn đoán vào: {0} [{1}] - {2}", tr.ICD_NAME, tr.ICD_CODE, tr.ICD_SUB_CODE));
                Console.WriteLine(string.Format("Chẩn đoán ra/hiện tại: {0} [{1}] - {2}", tr.ICD_NAME, tr.ICD_CODE, tr.ICD_TEXT));

                long treatmentId = tr.ID;

                // 1. Tờ điều trị (Trackings)
                HisTrackingViewFilter tkf = new HisTrackingViewFilter();
                tkf.TREATMENT_ID = treatmentId;
                var trackings = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, param);
                Console.WriteLine("\n--- CÁC TỜ ĐIỀU TRỊ (TRACKINGS) ---");
                if (trackings != null)
                {
                    foreach (var tk in trackings.OrderBy(x => x.TRACKING_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] (ID: {1}):", tk.TRACKING_TIME, tk.ID));
                        if (!string.IsNullOrEmpty(tk.ICD_TEXT)) Console.WriteLine("    Chẩn đoán: " + tk.ICD_TEXT);
                        if (!string.IsNullOrEmpty(tk.CONTENT)) Console.WriteLine("    Diễn biến: " + tk.CONTENT.Replace("\n", " | "));
                        if (!string.IsNullOrEmpty(tk.CARE_INSTRUCTION)) Console.WriteLine("    Chăm sóc: " + tk.CARE_INSTRUCTION.Replace("\n", " | "));
                        if (!string.IsNullOrEmpty(tk.MEDICAL_INSTRUCTION)) Console.WriteLine("    Y lệnh: " + tk.MEDICAL_INSTRUCTION.Replace("\n", " | "));
                    }
                }

                // 2. Dấu hiệu sinh tồn (DHST)
                HisDhstViewFilter dhstf = new HisDhstViewFilter();
                dhstf.TREATMENT_ID = treatmentId;
                var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", ApiConsumers.MosConsumer, dhstf, param);
                Console.WriteLine("\n--- DẤU HIỆU SINH TỒN (DHST) ---");
                if (dhsts != null)
                {
                    foreach (var d in dhsts.OrderByDescending(x => x.EXECUTE_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] Mạch: {1} ck/p | HA: {2}/{3} mmHg | NĐ: {4} °C | SpO2: {5}% | Cân nặng: {6} kg | Chiều cao: {7} cm | Nhịp thở: {8}",
                            d.EXECUTE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT, d.HEIGHT, d.BREATH_RATE));
                    }
                }

                // 3. Kết quả xét nghiệm (Tein)
                HisSereServTeinViewFilter teinf = new HisSereServTeinViewFilter();
                teinf.TDL_TREATMENT_ID = treatmentId;
                var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", ApiConsumers.MosConsumer, teinf, param);
                Console.WriteLine("\n--- KẾT QUẢ XÉT NGHIỆM CHI TIẾT ---");
                if (teins != null && teins.Count > 0)
                {
                    foreach (var t in teins.Where(x => !string.IsNullOrEmpty(x.VALUE)))
                    {
                        Console.WriteLine(string.Format("  • {0} ({1}): {2} {3}", t.TEST_INDEX_NAME, t.TEST_INDEX_CODE, t.VALUE, t.TEST_INDEX_UNIT_NAME));
                    }
                }

                // 4. Các dịch vụ Cận lâm sàng & Hình ảnh
                HisSereServViewFilter ssf = new HisSereServViewFilter();
                ssf.TREATMENT_ID = treatmentId;
                var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);
                Console.WriteLine("\n--- DANH SÁCH DỊCH VỤ CẬN LÂM SÀNG & HÌNH ẢNH ---");
                if (ssList != null)
                {
                    foreach (var s in ssList.OrderBy(x => x.TDL_INTRUCTION_TIME))
                    {
                        Console.WriteLine(string.Format("  [{0}] {1,-50} | Loại: {2,-15} | Phòng: {3}",
                            s.TDL_INTRUCTION_TIME, s.TDL_SERVICE_NAME, s.SERVICE_TYPE_NAME, s.EXECUTE_ROOM_NAME));
                    }
                }
            }
        }
    }
}
