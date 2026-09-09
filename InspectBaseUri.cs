using System;
using System.IO;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using HIS.Desktop.LocalStorage.ConfigSystem;

class InspectBaseUri
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        Run();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run()
    {
        Load.Init();
        var tm = new ClientTokenManager("HIS");
        var t = tm.GetType();
        var f = t.GetField("baseUriAcs", BindingFlags.NonPublic | BindingFlags.Instance);
        Console.WriteLine("baseUriAcs: " + f.GetValue(tm));

        var mCreateReq = t.GetMethod("CreateRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        Console.WriteLine("CreateRequest: " + (mCreateReq != null));
    }
}
