using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

public class InspectProgram
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            string path3 = Path.Combine(folderPath, "Plugins", "Module", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        var asm = Assembly.LoadFrom(@".\Plugins\Module\HIS.Desktop.Plugins.RationSchedule.dll");
        var t = asm.GetType("HIS.Desktop.Plugins.RationSchedule.RationSchedule.frmRationSchedule");
        var m = t.GetMethod("SaveProcess", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var mb = m.GetMethodBody();
        var bytes = mb.GetILAsByteArray();
        var mod = m.Module;

        Console.WriteLine("Tokens referenced in SaveProcess:");
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            try
            {
                int token = BitConverter.ToInt32(bytes, i);
                MemberInfo member = mod.ResolveMember(token);
                if (member != null)
                {
                    Console.WriteLine(string.Format("0x{0:X4}: {1} ({2})", i, member.Name, member.DeclaringType != null ? member.DeclaringType.Name : ""));
                }
            }
            catch { }
            try
            {
                int strToken = BitConverter.ToInt32(bytes, i);
                string s = mod.ResolveString(strToken);
                if (!string.IsNullOrEmpty(s))
                {
                    Console.WriteLine(string.Format("STR 0x{0:X4}: \"{1}\"", i, s));
                }
            }
            catch { }
        }
    }
}
