// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using Akavache.Sqlite3;

namespace Akavache.Tests
{
    /// <summary>
    /// DateTime operation tests for the <see cref="SqlRawPersistentBlobCache"/> class.
    /// </summary>
    public class SqliteBlobCacheDateTimeTests : BulkOperationsTestBase
    {
        /// <inheritdoc />
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new SqlRawPersistentBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }
}
