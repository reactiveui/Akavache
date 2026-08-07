// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the <see cref="RelativeTimeDownloadExtensions"/> overloads that take headers and leave
/// the fetch flag to the extension. Each test pins the request the overload produces, the fetch flag
/// it supplied, and the conversion of the caller's time span into the stored entry's expiration.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "These tests exist to exercise the string-URL overloads of the public Akavache API. "
        + "Each is paired with a Uri twin, so calling the Uri overload here would delete the only "
        + "coverage the string overloads have.")]
public class RelativeTimeDownloadExtensionsTests
{
    /// <summary>An absolute URL whose <see cref="Uri"/> form round-trips to the same text, so the cache key it produces is predictable.</summary>
    private const string SampleUrl = "http://localhost/data";

    /// <summary>How long from now a response stays valid when a test needs the entry to survive.</summary>
    private const int LivingHours = 1;

    /// <summary>How far in the past a response's expiration lands when a test needs the entry to lapse at once.</summary>
    private const int LapsedMinutes = -1;

    /// <summary>The <see cref="Uri"/> form of <see cref="SampleUrl"/>.</summary>
    private static readonly Uri SampleUri = new(SampleUrl);

    /// <summary>Headers a caller supplies, to show the caller's own collection is what reaches the request.</summary>
    private static readonly KeyValuePair<string, string>[] SampleHeaders = [new("X-Akavache-Test", "forwarded")];

    /// <summary>A download given headers sends them, and the cache serves the repeat because the extension did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUrl, HttpMethod.Post, TimeSpan.FromHours(LivingHours), SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A download whose time span has already elapsed stores the response with an expiration in the past.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithElapsedTimeSpanShouldStoreAnExpiredEntry() =>
        DownloadOverloadAssertions.AssertCachedEntryAlreadyExpired(
            static (cache, _) => cache.DownloadUrl(SampleUrl, HttpMethod.Post, TimeSpan.FromMinutes(LapsedMinutes), SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download given headers sends them, and the cache serves the repeat because the extension did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUri, HttpMethod.Post, TimeSpan.FromHours(LivingHours), SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download whose time span has already elapsed stores the response with an expiration in the past.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithElapsedTimeSpanShouldStoreAnExpiredEntry() =>
        DownloadOverloadAssertions.AssertCachedEntryAlreadyExpired(
            static (cache, _) => cache.DownloadUrl(SampleUri, HttpMethod.Post, TimeSpan.FromMinutes(LapsedMinutes), SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);
}
