using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            var inventecCore = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Core.dll");
            var tokenSystem = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.ClientSystem.dll");
            
            Type commonParamType = null;
            foreach (var t in inventecCore.GetTypes())
            {
                if (t.Name.Contains("CommonParam")) commonParamType = t;
            }
            if (commonParamType != null)
            {
                Console.WriteLine("CommonParam Type: " + commonParamType.FullName);
                foreach (var prop in commonParamType.GetProperties())
                {
                    Console.WriteLine("  Prop: " + prop.Name + " (" + prop.PropertyType.Name + ")");
                }
            }
            else
            {
                Console.WriteLine("Could not find CommonParam in Inventec.Core.dll");
            }
            
            Console.WriteLine("--- Login methods ---");
            var tManager = tokenSystem.GetType("Inventec.Token.ClientSystem.ClientTokenManager");
            if (tManager != null)
            {
                foreach (var m in tManager.GetMethods())
                {
                    if (m.Name.Contains("Login"))
                    {
                        Console.WriteLine("Method: " + m.Name);
                        foreach(var p in m.GetParameters()) Console.WriteLine("  Param: " + p.Name + " (" + p.ParameterType.Name + ")");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
