using System;
using System.IO;
using System.Reflection;

class InspectILDetails
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
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            Console.WriteLine("Method: " + m.Name);
            var body = m.GetMethodBody();
            if (body != null)
            {
                // Print local variables
                foreach (var lv in body.LocalVariables)
                {
                    Console.WriteLine("   Local: " + lv.LocalType.FullName);
                }
            }
        }
    }
}
