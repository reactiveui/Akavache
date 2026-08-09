// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests;
#else
namespace Akavache.Settings.Tests;
#endif

/// <summary>Enumeration values stored and round-tripped by the settings test fixtures.</summary>
public enum EnumTestValue
{
    /// <summary>The default.</summary>
    Default = 0,

    /// <summary>The option1.</summary>
    Option1 = 1,

    /// <summary>The option2.</summary>
    Option2 = 2,

    /// <summary>The option3.</summary>
    Option3 = 3,

    /// <summary>The option4.</summary>
    Option4 = 4,
}
