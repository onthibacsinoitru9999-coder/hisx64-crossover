using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var webClientAsm = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Common.WebApiClient.dll");
            foreach (var type in webClientAsm.GetTypes())
            {
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (m.Name.Contains("Get") || m.Name.Contains("Post") || m.Name.Contains("Send"))
                    {
                        var args = string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
                        Console.WriteLine(type.Name + "." + m.Name + " (" + args + ")");
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
