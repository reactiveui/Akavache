// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests for the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    public class InMemoryBlobCacheInterfaceTests : BlobCacheInterfaceTestBase
    {
        /// <inheritdoc/>
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new InMemoryBlobCache(RxApp.TaskpoolScheduler);
        }
    }
}
