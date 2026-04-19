// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

/*
 V11 reader app
 - Initializes v11 builder API
 - Points to same database location as v10 output by creating a direct SqliteBlobCache
 - Reads deterministic keys and validates values
*/

var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "akavache-test.db"));
Console.WriteLine($"V11 Reader starting. DB path: {dbPath}");

var allPass = true;

// Initialize Akavache v11 (Sqlite defaults just for serializer bootstrapping)
var instance = CacheDatabase.CreateBuilder("AkavacheCompatTest")
    .WithSerializer<SystemJsonSerializer>()
    .WithSqliteProvider()
    .WithSqliteDefaults()
    .Build();

// Create a direct SQLite cache instance pointing at the exact db path used by v10
using SqliteBlobCache readerCache = new(dbPath, instance.Serializer!);

// Keys
const string keyString = "compat:string";
const string keyInt = "compat:int";
const string keyPerson = "compat:person";
const string keyBytes = "compat:bytes";

// Expected values
var expectedString = "Hello, Akavache V10!";
var expectedInt = 42;
Person expectedPerson = new() { Name = "Ada Lovelace", Age = 36, Email = "ada@example.com" };
var expectedBytes = "ByteArray:CAFEBABE"u8.ToArray();

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
    readerCache.Dispose();
    await CacheDatabase.Shutdown();
}

Console.WriteLine(allPass ? "\n? Compatibility Verified" : "\n? Mismatch Found");
Environment.ExitCode = allPass ? 0 : 1;

/// <summary>
/// Represents a person for testing deserialization.
/// </summary>
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

