using System;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
        var asm = Assembly.LoadFrom(Path.Combine(baseDir, "HIS.Desktop.ApiConsumer.dll"));
        var t = asm.GetType("HIS.Desktop.ApiConsumer.HisRequestUriStore");
        if (t != null)
        {
            foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                Console.WriteLine(m.MemberType + ": " + m.Name);
            }
        }
    }
}
