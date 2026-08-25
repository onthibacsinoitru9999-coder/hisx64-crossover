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
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var f in fields)
            {
                string name = f.Name;
                if (name.Contains("PRES") || name.Contains("EXP_MEST") || name.Contains("IN_PATIENT") || name.Contains("OUT_PATIENT") || name.Contains("MEDICINE") || name.Contains("COPY"))
                {
                    Console.WriteLine(string.Format("{0} = {1}", name, f.GetValue(null)));
                }
            }
        }
    }
}
