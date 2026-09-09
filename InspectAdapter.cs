using System;
using System.Reflection;
using Inventec.Common.Adapter;

class Program
{
    static void Main()
    {
        var t = typeof(AdapterBase);
        Console.WriteLine("Methods in AdapterBase for Post:");
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name == "Post")
            {
                var pars = m.GetParameters();
                Console.WriteLine(m.Name + "(" + string.Join(", ", Array.ConvertAll(pars, p => p.ParameterType.Name + " " + p.Name)) + ")");
            }
        }
    }
}