using System;
using System.IO;
using System.Reflection;

class InspectTokenManager
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
        foreach (var t in asm.GetTypes())
        {
            if (t.Name.Contains("Token") || t.Name.Contains("Login") || t.Name.Contains("Acs"))
            {
                Console.WriteLine("Type: " + t.FullName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (m.DeclaringType == t)
                        Console.WriteLine("  Method: " + m.Name);
                }
            }
        }
    }
}
