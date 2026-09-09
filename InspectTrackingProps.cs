using System;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== HIS_TRACKING ===");
        foreach (var p in typeof(HIS_TRACKING).GetProperties()) Console.WriteLine(p.Name + " (" + p.PropertyType.Name + ")");
        Console.WriteLine("\n=== V_HIS_TRACKING ===");
        foreach (var p in typeof(V_HIS_TRACKING).GetProperties()) Console.WriteLine(p.Name + " (" + p.PropertyType.Name + ")");
    }
}
