using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

namespace SearchNinhBinh
{
    public class MyAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }
        public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
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

        static string RemoveSign4VietnameseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            string[] Signs = new string[] {
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
            for (int i = 1; i < Signs.Length; i++)
            {
                for (int j = 0; j < Signs[i].Length; j++)
                    str = str.Replace(Signs[i][j], Signs[0][i - 1]);
            }
            return str;
        }

        static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return RemoveSign4VietnameseString(s).Trim().ToLower();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CommonParam param = new CommonParam();
            Load.Init();

            // 1. Get Token
            string tokenCode = null;
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt");
            if (File.Exists(logPath))
            {
                try
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
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
                                    tokenCode = lines[i].Substring(idx, 64);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            if (string.IsNullOrEmpty(tokenCode))
            {
                var token = tokenManager.Login(param, "034727", "998199", "2.390.0");
                if (token != null) tokenCode = token.TokenCode;
                else
                {
                    token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    if (token != null) tokenCode = token.TokenCode;
                }
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                Console.WriteLine("❌ LỖI: Không thể đăng nhập hoặc lấy token!");
                return;
            }

            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
            var sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", tokenCode, "HIS");
            MyAdapter adapter = new MyAdapter();

            Console.WriteLine("===============================================================================");
            Console.WriteLine("1. TRA CỨU DANH SÁCH CƠ SỞ (BRANCHES)");
            Console.WriteLine("===============================================================================");
            try
            {
                HisBranchFilter bfRaw = new HisBranchFilter();
                var rawBranches = adapter.FetchList<HIS_BRANCH>("api/HisBranch/Get", mosConsumer, bfRaw, param);
                if (rawBranches != null && rawBranches.Count > 0)
                {
                    foreach (var b in rawBranches)
                    {
                        Console.WriteLine(string.Format("Branch ID: {0,3} | Code: {1,-10} | Name: {2}", b.ID, b.BRANCH_CODE, b.BRANCH_NAME));
                    }
                }
                else
                {
                    Console.WriteLine("Không lấy được danh sách cơ sở từ HisBranch/Get.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy Branch: " + ex.Message);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("2. TRA CỨU DANH SÁCH KHOA PHÒNG TẠI CƠ SỞ NINH BÌNH (BRANCH_ID = 81)");
            Console.WriteLine("===============================================================================");
            List<HIS_DEPARTMENT> nbDeptsRaw = null;
            try
            {
                HisDepartmentFilter df = new HisDepartmentFilter { BRANCH_ID = 81 };
                nbDeptsRaw = adapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, df, param);
                if (nbDeptsRaw != null && nbDeptsRaw.Count > 0)
                {
                    foreach (var d in nbDeptsRaw)
                    {
                        Console.WriteLine(string.Format("Dept ID: {0,4} | Code: {1,-10} | Name: {2} | Active: {3}", 
                            d.ID, d.DEPARTMENT_CODE, d.DEPARTMENT_NAME, d.IS_ACTIVE));
                    }
                }
                else
                {
                    Console.WriteLine("Không tìm thấy theo BRANCH_ID = 81 trong HisDepartment/Get. Lấy toàn bộ danh sách phòng ban:");
                    HisDepartmentFilter allDf = new HisDepartmentFilter();
                    var allDepts = adapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, allDf, param);
                    if (allDepts != null)
                    {
                        foreach (var d in allDepts.Where(x => x.BRANCH_ID == 81 || (x.DEPARTMENT_NAME != null && (x.DEPARTMENT_NAME.ToLower().Contains("ngoại") || x.DEPARTMENT_NAME.ToLower().Contains("ninh")))))
                        {
                            Console.WriteLine(string.Format("Dept ID: {0,4} | Branch: {1,3} | Code: {2,-10} | Name: {3}", 
                                d.ID, d.BRANCH_ID, d.DEPARTMENT_CODE, d.DEPARTMENT_NAME));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy Department: " + ex.Message);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("3. TRA CỨU TẤT CẢ BUỒNG BỆNH TẠI CƠ SỞ NINH BÌNH");
            Console.WriteLine("===============================================================================");
            List<long> allNbBedRoomIds = new List<long>();
            try
            {
                HisBedRoomViewFilter brf = new HisBedRoomViewFilter();
                var allBedRooms = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, brf, param);
                if (allBedRooms != null && nbDeptsRaw != null)
                {
                    var nbDeptIds = nbDeptsRaw.Select(x => x.ID).ToList();
                    var nbBedRooms = allBedRooms.Where(x => nbDeptIds.Contains(x.DEPARTMENT_ID)).ToList();
                    Console.WriteLine(string.Format("Tìm thấy {0} buồng bệnh tại Cơ sở Ninh Bình:", nbBedRooms.Count));
                    foreach (var br in nbBedRooms)
                    {
                        Console.WriteLine(string.Format("BedRoom ID: {0,4} | Dept ID: {1,4} | Code: {2,-10} | Name: {3}", 
                            br.ID, br.DEPARTMENT_ID, br.BED_ROOM_CODE, br.BED_ROOM_NAME));
                        allNbBedRoomIds.Add(br.ID);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy BedRoom: " + ex.Message);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("4. TRA CỨU BỆNH NHÂN ĐANG NẰM TẠI CƠ SỞ NINH BÌNH & KHOA NGOẠI");
            Console.WriteLine("===============================================================================");
            try
            {
                if (allNbBedRoomIds.Count > 0)
                {
                    HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter
                    {
                        BED_ROOM_IDs = allNbBedRoomIds,
                        IS_IN_ROOM = true
                    };
                    var inPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
                    if (inPatients != null && inPatients.Count > 0)
                    {
                        Console.WriteLine(string.Format("Tổng số bệnh nhân đang nằm buồng tại CS Ninh Bình: {0}", inPatients.Count));
                        foreach (var p in inPatients)
                        {
                            Console.WriteLine(string.Format("- BN: {0,-25} | Mã BN: {1,-10} | Mã BA: {2,-15} | Buồng: {3} | Giường: {4}", 
                                p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.BED_ROOM_NAME, p.BED_NAME));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Hiện không có bệnh nhân nào có trạng thái IS_IN_ROOM = true tại các buồng CS Ninh Bình. Thử quét không lọc IS_IN_ROOM...");
                        HisTreatmentBedRoomLViewFilter tbrfAll = new HisTreatmentBedRoomLViewFilter
                        {
                            BED_ROOM_IDs = allNbBedRoomIds
                        };
                        var allPatients = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrfAll, param);
                        if (allPatients != null && allPatients.Count > 0)
                        {
                            Console.WriteLine(string.Format("Tìm thấy {0} lượt nằm buồng:", allPatients.Count));
                            foreach (var p in allPatients.OrderByDescending(x => x.ADD_TIME).Take(20))
                            {
                                Console.WriteLine(string.Format("- BN: {0,-25} | Mã BN: {1,-10} | Buồng: {2} | Vào: {3} | Ra: {4}", 
                                    p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.BED_ROOM_NAME, p.ADD_TIME, p.REMOVE_TIME));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy TreatmentBedRoom: " + ex.Message);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("5. QUÉT TẤT CẢ HỒ SƠ ĐIỀU TRỊ GẦN ĐÂY CỦA BỆNH VIỆN");
            Console.WriteLine("===============================================================================");
            try
            {
                // Lấy các hồ sơ điều trị gần đây
                HisTreatmentViewFilter tfRecent = new HisTreatmentViewFilter();
                var recentTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tfRecent, param);
                if (recentTreatments != null && recentTreatments.Count > 0)
                {
                    Console.WriteLine(string.Format("Tổng số hồ sơ trả về từ GetView: {0}", recentTreatments.Count));
                    var matched = recentTreatments.Where(x => Normalize(x.TDL_PATIENT_NAME ?? "").Contains("cach")).ToList();
                    Console.WriteLine(string.Format("Trong đó có {0} hồ sơ có tên chứa 'Cách':", matched.Count));
                    foreach (var t in matched)
                    {
                        Console.WriteLine(string.Format("   BN: {0,-25} | Mã BN: {1,-10} | HSBA: {2,-12} | Vào: {3} | Khoa: {4} | Trạng thái: {5} | ICD: {6}", 
                            t.TDL_PATIENT_NAME, t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.IN_TIME,
                            t.END_DEPARTMENT_NAME,
                            t.IS_PAUSE == 1 ? "Ra viện" : "ĐANG ĐIỀU TRỊ",
                            t.ICD_CODE));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi quét hồ sơ điều trị: " + ex.Message);
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("6. TÌM TOÀN BỆNH VIỆN BỆNH NHÂN TÊN CÓ CHỨA TỪ 'CÁCH' / 'CACH'");
            Console.WriteLine("===============================================================================");
            try
            {
                HisPatientFilter pf = new HisPatientFilter();
                var pats = adapter.FetchList<HIS_PATIENT>("api/HisPatient/Get", mosConsumer, pf, param);
                if (pats != null)
                {
                    var matched = pats.Where(x => Normalize(x.VIR_PATIENT_NAME ?? "").Contains("cach")).ToList();
                    Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân tên chứa 'Cách':", matched.Count));
                    foreach (var p in matched)
                    {
                        Console.WriteLine("-------------------------------------------------------------------------------");
                        Console.WriteLine(string.Format("Họ tên: {0} | Mã BN: {1} | Năm sinh: {2} | Giới tính: {3} | Quê/Địa chỉ: {4}", 
                            p.VIR_PATIENT_NAME, p.PATIENT_CODE, p.DOB, p.GENDER_ID == 1 ? "Nữ" : "Nam", p.VIR_ADDRESS));
                        
                        HisTreatmentViewFilter pTf = new HisTreatmentViewFilter { PATIENT_ID = p.ID };
                        var pTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, pTf, param);
                        if (pTreatments != null && pTreatments.Count > 0)
                        {
                            foreach (var pt in pTreatments.OrderByDescending(x => x.IN_TIME))
                            {
                                Console.WriteLine(string.Format("   -> HSBA: {0} | Vào: {1} | Ra: {2} | Khoa: {3} (ID: {4}) | BS: {5} | ICD: {6} - {7} | Nằm viện: {8}",
                                    pt.TREATMENT_CODE, pt.IN_TIME, pt.OUT_TIME,
                                    pt.END_DEPARTMENT_NAME, pt.LAST_DEPARTMENT_ID,
                                    pt.DOCTOR_USERNAME,
                                    pt.ICD_CODE, pt.ICD_NAME ?? pt.ICD_TEXT,
                                    pt.IS_PAUSE == 1 ? "Đã ra viện" : "ĐANG ĐIỀU TRỊ"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi quét HIS_PATIENT: " + ex.Message);
            }
        }
    }
}
