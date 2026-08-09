// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary with Newtonsoft.Json serialization.
/// Use it for testing / mocking purposes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCache" /> class.
/// </remarks>
/// <seealso cref="InMemoryBlobCacheBase" />
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
/// <param name="serializer">The serializer to use for serializing and deserializing data.</param>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class InMemoryBlobCache(ISequencer scheduler, ISerializer? serializer) : InMemoryBlobCacheBase(scheduler, serializer)
{
    /// <summary>Initializes a new instance of the <see cref="InMemoryBlobCache" /> class with default scheduler.</summary>
    /// <param name="serialzerType">Type of the serialzer.</param>
    public InMemoryBlobCache(string serialzerType)
        : this(
            CacheDatabase.TaskpoolScheduler,
            ArgumentValidation.EnsureNotNull(
                AppLocator.Current.GetService<ISerializer>(contract: serialzerType),
                "No default serializer available. Please ensure Akavache.SystemTextJson is referenced."))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.</summary>
    /// <param name="serializer">The serializer to use for object serialization/deserialization.</param>
    public InMemoryBlobCache(ISerializer serializer)
        : this(CacheDatabase.TaskpoolScheduler, ArgumentValidation.EnsureNotNull(serializer))
    {
    }
}
