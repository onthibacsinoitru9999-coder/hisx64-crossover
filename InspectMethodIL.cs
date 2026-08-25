using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine(baseDir, shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        string p = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.ServiceReqList.dll";
        var asm = Assembly.LoadFrom(p);
        var t = asm.GetType("HIS.Desktop.Plugins.ServiceReqList.frmServiceReqList");
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var mb = m.GetMethodBody();
                if (mb != null)
                {
                    byte[] il = mb.GetILAsByteArray();
                    // print metadata tokens / strings
                    Console.WriteLine("Method: " + m.Name + " IL bytes: " + il.Length);
                }
            }
        }
    }
}
