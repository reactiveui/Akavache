// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Akavache.Settings.Core;

/// <summary>
/// Provides a base class for implementing type-safe application settings storage using Akavache.
/// This abstract class manages settings persistence, caching, and property change notifications.
/// </summary>
public abstract class SettingsStorage : ISettingsStorage
{
    private readonly IBlobCache _blobCache;
    private readonly Dictionary<string, object?> _cache;
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly string _keyPrefix;
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStorage"/> class.
    /// </summary>
    /// <param name="keyPrefix">The prefix used for all settings keys in the blob cache. Should be unique to avoid key collisions.</param>
    /// <param name="cache">The blob cache implementation where settings will be stored and retrieved.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPrefix"/> is null, empty, or whitespace.</exception>
    protected SettingsStorage(string keyPrefix, IBlobCache cache)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            throw new ArgumentException("Invalid key prefix", nameof(keyPrefix));
        }

        _keyPrefix = keyPrefix;
        _blobCache = cache;

        _cache = [];
        _cacheLock = new();
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes all settings properties by loading them from storage or setting them to their default values.
    /// This method uses reflection to enumerate properties and is useful for preloading settings at startup.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
#endif
    public Task InitializeAsync() =>
        Task.Run(() =>
            {
                foreach (var property in GetType().GetRuntimeProperties())
                {
                    property.GetValue(this);
                }
            });

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    public ValueTask DisposeAsync()
    {
        var result = _blobCache.DisposeAsync();
        GC.SuppressFinalize(this);
        return result;
    }

    /// <summary>
    /// Gets the value for the specified key from cache or storage, or creates and stores the default value if not found.
    /// This method provides efficient property access with automatic caching and storage persistence.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="defaultValue">The default value to use if no stored value exists.</param>
    /// <param name="key">The property name, automatically inferred from the calling member.</param>
    /// <returns>The setting value from cache, storage, or the provided default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("GetOrCreate requires types to be preserved for serialization.")]
    [RequiresDynamicCode("GetOrCreate requires types to be preserved for serialization.")]
#endif
    protected T? GetOrCreate<T>(T defaultValue, [CallerMemberName] string? key = null)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _cacheLock.EnterReadLock();

        try
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return (T?)value;
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return _blobCache.GetOrCreateObject($"{_keyPrefix}:{key}", () => defaultValue)
            .Do(x => AddToInternalCache(key, x)).Wait();
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed, automatically inferred from the calling member.</param>
    protected void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Sets or creates a setting value, updating both the in-memory cache and persistent storage.
    /// This method provides efficient property setting with automatic persistence and change notification.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="value">The value to store for this setting.</param>
    /// <param name="key">The property name, automatically inferred from the calling member.</param>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("SetOrCreate requires types to be preserved for serialization.")]
    [RequiresDynamicCode("SetOrCreate requires types to be preserved for serialization.")]
#endif
    protected void SetOrCreate<T>(T value, [CallerMemberName] string? key = null)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        AddToInternalCache(key, value);

        // Fire and forget, we retrieve the value from the in-memory cache from now on
        _blobCache.InsertObject($"{_keyPrefix}:{key}", value).Subscribe();

        OnPropertyChanged(key);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cacheLock.Dispose();
                _blobCache.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    private void AddToInternalCache(string key, object? value)
    {
        _cacheLock.EnterWriteLock();

        _cache[key] = value;

        _cacheLock.ExitWriteLock();
    }
}
