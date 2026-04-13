// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;

using SQLite;

namespace Akavache.V10toV11;

/// <summary>
/// Internal service that handles the one-time migration of data from V10 databases to V11 databases.
/// </summary>
internal static class V10MigrationService
{
    /// <summary>
    /// Sentinel key written to the V11 database to indicate migration has completed.
    /// </summary>
    internal const string MigrationSentinelKey = "__akavache_v10_migration_complete__";

    /// <summary>
    /// The minimum valid tick count for DateTime. Values at or below this threshold
    /// are treated as "no expiration" when converting from V10's tick-based format.
    /// </summary>
    private const long MinValidTicks = 630822816000000000L; // Year 2000 as ticks

    /// <summary>
    /// Migrates data from a V10 database file into a V11 SqliteBlobCache instance.
    /// </summary>
    /// <param name="v10DbPath">Full path to the V10 database file.</param>
    /// <param name="v11Cache">The V11 cache instance to migrate data into.</param>
    /// <param name="serializer">The current serializer, used for optional re-serialization.</param>
    /// <param name="options">Migration options.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static async Task MigrateAsync(
        string v10DbPath,
        SqliteBlobCache v11Cache,
        ISerializer serializer,
        V10MigrationOptions options)
    {
        if (!File.Exists(v10DbPath))
        {
            options.Logger?.Invoke($"V10 database not found at '{v10DbPath}', skipping.");
            return;
        }

        // Check if migration has already been completed
        if (await IsMigrationCompleteAsync(v11Cache).ConfigureAwait(false))
        {
            options.Logger?.Invoke($"Migration already completed for '{v10DbPath}', skipping.");
            return;
        }

        options.Logger?.Invoke($"Starting migration from '{v10DbPath}'...");

        // Open the V10 database read-only
        SqliteAkavacheConnection v10Connection = new(v10DbPath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.SharedCache);

        try
        {
            // Check if the CacheElement table exists in the V10 database
            var tableExists = await v10Connection.TableExistsAsync("CacheElement").ConfigureAwait(false);
            if (!tableExists)
            {
                options.Logger?.Invoke($"No CacheElement table found in '{v10DbPath}', skipping.");
                return;
            }

            // Read all entries from the V10 database
            var v10Entries = await v10Connection.QueryAsync<V10CacheElement>(static _ => true).ConfigureAwait(false);
            options.Logger?.Invoke($"Found {v10Entries.Count} entries in V10 database.");

            if (v10Entries.Count == 0)
            {
                await WriteMigrationSentinelAsync(v11Cache).ConfigureAwait(false);
                return;
            }

            // Convert and insert entries in batches using transactions
            List<CacheEntry> converted = new(v10Entries.Count);
            var failedCount = 0;

            foreach (var v10Entry in v10Entries)
            {
                try
                {
                    var v11Entry = ConvertEntry(v10Entry, serializer, options);
                    converted.Add(v11Entry);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    LogConvertEntryFailure(options, v10Entry.Key, ex);
                }
            }

            // Insert all converted entries in a single transaction
            await v11Cache.Connection.RunInTransactionAsync(tx =>
            {
                foreach (var entry in converted)
                {
                    tx.InsertOrReplace(entry);
                }
            }).ConfigureAwait(false);

            options.Logger?.Invoke($"Migrated {converted.Count} entries ({failedCount} failed).");

            // Write sentinel to prevent re-migration
            await WriteMigrationSentinelAsync(v11Cache).ConfigureAwait(false);
        }
        finally
        {
            await v10Connection.CloseAsync().ConfigureAwait(false);
        }

        // Optionally delete the old file
        if (!options.DeleteOldFiles)
        {
            return;
        }

        TryDeleteV10Database(v10DbPath, options);
    }

    /// <summary>
    /// Checks whether migration has already been completed for the given V11 cache.
    /// </summary>
    /// <param name="v11Cache">The V11 cache to check.</param>
    /// <returns>True if migration has already been completed.</returns>
    internal static async Task<bool> IsMigrationCompleteAsync(SqliteBlobCache v11Cache)
    {
        try
        {
            var sentinel = await v11Cache.Connection.FirstOrDefaultAsync<CacheEntry>(static e => e.Id == MigrationSentinelKey)
                .ConfigureAwait(false);
            return sentinel != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a V10 cache row into a V11 <see cref="CacheEntry"/>, optionally re-serializing the payload.
    /// </summary>
    /// <param name="v10Entry">The source V10 row.</param>
    /// <param name="serializer">The current serializer used for re-serialization.</param>
    /// <param name="options">The migration options controlling conversion behavior.</param>
    /// <returns>A new <see cref="CacheEntry"/> ready for insertion into the V11 cache.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static CacheEntry ConvertEntry(V10CacheElement v10Entry, ISerializer serializer, V10MigrationOptions options)
    {
        var createdAt = TicksToDateTimeOffset(v10Entry.CreatedAt);
        var expiresAt = ConvertExpiration(v10Entry.Expiration);
        var value = v10Entry.Value;

        // Optionally re-serialize from BSON to current format
        if (options.ReserializeToCurrentFormat && value is { Length: > 0 })
        {
            value = TryReserialize(value, v10Entry.TypeName, serializer, options);
        }

        return new()
        {
            Id = v10Entry.Key,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            TypeName = v10Entry.TypeName,
            Value = value,
        };
    }

    /// <summary>
    /// Attempts to re-serialize a V10 BSON payload using the current serializer for the given type.
    /// Returns the original bytes if the type cannot be resolved or re-serialization fails.
    /// </summary>
    /// <param name="value">The original payload bytes.</param>
    /// <param name="typeName">The type name recorded with the V10 entry.</param>
    /// <param name="serializer">The current serializer.</param>
    /// <param name="options">The migration options used for diagnostics.</param>
    /// <returns>The re-serialized bytes, or the original bytes when re-serialization is not possible.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration uses reflection to dynamically resolve types and call generic Serialize/Deserialize methods.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration uses reflection to dynamically resolve types and call generic Serialize/Deserialize methods.")]
    internal static byte[]? TryReserialize(byte[] value, string? typeName, ISerializer serializer, V10MigrationOptions options)
    {
        // Only attempt re-serialization if the data looks like BSON
        if (!UniversalSerializer.IsPotentialBsonData(value))
        {
            return value;
        }

        // We need the type to properly deserialize/re-serialize. Split the null check
        // out from the whitespace check so the compiler flow-tracks non-nullness on
        // every TFM (string.IsNullOrWhiteSpace is annotated with [NotNullWhen(false)]
        // on net6+ only).
        if (typeName is null)
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return value;
        }

        try
        {
            var type = ResolveType(typeName);
            if (type == null)
            {
                options.Logger?.Invoke($"Cannot resolve type '{typeName}' for re-serialization, keeping original bytes.");
                return value;
            }

            // Use reflection to call the generic UniversalSerializer.Deserialize<T> and Serialize<T>
            var deserializeMethod = typeof(UniversalSerializer)
                .GetMethod(nameof(UniversalSerializer.Deserialize))!
                .MakeGenericMethod(type);

            var deserialized = deserializeMethod.Invoke(null, [value, serializer, null]);
            if (deserialized == null)
            {
                return value;
            }

            var serializeMethod = typeof(UniversalSerializer)
                .GetMethod(nameof(UniversalSerializer.Serialize))!
                .MakeGenericMethod(type);

            return (byte[]?)serializeMethod.Invoke(null, [deserialized, serializer, null]);
        }
        catch (Exception ex)
        {
            LogReserializationFailure(options, typeName, ex);
            return value;
        }
    }

    /// <summary>
    /// Resolves a CLR <see cref="Type"/> from a possibly assembly-qualified name, falling back to scanning loaded assemblies.
    /// </summary>
    /// <param name="typeName">The type name to resolve.</param>
    /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if it cannot be found.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses Type.GetType and Assembly.GetType to resolve types dynamically.")]
    internal static Type? ResolveType(string typeName)
    {
        // First, try the full assembly-qualified name
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // Try with just the full name (without assembly qualification)
        var fullNamePart = typeName.Split(',')[0].Trim();
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a =>
            {
                try
                {
                    return a.GetType(fullNamePart);
                }
                catch
                {
                    return null;
                }
            })
            .FirstOrDefault(t => t != null);
    }

    /// <summary>
    /// Converts a V10 tick value to a UTC <see cref="DateTimeOffset"/>, returning the current time for invalid inputs.
    /// </summary>
    /// <param name="ticks">The tick count from the V10 row.</param>
    /// <returns>The corresponding <see cref="DateTimeOffset"/>.</returns>
    internal static DateTimeOffset TicksToDateTimeOffset(long ticks)
    {
        if (ticks is <= 0 or < MinValidTicks)
        {
            return DateTimeOffset.UtcNow;
        }

        try
        {
            return new(new(ticks, DateTimeKind.Utc));
        }
        catch
        {
            return DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Converts a V10 expiration tick value to a nullable <see cref="DateTimeOffset"/>, mapping zero or sentinel values to <c>null</c>.
    /// </summary>
    /// <param name="expirationTicks">The expiration tick count from the V10 row.</param>
    /// <returns>The expiration time, or <c>null</c> if the entry has no expiration.</returns>
    internal static DateTimeOffset? ConvertExpiration(long expirationTicks)
    {
        // V10 stored 0 or very small values to mean "no expiration"
        if (expirationTicks is <= 0 or < MinValidTicks)
        {
            return null;
        }

        // Check if already expired
        try
        {
            return new DateTimeOffset(new(expirationTicks, DateTimeKind.Utc));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the migration sentinel entry into the V11 cache to prevent re-migration on further runs.
    /// </summary>
    /// <param name="v11Cache">The V11 cache to mark as migrated.</param>
    /// <returns>A task representing the asynchronous writer.</returns>
    internal static async Task WriteMigrationSentinelAsync(SqliteBlobCache v11Cache)
    {
        CacheEntry sentinel = new()
        {
            Id = MigrationSentinelKey,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = null,
            TypeName = null,
            Value = [],
        };

        await v11Cache.Connection.InsertOrReplaceAsync(sentinel).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs a failure that occurred while converting a single V10 entry.
    /// </summary>
    /// <param name="options">Migration options carrying the logger.</param>
    /// <param name="key">The key of the entry that failed.</param>
    /// <param name="ex">The exception raised during conversion.</param>
    internal static void LogConvertEntryFailure(V10MigrationOptions options, string key, Exception ex) =>
        options.Logger?.Invoke($"Failed to convert entry '{key}': {ex.Message}");

    /// <summary>
    /// Logs a failure that occurred while attempting to re-serialize a payload for a given type.
    /// </summary>
    /// <param name="options">Migration options carrying the logger.</param>
    /// <param name="typeName">The type name involved in re-serialization.</param>
    /// <param name="ex">The exception raised during re-serialization.</param>
    internal static void LogReserializationFailure(V10MigrationOptions options, string? typeName, Exception ex) =>
        options.Logger?.Invoke($"Re-serialization failed for type '{typeName}': {ex.Message}. Keeping original bytes.");

    /// <summary>
    /// Attempts to delete the original V10 database file after migration, logging any failure.
    /// </summary>
    /// <param name="v10DbPath">Full path to the V10 database file.</param>
    /// <param name="options">Migration options carrying the logger.</param>
    internal static void TryDeleteV10Database(string v10DbPath, V10MigrationOptions options)
    {
        try
        {
            File.Delete(v10DbPath);
            options.Logger?.Invoke($"Deleted V10 database '{v10DbPath}'.");
        }
        catch (Exception ex)
        {
            options.Logger?.Invoke($"Failed to delete V10 database '{v10DbPath}': {ex.Message}");
        }
    }
}
