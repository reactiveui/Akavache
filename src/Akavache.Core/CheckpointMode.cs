// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Describes the durability level of a <see cref="IAkavacheConnection.CheckpointAsync"/> request.
/// Backends that do not have a concept of a write-ahead log should treat all values the same
/// (or as a no-op).
/// </summary>
public enum CheckpointMode
{
    /// <summary>
    /// A best-effort checkpoint that does not block concurrent readers/writers.
    /// Maps to <c>PRAGMA wal_checkpoint(PASSIVE)</c> on SQLite.
    /// </summary>
    Passive,

    /// <summary>
    /// A stronger checkpoint that waits for writers and ensures all committed data is
    /// flushed to the main database file. Maps to <c>PRAGMA wal_checkpoint(FULL)</c> on SQLite.
    /// </summary>
    Full,

    /// <summary>
    /// The strongest checkpoint that additionally truncates the write-ahead log file.
    /// Maps to <c>PRAGMA wal_checkpoint(TRUNCATE)</c> on SQLite.
    /// </summary>
    Truncate,
}
