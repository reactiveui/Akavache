// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache;

/// <summary>
/// Provides extension methods for handling user login credentials in secure blob caches.
/// </summary>
public static class LoginExtensions
{
    /// <summary>
    /// Saves a username and password combination in a secure blob cache.
    /// Note that this method allows exactly one username/password combination to be saved per host.
    /// Calling this method multiple times for the same host will overwrite the previous entry.
    /// </summary>
    /// <param name="blobCache">The secure blob cache to store the login data.</param>
    /// <param name="user">The username to save.</param>
    /// <param name="password">The password associated with the username.</param>
    /// <param name="host">The host identifier to associate with the login data.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached login data.</param>
    /// <returns>An observable that signals when the login data is saved.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
#endif
    public static IObservable<Unit> SaveLogin(this ISecureBlobCache blobCache, string user, string password, string host = "default", DateTimeOffset? absoluteExpiration = null) =>
        blobCache.InsertObject("login:" + host, new LoginInfo(user, password), absoluteExpiration);

    /// <summary>
    /// Retrieves the currently cached username and password for the specified host.
    /// If the cache does not contain login data for the host, this method returns an observable
    /// that signals an error with <see cref="KeyNotFoundException"/>.
    /// </summary>
    /// <param name="blobCache">The secure blob cache to retrieve the login data from.</param>
    /// <param name="host">The host identifier associated with the login data.</param>
    /// <returns>An observable that emits the cached login information.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetLogin requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using GetLogin requires types to be preserved for serialization")]
#endif
    public static IObservable<LoginInfo> GetLogin(this ISecureBlobCache blobCache, string host = "default") =>
        blobCache.GetObject<LoginInfo>("login:" + host).Select(x => x ?? throw new KeyNotFoundException($"Login for host '{host}' not found in cache."));

    /// <summary>
    /// Erases the login associated with the specified host.
    /// </summary>
    /// <param name="blobCache">The blob cache where to erase the data.</param>
    /// <param name="host">The host associated with the data.</param>
    /// <returns>A observable which signals when the erase is completed.</returns>
    public static IObservable<Unit> EraseLogin(this ISecureBlobCache blobCache, string host = "default") =>
        blobCache.InvalidateObject<LoginInfo>("login:" + host);
}
