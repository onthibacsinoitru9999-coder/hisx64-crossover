using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string refDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine(refDir, shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            string p2 = Path.Combine(pluginDir, shortName + ".dll");
            if (File.Exists(p2)) return Assembly.LoadFrom(p2);
            string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
            if (File.Exists(p3)) return Assembly.LoadFrom(p3);
            return null;
        };

        var asm = Assembly.LoadFrom(Path.Combine(pluginDir, "HIS.Desktop.Plugins.AssignPrescriptionPK.dll"));
        var module = asm.GetModules()[0];
        var t = asm.GetType("HIS.Desktop.Plugins.AssignPrescriptionPK.AssignPrescription.frmAssignPrescription");
        
        Action<string> inspectMethod = (methodName) => {
            Console.WriteLine("================ " + methodName + " ================");
            var m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (m == null) return;
            var body = m.GetMethodBody();
            if (body == null) return;
            var il = body.GetILAsByteArray();
            for (int i = 0; i < il.Length - 4; i++)
            {
                // check for ldstr (0x72)
                if (il[i] == 0x72)
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    try
                    {
                        string s = module.ResolveString(token);
                        if (!string.IsNullOrEmpty(s))
                            Console.WriteLine("  String: " + s);
                    }
                    catch {}
                }
                // check for call (0x28) or callvirt (0x6F)
                if (il[i] == 0x28 || il[i] == 0x6F)
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    try
                    {
                        var member = module.ResolveMember(token);
                        if (member != null && (member.Name.Contains("Create") || member.Name.Contains("Get") || member.Name.Contains("Post") || member.Name.Contains("Save") || member.Name.Contains("Prescription") || member.Name.Contains("ExpMest")))
                            Console.WriteLine("  Call: " + member.DeclaringType.Name + "." + member.Name);
                    }
                    catch {}
                }
            }
        };

        inspectMethod("ProcessChoicePrescriptionPrevious");
        inspectMethod("ProcessSaveData");
    }
}
