using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }
}

class Program
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string name = new AssemblyName(a.Name).Name + ".dll";
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", name);
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            return null;
        };
        RealMain();
    }

    static void RealMain()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string token = File.ReadAllText("doctor_standalone.token", Encoding.UTF8).Split('|')[0];
        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam param = new CommonParam();

        string[] patientNames = new string[]
        {
            "PHẠM THỊ LIÊN",
            "NGÔ VĂN XƯNG",
            "NGUYỄN THỊ ÚT",
            "BÙI THỊ RỊU",
            "ĐOÀN THỊ LỰU",
            "TRẦN THÁI DƯƠNG"
        };

        foreach (var name in patientNames)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(">>> BỆNH NHÂN: " + name);

            var tf = new HisTreatmentViewFilter
            {
                KEY_WORD = name
            };
            var list = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mos, tf, param);
            if (list == null || list.Count == 0)
            {
                Console.WriteLine(">>> KHÔNG TÌM THẤY HỒ SƠ VỚI TÊN: " + name);
                continue;
            }

            var activeTreatments = list.OrderByDescending(x => x.IN_TIME).ToList();
            var tr = activeTreatments.FirstOrDefault(x => x.IS_PAUSE != 1) ?? activeTreatments.First();

            long tid = tr.ID;
            int age = DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4));

            var tbrList = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mos, new HisTreatmentBedRoomViewFilter { TREATMENT_ID = tid, IS_IN_ROOM = true }, param);
            var tbr = (tbrList != null && tbrList.Count > 0) ? tbrList[0] : null;

            Console.WriteLine(string.Format("HỌ TÊN: {0} | TUỔI: {1} | GIỚI: {2} | MÃ BN: {3} | MÃ ĐT: {4}",
                tr.TDL_PATIENT_NAME, age, tr.TDL_PATIENT_GENDER_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE));
            Console.WriteLine(string.Format("Khoa điều trị: {0} | Buồng: {1} | Giường: {2} | Vào viện: {3}",
                tr.END_DEPARTMENT_NAME,
                tbr != null ? tbr.BED_ROOM_NAME : "Chưa xếp",
                tbr != null ? tbr.BED_NAME : "Chưa xếp",
                tr.IN_TIME));
            Console.WriteLine(string.Format("Chẩn đoán vào/hiện tại: [{0}] {1} (Chi tiết: {2})", tr.ICD_CODE, tr.ICD_NAME, tr.ICD_TEXT));

            // 1. TỜ ĐIỀU TRỊ (Bệnh sử, Khám lâm sàng, Diễn biến)
            var trks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, new HisTrackingViewFilter { TREATMENT_ID = tid, ORDER_FIELD = "TRACKING_TIME", ORDER_DIRECTION = "ASC" }, param);
            if (trks != null && trks.Count > 0)
            {
                Console.WriteLine("\n--- DIỄN BIẾN LÂM SÀNG, BỆNH SỬ & KHÁM (TỜ ĐIỀU TRỊ) ---");
                var first = trks.First();
                Console.WriteLine(string.Format("[Tờ đầu {0} - BS {1}]:", first.TRACKING_TIME, first.CREATOR));
                Console.WriteLine("Diễn biến: " + (first.CONTENT != null ? first.CONTENT.Trim() : ""));
                Console.WriteLine("Y lệnh: " + (first.CARE_INSTRUCTION != null ? first.CARE_INSTRUCTION.Trim() : ""));

                if (trks.Count > 1)
                {
                    var last = trks.Last();
                    Console.WriteLine(string.Format("\n[Tờ mới nhất {0} - BS {1}]:", last.TRACKING_TIME, last.CREATOR));
                    Console.WriteLine("Diễn biến: " + (last.CONTENT != null ? last.CONTENT.Trim() : ""));
                    Console.WriteLine("Y lệnh: " + (last.CARE_INSTRUCTION != null ? last.CARE_INSTRUCTION.Trim() : ""));
                }
            }

            // 2. DHST
            var dhsts = adapter.FetchList<V_HIS_DHST>("api/HisDhst/GetView", mos, new HisDhstViewFilter { TREATMENT_ID = tid, ORDER_FIELD = "EXECUTE_TIME", ORDER_DIRECTION = "DESC" }, param);
            if (dhsts != null && dhsts.Count > 0)
            {
                var d = dhsts.First();
                Console.WriteLine(string.Format("\n--- DẤU HIỆU SINH TỒN ({0}) ---", d.EXECUTE_TIME));
                Console.WriteLine(string.Format("Mạch: {0} ck/ph | HA: {1}/{2} mmHg | Nhiệt độ: {3} °C | SpO2: {4}% | Cân nặng: {5} kg",
                    d.PULSE, d.BLOOD_PRESSURE_MAX, d.BLOOD_PRESSURE_MIN, d.TEMPERATURE, d.SPO2, d.WEIGHT));
            }

            // 3. CẬN LÂM SÀNG & HỘI CHẨN & PTTT
            var allReqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, new HisServiceReqViewFilter { TREATMENT_ID = tid }, param);
            if (allReqs != null)
            {
                // CĐHA & TDCN (MRI, CT, X-quang, DEXA, Siêu âm, Điện tim)
                Console.WriteLine("\n--- CẬN LÂM SÀNG (CĐHA, TDCN) ---");
                var cdhaReqs = allReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 2 || x.SERVICE_REQ_TYPE_ID == 3 || x.SERVICE_REQ_TYPE_ID == 5).OrderByDescending(x => x.INTRUCTION_TIME).ToList();
                int cdhaCount = 0;
                foreach (var c in cdhaReqs)
                {
                    var sss = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = c.ID }, param);
                    if (sss == null) continue;
                    foreach (var s in sss)
                    {
                        var sses = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = s.ID }, param);
                        if (sses != null && sses.Count > 0)
                        {
                            foreach (var se in sses)
                            {
                                string desc = se.DESCRIPTION;
                                string concl = se.CONCLUDE;
                                if (!string.IsNullOrEmpty(desc) || !string.IsNullOrEmpty(concl))
                                {
                                    cdhaCount++;
                                    Console.WriteLine(string.Format("* [{0}] {1} (Y lệnh: {2})", c.INTRUCTION_TIME, s.TDL_SERVICE_NAME, c.SERVICE_REQ_CODE));
                                    if (!string.IsNullOrEmpty(desc)) Console.WriteLine("  Mô tả: " + desc.Replace("\r\n", " ").Replace("\n", " "));
                                    if (!string.IsNullOrEmpty(concl)) Console.WriteLine("  KẾT LUẬN: " + concl.Replace("\r\n", " ").Replace("\n", " "));
                                }
                            }
                        }
                    }
                }
                if (cdhaCount == 0) Console.WriteLine("Chưa có kết quả CĐHA có mô tả chi tiết.");

                // Hội chẩn chuyên khoa
                var hcReqs = allReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 9 || (x.SERVICE_REQ_TYPE_NAME != null && x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("hội chẩn"))).ToList();
                if (hcReqs.Count > 0)
                {
                    Console.WriteLine("\n--- PHIẾU HỘI CHẨN CHUYÊN KHOA ---");
                    foreach (var h in hcReqs)
                    {
                        var sssH = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = h.ID }, param);
                        if (sssH != null)
                        {
                            foreach (var sh in sssH)
                            {
                                var ssesH = adapter.FetchList<HIS_SERE_SERV_EXT>("api/HisSereServExt/Get", mos, new HisSereServExtFilter { SERE_SERV_ID = sh.ID }, param);
                                if (ssesH != null)
                                {
                                    foreach (var seh in ssesH)
                                    {
                                        Console.WriteLine(string.Format("* [{0}] {1} (Mã: {2})", h.INTRUCTION_TIME, sh.TDL_SERVICE_NAME, h.SERVICE_REQ_CODE));
                                        if (!string.IsNullOrEmpty(seh.DESCRIPTION)) Console.WriteLine("  Ý kiến: " + seh.DESCRIPTION.Replace("\r\n", " | ").Replace("\n", " | "));
                                        if (!string.IsNullOrEmpty(seh.CONCLUDE)) Console.WriteLine("  Kết luận: " + seh.CONCLUDE);
                                    }
                                }
                            }
                        }
                    }
                }

                // Y lệnh Phẫu thuật
                var ptttReqs = allReqs.Where(x => x.SERVICE_REQ_TYPE_ID == 4 || (x.SERVICE_REQ_TYPE_NAME != null && x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("phẫu"))).ToList();
                if (ptttReqs.Count > 0)
                {
                    Console.WriteLine("\n--- CHỈ ĐỊNH PHẪU THUẬT / THỦ THUẬT ---");
                    foreach (var p in ptttReqs)
                    {
                        Console.WriteLine(string.Format("* Mã YL: {0} | Loại: {1} | Thời gian: {2} | Trạng thái: {3}",
                            p.SERVICE_REQ_CODE, p.SERVICE_REQ_TYPE_NAME, p.INTRUCTION_TIME, p.SERVICE_REQ_STT_NAME));
                        var sssP = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, new HisSereServViewFilter { SERVICE_REQ_ID = p.ID }, param);
                        if (sssP != null)
                        {
                            foreach (var sp in sssP)
                            {
                                Console.WriteLine(string.Format("   -> Dịch vụ: [{0}] {1} (SL: {2})", sp.TDL_SERVICE_CODE, sp.TDL_SERVICE_NAME, sp.AMOUNT));
                            }
                        }
                    }
                }
            }
        }
    }
}
