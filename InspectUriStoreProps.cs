using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        var asm = Assembly.LoadFrom(Path.Combine(baseDir, "HIS.Desktop.ApiConsumer.dll"));
        var t = asm.GetType("HIS.Desktop.ApiConsumer.HisRequestUriStore");
        if (t != null)
        {
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (prop.Name.Contains("ExpMest") || prop.Name.Contains("Pres") || prop.Name.Contains("ServiceReq") || prop.Name.Contains("Medicine") || prop.Name.Contains("Copy") || prop.Name.Contains("Instruction"))
                {
                    try {
                        Console.WriteLine(string.Format("{0} => \"{1}\"", prop.Name, prop.GetValue(null, null)));
                    } catch {}
                }
            }
        }
    }
}
