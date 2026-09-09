using System;
using System.IO;
using System.Reflection;

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

        var asm = Assembly.Load("MOS.SDO");
        var t = asm.GetType("MOS.SDO.PresMedicineSDO");
        Console.WriteLine("=== PresMedicineSDO Properties ===");
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Console.WriteLine(p.Name + " (" + p.PropertyType.Name + ")");
        }
    }
}