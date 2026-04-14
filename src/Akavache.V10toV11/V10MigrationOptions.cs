// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.V10toV11;

/// <summary>
/// Configuration options for migrating data from Akavache V10 databases to V11.
/// </summary>
public record V10MigrationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to delete the old V10 database files after successful migration.
    /// Default is <c>false</c>.
    /// </summary>
    public bool DeleteOldFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to re-serialize cached values from the V10 format (typically BSON)
    /// to the current serializer format (e.g., System.Text.Json for AOT compatibility).
    /// When <c>false</c>, the original bytes are preserved and the UniversalSerializer handles format detection at read time.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ReserializeToCurrentFormat { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to migrate the LocalMachine cache (V10: blobs.db).
    /// Default is <c>true</c>.
    /// </summary>
    public bool MigrateLocalMachine { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to migrate the UserAccount cache (V10: userblobs.db).
    /// Default is <c>true</c>.
    /// </summary>
    public bool MigrateUserAccount { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to migrate the Secure cache (V10: secret.db).
    /// Default is <c>true</c>.
    /// </summary>
    public bool MigrateSecure { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional logging callback for migration progress and diagnostic messages.
    /// </summary>
    public Action<string>? Logger { get; set; }
}
