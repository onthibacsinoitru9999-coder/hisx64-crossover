using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var tokenSystem = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.ClientSystem.dll");
            var tManager = tokenSystem.GetType("Inventec.Token.ClientSystem.ClientTokenManager");
            
            Console.WriteLine("Constructors of ClientTokenManager:");
            foreach (var ctor in tManager.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var args = string.Join(", ", Array.ConvertAll(ctor.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
                Console.WriteLine("  ctor(" + args + ")");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
