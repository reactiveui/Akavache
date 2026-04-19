// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.V10toV11;

/// <summary>
/// Internal model matching the Akavache V10 <c>CacheElement</c> database schema.
/// Used to read rows from V10 databases during migration.
/// </summary>
/// <param name="Key">The cache key.</param>
/// <param name="TypeName">Optional fully qualified type name of the cached object.</param>
/// <param name="Value">The serialized value bytes.</param>
/// <param name="Expiration">The expiration time as <see cref="DateTime"/> ticks. A value of 0 or less indicates no expiration.</param>
/// <param name="CreatedAt">The creation time as <see cref="DateTime"/> ticks.</param>
internal sealed record V10CacheElement(
    string Key,
    string? TypeName,
    byte[]? Value,
    long Expiration,
    long CreatedAt);
