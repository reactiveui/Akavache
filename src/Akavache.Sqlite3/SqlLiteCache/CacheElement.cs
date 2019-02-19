// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Akavache.Sqlite3.Internal;

namespace Akavache.Sqlite3
{
    internal class CacheElement
    {
        [PrimaryKey]
        public string Key { get; set; }

        [Indexed]
        public string TypeName { get; set; }

        public byte[] Value { get; set; }

        [Indexed]
        public DateTime Expiration { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}