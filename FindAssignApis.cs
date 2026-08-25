using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string[] files = new string[] {
            @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.AssignPrescriptionPK.dll",
            @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.PrescriptionGeneral.dll",
            @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module\HIS.Desktop.Plugins.TreatmentManagement.dll"
        };
        var regex = new Regex(@"api/[A-Za-z0-9/]+");
        foreach (var f in files)
        {
            if (!File.Exists(f)) continue;
            byte[] bytes = File.ReadAllBytes(f);
            string str = Encoding.Unicode.GetString(bytes);
            var matches = regex.Matches(str);
            var set = new HashSet<string>();
            Console.WriteLine("=== File: " + Path.GetFileName(f) + " ===");
            foreach (Match m in matches)
            {
                if (set.Add(m.Value))
                {
                    Console.WriteLine("  " + m.Value);
                }
            }
        }
    }
}
