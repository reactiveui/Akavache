// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3.Internal;
using Splat;
using SQLitePCL;

namespace Akavache.Sqlite3
{
    internal static class SqliteOperationMixin
    {
        public static SQLite3.Result Checked(this IPreparedSqliteOperation connection, int sqlite3ErrorCode, string message = null)
        {
            var result = (SQLite3.Result)sqlite3ErrorCode;
            if (result == SQLite3.Result.OK || result == SQLite3.Result.Done || result == SQLite3.Result.Row)
            {
                return result;
            }

            var err = raw.sqlite3_errmsg(connection.Connection.Handle).utf8_to_string();
            var ex = new SQLiteException(result, (message ?? string.Empty) + ": " + err);

            connection.Log().Warn(ex, message);
            throw ex;
        }
    }
}
