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
using MOS.Filter;
using MOS.EFMODEL.DataModels;
using MOS.SDO;

namespace InspectNinhBinh
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

        static string RemoveSign(string str)
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

        static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return RemoveSign(s).Trim().ToLower();
        }

        static string ReadLiveToken()
        {
            string[] candidates = new string[] {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "HLSLogSystem.txt"),
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
            Load.Init();

            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (token == null)
            {
                Console.WriteLine("❌ LỖI: Không thể đăng nhập tài khoản vmc!");
                return;
            }

            string tokenCode = token.TokenCode;
            Console.WriteLine("✅ Đăng nhập vmc THÀNH CÔNG! TokenCode: " + tokenCode.Substring(0, 16) + "...");
            var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", tokenCode, "HIS");
            var sdaConsumer = new ApiConsumer("http://192.168.7.200:1410/", tokenCode, "HIS");
            MyAdapter adapter = new MyAdapter();

            // Kích hoạt WorkInfo
            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 },
                        new RoomSDO { RoomId = 5252 },
                        new RoomSDO { RoomId = 5251 },
                        new RoomSDO { RoomId = 5257 }
                    }
                };
                adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);
            }
            catch { }

            // 1. Lấy tất cả Branch
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("1. DANH SÁCH TẤT CẢ CƠ SỞ (BRANCH)");
            Console.WriteLine("===============================================================================");
            HisBranchFilter bf = new HisBranchFilter();
            var branches = adapter.FetchList<HIS_BRANCH>("api/HisBranch/Get", mosConsumer, bf, param);
            if (branches != null)
            {
                foreach (var b in branches)
                {
                    Console.WriteLine(string.Format("Branch ID: {0,3} | Code: {1,-10} | Name: {2}", b.ID, b.BRANCH_CODE, b.BRANCH_NAME));
                }
            }

            // 2. Lấy tất cả Department
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("2. TẤT CẢ KHOA PHÒNG (DEPARTMENT) TOÀN BỆNH VIỆN");
            Console.WriteLine("===============================================================================");
            HisDepartmentFilter df = new HisDepartmentFilter();
            var depts = adapter.FetchList<HIS_DEPARTMENT>("api/HisDepartment/Get", mosConsumer, df, param);
            Dictionary<long, HIS_DEPARTMENT> deptMap = new Dictionary<long, HIS_DEPARTMENT>();
            if (depts != null)
            {
                foreach (var d in depts)
                {
                    deptMap[d.ID] = d;
                    string nameNorm = Norm(d.DEPARTMENT_NAME ?? "");
                    if (d.BRANCH_ID == 81 || nameNorm.Contains("ngoai") || nameNorm.Contains("ninh") || nameNorm.Contains("tong hop"))
                    {
                        Console.WriteLine(string.Format("Dept ID: {0,4} | Branch ID: {1,3} | Code: {2,-10} | Active: {3} | Name: {4}", 
                            d.ID, d.BRANCH_ID, d.DEPARTMENT_CODE, d.IS_ACTIVE, d.DEPARTMENT_NAME));
                    }
                }
            }

            // 3. Lấy TẤT CẢ bệnh nhân đang nằm buồng trong toàn viện
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("3. QUÉT TOÀN BỘ BỆNH NHÂN ĐANG NẰM NỘI TRÚ TOÀN VIỆN (GetView)");
            Console.WriteLine("===============================================================================");
            HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
            tbrf.IS_IN_ROOM = true;
            var allInBeds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mosConsumer, tbrf, param);
            if (allInBeds != null)
            {
                Console.WriteLine(string.Format("Tổng số bệnh nhân đang nằm buồng toàn viện: {0} bệnh nhân", allInBeds.Count));
                
                // Nhóm theo khoa
                var grpDept = allInBeds.GroupBy(x => x.DEPARTMENT_ID).ToList();
                Console.WriteLine(string.Format("Phân bố trên {0} khoa phòng:", grpDept.Count));
                foreach (var g in grpDept)
                {
                    string dName = deptMap.ContainsKey(g.Key) ? deptMap[g.Key].DEPARTMENT_NAME : ("Khoa ID " + g.Key);
                    long bId = deptMap.ContainsKey(g.Key) ? deptMap[g.Key].BRANCH_ID : 0;
                    Console.WriteLine(string.Format("   - Khoa {0,-45} (DeptID: {1,3}, Branch: {2,2}): {3,3} BN", dName, g.Key, bId, g.Count()));
                }

                // Lọc bệnh nhân tên "Cách"
                Console.WriteLine("\n-------------------------------------------------------------------------------");
                Console.WriteLine("KẾT QUẢ TÌM BỆNH NHÂN TÊN CHỨA 'CÁCH' TRONG DANH SÁCH ĐANG NẰM VIỆN:");
                Console.WriteLine("-------------------------------------------------------------------------------");
                var matchCach = allInBeds.Where(x => Norm(x.TDL_PATIENT_NAME ?? "").Contains("cach")).ToList();
                if (matchCach.Count > 0)
                {
                    foreach (var p in matchCach)
                    {
                        string dName = deptMap.ContainsKey(p.DEPARTMENT_ID) ? deptMap[p.DEPARTMENT_ID].DEPARTMENT_NAME : "";
                        Console.WriteLine(string.Format("⭐ BN: {0,-25} | Mã BN: {1,-10} | Mã BA: {2,-12} | Khoa: {3} | Buồng: {4} - Giường: {5}",
                            p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, dName, p.BED_ROOM_NAME, p.BED_NAME));

                        // Lấy chi tiết hồ sơ điều trị
                        HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = p.TREATMENT_ID };
                        var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                        if (tList != null && tList.Count > 0)
                        {
                            var t = tList[0];
                            Console.WriteLine(string.Format("   -> Ngày sinh: {0} | Giới tính: {1} | Vào viện: {2} | BS điều trị: {3}",
                                t.TDL_PATIENT_DOB, t.TDL_PATIENT_GENDER_NAME, t.IN_TIME, t.DOCTOR_USERNAME));
                            Console.WriteLine(string.Format("   -> Chẩn đoán: {0} ({1})", t.ICD_NAME ?? t.ICD_TEXT, t.ICD_CODE));
                            Console.WriteLine(string.Format("   -> Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Không có bệnh nhân nào tên 'Cách' trong danh sách đang nằm buồng.");
                }
            }
            else
            {
                Console.WriteLine("Không lấy được danh sách TreatmentBedRoom/GetView: " + param.HasException);
            }

            // 4. Tìm kiếm nâng cao trong HIS_PATIENT và HIS_TREATMENT
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("4. TÌM KIẾM NÂNG CAO TẤT CẢ BỆNH NHÂN TÊN CÓ CHỮ 'CÁCH' TRONG LỊCH SỬ");
            Console.WriteLine("===============================================================================");
            HisPatientViewFilter pvf = new HisPatientViewFilter();
            pvf.KEY_WORD = "Cách";
            var patViewList = adapter.FetchList<V_HIS_PATIENT>("api/HisPatient/GetView", mosConsumer, pvf, param);
            if (patViewList != null && patViewList.Count > 0)
            {
                Console.WriteLine(string.Format("Tìm thấy {0} bệnh nhân tên 'Cách' trong CSDL bệnh nhân:", patViewList.Count));
                foreach (var p in patViewList)
                {
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.WriteLine(string.Format("BN: {0} | Mã BN: {1} | Ngày sinh: {2} | Giới: {3} | Địa chỉ: {4}", 
                        p.VIR_PATIENT_NAME, p.PATIENT_CODE, p.DOB, p.GENDER_NAME, p.VIR_ADDRESS));
                    
                    HisTreatmentViewFilter pTf = new HisTreatmentViewFilter { PATIENT_ID = p.ID };
                    var pTreatments = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, pTf, param);
                    if (pTreatments != null && pTreatments.Count > 0)
                    {
                        foreach (var pt in pTreatments.OrderByDescending(x => x.IN_TIME))
                        {
                            Console.WriteLine(string.Format("   • HSBA: {0} | Vào: {1} | Ra: {2} | Khoa: {3} | BS: {4} | ICD: {5} - {6} | Trạng thái: {7}",
                                pt.TREATMENT_CODE, pt.IN_TIME, pt.OUT_TIME,
                                pt.END_DEPARTMENT_NAME,
                                pt.DOCTOR_USERNAME,
                                pt.ICD_CODE, pt.ICD_NAME ?? pt.ICD_TEXT,
                                pt.IS_PAUSE == 1 ? "Đã ra viện" : "🔴 ĐANG ĐIỀU TRỊ"));
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Không tìm thấy trong HisPatient/GetView với KEY_WORD = 'Cách'.");
            }
        }
    }
}
