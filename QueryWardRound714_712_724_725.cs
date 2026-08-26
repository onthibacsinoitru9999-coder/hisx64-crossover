using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Token.Core;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

namespace WardRoundQuery
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
            StreamWriter logFile = new StreamWriter("ward_round_714_712_724_725_data.txt", false, Encoding.UTF8);

            Action<string> Log = delegate(string msg)
            {
                Console.WriteLine(msg);
                logFile.WriteLine(msg);
            };

            try
            {
                Load.Init();
                CommonParam param = new CommonParam();
                string tokenCode = "";

                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs\LogSystem.txt");
                    if (File.Exists(logPath))
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
                                    int tIdx = lines[i].IndexOf("TokenCode|") + 10;
                                    if (lines[i].Length >= tIdx + 64)
                                    {
                                        tokenCode = lines[i].Substring(tIdx, 64);
                                        Log("Found live token from LogSystem.txt: " + tokenCode.Substring(0, 8) + "...");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Could not read live token: " + ex.Message);
                }

                if (string.IsNullOrEmpty(tokenCode))
                {
                    ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                    var token = tokenManager.Login(param, "034727", "9981", "2.390.0");
                    if (token == null)
                    {
                        token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                    }
                    if (token != null)
                    {
                        tokenCode = token.TokenCode;
                        Log("Logged in successfully with token: " + tokenCode.Substring(0, 8) + "...");
                    }
                }

                if (string.IsNullOrEmpty(tokenCode))
                {
                    Log("LOGIN FAILED!");
                    return;
                }
                var mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", tokenCode, "MOS");
                MyAdapter adapter = new MyAdapter();

                Log("==========================================================================");
                Log("TRÍCH XUẤT DỮ LIỆU BỆNH NHÂN CÁC BUỒNG 714, 712, 724, 725 - KHOA CTCH & CỘT SỐNG (57)");
                Log("==========================================================================\n");

                // 1. Lấy tất cả buồng bệnh trong Khoa 57
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", mosConsumer, bf, param);
                
                string[] targetRoomKeywords = new string[] { "714", "712", "724", "725" };
                List<V_HIS_BED_ROOM> matchedRooms = new List<V_HIS_BED_ROOM>();

                if (bList != null)
                {
                    foreach (var kw in targetRoomKeywords)
                    {
                        var rms = bList.Where(x => x.BED_ROOM_NAME != null && x.BED_ROOM_NAME.Contains(kw)).ToList();
                        matchedRooms.AddRange(rms);
                        Log(string.Format("Tìm kiếm buồng '{0}': Tìm thấy {1} buồng", kw, rms.Count));
                        foreach (var r in rms)
                        {
                            Log(string.Format("   - Buồng ID: {0} | Tên: {1} | Mã: {2}", r.ID, r.BED_ROOM_NAME, r.BED_ROOM_CODE));
                        }
                    }
                }

                // 2. Lấy toàn bộ bệnh nhân đang nằm trong các buồng này
                List<V_HIS_TREATMENT_BED_ROOM> targetPatients = new List<V_HIS_TREATMENT_BED_ROOM>();
                foreach (var rm in matchedRooms.Distinct())
                {
                    HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                    tbrf.BED_ROOM_ID = rm.ID;
                    tbrf.IS_IN_ROOM = true;
                    var pts = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);
                    if (pts != null && pts.Count > 0)
                    {
                        targetPatients.AddRange(pts);
                    }
                }

                Log(string.Format("\nTổng số bệnh nhân đang nằm tại các buồng 714, 712, 724, 725: {0} bệnh nhân", targetPatients.Count));

                // 3. Trích xuất chi tiết từng bệnh nhân
                int idx = 1;
                foreach (var p in targetPatients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    long treatmentId = p.TREATMENT_ID;
                    Log("\n==========================================================================");
                    Log(string.Format("BỆNH NHÂN {0}: {1} | BUỒNG: {2} | GIƯỜNG: {3}", idx++, p.TDL_PATIENT_NAME, p.BED_ROOM_NAME, p.BED_NAME));
                    Log("==========================================================================");

                    // Treatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = treatmentId;
                    var tList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                    var t = tList != null ? tList.FirstOrDefault() : null;

                    if (t != null)
                    {
                        Log(string.Format("• Mã BN: {0} | Mã BA: {1} | Giới tính: {2} | Ngày sinh: {3} | Thẻ BHYT: {4}",
                            t.TDL_PATIENT_CODE, t.TREATMENT_CODE, t.TDL_PATIENT_GENDER_NAME, t.TDL_PATIENT_DOB, t.TDL_HEIN_CARD_NUMBER));
                        Log(string.Format("• Địa chỉ: {0}", t.TDL_PATIENT_ADDRESS));
                        Log(string.Format("• Thời gian vào viện: {0} | Trạng thái: {1}",
                            t.IN_TIME, t.IS_PAUSE == 1 ? "ĐÃ RA VIỆN" : "ĐANG ĐIỀU TRỊ"));
                        Log(string.Format("• Chẩn đoán vào viện: {0} [{1}] - {2}", t.IN_ICD_NAME, t.IN_ICD_CODE, t.IN_ICD_TEXT));
                        Log(string.Format("• Chẩn đoán hiện tại: {0} [{1}] - {2}", t.ICD_NAME, t.ICD_CODE, t.ICD_TEXT));
                        Log(string.Format("• Chẩn đoán kèm theo: {0} - {1}", t.ICD_SUB_CODE, t.ICD_TEXT));
                    }

                    // Department Transfers
                    HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter();
                    dtf.TREATMENT_ID = treatmentId;
                    var dts = adapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", mosConsumer, dtf, param);
                    if (dts != null && dts.Count > 0)
                    {
                        Log("\n--- LỊCH SỬ CHUYỂN KHOA / VÀO KHOA ---");
                        foreach (var dt in dts.OrderBy(x => x.DEPARTMENT_IN_TIME ?? x.CREATE_TIME))
                        {
                            Log(string.Format("  • [{0}] Khoa: {1} (ID {2}) | Từ: {3} | Bác sĩ/Người tạo: {4}",
                                dt.DEPARTMENT_IN_TIME ?? dt.CREATE_TIME, dt.DEPARTMENT_NAME, dt.DEPARTMENT_ID, dt.PREVIOUS_DEPARTMENT_NAME, dt.CREATOR));
                        }
                    }

                    // DHST
                    HisDhstViewFilter dhstFilter = new HisDhstViewFilter();
                    dhstFilter.TREATMENT_ID = treatmentId;
                    var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mosConsumer, dhstFilter, param);
                    if (dhsts != null && dhsts.Count > 0)
                    {
                        Log("\n--- DẤU HIỆU SINH TỒN (GẦN NHẤT ĐẾN CŨ NHẤT) ---");
                        foreach (var d in dhsts.OrderByDescending(x => x.EXECUTE_TIME ?? x.CREATE_TIME).Take(5))
                        {
                            Log(string.Format("  • [{0}]: Mạch {1} ck/p | HA: {2}/{3} mmHg | NĐ: {4} °C | SpO2: {5}% | Thở: {6} l/p | Cân nặng: {7} kg",
                                d.EXECUTE_TIME ?? d.CREATE_TIME, d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.BREATH_RATE, d.WEIGHT));
                        }
                    }

                    // Trackings (Tất cả tờ điều trị)
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = treatmentId;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        Log("\n--- LỊCH SỬ TỜ ĐIỀU TRỊ & DIỄN BIẾN LÂM SÀNG ---");
                        foreach (var trk in trks.OrderBy(x => x.TRACKING_TIME))
                        {
                            Log(string.Format("\n  [TỜ ĐIỀU TRỊ: {0}] Phòng: {1} | Bác sĩ: {2}",
                                trk.TRACKING_TIME, trk.ROOM_NAME, trk.CREATOR));
                            if (!string.IsNullOrEmpty(trk.CONTENT))
                                Log(string.Format("    - Diễn biến: {0}", trk.CONTENT.Replace("\r\n", "\n      ").Replace("\n", "\n      ")));
                            if (!string.IsNullOrEmpty(trk.MEDICAL_INSTRUCTION))
                                Log(string.Format("    - Y lệnh thuốc: {0}", trk.MEDICAL_INSTRUCTION.Replace("\r\n", "\n      ").Replace("\n", "\n      ")));
                            if (!string.IsNullOrEmpty(trk.CARE_INSTRUCTION))
                                Log(string.Format("    - Y lệnh chăm sóc: {0}", trk.CARE_INSTRUCTION.Replace("\r\n", "\n      ").Replace("\n", "\n      ")));
                        }
                    }

                    // SereServ (Tất cả dịch vụ: CĐHA, TDCN, Phẫu thuật, Thủ thuật, Xét nghiệm)
                    HisSereServViewFilter ssFilter = new HisSereServViewFilter();
                    ssFilter.TREATMENT_ID = treatmentId;
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mosConsumer, ssFilter, param);
                    if (sss != null && sss.Count > 0)
                    {
                        Log("\n--- TỔNG HỢP CẬN LÂM SÀNG, HÌNH ẢNH & CAN THIỆP PHẪU THUẬT ---");
                        var nonMedServices = sss.Where(x => x.TDL_SERVICE_TYPE_ID != 6 && x.TDL_SERVICE_TYPE_ID != 7).OrderBy(x => x.TDL_INTRUCTION_TIME).ToList();
                        foreach (var s in nonMedServices)
                        {
                            Log(string.Format("  • [{0}] [{1}] {2} (SL: {3} {4}) | Khoa/Phòng: {5} | Bác sĩ: {6} | Trạng thái: {7}",
                                s.TDL_INTRUCTION_TIME, s.SERVICE_TYPE_NAME, s.TDL_SERVICE_NAME, s.AMOUNT, s.SERVICE_UNIT_NAME, s.EXECUTE_ROOM_NAME, s.TDL_REQUEST_LOGINNAME, s.IS_EXPEND == 1 ? "Hao phí" : "Chỉ định"));
                        }
                    }

                    // Tein (Chi tiết các chỉ số xét nghiệm)
                    HisSereServTeinViewFilter teinFilter = new HisSereServTeinViewFilter();
                    teinFilter.TDL_TREATMENT_ID = treatmentId;
                    var teins = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mosConsumer, teinFilter, param);
                    if (teins != null && teins.Count > 0)
                    {
                        Log("\n--- KẾT QUẢ XÉT NGHIỆM CHI TIẾT ---");
                        var validTeins = teins.Where(x => !string.IsNullOrEmpty(x.VALUE)).ToList();
                        foreach (var grp in validTeins.GroupBy(x => x.TEST_INDEX_GROUP_NAME ?? "Xét nghiệm khác"))
                        {
                            Log(string.Format("\n  * [{0}]:", grp.Key));
                            foreach (var item in grp.OrderBy(x => x.MODIFY_TIME ?? x.CREATE_TIME).ThenBy(x => x.TEST_INDEX_NAME))
                            {
                                Log(string.Format("    - [{0}] {1} ({2}): {3} {4} [Tham chiếu: {5}]",
                                    item.MODIFY_TIME ?? item.CREATE_TIME, item.TEST_INDEX_NAME, item.TEST_INDEX_CODE, item.VALUE, item.TEST_INDEX_UNIT_NAME, item.NOTE));
                            }
                        }
                    }

                    // ServiceReq (Các yêu cầu y lệnh, đơn thuốc, chỉ định)
                    HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                    srf.TREATMENT_ID = treatmentId;
                    var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                    if (srs != null && srs.Count > 0)
                    {
                        Log("\n--- CÁC PHIẾU Y LỆNH & ĐƠN THUỐC ĐÃ KÊ (SERVICE_REQ) ---");
                        foreach (var req in srs.OrderByDescending(x => x.INTRUCTION_TIME))
                        {
                            Log(string.Format("  • [{0}] [{1}] Mã: {2} | BS: {3} | Phòng: {4} | Trạng thái: {5} | ICD: {6} - {7}",
                                req.INTRUCTION_TIME, req.SERVICE_REQ_TYPE_NAME, req.SERVICE_REQ_CODE, req.REQUEST_LOGINNAME, req.EXECUTE_ROOM_NAME, req.SERVICE_REQ_STT_NAME, req.ICD_CODE, req.ICD_NAME));
                        }
                    }

                    // Thuốc đã xuất / đã kê (ExpMestMedicine)
                    HisExpMestMedicineViewFilter emf = new HisExpMestMedicineViewFilter();
                    emf.TDL_TREATMENT_ID = treatmentId;
                    var ems = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mosConsumer, emf, param);
                    if (ems != null && ems.Count > 0)
                    {
                        Log("\n--- CHI TIẾT THUỐC ĐÃ KÊ / XUẤT DƯỢC ---");
                        foreach (var med in ems.OrderByDescending(x => x.EXP_TIME ?? x.CREATE_TIME))
                        {
                            Log(string.Format("  • [{0}] {1} ({2}) | SL: {3} {4} | Kho: {5} | HDSD: {6}",
                                med.EXP_TIME ?? med.CREATE_TIME, med.MEDICINE_TYPE_NAME, med.MEDICINE_TYPE_CODE, med.AMOUNT, med.SERVICE_UNIT_NAME, med.MEDI_STOCK_NAME, med.TUTORIAL));
                        }
                    }
                }

                Log("\n==========================================================================");
                Log("HOÀN TẤT TRÍCH XUẤT DỮ LIỆU TẤT CẢ BUỒNG 714, 712, 724, 725!");
                Log("==========================================================================");
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.ToString());
            }
            finally
            {
                logFile.Close();
            }
        }
    }
}
