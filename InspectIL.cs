using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine(refDir, shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        var t = asm.GetType("HIS.Desktop.Plugins.AssignPrescriptionPK.AssignPrescription.frmAssignPrescription");
        if (t != null)
        {
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                if (m.Name == "ProcessSaveData" || m.Name == "ProcessChoicePrescriptionPrevious" || m.Name.Contains("SaveInPatient") || m.Name.Contains("SaveOutPatient"))
                {
                    Console.WriteLine("Method: " + m.Name);
                    var body = m.GetMethodBody();
                    if (body != null)
                    {
                        var il = body.GetILAsByteArray();
                        Console.WriteLine("  IL Bytes: " + il.Length);
                    }
                }
            }
        }
    }
}
