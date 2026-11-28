// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace OrderCache;

/// <summary>Writes an order and reads it back after reopening an encrypted SQLite database.</summary>
internal static class OrderArchive
{
    /// <summary>Opens the same database twice using a caller-supplied encryption key.</summary>
    /// <param name="databasePath">The path to the database file.</param>
    /// <param name="password">The key used to open the encrypted archive.</param>
    /// <param name="order">The order to retain between sessions.</param>
    /// <param name="token">Cancels waiting for cache operations.</param>
    /// <returns>The order read through its generated JSON contract.</returns>
    internal static async Task<Order?> ReopenAsync(string databasePath, string password, Order order, CancellationToken token)
    {
        _ = CacheDatabase.CreateBuilder(nameof(OrderArchive)).WithEncryptedSqliteProvider();
        var serializer = new SystemJsonBsonSerializer();
        var metadata = OrderJsonContext.Default.Order;
        var key = $"orders/{order.Number}";
        using (var archive = new EncryptedSqliteBlobCache(databasePath, password, serializer))
        {
            var payload = archive.Serializer.Serialize(order, metadata);
            _ = await archive.Insert(key, payload, typeof(Order)).ToTask(token);
            _ = await archive.Flush().ToTask(token);
        }

        using var reopened = new EncryptedSqliteBlobCache(databasePath, password, serializer);
        var saved = await reopened.Get(key, typeof(Order)).ToTask(token);
        return saved is null ? null : reopened.Serializer.Deserialize(saved, metadata);
    }
}
