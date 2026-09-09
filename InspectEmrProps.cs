using System;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integrate", "EMR", new AssemblyName(a.Name).Name + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            return null;
        };
        Run();
    }

    static void Run()
    {
        Console.WriteLine("=== EMR_DOCUMENT ===");
        foreach (var p in typeof(EMR.EFMODEL.DataModels.EMR_DOCUMENT).GetProperties()) Console.Write(p.Name + ", ");
        Console.WriteLine("\n\n=== EMR_DOCUMENT_TYPE ===");
        foreach (var p in typeof(EMR.EFMODEL.DataModels.EMR_DOCUMENT_TYPE).GetProperties()) Console.Write(p.Name + ", ");
        Console.WriteLine("\n\n=== EMR_COVER ===");
        var coverType = typeof(EMR.EFMODEL.DataModels.EMR_DOCUMENT).Assembly.GetType("EMR.EFMODEL.DataModels.EMR_COVER");
        if (coverType != null)
        {
            foreach (var p in coverType.GetProperties()) Console.Write(p.Name + ", ");
        }
        else Console.WriteLine("Not found EMR_COVER");
    }
}
