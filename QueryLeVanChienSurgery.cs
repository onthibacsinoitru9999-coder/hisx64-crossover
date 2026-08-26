using System;
using System.Data;
using System.IO;
using System.Text;
using Inventec.Common.Repository;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.MANAGER.HisTreatment;
using MOS.MANAGER.HisSereServ;
using MOS.MANAGER.HisSereServExt;
using MOS.MANAGER.HisEkip;
using MOS.MANAGER.HisEkipUser;
using MOS.MANAGER.HisSereServPttt;

class Program
{
    static void Main()
    {
        try
        {
            long treatmentId = 7060265; // Lê Văn Chiến
            CommonParam param = new CommonParam();

            Console.WriteLine("=== QUERYING SURGERY RECORDS FOR LE VAN CHIEN (TreatmentId: " + treatmentId + ") ===");

            // 1. Get Treatment details
            HisTreatmentFilterQuery tFilter = new HisTreatmentFilterQuery();
            tFilter.ID = treatmentId;
            var treatments = new HisTreatmentManager(param).Get(tFilter);
            if (treatments != null && treatments.Count > 0)
            {
                var t = treatments[0];
                Console.WriteLine("Treatment Code: " + t.TREATMENT_CODE + " | Patient: " + t.TDL_PATIENT_NAME + " | InTime: " + t.IN_TIME + " | OutTime: " + t.OUT_TIME);
                Console.WriteLine("Clinical In Time: " + t.CLINICAL_IN_TIME + " | End Code: " + t.TREATMENT_END_TYPE_ID);
                Console.WriteLine("ICD: " + t.ICD_CODE + " - " + t.ICD_NAME);
            }

            // 2. Get SereServ with PTTT
            HisSereServFilterQuery ssFilter = new HisSereServFilterQuery();
            ssFilter.TREATMENT_ID = treatmentId;
            var sereServs = new HisSereServManager(param).Get(ssFilter);
            Console.WriteLine("Total SereServs: " + (sereServs != null ? sereServs.Count : 0));

            if (sereServs != null)
            {
                foreach (var ss in sereServs)
                {
                    if (ss.TDL_SERVICE_TYPE_ID == 4 || ss.TDL_SERVICE_TYPE_ID == 3 || ss.EKIP_ID.HasValue || ss.SERVICE_REQ_ID.HasValue)
                    {
                        // Check if surgery/procedure
                        if (ss.TDL_SERVICE_TYPE_ID == 4 || ss.TDL_SERVICE_TYPE_ID == 3 || (ss.TDL_SERVICE_NAME != null && (ss.TDL_SERVICE_NAME.ToLower().Contains("phẫu thuật") || ss.TDL_SERVICE_NAME.ToLower().Contains("mổ") || ss.TDL_SERVICE_NAME.ToLower().Contains("nẹp"))))
                        {
                            Console.WriteLine("\n--- SERVICE: " + ss.TDL_SERVICE_NAME + " (ID: " + ss.ID + ", Type: " + ss.TDL_SERVICE_TYPE_ID + ") ---");
                            Console.WriteLine("Req Service ID: " + ss.SERVICE_REQ_ID + " | EkipId: " + ss.EKIP_ID + " | Amount: " + ss.AMOUNT + " | Price: " + ss.VIR_PRICE);
                            Console.WriteLine("Req Doctor: " + ss.TDL_REQUEST_LOGINNAME + " (" + ss.TDL_REQUEST_USERNAME + ") | Execute Room: " + ss.TDL_EXECUTE_ROOM_ID);
                            Console.WriteLine("Req Time: " + ss.TDL_INTRUCTION_TIME);

                            // Query SereServExt
                            try
                            {
                                HisSereServExtFilterQuery extFilter = new HisSereServExtFilterQuery();
                                extFilter.SERE_SERV_ID = ss.ID;
                                var exts = new HisSereServExtManager(param).Get(extFilter);
                                if (exts != null && exts.Count > 0)
                                {
                                    foreach (var ext in exts)
                                    {
                                        Console.WriteLine("  [SereServExt] BeginTime: " + ext.BEGIN_TIME + " | EndTime: " + ext.END_TIME + " | Description: " + ext.DESCRIPTION);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("  Ext err: " + ex.Message);
                            }

                            // Query SereServPttt
                            try
                            {
                                HisSereServPtttFilterQuery ptttFilter = new HisSereServPtttFilterQuery();
                                ptttFilter.SERE_SERV_ID = ss.ID;
                                var pttts = new HisSereServPtttManager(param).Get(ptttFilter);
                                if (pttts != null && pttts.Count > 0)
                                {
                                    foreach (var pttt in pttts)
                                    {
                                        Console.WriteLine("  [SereServPttt] PTTT_GROUP: " + pttt.PTTT_GROUP_ID + " | PTTT_METHOD: " + pttt.PTTT_METHOD_ID + " | EMOTION: " + pttt.EMOTION_LESS_METHOD_ID);
                                        Console.WriteLine("    BEFORE_DIAGNOSTIC: " + pttt.BEFORE_PTTT_ICD_NAME + " | AFTER_DIAGNOSTIC: " + pttt.AFTER_PTTT_ICD_NAME);
                                        Console.WriteLine("    MANNER: " + pttt.MANNER + " | REAL_PTTT_METHOD: " + pttt.REAL_PTTT_METHOD);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("  Pttt err: " + ex.Message);
                            }

                            // Query Ekip users
                            if (ss.EKIP_ID.HasValue)
                            {
                                try
                                {
                                    HisEkipUserFilterQuery euFilter = new HisEkipUserFilterQuery();
                                    euFilter.EKIP_ID = ss.EKIP_ID.Value;
                                    var ekipUsers = new HisEkipUserManager(param).Get(euFilter);
                                    if (ekipUsers != null && ekipUsers.Count > 0)
                                    {
                                        Console.WriteLine("  [EKIP USERS - KÍP MỔ]:");
                                        foreach (var eu in ekipUsers)
                                        {
                                            Console.WriteLine("    Role ID: " + eu.EXECUTE_ROLE_ID + " | Login: " + eu.LOGINNAME + " | Name: " + eu.USERNAME + " | Department: " + eu.DEPARTMENT_ID);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("  [EKIP USERS]: None found for EkipId " + ss.EKIP_ID.Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("  Ekip err: " + ex.Message);
                                }
                            }
                        }
                    }
                }
            }

            // Also check all Tracking sheets for any text about surgery
            Console.WriteLine("\n=== TRACKING SHEETS (TỜ ĐIỀU TRỊ) ===");
            var trackingManager = new MOS.MANAGER.HisTracking.HisTrackingManager(param);
            var trFilter = new MOS.MANAGER.HisTracking.HisTrackingFilterQuery();
            trFilter.TREATMENT_ID = treatmentId;
            var trackings = trackingManager.Get(trFilter);
            if (trackings != null)
            {
                foreach (var tr in trackings)
                {
                    Console.WriteLine("[" + tr.TRACKING_TIME + " - BS: " + tr.CREATOR + "] " + tr.TRACKING_CONTENT);
                    if (!string.IsNullOrEmpty(tr.TREATMENT_INSTRUCTION))
                    {
                        Console.WriteLine("  Y LỆNH: " + tr.TREATMENT_INSTRUCTION);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FATAL ERROR: " + ex.ToString());
        }
    }
}
