using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

/*
 V11 reader app
 - Initializes v11 builder API
 - Points to same database location as v10 output by creating a direct SqliteBlobCache
 - Reads deterministic keys and validates values
*/

var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "akavache-test.db"));
Console.WriteLine($"V11 Reader starting. DB path: {dbPath}");

bool allPass = true;

// Initialize Akavache v11 (Sqlite defaults just for serializer bootstrapping)
var instance = CacheDatabase.CreateBuilder()
    //.WithSerializer<NewtonsoftBsonSerializer>()
    .WithSerializer<SystemJsonSerializer>()
    .WithApplicationName("AkavacheCompatTest")
    .WithSqliteDefaults()
    .Build();

// Create a direct SQLite cache instance pointing at the exact db path used by v10
using var readerCache = new SqliteBlobCache(dbPath, instance.Serializer!);

// Debug: Inspect tables and basic counts
try
{
    var tables = await readerCache.Connection.QueryAsync<NameRow>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
    Console.WriteLine("SQLite tables present:");
    foreach (var t in tables)
    {
        try
        {
            var count = await readerCache.Connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM \"{t.name}\"");
            Console.WriteLine($" • {t.name} (rows={count})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" • {t.name} (count error: {ex.Message})");
        }
    }

    var hasCacheElement = tables.Any(x => string.Equals(x.name, "CacheElement", StringComparison.OrdinalIgnoreCase));
    if (hasCacheElement)
    {
        Console.WriteLine("PRAGMA table_info('CacheElement'):");
        try
        {
            var cols = await readerCache.Connection.QueryAsync<PragmaRow>("PRAGMA table_info('CacheElement')");
            foreach (var c in cols)
            {
                Console.WriteLine($" • {c.name} (type={c.type})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting PRAGMA table_info: {ex.Message}");
        }

        try
        {
            var sample = await readerCache.Connection.QueryAsync<SampleRow>("SELECT Key, TypeName, length(Value) as LenValue, length(Data) as LenData, ExpiresAt FROM CacheElement ORDER BY Key");
            foreach (var s in sample)
            {
                Console.WriteLine($"Row: Key={s.Key}, TypeName={s.TypeName}, Len(Value)={s.LenValue}, Len(Data)={s.LenData}, ExpiresAt={s.ExpiresAt}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading sample rows from CacheElement: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"DB introspection error: {ex}");
}

// Keys
const string keyString = "compat:string";
const string keyInt = "compat:int";
const string keyPerson = "compat:person";
const string keyBytes = "compat:bytes";

// Expected values
var expectedString = "Hello, Akavache V10!";
var expectedInt = 42;
var expectedPerson = new Person { Name = "Ada Lovelace", Age = 36, Email = "ada@example.com" };
var expectedBytes = Encoding.UTF8.GetBytes("ByteArray:CAFEBABE");

try
{
    // Read string
    try
    {
        var s = await readerCache.GetObject<string>(keyString);
        var pass = s == expectedString;
        Console.WriteLine($"VERIFY key='{keyString}' type=string => {(pass ? "PASS" : $"FAIL (got '{s}')")}");
        allPass &= pass;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"VERIFY key='{keyString}' EXCEPTION: {ex.Message}");
        allPass = false;
    }

    // Read int
    try
    {
        var i = await readerCache.GetObject<int>(keyInt);
        var pass = i == expectedInt;
        Console.WriteLine($"VERIFY key='{keyInt}' type=int => {(pass ? "PASS" : $"FAIL (got {i})")}");
        allPass &= pass;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"VERIFY key='{keyInt}' EXCEPTION: {ex.Message}");
        allPass = false;
    }

    // Read person
    try
    {
        var p = await readerCache.GetObject<Person>(keyPerson);
        var pass = p != null && p.Name == expectedPerson.Name && p.Age == expectedPerson.Age && p.Email == expectedPerson.Email;
        Console.WriteLine($"VERIFY key='{keyPerson}' type=Person => {(pass ? "PASS" : $"FAIL (got Name={p?.Name},Age={p?.Age},Email={p?.Email})")}");
        allPass &= pass;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"VERIFY key='{keyPerson}' EXCEPTION: {ex.Message}");
        allPass = false;
    }

    // Read raw bytes
    try
    {
        var bytes = await readerCache.Get(keyBytes);
        var pass = bytes != null && bytes.SequenceEqual(expectedBytes);
        Console.WriteLine($"VERIFY key='{keyBytes}' type=byte[] => {(pass ? "PASS" : $"FAIL (len={bytes?.Length})")}");
        allPass &= pass;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"VERIFY key='{keyBytes}' EXCEPTION: {ex.Message}");
        allPass = false;
    }
}
finally
{
    await readerCache.DisposeAsync();
    await CacheDatabase.Shutdown();
}

Console.WriteLine(allPass ? "\n? Compatibility Verified" : "\n? Mismatch Found");
Environment.ExitCode = allPass ? 0 : 1;

public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class NameRow
{
    public string name { get; set; } = string.Empty;
}

public class PragmaRow
{
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
}

public class SampleRow
{
    public string Key { get; set; } = string.Empty;
    public string? TypeName { get; set; }
    public long? LenValue { get; set; }
    public long? LenData { get; set; }
    public string? ExpiresAt { get; set; }
}
