using System;
using System.IO;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Token.ClientSystem;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string tokenCode = null;
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            string p = Path.Combine(cur.FullName, "Logs", "LogSystem.txt");
            if (File.Exists(p))
            {
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buf = new byte[Math.Min(131072L, fs.Length)];
                    fs.Seek(fs.Length - buf.Length, SeekOrigin.Begin);
                    fs.Read(buf, 0, buf.Length);
                    string str = Encoding.UTF8.GetString(buf);
                    int idx = str.LastIndexOf("TokenCode|");
                    if (idx >= 0) { tokenCode = str.Substring(idx + 10, 64); break; }
                }
            }
            cur = cur.Parent;
        }

        Console.WriteLine("Token: " + (tokenCode != null ? tokenCode.Substring(0, 10) + "..." : "NULL"));

        if (string.IsNullOrEmpty(tokenCode))
        {
            Load.Init();
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            CommonParam param = new CommonParam();
            var tok = tokenManager.Login(param, "034727", "9981", "2.390.0");
            if (tok != null) tokenCode = tok.TokenCode;
            Console.WriteLine("Token from Login: " + (tokenCode != null ? tokenCode.Substring(0, 10) + "..." : "NULL"));
        }
    }
}
