// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;

namespace Akavache.Tests
{
    /// <summary>
    /// Encrypted tests for the <see cref="Sqlite3.SQLiteEncryptedBlobCache"/> class.
    /// </summary>
    public class EncryptedSqliteBlobCacheBulkOperationsTests : BulkOperationsTestBase
    {
        /// <inheritdoc />
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new BlockingDisposeBulkCache(new Akavache.Sqlite3.SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }
}
