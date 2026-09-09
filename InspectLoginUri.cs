using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

class InspectLoginUri
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
        var m = t.GetMethod("Login", new Type[] { typeof(Inventec.Core.CommonParam), typeof(string), typeof(string), typeof(string) });
        
        // Let's print all string tokens in the module
        var module = asm.GetModules()[0];
        var body = m.GetMethodBody();
        var bytes = body.GetILAsByteArray();
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x72) // ldstr opcode
            {
                int stringToken = BitConverter.ToInt32(bytes, i + 1);
                try
                {
                    string str = module.ResolveString(stringToken);
                    Console.WriteLine("ldstr: " + str);
                }
                catch { }
            }
        }
    }
}
