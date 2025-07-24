// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.EncryptedSqlite3;

namespace ReactiveMarbles.CacheDatabase.Tests
{
    /// <summary>
    /// Tests for the <see cref="EncryptedSqliteBlobCache"/> class.
    /// </summary>
    public class EncryptedSqliteBlobCacheTests : BlobCacheTestsBase
    {
        /// <inheritdoc/>
        protected override IBlobCache CreateBlobCache(string path) =>
            new EncryptedSqliteBlobCache(Path.Combine(path, "test.db"), "test1234");
    }
}
