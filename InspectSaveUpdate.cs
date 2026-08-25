using System;
using System.IO;
using System.Reflection;
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

        string p = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.AssignPrescriptionPK.dll";
        var asm = Assembly.LoadFrom(p);
        var module = asm.GetModules()[0];
        var t = asm.GetType("HIS.Desktop.Plugins.AssignPrescriptionPK.Save.Update.SaveUpdateBehavior");
        
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var mb = m.GetMethodBody();
            if (mb == null) continue;
            byte[] il = mb.GetILAsByteArray();
            for (int i = 0; i < il.Length - 4; i++)
            {
                if (il[i] == 0x72) // ldstr
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    try { 
                        string s = module.ResolveString(token);
                        if (s.Contains("api/") || s.Contains("InPatient"))
                            Console.WriteLine(m.Name + " -> Str: " + s); 
                    } catch {}
                }
            }
        }
    }
}
