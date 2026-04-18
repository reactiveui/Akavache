// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Defines the durability and performance characteristics of a checkpoint operation.
/// A checkpoint synchronizes the data between the write-ahead log (WAL) and the main database file.
/// </summary>
public enum CheckpointMode
{
    /// <summary>
    /// A lightweight checkpoint that processes as much data as possible without blocking current database operations.
    /// This mode does not wait for existing readers or writers to finish and is mapped to the SQLite passive checkpoint mode.
    /// </summary>
    Passive,

    /// <summary>
    /// A comprehensive checkpoint that ensures all committed transactions are fully merged into the main database.
    /// This mode waits for all active writers to complete and ensures the main database file is updated and flushed to disk.
    /// It maps to the SQLite full checkpoint mode.
    /// </summary>
    Full,

    /// <summary>
    /// The most extensive checkpoint that performs a full synchronization and then resets the write-ahead log.
    /// In addition to completing all pending writes, this mode waits for all readers to finish so it can truncate the log file back to its starting size.
    /// It maps to the SQLite truncate checkpoint mode.
    /// </summary>
    Truncate,
}
