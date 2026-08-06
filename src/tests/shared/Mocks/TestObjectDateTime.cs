// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests.Mocks;

/// <summary>
/// A fixture for when testing DateTime based tests. Deliberately kept assembly-internal:
/// the whole point of the fixture is to carry a bare <see cref="DateTime"/> with no offset,
/// which is exactly what must not appear on a type other assemblies can bind to.
/// </summary>
/// <remarks>
/// The properties must stay public. This fixture is round-tripped through System.Text.Json,
/// which ignores a non-public property without reporting anything, so narrowing them to match
/// the type's accessibility makes every round trip yield an empty object and the assertions
/// fail somewhere far away from the cause.
/// </remarks>
internal sealed class TestObjectDateTime
{
    /// <summary>Gets or sets the time stamp.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Gets or sets a nullable time stamp.</summary>
    public DateTime? TimestampNullable { get; set; }
}
