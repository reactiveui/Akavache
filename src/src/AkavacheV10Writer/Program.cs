using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Akavache.Sqlite3;

/*
 V10 writer app
 - Initializes Akavache v10
 - Writes deterministic data to a known sqlite file path so v11 app can read it
*/

// Deterministic test data
var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "akavache-test.db"));
Console.WriteLine($"V10 Writer starting. DB path: {dbPath}");

// Ensure parent directory exists
var dbDir = Path.GetDirectoryName(dbPath)!;
Directory.CreateDirectory(dbDir);

// V10 initialization
BlobCache.ApplicationName = "AkavacheCompatTest";
Akavache.Sqlite3.Registrations.Start("AkavacheCompatTest", () => { });

// Create a raw persistent cache pointing at our explicit path
using var cache = new SqlRawPersistentBlobCache(dbPath);

// Keys
const string keyString = "compat:string";
const string keyInt = "compat:int";
const string keyPerson = "compat:person";
const string keyBytes = "compat:bytes";

// Values (deterministic)
var valueString = "Hello, Akavache V10!";
var valueInt = 42;
var valuePerson = new Person { Name = "Ada Lovelace", Age = 36, Email = "ada@example.com" };
var valueBytes = Encoding.UTF8.GetBytes("ByteArray:CAFEBABE");

try
{
    // Insert string
    cache.InsertObject(keyString, valueString).Wait();
    Console.WriteLine($"Inserted: key='{keyString}', type=string, value='{valueString}'");

    // Insert int
    cache.InsertObject(keyInt, valueInt).Wait();
    Console.WriteLine($"Inserted: key='{keyInt}', type=int, value={valueInt}");

    // Insert complex object
    cache.InsertObject(keyPerson, valuePerson).Wait();
    Console.WriteLine($"Inserted: key='{keyPerson}', type=Person, value={{Name={valuePerson.Name},Age={valuePerson.Age},Email={valuePerson.Email}}}");

    // Insert raw bytes
    cache.Insert(keyBytes, valueBytes).Wait();
    Console.WriteLine($"Inserted: key='{keyBytes}', type=byte[], value='{BitConverter.ToString(valueBytes)}'");

    // Force flush
    cache.Flush().Wait();
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR during inserts: {ex}");
}
finally
{
    try
    {
        BlobCache.Shutdown().Wait();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Shutdown error: {ex}");
    }
}

Console.WriteLine("V10 Writer completed.");

public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}
