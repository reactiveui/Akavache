// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace OrderCache;

/// <summary>Reads delivery instructions from the cache and refreshes them on demand.</summary>
internal sealed class DeliveryInstructions : HttpMessageHandler
{
    /// <summary>Gets or sets the text the local dispatch service currently serves.</summary>
    internal string Note { get; set; } = "Leave with reception.";

    /// <summary>Uses the same key for the original download, a cached read and an explicit refresh.</summary>
    /// <param name="token">Cancels waiting for the three reads.</param>
    /// <returns>The cached and refreshed notes.</returns>
    internal static async Task<(string Cached, string Refreshed)> ReadAsync(CancellationToken token)
    {
        const string Key = "orders/42/delivery";
        var address = new Uri("https://dispatch.example/orders/42/delivery");
        using var transport = new DeliveryInstructions();
        using var service = new HttpService();
        service.HttpClient.Dispose();
        service.HttpClient = new(transport, disposeHandler: false);
        using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        cache.SetHttpService(service);

        _ = await cache.DownloadUrl(Key, address, false, cache.Scheduler.Now.AddHours(1)).ToTask(token);
        transport.Note = "Signature required.";
        var cached = await cache.DownloadUrl(Key, address).ToTask(token);
        var refreshed = await cache.DownloadUrl(Key, address, fetchAlways: true).ToTask(token);
        return (Encoding.UTF8.GetString(cached), Encoding.UTF8.GetString(refreshed));
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(Note, Encoding.UTF8, "text/plain") });
    }
}
