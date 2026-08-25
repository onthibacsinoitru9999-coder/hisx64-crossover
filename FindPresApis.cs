using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string[] dirs = new string[] {
            @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module",
            @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies"
        };
        var regex = new Regex(@"api/His[A-Za-z0-9/]*Pres[A-Za-z0-9/]*");
        var set = new HashSet<string>();
        foreach (var d in dirs)
        {
            foreach (var f in Directory.GetFiles(d, "*.dll"))
            {
                byte[] bytes = File.ReadAllBytes(f);
                string str = Encoding.Unicode.GetString(bytes);
                var matches = regex.Matches(str);
                foreach (Match m in matches)
                {
                    if (set.Add(m.Value))
                    {
                        Console.WriteLine(m.Value);
                    }
                }
            }
        }
    }
}
