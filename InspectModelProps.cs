using System;
using System.Reflection;
using MOS.EFMODEL.DataModels;

class Program
{
    static void Main()
    {
        PrintProps(typeof(HIS_TRACKING));
        PrintProps(typeof(V_HIS_TRACKING));
        PrintProps(typeof(V_HIS_SERE_SERV));
        PrintProps(typeof(V_HIS_EXP_MEST_MEDICINE));
    }

    static void PrintProps(Type t)
    {
        Console.WriteLine("\n=== TYPE: " + t.FullName + " ===");
        foreach (var p in t.GetProperties())
        {
            Console.Write(p.Name + " (" + p.PropertyType.Name + "), ");
        }
        Console.WriteLine();
    }
}
