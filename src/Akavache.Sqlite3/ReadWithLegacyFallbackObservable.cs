// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Fused V11-then-V10 cache read. Collapses the three-operator pipeline
/// <c>Connection.Get(...).SelectMany(entry =&gt; entry.Value ?? Connection.TryReadLegacyV10Value(...).SelectMany(legacy =&gt; legacy ?? Throw))</c>
/// into a single stateful observable that sequences the primary read, the legacy
/// fallback read, and the not-found error without allocating any Rx <c>SelectMany</c>
/// operator wrappers per subscription.
/// </summary>
/// <remarks>
/// <para>
/// The primary read against the V11 <c>CacheEntry</c> table is the hot path — the
/// fast exit returns <c>entry.Value</c> verbatim when present. Only cold misses (key
/// absent from V11) subscribe to the legacy read; the intermediate
/// <see cref="SerialDisposable"/> is reused so the caller's dispose
/// handle wires transparently to whichever subscription is active at the time.
/// </para>
/// <para>
/// Not-found propagates as <see cref="KeyNotFoundException"/> with the same message
/// shape as the prior Rx-chain implementation, so callers that pattern-match on the
/// exception message continue to work unchanged.
/// </para>
/// </remarks>
/// <param name="connection">The underlying sqlite connection used for both the V11 primary read and the V10 legacy fallback.</param>
/// <param name="key">The cache key to look up.</param>
/// <param name="type">Optional type filter; <see langword="null"/> for untyped reads.</param>
internal sealed class ReadWithLegacyFallbackObservable(IAkavacheConnection connection, string key, Type? type) : IObservable<byte[]>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = new SerialDisposable();
        subscription.Disposable = connection
            .Get(key, type?.FullName, now)
            .Subscribe(new PrimaryObserver(connection, key, type, now, observer, subscription));
        return subscription;
    }

    /// <summary>
    /// Builds the <see cref="KeyNotFoundException"/> emitted when neither the V11 nor
    /// the V10 tables contain the requested key. Format kept identical to the prior
    /// Rx-chain implementation so consumers that inspect the exception message
    /// continue to see the same shape.
    /// </summary>
    /// <param name="key">The cache key that failed to resolve.</param>
    /// <param name="type">The optional type filter used in the lookup.</param>
    /// <returns>The formatted not-found exception.</returns>
    internal static KeyNotFoundException CreateNotFound(string key, Type? type) =>
        new(type is null
            ? $"The given key '{key}' was not present in the cache."
            : $"The given key '{key}' (type '{type.FullName}') was not present in the cache.");

    /// <summary>
    /// Observes the V11 primary read. On a hit it emits the payload and completes;
    /// on a miss it transitions to <see cref="LegacyObserver"/> via a fresh
    /// subscription installed on the shared <see cref="SingleAssignmentDisposable"/>.
    /// </summary>
    /// <param name="connection">The SQLite connection used for the legacy fallback read.</param>
    /// <param name="key">The cache key being looked up.</param>
    /// <param name="type">Optional type filter.</param>
    /// <param name="now">Timestamp captured at <see cref="Subscribe"/> time, passed to the legacy read for expiry checks.</param>
    /// <param name="downstream">The downstream observer receiving the resolved payload.</param>
    /// <param name="subscription">Shared serial subscription handle — replaced with the legacy subscription on V11 miss.</param>
    private sealed class PrimaryObserver(
        IAkavacheConnection connection,
        string key,
        Type? type,
        DateTimeOffset now,
        IObserver<byte[]> downstream,
        SerialDisposable subscription) : IObserver<CacheEntry?>
    {
        /// <summary>Set on the first non-null payload; gates whether <see cref="OnCompleted"/> forwards the terminal or falls back to the V10 reader.</summary>
        private bool _emitted;

        /// <inheritdoc/>
        public void OnNext(CacheEntry? value)
        {
            if (value?.Value is not { } bytes)
            {
                return;
            }

            _emitted = true;
            downstream.OnNext(bytes);
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted()
        {
            if (_emitted)
            {
                downstream.OnCompleted();
                return;
            }

            // V11 missed — fall back to the V10 legacy store. Replace the current
            // inner subscription on the shared handle so external disposal still
            // wins against the secondary subscription.
            var legacy = connection.TryReadLegacyV10Value(key, now, type);
            subscription.Disposable = legacy.Subscribe(new LegacyObserver(key, type, downstream));
        }
    }

    /// <summary>
    /// Observes the V10 legacy fallback read. On a hit it emits the payload and
    /// completes; on a miss it surfaces a <see cref="KeyNotFoundException"/>.
    /// </summary>
    /// <param name="key">The cache key being looked up — used to format the not-found error.</param>
    /// <param name="type">Optional type filter — used to format the not-found error.</param>
    /// <param name="downstream">The downstream observer receiving the resolved payload.</param>
    private sealed class LegacyObserver(
        string key,
        Type? type,
        IObserver<byte[]> downstream) : IObserver<byte[]?>
    {
        /// <summary>Set on the first non-null legacy payload; gates whether <see cref="OnCompleted"/> forwards the terminal or surfaces a not-found error.</summary>
        private bool _emitted;

        /// <inheritdoc/>
        public void OnNext(byte[]? value)
        {
            if (value is null)
            {
                return;
            }

            _emitted = true;
            downstream.OnNext(value);
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted()
        {
            if (_emitted)
            {
                downstream.OnCompleted();
                return;
            }

            downstream.OnError(CreateNotFound(key, type));
        }
    }
}
