using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== EmrDocumentFilter properties ===");
        foreach (var p in typeof(EMR.Filter.EmrDocumentFilter).GetProperties()) Console.Write(p.Name + ", ");
    }
}
