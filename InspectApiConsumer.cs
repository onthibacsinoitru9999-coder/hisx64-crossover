using System;
using System.IO;
using System.Reflection;

class InspectApiConsumer
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        var asm = Assembly.LoadFrom(@"ReferencedAssemblies\Inventec.Common.WebApiClient.dll");
        var t = asm.GetType("Inventec.Common.WebApiClient.ApiConsumer");
        Console.WriteLine("Type: " + t.FullName);
        foreach (var c in t.GetConstructors())
        {
            var pars = string.Join(", ", System.Linq.Enumerable.Select(c.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
            Console.WriteLine("Ctor: (" + pars + ")");
        }
        foreach (var p in t.GetProperties())
        {
            Console.WriteLine("Property: " + p.Name + " (" + p.PropertyType.Name + ")");
        }
    }
}
