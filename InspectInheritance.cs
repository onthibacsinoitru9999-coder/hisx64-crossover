using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string path = Path.Combine(refDir, shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            path = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            return null;
        };

        var asm = Assembly.LoadFrom(Path.Combine(refDir, "MOS.SDO.dll"));
        var t = asm.GetType("MOS.SDO.InPatientPresSDO");
        while (t != null && t.FullName != "System.Object")
        {
            Console.WriteLine("Class: " + t.FullName);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.WriteLine("  " + p.PropertyType.Name + " " + p.Name);
            }
            t = t.BaseType;
        }
    }
}
