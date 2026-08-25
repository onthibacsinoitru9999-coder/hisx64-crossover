using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string path = Path.Combine(refDir, shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            path = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            path = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            return null;
        };

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        var t = asm.GetType("HIS.Desktop.Plugins.AssignPrescriptionPK.AssignPrescription.frmAssignPrescription");
        if (t != null)
        {
            var m = t.GetMethod("ProcessChoicePrescriptionPrevious", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null)
            {
                var body = m.GetMethodBody();
                Console.WriteLine("ProcessChoicePrescriptionPrevious IL length: " + (body != null ? body.GetILAsByteArray().Length.ToString() : "null"));
            }
        }

        // Check ApiConsumer calls or backend URIs
        var apiTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => {
            try { return a.GetTypes(); } catch { return new Type[0]; }
        }).Where(tp => tp.Name.Contains("HisExpMest") || tp.Name.Contains("HisServiceReq"));
        
        foreach (var tp in apiTypes)
        {
            Console.WriteLine("API Type: " + tp.FullName);
        }
    }
}
