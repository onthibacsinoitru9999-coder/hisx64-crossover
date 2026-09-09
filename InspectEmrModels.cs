using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = typeof(MOS.EFMODEL.DataModels.HIS_TREATMENT).Assembly;
        Console.WriteLine("=== EMR / COVER / CLINICAL DATA MODELS IN MOS.EFMODEL ===");
        var types = asm.GetTypes().Where(t => t.Name.Contains("EMR") || t.Name.Contains("COVER") || t.Name.Contains("DOC") || t.Name.Contains("SUMMARY") || t.Name.Contains("DHST") || t.Name.Contains("MEDICINE")).ToList();
        foreach (var t in types.Take(50))
        {
            Console.WriteLine(t.Name);
        }
    }
}
