// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3.Internal;

using Splat;

namespace Akavache.Sqlite3;

/// <summary>
/// A SQL operation connection.
/// </summary>
internal interface IPreparedSqliteOperation : IEnableLogger, IDisposable
{
    /// <summary>
    /// Gets the connection to the SQLite database.
    /// </summary>
    SQLiteConnection Connection { get; }
}