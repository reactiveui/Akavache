// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
var dbDir = Path.GetDirectoryName(dbPath);
_ = Directory.CreateDirectory(dbDir);

// V10 initialization
BlobCache.ApplicationName = "AkavacheCompatTest";
Akavache.Sqlite3.Registrations.Start("AkavacheCompatTest", static () => { });

// Create a raw persistent cache pointing at our explicit path
using SqlRawPersistentBlobCache cache = new(dbPath);

// Keys
const string keyString = "compat:string";
const string keyInt = "compat:int";
const string keyPerson = "compat:person";
const string keyBytes = "compat:bytes";

// Values (deterministic)
const string valueString = "Hello, Akavache V10!";
const int valueInt = 42;
Person valuePerson = new() { Name = "Ada Lovelace", Age = 36, Email = "ada@example.com" };
var valueBytes = "ByteArray:CAFEBABE"u8.ToArray();

try
{
    // Insert string
    _ = cache.InsertObject(keyString, valueString).Wait();
    Console.WriteLine($"Inserted: key='{keyString}', type=string, value='{valueString}'");

    // Insert int
    _ = cache.InsertObject(keyInt, valueInt).Wait();
    Console.WriteLine($"Inserted: key='{keyInt}', type=int, value={valueInt}");

    // Insert complex object
    _ = cache.InsertObject(keyPerson, valuePerson).Wait();
    Console.WriteLine($"Inserted: key='{keyPerson}', type=Person, value={{Name={valuePerson.Name},Age={valuePerson.Age},Email={valuePerson.Email}}}");

    // Insert raw bytes
    _ = cache.Insert(keyBytes, valueBytes).Wait();
    Console.WriteLine($"Inserted: key='{keyBytes}', type=byte[], value='{BitConverter.ToString(valueBytes)}'");

    // Force flush
    _ = cache.Flush().Wait();
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

/// <summary>
/// Represents a person for testing serialization.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{Name}")]
public class Person
{
    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the person's age.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the person's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
