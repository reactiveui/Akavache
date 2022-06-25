// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3.Internal;

using SQLitePCL;

namespace Akavache.Sqlite3;

internal class InvalidateAllSqliteOperation : IPreparedSqliteOperation
{
    private readonly SQLiteConnection _connection;

    public InvalidateAllSqliteOperation(SQLiteConnection connection)
    {
        _connection = connection;
        Connection = connection;
    }

    public SQLiteConnection Connection { get; protected set; }

    public Action PrepareToExecute() => () => this.Checked(raw.sqlite3_exec(_connection.Handle, "DELETE FROM CacheElement"));

    public void Dispose()
    {
    }
}