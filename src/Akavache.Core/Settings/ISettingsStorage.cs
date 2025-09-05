// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Akavache.Settings;

/// <summary>
/// Represents a storage interface for application settings that supports property change notifications and asynchronous operations.
/// </summary>
/// <seealso cref="INotifyPropertyChanged" />
/// <seealso cref="IDisposable" />
/// <seealso cref="IAsyncDisposable" />
public interface ISettingsStorage : INotifyPropertyChanged, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Loads all settings in this storage into the internal cache, initializing missing values with their defaults.
    /// While calling this method is optional, it is useful for applications with many settings where you want to
    /// load all settings at startup rather than loading them individually on first access.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
#endif
    Task InitializeAsync();
}
