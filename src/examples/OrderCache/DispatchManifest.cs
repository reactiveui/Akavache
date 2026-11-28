// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace OrderCache;

/// <summary>Opens a BSON dispatch manifest with a JSON writer and refreshes a cached order.</summary>
internal static class DispatchManifest
{
    /// <summary>Saves two orders as BSON, reopens them and delivers the cached and revised versions in order.</summary>
    /// <param name="databasePath">The path of the dispatch database.</param>
    /// <param name="order">The original order.</param>
    /// <param name="next">The next order on the manifest.</param>
    /// <param name="revised">The revised version of the original order.</param>
    /// <param name="token">Cancels waiting for cache operations.</param>
    /// <returns>The versions seen by the dispatcher and the order left after dispatch.</returns>
    [RequiresUnreferencedCode("The Newtonsoft.Json cache serializer reads the order properties at runtime.")]
    [RequiresDynamicCode("The Newtonsoft.Json cache serializer builds contracts at runtime.")]
    internal static async Task<(Order?[] Versions, Order[] Remaining)> RefreshAsync(
        string databasePath,
        Order order,
        Order next,
        Order revised,
        CancellationToken token)
    {
        _ = CacheDatabase.CreateBuilder(nameof(DispatchManifest)).WithSqliteProvider();
        var key = $"orders/{order.Number}";
        var nextKey = $"orders/{next.Number}";
        using (var writer = new SqliteBlobCache(databasePath, new NewtonsoftBsonSerializer()))
        {
            Dictionary<string, Order> batch = new() { [key] = order, [nextKey] = next };
            _ = await writer.InsertObjects(batch).ToTask(token);
            _ = await writer.Flush().ToTask(token);
        }

        using var cache = new SqliteBlobCache(databasePath, new NewtonsoftSerializer());
        var versions = await cache.GetAndFetchLatest(key, () => Task.FromResult(revised)).ToArray().ToTask(token);
        _ = await cache.InvalidateObject<Order>(key).ToTask(token);
        var remaining = await cache.GetAllObjects<Order>().ToArray().ToTask(token);
        return (versions, remaining);
    }
}
