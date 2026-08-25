using System;
using System.Reflection;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            Type commonParamType = null;
            string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
            foreach (var file in Directory.GetFiles(refDir, "*.dll"))
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name.Contains("CommonParam"))
                        {
                            commonParamType = t;
                            Console.WriteLine("Found CommonParam in " + file);
                            break;
                        }
                    }
                    if (commonParamType != null) break;
                }
                catch {}
            }
            if (commonParamType != null)
            {
                Console.WriteLine("CommonParam Type: " + commonParamType.FullName);
                foreach (var prop in commonParamType.GetProperties())
                {
                    Console.WriteLine("  Prop: " + prop.Name + " (" + prop.PropertyType.Name + ")");
                }
            }
            else
            {
                Console.WriteLine("Could not find CommonParam");
            }
            
            // Check for param types in WebApiClient or anywhere
            foreach (var file in Directory.GetFiles(refDir, "Inventec.Common*.dll"))
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name.Contains("Login") && t.Name.Contains("Req"))
                        {
                            Console.WriteLine("Found Request type: " + t.FullName);
                            foreach(var p in t.GetProperties()) Console.WriteLine("  Prop: " + p.Name);
                        }
                    }
                }
                catch {}
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
