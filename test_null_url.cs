using System;
using Akavache;
using Akavache.SystemTextJson;

// Simple test to see what error we get with null URL
class Program
{
    static void Main()
    {
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        
        try
        {
            // Test string URL null
            cache.LoadImageBytesFromUrl((string)null!);
            Console.WriteLine("No exception thrown for null string URL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception for null string URL: {ex.GetType().Name}: {ex.Message}");
        }
        
        try
        {
            // Test Uri URL null
            cache.LoadImageBytesFromUrl((Uri)null!);
            Console.WriteLine("No exception thrown for null Uri URL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception for null Uri URL: {ex.GetType().Name}: {ex.Message}");
        }
        
        try
        {
            // Test with key and null string URL
            cache.LoadImageBytesFromUrl("key", (string)null!);
            Console.WriteLine("No exception thrown for null string URL with key");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception for null string URL with key: {ex.GetType().Name}: {ex.Message}");
        }
        
        try
        {
            // Test with key and null Uri URL
            cache.LoadImageBytesFromUrl("key", (Uri)null!);
            Console.WriteLine("No exception thrown for null Uri URL with key");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception for null Uri URL with key: {ex.GetType().Name}: {ex.Message}");
        }
    }
}