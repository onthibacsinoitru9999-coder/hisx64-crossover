using System;
using System.IO;
using System.Reflection;
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

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        var t = asm.GetType("HIS.Desktop.Plugins.AssignPrescriptionPK.Save.SaveAbstract");
        if (t != null)
        {
            var mod = asm.GetModules()[0];
            var m = t.GetMethod("CheckValid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null)
            {
                Console.WriteLine("Method: CheckValid");
                var body = m.GetMethodBody();
                if (body != null)
                {
                    var il = body.GetILAsByteArray();
                    for (int i = 0; i < il.Length - 4; i++)
                    {
                        if (il[i] == 0x72) {
                            try { Console.WriteLine("  str: " + mod.ResolveString(BitConverter.ToInt32(il, i+1))); } catch {}
                        }
                        if (il[i] == 0x28 || il[i] == 0x6F) {
                            try { 
                                var mem = mod.ResolveMember(BitConverter.ToInt32(il, i+1)); 
                                if (mem != null)
                                    Console.WriteLine("  call: " + mem.DeclaringType.Name + "." + mem.Name); 
                            } catch {}
                        }
                    }
                }
            }
        }
    }
}
