using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class InspectMealRationDetail
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
        var asm = Assembly.LoadFrom(@".\Plugins\Module\HIS.Desktop.Plugins.MealRationDetail.dll");
        var t = asm.GetType("HIS.Desktop.Plugins.MealRationDetail.MealRationDetail.frmMealRationDetail");
        Console.WriteLine("Type: " + t.FullName);
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (f.Name.StartsWith("cbo") || f.Name.StartsWith("grid") || f.Name.StartsWith("txt") || f.Name.StartsWith("dte") || f.Name.Contains("Room") || f.Name.Contains("Department") || f.Name.Contains("Data"))
            {
                Console.WriteLine(string.Format("  Field: {0} ({1})", f.Name, f.FieldType.Name));
            }
        }
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (m.Name.Contains("Load") || m.Name.Contains("Init") || m.Name.Contains("Search") || m.Name.Contains("Fill") || m.Name.Contains("Set"))
            {
                Console.WriteLine("  Method: " + m.Name);
            }
        }
    }
}
