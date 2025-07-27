// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.Design;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Settings.Core;
#if ENCRYPTED
using ReactiveMarbles.CacheDatabase.EncryptedSettings;
#endif

namespace ReactiveMarbles.CacheDatabase.Settings;

/// <summary>
/// Empty Base.
/// </summary>
public abstract class SettingsBase : SettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    protected SettingsBase(string className)
        : base($"__{className}__", AppInfo.BlobCaches[className]!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <param name="serializer">The serializer.</param>
    protected SettingsBase(string className, ISerializer serializer)
        : base($"__{className}__", AppInfo.BlobCaches[className]!, serializer)
    {
    }
}
