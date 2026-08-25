using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string f = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.TreatmentManagement.dll";
        if (!File.Exists(f)) return;
        byte[] bytes = File.ReadAllBytes(f);
        string str = Encoding.Unicode.GetString(bytes);
        var regex = new Regex(@"api/[A-Za-z0-9/]+");
        var matches = regex.Matches(str);
        var set = new HashSet<string>();
        foreach (Match m in matches)
        {
            if (set.Add(m.Value) && (m.Value.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0 || m.Value.IndexOf("Pres", StringComparison.OrdinalIgnoreCase) >= 0 || m.Value.IndexOf("ExpMest", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Console.WriteLine("  " + m.Value);
            }
        }
    }
}
