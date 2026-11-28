// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace OrderCache;

/// <summary>Stores orders in memory using generated JSON metadata.</summary>
internal static class Program
{
    /// <summary>Saves an order, looks it up by key and removes it from the cache.</summary>
    /// <returns>Completion after checking the order and the remaining keys.</returns>
    /// <exception cref="InvalidOperationException">The cache returned an unexpected value.</exception>
    internal static async Task Main()
    {
        const string Key = "orders/42";
        const int OrderNumber = 42;
        const int KeyBytes = 32;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var token = deadline.Token;
        var instance = CacheDatabase.CreateBuilder("OrderCache")
            .WithSerializerSystemTextJson(OrderJsonContext.Default.Options)
            .WithInMemory()
            .Build();
        using var cache = instance.InMemory ?? throw new InvalidOperationException("Configure the in-memory cache before opening it.");
        var order = new Order(OrderNumber, "Ada");
        var metadata = OrderJsonContext.Default.Order;
        var bytes = SystemJsonSerializer.SerializeAot(order, metadata);
        var expires = cache.Scheduler.Now.AddHours(1);
        _ = await cache.Insert(Key, bytes, typeof(Order), expires).ToTask(token);

        var stored = await cache.Get(Key, typeof(Order)).ToTask(token) ?? throw new InvalidOperationException("The saved order is missing.");
        var restored = SystemJsonSerializer.DeserializeAot(stored, metadata);
        var created = await cache.GetCreatedAt(Key, typeof(Order)).ToTask(token);
        var key = await cache.GetAllKeys(typeof(Order)).ToTask(token);
        if (restored != order || created is null || key != Key)
        {
            throw new InvalidOperationException("The cached order, its creation time and key should be available.");
        }

        _ = await cache.UpdateExpiration(Key, typeof(Order), expires.AddHours(1)).ToTask(token);
        _ = await cache.Flush(typeof(Order)).ToTask(token);
        _ = await cache.Invalidate(Key, typeof(Order)).ToTask(token);
        _ = await cache.Vacuum().ToTask(token);
        var remaining = await cache.GetAll(typeof(Order)).ToList().ToTask(token);
        if (remaining.Count != 0)
        {
            throw new InvalidOperationException("The invalidated order still appears in the cache.");
        }

        var notes = await DeliveryInstructions.ReadAsync(token);
        if (notes != ("Leave with reception.", "Signature required."))
        {
            throw new InvalidOperationException("Only a forced refresh should load the new instructions.");
        }

        var directory = Directory.CreateTempSubdirectory("akavache-orders-");
        try
        {
            var database = Path.Combine(directory.FullName, "orders.db");
            var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(KeyBytes));
            var reopened = await OrderArchive.ReopenAsync(database, password, order, token);
            if (reopened != order)
            {
                throw new InvalidOperationException("The reopened archive should contain the saved order.");
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
