using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

class DisasmGV
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        try
        {
            var asm = Assembly.LoadFrom(@"ReferencedAssemblies\HIS.Desktop.LocalStorage.LocalData.dll");
            var gv = asm.GetType("HIS.Desktop.LocalStorage.LocalData.GlobalVariables");
            var cctor = gv.GetMethod(".cctor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (cctor != null)
            {
                var body = cctor.GetMethodBody();
                Console.WriteLine("Cctor byte length: " + body.GetILAsByteArray().Length);
            }
            foreach (var f in gv.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                Console.WriteLine("Field: " + f.Name + " (" + f.FieldType.Name + ")");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
