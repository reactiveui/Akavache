// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// Predicates shared by the serializer extension members. These deliberately live outside
/// <c>SerializerExtensions</c>: they interrogate a type or a caller-supplied delegate rather than
/// acting on a receiver, so binding them as extensions would publish them on the extension surface
/// of <see cref="Type"/> and <see cref="Func{T, TResult}"/> for every consumer.
/// </summary>
internal static class SerializerHelpers
{
    /// <summary>
    /// Decides whether <c>GetAndFetchLatest</c> should bypass the cache and refetch the value.
    /// Returns <c>true</c> when no fetch predicate has been supplied, when the cache has no
    /// creation timestamp, or when the predicate evaluates to <c>true</c> against the
    /// existing timestamp.
    /// </summary>
    /// <param name="fetchPredicate">Optional predicate that decides whether the cached value is stale.</param>
    /// <param name="createdAt">The cache entry's creation timestamp, or <c>null</c> if missing.</param>
    /// <returns><c>true</c> if the cache should be bypassed and the value refetched.</returns>
    internal static bool ShouldRefetchCachedValue(Func<DateTimeOffset, bool>? fetchPredicate, DateTimeOffset? createdAt) =>
        fetchPredicate is null || createdAt is null || fetchPredicate(createdAt.Value);
}
