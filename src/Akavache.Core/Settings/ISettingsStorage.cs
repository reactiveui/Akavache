// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Akavache.Settings;

/// <summary>
/// Interface for SettingsStorage.
/// </summary>
/// <seealso cref="INotifyPropertyChanged" />
/// <seealso cref="IDisposable" />
/// <seealso cref="IAsyncDisposable" />
public interface ISettingsStorage : INotifyPropertyChanged, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Loads every setting in this storage into the internal cache, or, if the value doesn't
    /// exist in the storage, initializes it with its default value. You dont HAVE to call this
    /// method, but it's handy for applications with a high number of settings where you want to
    /// load all settings on startup at once into the internal cache and not one-by-one at each request.
    /// </summary>
    /// <returns>A Task.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
#endif
    Task InitializeAsync();
}
