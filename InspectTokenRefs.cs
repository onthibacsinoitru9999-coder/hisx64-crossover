using System;
using System.IO;
using System.Reflection;

class InspectTokenRefs
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
        foreach (var r in asm.GetReferencedAssemblies())
        {
            Console.WriteLine("Ref: " + r.FullName);
        }

        var t = asm.GetType("Inventec.Token.ClientSystem.ClientTokenManager");
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var f in fields)
        {
            Console.WriteLine("Field: " + f.Name + " (" + f.FieldType.FullName + ")");
        }
    }
}
