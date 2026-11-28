// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace OrderCache;

/// <summary>Caches a proof-of-delivery photo and prepares a smaller version for the order list.</summary>
internal static class DeliveryPhoto
{
    /// <summary>Saves the original in the caller's cache and returns a separately cached thumbnail.</summary>
    /// <param name="cache">The cache used by the app that registered the platform's bitmap loader.</param>
    /// <param name="orderNumber">The order shown in the delivery list.</param>
    /// <param name="photo">The full-size bitmap owned by the caller.</param>
    /// <param name="token">Cancels waiting for cache operations.</param>
    /// <returns>The preview bitmap, which the caller disposes when the view closes.</returns>
    internal static async Task<IBitmap> SavePreviewAsync(IBlobCache cache, int orderNumber, IBitmap photo, CancellationToken token)
    {
        const float PreviewEdgePixels = 96;
        var key = $"delivery/{orderNumber}/photo";
        var previewKey = $"{key}/preview";
        var expires = cache.Scheduler.Now.AddDays(1);
        _ = await cache.SaveImage(key, photo, expires).ToTask(token);
        _ = await cache.CreateAndCacheThumbnail(key, previewKey, PreviewEdgePixels, PreviewEdgePixels, expires).ToTask(token);
        return await cache.LoadImage(previewKey).ToTask(token);
    }
}
