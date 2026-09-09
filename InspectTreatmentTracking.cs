using System;
using System.IO;
using System.Reflection;

public class InspectTreatmentTracking
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        var asm = Assembly.LoadFrom(@".\Plugins\Module\HIS.Desktop.Plugins.TreatmentTracking.dll");
        Console.WriteLine("Types in TreatmentTracking.dll referencing Ration:");
        foreach (var t in asm.GetTypes())
        {
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (m.Name.ToLower().Contains("ration") || m.Name.ToLower().Contains("suatan"))
                {
                    Console.WriteLine(string.Format("{0}.{1}", t.Name, m.Name));
                }
            }
        }
    }
}
