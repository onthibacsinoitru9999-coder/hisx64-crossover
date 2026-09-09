using System;
using System.IO;
using System.Reflection;

public class InspectMealRationDetailLogic
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
        var mod = t.Module;

        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var mb = m.GetMethodBody();
            if (mb == null) continue;
            var bytes = mb.GetILAsByteArray();
            for (int i = 0; i < bytes.Length - 4; i++)
            {
                try
                {
                    int token = BitConverter.ToInt32(bytes, i);
                    string str = mod.ResolveString(token);
                    if (!string.IsNullOrEmpty(str) && (str.Contains("api/") || str.Contains("HIS_") || str.Contains("SERVICE_REQ") || str.Contains("RATION")))
                    {
                        Console.WriteLine(string.Format("{0} -> \"{1}\"", m.Name, str));
                    }
                }
                catch { }
            }
        }
    }
}
