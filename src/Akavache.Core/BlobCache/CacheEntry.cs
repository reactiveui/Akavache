// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A entry in a memory cache.
/// </summary>
public class CacheEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry"/> class.
    /// </summary>
    /// <param name="typeName">The name of the type being stored.</param>
    /// <param name="value">The value being stored.</param>
    /// <param name="createdAt">The date and time the entry was created.</param>
    /// <param name="expiresAt">The date and time when the entry expires.</param>
    public CacheEntry(string? typeName, byte[] value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        TypeName = typeName;
        Value = value;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Gets or sets the date and time when the entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>
    /// Gets or sets the date and time when the entry will expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; protected set; }

    /// <summary>
    /// Gets or sets the type name of the entry.
    /// </summary>
    public string? TypeName { get; protected set; }

    /// <summary>
    /// Gets or sets the value of the entry.
    /// </summary>
    public byte[] Value { get; protected set; }
}
