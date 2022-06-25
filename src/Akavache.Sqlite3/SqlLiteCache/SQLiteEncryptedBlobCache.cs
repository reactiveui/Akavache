// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace Akavache.Sqlite3;

/// <summary>
/// A SQLite blob cache where all the entries are encrypted.
/// </summary>
public class SQLiteEncryptedBlobCache : SqlRawPersistentBlobCache, ISecureBlobCache
{
    private readonly IEncryptionProvider _encryption;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteEncryptedBlobCache"/> class.
    /// </summary>
    /// <param name="databaseFile">The location of the database.</param>
    /// <param name="encryptionProvider">The provider which encrypts and decrypts values.</param>
    /// <param name="scheduler">A scheduler to perform the operations on.</param>
    /// <exception cref="Exception">If there is no encryption provider available.</exception>
    public SQLiteEncryptedBlobCache(string databaseFile, IEncryptionProvider? encryptionProvider = null, IScheduler? scheduler = null)
        : base(databaseFile, scheduler) =>
        _encryption = encryptionProvider ?? Locator.Current.GetService<IEncryptionProvider>() ?? throw new InvalidOperationException("No IEncryptionProvider available. This should never happen, your DependencyResolver is broken");

    /// <inheritdoc />
    protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length == 0)
        {
            return Observable.Return(data);
        }

        return _encryption.EncryptBlock(data);
    }

    /// <inheritdoc />
    protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length == 0)
        {
            return Observable.Return(data);
        }

        return _encryption.DecryptBlock(data);
    }
}
