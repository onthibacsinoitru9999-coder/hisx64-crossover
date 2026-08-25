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

namespace FindAll14Patients
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

            // 1. Lấy tất cả buồng bệnh khoa 57
            HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
            bf.DEPARTMENT_ID = 57;
            var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);
            List<long> bedRoomIds = (bList != null) ? bList.Select(x => x.ID).ToList() : new List<long>();

            // 2. Lấy toàn bộ bệnh nhân đang nằm viện tại Khoa 57
            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
            tbrf.BED_ROOM_IDs = bedRoomIds;
            tbrf.IS_IN_ROOM = true;
            var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);

            string[] targets = new string[]
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

            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format("{0,-4} | {1,-24} | {2,-10} | {3,-16} | {4,-15} | {5,-15} | {6}", 
                "STT", "HỌ VÀ TÊN", "MÃ BN", "MÃ BA (HỒ SƠ)", "BUỒNG", "GIƯỜNG", "CHẨN ĐOÁN / TRẠNG THÁI"));
            Console.WriteLine("=========================================================================================================================");

            int idx = 1;
            foreach (var name in targets)
            {
                string norm = Normalize(name);
                V_HIS_TREATMENT_BED_ROOM matchedIn = null;
                if (inPatients != null)
                {
                    matchedIn = inPatients.FirstOrDefault(x => Normalize(x.TDL_PATIENT_NAME ?? "").Contains(norm) || norm.Contains(Normalize(x.TDL_PATIENT_NAME ?? "")));
                }

                if (matchedIn != null)
                {
                    // Lấy chi tiết treatment để có chẩn đoán
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = matchedIn.TREATMENT_ID;
                    var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    string cd = (trList != null && trList.Count > 0) ? trList[0].ICD_NAME : "";

                    Console.WriteLine(string.Format("{0,-4} | {1,-24} | {2,-10} | {3,-16} | {4,-15} | {5,-15} | {6}",
                        idx++, matchedIn.TDL_PATIENT_NAME, matchedIn.TDL_PATIENT_CODE, matchedIn.TREATMENT_CODE, 
                        matchedIn.BED_ROOM_NAME, matchedIn.BED_NAME, cd));
                }
                else
                {
                    // Tìm kiếm qua HisTreatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.KEY_WORD = name;
                    var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    var matchedTreat = (treats != null) ? treats.Where(x => Normalize(x.TDL_PATIENT_NAME ?? "").Contains(norm) || norm.Contains(Normalize(x.TDL_PATIENT_NAME ?? ""))).OrderByDescending(x => x.IN_TIME).FirstOrDefault() : null;

                    if (matchedTreat != null)
                    {
                        string status = (matchedTreat.IS_PAUSE == 1 || matchedTreat.OUT_TIME.HasValue) ? "Đã ra viện" : "Đang ĐT (Chưa gán giường/Khoa khác)";
                        Console.WriteLine(string.Format("{0,-4} | {1,-24} | {2,-10} | {3,-16} | {4,-15} | {5,-15} | {6} ({7})",
                            idx++, matchedTreat.TDL_PATIENT_NAME, matchedTreat.TDL_PATIENT_CODE, matchedTreat.TREATMENT_CODE,
                            "---", "---", matchedTreat.ICD_NAME, status));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0,-4} | {1,-24} | {2,-10} | {3,-16} | {4,-15} | {5,-15} | {6}",
                            idx++, name.ToUpper(), "KHÔNG TÌM THẤY", "---", "---", "---", "Chưa có trên hệ thống hoặc sai tên"));
                    }
                }
            }

            Console.WriteLine("=========================================================================================================================");
            
            // In danh sách chi tiết từng bệnh nhân để đối chiếu
            Console.WriteLine("\n--- CHI TIẾT ĐỐI CHIẾU THÔNG TIN BỆNH NHÂN ---");
            foreach (var name in targets)
            {
                Console.WriteLine("\n>>> BỆNH NHÂN: " + name);
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = name;
                var treats = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                if (treats != null && treats.Count > 0)
                {
                    foreach (var tr in treats.OrderByDescending(x => x.IN_TIME).Take(2))
                    {
                        Console.WriteLine(string.Format("   - BN: {0} | DOB: {1} | Giới: {2} | Mã BN: {3} | Mã BA: {4} | TreatmentId: {5} | Vào: {6} | Chẩn đoán: {7}",
                            tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_DOB, tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.ID, tr.IN_TIME, tr.ICD_NAME));
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
