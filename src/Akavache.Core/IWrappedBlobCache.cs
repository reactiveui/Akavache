// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Exposes the underlying blob cache used by a secure-cache wrapper.
/// </summary>
public interface IWrappedBlobCache
{
    /// <summary>
    /// Gets the underlying blob cache.
    /// </summary>
    IBlobCache InnerCache { get; }
}
