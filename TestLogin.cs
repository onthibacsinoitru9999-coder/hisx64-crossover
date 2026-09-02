using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Reflection;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Token.ClientSystem;
using HIS.Desktop.LocalStorage.ConfigSystem;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Load.Init();

        // Set static fields directly if needed
        try
        {
            var constType = typeof(ClientTokenManager).Assembly.GetType("Inventec.Token.ClientSystem.Constants");
            var fBase = constType.GetField("BASE_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fBase != null) fBase.SetValue(null, "http://192.168.7.200:1401/");
            var fLogin = constType.GetField("LOGIN_URI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fLogin != null) fLogin.SetValue(null, "api/Token/Login");
            Console.WriteLine("Set Constants.BASE_URI = " + fBase.GetValue(null));
        }
        catch (Exception ex) { Console.WriteLine("Const set ex: " + ex.Message); }

        CommonParam param = new CommonParam();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");

        Console.WriteLine("1. Trying login as vmc...");
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        if (token != null)
        {
            Console.WriteLine("Login vmc SUCCESS! Token: " + token.TokenCode);
        }
        else
        {
            Console.WriteLine("Login vmc failed. Messages: " + (param.Messages != null ? string.Join("; ", param.Messages) : "none") + " | BugCodes: " + (param.BugCodes != null ? string.Join("; ", param.BugCodes) : "none"));

            Console.WriteLine("2. Trying login as 034727...");
            param = new CommonParam();
            token = tokenManager.Login(param, "034727", "9981", "2.390.0");
            if (token != null)
            {
                Console.WriteLine("Login 034727 SUCCESS! Token: " + token.TokenCode);
            }
            else
            {
                Console.WriteLine("Login 034727 failed. Messages: " + (param.Messages != null ? string.Join("; ", param.Messages) : "none") + " | BugCodes: " + (param.BugCodes != null ? string.Join("; ", param.BugCodes) : "none"));
            }
        }

        if (token != null)
        {
            var mosConsumer = new Inventec.Common.WebApiClient.ApiConsumer("http://192.168.7.236:1608/", token.TokenCode, "HIS");
            var myAdapter = new MyAdapter();

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
            myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, param);

            HisTreatmentBedRoomLViewFilter tbrf = new HisTreatmentBedRoomLViewFilter { BED_ROOM_ID = 775, IS_IN_ROOM = true };
            var inP = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetLView", mosConsumer, tbrf, param);

            Console.WriteLine("Patients count in 712: " + (inP != null ? inP.Count : 0));
            if (inP != null)
            {
                foreach (var p in inP)
                {
                    Console.WriteLine("\n===============================================================================");
                    Console.WriteLine(string.Format("🧑 [{0}] {1} (Mã BN: {2}, Mã ĐT: {3}, ID: {4})", p.BED_NAME, p.TDL_PATIENT_NAME, p.TDL_PATIENT_CODE, p.TREATMENT_CODE, p.TREATMENT_ID));

                    // 1. Treatment
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter { ID = p.TREATMENT_ID };
                    var trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", mosConsumer, tf, param);
                    if (trs != null && trs.Count > 0)
                    {
                        var tr = trs[0];
                        Console.WriteLine("  ICD: " + tr.ICD_CODE + " - " + tr.ICD_NAME + " | Sub: " + tr.ICD_TEXT);
                        Console.WriteLine("  InTime: " + tr.IN_TIME + " | OutTime: " + tr.OUT_TIME);
                        Console.WriteLine("  HospitalizeReason: " + tr.HOSPITALIZE_REASON_NAME);
                        Console.WriteLine("  TreatmentResult: " + tr.TREATMENT_RESULT_NAME);
                    }

                    // 2. All Trackings
                    HisTrackingViewFilter trkFilter = new HisTrackingViewFilter { TREATMENT_ID = p.TREATMENT_ID };
                    var trks = myAdapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mosConsumer, trkFilter, param);
                    if (trks != null)
                    {
                        Console.WriteLine(string.Format("  --- TRACKINGS ({0} entries) ---", trks.Count));
                        foreach (var t in trks.OrderBy(x => x.TRACKING_TIME))
                        {
                            Console.WriteLine(string.Format("    [{0}] {1}", t.TRACKING_TIME, t.CONTENT != null ? t.CONTENT.Replace("\r\n", " ").Replace("\n", " ") : ""));
                        }
                    }

                    // 3. Service Reqs (Surgeries / Procedures / Consultations)
                    HisServiceReqViewFilter srf = new HisServiceReqViewFilter { TREATMENT_ID = p.TREATMENT_ID };
                    var srs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mosConsumer, srf, param);
                    if (srs != null)
                    {
                        var ptSrs = srs.Where(x => x.SERVICE_REQ_TYPE_ID == 4 || x.SERVICE_REQ_TYPE_ID == 1 || x.SERVICE_REQ_TYPE_ID == 11 || (x.SERVICE_REQ_TYPE_NAME != null && (x.SERVICE_REQ_TYPE_NAME.Contains("Phẫu thuật") || x.SERVICE_REQ_TYPE_NAME.Contains("Thủ thuật") || x.SERVICE_REQ_TYPE_NAME.Contains("Hội chẩn")))).ToList();
                        Console.WriteLine(string.Format("  --- SURGERIES / PROCEDURES / CONSULTATIONS ({0} entries) ---", ptSrs.Count));
                        foreach (var s in ptSrs)
                        {
                            Console.WriteLine(string.Format("    [{0}] {1} (Type: {2}, STT: {3})", s.INTRUCTION_TIME, s.SERVICE_REQ_TYPE_NAME, s.SERVICE_REQ_TYPE_ID, s.SERVICE_REQ_STT_NAME));
                        }
                    }
                }
            }
        }
    }
}

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
