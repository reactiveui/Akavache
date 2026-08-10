// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Exposes the underlying blob cache used by a secure-cache wrapper.</summary>
public interface IWrappedBlobCache
{
    /// <summary>Gets the underlying blob cache.</summary>
    IBlobCache InnerCache { get; }
}
