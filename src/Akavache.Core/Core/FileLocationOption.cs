// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>File Location Option.</summary>
public enum FileLocationOption
{
    /// <summary>Use the default location for the platform.</summary>
    Default = 0,

    /// <summary>Use the legacy location, if available on the platform.</summary>
    Legacy = 1,
}
