// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3;

/// <summary>
/// <para>
/// The main purpose of this class is to ensure older packages upgrade without breaking.
/// Existing installs of Akavache use a linker class referencing typeof(Akavache.Sqlite3.SQLitePersistentBlobCache)
/// This ensures that static analysis won't link these DLLs out.
/// </para>
/// <para>
/// This library was added to provide a default bundle implementation using the bundle_e_sqlite3 bundle.
/// Thus this class was moved here so it provides the hook for the linker and then registers and inits the sqlraw bundle.
/// </para>
/// </summary>
public class SQLitePersistentBlobCache : SqlRawPersistentBlobCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLitePersistentBlobCache"/> class.
    /// </summary>
    /// <param name="databaseFile">The location of the database file which to store the blobs in.</param>
    /// <param name="scheduler">Scheduler to use for contained observables.</param>
    public SQLitePersistentBlobCache(string databaseFile, IScheduler? scheduler = null)
        : base(databaseFile, scheduler)
    {
    }
}
