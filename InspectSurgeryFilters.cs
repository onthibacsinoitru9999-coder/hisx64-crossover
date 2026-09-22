using System;
using System.Reflection;
using MOS.Filter;
using MOS.EFMODEL.DataModels;

class Program
{
    static void PrintProps(Type t)
    {
        Console.WriteLine("=== " + t.Name + " ===");
        foreach (var p in t.GetProperties())
        {
            Console.WriteLine("  " + p.PropertyType.Name + " " + p.Name);
        }
    }

    static void Main()
    {
        PrintProps(typeof(HisTreatmentViewFilter));
        PrintProps(typeof(HisServiceReqViewFilter));
        PrintProps(typeof(HisSereServViewFilter));
    }
}
