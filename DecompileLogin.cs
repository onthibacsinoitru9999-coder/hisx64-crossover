using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

class DecompileLogin
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
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name == "Login" || m.Name == "CreateRequest")
            {
                Console.WriteLine("\n=== " + m.Name + " ===");
                var body = m.GetMethodBody();
                if (body != null)
                {
                    var il = body.GetILAsByteArray();
                    Console.WriteLine("IL length: " + il.Length);
                }
            }
        }

        // Let's also check all string literals / fields in the class
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            Console.WriteLine("Field: " + f.Name + " (" + f.FieldType.Name + ")");
        }
    }
}
