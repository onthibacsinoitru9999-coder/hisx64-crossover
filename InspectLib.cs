using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            var tokenSystem = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.ClientSystem.dll");
            var tokenCore = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.Core.dll");
            
            Console.WriteLine("---- ClientSystem Types ----");
            foreach (var type in tokenSystem.GetTypes().Where(t => t.Name.Contains("Login") || t.Name.Contains("Token") || t.Name.Contains("Acs") || t.Name.Contains("Acs")))
            {
                Console.WriteLine(type.FullName);
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name.Contains("Login")))
                {
                    Console.WriteLine("  Method: " + method.Name);
                    foreach (var p in method.GetParameters())
                    {
                        Console.WriteLine(string.Format("    Param: {0} ({1})", p.Name, p.ParameterType.Name));
                    }
                }
            }
            
            Console.WriteLine("---- Core Types ----");
            foreach (var type in tokenCore.GetTypes().Where(t => t.Name.Contains("Login") || t.Name.Contains("Token") || t.Name.Contains("SDO") || t.Name.Contains("Sdo")))
            {
                Console.WriteLine(type.FullName);
                foreach (var prop in type.GetProperties())
                {
                    Console.WriteLine("  Prop: " + prop.Name);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
