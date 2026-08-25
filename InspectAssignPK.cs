using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string path = Path.Combine(refDir, shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            path = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            path = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            return null;
        };

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        foreach (var t in asm.GetTypes())
        {
            if (t.Name.Contains("Copy") || t.Name.Contains("Prescription") || t.Name.Contains("InPatient") || t.Name.Contains("Processor"))
            {
                Console.WriteLine("Type: " + t.FullName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (m.Name.Contains("Copy") || m.Name.Contains("Save") || m.Name.Contains("Create") || m.Name.Contains("Process") || m.Name.Contains("GetPres"))
                    {
                        Console.WriteLine("  Method: " + m.Name);
                    }
                }
            }
        }
    }
}
