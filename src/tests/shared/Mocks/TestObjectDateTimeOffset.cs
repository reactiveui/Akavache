// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Mocks;
#else
namespace Akavache.Tests.Mocks;
#endif

/// <summary>Test object for doing DateTimeOffset tests.</summary>
public class TestObjectDateTimeOffset
{
    /// <summary>Gets or sets a timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets a nullable timestamp.</summary>
    public DateTimeOffset? TimestampNullable { get; set; }
}
