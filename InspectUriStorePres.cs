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
                string val = f.GetValue(null) != null ? f.GetValue(null).ToString() : "";
                if (val.IndexOf("Pres", StringComparison.OrdinalIgnoreCase) >= 0 || val.IndexOf("InPatient", StringComparison.OrdinalIgnoreCase) >= 0 || val.IndexOf("OutPatient", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine(string.Format("{0} = {1}", f.Name, val));
                }
            }
        }
    }
}
