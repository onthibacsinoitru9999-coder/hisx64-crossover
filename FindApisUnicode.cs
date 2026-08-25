using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = Directory.GetFiles(@"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module", "HIS.Desktop.Plugins.*.dll");
        var regex = new Regex(@"api/His[A-Za-z0-9/]+");
        foreach (var f in files)
        {
            byte[] bytes = File.ReadAllBytes(f);
            string str = Encoding.Unicode.GetString(bytes);
            var matches = regex.Matches(str);
            var set = new System.Collections.Generic.HashSet<string>();
            foreach (Match m in matches)
            {
                if (m.Value.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0 && set.Add(m.Value))
                {
                    Console.WriteLine(string.Format("{0} -> {1}", Path.GetFileName(f), m.Value));
                }
            }
        }
    }
}
