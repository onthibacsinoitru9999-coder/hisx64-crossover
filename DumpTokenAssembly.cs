using System;
using System.IO;
using System.Reflection;
using System.Text;

class DumpTokenAssembly
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
        foreach (var type in asm.GetTypes())
        {
            Console.WriteLine("Type: " + type.FullName);
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Console.WriteLine("  Method: " + m.ToString());
            }
        }
    }
}
