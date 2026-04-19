// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="ReadWithLegacyFallbackObservable"/> covering the primary OnError
/// path (line 99) and the legacy OnError path (line 146).
/// </summary>
[Category("Akavache")]
public class ReadWithLegacyFallbackObservableTests
{
    // ── PrimaryObserver.OnError (line 99) ────────────────────────────

    /// <summary>
    /// When the primary V11 Get call errors, the error propagates to the downstream
    /// observer via PrimaryObserver.OnError (line 99).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task PrimaryObserver_OnError_PropagatesDownstream()
    {
        InMemoryAkavacheConnection connection = new() { FailGet = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            Exception? ex = null;
            cache.Get("somekey").Subscribe(_ => { }, e => ex = e);

            await Assert.That(ex).IsTypeOf<InvalidOperationException>();
            await Assert.That(ex!.Message).Contains("Simulated Get failure");
        }
        finally
        {
            connection.FailGet = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// When the primary V11 typed Get call errors, the error propagates to the downstream
    /// observer via PrimaryObserver.OnError (line 99).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task PrimaryObserver_OnError_TypedGet_PropagatesDownstream()
    {
        InMemoryAkavacheConnection connection = new() { FailGet = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            Exception? ex = null;
            cache.Get("somekey", typeof(string)).Subscribe(_ => { }, e => ex = e);

            await Assert.That(ex).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            connection.FailGet = false;
            cache.Dispose();
        }
    }

    // ── LegacyObserver.OnError (line 146) ────────────────────────────

    /// <summary>
    /// When the V10 legacy read errors, the error propagates to the downstream
    /// observer via LegacyObserver.OnError (line 146).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task LegacyObserver_OnError_PropagatesDownstream()
    {
        InMemoryAkavacheConnection connection = new() { FailLegacyRead = true };

        // Don't insert the key into the primary store so the fallback is triggered.
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            Exception? ex = null;
            cache.Get("missing").Subscribe(_ => { }, e => ex = e);

            await Assert.That(ex).IsTypeOf<InvalidOperationException>();
            await Assert.That(ex!.Message).Contains("Simulated legacy read failure");
        }
        finally
        {
            connection.FailLegacyRead = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// When the typed V10 legacy read errors, the error propagates to the downstream
    /// observer via LegacyObserver.OnError (line 146).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task LegacyObserver_OnError_TypedGet_PropagatesDownstream()
    {
        InMemoryAkavacheConnection connection = new() { FailLegacyRead = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            Exception? ex = null;
            cache.Get("missing", typeof(string)).Subscribe(_ => { }, e => ex = e);

            await Assert.That(ex).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            connection.FailLegacyRead = false;
            cache.Dispose();
        }
    }

    // ── CreateNotFound format ────────────────────────────────────────

    /// <summary>
    /// CreateNotFound with null type produces a message without type info.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CreateNotFound_NullType_MessageWithoutTypeName()
    {
        var ex = ReadWithLegacyFallbackObservable.CreateNotFound("mykey", null);

        await Assert.That(ex.Message).Contains("mykey");
        await Assert.That(ex.Message).DoesNotContain("type");
    }

    /// <summary>
    /// CreateNotFound with a type produces a message including the type's FullName.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CreateNotFound_WithType_MessageIncludesTypeName()
    {
        var ex = ReadWithLegacyFallbackObservable.CreateNotFound("mykey", typeof(string));

        await Assert.That(ex.Message).Contains("mykey");
        await Assert.That(ex.Message).Contains("System.String");
    }

    // ── Subscription disposal ────────────────────────────────────────

    /// <summary>
    /// Disposing the subscription returned by ReadWithLegacyFallbackObservable
    /// before the primary read completes does not throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_ThenDispose_DoesNotThrow()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["k"] = [1, 2];
        var observable = new ReadWithLegacyFallbackObservable(connection, "k", null);

        byte[]? received = null;
        var sub = observable.Subscribe(
            v => received = v,
            _ => { },
            () => { });
        sub.Dispose();

        // Whether or not data was received depends on timing,
        // but disposing should not throw.
        await Task.CompletedTask;
    }
}
