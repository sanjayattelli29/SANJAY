using System;
using System.Linq;
using System.Reflection;

public class Program
{
    public static void Main()
    {
        try
        {
            var assembly = Assembly.Load("Imagekit");
            var types = assembly.GetTypes().Select(t => t.FullName).OrderBy(t => t);
            foreach (var type in types)
            {
                Console.WriteLine(type);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
