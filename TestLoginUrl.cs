using System;
using System.IO;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;

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
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        CommonParam param = new CommonParam();
        ClientTokenManager tm = new ClientTokenManager("HIS", "http://192.168.7.200:1401/");
        
        Console.WriteLine("Logging in with 034727...");
        var token = tm.Login(param, "034727", "9981", "2.390.0");
        if (token != null)
        {
            Console.WriteLine("SUCCESS 034727! Token = " + token.TokenCode);
        }
        else
        {
            Console.WriteLine("Failed 034727. HasException: " + param.HasException);
            if (param.Messages != null) foreach (var m in param.Messages) Console.WriteLine("Msg: " + m);
            if (param.BugCodes != null) foreach (var b in param.BugCodes) Console.WriteLine("Bug: " + b);

            Console.WriteLine("\nLogging in with vmc...");
            param = new CommonParam();
            token = tm.Login(param, "vmc", "789789", "2.390.0");
            if (token != null)
            {
                Console.WriteLine("SUCCESS vmc! Token = " + token.TokenCode);
            }
            else
            {
                Console.WriteLine("Failed vmc. HasException: " + param.HasException);
                if (param.Messages != null) foreach (var m in param.Messages) Console.WriteLine("Msg: " + m);
                if (param.BugCodes != null) foreach (var b in param.BugCodes) Console.WriteLine("Bug: " + b);
            }
        }
    }
}
