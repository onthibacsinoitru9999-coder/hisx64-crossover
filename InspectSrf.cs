using System;
using System.IO;
using System.Reflection;
using MOS.Filter;

public class InspectSrf
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
        Console.WriteLine("Properties of HisServiceReqViewFilter:");
        foreach (var p in typeof(HisServiceReqViewFilter).GetProperties())
        {
            if (p.Name.Contains("TREATMENT") || p.Name.Contains("ROOM") || p.Name.Contains("TIME") || p.Name.Contains("TYPE"))
            {
                Console.WriteLine(string.Format("  {0} : {1}", p.Name, p.PropertyType.Name));
            }
        }
    }
}
