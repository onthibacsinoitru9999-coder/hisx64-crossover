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

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string shortName = args.Name.Split(',')[0];
            string path1 = Path.Combine(baseDir, shortName + ".dll");
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            string path3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        try
        {
            Console.WriteLine("=== 1. MOS.SDO Types for ExpMest & Prescription ===");
            var mosSdo = Assembly.LoadFrom(Path.Combine(baseDir, "MOS.SDO.dll"));
            foreach (var type in mosSdo.GetTypes())
            {
                if (type.Name.Contains("Pres") || type.Name.Contains("ExpMest") || type.Name.Contains("Copy"))
                {
                    Console.WriteLine("SDO: " + type.FullName);
                    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        Console.WriteLine(string.Format("    {0} {1}", prop.PropertyType.Name, prop.Name));
                    }
                }
            }

            Console.WriteLine("\n=== 2. MOS.MANAGER HisExpMest & HisServiceReq APIs ===");
            var mosMgr = Assembly.LoadFrom(Path.Combine(baseDir, "MOS.MANAGER.dll"));
            foreach (var type in mosMgr.GetTypes())
            {
                if (type.Name.Contains("ExpMest") || type.Name.Contains("Prescription") || type.Name.Contains("Medicine") || type.Name.Contains("Copy"))
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (methods.Length > 0 && (type.Name.EndsWith("Manager") || type.Name.EndsWith("Get") || type.Name.EndsWith("Create") || type.Name.EndsWith("View")))
                    {
                        Console.WriteLine("Manager Type: " + type.FullName);
                        foreach (var m in methods)
                        {
                            var prms = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                            Console.WriteLine(string.Format("  - {0} {1}({2})", m.ReturnType.Name, m.Name, prms));
                        }
                    }
                }
            }

            Console.WriteLine("\n=== 3. AssignPrescriptionPK Inspection ===");
            string assignPk = Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll");
            if (File.Exists(assignPk))
            {
                var asmPk = Assembly.LoadFrom(assignPk);
                foreach (var type in asmPk.GetTypes())
                {
                    if (type.Name.Contains("Copy") || type.Name.Contains("Process") || type.Name.Contains("Prescription"))
                    {
                        Console.WriteLine("AssignPK Type: " + type.FullName);
                        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            if (m.Name.Contains("Copy") || m.Name.Contains("Create") || m.Name.Contains("Pres") || m.Name.Contains("Save"))
                            {
                                var prms = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                                Console.WriteLine(string.Format("    Method: {0} {1}({2})", m.ReturnType.Name, m.Name, prms));
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
