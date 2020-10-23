// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using Akavache.Sqlite3.Internal;

namespace Akavache.Sqlite3
{
    internal class BulkSelectByTypeSqliteOperation : BulkSelectSqliteOperation
    {
        public BulkSelectByTypeSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
            : base(conn, true, scheduler)
        {
        }
    }
}