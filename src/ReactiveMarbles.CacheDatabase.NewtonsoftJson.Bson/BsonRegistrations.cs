// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// Automatic registration for BSON serializer to ensure Akavache compatibility.
/// </summary>
public static class BsonRegistrations
{
    private static readonly object _lock = new();
    private static bool _registered;

    /// <summary>
    /// Ensures that the BSON serializer is registered as the default if no other serializer is set.
    /// This provides maximum compatibility with Akavache.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (_lock)
        {
            if (_registered)
            {
                return;
            }

            // Only set BSON as default if no serializer is currently set
            // This allows other packages to take precedence if they're explicitly initialized
            CoreRegistrations.Serializer ??= new NewtonsoftBsonSerializer();

            _registered = true;
        }
    }

    /// <summary>
    /// Forces BSON serializer to be set as the default, overriding any existing serializer.
    /// Use this for maximum Akavache compatibility.
    /// </summary>
    public static void ForceRegister()
    {
        lock (_lock)
        {
            CoreRegistrations.Serializer = new NewtonsoftBsonSerializer();
            _registered = true;
        }
    }
}
