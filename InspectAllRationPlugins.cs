using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class InspectAllRationPlugins
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

        Run();
    }

    static void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Module");
        var files = Directory.GetFiles(dir, "*Ration*.dll");
        foreach (var file in files)
        {
            try
            {
                var asm = Assembly.LoadFrom(file);
                Console.WriteLine("================================================================================");
                Console.WriteLine("Plugin: " + Path.GetFileName(file));
                foreach (var t in asm.GetTypes())
                {
                    if (t.Name.StartsWith("frm") || t.Name.Contains("Control") || t.Name.Contains("View"))
                    {
                        Console.WriteLine("  Form/View: " + t.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading " + Path.GetFileName(file) + ": " + ex.Message);
            }
        }
    }
}
