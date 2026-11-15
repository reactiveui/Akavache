// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif
#else
#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif
#endif

/// <summary>Validated file and serialization options for a SQLite cache.</summary>
/// <param name="FilePath">The database file path.</param>
/// <param name="Serializer">The configured serializer instance.</param>
/// <param name="ForcedDateTimeKind">The optional date kind applied to the cache.</param>
internal readonly record struct SqliteCacheOptions(string FilePath, ISerializer Serializer, DateTimeKind? ForcedDateTimeKind);
