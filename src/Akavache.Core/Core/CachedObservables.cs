// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Shared, cached observable singletons for frequently emitted trivial values.
/// Rx.NET's <see cref="Observable.Return{TResult}(TResult)"/> allocates a new
/// <c>ScalarObservable</c> on every call, so hot paths that emit <see cref="Unit.Default"/>
/// as a no-op success signal (e.g. <c>Flush</c>, bulk <c>Insert</c>, <c>Invalidate</c>)
/// allocate once per call. Routing through these cached instances is semantically
/// identical and allocation-free.
/// </summary>
internal static class CachedObservables
{
    /// <summary>A cached <see cref="IObservable{T}"/> that synchronously emits a single
    /// <see cref="Unit.Default"/> and completes. Use anywhere the library currently
    /// calls <c>Observable.Return(Unit.Default)</c> as a success signal.</summary>
    public static readonly IObservable<Unit> UnitDefault = Observable.Return(Unit.Default);
}
