// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Runs the examples against the package family under test.</summary>
[Category("Akavache")]
public class OrderCacheExampleTests
{
    /// <summary>Checks the order's lifetime, cached delivery instructions and encrypted disk round trip.</summary>
    /// <returns>Completion after checking the example's results.</returns>
    [Test]
    public async Task OrderCacheRunsToCompletion() =>
        await Assert.That(OrderCache.Program.Main).ThrowsNothing();

    /// <summary>Checks that a dispatcher can reopen a BSON batch and see the old and refreshed order.</summary>
    /// <param name="token">Cancels waiting for the SQLite operations.</param>
    /// <returns>Completion after checking both versions and the remaining order.</returns>
    [Test]
    public async Task DispatchManifestReadsBsonAndRefreshesOrder(CancellationToken token)
    {
        var directory = Directory.CreateTempSubdirectory("akavache-manifest-");
        try
        {
            OrderCache.Order original = new(1, "Ada");
            OrderCache.Order next = new(original.Number + 1, "Grace");
            var revised = original with { Customer = "Lin" };
            var (versions, remaining) = await OrderCache.DispatchManifest.RefreshAsync(
                Path.Combine(directory.FullName, "manifest.db"),
                original,
                next,
                revised,
                token);
            await Assert.That(versions is [var first, var second] && first == original && second == revised).IsTrue();
            await Assert.That(remaining).IsEquivalentTo([next]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>Checks that a subscriber receives the dispatcher's saved notification preference.</summary>
    /// <param name="token">Cancels the cache write and the wait for its notification.</param>
    /// <returns>Completion after reading the updated setting.</returns>
    [Test]
    public async Task DeliveryPreferencesPublishesSavedChoice(CancellationToken token)
    {
        var serializer = new SystemJsonSerializer { Options = OrderCache.OrderJsonContext.Default.Options };
        using var preferences = new OrderCache.DeliveryPreferences(new InMemoryBlobCache(serializer));
        var notification = preferences.Notifications.Where(static enabled => !enabled).Take(1).ToTask(token);
        await preferences.SetNotificationsAsync(false, token);
        await Assert.That(await notification).IsFalse();
    }
}
