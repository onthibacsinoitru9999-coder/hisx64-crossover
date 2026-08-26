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
using MOS.SDO;

namespace InspectPres
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

        static string GetLiveToken()
        {
            string[] searchDirs = new string[]
            {
                @"Logs\LogSystem.txt",
                @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
                @"D:\New folder (3)\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
            };

            foreach (var path in searchDirs)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
            return "";
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            StreamWriter log = new StreamWriter("inspect_prescriptions_result.txt", false, Encoding.UTF8);

            Action<string> Log = msg =>
            {
                Console.WriteLine(msg);
                log.WriteLine(msg);
            };

            try
            {
                CommonParam param = new CommonParam();
                Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
                string tokenCode = "";
                if (token != null && !string.IsNullOrEmpty(token.TokenCode))
                {
                    tokenCode = token.TokenCode;
                }
                else
                {
                    tokenCode = GetLiveToken();
                }

                if (string.IsNullOrEmpty(tokenCode))
                {
                    Log("ERROR: Khong the lay duoc Token dang nhap!");
                    return;
                }

                ApiConsumers.SetConsunmer(tokenCode);
                MyAdapter adapter = new MyAdapter();

                // Work info
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 },
                        new RoomSDO { RoomId = 5252 }, // 712
                        new RoomSDO { RoomId = 5251 }, // 714
                        new RoomSDO { RoomId = 5257 }  // 724
                    }
                };
                try { adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param); } catch { }

                Log("=== KIỂM TRA BỆNH NHÂN & ĐƠN THUỐC BUỒNG 712, 714, 724, 725 (KHOA 57) ===");
                Log(string.Format("Token: {0}", tokenCode.Substring(0, 16) + "..."));

                // 1. Get Bed Rooms in Dept 57
                HisBedRoomViewFilter bf = new HisBedRoomViewFilter();
                bf.DEPARTMENT_ID = 57;
                var bList = adapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, bf, param);

                string[] roomKws = new string[] { "712", "714", "724", "725" };
                var targetRooms = new List<V_HIS_BED_ROOM>();
                foreach (var kw in roomKws)
                {
                    var rms = bList.Where(x => x.BED_ROOM_NAME != null && x.BED_ROOM_NAME.Contains(kw)).ToList();
                    targetRooms.AddRange(rms);
                    foreach (var r in rms)
                    {
                        Log(string.Format("Buồng: {0} (ID: {1}, RoomID: {2})", r.BED_ROOM_NAME, r.ID, r.ROOM_ID));
                    }
                }

                // 2. Get In-Patients for these rooms
                var patients = new List<V_HIS_TREATMENT_BED_ROOM>();
                foreach (var rm in targetRooms)
                {
                    HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter();
                    tbrf.BED_ROOM_ID = rm.ID;
                    tbrf.IS_IN_ROOM = true;
                    var pts = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", ApiConsumers.MosConsumer, tbrf, param);
                    if (pts != null && pts.Count > 0)
                    {
                        patients.AddRange(pts);
                    }
                }

                Log(string.Format("\nTổng cộng tìm thấy: {0} bệnh nhân đang nằm điều trị.\n", patients.Count));

                // 3. Inspect each patient
                int count = 1;
                foreach (var p in patients.OrderBy(x => x.BED_ROOM_NAME).ThenBy(x => x.BED_NAME))
                {
                    Log("--------------------------------------------------------------------------------------------------");
                    Log(string.Format("{0}. BN: {1} | Buồng: {2} | Giường: {3} | Mã BN: {4} | Mã BA: {5} | TreatmentId: {6}",
                        count++, p.TDL_PATIENT_NAME, p.BED_ROOM_NAME, p.BED_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));

                    // Treatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.ID = p.TREATMENT_ID;
                    var trList = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    var tr = trList != null && trList.Count > 0 ? trList[0] : null;
                    if (tr != null)
                    {
                        Log(string.Format("   Chẩn đoán: {0} [{1}] - {2}", tr.ICD_NAME, tr.ICD_CODE, tr.ICD_TEXT));
                        Log(string.Format("   Vào viện: {0} | Trạng thái: {1} | Đối tượng ID: {2}",
                            tr.IN_TIME, tr.IS_PAUSE == 1 ? "Đã ra viện" : "Đang điều trị", tr.TDL_PATIENT_TYPE_ID));
                    }

                    // Check ExpMest on 20260827 (tomorrow)
                    HisExpMestViewFilter emfTom = new HisExpMestViewFilter();
                    emfTom.TDL_TREATMENT_ID = p.TREATMENT_ID;
                    var emList = adapter.FetchList<V_HIS_EXP_MEST>("api/HisExpMest/GetView", ApiConsumers.MosConsumer, emfTom, param);
                    if (emList != null && emList.Count > 0)
                    {
                        var tomMests = emList.Where(x => (x.TDL_INTRUCTION_TIME.HasValue && x.TDL_INTRUCTION_TIME.Value.ToString().StartsWith("20260827")) ||
                                                         (x.CREATE_TIME.HasValue && x.CREATE_TIME.Value.ToString().StartsWith("20260827"))).ToList();
                        if (tomMests.Count > 0)
                        {
                            Log(string.Format("   ⚠️ ĐÃ CÓ ĐƠN THUỐC NGÀY MAI (27/08/2026): {0} đơn", tomMests.Count));
                            foreach (var tm in tomMests)
                            {
                                Log(string.Format("      - Mã phiếu: {0} | Loại: {1} | Kho: {2} | Thời gian: {3}",
                                    tm.EXP_MEST_CODE, tm.EXP_MEST_TYPE_NAME, tm.MEDI_STOCK_NAME, tm.TDL_INTRUCTION_TIME ?? tm.CREATE_TIME));
                            }
                        }
                        else
                        {
                            Log("   Chưa có đơn thuốc ngày 27/08/2026.");
                        }
                    }

                    // Check Tracking on 20260827
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter();
                    trkFilter.TREATMENT_ID = p.TREATMENT_ID;
                    var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, trkFilter, param);
                    if (trks != null && trks.Count > 0)
                    {
                        var tomTrks = trks.Where(x => x.TRACKING_TIME.ToString().StartsWith("20260827")).ToList();
                        if (tomTrks.Count > 0)
                        {
                            Log(string.Format("   Đã có Tờ điều trị ngày 27/08/2026: TrackingId={0} lúc {1}", tomTrks[0].ID, tomTrks[0].TRACKING_TIME));
                        }
                        else
                        {
                            var latestTrk = trks.OrderByDescending(x => x.TRACKING_TIME).FirstOrDefault();
                            Log(string.Format("   Tờ điều trị mới nhất: TrackingId={0} lúc {1}",
                                latestTrk != null ? latestTrk.ID.ToString() : "N/A", latestTrk != null ? latestTrk.TRACKING_TIME.ToString() : "N/A"));
                        }
                    }

                    // Check ExpMestMedicines (Latest medicines prescribed)
                    HisExpMestMedicineViewFilter medFilter = new HisExpMestMedicineViewFilter();
                    medFilter.TDL_TREATMENT_ID = p.TREATMENT_ID;
                    var allMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", ApiConsumers.MosConsumer, medFilter, param);
                    if (allMeds != null && allMeds.Count > 0)
                    {
                        // Group by date or find latest instruction date
                        var dates = allMeds.Select(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().Substring(0, 8)).Distinct().OrderByDescending(d => d).ToList();
                        Log(string.Format("   Các ngày đã kê thuốc: {0}", string.Join(", ", dates.Take(5))));

                        string latestDate = dates.FirstOrDefault();
                        var latestMeds = allMeds.Where(x => (x.EXP_TIME ?? x.CREATE_TIME ?? 0).ToString().StartsWith(latestDate)).ToList();
                        Log(string.Format("   Đơn thuốc gần nhất ngày [{0}] (Tổng {1} khoản thuốc):", latestDate, latestMeds.Count));

                        foreach (var m in latestMeds.OrderBy(x => x.MEDI_STOCK_ID).ThenBy(x => x.MEDICINE_TYPE_NAME))
                        {
                            Log(string.Format("     • [{0}] (ID {1}) | SL: {2} {3} | Kho: {4} (ID {5}) | Cữ: S:{6} Tr:{7} C:{8} T:{9} | HDSD: {10}",
                                m.MEDICINE_TYPE_NAME, m.MEDICINE_TYPE_ID, m.AMOUNT, m.SERVICE_UNIT_NAME,
                                m.MEDI_STOCK_NAME, m.MEDI_STOCK_ID,
                                m.MORNING, m.NOON, m.AFTERNOON, m.EVENING,
                                m.TUTORIAL));
                        }
                    }
                    else
                    {
                        Log("   ⚠️ Chưa có đơn thuốc nào trong quá khứ.");
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
