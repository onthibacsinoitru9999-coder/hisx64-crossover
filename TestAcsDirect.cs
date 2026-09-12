using System;
using System.IO;
using System.Net.Http;
using System.Text;

class TestAcsDirect
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri("http://192.168.7.200:1401/");
            client.DefaultRequestHeaders.Add("ClientIpAddress", "100.93.206.93");

            string json = "{\"LoginName\":\"034727\",\"Password\":\"998199\",\"ApplicationCode\":\"HIS\"}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine("POST api/Token/Login with 034727...");
            try
            {
                var res = client.PostAsync("api/Token/Login", content).Result;
                Console.WriteLine("Status: " + res.StatusCode);
                var respStr = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response: " + respStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ex: " + ex.Message);
            }

            string json2 = "{\"LoginName\":\"vmc\",\"Password\":\"789789\",\"ApplicationCode\":\"HIS\"}";
            var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

            Console.WriteLine("\nPOST api/Token/Login with vmc...");
            try
            {
                var res = client.PostAsync("api/Token/Login", content2).Result;
                Console.WriteLine("Status: " + res.StatusCode);
                var respStr = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response: " + respStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ex: " + ex.Message);
            }
        }
    }
}
