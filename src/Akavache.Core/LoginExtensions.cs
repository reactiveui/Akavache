// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache;

/// <summary>Provides extension methods for handling user login credentials in secure blob caches.</summary>
public static class LoginExtensions
{
    /// <summary>
    /// Host identifier used when the caller does not supply one, so a cache that only ever
    /// holds a single login still has a stable key to store it under.
    /// </summary>
    internal const string DefaultHost = "default";

    /// <summary>Extension members for <c>ISecureBlobCache</c>.</summary>
    /// <param name="blobCache">The secure blob cache to store the login data.</param>
    extension(ISecureBlobCache blobCache)
    {
        /// <summary>
        /// Saves a username and password combination in a secure blob cache.
        /// Note that this method allows exactly one username/password combination to be saved per host.
        /// Calling this method multiple times for the same host will overwrite the previous entry.
        /// </summary>
        /// <param name="user">The username to save.</param>
        /// <param name="password">The password associated with the username.</param>
        /// <returns>An observable that signals when the login data is saved.</returns>
        [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
        public IObservable<Unit> SaveLogin(string user, string password) =>
            blobCache.SaveLogin(user, password, DefaultHost, (DateTimeOffset?)null);

        /// <summary>
        /// Saves a username and password combination in a secure blob cache.
        /// Note that this method allows exactly one username/password combination to be saved per host.
        /// Calling this method multiple times for the same host will overwrite the previous entry.
        /// </summary>
        /// <param name="user">The username to save.</param>
        /// <param name="password">The password associated with the username.</param>
        /// <param name="host">The host identifier to associate with the login data.</param>
        /// <returns>An observable that signals when the login data is saved.</returns>
        [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
        public IObservable<Unit> SaveLogin(string user, string password, string host) =>
            blobCache.SaveLogin(user, password, host, (DateTimeOffset?)null);

        /// <summary>
        /// Saves a username and password combination in a secure blob cache.
        /// Note that this method allows exactly one username/password combination to be saved per host.
        /// Calling this method multiple times for the same host will overwrite the previous entry.
        /// </summary>
        /// <param name="user">The username to save.</param>
        /// <param name="password">The password associated with the username.</param>
        /// <param name="host">The host identifier to associate with the login data.</param>
        /// <param name="absoluteExpiration">An optional expiration date for the cached login data.</param>
        /// <returns>An observable that signals when the login data is saved.</returns>
        [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
        public IObservable<Unit> SaveLogin(string user, string password, string host, DateTimeOffset? absoluteExpiration) =>
                blobCache.InsertObject($"login:{host}", new LoginInfo(user, password), absoluteExpiration);

        /// <summary>
        /// Retrieves the currently cached username and password for the specified host.
        /// If the cache does not contain login data for the host, this method returns an observable
        /// that signals an error with <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <returns>An observable that emits the cached login information.</returns>
        [RequiresUnreferencedCode("Using GetLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using GetLogin requires types to be preserved for serialization")]
        public IObservable<LoginInfo> GetLogin() =>
            blobCache.GetLogin(DefaultHost);

        /// <summary>
        /// Retrieves the currently cached username and password for the specified host.
        /// If the cache does not contain login data for the host, this method returns an observable
        /// that signals an error with <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <param name="host">The host identifier associated with the login data.</param>
        /// <returns>An observable that emits the cached login information.</returns>
        [RequiresUnreferencedCode("Using GetLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using GetLogin requires types to be preserved for serialization")]
        public IObservable<LoginInfo> GetLogin(string host) =>
                blobCache.GetObject<LoginInfo>($"login:{host}").Select(x => x ?? throw new KeyNotFoundException($"Login for host '{host}' not found in cache."));

        /// <summary>Erases the login associated with the specified host.</summary>
        /// <returns>A observable which signals when the erase is completed.</returns>
        public IObservable<Unit> EraseLogin() =>
            blobCache.EraseLogin(DefaultHost);

        /// <summary>Erases the login associated with the specified host.</summary>
        /// <param name="host">The host associated with the data.</param>
        /// <returns>A observable which signals when the erase is completed.</returns>
        public IObservable<Unit> EraseLogin(string host) =>
                blobCache.InvalidateObject<LoginInfo>($"login:{host}");
    }
}
