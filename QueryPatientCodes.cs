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

namespace QueryPatientCodes
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

            string[] requestedPatients = new string[]
            {
                "Phạm Văn Công",
                "Bế Văn Thường",
                "Nguyễn Trọng Tề",
                "Trần Thị Vi",
                "Lê Văn Chiến",
                "Nguyễn Thị Oanh",
                "Đinh Phú",
                "Hoàng Thị Phúc",
                "Nguyễn Thị Quế Hương",
                "Trần Thị Ngọc Hương",
                "Nguyễn Thị Biển",
                "Vũ Minh Phúc",
                "Nguyễn Thị Hoa",
                "Lưu Văn Khoa"
            };

            Console.WriteLine("=========================================================================================================");
            Console.WriteLine("LẤY TẤT CẢ BỆNH NHÂN ĐANG NẰM ĐIỀU TRỊ TẠI KHOA 57 (CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG)");
            Console.WriteLine("=========================================================================================================");

            HisBedLogViewFilter blFilter = new HisBedLogViewFilter();
            blFilter.IS_OUT = false;
            var bedLogs = adapter.FetchList<V_HIS_BED_LOG>("api/HisBedLog/GetView", ApiConsumers.MosConsumer, blFilter, param);

            // Lọc các bedLog thuộc khoa 57 hoặc buồng 7xx
            var activeBeds = bedLogs != null ? bedLogs.Where(x => x.DEPARTMENT_ID == 57 || (x.BED_ROOM_NAME != null && x.BED_ROOM_NAME.Contains("7"))).ToList() : new List<V_HIS_BED_LOG>();

            Console.WriteLine("Tìm thấy " + activeBeds.Count + " bệnh nhân đang nằm giường Khoa 57.");

            foreach (var reqName in requestedPatients)
            {
                Console.WriteLine("\n---------------------------------------------------------------------------------------------------------");
                Console.WriteLine(">>> TRA CỨU: " + reqName.ToUpper());

                string normReq = Normalize(reqName);

                // 1. Tìm trong giường bệnh active
                var matchedBeds = activeBeds.Where(x => Normalize(x.VIR_PATIENT_NAME ?? "").Contains(normReq) || normReq.Contains(Normalize(x.VIR_PATIENT_NAME ?? ""))).ToList();

                if (matchedBeds.Count > 0)
                {
                    foreach (var b in matchedBeds)
                    {
                        Console.WriteLine(string.Format("✔ [ĐANG NẰM KHOA 57] BN: {0} | Mã BN: {1} | Mã BA: {2} | TreatmentID: {3} | Buồng: {4} | Giường: {5} | Giới tính: {6} | DOB: {7}",
                            b.VIR_PATIENT_NAME, b.TDL_PATIENT_CODE, b.TDL_TREATMENT_CODE, b.TREATMENT_ID, b.BED_ROOM_NAME, b.BED_NAME, b.PATIENT_GENDER_NAME, b.TDL_PATIENT_DOB));
                    }
                }
                else
                {
                    // 2. Tìm kiếm trong HisTreatment đang điều trị
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.IS_PAUSE = false;
                    tf.KEY_WORD = reqName;
                    var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);

                    if (treats != null && treats.Count > 0)
                    {
                        var mTreats = treats.Where(x => Normalize(x.TDL_PATIENT_NAME ?? "").Contains(normReq)).ToList();
                        if (mTreats.Count > 0)
                        {
                            foreach (var tr in mTreats)
                            {
                                Console.WriteLine(string.Format("✔ [HỒ SƠ ĐANG ĐIỀU TRỊ] BN: {0} | Mã BN: {1} | Mã BA: {2} | TreatmentID: {3} | DOB: {4} | Vào: {5} | Chẩn đoán: {6}",
                                    tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID, tr.TDL_PATIENT_DOB, tr.IN_TIME, tr.ICD_NAME));
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Không tìm thấy bệnh nhân khớp chính xác tên '" + reqName + "' trong các hồ sơ đang điều trị.");
                            foreach (var tr in treats.Take(3))
                            {
                                Console.WriteLine(string.Format("   (Gợi ý gần đúng: {0} - Mã BN: {1} - Mã BA: {2} - Vào: {3})",
                                    tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.IN_TIME));
                            }
                        }
                    }
                    else
                    {
                        // 3. Thử tìm rộng không phân biệt trạng thái ra viện
                        HisTreatmentViewFilter tfAll = new HisTreatmentViewFilter();
                        tfAll.KEY_WORD = reqName;
                        var allTreats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tfAll, param);
                        if (allTreats != null && allTreats.Count > 0)
                        {
                            var mAll = allTreats.Where(x => Normalize(x.TDL_PATIENT_NAME ?? "").Contains(normReq)).OrderByDescending(x => x.IN_TIME).Take(2).ToList();
                            if (mAll.Count > 0)
                            {
                                foreach (var tr in mAll)
                                {
                                    Console.WriteLine(string.Format("✔ [HỒ SƠ TOÀN VIỆN] BN: {0} | Mã BN: {1} | Mã BA: {2} | TreatmentID: {3} | Vào: {4} | Ra: {5} | Chẩn đoán: {6}",
                                        tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID, tr.IN_TIME, tr.OUT_TIME, tr.ICD_NAME));
                                }
                            }
                            else
                            {
                                Console.WriteLine("❌ Không tìm thấy hồ sơ nào cho: " + reqName);
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Không tìm thấy bất kỳ hồ sơ nào qua từ khóa: " + reqName);
                        }
                    }
                }
            }
        }

        static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string s = input.ToLower().Trim();
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    s = s.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
            return s;
        }
    }
}
