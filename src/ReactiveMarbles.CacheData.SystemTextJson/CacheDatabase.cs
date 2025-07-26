// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.CacheDatabase.Core;

/// <summary>
/// A class which represents the global cache database instances.
/// </summary>
public static class CacheDatabase
{
    private static string? _applicationName;
    private static IBlobCache? _localMachine;
    private static IBlobCache? _userAccount;
    private static ISecureBlobCache? _secure;
    private static bool _shutdownRequested;

    static CacheDatabase() => InMemory = new SystemTextJson.InMemoryBlobCache(CoreRegistrations.TaskpoolScheduler);

    /// <summary>
    /// Gets or sets your application's name. Set this at startup, this defines where
    /// your data will be stored.
    /// </summary>
    public static string ApplicationName
    {
        get => _applicationName ?? throw new InvalidOperationException("Make sure to set CacheDatabase.ApplicationName on startup");
        set => _applicationName = value;
    }

    /// <summary>
    /// Gets or sets the local machine cache. Store data here that is unrelated to the
    /// user account or shouldn't be uploaded to other machines (i.e. image cache data).
    /// </summary>
    public static IBlobCache LocalMachine
    {
        get => _localMachine ?? (_shutdownRequested ? throw new ObjectDisposedException("CacheDatabase") : throw new InvalidOperationException("LocalMachine cache not initialized"));
        set => _localMachine = value;
    }

    /// <summary>
    /// Gets or sets the user account cache. Store data here that is associated with
    /// the user account and may be backed up to the cloud.
    /// </summary>
    public static IBlobCache UserAccount
    {
        get => _userAccount ?? (_shutdownRequested ? throw new ObjectDisposedException("CacheDatabase") : throw new InvalidOperationException("UserAccount cache not initialized"));
        set => _userAccount = value;
    }

    /// <summary>
    /// Gets or sets the secure cache. Store sensitive data here that should be encrypted.
    /// </summary>
    public static ISecureBlobCache Secure
    {
        get => _secure ?? (_shutdownRequested ? throw new ObjectDisposedException("CacheDatabase") : throw new InvalidOperationException("Secure cache not initialized"));
        set => _secure = value;
    }

    /// <summary>
    /// Gets the in-memory cache instance.
    /// </summary>
    public static IBlobCache InMemory { get; }

    /// <summary>
    /// Shuts down all cache instances and flushes pending operations.
    /// </summary>
    /// <returns>An observable that completes when shutdown is finished.</returns>
    public static IObservable<Unit> Shutdown()
    {
        _shutdownRequested = true;

        var disposables = new List<IObservable<Unit>>();

        if (_localMachine != null)
        {
            disposables.Add(_localMachine.Flush().Finally(() => _localMachine.Dispose()));
        }

        if (_userAccount != null)
        {
            disposables.Add(_userAccount.Flush().Finally(() => _userAccount.Dispose()));
        }

        if (_secure != null)
        {
            disposables.Add(_secure.Flush().Finally(() => _secure.Dispose()));
        }

        return disposables.Count == 0
            ? Observable.Return(Unit.Default)
            : disposables.Merge().TakeLast(1);
    }
}
