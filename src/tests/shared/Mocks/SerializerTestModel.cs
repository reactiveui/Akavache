// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Mocks;
#else
namespace Akavache.Tests.Mocks;
#endif

/// <summary>Test model for serializer tests.</summary>
public class SerializerTestModel
{
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the value.</summary>
    public int Value { get; set; }
}
