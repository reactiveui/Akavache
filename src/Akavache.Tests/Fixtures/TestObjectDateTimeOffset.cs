// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace Akavache.Tests
{
    /// <summary>
    /// Test object for doing DateTimeOffset tests.
    /// </summary>
    public class TestObjectDateTimeOffset
    {
        /// <summary>
        /// Gets or sets a timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a nullable timestamp.
        /// </summary>
        public DateTimeOffset? TimestampNullable { get; set; }
    }
}
