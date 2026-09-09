using System;
using System.IO;
using System.Reflection;

class InspectLoginIL
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        var asm = Assembly.LoadFrom(@"ReferencedAssemblies\Inventec.Token.ClientSystem.dll");
        var t = asm.GetType("Inventec.Token.ClientSystem.ClientTokenManager");
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            if (m.Name == "Login")
            {
                Console.WriteLine("Method: " + m.Name + " (" + string.Join(", ", System.Linq.Enumerable.Select(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");
            }
        }
    }
}
