// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.V10toV11;
#else
namespace Akavache.V10toV11;
#endif

/// <summary>Internal service that handles the one-time migration of data from V10 databases to V11 databases.</summary>
internal static class V10MigrationService
{
    /// <summary>Sentinel key written to the V11 database to indicate migration has completed.</summary>
    internal const string MigrationSentinelKey = "__akavache_v10_migration_complete__";

    /// <summary>
    /// The minimum valid tick count for DateTime. Values at or below this threshold
    /// are treated as "no expiration" when converting from V10's tick-based format.
    /// </summary>
    private const long MinValidTicks = 630_822_816_000_000_000L; // Year 2000 as ticks

    /// <summary>
    /// Gets or sets the clock used for the sentinel read and for the timestamps written when a
    /// legacy value carries no usable date. Defaults to the machine clock; tests substitute a fake
    /// so migration output is deterministic. Internal so it does not widen the migration surface.
    /// </summary>
    internal static TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <summary>
    /// Migrates data from a V10 database file into a V11
    /// <see cref="SqliteBlobCache"/> instance. Authored as a pure observable pipeline:
    /// check-sentinel → probe-table → stream-rows → upsert → write-sentinel → close,
    /// with each stage feeding the next via <c>SelectMany</c>. The returned observable
    /// emits a single <see cref="RxVoid"/> on success or the first stage error.
    /// </summary>
    /// <param name="v10DbPath">Full path to the V10 database file.</param>
    /// <param name="v11Cache">The V11 cache instance to migrate data into.</param>
    /// <param name="serializer">The current serializer, used for optional re-serialization.</param>
    /// <param name="options">Migration options.</param>
    /// <returns>A one-shot observable that fires when migration completes (or the file is absent / migration already ran).</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static IObservable<RxVoid> Migrate(
        string v10DbPath,
        SqliteBlobCache v11Cache,
        ISerializer serializer,
        V10MigrationOptions options)
    {
        if (!File.Exists(v10DbPath))
        {
            options.Logger?.Invoke($"V10 database not found at '{v10DbPath}', skipping.");
            return ImmutableReturnRxVoidSignal.Instance;
        }

        return IsMigrationComplete(v11Cache)
            .SelectMany(alreadyMigrated =>
            {
                if (alreadyMigrated)
                {
                    options.Logger?.Invoke($"Migration already completed for '{v10DbPath}', skipping.");
                    return ImmutableReturnRxVoidSignal.Instance;
                }

                options.Logger?.Invoke($"Starting migration from '{v10DbPath}'...");
                return MigrateCore(v10DbPath, v11Cache, serializer, options);
            });
    }

    /// <summary>
    /// Inner migration pipeline — runs against an opened v10 connection, walks the
    /// legacy rows, upserts into the v11 cache, writes the sentinel, and closes the
    /// v10 connection in the <c>Finally</c> operator regardless of success or error.
    /// Marked <c>internal</c> so tests can drive it without going through the
    /// sentinel short-circuit in <see cref="Migrate"/>.
    /// </summary>
    /// <param name="v10DbPath">The legacy database path (captured for delete-on-success).</param>
    /// <param name="v11Cache">The destination V11 cache.</param>
    /// <param name="serializer">The current serializer.</param>
    /// <param name="options">Migration options.</param>
    /// <returns>A one-shot observable that fires on migration completion.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static IObservable<RxVoid> MigrateCore(
        string v10DbPath,
        SqliteBlobCache v11Cache,
        ISerializer serializer,
        V10MigrationOptions options)
    {
        var v10Connection = SqlitePclRawConnection.Create(v10DbPath, password: null, readOnly: true);

        return v10Connection.TableExists("CacheElement")
            .SelectMany(tableExists =>
            {
                if (!tableExists)
                {
                    options.Logger?.Invoke($"No CacheElement table found in '{v10DbPath}', skipping.");
                    return ImmutableReturnRxVoidSignal.Instance;
                }

                return v10Connection.ReadAllLegacyV10Rows()
                    .ToList()
                    .SelectMany(v10Rows =>
                    {
                        options.Logger?.Invoke($"Found {v10Rows.Count} entries in V10 database.");
                        if (v10Rows.Count == 0)
                        {
                            return WriteMigrationSentinel(v11Cache);
                        }

                        var converted = new List<CacheEntry>(v10Rows.Count);
                        var failedCount = 0;
                        foreach (var row in v10Rows)
                        {
                            failedCount += ConvertRowInto(row, serializer, options, converted);
                        }

                        options.Logger?.Invoke($"Migrated {converted.Count} entries ({failedCount} failed).");

                        var upsert = converted.Count > 0
                            ? v11Cache.Connection.Upsert(converted)
                            : ImmutableReturnRxVoidSignal.Instance;

                        return upsert.SelectMany(_ => WriteMigrationSentinel(v11Cache));
                    });
            })
            .Finally(v10Connection.Dispose)
            .SelectMany(_ =>
            {
                if (options.DeleteOldFiles)
                {
                    TryDeleteV10Database(v10DbPath, options);
                }

                return ImmutableReturnRxVoidSignal.Instance;
            });
    }

    /// <summary>
    /// Checks whether migration has already been completed for the given V11 cache.
    /// Emits <see langword="true"/> when the sentinel row is present,
    /// <see langword="false"/> otherwise (including when the Get errors — treated as
    /// "not yet migrated" rather than propagating).
    /// </summary>
    /// <param name="v11Cache">The V11 cache to check.</param>
    /// <returns>A one-shot observable that emits the migration-complete flag.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IObservable<bool> IsMigrationComplete(SqliteBlobCache v11Cache) =>
        v11Cache.Connection
            .Get(MigrationSentinelKey, typeFullName: null, Clock.GetUtcNow())
            .Select(static sentinel => sentinel is not null)
            .Catch<bool, Exception>(static _ => ImmutableReturnFalseSignal.Instance);

    /// <summary>Converts a raw V10 legacy row into a V11 <see cref="CacheEntry"/>, optionally re-serializing the payload.</summary>
    /// <param name="row">The source V10 row.</param>
    /// <param name="serializer">The current serializer used for re-serialization.</param>
    /// <param name="options">The migration options controlling conversion behavior.</param>
    /// <returns>A new <see cref="CacheEntry"/> ready for insertion into the V11 cache.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static CacheEntry ConvertRow(in V10LegacyRow row, ISerializer serializer, V10MigrationOptions options)
    {
        var createdAt = TicksToDateTimeOffset(row.CreatedAt);
        var expiresAt = ConvertExpiration(row.Expiration);
        var value = row.Value;

        if (options.ReserializeToCurrentFormat && value is { Length: > 0 })
        {
            value = TryReserialize(value, row.TypeName, serializer, options);
        }

        return new(row.Key, row.TypeName, value, createdAt, expiresAt);
    }

    /// <summary>Legacy shim kept for existing unit tests that still operate on <see cref="V10CacheElement"/>.</summary>
    /// <param name="v10Entry">The V10 entry to convert.</param>
    /// <param name="serializer">The current serializer used for re-serialization.</param>
    /// <param name="options">The migration options controlling conversion behavior.</param>
    /// <returns>A new <see cref="CacheEntry"/> ready for insertion into the V11 cache.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static CacheEntry ConvertEntry(V10CacheElement v10Entry, ISerializer serializer, V10MigrationOptions options) =>
        ConvertRow(new(v10Entry.Key, v10Entry.TypeName, v10Entry.Value, v10Entry.Expiration, v10Entry.CreatedAt), serializer, options);

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
        if (!BsonDataHelper.IsPotentialBsonData(value))
        {
            return value;
        }

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
            if (type is null)
            {
                options.Logger?.Invoke($"Cannot resolve type '{typeName}' for re-serialization, keeping original bytes.");
                return value;
            }

            var deserializeMethod = GetSerializerMethod(nameof(UniversalSerializer.Deserialize)).MakeGenericMethod(type);
            var deserialized = deserializeMethod.Invoke(null, [value, serializer, null]);

            return deserialized is null
                ? value
                : (byte[]?)GetSerializerMethod(nameof(UniversalSerializer.Serialize))
                    .MakeGenericMethod(type)
                    .Invoke(null, [deserialized, serializer, null]);
        }
        catch (Exception ex)
        {
            LogReserializationFailure(options, typeName, ex);
            return value;
        }
    }

    /// <summary>Resolves a CLR <see cref="Type"/> from a possibly assembly-qualified name, falling back to scanning loaded assemblies.</summary>
    /// <param name="typeName">The type name to resolve.</param>
    /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if it cannot be found.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses Type.GetType and Assembly.GetType to resolve types dynamically.")]
    internal static Type? ResolveType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        var fullNamePart = typeName.Split(',')[0].Trim();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var candidate = TryGetType(assembly, fullNamePart);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Converts a V10 tick value to a UTC <see cref="DateTimeOffset"/>, returning the current time for invalid inputs.</summary>
    /// <param name="ticks">The tick count from the V10 row.</param>
    /// <returns>The corresponding <see cref="DateTimeOffset"/>.</returns>
    internal static DateTimeOffset TicksToDateTimeOffset(long ticks)
    {
        if (ticks is <= 0 or < MinValidTicks)
        {
            return Clock.GetUtcNow();
        }

        try
        {
            return new(new(ticks, DateTimeKind.Utc));
        }
        catch
        {
            return Clock.GetUtcNow();
        }
    }

    /// <summary>Converts a V10 expiration tick value to a nullable <see cref="DateTimeOffset"/>, mapping zero or sentinel values to <c>null</c>.</summary>
    /// <param name="expirationTicks">The expiration tick count from the V10 row.</param>
    /// <returns>The expiration time, or <c>null</c> if the entry has no expiration.</returns>
    internal static DateTimeOffset? ConvertExpiration(long expirationTicks)
    {
        if (expirationTicks is <= 0 or < MinValidTicks)
        {
            return null;
        }

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
    /// Writes the migration sentinel entry into the V11 cache to prevent re-migration
    /// on further runs. Returns the upsert observable directly so it composes into the
    /// migration pipeline without an extra Task conversion.
    /// </summary>
    /// <param name="v11Cache">The V11 cache to mark as migrated.</param>
    /// <returns>A one-shot observable that fires when the sentinel is committed.</returns>
    internal static IObservable<RxVoid> WriteMigrationSentinel(SqliteBlobCache v11Cache)
    {
        var sentinel = new CacheEntry(
            MigrationSentinelKey,
            TypeName: null,
            Value: [],
            CreatedAt: Clock.GetUtcNow(),
            ExpiresAt: null);

        return v11Cache.Connection.Upsert([sentinel]);
    }

    /// <summary>Logs a failure that occurred while converting a single V10 entry.</summary>
    /// <param name="options">Migration options carrying the logger.</param>
    /// <param name="key">The key of the entry that failed.</param>
    /// <param name="ex">The exception raised during conversion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogConvertEntryFailure(V10MigrationOptions options, string key, Exception ex) =>
        options.Logger?.Invoke($"Failed to convert entry '{key}': {ex.Message}");

    /// <summary>Logs a failure that occurred while attempting to re-serialize a payload for a given type.</summary>
    /// <param name="options">Migration options carrying the logger.</param>
    /// <param name="typeName">The type name involved in re-serialization.</param>
    /// <param name="ex">The exception raised during re-serialization.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogReserializationFailure(V10MigrationOptions options, string? typeName, Exception ex) =>
        options.Logger?.Invoke($"Re-serialization failed for type '{typeName}': {ex.Message}. Keeping original bytes.");

    /// <summary>Attempts to delete the original V10 database file after migration, logging any failure.</summary>
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

    /// <summary>
    /// Converts one legacy row into the collected batch, reporting a failure instead of ending the
    /// migration. Every failure mode inside the conversion is already absorbed there, so nothing a
    /// database can hold reaches the catch — it stands guard over future conversion work rather
    /// than over a path a test can drive.
    /// </summary>
    /// <param name="row">The source V10 row.</param>
    /// <param name="serializer">The current serializer.</param>
    /// <param name="options">Migration options carrying the logger.</param>
    /// <param name="converted">The batch the converted entry is added to.</param>
    /// <returns>The number of rows that failed — 1 when this one did, otherwise 0, so callers can sum it.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static int ConvertRowInto(in V10LegacyRow row, ISerializer serializer, V10MigrationOptions options, List<CacheEntry> converted)
    {
        try
        {
            converted.Add(ConvertRow(row, serializer, options));
            return 0;
        }
        catch (Exception ex)
        {
            LogConvertEntryFailure(options, row.Key, ex);
            return 1;
        }
    }

    /// <summary>
    /// Asks one assembly whether it holds a type. An assembly that cannot surface its types simply
    /// does not hold this one; reaching that requires an assembly the runtime has loaded but cannot
    /// read, which no test can arrange, so the probe is excluded rather than left permanently unhit.
    /// </summary>
    /// <param name="assembly">The assembly to ask.</param>
    /// <param name="fullName">The full type name to look for.</param>
    /// <returns>The type, or <see langword="null"/> when this assembly does not hold it.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Calls Assembly.GetType to resolve a type by name.")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static Type? TryGetType(Assembly assembly, string fullName)
    {
        try
        {
            return assembly.GetType(fullName);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or TypeLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves the open generic <c>UniversalSerializer</c> overload that takes an explicit
    /// <see cref="DateTimeKind"/>. Selected by parameter count because both <c>Serialize</c> and
    /// <c>Deserialize</c> are overloaded, so looking either up by name alone is ambiguous.
    /// </summary>
    /// <param name="name">The method name to resolve.</param>
    /// <returns>The open generic method definition.</returns>
    /// <exception cref="MissingMethodException">Thrown when no such overload exists, which means the serializer surface changed.</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Reflects over UniversalSerializer to call its generic Serialize/Deserialize methods.")]
    private static MethodInfo GetSerializerMethod(string name) =>
        Array.Find(
            typeof(UniversalSerializer).GetMethods(BindingFlags.Public | BindingFlags.Static),
            x => x.Name == name && x.IsGenericMethodDefinition && x.GetParameters().Length == 3)
        ?? throw new MissingMethodException(typeof(UniversalSerializer).FullName, name);
}
