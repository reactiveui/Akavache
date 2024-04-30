// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;
using SQLite;
using SQLitePCL;

namespace Akavache.Sqlite3;

internal static class SqliteOperationMixin
{
    public static SQLite3.Result Checked(this IPreparedSqliteOperation connection, int sqlite3ErrorCode, string? message = null)
    {
        var result = (SQLite3.Result)sqlite3ErrorCode;
        if (result is SQLite3.Result.OK or SQLite3.Result.Done or SQLite3.Result.Row)
        {
            return result;
        }

        var err = raw.sqlite3_errmsg(connection.Connection.Handle).utf8_to_string();
        var ex = SQLiteException.New(result, (message ?? string.Empty) + ": " + err);

        connection.Log().Warn(ex, message);
        throw ex;
    }
}
