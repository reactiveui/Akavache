// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.V10toV11;

/// <summary>
/// Configuration options for migrating data from Akavache V10 databases to V11.
/// </summary>
/// <param name="DeleteOldFiles">Whether to delete the old V10 database files after successful migration. Default <see langword="false"/>.</param>
/// <param name="ReserializeToCurrentFormat">Whether to re-serialize cached values from the V10 format (typically BSON) to the current serializer format (e.g. System.Text.Json for AOT compatibility). When <see langword="false"/> the original bytes are preserved and the UniversalSerializer handles format detection at read time. Default <see langword="true"/>.</param>
/// <param name="MigrateLocalMachine">Whether to migrate the LocalMachine cache (V10: <c>blobs.db</c>). Default <see langword="true"/>.</param>
/// <param name="MigrateUserAccount">Whether to migrate the UserAccount cache (V10: <c>userblobs.db</c>). Default <see langword="true"/>.</param>
/// <param name="MigrateSecure">Whether to migrate the Secure cache (V10: <c>secret.db</c>). Default <see langword="true"/>.</param>
/// <param name="Logger">Optional logging callback for migration progress and diagnostic messages.</param>
public sealed record V10MigrationOptions(
    bool DeleteOldFiles = false,
    bool ReserializeToCurrentFormat = true,
    bool MigrateLocalMachine = true,
    bool MigrateUserAccount = true,
    bool MigrateSecure = true,
    Action<string>? Logger = null);
