// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Plain-data snapshot of a single row from the legacy V10 <c>CacheElement</c> table.
/// Consumed by the v10→v11 migration service when it drains an entire v10 database file.
/// </summary>
/// <param name="Key">The cache key (primary key column on V10).</param>
/// <param name="TypeName">Optional type discriminator stored alongside the row, or <see langword="null"/>.</param>
/// <param name="Value">The serialized payload bytes, or <see langword="null"/>.</param>
/// <param name="Expiration">The expiration instant as a long tick count. 0 or a sentinel value means "never expires".</param>
/// <param name="CreatedAt">The creation instant as a long tick count.</param>
internal readonly record struct V10LegacyRow(
    string Key,
    string? TypeName,
    byte[]? Value,
    long Expiration,
    long CreatedAt);
