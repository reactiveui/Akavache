// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3.Internal;

namespace Akavache.Sqlite3;

// NB: This just makes OperationQueue's life easier by giving it a type
// name.
internal class BulkInvalidateByTypeSqliteOperation : BulkInvalidateSqliteOperation
{
    public BulkInvalidateByTypeSqliteOperation(SQLiteConnection conn)
        : base(conn, true)
    {
    }
}