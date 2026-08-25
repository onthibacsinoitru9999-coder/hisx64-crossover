using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        string[] dlls = Directory.GetFiles(@"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module", "*.dll");
        foreach (var dll in dlls)
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                var types = asm.GetTypes();
                foreach (var t in types)
                {
                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        if (m.Name.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0 && 
                           (m.Name.IndexOf("Pres", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("ExpMest", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("ServiceReq", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            Console.WriteLine(string.Format("{0} -> {1}.{2}", Path.GetFileName(dll), t.Name, m.Name));
                        }
                    }
                }
            }
            catch {}
        }
    }
}
