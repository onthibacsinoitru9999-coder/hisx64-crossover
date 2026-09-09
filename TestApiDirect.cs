using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

class TestApiDirect
{
    static string ReadLiveToken()
    {
        string[] candidates = new string[] {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LogSystem.txt"),
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };
        foreach (var logPath in candidates)
        {
            if (File.Exists(logPath))
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
        }
        return null;
    }

    static void Main()
    {
        string token = ReadLiveToken();
        Console.WriteLine("Live Token: " + token);

        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri("http://192.168.7.236:1608/");
            client.DefaultRequestHeaders.Add("TokenCode", token);
            client.DefaultRequestHeaders.Add("ClientIpAddress", "100.93.206.93");

            // 1. Get Branches
            Console.WriteLine("\n--- 1. GET api/HisBranch/Get ---");
            try
            {
                var res = client.GetAsync("api/HisBranch/Get").Result;
                Console.WriteLine("Status: " + res.StatusCode);
                var content = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Body length: " + content.Length);
                if (content.Length < 1000) Console.WriteLine("Body: " + content);
            }
            catch (Exception ex) { Console.WriteLine("Ex: " + ex.Message); }

            // 2. Get Departments
            Console.WriteLine("\n--- 2. GET api/HisDepartment/Get ---");
            try
            {
                var res = client.GetAsync("api/HisDepartment/Get").Result;
                Console.WriteLine("Status: " + res.StatusCode);
                var content = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Body length: " + content.Length);
                Console.WriteLine("Body sample: " + (content.Length > 500 ? content.Substring(0, 500) : content));
            }
            catch (Exception ex) { Console.WriteLine("Ex: " + ex.Message); }

            // 3. Search Patient by name 'Cách' in SDA or MOS
            Console.WriteLine("\n--- 3. Search Patient in MOS ---");
            try
            {
                // In MOS, filters in GET requests are often passed as base64 or json query parameters or POST
                // Let's test GET api/HisPatient/Get
                var res = client.GetAsync("api/HisPatient/Get").Result;
                Console.WriteLine("Status: " + res.StatusCode);
                var content = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Body length: " + content.Length);
                Console.WriteLine("Body sample: " + (content.Length > 500 ? content.Substring(0, 500) : content));
            }
            catch (Exception ex) { Console.WriteLine("Ex: " + ex.Message); }
        }
    }
}
