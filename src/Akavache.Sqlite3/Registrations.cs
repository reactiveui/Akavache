// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

using Splat;

namespace Akavache.Sqlite3;

/// <summary>
/// Adds registrations required for the SQLite3 integration.
/// </summary>
[Preserve(AllMembers = true)]
public class Registrations : IWantsToRegisterStuff
{
    /// <summary>
    /// Activates SQLite3 for the application and creates any required storage.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="initSql">A action to initialize SQLite3.</param>
    public static void Start(string applicationName, Action initSql)
    {
        BlobCache.ApplicationName = applicationName;
        initSql?.Invoke();
    }

    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        // NB: We want the most recently registered fs, since there really
        // only should be one
        var fs = Locator.Current.GetService<IFilesystemProvider>();
        if (fs is null)
        {
            throw new InvalidOperationException("Failed to initialize Akavache properly. Do you have a reference to Akavache.dll?");
        }

        var localCache = new Lazy<IBlobCache>(() =>
        {
            var directory = fs.GetDefaultLocalMachineCacheDirectory();

            if (directory is null || string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("There is a invalid directory being returned by the file system provider.");
            }

            fs.CreateRecursive(directory).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SqlRawPersistentBlobCache(Path.Combine(directory, "blobs.db"), BlobCache.TaskpoolScheduler);
        });
        resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");

        var userAccount = new Lazy<IBlobCache>(() =>
        {
            var directory = fs.GetDefaultRoamingCacheDirectory();

            if (directory is null || string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("There is a invalid directory being returned by the file system provider.");
            }

            fs.CreateRecursive(directory).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SqlRawPersistentBlobCache(Path.Combine(directory, "userblobs.db"), BlobCache.TaskpoolScheduler);
        });
        resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");

        var secure = new Lazy<ISecureBlobCache>(() =>
        {
            var directory = fs.GetDefaultSecretCacheDirectory();

            if (directory is null || string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("There is a invalid directory being returned by the file system provider.");
            }

            fs.CreateRecursive(directory).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SQLiteEncryptedBlobCache(Path.Combine(directory, "secret.db"), Locator.Current.GetService<IEncryptionProvider>(), BlobCache.TaskpoolScheduler);
        });
        resolver.Register(() => secure.Value, typeof(ISecureBlobCache));
    }
}
