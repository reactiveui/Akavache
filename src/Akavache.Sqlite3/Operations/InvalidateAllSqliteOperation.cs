// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLite;
using SQLitePCL;

namespace Akavache.Sqlite3;

internal class InvalidateAllSqliteOperation(SQLiteConnection connection) : IPreparedSqliteOperation
{
    private readonly SQLiteConnection _connection = connection;
    private bool _disposedValue;

    public SQLiteConnection Connection { get; protected set; } = connection;

    public Action PrepareToExecute() => () => this.Checked(raw.sqlite3_exec(_connection.Handle, "DELETE FROM CacheElement"));

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _connection.Dispose();
            }

            _disposedValue = true;
        }
    }
}
