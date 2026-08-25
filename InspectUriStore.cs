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
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var f in fields.Where(x => x.Name.Contains("ExpMest") || x.Name.Contains("Pres") || x.Name.Contains("ServiceReq") || x.Name.Contains("Medicine") || x.Name.Contains("Copy") || x.Name.Contains("Instruction")))
            {
                Console.WriteLine(string.Format("{0} = \"{1}\"", f.Name, f.GetValue(null)));
            }
        }
    }
}
