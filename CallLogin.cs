using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var inventecCore = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Core.dll");
            var tokenSystem = Assembly.LoadFrom(@"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies\Inventec.Token.ClientSystem.dll");
            
            Type commonParamType = inventecCore.GetType("Inventec.Core.CommonParam");
            var commonParamInst = Activator.CreateInstance(commonParamType);
            
            var tManager = tokenSystem.GetType("Inventec.Token.ClientSystem.ClientTokenManager");
            if (tManager != null)
            {
                var instance = Activator.CreateInstance(tManager, new object[] { "HIS", "http://127.0.0.1:8080/" });
                var methods = tManager.GetMethods();
                foreach (var m in methods)
                {
                    if (m.Name == "Login" && m.GetParameters().Length == 3)
                    {
                        Console.WriteLine("Invoking Login...");
                        var result = m.Invoke(instance, new object[] { commonParamInst, "vmc", "789789" });
                        Console.WriteLine("Done. Result is null? " + (result == null));
                        if (result != null)
                        {
                            foreach (var p in result.GetType().GetProperties())
                            {
                                Console.WriteLine("  " + p.Name + " = " + p.GetValue(result, null));
                            }
                        }
                        
                        var exceptionProp = commonParamType.GetProperty("HasException");
                        if (exceptionProp != null) Console.WriteLine("HasException: " + exceptionProp.GetValue(commonParamInst, null));
                        var messagesProp = commonParamType.GetProperty("Messages");
                        if (messagesProp != null) {
                            var msgs = messagesProp.GetValue(commonParamInst, null);
                            if (msgs != null) Console.WriteLine("Messages: " + msgs);
                        }
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
            if (ex.InnerException != null) Console.WriteLine("InnerError: " + ex.InnerException);
        }
    }
}
