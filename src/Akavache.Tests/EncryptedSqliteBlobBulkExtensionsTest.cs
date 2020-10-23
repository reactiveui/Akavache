// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Akavache.Sqlite3;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests bulk extensions.
    /// </summary>
    public class EncryptedSqliteBlobBulkExtensionsTest : BlobCacheExtensionsTestBase
    {
        /// <inheritdoc/>
        protected override IBlobCache CreateBlobCache(string path)
        {
            BlobCache.ApplicationName = "TestRunner";
            return new BlockingDisposeBulkCache(new SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
        }
    }
}
