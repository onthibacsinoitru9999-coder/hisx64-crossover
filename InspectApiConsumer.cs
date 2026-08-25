using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        
        // Find all strings used in methods
        foreach (var t in asm.GetTypes())
        {
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var body = m.GetMethodBody();
                if (body != null)
                {
                    var il = body.GetILAsByteArray();
                    // Let's inspect tokens in IL or type members
                }
            }
        }

        // Check ApiConsumer references
        var apiConsumerAsm = Assembly.LoadFrom(Path.Combine(baseDir, "HIS.Desktop.ApiConsumer.dll"));
        Console.WriteLine("=== ApiConsumer Classes & Methods ===");
        foreach (var t in apiConsumerAsm.GetTypes().Where(x => x.Name.Contains("ExpMest") || x.Name.Contains("ServiceReq") || x.Name.Contains("Medicine")))
        {
            Console.WriteLine("ApiConsumer: " + t.FullName);
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var prms = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                Console.WriteLine(string.Format("  - {0} {1}({2})", m.ReturnType.Name, m.Name, prms));
            }
        }
    }
}
