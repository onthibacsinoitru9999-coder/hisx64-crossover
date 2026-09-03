using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace HisAutomation
{
    public class SubmitEmergencySurgeryForm
    {
        public const string FORM_URL = "https://docs.google.com/forms/d/e/1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/formResponse";

        public static bool Submit(
            string location,
            string dept,
            string patientName,
            string age,
            string gender,
            string patientCode,
            string treatmentCode,
            string roomBed,
            string diagnosis,
            string surgeryMethod,
            string emergencyType,
            string doctorName,
            string mainSurgeon,
            string assistantSurgeon,
            string note)
        {
            var postData = new Dictionary<string, string>
            {
                { "entry.1771260210", location ?? "" },
                { "entry.1225676642", dept ?? "" },
                { "entry.1876536986", patientName ?? "" },
                { "entry.2076581334", age ?? "" },
                { "entry.688804617",  gender ?? "" },
                { "entry.1034142534", patientCode ?? "" },
                { "entry.1407489499", treatmentCode ?? "" },
                { "entry.1681626113", roomBed ?? "" },
                { "entry.1982187638", diagnosis ?? "" },
                { "entry.1121377833", surgeryMethod ?? "" },
                { "entry.777442342",  emergencyType ?? "" },
                { "entry.716253062",  doctorName ?? "" },
                { "entry.1993991632", mainSurgeon ?? "" },
                { "entry.1769648167", assistantSurgeon ?? "" },
                { "entry.1348503941", note ?? "" }
            };

            var sb = new StringBuilder();
            foreach (var kv in postData)
            {
                if (sb.Length > 0) sb.Append("&");
                sb.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value));
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FORM_URL);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return response.StatusCode == HttpStatusCode.OK;
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("🚑 TOOL GỬI CHỈ ĐỊNH MỔ CẤP CỨU — KHOA CTCH & CS HÀ NỘI");
            Console.WriteLine("===============================================================================");
            // Ready for automated or CLI calls
        }
    }
}
